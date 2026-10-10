param([string]$GamePath)
. (Join-Path $PSScriptRoot 'config.ps1')
if (-not $GamePath) { $GamePath = (Get-YjcmConfiguration).GamePath }
if (-not $GamePath -or -not (Test-Path -LiteralPath (Join-Path $GamePath 'SlayTheSpire2.exe'))) { throw 'Pass the real game directory using -GamePath or build.local.json.' }
if (Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'Close the game before deployment; nothing was changed.' }
$repo = $script:RepositoryRoot
& pwsh -NoProfile -File (Join-Path $repo 'tests/check_delivery.ps1')
Assert-YjcmNativeExit 'Package not verified'
$target = [IO.Path]::GetFullPath((Join-Path $GamePath 'mods/yjcm'))
if ((Split-Path $target -Leaf) -ne 'yjcm') { throw 'Deployment must stay in mods/yjcm' }
New-Item -ItemType Directory -Path $target -Force | Out-Null
$sourceRoot=Join-Path $repo 'artifacts/dist/yjcm'
$changed=@(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File | Where-Object {
    $destination=Join-Path $target ([IO.Path]::GetRelativePath($sourceRoot,$_.FullName))
    (Test-Path -LiteralPath $destination) -and (Get-FileHash -LiteralPath $_.FullName).Hash -ne (Get-FileHash -LiteralPath $destination).Hash
})
if($changed.Count) {
    $backupRoot=Join-Path $repo 'artifacts/deploy-backups'
    New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
    $archive=Join-Path $backupRoot ((Get-Date -Format 'yyyyMMdd_HHmmss_fff')+'.zip')
    $zip=[IO.Compression.ZipFile]::Open($archive,[IO.Compression.ZipArchiveMode]::Create)
    try { foreach($file in $changed) {
        $relative=[IO.Path]::GetRelativePath($sourceRoot,$file.FullName)
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,(Join-Path $target $relative),$relative.Replace('\','/'))
    }} finally {$zip.Dispose()}
    # Keep only the newest three exact ZIP files under this dedicated backup directory.
    $backups=@(Get-ChildItem -LiteralPath $backupRoot -File -Filter '*.zip'|Sort-Object Name -Descending|Select-Object -Skip 3)
    foreach($file in $backups) {
        if([IO.Path]::GetFullPath($file.DirectoryName) -ne [IO.Path]::GetFullPath($backupRoot)) {throw 'Backup outside expected directory'}
        Remove-Item -LiteralPath $file.FullName
    }
}
Get-ChildItem -LiteralPath (Join-Path $repo 'artifacts/dist/yjcm') -Force | Copy-Item -Destination $target -Recurse -Force
& pwsh -NoProfile -File (Join-Path $repo 'tests/check_delivery.ps1') -CheckSteam -GamePath $GamePath
Assert-YjcmNativeExit 'Deployed files failed verification'
Write-Output "DEPLOY_OK destination=$target"
