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
        public const string CloudSwordArray = "sword.cloudarray";
        public const string SwordRainGolden = "sword.rain.golden";
        public const string SwordRainSpirit = "sword.rain.spirit";
        public const string LightningEnchantment = "sword.lightning";
        public const string ChainLightningEnchantment = "sword.lightning.chain";
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
        public const string SanyuanMiyi = "artifact.sanyuan.miyi";
        public const string JiuxiaoXianxu = "array.jiuxiao.xianxu";
        public const string ShenjiSummon = "shenji.summon";
        public const string ShenjiGatling = "shenji.gatling";
        public const string ShenjiRocketJump = "shenji.rocketjump";
        public const string ShenjiShield = "shenji.shield";
        public const string ShenjiDualControl = "shenji.dualcontrol";
        public const string ShenjiGauss = "shenji.gauss";
        public const string ShenjiAccessories = "shenji.accessories";
        public const string ShenjiRecallRepair = "shenji.recallrepair";
        public const string ShenjiDarkShield = "shenji.darkshield";
        public const string ShenjiLegendaryFirepower = "shenji.legendary";
        public const string ShenjiRicochetLaser = "shenji.ricochetlaser";
        public const string ShenjiQuadControl = "shenji.quadcontrol";
        public const string ShenjiWyvern = "shenji.wyvern";
        public const string YangfuPrimer = "yangfu.primer";
        public const string TalismanZhuoyu = "yangfu.zhuoyu";
        public const string TalismanShenjie = "yangfu.shenjie";
        public const string TalismanShenxing = "yangfu.shenxing";
        public const string TalismanJiqing = "yangfu.jiqing";
        public const string TalismanBaicao = "yangfu.baicao";
        public const string TalismanXinjian = "yangfu.xinjian";
        public const string TalismanBeidou = "yangfu.beidou";
        public const string TalismanGuihai = "yangfu.guihai";
        public const string TalismanShenlian = "yangfu.shenlian";
        public const string TalismanHuijian = "yangfu.huijian";
        public const string TalismanBumei = "yangfu.bumei";
        public const string TalismanJiexin = "yangfu.jiexin";
        public const string TalismanLiuyang = "yangfu.liuyang";
        public const string TalismanTiandao = "yangfu.tiandao";
        public const string TalismanHuanri = "yangfu.huanri";
        public const string FireTornadoI = "yangfu.firetornado.1";
        public const string FireTornadoII = "yangfu.firetornado.2";
        public const string FireTornadoIII = "yangfu.firetornado.3";
        public const string FireTornadoIV = "yangfu.firetornado.4";
        public const string FireTornadoV = "yangfu.firetornado.5";
        public const string ThunderWindI = "yangfu.thunderwind.1";
        public const string ThunderWindII = "yangfu.thunderwind.2";
        public const string ThunderWindIII = "yangfu.thunderwind.3";
        public const string ThunderWindIV = "yangfu.thunderwind.4";
        public const string ThunderWindV = "yangfu.thunderwind.5";
        public const string BeanSoldiersI = "yangfu.beansoldiers.1";
        public const string BeanSoldiersII = "yangfu.beansoldiers.2";
        public const string BeanSoldiersIII = "yangfu.beansoldiers.3";
        public const string BeanSoldiersIV = "yangfu.beansoldiers.4";
        public const string BeanSoldiersV = "yangfu.beansoldiers.5";

        public static readonly List<YunjianziSkillNode> All = new List<YunjianziSkillNode>
        {
            N(SummonZhuxie, "诛邪剑契", "以灵力呼唤已经认主的诛邪，使仙剑回到手中。", 1, 0, 3),
            N(ArrayCore, "琉璃定枢", "凝成琉璃阵眼，在周围展开仙阵。", 1, 1, 0),
            N(ArrayRecovery, "灵机养形", "引导阵内灵力，缓慢恢复同伴的寻常伤势。", 1, 1, 0, ArrayCore),
            N(ArrayTeleport, "循纹神行", "沿阵纹移形，抵达仙阵覆盖的位置。", 1, 1, 3, ArrayCore),

            N(SwordEnchantment, "雷炁洗锋", "剑击附带镇压，使生灵短暂失神、机械失灵。目标受到控制后会暂时适应。", 2, 0, 0, SummonZhuxie),
            N(SwordWard, "剑罡卫形", "增强护体剑罡，使其能承受更多冲击，脱离攻击后也恢复得更快。", 2, 0, 1, SummonZhuxie),
            N(ArrayLightning, "雷符镇宫", "在阵中引下天雷，打击敌人并以电磁脉冲扰乱机械。落雷消耗灵力，效果随修为成长。", 2, 1, 1, ArrayCore),
            N(ArrayEdgeSlow, "缚形界纹", "敌人触碰阵界时受到雷击，行动也被阵纹牵制。刚挣脱的目标不会立刻被同一边界再次困住。", 2, 1, 2, ArrayCore),

            N(SwordShadowsI, "双仪分光", "分出剑影，蓄势射向敌阵，贯穿后回到身边。", 3, 0, 2, SummonZhuxie),
            N(BlinkChain, "神行逐寇", "缩地斩杀目标后，余势继续追向附近敌人，直至耗尽。", 3, 0, 3, BlinkStrike),
            N(SwordRainGolden, "九霄剑阵", "改进天剑雨，使其能积蓄更多跃击，并扩大连斩的范围。", 3, 0, 4, SwordRain),
            N(BlinkStrike, "缩地御剑", "闪身至敌后，持诛邪挥出蓄势斩击。可切换为征召时自动寻找出手机会。", 2, 0, 3, SummonZhuxie),
            N(SwordRain, "天剑垂象", "腾空跃向敌阵，落地后分出虚影穿梭连斩。穿梭与攻击范围随修为扩大。", 2, 0, 4, SummonZhuxie),
            N(CloudSwordArray, "云海剑匣", "紫府境花费1点灵气参悟，在米莉拉研究页完成2000进度后，可于仙器炼制台炼制。佩戴后获得以下效果：\n\n六剑归匣：额外增加6道常随剑影，自行寻敌攻伐。\n灵机周流：灵力自然恢复速度提高50%，移动速度增加0.5格/秒，近战与射击冷却系数各降低0.2。\n剑罡护主：剑罡上限提高50%，单次伤害最高按100点结算。剑罡受损越过三分之一刻度时，向5格内敌人爆发剑气，并将敌方单位击退至多4格。", 3, 3, 0),
            N(MovingArray, "身载周天", "将阵势维系在身边，阵域随云间子移动，不再依赖驻地的实体阵眼。", 3, 1, 4, ArrayCore),
            N(ArrayTeleportShock, "紫电惊雷", "循纹神行落定时释放震雷，推开附近敌人并以电磁脉冲扰乱机械。", 3, 1, 3, ArrayTeleport),

            N(SwordShadowsII, "四象巡天", "维系更多剑影，从周身射出，贯穿敌人后回收再发。", 4, 0, 2, SwordShadowsI),
            N(LightningEnchantment, "神霄雷篆", "在诛邪刻入引雷纹，使剑击与缩地斩击能够引下天雷，冲击目标周围并扰乱机械。剑影引雷仍有各自的间隔。", 4, 0, 0, SwordEnchantment),
            N(SwordHealing, "噬血奉主", "诛邪击中敌人后，将剑息引回主人身上，帮助收拢伤口、减轻流血。", 4, 0, 1, LightningEnchantment),
            N(DeathLock, "抱一守命", "遭受致命威胁时，诛邪暂时护住主人的生机。发动后需要很久才能恢复。", 4, 0, 5, SummonZhuxie),
            N(AllyArrayShield, "众生同庇", "为阵中同伴与友方机械提供灵盾，友方所在位置受到落雷时还能补充护盾。", 4, 1, 5, ArrayCore),
            N(ArrayBarrier, "天垣绝矢", "持续耗费灵力维持阵界，拦截从阵外射入的抛射物；成功拦截也消耗灵力。", 4, 1, 6, AllyArrayShield),
            N(ArrayEdgeStun, "镇魂界纹", "强化阵界控制，使越界敌人先短暂失神，随后行动迟缓。", 4, 1, 2, ArrayEdgeSlow),
            N(ArrayStorm, "万钧天狱", "凝成雷枪掷向目标，贯穿沿途并在落点爆发，伤及敌方单位和建筑、扰乱机械。雷枪落地后继续引雷，同时改善阵内被动落雷的频率。", 4, 1, 1, ArrayLightning),

            N(SwordShadowsIII, "太虚万剑", "以元神维系密集剑影，持续射向敌阵。", 5, 0, 2, SwordShadowsII),
            N(ChainLightningEnchantment, "万劫雷锋", "剑击额外释放连锁闪电，在附近敌人之间跳跃，并附带电磁脉冲。此效果与落雷分别生效。", 5, 0, 0, LightningEnchantment),
            N(ArmorIgnore, "照见真形", "使诛邪、缩地斩击与剑影越过装甲防护，直击目标。", 5, 0, 6, SummonZhuxie),
            N(ShadowAffixes, "诸天剑契", "使剑影承载已经掌握的镇压与引雷效果。剑影的引雷节奏仍与本剑不同。", 5, 0, 1, SwordShadowsI, SwordEnchantment),
            N(SwordRainSpirit, "太虚剑劫", "增强天剑雨虚影连斩的威力，并加快充能。", 5, 0, 4, SwordRainGolden),
            N(ArrayReflect, "诸法返照", "将阵外射来的敌方抛射物沿来路反射回去。", 5, 1, 6, ArrayBarrier),
            N(ArrayTeleportMastery, "一念周天", "进一步强化循纹神行落定时的震雷，使其达到化神层次。", 5, 1, 3, ArrayTeleportShock),
            N(SanyuanMiyi, "三垣秘仪斗", "金丹境花费1点灵气参悟，在米莉拉研究页完成2000进度后，可于仙器炼制台炼制。佩戴后获得以下效果：\n\n神机同契：绑定神机伤害提高30%，护盾恢复速度提高50%。命中敌人反哺剑罡并充能；不同神机接连命中同敌时额外共鸣。\n三曜归墟：满100能量自动连续扫射三道光束，将路径敌人推向远端。归墟印持续8秒，神机命中时向6格内同印敌人传递25%伤害。\n百骸归元：神机脱战10秒后，每秒消耗主人2点灵力修复3点普通损伤。不会复活或恢复缺失部件。", 4, 3, 1),
            N(JiuxiaoXianxu, "九霄仙絮", "化神境花费1点灵气参悟，在米莉拉研究页完成2000进度后，可于仙器炼制台炼制。佩戴后获得以下效果：\n\n九霄引雷：仙阵自动落雷间隔减半，最短为1秒；神霄雷电与雷枪爆发伤害翻倍。\n仙絮护身：受到的伤害降低50%。每次引下天雷，恢复剑罡上限的0.5%，最低恢复10点。\n雷炁化生：落雷处凝成雷兽，存续30秒，同地图最多20只。雷兽消散时返还对应落雷实际灵力消耗的80%。", 5, 3, 2),

            N(ShenjiSummon, "灵机启明", "掌握青羽核心的凝成方法，解锁基础神机培育与装备制造。现役神机与各类核心共用额度，总量未满时才能凝成新核心。", 1, 2, 4),
            N(ShenjiGatling, "连星机括", "解锁连星机括，以密集离子弹流压制聚集的敌人。", 2, 2, 0, ShenjiSummon),
            N(ShenjiRocketJump, "流火神行", "使神机能够远距离跃迁，自主作战时也可用来脱离近敌。", 2, 2, 2, ShenjiSummon),
            N(ShenjiShield, "玉光结盾", "为神机提供偏射护盾。受到攻击时消耗盾能，仍有盾能时可完整承受最后一击；电磁脉冲会使其直接破碎。", 2, 2, 3, ShenjiSummon),
            N(ShenjiDualControl, "神分四象", "提高可维系的神机与核心总额度，并扩大同时控制规模。额度未满时可继续凝成青羽核心。", 3, 2, 7, ShenjiSummon),
            N(ShenjiGauss, "日轮炼光", "解锁以日棱晶约束能量的贯日光铳，打击远处敌人。", 3, 2, 0, ShenjiGatling),
            N(ShenjiAccessories, "诸器同参", "使神机能够使用米莉拉通用配件，并解锁九霄天将使用的灵术演算芯片。", 4, 2, 5, ShenjiSummon),
            N(ShenjiRecallRepair, "败形归炉", "神机即将被彻底击溃时，将其召回云间子附近并重整机体。发动后需要较长时间恢复。", 4, 2, 6, ShenjiSummon),
            N(ShenjiDarkShield, "太阴天幕", "强化偏射护盾，提升承载能力、降低受击消耗，并使其能够抵御电磁脉冲。", 4, 2, 3, ShenjiShield),
            N(ShenjiLegendaryFirepower, "天工曜世", "解锁九霄神光仪，使神机能够持续输出高阶辉光火力。", 4, 2, 0, ShenjiGauss),
            N(ShenjiRicochetLaser, "流光千折", "使辉光命中后折向附近敌人。每次折射都会减弱。", 4, 2, 1, ShenjiGauss),
            N(ShenjiQuadControl, "念镇九宫", "进一步扩大控制规模与神机、核心的共同额度。额度未满时可继续凝成青羽核心。", 5, 2, 7, ShenjiDualControl),
            N(ShenjiWyvern, "旧契重明", "读懂前代手记中留下的旧约，呼唤曾与他同行的云麒麟。平日协助搬运，战时主动迎敌、保护友军。形态随云间子的境界变化，突破后再次呼唤即可显现新形态。", 3, 2, 8, ShenjiSummon),

        };

        private static YunjianziSkillNode N(string id, string label, string description, int realm, int branch, int lane, params string[] prerequisites)
        {
            return new YunjianziSkillNode(id, label, description, realm, branch, lane, prerequisites);
        }

        public static YunjianziSkillNode Get(string id)
        {
            return All.FirstOrDefault(node => node.Id == id);
        }

        public static List<string> NormalizeLearnedSkills(IEnumerable<string> learned)
        {
            // Qilin upgrades are now inherent realm benefits. Drop all retired
            // upgrade IDs from old saves so they no longer inflate skill costs.
            return (learned ?? Enumerable.Empty<string>())
                .Where(id => Get(id) != null).Distinct().ToList();
        }
    }

    internal sealed class YunjianziSkillNode
    {
        public readonly string Id;
        public readonly string Label;
        public readonly string Description;
        public readonly int Realm;
        public readonly int Branch;
        public readonly int Lane;
        public readonly string[] Prerequisites;

        public YunjianziSkillNode(string id, string label, string description, int realm, int branch, int lane, string[] prerequisites)
        {
            Id = id;
            Label = label;
            Description = description;
            Realm = realm;
            Branch = branch;
            Lane = lane;
            Prerequisites = prerequisites ?? new string[0];
        }
    }

    public sealed partial class CompYunjianziCultivation
    {
        private List<string> learnedSkills = new List<string>();

        public bool HasSkill(string id)
        {
            if (AwaitingInheritance) return false;
            return id == YunjianziSkills.SummonZhuxie ||
                (id != null && !id.StartsWith("yangfu.", StringComparison.Ordinal) &&
                learnedSkills != null && learnedSkills.Contains(id));
        }

        public int LearnedSkillCountAtRealm(int realm)
        {
            if (learnedSkills == null) return 0;
            return learnedSkills.Count(id =>
            {
                YunjianziSkillNode node = YunjianziSkills.Get(id);
                return node != null && node.Id != YunjianziSkills.SummonZhuxie &&
                    !IsArtifactSkill(node.Id) && node.Realm == realm;
            });
        }

        private static bool IsArtifactSkill(string id)
        {
            return id == YunjianziSkills.SanyuanMiyi || id == YunjianziSkills.CloudSwordArray || id == YunjianziSkills.JiuxiaoXianxu;
        }

        internal int SkillCost(YunjianziSkillNode node)
        {
            if (node != null && IsArtifactSkill(node.Id)) return 1;
            int bought = LearnedSkillCountAtRealm(node.Realm);
            // Each realm has its own purchase counter: first pick is free, then
            // 10%, 20%, 30%... of that realm's energy cap, capped at 200%.
            float fraction = Mathf.Min(bought * .10f, 2f);
            return Mathf.RoundToInt(EnergyCapForRealm(node.Realm) * fraction);
        }

        internal string SkillLockReason(YunjianziSkillNode node)
        {
            if (node == null) return "无效技能。";
            if (HasSkill(node.Id)) return "已掌握。";
            if (!HasOpenedBoundScroll) return "需要先研读前代手记。";
            if (Realm < node.Realm) return "需要达到" + RealmName(node.Realm) + "境。";
            for (int i = 0; i < node.Prerequisites.Length; i++)
            {
                if (!HasSkill(node.Prerequisites[i]))
                {
                    YunjianziSkillNode prerequisite = YunjianziSkills.Get(node.Prerequisites[i]);
                    return "前置未解锁：" + (prerequisite == null ? "诛邪剑契" : prerequisite.Label) + "。";
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
            YunjianziArtifactResearch.UnlockLearned(learnedSkills);
            if (node.Id == YunjianziSkills.SwordWard) Shield = ShieldMax;
            if (node.Id == YunjianziSkills.ShenjiShield || node.Id == YunjianziSkills.ShenjiDarkShield)
                RefreshBoundShenjiShields();
            Messages.Message("云间子已悟得「" + node.Label + "」。", Pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        private bool DebugSkillMode { get { return Prefs.DevMode && DebugSettings.godMode; } }

        private void DebugOpenBoundScroll()
        {
            if (!DebugSkillMode) return;
            boundScrollConsumed = true;
            AcceptInheritance();
            boundScrollCreated = true;
            cultivatedEnergyMax = Mathf.Max(cultivatedEnergyMax, 10f);
            Energy = EnergyMax;
            Messages.Message("DEV：已直接研读前代手记。", Pawn, MessageTypeDefOf.PositiveEvent, false);
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
            YunjianziArtifactResearch.UnlockLearned(learnedSkills);
            Shield = ShieldMax;
            Messages.Message("DEV：已解锁" + RealmLabel + "及之前的全部技能。", Pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        internal void DebugForceLearnSkill(YunjianziSkillNode node)
        {
            if (!DebugSkillMode || node == null || node.Id == YunjianziSkills.SummonZhuxie || HasSkill(node.Id)) return;
            if (learnedSkills == null) learnedSkills = new List<string>();
            learnedSkills.Add(node.Id);
            YunjianziArtifactResearch.UnlockLearned(learnedSkills);
            if (node.Id == YunjianziSkills.SwordWard) Shield = ShieldMax;
            if (node.Id == YunjianziSkills.ShenjiShield || node.Id == YunjianziSkills.ShenjiDarkShield)
                RefreshBoundShenjiShields();
            Messages.Message("DEV：强制解锁「" + node.Label + "」。", Pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        internal void DebugForgetSkill(YunjianziSkillNode node)
        {
            if (!DebugSkillMode || node == null || node.Id == YunjianziSkills.SummonZhuxie || learnedSkills == null) return;
            if (!learnedSkills.Remove(node.Id)) return;
            if (node.Id == YunjianziSkills.SwordWard) Shield = shield;
            Messages.Message("DEV：已移除「" + node.Label + "」。", Pawn, MessageTypeDefOf.NeutralEvent, false);
        }

        private void DebugResetSkills()
        {
            if (!DebugSkillMode) return;
            if (learnedSkills == null) learnedSkills = new List<string>();
            else learnedSkills.Clear();
            autoBlink = false;
            Shield = shield;
            CancelArrays();
            Messages.Message("DEV：技能树已重置，仅保留先天诛邪剑契。", Pawn, MessageTypeDefOf.NeutralEvent, false);
        }

        internal static float EnergyCapForRealm(int realm)
        {
            // Keep skill costs tied to the same authoritative caps used by
            // cultivation and breakthrough progression.
            return EnergyCaps[Mathf.Clamp(realm, 0, EnergyCaps.Length - 1)];
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
                learnedSkills = YunjianziSkills.NormalizeLearnedSkills(learnedSkills);
                YunjianziArtifactResearch.UnlockLearned(learnedSkills);
            }
        }

    }

    [StaticConstructorOnStartup]
    public sealed class Window_YunjianziCultivationTree : Window
    {
        private static readonly Color Panel = new Color(.025f, .075f, .09f, .99f);
        private static readonly Color JadeDark = new Color(.055f, .16f, .17f, .98f);
        private static readonly Color JadeLine = new Color(.50f, .78f, .76f, .82f);
        private static readonly Color Ivory = new Color(.88f, .91f, .82f, 1f);
        private static readonly Color Learned = new Color(.12f, .49f, .50f, .98f);
        private static readonly Color Available = new Color(.57f, .45f, .20f, .98f);
        private static readonly Color Locked = new Color(.12f, .18f, .19f, .98f);
        private static readonly Color LineLearned = new Color(.30f, .90f, .94f, .92f);
        private static readonly Color LineLocked = new Color(.34f, .39f, .40f, .70f);
        private static readonly Color Gold = new Color(.93f, .76f, .35f, 1f);
        private static readonly Color Mist = new Color(.64f, .82f, .82f, 1f);
        private readonly CompYunjianziCultivation comp;
        private readonly Dictionary<string, Rect> nodeRects = new Dictionary<string, Rect>();
        private int selectedBranch;

        private static readonly string[] BranchTitles = { "御剑诀", "仙阵诀", "神机诀", "仙器拾遗" };
        private static readonly Texture2D[] BranchIcons =
        {
            YunjianziAbilityTextures.SummonZhuxie,
            YunjianziAbilityTextures.ImmortalArray,
            YunjianziAbilityTextures.SummonShenji,
            YunjianziAbilityTextures.CultivationTree
        };
        private static readonly string[] BranchIntroductions =
        {
            "白刃至今，御剑之术由杀伐之道而生。山门以灵御剑，诛邪无数，又以剑正道之谬。以剑载敌、遁行为小乘；凭心御剑，方是大道。",
            "阵法之道，玄妙非常。入道之人可凭一器演化，瞒天拘地；亦可引动天地雷霆，困敌于阵中，或融入心神，御敌无形。一人起阵，亦自成方圆。",
            "器者载道，机者通神。神机诀取米莉拉灵械之学，合阵枢、灵纹与修行之法，以心御机，以机行道。灵念所至，百工与兵锋皆如臂使指。",
            "拾前代遗篇，参藏器之道。紫府藏剑，金丹御机，化神织雷。三器各费1点灵气参悟，再于米莉拉研究页各完成2000进度，即可在仙器炼制台炼制；无其他技能前置，不计入常规技能数量。"
        };
        private static readonly string[] BranchRootIds =
        {
            YunjianziSkills.SummonZhuxie,
            YunjianziSkills.ArrayCore,
            YunjianziSkills.ShenjiSummon,
            string.Empty
        };
        private static readonly string[][] BranchLaneLabels =
        {
            new[] { "雷锋", "剑契", "分光", "神行", "剑劫", "守命", "真形" },
            new[] { "养形", "雷宫", "界纹", "神行", "周天", "同庇", "天垣" },
            new[] { "主火力", "折射", "机动", "护盾", "预留", "同参", "归炉", "御机", "瑞兽真形" },
            new[] { "剑匣", "秘仪", "仙絮" }
        };

        public Window_YunjianziCultivationTree(CompYunjianziCultivation cultivation)
        {
            comp = cultivation;
            doCloseX = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            forcePause = true;
        }

        public override Vector2 InitialSize { get { return new Vector2(1480f, 820f); } }

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.DrawBoxSolid(inRect, Panel);
            DrawDaoScrollBackdrop(inRect);
            GUI.color = JadeLine;
            Widgets.DrawBox(inRect.ContractedBy(2f), 2);
            GUI.color = Color.white;

            Rect header = new Rect(10f, 9f, inRect.width - 20f, 50f);
            Widgets.DrawBoxSolid(header, new Color(.035f, .145f, .16f, .99f));
            Widgets.DrawBoxSolid(new Rect(header.x, header.y, 6f, header.height), Gold);
            GUI.color = new Color(.63f, .86f, .82f, .42f);
            Widgets.DrawLineHorizontal(header.x + 4f, header.yMax - 1f, header.width - 8f);
            GUI.color = Color.white;
            Widgets.DrawTextureFitted(new Rect(25f, 16f, 34f, 34f), YunjianziAbilityTextures.CultivationTree, 1f);
            Text.Font = GameFont.Medium;
            GUI.color = Ivory;
            Widgets.Label(new Rect(70f, 11f, 360f, 28f), "前代云间子手记 · 修为参悟");
            Text.Font = GameFont.Tiny;
            GUI.color = Mist;
            Widgets.Label(new Rect(72f, 36f, 350f, 16f), "循境问道 · 以灵印证 · 四诀同参");
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            DrawHeaderRealms(new Rect(438f, 14f, 260f, 34f));
            Rect energyBack = new Rect(720f, 20f, 330f, 22f);
            Widgets.DrawBoxSolid(energyBack, new Color(.015f, .035f, .045f, 1f));
            GUI.color = new Color(.56f, .82f, .77f, .72f);
            Widgets.DrawBox(energyBack, 1);
            GUI.color = Color.white;
            float energyPct = comp.Energy / Mathf.Max(1f, comp.EnergyMax);
            Widgets.DrawBoxSolid(new Rect(energyBack.x + 2f, energyBack.y + 2f, (energyBack.width - 4f) * energyPct, energyBack.height - 4f),
                new Color(.24f, .72f, .72f, 1f));
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Ivory;
            Widgets.Label(energyBack, string.Format("灵力  {0:0.#} / {1:0.#}", comp.Energy, comp.EnergyMax));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            if (Prefs.DevMode && DebugSettings.godMode)
            {
                GUI.color = new Color(1f, .72f, .30f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(1070f, 22f, 330f, 20f), "GOD MODE：左键解锁 · 右键遗忘");
                GUI.color = Color.white;
            }

            DrawTechniqueTabs(new Rect(18f, 66f, inRect.width - 36f, 38f));
            DrawTechniquePanel(new Rect(18f, 112f, inRect.width - 36f, 620f), selectedBranch);

            Rect future = new Rect(18f, 741f, inRect.width - 36f, 30f);
            Widgets.DrawBoxSolid(future, new Color(.035f, .105f, .115f, .92f));
            DrawLegendChip(new Rect(future.x + 410f, future.y + 6f, 116f, 18f), Learned, "◆ 已掌握");
            DrawLegendChip(new Rect(future.x + 536f, future.y + 6f, 116f, 18f), Available, "◇ 可参悟");
            DrawLegendChip(new Rect(future.x + 662f, future.y + 6f, 116f, 18f), Locked, "· 尚封存");
            GUI.color = Mist;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(future.x + 22f, future.y + 7f, 360f, 18f), "纵向专精轨道 · 悬停节点查看道法详解与灵力消耗");
            GUI.color = Color.white;
        }

        private static void DrawDaoScrollBackdrop(Rect rect)
        {
            GUI.color = new Color(.18f, .48f, .48f, .10f);
            for (float x = rect.x + 34f; x < rect.xMax; x += 72f)
                Widgets.DrawLineVertical(x, rect.y + 4f, rect.height - 8f);
            for (float y = rect.y + 32f; y < rect.yMax; y += 72f)
                Widgets.DrawLineHorizontal(rect.x + 4f, y, rect.width - 8f);
            GUI.color = new Color(.92f, .74f, .32f, .16f);
            Widgets.DrawLine(new Vector2(rect.x + 18f, rect.y + 18f), new Vector2(rect.x + 84f, rect.y + 18f), GUI.color, 2f);
            Widgets.DrawLine(new Vector2(rect.xMax - 84f, rect.yMax - 18f), new Vector2(rect.xMax - 18f, rect.yMax - 18f), GUI.color, 2f);
            GUI.color = Color.white;
        }

        private void DrawHeaderRealms(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(.018f, .07f, .078f, .9f));
            float step = rect.width / 5f;
            for (int i = 1; i <= 5; i++)
            {
                float size = i == comp.Realm ? 28f : 21f;
                Rect emblem = new Rect(rect.x + step * (i - .5f) - size * .5f,
                    rect.center.y - size * .5f, size, size);
                GUI.color = i == comp.Realm ? Color.white :
                    i < comp.Realm ? new Color(.66f, .92f, .88f, .78f) : new Color(.35f, .45f, .46f, .35f);
                GUI.DrawTexture(emblem, GizmoYunjianziCultivation.RealmTexture(i), ScaleMode.ScaleToFit, true);
                if (i == comp.Realm)
                {
                    GUI.color = Gold;
                    Widgets.DrawBox(emblem.ExpandedBy(2f), 1);
                }
            }
            GUI.color = Color.white;
            TooltipHandler.TipRegion(rect, "当前境界：" + comp.RealmLabel + "\n练气 · 筑基 · 紫府 · 金丹 · 化神");
        }

        private static void DrawLegendChip(Rect rect, Color color, string label)
        {
            Widgets.DrawBoxSolid(rect, new Color(color.r, color.g, color.b, .78f));
            GUI.color = Ivory;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private void DrawTechniqueTabs(Rect tabsRect)
        {
            float gap = 8f;
            float width = (tabsRect.width - gap * (BranchTitles.Length - 1)) / BranchTitles.Length;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            for (int branch = 0; branch < BranchTitles.Length; branch++)
            {
                Rect tab = new Rect(tabsRect.x + branch * (width + gap), tabsRect.y, width, tabsRect.height);
                bool selected = branch == selectedBranch;
                Widgets.DrawBoxSolid(tab, selected ? new Color(.10f, .36f, .37f, .98f) : new Color(.055f, .135f, .15f, .98f));
                GUI.color = selected ? JadeLine : new Color(.28f, .43f, .44f, .8f);
                Widgets.DrawBox(tab, selected ? 2 : 1);
                Rect icon = new Rect(tab.x + 18f, tab.y + 6f, 26f, 26f);
                GUI.color = selected ? Color.white : new Color(.62f, .72f, .72f, .72f);
                GUI.DrawTexture(icon, BranchIcons[branch], ScaleMode.ScaleToFit, true);
                GUI.color = selected ? Ivory : new Color(.65f, .73f, .73f);
                Widgets.Label(new Rect(tab.x + 46f, tab.y, tab.width - 58f, tab.height), BranchTitles[branch]);
                if (selected)
                {
                    GUI.color = Gold;
                    Widgets.DrawBoxSolid(new Rect(tab.x + 2f, tab.yMax - 3f, tab.width - 4f, 3f), Gold);
                }
                GUI.color = Color.white;
                if (Widgets.ButtonInvisible(tab)) selectedBranch = branch;
            }
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawTechniquePanel(Rect panel, int branch)
        {
            Widgets.DrawBoxSolid(panel, new Color(.025f, .085f, .10f, .98f));
            GUI.color = new Color(.36f, .62f, .62f, .72f);
            Widgets.DrawBox(panel, 1);
            GUI.color = Color.white;
            Rect intro = new Rect(panel.x + 14f, panel.y + 8f, panel.width - 28f, 43f);
            Widgets.DrawBoxSolid(intro, new Color(.055f, .15f, .16f, .88f));
            Widgets.DrawBoxSolid(new Rect(intro.x, intro.y, 4f, intro.height), Gold);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = new Color(.66f, .83f, .84f);
            Widgets.Label(new Rect(intro.x + 18f, intro.y + 2f, intro.width - 36f, intro.height - 4f), BranchIntroductions[branch]);
            GUI.color = Color.white;

            CacheNodeRects(panel, branch);
            DrawRealmBands(panel, branch);
            DrawPrerequisiteLines(branch);
            DrawBranchNodes(branch);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private void DrawRealmBands(Rect panel, int branch)
        {
            string[] lanes = BranchLaneLabels[branch];
            float contentX = panel.x + 86f;
            float contentWidth = panel.width - 108f;
            float laneStep = contentWidth / lanes.Length;

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = new Color(.54f, .76f, .78f);
            for (int lane = 0; lane < lanes.Length; lane++)
                Widgets.Label(new Rect(contentX + lane * laneStep, panel.y + 56f, laneStep, 22f), lanes[lane]);
            GUI.color = Color.white;

            float rowStart = panel.y + 174f;
            for (int realm = 1; realm <= 5; realm++)
            {
                float y = rowStart + (realm - 1) * 88f;
                Color band = realm % 2 == 0
                    ? new Color(.045f, .125f, .135f, .72f)
                    : new Color(.035f, .095f, .108f, .72f);
                Rect realmBand = new Rect(panel.x + 8f, y - 8f, panel.width - 16f, 68f);
                if (realm == comp.Realm) band = new Color(.07f, .19f, .19f, .90f);
                Widgets.DrawBoxSolid(realmBand, band);
                Widgets.DrawBoxSolid(new Rect(realmBand.x, realmBand.y, realm == comp.Realm ? 5f : 2f, realmBand.height),
                    realm == comp.Realm ? Gold : new Color(.24f, .48f, .48f, .55f));
                GUI.color = realm <= comp.Realm ? Ivory : new Color(.42f, .46f, .47f);
                Rect realmIcon = new Rect(panel.x + 17f, y + 3f, 25f, 25f);
                GUI.DrawTexture(realmIcon, GizmoYunjianziCultivation.RealmTexture(realm), ScaleMode.ScaleToFit, true);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(new Rect(panel.x + 8f, y + 29f, 68f, 24f), CompYunjianziCultivation.RealmName(realm));
                GUI.color = Color.white;
                Widgets.DrawLineHorizontal(contentX, y + 59f, contentWidth);
            }
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void CacheNodeRects(Rect panel, int branch)
        {
            nodeRects.Clear();
            string rootId = BranchRootIds[branch];
            nodeRects[rootId] = new Rect(panel.center.x - 86f, panel.y + 88f, 172f, 52f);

            string[] lanes = BranchLaneLabels[branch];
            float contentX = panel.x + 86f;
            float contentWidth = panel.width - 108f;
            float laneStep = contentWidth / lanes.Length;
            float nodeWidth = Mathf.Min(148f, laneStep - 14f);
            float rowStart = panel.y + 174f;
            List<YunjianziSkillNode> nodes = YunjianziSkills.All
                .Where(n => n.Branch == branch && n.Id != rootId).ToList();
            for (int i = 0; i < nodes.Count; i++)
            {
                YunjianziSkillNode node = nodes[i];
                int lane = Mathf.Clamp(node.Lane, 0, lanes.Length - 1);
                float centerX = contentX + laneStep * (lane + .5f);
                float y = rowStart + (node.Realm - 1) * 88f;
                nodeRects[node.Id] = new Rect(centerX - nodeWidth * .5f, y, nodeWidth, 52f);
            }
        }

        private void DrawPrerequisiteLines(int branch)
        {
            string rootId = BranchRootIds[branch];
            foreach (YunjianziSkillNode node in YunjianziSkills.All.Where(n => n.Branch == branch))
            {
                Rect child;
                if (!nodeRects.TryGetValue(node.Id, out child)) continue;
                for (int i = 0; i < node.Prerequisites.Length; i++)
                {
                    Rect parent;
                    if (!nodeRects.TryGetValue(node.Prerequisites[i], out parent)) continue;
                    bool lit = comp.HasSkill(node.Prerequisites[i]) && (comp.HasSkill(node.Id) || comp.SkillLockReason(node) == null);
                    DrawConnector(parent, child, lit ? LineLearned : LineLocked, node.Prerequisites[i] == rootId);
                }
            }
        }

        private static void DrawConnector(Rect parent, Rect child, Color color, bool fromRoot)
        {
            Vector2 start = new Vector2(parent.center.x, parent.yMax);
            Vector2 end = new Vector2(child.center.x, child.y);
            if (Mathf.Abs(parent.center.y - child.center.y) < 1f)
            {
                Vector2 sideStart = parent.center.x < child.center.x
                    ? new Vector2(parent.xMax, parent.center.y)
                    : new Vector2(parent.x, parent.center.y);
                Vector2 sideEnd = parent.center.x < child.center.x
                    ? new Vector2(child.x, child.center.y)
                    : new Vector2(child.xMax, child.center.y);
                Widgets.DrawLine(sideStart, sideEnd, color, 2f);
                return;
            }
            if (fromRoot)
            {
                float busY = parent.yMax + 16f;
                Widgets.DrawLine(start, new Vector2(start.x, busY), color, 2f);
                Widgets.DrawLine(new Vector2(start.x, busY), new Vector2(end.x, busY), color, 2f);
                Widgets.DrawLine(new Vector2(end.x, busY), end, color, 2f);
            }
            else if (Mathf.Abs(start.x - end.x) < 1f)
            {
                Widgets.DrawLine(start, end, color, 2f);
            }
            else
            {
                // Only genuine cross-specialisation prerequisites use a
                // diagonal; the wide single-technique page keeps these rare.
                Widgets.DrawLine(start, end, color, 2f);
            }
        }

        private void DrawBranchNodes(int branch)
        {
            foreach (YunjianziSkillNode node in YunjianziSkills.All.Where(n => n.Branch == branch))
            {
                Rect rect = nodeRects[node.Id];
                bool learned = comp.HasSkill(node.Id);
                string reason = comp.SkillLockReason(node);
                bool debug = Prefs.DevMode && DebugSettings.godMode;
                bool available = reason == null || (debug && node.Id != YunjianziSkills.SummonZhuxie);
                Color nodeColor = learned ? Learned : available ? Available : Locked;
                Widgets.DrawBoxSolid(rect.ExpandedBy(2f), new Color(0f, 0f, 0f, .38f));
                Widgets.DrawBoxSolid(rect, nodeColor);
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 5f), learned ? LineLearned : available ? Gold : LineLocked);
                GUI.color = learned ? new Color(.64f, .92f, .88f) : available ? new Color(.89f, .78f, .46f) : new Color(.28f, .35f, .35f);
                Widgets.DrawBox(rect, learned || available ? 2 : 1);
                if (Mouse.IsOver(rect))
                {
                    GUI.color = learned ? LineLearned : Gold;
                    Widgets.DrawBox(rect.ExpandedBy(3f), 1);
                }
                GUI.color = Color.white;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = learned || available ? Color.white : new Color(.62f, .65f, .66f);
                Widgets.Label(new Rect(rect.x + 5f, rect.y + 8f, rect.width - 10f, 25f), node.Label);
                int cost = comp.SkillCost(node);
                GUI.color = learned ? new Color(.70f, 1f, .94f) : available ? new Color(1f, .90f, .56f) : new Color(.50f, .56f, .57f);
                string state = learned ? "◆ 已悟" : available ? "◇ " + cost + " 灵" : "· 封存";
                Widgets.Label(new Rect(rect.x + 4f, rect.y + 33f, rect.width - 8f, 16f), state);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                string tip = node.Label + "（" + CompYunjianziCultivation.RealmName(node.Realm) + "）\n" + node.Description +
                    "\n\n本境界已学习：" + comp.LearnedSkillCountAtRealm(node.Realm) + "项\n本次消耗：" + cost + "当前灵力" +
                    "\n费用序列：第1～6项为0%、10%、20%、40%、70%、100%；第7项起每项固定为该境界标准灵力上限的200%。";
                if (node.Branch == 3) tip = node.Label + "（" + CompYunjianziCultivation.RealmName(node.Realm) + "）\n" + node.Description + "\n\n参悟消耗：1点灵气；研究进度：2000。";
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
