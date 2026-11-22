using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    // Included only by -p:YunjianziAudit=true. Never part of release builds.
    [StaticConstructorOnStartup]
    internal static class RimWorld17Checks
    {
        private static bool ran;
        static RimWorld17Checks()
        {
            if (GenCommandLine.CommandLineArgPassed("YJZCompatibilityAudit"))
                new Harmony("yunjianzi.development.runtimeaudit").Patch(
                    AccessTools.Method(typeof(UIRoot_Entry), nameof(UIRoot_Entry.UIRootUpdate)),
                    postfix: new HarmonyMethod(typeof(RimWorld17Checks), nameof(AfterEntryUpdate)));
        }

        private static void AfterEntryUpdate()
        {
            // Startup callbacks precede UIRoot.Init; Thing.DrawAt needs its
            // screenshot-mode handler even for a non-highlighted building.
            if (ran || Find.ScreenshotModeHandler == null) return;
            ran = true;
            Run();
        }

        private static void Run()
        {
            var results = new List<string>();
            results.Add("DETAIL LoadedMods=" + string.Join(",", LoadedModManager.RunningModsListForReading.Select(m => m.PackageId)));
            var previousGame = Current.Game;
            Current.Game = new Game();
            void Check(string name, Action action)
            {
                try { action(); results.Add("PASS " + name); }
                catch (Exception e) { results.Add("FAIL " + name + ": " + e); }
            }
            Check("All Yunjianzi Harmony patch classes installed", () =>
            {
                var installed = new HashSet<Type>();
                foreach (var method in Harmony.GetAllPatchedMethods())
                {
                    var info = Harmony.GetPatchInfo(method);
                    foreach (var patch in info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Concat(info.Finalizers))
                        if (patch.owner.StartsWith("alex.miliraprivateexpansion.yunjianzi")) installed.Add(patch.PatchMethod.DeclaringType);
                }
                var expected = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.GetName().Name == "Yunjianzi.FixedTemplate" || a.GetName().Name == "Yunjianzi.Buildings")
                    .SelectMany(a => a.GetTypes()).Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0).ToArray();
                var missing = expected.Where(t => !installed.Contains(t)).Select(t => t.FullName).ToArray();
                if (missing.Length > 0) throw new Exception(string.Join(", ", missing));
                results.Add("DETAIL Harmony patch classes=" + expected.Length);
            });
            Check("Ancot turret instantiate and base DrawAt", () =>
            {
                var def = DefDatabase<ThingDef>.GetNamed("YJZ_Building_SwordRainTurret");
                var thing = ThingMaker.MakeThing(def, GenStuff.DefaultStuffFor(def));
                AccessTools.DeclaredMethod(typeof(Building_YunjianziSwordRainTurret), "DrawAt")
                    .Invoke(thing, new object[] { Vector3.zero, false });
            });
            Check("Ancot laser inheritance", () =>
            {
                var thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("YJZ_Projectile_ShenjiLaser"));
                if (!(thing is Projectile_YunjianziShenjiLaser)) throw new Exception("Wrong projectile type");
            });
            Check("Three elemental tiers and actual material colours", () =>
            {
                float lastHealth = 0f, lastSize = 0f;
                foreach (int realm in new[] { 2, 3, 4 })
                {
                    var kind = ThunderBeastTier.ForRealm(realm);
                    var comp = kind.race.comps.OfType<CompProperties_ThunderBeast>().Single();
                    if (comp.visualRealm != realm || comp.lifetimeTicks != 1800) throw new Exception("Wrong tier properties");
                    if (kind.race.race.baseHealthScale <= lastHealth) throw new Exception("Health tier order");
                    lastHealth = kind.race.race.baseHealthScale;
                    var graphic = kind.lifeStages[0].bodyGraphicData.Graphic;
                    if (graphic.drawSize.x <= lastSize) throw new Exception("Visual size tier order");
                    lastSize = graphic.drawSize.x;
                    foreach (var mat in new[] { graphic.MatNorth, graphic.MatEast, graphic.MatSouth, graphic.MatWest })
                    {
                        var source = mat.mainTexture;
                        var previous = RenderTexture.active;
                        var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
                        var pixels = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                        try
                        {
                            Graphics.Blit(source, rt);
                            RenderTexture.active = rt;
                            pixels.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                            var colours = pixels.GetPixels().Where(c => c.a > .5f).ToArray();
                            if (colours.Length == 0) throw new Exception("Empty directional texture");
                            float red = colours.Sum(c => c.r), green = colours.Sum(c => c.g), blue = colours.Sum(c => c.b);
                            if (realm == 2 && (green <= red * 2f || blue <= red * 2f)) throw new Exception("Weak elemental is not cyan");
                            if (realm == 4 && (red <= blue * 2f || green <= blue * 2f)) throw new Exception("Strong elemental is not gold");
                        }
                        finally
                        {
                            RenderTexture.active = previous;
                            RenderTexture.ReleaseTemporary(rt);
                            UnityEngine.Object.Destroy(pixels);
                        }
                    }
                }
            });
            Check("Lightning mesh and three realm materials", () =>
            {
                var ribbon = new PurpleLightningRibbon();
                try
                {
                    foreach (int realm in new[] { 2, 3, 4 })
                    {
                        ribbon.Begin();
                        ribbon.EmpPulse(Vector3.zero, 2.5f, 13, .5f);
                        ribbon.ParticleExplosion(Vector3.zero, 2.5f, 1f, 13, .3f);
                        ribbon.Draw(.7f, realm);
                        PurpleLightningRibbon.DrawGlow(Vector3.zero, 1f, .2f, realm);
                    }
                }
                finally { ribbon.Dispose(); }
            });
            Check("Pursuit narrative migration and live stage counters", () =>
            {
                var pursuit=GameComponent_YunjianziPursuit.Instance;
                var original= pursuit.nodes;
                try
                {
                    var quest=new Quest {name="旧测站的回声",description="旧测站场景"};
                    var node=new YunjianziNodeState {index=0,quest=quest,barsBroken=3,defending=true,hitPoints=1};
                    pursuit.nodes=new List<YunjianziNodeState> {node};
                    pursuit.RefreshNarrative();
                    if(quest.name!="斩断邪引·吞日魔枢" || !quest.description.ToString().Contains("云间子感应到一处魔神节点"))
                        throw new Exception("Saved quest text was not migrated");
                    if(node.barsBroken!=3 || !node.defending || node.complete || node.hitPoints!=1)
                        throw new Exception("Narrative migration changed battle progress");
                    var part=new QuestPart_YunjianziNode {nodeIndex=0};
                    if(!part.DescriptionPart.Contains("0/2") || !part.DescriptionPart.Contains("0/5"))
                        throw new Exception("Final defense incorrectly counted as eradication");
                    node.complete=true;node.defending=false;
                    if(!part.DescriptionPart.Contains("1/2") || !part.DescriptionPart.Contains("1/5"))
                        throw new Exception("Quest description counter did not refresh");
                    pursuit.nodes.Add(new YunjianziNodeState {index=1,complete=true});
                    if(!pursuit.ProgressText(2).Contains("0/3") || !pursuit.ProgressText(2).Contains("2/5"))
                        throw new Exception("Space counter includes surface completions");
                    for(int i=2;i<5;i++)pursuit.nodes.Add(new YunjianziNodeState {index=i,complete=true});
                    if(!pursuit.ProgressText(4).Contains("3/3") || !pursuit.ProgressText(4).Contains("5/5"))
                        throw new Exception("Final counts incorrect");
                    if(GameComponent_YunjianziPursuit.QuestBody(0)!=GameComponent_YunjianziPursuit.QuestBody(1)
                       || GameComponent_YunjianziPursuit.QuestBody(2)!=GameComponent_YunjianziPursuit.QuestBody(4))
                        throw new Exception("Stage tasks do not share prose");
                    foreach(string key in new[]{"Reveal","SurfaceTask","Space","SpaceTask","Final","Phase","Cleared"})
                    {
                        string prose=GameComponent_YunjianziPursuit.Text(key);
                        if(prose.StartsWith("YJZ_Story_") || new[]{"众人","同伴","队伍","取出前代手记","观测台","舱室"}.Any(prose.Contains))
                            throw new Exception("Missing or obsolete prose: "+key);
                    }
                }
                finally {pursuit.nodes=original;}
            });
            Check("Pursuit node footprints, atlas meshes and rendering", () =>
            {
                var type = typeof(Building_YunjianziNode);
                var meshMethod = AccessTools.DeclaredMethod(type, "NodeMesh");
                var materialMethod = AccessTools.DeclaredMethod(type, "NodeMaterial");
                var drawMethod = AccessTools.DeclaredMethod(type, "DrawAt");
                for (int i = 0; i < 5; i++)
                {
                    int size = i < 2 ? 5 : 7;
                    var def = DefDatabase<ThingDef>.GetNamed(GameComponent_YunjianziPursuit.NodeDefNames[i]);
                    if (def.size != new IntVec2(size, size) || def.drawerType != DrawerType.RealtimeOnly || def.rotatable)
                        throw new Exception("Wrong footprint/drawer: " + def.defName);
                    var thing = ThingMaker.MakeThing(def);
                    foreach (int hp in new[] { thing.MaxHitPoints, 1 })
                    {
                        thing.HitPoints = hp;
                        drawMethod.Invoke(thing, new object[] { Vector3.zero, false });
                    }
                }
                for (int tier = 0; tier < 2; tier++)
                {
                    var mat = (Material)materialMethod.Invoke(null, new object[] { tier });
                    if (mat.mainTexture.width != 2172 || mat.mainTexture.height != 724)
                        throw new Exception("Missing or incorrectly sized atlas");
                    for (int frame = 0; frame < 3; frame++)
                    {
                        var mesh = (Mesh)meshMethod.Invoke(null, new object[] { tier, frame });
                        if (mesh.vertexCount != 4 || mesh.bounds.size.x > (tier == 0 ? 5.01f : 7.01f))
                            throw new Exception("Mesh exceeds footprint");
                        if (mesh.vertices.Min(v => v.z) != 0f || mesh.uv.Any(v => v.x < frame / 3f || v.x > (frame + 1) / 3f || v.y < 0 || v.y > 1))
                            throw new Exception("Incorrect ground anchor or atlas frame");
                    }
                }
            });
            Check("Legacy node Defs, numeric IDs and saved references migrate", () =>
            {
                var directory=new LoadedObjectDirectory();
                for(int i=0;i<5;i++)
                {
                    string legacy="YJZ_PursuitNode"+i;
                    var xml=new System.Xml.XmlDocument();xml.LoadXml("<def>"+legacy+"</def>");
                    var def=ScribeExtractor.DefFromNode<ThingDef>(xml.DocumentElement);
                    if(def==null || def.defName!=GameComponent_YunjianziPursuit.NodeDefNames[i])
                        throw new Exception("Old saved Def did not migrate: "+legacy);
                    if(char.IsDigit(def.defName[def.defName.Length-1]) || def.ConfigErrors().Any())
                        throw new Exception("Invalid node Def: "+string.Join("; ",def.ConfigErrors()));
                    var node=(Building_YunjianziNode)ThingMaker.MakeThing(def);
                    node.ThingID=legacy+(12340+i);
                    node.HitPoints=1234;
                    if(node.thingIDNumber!=12340+i)throw new Exception("Node index leaked into instance ID");
                    directory.RegisterLoaded(node);
                    if(directory.ObjectWithLoadID<Thing>("Thing_"+legacy+(12340+i))!=node)
                        throw new Exception("Legacy node reference was lost");
                    if(node.HitPoints!=1234)throw new Exception("Node health changed");
                    if(DefDatabase<ThingDef>.GetNamedSilentFail(legacy)!=null)throw new Exception("Invalid legacy Def still registered");
                }
                if(BackCompatibility.BackCompatibleDefName(typeof(ThingDef),"Steel")!="Steel"
                   || Thing.IDNumberFromThingID("Steel123")!=123)
                    throw new Exception("Unrelated vanilla identifiers changed");
            });
            Check("Legacy qilin corpse filters resolve without restoring new corpses", () =>
            {
                foreach(string suffix in new[]{"Purple","Golden","Spirit"})
                {
                    string raceName="YJZ_SkyWyvern_Race_"+suffix;
                    var race=DefDatabase<ThingDef>.GetNamed(raceName);
                    var xml=new System.Xml.XmlDocument();xml.LoadXml("<li>Corpse_"+raceName+"</li>");
                    var corpse=ScribeExtractor.DefFromNode<ThingDef>(xml.DocumentElement);
                    if(corpse==null || corpse.thingClass!=typeof(Corpse) || corpse.ingestible.sourceDef!=race)
                        throw new Exception("Old corpse reference is not a matching corpse Def");
                    if(race.race.hasCorpse || race.race.hasMeat || race.race.corpseDef!=null || race.race.leatherDef!=null)
                        throw new Exception("Qilin death/resource behavior changed");
                    if(corpse.ConfigErrors().Any())throw new Exception(string.Join("; ",corpse.ConfigErrors()));
                    var filter=new ThingFilter();filter.SetAllow(corpse,true);
                    if(!filter.Allows(corpse))throw new Exception("Old food/storage filter reference unusable");
                    if(DefDatabase<ThingDef>.AllDefs.Count(d=>d.defName==corpse.defName)!=1)
                        throw new Exception("Duplicate compatibility corpse Def");
                }
            });
            Check("RenderTexture Blit ReadPixels", () =>
            {
                var source = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                var readback = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                var previous = RenderTexture.active;
                var target = RenderTexture.GetTemporary(8, 8, 0, RenderTextureFormat.ARGB32);
                try
                {
                    source.SetPixels(Enumerable.Repeat(Color.red, 64).ToArray());
                    source.Apply();
                    Graphics.Blit(source, target);
                    RenderTexture.active = target;
                    readback.ReadPixels(new Rect(0, 0, 8, 8), 0, 0);
                    readback.Apply();
                    var pixel = readback.GetPixel(4, 4);
                    if (pixel.r < .8f || pixel.g > .2f || pixel.b > .2f) throw new Exception("Unexpected readback " + pixel);
                }
                finally
                {
                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(target);
                    UnityEngine.Object.Destroy(source);
                    UnityEngine.Object.Destroy(readback);
                }
            });
            Current.Game = previousGame;
            File.WriteAllLines(Path.Combine(GenFilePaths.SaveDataFolderPath, "yunjianzi-runtime-checks.txt"), results);
            foreach (string result in results) Log.Message("[YJZ 1.7 Audit] " + result);
            Log.Message("[YJZ 1.7 Audit] COMPLETE");
            Application.Quit();
        }
    }
}
