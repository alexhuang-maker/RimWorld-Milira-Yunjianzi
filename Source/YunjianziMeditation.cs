using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Yunjianzi
{
    public sealed class CompProperties_YunjianziMeditationSeat : CompProperties
    {
        public float extraEnergyPerSecond = 0.01f;
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
                        facility.def == YJZDefOf.YJZ_Building_YunjianziBanner));
            }
        }

        public float TotalRecoveryFactor
        {
            get
            {
                float baseFactor = 1f + Props.extraEnergyPerSecond / 0.005f;
                return baseFactor * (1f + LinkedBannerCount * 0.25f);
            }
        }

        public override string CompInspectStringExtra()
        {
            return string.Format("打坐灵力恢复：自然恢复的约 {0:0.#} 倍\n云间旗帜：{1} / 4（每面 +25%）\n突破仪台强度：{2:0.##}",
                TotalRecoveryFactor, LinkedBannerCount, Props.breakthroughPower);
        }
    }

    public sealed class JobDriver_YunjianziMeditate : JobDriver
    {
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
                    const float naturalRecoveryPerSecond = 0.005f;
                    float bannerFactor = 1f + seatComp.LinkedBannerCount * 0.25f;
                    float meditationRecovery =
                        (naturalRecoveryPerSecond + seatComp.Props.extraEnergyPerSecond) * bannerFactor -
                        naturalRecoveryPerSecond;
                    cultivation.Energy += cultivation.EnergyMax * meditationRecovery;
                    if (cultivation.Energy >= cultivation.EnergyMax - 0.01f)
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
}
