$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$textures = Join-Path $root 'Content/Textures/Yunjianzi/Apparel/JiuxiaoXianxu'
foreach($path in @('Source/YunjianziXianxu.cs','1.6/Defs/ApparelDefs/Yunjianzi_Apparel.xml','1.6/Defs/ApparelDefs/Yunjianzi_ApparelLayers.xml')) {
    $before = Join-Path "$PSScriptRoot/before" (Split-Path $path -Leaf)
    if((Get-FileHash -LiteralPath $before).Hash -ne (Get-FileHash -LiteralPath (Join-Path $root $path)).Hash) { throw "Layer settings changed: $path" }
}
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
public static class XianxuThunderChecks {
    static Bitmap Load(string root,string name) { return new Bitmap(Path.Combine(root,"JiuxiaoXianxu"+name+".png")); }
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    public static void Run(string root,string output) {
        using(var combined=Load(root,"")) using(var back=Load(root,"_Back_south")) using(var front=Load(root,"_Front_south"))
        using(var north=Load(root,"_Front_north")) using(var east=Load(root,"_Front_east")) using(var west=Load(root,"_Front_west")) {
            foreach(var b in new[]{combined,back,front,north,east,west}) Check(b.Width==512 && b.Height==512,"Canvas must remain 512 square");
            int clear=0;
            for(int y=0;y<512;y++) for(int x=0;x<512;x++) {
                Color c=combined.GetPixel(x,y), b=back.GetPixel(x,y), f=front.GetPixel(x,y);
                Check(c.ToArgb()==(y<146?b:f).ToArgb(),"Split introduced a seam or lost artwork");
                Check((y<146?f:b).A==0,"Wrong south pass");
                Check(north.GetPixel(x,y).ToArgb()==c.ToArgb(),"North must keep complete foreground");
                Check(east.GetPixel(x,y).ToArgb()==west.GetPixel(511-x,y).ToArgb(),"Side mirroring mismatch");
                if(c.A==0)clear++;
            }
            Check(clear>512*512/2,"Center/background must remain predominantly transparent");
            foreach(string d in new[]{"north","east","west"}) using(var b=Load(root,"_Back_"+d))
                for(int y=0;y<512;y++) for(int x=0;x<512;x++) Check(b.GetPixel(x,y).A==0,"Unexpected rear layer");
            foreach(string d in new[]{"north","east","south","west"}) using(var b=Load(root,"_Female_"+d)) {
                Check(b.Width==2 && b.Height==2,"Native apparel placeholders changed");
                for(int y=0;y<2;y++) for(int x=0;x<2;x++) Check(b.GetPixel(x,y).A==0,"Duplicate native apparel");
            }
            using(var preview=new Bitmap(1020,740,PixelFormat.Format32bppArgb)) using(var g=Graphics.FromImage(preview))
            using(var font=new Font("Arial",14)) using(var brush=new SolidBrush(Color.White)) {
                g.Clear(Color.FromArgb(30,39,51));
                Bitmap[] images={combined,north,east,back,front,west};
                string[] labels={"SOUTH - combined","NORTH - front pass","EAST - front pass","SOUTH - back pass","SOUTH - front pass","WEST - front pass"};
                for(int i=0;i<6;i++) {
                    int x=i%3*340,y=i/3*370;
                    g.DrawString(labels[i],font,brush,x+12,y+8);
                    g.DrawImage(images[i],x+10,y+38,320,320);
                }
                preview.Save(output,ImageFormat.Png);
            }
        }
        Console.WriteLine("PASS: unchanged renderer/Defs; exact south split; north composite; east/west mirror; empty backs and native apparel; alpha and dimensions.");
    }
}
'@
[XianxuThunderChecks]::Run($textures, (Join-Path $PSScriptRoot 'FourDirections_QA.png'))
