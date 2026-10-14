using HarmonyLib;
using RimWorld;
using Verse;

namespace Yunjianzi
{
    /// <summary>
    /// Yunjianzi's visible hair, ornaments, and halo are fixed HAR body addons.
    /// Vanilla only sees her deliberately hidden Bald HairDef and can therefore
    /// mistake the fixed appearance for an ideoligion-forbidden style.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_StyleTracker), nameof(Pawn_StyleTracker.CanDesireLookChange), MethodType.Getter)]
    internal static class Patch_PawnStyleTracker_YunjianziFixedAppearance
    {
        private static void Postfix(Pawn_StyleTracker __instance, ref bool __result)
        {
            if (!__result || __instance == null) return;

            Pawn pawn = __instance.pawn;
            CompYunjianziCultivation cultivation = pawn == null
                ? null
                : pawn.TryGetComp<CompYunjianziCultivation>();
            if (cultivation != null && cultivation.IsYunjianzi)
                __result = false;
        }
    }
}
