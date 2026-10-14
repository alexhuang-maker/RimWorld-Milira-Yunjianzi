using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    internal static class YunjianziProjectileShieldUtility
    {
        private static readonly AccessTools.FieldRef<ProjectileProperties, int> BaseDamage =
            AccessTools.FieldRefAccess<ProjectileProperties, int>("damageAmountBase");

        internal static float InterceptionCost(Projectile projectile, int stage)
        {
            ProjectileProperties properties = projectile.def.projectile;
            // Some mod projectiles implement their own impact and have neither
            // a base damage amount nor a DamageDef. Querying vanilla DamageAmount
            // for those logs an error; retain the shield's minimum interception cost.
            if (properties == null || (properties.damageDef == null && BaseDamage(properties) == -1))
                return 12f;

            return Mathf.Max(12f, projectile.DamageAmount * (stage >= 4 ? .6f : 1f));
        }
    }

    internal static class YunjianziShenjiRepairUtility
    {
        internal static bool NeedsRepair(Pawn mech)
        {
            if (mech.Dead || mech.health?.hediffSet == null || mech.TryGetComp<CompMechRepairable>() == null)
                return false;

            foreach (Hediff hediff in mech.health.hediffSet.hediffs)
            {
                if (hediff is Hediff_MissingPart || (hediff is Hediff_Injury && hediff.Severity > 0f))
                    return true;
            }

            if (mech.apparel != null)
            {
                foreach (Apparel apparel in mech.apparel.WornApparel)
                {
                    // Indestructible utility gear has no repairable durability.
                    // Milira's generic apparel check does not exclude it.
                    if (apparel.def.useHitPoints && apparel.HitPoints < apparel.MaxHitPoints)
                        return true;
                }
            }

            // Shenji weapons are manufactured and equipped by the player.
            // An empty weapon slot is not a wound for the repair job to heal.
            return false;
        }
    }

    [HarmonyPatch(typeof(MechRepairUtility), nameof(MechRepairUtility.CanRepair))]
    internal static class Patch_Shenji_RepairCompletion
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Pawn mech, ref bool __result)
        {
            if (mech?.TryGetComp<CompYunjianziShenji>() != null)
                __result = YunjianziShenjiRepairUtility.NeedsRepair(mech);
        }
    }

    [HarmonyPatch(typeof(Skyfaller_FlyingPawn), "DrawAt")]
    internal static class Patch_FlyingPawn_InitializeRenderTree
    {
        private static void Prefix(Skyfaller_FlyingPawn __instance)
        {
            // The carried pawn is outside the map's normal pawn draw phases.
            // Its cached pre-render result can outlive a dirtied render tree.
            // Initialize on the main draw thread before DrawNowAt consumes it.
            PawnRenderer renderer = __instance.Pawn?.Drawer?.renderer;
            if (renderer != null && !renderer.renderTree.Resolved)
                renderer.EnsureGraphicsInitialized();
        }
    }
}
