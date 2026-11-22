using UnityEngine;
using Verse;

namespace Yunjianzi
{
    public sealed partial class Building_YunjianziNode
    {
        // UV windows select original ImageGen atlas frames without resampling the art.
        // Geometry uses a shared pixels-per-cell ratio and bottom-centre world anchor.
        private static readonly RectInt[,] Bounds = {
            { new RectInt(160,126,461,514), new RectInt(129,131,457,509), new RectInt(101,379,513,261) },
            { new RectInt(57,28,660,648), new RectInt(53,30,649,644), new RectInt(45,291,646,380) }
        };
        private static readonly Mesh[,] Meshes = new Mesh[2,3];
        private static readonly Material[] Materials = new Material[2];
        private static readonly MaterialPropertyBlock Tint = new MaterialPropertyBlock();
        private static Material NodeMaterial(int tier)
        {
            if(Materials[tier]==null)
                Materials[tier]=MaterialPool.MatFrom("Yunjianzi/Buildings/PursuitNodes/"+
                    (tier==0?"BasicStates":"AdvancedStates"),ShaderDatabase.Transparent);
            return Materials[tier];
        }
        private static Mesh NodeMesh(int tier,int frame)
        {
            if(Meshes[tier,frame]!=null)return Meshes[tier,frame];
            RectInt b=Bounds[tier,frame];
            float pixelsPerCell=(tier==0?513f/5f:660f/7f);
            float half=b.width/pixelsPerCell/2f, height=b.height/pixelsPerCell;
            float u0=(frame*724+b.x)/2172f,u1=(frame*724+b.xMax)/2172f;
            float v0=1f-b.yMax/724f,v1=1f-b.y/724f;
            var mesh=new Mesh {name="YJZ_Node_"+tier+"_"+frame};
            mesh.vertices=new[]{new Vector3(-half,0,0),new Vector3(-half,0,height),new Vector3(half,0,height),new Vector3(half,0,0)};
            mesh.uv=new[]{new Vector2(u0,v0),new Vector2(u0,v1),new Vector2(u1,v1),new Vector2(u1,v0)};
            mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
            Meshes[tier,frame]=mesh;return mesh;
        }
        protected override void DrawAt(Vector3 drawLoc,bool flip=false)
        {
            var n=State;
            int tier=def.size.x>=7?1:0;
            bool phase=n!=null && !n.complete && n.defending;
            int frame=n!=null && n.complete?2:phase?0:HitPoints<=MaxHitPoints/2?1:0;
            Mesh mesh=NodeMesh(tier,frame);Material material=NodeMaterial(tier);
            drawLoc.z-=def.size.z*.5f;
            if(!phase)
            {
                Tint.SetColor(ShaderPropertyIDs.Color,Color.white);
                Graphics.DrawMesh(mesh,Matrix4x4.TRS(drawLoc,Quaternion.identity,Vector3.one),material,0,null,0,Tint);
                return;
            }
            // Nine restrained subpixel samples soften the ORIGINAL complete sprite.
            // Uniform alpha applies to stones, roots, outline and void together; no replacement polygons.
            Tint.SetColor(ShaderPropertyIDs.Color,new Color(1,1,1,.0389f));
            for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
            {
                Vector3 p=drawLoc+new Vector3(x*.035f,0,z*.035f);
                Graphics.DrawMesh(mesh,Matrix4x4.TRS(p,Quaternion.identity,Vector3.one),material,0,null,0,Tint);
            }
        }
    }
}
