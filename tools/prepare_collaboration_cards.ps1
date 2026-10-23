param(
    [string]$MaterialRoot='E:\sts2mod\素材',
    [Parameter(Mandatory=$true)][string]$FfmpegPath
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$cards=@(
    @{Name='水火无敌';Portrait='sun_qian_shui_huo_wu_di';Voice='shui_huo_wu_di'},
    @{Name='水火兼容';Portrait='xin_san_guo_shui_huo_jian_rong';Voice='shui_huo_jian_rong'}
)
$portraitDir=Join-Path $repo 'mod/Yjcm/Images/Cards'
$voiceDir=Join-Path $repo 'assets/loose/Voice/Bank/Assets'
$backupDir=Join-Path $MaterialRoot '语音素材/武诸葛'
foreach($directory in @($portraitDir,$voiceDir,$backupDir)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}
foreach($card in $cards) {
    $art=Join-Path $MaterialRoot "图片处理后/武诸葛/$($card.Name).png"
    $mp3=Join-Path $MaterialRoot "语音原素材/武诸葛/$($card.Name).mp3"
    $wav=Join-Path $voiceDir "$($card.Voice).wav"
    if(!(Test-Path -LiteralPath $art) -or !(Test-Path -LiteralPath $mp3)) {throw "Missing supplied asset: $($card.Name)"}
    Copy-Item -LiteralPath $art -Destination (Join-Path $portraitDir "$($card.Portrait).png") -Force
    & $FfmpegPath -hide_banner -loglevel error -y -i $mp3 -vn -ar 48000 -ac 1 -c:a pcm_s16le $wav
    if($LASTEXITCODE -ne 0){throw "Audio conversion failed: $mp3"}
    Copy-Item -LiteralPath $wav -Destination (Join-Path $backupDir "$($card.Name).wav") -Force
    Write-Output "COLLABORATION_ASSETS_OK $($card.Name)"
}
