using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    [StaticConstructorOnStartup]
    public sealed class ThingYunjianziSwordShadow : Thing
    {
        private enum FlightState
        {
            Approach, // Legacy save value; now returns to the owner like Idle.
            Windup,
            Charge,
            Idle
        }

        private Pawn owner;
        private Thing target;
        private Thing previousTarget;
        private int shadowIndex;
        private Vector3 exactPosition;
        private float facingAngle;
        private FlightState flightState;
        private Vector3 exitPoint;
        private int windupUntilTick;
        private int nextChargeTick;
        private int launchTick;
        private int flightMechanismVersion;
        private List<Thing> hitThisRun = new List<Thing>();
        private Material cachedMaterial;
        private Material cachedGlowMaterial;

        public Pawn Owner { get { return owner; } }
        public int ShadowIndex { get { return shadowIndex; } }

        private Material SwordMaterial
        {
            get
            {
                if (cachedMaterial == null)
                    cachedMaterial = MaterialPool.MatFrom(def.graphicData.texPath, ShaderDatabase.TransparentPostLight);
                return cachedMaterial;
            }
        }

        private Material GlowMaterial
        {
            get
            {
                if (cachedGlowMaterial == null)
                    cachedGlowMaterial = MaterialPool.MatFrom(def.graphicData.texPath, ShaderDatabase.MoteGlow);
                return cachedGlowMaterial;
            }
        }

        public void Initialize(Pawn pawn, int index, Thing initialTarget)
        {
            owner = pawn;
            shadowIndex = index;
            target = initialTarget;
            exactPosition = LaunchPosition();
            nextChargeTick = Find.TickManager.TicksGame + index * 3;
            flightState = FlightState.Idle;
            flightMechanismVersion = 1;
        }

        protected override void Tick()
        {
            base.Tick();
            CompYunjianziCultivation comp = owner == null ? null : owner.TryGetComp<CompYunjianziCultivation>();
            CompYangfuBeanSoldier beanComp = owner == null ? null : owner.TryGetComp<CompYangfuBeanSoldier>();
            int activeShadowCount = comp != null ? comp.ShadowCount : beanComp != null ? beanComp.SwordShadowCount : 0;
            if (!Spawned || Map == null || owner == null || owner.Dead || !owner.Spawned || owner.Map != Map ||
                activeShadowCount <= 0 || shadowIndex >= activeShadowCount)
            {
                if (!Destroyed) Destroy(DestroyMode.Vanish);
                return;
            }

            int now = Find.TickManager.TicksGame;
            // A launched sword completes its straight pass even if its target dies.
            // Cancelling aim returns it home; it must never launch from the enemy line.
            if (flightState == FlightState.Windup && (owner.Downed || !ValidTarget(target)))
                flightState = FlightState.Idle;

            Vector3 before = exactPosition;
            bool wasAiming = flightState == FlightState.Windup;
            switch (flightState)
            {
                case FlightState.Idle:
                case FlightState.Approach:
                    TickIdle(now);
                    break;
                case FlightState.Windup:
                    TickWindup(now);
                    break;
                case FlightState.Charge:
                    TickCharge(now, comp, beanComp, activeShadowCount);
                    break;
            }
            if (Destroyed || !Spawned || Map == null) return;

            Vector3 movement = exactPosition - before;
            if (!wasAiming && flightState != FlightState.Windup && movement.sqrMagnitude > .0001f)
                facingAngle = Mathf.Atan2(movement.x, movement.z) * Mathf.Rad2Deg;
            IntVec3 cell = exactPosition.ToIntVec3();
            if (cell.InBounds(Map)) Position = cell;

            int trailInterval = flightState == FlightState.Charge ? 1 : 3;
            bool atRest = flightState == FlightState.Idle &&
                Vector3.SqrMagnitude(exactPosition - LaunchPosition()) <= .0001f;
            if (!atRest && !wasAiming && flightState != FlightState.Windup &&
                movement.sqrMagnitude > .0001f && this.IsHashIntervalTick(trailInterval))
            {
                FleckCreationData trail = FleckMaker.GetDataStatic(exactPosition, Map, YJZDefOf.YJZ_Fleck_SwordShadowAfterimage,
                    flightState == FlightState.Charge ? 1.38f : .88f);
                trail.rotation = facingAngle;
                Map.flecks.CreateFleck(trail);

                // Wings of Democracy's moving weapons build their tail by
                // emitting a Fleck every tick (doTrail/trailFreauency=1), not
                // by attaching one long texture to the weapon.  Do the same
                // here: every wisp is born behind the hilt, then remains in
                // world space while drifting, expanding and fading away.
                if (flightState == FlightState.Charge || this.IsHashIntervalTick(6))
                {
                    Quaternion heading = Quaternion.AngleAxis(facingAngle, Vector3.up);
                    Vector3 forward = heading * Vector3.forward;
                    Vector3 tangent = new Vector3(-forward.z, 0f, forward.x);
                    float wave = Mathf.Sin((now + shadowIndex * 19) * .71f);
                    Vector3 emitter = exactPosition - forward * .62f + tangent * wave * .12f;
                    FleckCreationData cloud = FleckMaker.GetDataStatic(emitter, Map,
                        YJZDefOf.YJZ_Fleck_SwordShadowCloudWake,
                        flightState == FlightState.Charge ? .38f : .27f);
                    cloud.rotation = facingAngle + wave * 13f;
                    cloud.rotationRate = wave * 32f;
                    cloud.velocityAngle = facingAngle + 180f + wave * 18f;
                    cloud.velocitySpeed = flightState == FlightState.Charge ? .075f : .035f;
                    Map.flecks.CreateFleck(cloud);
                }
            }
        }

        // Cooldown is spent returning to the moving caster, then waiting on the body.
        // Only the outbound Charge state can hit anything.
        private void TickIdle(int now)
        {
            Vector3 origin = LaunchPosition();
            exactPosition = Vector3.MoveTowards(exactPosition, origin, .8f);
            if (Vector3.SqrMagnitude(exactPosition - origin) > .0001f) return;

            exactPosition = origin;
            if (owner.Downed || now < nextChargeTick) return;
            if (!PrepareChargeRun()) Destroy(DestroyMode.Vanish);
        }

        private Vector3 LaunchPosition()
        {
            Vector3 point = owner.DrawPos;
            point.z += .15f;
            point.y = Altitudes.AltitudeFor(AltitudeLayer.Projectile);
            return point;
        }

        private void AimFromOwner()
        {
            exactPosition = LaunchPosition();
            Vector3 direction = target.DrawPos - exactPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < .04f)
            {
                float angle = shadowIndex * 137.5f * Mathf.Deg2Rad;
                direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            }
            direction.Normalize();
            exitPoint = ClampToMap(target.DrawPos + direction * 8f);
            facingAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        private void TickWindup(int now)
        {
            // Re-anchor every tick: movement during aiming cannot leave a remote muzzle.
            AimFromOwner();
            if (now < windupUntilTick) return;
            flightState = FlightState.Charge;
            launchTick = now;
            hitThisRun.Clear();
            FleckMaker.ThrowMicroSparks(exactPosition, Map);
        }

        private void TickCharge(int now, CompYunjianziCultivation comp, CompYangfuBeanSoldier beanComp, int activeShadowCount)
        {
            Vector3 before = exactPosition;
            // A short acceleration makes the point of departure readable at normal speed.
            float speed = Mathf.Lerp(.25f, .9f, (now - launchTick - 1) / 8f);
            exactPosition = Vector3.MoveTowards(exactPosition, exitPoint, speed);
            HitAlongSegment(before, exactPosition, comp, beanComp);
            if (Vector3.SqrMagnitude(exactPosition - exitPoint) > .01f) return;

            previousTarget = target;
            nextChargeTick = now + Mathf.Max(90, activeShadowCount * 30);
            flightState = FlightState.Idle;
        }

        private void HitAlongSegment(Vector3 from, Vector3 to, CompYunjianziCultivation comp, CompYangfuBeanSoldier beanComp)
        {
            Vector3 midpoint = (from + to) * .5f;
            float radius = Vector3.Distance(from, to) * .5f + 1.35f;
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(midpoint.ToIntVec3(), Map, radius, true))
            {
                if (!ValidTarget(thing) || hitThisRun.Contains(thing)) continue;
                if (DistanceToSegment(thing.DrawPos, from, to) > 1.05f) continue;
                hitThisRun.Add(thing);
                if (comp != null) comp.OnShadowProjectileHit(thing, shadowIndex);
                else beanComp?.OnSwordShadowHit(thing);
                if (Map != null) FleckMaker.ThrowMicroSparks(thing.DrawPos, Map);
            }
        }

        private bool PrepareChargeRun()
        {
            target = FindBestChargeTarget();
            if (!ValidTarget(target)) return false;

            AimFromOwner();
            windupUntilTick = Find.TickManager.TicksGame + 8;
            flightState = FlightState.Windup;
            return true;
        }

        private bool ValidTarget(Thing thing)
        {
            return thing != null && !thing.Destroyed && thing.Spawned && thing.Map == Map && thing.HostileTo(owner) &&
                ((thing is Pawn && !((Pawn)thing).Dead) || thing is Building_Turret) &&
                thing.Position.DistanceTo(owner.Position) <= 25.9f;
        }

        private Thing FindBestChargeTarget()
        {
            if (owner == null || owner.Map == null) return null;
            List<Thing> candidates = GenRadial.RadialDistinctThingsAround(owner.Position, owner.Map, 25.9f, true)
                .Where(ValidTarget)
                .ToList();
            if (candidates.Count == 0) return null;
            return candidates
                .OrderByDescending(candidate => ChargeLineScore(candidate, candidates))
                .ThenBy(candidate => candidate == previousTarget ? 1 : 0)
                .ThenBy(candidate => candidate.Position.DistanceToSquared(owner.Position))
                .FirstOrDefault();
        }

        private float ChargeLineScore(Thing candidate, List<Thing> candidates)
        {
            Vector3 center = candidate.DrawPos;
            Vector3 start = LaunchPosition();
            Vector3 direction = center - start;
            direction.y = 0f;
            if (direction.sqrMagnitude < .04f) return 0f;
            direction.Normalize();
            Vector3 end = ClampToMap(center + direction * 8f);
            int intercepts = candidates.Count(other => DistanceToSegment(other.DrawPos, start, end) <= 1.05f);
            return intercepts * 100f - Vector3.Distance(start, center) - (candidate == previousTarget ? 35f : 0f);
        }

        private Vector3 ClampToMap(Vector3 point)
        {
            if (Map == null) return point;
            point.x = Mathf.Clamp(point.x, .5f, Map.Size.x - 1.5f);
            point.z = Mathf.Clamp(point.z, .5f, Map.Size.z - 1.5f);
            point.y = Altitudes.AltitudeFor(AltitudeLayer.Projectile);
            return point;
        }

        private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            point.y = start.y = end.y = 0f;
            Vector3 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared < .0001f) return Vector3.Distance(point, start);
            float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
            return Vector3.Distance(point, start + segment * t);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            // Reabsorbed swords stay hidden until the next short windup, avoiding
            // a stack of glowing blades obscuring the caster throughout cooldown.
            if (flightState == FlightState.Idle && owner != null &&
                Vector3.SqrMagnitude(exactPosition - LaunchPosition()) <= .0001f) return;
            Vector2 size = def.graphicData.drawSize;
            Vector3 loc = exactPosition == Vector3.zero ? drawLoc : exactPosition;
            Quaternion rotation = Quaternion.AngleAxis(facingAngle, Vector3.up);
            float pulse = 1f + Mathf.Sin((Find.TickManager.TicksGame + shadowIndex * 11) * .24f) * .055f;
            Matrix4x4 glow = Matrix4x4.TRS(loc - new Vector3(0f, .002f, 0f), rotation,
                new Vector3(size.x * 1.52f * pulse, 1f, size.y * 1.28f * pulse));
            Matrix4x4 body = Matrix4x4.TRS(loc + new Vector3(0f, .004f, 0f), rotation,
                new Vector3(size.x * 1.32f, 1f, size.y * 1.16f));
            Graphics.DrawMesh(MeshPool.plane10, glow, GlowMaterial, 0);
            Graphics.DrawMesh(MeshPool.plane10, body, SwordMaterial, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref owner, "yjz_shadowOwner");
            Scribe_References.Look(ref target, "yjz_shadowTarget");
            Scribe_References.Look(ref previousTarget, "yjz_shadowPreviousTarget");
            Scribe_Values.Look(ref shadowIndex, "yjz_shadowIndex", 0);
            Scribe_Values.Look(ref exactPosition, "yjz_shadowPosition");
            Scribe_Values.Look(ref facingAngle, "yjz_shadowAngle", 0f);
            Scribe_Values.Look(ref flightState, "yjz_shadowFlightState", FlightState.Approach);
            Scribe_Values.Look(ref exitPoint, "yjz_shadowExitPoint");
            Scribe_Values.Look(ref windupUntilTick, "yjz_shadowWindupUntil", 0);
            Scribe_Values.Look(ref nextChargeTick, "yjz_shadowNextCharge", 0);
            Scribe_Values.Look(ref launchTick, "yjz_shadowLaunchTick", 0);
            Scribe_Values.Look(ref flightMechanismVersion, "yjz_shadowFlightMechanismVersion", 0);
            Scribe_Collections.Look(ref hitThisRun, "yjz_shadowHitThisRun", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (hitThisRun == null) hitThisRun = new List<Thing>();
                if (flightMechanismVersion < 1)
                {
                    // Keep the saved location and fly home before using the new launch cycle.
                    flightState = FlightState.Idle;
                    hitThisRun.Clear();
                    flightMechanismVersion = 1;
                }
            }
        }
    }

}
