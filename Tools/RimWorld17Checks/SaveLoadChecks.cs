using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace Yunjianzi
{
    [StaticConstructorOnStartup]
    internal static class YunjianziSaveLoadChecks
    {
        private static bool started, finished;
        private static int frames;
        private static bool roundtrip;
        private static int loadErrors;
        private static readonly List<string> errors=new List<string>();
        static YunjianziSaveLoadChecks()
        {
            if(!GenCommandLine.CommandLineArgPassed("YJZSaveAudit"))return;
            var h=new Harmony("yunjianzi.development.saveaudit");
            h.Patch(AccessTools.Method(typeof(UIRoot_Entry),nameof(UIRoot_Entry.UIRootUpdate)),
                postfix:new HarmonyMethod(typeof(YunjianziSaveLoadChecks),nameof(Start)));
            h.Patch(AccessTools.Method(typeof(Root_Play),"Update"),
                postfix:new HarmonyMethod(typeof(YunjianziSaveLoadChecks),nameof(AfterPlayUpdate)));
            foreach(var method in typeof(Log).GetMethods().Where(m=>m.Name=="Error" && m.GetParameters().Length>0 && m.GetParameters()[0].ParameterType==typeof(string)))
                h.Patch(method,prefix:new HarmonyMethod(typeof(YunjianziSaveLoadChecks),nameof(CaptureError)));
        }
        private static void CaptureError(string __0)
        {
            lock(errors) if(errors.Count<4000)errors.Add(__0);
        }
        private static void Start()
        {
            if(started || Find.ScreenshotModeHandler==null)return;
            started=true;
            Log.Message("[YJZ Save Audit] Loading isolated save copy");
            GameDataSaveLoader.LoadGame("云间行");
        }
        private static void AfterPlayUpdate()
        {
            if(!started || finished || Current.ProgramState!=ProgramState.Playing || LongEventHandler.AnyEventNowOrWaiting)return;
            if(frames==0) { lock(errors) loadErrors=errors.Count; }
            if(++frames<180)return;
            finished=true;
            var result=new List<string>();
            try
            {
                if(Find.Maps.Count==0)throw new Exception("No maps loaded");
                result.Add("Loaded mods="+LoadedModManager.RunningModsListForReading.Count());
                foreach(var map in Find.Maps)
                {
                    if(map.areaManager?.Home==null || map.mapDrawer==null || map.glowGrid==null)
                        throw new Exception("Map initialization incomplete");
                    result.Add($"MAP {map.uniqueID}: things={map.listerThings.AllThings.Count}, pawns={map.mapPawns.AllPawns.Count}, areas={map.areaManager.AllAreas.Count}");
                }
                result.Add("PASS save loaded and 180 play frames completed");
                result.Add($"LoadErrors={loadErrors}, PlayErrors={errors.Count-loadErrors}, Roundtrip={roundtrip}");
                lock(errors)result.AddRange(errors.Select(e=>"ERROR "+e));
                // Migration is explicit and writes only a new file in the isolated folder.
                // A second load then checks that the engine has removed obsolete references.
                if(!roundtrip && errors.Count==loadErrors &&
                    (errors.Count==0 || GenCommandLine.CommandLineArgPassed("YJZSaveMigration")))
                {
                    GameDataSaveLoader.SaveGame("云间行_1.7验证副本");
                    result.Add("Saved isolated roundtrip copy");
                    File.WriteAllLines(Path.Combine(GenFilePaths.SaveDataFolderPath,"save-audit-first-load.txt"),result);
                    roundtrip=true; frames=0; finished=false;
                    lock(errors) errors.Clear();
                    GameDataSaveLoader.LoadGame("云间行_1.7验证副本");
                    return;
                }
            }
            catch(Exception e){result.Add("FAIL "+e);}
            File.WriteAllLines(Path.Combine(GenFilePaths.SaveDataFolderPath,"save-audit.txt"),result);
            foreach(string line in result)Log.Message("[YJZ Save Audit] "+line);
            Application.Quit();
        }
    }
}
