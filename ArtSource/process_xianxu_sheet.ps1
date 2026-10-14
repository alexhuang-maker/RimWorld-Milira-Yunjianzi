param(
    [Parameter(Mandatory = $true)][string]$InputPath,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)

Add-Type -AssemblyName System.Drawing
[System.IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null
$source = [System.Drawing.Bitmap]::FromFile($InputPath)

$third = [int]($source.Width / 3)
$views = @(
    @{ Name = 'south'; Crop = [System.Drawing.Rectangle]::new(0, 0, $third, $source.Height); Front = [System.Drawing.Rectangle]::new([int]($third*.18), [int]($source.Height*.25), [int]($third*.64), [int]($source.Height*.40)) },
    @{ Name = 'north'; Crop = [System.Drawing.Rectangle]::new($third, 0, $third, $source.Height); Front = [System.Drawing.Rectangle]::new([int]($third*.17), [int]($source.Height*.25), [int]($third*.66), [int]($source.Height*.40)) },
    @{ Name = 'east';  Crop = [System.Drawing.Rectangle]::new($third*2, 0, $source.Width-$third*2, $source.Height); Front = [System.Drawing.Rectangle]::new([int]($third*.16), [int]($source.Height*.24), [int]($third*.50), [int]($source.Height*.42)) }
)

function Is-Silk([System.Drawing.Color]$c) {
    # Discard the deliberately flat magenta background and green mannequin.
    $isMagenta = $c.R -gt 140 -and $c.B -gt 110 -and $c.G -lt ([Math]::Min($c.R, $c.B) * .72)
    $isGreen = $c.G -gt 45 -and $c.G -gt ($c.R * 1.35) -and $c.G -gt ($c.B * 1.28)
    return -not ($isMagenta -or $isGreen)
}

foreach ($view in $views) {
    $crop = $view.Crop
    $frontRect = $view.Front
    $back = [System.Drawing.Bitmap]::new(512, 512, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $front = [System.Drawing.Bitmap]::new(512, 512, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($dy = 0; $dy -lt 512; $dy++) {
        for ($dx = 0; $dx -lt 512; $dx++) {
            $sx = $crop.X + [Math]::Min($crop.Width - 1, [int]($dx * $crop.Width / 512.0))
            $sy = $crop.Y + [Math]::Min($crop.Height - 1, [int]($dy * $crop.Height / 512.0))
            $color = $source.GetPixel($sx, $sy)
            if (-not (Is-Silk $color)) { continue }
            $localX = $sx - $crop.X
            $localY = $sy - $crop.Y
            $target = if ($frontRect.Contains($localX, $localY)) { $front } else { $back }
            $alpha = [Math]::Min(255, [int]((([Math]::Min($color.B, $color.G) - $color.R) + 45) * 2.5))
            if ($alpha -lt 24) { continue }
            $target.SetPixel($dx, $dy, [System.Drawing.Color]::FromArgb($alpha, $color.R, $color.G, $color.B))
        }
    }
    $back.Save((Join-Path $OutputDirectory ("JiuxiaoXianxu_Back_{0}.png" -f $view.Name)), [System.Drawing.Imaging.ImageFormat]::Png)
    $front.Save((Join-Path $OutputDirectory ("JiuxiaoXianxu_Front_{0}.png" -f $view.Name)), [System.Drawing.Imaging.ImageFormat]::Png)
    $back.Dispose(); $front.Dispose()
}

# RimWorld mirrors east for west poorly when directional depth is important, so emit explicit mirrored files.
foreach ($layer in @('Back', 'Front')) {
    $eastPath = Join-Path $OutputDirectory ("JiuxiaoXianxu_{0}_east.png" -f $layer)
    $westPath = Join-Path $OutputDirectory ("JiuxiaoXianxu_{0}_west.png" -f $layer)
    $west = [System.Drawing.Bitmap]::FromFile($eastPath)
    $west.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX)
    $west.Save($westPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $west.Dispose()
}
$source.Dispose()
