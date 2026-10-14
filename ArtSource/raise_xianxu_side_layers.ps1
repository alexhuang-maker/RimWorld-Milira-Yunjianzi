param([Parameter(Mandatory = $true)][string]$TextureDirectory)

Add-Type -AssemblyName System.Drawing
$pf = [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
$png = [System.Drawing.Imaging.ImageFormat]::Png

function Keep-LargestAlphaComponent([System.Drawing.Bitmap]$source) {
    $visited = [bool[]]::new(512 * 512)
    $largest = $null
    for ($y=0; $y-lt512; $y++) { for ($x=0; $x-lt512; $x++) {
        $start=$y*512+$x
        if($visited[$start] -or $source.GetPixel($x,$y).A-lt16){continue}
        $stack=[System.Collections.Generic.Stack[int]]::new(); $component=[System.Collections.Generic.List[int]]::new()
        $stack.Push($start); $visited[$start]=$true
        while($stack.Count-gt0){
            $i=$stack.Pop(); $component.Add($i); $cx=$i%512; $cy=[int][Math]::Floor($i/512)
            $neighbors = @(($i - 1), ($i + 1), ($i - 512), ($i + 512))
            foreach($n in $neighbors){
                if($n-lt0 -or $n-ge262144 -or $visited[$n]){continue}
                $nx=$n%512; $ny=[int][Math]::Floor($n/512)
                if([Math]::Abs($nx-$cx)+[Math]::Abs($ny-$cy)-ne1){continue}
                $visited[$n]=$true
                if($source.GetPixel($nx,$ny).A-ge16){$stack.Push($n)}
            }
        }
        if($null-eq$largest -or $component.Count-gt$largest.Count){$largest=$component}
    }}
    $result=[System.Drawing.Bitmap]::new(512,512,$pf)
    foreach($i in $largest){$x=$i%512;$y=[int][Math]::Floor($i/512);$result.SetPixel($x,$y,$source.GetPixel($x,$y))}
    return $result
}

foreach ($direction in @('east', 'west')) {
    $backPath = Join-Path $TextureDirectory "JiuxiaoXianxu_Back_$direction.png"
    $frontPath = Join-Path $TextureDirectory "JiuxiaoXianxu_Front_$direction.png"
    $back = [System.Drawing.Bitmap]::FromFile($backPath)
    $front = [System.Drawing.Bitmap]::FromFile($frontPath)
    $raised = [System.Drawing.Bitmap]::new(512, 512, $pf)
    $graphics = [System.Drawing.Graphics]::FromImage($raised)
    $graphics.DrawImageUnscaled($back, 0, 0)
    $graphics.DrawImageUnscaled($front, 0, 0)
    $graphics.Dispose(); $back.Dispose(); $front.Dispose()
    $clean = Keep-LargestAlphaComponent $raised
    $raised.Dispose(); $raised = $clean

    $empty = [System.Drawing.Bitmap]::new(512, 512, $pf)
    $backTemp = "$backPath.raised.png"
    $frontTemp = "$frontPath.raised.png"
    $empty.Save($backTemp, $png)
    $raised.Save($frontTemp, $png)
    $empty.Dispose(); $raised.Dispose()
    Move-Item -LiteralPath $backTemp -Destination $backPath -Force
    Move-Item -LiteralPath $frontTemp -Destination $frontPath -Force
}
