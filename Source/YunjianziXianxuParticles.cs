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

        internal static void Draw(Pawn pawn, Vector3 origin, Rot4 rotation, float scale, int ticks, Vector3 depthAxis)
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
                    // Three emitters trace the lotus crown in every view; the
                    // remaining sparks emerge along the flowing tails.
                    if (i < 3) progress = .025f + i * .075f;
                    XianxuParticleMotion.Point(rot, mirror, progress, out float x, out float y);
                    XianxuParticleMotion.Drift(rot, mirror, i, age, out float driftX, out float driftY);
                    x += driftX;
                    y += driftY;
                    float opacity = Mathf.Sin(age * Mathf.PI) * .85f;
                    Color color = i % 2 == 0 ? new Color(.48f,.86f,1f,opacity) : new Color(.75f,.55f,1f,opacity);
                    DrawGlow(At(origin,scale,rot,depthAxis,x,y), (.025f + .045f * (1f-age)) * scale, color);
                }

                // Two staggered trains of light climb the original lightning
                // cores, with short fading wakes and no accumulated objects.
                for (int flow = 0; flow < XianxuParticleMotion.FlowsPerLane; flow++)
                {
                    float lead = XianxuParticleMotion.FlowProgress(ticks, pawn.thingIDNumber, flow);
                    for (int sample = 0; sample < XianxuParticleMotion.SamplesPerFlow; sample++)
                    {
                        float p = lead + sample * .018f;
                        if (p > 1f) break;
                        XianxuParticleMotion.Point(rot, mirror, p, out float fx, out float fy);
                        float alpha = .62f * (1f - sample / (float)XianxuParticleMotion.SamplesPerFlow);
                        Color color = flow == 0 ? new Color(.58f,.9f,1f,alpha) : new Color(.76f,.52f,1f,alpha);
                        DrawGlow(At(origin,scale,rot,depthAxis,fx,fy), (.10f - sample * .012f) * scale, color);
                    }
                }

                // A local lotus shimmer breathes with the full-silhouette glow.
                XianxuParticleMotion.Point(rot,mirror,.10f,out float cx,out float cy);
                DrawGlow(At(origin,scale,rot,depthAxis,cx,cy), .18f * scale,
                    new Color(.65f,.58f,1f,XianxuParticleMotion.GlowAlpha(ticks,pawn.thingIDNumber)));

                float head = XianxuParticleMotion.FlowProgress(ticks, pawn.thingIDNumber, 0);
                if (((long)ticks + pawn.thingIDNumber) % 36 >= 18) continue;
                for (int i=0;i<5;i++)
                {
                    float p0 = head + i * .013f, p1 = p0 + .013f;
                    if (p1 > 1f) break;
                    XianxuParticleMotion.Point(rot,mirror,p0,out float x0,out float y0);
                    XianxuParticleMotion.Point(rot,mirror,p1,out float x1,out float y1);
                    // The same displacement is reflected for the opposite lane.
                    x0 += outward * Mathf.Sin(((ticks/3)%360) * 1.7f + i*2.3f) * 4f;
                    x1 += outward * Mathf.Sin(((ticks/3)%360) * 1.7f + (i+1)*2.3f) * 4f;
                    Vector3 a=At(origin,scale,rot,depthAxis,x0,y0), b=At(origin,scale,rot,depthAxis,x1,y1);
                    Vector3 delta=b-a;
                    Properties.SetColor(ShaderPropertyIDs.Color,new Color(.7f,.9f,1f,.9f));
                    Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS((a+b)*.5f,
                        Quaternion.LookRotation(delta),new Vector3(.017f*scale,1f,delta.magnitude+.008f)),
                        Glow,0,null,0,Properties);
                }
            }
        }

        private static Vector3 At(Vector3 origin,float scale,int rotation,Vector3 depthAxis,float x,float y)
        {
            XianxuDrape.Position(rotation,x,y,scale,out float dx,out float depth,out float dz);
            return origin + new Vector3(dx,0f,dz) + depthAxis * (depth + .002f);
        }

        private static void DrawGlow(Vector3 position,float size,Color color)
        {
            Properties.SetColor(ShaderPropertyIDs.Color,color);
            Graphics.DrawMesh(MeshPool.plane10,Matrix4x4.TRS(position,Quaternion.identity,
                new Vector3(size,1f,size)),Glow,0,null,0,Properties);
        }
    }
}
