using AriandelLibrary.FA;
using FacialAnimation;
using HarmonyLib;
using Verse;

namespace Yunjianzi.FA
{
    [StaticConstructorOnStartup]
    internal static class FacialAnimationBootstrap
    {
        static FacialAnimationBootstrap()
        {
            new Harmony("alex.miliraprivateexpansion.yunjianzi.facialanimation").PatchAll();
        }
    }

    // Ariandel's normal NPCFA route intentionally requires a registered unique
    // NPC instance. Character Editor can create unregistered duplicate previews,
    // so bind the face by PawnKindDef as well. This keeps every test generation
    // visually identical without weakening the persistent unique identity system.
    [HarmonyPatch(typeof(FacialAnimationControllerComp), "GatherPawnParam")]
    [HarmonyPriority(Priority.Last)]
    internal static class Patch_FacialAnimationControllerComp_GatherPawnParam
    {
        private static void Postfix(FacialAnimationControllerComp __instance)
        {
            Pawn pawn = __instance == null ? null : __instance.parent as Pawn;
            if (pawn == null || pawn.kindDef == null || pawn.kindDef.defName != "YJZ_Yunjianzi")
            {
                return;
            }

            foreach (ThingComp comp in pawn.AllComps)
            {
                FATools_NPC.TrySetNPCFaceType(pawn, comp, "Yunjianzi");
            }
        }
    }
}
