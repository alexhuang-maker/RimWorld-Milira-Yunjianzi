using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    internal static class YunjianziSkills
    {
        public const string SummonZhuxie = "sword.summon";
        public const string ArrayCore = "array.core";
        public const string ArrayRecovery = "array.recovery";
        public const string ArrayTeleport = "array.teleport";
        public const string SwordEnchantment = "sword.enchantment";
        public const string SwordWard = "sword.ward";
        public const string ArrayLightning = "array.lightning";
        public const string ArrayEdgeSlow = "array.edge.slow";
        public const string SwordShadowsI = "sword.shadows.1";
        public const string BlinkStrike = "sword.blink";
        public const string SwordRain = "sword.rain";
        public const string SwordRainGolden = "sword.rain.golden";
        public const string SwordRainSpirit = "sword.rain.spirit";
        public const string LightningEnchantment = "sword.lightning";
        public const string MovingArray = "array.moving";
        public const string ArrayTeleportShock = "array.teleport.shock";
        public const string SwordShadowsII = "sword.shadows.2";
        public const string BlinkChain = "sword.blink.chain";
        public const string SwordHealing = "sword.healing";
        public const string DeathLock = "sword.deathlock";
        public const string AllyArrayShield = "array.allyshield";
        public const string ArrayBarrier = "array.barrier";
        public const string ArrayEdgeStun = "array.edge.stun";
        public const string SwordShadowsIII = "sword.shadows.3";
        public const string ArmorIgnore = "sword.armorignore";
        public const string ShadowAffixes = "sword.shadowaffixes";
        public const string ArrayReflect = "array.reflect";
        public const string ArrayStorm = "array.storm";
        public const string ArrayTeleportMastery = "array.teleport.mastery";

        public static readonly List<YunjianziSkillNode> All = new List<YunjianziSkillNode>
        {
            N(SummonZhuxie, "唤起诛邪", "研读宝卷后先天掌握。消耗10灵力召回世界中唯一的诛邪。", 1, 0, 0),
            N(ArrayCore, "琉璃阵眼", "解锁仙阵诀：布置持续60秒的固定琉璃阵眼。", 1, 1, 0),
            N(ArrayRecovery, "阵中回元", "阵内友方缓慢恢复非永久伤势。", 1, 1, 1, ArrayCore),
            N(ArrayTeleport, "阵遁", "解锁紫电阵遁，可从阵外或阵内传送至阵法覆盖范围。", 1, 1, 2, ArrayCore),

            N(SwordEnchantment, "诛邪附灵", "诛邪命中短暂眩晕生物、麻痹机械；目标随后获得控制免疫，不能形成永控。", 2, 0, 0, SummonZhuxie),
            N(SwordWard, "御剑护体", "解锁诛邪的灵力格挡条，脱离伤害后缓慢恢复。", 2, 0, 1, SummonZhuxie),
            N(ArrayLightning, "引雷入阵", "仙阵每10秒落下两道雷霆，尽量锁定不同敌人。", 2, 1, 0, ArrayCore),
            N(ArrayEdgeSlow, "迟滞阵纹", "敌人触碰固定阵或随身阵边界时减速10秒；同一敌人短时间内不会再次触发。", 2, 1, 1, ArrayCore),

            N(SwordShadowsI, "剑影·双仪", "解锁两把剑影，在敌阵间持续穿梭攻击。", 3, 0, 0, SummonZhuxie),
            N(BlinkStrike, "瞬斩", "消耗20灵力闪现至敌人身后强力斩击，冷却5秒；可右键开启自动施放。", 3, 0, 1, SummonZhuxie),
            N(SwordRain, "天剑雨", "消耗80灵力，在迫击炮爆炸范围内降下两轮纯伤害剑雨。", 3, 0, 2, SummonZhuxie),
            N(LightningEnchantment, "雷纹附魔", "诛邪命中与瞬斩命中附加落雷。需要先掌握诛邪附灵。", 3, 0, 3, SwordEnchantment),
            N(MovingArray, "随身阵", "布阵时额外生成一个丝滑跟随云间子的阵法领域。", 3, 1, 0, ArrayCore),
            N(ArrayTeleportShock, "震雷遁", "阵遁落点一格范围内产生击退；境界越高，范围与距离越大。", 3, 1, 1, ArrayTeleport),

            N(SwordShadowsII, "剑影·四象", "剑影数量由两把提升至四把。", 4, 0, 0, SwordShadowsI),
            N(BlinkChain, "诛邪连斩", "瞬斩击杀目标（包括附带落雷）后，自动寻找下一目标继续释放。", 4, 0, 1, BlinkStrike),
            N(SwordHealing, "剑息疗伤", "诛邪命中会修复自身伤口并降低流血。", 4, 0, 2, LightningEnchantment),
            N(DeathLock, "诛邪护主", "手持诛邪濒危时锁血20秒；具有较长的再次触发间隔。", 4, 0, 3, SummonZhuxie),
            N(SwordRainGolden, "天剑雨·藏锋", "天剑雨冷却缩短5秒，可储存两次充能，并扩大覆盖范围。", 4, 0, 4, SwordRain),
            N(AllyArrayShield, "众生灵盾", "阵内友方与友方机械获得弱版灵力护盾。", 4, 1, 0, ArrayCore),
            N(ArrayBarrier, "金丹界壁", "仙阵改为每秒消耗5灵力持续维持，并阻挡阵外射入的抛射物。", 4, 1, 1, AllyArrayShield),
            N(ArrayEdgeStun, "镇魂阵纹", "迟滞阵纹强化：触碰阵法边界的敌人先被眩晕3秒，随后继续承受10秒减速。", 4, 1, 2, ArrayEdgeSlow),

            N(SwordShadowsIII, "剑影·八荒", "剑影数量由四把提升至八把。", 5, 0, 0, SwordShadowsII),
            N(ArmorIgnore, "破妄剑意", "诛邪、瞬斩与剑影攻击无视护甲。", 5, 0, 1, SummonZhuxie),
            N(ShadowAffixes, "万剑同契", "剑影继承已学习的眩晕/麻痹与落雷附魔；每把剑影的落雷独立使用五倍冷却。", 5, 0, 2, SwordShadowsI, SwordEnchantment),
            N(SwordRainSpirit, "天剑雨·化神", "天剑雨冷却再次缩短5秒，并在原有两轮后追加两轮攻击。", 5, 0, 3, SwordRainGolden),
            N(ArrayReflect, "返照界壁", "界壁不再仅仅阻挡，而会把敌方抛射物反射回去。", 5, 1, 0, ArrayBarrier),
            N(ArrayStorm, "万钧雷域", "仙阵落雷频率提升至5秒，并强化雷击。", 5, 1, 1, ArrayLightning),
            N(ArrayTeleportMastery, "化神阵遁", "阵遁的击退范围与距离提升至化神层次。", 5, 1, 2, ArrayTeleportShock)
        };

        private static YunjianziSkillNode N(string id, string label, string description, int realm, int branch, int column, params string[] prerequisites)
        {
            return new YunjianziSkillNode(id, label, description, realm, branch, column, prerequisites);
        }

        public static YunjianziSkillNode Get(string id)
        {
            return All.FirstOrDefault(node => node.Id == id);
        }
    }

    internal sealed class YunjianziSkillNode
    {
        public readonly string Id;
        public readonly string Label;
        public readonly string Description;
        public readonly int Realm;
        public readonly int Branch;
        public readonly int Column;
        public readonly string[] Prerequisites;

        public YunjianziSkillNode(string id, string label, string description, int realm, int branch, int column, string[] prerequisites)
        {
            Id = id;
            Label = label;
            Description = description;
            Realm = realm;
            Branch = branch;
            Column = column;
            Prerequisites = prerequisites ?? new string[0];
        }
    }

    public sealed partial class CompYunjianziCultivation
    {
        private static readonly float[] SkillCostFractions = { 0f, .10f, .20f, .40f, .70f, 1f };
        private List<string> learnedSkills = new List<string>();

        public bool HasSkill(string id)
        {
            return id == YunjianziSkills.SummonZhuxie || (learnedSkills != null && learnedSkills.Contains(id));
        }

        public int LearnedSkillCountAtRealm(int realm)
        {
            if (learnedSkills == null) return 0;
            return learnedSkills.Count(id =>
            {
                YunjianziSkillNode node = YunjianziSkills.Get(id);
                return node != null && node.Id != YunjianziSkills.SummonZhuxie && node.Realm == realm;
            });
        }

        internal int SkillCost(YunjianziSkillNode node)
        {
            int bought = LearnedSkillCountAtRealm(node.Realm);
            float fraction = bought < SkillCostFractions.Length ? SkillCostFractions[bought] : 2f;
            return Mathf.RoundToInt(EnergyCapForRealm(node.Realm) * fraction);
        }

        internal string SkillLockReason(YunjianziSkillNode node)
        {
            if (node == null) return "无效技能。";
            if (HasSkill(node.Id)) return "已掌握。";
            if (!HasOpenedBoundScroll) return "需要先研读云间宝卷。";
            if (Realm < node.Realm) return "需要达到" + RealmName(node.Realm) + "境。";
            for (int i = 0; i < node.Prerequisites.Length; i++)
            {
                if (!HasSkill(node.Prerequisites[i]))
                {
                    YunjianziSkillNode prerequisite = YunjianziSkills.Get(node.Prerequisites[i]);
                    return "前置未解锁：" + (prerequisite == null ? "唤起诛邪" : prerequisite.Label) + "。";
                }
            }
            int cost = SkillCost(node);
            if (Energy + .001f < cost) return "当前灵力不足，需要" + cost + "点。";
            return null;
        }

        internal void TryLearnSkill(YunjianziSkillNode node)
        {
            string reason = SkillLockReason(node);
            if (reason != null)
            {
                if (reason != "已掌握。") Messages.Message(reason, Pawn, MessageTypeDefOf.RejectInput, false);
                return;
            }
            int cost = SkillCost(node);
            if (cost > 0 && !TrySpend(cost)) return;
            if (learnedSkills == null) learnedSkills = new List<string>();
            learnedSkills.Add(node.Id);
            if (node.Id == YunjianziSkills.SwordWard) Shield = ShieldMax;
            Messages.Message("云间子已悟得「" + node.Label + "」。", Pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        private bool DebugSkillMode { get { return Prefs.DevMode && DebugSettings.godMode; } }

        private void DebugOpenBoundScroll()
        {
            if (!DebugSkillMode) return;
            boundScrollConsumed = true;
            boundScrollCreated = true;
            cultivatedEnergyMax = Mathf.Max(cultivatedEnergyMax, 10f);
            Energy = EnergyMax;
            Messages.Message("DEV：已直接研读云间宝卷。", Pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        private void DebugAdvanceRealm()
        {
            if (!DebugSkillMode || Realm >= 5 || RealmHediff == null) return;
            RealmHediff.Severity = Mathf.Clamp(Realm + 1, 1, 5);
            DebugFillCultivation();
            Messages.Message("DEV：境界已提升至" + RealmLabel + "。", Pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        private void DebugFillCultivation()
        {
            if (!DebugSkillMode) return;
            if (!HasOpenedBoundScroll) DebugOpenBoundScroll();
            cultivatedEnergyMax = RealmEnergyCap;
            Energy = EnergyMax;
            Shield = ShieldMax;
            Messages.Message("DEV：灵力、修为上限与剑罡已补满。", Pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        private void DebugUnlockThroughCurrentRealm()
        {
            if (!DebugSkillMode) return;
            if (!HasOpenedBoundScroll) DebugOpenBoundScroll();
            if (learnedSkills == null) learnedSkills = new List<string>();
            foreach (YunjianziSkillNode node in YunjianziSkills.All.Where(n => n.Id != YunjianziSkills.SummonZhuxie && n.Realm <= Realm))
                if (!learnedSkills.Contains(node.Id)) learnedSkills.Add(node.Id);
            if (HasSkill(YunjianziSkills.SwordWard)) Shield = ShieldMax;
            Messages.Message("DEV：已解锁" + RealmLabel + "及之前的全部技能。", Pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        internal void DebugForceLearnSkill(YunjianziSkillNode node)
        {
            if (!DebugSkillMode || node == null || node.Id == YunjianziSkills.SummonZhuxie || HasSkill(node.Id)) return;
            if (learnedSkills == null) learnedSkills = new List<string>();
            learnedSkills.Add(node.Id);
            if (node.Id == YunjianziSkills.SwordWard) Shield = ShieldMax;
            Messages.Message("DEV：强制解锁「" + node.Label + "」。", Pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        internal void DebugForgetSkill(YunjianziSkillNode node)
        {
            if (!DebugSkillMode || node == null || node.Id == YunjianziSkills.SummonZhuxie || learnedSkills == null) return;
            if (!learnedSkills.Remove(node.Id)) return;
            if (node.Id == YunjianziSkills.SwordWard) Shield = 0f;
            Messages.Message("DEV：已移除「" + node.Label + "」。", Pawn, MessageTypeDefOf.NeutralEvent, false);
        }

        private void DebugResetSkills()
        {
            if (!DebugSkillMode) return;
            if (learnedSkills == null) learnedSkills = new List<string>();
            else learnedSkills.Clear();
            autoBlink = false;
            Shield = 0f;
            CancelArrays();
            Messages.Message("DEV：技能树已重置，仅保留先天唤起诛邪。", Pawn, MessageTypeDefOf.NeutralEvent, false);
        }

        internal static float EnergyCapForRealm(int realm)
        {
            return new[] { 0f, 100f, 200f, 500f, 1000f, 2000f }[Mathf.Clamp(realm, 0, 5)];
        }

        internal static string RealmName(int realm)
        {
            return new[] { "未入道", "练气", "筑基", "紫府", "金丹", "化神" }[Mathf.Clamp(realm, 0, 5)];
        }

        private void ExposeSkillTreeData()
        {
            Scribe_Collections.Look(ref learnedSkills, "yjz_learnedSkills", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (learnedSkills == null) learnedSkills = new List<string>();
                learnedSkills = learnedSkills.Where(id => YunjianziSkills.Get(id) != null).Distinct().ToList();
            }
        }
    }

    public sealed class Window_YunjianziCultivationTree : Window
    {
        private static readonly Color Panel = new Color(.035f, .10f, .13f, .98f);
        private static readonly Color Learned = new Color(.20f, .72f, .76f, .96f);
        private static readonly Color Available = new Color(.76f, .65f, .32f, .98f);
        private static readonly Color Locked = new Color(.19f, .24f, .26f, .98f);
        private static readonly Color LineLearned = new Color(.30f, .90f, .94f, .92f);
        private static readonly Color LineLocked = new Color(.34f, .39f, .40f, .70f);
        private readonly CompYunjianziCultivation comp;
        private readonly Dictionary<string, Rect> nodeRects = new Dictionary<string, Rect>();

        public Window_YunjianziCultivationTree(CompYunjianziCultivation cultivation)
        {
            comp = cultivation;
            doCloseX = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            forcePause = true;
        }

        public override Vector2 InitialSize { get { return new Vector2(1180f, 820f); } }

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.DrawBoxSolid(inRect, Panel);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(20f, 14f, 500f, 36f), "云间宝卷 · 修为参悟");
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(520f, 19f, 520f, 26f), string.Format("境界：{0}    当前灵力：{1:0}/{2:0}", comp.RealmLabel, comp.Energy, comp.EnergyMax));
            if (Prefs.DevMode && DebugSettings.godMode)
            {
                GUI.color = new Color(1f, .72f, .30f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(790f, 43f, 330f, 20f), "GOD MODE：左键免费解锁；右键移除节点");
                GUI.color = Color.white;
            }
            Widgets.DrawLineHorizontal(18f, 55f, inRect.width - 36f);

            Rect swordPanel = new Rect(18f, 70f, 530f, 665f);
            Rect arrayPanel = new Rect(562f, 70f, 530f, 665f);
            DrawBranchPanel(swordPanel, 0, "御剑诀", "先天已掌握：唤起诛邪");
            DrawBranchPanel(arrayPanel, 1, "仙阵诀", "从琉璃阵眼起步，逐层拆分阵法特效");
            DrawPrerequisiteLines();
            DrawBranchNodes(swordPanel, 0);
            DrawBranchNodes(arrayPanel, 1);

            Rect future = new Rect(18f, 744f, inRect.width - 36f, 24f);
            GUI.color = new Color(.62f, .72f, .74f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(future, "阳符诀 · 后续开放");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private void DrawBranchPanel(Rect panel, int branch, string title, string subtitle)
        {
            Widgets.DrawMenuSection(panel);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(panel.x + 10f, panel.y + 8f, panel.width - 20f, 30f), title);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(.65f, .80f, .82f);
            Widgets.Label(new Rect(panel.x + 10f, panel.y + 39f, panel.width - 20f, 22f), subtitle);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            for (int realm = 1; realm <= 5; realm++)
            {
                float y = panel.y + 75f + (realm - 1) * 115f;
                GUI.color = realm <= comp.Realm ? new Color(.48f, .86f, .86f) : new Color(.42f, .46f, .47f);
                Widgets.Label(new Rect(panel.x + 12f, y + 22f, 48f, 28f), CompYunjianziCultivation.RealmName(realm));
                GUI.color = Color.white;
                Widgets.DrawLineHorizontal(panel.x + 60f, y + 55f, panel.width - 72f);
            }
        }

        private void CacheNodeRects(Rect panel, int branch)
        {
            List<YunjianziSkillNode> nodes = YunjianziSkills.All.Where(n => n.Branch == branch).ToList();
            for (int i = 0; i < nodes.Count; i++)
            {
                YunjianziSkillNode node = nodes[i];
                float x = panel.x + 65f + node.Column * 89f;
                float y = panel.y + 77f + (node.Realm - 1) * 115f;
                nodeRects[node.Id] = new Rect(x, y, 84f, 54f);
            }
        }

        private void DrawPrerequisiteLines()
        {
            nodeRects.Clear();
            CacheNodeRects(new Rect(18f, 70f, 530f, 665f), 0);
            CacheNodeRects(new Rect(562f, 70f, 530f, 665f), 1);
            foreach (YunjianziSkillNode node in YunjianziSkills.All)
            {
                Rect child;
                if (!nodeRects.TryGetValue(node.Id, out child)) continue;
                for (int i = 0; i < node.Prerequisites.Length; i++)
                {
                    Rect parent;
                    if (!nodeRects.TryGetValue(node.Prerequisites[i], out parent)) continue;
                    bool lit = comp.HasSkill(node.Prerequisites[i]) && (comp.HasSkill(node.Id) || comp.SkillLockReason(node) == null);
                    Widgets.DrawLine(new Vector2(parent.center.x, parent.yMax), new Vector2(child.center.x, child.y), lit ? LineLearned : LineLocked, 2f);
                }
            }
        }

        private void DrawBranchNodes(Rect panel, int branch)
        {
            foreach (YunjianziSkillNode node in YunjianziSkills.All.Where(n => n.Branch == branch))
            {
                Rect rect = nodeRects[node.Id];
                bool learned = comp.HasSkill(node.Id);
                string reason = comp.SkillLockReason(node);
                bool debug = Prefs.DevMode && DebugSettings.godMode;
                bool available = reason == null || (debug && node.Id != YunjianziSkills.SummonZhuxie);
                Widgets.DrawBoxSolid(rect, learned ? Learned : available ? Available : Locked);
                Widgets.DrawBox(rect, learned || available ? 2 : 1);
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = learned || available ? Color.white : new Color(.62f, .65f, .66f);
                Widgets.Label(rect.ContractedBy(3f), node.Label);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                int cost = comp.SkillCost(node);
                string tip = node.Label + "（" + CompYunjianziCultivation.RealmName(node.Realm) + "）\n" + node.Description +
                    "\n\n本境界已学习：" + comp.LearnedSkillCountAtRealm(node.Realm) + "项\n本次消耗：" + cost + "当前灵力" +
                    "\n费用序列：第1～6项为0%、10%、20%、40%、70%、100%；第7项起每项固定为该境界标准灵力上限的200%。";
                if (!learned && reason != null) tip += debug ? "\n\nGOD MODE：可忽略限制免费解锁。" : "\n\n尚不可学习：" + reason;
                if (debug && learned && node.Id != YunjianziSkills.SummonZhuxie) tip += "\n\nGOD MODE：右键移除此节点。";
                TooltipHandler.TipRegion(rect, tip);
                Event current = Event.current;
                if (debug && learned && node.Id != YunjianziSkills.SummonZhuxie && current != null && current.type == EventType.MouseDown && current.button == 1 && rect.Contains(current.mousePosition))
                {
                    comp.DebugForgetSkill(node);
                    current.Use();
                }
                else if (!learned && Widgets.ButtonInvisible(rect))
                {
                    if (debug) comp.DebugForceLearnSkill(node);
                    else comp.TryLearnSkill(node);
                }
            }
            Text.Font = GameFont.Small;
        }
    }
}
