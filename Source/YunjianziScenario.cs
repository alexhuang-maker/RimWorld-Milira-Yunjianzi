using System;
using RimWorld;
using Verse;
namespace Yunjianzi
{
    // Kept only so existing saves can deserialize their scenario.
    public sealed class ScenPart_YunjianziSoleColonist : ScenPart
    {
        public override string Summary(Scenario scen) { return "可接纳新的同伴。"; }
    }
    internal static class YunjianziSoleColonistUtility
    {
        internal static bool RuleActive => false;
        internal static bool IsYunjianzi(Pawn p) => p?.kindDef?.defName == "YJZ_Yunjianzi";
    }
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

}
