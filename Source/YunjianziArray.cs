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
    [StaticConstructorOnStartup]
    public sealed class ThingYunjianziSwordShadow : Thing
    {
        private enum FlightState
        {
            Approach,
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
        private Vector3 approachStart;
        private Vector3 approachControl;
        private Vector3 stagingPoint;
        private Vector3 exitPoint;
        private float approachProgress;
        private int windupUntilTick;
        private int nextChargeTick;
        private bool routePrepared;
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
            exactPosition = pawn.DrawPos;
            exactPosition.y = Altitudes.AltitudeFor(AltitudeLayer.Projectile);
            nextChargeTick = Find.TickManager.TicksGame;
            routePrepared = false;
        }

        protected override void Tick()
        {
            base.Tick();
            CompYunjianziCultivation comp = owner == null ? null : owner.TryGetComp<CompYunjianziCultivation>();
            if (!Spawned || Map == null || owner == null || owner.Dead || !owner.Spawned || owner.Map != Map ||
                comp == null || comp.Realm < 3 || shadowIndex >= comp.ShadowCount)
            {
                if (!Destroyed) Destroy(DestroyMode.Vanish);
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (!routePrepared && !(now >= nextChargeTick ? PrepareChargeRun(true) : PrepareIdleMove()))
            {
                Destroy(DestroyMode.Vanish);
                return;
            }

            if (!ValidTarget(target) && !PrepareIdleMove())
            {
                Destroy(DestroyMode.Vanish);
                return;
            }

            Vector3 before = exactPosition;
            switch (flightState)
            {
                case FlightState.Idle:
                    TickIdle(now);
                    break;
                case FlightState.Approach:
                    TickApproach(now);
                    break;
                case FlightState.Windup:
                    TickWindup(now);
                    break;
                case FlightState.Charge:
                    TickCharge(now, comp);
                    break;
            }

            Vector3 movement = exactPosition - before;
            if (movement.sqrMagnitude > .0001f)
                facingAngle = Mathf.Atan2(movement.x, movement.z) * Mathf.Rad2Deg;
            IntVec3 cell = exactPosition.ToIntVec3();
            if (cell.InBounds(Map)) Position = cell;

            int trailInterval = flightState == FlightState.Charge ? 1 : 3;
            if (!Destroyed && this.IsHashIntervalTick(trailInterval))
            {
                FleckCreationData trail = FleckMaker.GetDataStatic(exactPosition, Map, YJZDefOf.YJZ_Fleck_SwordShadowAfterimage,
                    flightState == FlightState.Charge ? 1.38f : .88f);
                trail.rotation = facingAngle;
                Map.flecks.CreateFleck(trail);
            }
        }

        // Keter-style idle travel: cooldown is spent crossing the hostile
        // formation between 7-11 cell waypoints, never waiting at one point.
        private void TickIdle(int now)
        {
            float length = Mathf.Max(1f, Vector3.Distance(approachStart, stagingPoint));
            approachProgress = Mathf.Min(1f, approachProgress + .8f / length);
            float eased = Mathf.SmoothStep(0f, 1f, approachProgress);
            exactPosition = Vector3.Lerp(approachStart, stagingPoint, eased);
            exactPosition.y = Altitudes.AltitudeFor(AltitudeLayer.Projectile);
            if (approachProgress < .999f) return;

            exactPosition = stagingPoint;
            if (now >= nextChargeTick)
            {
                if (!PrepareChargeRun(false)) Destroy(DestroyMode.Vanish);
            }
            else if (!PrepareIdleMove())
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        private void TickApproach(int now)
        {
            float length = Mathf.Max(1f, Vector3.Distance(approachStart, stagingPoint));
            approachProgress = Mathf.Min(1f, approachProgress + .55f / length);
            float eased = Mathf.SmoothStep(0f, 1f, approachProgress);
            float inverse = 1f - eased;
            exactPosition = inverse * inverse * approachStart + 2f * inverse * eased * approachControl + eased * eased * stagingPoint;
            exactPosition.y = Altitudes.AltitudeFor(AltitudeLayer.Projectile);
            if (approachProgress >= .999f)
            {
                exactPosition = stagingPoint;
                flightState = FlightState.Windup;
                // Match Keter's chargeWaitTicks: only the short aiming beat is
                // stationary. The long attack interval is consumed in Idle.
                windupUntilTick = now + 8;
            }
        }

        private void TickWindup(int now)
        {
            // Old saves may contain the former full-cooldown timestamp.
            if (windupUntilTick - now > 8) windupUntilTick = now + 8;
            if (now < windupUntilTick)
            {
                // A very small lateral drift keeps the sword visibly alive
                // while it decelerates and aligns for the charge.
                Vector3 chargeDirection = exitPoint - stagingPoint;
                chargeDirection.y = 0f;
                if (chargeDirection.sqrMagnitude > .001f)
                {
                    chargeDirection.Normalize();
                    Vector3 tangent = new Vector3(-chargeDirection.z, 0f, chargeDirection.x);
                    exactPosition += tangent * (((shadowIndex & 1) == 0) ? .018f : -.018f);
                }
                return;
            }
            flightState = FlightState.Charge;
            hitThisRun.Clear();
            if (Map != null) FleckMaker.ThrowMicroSparks(exactPosition, Map);
        }

        private void TickCharge(int now, CompYunjianziCultivation comp)
        {
            Vector3 before = exactPosition;
            exactPosition = Vector3.MoveTowards(exactPosition, exitPoint, .9f);
            HitAlongSegment(before, exactPosition, comp);
            if (Vector3.SqrMagnitude(exactPosition - exitPoint) > .01f) return;

            previousTarget = target;
            nextChargeTick = now + Mathf.Max(90, comp.ShadowCount * 30);
            if (!PrepareIdleMove())
                Destroy(DestroyMode.Vanish);
        }

        private bool PrepareIdleMove()
        {
            target = FindBestChargeTarget();
            if (!ValidTarget(target)) return false;

            List<Thing> formation = GenRadial.RadialDistinctThingsAround(owner.Position, owner.Map, 25.9f, true)
                .Where(ValidTarget)
                .ToList();
            Vector3 center = formation.Count == 0
                ? target.DrawPos
                : new Vector3((float)formation.Average(t => t.DrawPos.x), 0f,
                    (float)formation.Average(t => t.DrawPos.z));
            center.y = Altitudes.AltitudeFor(AltitudeLayer.Projectile);

            float angle = (shadowIndex * 137.5f + Find.TickManager.TicksGame * 2.17f) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            float distance = 7f + ((shadowIndex * 17 + Find.TickManager.TicksGame) % 41) / 10f;
            Vector3 destination = ClampToMap(center + direction * distance);
            if (Vector3.Distance(exactPosition, destination) < 5f)
                destination = ClampToMap(center - direction * distance);

            approachStart = exactPosition;
            stagingPoint = destination;
            approachProgress = 0f;
            flightState = FlightState.Idle;
            routePrepared = true;
            return true;
        }

        private void HitAlongSegment(Vector3 from, Vector3 to, CompYunjianziCultivation comp)
        {
            Vector3 midpoint = (from + to) * .5f;
            float radius = Vector3.Distance(from, to) * .5f + 1.35f;
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(midpoint.ToIntVec3(), Map, radius, true))
            {
                if (!ValidTarget(thing) || hitThisRun.Contains(thing)) continue;
                if (DistanceToSegment(thing.DrawPos, from, to) > 1.05f) continue;
                hitThisRun.Add(thing);
                comp.OnShadowProjectileHit(thing, shadowIndex);
                if (Map != null) FleckMaker.ThrowMicroSparks(thing.DrawPos, Map);
            }
        }

        private bool PrepareChargeRun(bool keepInitialTarget)
        {
            if (!keepInitialTarget || !ValidTarget(target)) target = FindBestChargeTarget();
            if (!ValidTarget(target)) return false;

            Vector3 targetCenter = target.DrawPos;
            targetCenter.y = Altitudes.AltitudeFor(AltitudeLayer.Projectile);
            Vector3 direction = targetCenter - exactPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < .04f)
            {
                float angle = (shadowIndex * 137.5f + Find.TickManager.TicksGame * .37f) * Mathf.Deg2Rad;
                direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            }
            direction.Normalize();

            approachStart = exactPosition;
            stagingPoint = ClampToMap(targetCenter - direction * 8f);
            exitPoint = ClampToMap(targetCenter + direction * 8f);
            Vector3 perpendicular = new Vector3(-direction.z, 0f, direction.x);
            float curveSide = ((shadowIndex & 1) == 0 ? 1f : -1f) * (1.5f + .15f * (shadowIndex % 3));
            approachControl = (approachStart + stagingPoint) * .5f + perpendicular * curveSide;
            approachControl.y = Altitudes.AltitudeFor(AltitudeLayer.Projectile);
            approachProgress = 0f;
            hitThisRun.Clear();
            flightState = FlightState.Approach;
            routePrepared = true;
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
                .Where(t => GenSight.LineOfSight(owner.Position, t.Position, owner.Map))
                .ToList();
            if (candidates.Count == 0) return null;
            return candidates
                .OrderByDescending(candidate => ChargeLineScore(candidate, candidates))
                .ThenBy(candidate => candidate == previousTarget ? 1 : 0)
                .ThenBy(candidate => candidate.Position.DistanceToSquared(exactPosition.ToIntVec3()))
                .FirstOrDefault();
        }

        private float ChargeLineScore(Thing candidate, List<Thing> candidates)
        {
            Vector3 center = candidate.DrawPos;
            Vector3 direction = center - exactPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < .04f) return 0f;
            direction.Normalize();
            Vector3 start = center - direction * 8f;
            Vector3 end = center + direction * 8f;
            int intercepts = candidates.Count(other => DistanceToSegment(other.DrawPos, start, end) <= 1.05f);
            return intercepts * 100f - Vector3.Distance(exactPosition, center) - (candidate == previousTarget ? 35f : 0f);
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
            Vector2 size = def.graphicData.drawSize;
            Vector3 loc = exactPosition == Vector3.zero ? drawLoc : exactPosition;
            Quaternion rotation = Quaternion.AngleAxis(facingAngle, Vector3.up);
            Matrix4x4 glow = Matrix4x4.TRS(loc - new Vector3(0f, .002f, 0f), rotation, new Vector3(size.x * 1.22f, 1f, size.y * 1.12f));
            Matrix4x4 body = Matrix4x4.TRS(loc, rotation, new Vector3(size.x * 1.12f, 1f, size.y));
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
            Scribe_Values.Look(ref approachStart, "yjz_shadowApproachStart");
            Scribe_Values.Look(ref approachControl, "yjz_shadowApproachControl");
            Scribe_Values.Look(ref stagingPoint, "yjz_shadowStagingPoint");
            Scribe_Values.Look(ref exitPoint, "yjz_shadowExitPoint");
            Scribe_Values.Look(ref approachProgress, "yjz_shadowApproachProgress", 0f);
            Scribe_Values.Look(ref windupUntilTick, "yjz_shadowWindupUntil", 0);
            Scribe_Values.Look(ref nextChargeTick, "yjz_shadowNextCharge", 0);
            Scribe_Values.Look(ref routePrepared, "yjz_shadowRoutePrepared", false);
            Scribe_Collections.Look(ref hitThisRun, "yjz_shadowHitThisRun", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && hitThisRun == null) hitThisRun = new List<Thing>();
        }
    }

    [StaticConstructorOnStartup]
    public sealed class ThingYunjianziArrayCore : Building
    {
        private static Material arrayFieldMaterial;

        private Pawn owner;
        private bool followsOwner;
        private int realmAtCast;
        private int expireTick;
        private int nextLightningTick;
        private MaterialPropertyBlock fieldPropertyBlock;

        private static Material ArrayFieldMaterial
        {
            get
            {
                if (arrayFieldMaterial == null)
                    arrayFieldMaterial = MaterialPool.MatFrom("Yunjianzi/Array/ArrayField", ShaderDatabase.Transparent);
                return arrayFieldMaterial;
            }
        }

        public Pawn Owner { get { return owner; } }
        public int RealmAtCast { get { return realmAtCast; } }
        public float Radius { get { return 4f + realmAtCast; } }
        public bool Active { get { return Spawned && owner != null && !owner.Dead && (realmAtCast >= 4 || Find.TickManager.TicksGame < expireTick); } }

        public void Initialize(Pawn caster, bool follow, int realm, int durationTicks)
        {
            owner = caster;
            followsOwner = follow;
            realmAtCast = realm;
            expireTick = durationTicks < 0 ? int.MaxValue : Find.TickManager.TicksGame + durationTicks;
            nextLightningTick = Find.TickManager.TicksGame + LightningInterval;
        }

        private int LightningInterval
        {
            get
            {
                if (realmAtCast < 2) return int.MaxValue;
                if (realmAtCast == 2) return 600;
                if (realmAtCast < 5) return 480;
                return 300;
            }
        }

        protected override void Tick()
        {
            base.Tick();
            if (!Active)
            {
                if (!Destroyed) Destroy(DestroyMode.Vanish);
                return;
            }
            if (this.IsHashIntervalTick(30) && TryMigrateRealmDef()) return;
            if (followsOwner && owner.Spawned && owner.Map == Map && Position != owner.Position && this.IsHashIntervalTick(15))
            {
                Map map = Map;
                IntVec3 destination = owner.Position;
                DeSpawn();
                GenSpawn.Spawn(this, destination, map);
                return;
            }
            if (this.IsHashIntervalTick(60)) ApplyFriendlyEffects();
            if (this.IsHashIntervalTick(90)) ApplyTaunt();
            if (realmAtCast >= 2 && Find.TickManager.TicksGame >= nextLightningTick)
            {
                CastLightning();
                nextLightningTick = Find.TickManager.TicksGame + LightningInterval;
            }
        }

        private bool TryMigrateRealmDef()
        {
            ThingDef expectedDef = DefForRealm(realmAtCast);
            if (expectedDef == null || def == expectedDef || Map == null) return false;

            Map map = Map;
            IntVec3 position = Position;
            Faction faction = Faction;
            float hitPointFraction = MaxHitPoints <= 0 ? 1f : HitPoints / (float)MaxHitPoints;
            ThingYunjianziArrayCore replacement = (ThingYunjianziArrayCore)ThingMaker.MakeThing(expectedDef);
            replacement.owner = owner;
            replacement.followsOwner = followsOwner;
            replacement.realmAtCast = realmAtCast;
            replacement.expireTick = expireTick;
            replacement.nextLightningTick = nextLightningTick;
            GenSpawn.Spawn(replacement, position, map);
            if (faction != null) replacement.SetFaction(faction);
            replacement.HitPoints = Mathf.Clamp(Mathf.RoundToInt(replacement.MaxHitPoints * hitPointFraction), 1, replacement.MaxHitPoints);
            Destroy(DestroyMode.Vanish);
            return true;
        }

        private void ApplyTaunt()
        {
            if (Map == null || owner == null || Faction == null) return;
            float tauntRadius = Radius + 6f;
            foreach (Pawn pawn in GenRadial.RadialDistinctThingsAround(Position, Map, tauntRadius, true).OfType<Pawn>())
            {
                if (pawn.Dead || pawn.Downed || !pawn.Spawned || pawn.Faction == null || pawn.Faction == Faction.OfPlayer ||
                    pawn.InMentalState || !pawn.HostileTo(owner) || pawn.WorkTagIsDisabled(WorkTags.Violent) ||
                    !GenSight.LineOfSight(pawn.Position, Position, Map)) continue;

                // Purple Palace and above can have a fixed and a following core.
                // Keep the closest existing core so the two taunts do not make an
                // enemy alternate targets and restart its job every 90 ticks.
                ThingYunjianziArrayCore currentCore = pawn.mindState.enemyTarget as ThingYunjianziArrayCore;
                if (currentCore != null && currentCore != this && currentCore.Active &&
                    pawn.Position.DistanceToSquared(currentCore.Position) <= pawn.Position.DistanceToSquared(Position)) continue;

                pawn.mindState.enemyTarget = this;
                Job current = pawn.CurJob;
                bool alreadyAttackingCore = current != null &&
                    ((current.targetA.IsValid && current.targetA.Thing == this) ||
                     (current.targetB.IsValid && current.targetB.Thing == this));
                if (!alreadyAttackingCore && pawn.jobs != null)
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
            }
        }

        private IEnumerable<Pawn> PawnsInside()
        {
            if (Map == null) yield break;
            foreach (Pawn pawn in Map.mapPawns.AllPawnsSpawned)
                if (!pawn.Dead && pawn.Position.DistanceTo(Position) <= Radius) yield return pawn;
        }

        private bool Friendly(Pawn pawn)
        {
            return pawn != null && owner != null && pawn.Faction == owner.Faction;
        }

        private void ApplyFriendlyEffects()
        {
            foreach (Pawn pawn in PawnsInside().Where(Friendly))
            {
                Hediff_Injury injury = pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>()
                    .Where(h => h.Severity > 0f && !h.IsPermanent())
                    .OrderByDescending(h => h.BleedRate).ThenByDescending(h => h.Severity).FirstOrDefault();
                if (injury != null) injury.Heal(Mathf.Min(.25f, injury.Severity));
                if (realmAtCast >= 4) RefreshArrayShield(pawn);
            }
            if (!followsOwner && realmAtCast >= 2)
            {
                CompYunjianziCultivation comp = owner.TryGetComp<CompYunjianziCultivation>();
                if (comp != null && owner.Position.DistanceTo(Position) <= Radius) comp.Shield += 2f;
            }
        }

        private void RefreshArrayShield(Pawn pawn)
        {
            Hediff shield = pawn.health.hediffSet.GetFirstHediffOfDef(YJZDefOf.YJZ_ArrayShield);
            if (shield == null)
            {
                shield = HediffMaker.MakeHediff(YJZDefOf.YJZ_ArrayShield, pawn);
                shield.Severity = 35f;
                pawn.health.AddHediff(shield);
            }
            else shield.Severity = Mathf.Min(35f, shield.Severity + 1f);
            HediffComp_Disappears disappears = shield.TryGetComp<HediffComp_Disappears>();
            if (disappears != null) disappears.SetDuration(120);
        }

        private void CastLightning()
        {
            List<Thing> enemies = GenRadial.RadialDistinctThingsAround(Position, Map, Radius, true)
                .Where(t => t != null && t.Spawned && t.HostileTo(owner) &&
                    ((t is Pawn && !((Pawn)t).Dead) || t is Building_Turret))
                .OrderBy(t => t.Position.DistanceToSquared(Position)).Take(2).ToList();
            foreach (Thing enemy in enemies)
            {
                DamageInfo info = new DamageInfo(DamageDefOf.Burn, realmAtCast >= 5 ? 22f : 16f, realmAtCast >= 5 ? .9f : .55f, -1f, owner);
                enemy.TakeDamage(info);
                YunjianziCombatEffects.ShowLightning(enemy);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (Active)
            {
                if (fieldPropertyBlock == null) fieldPropertyBlock = new MaterialPropertyBlock();
                Color fieldColor = realmAtCast >= 5
                    ? new Color(.75f, .96f, 1f, .30f)
                    : new Color(.58f, .90f, .95f, .22f);
                fieldPropertyBlock.SetColor(ShaderPropertyIDs.Color, fieldColor);
                Vector3 fieldPosition = Position.ToVector3ShiftedWithAltitude(AltitudeLayer.FloorEmplacement);
                Matrix4x4 matrix = Matrix4x4.TRS(
                    fieldPosition,
                    Quaternion.identity,
                    new Vector3(Radius * 2f, 1f, Radius * 2f));
                Graphics.DrawMesh(MeshPool.plane10, matrix, ArrayFieldMaterial, 0, null, 0, fieldPropertyBlock);
            }
            base.DrawAt(drawLoc, flip);
            if (Active && Find.Selector.IsSelected(this))
            {
                Color color = realmAtCast >= 5 ? new Color(.72f, .95f, 1f) : new Color(.35f, .85f, .92f);
                GenDraw.DrawRadiusRing(Position, Radius, color);
            }
        }

        public override string GetInspectString()
        {
            string duration = realmAtCast >= 4 ? "持续维持：每秒消耗5灵力" : "剩余时间：" + Math.Max(0, expireTick - Find.TickManager.TicksGame).ToStringTicksToPeriod();
            return "境界：" + new[] { "未定", "练气", "筑基", "紫府", "金丹", "化神" }[Mathf.Clamp(realmAtCast, 0, 5)] +
                "\n耐久：" + HitPoints + " / " + MaxHitPoints +
                "\n嘲讽半径：" + (Radius + 6f).ToString("0") + "格" +
                "\n仙阵半径：" + Radius.ToString("0") + "格\n" + duration;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref owner, "yjz_arrayOwner");
            Scribe_Values.Look(ref followsOwner, "yjz_arrayFollowsOwner", false);
            Scribe_Values.Look(ref realmAtCast, "yjz_arrayRealm", 1);
            Scribe_Values.Look(ref expireTick, "yjz_arrayExpireTick", 0);
            Scribe_Values.Look(ref nextLightningTick, "yjz_arrayNextLightning", 0);
        }

        public static IEnumerable<ThingYunjianziArrayCore> ActiveOn(Map map)
        {
            if (map == null || YJZDefOf.YJZ_Thing_ArrayCore == null) yield break;
            foreach (ThingDef coreDef in ArrayCoreDefs())
                if (coreDef != null)
                    foreach (ThingYunjianziArrayCore core in map.listerThings.ThingsOfDef(coreDef).OfType<ThingYunjianziArrayCore>())
                        if (core.Active) yield return core;
        }

        public static ThingDef DefForRealm(int realm)
        {
            if (realm >= 5) return YJZDefOf.YJZ_Thing_ArrayCore_SpiritTransformation;
            if (realm == 4) return YJZDefOf.YJZ_Thing_ArrayCore_GoldenCore;
            if (realm == 3) return YJZDefOf.YJZ_Thing_ArrayCore_PurplePalace;
            if (realm == 2) return YJZDefOf.YJZ_Thing_ArrayCore_Foundation;
            return YJZDefOf.YJZ_Thing_ArrayCore;
        }

        private static IEnumerable<ThingDef> ArrayCoreDefs()
        {
            yield return YJZDefOf.YJZ_Thing_ArrayCore;
            yield return YJZDefOf.YJZ_Thing_ArrayCore_Foundation;
            yield return YJZDefOf.YJZ_Thing_ArrayCore_PurplePalace;
            yield return YJZDefOf.YJZ_Thing_ArrayCore_GoldenCore;
            yield return YJZDefOf.YJZ_Thing_ArrayCore_SpiritTransformation;
        }

        public static bool TryIntercept(Projectile projectile)
        {
            if (projectile == null || !projectile.Spawned || projectile.Map == null || projectile.Destroyed) return false;
            Map map = projectile.Map;
            Vector3 exact = projectile.ExactPosition;
            IntVec3 current = exact.ToIntVec3();
            Thing projectileLauncher = projectile.Launcher;
            IntVec3 origin = projectileLauncher != null && projectileLauncher.Spawned ? projectileLauncher.Position : current + new IntVec3(999, 0, 999);
            foreach (ThingYunjianziArrayCore core in ActiveOn(map).Where(c => c.realmAtCast >= 4))
            {
                if (current.DistanceTo(core.Position) > core.Radius || origin.DistanceTo(core.Position) <= core.Radius) continue;
                Thing launcher = projectileLauncher;
                if (launcher != null && core.owner != null && !launcher.HostileTo(core.owner)) continue;
                ThingDef projectileDef = projectile.def;
                bool reflect = core.realmAtCast >= 5 && launcher != null && launcher.Spawned && launcher.Map == map;
                projectile.Destroy(DestroyMode.Vanish);
                FleckMaker.Static(current, map, FleckDefOf.PsycastAreaEffect, .45f);
                if (reflect)
                {
                    Projectile reflected = (Projectile)GenSpawn.Spawn(projectileDef, current, map);
                    reflected.Launch(core.owner, exact, launcher, launcher, ProjectileHitFlags.IntendedTarget);
                }
                return true;
            }
            return false;
        }
    }

    public sealed partial class CompYunjianziCultivation
    {
        private int arrayBlinkReadyTick;

        private IEnumerable<ThingYunjianziArrayCore> ActiveArrays()
        {
            if (Pawn == null || Pawn.Map == null) return Enumerable.Empty<ThingYunjianziArrayCore>();
            return ThingYunjianziArrayCore.ActiveOn(Pawn.Map).Where(c => c.Owner == Pawn);
        }

        private void TickArraySystem()
        {
            if (Realm >= 4 && Pawn.IsHashIntervalTick(60) && ActiveArrays().Any())
            {
                if (!TrySpend(5f, false))
                {
                    CancelArrays();
                    if (Pawn.Faction == Faction.OfPlayer) Messages.Message("灵力耗尽，仙阵自行消散。", Pawn, MessageTypeDefOf.NegativeEvent);
                }
            }
        }

        private IEnumerable<Gizmo> GetArrayGizmos()
        {
            bool active = ActiveArrays().Any();
            yield return new Command_Action
            {
                defaultLabel = active && Realm >= 4 ? "仙阵诀·收阵" : "仙阵诀·布阵",
                defaultDesc = Realm >= 4 ? "开启或主动取消固定阵与随身阵。维持时整套阵法每秒消耗5点灵力。" : "消耗灵力在脚下布置持续60秒的仙阵。紫府起同时生成随身阵。",
                icon = YunjianziAbilityTextures.ImmortalArray,
                action = active && Realm >= 4 ? (Action)CancelArrays : ActivateArrays
            };
            if (active)
            {
                Command_Action blink = new Command_Action
                {
                    defaultLabel = "仙阵诀·阵遁",
                    defaultDesc = "闪现到当前任一仙阵内部的可站立位置。冷却20秒，不额外消耗灵力。",
                    icon = YunjianziAbilityTextures.ImmortalArray,
                    action = BeginArrayBlink
                };
                int remaining = arrayBlinkReadyTick - Find.TickManager.TicksGame;
                if (!ActiveArrays().Any(c => Pawn.Position.DistanceTo(c.Position) <= c.Radius)) blink.Disable("云间子必须先进入阵内。");
                else if (remaining > 0) blink.Disable("冷却中：" + (remaining / 60f).ToString("0.0") + "秒");
                yield return blink;
            }
        }

        private void ActivateArrays()
        {
            if (Realm <= 0 || !Pawn.Spawned) return;
            int cost = Realm >= 4 ? 0 : Realm >= 3 ? 100 : 50;
            if (cost > 0 && !TrySpend(cost)) return;
            CancelArrays();
            SpawnArray(false, Realm >= 4 ? -1 : 3600);
            if (Realm >= 3) SpawnArray(true, Realm >= 4 ? -1 : 3600);
            Messages.Message(Realm >= 4 ? "仙阵展开。阵法将持续消耗灵力。" : "琉璃阵眼落定，仙阵展开。", Pawn, MessageTypeDefOf.PositiveEvent);
        }

        private void SpawnArray(bool follows, int duration)
        {
            ThingDef coreDef = ThingYunjianziArrayCore.DefForRealm(Realm);
            ThingYunjianziArrayCore core = (ThingYunjianziArrayCore)ThingMaker.MakeThing(coreDef);
            core.Initialize(Pawn, follows, Realm, duration);
            GenSpawn.Spawn(core, Pawn.Position, Pawn.Map);
            if (Pawn.Faction != null) core.SetFaction(Pawn.Faction);
        }

        private void CancelArrays()
        {
            foreach (ThingYunjianziArrayCore core in ActiveArrays().ToList())
                if (!core.Destroyed) core.Destroy(DestroyMode.Vanish);
        }

        private void BeginArrayBlink()
        {
            if (Find.TickManager.TicksGame < arrayBlinkReadyTick) return;
            TargetingParameters parms = new TargetingParameters();
            parms.canTargetLocations = true;
            parms.canTargetPawns = false;
            Find.Targeter.BeginTargeting(parms, delegate(LocalTargetInfo target) { TryArrayBlink(target.Cell); });
        }

        private void TryArrayBlink(IntVec3 cell)
        {
            if (!cell.InBounds(Pawn.Map) || !cell.Standable(Pawn.Map) || !ActiveArrays().Any(c => cell.DistanceTo(c.Position) <= c.Radius))
            {
                Messages.Message("目标必须是仙阵内部的可站立位置。", Pawn, MessageTypeDefOf.RejectInput);
                return;
            }
            Pawn.pather.StopDead();
            Pawn.Position = cell;
            arrayBlinkReadyTick = Find.TickManager.TicksGame + 1200;
            FleckMaker.Static(cell, Pawn.Map, FleckDefOf.PsycastAreaEffect, 1f);
        }

        private void ExposeArrayData()
        {
            Scribe_Values.Look(ref arrayBlinkReadyTick, "yjz_arrayBlinkReadyTick", 0);
        }
    }

    [HarmonyPatch(typeof(Projectile), "Tick")]
    internal static class Patch_Projectile_Tick_YunjianziArray
    {
        private static bool Prefix(Projectile __instance)
        {
            return !ThingYunjianziArrayCore.TryIntercept(__instance);
        }
    }
}
