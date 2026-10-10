$ErrorActionPreference = 'Stop'
$script:RepositoryRoot = Split-Path $PSScriptRoot -Parent
function Get-YjcmConfiguration {
    $configFile = Join-Path $script:RepositoryRoot 'build.local.json'
    $config = if (Test-Path -LiteralPath $configFile) { Get-Content -LiteralPath $configFile -Raw | ConvertFrom-Json } else { [pscustomobject]@{} }
    return $config
}
function Assert-YjcmNativeExit([string]$Message) {
    if ($LASTEXITCODE -ne 0) { throw "$Message (exit=$LASTEXITCODE)" }
}
function Get-YjcmSourceFingerprint {
    $repo=$script:RepositoryRoot
    $files=@(Get-ChildItem -LiteralPath (Join-Path $repo 'mod/src') -Recurse -File -Filter '*.cs')
    $files+=@(Get-ChildItem -LiteralPath (Join-Path $repo 'loader') -File | Where-Object Extension -in @('.cs','.csproj'))
    foreach($relative in @('mod/Yjcm.csproj','Directory.Build.props','dependencies/GameReferences.props','dependencies/stable-0.107.1/manifest.json','dependencies/beta-0.111.0/manifest.json')) { $files+=Get-Item -LiteralPath (Join-Path $repo $relative) }
    $lines=@($files|Sort-Object FullName|ForEach-Object { [IO.Path]::GetRelativePath($repo,$_.FullName)+':'+(Get-FileHash -LiteralPath $_.FullName).Hash })
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))))
}
