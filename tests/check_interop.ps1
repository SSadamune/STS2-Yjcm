param([ValidateSet('stable','beta')][string]$Variant)
$ErrorActionPreference='Stop'
$dev=Split-Path $PSScriptRoot -Parent
$workspace=$dev
$version=if($Variant -eq 'stable') {'0.107.1'} else {'0.111.0'}
$source=Get-Content -LiteralPath (Join-Path $workspace 'mod\src\Interop\PublicApi.cs') -Raw
if($source -match '#if GAME_BETA') { throw 'The JSON public API must be version-independent' }
foreach($file in @('Interop\YjcmPublicApi.cs','Interop\SunqianUniverseInterop.cs','Settings\WuZhugeliangSettings.cs','Visuals\CardEffectVfx.cs','Visuals\ShiWeiYanVfx.cs','Monsters\MouSimaYiSpineVisuals.cs')) {
    if((Get-Content -LiteralPath (Join-Path $workspace "mod\src\$file") -Raw) -match '#if GAME_BETA') { throw "Shared API/settings/visual must be version-independent: $file" }
}
# Only interface signatures are public; the partner implementation remains local/ignored.
$contract = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'fixtures/sunqian-api-signatures.json') -Raw | ConvertFrom-Json
$proxy = Get-Content -LiteralPath (Join-Path $workspace 'mod\src\Interop\SunqianUniverseInterop.cs') -Raw
$pattern = 'public static (?:async\s+)?(?<return>[\w?<>]+) (?<name>\w+)\((?<args>[^)]*)\)'
$remoteSignatures = @{}
foreach($method in $contract.methods.PSObject.Properties) { $remoteSignatures[$method.Name] = $method.Value }
$reverseChecks = 0
foreach($m in [regex]::Matches($proxy,$pattern)) {
    $name=$m.Groups['name'].Value
    $signature=$m.Groups['return'].Value + '(' + ($m.Groups['args'].Value -replace '\s','') + ')'
    if($remoteSignatures[$name] -ne $signature) { throw "Sunqian commit signature mismatch: $name" }
    $reverseChecks++
}
if($reverseChecks -ne 16 -or $contract.commit -ne '5c631501' -or $contract.isReady -ne $true -or $contract.type -ne 'Squ.Api.SunqianUniversePublicApi, sunqian-universe' -or $proxy -notmatch 'Squ.Api.SunqianUniversePublicApi, sunqian-universe') { throw 'Sunqian proxy target/property drift' }
Write-Output "SUNQIAN_SIGNATURES_OK methods=$reverseChecks commit=5c631501"
$dreamInterop=Get-Content -LiteralPath (Join-Path $workspace 'mod\src\Interop\YiZhenQingMengInterop.cs') -Raw
foreach($required in @(
    'IsModReady && SunqianUniverseInterop.IsSupportingActorCharacter(player?.Character)',
    'CombatManager.Instance is { IsInProgress: true }',
    'SunqianUniverseInterop.CreateBurningHoverTip()',
    'SunqianUniverseInterop.ApplyBurning(',
    'target.Powers?.FirstOrDefault(SunqianUniverseInterop.IsBurningPower)',
    'HoverTipFactory.FromPower<HuoGongPower>()'
)) {
    if(-not $dreamInterop.Contains($required)) { throw "YiZhenQingMeng interop branch missing: $required" }
}
$dreamCard=Get-Content -LiteralPath (Join-Path $workspace 'mod\src\Cards\YiZhenQingMeng.cs') -Raw
foreach($required in @(
    'YiZhenQingMengInterop.EffectHoverTips(this)',
    'YiZhenQingMengInterop.LibraryHoverTips()',
    'YiZhenQingMengInterop.AddEffectText(description, this)',
    'YiZhenQingMengInterop.ApplyEffect(',
    'YiZhenQingMengInterop.FindEffect(enemy, useBurning)'
)) {
    if(-not $dreamCard.Contains($required)) { throw "YiZhenQingMeng card integration missing: $required" }
}
$dreamPower=Get-Content -LiteralPath (Join-Path $workspace 'mod\src\Powers\YiZhenQingMengPower.cs') -Raw
if(-not $dreamPower.Contains('YiZhenQingMengInterop.Affects(Owner.Player)') -or
   -not $dreamPower.Contains('YiZhenQingMengInterop.FindEffect(enemy, useBurning)') -or
   -not $dreamPower.Contains('PowerCmd.Remove(effect)')) {
    throw 'YiZhenQingMeng end-turn interop cleanup drift'
}
$cardsLoc=Get-Content -LiteralPath (Join-Path $workspace 'mod\Yjcm\localization\zhs\cards.json') -Raw | ConvertFrom-Json
$dreamDescription=$cardsLoc.'YJCM_CARD_YI_ZHEN_QING_MENG.description'
if(([regex]::Matches($dreamDescription,'\{DreamFireText\}').Count -ne 4) -or
   $cardsLoc.'YJCM_CARD_YI_ZHEN_QING_MENG.fireAttackInlineText' -notmatch '火攻' -or
   $cardsLoc.'YJCM_CARD_YI_ZHEN_QING_MENG.burningInlineText' -notmatch '灼烧' -or
   $cardsLoc.'YJCM_CARD_YI_ZHEN_QING_MENG.interopLibraryHover' -notmatch '所有.*火攻.*灼烧') {
    throw 'YiZhenQingMeng dynamic localization drift'
}
$project=Join-Path $PSScriptRoot 'InteropTests\InteropTests.csproj'
$buildLog=Join-Path $PSScriptRoot "interop-$Variant.log"
& dotnet build $project -c Release "-p:GameApiVariant=$Variant" -v quiet *> $buildLog
if($LASTEXITCODE -ne 0) { Get-Content -LiteralPath $buildLog -Tail 35; throw "Interop consumer build failed: $Variant" }
& dotnet (Join-Path $dev "artifacts/bin/InteropTests/$Variant/Release/net9.0/InteropTests.dll") (Join-Path $dev "artifacts\staged\Yjcm.$version.dll") (Join-Path $workspace "dependencies\$Variant-$version")
if($LASTEXITCODE -ne 0) { throw "Interop contract failed: $Variant" }
