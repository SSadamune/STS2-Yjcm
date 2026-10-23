param([ValidateSet('stable','beta')][string]$Variant)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$version=if($Variant -eq 'stable'){'0.107.1'}else{'0.111.0'}
foreach($name in @('GodotSharp','0Harmony','sts2','STS2-RitsuLib')) {
    [void][Reflection.Assembly]::LoadFrom((Join-Path $repo "dependencies/$Variant-$version/$name.dll"))
}
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo "artifacts/staged/Yjcm.$version.dll"))
$flags=[Reflection.BindingFlags]'Static,Instance,Public,NonPublic'
$script:checks=0
function Require($condition,[string]$message) {
    if(!$condition){throw $message}
    $script:checks++
}
function Mutable($model) {
    [MegaCrit.Sts2.Core.Models.AbstractModel].GetProperty('IsMutable',$flags).SetValue($model,$true)
    return $model
}
$powerType=[MegaCrit.Sts2.Core.Models.PowerModel]
function Power($type,$owner) {
    $power=Mutable ([Activator]::CreateInstance($type))
    $powerType.GetField('_owner',$flags).SetValue($power,$owner)
    $powerType.GetField('_amount',$flags).SetValue($power,3)
    return $power
}
$owner=[MegaCrit.Sts2.Core.Entities.Creatures.Creature]::new([MegaCrit.Sts2.Core.Entities.Players.Player]$null,100,100)
$attack=Mutable ([Activator]::CreateInstance($assembly.GetType('Yjcm.SunQianShuiHuoWuDi',$true)))
$ability=Mutable ([Activator]::CreateInstance($assembly.GetType('Yjcm.XinSanGuoShuiHuoJianRong',$true)))
Require ($attack.Type -eq 'Attack' -and $ability.Type -eq 'Power') 'Incorrect card types'
foreach($card in @($attack,$ability)) {
    Require ($card.Rarity -eq 'Uncommon' -and $card.EnergyCost.Canonical -eq 2) 'Both cards must be two-cost Uncommon'
    $attr=$card.GetType().GetCustomAttributesData()|Where-Object {$_.AttributeType.Name -eq 'RegisterCardAttribute'}
    Require ($attr.ConstructorArguments[0].Value.Name -eq 'WuZhugeliangCardPool') 'Card must register only with Wu Zhugeliang'
    Require ($null -eq $assembly.GetType("Yjcm.WeiDongZhuo$($card.GetType().Name)")) 'New collaboration card must not be cloned'
}
Require ($attack.DynamicVars.Damage.BaseValue -eq 7) 'Base damage must be seven'
$attack.GetType().GetMethod('OnUpgrade',$flags).Invoke($attack,@())
Require ($attack.DynamicVars.Damage.BaseValue -eq 11) 'Upgraded damage must be eleven'
$ability.GetType().GetMethod('OnUpgrade',$flags).Invoke($ability,@())
Require ($ability.EnergyCost.GetResolved() -eq 1) 'Upgraded ability must cost one'
$reversalType=$assembly.GetType('Yjcm.ShuiHuoJianRongPower',$true)
$reversal=Power $reversalType $owner
Require ($reversal.StackType -eq 'None') 'Reversal must not multiply when replayed'
Require ($reversalType.GetMethod('AfterSideTurnEnd',$flags).DeclaringType -ne $reversalType) 'Reversal must not change native duration ticks'

# Run actual Harmony detours under the game's .NET 9, not PowerShell's .NET 10.
$testProject=Join-Path $PSScriptRoot 'CollaborationTests/CollaborationTests.csproj'
$testLog=Join-Path $repo "artifacts/logs/collaboration-$Variant.log"
& dotnet build $testProject -c Release "-p:GameApiVariant=$Variant" -v quiet *> $testLog
if($LASTEXITCODE -ne 0){Get-Content -LiteralPath $testLog -Tail 35;throw 'Collaboration test build failed'}
& dotnet (Join-Path $repo "artifacts/bin/CollaborationTests/$Variant/Release/net9.0/CollaborationTests.dll") (Join-Path $repo "artifacts/staged/Yjcm.$version.dll") (Join-Path $repo "dependencies/$Variant-$version")
if($LASTEXITCODE -ne 0){throw "Native collaboration mechanics failed: $Variant"}
$cards=Get-Content -LiteralPath (Join-Path $repo 'mod/Yjcm/localization/zhs/cards.json') -Raw|ConvertFrom-Json
$powers=Get-Content -LiteralPath (Join-Path $repo 'mod/Yjcm/localization/zhs/powers.json') -Raw|ConvertFrom-Json
Require ($cards.'YJCM_HOVER_COLLABORATION.title' -eq '联动') 'Collaboration hover title missing'
Require ($cards.'YJCM_HOVER_COLLABORATION_SUN_QIAN.description' -eq '本牌来自于新三国孙乾mod') 'Sunqian attribution mismatch'
Require ($cards.'YJCM_HOVER_COLLABORATION_XIN_SAN_GUO.description' -eq '本牌来自于新三国首播版mod') 'Newsanguo attribution mismatch'
Require ($null -ne $powers.'YJCM_POWER_SHUI_HUO_JIAN_RONG_POWER.description') 'Reversal power localization missing'
$powerEntry=[STS2RitsuLib.Content.ModContentRegistry]::GetFixedPublicEntry('Yjcm',$reversalType)
Require ($powerEntry -eq 'YJCM_POWER_SHUI_HUO_JIAN_RONG_POWER') 'Native registration-derived power ID differs from localization'
Require ($null -ne $powers."$powerEntry.description") 'Actual registered power description missing'
foreach($card in @($attack,$ability)) {
    $cardEntry=[STS2RitsuLib.Content.ModContentRegistry]::GetFixedPublicEntry('Yjcm',$card.GetType())
    Require ($null -ne $cards."$cardEntry.title") 'Actual registered card title missing'
    $source=Get-Content -LiteralPath (Join-Path $repo "mod/src/Cards/$($card.GetType().Name).cs") -Raw
    Require ($source.Contains('CollaborationHoverTip.For(') -and $source.Contains('VoicePlayer.PlayWav(')) 'Card hover/voice must be wired'
}
Write-Output "COLLABORATION_CARDS_OK variant=$Variant assertions=$script:checks"
