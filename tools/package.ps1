param([string]$GodotPath)
. (Join-Path $PSScriptRoot 'config.ps1')
$repo = $script:RepositoryRoot
$fingerprint=Get-YjcmSourceFingerprint
foreach($v in @('stable','beta')) {
    $version=if($v -eq 'stable'){'0.107.1'}else{'0.111.0'}
    $stamp=Get-Content -LiteralPath (Join-Path $repo "artifacts/staged/$v.build.json") -Raw | ConvertFrom-Json
    if($stamp.sourceFingerprint -ne $fingerprint -or $stamp.dllSha256 -ne (Get-FileHash -LiteralPath (Join-Path $repo "artifacts/staged/Yjcm.$version.dll")).Hash) { throw "Stale or modified build: $v. Run tools/build.ps1 first." }
}
if (-not $GodotPath) { $GodotPath = (Get-YjcmConfiguration).GodotPath }
if (-not $GodotPath -or -not (Test-Path -LiteralPath $GodotPath)) { throw 'Set GodotPath in build.local.json or pass -GodotPath (Godot 4.5.1 Mono).' }
$dist = Join-Path $repo 'artifacts/dist/yjcm'
New-Item -ItemType Directory -Path $dist -Force | Out-Null
& $GodotPath --headless --editor --path (Join-Path $repo 'mod') --quit *> (Join-Path $repo 'artifacts/logs/godot-import.log')
Assert-YjcmNativeExit 'Godot import failed'
& $GodotPath --headless --path (Join-Path $repo 'mod') --export-pack 'Windows Desktop' (Join-Path $dist 'Yjcm.pck') *> (Join-Path $repo 'artifacts/logs/godot-export.log')
Assert-YjcmNativeExit 'Godot export failed'
& node (Join-Path $repo 'tools/normalize-pack.mjs') (Join-Path $dist 'Yjcm.pck')
Assert-YjcmNativeExit 'Unified PCK normalization failed'
foreach ($name in @('Yjcm.dll','Yjcm.0.107.1.dll','Yjcm.0.111.0.dll')) {
    Copy-Item -LiteralPath (Join-Path $repo "artifacts/staged/$name") -Destination (Join-Path $dist $name) -Force
}
Copy-Item -LiteralPath (Join-Path $repo 'mod/mod_manifest.json') -Destination (Join-Path $dist 'mod_manifest.json') -Force
foreach ($folder in @('Voice','Spine','MeshAnimation')) {
    $source = Join-Path $repo "assets/loose/$folder"
    New-Item -ItemType Directory -Path (Join-Path $dist $folder) -Force | Out-Null
    Get-ChildItem -LiteralPath $source -Force | Copy-Item -Destination (Join-Path $dist $folder) -Recurse -Force
}
New-Item -ItemType Directory -Path (Join-Path $dist '联动接口') -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $repo 'docs/interop') -Force | Copy-Item -Destination (Join-Path $dist '联动接口') -Recurse -Force
& node (Join-Path $repo 'tests/audit_packs.mjs')
Assert-YjcmNativeExit 'PCK audit failed'
& pwsh -NoProfile -File (Join-Path $repo 'tests/check_delivery.ps1')
Assert-YjcmNativeExit 'Package delivery audit failed'
Write-Output "PACKAGE_OK destination=$dist"
