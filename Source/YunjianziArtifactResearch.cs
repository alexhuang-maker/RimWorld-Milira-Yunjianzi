using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace Yunjianzi
{
    // Knowledge is colony-wide and persists after the cultivator leaves the map.
    public sealed class YunjianziArtifactKnowledge : GameComponent
    {
        private List<string> unlockedProjects = new List<string>();

        public YunjianziArtifactKnowledge(Game game) { }

        internal bool Contains(string project) => unlockedProjects?.Contains(project) == true;

        internal void Unlock(string project)
        {
            if (unlockedProjects == null) unlockedProjects = new List<string>();
            if (!unlockedProjects.Contains(project)) unlockedProjects.Add(project);
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref unlockedProjects, "yjz_artifactResearch", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && unlockedProjects == null)
                unlockedProjects = new List<string>();
        }
    }

    internal static class YunjianziArtifactResearch
    {
        internal static string ProjectFor(string skill)
        {
            if (skill == YunjianziSkills.SanyuanMiyi) return "YJZ_Research_SanyuanMiyi";
            if (skill == YunjianziSkills.CloudSwordArray) return "YJZ_Research_CloudSwordArray";
            if (skill == YunjianziSkills.JiuxiaoXianxu) return "YJZ_Research_JiuxiaoXianxu";
            return null;
        }

        internal static void UnlockLearned(IEnumerable<string> skills)
        {
            var knowledge = Current.Game?.GetComponent<YunjianziArtifactKnowledge>();
            if (knowledge == null || skills == null) return;
            foreach (string skill in skills)
            {
                string project = ProjectFor(skill);
                if (project != null) knowledge.Unlock(project);
            }
        }

        internal static bool IsUnlocked(ResearchProjectDef project)
        {
            if (project.defName != "YJZ_Research_CloudSwordArray" &&
                project.defName != "YJZ_Research_JiuxiaoXianxu" && project.defName != "YJZ_Research_SanyuanMiyi") return true;
            return Current.Game?.GetComponent<YunjianziArtifactKnowledge>()?.Contains(project.defName) == true;
        }
    }

    [HarmonyPatch(typeof(ResearchProjectDef), "CanStartNow", MethodType.Getter)]
    internal static class Patch_YunjianziArtifactResearchGate
    {
        private static void Postfix(ResearchProjectDef __instance, ref bool __result)
        {
            __result = __result && YunjianziArtifactResearch.IsUnlocked(__instance);
        }
    }
}
