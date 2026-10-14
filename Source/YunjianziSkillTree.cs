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
            N(SummonZhuxie, "诛邪剑契", "道书封底的剑图已经苏醒。以灵力呼唤，世间唯一的诛邪便会回到传人手中。", 1, 0, 3),
            N(ArrayCore, "琉璃定枢", "从《剑阵篇》中悟出最初阵式，以琉璃灵珠镇住中央，在周围展开一座短暂仙阵。", 1, 1, 0),
            N(ArrayRecovery, "灵机养形", "阵势流转时分出温和生机，使阵中同伴的寻常伤势缓慢愈合。", 1, 1, 0, ArrayCore),
            N(ArrayTeleport, "循纹神行", "消耗100点灵力，借紫电循阵纹而行，遁入仙阵覆盖之处。", 1, 1, 3, ArrayCore),

            N(SwordEnchantment, "雷炁洗锋", "剑意随锋而入，使生灵失神、机械失灵。受过一次镇压者会暂时适应，因而不会被剑意无休止地困住。", 2, 0, 0, SummonZhuxie),
            N(SwordWard, "剑罡卫形", "精炼随境界自然壮大的护体剑罡，使其上限额外提高50%，并将脱离攻势后的重聚速度提升至三倍。", 2, 0, 1, SummonZhuxie),
            N(ArrayLightning, "雷符镇宫", "将雷意接入阵纹，使仙阵周期性追索阵中之敌，落点附带同范围EMP脉冲；每道雷消耗10点灵力，数量、威力与范围随境界增长。", 2, 1, 1, ArrayCore),
            N(ArrayEdgeSlow, "缚形界纹", "敌人触碰阵界时会遭强雷轰击并被阵纹牵住；同一道边界无法接连困住已经适应的目标。", 2, 1, 2, ArrayCore),

            N(SwordShadowsI, "双仪分光", "一念分作双仪剑影，自身前蓄势射出，贯穿敌阵后回归。", 3, 0, 2, SummonZhuxie),
            N(BlinkChain, "神行逐寇", "缩地一斩诛敌之后，余势会自行寻找下一个目标，直至剑势已尽。", 3, 0, 3, BlinkStrike),
            N(SwordRainGolden, "九霄剑阵", "将剑势藏入紫府，使天剑雨可储存两次跃击，在境界成长之外额外增加1.5格连斩半径，斩击宽度随之扩大。", 3, 0, 4, SwordRain),
            N(BlinkStrike, "缩地御剑", "身随剑意闪至敌后，挥出蓄势一斩；也可让剑心自行判断出手时机。", 2, 0, 3, SummonZhuxie),
            N(SwordRain, "天剑垂象", "以剑罡护体腾空蓄势，霸体跃向敌阵，落地后分出淡蓝虚影穿梭连斩；每升一境，穿梭距离与伤害范围随之扩大。", 2, 0, 4, SummonZhuxie),
            N(CloudSwordArray, "云海剑匣", "以紫府神识统御六柄常随灵剑，解锁高级法器“云海剑轮”的炼制。固定消耗100点灵力，不计入本境界技能数量。", 3, 0, 6, SwordWard),
            N(MovingArray, "身载周天", "阵眼隐入周天，仙阵不再留驻原地，唯一阵域随云间子行止而移动。", 3, 1, 4, ArrayCore),
            N(ArrayTeleportShock, "紫电惊雷", "阵遁落定之际迸发震雷，以EMP扰乱近敌并将其推开；修为越深，雷势越盛。", 3, 1, 3, ArrayTeleport),

            N(SwordShadowsII, "四象巡天", "双仪再分四象，更多剑影自周身射出，贯敌后回收再发。", 4, 0, 2, SwordShadowsI),
            N(LightningEnchantment, "神霄雷篆", "把引雷阵纹刻入诛邪，使剑击与缩地御剑能够招来一道追随剑锋的天雷，在落点半径2.5格内造成神霄雷炁范围伤害与EMP脉冲；剑影承载雷纹时仍保留各自的引雷间隔。", 4, 0, 0, SwordEnchantment),
            N(SwordHealing, "噬血奉主", "诛邪饮下敌势，再以温和剑息反哺主人，收拢伤口与流血。", 4, 0, 1, LightningEnchantment),
            N(DeathLock, "抱一守命", "主人命悬一线时，诛邪会强行镇住生机；此等护主之举不能频繁施展。", 4, 0, 5, SummonZhuxie),
            N(AllyArrayShield, "众生同庇", "仙阵为阵中同伴与友方机械覆上灵盾；雷霆命中友方所在区域时还会补充一层护盾。", 4, 1, 5, ArrayCore),
            N(ArrayBarrier, "天垣绝矢", "以持续灵力维系阵界，使阵外来袭的抛射物止步；每次成功拦截固定消耗10点灵力。", 4, 1, 6, AllyArrayShield),
            N(ArrayEdgeStun, "镇魂界纹", "缚形界纹再添镇魂之力，越界之敌会先失神片刻，而后步履沉重。", 4, 1, 2, ArrayEdgeSlow),
            N(ArrayStorm, "万钧天狱", "凝聚不超过两秒的紫电雷枪并直线掷出，沿途半径3格贯穿、落点半径7.5格爆发，两段均可伤及敌方单位与建筑，并各自附带EMP，每段基础伤害金丹1200、化神1500，穿甲1000%。随后斜插为雷引，持续二十秒召来天雷轰击。主动施展消耗500点灵力，冷却10秒；同时缩短阵中被动落雷间隔。", 4, 1, 1, ArrayLightning),

            N(SwordShadowsIII, "太虚万剑", "四象铺陈八荒，剑影自周身接连射出，如群星贯穿敌阵。", 5, 0, 2, SwordShadowsII),
            N(ChainLightningEnchantment, "万劫雷锋", "雷纹化作游走不息的细小电芒。每次剑击都会额外放出无须蓄势的闪电链，在邻近敌人间跳跃，每次命中附带EMP；此雷由剑锋自生，不与从天而降的雷霆同息。", 5, 0, 0, LightningEnchantment),
            N(ArmorIgnore, "照见真形", "剑意越过甲胄表象，令诛邪、缩地御剑与剑影直指其内。", 5, 0, 6, SummonZhuxie),
            N(ShadowAffixes, "诸天剑契", "诸剑影与诛邪同契，能够承载已经参悟的镇压与雷纹；分化之剑引雷稍缓，仍各有天威。", 5, 0, 1, SwordShadowsI, SwordEnchantment),
            N(SwordRainSpirit, "太虚剑劫", "以太虚剑意驱动虚影，天剑雨穿梭连斩的伤害翻倍，充能也更加迅速。", 5, 0, 4, SwordRainGolden),
            N(ArrayReflect, "诸法返照", "界壁不再一味承受来袭之物，而会循其来路将敌方抛射物返照回去。", 5, 1, 6, ArrayBarrier),
            N(ArrayTeleportMastery, "一念周天", "身与阵纹浑然相合，阵遁落定时迸发的震雷达到化神层次。", 5, 1, 3, ArrayTeleportShock),
            N(JiuxiaoXianxu, "九霄织雷", "以元神将天雷织入仙絮，解锁化神法器“九霄仙絮”的炼制；佩用后可令仙阵雷威与一切神霄雷炁大幅增长。自身引发的每一道天雷落下时，不论是否命中目标，都会为护体剑罡充能。固定消耗100点灵力，不计入本境界技能数量。", 5, 1, 0, ArrayStorm),

            N(ShenjiSummon, "灵机启明", "解锁主动凝成青羽神机核心，并解锁神机装备制造与米莉安培育器中的青羽机侍配方；全世界神机与三类核心总量低于一时才可凝成。", 1, 2, 4),
            N(ShenjiGatling, "连星机括", "青芒灵铳展开多重铳管，机括循环如群星循轨，以密集离子弹流压制成群之敌。", 2, 2, 0, ShenjiSummon),
            N(ShenjiRocketJump, "流火神行", "神机获得长距离跃迁能力；自主作战时若被敌人逼近，她也会借此迅速脱身。", 2, 2, 2, ShenjiSummon),
            N(ShenjiShield, "玉光结盾", "百道细小灵路合为一面偏射护盾，容量600；普通、远程与爆炸命中固定消耗50点，明确近战命中消耗300点。尚有能量时完整挡住耗尽护盾的最后一击；遭受EMP时会直接破碎。", 2, 2, 3, ShenjiSummon),
            N(ShenjiDualControl, "神分四象", "神识分镇四方而不乱，将神机与三类核心的全世界总量上限提升至四，并可同时维系四位神机米莉安；低于额度时可主动凝成青羽核心。", 3, 2, 7, ShenjiSummon),
            N(ShenjiGauss, "日轮炼光", "神机武装舍弃实体弹丸，以日棱晶约束淡蓝脉冲辉光，迅疾洞穿远处之敌。", 3, 2, 0, ShenjiGatling),
            N(ShenjiAccessories, "诸器同参", "参悟道书中的通机之理，使神机能够使用米莉拉通用配件；并解锁可供九霄天将安装的灵术演算芯片。", 4, 2, 5, ShenjiSummon),
            N(ShenjiRecallRepair, "败形归炉", "神机将被彻底击溃时，道书印记会把她召回云间子身旁并重整形体；此法需要很久才能再次积蓄。", 4, 2, 6, ShenjiSummon),
            N(ShenjiDarkShield, "太阴天幕", "以暗物质约束技术重炼玉光之盾，容量提升至2000点；普通、远程与爆炸命中固定消耗10点、明确近战命中消耗50点。尚有能量时完整挡住最后一击，并可无消耗抵挡EMP。", 4, 2, 3, ShenjiShield),
            N(ShenjiLegendaryFirepower, "天工曜世", "金丹为炉，神机为器。最终器形完全展开，以永不枯竭的辉光维持传奇般的火力。", 4, 2, 0, ShenjiGauss),
            N(ShenjiRicochetLaser, "流光千折", "辉光命中后借附近敌人的能量轮廓连续折跃；每次折射都会衰减，却足以扰乱整片敌阵。", 4, 2, 1, ShenjiGauss),
            N(ShenjiQuadControl, "念镇九宫", "元神遍照九宫，将神机与三类核心的全世界总量上限提升至九，并可同时维系九位神机米莉安；低于额度时可主动凝成青羽核心。", 5, 2, 7, ShenjiDualControl),
            N(ShenjiWyvern, "旧契重明", "读懂道书末页的云纹蹄印，呼唤曾与上一代云间子并肩远行的云麒麟。她并非受人驱使的坐骑，只有认可新传人后才会续上旧约；平日协助搬运，战时则自行迎敌，并以瑞气和灵盾护佑友军。她的形态随云间子的境界成长，不必另行领悟进阶技能；突破后再次唤起，即显现相应境界的真形。各形态能力详见云麒麟的介绍。", 3, 2, 8, ShenjiSummon),

            N(YangfuPrimer, "灵墨书符", "读懂《阳符篇》卷首：研墨裁纸，以灵力落笔。由此可修建画符台，并继续参悟书中的符图。", 1, 3, 2),
            N(FireTornadoI, "燃符生焰", "赤符燃尽，符中火意化作一股受控龙卷，在指定之处持续追逐焚烧。", 1, 3, 6, YangfuPrimer),
            N(ThunderWindI, "巽符引风", "紫符先唤神风收拢敌群，待风眼成形，再引一道天雷落下。", 1, 3, 7, YangfuPrimer),
            N(BeanSoldiersI, "点豆生灵", "青符落地，唤出少量机巧符兵。它们会自行追敌，并以最后一息引爆符胆。", 1, 3, 8, YangfuPrimer),
            N(TalismanZhuoyu, "啄玉符", "参悟良工知止之意，使制成器物更容易抵达上乘品阶，并可在画符台绘成实体阳符。", 2, 3, 0, YangfuPrimer),
            N(TalismanShenjie, "截真符", "从万千表象中截取一线真意，大幅加快对未知事物的研究。", 2, 3, 1, YangfuPrimer),
            N(TalismanShenxing, "神行符", "引轻灵之气行于足下，使脚步如云影越岭。", 2, 3, 2, YangfuPrimer),
            N(TalismanJiqing, "寄情符", "把山水草木收进心境；四周越清雅华美，言谈与心神便越从容。", 2, 3, 3, YangfuPrimer),
            N(TalismanBaicao, "百草符", "使百草医理映照病机，大幅精进医疗与手术，并避开最凶险的失误。", 2, 3, 4, YangfuPrimer),
            N(TalismanXinjian, "心剑符", "心剑斩去言语虚声，使交易中的迟疑、渴求与底线清晰可辨。", 2, 3, 5, YangfuPrimer),
            N(FireTornadoII, "赤龙卷地", "筑基真炁令火龙卷行得更远，也燃得更久、更烈。", 2, 3, 6, FireTornadoI),
            N(ThunderWindII, "风眼聚形", "神风能够牵制敌人更久，并在风眼成形后接连引下天雷。", 2, 3, 7, ThunderWindI),
            N(BeanSoldiersII, "黄巾借甲", "符图可以承载更多兵形，唤出的符兵也更坚固，爆裂之势更盛。", 2, 3, 8, BeanSoldiersI),
            N(TalismanBeidou, "斗转符", "斗柄流转不息，使出手、瞄准与御剑的节律更为迅捷。", 3, 3, 2, TalismanShenxing),
            N(TalismanGuihai, "百川归海符", "百脉如川，紫府如海；逸散灵气重新归流周天，大幅加快一切回灵。", 3, 3, 1, TalismanShenjie),
            N(TalismanShenlian, "神炼符", "器物尚未成形，神念已见其完成后的模样，因而诸般工作皆能大幅加快。", 3, 3, 0, TalismanZhuoyu),
            N(TalismanHuijian, "慧剑斩念符", "一线慧剑横过灵台，隔绝外来低语，也斩去纷乱妄念。", 3, 3, 3, TalismanJiqing),
            N(FireTornadoIII, "金乌游天", "紫府神识能够在更远处维系火符，使龙卷久久不散。", 3, 3, 6, FireTornadoII),
            N(ThunderWindIII, "紫霄召雷", "紫府神识稳住风眼，使聚拢更持久，并引来更多雷霆。", 3, 3, 7, ThunderWindII),
            N(BeanSoldiersIII, "六丁结伍", "紫府神识可同时维系一队符兵，使它们结伴奔赴敌阵。", 3, 3, 8, BeanSoldiersII),
            N(TalismanBumei, "不昧真灵符", "真灵历劫而不昧，所学所见皆归于本心，不再因心情动摇而失控。", 4, 3, 3, TalismanHuijian),
            N(TalismanJiexin, "劫心符", "以一滴本源心血唤醒全身潜能，换取肉身、工作、医疗与战斗的全面强盛。", 4, 3, 4, TalismanBaicao),
            N(TalismanLiuyang, "六阳真形符", "偏取六阳，使射击与格杀臻于极境；代价是文思枯灼，心火难平。", 4, 3, 2, TalismanBeidou),
            N(FireTornadoIV, "大日炎狱", "金丹为火种，令龙卷越过更远距离，燃烧得更加持久而猛烈。", 4, 3, 6, FireTornadoIII),
            N(ThunderWindIV, "罡风锁岳", "金丹法力使神风久聚不散，随后以连绵雷霆收束整道术式。", 4, 3, 7, ThunderWindIII),
            N(BeanSoldiersIV, "六甲成阵", "金丹法力可赋予更多豆兵坚实形体，使一队符兵如小型军阵般冲锋。", 4, 3, 8, BeanSoldiersIII),
            N(TalismanTiandao, "万艺通玄符", "百艺之理在符中彼此贯通，使所有能够从事的技艺暂时臻于极境。", 5, 3, 0, TalismanShenlian, TalismanBumei),
            N(TalismanHuanri, "偷天符", "从天数中偷去肉身走向衰老的那一步，使生理寿元不再流逝。", 5, 3, 4, TalismanJiexin)
            ,N(FireTornadoV, "焚天煮海", "元神遥御赤符，火龙卷可在极远处长久肆虐，并显露此术全部威能。", 5, 3, 6, FireTornadoIV)
            ,N(ThunderWindV, "九霄雷狱", "元神镇住浩大风眼，将敌群久困其中，最终召来此术最盛的一场雷霆。", 5, 3, 7, ThunderWindIV)
            ,N(BeanSoldiersV, "天兵奉敕", "元神一念分化众兵，符军数量、形体与自爆威势皆达到此法极致。", 5, 3, 8, BeanSoldiersIV)
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
            return id == YunjianziSkills.SummonZhuxie || (learnedSkills != null && learnedSkills.Contains(id));
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
            return id == YunjianziSkills.CloudSwordArray || id == YunjianziSkills.JiuxiaoXianxu;
        }

        internal int SkillCost(YunjianziSkillNode node)
        {
            if (node != null && IsArtifactSkill(node.Id)) return 100;
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
            Shield = ShieldMax;
            Messages.Message("DEV：已解锁" + RealmLabel + "及之前的全部技能。", Pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        internal void DebugForceLearnSkill(YunjianziSkillNode node)
        {
            if (!DebugSkillMode || node == null || node.Id == YunjianziSkills.SummonZhuxie || HasSkill(node.Id)) return;
            if (learnedSkills == null) learnedSkills = new List<string>();
            learnedSkills.Add(node.Id);
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

        private static readonly string[] BranchTitles = { "御剑诀", "仙阵诀", "神机诀", "阳符诀" };
        private static readonly Texture2D[] BranchIcons =
        {
            YunjianziAbilityTextures.SummonZhuxie,
            YunjianziAbilityTextures.ImmortalArray,
            YunjianziAbilityTextures.SummonShenji,
            YunjianziAbilityTextures.TalismanSlot
        };
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
            new[] { "雷锋", "剑契", "分光", "神行", "剑劫", "守命", "真形" },
            new[] { "养形", "雷宫", "界纹", "神行", "周天", "同庇", "天垣" },
            new[] { "主火力", "折射", "机动", "护盾", "预留", "同参", "归炉", "御机", "瑞兽真形" },
            new[] { "百工", "格物", "斗转", "斩念", "炼形", "心剑", "火法", "风雷", "道兵" }
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
            Widgets.Label(new Rect(70f, 11f, 360f, 28f), "云间宝卷 · 修为参悟");
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

        private static void DrawYangfuConcept(Rect panel)
        {
            float top = panel.y + 72f;
            float gap = 10f;
            float cardWidth = (panel.width - 34f - gap * 3f) / 4f;
            float cardHeight = 398f;
            DrawConceptCard(new Rect(panel.x + 17f, top, cardWidth, cardHeight), "筑基 · 一符位",
                "啄玉符：大幅提高成品品阶\n\n截真符：大幅提高科研速度\n\n神行符：大幅提高移动速度\n\n寄情符：美景转化为魅力与心境\n\n百草符：精通医术，避免灾难性失败\n\n心剑符：洞彻人心，提高贸易优势",
                new Color(.15f, .35f, .31f, .94f));
            DrawConceptCard(new Rect(panel.x + 17f + cardWidth + gap, top, cardWidth, cardHeight), "紫府 · 二符位",
                "斗转符：攻速提升，缩短武器特效冷却\n\n百川归海符：大幅提高灵力恢复速度\n\n神炼符：大幅提高制作工作效率\n\n慧剑斩念符：免疫负面心灵低语与精神扰动",
                new Color(.22f, .25f, .46f, .94f));
            DrawConceptCard(new Rect(panel.x + 17f + (cardWidth + gap) * 2f, top, cardWidth, cardHeight), "金丹 · 三符位",
                "不昧真灵符：心境圆满，法门施放加快\n\n劫心符：消耗本源，全面淬炼肉身\n\n六阳真形符：战斗技能20级；艺术归零，并大幅降低心境",
                new Color(.47f, .33f, .12f, .94f));
            DrawConceptCard(new Rect(panel.x + 17f + (cardWidth + gap) * 3f, top, cardWidth, cardHeight), "化神 · 四符位",
                "万艺通玄符：所有未被背景禁止的技能臻于极境\n\n偷天符：生理年龄停止增长\n\n符位只限制同时生效数量；已学会和储存的符咒不受限制。",
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
