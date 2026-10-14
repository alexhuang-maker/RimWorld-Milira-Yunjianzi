param([Parameter(Mandatory = $true)][string]$TextureDirectory)

Add-Type -AssemblyName System.Drawing
$format = [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
$png = [System.Drawing.Imaging.ImageFormat]::Png

function New-Canvas { [System.Drawing.Bitmap]::new(512, 512, $format) }
function Save-Canvas([string]$path, [System.Drawing.Bitmap]$image) {
    $temporary = "$path.soft-pibo.png"
    $image.Save($temporary, $png)
    Move-Item -LiteralPath $temporary -Destination $path -Force
}

# Preserve the first original top ornaments from the current art, but replace the
# rigid# connecting member as layered silksoft pibo rather than a rigidrigid ring.
$southPath = Join-Path $TextureDirectory 'JiuxiaoXianxu_Back_south.png'
$oldSouth = [System.Drawing.Bitmap]::FromFile($southPath)
$south = New-Canvas
$g = [System.Drawing.Graphics]::FromImage($south)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality

$path = [System.Drawing.Drawing2D.GraphicsPath]::new()
$path.AddBezier(112, 132, 54, 120, 56, 64, 145, 54)
$path.AddBezier(145, 54, 208, 42, 304, 42, 367, 54)
$path.AddBezier(367, 54, 456, 64, 458, 120, 400, 132)

# Muted gold piping around a broad fabric band, with two quiet jade woven lines.
$edge = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(215, 157, 130, 72), 26)
$edge.StartCap = $edge.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$cloth = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(226, 174, 190, 183), 20)
$cloth.StartCap = $cloth.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$foldLight = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(155, 205, 216, 210), 4)
$foldDark = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(145, 52, 132, 108), 2)
$g.DrawPath($edge, $path)
$g.DrawPath($cloth, $path)
$g.DrawPath($foldLight, $path)
$g.DrawPath($foldDark, $path)
$edge.Dispose(); $cloth.Dispose(); $foldLight.Dispose(); $foldDark.Dispose(); $path.Dispose()

# Keep the first-version central jade clasp and both flower connectors.
$g.DrawImage($oldSouth, [System.Drawing.Rectangle]::new(225, 18, 62, 70), [System.Drawing.Rectangle]::new(225, 18, 62, 70), [System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($oldSouth, [System.Drawing.Rectangle]::new(82, 91, 72, 67), [System.Drawing.Rectangle]::new(82, 91, 72, 67), [System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($oldSouth, [System.Drawing.Rectangle]::new(358, 91, 72, 67), [System.Drawing.Rectangle]::new(358, 91, 72, 67), [System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose(); $oldSouth.Dispose()
Save-Canvas $southPath $south
$south.Dispose()

# The side view shows only one half of the same pibo. Remove the metallic reading
# from its upper rear segment while preserving the authored curve and tip jewel.
foreach ($direction in @('east', 'west')) {
    $pathName = Join-Path $TextureDirectory "JiuxiaoXianxu_Back_$direction.png"
    $source = [System.Drawing.Bitmap]::FromFile($pathName)
    $result = New-Canvas
    for ($y = 0; $y -lt 512; $y++) {
        for ($x = 0; $x -lt 512; $x++) {
            $c = $source.GetPixel($x, $y)
            if ($c.A -eq 0) { continue }
            $isJade = $c.G -gt ($c.R * 1.25) -and $c.G -gt ($c.B * 1.04)
            if ($isJade) {
                $color = [System.Drawing.Color]::FromArgb([Math]::Min(190, $c.A), 62, 145, 116)
            } else {
                $l = [int](($c.R + $c.G + $c.B) / 3)
                $color = [System.Drawing.Color]::FromArgb([Math]::Min(205, $c.A),
                    [Math]::Min(190, 130 + [int]($l * .20)),
                    [Math]::Min(202, 140 + [int]($l * .21)),
                    [Math]::Min(196, 137 + [int]($l * .20)))
            }
            $result.SetPixel($x, $y, $color)
        }
    }
    $source.Dispose(); Save-Canvas $pathName $result; $result.Dispose()
}
