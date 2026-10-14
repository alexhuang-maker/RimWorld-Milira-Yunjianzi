using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    internal static class YunjianziLightningDamage
    {
        // Purple lightning uses the requested 2.5-cell radius (vanilla is 1.9).
        internal const float MinimumRadius = 2.5f;

        internal static void Apply(IntVec3 cell, Map map, Pawn caster = null,
            float damage = -1f, float armorPenetration = .75f, float radius = MinimumRadius,
            float empDamage = YunjianziEmp.LightningDamage, float empRadius = -1f)
        {
            if (map == null || !cell.InBounds(map) || cell.Fogged(map)) return;
            radius = Mathf.Max(MinimumRadius, radius);
            if (damage < 0f) damage = YJZDefOf.YJZ_ShenxiaoLightning.defaultDamage;
            if (damage <= 0f) return;

            // Retain the abilities' hostile-target policy while delegating blast
            // cells, walls and multi-cell hit deduplication to the vanilla engine.
            empRadius = Mathf.Max(radius, empRadius);
            List<Thing> ignored = IgnoredTargets(cell, map, caster, empRadius);
            // Separate real EMP damage, with the same hostile policy and vanilla
            // explosion occlusion. Visual density limits never suppress gameplay.
            ExplodeEmp(cell, map, caster, empRadius, empDamage, ignored);
            GenExplosion.DoExplosion(cell, map, radius, YJZDefOf.YJZ_ShenxiaoLightning, caster,
                damAmount: Mathf.Max(1, Mathf.RoundToInt(damage)), armorPenetration: armorPenetration,
                damageFalloff: false, ignoredThings: ignored,
                doVisualEffects: false, doSoundEffects: false, screenShakeFactor: 0f);
        }

        internal static void ApplyEmpArea(IntVec3 cell, Map map, Pawn caster, float radius,
            float amount = YunjianziEmp.LightningDamage)
        {
            if (map == null || !cell.InBounds(map) || cell.Fogged(map) || radius <= 0f) return;
            // The teleport shock moves victims immediately: apply EMP to the
            // same snapshot before knockback, rather than chasing them with a
            // delayed explosion wave after they have left the original radius.
            foreach (Thing target in GenRadial.RadialDistinctThingsAround(cell, map, radius, true)
                .Where(t => t != caster && (caster == null || t.HostileTo(caster)) &&
                    (t is Pawn || t is Building)).ToList())
                YunjianziEmp.Apply(target, caster, amount);
            YunjianziPurpleLightning.EmpPulse(cell.ToVector3Shifted(), map, radius);
        }

        private static void ExplodeEmp(IntVec3 cell, Map map, Pawn caster, float radius,
            float amount, List<Thing> ignored)
        {
            if (amount <= 0f) return;
            GenExplosion.DoExplosion(cell, map, radius, DamageDefOf.EMP, caster,
                damAmount: Mathf.Max(1, Mathf.RoundToInt(amount)), armorPenetration: 0f,
                damageFalloff: false, ignoredThings: ignored,
                doVisualEffects: false, doSoundEffects: false, screenShakeFactor: 0f);
        }

        private static List<Thing> IgnoredTargets(IntVec3 cell, Map map, Pawn caster, float radius)
        {
            List<Thing> ignored = new List<Thing>();
            if (caster != null)
            {
                ignored.Add(caster);
                foreach (Thing thing in GenRadial.RadialDistinctThingsAround(cell, map, radius, true))
                    if (!thing.HostileTo(caster) || (!(thing is Pawn) && !(thing is Building)))
                        ignored.Add(thing);
            }
            return ignored;
        }
    }
}
