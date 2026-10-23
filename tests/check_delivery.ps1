param([switch]$CheckSteam,[string]$GamePath)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
. (Join-Path $repo 'tools/config.ps1')
$fingerprint=Get-YjcmSourceFingerprint
foreach($v in @('stable','beta')) {
    $version=if($v -eq 'stable'){'0.107.1'}else{'0.111.0'}
    $stamp=Get-Content -LiteralPath (Join-Path $repo "artifacts/staged/$v.build.json") -Raw | ConvertFrom-Json
    if($stamp.sourceFingerprint -ne $fingerprint -or -not $stamp.testsPassed -or $stamp.dllSha256 -ne (Get-FileHash -LiteralPath (Join-Path $repo "artifacts/staged/Yjcm.$version.dll")).Hash) { throw "Stale, unchecked or modified build: $v" }
}
if ($CheckSteam) {
    if (-not $GamePath) {
        $config=Get-Content -LiteralPath (Join-Path $repo 'build.local.json') -Raw | ConvertFrom-Json
        $GamePath=$config.GamePath
    }
    if (-not $GamePath) { throw 'GamePath required for Steam audit' }
    $target=[IO.Path]::GetFullPath((Join-Path $GamePath 'mods/yjcm'))
} else { $target=Join-Path $repo 'artifacts/dist/yjcm' }
function Assert-SameFile([string]$a,[string]$b) {
    if ((Get-FileHash -LiteralPath $a).Hash -ne (Get-FileHash -LiteralPath $b).Hash) { throw "Content mismatch: $a <> $b" }
}
foreach ($name in @('Yjcm.dll','Yjcm.0.107.1.dll','Yjcm.0.111.0.dll')) {
    Assert-SameFile (Join-Path $repo "artifacts/staged/$name") (Join-Path $target $name)
}
Assert-SameFile (Join-Path $repo 'mod/mod_manifest.json') (Join-Path $target 'mod_manifest.json')
if (([Reflection.AssemblyName]::GetAssemblyName((Join-Path $target 'Yjcm.dll'))).Name -ne 'Yjcm.Loader') { throw 'Loader assembly identity changed' }
foreach ($version in @('0.107.1','0.111.0')) {
    if (([Reflection.AssemblyName]::GetAssemblyName((Join-Path $target "Yjcm.$version.dll"))).Name -ne 'Yjcm') { throw 'Content assembly identity changed' }
}
$manifest=Get-Content -LiteralPath (Join-Path $target 'mod_manifest.json') -Raw | ConvertFrom-Json
if ($manifest.id -ne 'Yjcm') { throw 'Wrong ModId' }
if ($CheckSteam) { Assert-SameFile (Join-Path $repo 'artifacts/dist/yjcm/Yjcm.pck') (Join-Path $target 'Yjcm.pck') }
if (-not (Test-Path -LiteralPath (Join-Path $target 'Yjcm.pck'))) { throw 'PCK missing' }
foreach($retired in Get-YjcmRetiredRuntimePaths $target) {
    if(Test-Path -LiteralPath $retired){throw "Retired attack prototype remains: $retired"}
}
$count=0
foreach ($folder in @('Voice','Spine','MeshAnimation')) {
    $sourceRoot=Join-Path $repo "assets/loose/$folder"
    foreach ($file in Get-ChildItem -LiteralPath $sourceRoot -File -Recurse -Force) {
        $relative=[IO.Path]::GetRelativePath($sourceRoot,$file.FullName)
        Assert-SameFile $file.FullName (Join-Path $target "$folder/$relative")
        $count++
    }
}
$docRoot=Join-Path $repo 'docs/interop'
foreach ($file in Get-ChildItem -LiteralPath $docRoot -File -Recurse) {
    Assert-SameFile $file.FullName (Join-Path $target ('联动接口/' + [IO.Path]::GetRelativePath($docRoot,$file.FullName)))
}
foreach ($legacy in @('WuZhugeliang.dll','WuZhugeliang.pck')) {
    if (Test-Path -LiteralPath (Join-Path $target $legacy)) { throw "Legacy runtime remains: $legacy" }
}
Write-Output "DELIVERY_OK target=$target looseFiles=$count unified=stable+beta+loader+PCK"
