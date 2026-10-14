using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Yunjianzi
{
    /// <summary>
    /// Building extension Defs are optional from the character mod's point of
    /// view. Resolve them lazily so the core mod can load without the separate
    /// building package and without DefOf missing-field errors.
    /// </summary>
    internal static class YunjianziOptionalBuildingDefs
    {
        internal static ThingDef Banner => DefDatabase<ThingDef>.GetNamedSilentFail("YJZ_Building_YunjianziBanner");
        internal static ThingDef SpiritConvergencePool => DefDatabase<ThingDef>.GetNamedSilentFail("YJZ_Building_SpiritConvergencePool");
        internal static ThingDef CelestialDaoGate => DefDatabase<ThingDef>.GetNamedSilentFail("YJZ_Building_CelestialDaoGate");
        internal static ThingDef MeditationCushion => DefDatabase<ThingDef>.GetNamedSilentFail("YJZ_Building_MeditationCushion");
        internal static ThingDef WhiteJadePlatform => DefDatabase<ThingDef>.GetNamedSilentFail("YJZ_Building_WhiteJadePlatform");

        internal static IEnumerable<Thing> ThingsOfDef(Map map, ThingDef def)
        {
            return map == null || def == null
                ? Enumerable.Empty<Thing>()
                : map.listerThings.ThingsOfDef(def);
        }
    }

    public sealed class CompProperties_YunjianziMeditationSeat : CompProperties
    {
        public float baseRecoveryFactor = 1f;
        public float breakthroughPower;

        public CompProperties_YunjianziMeditationSeat()
        {
            compClass = typeof(CompYunjianziMeditationSeat);
        }
    }

    public sealed class CompYunjianziMeditationSeat : ThingComp
    {
        public CompProperties_YunjianziMeditationSeat Props
        {
            get { return (CompProperties_YunjianziMeditationSeat)props; }
        }

        public int LinkedBannerCount
        {
            get
            {
                CompAffectedByFacilities affected = parent.TryGetComp<CompAffectedByFacilities>();
                if (affected == null) return 0;
                return System.Math.Min(4, affected.LinkedFacilitiesListForReading
                    .Count(facility => facility != null && facility.Spawned &&
                        facility.def == YunjianziOptionalBuildingDefs.Banner));
            }
        }

        private static bool IsOperationalFacility(Thing facility)
        {
            if (facility == null || !facility.Spawned) return false;
            CompPowerTrader power = facility.TryGetComp<CompPowerTrader>();
            return power == null || (power.PowerOn && power.PowerOutput > 0.01f);
        }

        private bool HasLinkedOperationalFacility(ThingDef facilityDef)
        {
            CompAffectedByFacilities affected = parent.TryGetComp<CompAffectedByFacilities>();
            return affected != null && affected.LinkedFacilitiesListForReading.Any(
                facility => facility != null && facility.def == facilityDef && IsOperationalFacility(facility));
        }

        public bool HasSpiritConvergencePool
        {
            get { return HasLinkedOperationalFacility(YunjianziOptionalBuildingDefs.SpiritConvergencePool); }
        }

        public bool HasCelestialDaoGate
        {
            get { return HasLinkedOperationalFacility(YunjianziOptionalBuildingDefs.CelestialDaoGate); }
        }

        public float AdvancedRecoveryBonus
        {
            get { return (HasSpiritConvergencePool ? 0.5f : 0f) + (HasCelestialDaoGate ? 1f : 0f); }
        }

        public float AdvancedBreakthroughBonus
        {
            get { return (HasSpiritConvergencePool ? 0.25f : 0f) + (HasCelestialDaoGate ? 0.5f : 0f); }
        }

        public float RecoverySupportBonus
        {
            get { return LinkedBannerCount * 0.25f + AdvancedRecoveryBonus; }
        }

        public float TotalRecoveryFactor
        {
            get
            {
                return Props.baseRecoveryFactor + RecoverySupportBonus;
            }
        }

        public override string CompInspectStringExtra()
        {
            return string.Format("打坐修行效率：约 {0:0.#} 倍\n云间旗帜：{1} / 4（每面 +25%）\n青灵聚元池：{2}\n云阙问道仪：{3}\n突破总加速：+{4:0}%",
                TotalRecoveryFactor, LinkedBannerCount,
                HasSpiritConvergencePool ? "已连接（恢复 +50%，突破 +25%）" : "未连接或未工作",
                HasCelestialDaoGate ? "已连接（恢复 +100%，突破 +50%）" : "未连接或未工作",
                (Props.breakthroughPower + LinkedBannerCount * 0.25f + AdvancedBreakthroughBonus) * 100f);
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.CompFloatMenuOptions(selPawn)) yield return option;
            if (!parent.Spawned || parent.Faction != Faction.OfPlayer || selPawn == null) yield break;
            CompYunjianziCultivation comp = selPawn.TryGetComp<CompYunjianziCultivation>();
            if (comp == null || !comp.IsYunjianzi || selPawn.Dead) yield break;

            string actionLabel = comp.CanBreakthrough ? "闭关突破" : "打坐修行";
            FloatMenuOption meditation = new FloatMenuOption(actionLabel, delegate
            {
                comp.BeginMeditationOrBreakthrough(parent as Building);
            });
            string disabledReason = null;
            if (selPawn.Downed) disabledReason = "云间子已倒地";
            else if (!comp.HasOpenedBoundScroll) disabledReason = "尚未研读遗仙宝卷";
            if (comp.CanBreakthrough && comp.Realm >= 3 && parent.def != YunjianziOptionalBuildingDefs.WhiteJadePlatform)
                disabledReason = "突破金丹及之后必须使用白玉修行台";
            else if (!selPawn.CanReserveAndReach(parent, PathEndMode.OnCell, Danger.Deadly))
                disabledReason = "无法抵达或设施已被占用";
            if (disabledReason != null)
            {
                meditation.Disabled = true;
                meditation.Label = actionLabel + "（" + disabledReason + "）";
            }
            yield return meditation;
        }
    }

    public sealed class JobDriver_YunjianziMeditate : JobDriver
    {
        // Yunjianzi's south-facing portrait extends much farther toward the
        // camera than a vanilla pawn.  Move only the rendered body north while
        // meditating so it sits in the platform's inner ring and leaves the
        // front of the cushion visible; the pawn's actual occupied cell stays
        // unchanged.
        public override Vector3 ForcedBodyOffset
        {
            get
            {
                Building seat = Seat;
                if (seat == null) return Vector3.zero;
                return seat.def == YunjianziOptionalBuildingDefs.WhiteJadePlatform
                    // The 3x3 platform has a true center cell, while the
                    // sitting disc is about 13% north of the asymmetric
                    // texture's geometric center.  Yunjianzi's large south-
                    // facing graphic needs a little more northward correction
                    // to read as seated on that disc.
                    ? new Vector3(0f, 0f, 0.60f)
                    : new Vector3(0f, 0f, 0.28f);
            }
        }

        private Building Seat
        {
            get { return job.GetTarget(TargetIndex.B).Thing as Building; }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.B), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.B);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

            Toil meditate = ToilMaker.MakeToil("YunjianziMeditate");
            meditate.defaultCompleteMode = ToilCompleteMode.Never;
            meditate.handlingFacing = true;
            meditate.tickAction = delegate
            {
                Building seat = Seat;
                CompYunjianziMeditationSeat seatComp = seat == null ? null : seat.TryGetComp<CompYunjianziMeditationSeat>();
                CompYunjianziCultivation cultivation = pawn.TryGetComp<CompYunjianziCultivation>();
                if (seatComp == null || cultivation == null || !cultivation.IsYunjianzi)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                pawn.Rotation = Rot4.South;
                if (pawn.IsHashIntervalTick(60))
                {
                    // Ordered right-click meditation remains in force until the
                    // player cancels it.  Schedule-created meditation yields as
                    // soon as the timetable leaves Cultivate/Meditate.
                    if (!job.playerForced && job.workGiverDef == null &&
                        !YunjianziCultivationSchedule.IsCultivationTime(pawn))
                    {
                        EndJobWith(JobCondition.Succeeded);
                        return;
                    }

                    cultivation.MeditateForTicks(60, seatComp);
                    if (!cultivation.HasOpenedBoundScroll ||
                        (cultivation.Energy >= cultivation.EnergyMax - 0.01f &&
                         cultivation.EnergyMax >= cultivation.RealmEnergyCap - 0.01f))
                    {
                        EndJobWith(JobCondition.Succeeded);
                        return;
                    }
                }
                if (pawn.IsHashIntervalTick(120) && pawn.Spawned)
                    FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect, 0.45f);
            };
            yield return meditate;
        }
    }

    /// <summary>
    /// Normal work-tab entry for cultivation. Only the two purpose-built seats
    /// are candidates; vanilla meditation spots and bare cells are never used.
    /// </summary>
    public sealed class WorkGiver_YunjianziCultivate : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.OnCell;
        public override bool Prioritized => true;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null) return Enumerable.Empty<Thing>();
            return YunjianziOptionalBuildingDefs.ThingsOfDef(
                    pawn.Map, YunjianziOptionalBuildingDefs.WhiteJadePlatform)
                .Concat(YunjianziOptionalBuildingDefs.ThingsOfDef(
                    pawn.Map, YunjianziOptionalBuildingDefs.MeditationCushion));
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            CompYunjianziCultivation cultivation;
            if (!YunjianziCultivationSchedule.CanAutoCultivate(pawn, out cultivation)) return false;
            if (!(t is Building) || !t.Spawned || t.Faction != pawn.Faction || t.IsForbidden(pawn)) return false;
            if (t.def != YunjianziOptionalBuildingDefs.MeditationCushion &&
                t.def != YunjianziOptionalBuildingDefs.WhiteJadePlatform) return false;
            if (t.TryGetComp<CompYunjianziMeditationSeat>() == null) return false;
            return pawn.CanReserveAndReach(t, PathEndMode.OnCell,
                forced ? Danger.Deadly : pawn.NormalMaxDanger());
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!HasJobOnThing(pawn, t, forced)) return null;
            Job job = JobMaker.MakeJob(YJZDefOf.YJZ_Meditate, t.Position, t);
            job.expiryInterval = 500;
            job.checkOverrideOnExpire = true;
            return job;
        }

        public override float GetPriority(Pawn pawn, TargetInfo t)
        {
            CompYunjianziMeditationSeat seat = t.Thing?.TryGetComp<CompYunjianziMeditationSeat>();
            return seat == null ? 0f : seat.TotalRecoveryFactor;
        }
    }

    /// <summary>
    /// Timetable bridge for Yunjianzi.  The custom assignment deliberately
    /// borrows vanilla Meditate's priority calculation, while the actual job
    /// is replaced with cultivation on the best reachable cultivation seat.
    /// </summary>
    internal static class YunjianziCultivationSchedule
    {
        internal static TimeAssignmentDef RawAssignment(Pawn pawn)
        {
            if (pawn == null || pawn.timetable == null) return null;
            return pawn.timetable.GetAssignment(GenLocalDate.HourOfDay(pawn));
        }

        internal static bool IsCultivationTime(Pawn pawn)
        {
            TimeAssignmentDef assignment = RawAssignment(pawn);
            return assignment == YJZDefOf.YJZ_Cultivate || assignment == TimeAssignmentDefOf.Meditate;
        }

        internal static bool CanAutoCultivate(Pawn pawn, out CompYunjianziCultivation cultivation)
        {
            cultivation = pawn == null ? null : pawn.TryGetComp<CompYunjianziCultivation>();
            if (cultivation == null || !cultivation.IsYunjianzi || !cultivation.HasOpenedBoundScroll)
                return false;
            if (cultivation.AutoCultivationCoolingDown) return false;
            if (!pawn.Spawned || pawn.Dead || pawn.Downed || pawn.Drafted || pawn.InMentalState)
                return false;
            return cultivation.Energy < cultivation.EnergyMax - 0.01f ||
                   cultivation.EnergyMax < cultivation.RealmEnergyCap - 0.01f;
        }

        internal static Building BestReachableSeat(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null) return null;
            IEnumerable<Thing> candidates =
                YunjianziOptionalBuildingDefs.ThingsOfDef(pawn.Map, YunjianziOptionalBuildingDefs.WhiteJadePlatform)
                    .Concat(YunjianziOptionalBuildingDefs.ThingsOfDef(
                        pawn.Map, YunjianziOptionalBuildingDefs.MeditationCushion));

            return candidates
                .OfType<Building>()
                .Where(seat => seat.Spawned && seat.Faction == pawn.Faction &&
                    !seat.IsForbidden(pawn) &&
                    seat.TryGetComp<CompYunjianziMeditationSeat>() != null &&
                    pawn.CanReserveAndReach(seat, PathEndMode.OnCell, Danger.Deadly))
                .OrderByDescending(seat => seat.TryGetComp<CompYunjianziMeditationSeat>().TotalRecoveryFactor)
                .ThenBy(seat => pawn.Position.DistanceToSquared(seat.Position))
                .FirstOrDefault();
        }
    }

    /// <summary>
    /// Make the custom timetable assignment participate in the exact same
    /// vanilla priority path as Royalty's Meditate assignment, but only for
    /// Yunjianzi.  The schedule grid itself still stores and displays Cultivate.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_TimetableTracker), "get_CurrentAssignment")]
    internal static class Patch_CurrentAssignment_YunjianziCultivate
    {
        private static void Postfix(Pawn ___pawn, ref TimeAssignmentDef __result)
        {
            if (__result != YJZDefOf.YJZ_Cultivate || ___pawn == null) return;
            CompYunjianziCultivation cultivation = ___pawn.TryGetComp<CompYunjianziCultivation>();
            if (cultivation != null && cultivation.IsYunjianzi)
                __result = TimeAssignmentDefOf.Meditate;
        }
    }

    /// <summary>
    /// During either Cultivate or vanilla Meditate time, send Yunjianzi to the
    /// most efficient free cultivation seat.  Vanilla meditation remains the
    /// fallback for an explicit Meditate block if no cultivation seat is usable.
    /// </summary>
    [HarmonyPatch(typeof(JobGiver_Meditate), "TryGiveJob")]
    internal static class Patch_JobGiverMeditate_YunjianziCultivate
    {
        private static bool Prefix(Pawn pawn, ref Job __result)
        {
            TimeAssignmentDef rawAssignment = YunjianziCultivationSchedule.RawAssignment(pawn);
            bool customAssignment = rawAssignment == YJZDefOf.YJZ_Cultivate;
            if (!customAssignment && rawAssignment != TimeAssignmentDefOf.Meditate) return true;

            CompYunjianziCultivation cultivation;
            if (YunjianziCultivationSchedule.CanAutoCultivate(pawn, out cultivation))
            {
                Building seat = YunjianziCultivationSchedule.BestReachableSeat(pawn);
                if (seat != null)
                {
                    Job job = JobMaker.MakeJob(YJZDefOf.YJZ_Meditate, seat.Position, seat);
                    job.expiryInterval = 300;
                    job.checkOverrideOnExpire = true;
                    __result = job;
                    return false;
                }
            }

            // A dedicated Cultivate block never degrades into unrelated vanilla
            // meditation.  An explicit Meditate block may still use vanilla's
            // meditation spot when no cultivation seat is available.
            if (customAssignment)
            {
                __result = null;
                return false;
            }
            return true;
        }
    }
}
