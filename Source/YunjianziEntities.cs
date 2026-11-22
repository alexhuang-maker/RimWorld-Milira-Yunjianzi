using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Yunjianzi
{
    public static class YunjianziEntities
    {
        public static readonly string[] Ids = {"Blade","Shield","Hunter","Sniper","Mender","Breaker","Mortar","Tomb"};
        public static int Realm
        {
            get
            {
                var story = GameComponent_YunjianziPursuit.Instance;
                Pawn p = story?.target;
                if (story == null || !story.enabled || p == null || p.Dead || p.Faction != Faction.OfPlayer || !ModsConfig.AnomalyActive) return 0;
                return p.TryGetComp<CompYunjianziCultivation>()?.Realm ?? 0;
            }
        }
        public static List<Pawn> Generate(float points, bool boss = false)
        {
            var pawns = new List<Pawn>();
            foreach (int i in YunjianziEntityRules.Compose(points,Realm,boss,n=>Rand.Range(0,n)))
            {
                var kind=DefDatabase<PawnKindDef>.GetNamed("YJZ_Entity_"+Ids[i]);
                pawns.Add(PawnGenerator.GeneratePawn(kind,Faction.OfEntities));
            }
            return pawns;
        }
        public static bool FireQuestWave(Map map,YunjianziWave wave,YunjianziNodeState node)
        {
            if (Realm < 3 || Faction.OfEntities == null || !Find.Storyteller.difficulty.allowBigThreats) return false;
            bool final = node != null && node.barsBroken >= (node.index<2 ? GameComponent_YunjianziPursuit.Settings.surfaceBars : GameComponent_YunjianziPursuit.Settings.spaceBars);
            bool boss = final && !node.entityBossSpawned;
            if (boss && Realm < 4) return false;
            Pawn defender=map.mapPawns.AllPawnsSpawned.FirstOrDefault(p=>p.Faction==Faction.OfPlayer && !p.Dead && !p.Downed);
            if(defender==null)return false;
            var cells=map.AllCells.Where(c=>c.Standable(map) && !c.Fogged(map) && c.DistanceToSquared(defender.Position)>144
                && map.reachability.CanReach(c,defender,PathEndMode.Touch,TraverseParms.For(TraverseMode.PassDoors))).ToList();
            if(cells.Count==0)return false;
            if(wave.points<=0)wave.points=StorytellerUtility.DefaultThreatPointsNow(map);
            wave.points=Math.Max(YunjianziEntityRules.MinimumCost,wave.points);
            wave.attempts++;
            var pawns=Generate(wave.points,boss);
            if(pawns.Count==0)return false;
            IntVec3 origin=cells.RandomElement();
            foreach(var pawn in pawns)
            {
                // Each placement is rechecked; never use Closewalk's invalid fallback in orbit.
                var near=cells.Where(c=>c.DistanceToSquared(origin)<100).ToList();
                IntVec3 cell=near.Count>0?near.RandomElement():origin;
                if(map.Tile.LayerDef.isSpace) pawn.health.AddHediff(DefDatabase<HediffDef>.GetNamed("YJZ_EntityVoidAdaptation"));
                GenSpawn.Spawn(pawn,cell,map);
                FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(),map,1.2f,new Color(.32f,.17f,.19f));
            }
            LordMaker.MakeNewLord(Faction.OfEntities,new LordJob_AssaultColony(Faction.OfEntities,canKidnap:false,canTimeoutOrFlee:false,canSteal:false),map,pawns);
            wave.enemies=pawns;wave.faction=Faction.OfEntities;wave.executed=Find.TickManager.TicksGame;wave.success=true;
            if(boss) {node.entityBossSpawned=true;node.entityBoss=pawns.First(p=>p.kindDef.defName=="YJZ_Entity_Tomb");}
            return true;
        }
    }

    // These assaults do not derive from EntitySwarm (which also handles shamblers).
    // Replace the complete selected encounter so their species-specific Lords never
    // assume our creatures have sightstealer/chimera/gorehulk-only components.
    [HarmonyPatch]
    public static class Patch_YunjianziEntitySwarm
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            foreach(var type in new[]{typeof(IncidentWorker_GorehulkAssault),typeof(IncidentWorker_ChimeraAssault),typeof(IncidentWorker_SightstealerSwarm)})
                yield return AccessTools.Method(type,"TryExecuteWorker");
        }
        static bool Prefix(IncidentWorker __instance,IncidentParms parms,ref bool __result)
        {
            float points=parms.points*(__instance is IncidentWorker_SightstealerSwarm?.5f:1f);
            if(YunjianziEntities.Realm<3 || points<YunjianziEntityRules.MinimumCost || !(parms.target is Map map) || !Rand.Chance(.35f))return true;
            parms.faction=Faction.OfEntities;parms.raidArrivalMode=PawnsArrivalModeDefOf.EdgeWalkIn;
            if(!parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms))return true;
            var list=YunjianziEntities.Generate(points);
            if(list.Count==0)return true;
            parms.raidArrivalMode.Worker.Arrive(list,parms);
            LordMaker.MakeNewLord(Faction.OfEntities,new LordJob_AssaultColony(Faction.OfEntities,canKidnap:false,canTimeoutOrFlee:false,canSteal:false),map,list);
            // Names only, per the requested UI scope.
            string names=string.Join("、",list.Select(p=>p.kindDef.label).Distinct());
            Find.LetterStack.ReceiveLetter(names,names,LetterDefOf.ThreatBig,new LookTargets(list));
            __result=true;return false;
        }
    }
}
