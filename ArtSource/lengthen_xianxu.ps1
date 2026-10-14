param([Parameter(Mandatory = $true)][string]$TextureDirectory)

Add-Type -AssemblyName System.Drawing
Get-ChildItem -LiteralPath $TextureDirectory -Filter 'JiuxiaoXianxu_*_*.png' | ForEach-Object {
    $source = [System.Drawing.Bitmap]::FromFile($_.FullName)
    $edited = [System.Drawing.Bitmap]::new($source.Width, $source.Height,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $anchor = [int]($source.Height * .36)
    $stretch = 1.55

    for ($y = 0; $y -lt $source.Height; $y++) {
        $sourceY = if ($y -le $anchor) { $y } else { $anchor + [int](($y - $anchor) / $stretch) }
        if ($sourceY -ge $source.Height) { continue }
        for ($x = 0; $x -lt $source.Width; $x++) {
            $color = $source.GetPixel($x, $sourceY)
            if ($color.A -eq 0) { continue }
            # Make the cloth more material and saturated without adding emissive glow.
            $alpha = [Math]::Min(255, [int]($color.A * 1.28))
            $red = [Math]::Min(255, [int]($color.R * .82))
            $green = [Math]::Min(255, [int]($color.G * 1.02))
            $blue = [Math]::Min(255, [int]($color.B * 1.05))
            $edited.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($alpha, $red, $green, $blue))
        }
    }

    $source.Dispose()
    $temporary = "$($_.FullName).lengthened.png"
    $edited.Save($temporary, [System.Drawing.Imaging.ImageFormat]::Png)
    $edited.Dispose()
    Move-Item -LiteralPath $temporary -Destination $_.FullName -Force
}
