using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace Yunjianzi
{
    /// <summary>
    /// A persisted scenario marker. Because it lives in Current.Game.Scenario,
    /// the restriction follows the save and never affects other starting scenarios.
    /// </summary>
    public sealed class ScenPart_YunjianziSoleColonist : ScenPart
    {
        public override bool AllowPlayerStartingPawn(Pawn pawn, bool tryingToRedress, PawnGenerationRequest req)
        {
            return YunjianziSoleColonistUtility.IsYunjianzi(pawn);
        }

        public override string Summary(Scenario scen)
        {
            return "仅云间子可以成为永久殖民者；神机、动物、符兵与任务临时来客不受影响。";
        }

        public override void Tick()
        {
            if (Find.TickManager == null || Find.TickManager.TicksGame % 250 != 0)
            {
                return;
            }

            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned.ToList())
                {
                    if (!YunjianziSoleColonistUtility.IsForbiddenPermanentColonist(pawn))
                    {
                        continue;
                    }

                    pawn.SetFaction(null);
                    YunjianziSoleColonistUtility.NotifyRejected(pawn);
                }
            }
        }
    }

    /// <summary>
    /// Completes every research project defined by the base Milira Race mod.
    /// Filtering by the owning ModContentPack keeps projects from Milira add-ons
    /// (Imperium, Wings of Democracy, etc.) out of this scenario automatically.
    /// </summary>
    public sealed class ScenPart_UnlockBaseMiliraResearch : ScenPart
    {
        private const string MiliraPackageId = "Ancot.MiliraRace";

        public override string Summary(Scenario scen)
        {
            return "初始解锁所有米莉拉本体科技（不包括拓展 Mod）";
        }

        public override void PostGameStart()
        {
            foreach (ResearchProjectDef project in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
            {
                ModContentPack owner = project.modContentPack;
                if (owner != null && string.Equals(owner.PackageId, MiliraPackageId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    Find.ResearchManager.FinishProject(project, doCompletionDialog: false, null,
                        doCompletionLetter: false);
                }
            }
        }
    }

    internal static class YunjianziSoleColonistUtility
    {
        internal static bool RuleActive
        {
            get
            {
                if (Current.Game == null)
                {
                    return false;
                }

                Scenario scenario = Find.Scenario;
                return scenario != null && scenario.AllParts.OfType<ScenPart_YunjianziSoleColonist>().Any();
            }
        }

        internal static bool IsYunjianzi(Pawn pawn)
        {
            return pawn != null && pawn.kindDef != null && pawn.kindDef.defName == "YJZ_Yunjianzi";
        }

        internal static bool IsForbiddenPermanentColonist(Pawn pawn)
        {
            return RuleActive && pawn != null && pawn.RaceProps != null && pawn.RaceProps.Humanlike &&
                   !pawn.IsQuestLodger() && !IsYunjianzi(pawn);
        }

        internal static void NotifyRejected(Pawn pawn)
        {
            if (Current.ProgramState == ProgramState.Playing && pawn != null && pawn.Spawned)
            {
                Messages.Message("孤云命数拒绝了新的永久殖民者加入。", pawn,
                    MessageTypeDefOf.RejectInput, historical: false);
            }
        }
    }

    /// <summary>
    /// Covers recruitment, wanderer joins and other code paths that call SetFaction.
    /// Quest lodgers are deliberately allowed because they never become permanent colonists.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction), new Type[] { typeof(Faction), typeof(Pawn) })]
    internal static class Patch_Pawn_SetFaction_YunjianziSoleColonist
    {
        private static bool Prefix(Pawn __instance, Faction newFaction)
        {
            // Faction.OfPlayer logs an error while the player faction has not yet
            // been created (notably while world factions generate).  The faction
            // passed to SetFaction already carries the same information.
            if (newFaction != null && newFaction.IsPlayer &&
                YunjianziSoleColonistUtility.IsForbiddenPermanentColonist(__instance))
            {
                YunjianziSoleColonistUtility.NotifyRejected(__instance);
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Some incidents generate a pawn with OfPlayer already assigned and therefore
    /// never call Pawn.SetFaction. Neutralize that direct-generation path as a safety net.
    /// </summary>
    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn),
        new Type[] { typeof(PawnGenerationRequest) })]
    [HarmonyPriority(Priority.Last)]
    internal static class Patch_PawnGenerator_YunjianziSoleColonist
    {
        private static void Postfix(Pawn __result)
        {
            // GeneratePawn also runs during world generation, before OfPlayer is
            // guaranteed to exist.  Inspect the generated pawn's faction directly
            // so this safety net stays silent in that phase.
            if (__result == null || __result.Faction == null || !__result.Faction.IsPlayer ||
                !YunjianziSoleColonistUtility.IsForbiddenPermanentColonist(__result))
            {
                return;
            }

            __result.SetFaction(null);
            YunjianziSoleColonistUtility.NotifyRejected(__result);
        }
    }

    /// <summary>
    /// Vanilla marks every quest whose intended result increases colony
    /// population with QuestScriptDef.rootIncreasesPopulation. Rejecting it in
    /// CanRun keeps it out of storyteller and natural-random quest selection,
    /// so no unavailable recruitment quest or quest letter is generated.
    /// </summary>
    [HarmonyPatch(typeof(QuestScriptDef), nameof(QuestScriptDef.CanRun),
        new Type[] { typeof(Slate), typeof(IIncidentTarget) })]
    internal static class Patch_QuestScriptDef_CanRun_YunjianziSoleColonist
    {
        private static void Postfix(QuestScriptDef __instance, ref bool __result)
        {
            if (__result && YunjianziSoleColonistUtility.RuleActive &&
                __instance != null && __instance.rootIncreasesPopulation)
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// Kiiro Story: Events Expanded does not mark its orphan-entrustment quest
    /// as rootIncreasesPopulation, so the generic quest guard above cannot see
    /// it. Block the incident at the common IncidentWorker entry point without
    /// taking a compile-time dependency on the optional Kiiro assembly.
    /// </summary>
    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.CanFireNow))]
    internal static class Patch_IncidentWorker_CanFireNow_YunjianziSoleColonist
    {
        private const string KiiroEntrustOrphanIncident = "Kiiro_Refugee_Baby";

        private static bool Prefix(IncidentDef ___def, ref bool __result)
        {
            if (YunjianziSoleColonistUtility.RuleActive && ___def != null &&
                ___def.defName == KiiroEntrustOrphanIncident)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// The Ideology random-recruit ritual creates WandererJoins directly and
    /// does not ask QuestScriptDef.CanRun first. Suppress that one direct path
    /// as well, instead of creating a quest whose reward is forbidden.
    /// </summary>
    [HarmonyPatch(typeof(RitualAttachableOutcomeEffectWorker_RandomRecruit),
        nameof(RitualAttachableOutcomeEffectWorker_RandomRecruit.Apply))]
    internal static class Patch_RandomRecruitRitual_YunjianziSoleColonist
    {
        private static bool Prefix(ref string extraOutcomeDesc)
        {
            if (!YunjianziSoleColonistUtility.RuleActive)
            {
                return true;
            }

            extraOutcomeDesc = null;
            return false;
        }
    }
}
