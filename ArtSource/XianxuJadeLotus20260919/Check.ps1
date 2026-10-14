$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$mod = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$textures = Join-Path $mod 'Content/Textures/Yunjianzi/Apparel/JiuxiaoXianxu'
$geometry = Get-Content -LiteralPath "$mod/Source/YunjianziXianxuDrape.cs" -Raw
$checks = @'
namespace Yunjianzi {
public static class JadeLotusChecks {
    static void Check(bool ok,string message) { if(!ok) throw new System.Exception(message); }
    static System.Drawing.Bitmap Load(string path,string suffix) {
        return new System.Drawing.Bitmap(System.IO.Path.Combine(path,"JiuxiaoXianxu"+suffix+".png"));
    }
    public static void Run(string root,string output) {
        foreach(string folder in new[]{root,System.IO.Path.Combine(root,"Normal")}) {
            using(var south=Load(folder,"")) using(var north=Load(folder,"_Front_north"))
            using(var east=Load(folder,"_Front_east")) using(var west=Load(folder,"_Front_west")) {
                foreach(var b in new[]{south,north,east,west}) Check(b.Width==512 && b.Height==512,"512 RGBA canvas required");
                int clear=0;
                for(int y=0;y<512;y++) for(int x=0;x<512;x++) {
                    int c=south.GetPixel(x,y).ToArgb();
                    Check(c==south.GetPixel(511-x,y).ToArgb(),"Front symmetry");
                    Check(c==north.GetPixel(x,y).ToArgb(),"Complete north image");
                    Check(east.GetPixel(x,y).ToArgb()==west.GetPixel(511-x,y).ToArgb(),"Side mirror");
                    if(south.GetPixel(x,y).A==0) clear++;
                }
                Check(clear>512*512/2,"Transparent head/body area");
            }
        }
        using(var preview=new System.Drawing.Bitmap(1360,450))
        using(var g=System.Drawing.Graphics.FromImage(preview))
        using(var font=new System.Drawing.Font("Arial",12)) {
            g.Clear(System.Drawing.Color.FromArgb(30,39,51));
            string[] titles={"JADE - FRONT","JADE - SIDE","THUNDER - FRONT","THUNDER - SIDE"};
            for(int view=0;view<4;view++) {
                string folder=view<2?System.IO.Path.Combine(root,"Normal"):root;
                int rot=view%2==0?2:1;
                using(var sprite=Load(folder,rot==2?"":"_Front_east")) {
                    g.DrawString(titles[view],font,System.Drawing.Brushes.White,view*340+12,8);
                    // Diagnostic projection uses the actual runtime position
                    // function, with no synthetic pawn or claimed in-game shot.
                    for(int row=0;row<512;row++) {
                        float x0,d,z0,x1,z1;
                        XianxuDrape.Position(rot,0,row,1.5f,out x0,out d,out z0);
                        XianxuDrape.Position(rot,512,row+1,1.5f,out x1,out d,out z1);
                        g.DrawImage(sprite,new System.Drawing.RectangleF(view*340+170+x0*180,250-z0*180,
                            (x1-x0)*180,System.Math.Max(.5f,(z0-z1)*180)),
                            new System.Drawing.RectangleF(0,row,512,1),System.Drawing.GraphicsUnit.Pixel);
                    }
                }
            }
            g.DrawString("Geometry preview only - no pawn, hair or wing occlusion simulation",font,System.Drawing.Brushes.LightGray,12,425);
            preview.Save(output,System.Drawing.Imaging.ImageFormat.Png);
        }
        System.Console.WriteLine("PASS: both forms have complete RGBA sprites, mirrored sides and transparent centers; runtime geometry preview saved.");
    }
}}
'@
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition ($geometry + "`n" + $checks)
[Yunjianzi.JadeLotusChecks]::Run($textures, "$PSScriptRoot/Forms_Geometry_QA.png")
