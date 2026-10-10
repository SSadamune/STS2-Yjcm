$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo 'artifacts/interop'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$archive=Join-Path $output ('Yjcm-Sunqian-接口源码_'+(Get-Date -Format 'yyyyMMdd_HHmmss_fff')+'.zip')
$sourceRoot=Join-Path $repo 'mod/src'
$files=@(foreach($folder in @('Interop','Equipment','Orbs')) { Get-ChildItem -LiteralPath (Join-Path $sourceRoot $folder) -Filter '*.cs' -File })
foreach($file in @('MainFile.cs','Powers/HuoGongPower.cs','Powers/KuangFengPower.cs','Powers/LianHuanPower.cs','Cards/SlotOptionCards.cs','Characters/WuZhugeliang/Yjcm.cs','Characters/WeiDongZhuo.cs','Relics/YjcmQingShi.cs','Relics/YjcmJieQingShi.cs')) { $files+=Get-Item -LiteralPath (Join-Path $sourceRoot $file) }
$files+=@(Get-ChildItem -LiteralPath (Join-Path $repo 'docs/interop') -Recurse -File)
$files+=Get-Item -LiteralPath (Join-Path $repo 'LICENSE')
$files+=Get-Item -LiteralPath (Join-Path $repo 'THIRD_PARTY_NOTICES.md')
$manifest=[Collections.Generic.List[object]]::new()
$zip=[IO.Compression.ZipFile]::Open($archive,[IO.Compression.ZipArchiveMode]::Create)
try {
    foreach($file in $files) {
        $relative=[IO.Path]::GetRelativePath($repo,$file.FullName).Replace('\','/')
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$file.FullName,$relative)
        $manifest.Add([pscustomobject]@{Path=$relative;Sha256=(Get-FileHash -LiteralPath $file.FullName).Hash})
    }
    $entry=$zip.CreateEntry('source-manifest.json');$writer=[IO.StreamWriter]::new($entry.Open(),[Text.UTF8Encoding]::new($false))
    try {$writer.Write((@{modId='Yjcm';variants=@('stable','beta');scope='Implementation excerpt, not a standalone build';files=$manifest}|ConvertTo-Json -Depth 6))}finally{$writer.Dispose()}
}finally{$zip.Dispose()}
Write-Output "INTEROP_SOURCE_PACKAGED files=$($files.Count) zip=$archive"
