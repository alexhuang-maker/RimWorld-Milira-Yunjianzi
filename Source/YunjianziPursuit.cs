using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Yunjianzi
{
    public sealed class ScenPart_YunjianziPursuit : ScenPart
    {
        public override string Summary(Scenario scen) => "未知魔神持续追索云间子。迁往新驻地可暂避追踪，修行后可循迹斩断连接。";
    }

    public sealed class YunjianziPursuitSettings : Def
    {
        public int raidInterval = 30000, retryInterval = 2500, defenseTicks = 180000;
        public int surfaceBars = 3, spaceBars = 5, surfaceWaves = 2, spaceWaves = 3;
        public int waveInterval = 30000;
    }

    public sealed class YunjianziNodeState : IExposable
    {
        public int index, barsBroken, hitPoints = -1, defenseUntil, nextWave;
        public bool defending, complete;
        public bool entityBossSpawned;
        public bool entityBossDefeated;
        public Pawn entityBoss;
        public MapParent site;
        public Quest quest;
        public Faction faction;
        public string blocked;
        public List<YunjianziWave> waves = new List<YunjianziWave>();
        public void ExposeData()
        {
            Scribe_Values.Look(ref index, "index"); Scribe_Values.Look(ref barsBroken, "barsBroken");
            Scribe_Values.Look(ref hitPoints, "hitPoints", -1); Scribe_Values.Look(ref defenseUntil, "defenseUntil");
            Scribe_Values.Look(ref nextWave, "nextWave"); Scribe_Values.Look(ref defending, "defending");
            Scribe_Values.Look(ref complete, "complete"); Scribe_Values.Look(ref blocked, "blocked");
            Scribe_Values.Look(ref entityBossSpawned, "entityBossSpawned");
            Scribe_Values.Look(ref entityBossDefeated, "entityBossDefeated");
            Scribe_References.Look(ref entityBoss, "entityBoss");
            Scribe_References.Look(ref site, "site"); Scribe_References.Look(ref faction, "faction");
            Scribe_References.Look(ref quest, "quest");
            Scribe_Collections.Look(ref waves, "waves", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && waves == null) waves = new List<YunjianziWave>();
        }
    }
    public sealed class YunjianziWave : IExposable
    {
        public string id;
        public int scheduled, executed, attempts;
        public float points;
        public Faction faction;
        public bool success;
        public PawnGroupKindDef groupKind;
        public List<Pawn> enemies = new List<Pawn>();
        public void ExposeData()
        {
            Scribe_Values.Look(ref id,"id"); Scribe_Values.Look(ref scheduled,"scheduled");
            Scribe_Values.Look(ref executed,"executed"); Scribe_Values.Look(ref attempts,"attempts");
            Scribe_Values.Look(ref points,"points"); Scribe_Values.Look(ref success,"success");
            Scribe_References.Look(ref faction,"faction");
            Scribe_Defs.Look(ref groupKind,"groupKind");
            Scribe_Collections.Look(ref enemies,"enemies",LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && enemies == null) enemies = new List<Pawn>();
        }
    }

    public sealed partial class GameComponent_YunjianziPursuit : GameComponent
    {
        public Pawn target, companion;
        public bool enabled, initialized, locked, warned, revealed, spaceRevealed, finished;
        public int cycleStart, nextRaid, nextReminder, nextSiteTry;
        public PlanetTile residence = PlanetTile.Invalid;
        public List<YunjianziNodeState> nodes = new List<YunjianziNodeState>();
        public static readonly string[] Names = {"吞日魔枢", "葬星邪印", "噬界天渊", "万劫魔巢", "终焉界楔"};
        public static readonly string[] NodeDefNames = {"YJZ_PursuitNode_SunlessNexus", "YJZ_PursuitNode_StarTombSeal", "YJZ_PursuitNode_WorldDevouringAbyss", "YJZ_PursuitNode_MyriadDoomNest", "YJZ_PursuitNode_FinalRiftAnchor"};
        public static string QuestTitle(int index) => (index<2?"斩断邪引·":"天外斩邪·")+Names[index];
        public static GameComponent_YunjianziPursuit Instance => Current.Game?.GetComponent<GameComponent_YunjianziPursuit>();
        public static YunjianziPursuitSettings Settings => DefDatabase<YunjianziPursuitSettings>.GetNamed("YJZ_PursuitSettings");
        public static int Now => Find.TickManager.TicksGame;
        public bool SurfaceDone => nodes.Count >= 2 && nodes[0].complete && nodes[1].complete;
        public GameComponent_YunjianziPursuit(Game game) { }
        public override void ExposeData()
        {
            Scribe_References.Look(ref target,"yjz_target"); Scribe_References.Look(ref companion,"yjz_companion");
            Scribe_Values.Look(ref enabled,"yjz_enabled"); Scribe_Values.Look(ref initialized,"yjz_initialized");
            Scribe_Values.Look(ref cycleStart,"yjz_cycleStart"); Scribe_Values.Look(ref nextRaid,"yjz_nextRaid");
            Scribe_Values.Look(ref nextReminder,"yjz_nextReminder"); Scribe_Values.Look(ref nextSiteTry,"yjz_nextSiteTry");
            Scribe_Values.Look(ref locked,"yjz_locked"); Scribe_Values.Look(ref warned,"yjz_warned");
            Scribe_Values.Look(ref revealed,"yjz_revealed"); Scribe_Values.Look(ref spaceRevealed,"yjz_spaceRevealed");
            Scribe_Values.Look(ref finished,"yjz_finished");
            Scribe_Values.Look(ref spaceUnavailableNotified,"yjz_spaceUnavailableNotified");
            Scribe_Values.Look(ref residence,"yjz_residence",PlanetTile.Invalid);
            Scribe_Collections.Look(ref nodes,"yjz_nodes",LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && nodes == null) nodes = new List<YunjianziNodeState>();
        }
        public static string Text(string key) => ("YJZ_Story_"+key).Translate();
        public void Letter(string title, string text, LookTargets look = null)
        { Find.LetterStack.ReceiveLetter(title,text,LetterDefOf.NeutralEvent,look ?? new LookTargets(target)); }
        private void TrackingLetter(string kind)
        {
            string prefix = target?.TryGetComp<CompYunjianziCultivation>()?.Realm >= 3 ? "Late" : "Early";
            Letter(Text(prefix+kind+"Title"),Text(prefix+kind));
        }
        private bool DedicatedScenario()
        {
            Scenario s=Find.Scenario;
            return s != null && (s.AllParts.Any(p=>p is ScenPart_YunjianziPursuit) ||
                (s.name == "云间子·孤云异象" && s.AllParts.Any(p=>p is ScenPart_YunjianziSoleColonist)
                 && s.AllParts.Any(p=>p is ScenPart_YunjianziStartingShenji)));
        }
        private void Initialize()
        {
            if (!DedicatedScenario()) return;
            // Remove only our original scenario's join restrictions, never other scenarios.
            var parts=AccessTools.Field(typeof(Scenario),"parts").GetValue(Find.Scenario) as List<ScenPart>;
            string[] joins={"WandererJoin","StrangerInBlackJoin","GameEndedWanderersJoin","CreepJoinerJoin","CreepJoinerJoin_Metalhorror"};
            parts?.RemoveAll(p=>p is ScenPart_DisableIncident d && joins.Contains(d.Incident?.defName));
            target=Find.Maps.SelectMany(m=>m.mapPawns.AllPawns).FirstOrDefault(YunjianziSoleColonistUtility.IsYunjianzi);
            if (target == null) return;
            if (target.TryGetComp<CompYunjianziCultivation>()?.AwaitingInheritance == true) return;
            enabled=initialized=true;
            ResetCycle(target.Map?.Tile ?? PlanetTile.Invalid, false);
        }
        private void ResetCycle(PlanetTile tile, bool notify)
        {
            residence=tile; cycleStart=Now; locked=warned=false;
            nextRaid=Now+(SurfaceDone?20:10)*GenDate.TicksPerDay;
            nextReminder=Now+(SurfaceDone?28:18)*GenDate.TicksPerDay;
            if(notify) TrackingLetter("Move");
        }
        public override void GameComponentTick()
        {
            if (Now%250!=0) return;
            if (!initialized) Initialize();
            RefreshNarrative();
            if (!enabled || finished) return;
            foreach(var node in nodes.ToList()) TickNode(node);
            if (finished) return;
            Map map=target != null && !target.Dead && target.Faction==Faction.OfPlayer ? target.MapHeld : null;
            if (map == null || !target.Spawned) return;
            bool taskMap=nodes.Any(n=>n.site==map.Parent);
            if(!taskMap && map.IsPlayerHome && map.Tile!=residence) ResetCycle(map.Tile,true);
            TryProgress(map);
            int warning=YunjianziPursuitRules.WarningTick(cycleStart,SurfaceDone);
            int lockTick=YunjianziPursuitRules.LockTick(cycleStart,SurfaceDone);
            if (!locked && Now>=lockTick)
            {
                warned=true; locked=true; nextRaid=Now; TrackingLetter("Lock");
                if(nextReminder<=Now)nextReminder=Now+9*GenDate.TicksPerDay;
            }
            else if (!warned && Now>=warning) { warned=true; TrackingLetter("Warning"); }
            else if (locked && Now>=nextReminder) { TrackingLetter("Reminder"); nextReminder=Now+9*GenDate.TicksPerDay; }
            if (!locked || taskMap || !map.IsPlayerHome || Now<nextRaid) return;
            var wave=new YunjianziWave {id="pursuit-"+Now,scheduled=Now};
            bool ok=FireWave(map,wave,null);
            nextRaid=Now+(ok?Settings.raidInterval:Settings.retryInterval);
        }
        private void TryProgress(Map map)
        {
            int realm=target.TryGetComp<CompYunjianziCultivation>()?.Realm ?? 0;
            if(realm<4) return;
            if(!revealed) { revealed=true; Letter(Text("RevealTitle"),Text("Reveal")); }
            int index=nodes.Count(n=>n.complete);
            if(index>=5 || nodes.Any(n=>!n.complete && n.site!=null && !n.site.Destroyed) || Now<nextSiteTry) return;
            nextSiteTry=Now+Settings.retryInterval;
            if(index>=2)
            {
                if(realm<5 || !SurfaceDone) return;
                if(!ModsConfig.OdysseyActive) { if(!spaceUnavailableNotified) { Letter("天外任务待办","地表连接已断。后续三处连接需要启用 Odyssey 的真实太空地图与运输能力。"); spaceUnavailableNotified=true; } return; }
                if(!spaceRevealed) { spaceRevealed=true; Letter(Text("SpaceTitle"),Text("Space")); }
            }
            PlanetLayer layer=null;
            if(index>=2 && !Find.WorldGrid.TryGetFirstAdjacentLayerOfDef(map.Tile,PlanetLayerDefOf.Orbit,out layer))
            { if(map.Tile.LayerDef==PlanetLayerDefOf.Orbit) layer=map.Tile.Layer; else return; }
            PlanetTile tile;
            if(!TileFinder.TryFindNewSiteTile(out tile,map.Tile,3,20,canBeSpace:index>=2,layer:layer))
            { Log.Warning("[Yunjianzi] No reachable node site; retry scheduled."); return; }
            var site=(MapParent)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed(index<2?"YJZ_SurfaceNodeSite":"YJZ_SpaceNodeSite"));
            site.Tile=tile;
            var state=nodes.FirstOrDefault(n=>n.index==index);
            if(state==null) {state=new YunjianziNodeState{index=index};nodes.Add(state);}
            state.site=site;
            Find.WorldObjects.Add(site);
            if(state.quest==null)
            {
                state.quest=Quest.MakeRaw();
                state.quest.root=DefDatabase<QuestScriptDef>.GetNamed("YJZ_NodeQuest");
                state.quest.name=QuestTitle(index);state.quest.description=QuestBody(index);
                state.quest.AddPart(new QuestPart_YunjianziNode{nodeIndex=index});
                state.quest.SetInitiallyAccepted();Find.QuestManager.Add(state.quest);
            }
            Letter(QuestTitle(index),QuestBody(index)+"\n\n"+ProgressText(index),new LookTargets(site));
        }
        public YunjianziNodeState State(MapParent parent) => nodes.FirstOrDefault(n=>n.site==parent);
        public void BreakBar(YunjianziNodeState n)
        {
            if(n==null || n.complete || n.defending) return;
            n.barsBroken++; n.defending=true; n.hitPoints=1;
            n.defenseUntil=Now+Settings.defenseTicks; n.nextWave=Now; n.waves.Clear();
            int count=n.index<2?Settings.surfaceWaves:Settings.spaceWaves;
            for(int i=0;i<count;i++) n.waves.Add(new YunjianziWave{id=$"node-{n.index}-bar-{n.barsBroken}-wave-{i}",scheduled=Now+i*Settings.waveInterval});
            Letter(Text("PhaseTitle"),Text("Phase"),new LookTargets(n.site));
        }
        private void TickNode(YunjianziNodeState n)
        {
            if(n.complete || !n.defending || n.site?.Map==null) return;
            Map map=n.site.Map;
            // Upgrading a save during its final defense must still require the new guardian.
            if(ModsConfig.AnomalyActive && !n.entityBossSpawned &&
                n.barsBroken >= (n.index<2?Settings.surfaceBars:Settings.spaceBars) && n.waves.All(w=>w.success))
                n.waves.Add(new YunjianziWave{id=$"node-{n.index}-guardian",scheduled=Now});
            if(!map.mapPawns.AllPawnsSpawned.Any(p=>p.Faction==Faction.OfPlayer && !p.Dead && !p.Downed)) return;
            YunjianziWave pending=n.waves.FirstOrDefault(w=>!w.success);
            if(pending!=null && Now>=pending.scheduled && Now>=n.nextWave)
            {
                if((ModsConfig.AnomalyActive ? YunjianziEntities.FireQuestWave(map,pending,n) : FireWave(map,pending,n.faction))) {n.faction=pending.faction;n.blocked=null;n.nextWave=Now+Settings.waveInterval;}
                else {n.nextWave=Now+Settings.retryInterval;n.blocked="增援暂受阻，等待合法袭击来源或到达位置；该波尚未计为完成。";}
            }
            if(n.entityBossSpawned && !n.entityBossDefeated) return;
            if(!YunjianziPursuitRules.DefenseComplete(Now,n.defenseUntil,true,n.waves.All(w=>w.success),n.waves.SelectMany(w=>w.enemies).Any(p=>p!=null && p.Spawned && p.Map==map && GenHostility.IsActiveThreatToPlayer(p,true)))) return;
            n.defending=false;
            int bars=n.index<2?Settings.surfaceBars:Settings.spaceBars;
            if(n.barsBroken<bars) {n.hitPoints=-1;Messages.Message("节点重新显形，可以继续攻击。",new LookTargets(n.site),MessageTypeDefOf.NeutralEvent);return;}
            n.complete=true;
            n.quest?.End(QuestEndOutcome.Success,sendLetter:false);
            if(n.index==1)
            {
                if(!locked) {nextRaid=cycleStart+20*GenDate.TicksPerDay;nextReminder=cycleStart+28*GenDate.TicksPerDay;warned=false;}
            }
            if(n.index==4) {finished=true;nextRaid=int.MaxValue;Letter(Text("FinalTitle"),Text("Final")+"\n\n"+ProgressText(n.index));}
            else
            {
                string text=Text("Cleared")+"\n\n"+ProgressText(n.index);
                if(n.index==1)
                    text+=(locked?"\n\n当前锁定仍在，迁往新驻地后获得二十天准备期。":"\n\n当前周期改为第十九天预警、第二十天锁定。")
                        +"\n地表节点已全部根除，后续天外节点需达到化神境界。";
                Letter(Text("ClearedTitle"),text,new LookTargets(n.site));
            }
        }
        internal static bool FireWave(Map map,YunjianziWave wave,Faction fixedFaction)
        {
            if(!Find.Storyteller.difficulty.allowBigThreats) return false;
            bool space=map.Tile.LayerDef.isSpace;
            if(space && ModsConfig.AnomalyActive && Faction.OfEntities!=null)
                return FireEntities(map,wave);
            Faction faction=wave.faction ?? fixedFaction;
            if(space) faction=Faction.OfMechanoids; // Vanilla vacuum-safe source; never spawn unprotected humans.
            if(faction!=null && (faction.defeated || !faction.HostileTo(Faction.OfPlayer))) return false;
            if(wave.points<=0)wave.points=StorytellerUtility.DefaultThreatPointsNow(map);
            var parms=new IncidentParms {target=map,points=wave.points>0?wave.points:StorytellerUtility.DefaultThreatPointsNow(map),faction=faction,raidStrategy=RaidStrategyDefOf.ImmediateAttack};
            if(space)
            {
                IntVec3 cell=map.AllCells.Where(c=>c.Standable(map) && !c.Roofed(map)).RandomElementWithFallback(IntVec3.Invalid);
                if(!cell.IsValid || faction==null) return false;
                parms.raidArrivalMode=PawnsArrivalModeDefOf.CenterDrop;parms.spawnCenter=cell;
            }
            wave.attempts++;
            List<Pawn> pawns;
            var worker=(IncidentWorker_RaidEnemy)IncidentDefOf.RaidEnemy.Worker;
            if(!worker.TryGenerateRaidInfo(parms,out pawns))
            {Log.Warning($"[Yunjianzi] Wave {wave.id} failed attempt {wave.attempts}; retry deferred.");return false;}
            // Native generation chooses legal faction, points and arrival; the returned list tracks only this wave.
            parms.raidStrategy.Worker.MakeLords(parms,pawns);
            wave.faction=parms.faction;wave.points=parms.points;wave.enemies=pawns;wave.executed=Now;wave.success=true;
            Find.LetterStack.ReceiveLetter("追兵抵达","魔神推动的敌人已经抵达。",LetterDefOf.ThreatBig,new LookTargets(pawns));
            Log.Message($"[Yunjianzi] {wave.id}: faction={parms.faction.def.defName}, points={parms.points}, pawns={pawns.Count}, tick={Now}");
            return true;
        }
        private static bool FireEntities(Map map,YunjianziWave wave)
        {
            wave.attempts++;
            Pawn defender=map.mapPawns.AllPawnsSpawned.FirstOrDefault(p=>p.Faction==Faction.OfPlayer && !p.Downed && !p.Dead);
            if(defender==null)return false;
            // Entity incidents normally enter from the surface map edge. Orbital raids
            // instead manifest on reachable platform cells, with protection applied BEFORE spawning.
            var cells=map.AllCells.Where(c=>c.Standable(map) && !c.Fogged(map) && c.DistanceToSquared(defender.Position)>144
                && map.reachability.CanReach(c,defender,PathEndMode.Touch,TraverseParms.For(TraverseMode.PassDoors))).ToList();
            if(cells.Count==0)return false;
            if(wave.groupKind==null)
            {
                string[] groups={"Sightstealers","Gorehulks","Chimeras"};
                var candidates=groups.Select(g=>DefDatabase<PawnGroupKindDef>.GetNamedSilentFail(g))
                    .Where(g=>g!=null && Faction.OfEntities.def.pawnGroupMakers.Any(m=>m.kindDef==g)).ToList();
                if(candidates.Count==0)return false;
                wave.groupKind=candidates.RandomElement();
            }
            wave.faction=Faction.OfEntities;
            if(wave.points<=0)wave.points=Math.Max(StorytellerUtility.DefaultThreatPointsNow(map),wave.faction.def.MinPointsToGeneratePawnGroup(wave.groupKind)*1.05f);
            var group=new PawnGroupMakerParms{groupKind=wave.groupKind,tile=map.Tile,faction=wave.faction,points=wave.points};
            List<Pawn> pawns=PawnGroupMakerUtility.GeneratePawns(group).ToList();
            if(pawns.Count==0)return false;
            HediffDef protection=DefDatabase<HediffDef>.GetNamed("YJZ_EntityVoidAdaptation");
            IntVec3 origin=cells.RandomElement();
            foreach(Pawn p in pawns)
            {
                p.health.AddHediff(protection);
                IntVec3 cell=CellFinder.RandomClosewalkCellNear(origin,map,5);
                GenSpawn.Spawn(p,cell,map);
                FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(),map,2f,new Color(.5f,.25f,.65f));
            }
            LordMaker.MakeNewLord(wave.faction,new LordJob_AssaultColony(wave.faction,canKidnap:false,canTimeoutOrFlee:false,canSteal:false),map,pawns);
            wave.enemies=pawns;wave.executed=Now;wave.success=true;
            Find.LetterStack.ReceiveLetter("天外实体显现","节点附近浮现出异象实体。未知力量使它们适应真空，它们正向队伍逼近。",LetterDefOf.ThreatBig,new LookTargets(pawns));
            Log.Message($"[Yunjianzi] {wave.id}: entities={wave.groupKind.defName}, points={wave.points}, pawns={pawns.Count}, tick={Now}");
            return true;
        }
    }

    public class YunjianziSurfaceNodeSite : MapParent
    {
        public override string Label => GameComponent_YunjianziPursuit.Names[GameComponent_YunjianziPursuit.Instance?.State(this)?.index ?? 0];
        public override bool GravShipCanLandOn => true;
        public override AcceptanceReport CanBeSettled => false;
        public override void Abandon(bool wasGravshipLaunch) { }
        public override void PostMapGenerate() {base.PostMapGenerate();YunjianziNodeMap.EnsureNode(this);}
        protected override bool UseGenericEnterMapFloatMenuOption => false;
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions(()=>Spawned,
                ()=>new CaravanArrivalAction_YunjianziNode(this),"进入"+Label,caravan,Tile,this);
        }
        public override string GetInspectString() => base.GetInspectString()+"\n"+YunjianziNodeMap.Status(this);
    }
    public class YunjianziSpaceNodeSite : SpaceMapParent
    {
        public override string Label => GameComponent_YunjianziPursuit.Names[GameComponent_YunjianziPursuit.Instance?.State(this)?.index ?? 2];
        public override bool GravShipCanLandOn => true;
        public override AcceptanceReport CanBeSettled => false;
        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject) {alsoRemoveWorldObject=false;return false;}
        public override void Abandon(bool wasGravshipLaunch) { }
        public override void PostMapGenerate() {base.PostMapGenerate();YunjianziNodeMap.EnsureNode(this);}
        public override string GetInspectString() => base.GetInspectString()+"\n"+YunjianziNodeMap.Status(this);
    }
    public static class YunjianziNodeMap
    {
        public static void EnsureNode(MapParent parent)
        {
            var n=GameComponent_YunjianziPursuit.Instance?.State(parent);
            if(n==null || n.complete || parent.Map.listerThings.AllThings.OfType<Building_YunjianziNode>().Any())return;
            var thing=ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(GameComponent_YunjianziPursuit.NodeDefNames[n.index]));
            Map map=parent.Map;
            IntVec3 location=map.AllCells.Where(c=>GenAdj.OccupiedRect(c,Rot4.North,thing.def.size)
                .All(p=>p.InBounds(map) && p.Standable(map) && p.GetEdifice(map)==null))
                .OrderBy(c=>c.DistanceToSquared(map.Center)).DefaultIfEmpty(IntVec3.Invalid).First();
            if(!location.IsValid) { n.blocked="节点缺少足够的完整落地空间。"; Log.Error("[Yunjianzi] No clear node footprint on "+parent.Label); return; }
            GenSpawn.Spawn(thing,location,map);
            if(n.hitPoints<0)n.hitPoints=thing.MaxHitPoints;
            thing.HitPoints=Math.Max(1,n.hitPoints);
        }
        public static string Status(MapParent parent)
        {
            var n=GameComponent_YunjianziPursuit.Instance?.State(parent);
            if(n==null)return "等待连接数据";
            string progress=GameComponent_YunjianziPursuit.Instance.ProgressText(n.index)+"\n";
            if(n.complete)return progress+"连接已断";
            int bars=n.index<2?GameComponent_YunjianziPursuit.Settings.surfaceBars:GameComponent_YunjianziPursuit.Settings.spaceBars;
            return progress+$"已破坏血条：{n.barsBroken}/{bars}\n"+(n.defending?$"暂不可攻击；守备剩余：{Math.Max(0,n.defenseUntil-GameComponent_YunjianziPursuit.Now).ToStringTicksToPeriod()}\n增援：{n.waves.Count(w=>w.success)}/{n.waves.Count}；仍须解除本轮敌人威胁":"连接可攻击")+(n.blocked==null?"":"\n"+n.blocked);
        }
    }
    public sealed class CaravanArrivalAction_YunjianziNode : CaravanArrivalAction
    {
        private MapParent site;
        public CaravanArrivalAction_YunjianziNode() { }
        public CaravanArrivalAction_YunjianziNode(MapParent site) {this.site=site;}
        public override string Label => "进入"+site.Label;
        public override string ReportString => "正在前往"+site.Label;
        public override FloatMenuAcceptanceReport StillValid(Caravan caravan,PlanetTile tile)
            => site!=null && site.Spawned && site.Tile==tile;
        public override void ExposeData() {base.ExposeData();Scribe_References.Look(ref site,"site");}
        public override void Arrived(Caravan caravan)
        {
            LongEventHandler.QueueLongEvent(()=>
            {
                Map map=GetOrGenerateMapUtility.GetOrGenerateMap(site.Tile,new IntVec3(180,1,180),null);
                YunjianziNodeMap.EnsureNode(site);
                CaravanEnterMapUtility.Enter(caravan,map,CaravanEnterMode.Edge,CaravanDropInventoryMode.DoNotDrop,true);
            },"GeneratingMapForNewEncounter",false,null);
        }
    }
    public sealed class QuestPart_YunjianziNode : QuestPart
    {
        public int nodeIndex;
        private MapParent Site => GameComponent_YunjianziPursuit.Instance?.nodes.FirstOrDefault(n=>n.index==nodeIndex)?.site;
        public override string DescriptionPart => Site==null
            ? GameComponent_YunjianziPursuit.Instance?.ProgressText(nodeIndex)+"\n正在恢复任务地点"
            : YunjianziNodeMap.Status(Site);
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets {get {if(Site!=null)yield return new GlobalTargetInfo(Site);}}
        public override IEnumerable<GlobalTargetInfo> QuestSelectTargets => QuestLookTargets;
        public override void ExposeData() {base.ExposeData();Scribe_Values.Look(ref nodeIndex,"nodeIndex");}
    }
    public sealed class GenStep_YunjianziPlatform : GenStep
    {
        public override int SeedPart => 819210;
        public override void Generate(Map map,GenStepParams parms)
        {
            TerrainDef floor=DefDatabase<TerrainDef>.GetNamed("MechanoidPlatform");
            foreach(IntVec3 c in CellRect.CenteredOn(map.Center,35).ClipInsideMap(map)) map.terrainGrid.SetTerrain(c,floor);
        }
    }
    public sealed partial class Building_YunjianziNode : Building
    {
        private YunjianziNodeState State => Spawned?GameComponent_YunjianziPursuit.Instance?.State(Map.Parent):null;
        public override void PreApplyDamage(ref DamageInfo dinfo,out bool absorbed)
        {
            // Absorb before the vanilla damage worker can destroy the building or spill damage into another bar.
            absorbed=true;
            var n=State;if(n==null || n.complete || n.defending)return;
            n.hitPoints=YunjianziPursuitRules.RemainingHealth(n.hitPoints<0?MaxHitPoints:n.hitPoints,dinfo.Amount,n.defending,n.complete);
            HitPoints=Math.Max(1,n.hitPoints);
            if(n.hitPoints==0)GameComponent_YunjianziPursuit.Instance.BreakBar(n);
        }
        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            var n=State;if(n!=null && !n.defending && !n.complete && n.hitPoints<0)n.hitPoints=MaxHitPoints;
            if(n!=null)HitPoints=Math.Max(1,n.hitPoints);
        }
        public override void Destroy(DestroyMode mode=DestroyMode.Vanish)
        {
            if(State!=null && !State.complete)return;
            base.Destroy(mode);
        }
        public override string GetInspectString() => base.GetInspectString()+"\n"+YunjianziNodeMap.Status(Map.Parent);
    }
}
