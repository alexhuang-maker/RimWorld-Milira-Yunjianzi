using RimWorld;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    public sealed class CompProperties_CloudSwordArray : CompProperties
    {
        public int swordCount = 6;
        public float radius = 1.22f;

        public CompProperties_CloudSwordArray()
        {
            compClass = typeof(CompCloudSwordArray);
        }
    }

    [StaticConstructorOnStartup]
    public sealed class CompCloudSwordArray : ThingComp
    {
        private static readonly Material Sword = MaterialPool.MatFrom(
            "Yunjianzi/Apparel/CloudSwordArray/CloudSwordArrayV2", ShaderDatabase.TransparentPostLight);
        private static readonly Material Glow = MaterialPool.MatFrom(
            "Yunjianzi/Apparel/CloudSwordArray/CloudSwordArrayV2", ShaderDatabase.MoteGlow);

        private CompProperties_CloudSwordArray Props => (CompProperties_CloudSwordArray)props;

        public override void CompDrawWornExtras()
        {
            Pawn pawn = (parent as Apparel)?.Wearer;
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed) return;
            if (!pawn.Drafted && pawn.mindState?.enemyTarget == null) return;

            Vector3 center = pawn.DrawPos;
            int now = Find.TickManager.TicksGame;
            float breathe = Mathf.Sin(now * .075f) * .06f;
            int count = Mathf.Max(6, Props.swordCount);
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? .5f : i / (float)(count - 1);
                // A wide halo-like fan leaves the pawn readable while evoking a
                // ring of weightless spirit swords behind her.
                float angle = Mathf.Lerp(-72f, 72f, t);
                float radians = angle * Mathf.Deg2Rad;
                float radius = Props.radius + breathe + Mathf.Sin(now * .11f + i * .9f) * .035f;
                Vector3 offset = new Vector3(Mathf.Sin(radians) * radius, 0f,
                    .10f + Mathf.Cos(radians) * radius * .54f);
                Vector3 loc = center + offset;
                loc.y = center.y + .008f + i * .0002f;
                // The source sprite already points upward. Fan outward from that
                // orientation instead of turning every blade upside down.
                float rotation = angle * .72f + Mathf.Sin(now * .055f + i) * 1.8f;
                float scale = .57f + (1f - Mathf.Abs(t - .5f) * 2f) * .07f;
                Matrix4x4 glow = Matrix4x4.TRS(loc - new Vector3(0f, .002f, 0f),
                    Quaternion.AngleAxis(rotation, Vector3.up), new Vector3(scale * 1.28f, 1f, scale * 1.28f));
                Matrix4x4 body = Matrix4x4.TRS(loc,
                    Quaternion.AngleAxis(rotation, Vector3.up), new Vector3(scale, 1f, scale));
                Graphics.DrawMesh(MeshPool.plane10, glow, Glow, 0);
                Graphics.DrawMesh(MeshPool.plane10, body, Sword, 0);
            }
        }
    }
}
