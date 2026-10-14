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
    public sealed class ThingYunjianziLightningChain : Thing
    {
        // Retained for old saves / callers. New chains are purely transient map VFX.
        private bool visualRegistered;
        private List<Vector3> points = new List<Vector3>();
        private int expireTick;

        public void Initialize(List<Vector3> sourcePoints)
        {
            points = sourcePoints == null ? new List<Vector3>() : sourcePoints.ToList();
            expireTick = Find.TickManager.TicksGame + 24;
        }

        protected override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame >= expireTick && !Destroyed) Destroy(DestroyMode.Vanish);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (visualRegistered || points == null || points.Count < 2 || Map != Find.CurrentMap ||
                Find.TickManager.TicksGame >= expireTick) return;
            visualRegistered = true;
            YunjianziPurpleLightning.Chain(points, Map);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref points, "yjz_chainPoints", LookMode.Value);
            Scribe_Values.Look(ref expireTick, "yjz_chainExpireTick", 0);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && points == null) points = new List<Vector3>();
        }
    }

    [StaticConstructorOnStartup]
    public sealed class ThingYunjianziThunderSpearAnchor : Thing
    {
        private static readonly Material LeftMaterial = MaterialPool.MatFrom(
            "Yunjianzi/Effects/ThunderSpear/ThunderSpear_PlantedLeft", ShaderDatabase.MoteGlow);
        private static readonly Material RightMaterial = MaterialPool.MatFrom(
            "Yunjianzi/Effects/ThunderSpear/ThunderSpear_PlantedRight", ShaderDatabase.MoteGlow);
        private bool pointsRight;
        private int expireTick;

        public void Initialize(bool spearPointsRight, int durationTicks)
        {
            pointsRight = spearPointsRight;
            expireTick = Find.TickManager.TicksGame + durationTicks;
        }

        protected override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame >= expireTick && !Destroyed) Destroy(DestroyMode.Vanish);
            else if (Spawned)
            {
                // Keep the planted weapon itself rigid.  Motion comes from
                // short-lived electrical particles around the impact point.
                if (this.IsHashIntervalTick(20))
                    YunjianziPurpleLightning.Glow(DrawPos, Map, .72f);
                if (this.IsHashIntervalTick(13))
                {
                    Vector3 outer = DrawPos + Vector3Utility.RandomHorizontalOffset(Rand.Range(.35f, 1.15f));
                    YunjianziPurpleLightning.Arc(outer, DrawPos, Map, .42f);
                }
                if (this.IsHashIntervalTick(9))
                    FleckMaker.ThrowMicroSparks(DrawPos + Vector3Utility.RandomHorizontalOffset(.28f), Map);
                if (this.IsHashIntervalTick(47))
                    YunjianziPurpleLightning.Burst(DrawPos, Map, .38f);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            drawLoc.y = Altitudes.AltitudeFor(AltitudeLayer.ItemImportant);
            float size = 3.4f * ThunderSpearGlow.ChargeAndAnchorScale;
            Matrix4x4 matrix = Matrix4x4.TRS(drawLoc, Quaternion.identity, new Vector3(size, 1f, size));
            Graphics.DrawMesh(MeshPool.plane10, matrix, pointsRight ? RightMaterial : LeftMaterial, 0, null, 0, ThunderSpearGlow.Properties);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref pointsRight, "yjz_thunderSpearPointsRight", false);
            Scribe_Values.Look(ref expireTick, "yjz_thunderSpearAnchorExpireTick", 0);
        }
    }

    public sealed class ThingYunjianziSwordRain : Thing
    {
        private Thing instigator;
        private int realmAtCast;
        private int startTick;
        private int wavesReleased;
        private int waveCount = 2;
        private float blastRadius = 3.9f;
        private int damagePerWave = 42;
        private float armorPenetration = .62f;

        public void Initialize(Pawn caster, int realm, int waves, float radius)
        {
            Initialize((Thing)caster, realm, waves, radius, 42, .62f);
        }

        public void Initialize(Thing caster, int realm, int waves, float radius, int damagePerWave, float armorPenetration)
        {
            instigator = caster;
            realmAtCast = realm;
            waveCount = Mathf.Max(2, waves);
            blastRadius = Mathf.Max(1f, radius);
            this.damagePerWave = Mathf.Max(1, damagePerWave);
            this.armorPenetration = Mathf.Max(0f, armorPenetration);
            startTick = Find.TickManager.TicksGame;
        }

        // This Thing is only a timed gameplay driver. All visible content is
        // emitted as Flecks, so drawing the driver itself would show RimWorld's
        // magenta missing-texture square at the impact center.
        protected override void DrawAt(Vector3 drawLoc, bool flip = false) { }

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned || Map == null)
            {
                if (!Destroyed) Destroy(DestroyMode.Vanish);
                return;
            }

            int elapsed = Find.TickManager.TicksGame - startTick;
            int finalWaveTick = 35 + (waveCount - 1) * 47;
            if (elapsed < finalWaveTick + 18 && this.IsHashIntervalTick(2)) SpawnFallingBlade();
            while (wavesReleased < waveCount && elapsed >= 35 + wavesReleased * 47)
            {
                ReleaseWave();
                wavesReleased++;
            }
            if (elapsed >= finalWaveTick + 30) Destroy(DestroyMode.Vanish);
        }

        private void SpawnFallingBlade()
        {
            float angle = Rand.Range(0f, 360f);
            float distance = Mathf.Sqrt(Rand.Value) * blastRadius;
            Vector3 point = Position.ToVector3Shifted();
            point.x += Mathf.Cos(angle * Mathf.Deg2Rad) * distance;
            point.z += Mathf.Sin(angle * Mathf.Deg2Rad) * distance;
            FleckCreationData blade = FleckMaker.GetDataStatic(point, Map, YJZDefOf.YJZ_Fleck_SwordRainBlade,
                Rand.Range(.78f, 1.22f));
            blade.rotation = Rand.Range(-14f, 14f);
            Map.flecks.CreateFleck(blade);
            if (Rand.Chance(.25f)) FleckMaker.ThrowLightningGlow(point, Map, Rand.Range(.35f, .75f));
        }

        private void ReleaseWave()
        {
            for (int i = 0; i < 18; i++) SpawnFallingBlade();
            FleckCreationData impact = FleckMaker.GetDataStatic(Position.ToVector3Shifted(), Map,
                YJZDefOf.YJZ_Fleck_SwordRainImpact, 1.25f);
            impact.rotation = Rand.Range(0f, 360f);
            Map.flecks.CreateFleck(impact);
            FleckMaker.ThrowLightningGlow(Position.ToVector3Shifted(), Map, 3.2f);
            FleckMaker.ThrowMicroSparks(Position.ToVector3Shifted(), Map);

            GenExplosion.DoExplosion(Position, Map, blastRadius, DamageDefOf.Cut, instigator,
                damagePerWave, armorPenetration, SoundDefOf.BulletImpact_Ground);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // Keep the original save key so pre-turret saves continue to load.
            // Pawn references written by older versions are valid Thing refs.
            Scribe_References.Look(ref instigator, "yjz_swordRainOwner");
            Scribe_Values.Look(ref realmAtCast, "yjz_swordRainRealm", 3);
            Scribe_Values.Look(ref startTick, "yjz_swordRainStartTick", 0);
            Scribe_Values.Look(ref wavesReleased, "yjz_swordRainWaves", 0);
            Scribe_Values.Look(ref waveCount, "yjz_swordRainWaveCount", 2);
            Scribe_Values.Look(ref blastRadius, "yjz_swordRainRadius", 3.9f);
            Scribe_Values.Look(ref damagePerWave, "yjz_swordRainDamage", 42);
            Scribe_Values.Look(ref armorPenetration, "yjz_swordRainArmorPenetration", .62f);
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
        private CompYunjianziCultivation OwnerComp { get { return owner == null ? null : owner.TryGetComp<CompYunjianziCultivation>(); } }
        private bool OwnerHasSkill(string id)
        {
            CompYunjianziCultivation comp = OwnerComp;
            return comp != null && comp.HasSkill(id);
        }
        // Radius is authoritative for drawing, coverage, edge control and
        // lightning targeting. Every realm uses 1.5x the previous radius.
        public float Radius { get { return (4f + realmAtCast) * 3f; } }
        public IntVec3 EffectCenter
        {
            get { return followsOwner && owner != null && owner.Spawned && owner.Map == Map ? owner.Position : Position; }
        }
        private Vector3 EffectDrawCenter
        {
            get
            {
                Vector3 result = followsOwner && owner != null && owner.Spawned && owner.Map == Map
                    ? owner.DrawPos
                    : Position.ToVector3Shifted();
                // Keep the magical boundary above building tops but below pawns.
                result.y = Altitudes.AltitudeFor(AltitudeLayer.ItemImportant);
                return result;
            }
        }
        public bool Active { get { return Spawned && owner != null && !owner.Dead && (OwnerHasSkill(YunjianziSkills.ArrayBarrier) || Find.TickManager.TicksGame < expireTick); } }

        private IEnumerable<IntVec3> CoverageCenters()
        {
            yield return EffectCenter;
        }

        public bool Covers(IntVec3 cell)
        {
            return CoverageCenters().Any(center => cell.DistanceTo(center) <= Radius);
        }

        public void Initialize(Pawn caster, bool follow, int realm, int durationTicks)
        {
            owner = caster;
            followsOwner = follow;
            realmAtCast = realm;
            expireTick = durationTicks < 0 ? int.MaxValue : Find.TickManager.TicksGame + durationTicks;
            int interval = LightningInterval;
            nextLightningTick = interval == int.MaxValue ? int.MaxValue : Find.TickManager.TicksGame + interval;
        }

        private int LightningInterval
        {
            get
            {
                if (!OwnerHasSkill(YunjianziSkills.ArrayLightning)) return int.MaxValue;
                int[] intervalByRealm = { 600, 360, 300, 240, 180, 150 };
                int interval = OwnerHasSkill(YunjianziSkills.ArrayStorm)
                    ? 120
                    : intervalByRealm[Mathf.Clamp(realmAtCast, 0, intervalByRealm.Length - 1)];
                return OwnerWearingJiuxiaoXianxu() ? Mathf.Max(60, interval / 2) : interval;
            }
        }

        private int LightningTargetCount { get { return Mathf.Clamp(realmAtCast + 1, 3, 6); } }
        private float LightningDamage { get { return new[] { 0f, 20f, 32f, 42f, 54f, 68f }[Mathf.Clamp(realmAtCast, 0, 5)]; } }
        private float LightningRadius { get { return new[] { 0f, .45f, .7f, .95f, 1.2f, 1.5f }[Mathf.Clamp(realmAtCast, 0, 5)]; } }
        private float LightningArmorPenetration { get { return .25f + realmAtCast * .18f; } }

        private bool OwnerWearingJiuxiaoXianxu()
        {
            CompYunjianziCultivation comp = owner == null ? null : owner.TryGetComp<CompYunjianziCultivation>();
            return comp != null && comp.WearingJiuxiaoXianxu;
        }

        protected override void Tick()
        {
            base.Tick();
            // Realtime drawing is culled against Thing.Position.  A pawn-bound
            // array therefore has to move its hidden core as well as its visual
            // center, otherwise the field vanishes once its original map section
            // leaves the camera. Thing.Position safely refreshes ThingGrid,
            // cover and region registrations for a spawned thing.
            if (followsOwner && owner != null && owner.Spawned && owner.Map == Map && Position != owner.Position)
                Position = owner.Position;
            if (!Active)
            {
                if (!Destroyed) Destroy(DestroyMode.Vanish);
                return;
            }
            if (this.IsHashIntervalTick(30) && TryMigrateRealmDef()) return;
            // Save migration: arrays cast before this update become the single
            // pawn-bound field as soon as 身载周天 is known.
            followsOwner = OwnerHasSkill(YunjianziSkills.MovingArray);
            if (OwnerHasSkill(YunjianziSkills.ArrayEdgeSlow) && this.IsHashIntervalTick(5)) ApplyEdgeControl();
            if (this.IsHashIntervalTick(60)) ApplyFriendlyEffects();
            if (this.IsHashIntervalTick(90)) ApplyTaunt();
            if (OwnerHasSkill(YunjianziSkills.ArrayLightning) && nextLightningTick == int.MaxValue)
                nextLightningTick = Find.TickManager.TicksGame + LightningInterval;
            if (OwnerHasSkill(YunjianziSkills.ArrayLightning) && Find.TickManager.TicksGame >= nextLightningTick)
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
            // The fixed core remains the attackable anchor. The following field
            // is a pawn-bound aura (as in MoeLotl's fire ring), so it must not
            // redirect enemies toward its old ThingGrid cell.
            if (followsOwner) return;
            if (Map == null || owner == null || Faction == null) return;
            float tauntRadius = Radius + 6f;
            IntVec3 center = EffectCenter;
            foreach (Pawn pawn in GenRadial.RadialDistinctThingsAround(center, Map, tauntRadius, true).OfType<Pawn>())
            {
                if (pawn.Dead || pawn.Downed || !pawn.Spawned || pawn.Faction == null || pawn.Faction == Faction.OfPlayer ||
                    pawn.InMentalState || !pawn.HostileTo(owner) || pawn.WorkTagIsDisabled(WorkTags.Violent) ||
                    !GenSight.LineOfSight(pawn.Position, center, Map)) continue;

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
                if (!pawn.Dead && Covers(pawn.Position)) yield return pawn;
        }

        private bool Friendly(Pawn pawn)
        {
            return pawn != null && owner != null && pawn.Faction == owner.Faction;
        }

        private void ApplyEdgeControl()
        {
            if (Map == null || owner == null) return;
            foreach (Pawn pawn in Map.mapPawns.AllPawnsSpawned)
            {
                if (pawn == null || pawn.Dead || pawn.Downed || !pawn.HostileTo(owner) || pawn.health == null) continue;
                bool touchesEdge = CoverageCenters().Any(center => Mathf.Abs(pawn.Position.DistanceTo(center) - Radius) <= 1.1f);
                if (!touchesEdge || pawn.health.hediffSet.HasHediff(YJZDefOf.YJZ_ArrayEdgeControlImmunity)) continue;

                CompYunjianziCultivation cultivation = OwnerComp;
                bool paidForBoundaryLightning = cultivation != null && cultivation.TrySpend(10f, false);

                Hediff slow = HediffMaker.MakeHediff(YJZDefOf.YJZ_ArrayEdgeSlow, pawn);
                pawn.health.AddHediff(slow);
                HediffComp_Disappears slowTimer = slow.TryGetComp<HediffComp_Disappears>();
                if (slowTimer != null)
                    slowTimer.SetDuration(OwnerHasSkill(YunjianziSkills.ArrayEdgeStun) ? 780 : 600);
                Hediff immunity = HediffMaker.MakeHediff(YJZDefOf.YJZ_ArrayEdgeControlImmunity, pawn);
                pawn.health.AddHediff(immunity);
                if (OwnerHasSkill(YunjianziSkills.ArrayEdgeStun) && pawn.stances != null && pawn.stances.stunner != null)
                    pawn.stances.stunner.StunFor(180, owner);
                if (paidForBoundaryLightning)
                    StrikeCell(pawn.Position, 10f, 1.5f);
                FleckMaker.Static(pawn.Position, Map, FleckDefOf.PsycastAreaEffect, .55f);
            }
        }

        private void ApplyFriendlyEffects()
        {
            foreach (Pawn pawn in PawnsInside().Where(Friendly))
            {
                Hediff_Injury injury = pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>()
                    .Where(h => h.Severity > 0f && !h.IsPermanent())
                    .OrderByDescending(h => h.BleedRate).ThenByDescending(h => h.Severity).FirstOrDefault();
                if (injury != null && OwnerHasSkill(YunjianziSkills.ArrayRecovery)) injury.Heal(Mathf.Min(.25f, injury.Severity));
                if (OwnerHasSkill(YunjianziSkills.AllyArrayShield)) RefreshArrayShield(pawn);
            }
            if (OwnerHasSkill(YunjianziSkills.SwordWard))
            {
                CompYunjianziCultivation comp = owner.TryGetComp<CompYunjianziCultivation>();
                if (comp != null && Covers(owner.Position)) comp.Shield += 2f;
            }
        }

        private void RefreshArrayShield(Pawn pawn, float amount = 1f, bool fullOnCreate = true)
        {
            Hediff shield = pawn.health.hediffSet.GetFirstHediffOfDef(YJZDefOf.YJZ_ArrayShield);
            if (shield == null)
            {
                shield = HediffMaker.MakeHediff(YJZDefOf.YJZ_ArrayShield, pawn);
                shield.Severity = fullOnCreate ? 35f : amount;
                pawn.health.AddHediff(shield);
            }
            else shield.Severity = Mathf.Min(35f, shield.Severity + amount);
            HediffComp_Disappears disappears = shield.TryGetComp<HediffComp_Disappears>();
            if (disappears != null) disappears.SetDuration(120);
        }

        private void CastLightning()
        {
            List<Thing> enemies = CoverageCenters()
                .SelectMany(center => GenRadial.RadialDistinctThingsAround(center, Map, Radius, true))
                .Where(t => t != null && t.Spawned && t.HostileTo(owner) &&
                    ((t is Pawn && !((Pawn)t).Dead) || t is Building_Turret))
                .Distinct()
                .OrderBy(t => CoverageCenters().Min(center => t.Position.DistanceToSquared(center)))
                .Take(LightningTargetCount).ToList();
            if (enemies.Count == 0) return;

            Dictionary<Thing, int> hitsByTarget = new Dictionary<Thing, int>();
            for (int strikeIndex = 0; strikeIndex < LightningTargetCount; strikeIndex++)
            {
                Thing enemy = enemies[strikeIndex % enemies.Count];
                if (enemy == null || enemy.Destroyed || !enemy.Spawned || !enemy.HostileTo(owner)) continue;
                CompYunjianziCultivation cultivation = OwnerComp;
                if (cultivation == null || !cultivation.TrySpend(10f, false)) break;
                int previousHits;
                hitsByTarget.TryGetValue(enemy, out previousHits);
                float repeatMultiplier = Mathf.Max(.5f, 1f - previousHits * .1f);
                StrikeCell(enemy.Position, 10f, repeatMultiplier);
                hitsByTarget[enemy] = previousHits + 1;
            }
        }

        public void StrikeCell(IntVec3 cell, float paidLightningCost, float strengthMultiplier = 1f)
        {
            if (Map == null || owner == null || !cell.InBounds(Map)) return;
            float radius = Mathf.Max(YunjianziLightningDamage.MinimumRadius,
                LightningRadius * Mathf.Sqrt(Mathf.Max(.25f, strengthMultiplier)));
            float damage = LightningDamage * strengthMultiplier;
            List<Thing> things = GenRadial.RadialDistinctThingsAround(cell, Map, radius, true).ToList();
            foreach (Thing thing in things)
            {
                Pawn pawn = thing as Pawn;
                if (pawn != null && !pawn.Dead && Friendly(pawn))
                {
                    if (OwnerHasSkill(YunjianziSkills.AllyArrayShield))
                        RefreshArrayShield(pawn, 4f + realmAtCast * 2f, false);
                    continue;
                }
            }
            YunjianziCombatEffects.ShowLightningAt(cell, Map, owner, paidLightningCost,
                Mathf.Clamp(.85f + realmAtCast * .09f + (strengthMultiplier - 1f) * .3f, .8f, 1.5f),
                damage, LightningArmorPenetration, radius);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (Active)
            {
                if (fieldPropertyBlock == null) fieldPropertyBlock = new MaterialPropertyBlock();
                Color fieldColor = realmAtCast >= 5
                    ? new Color(.90f, .98f, 1f, .62f)
                    : new Color(.72f, .94f, 1f, .48f);
                fieldPropertyBlock.SetColor(ShaderPropertyIDs.Color, fieldColor);
                DrawField(EffectDrawCenter);
            }
            // A pawn-bound formation has no stationary array eye at its old
            // spawn cell; the persistent Thing exists only to own saved state.
            if (!followsOwner) base.DrawAt(drawLoc, flip);
            if (Active && Find.Selector.IsSelected(this))
            {
                Color color = realmAtCast >= 5 ? new Color(.72f, .95f, 1f) : new Color(.35f, .85f, .92f);
                foreach (IntVec3 center in CoverageCenters()) GenDraw.DrawRadiusRing(center, Radius, color);
            }
        }

        private void DrawField(Vector3 center)
        {
            center.y = Altitudes.AltitudeFor(AltitudeLayer.ItemImportant);
            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(Radius * 2f, 1f, Radius * 2f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, ArrayFieldMaterial, 0, null, 0, fieldPropertyBlock);
        }

        public override string GetInspectString()
        {
            string duration = OwnerHasSkill(YunjianziSkills.ArrayBarrier) ? "阵势长存：持续牵引云间子的灵力" : "阵势将散：" + Math.Max(0, expireTick - Find.TickManager.TicksGame).ToStringTicksToPeriod();
            return "境界：" + new[] { "未定", "练气", "筑基", "紫府", "金丹", "化神" }[Mathf.Clamp(realmAtCast, 0, 5)] +
                "\n耐久：" + HitPoints + " / " + MaxHitPoints +
                "\n嘲讽半径：" + (Radius + 6f).ToString("0") + "格" +
                "\n仙阵半径：" + Radius.ToString("0") + "格" +
                (followsOwner ? "\n身载周天：阵域随身" : "") + "\n" + duration;
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
            foreach (ThingYunjianziArrayCore core in ActiveOn(map).Where(c => c.OwnerHasSkill(YunjianziSkills.ArrayBarrier)))
            {
                if (!core.Covers(current) || core.Covers(origin)) continue;
                Thing launcher = projectileLauncher;
                if (launcher != null && core.owner != null && !launcher.HostileTo(core.owner)) continue;
                CompYunjianziCultivation cultivation = core.OwnerComp;
                if (cultivation == null || !cultivation.TrySpend(10f, false)) continue;
                ThingDef projectileDef = projectile.def;
                bool reflect = core.OwnerHasSkill(YunjianziSkills.ArrayReflect) && launcher != null && launcher.Spawned && launcher.Map == map;
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
        private static readonly Material ThunderSpearChargeMaterial = MaterialPool.MatFrom(
            "Yunjianzi/Effects/ThunderSpear/ThunderSpear", ShaderDatabase.MoteGlow);
        private int arrayBlinkReadyTick;
        private int arrayStormReadyTick;
        private int arrayStormEndTick;
        private int arrayStormNextStrikeTick;
        private IntVec3 arrayStormTarget = IntVec3.Invalid;
        private int thunderSpearChargeEndTick;
        private IntVec3 thunderSpearChargeTarget = IntVec3.Invalid;

        private IEnumerable<ThingYunjianziArrayCore> ActiveArrays()
        {
            if (Pawn == null || Pawn.Map == null) return Enumerable.Empty<ThingYunjianziArrayCore>();
            return ThingYunjianziArrayCore.ActiveOn(Pawn.Map).Where(c => c.Owner == Pawn);
        }

        private void TickArraySystem()
        {
            if (HasSkill(YunjianziSkills.ArrayBarrier) && Pawn.IsHashIntervalTick(60) && ActiveArrays().Any())
            {
                if (!TrySpend(5f, false))
                {
                    CancelArrays();
                    if (Pawn.Faction == Faction.OfPlayer) Messages.Message("灵力耗尽，仙阵自行消散。", Pawn, MessageTypeDefOf.NegativeEvent);
                }
            }
            TickArrayStormBarrage();
        }

        private IEnumerable<Gizmo> GetArrayGizmos()
        {
            bool active = ActiveArrays().Any();
            bool follows = HasSkill(YunjianziSkills.MovingArray);
            bool sustained = HasSkill(YunjianziSkills.ArrayBarrier);
            string deployDescription = follows
                ? "身载周天，阵随身行。阵眼隐入周天，唯一阵域随云间子行止而移，无须在原地留驻琉璃阵眼。"
                : "以灵力凝成琉璃阵眼，在脚下展开驻留原地的雷阵。悟得身载周天后，阵眼隐去，唯一阵域将随步而移。";
            deployDescription += sustained
                ? "\n\n展开不消耗灵力；维持每秒消耗5点灵力，直到主动收阵或灵力耗尽。"
                : "\n\n布阵消耗" + (follows ? "100" : "50") + "点灵力，持续60秒。再次运诀即可收阵。";
            yield return new Command_Action
            {
                defaultLabel = active ? "仙阵诀·收阵" : "仙阵诀·布阵",
                defaultDesc = active
                    ? "收束雷旗与阵纹，立即撤去当前仙阵并停止一切维持消耗。"
                    : deployDescription,
                icon = YunjianziAbilityTextures.ImmortalArray,
                action = active ? (Action)CancelArrays : ActivateArrays
            };
            if (active && HasSkill(YunjianziSkills.ArrayTeleport))
            {
                Command_Action blink = new Command_Action
                {
                    defaultLabel = "仙阵诀·循纹神行",
                    defaultDesc = "借阵纹移形，可一步踏入仙阵覆盖之处。紫府之后，落足时震雷外荡，逼退近敌；修为愈深，雷势越盛。\n\n消耗100点灵力，冷却20秒。",
                    icon = YunjianziAbilityTextures.ArrayTeleport,
                    action = BeginArrayBlink
                };
                int remaining = arrayBlinkReadyTick - Find.TickManager.TicksGame;
                if (remaining > 0) blink.Disable("冷却中：" + (remaining / 60f).ToString("0.0") + "秒");
                else if (Energy + .001f < 100f) blink.Disable("灵力不足：需要100点。");
                yield return blink;
            }
            if (active && HasSkill(YunjianziSkills.ArrayStorm))
            {
                Command_Action storm = new Command_Action
                {
                    defaultLabel = "仙阵诀·万钧天狱",
                    defaultDesc = "在手中凝聚紫电雷枪并直线掷向阵内目标。沿途半径3格贯穿、落点半径7.5格爆发，每段基础伤害" + ThingYunjianziThunderSpear.DamageAtRealm(Realm).ToString("0") + "、穿甲1000%，可伤及敌方建筑；同一目标沿途只命中一次，落点独立结算。九霄仙墟的雷伤加成仍然生效。随后枪尖斜插入地，持续20秒引动天雷轰击。\n\n蓄力不超过2秒，消耗500点灵力，冷却10秒。",
                    icon = YunjianziAbilityTextures.ImmortalArray,
                    action = BeginArrayStormBarrage
                };
                int remaining = arrayStormReadyTick - Find.TickManager.TicksGame;
                if (remaining > 0) storm.Disable("冷却中：" + (remaining / 60f).ToString("0.0") + "秒");
                else if (Energy + .001f < 500f) storm.Disable("灵力不足：需要500点。");
                yield return storm;
            }
        }

        private void ActivateArrays()
        {
            if (Realm <= 0 || !Pawn.Spawned) return;
            bool follows = HasSkill(YunjianziSkills.MovingArray);
            bool sustained = HasSkill(YunjianziSkills.ArrayBarrier);
            int cost = sustained ? 0 : follows ? 100 : 50;
            if (cost > 0 && !TrySpend(cost)) return;
            CancelArrays();
            SpawnArray(follows, sustained ? -1 : 3600);
            string message = follows ? "阵眼隐入周天，仙阵随身展开。" : "琉璃阵眼落定，仙阵展开。";
            if (sustained) message += "阵法将持续消耗灵力。";
            Messages.Message(message, Pawn, MessageTypeDefOf.PositiveEvent);
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
            if (!cell.InBounds(Pawn.Map) || !cell.Standable(Pawn.Map) || !ActiveArrays().Any(c => c.Covers(cell)))
            {
                Messages.Message("目标必须是任一仙阵内部的可站立位置。", Pawn, MessageTypeDefOf.RejectInput);
                return;
            }
            if (!TrySpend(100f)) return;
            Map map = Pawn.Map;
            IntVec3 originCell = Pawn.Position;
            Vector3 origin = Pawn.DrawPos;
            Pawn.pather.StopDead();
            Pawn.Position = cell;
            Pawn.Notify_Teleported(false, true);
            arrayBlinkReadyTick = Find.TickManager.TicksGame + 1200;
            YunjianziCombatEffects.ShowArrayTeleport(origin, Pawn.DrawPos, map);
            ThunderBeastUtility.TrySummon(Pawn, originCell, map, true);
            ThunderBeastUtility.TrySummon(Pawn, cell, map, true);
            if (HasSkill(YunjianziSkills.ArrayTeleportShock))
            {
                YunjianziPurpleLightning.Burst(Pawn.DrawPos, map, 1.5f);
                ApplyArrayBlinkKnockback(cell, map);
            }
        }

        private void BeginArrayStormBarrage()
        {
            if (Find.TickManager.TicksGame < arrayStormReadyTick || Energy + .001f < 500f) return;
            TargetingParameters parms = new TargetingParameters();
            parms.canTargetLocations = true;
            parms.canTargetPawns = true;
            Find.Targeter.BeginTargeting(parms, delegate(LocalTargetInfo target) { TryStartArrayStormBarrage(target.Cell); });
        }

        private void TryStartArrayStormBarrage(IntVec3 cell)
        {
            if (Pawn == null || Pawn.Map == null || !cell.InBounds(Pawn.Map) || !ActiveArrays().Any(c => c.Covers(cell)))
            {
                Messages.Message("目标地点必须位于仙阵覆盖范围内。", Pawn, MessageTypeDefOf.RejectInput);
                return;
            }
            if (!TrySpend(500f)) return;
            thunderSpearChargeTarget = cell;
            thunderSpearChargeEndTick = Find.TickManager.TicksGame + 120;
            arrayStormTarget = IntVec3.Invalid;
            arrayStormEndTick = 0;
            arrayStormReadyTick = Find.TickManager.TicksGame + 600;
            Messages.Message("紫电聚形，万钧雷枪正在凝成。", Pawn, MessageTypeDefOf.ThreatBig, false);
        }

        private void TickArrayStormBarrage()
        {
            if (thunderSpearChargeEndTick > 0)
            {
                if (Pawn == null || Pawn.Map == null || !Pawn.Spawned || Pawn.Dead)
                {
                    thunderSpearChargeEndTick = 0;
                    thunderSpearChargeTarget = IntVec3.Invalid;
                    return;
                }
                if (Find.TickManager.TicksGame < thunderSpearChargeEndTick)
                {
                    if (Find.TickManager.TicksGame % 18 == 0)
                    {
                        Vector3 hand = Pawn.DrawPos + new Vector3(.3f, 0f, .4f);
                        YunjianziPurpleLightning.Arc(hand, hand + new Vector3(.15f, 0f, 1.35f), Pawn.Map, .6f);
                    }
                    return;
                }
                ThingYunjianziThunderSpear spear = (ThingYunjianziThunderSpear)ThingMaker.MakeThing(YJZDefOf.YJZ_Projectile_ThunderSpear);
                GenSpawn.Spawn(spear, Pawn.Position, Pawn.Map);
                spear.Initialize(Pawn, thunderSpearChargeTarget, Realm);
                thunderSpearChargeEndTick = 0;
                thunderSpearChargeTarget = IntVec3.Invalid;
                return;
            }
            if (arrayStormEndTick <= Find.TickManager.TicksGame || !arrayStormTarget.IsValid ||
                Pawn == null || Pawn.Map == null || !Pawn.Spawned) return;
            if (Find.TickManager.TicksGame < arrayStormNextStrikeTick) return;
            ThingYunjianziArrayCore core = ActiveArrays().FirstOrDefault();
            if (core == null)
            {
                arrayStormEndTick = 0;
                arrayStormTarget = IntVec3.Invalid;
                return;
            }

            float bombardRadius = 2.5f + Realm * .5f;
            List<Thing> enemies = GenRadial.RadialDistinctThingsAround(arrayStormTarget, Pawn.Map, bombardRadius, true)
                .Where(t => t != null && t.Spawned && t.HostileTo(Pawn) &&
                    ((t is Pawn && !((Pawn)t).Dead) || t is Building_Turret)).ToList();
            IntVec3 strikeCell;
            if (enemies.Count > 0)
                strikeCell = enemies.RandomElement().Position;
            else
            {
                List<IntVec3> cells = GenRadial.RadialCellsAround(arrayStormTarget, bombardRadius, true)
                    .Where(c => c.InBounds(Pawn.Map)).ToList();
                strikeCell = cells.Count > 0 ? cells.RandomElement() : arrayStormTarget;
            }
            core.StrikeCell(strikeCell, 5f, 1.15f);
            arrayStormNextStrikeTick += 60;
        }

        internal void NotifyThunderSpearImpact(IntVec3 cell, bool arrivedFromLeft)
        {
            if (Pawn == null || Pawn.Map == null || !cell.InBounds(Pawn.Map)) return;
            arrayStormTarget = cell;
            arrayStormNextStrikeTick = Find.TickManager.TicksGame;
            arrayStormEndTick = Find.TickManager.TicksGame + 1200;
            ThingYunjianziThunderSpearAnchor anchor =
                (ThingYunjianziThunderSpearAnchor)ThingMaker.MakeThing(YJZDefOf.YJZ_Effect_ThunderSpearAnchor);
            GenSpawn.Spawn(anchor, cell, Pawn.Map);
            // A spear arriving from the left keeps its tail to the upper-left,
            // while its spearhead is buried toward the lower-right.
            anchor.Initialize(arrivedFromLeft, 1200);
            Messages.Message("雷枪镇地，万钧天狱展开。", new TargetInfo(cell, Pawn.Map), MessageTypeDefOf.ThreatBig, false);
        }

        private void DrawThunderSpearCharge(Vector3 pawnDrawPos)
        {
            if (thunderSpearChargeEndTick <= Find.TickManager.TicksGame || !thunderSpearChargeTarget.IsValid ||
                Pawn == null || Pawn.Map == null || !Pawn.Spawned) return;
            float progress = 1f - Mathf.Clamp01((thunderSpearChargeEndTick - Find.TickManager.TicksGame) / 120f);
            float eased = Mathf.SmoothStep(.18f, 1f, progress);
            Vector3 direction = thunderSpearChargeTarget.ToVector3Shifted() - pawnDrawPos;
            direction.y = 0f;
            if (direction.sqrMagnitude < .001f) direction = Vector3.forward;
            direction.Normalize();
            Vector3 loc = pawnDrawPos + direction * .58f;
            loc.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float pulse = 1f + Mathf.Sin(Find.TickManager.TicksGame * .36f) * .07f;
            float scale = eased * pulse * ThunderSpearGlow.ChargeAndAnchorScale;
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.AngleAxis(angle, Vector3.up),
                new Vector3(1.55f * scale, 1f, 3.1f * scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, ThunderSpearChargeMaterial, 0, null, 0, ThunderSpearGlow.Properties);
        }

        private void ApplyArrayBlinkKnockback(IntVec3 center, Map map)
        {
            int effectiveRealm = HasSkill(YunjianziSkills.ArrayTeleportMastery) ? 5 : Mathf.Min(Realm, 4);
            int radius = Mathf.Clamp(effectiveRealm - 2, 1, 3);
            int distance = Mathf.Clamp(effectiveRealm - 1, 2, 4);
            YunjianziLightningDamage.ApplyEmpArea(center, map, Pawn, radius);
            List<Pawn> enemies = GenRadial.RadialDistinctThingsAround(center, map, radius, true)
                .OfType<Pawn>()
                .Where(p => p != Pawn && !p.Dead && p.Spawned && p.HostileTo(Pawn))
                .ToList();
            for (int index = 0; index < enemies.Count; index++)
            {
                Pawn enemy = enemies[index];
                int dx = Math.Sign(enemy.Position.x - center.x);
                int dz = Math.Sign(enemy.Position.z - center.z);
                if (dx == 0 && dz == 0)
                {
                    Rot4 fallback = new Rot4(index % 4);
                    dx = fallback.FacingCell.x;
                    dz = fallback.FacingCell.z;
                }
                IntVec3 direction = new IntVec3(dx, 0, dz);
                IntVec3 destination = enemy.Position;
                for (int step = 1; step <= distance; step++)
                {
                    IntVec3 candidate = enemy.Position + direction * step;
                    if (!candidate.InBounds(map) || !candidate.Standable(map) || candidate.Fogged(map) || candidate.GetFirstPawn(map) != null) break;
                    destination = candidate;
                }
                if (destination == enemy.Position) continue;
                Vector3 from = enemy.DrawPos;
                if (enemy.pather != null) enemy.pather.StopDead();
                enemy.Position = destination;
                enemy.Notify_Teleported(false, true);
                YunjianziCombatEffects.ShowKnockback(from, enemy.DrawPos, map);
            }
        }

        private void ExposeArrayData()
        {
            Scribe_Values.Look(ref arrayBlinkReadyTick, "yjz_arrayBlinkReadyTick", 0);
            Scribe_Values.Look(ref arrayStormReadyTick, "yjz_arrayStormReadyTick", 0);
            Scribe_Values.Look(ref arrayStormEndTick, "yjz_arrayStormEndTick", 0);
            Scribe_Values.Look(ref arrayStormNextStrikeTick, "yjz_arrayStormNextStrikeTick", 0);
            Scribe_Values.Look(ref arrayStormTarget, "yjz_arrayStormTarget", IntVec3.Invalid);
            Scribe_Values.Look(ref thunderSpearChargeEndTick, "yjz_thunderSpearChargeEndTick", 0);
            Scribe_Values.Look(ref thunderSpearChargeTarget, "yjz_thunderSpearChargeTarget", IntVec3.Invalid);
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
