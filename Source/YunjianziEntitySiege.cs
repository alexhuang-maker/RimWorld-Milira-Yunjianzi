using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Yunjianzi
{
    public class Building_YunjianziTombstone : Building
    {
        public const float AuraRadius=7;
        public static Building_YunjianziTombstone Fall(Map map,IntVec3 cell,Pawn caster)
        {
            var rect=GenAdj.OccupiedRect(cell,Rot4.North,new IntVec2(2,2));
            if(rect.Any(c=>!c.InBounds(map)))return null;
            var before=new HashSet<Thing>(rect.SelectMany(c=>c.GetThingList(map)));
            foreach(var obstruction in before.Where(t=>(t is Building || t is Plant) && !(t is Building_YunjianziTombstone)).ToList())
                if(obstruction.def.useHitPoints && obstruction.def.destroyable)
                    obstruction.TakeDamage(new DamageInfo(DamageDefOf.Blunt,250,0,-1,caster));
            // Automatic rebuild creates a fresh blueprint during destruction. It cannot reserve the landing site.
            foreach(var blueprint in rect.SelectMany(c=>c.GetThingList(map)).OfType<Blueprint>().Distinct().Where(t=>!before.Contains(t)).ToList())
                blueprint.Destroy(DestroyMode.Vanish);
            // Existing possessions block the footprint. Only fresh demolition debris may move aside.
            foreach(var item in rect.SelectMany(c=>c.GetThingList(map)).Distinct().Where(t=>t.def.category==ThingCategory.Item && !before.Contains(t)).ToList())
            {
                var old=item.Position;item.DeSpawn();
                if(!GenPlace.TryPlaceThing(item,cell,map,ThingPlaceMode.Near,extraValidator:c=>!rect.Contains(c)))
                    GenSpawn.Spawn(item,old,map);
            }
            if(rect.Any(c=>!c.Standable(map) || !c.GetTerrain(map).affordances.Contains(TerrainAffordanceDefOf.Heavy)
                || c.GetThingList(map).Any(t=>t.def.category==ThingCategory.Item || t is Building || t is Plant || t is Blueprint || t is Frame)))return null;
            var stone=(Building_YunjianziTombstone)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("YJZ_EntityTombstone"));
            stone.SetFaction(caster?.Faction ?? Faction.OfEntities);
            GenSpawn.Spawn(stone,cell,map,Rot4.North,WipeMode.VanishOrMoveAside);
            stone.RefreshAura();return stone;
        }
        protected override void Tick()
        {
            base.Tick();
            if(this.IsHashIntervalTick(30))RefreshAura();
        }
        public void RefreshAura()
        {
            if(!Spawned)return;
            foreach(var pawn in Map.mapPawns.AllPawnsSpawned.Where(p=>!p.Dead && p.Position.DistanceTo(Position)<=AuraRadius).ToList())
            {
                string name=pawn.TryGetComp<CompYunjianziEntity>()!=null?"YJZ_TombstoneEmpower":pawn.Faction?.IsPlayer==true?"YJZ_TombstoneWeaken":null;
                if(name==null)continue;
                var def=DefDatabase<HediffDef>.GetNamed(name);
                var effect=pawn.health.hediffSet.GetFirstHediffOfDef(def) ?? pawn.health.AddHediff(def);
                effect.TryGetComp<HediffComp_Disappears>().ticksToDisappear=60;
            }
        }
        public override void DrawExtraSelectionOverlays()
        {base.DrawExtraSelectionOverlays();GenDraw.DrawRadiusRing(Position,AuraRadius);}
    }

    public static class YunjianziMobileMortar
    {
        public static Pawn Target(Pawn pawn,float range)
            =>pawn.Map.mapPawns.AllPawnsSpawned.Where(p=>p!=pawn && !p.Dead && !p.Downed && p.HostileTo(pawn)
                && p.Position.DistanceTo(pawn.Position)<=range
                && !p.health.hediffSet.hediffs.Any(h=>h.TryGetComp<HediffComp_Invisibility>()?.PsychologicallyVisible==false)
                && GenSight.LineOfSight(pawn.Position,p.Position,pawn.Map))
                .OrderBy(p=>p.Position.DistanceToSquared(pawn.Position)).FirstOrDefault();
        public static void Tick(CompYunjianziEntity c)
        {
            var pawn=c.Pawn;int now=Find.TickManager.TicksGame;
            if(pawn.Position.Roofed(pawn.Map)){c.windupUntil=0;return;}
            if(c.windupUntil>0)
            {
                if(now<c.windupUntil)return;
                c.windupUntil=0;c.nextSkill=now+480;
                float distance=pawn.Position.DistanceTo(c.aim);
                if(distance<6 || distance>35 || !c.aim.InBounds(pawn.Map))return;
                var verb=pawn.verbTracker.AllVerbs.FirstOrDefault(v=>v is Verb_YunjianziEntityShot);
                verb?.TryStartCastOn(new LocalTargetInfo(c.aim),nonInterruptingSelfCast:true);
                return;
            }
            if(now<c.nextSkill || !pawn.IsHashIntervalTick(30))return;
            var target=Target(pawn,35);
            if(target==null || target.Position.DistanceTo(pawn.Position)<6)return;
            c.aim=target.Position;c.windupUntil=now+180;
        }
    }
    public class JobGiver_YunjianziMobileMortar : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            var comp=pawn.TryGetComp<CompYunjianziEntity>();
            if(comp?.Role!=6 || !comp.Active)return null;
            var target=YunjianziMobileMortar.Target(pawn,65);
            if(target==null)return null;
            var origin=pawn.Position;
            var candidates=GenRadial.RadialCellsAround(origin,7,false).Where(c=>c.InBounds(pawn.Map) && c.Standable(pawn.Map)
                && !c.Roofed(pawn.Map) && c.GetFirstPawn(pawn.Map)==null && c.DistanceTo(origin)>=3
                && pawn.CanReach(c,PathEndMode.OnCell,Danger.Deadly)
                && GenSight.LineOfSight(c,target.Position,pawn.Map)).ToList();
            if(candidates.Count==0)return null;
            // Keep standoff distance while alternating lateral paths during the firing cycle.
            float side=((Find.TickManager.TicksGame/300+pawn.thingIDNumber)%2==0)?1:-1;
            var cell=candidates.OrderBy(c=>Math.Abs(c.DistanceTo(target.Position)-21)
                + .1f*c.DistanceTo(origin)+side*.12f*(c.x-origin.x)).First();
            var job=JobMaker.MakeJob(JobDefOf.Goto,cell);job.expiryInterval=180;job.checkOverrideOnExpire=true;
            return job;
        }
    }
}
