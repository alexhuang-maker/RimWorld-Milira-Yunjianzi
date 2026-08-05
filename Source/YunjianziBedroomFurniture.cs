using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    public sealed class CompProperties_YunjianziSereneBed : CompProperties
    {
        // Natural recovery is 0.5% per in-game second. This extra 0.05% makes
        // sleeping in the bed recover spiritual energy at 1.1 times the base rate.
        public float extraEnergyFractionPerSecond = 0.0005f;

        public CompProperties_YunjianziSereneBed()
        {
            compClass = typeof(CompYunjianziSereneBed);
        }
    }

    public sealed class CompYunjianziSereneBed : ThingComp
    {
        public CompProperties_YunjianziSereneBed Props
        {
            get { return (CompProperties_YunjianziSereneBed)props; }
        }

        public override void CompTick()
        {
            base.CompTick();
            Building_Bed bed = parent as Building_Bed;
            if (bed == null || !bed.Spawned || (Find.TickManager.TicksGame + parent.thingIDNumber) % 60 != 0)
                return;

            foreach (Pawn pawn in bed.CurOccupants)
            {
                if (pawn == null || pawn.Dead || pawn.Awake()) continue;
                CompYunjianziCultivation cultivation = pawn.TryGetComp<CompYunjianziCultivation>();
                if (cultivation == null || !cultivation.IsYunjianzi) continue;
                cultivation.Energy += cultivation.EnergyMax * Props.extraEnergyFractionPerSecond;
            }
        }

        public override string CompInspectStringExtra()
        {
            return "云间子在此入睡时，灵力恢复速度为自然恢复的1.1倍，并免疫普通睡眠打扰。";
        }
    }

    internal static class YunjianziSereneBedUtility
    {
        internal static bool IsProtectedSleeper(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Awake()) return false;
            Building_Bed bed = pawn.CurrentBed();
            return bed != null && bed.GetComp<CompYunjianziSereneBed>() != null;
        }
    }

    // United Light Industries prevents the memory at its insertion point rather
    // than removing it later.  Doing the same avoids a polling scan and a one-tick
    // mood flicker while preserving wake-ups caused by danger, injury and fire.
    [HarmonyPatch(typeof(MemoryThoughtHandler), nameof(MemoryThoughtHandler.TryGainMemory),
        new Type[] { typeof(ThoughtDef), typeof(Pawn), typeof(Precept) })]
    internal static class Patch_SereneBed_BlockDisturbedSleepDef
    {
        private static bool Prefix(ThoughtDef def, Pawn ___pawn)
        {
            return def != ThoughtDefOf.SleepDisturbed || !YunjianziSereneBedUtility.IsProtectedSleeper(___pawn);
        }
    }

    [HarmonyPatch(typeof(MemoryThoughtHandler), nameof(MemoryThoughtHandler.TryGainMemory),
        new Type[] { typeof(Thought_Memory), typeof(Pawn) })]
    internal static class Patch_SereneBed_BlockDisturbedSleepMemory
    {
        private static bool Prefix(Thought_Memory newThought, Pawn ___pawn)
        {
            return newThought == null || newThought.def != ThoughtDefOf.SleepDisturbed ||
                !YunjianziSereneBedUtility.IsProtectedSleeper(___pawn);
        }
    }

    public sealed class PlaceWorker_YunjianziWallAdjacent : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
            Thing thingToIgnore = null, Thing thing = null)
        {
            CellRect occupied = GenAdj.OccupiedRect(loc, rot, checkingDef.Size);
            IntVec3 facing = rot.FacingCell;
            foreach (IntVec3 cell in occupied.Cells)
            {
                IntVec3 wallCell = cell + facing;
                if (!wallCell.InBounds(map)) continue;
                Building edifice = wallCell.GetEdifice(map);
                if (edifice != null && edifice.def.IsWall) return true;
            }
            return "悬书架背面必须正对并紧贴一面墙。";
        }
    }
}
