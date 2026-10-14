using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Yunjianzi
{
    public sealed class CompProperties_QilinSaliva : CompProperties
    {
        public int intervalDays = 10;
        public int amount = 1;
        public ThingDef resourceDef;

        public CompProperties_QilinSaliva() => compClass = typeof(CompQilinSaliva);
    }

    // Use the same growth, save and handler-yield logic as milk, with its own
    // resource/job names so this companion is never presented as a dairy animal.
    public sealed class CompQilinSaliva : CompHasGatherableBodyResource
    {
        private CompProperties_QilinSaliva Props => (CompProperties_QilinSaliva)props;
        protected override int GatherResourcesIntervalDays => Props.intervalDays;
        protected override int ResourceAmount => Props.amount;
        protected override ThingDef ResourceDef => Props.resourceDef;
        protected override string SaveKey => "qilinSalivaFullness";
        protected override bool Active => base.Active && parent is Pawn pawn && !pawn.Dead;

        public void RestoreFullness(float value) => fullness = Mathf.Clamp01(value);

        public override string CompInspectStringExtra() => Active
            ? "麟涎凝聚：" + Fullness.ToStringPercent() + "（每" + Props.intervalDays + "天" + Props.amount + "份）"
            : null;
    }

    public sealed class WorkGiver_GatherQilinSaliva : WorkGiver_GatherAnimalBodyResources
    {
        protected override JobDef JobDef => DefDatabase<JobDef>.GetNamed("YJZ_GatherQilinSaliva");
        protected override CompHasGatherableBodyResource GetComp(Pawn animal) => animal.TryGetComp<CompQilinSaliva>();
    }

    public sealed class JobDriver_GatherQilinSaliva : JobDriver_GatherAnimalBodyResources
    {
        protected override float WorkTotal => 400f;
        protected override CompHasGatherableBodyResource GetComp(Pawn animal) => animal.TryGetComp<CompQilinSaliva>();
    }

    internal static class CloudQilinUtility
    {
        internal static bool IsQilin(ThingDef def) => def?.GetCompProperties<CompProperties_YunjianziSkyWyvern>() != null;
        internal static bool IsQilin(Thing thing) => IsQilin(thing?.def);
        internal static bool IsLivestockDesignation(DesignationDef def) =>
            def == DesignationDefOf.Slaughter || def == DesignationDefOf.ReleaseAnimalToWild;

        internal static void ClearLivestockDesignations(Pawn pawn)
        {
            if (pawn?.Map == null) return;
            foreach (DesignationDef def in new[] { DesignationDefOf.Slaughter, DesignationDefOf.ReleaseAnimalToWild })
            {
                Designation designation = pawn.Map.designationManager.DesignationOn(pawn, def);
                if (designation != null) pawn.Map.designationManager.RemoveDesignation(designation);
            }
        }
    }

    [HarmonyPatch(typeof(Designator_Slaughter), nameof(Designator_Slaughter.CanDesignateThing))]
    internal static class Patch_Qilin_NoSlaughterDesignator
    {
        private static bool Prefix(Thing t, ref AcceptanceReport __result)
        {
            if (!CloudQilinUtility.IsQilin(t)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class Patch_Qilin_NoLivestockCheckboxes
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(PawnColumnWorker_Slaughter), "HasCheckbox");
            yield return AccessTools.DeclaredMethod(typeof(PawnColumnWorker_ReleaseAnimalToWild), "HasCheckbox");
        }

        private static void Postfix(Pawn pawn, ref bool __result)
        {
            if (CloudQilinUtility.IsQilin(pawn)) __result = false;
        }
    }

    [HarmonyPatch(typeof(DesignationManager), nameof(DesignationManager.AddDesignation))]
    internal static class Patch_Qilin_NoLivestockDesignation
    {
        private static bool Prefix(Designation newDes) =>
            !CloudQilinUtility.IsLivestockDesignation(newDes.def) || !CloudQilinUtility.IsQilin(newDes.target.Thing);
    }

    [HarmonyPatch(typeof(AutoSlaughterManager), nameof(AutoSlaughterManager.CanEverAutoSlaughter))]
    internal static class Patch_Qilin_NoAutoSlaughter
    {
        private static void Postfix(Pawn animal, ref bool __result)
        {
            if (CloudQilinUtility.IsQilin(animal)) __result = false;
        }
    }

    [HarmonyPatch(typeof(AutoSlaughterManager), "TryPopulateMissingAnimals")]
    internal static class Patch_Qilin_NoAutoSlaughterConfig
    {
        private static void Postfix(AutoSlaughterManager __instance) =>
            __instance.configs.RemoveAll(c => CloudQilinUtility.IsQilin(c.animal));
    }

    [HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.ShouldBeSlaughtered))]
    internal static class Patch_Qilin_NotSlaughterable
    {
        private static bool Prefix(Pawn pawn, ref bool __result)
        {
            if (!CloudQilinUtility.IsQilin(pawn)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(StatWorker_MeatAmount), nameof(StatWorker_MeatAmount.ShouldShowFor))]
    internal static class Patch_Qilin_HideMeatAmount
    {
        private static void Postfix(StatRequest req, ref bool __result)
        {
            if (CloudQilinUtility.IsQilin(req.Def as ThingDef)) __result = false;
        }
    }

    // Vanilla always adds an adult-meat entry, even for races with hasMeat=false.
    [HarmonyPatch(typeof(AnimalProductionUtility), nameof(AnimalProductionUtility.AnimalProductionStats))]
    internal static class Patch_Qilin_ResourceStats
    {
        private static bool Prefix(ThingDef d, ref IEnumerable<StatDrawEntry> __result)
        {
            if (!CloudQilinUtility.IsQilin(d)) return true;
            __result = ResourceStats(d.GetCompProperties<CompProperties_QilinSaliva>());
            return false;
        }

        private static IEnumerable<StatDrawEntry> ResourceStats(CompProperties_QilinSaliva props)
        {
            if (props == null) yield break;
            yield return new StatDrawEntry(StatCategoryDefOf.AnimalProductivity, "凝聚产物", props.resourceDef.LabelCap,
                "云麒麟自愿赠予同行者的灵药，服用后治愈一项最严重的健康问题，效果等同于治疗机械液。", 9880,
                null, new[] { new Dialog_InfoCard.Hyperlink(props.resourceDef) });
            yield return new StatDrawEntry(StatCategoryDefOf.AnimalProductivity, "每次采集数量", props.amount + "份",
                "凝聚完成后由驯兽员采集，采集成功率与原版挤奶相同。", 9870);
            yield return new StatDrawEntry(StatCategoryDefOf.AnimalProductivity, "麟涎凝聚周期", props.intervalDays + "天",
                "正常状态下凝聚一份麟涎所需的时间。营养不良会与原版挤奶一样减慢凝聚；完成后等待采集，不继续积存。", 9860);
        }
    }
}
