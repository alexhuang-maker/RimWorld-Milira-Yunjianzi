using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Yunjianzi
{
    public sealed partial class CompYunjianziCultivation
    {
        private bool CanBeginSwordRainJump => Pawn != null && Pawn.Spawned && !Pawn.Dead &&
            !Pawn.Downed && !Pawn.InMentalState && !Pawn.stances.stunner.Stunned &&
            !Pawn.Position.Roofed(Pawn.Map);

        private bool IsValidSwordRainLanding(IntVec3 cell)
        {
            return Pawn != null && Pawn.Spawned && cell.InBounds(Pawn.Map) &&
                !cell.Roofed(Pawn.Map) && JumpUtility.ValidJumpTarget(Pawn, Pawn.Map, cell) &&
                JumpUtility.CanHitTargetFrom(Pawn, Pawn.Position, cell, 35.9f);
        }

        private void LaunchSwordRainJump(IntVec3 destination, int damage, float radius)
        {
            Pawn caster = Pawn;
            Map map = caster.Map;
            IntVec3 origin = caster.Position;
            caster.rotationTracker.FaceCell(destination);
            SoundDef.Named("Longjump_Jump").PlayOneShot(new TargetInfo(origin, map));
            FleckMaker.ThrowDustPuff(caster.DrawPos, map, 1.2f);
            var flyer = (PawnFlyerYunjianziSwordRain)PawnFlyer.MakeFlyer(
                YJZDefOf.YJZ_PawnFlyer_SwordRain, caster, destination, null,
                SoundDef.Named("Longjump_Land"), flyWithCarriedThing: true);
            flyer.Initialize(damage, radius);
            GenSpawn.Spawn(flyer, origin, map);
        }
    }

    public sealed class PawnFlyerWorkerYunjianziSwordRain : PawnFlyerWorker
    {
        public PawnFlyerWorkerYunjianziSwordRain(PawnFlyerProperties properties) : base(properties) { }
        public override float AdjustedProgress(float t) => SwordRainMotion.Progress(t);
        public override float GetHeight(float t) => SwordRainMotion.Height(t);
    }

    // Keep vanilla's holder, render phases, captured jobs and save/load lifecycle.
    public sealed class PawnFlyerYunjianziSwordRain : PawnFlyer
    {
        private int impactDamage = 84;
        private float impactRadius = 3.9f;
        private bool impactReleased;

        public void Initialize(int damage, float radius)
        {
            impactDamage = Mathf.Max(1, damage);
            impactRadius = Mathf.Max(1f, radius);
            FaceLandingPoint();
        }

        private void FaceLandingPoint()
        {
            Pawn caster = FlyingPawn;
            if (caster == null) return;
            // Use the ground trajectory, not DrawPos: its artificial height offset
            // would make the pawn turn away again during the vertical descent.
            Vector3 direction = DestinationPos - startVec;
            direction.y = 0f;
            if (direction.sqrMagnitude > .0001f)
                Rotation = Pawn_RotationTracker.RotFromAngleBiased(direction.AngleFlat());
            caster.Rotation = Rotation;
        }

        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            // Both phases run on the main thread. Reassert the facing after held
            // pawn ticks/rotation updates, before the vanilla renderer caches it.
            if (phase != DrawPhase.ParallelPreDraw) FaceLandingPoint();
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
        }

        protected override void TickInterval(int delta)
        {
            FaceLandingPoint();
            if (Spawned && FlyingPawn != null && ticksFlying < ticksFlightTime && ticksFlying % 6 == 0)
            {
                Vector3 point = DrawPos;
                if (point.ToIntVec3().InBounds(Map))
                {
                    FleckCreationData glow = FleckMaker.GetDataStatic(point, Map,
                        YJZDefOf.YJZ_Fleck_SwordRainAura, ticksFlying >= ticksFlightTime * .8f ? 1.6f : 1.1f);
                    Map.flecks.CreateFleck(glow);
                }
            }
            base.TickInterval(delta);
        }

        protected override void RespawnPawn()
        {
            FaceLandingPoint();
            Pawn caster = FlyingPawn;
            base.RespawnPawn();
            if (impactReleased || caster == null || !caster.Spawned || caster.Dead) return;
            impactReleased = true;
            Map map = caster.Map;
            IntVec3 center = caster.Position;
            PlayImpact(map, center.ToVector3Shifted());
            var echo = (ThingYunjianziSwordRainEcho)ThingMaker.MakeThing(YJZDefOf.YJZ_Thing_SwordRainEcho);
            echo.Initialize(caster, impactDamage, impactRadius, center.ToVector3Shifted());
            GenSpawn.Spawn(echo, center, map);
        }

        private void PlayImpact(Map map, Vector3 center)
        {
            // The vanilla explosion's shake is gated by doVisualEffects. Use its
            // distance scaling here while replacing the fireball with sword qi.
            if (map == Find.CurrentMap && Find.Camera != null)
            {
                float distance = Mathf.Max(1f, (center - Find.Camera.transform.position).magnitude);
                Find.CameraDriver.shaker.DoShake(Mathf.Min(.65f, 4f * impactRadius * .65f / distance));
            }
            Rand.PushState(thingIDNumber ^ Find.TickManager.TicksGame);
            try
            {
                FleckMaker.Static(center, map, YJZDefOf.YJZ_Fleck_SwordRainAura, 3f);
                FleckMaker.ThrowDustPuff(center, map, 2.2f);
            }
            finally { Rand.PopState(); }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref impactDamage, "swordRainImpactDamage", 84);
            Scribe_Values.Look(ref impactRadius, "swordRainImpactRadius", 3.9f);
            Scribe_Values.Look(ref impactReleased, "swordRainImpactReleased", false);
        }
    }

    [HarmonyPatch(typeof(StunHandler), nameof(StunHandler.StunFor))]
    public static class Patch_StunHandler_SwordRainSuperArmor
    {
        public static bool Prefix(StunHandler __instance)
        {
            return !(__instance.parent?.ParentHolder is PawnFlyerYunjianziSwordRain);
        }
    }
}
