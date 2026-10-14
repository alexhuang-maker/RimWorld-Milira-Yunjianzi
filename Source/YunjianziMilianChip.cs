using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Yunjianzi
{
    /// <summary>
    /// Milian Modification generates the workbench recipe from its
    /// MilianComponentDef. This postfix only supplies the cultivation-tree
    /// prerequisite that RecipeDef cannot express in XML.
    /// </summary>
    [HarmonyPatch(typeof(RecipeDef), nameof(RecipeDef.AvailableNow), MethodType.Getter)]
    internal static class Patch_RecipeDef_CloudCommandChipSkillGate
    {
        private static void Postfix(RecipeDef __instance, ref bool __result)
        {
            if (!__result || __instance?.ProducedThingDef != YJZDefOf.YJZ_MilianFitting_CloudCommandChip) return;
            if (Current.Game == null)
            {
                __result = false;
                return;
            }
            __result = Find.Maps.Any(map => map.mapPawns.AllPawns.Any(pawn =>
            {
                CompYunjianziCultivation cultivation = pawn.TryGetComp<CompYunjianziCultivation>();
                return cultivation != null && cultivation.IsYunjianzi &&
                    cultivation.HasSkill(YunjianziSkills.ShenjiAccessories);
            }));
        }
    }
}
