using UnityEngine;
using Verse;

namespace Yunjianzi
{
    [StaticConstructorOnStartup]
    internal static class XianxuParticles
    {
        // Native LightningGlow is also used by this mod's lightning renderer.
        private static readonly Material Glow = MaterialPool.MatFrom("Things/Mote/LightningGlow", ShaderDatabase.MoteGlow);
        private static readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();

        internal static void Draw(Pawn pawn, Vector3 origin, Rot4 rotation, bool front, float scale, int ticks)
        {
            if (!pawn.Spawned || pawn.Map == null || pawn.Map != Find.CurrentMap || pawn.Position.Fogged(pawn.Map)) return;
            int rot = rotation.AsInt;
            bool side = rot == 1 || rot == 3;
            // These particles are evaluated from game time, not emitted every
            // render frame. Pausing freezes them, speed-up never increases the
            // active count, and unequipping/despawning leaves no pooled effects.
            for (int lane = 0; lane < (side ? 1 : 2); lane++)
            {
                bool mirror = lane == 1;
                float outward = side ? (rot == 1 ? 1f : -1f) : (mirror ? 1f : -1f);
                for (int i = 0; i < XianxuParticleMotion.SparksPerLane; i++)
                {
                    XianxuParticleMotion.Spark(ticks, pawn.thingIDNumber, i, out float progress, out float age);
                    XianxuParticleMotion.Point(rot, mirror, progress, out float x, out float y);
                    // Classify at the anchor, so drifting never jumps layers.
                    if (XianxuParticleMotion.InFront(rot, y) != front) continue;
                    x += outward * age * 18f;
                    y -= age * 10f;
                    float opacity = Mathf.Sin(age * Mathf.PI) * .65f;
                    Color color = i % 2 == 0 ? new Color(.48f,.86f,1f,opacity) : new Color(.75f,.55f,1f,opacity);
                    DrawGlow(At(origin,scale,x,y), (.018f + .032f * (1f-age)) * scale, color);
                }

                // A soft travelling pulse followed by a short, flickering arc.
                float head = ((ticks + pawn.thingIDNumber % 150) % 150) / 150f;
                XianxuParticleMotion.Point(rot, mirror, head, out float hx, out float hy);
                float pulse = .32f + .12f * Mathf.Sin(ticks * .06f);
                if (XianxuParticleMotion.InFront(rot, hy) == front)
                    DrawGlow(At(origin,scale,hx,hy), .13f * scale, new Color(.55f,.8f,1f,pulse));
                if ((ticks + pawn.thingIDNumber % 42) % 42 >= 14) continue;
                for (int i=0;i<5;i++)
                {
                    float p0 = head + i * .013f, p1 = p0 + .013f;
                    if (p1 > 1f) break;
                    XianxuParticleMotion.Point(rot,mirror,p0,out float x0,out float y0);
                    XianxuParticleMotion.Point(rot,mirror,p1,out float x1,out float y1);
                    if (XianxuParticleMotion.InFront(rot,y0) != front || XianxuParticleMotion.InFront(rot,y1) != front) continue;
                    // The same displacement is reflected for the opposite lane.
                    x0 += outward * Mathf.Sin((ticks/3) * 1.7f + i*2.3f) * 3f;
                    x1 += outward * Mathf.Sin((ticks/3) * 1.7f + (i+1)*2.3f) * 3f;
                    Vector3 a=At(origin,scale,x0,y0), b=At(origin,scale,x1,y1);
                    Vector3 delta=b-a;
                    Properties.SetColor(ShaderPropertyIDs.Color,new Color(.7f,.9f,1f,.8f));
                    Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS((a+b)*.5f,
                        Quaternion.LookRotation(delta),new Vector3(.012f*scale,1f,delta.magnitude+.008f)),
                        Glow,0,null,0,Properties);
                }
            }
        }

        private static Vector3 At(Vector3 origin,float scale,float x,float y)
        {
            return origin + new Vector3((x/512f-.5f)*scale,.002f,(.5f-y/512f)*scale);
        }

        private static void DrawGlow(Vector3 position,float size,Color color)
        {
            Properties.SetColor(ShaderPropertyIDs.Color,color);
            Graphics.DrawMesh(MeshPool.plane10,Matrix4x4.TRS(position,Quaternion.identity,
                new Vector3(size,1f,size)),Glow,0,null,0,Properties);
        }
    }
}
