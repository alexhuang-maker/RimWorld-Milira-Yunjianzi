using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using HarmonyLib;
using UnityEngine;

namespace Yunjianzi
{
    public sealed partial class CompYunjianziCultivation
    {
        // Opted in only at pawn generation. Missing in legacy saves means the
        // existing cultivator keeps her realm, gear, skills and unlocked story.
        private bool inheritanceJourney;
        private bool inheritanceBenefitsApplied;
        public bool AwaitingInheritance => inheritanceJourney && !boundScrollConsumed;

        internal void BeginInheritanceJourney()
        {
            if (!IsYunjianzi || inheritanceJourney || boundScrollConsumed) return;
            inheritanceJourney = true;
            Pawn.Name = new NameTriple("西娅", null, "阿纳斯塔西亚");
            Pawn.story.Adulthood = DefDatabase<BackstoryDef>.GetNamed("YJZ_Backstory_Explorer");
            var trait = Pawn.story.traits.GetTrait(YJZDefOf.YJZ_Trait_CloudseaSwordHeart);
            if (trait != null) Pawn.story.traits.RemoveTrait(trait);
            if (RealmHediff != null) Pawn.health.RemoveHediff(RealmHediff);
            // The template's former final levels included the inherited story
            // and trait. Hold back exactly those gains, retaining childhood.
            foreach (var gain in InheritanceSkillGains())
                Pawn.skills.GetSkill(gain.Key).levelInt = System.Math.Max(0,
                    Pawn.skills.GetSkill(gain.Key).levelInt - gain.Value);
            Pawn.story.HairColor = UnityEngine.Color.white;
            foreach (var apparel in Pawn.apparel.WornApparel.ToList())
            {
                Pawn.apparel.Remove(apparel);
                apparel.Destroy();
            }
            var cloak = (Apparel)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Milira_Cloak"),
                DefDatabase<ThingDef>.GetNamed("Milira_FeatherThread"));
            Pawn.apparel.Wear(cloak, false);
        }

        private IEnumerable<KeyValuePair<SkillDef,int>> InheritanceSkillGains()
        {
            // skillGains are applied by vanilla pawn generation, not by
            // TraitSet.GainTrait or changing the adulthood at runtime.
            var total = new Dictionary<SkillDef,int>();
            foreach (var pair in YJZDefOf.YJZ_Backstory_Adulthood.skillGains)
                total[pair.skill] = pair.amount;
            foreach (var pair in YJZDefOf.YJZ_Trait_CloudseaSwordHeart.DataAtDegree(0).skillGains)
                total[pair.skill] = (total.TryGetValue(pair.skill, out int n) ? n : 0) + pair.amount;
            return total;
        }

