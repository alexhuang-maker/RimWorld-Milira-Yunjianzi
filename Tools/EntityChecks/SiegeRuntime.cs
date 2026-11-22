using System;
using System.Linq;
using RimWorld;
using Verse;

namespace Yunjianzi
{
    public static partial class EntityRuntimeAudit
    {
        static int movingShells;
        static int shellShotFrame=-1;
        static bool fallingCaptured;
        static void SiegeScreenshots()
        {
            var tomb=actors.LastOrDefault()?.TryGetComp<CompYunjianziEntity>();
            if(!fallingCaptured && tomb!=null && tomb.windupUntil>Find.TickManager.TicksGame && tomb.windupUntil-Find.TickManager.TicksGame<25 && tomb.strikes.Count>0)
            {
                fallingCaptured=true;
                Find.CameraDriver.SetRootPosAndSize(tomb.strikes[0].ToVector3Shifted(),12);
                ScreenshotTaker.TakeNonSteamShot("tombstone-falling-and-persistent");
            }
            if(shells>0 && shellShotFrame<0)
            {
                shellShotFrame=frame;
                var mortar=actors.First(p=>p.TryGetComp<CompYunjianziEntity>().Role==6);
                Find.CameraDriver.SetRootPosAndSize((mortar.DrawPos+mortar.TryGetComp<CompYunjianziEntity>().aim.ToVector3Shifted())*.5f,17);
            }
            if(shellShotFrame>=0 && frame==shellShotFrame+12)ScreenshotTaker.TakeNonSteamShot("mortar-moving-overhead-shell");
        }
        static void SiegeChecks(Pawn owner)
        {
            var cell=new IntVec3(86,0,27);
            Check("siege definitions and native mortar arc",()=>{
                foreach(string name in new[]{"YJZ_EntityTombstone","YJZ_EntityShell"})
                {
                    var d=DefDatabase<ThingDef>.GetNamed(name);
                    Require(!d.ConfigErrors().Any(),string.Join(";",d.ConfigErrors()));
                    Require(d.graphicData.Graphic.MatSingle!=BaseContent.BadMat,"missing sprite");
                }
                var shell=DefDatabase<ThingDef>.GetNamed("YJZ_EntityShell").projectile;
                Require(shell.flyOverhead && shell.arcHeightFactor>=5 && shell.speed==12,"not overhead mortar arc");
            });
            Check("2x2 tombstone placement, edge, items and surviving buildings",()=>{
                Require(Building_YunjianziTombstone.Fall(map,new IntVec3(map.Size.x-1,0,map.Size.z-1),null)==null,"offmap accepted");
                var item=ThingMaker.MakeThing(ThingDefOf.Steel);item.stackCount=17;
                GenSpawn.Spawn(item,cell+new IntVec3(1,0,1),map);
                Require(Building_YunjianziTombstone.Fall(map,cell,null)==null,"item overwritten");
                Require(item.Spawned && item.stackCount==17,"ground item lost");item.Destroy();
                var wall=ThingMaker.MakeThing(ThingDefOf.Wall,ThingDefOf.Plasteel);
                GenSpawn.Spawn(wall,cell+new IntVec3(1,0,0),map);int hp=wall.HitPoints;
                wall.SetFaction(Faction.OfPlayer);map.areaManager.Home[wall.Position]=true;Find.PlaySettings.autoRebuild=true;
                Require(Building_YunjianziTombstone.Fall(map,cell,null)==null,"surviving building overwritten");
                Require(wall.Spawned && wall.HitPoints>0 && wall.HitPoints<hp,"building not damaged");
                wall.HitPoints=1;
                var before=map.listerThings.AllThings.Where(t=>t.def.category==ThingCategory.Item).ToList();
                var stone=Building_YunjianziTombstone.Fall(map,cell,null);
                Require(wall.Destroyed && stone!=null,"destroyed building not replaced");
                Require(stone.OccupiedRect().Count()==4,"wrong footprint");
                Require(!stone.OccupiedRect().SelectMany(c=>c.GetThingList(map)).Any(t=>t.def.category==ThingCategory.Item),"debris inside footprint");
                int count=map.listerThings.AllThings.Count(t=>t is Building_YunjianziTombstone);
                Require(Building_YunjianziTombstone.Fall(map,cell,null)==null && map.listerThings.AllThings.Count(t=>t is Building_YunjianziTombstone)==count,"duplicate overlapping stone");
                stone.Destroy();
                var bed=ThingMaker.MakeThing(ThingDefOf.Bed,ThingDefOf.Plasteel);bed.HitPoints=1;
                GenSpawn.Spawn(bed,cell,map,Rot4.North);
                var second=Building_YunjianziTombstone.Fall(map,cell,null);
                Require(bed.Destroyed && second!=null,"building with salvage blocks replacement");
                Require(map.listerThings.AllThings.Except(before).Any(t=>t.def==ThingDefOf.Plasteel),"demolition debris lost");
                Require(!second.OccupiedRect().SelectMany(c=>c.GetThingList(map)).Any(t=>t.def.category==ThingCategory.Item),"salvage left underneath stone");
                second.Destroy();
            });
            foreach(var c in new CellRect(83,25,12,9))foreach(var t in c.GetThingList(map).ToList())if(!(t is Pawn) && t.def.destroyable)t.Destroy();
            Check("vegetation is crushed without blocking the tombstone",()=>{
                var grass=ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Plant_Grass"));
                GenSpawn.Spawn(grass,cell,map);
                var stone=Building_YunjianziTombstone.Fall(map,cell,null);
                Require(grass.Destroyed && stone!=null,"vegetation prevented landing");stone.Destroy();
            });
            Check("aura weakens players, empowers entities, does not stack and expires",()=>{
                var original=owner.Position;owner.Position=cell+new IntVec3(-2,0,0);owner.Notify_Teleported();
                var enemy=PawnGenerator.GeneratePawn(DefDatabase<PawnKindDef>.GetNamed("YJZ_Entity_Shield"),Faction.OfEntities);
                GenSpawn.Spawn(enemy,cell+new IntVec3(-2,0,1),map);
                float speed=owner.GetStatValue(StatDefOf.MoveSpeed),enemySpeed=enemy.GetStatValue(StatDefOf.MoveSpeed);
                float damage=owner.GetStatValue(StatDefOf.IncomingDamageFactor),enemyDamage=enemy.GetStatValue(StatDefOf.IncomingDamageFactor);
                var a=Building_YunjianziTombstone.Fall(map,cell,null);
                var b=Building_YunjianziTombstone.Fall(map,cell+new IntVec3(0,0,3),null);
                Require(a!=null && b!=null,"test stones missing");a.RefreshAura();b.RefreshAura();
                var weaken=DefDatabase<HediffDef>.GetNamed("YJZ_TombstoneWeaken");
                var empower=DefDatabase<HediffDef>.GetNamed("YJZ_TombstoneEmpower");
                Require(owner.health.hediffSet.hediffs.Count(h=>h.def==weaken)==1 && enemy.health.hediffSet.hediffs.Count(h=>h.def==empower)==1,"stacked/missing aura");
                Require(Math.Abs(owner.GetStatValue(StatDefOf.MoveSpeed)-speed*.85f)<.02f && Math.Abs(enemy.GetStatValue(StatDefOf.MoveSpeed)-enemySpeed*1.12f)<.02f,"speed modifiers wrong");
                Require(Math.Abs(owner.GetStatValue(StatDefOf.IncomingDamageFactor)-damage*1.15f)<.02f && Math.Abs(enemy.GetStatValue(StatDefOf.IncomingDamageFactor)-enemyDamage*.85f)<.02f,$"damage modifiers wrong: {damage} => {owner.GetStatValue(StatDefOf.IncomingDamageFactor)}, {enemyDamage} => {enemy.GetStatValue(StatDefOf.IncomingDamageFactor)}");
                var effect=owner.health.hediffSet.GetFirstHediffOfDef(weaken);var timer=effect.TryGetComp<HediffComp_Disappears>();
                owner.Position=new IntVec3(30,0,30);owner.Notify_Teleported();timer.ticksToDisappear=20;a.RefreshAura();b.RefreshAura();
                Require(timer.ticksToDisappear==20,"out of range refreshed");
                a.Destroy();b.Destroy();float delta=0;for(int i=0;i<60;i++)timer.CompPostTick(ref delta);
                Require(timer.CompShouldRemove,"destroyed aura permanent");
                owner.health.RemoveHediff(effect);enemy.Destroy();owner.Position=original;owner.Notify_Teleported();
            });
        }
    }
}
