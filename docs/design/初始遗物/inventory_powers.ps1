param([ValidateSet('stable','beta')][string]$Variant='beta')
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$version=if($Variant -eq 'stable') {'0.107.1'} else {'0.111.0'}
$deps=Join-Path $workspace "编译依赖\$Variant-$version"
foreach($name in @('GodotSharp.dll','0Harmony.dll','sts2.dll')) { [void][Reflection.Assembly]::LoadFrom((Join-Path $deps $name)) }
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $deps 'sts2.dll'))
$powerType=$assembly.GetType('MegaCrit.Sts2.Core.Models.PowerModel',$true)
$rows=@()
foreach($type in $assembly.GetTypes() | Where-Object { !$_.IsAbstract -and $_.IsSubclassOf($powerType) } | Sort-Object Name) {
    # Pure metadata getters on uninitialized fixtures. No game, save, battle,
    # power application, or gameplay code modification.
    $instance=[Runtime.CompilerServices.RuntimeHelpers]::GetUninitializedObject($type)
    $row=[ordered]@{class=$type.Name;fullName=$type.FullName;base=$type.BaseType.Name}
    foreach($property in @('Type','StackType','InstanceType','AllowNegative')) {
        try { $row[$property]=[string]$type.GetProperty($property).GetValue($instance) }
        catch { throw "Cannot classify $($type.Name).$property : $($_.Exception.GetBaseException().Message)" }
    }
    $rows += $row
}
$out=Join-Path $PSScriptRoot "原版分析\powers-$Variant-$version.json"
$rows | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output "POWER_INVENTORY_OK variant=$Variant total=$($rows.Count) buffs=$(@($rows|Where-Object Type -eq Buff).Count) file=$out"
