using RimWorld;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    public sealed partial class CompYunjianziCultivation
    {
        private int lastZhuxieSlashVariant = -1;

        internal void PlayZhuxieSlash(Vector3 impact)
        {
            Pawn caster = Pawn;
            if (caster == null || !caster.Spawned || caster.Map == null) return;

            // Keep decorative choices independent of hit/damage random rolls.
            Rand.PushState(caster.thingIDNumber ^ Find.TickManager.TicksGame ^ 0x5A17);
            try
            {
                bool hasPrevious = lastZhuxieSlashVariant >= 0 && lastZhuxieSlashVariant < 4;
                int variant = Rand.Range(0, hasPrevious ? 3 : 4);
                if (hasPrevious && variant >= lastZhuxieSlashVariant) variant++;
                bool diagonal = variant == 0 || variant == 2;
                bool mirrored = variant >= 2;
                FleckDef fleck = diagonal ? YJZDefOf.YJZ_Fleck_ZhuxieSlashA : YJZDefOf.YJZ_Fleck_ZhuxieSlashB;

                Vector3 from = caster.DrawPos;
                Vector3 delta = impact - from;
                delta.y = 0f;
                float angle = delta.sqrMagnitude < .001f ? 0f : Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                Vector3 centre = Vector3.Lerp(from, impact, .68f);
                // Match the old 4.8-wide slash and its realm scaling. The diagonal
                // artwork's long axis spans about 1.3 times its square canvas size.
                float realmScale = .72f + (Realm - 2) * .08f;
                float size = 4.8f * realmScale / (diagonal ? 1.3f : 1f);
                FleckCreationData data = FleckMaker.GetDataStatic(centre, caster.Map, fleck, size);
                data.exactScale = new Vector3(mirrored ? -size : size, 1f, size);
                // Both variants in a mirror pair share rotation, preserving the
                // opposite sweep instead of rotating the mirror back into the original.
                data.rotation = angle + (diagonal ? 135f : 90f);
                caster.Map.flecks.CreateFleck(data);
                lastZhuxieSlashVariant = variant;
            }
            finally
            {
                Rand.PopState();
            }
        }
    }
}
