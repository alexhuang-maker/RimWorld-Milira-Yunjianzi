using System.Collections.Generic;
using System.Linq;
using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Yunjianzi
{
    public sealed class Building_YunjianziSwordRainTurret : Building_SpinTurretGun
    {
        private static readonly CachedMaterial HoveringSwordMat = new CachedMaterial(
            "Yunjianzi/Buildings/Security/SwordNurturingPool_Sword",
            ShaderDatabase.Cutout);
        private static readonly CachedMaterial HoveringSwordGlowMat = new CachedMaterial(
            "Yunjianzi/Buildings/Security/SwordNurturingPool_Sword",
            ShaderDatabase.MoteGlow);
        private static readonly CachedMaterial LockOverlayMat = new CachedMaterial(
            "Yunjianzi/Buildings/Security/SwordNurturingPool_LockOverlay",
            ShaderDatabase.Cutout);

        private bool wasBursting;
        private int swordFlashUntilTick;
        private IntVec3 pendingManualTarget = IntVec3.Invalid;
        private int pendingManualStrikeTick;
        private int manualCooldownUntilTick;

        public override LocalTargetInfo TryFindNewTarget()
        {
            if (!Spawned || Map == null || AttackVerb == null)
            {
                return LocalTargetInfo.Invalid;
            }

            IAttackTargetSearcher searcher = TargSearcher();
            List<Thing> candidates = Map.attackTargetsCache
                .GetPotentialTargetsFor(searcher)
                .Select(target => target.Thing)
                .Where(IsEligibleAutoTarget)
                .ToList();
            if (candidates.Count == 0)
            {
                return LocalTargetInfo.Invalid;
            }

            const float strikeRadius = 2.5f;
            float radiusSquared = strikeRadius * strikeRadius;
            Thing best = null;
            int bestCount = -1;
            float bestThreat = -1f;
            float bestDistance = float.MaxValue;

            foreach (Thing candidate in candidates)
            {
                int count = 0;
                float threat = 0f;
                foreach (Thing nearby in candidates)
                {
                    if (nearby.Position.DistanceToSquared(candidate.Position) > radiusSquared) continue;
                    count++;
                    Pawn pawn = nearby as Pawn;
                    threat += pawn != null ? Mathf.Max(1f, pawn.MarketValue) : 1f;
                }

                float distance = candidate.Position.DistanceToSquared(Position);
                if (count > bestCount
                    || count == bestCount && threat > bestThreat
                    || count == bestCount && Mathf.Approximately(threat, bestThreat) && distance < bestDistance)
                {
                    best = candidate;
                    bestCount = count;
                    bestThreat = threat;
                    bestDistance = distance;
                }
            }

            return best ?? LocalTargetInfo.Invalid;
        }

        private bool IsEligibleAutoTarget(Thing thing)
        {
            if (thing == null || thing.Destroyed || !thing.Spawned || !IsValidTarget(thing)) return false;
            float distance = thing.Position.DistanceTo(Position);
            return distance >= AttackVerb.verbProps.EffectiveMinRange(thing, this)
                && distance <= AttackVerb.EffectiveRange;
        }

        protected override void Tick()
        {
            base.Tick();
            bool bursting = AttackVerb?.Bursting == true;
            if (bursting && !wasBursting)
            {
                swordFlashUntilTick = Find.TickManager.TicksGame + 24;
            }
            wasBursting = bursting;

            if (pendingManualTarget.IsValid
                && Find.TickManager.TicksGame >= pendingManualStrikeTick)
            {
                ReleaseManualStrike();
            }

            bool manualWarmup = pendingManualTarget.IsValid;
            if (Spawned && (bursting || manualWarmup || Find.TickManager.TicksGame < swordFlashUntilTick)
                && this.IsHashIntervalTick(2))
            {
                Vector3 glowPos = HoveringSwordDrawPos(DrawPos);
                FleckMaker.ThrowLightningGlow(glowPos, Map, bursting || manualWarmup ? 2.25f : 1.3f);
                if (Rand.Chance(.65f)) FleckMaker.ThrowMicroSparks(glowPos, Map);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            // This is a fixed cultivation pool, not a rotating gun top.  The
            // restrained sword is a separate realtime layer so it can breathe.
            base.DrawAt(drawLoc, flip);
            Vector3 swordPos = HoveringSwordDrawPos(drawLoc);
            Vector3 scale = new Vector3(.72f, 1f, 1.42f);
            Graphics.DrawMesh(
                MeshPool.plane10,
                Matrix4x4.TRS(swordPos, Quaternion.identity, scale),
                HoveringSwordMat.Material,
                0);

            // During warmup and firing, add a pulsing additive copy of the
            // blade itself.  This makes the sword—not only the surrounding
            // flecks—flare visibly before releasing its sword intent.
            bool swordLit = AttackVerb?.Bursting == true
                || pendingManualTarget.IsValid
                || Find.TickManager.TicksGame < swordFlashUntilTick;
            if (swordLit)
            {
                float pulse = 1.08f + Mathf.Sin(GenTicks.TicksGame * Mathf.PI * 2f / 18f) * .06f;
                Vector3 glowScale = new Vector3(scale.x * pulse, 1f, scale.z * pulse);
                Vector3 swordGlowPos = swordPos;
                swordGlowPos.y += .002f;
                Graphics.DrawMesh(
                    MeshPool.plane10,
                    Matrix4x4.TRS(swordGlowPos, Quaternion.identity, glowScale),
                    HoveringSwordGlowMat.Material,
                    0);
            }

            // Redraw only the near half of the central binding ring above the
            // sword.  The far half remains in the base layer, so the blade
            // visibly threads through the restraint instead of sitting on it.
            Vector3 lockPos = drawLoc;
            lockPos.y = AltitudeLayer.MoteOverhead.AltitudeFor() + .004f;
            Vector2 drawSize = def.graphicData.drawSize;
            Graphics.DrawMesh(
                MeshPool.plane10,
                Matrix4x4.TRS(lockPos, Quaternion.identity,
                    new Vector3(drawSize.x, 1f, drawSize.y)),
                LockOverlayMat.Material,
                0);
        }

        private static Vector3 HoveringSwordDrawPos(Vector3 basePos)
        {
            float bob = Mathf.Sin(GenTicks.TicksGame * Mathf.PI * 2f / 150f) * .12f;
            basePos.z += .58f + bob;
            basePos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            return basePos;
        }

        public override void DrawExtraSelectionOverlays()
        {
            Verb verb = AttackVerb;
            if (verb == null)
            {
                return;
            }

            Vector3 center = GenThing.TrueCenter(
                Position,
                Rotation,
                def.Size,
                AltitudeLayer.MetaOverlays.AltitudeFor());
            GenDraw.DrawCircleOutline(center, verb.EffectiveRange);
            if (verb.verbProps.minRange > 0f)
            {
                GenDraw.DrawCircleOutline(center, verb.verbProps.minRange);
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                if (gizmo is Command_VerbTarget)
                {
                    // Building_SpinTurretGun exposes its gun verb directly.
                    // Replace that route with OrderAttack below so the chosen
                    // cell becomes this building's forced target reliably.
                    continue;
                }
                yield return gizmo;
            }

            if (Faction == Faction.OfPlayer && AttackVerb != null)
            {
                Command_Action manualStrike = new Command_Action
                {
                    defaultLabel = "手动指定剑气落点",
                    defaultDesc = "命令养剑台将下一轮交错剑意投向指定目标。",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack"),
                    hotKey = KeyBindingDefOf.Misc4,
                    action = BeginManualTargeting
                };
                if (Find.TickManager.TicksGame < manualCooldownUntilTick || pendingManualTarget.IsValid)
                {
                    manualStrike.Disable(pendingManualTarget.IsValid
                        ? "剑意正在凝聚"
                        : "养剑台尚在温养剑意");
                }
                yield return manualStrike;
            }
        }

        private void BeginManualTargeting()
        {
            TargetingParameters parameters = AttackVerb.verbProps.targetParams;
            Find.Targeter.BeginTargeting(
                parameters,
                QueueManualStrike,
                null,
                null,
                ContentFinder<Texture2D>.Get("UI/Commands/Attack"));
        }

        private void QueueManualStrike(LocalTargetInfo target)
        {
            if (!Spawned || Map == null || AttackVerb == null || !target.IsValid) return;
            float distance = (target.Cell - Position).LengthHorizontal;
            if (distance < AttackVerb.verbProps.minRange)
            {
                Messages.Message("MessageTargetBelowMinimumRange".Translate(), this,
                    MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (distance > AttackVerb.EffectiveRange)
            {
                Messages.Message("MessageTargetBeyondMaximumRange".Translate(), this,
                    MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (!AttackVerb.Available())
            {
                Messages.Message("养剑台当前无法出剑。", this,
                    MessageTypeDefOf.RejectInput, false);
                return;
            }

            pendingManualTarget = target.Cell;
            pendingManualStrikeTick = Find.TickManager.TicksGame + 144;
            swordFlashUntilTick = pendingManualStrikeTick + 24;
        }

        private void ReleaseManualStrike()
        {
            IntVec3 target = pendingManualTarget;
            pendingManualTarget = IntVec3.Invalid;
            if (!Spawned || Map == null || !target.InBounds(Map)) return;

            ThingYunjianziCrossSwordStrike strike =
                (ThingYunjianziCrossSwordStrike)ThingMaker.MakeThing(
                    DefDatabase<ThingDef>.GetNamed("YJZ_Thing_CrossSwordStrike"));
            strike.Initialize(this, 2.5f, 224, .45f);
            GenSpawn.Spawn(strike, target, Map);
            FleckMaker.ThrowLightningGlow(HoveringSwordDrawPos(DrawPos), Map, 2.2f);
            manualCooldownUntilTick = Find.TickManager.TicksGame + 1080;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref pendingManualTarget, "yjz_manualSwordTarget", IntVec3.Invalid);
            Scribe_Values.Look(ref pendingManualStrikeTick, "yjz_manualSwordStrikeTick", 0);
            Scribe_Values.Look(ref manualCooldownUntilTick, "yjz_manualSwordCooldown", 0);
        }
    }

    public sealed class PlaceWorker_YunjianziTurretRadius : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(
            BuildableDef checkingDef,
            IntVec3 loc,
            Rot4 rot,
            Map map,
            Thing thingToIgnore = null,
            Thing thing = null)
        {
            ThingDef turretDef = (ThingDef)checkingDef;
            VerbProperties verb = turretDef.building.turretGunDef.Verbs.Find(
                candidate => candidate.verbClass == typeof(Verb_Shoot)
                    || typeof(Verb_Spray).IsAssignableFrom(candidate.verbClass));
            if (verb != null)
            {
                Vector3 center = GenThing.TrueCenter(
                    loc,
                    rot,
                    turretDef.Size,
                    AltitudeLayer.MetaOverlays.AltitudeFor());
                if (verb.range > 0f)
                {
                    GenDraw.DrawCircleOutline(center, verb.range);
                }
                if (verb.minRange > 0f)
                {
                    GenDraw.DrawCircleOutline(center, verb.minRange);
                }
            }
            return true;
        }
    }

    /// <summary>
    /// A high-arc sword casket. The carrier itself does not explode: on impact
    /// it unfolds into two short sword-rain waves whose combined nominal damage
    /// matches one complete Milira heavy-rocket salvo (8 x 28 = 224).
    /// </summary>
    public sealed class ProjectileYunjianziSwordRainShell : Projectile
    {
        // The attack is projected sword intent; there is no physical shell to
        // render between the pool and the target.
        protected override void DrawAt(Vector3 drawLoc, bool flip = false) { }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            IntVec3 impactCell = Position;
            Thing shotInstigator = launcher;

            if (!blockedByShield && impactMap != null && impactCell.InBounds(impactMap))
            {
                ThingYunjianziCrossSwordStrike strike =
                    (ThingYunjianziCrossSwordStrike)ThingMaker.MakeThing(
                        DefDatabase<ThingDef>.GetNamed("YJZ_Thing_CrossSwordStrike"));
                strike.Initialize(shotInstigator, 2.5f, 224, .45f);
                GenSpawn.Spawn(strike, impactCell, impactMap);
            }

            base.Impact(hitThing, blockedByShield);
        }
    }

    /// <summary>
    /// Two deliberate spirit-sword cuts: northwest to southeast, followed by
    /// northeast to southwest.  Each impact applies one half of the shell's
    /// nominal damage and together they leave an X-shaped sword-qi detonation.
    /// </summary>
    public sealed class ThingYunjianziCrossSwordStrike : Thing
    {
        private static readonly CachedMaterial SpiritSwordMat = new CachedMaterial(
            "Yunjianzi/Buildings/Security/CrossSwordStrike",
            ShaderDatabase.MoteGlow);
        private static readonly CachedMaterial CrossBurstMat = new CachedMaterial(
            "Yunjianzi/Buildings/Security/CrossSwordBurst",
            ShaderDatabase.MoteGlow);

        private Thing instigator;
        private int startTick;
        private int stagesReleased;
        private float blastRadius = 2.5f;
        private int totalDamage = 224;
        private float armorPenetration = .45f;

        public void Initialize(Thing source, float radius, int damage, float armorPenetration)
        {
            instigator = source;
            blastRadius = Mathf.Max(1f, radius);
            // Resolve one complete Milira rocket salvo as two quarter-strength
            // cuts followed by a half-strength X-shaped detonation.
            totalDamage = Mathf.Max(3, damage);
            this.armorPenetration = Mathf.Max(0f, armorPenetration);
            startTick = Find.TickManager.TicksGame;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            int elapsed = Find.TickManager.TicksGame - startTick;
            if (elapsed >= 66 && elapsed < 84)
            {
                float burstProgress = (elapsed - 66f) / 18f;
                float burstScale = Mathf.Lerp(1.35f, 2.35f, Mathf.Sin(burstProgress * Mathf.PI * .5f));
                Vector3 burstPos = drawLoc;
                burstPos.y = AltitudeLayer.MoteOverhead.AltitudeFor() + .002f;
                Graphics.DrawMesh(
                    MeshPool.plane10,
                    Matrix4x4.TRS(burstPos, Quaternion.identity,
                        new Vector3(burstScale, 1f, burstScale)),
                    CrossBurstMat.Material,
                    0);
            }

            int wave = elapsed < 40 ? 0 : 1;
            int localTick = wave == 0 ? elapsed : elapsed - 40;
            if (localTick < 0 || localTick > 34) return;

            float progress = Mathf.Clamp01(localTick / 26f);
            Vector3 origin = wave == 0
                ? new Vector3(-2.1f, 0f, 2.1f)
                : new Vector3(2.1f, 0f, 2.1f);
            Vector3 end = wave == 0
                ? new Vector3(.22f, 0f, -.22f)
                : new Vector3(-.22f, 0f, -.22f);
            Vector3 position = drawLoc + Vector3.Lerp(origin, end, progress);
            position.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            float scalePulse = 1f + Mathf.Sin(progress * Mathf.PI) * .16f;
            Graphics.DrawMesh(
                MeshPool.plane10,
                Matrix4x4.TRS(position, Quaternion.identity,
                    new Vector3((wave == 0 ? 1.5f : -1.5f) * scalePulse, 1f,
                        1.5f * scalePulse)),
                SpiritSwordMat.Material,
                0);
        }

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned || Map == null)
            {
                if (!Destroyed) Destroy(DestroyMode.Vanish);
                return;
            }

            int elapsed = Find.TickManager.TicksGame - startTick;
            if ((elapsed < 30 || elapsed >= 40 && elapsed < 70) && this.IsHashIntervalTick(2))
            {
                int wave = elapsed < 40 ? 0 : 1;
                int localTick = wave == 0 ? elapsed : elapsed - 40;
                float progress = Mathf.Clamp01(localTick / 26f);
                Vector3 origin = wave == 0
                    ? new Vector3(-2.1f, 0f, 2.1f)
                    : new Vector3(2.1f, 0f, 2.1f);
                Vector3 point = DrawPos + Vector3.Lerp(origin, Vector3.zero, progress);
                FleckMaker.ThrowLightningGlow(point, Map, .45f + progress * .7f);
            }

            while (stagesReleased < 3)
            {
                int releaseTick = stagesReleased == 0 ? 26 : stagesReleased == 1 ? 66 : 72;
                if (elapsed < releaseTick) break;
                if (stagesReleased < 2) ReleaseSlash(stagesReleased);
                else ReleaseFinalBurst();
                stagesReleased++;
            }
            if (elapsed >= 84) Destroy(DestroyMode.Vanish);
        }

        private void ReleaseSlash(int wave)
        {
            Vector3 center = Position.ToVector3Shifted();
            FleckCreationData slash = FleckMaker.GetDataStatic(
                center,
                Map,
                YJZDefOf.YJZ_Fleck_SwordRainImpact,
                1.45f);
            slash.rotation = wave == 0 ? -45f : 45f;
            Map.flecks.CreateFleck(slash);
            FleckMaker.ThrowLightningGlow(center, Map, 2.15f);
            for (int i = 0; i < 3; i++) FleckMaker.ThrowMicroSparks(center, Map);

            GenExplosion.DoExplosion(
                Position,
                Map,
                blastRadius,
                DamageDefOf.Cut,
                instigator,
                Mathf.Max(1, totalDamage / 4),
                armorPenetration,
                SoundDefOf.BulletImpact_Ground);
        }

        private void ReleaseFinalBurst()
        {
            Vector3 center = Position.ToVector3Shifted();
            FleckMaker.ThrowLightningGlow(center, Map, 3.6f);
            for (int i = 0; i < 7; i++) FleckMaker.ThrowMicroSparks(center, Map);
            FleckMaker.Static(Position, Map, FleckDefOf.PsycastAreaEffect, 1.15f);
            GenExplosion.DoExplosion(
                Position,
                Map,
                blastRadius,
                DamageDefOf.Cut,
                instigator,
                Mathf.Max(1, totalDamage - 2 * (totalDamage / 4)),
                armorPenetration,
                SoundDefOf.BulletImpact_Ground);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref instigator, "yjz_crossSwordInstigator");
            Scribe_Values.Look(ref startTick, "yjz_crossSwordStartTick", 0);
            Scribe_Values.Look(ref stagesReleased, "yjz_crossSwordWaves", 0);
            Scribe_Values.Look(ref blastRadius, "yjz_crossSwordRadius", 2.5f);
            Scribe_Values.Look(ref totalDamage, "yjz_crossSwordDamage", 224);
            Scribe_Values.Look(ref armorPenetration, "yjz_crossSwordArmorPenetration", .45f);
        }
    }
}
