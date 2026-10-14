param(
    [Parameter(Mandatory = $true)][string]$SouthSource,
    [Parameter(Mandatory = $true)][string]$NorthSource,
    [Parameter(Mandatory = $true)][string]$EastSource,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)

Add-Type -AssemblyName System.Drawing
[System.IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null

function Convert-Sprite([string]$path, [bool]$removeChecker) {
    $source = [System.Drawing.Bitmap]::FromFile($path)
    $clean = [System.Drawing.Bitmap]::new($source.Width, $source.Height,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt $source.Height; $y++) {
        for ($x = 0; $x -lt $source.Width; $x++) {
            $c = $source.GetPixel($x, $y)
            if ($c.A -eq 0) { continue }
            if ($removeChecker) {
                $maximum = [Math]::Max($c.R, [Math]::Max($c.G, $c.B))
                $minimum = [Math]::Min($c.R, [Math]::Min($c.G, $c.B))
                if ($minimum -gt 150 -and ($maximum - $minimum) -lt 28) { continue }
            }
            $clean.SetPixel($x, $y, $c)
        }
    }
    $source.Dispose()

    $result = [System.Drawing.Bitmap]::new(512, 512,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($result)
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.DrawImage($clean, [System.Drawing.Rectangle]::new(0, 0, 512, 512))
    $graphics.Dispose(); $clean.Dispose()
    return $result
}

function Save-Layers([System.Drawing.Bitmap]$sprite, [string]$direction, [int]$backCutoff) {
    $back = [System.Drawing.Bitmap]::new(512, 512, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $front = [System.Drawing.Bitmap]::new(512, 512, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt 512; $y++) {
        for ($x = 0; $x -lt 512; $x++) {
            $c = $sprite.GetPixel($x, $y)
            if ($c.A -eq 0) { continue }
            if ($backCutoff -gt 0 -and $y -lt $backCutoff) { $back.SetPixel($x, $y, $c) }
            else { $front.SetPixel($x, $y, $c) }
        }
    }
    $back.Save((Join-Path $OutputDirectory "JiuxiaoXianxu_Back_$direction.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $front.Save((Join-Path $OutputDirectory "JiuxiaoXianxu_Front_$direction.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $back.Dispose(); $front.Dispose()
}

$south = Convert-Sprite $SouthSource $true
$north = Convert-Sprite $NorthSource $false
$east = Convert-Sprite $EastSource $true
Save-Layers $south 'south' 112
Save-Layers $north 'north' 0
Save-Layers $east 'east' 512

$west = [System.Drawing.Bitmap]$east.Clone()
$west.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX)
Save-Layers $west 'west' 512
$south.Save((Join-Path $OutputDirectory 'JiuxiaoXianxu.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$south.Dispose(); $north.Dispose(); $east.Dispose(); $west.Dispose()
