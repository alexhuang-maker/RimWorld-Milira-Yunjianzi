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
            "Yunjianzi/Apparel/CloudSwordArray/CloudSwordBlade", ShaderDatabase.TransparentPostLight);

        private CompProperties_CloudSwordArray Props => (CompProperties_CloudSwordArray)props;

        public override void CompDrawWornExtras()
        {
            Pawn pawn = (parent as Apparel)?.Wearer;
            if (pawn == null || !pawn.Drafted || !pawn.Spawned || pawn.Dead || pawn.Downed) return;

            Vector3 center = pawn.DrawPos;
            Rot4 facing = pawn.Rotation;
            bool horizontal = facing.IsHorizontal;
            // Side views show the near rank only; the far three are occluded by it.
            center.y += facing == Rot4.North ? .04f : -.018f;
            int count = Props.swordCount;
            int pairs = (count + 1) / 2;
            for (int i = 0; i < count; i++)
            {
                int pair = i / 2;
                float side = i % 2 == 0 ? -1f : 1f;
                if (facing == Rot4.North || facing == Rot4.West) side = -side;
                float spread = pairs <= 1 ? 0f : pair / (float)(pairs - 1);
                float hover = Mathf.Sin(Find.TickManager.TicksGame * .035f + pair * .7f) * .022f;
                bool far = horizontal && (facing == Rot4.East ? side < 0 : side > 0);
                if (far) continue;
                Vector3 local = new Vector3(side * Props.radius * (.43f + .57f * spread),
                    far ? -.004f : 0f, -.12f + .52f * spread + hover + (horizontal ? (far ? .14f : -.14f) : 0f));
                float length = .78f + .18f * spread;
                Quaternion tilt = Quaternion.AngleAxis(180f - side * (12f + 17f * spread), Vector3.up);
                if (horizontal)
                {
                    float rear = facing == Rot4.East ? -1f : 1f;
                    local.x = rear * (.70f + .28f * pair + (far ? .12f : 0f));
                    local.z = -.20f + .18f * pair + (far ? .22f : 0f) + hover;
                    tilt = Quaternion.AngleAxis(180f - rear * (14f + 9f * pair + (far ? 4f : 0f)), Vector3.up);
                }
                Matrix4x4 blade = Matrix4x4.TRS(center + local, tilt,
                    new Vector3(length * (horizontal ? 1.05f : 1.25f), 1f, length));
                Graphics.DrawMesh(MeshPool.plane10, blade, Sword, 0);
            }
        }
    }
}
