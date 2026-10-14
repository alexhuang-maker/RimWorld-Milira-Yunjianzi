param(
    [Parameter(Mandatory = $true)][string]$SouthSource,
    [Parameter(Mandatory = $true)][string]$EastSource,
    [Parameter(Mandatory = $true)][string]$TextureDirectory
)

Add-Type -AssemblyName System.Drawing
$pf = [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
$png = [System.Drawing.Imaging.ImageFormat]::Png

function New-Canvas { [System.Drawing.Bitmap]::new(512, 512, $pf) }

function Remove-Baked-Checker([string]$path) {
    $source = [System.Drawing.Bitmap]::FromFile($path)
    $result = [System.Drawing.Bitmap]::new($source.Width, $source.Height, $pf)
    $corner = $source.GetPixel(0, 0)
    # Portrait side generations use a brighter two-tone checker (roughly
    # 246/254); remove both cells, not only the white half.
    $backgroundFloor = if ($corner.R -gt 249) { 242 } else { 238 }
    for ($y = 0; $y -lt $source.Height; $y++) {
        for ($x = 0; $x -lt $source.Width; $x++) {
            $c = $source.GetPixel($x, $y)
            $min = [Math]::Min($c.R, [Math]::Min($c.G, $c.B))
            $max = [Math]::Max($c.R, [Math]::Max($c.G, $c.B))
            $range = $max - $min
            # Image generation preview checker is nearly neutral and very bright.
            # The artifact retains sage hue, gold saturation, or green linework.
            if ($min -gt $backgroundFloor -and $range -lt 6) { continue }
            $alpha = 255
            $isJade = $c.G -gt ($c.R * 1.20) -and $c.G -gt ($c.B * 1.04)
            $isGold = $c.R -gt ($c.B * 1.16) -and $c.G -gt ($c.B * 1.06)
            if (-not $isJade -and -not $isGold -and $range -lt 48) {
                # Clean ivory-white instead of the former grey-green cast.
                $r = [Math]::Min(250, $c.R + 24)
                $g = [Math]::Min(252, $c.G + 24)
                $b = [Math]::Min(250, $c.B + 25)
            } else { $r=$c.R; $g=$c.G; $b=$c.B }
            $result.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($alpha, $r, $g, $b))
        }
    }
    $source.Dispose()
    return $result
}

function Find-Bounds([System.Drawing.Bitmap]$image) {
    $left=$image.Width; $top=$image.Height; $right=-1; $bottom=-1
    for ($y=0; $y-lt$image.Height; $y++) {
        for ($x=0; $x-lt$image.Width; $x++) {
            if ($image.GetPixel($x,$y).A -lt 12) { continue }
            if ($x-lt$left){$left=$x}; if($x-gt$right){$right=$x}
            if ($y-lt$top){$top=$y}; if($y-gt$bottom){$bottom=$y}
        }
    }
    return [System.Drawing.Rectangle]::new($left,$top,$right-$left+1,$bottom-$top+1)
}

function Fit-To-Canvas([System.Drawing.Bitmap]$image, [int]$marginX, [int]$marginY) {
    $bounds = Find-Bounds $image
    $scale = [Math]::Min((512-2*$marginX)/$bounds.Width, (512-2*$marginY)/$bounds.Height)
    $w=[int]($bounds.Width*$scale); $h=[int]($bounds.Height*$scale)
    $x=[int]((512-$w)/2); $y=[int]((512-$h)/2)
    $result=New-Canvas
    $g=[System.Drawing.Graphics]::FromImage($result)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($image,[System.Drawing.Rectangle]::new($x,$y,$w,$h),$bounds,[System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose()
    return $result
}

function Save([System.Drawing.Bitmap]$image,[string]$name) {
    $path=Join-Path $TextureDirectory $name
    $temp="$path.new.png"; $image.Save($temp,$png)
    Move-Item -LiteralPath $temp -Destination $path -Force
}

$southRaw=Remove-Baked-Checker $SouthSource
$south=Fit-To-Canvas $southRaw 18 7
$southRaw.Dispose()

# South: the butterfly pibo and hollow bi-disc are behind the pawn; only the
# hanging banners begin in front below shoulder height.
$southBack=New-Canvas; $southFront=New-Canvas
for($y=0;$y-lt512;$y++){for($x=0;$x-lt512;$x++){
    $c=$south.GetPixel($x,$y); if($c.A-eq0){continue}
    if($y-lt146){$southBack.SetPixel($x,$y,$c)}else{$southFront.SetPixel($x,$y,$c)}
}}
Save $southBack 'JiuxiaoXianxu_Back_south.png'
Save $southFront 'JiuxiaoXianxu_Front_south.png'

# North/back view: the complete central jade bi-disc must be visible over the pawn.
$empty=New-Canvas; Save $empty 'JiuxiaoXianxu_Back_north.png'; $empty.Dispose()
Save $south 'JiuxiaoXianxu_Front_north.png'
Save $south 'JiuxiaoXianxu.png'
$southBack.Dispose(); $southFront.Dispose(); $south.Dispose()

$eastRaw=Remove-Baked-Checker $EastSource
$east=Fit-To-Canvas $eastRaw 70 8
$eastRaw.Dispose()
$eastBack=New-Canvas; $eastFront=New-Canvas
for($y=0;$y-lt512;$y++){for($x=0;$x-lt512;$x++){
    $c=$east.GetPixel($x,$y); if($c.A-eq0){continue}
    # Only the upper pibo and edge-on jade pass behind the head; the long outer
    # banner stays visible and unbroken rather than disappearing under hair/wings.
    if($y-lt156){$eastBack.SetPixel($x,$y,$c)}else{$eastFront.SetPixel($x,$y,$c)}
}}
Save $eastBack 'JiuxiaoXianxu_Back_east.png'
Save $eastFront 'JiuxiaoXianxu_Front_east.png'

$westBack=[System.Drawing.Bitmap]$eastBack.Clone(); $westBack.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX)
$westFront=[System.Drawing.Bitmap]$eastFront.Clone(); $westFront.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX)
Save $westBack 'JiuxiaoXianxu_Back_west.png'
Save $westFront 'JiuxiaoXianxu_Front_west.png'
$east.Dispose(); $eastBack.Dispose(); $eastFront.Dispose(); $westBack.Dispose(); $westFront.Dispose()