        private void AcceptInheritance()
        {
            if (!inheritanceJourney || inheritanceBenefitsApplied) return;
            inheritanceBenefitsApplied = true;
            Pawn.Name = new NameTriple("西娅", "云间子", "阿纳斯塔西亚");
            EnsureRealmAndAbilities();
            foreach (var gain in InheritanceSkillGains())
            {
                var skill = Pawn.skills.GetSkill(gain.Key);
                skill.levelInt = System.Math.Min(20, skill.levelInt + gain.Value);
            }
            Pawn.skills.Notify_SkillDisablesChanged();
            Pawn.skills.DirtyAptitudes();
            Pawn.Drawer.renderer.SetAllGraphicsDirty();
            PortraitsCache.SetDirty(Pawn);
            EnsureMechanitorTracker();
            Shield = ShieldMax;
        }
    }

    // Starting supplies are real saved objects while sealed. Inheritance
    // rewards still materialize only when legitimately opened.
    public sealed class Building_YunjianziInheritanceChest : Building_Storage, IOpenable, IThingHolder
    {
        public const int StackCapacity = 30;
        internal const string LockedHint = "这个精美的匣子似乎被某种古老而玄妙的力量封印着，仍在静静等待那个有缘人的出现。";
        internal const string LockedMenuLabel = "匣上的封印尚未散去";
        private Pawn inheritor;
        private bool rewardsCreated;
        private ThingOwner<Thing> sealedSupplies;
        public Building_YunjianziInheritanceChest() { sealedSupplies = new ThingOwner<Thing>(this, false); }
        public ThingOwner GetDirectlyHeldThings() => sealedSupplies;
        public void GetChildHolders(List<IThingHolder> outChildren) =>
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, sealedSupplies);
        public int StoredStackCount => sealedSupplies.Count + (Spawned ? slotGroup.HeldThingsCount : 0);
        public float StoredMass => sealedSupplies.Sum(t => t.GetStatValue(StatDefOf.Mass) * t.stackCount) +
            (Spawned ? slotGroup.HeldThings.Sum(t => t.GetStatValue(StatDefOf.Mass) * t.stackCount) : 0f);

        public void BindTo(Pawn pawn) { inheritor = pawn; }
        public bool Unlocked => inheritor?.TryGetComp<CompYunjianziCultivation>()?.HasOpenedBoundScroll == true;
        public bool RewardsCreated => rewardsCreated;
        public override void PostMake()
        {
            base.PostMake();
            settings.filter.SetDisallowAll();
        }
        public bool CanOpen => Unlocked && (!rewardsCreated || sealedSupplies.Count > 0);
        public int OpenTicks => 120;
        public void Open()
        {
            if (!CanOpen) return;
            if (!rewardsCreated)
            {
                sealedSupplies.TryAdd(ThingMaker.MakeThing(YJZDefOf.YJZ_Apparel_YunjianRobe));
                Thing jade = ThingMaker.MakeThing(ThingDefOf.Jade);
                jade.stackCount = 75;
                sealedSupplies.TryAdd(jade);
                rewardsCreated = true;
                settings.filter.SetAllowAll(GetParentStoreSettings().filter);
            }
            Notify_SettingsChanged();
            sealedSupplies.TryDropAll(Position, Map, ThingPlaceMode.Near, (t, count) => t.SetForbidden(false, false));
        }
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn) => GetMultiSelectFloatMenuOptions(new[]{selPawn});
        public override IEnumerable<FloatMenuOption> GetMultiSelectFloatMenuOptions(IEnumerable<Pawn> selPawns)
        {
            foreach (var option in base.GetMultiSelectFloatMenuOptions(selPawns)) yield return option;
            if (rewardsCreated && sealedSupplies.Count == 0) yield break;
            if (!Unlocked) { yield return new FloatMenuOption(LockedMenuLabel, () =>
                Messages.Message(LockedHint,this,MessageTypeDefOf.RejectInput,false)); yield break; }
            Pawn opener = selPawns.FirstOrDefault(p => p.Faction==Faction.OfPlayer && p.RaceProps.Humanlike &&
                p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && p.CanReserveAndReach(this, PathEndMode.Touch, Danger.Deadly));
            yield return new FloatMenuOption(opener == null ? "无法打开：无法抵达" : "打开传承宝箱",
                opener == null ? null : () => {
                    var job=JobMaker.MakeJob(JobDefOf.Open,this);
                    job.ignoreDesignations=true;
                    opener.jobs.TryTakeOrderedJob(job,JobTag.Misc);
                });
        }
        public override void Notify_ThingSelected()
        {
            base.Notify_ThingSelected();
            if(Spawned && !rewardsCreated && !Unlocked)
                Messages.Message(LockedHint,this,MessageTypeDefOf.RejectInput,false);
        }
        public override string GetInspectString()
        {
            string state = !Unlocked ? LockedHint :
                !rewardsCreated ? "封印已解：可取用云间道袍一件及翡翠75枚。" : base.GetInspectString();
            return state + "\n储物容量：" + StoredStackCount + " / " + StackCapacity + " 组" +
                "\n物品重量：" + StoredMass.ToString("0.##") + " kg（储量不设重量上限）";
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = Map;
            IntVec3 position = Position;
            base.Destroy(mode);
            if (map != null && (mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize))
                sealedSupplies.TryDropAll(position, map, ThingPlaceMode.Near);
            sealedSupplies.ClearAndDestroyContents();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref inheritor, "yjz_inheritor");
            Scribe_Values.Look(ref rewardsCreated, "yjz_rewardsCreated");
            Scribe_Deep.Look(ref sealedSupplies, "yjz_sealedSupplies", this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && sealedSupplies == null)
                sealedSupplies = new ThingOwner<Thing>(this, false);
        }
    }

    [HarmonyPatch(typeof(Building_Storage), nameof(Building_Storage.Accepts))]
    internal static class Patch_InheritanceChest_StorageLock
    {
        static bool Prefix(Building_Storage __instance, ref bool __result)
        {
            if (__instance is Building_YunjianziInheritanceChest chest && !chest.RewardsCreated)
            { __result = false; return false; }
            return true;
        }
    }

    [HarmonyPatch(typeof(StoreUtility), nameof(StoreUtility.IsGoodStoreCell))]
    internal static class Patch_InheritanceChest_HaulLock
    {
        static void Postfix(IntVec3 c, Map map, ref bool __result)
        {
            if (__result && c.GetSlotGroup(map)?.parent is Building_YunjianziInheritanceChest chest && !chest.RewardsCreated)
                __result = false;
        }
    }

    [HarmonyPatch(typeof(PawnRenderNode), nameof(PawnRenderNode.GraphicFor))]
    internal static class Patch_Inheritance_WhiteWings
    {
        [HarmonyPriority(Priority.Last)]
        static void Postfix(PawnRenderNode __instance, Pawn pawn, ref Graphic __result)
        {
            if (pawn?.TryGetComp<CompYunjianziCultivation>()?.AwaitingInheritance != true) return;
            string label = __instance.Props?.debugLabel;
            if (label != "left wing" && label != "left wing behind" && label != "right wing" && label != "right wing behind") return;
            __result = GraphicDatabase.Get<Graphic_Multi>(__instance.Props.texPath, ShaderDatabase.Cutout, Vector2.one, Color.white);
        }
    }

    // Let Milira's original white-wing flight animation run until inheritance.
    [HarmonyPatch]
    internal static class Patch_Inheritance_WhiteFlight
    {
        static System.Reflection.MethodBase TargetMethod() => AccessTools.Method("AriandelLibrary.Milira.AL_FlyAnimationUtility:TryGetCustomFlyAnimation");
        static bool Prepare() => TargetMethod() != null;
        static bool Prefix(Pawn pawn, ref AnimationDef result, ref bool __result)
        {
            if (pawn?.TryGetComp<CompYunjianziCultivation>()?.AwaitingInheritance != true) return true;
            result = null; __result = false; return false;
        }
    }
}
