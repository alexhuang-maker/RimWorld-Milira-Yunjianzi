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
    public sealed class CompProperties_YunjianziSkyWyvern : CompProperties
    {
        public int stage;
        public string groundTexPath;
        public float drawSize = 3f;

        public CompProperties_YunjianziSkyWyvern()
        {
            compClass = typeof(CompYunjianziSkyWyvern);
        }
    }

    /// <summary>
    /// The unique 神机诀 beast. It remains grounded and uses one coherent directional
    /// form; combat state affects abilities, not art.
    /// </summary>
    public sealed class CompYunjianziSkyWyvern : ThingComp
    {
        private static readonly Dictionary<Map, HashSet<CompYunjianziSkyWyvern>> SpawnedByMap =
            new Dictionary<Map, HashSet<CompYunjianziSkyWyvern>>();

        private int ownerThingId = -1;
        private int combatUntilTick;
        private float shield;
        private int shieldBrokenUntil;
        private int clawReadyTick;
        private int missileReadyTick;
        private int breathCharges;
        private int nextBreathChargeTick;
        private bool animalBonded;
        private bool raidReleased;
        // Kept only so old saves containing the former mech-control state can
        // be read. Runtime control is now entirely animal-based.
        private bool nativeControlBound;
        private int nextNativeControlAttemptTick;
        private Pawn ownerCache;
        private Graphic groundGraphic;

        private CompProperties_YunjianziSkyWyvern Props => (CompProperties_YunjianziSkyWyvern)props;
        public Pawn Pawn => parent as Pawn;
        public int Stage => Props.stage;
        public Pawn Owner
        {
            get
            {
                if (ownerThingId < 0) return null;
                if (ownerCache != null && ownerCache.thingIDNumber == ownerThingId && !ownerCache.Destroyed)
                    return ownerCache;
                foreach (Map map in Find.Maps)
                {
                    Pawn found = map.mapPawns.AllPawns.FirstOrDefault(p => p.thingIDNumber == ownerThingId);
                    if (found != null) return ownerCache = found;
                }
                Pawn worldPawn = Find.WorldPawns == null ? null : Find.WorldPawns.AllPawnsAlive.FirstOrDefault(p => p.thingIDNumber == ownerThingId);
                return ownerCache = worldPawn;
            }
        }
        public bool IsBoundTo(Pawn owner) => owner != null && ownerThingId == owner.thingIDNumber;
        public bool RaidReleased => raidReleased;
        public bool CanUseBreath => Stage >= 4 && breathCharges > 0 && Pawn?.Spawned == true;
        public bool CanProjectShield => shield > 0f && Find.TickManager.TicksGame >= shieldBrokenUntil;
        private float ShieldMax => Stage >= 5 ? 25000f : Stage >= 4 ? 8000f : 2500f;
        private float ShieldRadius => Stage >= 5 ? 10f : Stage >= 4 ? 8.5f : 7f;
        private int BreathMax => Stage >= 5 ? 5 : Stage >= 4 ? 2 : 0;
        private int BreathRecharge => Stage >= 5 ? 30000 : 60000;

        public bool BindTo(Pawn owner)
        {
            ownerThingId = owner?.thingIDNumber ?? -1;
            ownerCache = owner;
            shield = ShieldMax;
            breathCharges = BreathMax;
            nextBreathChargeTick = Find.TickManager.TicksGame + BreathRecharge;
            animalBonded = false;
            return EnsureAnimalBond();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Register(this);
            if (!respawningAfterLoad)
            {
                shield = ShieldMax;
                breathCharges = BreathMax;
                nextBreathChargeTick = Find.TickManager.TicksGame + BreathRecharge;
            }
            EnsureAnimalBond();
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            Unregister(this, map);
            base.PostDeSpawn(map, mode);
        }

        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = Pawn;
            if (pawn == null || pawn.Dead || !pawn.Spawned) return;

            if (!animalBonded && pawn.IsHashIntervalTick(600)) EnsureAnimalBond();
            if (pawn.IsHashIntervalTick(60))
            {
                DrawMovementEffects();
                UpdateRaidReleaseState();
            }
            if (!pawn.IsHashIntervalTick(30)) return;

            if (Find.TickManager.TicksGame >= shieldBrokenUntil)
                shield = Mathf.Min(ShieldMax, shield + ShieldMax / 600f);
            RechargeBreath();
            ApplyAccelerationAura();
            TryClawSweep();
            if (Stage >= 5)
            {
                TryCrushingField();
                TryBackMissile();
            }
        }

        private bool IsInCombat()
        {
            Pawn pawn = Pawn;
            int now = Find.TickManager.TicksGame;
            bool threatened = raidReleased || pawn.Drafted || pawn.mindState?.enemyTarget != null ||
                pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn).Any(t => t.Thing != null && t.Thing.Position.DistanceTo(pawn.Position) <= 25f);
            if (threatened) combatUntilTick = now + 600;
            return now < combatUntilTick;
        }

        private void RechargeBreath()
        {
            if (BreathMax <= 0 || breathCharges >= BreathMax) return;
            int now = Find.TickManager.TicksGame;
            if (nextBreathChargeTick <= 0) nextBreathChargeTick = now + BreathRecharge;
            while (breathCharges < BreathMax && now >= nextBreathChargeTick)
            {
                breathCharges++;
                nextBreathChargeTick += BreathRecharge;
            }
        }

        private void DrawMovementEffects()
        {
            Pawn pawn = Pawn;
            if (pawn == null || !pawn.pather.Moving) return;
            if (Rand.Chance(.3f))
                FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, .3f + Props.drawSize * .03f);
        }

        private void ApplyAccelerationAura()
        {
            Pawn pawn = Pawn;
            foreach (Pawn ally in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (ally.Faction != pawn.Faction || ally.Dead || ally.Position.DistanceTo(pawn.Position) > 9f) continue;
                Hediff hediff = ally.health?.hediffSet?.GetFirstHediffOfDef(YJZDefOf.YJZ_SkyWyvernAcceleration);
                if (hediff == null && ally.health != null) hediff = ally.health.AddHediff(YJZDefOf.YJZ_SkyWyvernAcceleration);
                if (hediff != null)
                {
                    hediff.Severity = 1f;
                    HediffComp_Disappears expires = hediff.TryGetComp<HediffComp_Disappears>();
                    if (expires != null) expires.ticksToDisappear = 120;
                }
            }
        }

        private IEnumerable<Thing> HostilesInRadius(IntVec3 center, float radius)
        {
            Pawn pawn = Pawn;
            return GenRadial.RadialDistinctThingsAround(center, pawn.Map, radius, true)
                .Where(t => t != null && t != pawn && t.HostileTo(pawn) && (!(t is Pawn p) || !p.Dead));
        }

        private void TryClawSweep()
        {
            int now = Find.TickManager.TicksGame;
            if (!IsInCombat() || now < clawReadyTick) return;
            Thing target = HostilesInRadius(Pawn.Position, 2.8f).OrderBy(t => t.Position.DistanceToSquared(Pawn.Position)).FirstOrDefault();
            if (target == null) return;
            clawReadyTick = now + (Stage >= 5 ? 75 : 105);
            int traces = Stage >= 5 ? 5 : 3;
            float damage = Stage >= 5 ? 58f : Stage >= 4 ? 42f : 30f;
            foreach (Thing victim in HostilesInRadius(target.Position, Stage >= 5 ? 3.2f : 2.2f).ToList())
            {
                victim.TakeDamage(new DamageInfo(DamageDefOf.Cut, damage, Stage >= 5 ? 2f : .8f, -1f, Pawn));
                if (Stage >= 5)
                {
                    victim.TakeDamage(new DamageInfo(DamageDefOf.EMP, 18f, 1f, -1f, Pawn));
                    if (victim is Pawn p && p.stances != null) p.stances.stunner.StunFor(75, Pawn);
                }
            }
            for (int i = 0; i < traces; i++)
            {
                Vector3 offset = new Vector3((i - (traces - 1) * .5f) * .16f, 0f, (i % 2 == 0 ? -.12f : .12f));
                GenDraw.DrawLineBetween(Pawn.DrawPos + offset, target.DrawPos + offset, SimpleColor.Cyan);
                FleckMaker.ThrowLightningGlow(target.DrawPos + offset, Pawn.Map, .8f);
            }
        }

        private void TryCrushingField()
        {
            foreach (Thing victim in HostilesInRadius(Pawn.Position, 2.8f).ToList())
            {
                victim.TakeDamage(new DamageInfo(DamageDefOf.Blunt, 12f, 1.2f, -1f, Pawn));
                if (Rand.Chance(.35f)) FleckMaker.ThrowDustPuff(victim.Position, Pawn.Map, 1.2f);
            }
        }

        private void TryBackMissile()
        {
            int now = Find.TickManager.TicksGame;
            if (!IsInCombat() || now < missileReadyTick) return;
            Thing target = HostilesInRadius(Pawn.Position, 32f).OrderBy(t => t.Position.DistanceToSquared(Pawn.Position)).FirstOrDefault();
            if (target == null) return;
            missileReadyTick = now + 180;
            FleckMaker.ThrowLightningGlow(Pawn.DrawPos + new Vector3(0f, 0f, .65f), Pawn.Map, 1.4f);
            GenExplosion.DoExplosion(target.Position, Pawn.Map, 2.4f, DamageDefOf.Bomb, Pawn, 32, .55f);
        }

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            if (!dinfo.Def.harmsHealth || shield <= 0f) return;
            float adaptation = Stage >= 4 ? (dinfo.Def.isRanged ? .55f : .72f) : 1f;
            float cost = dinfo.Amount * adaptation;
            if (shield >= cost)
            {
                shield -= cost;
                absorbed = true;
            }
            else
            {
                float pass = 1f - shield / Mathf.Max(1f, cost);
                dinfo.SetAmount(dinfo.Amount * pass);
                shield = 0f;
                shieldBrokenUntil = Find.TickManager.TicksGame + 1800;
            }
            if (Pawn?.Spawned == true) FleckMaker.ThrowLightningGlow(Pawn.DrawPos, Pawn.Map, 1.2f + ShieldRadius * .06f);
        }

        public bool TryInterceptProjectile(Projectile projectile)
        {
            Pawn pawn = Pawn;
            if (!CanProjectShield || projectile == null || !projectile.Spawned || pawn?.Spawned != true || projectile.Map != pawn.Map) return false;
            Thing launcher = projectile.Launcher;
            if (launcher != null && launcher.Faction == pawn.Faction) return false;
            Vector3 delta = projectile.ExactPosition - pawn.DrawPos;
            delta.y = 0f;
            if (delta.sqrMagnitude > ShieldRadius * ShieldRadius) return false;
            float cost = Mathf.Max(12f, projectile.DamageAmount * (Stage >= 4 ? .6f : 1f));
            shield -= cost;
            FleckMaker.ThrowLightningGlow(projectile.ExactPosition, pawn.Map, 1f);
            if (shield <= 0f)
            {
                shield = 0f;
                shieldBrokenUntil = Find.TickManager.TicksGame + 1800;
                FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect, 2.4f);
            }
            projectile.Destroy(DestroyMode.Vanish);
            return true;
        }

        // Retained as a save-safe legacy implementation; the actual command is
        // now supplied by AbilityDef so AI and player casts share one path.
        private IEnumerable<Gizmo> LegacyBreathGizmo()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
            if (Stage < 4 || Pawn?.Faction != Faction.OfPlayer) yield break;
            Command_Action breath = new Command_Action
            {
                defaultLabel = "雷霆龙息",
                defaultDesc = $"向十五格内喷吐高密度雷霆，造成迫击炮范围的雷电、EMP与麻痹。\n当前充能：{breathCharges}/{BreathMax}",
                icon = YunjianziAbilityTextures.SummonSkyWyvern,
                action = BeginBreathTargeting
            };
            if (breathCharges <= 0) breath.Disable("龙息尚未完成蓄力。");
            yield return breath;
        }

        private void BeginBreathTargeting()
        {
            if (breathCharges <= 0 || Pawn?.Spawned != true) return;
            TargetingParameters parms = new TargetingParameters
            {
                canTargetLocations = true,
                canTargetPawns = true,
                canTargetBuildings = true,
                validator = t => t.IsValid && t.Cell.InBounds(Pawn.Map) && t.Cell.DistanceTo(Pawn.Position) <= 15.9f
            };
            Find.Targeter.BeginTargeting(parms, t => CastBreath(t.Cell),
                t => GenDraw.DrawRadiusRing(t.Cell, 3.9f, new Color(.35f, .92f, 1f)), null, Pawn, null,
                YunjianziAbilityTextures.SummonSkyWyvern, true);
        }

        private void CastBreath(IntVec3 cell)
        {
            if (breathCharges <= 0 || Pawn?.Spawned != true || cell.DistanceTo(Pawn.Position) > 15.9f) return;
            breathCharges--;
            if (breathCharges < BreathMax && nextBreathChargeTick <= Find.TickManager.TicksGame)
                nextBreathChargeTick = Find.TickManager.TicksGame + BreathRecharge;
            GenDraw.DrawLineBetween(Pawn.DrawPos, cell.ToVector3Shifted(), SimpleColor.Cyan);
            int bolts = Stage >= 5 ? 7 : 4;
            for (int i = 0; i < bolts; i++)
            {
                IntVec3 strike = CellFinder.RandomClosewalkCellNear(cell, Pawn.Map, 4);
                if (!strike.IsValid) strike = cell;
                Pawn.Map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(Pawn.Map, strike));
                FleckMaker.ThrowLightningGlow(strike.ToVector3Shifted(), Pawn.Map, 2.4f);
            }
            float damage = Stage >= 5 ? 120f : 72f;
            foreach (Thing victim in HostilesInRadius(cell, 3.9f).ToList())
            {
                victim.TakeDamage(new DamageInfo(DamageDefOf.Burn, damage, Stage >= 5 ? 2.2f : 1.1f, -1f, Pawn));
                victim.TakeDamage(new DamageInfo(DamageDefOf.EMP, damage * .5f, 1f, -1f, Pawn));
                if (victim is Pawn p && p.stances != null) p.stances.stunner.StunFor(Stage >= 5 ? 240 : 150, Pawn);
            }
        }

        public bool TryCastBreath(IntVec3 cell)
        {
            if (!CanUseBreath || !cell.InBounds(Pawn.Map) || cell.DistanceTo(Pawn.Position) > 15.9f)
                return false;
            CastBreath(cell);
            return true;
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Pawn pawn = Pawn;
            if (pawn == null) return;
            if (groundGraphic == null)
                groundGraphic = GraphicDatabase.Get<Graphic_Multi>(Props.groundTexPath, ShaderDatabase.Cutout,
                    new Vector2(Props.drawSize, Props.drawSize), Color.white);
            Vector3 drawPos = pawn.DrawPos;
            if (pawn.pather != null && pawn.pather.Moving)
            {
                // Hidden chibi feet still need a readable two-beat gait. A
                // restrained vertical step and tiny lateral weight shift avoid
                // the old frictionless glide without reintroducing leg sprites.
                float phase = (Find.TickManager.TicksGame + pawn.thingIDNumber * 7) * .22f;
                drawPos.z += Mathf.Abs(Mathf.Sin(phase)) * .045f;
                Vector3 lateral = pawn.Rotation.AsIntVec3.ToVector3().RotatedBy(90f) * (Mathf.Sin(phase * .5f) * .018f);
                drawPos += lateral;
            }
            groundGraphic.Draw(drawPos, pawn.Rotation, pawn);
            if (shield > 0f && Find.TickManager.TicksGame >= shieldBrokenUntil)
                GenDraw.DrawRadiusRing(pawn.Position, ShieldRadius, new Color(.18f, .82f, 1f, .18f));
        }

        private bool EnsureAnimalBond()
        {
            if (animalBonded) return true;
            Pawn pawn = Pawn;
            Pawn owner = Owner;
            if (pawn == null || owner == null) return false;
            try
            {
                // This pawn used to be implemented as a bandwidth mech. Remove
                // that legacy relation before assigning the vanilla animal
                // master so old saves migrate without consuming bandwidth.
                if (ModsConfig.BiotechActive && pawn.relations != null &&
                    pawn.relations.DirectRelationExists(PawnRelationDefOf.Overseer, owner))
                    pawn.relations.RemoveDirectRelation(PawnRelationDefOf.Overseer, owner);

                if (pawn.Faction != Faction.OfPlayer) pawn.SetFaction(Faction.OfPlayer);
                if (pawn.playerSettings == null) pawn.playerSettings = new Pawn_PlayerSettings(pawn);
                if (pawn.training == null) pawn.training = new Pawn_TrainingTracker(pawn);
                pawn.playerSettings.Master = owner;
                pawn.playerSettings.followDrafted = true;

                TrainableDef[] essentials =
                {
                    TrainableDefOf.Tameness,
                    TrainableDefOf.Obedience,
                    TrainableDefOf.Release,
                    DefDatabase<TrainableDef>.GetNamedSilentFail("Haul"),
                    DefDatabase<TrainableDef>.GetNamedSilentFail("Rescue")
                };
                foreach (TrainableDef trainable in essentials)
                    if (trainable != null && !pawn.training.HasLearned(trainable))
                        pawn.training.Train(trainable, owner, true);

                if (ModsConfig.OdysseyActive)
                {
                    TrainableDef attack = DefDatabase<TrainableDef>.GetNamedSilentFail("AttackTarget");
                    if (attack != null && !pawn.training.HasLearned(attack))
                        pawn.training.Train(attack, owner, true);
                }

                animalBonded = true;
                return true;
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[Yunjianzi] Sky wyvern animal bonding failed: " + ex,
                    pawn.thingIDNumber ^ 0x71D23);
                return false;
            }
        }

        private void UpdateRaidReleaseState()
        {
            Pawn pawn = Pawn;
            if (pawn?.Map == null) return;
            bool shouldRelease = pawn.Map.IsPlayerHome &&
                GenHostility.AnyHostileActiveThreatToPlayer(pawn.Map);
            if (raidReleased == shouldRelease) return;

            raidReleased = shouldRelease;
            if (shouldRelease) combatUntilTick = Find.TickManager.TicksGame + 1200;
            if (pawn.mindState != null) pawn.mindState.enemyTarget = null;
            pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced, false, true);
            if (shouldRelease)
                Messages.Message("袭击来临，云龙自行解放并扑向敌群。", pawn,
                    MessageTypeDefOf.ThreatSmall, false);
        }

        private bool EnsureNativeControlRelation()
        {
            if (nativeControlBound) return true;
            if (Find.TickManager.TicksGame < nextNativeControlAttemptTick) return false;
            nextNativeControlAttemptTick = Find.TickManager.TicksGame + 300;
            Pawn pawn = Pawn;
            Pawn owner = Owner;
            if (!ModsConfig.BiotechActive || pawn == null || owner == null || pawn.relations == null) return false;
            bool relationAdded = false;
            try
            {
                if (owner.mechanitor == null)
                    owner.mechanitor = new Pawn_MechanitorTracker(owner);
                owner.mechanitor.Notify_PawnSpawned(true);
                if (!pawn.relations.DirectRelationExists(PawnRelationDefOf.Overseer, owner))
                {
                    pawn.relations.AddDirectRelation(PawnRelationDefOf.Overseer, owner);
                    relationAdded = true;
                }
                owner.mechanitor.AssignPawnControlGroup(pawn, MechWorkModeDefOf.Work);
                nativeControlBound = MechanitorUtility.GetMechControlGroup(pawn) != null;
                if (!nativeControlBound && relationAdded)
                    pawn.relations.RemoveDirectRelation(PawnRelationDefOf.Overseer, owner);
                return nativeControlBound;
            }
            catch (Exception ex)
            {
                if (relationAdded && pawn.relations.DirectRelationExists(PawnRelationDefOf.Overseer, owner))
                    pawn.relations.RemoveDirectRelation(PawnRelationDefOf.Overseer, owner);
                Log.ErrorOnce("[Yunjianzi] 云间机飞龙控制关系初始化失败：" + ex, pawn.thingIDNumber ^ 0x71D23);
                return false;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ownerThingId, "yjz_wyvernOwner", -1);
            bool legacyFlying = false;
            Scribe_Values.Look(ref legacyFlying, "yjz_wyvernFlying", false);
            Scribe_Values.Look(ref combatUntilTick, "yjz_wyvernCombatUntil", 0);
            Scribe_Values.Look(ref shield, "yjz_wyvernShield", 0f);
            Scribe_Values.Look(ref shieldBrokenUntil, "yjz_wyvernShieldBrokenUntil", 0);
            Scribe_Values.Look(ref clawReadyTick, "yjz_wyvernClawReady", 0);
            Scribe_Values.Look(ref missileReadyTick, "yjz_wyvernMissileReady", 0);
            Scribe_Values.Look(ref breathCharges, "yjz_wyvernBreathCharges", 0);
            Scribe_Values.Look(ref nextBreathChargeTick, "yjz_wyvernNextBreath", 0);
            Scribe_Values.Look(ref nativeControlBound, "yjz_wyvernNativeControl", false);
            Scribe_Values.Look(ref animalBonded, "yjz_wyvernAnimalBonded", false);
            Scribe_Values.Look(ref raidReleased, "yjz_wyvernRaidReleased", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ownerCache = null;
                nextNativeControlAttemptTick = 0;
                animalBonded = false;
            }
        }

        private static void Register(CompYunjianziSkyWyvern comp)
        {
            Map map = comp?.Pawn?.Map;
            if (map == null) return;
            if (!SpawnedByMap.TryGetValue(map, out HashSet<CompYunjianziSkyWyvern> set))
                SpawnedByMap[map] = set = new HashSet<CompYunjianziSkyWyvern>();
            set.Add(comp);
        }

        private static void Unregister(CompYunjianziSkyWyvern comp, Map map)
        {
            if (map == null || !SpawnedByMap.TryGetValue(map, out HashSet<CompYunjianziSkyWyvern> set)) return;
            set.Remove(comp);
            if (set.Count == 0) SpawnedByMap.Remove(map);
        }

        public static bool TryInterceptOnMap(Projectile projectile)
        {
            if (projectile?.Map == null || !SpawnedByMap.TryGetValue(projectile.Map, out HashSet<CompYunjianziSkyWyvern> set))
                return false;
            foreach (CompYunjianziSkyWyvern comp in set)
                if (comp != null && comp.TryInterceptProjectile(projectile)) return true;
            return false;
        }
    }

    /// <summary>
    /// Vanilla Animal_PreMain insertion. It stays dormant outside an active
    /// home-map threat, then uses the standard enemy acquisition and attack
    /// job machinery (including AI-castable abilities) for the released dragon.
    /// </summary>
    public sealed class JobGiver_YunjianziSkyWyvernRaidAssault : JobGiver_AIFightEnemies
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            CompYunjianziSkyWyvern comp = pawn?.TryGetComp<CompYunjianziSkyWyvern>();
            return comp?.RaidReleased == true ? base.TryGiveJob(pawn) : null;
        }

        protected override bool ExtraTargetValidator(Pawn pawn, Thing target)
        {
            if (!base.ExtraTargetValidator(pawn, target) || target is not IAttackTarget attackTarget)
                return false;
            return GenHostility.IsActiveThreatToPlayer(attackTarget);
        }
    }

    public sealed class CompProperties_AbilitySkyWyvernBreath : CompProperties_AbilityEffect
    {
        public CompProperties_AbilitySkyWyvernBreath()
        {
            compClass = typeof(CompAbilityEffect_SkyWyvernBreath);
        }
    }

    public sealed class CompAbilityEffect_SkyWyvernBreath : CompAbilityEffect
    {
        private CompYunjianziSkyWyvern Dragon => parent?.pawn?.TryGetComp<CompYunjianziSkyWyvern>();

        public override bool CanCast => Dragon?.CanUseBreath == true;

        public override bool GizmoDisabled(out string reason)
        {
            if (Dragon?.CanUseBreath == true)
            {
                reason = null;
                return false;
            }
            reason = "龙息尚未完成蓄力。";
            return true;
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return Dragon?.RaidReleased == true && CanTarget(target, hostileOnly: true);
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return CanTarget(target, hostileOnly: false);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            return CanTarget(target, hostileOnly: false);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Dragon?.TryCastBreath(target.Cell);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            if (target.IsValid) GenDraw.DrawRadiusRing(target.Cell, 3.9f, new Color(.35f, .92f, 1f));
        }

        private bool CanTarget(LocalTargetInfo target, bool hostileOnly)
        {
            Pawn caster = parent?.pawn;
            if (Dragon?.CanUseBreath != true || caster?.Map == null || !target.IsValid ||
                !target.Cell.InBounds(caster.Map) || target.Cell.DistanceTo(caster.Position) > 15.9f)
                return false;
            if (!hostileOnly) return true;
            return target.Thing is IAttackTarget attackTarget &&
                GenHostility.IsActiveThreatToPlayer(attackTarget);
        }
    }

    public sealed partial class CompYunjianziCultivation
    {
        private IEnumerable<Gizmo> GetSkyWyvernGizmos()
        {
            Command_Action summon = new Command_Action
            {
                defaultLabel = "神机诀·唤云间机飞龙",
                defaultDesc = "消耗100点灵力，唤醒或召回世界中唯一的云间机飞龙。已解锁更高形态时会以新躯替换旧躯。",
                icon = YunjianziAbilityTextures.SummonSkyWyvern,
                action = SummonSkyWyvern
            };
            if (Energy < 100f) summon.Disable("灵力不足：需要100点。");
            yield return summon;
        }

        private List<Pawn> BoundSkyWyverns()
        {
            List<Pawn> result = new List<Pawn>();
            foreach (Map map in Find.Maps)
                result.AddRange(map.mapPawns.AllPawns.Where(p => p.TryGetComp<CompYunjianziSkyWyvern>() != null && !p.Dead));
            if (Find.WorldPawns != null)
                result.AddRange(Find.WorldPawns.AllPawnsAlive.Where(p => p.TryGetComp<CompYunjianziSkyWyvern>() != null && !result.Contains(p)));
            return result;
        }

        private PawnKindDef DesiredSkyWyvernKind()
        {
            if (HasSkill(YunjianziSkills.ShenjiWyvernAscension)) return YJZDefOf.YJZ_SkyWyvern_Spirit;
            if (HasSkill(YunjianziSkills.ShenjiWyvernAdaptiveShield)) return YJZDefOf.YJZ_SkyWyvern_Golden;
            return YJZDefOf.YJZ_SkyWyvern_Purple;
        }

        private void SummonSkyWyvern()
        {
            if (Pawn == null || !Pawn.Spawned || !TrySpend(100f)) return;
            PawnKindDef desired = DesiredSkyWyvernKind();
            List<Pawn> existing = BoundSkyWyverns();
            Pawn same = existing.FirstOrDefault(p => p.kindDef == desired && p.TryGetComp<CompYunjianziSkyWyvern>()?.IsBoundTo(Pawn) == true);
            if (same != null)
            {
                same.TryGetComp<CompYunjianziSkyWyvern>()?.BindTo(Pawn);
                if (same.Spawned && same.Map != Pawn.Map) same.DeSpawn();
                if (!same.Spawned) GenSpawn.Spawn(same, Pawn.Position, Pawn.Map);
                same.Position = CellFinder.RandomClosewalkCellNear(Pawn.Position, Pawn.Map, 4);
                same.Notify_Teleported(false, true);
                FleckMaker.Static(same.Position, same.Map, FleckDefOf.PsycastSkipOuterRingExit, 2f);
                return;
            }

            foreach (Pawn old in existing.ToList())
            {
                if (old.Spawned) FleckMaker.Static(old.Position, old.Map, FleckDefOf.PsycastSkipFlashEntry, 2f);
                old.Destroy(DestroyMode.Vanish);
            }
            Pawn wyvern = PawnGenerator.GeneratePawn(desired, Faction.OfPlayer);
            IntVec3 cell = CellFinder.RandomClosewalkCellNear(Pawn.Position, Pawn.Map, 4);
            if (!cell.IsValid) cell = Pawn.Position;
            GenSpawn.Spawn(wyvern, cell, Pawn.Map);
            CompYunjianziSkyWyvern comp = wyvern.TryGetComp<CompYunjianziSkyWyvern>();
            if (comp == null || !comp.BindTo(Pawn))
            {
                wyvern.Destroy(DestroyMode.Vanish);
                Energy += 100f;
                Messages.Message("云间机飞龙未能进入原生控制组，召唤已回滚并返还灵力。", Pawn, MessageTypeDefOf.RejectInput, false);
                return;
            }
            FleckMaker.Static(cell, Pawn.Map, FleckDefOf.PsycastSkipOuterRingExit, 2.4f);
            Messages.Message("建木神枢开启，云间机飞龙应召而至。", wyvern, MessageTypeDefOf.PositiveEvent, false);
        }
    }

    [HarmonyPatch(typeof(Projectile), "Tick")]
    internal static class Patch_Projectile_Tick_YunjianziSkyWyvernShield
    {
        private static bool Prefix(Projectile __instance)
        {
            if (__instance?.Spawned != true || __instance.Map == null) return true;
            return !CompYunjianziSkyWyvern.TryInterceptOnMap(__instance);
        }
    }
}
