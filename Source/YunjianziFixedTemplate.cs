using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Yunjianzi
{
    public sealed class FixedSkillSpec
    {
        public SkillDef skill;
        public int level;
        public Passion passion = Passion.None;
    }

    public sealed class FixedPawnTemplateExtension : DefModExtension
    {
        public List<FixedSkillSpec> skills = new List<FixedSkillSpec>();
        public ThingDef onlyInitialApparel;
        public bool clearInitialEquipment;
        public bool clearInitialInventory;
    }

    [StaticConstructorOnStartup]
    internal static class YunjianziBootstrap
    {
        static YunjianziBootstrap()
        {
            // Patch the assembly exactly once.  A second PatchAll used to live in
            // the optional facial-animation integration and installed every
            // postfix/prefix twice when that integration was present.
            new Harmony("alex.miliraprivateexpansion.yunjianzi").PatchAll();
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) })]
    [HarmonyAfter("Ariandel.AriandelLibrary.Patch")]
    [HarmonyPriority(Priority.Last)]
    internal static class Patch_PawnGenerator_GeneratePawn
    {
        private static void Postfix(Pawn __result)
        {
            if (__result == null || __result.kindDef == null)
            {
                return;
            }

            FixedPawnTemplateExtension extension = __result.kindDef.GetModExtension<FixedPawnTemplateExtension>();
            if (extension == null)
            {
                return;
            }

            if (__result.skills != null && extension.skills != null)
            {
                foreach (FixedSkillSpec spec in extension.skills)
                {
                    if (spec == null || spec.skill == null)
                    {
                        continue;
                    }

                    SkillRecord record = __result.skills.GetSkill(spec.skill);
                    record.levelInt = Math.Max(0, Math.Min(20, spec.level));
                    record.passion = spec.passion;
                    record.xpSinceLastLevel = 0f;
                    record.xpSinceMidnight = 0f;
                }
            }

            NormalizeInitialGear(__result, extension);
        }

        private static void NormalizeInitialGear(Pawn pawn, FixedPawnTemplateExtension extension)
        {
            if (pawn.apparel != null && extension.onlyInitialApparel != null)
            {
                List<Apparel> worn = pawn.apparel.WornApparel.ToList();
                foreach (Apparel apparel in worn)
                {
                    if (apparel.def == extension.onlyInitialApparel)
                    {
                        continue;
                    }

                    pawn.apparel.Remove(apparel);
                    apparel.Destroy(DestroyMode.Vanish);
                }

                if (!pawn.apparel.WornApparel.Any(apparel => apparel.def == extension.onlyInitialApparel))
                {
                    Apparel robe = ThingMaker.MakeThing(extension.onlyInitialApparel) as Apparel;
                    if (robe != null)
                    {
                        CompQuality quality = robe.TryGetComp<CompQuality>();
                        if (quality != null)
                        {
                            quality.SetQuality(QualityCategory.Excellent, ArtGenerationContext.Outsider);
                        }
                        pawn.apparel.Wear(robe, false);
                    }
                }
            }

            if (extension.clearInitialEquipment && pawn.equipment != null)
            {
                foreach (ThingWithComps equipment in pawn.equipment.AllEquipmentListForReading.ToList())
                {
                    pawn.equipment.Remove(equipment);
                    equipment.Destroy(DestroyMode.Vanish);
                }
            }

            if (extension.clearInitialInventory && pawn.inventory != null)
            {
                pawn.inventory.innerContainer.ClearAndDestroyContents();
            }
        }
    }
}
