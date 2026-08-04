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
            return string.Format("打坐修行效率：约 {0:0.#} 倍\n云间旗帜：{1} / 4（每面 +25%）\n突破仪台加速：+{2:0}%",
                TotalRecoveryFactor, LinkedBannerCount, Props.breakthroughPower * 100f);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
            if (!parent.Spawned || parent.Faction != Faction.OfPlayer) yield break;

            Pawn yunjianzi = parent.Map.mapPawns.FreeColonistsSpawned.FirstOrDefault(pawn =>
            {
                CompYunjianziCultivation cultivation = pawn.TryGetComp<CompYunjianziCultivation>();
                return cultivation != null && cultivation.IsYunjianzi && cultivation.CanBreakthrough;
            });
            if (yunjianzi == null) yield break;

            CompYunjianziCultivation comp = yunjianzi.TryGetComp<CompYunjianziCultivation>();
            Command_Action breakthrough = new Command_Action
            {
                defaultLabel = "命云间子闭关突破",
                defaultDesc = "让云间子返回此修行设施闭关突破。突破期间无法移动，但紫府境之后的剑影仍会自主御敌。",
                icon = YunjianziAbilityTextures.Meditate,
                action = delegate { comp.BeginMeditationOrBreakthrough(parent as Building); }
            };
            if (comp.Realm >= 3 && parent.def != YJZDefOf.YJZ_Building_WhiteJadePlatform)
                breakthrough.Disable("突破金丹及之后必须使用白玉修行台。");
            yield return breakthrough;
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
}
