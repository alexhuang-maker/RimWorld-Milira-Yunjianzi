using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Yunjianzi
{
    public sealed class CompProperties_YunjianziBoundScroll : CompProperties
    {
        public CompProperties_YunjianziBoundScroll()
        {
            compClass = typeof(CompYunjianziBoundScroll);
        }
    }

    public sealed class CompYunjianziBoundScroll : ThingComp
    {
        private string boundPawnId;

        public void BindTo(Pawn pawn)
        {
            boundPawnId = pawn == null ? null : pawn.GetUniqueLoadID();
        }

        public bool IsBoundTo(Pawn pawn)
        {
            return pawn != null && !boundPawnId.NullOrEmpty() && boundPawnId == pawn.GetUniqueLoadID();
        }

        public override string CompInspectStringExtra()
        {
            return "此宝卷只回应云间子的神魂印记。研读10秒后消耗，使初始灵力上限提升至10点。";
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref boundPawnId, "yjz_boundPawnId");
        }
    }

    [StaticConstructorOnStartup]
    internal static class YunjianziBreakthroughTextures
    {
        public static readonly Texture2D BoundScroll =
            ContentFinder<Texture2D>.Get("Yunjianzi/Items/BoundImmortalScroll", true);
    }

    public sealed partial class CompYunjianziCultivation
    {
        private float cultivatedEnergyMax = -1f;
        private bool boundScrollCreated;
        private bool boundScrollConsumed;

        public bool HasOpenedBoundScroll { get { return boundScrollConsumed; } }
        public bool CanBreakthrough
        {
            get
            {
                return boundScrollConsumed && Realm >= 1 && Realm < 5 &&
                    EnergyMax >= RealmEnergyCap - 0.001f;
            }
        }

        public float CultivationGainPerHour { get { return Realm >= 3 ? 20f : 10f; } }

        private void InitializeCultivationProgress()
        {
            if (cultivatedEnergyMax < 0f)
                cultivatedEnergyMax = boundScrollConsumed ? 10f : 0f;
        }

        private void TryCreateBoundScroll()
        {
            if (boundScrollCreated || boundScrollConsumed || Pawn == null || !Pawn.Spawned || Pawn.Map == null)
                return;

            ThingWithComps scroll = ThingMaker.MakeThing(YJZDefOf.YJZ_Item_BoundImmortalScroll) as ThingWithComps;
            if (scroll == null) return;
            CompYunjianziBoundScroll binding = scroll.TryGetComp<CompYunjianziBoundScroll>();
            if (binding != null) binding.BindTo(Pawn);
            GenSpawn.Spawn(scroll, Pawn.Position, Pawn.Map);
            scroll.SetForbidden(false, false);
            boundScrollCreated = true;
            Messages.Message("遗仙宝卷随云间子一同显现。研读宝卷可开启最初的灵力。", scroll,
                MessageTypeDefOf.PositiveEvent, false);
        }

        private Thing FindBoundScroll()
        {
            if (Pawn == null || !Pawn.Spawned || Pawn.Map == null) return null;
            foreach (Thing thing in Pawn.Map.listerThings.ThingsOfDef(YJZDefOf.YJZ_Item_BoundImmortalScroll))
            {
                CompYunjianziBoundScroll binding = thing.TryGetComp<CompYunjianziBoundScroll>();
                if (binding != null && binding.IsBoundTo(Pawn)) return thing;
            }
            return null;
        }

        private IEnumerable<Gizmo> GetBoundScrollGizmos()
        {
            if (boundScrollConsumed) yield break;
            Thing scroll = FindBoundScroll();
            if (scroll == null) yield break;

            Command_Action read = new Command_Action
            {
                defaultLabel = "研读遗仙宝卷",
                defaultDesc = "前往绑定于云间子的遗仙宝卷处研读10秒。研读完成后宝卷消耗，灵力上限提升至10点并补满。",
                icon = YunjianziBreakthroughTextures.BoundScroll,
                action = delegate
                {
                    if (Pawn == null || !Pawn.Spawned || scroll == null || !scroll.Spawned) return;
                    Job job = JobMaker.MakeJob(YJZDefOf.YJZ_ReadBoundScroll, scroll);
                    Pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
            };
            yield return read;
        }

        public void CompleteBoundScrollStudy(Thing scroll)
        {
            if (boundScrollConsumed || scroll == null) return;
            CompYunjianziBoundScroll binding = scroll.TryGetComp<CompYunjianziBoundScroll>();
            if (binding == null || !binding.IsBoundTo(Pawn)) return;

            boundScrollConsumed = true;
            cultivatedEnergyMax = Mathf.Max(cultivatedEnergyMax, 10f);
            Energy = EnergyMax;
            scroll.Destroy(DestroyMode.Vanish);
            Messages.Message("云间子悟得宝卷真意，灵力上限提升至10点。", Pawn,
                MessageTypeDefOf.PositiveEvent, false);
        }

        public bool AddCultivation(float amount, bool announceCap)
        {
            if (!boundScrollConsumed || amount <= 0f || RealmEnergyCap <= 0f) return false;
            float before = EnergyMax;
            cultivatedEnergyMax = Mathf.Clamp(cultivatedEnergyMax + amount, 0f, RealmEnergyCap);
            bool reached = before < RealmEnergyCap - 0.001f && EnergyMax >= RealmEnergyCap - 0.001f;
            if (reached && announceCap)
            {
                Messages.Message(Realm < 5
                        ? RealmLabel + "修为已臻圆满，可以在修行设施上闭关突破。"
                        : "化神境修为已臻圆满。",
                    Pawn, MessageTypeDefOf.PositiveEvent, false);
            }
            return EnergyMax > before + 0.0001f;
        }

        public void NotifyEnemyDestroyed(Pawn victim, float marketValue)
        {
            if (victim == null || marketValue <= 0f || !boundScrollConsumed) return;
            float gain = marketValue / 500f * CultivationGainPerHour;
            float before = EnergyMax;
            if (!AddCultivation(gain, true)) return;
            Messages.Message(string.Format("云间子斩除{0}，炼化战斗所得，灵力上限 +{1:0.##}。",
                victim.LabelShortCap, EnergyMax - before), Pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        public void MeditateForTicks(int ticks, CompYunjianziMeditationSeat seat)
        {
            if (!boundScrollConsumed || seat == null || ticks <= 0) return;
            const float naturalRecoveryPerSecond = 0.005f;
            float bannerFactor = 1f + seat.LinkedBannerCount * 0.25f;
            float meditationRecovery =
                (naturalRecoveryPerSecond + seat.Props.extraEnergyPerSecond) * bannerFactor -
                naturalRecoveryPerSecond;

            if (Energy < EnergyMax - 0.001f)
                Energy += EnergyMax * meditationRecovery * (ticks / 60f);

            if (Energy >= EnergyMax - 0.001f && EnergyMax < RealmEnergyCap - 0.001f)
            {
                float gain = CultivationGainPerHour * (ticks / 2500f) * seat.TotalRecoveryFactor;
                AddCultivation(gain, true);
                Energy = EnergyMax;
            }
        }

        public void BeginMeditationOrBreakthrough(Building seat)
        {
            if (seat == null || Pawn == null || !Pawn.Spawned || seat.Map != Pawn.Map) return;
            CompYunjianziMeditationSeat seatComp = seat.TryGetComp<CompYunjianziMeditationSeat>();
            if (seatComp == null) return;
            if (!boundScrollConsumed)
            {
                Messages.Message("必须先研读遗仙宝卷，才能开始修行。", Pawn, MessageTypeDefOf.RejectInput, false);
                return;
            }

            JobDef jobDef = YJZDefOf.YJZ_Meditate;
            if (CanBreakthrough)
            {
                if (Realm >= 3 && seat.def != YJZDefOf.YJZ_Building_WhiteJadePlatform)
                {
                    Messages.Message("突破金丹及之后的境界必须使用白玉修行台。", Pawn,
                        MessageTypeDefOf.RejectInput, false);
                    return;
                }
                jobDef = YJZDefOf.YJZ_Breakthrough;
            }

            Job job = JobMaker.MakeJob(jobDef, seat.Position, seat);
            Pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        public void CompleteBreakthrough(int expectedRealm)
        {
            if (!CanBreakthrough || Realm != expectedRealm || RealmHediff == null) return;
            RealmHediff.Severity = expectedRealm + 1f;
            Shield = ShieldMax;
            Messages.Message(string.Format("云间子破境成功，踏入{0}。", RealmLabel), Pawn,
                MessageTypeDefOf.PositiveEvent, false);
        }

        private void ExposeBreakthroughData()
        {
            Scribe_Values.Look(ref cultivatedEnergyMax, "yjz_cultivatedEnergyMax", -1f);
            Scribe_Values.Look(ref boundScrollCreated, "yjz_boundScrollCreated", false);
            Scribe_Values.Look(ref boundScrollConsumed, "yjz_boundScrollConsumed", false);
        }
    }

    public sealed class JobDriver_YunjianziReadBoundScroll : JobDriver
    {
        private Thing Scroll { get { return job.GetTarget(TargetIndex.A).Thing; } }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Scroll, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil read = Toils_General.Wait(600, TargetIndex.A);
            read.WithProgressBarToilDelay(TargetIndex.A);
            yield return read;

            Toil finish = ToilMaker.MakeToil("YunjianziFinishReadingBoundScroll");
            finish.initAction = delegate
            {
                CompYunjianziCultivation cultivation = pawn.TryGetComp<CompYunjianziCultivation>();
                if (cultivation != null) cultivation.CompleteBoundScrollStudy(Scroll);
            };
            finish.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finish;
        }
    }

    public sealed class JobDriver_YunjianziBreakthrough : JobDriver
    {
        private float workLeft = -1f;
        private float initialWork;
        private int startingRealm;
        private int elapsedTicks;
        private bool stormCalled;

        private Building Seat { get { return job.GetTarget(TargetIndex.B).Thing as Building; } }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Seat, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.B);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

            Toil retreat = ToilMaker.MakeToil("YunjianziClosedDoorBreakthrough");
            retreat.defaultCompleteMode = ToilCompleteMode.Never;
            retreat.handlingFacing = true;
            retreat.initAction = delegate
            {
                CompYunjianziCultivation cultivation = pawn.TryGetComp<CompYunjianziCultivation>();
                if (cultivation == null || !cultivation.CanBreakthrough)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if (workLeft < 0f)
                {
                    startingRealm = cultivation.Realm;
                    initialWork = 60000f * Mathf.Pow(2f, startingRealm - 1);
                    workLeft = initialWork;
                }
                Messages.Message("云间子开始闭关，周遭灵气正向修行之所汇聚。", pawn,
                    MessageTypeDefOf.PositiveEvent, false);
            };
            retreat.tickAction = delegate
            {
                Building seat = Seat;
                CompYunjianziMeditationSeat seatComp = seat == null ? null : seat.TryGetComp<CompYunjianziMeditationSeat>();
                CompYunjianziCultivation cultivation = pawn.TryGetComp<CompYunjianziCultivation>();
                if (seatComp == null || cultivation == null || !cultivation.CanBreakthrough ||
                    cultivation.Realm != startingRealm)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                pawn.pather.StopDead();
                pawn.Rotation = Rot4.South;
                elapsedTicks++;
                float speed = 1f + seatComp.Props.breakthroughPower + seatComp.LinkedBannerCount * 0.25f;
                workLeft -= speed;

                if (!stormCalled && elapsedTicks >= 2500 && pawn.Map != null)
                {
                    WeatherDef thunderstorm = DefDatabase<WeatherDef>.GetNamedSilentFail("RainyThunderstorm");
                    if (thunderstorm != null) pawn.Map.weatherManager.TransitionTo(thunderstorm);
                    stormCalled = true;
                    Messages.Message("闭关引动天象，雷雨汇聚于云间子所在之地。", pawn,
                        MessageTypeDefOf.PositiveEvent, false);
                }

                if (pawn.IsHashIntervalTick(30) && pawn.Spawned)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        IntVec3 cell = CellFinder.RandomClosewalkCellNear(pawn.Position, pawn.Map, 4);
                        FleckMaker.Static(cell, pawn.Map, FleckDefOf.PsycastAreaEffect, Rand.Range(0.3f, 0.65f));
                    }
                    FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 0.55f);
                }

                if (workLeft <= 0f)
                {
                    cultivation.CompleteBreakthrough(startingRealm);
                    EndJobWith(JobCondition.Succeeded);
                }
            };
            retreat.WithProgressBar(TargetIndex.B, delegate
            {
                return initialWork <= 0f ? 0f : Mathf.Clamp01(1f - workLeft / initialWork);
            });
            yield return retreat;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workLeft, "yjz_breakthroughWorkLeft", -1f);
            Scribe_Values.Look(ref initialWork, "yjz_breakthroughInitialWork", 0f);
            Scribe_Values.Look(ref startingRealm, "yjz_breakthroughStartingRealm", 0);
            Scribe_Values.Look(ref elapsedTicks, "yjz_breakthroughElapsedTicks", 0);
            Scribe_Values.Look(ref stormCalled, "yjz_breakthroughStormCalled", false);
        }
    }

    [HarmonyPatch(typeof(Pawn), "Kill", new Type[] { typeof(DamageInfo?), typeof(Hediff) })]
    internal static class Patch_Pawn_Kill_YunjianziCultivationReward
    {
        internal struct KillState
        {
            public Pawn attacker;
            public float marketValue;
            public bool valid;
        }

        private static void Prefix(Pawn __instance, DamageInfo? dinfo, out KillState __state)
        {
            __state = new KillState();
            if (__instance == null || __instance.Dead || !dinfo.HasValue) return;
            Pawn attacker = dinfo.Value.Instigator as Pawn;
            CompYunjianziCultivation cultivation = attacker == null ? null : attacker.TryGetComp<CompYunjianziCultivation>();
            if (cultivation == null || !cultivation.IsYunjianzi || !__instance.HostileTo(attacker)) return;

            __state.attacker = attacker;
            __state.marketValue = Mathf.Max(0f, __instance.GetStatValue(StatDefOf.MarketValue));
            __state.valid = true;
        }

        private static void Postfix(Pawn __instance, KillState __state)
        {
            if (!__state.valid || __instance == null || !__instance.Dead || __state.attacker == null) return;
            CompYunjianziCultivation cultivation = __state.attacker.TryGetComp<CompYunjianziCultivation>();
            if (cultivation != null) cultivation.NotifyEnemyDestroyed(__instance, __state.marketValue);
        }
    }
}
