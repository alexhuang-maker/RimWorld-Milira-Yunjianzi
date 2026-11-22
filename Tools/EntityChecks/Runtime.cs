using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Yunjianzi
{
    [StaticConstructorOnStartup]
    public static partial class EntityRuntimeAudit
    {
        static bool started,staged,done,reloading,shotCaptured;
        static int frame,startTick,spines,shells;
        static int savedHunterId,savedHunterCooldown;
        static bool hunterAutoCloaked;
        static Map map;
        static List<Pawn> actors=new List<Pawn>();
        static List<Pawn> dummies=new List<Pawn>();
        static readonly HashSet<int> releasedSkills=new HashSet<int>();
        static readonly List<string> results=new List<string>();
        static readonly List<string> errors=new List<string>();
        static string FilePath=>Path.Combine(GenFilePaths.SaveDataFolderPath,"entity-audit.txt");
        static EntityRuntimeAudit()
        {
            if(!GenCommandLine.CommandLineArgPassed("YJZEntityAudit"))return;
            var h=new Harmony("yunjianzi.entity.audit");
            h.Patch(AccessTools.Method(typeof(UIRoot_Entry),nameof(UIRoot_Entry.UIRootUpdate)),postfix:new HarmonyMethod(typeof(EntityRuntimeAudit),nameof(Entry)));
            h.Patch(AccessTools.Method(typeof(Root_Play),"Update"),postfix:new HarmonyMethod(typeof(EntityRuntimeAudit),nameof(Play)));
            h.Patch(AccessTools.Method(typeof(CompYunjianziEntity),"ReleaseSkill"),postfix:new HarmonyMethod(typeof(EntityRuntimeAudit),nameof(Released)));
            h.Patch(AccessTools.Method(typeof(CompYunjianziEntity),"TryHunterCloak"),postfix:new HarmonyMethod(typeof(EntityRuntimeAudit),nameof(Cloaked)));
            h.Patch(AccessTools.Method(typeof(Projectile),"Launch",new[]{typeof(Thing),typeof(Vector3),typeof(LocalTargetInfo),typeof(LocalTargetInfo),typeof(ProjectileHitFlags),typeof(bool),typeof(Thing),typeof(ThingDef)}),postfix:new HarmonyMethod(typeof(EntityRuntimeAudit),nameof(Launched)));
            foreach(var m in typeof(Log).GetMethods().Where(m=>m.Name=="Error" && m.GetParameters().Length>0 && m.GetParameters()[0].ParameterType==typeof(string)))
                h.Patch(m,prefix:new HarmonyMethod(typeof(EntityRuntimeAudit),nameof(Error)));
        }
        static void Error(string __0){if(errors.Count<100)errors.Add(__0);}
        static void Released(CompYunjianziEntity __instance){if(__instance.motionStart==Find.TickManager.TicksGame)releasedSkills.Add(__instance.Role);}
        static void Cloaked(CompYunjianziEntity __instance,bool __result){if(__result && actors.Contains(__instance.Pawn))hunterAutoCloaked=true;}
        static void Launched(Projectile __instance,Thing launcher)
        {if(__instance is Projectile_YunjianziSpine)spines++;if(__instance is Projectile_YunjianziFleshShell){shells++;if(launcher is Pawn p && p.pather.Moving)movingShells++;}}
        static void Check(string name,Action action)
        {
            try{action();results.Add("PASS "+name);}catch(Exception e){results.Add("FAIL "+name+": "+e);}
            File.WriteAllLines(FilePath,results);
        }
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static void Entry()
        {
            if(started || Find.ScreenshotModeHandler==null)return;started=true;
            Prefs.RunInBackground=true;Application.runInBackground=true;
            foreach(string id in YunjianziEntities.Ids)
            {
                Check(id+" definitions and four directional materials",()=>{
                    var kind=DefDatabase<PawnKindDef>.GetNamed("YJZ_Entity_"+id);
                    Require(Math.Abs(kind.combatPower-YunjianziEntityRules.Costs[Array.IndexOf(YunjianziEntities.Ids,id)])<.01f,"roster cost differs from def");
                    var issues=kind.race.ConfigErrors().Concat(kind.ConfigErrors()).ToArray();
                    Require(issues.Length==0,string.Join(";",issues));
                    Require(!kind.race.race.hasCorpse && kind.race.race.corpseDef==null,"corpse enabled");
                    Require(kind.race.description.NullOrEmpty(),"unexpected prose");
                    Graphic g=kind.lifeStages[0].bodyGraphicData.Graphic;
                    foreach(var m in new[]{g.MatNorth,g.MatEast,g.MatSouth,g.MatWest})
                        Require(m!=null && m!=BaseContent.BadMat && m.mainTexture.width>100,"invalid directional material");
                });
            }
            results.Add("GENERATING clean isolated test map");File.WriteAllLines(FilePath,results);
            Current.Game=null;UnityEngine.SceneManagement.SceneManager.LoadScene("Play");
        }
        static void Play()
        {
            if(!started || done || Current.ProgramState!=ProgramState.Playing || LongEventHandler.AnyEventNowOrWaiting)return;
            try
            {
                // Suppress third-party welcome/letter pause windows only in this isolated audit.
                foreach(var window in Find.WindowStack.Windows.Where(w=>w.forcePause).ToList())
                {window.forcePause=false;Find.WindowStack.TryRemove(window,false);}
                if(reloading)
                {
                    Find.TickManager.CurTimeSpeed=TimeSpeed.Paused;
                    if(++frame<90)return;
                    done=true;
                    Check("tombstone persists through save/load",()=>Require(Find.Maps.SelectMany(m=>m.listerThings.AllThings).Any(t=>t is Building_YunjianziTombstone),"persistent tombstone lost"));
                    Check("reload preserves five defeated guardians without corpses",()=>{
                        var nodes=GameComponent_YunjianziPursuit.Instance.nodes;
                        Require(nodes.Count==5 && nodes.All(n=>n.entityBossSpawned && n.entityBossDefeated),"guardian flags lost");
                        Require(Find.Maps.SelectMany(m=>m.mapPawns.AllPawnsSpawned).Any(p=>p.TryGetComp<CompYunjianziEntity>()!=null),"live enemies lost");
                    });
                    foreach(var error in errors)results.Add("ERROR "+error);
                    Check("hunter cloak duration and cooldown survive save/load",()=>{
                        var hunter=Find.Maps.SelectMany(m=>m.mapPawns.AllPawnsSpawned).First(p=>p.thingIDNumber==savedHunterId);
                        var cloak=hunter.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("YJZ_EntityHunterCloak"));
                        Require(cloak!=null && cloak.TryGetComp<HediffComp_Disappears>().ticksToDisappear>0 && cloak.TryGetComp<HediffComp_Disappears>().ticksToDisappear<=73,"cloak time reset on load");
                        Require(hunter.TryGetComp<CompYunjianziEntity>().nextSkill==savedHunterCooldown,"cooldown reset on load");
                        var timer=cloak.TryGetComp<HediffComp_Disappears>();float delta=0;
                        for(int i=0;i<73;i++)timer.CompPostTick(ref delta);
                        Require(timer.CompShouldRemove,"cloak does not expire");
                    });
                    File.WriteAllLines(FilePath,results);Application.Quit();return;
                }
                if(!staged) {Stage();return;}
                frame++;
                SiegeScreenshots();
                if(frame>45)Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;
                int elapsed=Find.TickManager.TicksGame-startTick;
                if(frame==30)ScreenshotTaker.TakeNonSteamShot("enemy-lineup-v13-concealed-limbs");
                if(frame==45)
                {
                    for(int i=0;i<actors.Count;i++)
                    {
                        int slot=i==6?4:i==4?6:i;
                        actors[i].Position=new IntVec3(30+(slot%4)*18,0,35+(slot/4)*20);actors[i].Notify_Teleported();
                        dummies[i].Position=actors[i].Position+new IntVec3(8,0,0);dummies[i].Notify_Teleported();
                    }
                    Find.CameraDriver.SetRootPosAndSize(new Vector3(60,0,46),26);
                    actors[1].TakeDamage(new DamageInfo(DamageDefOf.Cut,12,1));
                    Find.TickManager.CurTimeSpeed=TimeSpeed.Normal;startTick=Find.TickManager.TicksGame;
                }
                if(frame==180)ScreenshotTaker.TakeNonSteamShot("enemy-combat-v13");
                if(!shotCaptured && spines>0 && shells>0) {shotCaptured=true;ScreenshotTaker.TakeNonSteamShot("enemy-projectiles-v13");}
                if(elapsed<1800 && frame<6000)return;
                Check("ranged AI launched both custom projectile types",()=>Require(spines>0 && shells>0,$"spines={spines}, shells={shells}"));
                Check("mortar fires during actual path movement",()=>Require(movingShells>0,"no projectile launched while moving"));
                Check("dash, breaker smash and tomb strikes exercised",()=>Require(new[]{0,5,7}.All(releasedSkills.Contains),"special roles observed: "+string.Join(",",releasedSkills)));
                Check("hunter AI automatically cloaks during approach",()=>Require(hunterAutoCloaked,"hunter never auto-cloaked"));
                results.Add($"DETAIL combat ticks={elapsed}, frames={frame}, spines={spines}, shells={shells}");
                Check("save serializes enemy and quest state",()=>{
                    Building_YunjianziTombstone.Fall(map,new IntVec3(90,0,27),null);
                    var h=PawnGenerator.GeneratePawn(DefDatabase<PawnKindDef>.GetNamed("YJZ_Entity_Hunter"),Faction.OfEntities);
                    GenSpawn.Spawn(h,CellFinder.RandomClosewalkCellNear(map.Center,map,8),map);
                    h.health.AddHediff(DefDatabase<HediffDef>.GetNamed("YJZ_EntityHunterCloak")).TryGetComp<HediffComp_Disappears>().ticksToDisappear=73;
                    savedHunterId=h.thingIDNumber;savedHunterCooldown=Find.TickManager.TicksGame+900;
                    h.TryGetComp<CompYunjianziEntity>().nextSkill=savedHunterCooldown;
                    Find.TickManager.CurTimeSpeed=TimeSpeed.Paused;
                    GameDataSaveLoader.SaveGame("邪物验收副本");
                });
                reloading=true;frame=0;GameDataSaveLoader.LoadGame("邪物验收副本");
            }
            catch(Exception e){done=true;results.Add("FAIL stage/play "+e);File.WriteAllLines(FilePath,results);Application.Quit();}
        }
        static void Stage()
        {
            staged=true;map=Find.CurrentMap ?? Find.Maps.First();
            var owner=map.mapPawns.AllPawnsSpawned.FirstOrDefault(p=>p.TryGetComp<CompYunjianziCultivation>()?.IsYunjianzi==true);
            if(owner==null)
            {
                owner=PawnGenerator.GeneratePawn(DefDatabase<PawnKindDef>.GetNamed("YJZ_Yunjianzi"),Faction.OfPlayer);
                GenSpawn.Spawn(owner,CellFinder.RandomClosewalkCellNear(map.Center,map,8),map);
            }
            var cultivation=owner.TryGetComp<CompYunjianziCultivation>();
            if(!cultivation.HasOpenedBoundScroll)
            {
                var scroll=(Thing)AccessTools.Method(typeof(CompYunjianziCultivation),"FindBoundScroll").Invoke(cultivation,null);
                Require(scroll!=null,"test inheritor has no bound scroll");
                cultivation.CompleteBoundScrollStudy(scroll);
            }
            Require(cultivation.RealmHediff!=null,"test inheritance not initialized");
            var story=GameComponent_YunjianziPursuit.Instance;
            story.enabled=true;story.initialized=true;story.target=owner;story.finished=true;
            Check("runtime realm gates and random roster budget",()=>{
                foreach(int realm in new[]{2,3,4,5})
                {
                    cultivation.RealmHediff.Severity=realm;
                    var group=YunjianziEntities.Generate(2500);
                    Require(realm>=3 || group.Count==0,"low realm generated");
                    Require(group.All(p=>p.TryGetComp<CompYunjianziEntity>().Role<7),"random boss");
                    Require(realm>=4 || group.All(p=>p.TryGetComp<CompYunjianziEntity>().Role<5),"early medium");
                    foreach(var p in group)p.Destroy(DestroyMode.Vanish);
                }
                story.enabled=false;Require(YunjianziEntities.Generate(2500).Count==0,"scenario bypass");story.enabled=true;
                cultivation.RealmHediff.Severity=4;
            });
            Check("all eight deaths leave no corpse or loot; exact fivefold cultivation",()=>{
                AccessTools.Field(typeof(CompYunjianziCultivation),"boundScrollConsumed").SetValue(cultivation,true);
                var energy=AccessTools.Field(typeof(CompYunjianziCultivation),"cultivatedEnergyMax");
                foreach(string id in YunjianziEntities.Ids)
                {
                    var p=PawnGenerator.GeneratePawn(DefDatabase<PawnKindDef>.GetNamed("YJZ_Entity_"+id),Faction.OfEntities);
                    var cell=CellFinder.RandomClosewalkCellNear(owner.Position,map,8);GenSpawn.Spawn(p,cell,map);
                    var beforeThings=new HashSet<Thing>(map.listerThings.AllThings);
                    energy.SetValue(cultivation,1f);
                    float value=p.GetStatValue(StatDefOf.MarketValue);
                    float expected=value/500f*cultivation.CultivationGainPerHour*5;
                    AnomalyIncidentUtility.PawnShardOnDeath(p);
                    Require(!p.health.hediffSet.HasHediff(HediffDefOf.ShardHolder),"shard injected");
                    p.Kill(new DamageInfo(DamageDefOf.Cut,99999,99,-1,owner));
                    Require(p.Dead && p.Corpse==null,"corpse remains "+id);
                    Require(!map.listerThings.AllThings.Except(beforeThings).Any(t=>t.def.category==ThingCategory.Item),"loot remains "+id);
                    float gained=(float)energy.GetValue(cultivation)-1;
                    Require(Math.Abs(gained-expected)<.1f,$"reward {id}: {gained} expected {expected}");
                    cultivation.NotifyEnemyDestroyed(p,value);
                    Require(Math.Abs((float)energy.GetValue(cultivation)-1-gained)<.01,"duplicate reward");
                }
            });
            // Clear only the isolated copy's test rectangle; real saves are never written.
            var rect=new CellRect(25,25,70,45);
            foreach(var cell in rect)
            {
                foreach(var t in cell.GetThingList(map).ToList())if(!(t is Pawn) && t.def.destroyable)t.Destroy(DestroyMode.Vanish);
                map.terrainGrid.SetTerrain(cell,TerrainDefOf.Soil);map.roofGrid.SetRoof(cell,null);map.fogGrid.Unfog(cell);
            }
            Check("natural entity swarm entry honors realm and scenario gates",()=>{
                bool sawSmall=false,sawMedium=false;
                foreach(var type in new[]{typeof(IncidentWorker_GorehulkAssault),typeof(IncidentWorker_ChimeraAssault),typeof(IncidentWorker_SightstealerSwarm)})
                {
                    var worker=DefDatabase<IncidentDef>.AllDefs.First(d=>d.workerClass==type).Worker;
                    var method=AccessTools.Method(type,"TryExecuteWorker");
                    foreach(int realm in new[]{2,3,4,0})
                    {
                        story.enabled=realm!=0;cultivation.RealmHediff.Severity=realm==0?4:realm;
                        bool selected=false;
                        for(int attempt=0;attempt<(realm>=3?15:1) && !selected;attempt++)
                        {
                            var before=new HashSet<Pawn>(map.mapPawns.AllPawnsSpawned);
                            var parms=new IncidentParms{target=map,points=2000,faction=Faction.OfEntities};
                            method.Invoke(worker,new object[]{parms});
                            foreach(var p in map.mapPawns.AllPawnsSpawned.Where(p=>!before.Contains(p)).ToList())
                            {
                                var c=p.TryGetComp<CompYunjianziEntity>();
                                if(c!=null)
                                {
                                    selected=true;
                                    Require(realm>=3 && c.Role!=7,"illegal natural enemy");
                                    Require(realm>=4 || c.Role<5,"early medium");
                                    sawSmall|=c.Role<5;sawMedium|=c.Role>=5;
                                }
                                p.Destroy(DestroyMode.Vanish);
                            }
                        }
                        Require(realm<3 || selected,"custom assault never selected: "+type.Name);
                    }
                }
                Require(sawSmall && sawMedium,"new roster never selected");
                story.enabled=true;cultivation.RealmHediff.Severity=4;
            });
            SiegeChecks(owner);
            Check("shield direction and support healing",()=>{
                var shield=PawnGenerator.GeneratePawn(DefDatabase<PawnKindDef>.GetNamed("YJZ_Entity_Shield"),Faction.OfEntities);
                var mender=PawnGenerator.GeneratePawn(DefDatabase<PawnKindDef>.GetNamed("YJZ_Entity_Mender"),Faction.OfEntities);
                GenSpawn.Spawn(shield,new IntVec3(30,0,30),map,Rot4.North);
                GenSpawn.Spawn(mender,new IntVec3(33,0,30),map);
                owner.Position=new IntVec3(30,0,35);owner.Notify_Teleported();
                var hit=new DamageInfo(DamageDefOf.Bullet,20,.3f,-1,owner);
                shield.TryGetComp<CompYunjianziEntity>().PostPreApplyDamage(ref hit,out _);
                Require(Math.Abs(hit.Amount-8)<.01f,"front not protected");
                shield.Rotation=Rot4.South;hit=new DamageInfo(DamageDefOf.Bullet,20,.3f,-1,owner);
                shield.TryGetComp<CompYunjianziEntity>().PostPreApplyDamage(ref hit,out _);
                Require(Math.Abs(hit.Amount-20)<.01f,"rear incorrectly protected");
                shield.TakeDamage(new DamageInfo(DamageDefOf.Cut,20,99));
                float before=shield.health.hediffSet.hediffs.OfType<Hediff_Injury>().Sum(h=>h.Severity);
                var heal=mender.TryGetComp<CompYunjianziEntity>();
                AccessTools.Method(typeof(CompYunjianziEntity),"Mend").Invoke(heal,null);
                float after=shield.health.hediffSet.hediffs.OfType<Hediff_Injury>().Sum(h=>h.Severity);
                Require(after<before && heal.linked==shield,"support not healing");
                AccessTools.Method(typeof(CompYunjianziEntity),"Mend").Invoke(heal,null);
                Require(Math.Abs(shield.health.hediffSet.hediffs.OfType<Hediff_Injury>().Sum(h=>h.Severity)-after)<.01f,"stacking heals");
                shield.Destroy(DestroyMode.Vanish);mender.Destroy(DestroyMode.Vanish);
                owner.Position=CellFinder.RandomClosewalkCellNear(map.Center,map,8);owner.Notify_Teleported();
            });
            Check("hunter cloak hides, blocks repeats, breaks on damage and melee, respects disruptor",()=>{
                var h=PawnGenerator.GeneratePawn(DefDatabase<PawnKindDef>.GetNamed("YJZ_Entity_Hunter"),Faction.OfEntities);
                GenSpawn.Spawn(h,new IntVec3(30,0,30),map);
                owner.Position=new IntVec3(36,0,30);owner.Notify_Teleported();
                var c=h.TryGetComp<CompYunjianziEntity>();var cast=AccessTools.Method(typeof(CompYunjianziEntity),"TryHunterCloak");
                var def=DefDatabase<HediffDef>.GetNamed("YJZ_EntityHunterCloak");
                Require((bool)cast.Invoke(c,null),"cloak not activated");
                var cloak=h.health.hediffSet.GetFirstHediffOfDef(def);
                Require(!cloak.TryGetComp<HediffComp_Invisibility>().PsychologicallyVisible && cloak.TryGetComp<HediffComp_Invisibility>().GetAlpha()==0,"still visible/targetable");
                Require(cloak.TryGetComp<HediffComp_Disappears>().ticksToDisappear==240,"wrong duration");
                Require(!(bool)cast.Invoke(c,null),"cloak repeats during cooldown");
                h.TakeDamage(new DamageInfo(DamageDefOf.Cut,1,99,-1,owner));
                Require(!h.health.hediffSet.HasHediff(def),"damage did not reveal");
                Require(!(bool)cast.Invoke(c,null),"damage reset cooldown");
                c.nextSkill=0;Require((bool)cast.Invoke(c,null),"cloak restart failed");
                owner.Position=new IntVec3(31,0,30);owner.Notify_Teleported();
                var verb=h.meleeVerbs.TryGetMeleeVerb(owner);Require(verb.TryStartCastOn(owner),"melee cast failed");
                Require(!h.health.hediffSet.HasHediff(def),"melee did not reveal");
                owner.Position=new IntVec3(36,0,30);owner.Notify_Teleported();c.nextSkill=0;
                h.health.AddHediff(HediffDefOf.DisruptorFlash);
                Require(!(bool)cast.Invoke(c,null),"disruptor did not prevent cloak");
                h.Destroy(DestroyMode.Vanish);owner.Position=CellFinder.RandomClosewalkCellNear(map.Center,map,8);owner.Notify_Teleported();
            });
            foreach(string id in YunjianziEntities.Ids)
            {
                int i=actors.Count;
                int slot=i==6?4:i==4?6:i; // Keep mortar targets outside the tomb's opening strike radius.
                var p=PawnGenerator.GeneratePawn(DefDatabase<PawnKindDef>.GetNamed("YJZ_Entity_"+id),Faction.OfEntities);
                GenSpawn.Spawn(p,new IntVec3(30+(slot%4)*18,0,35+(slot/4)*20),map,Rot4.East);actors.Add(p);
                var dummy=PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist,Faction.OfPlayer);
                dummies.Add(dummy);
                GenSpawn.Spawn(dummy,p.Position+new IntVec3(8,0,0),map);
                dummy.drafter.Drafted=true;
                LordMaker.MakeNewLord(Faction.OfEntities,new LordJob_AssaultColony(Faction.OfEntities,canKidnap:false,canTimeoutOrFlee:false,canSteal:false),map,new[]{p});
            }
            Check("all five quest guardians spawn once and retain defeat flags",()=>{
                story.nodes.Clear();
                for(int i=0;i<5;i++)
                {
                    var n=new YunjianziNodeState{index=i,site=map.Parent,barsBroken=i<2?3:5,defending=true};
                    story.nodes.Add(n);
                    var wave=new YunjianziWave{id="audit-boss-"+i,points=180};
                    Require(YunjianziEntities.FireQuestWave(map,wave,n),"quest spawn failed");
                    Require(wave.enemies.Count(p=>p.kindDef.defName=="YJZ_Entity_Tomb")==1,"boss count");
                    n.waves.Add(wave);
                    foreach(var p in wave.enemies)p.Kill(new DamageInfo(DamageDefOf.Cut,99999,99,-1,owner));
                    Require(n.entityBossDefeated,"defeat not registered");
                    var repeat=new YunjianziWave{id="audit-repeat-"+i,points=180};
                    Require(YunjianziEntities.FireQuestWave(map,repeat,n),"second wave failed");
                    Require(repeat.enemies.All(p=>p.kindDef.defName!="YJZ_Entity_Tomb"),"duplicate guardian");
                    foreach(var p in repeat.enemies)p.Destroy(DestroyMode.Vanish);
                }
            });
            for(int h=0;h<24 && GenLocalDate.HourOfDay(map)!=12;h++)Find.TickManager.gameStartAbsTick+=2500;
            for(int i=0;i<actors.Count;i++)
            {
                actors[i].Position=i<5?new IntVec3(36+i*10,0,40):new IntVec3(42+(i-5)*16,0,58);
                actors[i].Notify_Teleported();actors[i].Rotation=Rot4.East;
                dummies[i].Position=actors[i].Position+new IntVec3(4,0,0);dummies[i].Notify_Teleported();
            }
            Find.CameraDriver.SetRootPosAndSize(new Vector3(58,0,49),19);
            Find.TickManager.CurTimeSpeed=TimeSpeed.Paused;startTick=Find.TickManager.TicksGame;
            results.Add("STAGED combat arena");File.WriteAllLines(FilePath,results);
        }
    }
}
