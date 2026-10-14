param([Parameter(Mandatory = $true)][string]$TextureDirectory)

Add-Type -AssemblyName System.Drawing
$pixelFormat = [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
$png = [System.Drawing.Imaging.ImageFormat]::Png

function New-Canvas { [System.Drawing.Bitmap]::new(512, 512, $pixelFormat) }

function Save-Bitmap([System.Drawing.Bitmap]$bitmap, [string]$path) {
    $temporary = "$path.fixed.png"
    $bitmap.Save($temporary, $png)
    Move-Item -LiteralPath $temporary -Destination $path -Force
}

function Composite([System.Drawing.Bitmap]$bottom, [System.Drawing.Bitmap]$top) {
    $result = New-Canvas
    $graphics = [System.Drawing.Graphics]::FromImage($result)
    $graphics.DrawImageUnscaled($bottom, 0, 0)
    $graphics.DrawImageUnscaled($top, 0, 0)
    $graphics.Dispose()
    return $result
}

function Tone-Texture([System.Drawing.Bitmap]$source) {
    $result = New-Canvas
    for ($y = 0; $y -lt 512; $y++) {
        for ($x = 0; $x -lt 512; $x++) {
            $c = $source.GetPixel($x, $y)
            if ($c.A -eq 0) { continue }
            $isGreenPattern = $c.G -gt ($c.R * 1.35) -and $c.G -gt ($c.B * 1.08)
            $isGold = $c.R -gt ($c.B * 1.22) -and $c.G -gt ($c.B * 1.08)
            if ($isGreenPattern) {
                # The woven jade lines are the only mildly luminous element.
                $r = [Math]::Min(125, [int](55 + $c.R * .30))
                $g = [Math]::Min(205, [int](105 + $c.G * .38))
                $b = [Math]::Min(165, [int](75 + $c.B * .34))
            } elseif ($isGold) {
                $r = [Math]::Min(195, [int](80 + $c.R * .44))
                $g = [Math]::Min(170, [int](65 + $c.G * .40))
                $b = [Math]::Min(105, [int](30 + $c.B * .30))
            } else {
                # Cloth/cloud body matches the robe rather than reading as emissive white.
                $luma = [int](($c.R * .24) + ($c.G * .56) + ($c.B * .20))
                $r = [Math]::Min(215, [int](112 + $luma * .40))
                $g = [Math]::Min(222, [int](118 + $luma * .41))
                $b = [Math]::Min(218, [int](116 + $luma * .40))
            }
            $result.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($c.A, $r, $g, $b))
        }
    }
    return $result
}

function Transform-Horizontal([System.Drawing.Bitmap]$source, [double]$scaleX, [int]$offsetX, [double]$scaleY, [int]$offsetY) {
    $result = New-Canvas
    $graphics = [System.Drawing.Graphics]::FromImage($result)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $width = [int](512 * $scaleX)
    $height = [int](512 * $scaleY)
    $graphics.DrawImage($source, [System.Drawing.Rectangle]::new($offsetX, $offsetY, $width, $height))
    $graphics.Dispose()
    return $result
}

# South: spread both hanging halves away from the face and widen/lower the rear half-ring.
$southFrontPath = Join-Path $TextureDirectory 'JiuxiaoXianxu_Front_south.png'
$southFrontSource = [System.Drawing.Bitmap]::FromFile($southFrontPath)
$southFront = New-Canvas
$southGraphics = [System.Drawing.Graphics]::FromImage($southFront)
$southGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$leftSource = [System.Drawing.Rectangle]::new(0, 0, 256, 512)
$rightSource = [System.Drawing.Rectangle]::new(256, 0, 256, 512)
$southGraphics.DrawImage($southFrontSource, [System.Drawing.Rectangle]::new(-28, 0, 256, 512), $leftSource, [System.Drawing.GraphicsUnit]::Pixel)
$southGraphics.DrawImage($southFrontSource, [System.Drawing.Rectangle]::new(284, 0, 256, 512), $rightSource, [System.Drawing.GraphicsUnit]::Pixel)
$southGraphics.Dispose(); $southFrontSource.Dispose()

$southBackPath = Join-Path $TextureDirectory 'JiuxiaoXianxu_Back_south.png'
$southBackSource = [System.Drawing.Bitmap]::FromFile($southBackPath)
$southBack = Transform-Horizontal $southBackSource 1.34 -87 1.18 12
$southBackSource.Dispose()

$toned = Tone-Texture $southFront; $southFront.Dispose(); Save-Bitmap $toned $southFrontPath; $toned.Dispose()
$toned = Tone-Texture $southBack; $southBack.Dispose(); Save-Bitmap $toned $southBackPath; $toned.Dispose()

# Side profiles: reconstruct the complete object, then put only the upper half-ring
# behind the head. The connector and the uninterrupted hanging silk stay in front.
foreach ($direction in @('east', 'west')) {
    $backPath = Join-Path $TextureDirectory "JiuxiaoXianxu_Back_$direction.png"
    $frontPath = Join-Path $TextureDirectory "JiuxiaoXianxu_Front_$direction.png"
    $oldBack = [System.Drawing.Bitmap]::FromFile($backPath)
    $oldFront = [System.Drawing.Bitmap]::FromFile($frontPath)
    $whole = Composite $oldBack $oldFront
    $oldBack.Dispose(); $oldFront.Dispose()
    $newBack = New-Canvas
    $newFront = New-Canvas
    for ($y = 0; $y -lt 512; $y++) {
        for ($x = 0; $x -lt 512; $x++) {
            $c = $whole.GetPixel($x, $y)
            if ($c.A -eq 0) { continue }
            if ($y -lt 128) { $newBack.SetPixel($x, $y, $c) }
            else { $newFront.SetPixel($x, $y, $c) }
        }
    }
    $whole.Dispose()
    $toned = Tone-Texture $newBack; $newBack.Dispose(); Save-Bitmap $toned $backPath; $toned.Dispose()
    $toned = Tone-Texture $newFront; $newFront.Dispose(); Save-Bitmap $toned $frontPath; $toned.Dispose()
}

# North and inventory art receive the same restrained material treatment.
foreach ($name in @('JiuxiaoXianxu.png', 'JiuxiaoXianxu_Back_north.png', 'JiuxiaoXianxu_Front_north.png')) {
    $path = Join-Path $TextureDirectory $name
    $source = [System.Drawing.Bitmap]::FromFile($path)
    $toned = Tone-Texture $source
    $source.Dispose(); Save-Bitmap $toned $path; $toned.Dispose()
}
