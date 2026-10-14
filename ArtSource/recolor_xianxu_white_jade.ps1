param([Parameter(Mandatory = $true)][string]$TextureDirectory)

Add-Type -AssemblyName System.Drawing

function Recolor-Texture([string]$path) {
    $source = [System.Drawing.Bitmap]::FromFile($path)
    $edited = [System.Drawing.Bitmap]::new($source.Width, $source.Height,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt $source.Height; $y++) {
        for ($x = 0; $x -lt $source.Width; $x++) {
            $c = $source.GetPixel($x, $y)
            if ($c.A -eq 0) { continue }
            $isGold = $c.R -gt 115 -and $c.G -gt 75 -and $c.B -lt ($c.G * .86)
            if ($isGold) {
                $edited.SetPixel($x, $y, $c)
                continue
            }
            $isBrightThunderInk = $c.G -gt 202 -and $c.B -gt 205 -and $c.R -lt 225
            if ($isBrightThunderInk) {
                $light = [Math]::Max(0, [Math]::Min(70, [int](($c.G - 202) * 1.25)))
                $edited.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($c.A,
                    18 + [int]($light * .35), 145 + $light, 116 + [int]($light * .62)))
                continue
            }
            # Blue fabric becomes ivory-white jade silk while preserving painted shading.
            $value = [Math]::Max($c.R, [Math]::Max($c.G, $c.B))
            $shade = [Math]::Max(0, [Math]::Min(255, [int](170 + $value * .30)))
            $edited.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($c.A,
                [Math]::Min(255, $shade + 10), [Math]::Min(255, $shade + 18), [Math]::Min(255, $shade + 14)))
        }
    }
    $source.Dispose()
    $temporary = "$path.recolored.png"
    $edited.Save($temporary, [System.Drawing.Imaging.ImageFormat]::Png)
    $edited.Dispose()
    Move-Item -LiteralPath $temporary -Destination $path -Force
}

Get-ChildItem -LiteralPath $TextureDirectory -Filter 'JiuxiaoXianxu*.png' |
    Where-Object { $_.Name -notlike '*_Female_*' } |
    ForEach-Object { Recolor-Texture $_.FullName }

# AlienRace probes this conventional apparel path even though actual rendering is handled by the comp.
foreach ($direction in @('north', 'east', 'south', 'west')) {
    $placeholder = [System.Drawing.Bitmap]::new(2, 2,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $placeholder.Save((Join-Path $TextureDirectory "JiuxiaoXianxu_Female_$direction.png"),
        [System.Drawing.Imaging.ImageFormat]::Png)
    $placeholder.Dispose()
}

# For side views, only the torso-crossing middle stays behind the pawn. The exposed upper and lower
# sections render in front so hair/wings cannot erase the whole artifact.
foreach ($direction in @('east', 'west')) {
    $backPath = Join-Path $TextureDirectory "JiuxiaoXianxu_Back_$direction.png"
    $frontPath = Join-Path $TextureDirectory "JiuxiaoXianxu_Front_$direction.png"
    $source = [System.Drawing.Bitmap]::FromFile($backPath)
    $back = [System.Drawing.Bitmap]::new(512, 512, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $front = [System.Drawing.Bitmap]::new(512, 512, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt 512; $y++) {
        for ($x = 0; $x -lt 512; $x++) {
            $c = $source.GetPixel($x, $y)
            if ($c.A -eq 0) { continue }
            if ($y -ge 145 -and $y -lt 335) { $back.SetPixel($x, $y, $c) }
            else { $front.SetPixel($x, $y, $c) }
        }
    }
    $source.Dispose()
    $back.Save($backPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $front.Save($frontPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $back.Dispose(); $front.Dispose()
}
