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
            return "宝卷只回应云间子的神魂印记。静心研读后，纸上文字将沉入识海，化作最初一缕灵力。";
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
        private int autoCultivationResumeTick;

        public bool HasOpenedBoundScroll { get { return boundScrollConsumed; } }
        internal bool AutoCultivationCoolingDown
        {
            get { return Find.TickManager != null && Find.TickManager.TicksGame < autoCultivationResumeTick; }
        }
        public bool CanBreakthrough
        {
            get
            {
                return boundScrollConsumed && Realm >= 1 && Realm < 5 &&
                    EnergyMax >= RealmEnergyCap - 0.001f;
            }
        }

        // Cultivation is intended to occupy meaningful stretches of colony
        // time. The previous 10/20 points per hour made even late realms pass
        // in a handful of work sessions.
        public float CultivationGainPerHour { get { return Realm >= 3 ? 4f : 2f; } }

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
                defaultDesc = "前往遗仙宝卷所在之处静心研读。书中文字将脱离纸墨，沉入识海，显出传承的第一篇章。",
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
            Messages.Message("卷上墨迹尽化流光，归入识海。云间子由此引气入脉，踏出修行第一步。", Pawn,
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
                Messages.Message(CultivationCapMessage(), Pawn, MessageTypeDefOf.PositiveEvent, false);
            }
            return EnergyMax > before + 0.0001f;
        }

        private string CultivationCapMessage()
        {
            switch (Realm)
            {
                case 1:
                    return "云间子周身灵气充盈，百脉已通。练气圆满，可闭关叩问筑基。";
                case 2:
                    return "云间子道基已固，气血、灵脉与神魂渐成一体。筑基圆满，可闭关开辟紫府。";
                case 3:
                    return "云间子紫府澄明，神识内照，万缕灵机正归于一点。紫府圆满，可闭关凝结金丹。";
                case 4:
                    return "云间子金丹无垢，性命相抱，形与神之间的界限已然松动。金丹圆满，可闭关叩问化神。";
                default:
                    return "云间子神与道合，化神境修为已臻圆满。";
            }
        }

        private static string BreakthroughMessage(int newRealm)
        {
            switch (newRealm)
            {
                case 2:
                    return "灵气沉入血肉，百脉如根系扎入生命深处。";
                case 3:
                    return "眉心灵台洞开，神识第一次越过耳目，触及天地间无声流转的灵机。";
                case 4:
                    return "精、气、神汇于一处，在紫府之中凝成一枚性命真种。";
                case 5:
                    return "金丹化开，真灵自形骸深处醒来，神魂从此不再受血肉所限。";
                default:
                    return "云间子破境成功。";
            }
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
            const float naturalRecoveryPerSecond = 0.004f;
            // All cultivation facilities stack additively.  Passive recovery
            // already runs separately, so add only the portion above 1x here.
            float meditationRecovery = naturalRecoveryPerSecond *
                (seat.TotalRecoveryFactor - 1f);
            meditationRecovery *= RobeEnergyRecoveryFactor;

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
                if (Realm >= 3 && seat.def != YunjianziOptionalBuildingDefs.WhiteJadePlatform)
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

        public bool CompleteBreakthrough(int expectedRealm)
        {
            // Revalidate only the identity of the breakthrough being completed.
            // The job already checked the cultivation cap before it began; using
            // CanBreakthrough again at the finish can strand a completed,
            // non-interruptible job if another mod temporarily changes a cached
            // stat while the pawn is on the platform.
            Hediff realmHediff = RealmHediff;
            if (Realm != expectedRealm || expectedRealm < 1 || expectedRealm >= 5 || realmHediff == null)
                return false;

            // Commit the actual realm first. Everything below is presentation or
            // cache maintenance and must not be able to roll back the promotion.
            realmHediff.Severity = expectedRealm + 1f;
            bool advanced = Realm >= expectedRealm + 1;
            if (!advanced) return false;

            // Hediff.Severity already updates its stage. This explicit cache
            // invalidation is retained for modded stats, but isolated so a
            // third-party postfix cannot trap the pawn in the breakthrough job.
            try { Pawn.health.Notify_HediffChanged(realmHediff); }
            catch (Exception exception)
            {
                Log.Error("[云间子] 破境已完成，但刷新境界缓存时发生异常：" + exception);
            }
            // The next realm has a larger cultivation cap. Without a grace
            // period the normal work giver immediately starts another meditate
            // job on the same seat, making the completed breakthrough appear
            // stuck until the pawn is drafted.
            autoCultivationResumeTick = Find.TickManager.TicksGame + 2500;
            try { MaintainFastingNeeds(); }
            catch (Exception exception)
            {
                Log.Error("[云间子] 破境已完成，但刷新辟谷需求时发生异常：" + exception);
            }
            Shield = ShieldMax;
            try
            {
                Messages.Message(BreakthroughMessage(expectedRealm + 1), Pawn,
                    MessageTypeDefOf.PositiveEvent, false);
            }
            catch (Exception exception)
            {
                Log.Error("[云间子] 破境已完成，但显示破境消息时发生异常：" + exception);
            }
            return true;
        }

        private void ExposeBreakthroughData()
        {
            Scribe_Values.Look(ref cultivatedEnergyMax, "yjz_cultivatedEnergyMax", -1f);
            Scribe_Values.Look(ref boundScrollCreated, "yjz_boundScrollCreated", false);
            Scribe_Values.Look(ref boundScrollConsumed, "yjz_boundScrollConsumed", false);
            Scribe_Values.Look(ref autoCultivationResumeTick, "yjz_autoCultivationResumeTick", 0);
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
                float speed = 1f + seatComp.Props.breakthroughPower + seatComp.LinkedBannerCount * 0.25f +
                    seatComp.AdvancedBreakthroughBonus;
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
                    YunjianziPurpleLightning.Glow(pawn.DrawPos, pawn.Map, 0.55f);
                }

                if (workLeft <= 0f)
                {
                    workLeft = 0f;
                    bool completed = false;
                    try
                    {
                        completed = cultivation.CompleteBreakthrough(startingRealm);
                    }
                    catch (Exception exception)
                    {
                        Log.Error("[云间子] 闭关收束时发生异常：" + exception);
                    }
                    finally
                    {
                        // EndJobWith delegates to Pawn_JobTracker.EndCurrentJob.
                        // Always release a full progress, non-interruptible job;
                        // a successfully committed realm counts as success even
                        // if a later third-party notification threw an exception.
                        completed = completed || cultivation.Realm > startingRealm;
                        EndJobWith(completed ? JobCondition.Succeeded : JobCondition.Incompletable);
                    }
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
