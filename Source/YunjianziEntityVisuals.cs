using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    // Import three original atlas cells at runtime. No repainted or flattened sprite derivatives.
    public class Graphic_YunjianziEntity : Graphic
    {
        readonly Material[] materials=new Material[4];
        public override Material MatNorth=>materials[0];
        public override Material MatEast=>materials[1];
        public override Material MatSouth=>materials[2];
        public override Material MatWest=>materials[3];
        public override Material MatSingle=>MatSouth;
        public override bool ShouldDrawRotated=>false;
        public override Material MatAt(Rot4 rot,Thing thing=null)=>materials[rot.AsInt];
        public override void Init(GraphicRequest req)
        {
            data=req.graphicData;path=req.path;color=req.color;colorTwo=req.colorTwo;drawSize=req.drawSize;
            var source=ContentFinder<Texture2D>.Get(path);
            RenderTexture previous=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32);
            var read=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);
            try
            {
                Graphics.Blit(source,rt);RenderTexture.active=rt;
                read.ReadPixels(new Rect(0,0,source.width,source.height),0,0);read.Apply();
                int width=source.width/3;
                for(int direction=0;direction<4;direction++)
                {
                    int frame=direction==0?2:direction==2?0:1;
                    Color[] pixels=read.GetPixels(frame*width,0,width,source.height);
                    bool mirror=direction==1;
                    if(path.EndsWith("Sniper",StringComparison.Ordinal))mirror=direction==3;
                    if(mirror)for(int y=0;y<source.height;y++)for(int x=0;x<width/2;x++)
                    {int a=y*width+x,b=y*width+width-1-x;var t=pixels[a];pixels[a]=pixels[b];pixels[b]=t;}
                    var tex=new Texture2D(width,source.height,TextureFormat.RGBA32,false);
                    tex.name=path+"_"+direction;tex.wrapMode=TextureWrapMode.Clamp;tex.filterMode=FilterMode.Bilinear;
                    tex.SetPixels(pixels);tex.Apply(false,true);
                    materials[direction]=MaterialPool.MatFrom(tex,req.shader,color);
                }
            }
            finally {RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(read);}
        }
        public override void TryInsertIntoAtlas(TextureAtlasGroup groupKey)
        {foreach(var mat in materials)GlobalTextureAtlasManager.TryInsertStatic(groupKey,(Texture2D)mat.mainTexture,null);}
        public override Graphic GetColoredVersion(Shader shader,Color first,Color second)
            =>GraphicDatabase.Get<Graphic_YunjianziEntity>(path,shader,drawSize,first,second,data);
    }

    public class PawnRenderNodeWorker_YunjianziEntity : PawnRenderNodeWorker_AnimalBody
    {
        public override Vector3 OffsetFor(PawnRenderNode node,PawnDrawParms parms,out Vector3 pivot)
        {
            Vector3 result=base.OffsetFor(node,parms,out pivot);
            var c=parms.pawn.TryGetComp<CompYunjianziEntity>();
            if(c==null || !c.Active || parms.flags.FlagSet(PawnRenderFlags.Portrait))return result;
            int now=Find.TickManager.TicksGame;
            if(now<c.motionEnd)
            {
                float t=(now-c.motionStart)/(float)Math.Max(1,c.motionEnd-c.motionStart);
                result+=c.motionDirection*Mathf.Sin(t*Mathf.PI)*(c.Role==6?-.23f:.20f);
            }
            return result;
        }
        public override Quaternion RotationFor(PawnRenderNode node,PawnDrawParms parms)
        {
            var original=base.RotationFor(node,parms);var c=parms.pawn.TryGetComp<CompYunjianziEntity>();
            if(c==null || !c.Active || parms.flags.FlagSet(PawnRenderFlags.Portrait))return original;
            int now=Find.TickManager.TicksGame;
            float angle=0;
            if(now<c.motionEnd)angle=Mathf.Sin((now-c.motionStart)*Mathf.PI/Math.Max(1,c.motionEnd-c.motionStart))*(c.Role==5?14:7);
            else if(c.windupUntil>now)angle=Mathf.Sin(now*.4f)*1.5f;
            return original*Quaternion.AngleAxis(angle,Vector3.up);
        }
    }
    public static class EntityVisuals
    {
        public static readonly Material Red=SolidColorMaterials.SimpleSolidColorMaterial(new Color(.65f,.16f,.22f,.8f));
        public static readonly Material Stone=SolidColorMaterials.SimpleSolidColorMaterial(new Color(.32f,.29f,.30f));
        static Mesh shard;
        static Mesh Shard
        {
            get
            {
                if(shard!=null)return shard;
                shard=new Mesh{name="YJZ_FleshStoneShard"};
                shard.vertices=new[]{new Vector3(0,0,.65f),new Vector3(.23f,0,.12f),new Vector3(.13f,0,-.45f),new Vector3(-.12f,0,-.35f),new Vector3(-.19f,0,.08f)};
                shard.triangles=new[]{0,1,4,1,2,4,2,3,4};shard.RecalculateNormals();shard.RecalculateBounds();return shard;
            }
        }
        public static void Ring(Vector3 center,float radius)
        {
            center.y=AltitudeLayer.MetaOverlays.AltitudeFor();
            for(int i=0;i<28;i++)
            {
                float a=i*Mathf.PI*2/28,b=(i+1)*Mathf.PI*2/28;
                GenDraw.DrawLineBetween(center+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius),center+new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius),Red,.065f);
            }
        }
        public static void Pulse(Map map,Vector3 pos,float size)
        {
            if(map==null)return;
            map.GetComponent<MapComponent_YunjianziEntityEffects>().Add(pos,size);
        }
        public static void Projectile(Vector3 pos,Quaternion rotation,float size)
        {
            pos.y=AltitudeLayer.Projectile.AltitudeFor();
            Graphics.DrawMesh(Shard,Matrix4x4.TRS(pos,rotation,new Vector3(size,1,size)),Stone,0);
            Graphics.DrawMesh(Shard,Matrix4x4.TRS(pos+new Vector3(0,.001f,0),rotation,new Vector3(size*.3f,1,size*.72f)),Red,0);
        }
        public static void FallingStone(Vector3 target,float remaining)
        {
            target+=new Vector3(.5f,0,.5f+Mathf.Clamp01(remaining/45f)*6);
            target.y=AltitudeLayer.Projectile.AltitudeFor();
            var graphic=DefDatabase<ThingDef>.GetNamed("YJZ_EntityTombstone").graphicData.Graphic;
            Graphics.DrawMesh(MeshPool.GridPlane(graphic.drawSize),target,Quaternion.identity,graphic.MatSingle,0);
        }
        public static Vector3 Muzzle(Thing launcher,Vector3 fallback)
        {
            var pawn=launcher as Pawn;var comp=pawn?.TryGetComp<CompYunjianziEntity>();
            if(comp==null)return fallback;
            // Atlas-relative muzzle points follow the actual enlarged silhouettes.
            Vector2 size=pawn.kindDef.lifeStages[0].bodyGraphicData.drawSize;
            float x,z;
            if(comp.Role==6)
            {
                x=pawn.Rotation.FacingCell.x*.46f;
                z=pawn.Rotation==Rot4.North?.27f:pawn.Rotation==Rot4.South?.06f:.04f;
            }
            else
            {
                x=pawn.Rotation==Rot4.North?-.25f:pawn.Rotation==Rot4.South?.25f:pawn.Rotation.FacingCell.x*.31f;
                z=.28f;
            }
            return fallback+new Vector3(x*size.x,0,z*size.y);
        }
    }
    public class MapComponent_YunjianziEntityEffects : MapComponent
    {
        struct Flash {public Vector3 pos;public float size;public int tick;}
        readonly List<Flash> flashes=new List<Flash>();
        public MapComponent_YunjianziEntityEffects(Map map):base(map){}
        public void Add(Vector3 p,float size)
        {
            if(flashes.Count>=200)flashes.RemoveAt(0);
            flashes.Add(new Flash{pos=p,size=size,tick=Find.TickManager.TicksGame});
        }
        public override void MapComponentTick()=>flashes.RemoveAll(f=>Find.TickManager.TicksGame-f.tick>26);
        public override void MapComponentUpdate()
        {
            if(Find.CurrentMap!=map)return;
            int now=Find.TickManager.TicksGame;
            flashes.RemoveAll(f=>now-f.tick>26);
            foreach(var f in flashes)
            {
                float t=Mathf.Clamp01((now-f.tick)/26f);
                EntityVisuals.Ring(f.pos,f.size*(.25f+t));
                for(int i=0;i<5;i++)
                {
                    float a=i*1.2566f;
                    var p=f.pos+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*t*f.size;
                    EntityVisuals.Projectile(p,Quaternion.AngleAxis(i*72+t*180,Vector3.up),.35f*(1-t));
                }
            }
        }
    }
    public class Projectile_YunjianziSpine : Bullet
    {
        public override void Launch(Thing launcher,Vector3 origin,LocalTargetInfo usedTarget,LocalTargetInfo intendedTarget,ProjectileHitFlags hitFlags,bool preventFriendlyFire=false,Thing equipment=null,ThingDef targetCoverDef=null)
            =>base.Launch(launcher,EntityVisuals.Muzzle(launcher,origin),usedTarget,intendedTarget,hitFlags,preventFriendlyFire,equipment,targetCoverDef);
        protected override void DrawAt(Vector3 drawLoc,bool flip=false)=>EntityVisuals.Projectile(ExactPosition,ExactRotation,.8f);
        protected override void Impact(Thing hitThing,bool blockedByShield=false)
        {EntityVisuals.Pulse(Map,ExactPosition,.6f);base.Impact(hitThing,blockedByShield);}
    }
    public class Projectile_YunjianziFleshShell : Projectile_Explosive
    {
        public override void Launch(Thing launcher,Vector3 origin,LocalTargetInfo usedTarget,LocalTargetInfo intendedTarget,ProjectileHitFlags hitFlags,bool preventFriendlyFire=false,Thing equipment=null,ThingDef targetCoverDef=null)
            =>base.Launch(launcher,EntityVisuals.Muzzle(launcher,origin),usedTarget,intendedTarget,hitFlags,preventFriendlyFire,equipment,targetCoverDef);
        protected override void DrawAt(Vector3 drawLoc,bool flip=false)=>base.DrawAt(drawLoc,flip);
        protected override void Impact(Thing hitThing,bool blockedByShield=false)
        {if(!blockedByShield)EntityVisuals.Pulse(Map,ExactPosition,2.8f);base.Impact(hitThing,blockedByShield);}
    }
}
