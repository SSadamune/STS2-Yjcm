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
$project=Join-Path $PSScriptRoot 'InteropTests\InteropTests.csproj'
$buildLog=Join-Path $PSScriptRoot "interop-$Variant.log"
& dotnet build $project -c Release "-p:GameApiVariant=$Variant" -v quiet *> $buildLog
if($LASTEXITCODE -ne 0) { Get-Content -LiteralPath $buildLog -Tail 35; throw "Interop consumer build failed: $Variant" }
& dotnet (Join-Path $dev "artifacts/bin/InteropTests/$Variant/Release/net9.0/InteropTests.dll") (Join-Path $dev "artifacts\staged\Yjcm.$version.dll") (Join-Path $workspace "dependencies\$Variant-$version")
if($LASTEXITCODE -ne 0) { throw "Interop contract failed: $Variant" }
