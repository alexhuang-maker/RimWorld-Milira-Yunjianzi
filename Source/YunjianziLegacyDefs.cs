using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Yunjianzi
{
    // This hook must run during Def generation, before StaticConstructorOnStartup.
    public sealed class YunjianziLegacyDefLoader : Mod
    {
        public YunjianziLegacyDefLoader(ModContentPack content) : base(content)
        {
            new Harmony("alex.miliraprivateexpansion.yunjianzi.legacydefs").Patch(
                AccessTools.Method(typeof(ThingDefGenerator_Corpses), "ImpliedCorpseDefs"),
                postfix: new HarmonyMethod(typeof(YunjianziLegacyDefLoader), nameof(AddLegacyCorpses)));
        }

        private static void AddLegacyCorpses(ref IEnumerable<ThingDef> __result, bool hotReload)
        {
            __result = WithLegacyCorpses(__result, hotReload);
        }

        private static IEnumerable<ThingDef> WithLegacyCorpses(IEnumerable<ThingDef> original, bool hotReload)
        {
            var emitted = new HashSet<string>();
            foreach (var def in original) { emitted.Add(def.defName); yield return def; }
            var factory = AccessTools.MethodDelegate<Func<ThingDef, bool, string, ThingDef>>(
                AccessTools.Method(typeof(ThingDefGenerator_Corpses), "GenerateCorpseDef",
                    new[] { typeof(ThingDef), typeof(bool), typeof(string) }));
            foreach (string suffix in new[] { "Purple", "Golden", "Spirit" })
            {
                string raceName = "YJZ_SkyWyvern_Race_" + suffix;
                string corpseName = "Corpse_" + raceName;
                var race = DefDatabase<ThingDef>.GetNamedSilentFail(raceName);
                if (race == null || emitted.Contains(corpseName)
                    || DefDatabase<ThingDef>.GetNamedSilentFail(corpseName) != null) continue;
                // Restore the Def for old food/storage filters and any legacy corpse.
                // Never set hasCorpse or corpseDef on the race: new qilin still dissipate.
                yield return factory(race, hotReload, corpseName);
            }
        }
    }

    internal static class YunjianziLegacyNodeIds
    {
        internal static string RenameDef(string oldName)
        {
            for (int i = 0; i < GameComponent_YunjianziPursuit.NodeDefNames.Length; i++)
                if (oldName == "YJZ_PursuitNode" + i) return GameComponent_YunjianziPursuit.NodeDefNames[i];
            return oldName;
        }

        internal static string RenameThingId(string oldId)
        {
            if (oldId == null) return null;
            for (int i = 0; i < GameComponent_YunjianziPursuit.NodeDefNames.Length; i++)
            {
                string prefix = "YJZ_PursuitNode" + i;
                if (!oldId.StartsWith(prefix, StringComparison.Ordinal) || oldId.Length == prefix.Length) continue;
                string number = oldId.Substring(prefix.Length);
                if (int.TryParse(number, out int id) && id >= 0)
                    return GameComponent_YunjianziPursuit.NodeDefNames[i] + number;
            }
            return oldId;
        }
    }

    [HarmonyPatch(typeof(BackCompatibility), nameof(BackCompatibility.BackCompatibleDefName))]
    internal static class Patch_YunjianziLegacyNodeDef
    {
        private static void Prefix(Type defType, ref string defName)
        {
            if (defType == typeof(ThingDef)) defName = YunjianziLegacyNodeIds.RenameDef(defName);
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.IDNumberFromThingID))]
    internal static class Patch_YunjianziLegacyNodeNumber
    {
        private static void Prefix(ref string thingID)
        {
            thingID = YunjianziLegacyNodeIds.RenameThingId(thingID);
        }
    }

    [HarmonyPatch(typeof(LoadedObjectDirectory), nameof(LoadedObjectDirectory.RegisterLoaded))]
    internal static class Patch_YunjianziLegacyNodeReferences
    {
        private static void Postfix(ILoadReferenceable reffable,
            Dictionary<string, ILoadReferenceable> ___allObjectsByLoadID)
        {
            if (!(reffable is Building_YunjianziNode node)) return;
            int index = Array.IndexOf(GameComponent_YunjianziPursuit.NodeDefNames, node.def.defName);
            if (index < 0) return;
            string oldId = "Thing_YJZ_PursuitNode" + index + node.thingIDNumber;
            if (!___allObjectsByLoadID.ContainsKey(oldId)) ___allObjectsByLoadID.Add(oldId, node);
        }
    }
}
