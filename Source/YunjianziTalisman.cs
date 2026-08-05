using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Yunjianzi
{
    public sealed class CompProperties_YunjianziTalisman : CompProperties
    {
        public CompProperties_YunjianziTalisman()
        {
            compClass = typeof(CompYunjianziTalisman);
        }
    }

    /// <summary>
    /// Makes the physical talisman itself actionable.  The original slot-only
    /// implementation left crafted talismans behaving like inert resources.
    /// </summary>
    public sealed class CompYunjianziTalisman : ThingComp
    {
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            CompYunjianziCultivation cultivation = selPawn == null ? null : selPawn.TryGetComp<CompYunjianziCultivation>();
            if (cultivation == null || !cultivation.IsYunjianzi)
            {
                yield return new FloatMenuOption("装备阳符（仅云间子可佩用）", null);
                yield break;
            }
            if (cultivation.TalismanSlotCount <= 0)
            {
                yield return new FloatMenuOption("装备阳符（筑基境解锁第一个阳符位）", null);
                yield break;
            }
            if (!cultivation.CanEquipTalismanDef(parent.def))
            {
                yield return new FloatMenuOption("装备「" + parent.LabelCap + "」（尚未参悟此符）", null);
                yield break;
            }

            for (int slot = 0; slot < cultivation.TalismanSlotCount; slot++)
            {
                int capturedSlot = slot;
                Thing current = cultivation.ActiveTalismanThingAt(slot);
                if (current != null && current.def == parent.def)
                {
                    yield return new FloatMenuOption("阳符位·" + (slot + 1) + "已装备「" + parent.LabelCap + "」", null);
                    continue;
                }

                string replacement = current == null ? string.Empty : "（替换「" + current.LabelCap + "」）";
                FloatMenuOption option = new FloatMenuOption(
                    "装备到阳符位·" + (slot + 1) + replacement,
                    delegate { cultivation.OrderEquipTalisman(capturedSlot, parent); });
                if (parent.Spawned && !selPawn.CanReserveAndReach(parent, PathEndMode.Touch, Danger.Deadly))
                {
                    option.Disabled = true;
                    option.Label += "（无法抵达或已被预定）";
                }
                yield return option;
            }
        }

        public override string CompInspectStringExtra()
        {
            return "选中云间子后右键此符，可将它装备到已解锁的阳符位。";
        }
    }

    public sealed class YunjianziTalismanRecipeExtension : DefModExtension
    {
        public string skillId;
    }

    public sealed class RecipeWorker_YunjianziTalisman : RecipeWorker
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (!base.AvailableOnNow(thing, part)) return false;
            YunjianziTalismanRecipeExtension extension = recipe.GetModExtension<YunjianziTalismanRecipeExtension>();
            if (extension == null || extension.skillId.NullOrEmpty() || thing == null || thing.Map == null) return false;

            return thing.Map.mapPawns.AllPawnsSpawned.Any(pawn =>
                pawn.Faction == Faction.OfPlayer &&
                pawn.TryGetComp<CompYunjianziCultivation>() != null &&
                pawn.TryGetComp<CompYunjianziCultivation>().HasSkill(extension.skillId));
        }

        public override AcceptanceReport AvailableReport(Thing thing, BodyPartRecord part = null)
        {
            if (AvailableOnNow(thing, part)) return true;
            return "需要云间子先在修行面板中参悟对应符咒。";
        }
    }

    public sealed partial class CompYunjianziCultivation
    {
        private static readonly Dictionary<string, string> TalismanSkills = new Dictionary<string, string>
        {
            { "YJZ_Talisman_Zhuoyu", YunjianziSkills.TalismanZhuoyu },
            { "YJZ_Talisman_Shenjie", YunjianziSkills.TalismanShenjie },
            { "YJZ_Talisman_Shenxing", YunjianziSkills.TalismanShenxing },
            { "YJZ_Talisman_Jiqing", YunjianziSkills.TalismanJiqing },
            { "YJZ_Talisman_Baicao", YunjianziSkills.TalismanBaicao },
            { "YJZ_Talisman_Xinjian", YunjianziSkills.TalismanXinjian },
            { "YJZ_Talisman_Beidou", YunjianziSkills.TalismanBeidou },
            { "YJZ_Talisman_Guihai", YunjianziSkills.TalismanGuihai },
            { "YJZ_Talisman_Shenlian", YunjianziSkills.TalismanShenlian },
            { "YJZ_Talisman_Huijian", YunjianziSkills.TalismanHuijian },
            { "YJZ_Talisman_Bumei", YunjianziSkills.TalismanBumei },
            { "YJZ_Talisman_Jiexin", YunjianziSkills.TalismanJiexin },
            { "YJZ_Talisman_Liuyang", YunjianziSkills.TalismanLiuyang },
            { "YJZ_Talisman_Tiandao", YunjianziSkills.TalismanTiandao },
            { "YJZ_Talisman_Huanri", YunjianziSkills.TalismanHuanri }
        };

        private ThingOwner<Thing> equippedTalismans;
        private List<string> activeTalismanDefs = new List<string>();

        public int TalismanSlotCount { get { return Mathf.Clamp(Realm - 1, 0, 4); } }

        public ThingOwner GetDirectlyHeldThings()
        {
            EnsureTalismanState();
            return equippedTalismans;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        private void EnsureTalismanState()
        {
            if (equippedTalismans == null) equippedTalismans = new ThingOwner<Thing>(this, false, LookMode.Deep);
            if (activeTalismanDefs == null) activeTalismanDefs = new List<string>();
            while (activeTalismanDefs.Count < 4) activeTalismanDefs.Add(string.Empty);
            while (activeTalismanDefs.Count > 4) activeTalismanDefs.RemoveAt(activeTalismanDefs.Count - 1);

            for (int slot = TalismanSlotCount; slot < activeTalismanDefs.Count; slot++)
                if (!activeTalismanDefs[slot].NullOrEmpty()) UnequipTalisman(slot, false);
        }

        internal void ExposeTalismanData()
        {
            Scribe_Deep.Look(ref equippedTalismans, "yjz_equippedTalismans", this);
            Scribe_Collections.Look(ref activeTalismanDefs, "yjz_activeTalismanDefs", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) EnsureTalismanState();
        }

        public bool HasActiveTalisman(string defName)
        {
            EnsureTalismanState();
            return !defName.NullOrEmpty() && activeTalismanDefs.Take(TalismanSlotCount).Contains(defName);
        }

        internal IEnumerable<Gizmo> GetTalismanGizmos()
        {
            EnsureTalismanState();
            for (int slot = 0; slot < TalismanSlotCount; slot++)
            {
                int capturedSlot = slot;
                Thing active = ActiveTalismanThing(slot);
                Texture2D slotIcon = active == null ? YunjianziAbilityTextures.TalismanSlot :
                    ContentFinder<Texture2D>.Get(active.def.graphicData.texPath, false) ?? YunjianziAbilityTextures.TalismanSlot;
                yield return new CommandYunjianziTalismanSlot
                {
                    comp = this,
                    slot = capturedSlot,
                    defaultLabel = active == null ? "阳符位·" + (slot + 1) : "阳符位·" + (slot + 1) + "「" + active.LabelCap + "」",
                    defaultDesc = active == null
                        ? "空置阳符位。右键选择已经参悟并持有的符咒。"
                        : "当前生效：" + active.LabelCap + "。\n\n右键替换或卸下；被替换的实体符咒会返回云间子的物品栏。",
                    icon = slotIcon,
                    action = delegate { OpenTalismanMenu(capturedSlot); }
                };
            }
        }

        private Thing ActiveTalismanThing(int slot)
        {
            if (slot < 0 || slot >= activeTalismanDefs.Count || activeTalismanDefs[slot].NullOrEmpty()) return null;
            return equippedTalismans.FirstOrDefault(thing => thing.def.defName == activeTalismanDefs[slot]);
        }

        internal void OpenTalismanMenu(int slot)
        {
            EnsureTalismanState();
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            Thing current = ActiveTalismanThing(slot);
            if (current != null)
                options.Add(new FloatMenuOption("卸下「" + current.LabelCap + "」", delegate { UnequipTalisman(slot, true); }));

            HashSet<string> activeDefs = new HashSet<string>(activeTalismanDefs.Where(defName => !defName.NullOrEmpty()));
            foreach (Thing talisman in AvailableTalismans().Where(thing => !activeDefs.Contains(thing.def.defName))
                .GroupBy(thing => thing.def).Select(group => group.First()).OrderBy(thing => thing.LabelCap))
            {
                Thing captured = talisman;
                string location = captured.holdingOwner == (Pawn.inventory == null ? null : Pawn.inventory.innerContainer) ? "随身" : "地图";
                options.Add(new FloatMenuOption("装备「" + captured.LabelCap + "」  ·  " + location,
                    delegate { OrderEquipTalisman(slot, captured); }, captured.def.uiIcon, Color.white));
            }

            if (options.Count == 0)
                options.Add(new FloatMenuOption("没有已参悟且可用的符咒", null));
            Find.WindowStack.Add(new FloatMenu(options, "阳符位·" + (slot + 1), false));
        }

        private IEnumerable<Thing> AvailableTalismans()
        {
            IEnumerable<Thing> carried = Pawn.inventory == null ? Enumerable.Empty<Thing>() : Pawn.inventory.innerContainer;
            IEnumerable<Thing> mapThings = Pawn.Spawned && Pawn.Map != null ? Pawn.Map.listerThings.AllThings : Enumerable.Empty<Thing>();
            return carried.Concat(mapThings).Where(thing => thing != null && IsTalisman(thing.def) && CanEquipTalismanDef(thing.def));
        }

        private static bool IsTalisman(ThingDef def)
        {
            return def != null && TalismanSkills.ContainsKey(def.defName);
        }

        internal bool CanEquipTalismanDef(ThingDef def)
        {
            string skill;
            return def != null && TalismanSkills.TryGetValue(def.defName, out skill) && HasSkill(skill);
        }

        internal Thing ActiveTalismanThingAt(int slot)
        {
            EnsureTalismanState();
            return ActiveTalismanThing(slot);
        }

        internal void OrderEquipTalisman(int slot, Thing source)
        {
            if (slot < 0 || slot >= TalismanSlotCount || source == null || !CanEquipTalismanDef(source.def)) return;
            if (!source.Spawned)
            {
                EquipTalisman(slot, source);
                return;
            }
            if (Pawn == null || Pawn.jobs == null || !Pawn.CanReserveAndReach(source, PathEndMode.Touch, Danger.Deadly))
            {
                Messages.Message("无法抵达或预定这张符咒。", source, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("YJZ_EquipTalisman"), source);
            job.count = slot + 1;
            job.playerForced = true;
            Pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        internal bool EquipTalisman(int slot, Thing source)
        {
            if (slot < 0 || slot >= TalismanSlotCount || source == null || !CanEquipTalismanDef(source.def)) return false;
            UnequipTalisman(slot, false);

            Thing one = source.stackCount > 1 ? source.SplitOff(1) : source;
            if (one.Spawned) one.DeSpawn();
            if (!equippedTalismans.TryAddOrTransfer(one, false))
            {
                if (Pawn.Spawned) GenPlace.TryPlaceThing(one, Pawn.Position, Pawn.Map, ThingPlaceMode.Near);
                Messages.Message("符咒未能收入符位。", Pawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            activeTalismanDefs[slot] = one.def.defName;
            Messages.Message("阳符位·" + (slot + 1) + "已装备「" + one.LabelCap + "」。", Pawn, MessageTypeDefOf.PositiveEvent, false);
            return true;
        }

        private void UnequipTalisman(int slot, bool message)
        {
            if (slot < 0 || slot >= activeTalismanDefs.Count || activeTalismanDefs[slot].NullOrEmpty()) return;
            Thing active = ActiveTalismanThing(slot);
            activeTalismanDefs[slot] = string.Empty;
            if (active != null)
            {
                bool returned = Pawn != null && Pawn.inventory != null && Pawn.inventory.innerContainer.TryAddOrTransfer(active, false);
                if (!returned && Pawn != null && Pawn.Spawned)
                {
                    equippedTalismans.Remove(active);
                    GenPlace.TryPlaceThing(active, Pawn.Position, Pawn.Map, ThingPlaceMode.Near);
                }
            }
            if (message) Messages.Message("阳符位·" + (slot + 1) + "已空置。", Pawn, MessageTypeDefOf.NeutralEvent, false);
        }
    }

    public sealed class CommandYunjianziTalismanSlot : Command_Action
    {
        public CompYunjianziCultivation comp;
        public int slot;

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                if (comp == null) yield break;
                yield return new FloatMenuOption("管理阳符位·" + (slot + 1), delegate { comp.OpenTalismanMenu(slot); });
            }
        }
    }

    public sealed class JobDriver_YunjianziEquipTalisman : JobDriver
    {
        private Thing Talisman { get { return job.targetA.Thing; } }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, 1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil equip = new Toil();
            equip.defaultCompleteMode = ToilCompleteMode.Instant;
            equip.initAction = delegate
            {
                Thing talisman = Talisman;
                CompYunjianziCultivation cultivation = pawn.TryGetComp<CompYunjianziCultivation>();
                int slot = Mathf.Max(0, pawn.CurJob.count - 1);
                if (talisman == null || cultivation == null || !cultivation.EquipTalisman(slot, talisman))
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
            };
            yield return equip;
        }
    }
}
