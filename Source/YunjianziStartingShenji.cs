using System.Linq;
using RimWorld;
using Verse;

namespace Yunjianzi
{
    // Game.InitNewGame invokes this after the starting colonist's realm and
    // mechanitor tracker have spawned. It is not invoked when loading a save.
    public sealed class ScenPart_YunjianziStartingShenji : ScenPart
    {
        private bool granted;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref granted, "yjz_startingShenjiGranted", false);
        }

        public override string Summary(Scenario scen)
        {
            return "开局随行一台青羽机侍，配备普通品质青羽玉甲、青芒灵铳和羽仿布内衬。无需学习神机诀即可控制，不占带宽；新造神机仍须解锁灵机启明，并受神机额度限制。";
        }

        public override void PostGameStart()
        {
            if (granted) return;
            Pawn owner = Find.Maps.SelectMany(map => map.mapPawns.FreeColonistsSpawned)
                .FirstOrDefault(YunjianziSoleColonistUtility.IsYunjianzi);
            if (owner == null)
            {
                Log.Error("[Yunjianzi] 开局未找到云间子，无法派发随行青羽机侍。");
                return;
            }

            Pawn mech = PawnGenerator.GeneratePawn(YJZDefOf.YJZ_Shenji, Faction.OfPlayer);
            // Only this newly created gift receives a full kit. Gestated mechs
            // still require separately crafted weapons and armor.
            foreach (Apparel apparel in mech.apparel.WornApparel.ToList())
            {
                mech.apparel.Remove(apparel);
                apparel.Destroy();
            }
            foreach (ThingWithComps equipment in mech.equipment.AllEquipmentListForReading.ToList())
            {
                mech.equipment.Remove(equipment);
                equipment.Destroy();
            }
            mech.apparel.Wear((Apparel)MakeNormalGear(YJZDefOf.Milian_Lining, YJZDefOf.Milira_FeatherThread), false);
            mech.apparel.Wear((Apparel)MakeNormalGear(YJZDefOf.YJZ_Apparel_ShenjiArmor_Base), false);
            mech.equipment.AddEquipment(MakeNormalGear(YJZDefOf.YJZ_Weapon_Shenji_Assault));

            GenSpawn.Spawn(mech, CellFinder.RandomClosewalkCellNear(owner.Position, owner.Map, 3), owner.Map);
            granted = true;
            // Direct binding is the one-time starting allowance; do not grant
            // ShenjiSummon or weaken the production/gestation skill checks.
            if (!mech.TryGetComp<CompYunjianziShenji>().BindTo(owner))
                Log.Error("[Yunjianzi] 随行青羽机侍暂未进入控制组，将由神机组件继续尝试绑定。");
        }

        private static ThingWithComps MakeNormalGear(ThingDef def, ThingDef stuff = null)
        {
            ThingWithComps gear = (ThingWithComps)ThingMaker.MakeThing(def, stuff);
            gear.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Normal, ArtGenerationContext.Outsider);
            return gear;
        }
    }
}
