param(
    [string]$Source=(Join-Path $PSScriptRoot '刀型草图_v2_非透明.png'),
    [string]$Destination=(Join-Path $PSScriptRoot 'weidongzhuo_blade_256.png')
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$inputBitmap=[Drawing.Bitmap]::FromFile($Source)
try {
    $cutout=New-Object Drawing.Bitmap($inputBitmap.Width,$inputBitmap.Height,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $cleared=0
        # User-authorized local matting: the generated background is neutral
        # white/gray; the weapon is saturated gold/bronze/red. Remove neutral
        # bright pixels everywhere, including the enclosed ring-pommel hole.
        for($y=0;$y -lt $inputBitmap.Height;$y++) {
            for($x=0;$x -lt $inputBitmap.Width;$x++) {
                $pixel=$inputBitmap.GetPixel($x,$y)
                $lo=[Math]::Min([Math]::Min([int]$pixel.R,[int]$pixel.G),[int]$pixel.B)
                $hi=[Math]::Max([Math]::Max([int]$pixel.R,[int]$pixel.G),[int]$pixel.B)
                if($lo -ge 178 -and ($hi-$lo) -le 30) {
                    $cutout.SetPixel($x,$y,[Drawing.Color]::FromArgb(0,0,0,0))
                    $cleared++
                } else { $cutout.SetPixel($x,$y,$pixel) }
            }
        }
        if($cleared -lt $inputBitmap.Width*$inputBitmap.Height*0.35) { throw 'Unexpected background coverage; inspect before using this asset' }
        $output=New-Object Drawing.Bitmap(256,256,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics=[Drawing.Graphics]::FromImage($output)
            try {
                $graphics.Clear([Drawing.Color]::Transparent)
                $graphics.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.CompositingQuality=[Drawing.Drawing2D.CompositingQuality]::HighQuality
                $graphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.DrawImage($cutout,(New-Object Drawing.Rectangle(0,0,256,256)),0,0,$cutout.Width,$cutout.Height,[Drawing.GraphicsUnit]::Pixel)
            } finally { $graphics.Dispose() }
            if(Test-Path -LiteralPath $Destination) { throw 'Refusing to overwrite an existing final icon; choose another destination' }
            $output.Save($Destination,[Drawing.Imaging.ImageFormat]::Png)
        } finally { $output.Dispose() }
    } finally { $cutout.Dispose() }
} finally { $inputBitmap.Dispose() }
$check=[Drawing.Bitmap]::FromFile($Destination)
try {
    $transparent=0;$opaque=0;$partial=0
    for($y=0;$y -lt $check.Height;$y++) { for($x=0;$x -lt $check.Width;$x++) {
        $a=$check.GetPixel($x,$y).A
        if($a -eq 0) {$transparent++} elseif($a -eq 255) {$opaque++} else {$partial++}
    }}
    if($check.Width -ne 256 -or $check.Height -ne 256 -or $transparent -lt 20000 -or $opaque -lt 5000 -or $partial -eq 0) { throw 'Invalid icon dimensions or alpha channel' }
    [ordered]@{width=$check.Width;height=$check.Height;format=[string]$check.PixelFormat;transparentPixels=$transparent;opaquePixels=$opaque;antialiasedPixels=$partial;file=$Destination} |
        ConvertTo-Json | Tee-Object -FilePath (Join-Path $PSScriptRoot '图片透明校验.json')
} finally { $check.Dispose() }
