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
        public const string ShenjiWyvernAdaptiveShield = "shenji.wyvern.adaptive";
        public const string ShenjiWyvernBreath = "shenji.wyvern.breath";
        public const string ShenjiWyvernAscension = "shenji.wyvern.ascension";
        public const string ShenjiWyvernArsenal = "shenji.wyvern.arsenal";
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
            N(SummonZhuxie, "唤起诛邪", "研读宝卷后先天掌握。消耗10灵力召回世界中唯一的诛邪。", 1, 0, 3),
            N(ArrayCore, "琉璃阵眼", "解锁仙阵诀：布置持续60秒的固定琉璃阵眼。", 1, 1, 0),
            N(ArrayRecovery, "阵中回元", "阵内友方缓慢恢复非永久伤势。", 1, 1, 0, ArrayCore),
            N(ArrayTeleport, "阵遁", "解锁紫电阵遁，可从阵外或阵内传送至阵法覆盖范围。", 1, 1, 3, ArrayCore),

            N(SwordEnchantment, "诛邪附灵", "诛邪命中短暂眩晕生物、麻痹机械；目标随后获得控制免疫，不能形成永控。", 2, 0, 0, SummonZhuxie),
            N(SwordWard, "御剑护体", "解锁诛邪的灵力格挡条，脱离伤害后缓慢恢复。", 2, 0, 1, SummonZhuxie),
            N(ArrayLightning, "引雷入阵", "仙阵每10秒落下两道雷霆，尽量锁定不同敌人。", 2, 1, 1, ArrayCore),
            N(ArrayEdgeSlow, "迟滞阵纹", "敌人触碰固定阵或随身阵边界时减速10秒；同一敌人短时间内不会再次触发。", 2, 1, 2, ArrayCore),

            N(SwordShadowsI, "剑影·双仪", "解锁两把剑影，在敌阵间持续穿梭攻击。", 3, 0, 2, SummonZhuxie),
            N(BlinkStrike, "瞬斩", "消耗20灵力闪现至敌人身后强力斩击，冷却5秒；可右键开启自动施放。", 3, 0, 3, SummonZhuxie),
            N(SwordRain, "天剑雨", "消耗80灵力，在迫击炮爆炸范围内降下两轮纯伤害剑雨。", 3, 0, 4, SummonZhuxie),
            N(LightningEnchantment, "雷纹附魔", "诛邪命中与瞬斩命中附加落雷。需要先掌握诛邪附灵。", 3, 0, 0, SwordEnchantment),
            N(MovingArray, "随身阵", "布阵时额外生成一个丝滑跟随云间子的阵法领域。", 3, 1, 4, ArrayCore),
            N(ArrayTeleportShock, "震雷遁", "阵遁落点一格范围内产生击退；境界越高，范围与距离越大。", 3, 1, 3, ArrayTeleport),

            N(SwordShadowsII, "剑影·四象", "剑影数量由两把提升至四把。", 4, 0, 2, SwordShadowsI),
            N(BlinkChain, "诛邪连斩", "瞬斩击杀目标（包括附带落雷）后，自动寻找下一目标继续释放。", 4, 0, 3, BlinkStrike),
            N(SwordHealing, "剑息疗伤", "诛邪命中会修复自身伤口并降低流血。", 4, 0, 0, LightningEnchantment),
            N(DeathLock, "诛邪护主", "手持诛邪濒危时锁血20秒；具有较长的再次触发间隔。", 4, 0, 5, SummonZhuxie),
            N(SwordRainGolden, "天剑雨·藏锋", "天剑雨冷却缩短5秒，可储存两次充能，并扩大覆盖范围。", 4, 0, 4, SwordRain),
            N(AllyArrayShield, "众生灵盾", "阵内友方与友方机械获得弱版灵力护盾。", 4, 1, 5, ArrayCore),
            N(ArrayBarrier, "金丹界壁", "仙阵改为每秒消耗5灵力持续维持，并阻挡阵外射入的抛射物。", 4, 1, 6, AllyArrayShield),
            N(ArrayEdgeStun, "镇魂阵纹", "迟滞阵纹强化：触碰阵法边界的敌人先被眩晕3秒，随后继续承受10秒减速。", 4, 1, 2, ArrayEdgeSlow),

            N(SwordShadowsIII, "剑影·八荒", "剑影数量由四把提升至八把。", 5, 0, 2, SwordShadowsII),
            N(ArmorIgnore, "破妄剑意", "诛邪、瞬斩与剑影攻击无视护甲。", 5, 0, 6, SummonZhuxie),
            N(ShadowAffixes, "万剑同契", "剑影继承已学习的眩晕/麻痹与落雷附魔；每把剑影的落雷独立使用五倍冷却。", 5, 0, 1, SwordShadowsI, SwordEnchantment),
            N(SwordRainSpirit, "天剑雨·化神", "天剑雨冷却再次缩短5秒，并在原有两轮后追加两轮攻击。", 5, 0, 4, SwordRainGolden),
            N(ArrayReflect, "返照界壁", "界壁不再仅仅阻挡，而会把敌方抛射物反射回去。", 5, 1, 6, ArrayBarrier),
            N(ArrayStorm, "万钧雷域", "仙阵落雷频率提升至5秒，并强化雷击。", 5, 1, 1, ArrayLightning),
            N(ArrayTeleportMastery, "化神阵遁", "阵遁的击退范围与距离提升至化神层次。", 5, 1, 3, ArrayTeleportShock),

            N(ShenjiSummon, "唤神机", "消耗10灵力召唤一台不占机械带宽、与云间子绑定的专属火力支援米莉安。", 1, 2, 4),
            N(ShenjiGatling, "神机炮·连星", "神机炮强化到原版速射机枪级火力。", 2, 2, 0, ShenjiSummon),
            N(ShenjiRocketJump, "火箭跃迁", "神机获得冷却一天的火箭跳跃；自动战斗会在近敌压迫时后撤跃迁。", 2, 2, 2, ShenjiSummon),
            N(ShenjiShield, "百灵护盾", "神机获得100点可恢复护盾。", 2, 2, 3, ShenjiSummon),
            N(ShenjiDualControl, "双机同调", "云间子可同时维持两台神机。", 3, 2, 7, ShenjiSummon),
            N(ShenjiGauss, "神机炮·辉光", "神机炮改用淡蓝色脉冲激光，火力达到米莉拉帝国高阶步枪级别。", 3, 2, 0, ShenjiGatling),
            N(ShenjiAccessories, "万机接口", "开放米莉拉通用配件接口；不接入民主之翼专属配件。", 3, 2, 5, ShenjiSummon),
            N(ShenjiRecallRepair, "败阵归元", "神机被击溃时传送回云间子身边并完全修复，冷却两天。", 4, 2, 6, ShenjiSummon),
            N(ShenjiDarkShield, "暗质护盾", "护盾升级为米莉拉帝国暗物质护盾腰带级防护。", 4, 2, 3, ShenjiShield),
            N(ShenjiLegendaryFirepower, "神机炮·传说", "神机炮达到传奇高斯步枪级火力，武器能量永不枯竭。", 4, 2, 0, ShenjiGauss),
            N(ShenjiRicochetLaser, "流光折跃", "神机激光命中后向目标附近至多三个敌人连续跳射；每次跳射造成主射击30%的伤害。", 4, 2, 1, ShenjiGauss),
            N(ShenjiQuadControl, "四象神机", "云间子可同时维持四台终极神机。", 5, 2, 7, ShenjiDualControl),
            N(ShenjiWyvern, "云间机飞龙", "唤醒唯一的机械天兽。紫府形态可在战斗中起飞，以三道青雷爪痕横扫近敌，并为友军提供加速与范围护盾；平时仅负责搬运。", 3, 2, 8, ShenjiSummon),
            N(ShenjiWyvernAdaptiveShield, "金丹龙躯", "飞龙体型与装甲进化，移动速度提升到普通小人水平，并获得会针对近期伤害逐步适应的厚重护盾。", 4, 2, 8, ShenjiWyvern),
            N(ShenjiWyvernBreath, "雷霆龙息", "飞龙获得十五格雷电龙息：每日恢复一发、最多储存两发，以迫击炮级范围造成高额雷电伤害、EMP与麻痹。", 4, 2, 9, ShenjiWyvern),
            N(ShenjiWyvernAscension, "化神天兽", "飞龙完成终极重构：巨躯、五道电爪、近身碾压场与五十万级结构耐久完全展开，移动速度提升至普通小人的130%。", 5, 2, 8, ShenjiWyvernAdaptiveShield),
            N(ShenjiWyvernArsenal, "万雷神枢", "龙息改为半日恢复一发、最多五发；背部神枢自动向敌阵发射激光火箭，并强化所有雷电、EMP与麻痹效果。", 5, 2, 9, ShenjiWyvernBreath, ShenjiWyvernAscension),

            N(YangfuPrimer, "阳符入门", "研墨裁纸，以灵力落笔。参悟后开启画符台与阳符传承。", 1, 3, 2),
            N(FireTornadoI, "火龙卷·燃符", "向十五格内凝出赤符，符破后生成受控火龙卷，持续游戏内半小时。", 1, 3, 6, YangfuPrimer),
            N(ThunderWindI, "天雷神风·引风", "向十五格内掷出紫符，聚拢十格内敌人十秒，随后降下一道强化天雷。", 1, 3, 7, YangfuPrimer),
            N(BeanSoldiersI, "撒豆成兵·二灵", "向十五格内掷出青符，在目标周围唤出两台追敌自爆的符兵。", 1, 3, 8, YangfuPrimer),
            N(TalismanZhuoyu, "啄玉符", "大幅提高制作物品的品阶。参悟后可在画符台绘制啄玉符。", 2, 3, 0, YangfuPrimer),
            N(TalismanShenjie, "神截符", "截天地神妙秘藏于自身，大幅提升科研速度。", 2, 3, 1, YangfuPrimer),
            N(TalismanShenxing, "神行符", "引轻灵之气入体，大幅提升移动速度。", 2, 3, 2, YangfuPrimer),
            N(TalismanJiqing, "寄情符", "寄情山水草木；环境越华丽，魅力与心境增益越高。", 2, 3, 3, YangfuPrimer),
            N(TalismanBaicao, "百草符", "通晓百草，大幅提升医术并避免灾难性的治疗失败。", 2, 3, 4, YangfuPrimer),
            N(TalismanXinjian, "心剑符", "心剑洞彻人心，提高贸易优势。", 2, 3, 5, YangfuPrimer),
            N(FireTornadoII, "火龙卷·筑基", "施法距离增至二十格，火龙卷更持久、更炽烈。", 2, 3, 6, FireTornadoI),
            N(ThunderWindII, "天雷神风·筑基", "聚拢持续十五秒，最终连续降下两道强化天雷。", 2, 3, 7, ThunderWindI),
            N(BeanSoldiersII, "撒豆成兵·五灵", "符兵增至五台，耐久与自爆威力同步提升。", 2, 3, 8, BeanSoldiersI),
            N(TalismanBeidou, "北斗符", "北斗星辉辟易外邪，提高攻速并缩短武器特效冷却。", 3, 3, 2, TalismanShenxing),
            N(TalismanGuihai, "归海符", "真元如海，万脉归流，大幅提升灵力恢复速度。", 3, 3, 1, TalismanShenjie),
            N(TalismanShenlian, "神炼符", "神念锻器，大幅提升制作工作的效率。", 3, 3, 0, TalismanZhuoyu),
            N(TalismanHuijian, "惠剑斩念符", "惠剑斩妄念，不再受到心灵低语影响。", 3, 3, 3, TalismanJiqing),
            N(FireTornadoIII, "火龙卷·紫府", "施法距离增至二十五格，龙卷持续约一个半游戏时辰。", 3, 3, 6, FireTornadoII),
            N(ThunderWindIII, "天雷神风·紫府", "聚拢持续二十秒，最终降下三道强化天雷。", 3, 3, 7, ThunderWindII),
            N(BeanSoldiersIII, "撒豆成兵·八灵", "符兵增至八台，拥有四百五十点耐久。", 3, 3, 8, BeanSoldiersII),
            N(TalismanBumei, "不昧真灵符", "凝聚万世记忆，抵达不昧心境，心境永不退转。", 4, 3, 3, TalismanHuijian),
            N(TalismanJiexin, "劫心符", "消耗本源心血淬炼肉身，全面提升身体属性。", 4, 3, 4, TalismanBaicao),
            N(TalismanLiuyang, "六阳真形符", "战斗技能臻于极限，却失去艺术感知并使心境浮躁。", 4, 3, 2, TalismanBeidou),
            N(FireTornadoIV, "火龙卷·金丹", "施法距离增至三十格，火龙卷进一步扩大持续与伤害。", 4, 3, 6, FireTornadoIII),
            N(ThunderWindIV, "天雷神风·金丹", "聚拢持续二十五秒，最终降下四道强化天雷。", 4, 3, 7, ThunderWindIII),
            N(BeanSoldiersIV, "撒豆成兵·十一灵", "符兵增至十一台，拥有七百点耐久。", 4, 3, 8, BeanSoldiersIII),
            N(TalismanTiandao, "天道符", "既度天劫，洞彻天道：所有未被背景禁用的技能视作二十级。", 5, 3, 0, TalismanShenlian, TalismanBumei),
            N(TalismanHuanri, "换日符", "偷天而后换日，使生理寿元不再流逝。", 5, 3, 4, TalismanJiexin)
            ,N(FireTornadoV, "火龙卷·化神", "施法距离增至三十五格，火龙卷持续两个游戏时辰并达到最高威力。", 5, 3, 6, FireTornadoIV)
            ,N(ThunderWindV, "天雷神风·化神", "聚拢持续三十秒，最终降下五道最高威力的强化天雷。", 5, 3, 7, ThunderWindIV)
            ,N(BeanSoldiersV, "撒豆成兵·十五灵", "符兵增至十五台，拥有一千点耐久与三倍自爆威力。", 5, 3, 8, BeanSoldiersIV)
        };

        private static YunjianziSkillNode N(string id, string label, string description, int realm, int branch, int lane, params string[] prerequisites)
        {
            return new YunjianziSkillNode(id, label, description, realm, branch, lane, prerequisites);
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
            // Each realm has its own purchase counter: first pick is free, then
            // 10%, 20%, 30%... of that realm's energy cap, capped at 200%.
            float fraction = Mathf.Min(bought * .10f, 2f);
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
        private static readonly Color Panel = new Color(.025f, .075f, .09f, .99f);
        private static readonly Color JadeDark = new Color(.055f, .16f, .17f, .98f);
        private static readonly Color JadeLine = new Color(.50f, .78f, .76f, .82f);
        private static readonly Color Ivory = new Color(.88f, .91f, .82f, 1f);
        private static readonly Color Learned = new Color(.12f, .49f, .50f, .98f);
        private static readonly Color Available = new Color(.57f, .45f, .20f, .98f);
        private static readonly Color Locked = new Color(.12f, .18f, .19f, .98f);
        private static readonly Color LineLearned = new Color(.30f, .90f, .94f, .92f);
        private static readonly Color LineLocked = new Color(.34f, .39f, .40f, .70f);
        private readonly CompYunjianziCultivation comp;
        private readonly Dictionary<string, Rect> nodeRects = new Dictionary<string, Rect>();
        private int selectedBranch;

        private static readonly string[] BranchTitles = { "御剑诀", "仙阵诀", "神机诀", "阳符诀" };
        private static readonly string[] BranchIntroductions =
        {
            "白刃至今，御剑之术由杀伐之道而生。山门以灵御剑，诛邪无数，又以剑正道之谬。以剑载敌、遁行为小乘；凭心御剑，方是大道。",
            "阵法之道，玄妙非常。入道之人可凭一器演化，瞒天拘地；亦可引动天地雷霆，困敌于阵中，或融入心神，御敌无形。一人起阵，亦自成方圆。",
            "器者载道，机者通神。神机诀取米莉拉灵械之学，合阵枢、灵纹与修行之法，以心御机，以机行道。灵念所至，百工与兵锋皆如臂使指。",
            "古有五经论道，观天地广阔、五行变换，皆可采其意而书于符中。符可护身益生，亦可蕴火、雷、风、兵之术；一纸虽轻，落笔即成法。"
        };
        private static readonly string[] BranchRootIds =
        {
            YunjianziSkills.SummonZhuxie,
            YunjianziSkills.ArrayCore,
            YunjianziSkills.ShenjiSummon,
            YunjianziSkills.YangfuPrimer
        };
        private static readonly string[][] BranchLaneLabels =
        {
            new[] { "附灵", "护体·同契", "剑影", "瞬斩", "天剑雨", "护主", "破甲" },
            new[] { "回元", "雷域", "阵纹", "阵遁", "随身阵", "灵盾", "界壁" },
            new[] { "主火力", "折射", "机动", "护盾", "预留", "接口", "败阵归元", "控制", "天兽龙躯", "雷霆龙息" },
            new[] { "器道", "悟道", "身法", "心境", "医身", "心剑", "火龙卷", "天雷神风", "撒豆成兵" }
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
            GUI.color = JadeLine;
            Widgets.DrawBox(inRect.ContractedBy(2f), 2);
            GUI.color = Color.white;

            Rect header = new Rect(10f, 9f, inRect.width - 20f, 50f);
            Widgets.DrawBoxSolid(header, JadeDark);
            GUI.color = new Color(.63f, .86f, .82f, .42f);
            Widgets.DrawLineHorizontal(header.x + 4f, header.yMax - 1f, header.width - 8f);
            GUI.color = Color.white;
            Widgets.DrawTextureFitted(new Rect(20f, 16f, 34f, 34f), YunjianziAbilityTextures.CultivationTree, 1f);
            Text.Font = GameFont.Medium;
            GUI.color = Ivory;
            Widgets.Label(new Rect(66f, 15f, 430f, 34f), "云间宝卷 · 修为参悟");
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect realmBadge = new Rect(506f, 17f, 116f, 30f);
            Widgets.DrawBoxSolid(realmBadge, new Color(.12f, .30f, .30f, .95f));
            GUI.color = Ivory;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(realmBadge, comp.RealmLabel);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Rect energyBack = new Rect(642f, 23f, 330f, 18f);
            Widgets.DrawBoxSolid(energyBack, new Color(.015f, .035f, .045f, 1f));
            float energyPct = comp.Energy / Mathf.Max(1f, comp.EnergyMax);
            Widgets.DrawBoxSolid(new Rect(energyBack.x + 2f, energyBack.y + 2f, (energyBack.width - 4f) * energyPct, energyBack.height - 4f),
                new Color(.24f, .72f, .72f, 1f));
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(energyBack, string.Format("灵力 {0:0}/{1:0}", comp.Energy, comp.EnergyMax));
            Text.Anchor = TextAnchor.UpperLeft;
            if (Prefs.DevMode && DebugSettings.godMode)
            {
                GUI.color = new Color(1f, .72f, .30f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(990f, 22f, 390f, 20f), "GOD MODE：左键免费解锁；右键移除节点");
                GUI.color = Color.white;
            }

            DrawTechniqueTabs(new Rect(18f, 66f, inRect.width - 36f, 38f));
            DrawTechniquePanel(new Rect(18f, 112f, inRect.width - 36f, 620f), selectedBranch);

            Rect future = new Rect(18f, 744f, inRect.width - 36f, 24f);
            GUI.color = new Color(.62f, .72f, .74f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(future, "纵向专精轨道 · 青色为已掌握，金色为可参悟");
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
                GUI.color = selected ? Ivory : new Color(.65f, .73f, .73f);
                Widgets.Label(tab, BranchTitles[branch]);
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
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = new Color(.66f, .83f, .84f);
            Widgets.Label(new Rect(panel.x + 34f, panel.y + 5f, panel.width - 68f, 48f), BranchIntroductions[branch]);
            GUI.color = Color.white;

            CacheNodeRects(panel, branch);
            DrawRealmBands(panel, branch);
            DrawPrerequisiteLines(branch);
            DrawBranchNodes(branch);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private static void DrawYangfuConcept(Rect panel)
        {
            float top = panel.y + 72f;
            float gap = 10f;
            float cardWidth = (panel.width - 34f - gap * 3f) / 4f;
            float cardHeight = 398f;
            DrawConceptCard(new Rect(panel.x + 17f, top, cardWidth, cardHeight), "筑基 · 一符位",
                "啄玉符：大幅提高成品品阶\n\n神截符：大幅提高科研速度\n\n神行符：大幅提高移动速度\n\n寄情符：美景转化为魅力与心境\n\n百草符：精通医术，避免灾难性失败\n\n心剑符：洞彻人心，提高贸易优势",
                new Color(.15f, .35f, .31f, .94f));
            DrawConceptCard(new Rect(panel.x + 17f + cardWidth + gap, top, cardWidth, cardHeight), "紫府 · 二符位",
                "北斗符：攻速提升，缩短武器特效冷却\n\n归海符：大幅提高灵力恢复速度\n\n神炼符：大幅提高制作工作效率\n\n惠剑斩念符：免疫负面心灵低语与精神扰动",
                new Color(.22f, .25f, .46f, .94f));
            DrawConceptCard(new Rect(panel.x + 17f + (cardWidth + gap) * 2f, top, cardWidth, cardHeight), "金丹 · 三符位",
                "不昧真灵符：心境圆满，法门施放加快\n\n劫心符：消耗本源，全面淬炼肉身\n\n六阳真形符：战斗技能20级；艺术归零，并大幅降低心境",
                new Color(.47f, .33f, .12f, .94f));
            DrawConceptCard(new Rect(panel.x + 17f + (cardWidth + gap) * 3f, top, cardWidth, cardHeight), "化神 · 四符位",
                "天道符：所有未被背景禁止的技能按20级结算\n\n换日符：生理年龄停止增长\n\n符位只限制同时生效数量；已学会和储存的符咒不受限制。",
                new Color(.40f, .18f, .42f, .94f));

            Rect combat = new Rect(panel.x + 17f, top + cardHeight + 14f, panel.width - 34f, 92f);
            DrawConceptCard(combat, "战斗术符 · 独立直线进阶",
                "火龙卷：移动焚烧与区域驱赶　　天雷神风：聚拢敌军后雷击　　撒豆成兵：召唤短命符兵冲锋自爆。右键选择术式，左键施放；三条路线分别进阶。",
                new Color(.38f, .22f, .15f, .94f));
        }

        private static void DrawConceptCard(Rect rect, string title, string body, Color fill)
        {
            Widgets.DrawBoxSolid(rect, fill);
            Widgets.DrawBox(rect, 1);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = new Color(.88f, .95f, .91f);
            Widgets.Label(new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, 28f), title);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(.88f, .90f, .88f);
            Widgets.Label(new Rect(rect.x + 14f, rect.y + 48f, rect.width - 28f, rect.height - 60f), body);
            GUI.color = Color.white;
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
                Widgets.DrawBoxSolid(new Rect(panel.x + 8f, y - 8f, panel.width - 16f, 68f), band);
                GUI.color = realm <= comp.Realm ? Ivory : new Color(.42f, .46f, .47f);
                Widgets.Label(new Rect(panel.x + 14f, y + 10f, 62f, 32f), CompYunjianziCultivation.RealmName(realm));
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
                Widgets.DrawBoxSolid(rect, learned ? Learned : available ? Available : Locked);
                GUI.color = learned ? new Color(.64f, .92f, .88f) : available ? new Color(.89f, .78f, .46f) : new Color(.28f, .35f, .35f);
                Widgets.DrawBox(rect, learned || available ? 2 : 1);
                GUI.color = Color.white;
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
