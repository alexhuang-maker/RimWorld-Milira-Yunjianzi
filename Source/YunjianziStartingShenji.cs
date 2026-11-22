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
            return "陵墓正道两侧各列三座碎片方舟，须先研读宝卷、接受传承才能开启；仅一座封存着青羽机侍，开启后获得并接入云间灵脉的控制组。";
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

            // Orbital starts grant the same one-time companion from the ark.
            // Preserve the previous grant path for non-orbital scenarios.
            if(owner.Map.GetComponent<MapComponent_YunjianziMemorial>().altar.IsValid)return;
            GrantAt(owner,owner.Position);
        }

        internal bool GrantAt(Pawn owner,IntVec3 position)
        {
            if(granted || owner==null || !owner.Spawned)return false;

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

            GenSpawn.Spawn(mech, CellFinder.RandomClosewalkCellNear(position, owner.Map, 3), owner.Map);
            granted = true;
            Current.Game.GetComponent<GameComponent_YunjianziPursuit>().companion = mech;
            // Direct binding is the one-time starting allowance; do not grant
            // ShenjiSummon or weaken the production/gestation skill checks.
            if (!mech.TryGetComp<CompYunjianziShenji>().BindTo(owner) &&
                owner.TryGetComp<CompYunjianziCultivation>()?.AwaitingInheritance != true)
                Log.Error("[Yunjianzi] 随行青羽机侍暂未进入控制组，将由神机组件继续尝试绑定。");
            return true;
        }

        private static ThingWithComps MakeNormalGear(ThingDef def, ThingDef stuff = null)
        {
            ThingWithComps gear = (ThingWithComps)ThingMaker.MakeThing(def, stuff);
            gear.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Normal, ArtGenerationContext.Outsider);
            return gear;
        }
    }
}
