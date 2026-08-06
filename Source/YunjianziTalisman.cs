using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
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

        private static readonly Dictionary<string, string> TalismanEffectHediffs = new Dictionary<string, string>
        {
            { "YJZ_Talisman_Shenjie", "YJZ_TalismanEffect_Shenjie" },
            { "YJZ_Talisman_Shenxing", "YJZ_TalismanEffect_Shenxing" },
            { "YJZ_Talisman_Jiqing", "YJZ_TalismanEffect_Jiqing" },
            { "YJZ_Talisman_Baicao", "YJZ_TalismanEffect_Baicao" },
            { "YJZ_Talisman_Xinjian", "YJZ_TalismanEffect_Xinjian" },
            { "YJZ_Talisman_Beidou", "YJZ_TalismanEffect_Beidou" },
            { "YJZ_Talisman_Shenlian", "YJZ_TalismanEffect_Shenlian" },
            { "YJZ_Talisman_Huijian", "YJZ_TalismanEffect_Huijian" },
            { "YJZ_Talisman_Bumei", "YJZ_TalismanEffect_Bumei" },
            { "YJZ_Talisman_Jiexin", "YJZ_TalismanEffect_Jiexin" },
            { "YJZ_Talisman_Liuyang", "YJZ_TalismanEffect_Liuyang" }
        };

        private static readonly Dictionary<string, string> TalismanEffectDescriptions = new Dictionary<string, string>
        {
            { "YJZ_Talisman_Zhuoyu", "制作成品品质提高两个等级（最高传奇）。" },
            { "YJZ_Talisman_Shenjie", "研究速度 +100%。" },
            { "YJZ_Talisman_Shenxing", "移动速度 +2 格/秒。" },
            { "YJZ_Talisman_Jiqing", "社交影响 +25%，精神崩溃阈值 -10%；所在环境越华丽，额外心情越高。" },
            { "YJZ_Talisman_Baicao", "医疗质量与手术成功率 +50%，由云间子主刀时不会发生手术失败。" },
            { "YJZ_Talisman_Xinjian", "贸易价格改善 +20%，社交影响 +10%。" },
            { "YJZ_Talisman_Beidou", "远近攻击与瞄准冷却缩短25%，御剑特效冷却同步缩短。" },
            { "YJZ_Talisman_Guihai", "灵力自然恢复速度提高至三倍。" },
            { "YJZ_Talisman_Shenlian", "全局工作速度 +75%。" },
            { "YJZ_Talisman_Huijian", "心灵敏感度降至零，精神崩溃阈值 -25%。" },
            { "YJZ_Talisman_Bumei", "学习速度 +100%，免疫由心情引发的精神崩溃。" },
            { "YJZ_Talisman_Jiexin", "移动、工作、命中、医疗与承伤能力全面强化。" },
            { "YJZ_Talisman_Liuyang", "射击与格斗固定20级、艺术固定0级；精神崩溃阈值 +25%。" },
            { "YJZ_Talisman_Tiandao", "所有未被背景禁用的技能固定为20级。" },
            { "YJZ_Talisman_Huanri", "生理年龄停止增长。" }
        };

        private ThingOwner<Thing> equippedTalismans;
        private List<string> activeTalismanDefs = new List<string>();
        private List<string> tiandaoOriginalSkillDefs = new List<string>();
        private List<int> tiandaoOriginalSkillLevels = new List<int>();
        private List<float> tiandaoOriginalSkillXp = new List<float>();
        private List<float> tiandaoOriginalSkillXpToday = new List<float>();
        private bool tiandaoApplied;

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
            Scribe_Collections.Look(ref tiandaoOriginalSkillDefs, "yjz_tiandaoSkillDefs", LookMode.Value);
            Scribe_Collections.Look(ref tiandaoOriginalSkillLevels, "yjz_tiandaoSkillLevels", LookMode.Value);
            Scribe_Collections.Look(ref tiandaoOriginalSkillXp, "yjz_tiandaoSkillXp", LookMode.Value);
            Scribe_Collections.Look(ref tiandaoOriginalSkillXpToday, "yjz_tiandaoSkillXpToday", LookMode.Value);
            Scribe_Values.Look(ref tiandaoApplied, "yjz_tiandaoApplied", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureTalismanState();
                EnsureTiandaoLists();
                MaintainTalismanEffects();
            }
        }

        private void EnsureTiandaoLists()
        {
            if (tiandaoOriginalSkillDefs == null) tiandaoOriginalSkillDefs = new List<string>();
            if (tiandaoOriginalSkillLevels == null) tiandaoOriginalSkillLevels = new List<int>();
            if (tiandaoOriginalSkillXp == null) tiandaoOriginalSkillXp = new List<float>();
            if (tiandaoOriginalSkillXpToday == null) tiandaoOriginalSkillXpToday = new List<float>();
        }

        /// <summary>
        /// Applies physical talisman effects without permanently rewriting the
        /// pawn template.  Tiandao snapshots every usable skill before raising
        /// it to 20 and restores both level and XP when the talisman leaves its
        /// slot.  This also makes existing saves safe when the effect is added.
        /// </summary>
        internal void MaintainTalismanEffects()
        {
            if (Pawn == null || Pawn.skills == null) return;
            EnsureTalismanState();
            EnsureTiandaoLists();
            HashSet<string> activeDefs = new HashSet<string>(activeTalismanDefs.Take(TalismanSlotCount)
                .Where(defName => !defName.NullOrEmpty()));

            foreach (KeyValuePair<string, string> pair in TalismanEffectHediffs)
            {
                HediffDef effectDef = DefDatabase<HediffDef>.GetNamedSilentFail(pair.Value);
                if (effectDef == null || Pawn.health == null) continue;
                Hediff existing = Pawn.health.hediffSet.GetFirstHediffOfDef(effectDef);
                if (activeDefs.Contains(pair.Key))
                {
                    if (existing == null) Pawn.health.AddHediff(effectDef);
                }
                else if (existing != null)
                {
                    Pawn.health.RemoveHediff(existing);
                }
            }

            MaintainJiqingMood(activeDefs.Contains("YJZ_Talisman_Jiqing"));

            bool tiandaoActive = activeDefs.Contains("YJZ_Talisman_Tiandao");
            bool liuyangActive = activeDefs.Contains("YJZ_Talisman_Liuyang");
            bool active = tiandaoActive || liuyangActive;

            if (active)
            {
                if (!tiandaoApplied)
                {
                    tiandaoOriginalSkillDefs.Clear();
                    tiandaoOriginalSkillLevels.Clear();
                    tiandaoOriginalSkillXp.Clear();
                    tiandaoOriginalSkillXpToday.Clear();
                    foreach (SkillRecord record in Pawn.skills.skills)
                    {
                        if (record == null || record.def == null || record.TotallyDisabled) continue;
                        tiandaoOriginalSkillDefs.Add(record.def.defName);
                        tiandaoOriginalSkillLevels.Add(record.levelInt);
                        tiandaoOriginalSkillXp.Add(record.xpSinceLastLevel);
                        tiandaoOriginalSkillXpToday.Add(record.xpSinceMidnight);
                    }
                    tiandaoApplied = true;
                }

                // Rebuild from the saved baseline before composing simultaneous
                // skill talismans, so slot ordering cannot change the result.
                for (int i = 0; i < tiandaoOriginalSkillDefs.Count && i < tiandaoOriginalSkillLevels.Count; i++)
                {
                    SkillDef originalDef = DefDatabase<SkillDef>.GetNamedSilentFail(tiandaoOriginalSkillDefs[i]);
                    if (originalDef == null) continue;
                    SkillRecord originalRecord = Pawn.skills.GetSkill(originalDef);
                    if (originalRecord != null) originalRecord.levelInt = tiandaoOriginalSkillLevels[i];
                }
                if (tiandaoActive)
                    foreach (SkillRecord record in Pawn.skills.skills)
                        if (record != null && !record.TotallyDisabled) record.levelInt = 20;
                if (liuyangActive)
                {
                    Pawn.skills.GetSkill(SkillDefOf.Shooting).levelInt = 20;
                    Pawn.skills.GetSkill(SkillDefOf.Melee).levelInt = 20;
                    Pawn.skills.GetSkill(SkillDefOf.Artistic).levelInt = 0;
                }
                return;
            }

            if (!tiandaoApplied) return;
            int count = new[]
            {
                tiandaoOriginalSkillDefs.Count,
                tiandaoOriginalSkillLevels.Count,
                tiandaoOriginalSkillXp.Count,
                tiandaoOriginalSkillXpToday.Count
            }.Min();
            for (int i = 0; i < count; i++)
            {
                SkillDef def = DefDatabase<SkillDef>.GetNamedSilentFail(tiandaoOriginalSkillDefs[i]);
                if (def == null) continue;
                SkillRecord record = Pawn.skills.GetSkill(def);
                if (record == null) continue;
                record.levelInt = Mathf.Clamp(tiandaoOriginalSkillLevels[i], 0, 20);
                record.xpSinceLastLevel = Mathf.Max(0f, tiandaoOriginalSkillXp[i]);
                record.xpSinceMidnight = Mathf.Max(0f, tiandaoOriginalSkillXpToday[i]);
            }
            tiandaoApplied = false;
            tiandaoOriginalSkillDefs.Clear();
            tiandaoOriginalSkillLevels.Clear();
            tiandaoOriginalSkillXp.Clear();
            tiandaoOriginalSkillXpToday.Clear();
        }

        private void MaintainJiqingMood(bool active)
        {
            if (Pawn.needs?.mood?.thoughts?.memories == null || !Pawn.IsHashIntervalTick(250)) return;
            ThoughtDef thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("YJZ_Thought_JiqingScenery");
            if (thoughtDef == null) return;

            Pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(thoughtDef);
            if (!active || !Pawn.Spawned) return;

            Room room = Pawn.GetRoom();
            float impressiveness = room?.GetStat(RoomStatDefOf.Impressiveness) ?? 0f;
            int stage = impressiveness >= 120f ? 2 : impressiveness >= 60f ? 1 : 0;
            Pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(thoughtDef, stage));
        }

        public bool HasActiveTalisman(string defName)
        {
            EnsureTalismanState();
            return !defName.NullOrEmpty() && activeTalismanDefs.Take(TalismanSlotCount).Contains(defName);
        }

        internal float TalismanEnergyRecoveryFactor { get { return HasActiveTalisman("YJZ_Talisman_Guihai") ? 3f : 1f; } }
        internal float TalismanCooldownFactor { get { return HasActiveTalisman("YJZ_Talisman_Beidou") ? .75f : 1f; } }

        internal static bool PawnHasActiveTalisman(Pawn pawn, string defName)
        {
            CompYunjianziCultivation comp = pawn == null ? null : pawn.TryGetComp<CompYunjianziCultivation>();
            return comp != null && comp.HasActiveTalisman(defName);
        }

        private static string EffectDescription(string defName)
        {
            string description;
            return defName != null && TalismanEffectDescriptions.TryGetValue(defName, out description) ? description : string.Empty;
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
                        : "当前生效：" + active.LabelCap + "。\n\n" + EffectDescription(active.def.defName) +
                          "\n\n右键替换或卸下；被替换的实体符咒会返回云间子的物品栏。",
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
            MaintainTalismanEffects();
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
            MaintainTalismanEffects();
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

    [HarmonyPatch(typeof(QualityUtility), nameof(QualityUtility.GenerateQualityCreatedByPawn),
        new Type[] { typeof(Pawn), typeof(SkillDef), typeof(bool) })]
    internal static class Patch_QualityUtility_YunjianziZhuoyu
    {
        private static void Postfix(Pawn pawn, ref QualityCategory __result)
        {
            if (CompYunjianziCultivation.PawnHasActiveTalisman(pawn, "YJZ_Talisman_Zhuoyu"))
                __result = (QualityCategory)Mathf.Clamp((int)__result + 2,
                    (int)QualityCategory.Awful, (int)QualityCategory.Legendary);
        }
    }

    [HarmonyPatch(typeof(Recipe_Surgery), "CheckSurgeryFail")]
    internal static class Patch_RecipeSurgery_YunjianziBaicao
    {
        private static bool Prefix(Pawn surgeon, ref bool __result)
        {
            if (!CompYunjianziCultivation.PawnHasActiveTalisman(surgeon, "YJZ_Talisman_Baicao")) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(MentalStateHandler), nameof(MentalStateHandler.TryStartMentalState))]
    internal static class Patch_MentalState_YunjianziBumei
    {
        private static bool Prefix(Pawn ___pawn, bool causedByMood, ref bool __result)
        {
            if (!causedByMood || !CompYunjianziCultivation.PawnHasActiveTalisman(___pawn, "YJZ_Talisman_Bumei")) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Pawn_AgeTracker), "TickBiologicalAge")]
    internal static class Patch_PawnAge_YunjianziHuanri
    {
        private static bool Prefix(Pawn ___pawn)
        {
            return !CompYunjianziCultivation.PawnHasActiveTalisman(___pawn, "YJZ_Talisman_Huanri");
        }
    }
}
