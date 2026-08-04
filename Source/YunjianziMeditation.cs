using System.Collections.Generic;
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

        public override string CompInspectStringExtra()
        {
            float totalFactor = 1f + Props.extraEnergyPerSecond / 0.005f;
            return string.Format("打坐灵力恢复：自然恢复的约 {0:0.#} 倍\n突破仪台强度：{1:0.##}", totalFactor, Props.breakthroughPower);
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
                    cultivation.Energy += cultivation.EnergyMax * seatComp.Props.extraEnergyPerSecond;
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
