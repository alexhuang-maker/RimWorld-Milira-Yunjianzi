param([Parameter(Mandatory = $true)][string]$TexturePath)

Add-Type -AssemblyName System.Drawing
$source = [System.Drawing.Bitmap]::FromFile($TexturePath)
$edited = [System.Drawing.Bitmap]::new($source.Width, $source.Height,
    [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$cx = ($source.Width - 1) / 2.0
$cy = ($source.Height - 1) / 2.0
$keepRadius = $source.Width * 0.405

for ($y = 0; $y -lt $source.Height; $y++) {
    for ($x = 0; $x -lt $source.Width; $x++) {
        $dx = $x - $cx; $dy = $y - $cy
        if (($dx * $dx + $dy * $dy) -lt ($keepRadius * $keepRadius)) { continue }
        $color = $source.GetPixel($x, $y)
        # Remove the white sword/rhombus/rune ink while retaining the cyan cloud aura.
        $maximum = [Math]::Max($color.R, [Math]::Max($color.G, $color.B))
        $minimum = [Math]::Min($color.R, [Math]::Min($color.G, $color.B))
        if ($minimum -gt 175 -and ($maximum - $minimum) -lt 48) { continue }
        $edited.SetPixel($x, $y, $color)
    }
}

$source.Dispose()
$temporary = "$TexturePath.edited.png"
$edited.Save($temporary, [System.Drawing.Imaging.ImageFormat]::Png)
$edited.Dispose()
Move-Item -LiteralPath $temporary -Destination $TexturePath -Force
