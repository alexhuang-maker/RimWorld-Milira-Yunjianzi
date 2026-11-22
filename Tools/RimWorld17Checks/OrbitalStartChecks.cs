using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using UnityEngine;

namespace Yunjianzi
{
    [StaticConstructorOnStartup]
    internal static class OrbitalStartChecks
    {
        static bool started,finished,reviewDone;
        static int frames;
        static int phase,startTick;
        static int relicCount;
        static int[] inheritedLevels;
        static int jadeBeforeOpening;
        static int rewardArkId;
        static readonly List<string> lines=new List<string>();
        static string seed="YJZ-Orbital-01";
        static int size=150;
        static string FilePath=>Path.Combine(GenFilePaths.SaveDataFolderPath,"orbital-audit.txt");
        static readonly List<string> errors=new List<string>();
        static OrbitalStartChecks()
        {
            if(!GenCommandLine.CommandLineArgPassed("YJZOrbitalAudit"))return;
            var h=new Harmony("yunjianzi.orbital.audit");
            h.Patch(AccessTools.Method(typeof(UIRoot_Entry),nameof(UIRoot_Entry.UIRootUpdate)),postfix:new HarmonyMethod(typeof(OrbitalStartChecks),nameof(Entry)));
            h.Patch(AccessTools.Method(typeof(Root_Play),"SetupForQuickTestPlay"),prefix:new HarmonyMethod(typeof(OrbitalStartChecks),nameof(Setup)));
            h.Patch(AccessTools.Method(typeof(Root_Play),"Update"),postfix:new HarmonyMethod(typeof(OrbitalStartChecks),nameof(Play)));
            foreach(var m in typeof(Log).GetMethods().Where(m=>m.Name=="Error" && m.GetParameters().Length>0 && m.GetParameters()[0].ParameterType==typeof(string)))
                h.Patch(m,prefix:new HarmonyMethod(typeof(OrbitalStartChecks),nameof(Error)));
        }
        static void Error(string __0){if(errors.Count<300)errors.Add(__0);}
        static void Entry()
        {
            if(started || Find.ScreenshotModeHandler==null)return;
            started=true;Prefs.RunInBackground=true;Application.runInBackground=true;
            if(GenCommandLine.CommandLineArgPassed("YJZSanctumPreview"))
            {GameDataSaveLoader.LoadGame("孤云遗冢_研读后验证");return;}
            if(GenCommandLine.CommandLineArgPassed("YJZArkRecovery"))
            {GameDataSaveLoader.LoadGame("孤云遗冢_失败诊断");return;}
            if(GenCommandLine.CommandLineArgPassed("YJZMonumentCatalog") || GenCommandLine.CommandLineArgPassed("YJZInheritanceReview") || GenCommandLine.CommandLineArgPassed("YJZReviewFull"))
            {GameDataSaveLoader.LoadGame("孤云遗冢_研读前验证");return;}
            if(GenCommandLine.CommandLineArgPassed("YJZDockingRegression"))
            {
                seed="YJZ-Docking-Regression";size=100;
                GameDataSaveLoader.LoadGame("孤云遗冢_失败诊断");return;
            }
            Current.Game=null;UnityEngine.SceneManagement.SceneManager.LoadScene("Play");
        }
        static bool Setup()
        {
            Current.ProgramState=ProgramState.Entry;Game.ClearCaches();
            Current.Game=new Game();Current.Game.InitData=new GameInitData();
            Current.Game.Scenario=DefDatabase<ScenarioDef>.GetNamed("YJZ_YunjianziAnomalyStart").scenario;
            Log.Message("[YJZ Orbital Audit] PARTS "+string.Join(",",Find.Scenario.AllParts.Select(p=>p.GetType().Name)));
            Log.Message("[YJZ Orbital Audit] STEPS "+string.Join(",",DefDatabase<MapGeneratorDef>.GetNamed("YJZ_OrbitalMemorial").genSteps.Select(g=>g.defName)));
            Find.Scenario.PreConfigure();
            Current.Game.storyteller=new Storyteller(StorytellerDefOf.Cassandra,DifficultyDefOf.Rough);
            if(GenCommandLine.TryGetCommandLineArg("YJZSeed",out var argSeed))seed=argSeed;
            if(GenCommandLine.TryGetCommandLineArg("YJZMapSize",out var argSize))size=int.Parse(argSize);
            Current.Game.World=WorldGenerator.GenerateWorld(0.3f,seed,OverallRainfall.Normal,OverallTemperature.Normal,OverallPopulation.Normal,LandmarkDensity.Normal);
            Find.GameInitData.ChooseRandomStartingTile();
            // WorldGenerator already invokes Scenario.PostWorldGenerate. Calling
            // it again creates a second player faction; only reselect the forced
            // orbital tile after QuickTest's surface-tile default.
            foreach(var part in Find.Scenario.AllParts.OfType<ScenPart_ForcedMap>())part.PostWorldGenerate();
            Find.TickManager.gameStartAbsTick=GenTicks.ConfiguredTicksAbsAtGameStart;
            Find.GameInitData.mapSize=size;Find.Scenario.PostIdeoChosen();
            Log.Message("[YJZ Orbital Audit] STARTPAWNS "+Find.GameInitData.startingAndOptionalPawns.Count);
            return false;
        }
        static void Play()
        {
            if(!started || finished || Current.ProgramState!=ProgramState.Playing || LongEventHandler.AnyEventNowOrWaiting)return;
            if(++frames<90)return;
            try
            {
                Map map=Find.CurrentMap;
                if(GenCommandLine.CommandLineArgPassed("YJZSanctumPreview"))
                {
                    if(frames==90)
                    {
                        Find.TickManager.CurTimeSpeed=TimeSpeed.Paused;
                        foreach(var window in Find.WindowStack.Windows.ToList())Find.WindowStack.TryRemove(window,false);
                        Find.ScreenshotModeHandler.Active=true;
                        Find.CameraDriver.SetRootPosAndSize(GenStep_YunjianziOrbitalFoundation.Shrine(map).ToVector3Shifted(),19);
                        var inner=map.GetComponent<MapComponent_YunjianziMemorial>().altar.GetRoom(map);
                        var outer=GenStep_YunjianziOrbitalFoundation.Shrine(map).GetRoom(map);
                        Require(inner!=outer && inner.Temperature>18 && outer.Temperature>18,"Inner/outer hall heating failed");
                        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath,"sanctum-preview.txt"),"PASS separate heated halls: inner="+inner.Temperature+" outer="+outer.Temperature);
                    }
                    if(frames==120)ScreenshotTaker.TakeNonSteamShot("inner-sanctum-revealed");
                    if(frames<150)return;
                    finished=true;Application.Quit();return;
                }
                if(phase==0 && GenCommandLine.CommandLineArgPassed("YJZArkRecovery"))
                {
                    var recoveringPawn=map.mapPawns.FreeColonistsSpawned.Single(YunjianziSoleColonistUtility.IsYunjianzi);
                    inheritedLevels=recoveringPawn.skills.skills.Select(s=>s.levelInt).ToArray();
                    relicCount=map.listerThings.AllThings.Count(t=>t.def.defName.StartsWith("YJZ_Relic_"));
                    phase=6;
                    lines.Add("RESUME post-study corner-ark fixture for actual chest opening, temperature recovery and departure");
                }
                if(phase==0 && GenCommandLine.CommandLineArgPassed("YJZReviewFull") && !reviewDone)
                {
                    ReviewInheritance(map);reviewDone=true;frames=0;
                    GameDataSaveLoader.LoadGame("孤云遗冢_研读前验证");return;
                }
                if(GenCommandLine.CommandLineArgPassed("YJZInheritanceReview"))
                {
                    if(frames==90)
                    {
                        ReviewInheritance(map);
                        Find.ScreenshotModeHandler.Active=true;
                        Find.CameraDriver.SetRootPosAndSize(GenStep_YunjianziOrbitalFoundation.Dock(map).ToVector3Shifted(),15);
                    }
                    if(frames==125)ScreenshotTaker.TakeNonSteamShot("inheritance-review-ship-and-dock");
                    if(frames<150)return;
                    finished=true;Application.Quit();return;
                }
                if(phase==0 && GenCommandLine.CommandLineArgPassed("YJZMonumentCatalog"))
                { ExportMonumentCatalog();finished=true;Application.Quit();return; }
                if(phase==4)
                {
                    var marker=Find.Maps.SelectMany(m=>m.listerThings.AllThings).OfType<GravshipLandingMarker>().FirstOrDefault();
                    Require(marker!=null,"No landing marker");
                    marker.BeginLanding(Find.GravshipController);phase=5;frames=0;return;
                }
                if(phase==5 && WorldComponent_GravshipController.CutsceneInProgress)return;
                foreach(var window in Find.WindowStack.Windows.ToList())Find.WindowStack.TryRemove(window,false);
                var pawn=map.mapPawns.FreeColonistsSpawned.Single(YunjianziSoleColonistUtility.IsYunjianzi);
                var altar=map.GetComponent<MapComponent_YunjianziMemorial>().altar;
                var scrolls=map.listerThings.ThingsOfDef(YJZDefOf.YJZ_Item_BoundImmortalScroll);
                var engine=map.listerThings.AllThings.OfType<Building_GravEngine>().Single();
                var cultivation=pawn.TryGetComp<CompYunjianziCultivation>();
                if(phase==0 && GenCommandLine.CommandLineArgPassed("YJZDockingRegression"))
                {
                    Require(!pawn.CanReach(altar,PathEndMode.Touch,Danger.Deadly),"Regression fixture no longer reproduces blocked exit");
                    AccessTools.Method(typeof(MapComponent_YunjianziMemorial),"EnsureDockingHatch").Invoke(map.GetComponent<MapComponent_YunjianziMemorial>(),null);
                    Require(pawn.CanReach(altar,PathEndMode.Touch,Danger.Deadly),"Docking hatch did not restore route");
                    lines.Add("PASS previously blocked exit restored by pressurized docking hatch");
                }
                if(phase==0 || phase==1)
                {
                    Require(cultivation.AwaitingInheritance && cultivation.Realm==0,"Cultivation unlocked before reading");
                    Require(pawn.Name is NameTriple name && name.First=="西娅" && name.Last=="阿纳斯塔西亚" && !name.NickSet,"Initial name incorrect");
                    Require(pawn.story.Adulthood.defName=="YJZ_Backstory_Explorer" && !pawn.story.traits.HasTrait(YJZDefOf.YJZ_Trait_CloudseaSwordHeart),"Inherited story/trait before reading");
                    Require(pawn.apparel.WornApparel.Count==1 && pawn.apparel.WornApparel.Any(a=>a.def.defName=="Milira_Cloak" && a.Stuff.defName=="Milira_FeatherThread"),"Incorrect starting clothes / hood still present");
                    Require(!cultivation.CompGetGizmosExtra().OfType<Command_Action>().Any(g=>g.defaultLabel=="研读前代手记"),"Old reading gizmo remains");
                    Require(scrolls[0].Faction==null && scrolls[0].IsForbidden(pawn),"Scroll owned or auto-haulable");
                    Require(scrolls[0].GetFloatMenuOptions(pawn).Count(o=>o.Label=="研读传承宝卷" && o.action!=null)==1,"Single-pawn right-click reading action missing or duplicated");
                    var sealedChest=map.listerThings.AllThings.OfType<Building_YunjianziInheritanceChest>().Single();
                    sealedChest.Open();
                    Require(!sealedChest.CanOpen && !sealedChest.RewardsCreated && sealedChest.def.size==IntVec2.One && sealedChest.def.building.maxItemsInCell==30 && sealedChest.def.Minifiable,"Chest lock/capacity/portability incorrect");
                    Require(!sealedChest.Accepts(ThingMaker.MakeThing(ThingDefOf.Steel)),"Sealed chest accepts storage");
                    var shrine=GenStep_YunjianziOrbitalFoundation.Shrine(map);
                    Require(!map.listerThings.AllThings.Any(t=>t.def.defName=="YJZ_MemorialLifeSupport" && CellRect.CenteredOn(shrine,35,31).Contains(t.Position)),"Life-support pump inside mausoleum");
                    var exteriorDoors=YunjianziMemorialLayout.Cells().Where(c=>c.Value=='D' && GenAdj.CardinalDirections.Any(d=>YunjianziMemorialLayout.OutsidePlan(c.Key+d))).ToList();
                    Require(exteriorDoors.Count==1 && exteriorDoors[0].Key==new IntVec3(0,0,-15),"Memorial has extra exterior entrances");
                    Require(map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("YJZ_PredecessorCenotaph")).Single().Position==shrine+YunjianziMemorialLayout.SteleOffset,"Stele not at rear of sanctum");
                    Require(!map.listerThings.AllThings.Any(t=>t.def.defName.StartsWith("YJZ_Relic_") && CellRect.CenteredOn(shrine,35,31).Overlaps(t.OccupiedRect())),"Relic inside intact mausoleum");
                    var cushion=map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("YJZ_Building_MeditationCushion")).Single();
                    Require(cushion.def.Minifiable && cushion.Position==altar+new IntVec3(0,0,-2),"Portable basic cushion misplaced");
                    Require(YunjianziMemorialLayout.Cells().All(c=>(shrine+c.Key).GetTerrain(map).defName=="TileMarble"),"Memorial floor is not uniformly ordinary marble");
                    var shelves=map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("Shelf")).OfType<Building_Storage>().Where(t=>CellRect.CenteredOn(shrine,35,31).Contains(t.Position)).ToList();
                    Require(shelves.Count==8 && shelves.All(t=>t.Stuff.defName=="BlocksMarble"),"Marble shelves missing");
                    Require(shelves.Select(t=>t.GetRoom()).Distinct().Count()==4,"Four distinct treasure side halls missing");
                    Require(shelves.All(t=>pawn.CanReach(t,PathEndMode.Touch,Danger.Deadly) && !t.GetRoom().ExposedToSpace),"Side hall shelf inaccessible or unsealed");
                    Require(map.listerThings.ThingsOfDef(ThingDefOf.SteleGrand).Count==2 && map.listerThings.ThingsOfDef(ThingDefOf.SteleLarge).Count==2,"Vanilla monument focal pieces missing");
                    lines.Add("PASS single main entrance, four sealed treasure side halls, eight reachable marble shelves stocked across four rooms");
                    Require(altar.GetRoom(map)!=shrine.GetRoom(map) && !altar.GetRoom(map).ExposedToSpace,"Sanctum is not a separate sealed room");
                    Require((shrine+new IntVec3(0,0,3)).GetEdifice(map) is Building_Door,"Inner hall entrance missing");
                    foreach(var wall in YunjianziMemorialLayout.Cells().Where(c=>c.Value=='#' && YunjianziMemorialLayout.Sanctum.Contains(c.Key)))
                        Require((shrine+wall.Key).GetEdifice(map)?.def==ThingDefOf.Wall,"Chamfered sanctum wall has a gap");
                    Require(pawn.CanReach(altar,PathEndMode.Touch,Danger.Deadly) && pawn.CanReach(cushion,PathEndMode.Touch,Danger.Deadly),"Inner hall inheritance route blocked");
                    lines.Add("PASS separate rear sanctum, complete marble walls, reachable altar and cushion");
                    Require(pawn.story.HairColor==Color.white,"Initial hair tint not white");
                    if(phase==1)CapturePortrait(pawn,"before-inheritance");
                    lines.Add("PASS sealed inheritance, explorer story, no trait/realm, feather-thread outfit, 1x1/30-stack chest, intact marble hall");
                    if(GenCommandLine.CommandLineArgPassed("YJZSupplyChest"))
                    {
                        var supplies=sealedChest.GetDirectlyHeldThings();
                        Require(supplies.Count==0 && sealedChest.StoredStackCount==0,"Starting materials still packed into chest");
                        CheckMemorialSupplies(map);
                        foreach(var equipment in new[]{"Milian_Recharger","Milira_UniversalBench"})
                        {
                            Require(!map.listerThings.AllThings.OfType<MinifiedThing>().Any(t=>t.InnerThing.def.defName==equipment),"Spare device generated: "+equipment);
                            Require(map.listerThings.AllThings.Any(t=>t is Building && t.def.defName==equipment && engine.ValidSubstructure.Contains(t.Position)),"Installed ship equipment missing: "+equipment);
                        }
                        foreach(var pair in new Dictionary<string,int>{{"Milira_SunPlateSteel",150},{"Milira_SolarCrystal",25},{"Milira_SunLightFuel",100},{"MedicineIndustrial",20}})
                            Require(map.listerThings.AllThings.Where(t=>t.def.defName==pair.Key).Sum(t=>t.stackCount)==pair.Value,"Ship supplies changed: "+pair.Key);
                        // Colonists may have already collected/eaten a meal
                        // between the initial snapshot and a save reload.
                        Require(map.listerThings.AllThings.Any(t=>t.def.defName=="Milira_TravellerFood") && !supplies.Any(t=>t.def.defName=="Milira_TravellerFood"),"Food not available aboard");
                        Require(!map.listerThings.AllThings.Any(t=>(t is MinifiedThing m?m.InnerThing:t).def.defName=="Bedroll"),"Redundant bedroll generated");
                        if(phase==1)
                        {
                            int count=supplies.Count;var position=sealedChest.Position;
                            var packed=sealedChest.MakeMinified();GenSpawn.Spawn(packed,position,map);
                            packed.GetDirectlyHeldThings().Remove(sealedChest);packed.Destroy();GenSpawn.Spawn(sealedChest,position,map);
                            Require(sealedChest.GetDirectlyHeldThings().Count==count && !sealedChest.RewardsCreated,"Sealed supplies lost on minification");
                        }
                        lines.Add("PASS shelf supplies persist; chest empty until rewards; no spare devices; installed ship equipment and Milira supplies preserved");
                    }
                    var relics=map.listerThings.AllThings.Where(t=>t.def.defName.StartsWith("YJZ_Relic_")).ToList();
                    if(phase==0)relicCount=relics.Count;
                    Require(relics.Count==relicCount && relics.Count>=16 && relics.Select(t=>t.def).Distinct().Count()==8,"Missing or duplicated relic variants");
                    Require(relics.All(t=>t is Building && t.def.tickerType==TickerType.Never && !t.def.bringAlongOnGravship),"Relics are not inert stationary scenery");
                    lines.Add("RELICS "+relics.Count+" "+string.Join(",",relics.GroupBy(t=>t.def.defName).Select(g=>g.Key+":"+g.Count())));
                    Require(map.Tile.LayerDef==PlanetLayerDefOf.Orbit,"Not in Orbit");
                    Require(scrolls.Count==1 && scrolls[0].Position==altar,"Scroll not uniquely at altar: "+string.Join(",",scrolls.Select(s=>s.Position))+" expected "+altar);
                    Require(scrolls[0].TryGetComp<CompYunjianziBoundScroll>().IsBoundTo(pawn),"Scroll not bound");
                    Require(pawn.CanReach(scrolls[0],PathEndMode.Touch,Danger.Deadly),"Altar unreachable");
                    if(GenCommandLine.CommandLineArgPassed("YJZOutdoorDock"))
                    {
                        var dock=GenStep_YunjianziOrbitalFoundation.Dock(map);
                        foreach(var c in CellRect.CenteredOn(dock,25,25).EdgeCells.Where(c=>c.z<dock.z+12))
                            Require(!c.Roofed(map) && c.GetEdifice(map)?.def!=ThingDefOf.Wall,"Landing apron perimeter enclosed at "+c+" edifice="+c.GetEdifice(map));
                        var apron=dock+new IntVec3(-11,0,0);
                        Require(apron.GetRoom(map).ExposedToSpace && !apron.Roofed(map),"Landing apron is indoors");
                        var airlock=dock+new IntVec3(0,0,17);
                        Require(!airlock.GetRoom(map).ExposedToSpace && airlock.GetRoom(map).Vacuum<0.2f,"Mausoleum airlock unsealed");
                        var thrusters=map.listerThings.AllThings.Select(t=>t.TryGetComp<CompGravshipThruster>()).Where(t=>t!=null).ToList();
                        Require(thrusters.Count==4 && thrusters.All(t=>CompGravshipThruster.IsOutdoors(t.parent.def,map,t.parent.Position,t.parent.Rotation)),"Thruster exhaust not outdoors");
                        lines.Add("PASS outdoor roofless landing apron, sealed mausoleum airlock and four outdoor thrusters");
                    }
                    Require(map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("YJZ_PredecessorCenotaph")).Count==1,"Cenotaph missing");
                    Require(map.mapPawns.AllPawns.Count(p=>p.kindDef.defName=="YJZ_Shenji")==0,"Companion granted before ark opening");
                    var arks=map.listerThings.AllThings.OfType<Building_YunjianziStartingShenjiArk>().ToList();
                    Require(arks.Count==6 && arks.All(a=>!a.CanOpen && !a.Opened),"Arks unlocked before inheritance");
                    foreach(var sealedArk in arks)
                    {
                        sealedArk.Open();
                        Require(!sealedArk.Opened,"Direct Open bypassed inheritance lock");
                        CheckOpeningMenu(sealedArk,pawn,false,"开启碎片方舟");
                    }
                    CheckOpeningMenu(sealedChest,pawn,false,"打开传承宝箱");
                    Require(map.mapPawns.AllPawns.Count(p=>p.kindDef.defName=="YJZ_Shenji")==0,"Locked ark granted companion");
                    lines.Add("PASS six arks locked before inheritance, direct Open blocked, no duplicate opening entries");
                    var ark=arks.Single(a=>a.HasCompanionReward);
                    if(phase==0)rewardArkId=ark.thingIDNumber;
                    Require(ark.thingIDNumber==rewardArkId,"Reward ark rerolled on reload");
                    var left=arks.Where(a=>a.Position.x<shrine.x).ToList();
                    var right=arks.Where(a=>a.Position.x>shrine.x).ToList();
                    Require(left.Count==3 && right.Count==3,"Arks not three per side");
                    var rightCells=new HashSet<IntVec3>(right.SelectMany(a=>a.OccupiedRect().Cells));
                    Require(left.SelectMany(a=>a.OccupiedRect().Cells).All(c=>rightCells.Contains(new IntVec3(2*shrine.x-c.x,0,c.z))),"Ark footprints not mirrored");
                    Require(arks.Select(a=>a.Position.z).Distinct().Count()==3 && arks.GroupBy(a=>a.Position.z).All(g=>g.Count()==2),"Ark rows misaligned");
                    Require(arks.All(a=>pawn.CanReach(a,PathEndMode.Touch,Danger.Deadly)),"An ark cannot be reached");
                    Require(arks.All(a=>a.OccupiedRect().Cells.All(c=>c.GetEdifice(map)==a)),"Ark overlaps another structure");
                    lines.Add("PASS six reachable arks, three mirrored pairs, one saved companion reward");
                    foreach(var c in new[]{pawn.Position,altar,GenStep_YunjianziOrbitalFoundation.Dock(map)})
                    {
                        var room=c.GetRoom(map);lines.Add("ROOM "+c+" cells="+room?.CellCount+" vacuum="+room?.Vacuum+" temp="+room?.Temperature+" openRoof="+room?.OpenRoofCount);
                        Require(room!=null && room.Vacuum<0.2f && room.Temperature>10f,"Unsafe initial room");
                    }
                    lines.Add("PASS generation/reload phase="+phase+" seed="+seed+" mapSize="+size);
                    if(phase==0)
                    {
                        GameDataSaveLoader.SaveGame("孤云遗冢_研读前验证");
                        phase=1;frames=0;Flush();GameDataSaveLoader.LoadGame("孤云遗冢_研读前验证");return;
                    }
                    phase=2;frames=0;startTick=Find.TickManager.TicksGame;
                    Find.CameraDriver.SetRootPosAndSize(shrine.ToVector3Shifted(),19);
                    Find.ScreenshotModeHandler.Active=true;
                    scrolls[0].GetFloatMenuOptions(pawn).Single(o=>o.Label=="研读传承宝卷").action();
                    Find.TickManager.CurTimeSpeed=TimeSpeed.Superfast;Flush();return;
                }
                if(phase==12)
                {
                    var arks=map.listerThings.AllThings.OfType<Building_YunjianziStartingShenjiArk>().ToList();
                    Require(arks.Count==6 && arks.All(a=>a.CanOpen && !a.Opened),"Inheritance did not unlock all arks after reload");
                    Require(arks.Single(a=>a.HasCompanionReward).thingIDNumber==rewardArkId,"Reward ark changed during inheritance");
                    foreach(var ark in arks)CheckOpeningMenu(ark,pawn,true,"开启碎片方舟");
                    CheckOpeningMenu(map.listerThings.AllThings.OfType<Building_YunjianziInheritanceChest>().Single(),pawn,true,"打开传承宝箱");
                    lines.Add("PASS post-inheritance: all arks and chest have exactly one actual right-click opening action");
                    phase=10;frames=0;startTick=Find.TickManager.TicksGame;
                    CheckOpeningMenu(arks.First(a=>!a.HasCompanionReward),pawn,true,"开启碎片方舟").action();
                    Find.TickManager.CurTimeSpeed=TimeSpeed.Superfast;Flush();return;
                }
                if(phase==10)
                {
                    if(frames==120)ScreenshotTaker.TakeNonSteamShot("inner-sanctum-layout");
                    if(frames<150)return;
                    var arks=map.listerThings.AllThings.OfType<Building_YunjianziStartingShenjiArk>().ToList();
                    if(!arks.Any(a=>a.Opened) && Find.TickManager.TicksGame-startTick<2500)return;
                    Require(arks.Any(a=>a.Opened && !a.HasCompanionReward),"Empty ark actual opening failed");
                    foreach(var empty in arks.Where(a=>!a.HasCompanionReward)){empty.Open();empty.Open();}
                    Require(arks.Count(a=>a.Opened)==5 && map.mapPawns.AllPawns.Count(p=>p.kindDef.defName=="YJZ_Shenji")==0,"Empty ark granted a companion");
                    lines.Add("PASS all five empty arks open without granting any companion");
                    Find.TickManager.CurTimeSpeed=TimeSpeed.Paused;
                    GameDataSaveLoader.SaveGame("孤云遗冢_空方舟验证");phase=11;frames=0;Flush();GameDataSaveLoader.LoadGame("孤云遗冢_空方舟验证");return;
                }
                if(phase==11)
                {
                    var arks=map.listerThings.AllThings.OfType<Building_YunjianziStartingShenjiArk>().ToList();
                    var ark=arks.Single(a=>a.HasCompanionReward);
                    Require(arks.Count(a=>a.Opened)==5 && !ark.Opened && ark.thingIDNumber==rewardArkId,"Empty/occupied ark states changed on reload");
                    phase=8;frames=0;startTick=Find.TickManager.TicksGame;
                    CheckOpeningMenu(ark,pawn,true,"开启碎片方舟").action();
                    Find.TickManager.CurTimeSpeed=TimeSpeed.Superfast;Flush();return;
                }
                if(phase==8)
                {
                    var ark=map.listerThings.AllThings.OfType<Building_YunjianziStartingShenjiArk>().Single(a=>a.HasCompanionReward);
                    if(!ark.Opened && Find.TickManager.TicksGame-startTick<2500)return;
                    Require(ark.Opened,"Ark opening job failed: "+pawn.CurJobDef);
                    ark.Open();
                    Require(map.mapPawns.AllPawns.Count(p=>p.kindDef.defName=="YJZ_Shenji")==1,"Ark duplicated or failed to grant companion");
                    Require(scrolls.Count==0 && cultivation.HasOpenedBoundScroll,"Ark opened without completed inheritance");
                    lines.Add("PASS post-inheritance ark opening grants one equipped companion; repeat opening cannot duplicate");
                    Find.TickManager.CurTimeSpeed=TimeSpeed.Paused;
                    GameDataSaveLoader.SaveGame("孤云遗冢_方舟验证");phase=9;frames=0;Flush();GameDataSaveLoader.LoadGame("孤云遗冢_方舟验证");return;
                }
                if(phase==9)
                {
                    var ark=map.listerThings.AllThings.OfType<Building_YunjianziStartingShenjiArk>().Single(a=>a.HasCompanionReward);
                    Require(ark.Opened && !ark.CanOpen && map.mapPawns.AllPawns.Count(p=>p.kindDef.defName=="YJZ_Shenji")==1,"Ark state lost on reload");
                    Require(map.listerThings.AllThings.OfType<Building_YunjianziStartingShenjiArk>().All(a=>a.Opened && !a.CanOpen),"Empty ark reopened after save");
                    Find.CameraDriver.SetRootPosAndSize(GenStep_YunjianziOrbitalFoundation.Shrine(map).ToVector3Shifted(),19);
                    Find.ScreenshotModeHandler.Active=true;
                    phase=6;frames=0;
                    CheckOpeningMenu(ark,pawn,false,"开启碎片方舟");
                    Find.TickManager.CurTimeSpeed=TimeSpeed.Paused;Flush();return;
                }
                if(phase==2)
                {
                    if(frames==90)ScreenshotTaker.TakeNonSteamShot("orbital-memorial-"+seed);
                    Find.TickManager.CurTimeSpeed=TimeSpeed.Superfast;
                    if(Find.TickManager.TicksGame-startTick<2500 && frames<15000)return;
                    Require(Find.TickManager.TicksGame-startTick>=2500,"Simulation did not advance one game hour");
                    Require(scrolls.Count==0,"Read job did not consume scroll; job="+pawn.CurJobDef+" pawn="+pawn.Position);
                    Require(cultivation.Realm==1 && cultivation.HasOpenedBoundScroll && !cultivation.AwaitingInheritance,"Reading did not unlock cultivation");
                    Require(cultivation.EnergyMax>=10 && cultivation.EnergyMax<100 && !cultivation.CanBreakthrough,"Inheritance skipped initial cultivation");
                    Require(pawn.Name is NameTriple inheritedName && inheritedName.Nick=="云间子" && inheritedName.First=="西娅","Inherited name incorrect");
                    Require(pawn.story.Adulthood==YJZDefOf.YJZ_Backstory_Adulthood && pawn.story.traits.allTraits.Count(t=>t.def==YJZDefOf.YJZ_Trait_CloudseaSwordHeart)==1,"Inherited story/trait missing or duplicated");
                    inheritedLevels=pawn.skills.skills.Select(s=>s.levelInt).ToArray();
                    var chest=map.listerThings.AllThings.OfType<Building_YunjianziInheritanceChest>().Single();
                    Require(chest.CanOpen,"Chest did not unlock");
                    foreach(var p in map.mapPawns.AllPawnsSpawned.Where(p=>p.Faction==Faction.OfPlayer))
                    {
                        lines.Add("HEALTH "+p.kindDef+" "+string.Join(",",p.health.hediffSet.hediffs.Select(h=>h.def.defName+":"+h.Severity)));
                        // No hood and an outdoor apron deliberately permit
                        // transient cold. Vanilla's first capacity penalty
                        // starts at 0.04; log lesser exposure and check recovery.
                        Require(!p.Dead && !p.Downed && !p.health.hediffSet.hediffs.Any(h=>
                            h.def.defName=="VacuumExposure" || (h.def.defName=="Hypothermia" && h.Severity>=0.04f)),"Environmental harm with capacity loss");
                    }
                    lines.Add("PASS read job completed and 2500 ticks survived");
                    Find.TickManager.CurTimeSpeed=TimeSpeed.Paused;
                    GameDataSaveLoader.SaveGame("孤云遗冢_研读后验证");
                    phase=12;frames=0;Flush();GameDataSaveLoader.LoadGame("孤云遗冢_研读后验证");return;
                }
                if(phase==6)
                {
                    if(frames==90)
                    {
                        Find.TickManager.CurTimeSpeed=TimeSpeed.Paused;
                        Find.ScreenshotModeHandler.Active=true;
                        map.fogGrid.ClearAllFog();
                        Find.CameraDriver.SetRootPosAndSize(GenStep_YunjianziOrbitalFoundation.Shrine(map).ToVector3Shifted(),19);
                    }
                    if(frames<120)return;
                    if(frames==120){ScreenshotTaker.TakeNonSteamShot("four-halls-memorial");return;}
                    Require(inheritedLevels.SequenceEqual(pawn.skills.skills.Select(s=>s.levelInt)),"Skill gains changed on reload");
                    var chest=map.listerThings.AllThings.OfType<Building_YunjianziInheritanceChest>().Single();
                    Require(chest.CanOpen && cultivation.HasOpenedBoundScroll,"Unlock lost on reload");
                    var companion=map.mapPawns.AllPawnsSpawned.Single(p=>p.kindDef.defName=="YJZ_Shenji");
                    CheckOpeningMenu(chest,pawn,true,"打开传承宝箱",new List<Pawn>{pawn,companion});
                    lines.Add("PASS human/companion multiselect keeps one chest opening entry");
                    CapturePortrait(pawn,"after-inheritance");
                    jadeBeforeOpening=CountJade(map);
                    CheckOpeningMenu(chest,pawn,true,"打开传承宝箱").action();
                    phase=7;frames=0;startTick=Find.TickManager.TicksGame;Find.TickManager.CurTimeSpeed=TimeSpeed.Superfast;return;
                }
                if(phase==7)
                {
                    var chest=map.listerThings.AllThings.OfType<Building_YunjianziInheritanceChest>().Single();
                    if(!chest.RewardsCreated && Find.TickManager.TicksGame-startTick<1500)return;
                    Require(chest.RewardsCreated && !chest.CanOpen,"Actual open job failed: "+pawn.CurJobDef);
                    CheckOpeningMenu(chest,pawn,false,"打开传承宝箱");
                    lines.Add("WARMUP altarTemp="+altar.GetRoom(map)?.Temperature+" pawnTemp="+pawn.Position.GetRoom(map)?.Temperature+" cold="+
                        (pawn.health.hediffSet.hediffs.FirstOrDefault(h=>h.def.defName=="Hypothermia")?.Severity??0));
                    Require(map.listerThings.ThingsOfDef(YJZDefOf.YJZ_Apparel_YunjianRobe).Count==1,"Robe reward not unique");
                    Require(CountJade(map)==jadeBeforeOpening+75,"Jade reward is not one stack");
                    if(GenCommandLine.CommandLineArgPassed("YJZSupplyChest"))
                    {
                        Require(chest.GetDirectlyHeldThings().Count==0 && chest.StoredStackCount==2,"Chest should hold exactly two reward stacks");
                        Require(chest.slotGroup.HeldThings.Count(t=>t.def==YJZDefOf.YJZ_Apparel_YunjianRobe && t.stackCount==1)==1,"Chest robe reward missing");
                        Require(chest.slotGroup.HeldThings.Where(t=>t.def==ThingDefOf.Jade).Sum(t=>t.stackCount)==75,"Chest jade reward must be exactly 75");
                        CheckMemorialSupplies(map);
                        lines.Add("PASS chest contains only robe and 75 reward jade; shelf materials unchanged");
                    }
                    chest.Open();
                    Require(map.listerThings.ThingsOfDef(YJZDefOf.YJZ_Apparel_YunjianRobe).Count==1,"Reward duplicated on second open");
                    IntVec3 position=chest.Position;
                    var minified=chest.MakeMinified();
                    Require(minified.InnerThing==chest,"Chest failed minification");
                    GenSpawn.Spawn(minified,position,map);
                    minified.GetDirectlyHeldThings().Remove(chest);minified.Destroy();GenSpawn.Spawn(chest,position,map);
                    Require(chest.RewardsCreated && !chest.CanOpen && chest.Accepts(ThingMaker.MakeThing(ThingDefOf.Steel)),"Portable chest lost rewards/storage state");
                    lines.Add("PASS actual chest opening: one robe and one jade stack; no duplicate rewards; portable storage unlocked");
                    Find.TickManager.CurTimeSpeed=TimeSpeed.Paused;
                    GameDataSaveLoader.SaveGame("孤云遗冢_宝匣验证");phase=3;frames=0;Flush();GameDataSaveLoader.LoadGame("孤云遗冢_宝匣验证");return;
                }
                if(phase==3)
                {
                    Require(map.listerThings.AllThings.OfType<Building_YunjianziInheritanceChest>().Single().RewardsCreated,"Chest state lost after save");
                    if(frames==90){Find.ScreenshotModeHandler.Active=true;Find.CameraDriver.SetRootPosAndSize(GenStep_YunjianziOrbitalFoundation.Shrine(map).ToVector3Shifted(),19);return;}
                    if(frames<120)return;
                    if(frames==120){ScreenshotTaker.TakeNonSteamShot("orbital-empty-altar-"+seed);return;}
                    if(frames==121){Find.CameraDriver.SetRootPosAndSize((map.Center+new IntVec3(30,0,13)).ToVector3Shifted(),13);return;}
                    if(frames<150)return;
                    if(frames==150){ScreenshotTaker.TakeNonSteamShot("orbital-relics-"+seed);return;}
                    if(frames==151){Find.CameraDriver.SetRootPosAndSize(map.Center.ToVector3Shifted(),43);return;}
                    if(frames<180)return;
                    if(frames==180){ScreenshotTaker.TakeNonSteamShot("orbital-overview-"+seed);return;}
                    if(frames==181){Find.CameraDriver.SetRootPosAndSize(GenStep_YunjianziOrbitalFoundation.Dock(map).ToVector3Shifted(),15);return;}
                    if(frames==210){ScreenshotTaker.TakeNonSteamShot("inheritance-review-ship-and-dock");return;}
                    if(frames<240)return;
                    Require(map.listerThings.AllThings.Count(t=>t.def.defName.StartsWith("YJZ_Relic_"))==relicCount,"Relics decayed or respawned after ticks/reload");
                    Find.ScreenshotModeHandler.Active=false;
                    Require(scrolls.Count==0,"Consumed scroll regenerated after reload");
                    var console=map.listerThings.AllThings.Select(t=>t.TryGetComp<CompPilotConsole>()).FirstOrDefault(c=>c!=null);
                    Require(console!=null,"Pilot console missing");
                    var launch=console.CanUseNow();
                    lines.Add("ENGINE fuel="+engine.TotalFuel+" range="+engine.MaxLaunchDistance+" floor="+engine.ValidSubstructure.Count+" launch="+launch.Accepted+" reason="+launch.Reason);
                    Require(launch.Accepted,"Cannot launch: "+launch.Reason);
                    Require(engine.ValidSubstructure.Count>0 && engine.ValidSubstructure.Count<=200,"Station attached to ship");
                    var destination=Find.WorldGrid.Surface.GetClosestTile_NewTemp(map.Tile,true);
                    Require(destination.Valid,"No landing destination");
                    Require(GravshipUtility.TryGetPathFuelCost(map.Tile,destination,out float cost,out int distance,10f,engine.FuelUseageFactor) && cost<=engine.TotalFuel,"No affordable surface destination");
                    var crew=map.mapPawns.AllPawnsSpawned.Where(p=>p.Faction==Faction.OfPlayer).ToList();
                    foreach(var p in crew)
                    {
                        p.jobs.StopAll();p.Position=engine.ValidSubstructure.First(c=>c.Standable(map));p.Notify_Teleported();
                    }
                    engine.ConsumeFuel(destination);
                    engine.launchInfo=new LaunchInfo{quality=1f,doNegativeOutcome=false};
                    var ship=GravshipUtility.GenerateGravship(engine);
                    Require(ship.Pawns.Count()==crew.Count,"Crew missing from ship");
                    Require(!ship.Things.Any(t=>t.def.defName.StartsWith("YJZ_Memorial") || t.def.defName=="YJZ_PredecessorCenotaph"),"Memorial departed with ship");
                    Require(map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("YJZ_PredecessorCenotaph")).Count==1,"Memorial removed on departure");
                    lines.Add("PASS vanilla gravship extraction: crew="+ship.Pawns.Count()+" fuelCost="+cost+" station remains");
                    phase=4;frames=0;Flush();
                    GravshipUtility.TravelTo(ship,map.Tile,destination);
                    LongEventHandler.QueueLongEvent(()=>GravshipUtility.ArriveNewMap(ship),"GeneratingMap",false,null);return;
                }
                if(phase==5)
                {
                    Require(map.Tile.LayerDef==PlanetLayerDefOf.Surface,"Did not arrive on surface");
                    Require(scrolls.Count==0,"Scroll duplicated after flight");
                    Require(!map.listerThings.AllThings.Any(t=>t.def.defName.StartsWith("YJZ_Relic_")),"Starting relics appeared on a later surface map");
                    lines.Add("PASS relics persisted in starting map only; none on later map");
                    Require(map.mapPawns.AllPawnsSpawned.Count(p=>p.Faction==Faction.OfPlayer)==2,"Crew lost after flight");
                    lines.Add("PASS landed intact on surface; no duplicate scroll");
                    finished=true;Flush();Application.Quit();
                }
            }
            catch(Exception e)
            {
                lines.Add("FAIL phase="+phase+" "+e);
                if(Find.CurrentMap!=null)
                {
                    GameDataSaveLoader.SaveGame("孤云遗冢_失败诊断");
                    var diagnostic=Find.CurrentMap;
                    var dock=GenStep_YunjianziOrbitalFoundation.Dock(diagnostic);
                    var rows=new List<string>();
                    for(int z=dock.z+15;z>=dock.z-14;z--)
                    {
                        var row="";
                        for(int x=dock.x-14;x<=dock.x+14;x++)
                        {
                            var c=new IntVec3(x,0,z);var ed=c.GetEdifice(diagnostic);
                            row+=c.GetFirstPawn(diagnostic)!=null?'P':ed is Building_Door?'D':ed!=null?(ed.def.passability==Traversability.Impassable?'#':'b'):c.Standable(diagnostic)?'.':' ';
                        }
                        rows.Add(row);
                    }
                    File.WriteAllLines(Path.Combine(GenFilePaths.SaveDataFolderPath,"failure-layout.txt"),rows);
                }
                finished=true;Flush();Application.Quit();
            }
        }
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static void CheckMemorialSupplies(Map map)
        {
            var shrine=GenStep_YunjianziOrbitalFoundation.Shrine(map);
            var shelves=map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("Shelf")).OfType<Building_Storage>()
                .Where(t=>CellRect.CenteredOn(shrine,35,31).Contains(t.Position)).ToList();
            Require(shelves.Count==8 && shelves.All(s=>s.slotGroup.HeldThingsCount>0 && s.slotGroup.HeldThingsCount<=6),"Materials not distributed across eight shelves");
            Require(shelves.All(s=>s.slotGroup.HeldThings.All(t=>s.Accepts(t))),"Shelf rejects assigned material");
            Require(shelves.All(s=>!s.GetStoreSettings().filter.Allows(DefDatabase<ThingDef>.GetNamed("Milira_TravellerFood"))),"Shelves drawing ship food");
            var contents=shelves.SelectMany(s=>s.slotGroup.HeldThings).ToList();
            foreach(var pair in new Dictionary<string,int>{{"Steel",540},{"Plasteel",240},{"ComponentIndustrial",25},{"Jade",200},{"YJZ_TrueJadeSteel",200},{"ComponentSpacer",2},{"GravlitePanel",100}})
            {
                Require(contents.Where(t=>t.def.defName==pair.Key).Sum(t=>t.stackCount)==pair.Value,"Incorrect shelf quantity: "+pair.Key);
                Require(!map.listerThings.AllThings.Any(t=>t.def.defName==pair.Key && CellRect.CenteredOn(GenStep_YunjianziOrbitalFoundation.Dock(map),27,27).Contains(t.Position)),"Material left on ship: "+pair.Key);
            }
        }
        static int CountJade(Map map)
        {
            // A hauling pawn can be holding a stack between our two checks.
            // Include carried/inventory stacks, not just spawned map items.
            int count=map.listerThings.ThingsOfDef(ThingDefOf.Jade).Sum(t=>t.stackCount);
            count+=map.listerThings.AllThings.OfType<Building_YunjianziInheritanceChest>().Sum(c=>c.GetDirectlyHeldThings().Where(t=>t.def==ThingDefOf.Jade).Sum(t=>t.stackCount));
            foreach(var pawn in map.mapPawns.AllPawns)
            {
                count+=pawn.inventory?.innerContainer.Where(t=>t.def==ThingDefOf.Jade).Sum(t=>t.stackCount)??0;
                Thing carried=pawn.carryTracker?.CarriedThing;
                if(carried?.def==ThingDefOf.Jade)count+=carried.stackCount;
            }
            return count;
        }
        static void ReviewInheritance(Map map)
        {
            var report=new List<string>();
            var pawn=map.mapPawns.FreeColonistsSpawned.Single(YunjianziSoleColonistUtility.IsYunjianzi);
            var comp=pawn.TryGetComp<CompYunjianziCultivation>();
            var scroll=map.listerThings.ThingsOfDef(YJZDefOf.YJZ_Item_BoundImmortalScroll).Single();
            var altar=scroll.Position;
            report.Add("INITIAL realm="+comp.Realm+" energy="+comp.Energy+" name="+pawn.Name.ToStringFull);
            var console=map.listerThings.AllThings.Select(t=>t.TryGetComp<CompPilotConsole>()).First(c=>c!=null);
            report.Add("BEFORE_STUDY consoleCanUse="+console.CanUseNow().Accepted);
            scroll.DeSpawn();pawn.inventory.innerContainer.TryAdd(scroll);
            report.Add("INVENTORY_SCROLL readAction="+scroll.GetFloatMenuOptions(pawn).Any(o=>o.Label=="研读传承宝卷" && o.action!=null));
            pawn.inventory.innerContainer.Remove(scroll);GenSpawn.Spawn(scroll,altar,map);
            scroll.Destroy();
            AccessTools.Method(typeof(CompYunjianziCultivation),"TryCreateBoundScroll").Invoke(comp,null);
            report.Add("LOST_SCROLL replacementCount="+map.listerThings.ThingsOfDef(YJZDefOf.YJZ_Item_BoundImmortalScroll).Count);
            scroll=map.listerThings.ThingsOfDef(YJZDefOf.YJZ_Item_BoundImmortalScroll).FirstOrDefault();
            if(scroll==null){scroll=ThingMaker.MakeThing(YJZDefOf.YJZ_Item_BoundImmortalScroll);scroll.TryGetComp<CompYunjianziBoundScroll>().BindTo(pawn);GenSpawn.Spawn(scroll,altar,map);}
            comp.CompleteBoundScrollStudy(scroll);
            report.Add("AFTER_STUDY realm="+comp.Realm+" energy="+comp.Energy+" energyMax="+comp.EnergyMax+" canBreakthrough="+comp.CanBreakthrough);
            var skills=pawn.skills.skills.Select(s=>s.levelInt).ToArray();
            comp.CompleteBoundScrollStudy(scroll);
            report.Add("REPEAT_STUDY skillsUnchanged="+skills.SequenceEqual(pawn.skills.skills.Select(s=>s.levelInt)));
            AccessTools.Method(typeof(CompYunjianziCultivation),"TryCreateBoundScroll").Invoke(comp,null);
            report.Add("AFTER_STUDY scrollCount="+map.listerThings.ThingsOfDef(YJZDefOf.YJZ_Item_BoundImmortalScroll).Count);
            report.Add("INHERITANCE_LETTERS="+Find.LetterStack.LettersListForReading.Count(l=>l.Label=="YJZ_InheritanceTitle".Translate()));
            var engine=map.listerThings.AllThings.OfType<Building_GravEngine>().Single();
            report.Add("SHIP fuel="+engine.TotalFuel+" range="+engine.MaxLaunchDistance+" floor="+engine.ValidSubstructure.Count);
            foreach(var group in map.listerThings.AllThings.Where(t=>t is Building && engine.ValidSubstructure.Contains(t.Position)).GroupBy(t=>t.def.defName).OrderBy(g=>g.Key))
                report.Add("SHIP_BUILDING "+group.Key+"="+group.Count());
            File.WriteAllLines(Path.Combine(GenFilePaths.SaveDataFolderPath,"inheritance-review.txt"),report);
        }
        static void ExportMonumentCatalog()
        {
            var catalog=new System.Xml.Linq.XElement("Monuments");
            for(int seedIndex=0;seedIndex<40;seedIndex++)
            {
                Rand.PushState(70000+seedIndex);
                try
                {
                    var allowed=new ThingFilter();
                    foreach(var def in new[]{ThingDefOf.Wall,ThingDefOf.Door,ThingDefOf.Column,ThingDefOf.SteleLarge,ThingDefOf.SteleGrand})allowed.SetAllow(def,true);
                    var sketch=RimWorld.SketchGen.SketchGen.Generate(SketchResolverDefOf.Monument,new RimWorld.SketchGen.SketchResolveParams{
                        sketch=new Sketch(),monumentSize=new IntVec2(35,31),monumentOpen=false,onlyBuildableByPlayer=true,
                        allowWood=false,allowedMonumentThings=allowed,addFloors=true});
                    var bounds=sketch.OccupiedRect;
                    var item=new System.Xml.Linq.XElement("Monument",new System.Xml.Linq.XAttribute("seed",70000+seedIndex));
                    var rows=new List<string>();
                    for(int z=bounds.maxZ;z>=bounds.minZ;z--)
                    {
                        var row="";
                        for(int x=bounds.minX;x<=bounds.maxX;x++)
                        {
                            var c=new IntVec3(x,0,z);var thing=sketch.Things.FirstOrDefault(t=>t.OccupiedRect.Contains(c));
                            row+=thing!=null?(thing.def==ThingDefOf.Wall?'#':thing.def==ThingDefOf.Door?'D':thing.def==ThingDefOf.Column?'C':'S'):sketch.Terrain.Any(t=>t.pos==c)?'.':' ';
                        }
                        rows.Add(row);
                    }
                    item.Add(new System.Xml.Linq.XElement("Rows",string.Join("\n",rows)));
                    foreach(var t in sketch.Things)item.Add(new System.Xml.Linq.XElement("Thing",new System.Xml.Linq.XAttribute("def",t.def.defName),new System.Xml.Linq.XAttribute("x",t.pos.x),new System.Xml.Linq.XAttribute("z",t.pos.z),new System.Xml.Linq.XAttribute("rot",t.rot.AsInt)));
                    foreach(var t in sketch.Terrain)item.Add(new System.Xml.Linq.XElement("Floor",new System.Xml.Linq.XAttribute("x",t.pos.x),new System.Xml.Linq.XAttribute("z",t.pos.z)));
                    catalog.Add(item);
                }
                finally{Rand.PopState();}
            }
            catalog.Save(Path.Combine(GenFilePaths.SaveDataFolderPath,"monument-catalog.xml"));
        }
        static void CapturePortrait(Pawn pawn,string name)
        {
            var texture=PortraitsCache.Get(pawn,new Vector2(512,512),Rot4.South,cameraZoom:1.5f,supersample:false,compensateForUIScale:false,renderHeadgear:false);
            var previous=RenderTexture.active;
            try
            {
                RenderTexture.active=texture;
                var pixels=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);
                pixels.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(GenFilePaths.SaveDataFolderPath,name+".png"),pixels.EncodeToPNG());
                UnityEngine.Object.Destroy(pixels);
            }
            finally {RenderTexture.active=previous;}
        }
        static FloatMenuOption CheckOpeningMenu(Thing target,Pawn pawn,bool expected,string label,List<Pawn> selected=null)
        {
            // A real click follows room discovery. Unfogging only the clicked
            // tile leaves Room.Fogged true and trips vanilla CleanRoom's null
            // filth-list path; do not manufacture that partial-fog UI state.
            target.Map.fogGrid.ClearAllFog();
            var options=FloatMenuMakerMap.GetOptions(selected??new List<Pawn>{pawn},target.Position.ToVector3Shifted(),out var context);
            Require(context!=null,"Actual float-menu context missing");
            if(selected!=null)Require(context.IsMultiselect,"Companion not selectable alongside inheritor");
            Require(!new FloatMenuOptionProvider_OpenThing().GetOptionsFor(target,context).Any(),"Duplicate vanilla OpenThing action remains");
            var opening=options.Where(o=>o.Label==label).ToList();
            Require(opening.Count==(expected?1:0) && (!expected || opening[0].action!=null),"Incorrect actual opening menu for "+target.def.defName+": "+string.Join(" | ",options.Select(o=>o.Label)));
            if(!expected && ((target is Building_YunjianziStartingShenjiArk ark && !ark.Opened) || (target is Building_YunjianziInheritanceChest chest && !chest.RewardsCreated)))
            {
                bool isArk=target is Building_YunjianziStartingShenjiArk;
                string hint=isArk?Building_YunjianziStartingShenjiArk.LockedHint:Building_YunjianziInheritanceChest.LockedHint;
                string hintLabel=isArk?Building_YunjianziStartingShenjiArk.LockedMenuLabel:Building_YunjianziInheritanceChest.LockedMenuLabel;
                Require(options.Count(o=>o.Label==hintLabel && o.action!=null)==1,"Missing or duplicated sealed-state hint action");
                Find.Selector.ClearSelection();Find.Selector.Select(target,false,false);
                var liveMessages=(List<Verse.Message>)AccessTools.Field(typeof(Messages),"liveMessages").GetValue(null);
                Require(liveMessages.Any(m=>m.text==hint),"Selecting sealed relic did not display narrative notification");
                options.Single(o=>o.Label==hintLabel).action();
                Require(!((IOpenable)target).CanOpen,"Narrative hint unlocked sealed relic");
                Require(!hint.Contains("研读") && !hint.Contains("宝卷"),"Sealed hint reveals inheritance solution");
                Find.Selector.ClearSelection();
            }
            return expected?opening[0]:null;
        }
        static void Flush(){File.WriteAllLines(FilePath,lines.Concat(errors.Select(e=>"ERROR "+e)));foreach(var line in lines)Log.Message("[YJZ Orbital Audit] "+line);}
    }
}
