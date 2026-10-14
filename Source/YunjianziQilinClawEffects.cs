using RimWorld;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    internal static class YunjianziQilinClawEffects
    {
        internal static void Play(Pawn caster, Vector3 impact, int stage, ref int lastVariant)
        {
            if (caster == null || !caster.Spawned || caster.Map == null) return;

            // Visual variation must not consume the combat simulation's random stream.
            Rand.PushState(caster.thingIDNumber ^ Find.TickManager.TicksGame);
            try
            {
                int variant = Rand.Range(0, lastVariant >= 0 && lastVariant < 4 ? 3 : 4);
                if (lastVariant >= 0 && lastVariant < 4 && variant >= lastVariant) variant++;
                FleckDef fleck;
                float textureHeading;
                switch (variant)
                {
                    case 0: fleck = YJZDefOf.YJZ_Fleck_QilinThunderClawA; textureHeading = 135f; break;
                    case 1: fleck = YJZDefOf.YJZ_Fleck_QilinThunderClawB; textureHeading = 90f; break;
                    case 2: fleck = YJZDefOf.YJZ_Fleck_QilinThunderClawA; textureHeading = 135f; break;
                    default: fleck = YJZDefOf.YJZ_Fleck_QilinThunderClawB; textureHeading = 90f; break;
                }

                Vector3 direction = impact - caster.DrawPos;
                direction.y = 0f;
                float heading = direction.sqrMagnitude > .001f
                    ? Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg : 0f;
                // The old arcs spanned caster-to-target distance (at most 2.8 cells).
                // Match that length; diagonal art occupies about 1.3 canvas widths.
                float span = Mathf.Clamp(direction.magnitude, 1.35f, 2.8f);
                float artLength = variant == 0 || variant == 2 ? 1.3f : 1f;
                float size = span * (stage >= 5 ? 1.06f : 1f) / artLength;
                FleckCreationData data = FleckMaker.GetDataStatic(impact, caster.Map, fleck, size);
                // Modest emissive gain preserves the approved purple texture and alpha.
                data.instanceColor = new Color(1.12f, 1.12f, 1.12f, 1f);
                // Graphic_Fleck selects its mirrored mesh for negative X scale.
                // Share the same rotation within each pair to keep the left/right rake distinct.
                data.exactScale = new Vector3(variant >= 2 ? -size : size, 1f, size);
                data.rotation = heading - textureHeading + Rand.Range(-12f, 12f);
                caster.Map.flecks.CreateFleck(data);
                lastVariant = variant;
            }
            finally
            {
                Rand.PopState();
            }
        }
    }
}
