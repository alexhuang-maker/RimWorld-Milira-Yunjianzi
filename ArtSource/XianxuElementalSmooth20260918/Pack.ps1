$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
# Only sprite packing: preserve the generated art and its alpha, then apply
# the existing front/back contract. No repainting or background extraction.
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
public static class XianxuThunderPacking {
    static Bitmap Empty() { return new Bitmap(512,512,PixelFormat.Format32bppArgb); }
    static Rectangle Bounds(Bitmap image) {
        int l=image.Width,t=image.Height,r=-1,b=-1,clear=0;
        for(int y=0;y<image.Height;y++) for(int x=0;x<image.Width;x++) {
            byte a=image.GetPixel(x,y).A;
            if(a==0) clear++;
            if(a<4) continue;
            l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);
        }
        if(r<0 || clear<image.Width*image.Height/3) throw new Exception("Source needs genuine transparent alpha");
        return Rectangle.FromLTRB(Math.Max(0,l-3),Math.Max(0,t-3),Math.Min(image.Width,r+4),Math.Min(image.Height,b+4));
    }
    static Bitmap Pack(string path, int top, int height, float centerX) {
        using(var source=new Bitmap(path)) {
            Rectangle bounds=Bounds(source);
            float scale=(float)height/bounds.Height;
            int width=(int)Math.Round(bounds.Width*scale);
            if(width>476) throw new Exception("Sprite exceeds established canvas width");
            var result=Empty();
            using(var g=Graphics.FromImage(result)) {
                g.CompositingMode=CompositingMode.SourceCopy;
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                g.DrawImage(source,new Rectangle((int)(centerX-width/2f),top,width,height),bounds,GraphicsUnit.Pixel);
            }
            return result;
        }
    }
    static void Save(Bitmap image,string dir,string name) { image.Save(Path.Combine(dir,name+".png"),ImageFormat.Png); }
    public static void Run(string sourceDir,string targetDir) {
        Directory.CreateDirectory(targetDir);
        using(var south=Pack(Path.Combine(sourceDir,"Elemental_South_Source.png"),7,498,256))
        using(var east=Pack(Path.Combine(sourceDir,"Elemental_East_Source.png"),20,444,260))
        using(var back=Empty()) using(var front=Empty()) using(var empty=Empty()) {
            // User requested exact bilateral symmetry, including fine arcs.
            // Reflect the generated left half after packing, before layering.
            for(int y=0;y<512;y++) for(int x=0;x<256;x++)
                south.SetPixel(511-x,y,south.GetPixel(x,y));
            for(int y=0;y<512;y++) for(int x=0;x<512;x++)
                (y<146?back:front).SetPixel(x,y,south.GetPixel(x,y));
            Save(back,targetDir,"JiuxiaoXianxu_Back_south");
            Save(front,targetDir,"JiuxiaoXianxu_Front_south");
            Save(south,targetDir,"JiuxiaoXianxu_Front_north");
            Save(south,targetDir,"JiuxiaoXianxu");
            Save(east,targetDir,"JiuxiaoXianxu_Front_east");
            using(var west=(Bitmap)east.Clone()) {
                west.RotateFlip(RotateFlipType.RotateNoneFlipX);
                Save(west,targetDir,"JiuxiaoXianxu_Front_west");
            }
            foreach(var dir in new[]{"north","east","west"}) Save(empty,targetDir,"JiuxiaoXianxu_Back_"+dir);
        }
    }
}
'@
$target = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'Content/Textures/Yunjianzi/Apparel/JiuxiaoXianxu'
[XianxuThunderPacking]::Run($PSScriptRoot, $target)
Write-Output "Packed 9 runtime textures with original layer layout."
