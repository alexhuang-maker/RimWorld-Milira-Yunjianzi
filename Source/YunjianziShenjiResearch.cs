using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Yunjianzi
{
    internal static class YunjianziShenjiResearch
    {
        internal static bool CanResearch(Pawn pawn) => pawn?.def?.defName == "YJZ_Shenji_Race_Tianjiang";

        // Keep vanilla's reservations, walking, power/project checks, progress
        // bars and sounds. Only its work delegate assumes a human skill tracker.
        // Patching toil creation also repairs already-saved vanilla jobs on load.
        internal static IEnumerable<Toil> ResearchToils(JobDriver_Research driver, IEnumerable<Toil> original)
        {
            foreach (Toil toil in original)
            {
                if (toil.activeSkill?.Invoke() == SkillDefOf.Intellectual)
                {
                    if (toil.tickAction != null)
                        toil.tickAction = () => ResearchTick(driver, toil.actor, 1);
                    if (toil.tickIntervalAction != null)
                        toil.tickIntervalAction = delta => ResearchTick(driver, toil.actor, delta);
                }
                yield return toil;
            }
        }

        private static void ResearchTick(JobDriver driver, Pawn actor, int delta)
        {
            float speed = actor.GetStatValue(StatDefOf.ResearchSpeed) *
                driver.job.targetA.Thing.GetStatValue(StatDefOf.ResearchSpeedFactor);
            Find.ResearchManager.ResearchPerformed(speed * delta, actor);
            actor.skills?.Learn(SkillDefOf.Intellectual, .1f * delta);
            actor.GainComfortFromCellIfPossible(delta, chairsOnly: true);
        }

        internal static IEnumerable<Toil> ScannerToils(JobDriver_OperateScanner driver, IEnumerable<Toil> original)
        {
            foreach (Toil toil in original)
            {
                if (toil.activeSkill?.Invoke() == SkillDefOf.Intellectual && toil.tickAction != null)
                {
                    toil.tickAction = () =>
                    {
                        Pawn actor = toil.actor;
                        driver.job.targetA.Thing.TryGetComp<CompScanner>().Used(actor);
                        actor.skills?.Learn(SkillDefOf.Intellectual, .035f);
                        actor.GainComfortFromCellIfPossible(1, chairsOnly: true);
                    };
                }
                yield return toil;
            }
        }
    }

    public sealed class WorkGiver_YunjianziShenjiResearch : WorkGiver_Researcher
    {
        public override bool ShouldSkip(Pawn pawn, bool forced = false) =>
            !YunjianziShenjiResearch.CanResearch(pawn) || base.ShouldSkip(pawn, forced);

        public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false) =>
            YunjianziShenjiResearch.CanResearch(pawn) && base.HasJobOnThing(pawn, thing, forced);
    }

    public sealed class WorkGiver_YunjianziShenjiScanner : WorkGiver_OperateScanner
    {
        public override bool ShouldSkip(Pawn pawn, bool forced = false) =>
            !YunjianziShenjiResearch.CanResearch(pawn) || base.ShouldSkip(pawn, forced);

        public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false) =>
            YunjianziShenjiResearch.CanResearch(pawn) && base.HasJobOnThing(pawn, thing, forced);
    }

    [HarmonyPatch(typeof(JobDriver_Research), "MakeNewToils")]
    internal static class Patch_ShenjiResearchToils
    {
        private static void Postfix(JobDriver_Research __instance, ref IEnumerable<Toil> __result)
        {
            if (YunjianziShenjiResearch.CanResearch(__instance.pawn))
                __result = YunjianziShenjiResearch.ResearchToils(__instance, __result);
        }
    }

    [HarmonyPatch(typeof(JobDriver_OperateScanner), "MakeNewToils")]
    internal static class Patch_ShenjiScannerToils
    {
        private static void Postfix(JobDriver_OperateScanner __instance, ref IEnumerable<Toil> __result)
        {
            if (YunjianziShenjiResearch.CanResearch(__instance.pawn))
                __result = YunjianziShenjiResearch.ScannerToils(__instance, __result);
        }
    }
}
