using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

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
    /// The unique biological cloud qilin bound to Yunjianzi's lineage. It remains
    /// grounded and uses one coherent directional form; combat state affects
    /// abilities, not art.
    /// </summary>
    [StaticConstructorOnStartup]
    public sealed partial class CompYunjianziSkyWyvern : ThingComp
    {
        private static readonly Material ShieldMaterial = MaterialPool.MatFrom(
            "Yunjianzi/Effects/SwordWardShield", ShaderDatabase.Transparent);

        private static readonly Dictionary<Map, HashSet<CompYunjianziSkyWyvern>> SpawnedByMap =
            new Dictionary<Map, HashSet<CompYunjianziSkyWyvern>>();

        private int ownerThingId = -1;
        private int combatUntilTick;
        private readonly QilinBattleRoarState battleRoar = new QilinBattleRoarState();
        private float shield;
        private int shieldBrokenUntil;
        private int clawReadyTick;
        private int lastClawVariant = -1;
        private int missileReadyTick;
        private int crushingFieldReadyTick;
        private int breathCharges;
        private int nextBreathChargeTick;
        private bool animalBonded;
        private bool raidReleased;
        private bool legacyAgeHealthRepaired;
        // Kept only so old saves containing the former mech-control state can
        // be read. Runtime control is now entirely animal-based.
        private bool nativeControlBound;
        private int nextNativeControlAttemptTick;
        private Pawn ownerCache;
        private Graphic groundNorthGraphic;
        private Graphic groundEastGraphic;
        private Graphic groundSouthGraphic;
        private Graphic groundWestGraphic;
        private Material sleepingMaterial;

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
        private bool CombatShieldVisible
        {
            get
            {
                Pawn pawn = Pawn;
                if (pawn == null || !CanProjectShield) return false;
                // Following, hauling and other ordinary movement must not
                // display the combat field.  The raid state covers autonomous
                // fighting; Drafted covers direct player combat control.
                return raidReleased || pawn.Drafted;
            }
        }

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
            RepairLegacyGeneratedAgeAndHealth();
            ClearCarriedFilth();
            RemoveLegacyBorrowedRegeneration();
            EnsureCloudQilinRegeneration();
            EnsureAnimalBond();
            CloudQilinUtility.ClearLivestockDesignations(Pawn);
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

            if (!legacyAgeHealthRepaired) RepairLegacyGeneratedAgeAndHealth();
            if (!animalBonded && pawn.IsHashIntervalTick(600)) EnsureAnimalBond();
            if (pawn.IsHashIntervalTick(600))
            {
                EnsureCloudQilinRegeneration();
                CloudQilinUtility.ClearLivestockDesignations(pawn);
            }
            if (pawn.IsHashIntervalTick(60))
            {
                UpdateRaidReleaseState();
            }
            if (pawn.IsHashIntervalTick(15)) TickCloudQilinRegeneration();
            if (pawn.IsHashIntervalTick(30)) UpdateBattleRoar();
            // Five-tick resolution keeps 50% haste accurate for the custom
            // attacks (70/50-tick claws, 120-tick missiles, 20-tick pressure).
            if (!pawn.Downed && battleRoar.WasInCombat && pawn.IsHashIntervalTick(5))
            {
                TryClawSweep();
                if (Stage >= 5)
                {
                    TryCrushingField();
                    TryBackMissile();
                }
            }
            if (!pawn.IsHashIntervalTick(30)) return;

            if (Find.TickManager.TicksGame >= shieldBrokenUntil)
                shield = Mathf.Min(ShieldMax, shield + ShieldMax / 600f);
            RechargeBreath();
            ApplyAccelerationAura();
        }

        private bool BattleRoarActive => Pawn.health.hediffSet.HasHediff(YJZDefOf.YJZ_QilinBattleRoar);

        private void UpdateBattleRoar()
        {
            Pawn pawn = Pawn;
            int now = Find.TickManager.TicksGame;
            if (!battleRoar.TryEnterCombat(now, IsInCombat(), !pawn.Downed)) return;

            Hediff buff = pawn.health.hediffSet.GetFirstHediffOfDef(YJZDefOf.YJZ_QilinBattleRoar)
                ?? pawn.health.AddHediff(YJZDefOf.YJZ_QilinBattleRoar);
            buff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = QilinBattleRoarState.DurationTicks;

            // Match vanilla CompAbilityEffect_AnimalRoar. The life-stage angry
            // sound is available without Odyssey; its extra roar FX are optional.
            pawn.caller?.DoCall(forceAggressive: true);
            DefDatabase<SoundDef>.GetNamedSilentFail("Ability_TerrorRoar")?.PlayOneShot(new TargetInfo(pawn));
            EffecterDef roarEffect = DefDatabase<EffecterDef>.GetNamedSilentFail("TerrorRoar");
            if (roarEffect != null)
            {
                Effecter effecter = roarEffect.Spawn(pawn, pawn.Map);
                pawn.Map.effecterMaintainer.AddEffecterToMaintain(effecter, new TargetInfo(pawn), pawn, 20);
            }
            else
            {
                FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect, 3f);
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

        private void ClearCarriedFilth()
        {
            Pawn pawn = Pawn;
            pawn?.filth?.CarriedFilthListForReading?.Clear();
        }

        private void RemoveLegacyBorrowedRegeneration()
        {
            Pawn pawn = Pawn;
            if (pawn?.health == null) return;
            Hediff legacy = pawn.health.hediffSet.hediffs.FirstOrDefault(h => h.def.defName == "Regeneration");
            if (legacy != null) pawn.health.RemoveHediff(legacy);
        }

        private void EnsureCloudQilinRegeneration()
        {
            Pawn pawn = Pawn;
            if (pawn?.health == null || YJZDefOf.YJZ_SkyWyvernRegeneration == null ||
                pawn.health.hediffSet.HasHediff(YJZDefOf.YJZ_SkyWyvernRegeneration)) return;

            pawn.health.AddHediff(YJZDefOf.YJZ_SkyWyvernRegeneration);
        }

        private void TickCloudQilinRegeneration()
        {
            Pawn pawn = Pawn;
            if (pawn?.health == null || pawn.Dead) return;

            float healingPerDay = Stage >= 5 ? 5000f : Stage >= 4 ? 1500f : 500f;
            float remaining = healingPerDay * 15f / GenDate.TicksPerDay;
            List<Hediff_Injury> injuries = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .OrderByDescending(h => h.IsPermanent())
                .ThenByDescending(h => h.Severity)
                .ToList();
            foreach (Hediff_Injury injury in injuries)
            {
                float healed = Mathf.Min(remaining, injury.Severity);
                injury.Heal(healed);
                remaining -= healed;
                if (remaining <= 0f) return;
            }

            Hediff_MissingPart missing = pawn.health.hediffSet.hediffs
                .OfType<Hediff_MissingPart>()
                .FirstOrDefault(h => h.Part?.parent != null &&
                    pawn.health.hediffSet.GetFirstHediffMatchingPart<Hediff_MissingPart>(h.Part.parent) == null &&
                    pawn.health.hediffSet.GetFirstHediffMatchingPart<Hediff_AddedPart>(h.Part.parent) == null);
            if (missing == null) return;

            BodyPartRecord part = missing.Part;
            pawn.health.RestorePart(part);
            Hediff regrowingPart = pawn.health.AddHediff(HediffDefOf.Misc, part);
            if (regrowingPart != null)
            {
                float partHealth = pawn.health.hediffSet.GetPartHealth(part);
                regrowingPart.Severity = Mathf.Max(partHealth - 1f, partHealth * .9f);
            }
        }

        private void RepairLegacyGeneratedAgeAndHealth()
        {
            Pawn pawn = Pawn;
            if (legacyAgeHealthRepaired || pawn?.ageTracker == null || pawn.health == null) return;

            // Old versions let PawnGenerator choose an age across the 9999-year
            // lifespan. Only migrate those impossible biological ages; normal
            // injuries and diseases remain untouched.
            if (pawn.ageTracker.AgeBiologicalYears >= 100)
            {
                pawn.ageTracker.AgeBiologicalTicks = 30L * GenDate.TicksPerYear;
                HediffDef asthma = DefDatabase<HediffDef>.GetNamedSilentFail("Asthma");
                HediffDef arteryBlockage = DefDatabase<HediffDef>.GetNamedSilentFail("HeartArteryBlockage");
                List<Hediff> generatedOldAgeHediffs = pawn.health.hediffSet.hediffs
                    .Where(h => h.def == asthma || h.def == arteryBlockage)
                    .ToList();
                foreach (Hediff hediff in generatedOldAgeHediffs)
                    pawn.health.RemoveHediff(hediff);
            }
            legacyAgeHealthRepaired = true;
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
            if (now < clawReadyTick) return;
            Thing target = HostilesInRadius(Pawn.Position, 2.8f).OrderBy(t => t.Position.DistanceToSquared(Pawn.Position)).FirstOrDefault();
            if (target == null) { clawReadyTick = now + 15; return; }
            clawReadyTick = now + QilinBattleRoarState.AttackInterval(Stage >= 5 ? 75 : 105, BattleRoarActive);
            float damage = Stage >= 5 ? 58f : Stage >= 4 ? 42f : 30f;
            // Capture the impact before damage can destroy/despawn the target.
            YunjianziQilinClawEffects.Play(Pawn, target.DrawPos, Stage, ref lastClawVariant);
            if (Stage >= 5) YunjianziPurpleLightning.EmpPulse(target.DrawPos, Pawn.Map, 3.2f);
            foreach (Thing victim in HostilesInRadius(target.Position, Stage >= 5 ? 3.2f : 2.2f).ToList())
            {
                victim.TakeDamage(new DamageInfo(DamageDefOf.Cut, damage, Stage >= 5 ? 2f : .8f, -1f, Pawn));
                if (Stage >= 5)
                {
                    YunjianziEmp.Apply(victim, Pawn, 18f);
                    if (victim is Pawn p && p.stances != null) p.stances.stunner.StunFor(75, Pawn);
                }
            }
        }

        private void TryCrushingField()
        {
            int now = Find.TickManager.TicksGame;
            if (now < crushingFieldReadyTick) return;
            crushingFieldReadyTick = now + QilinBattleRoarState.AttackInterval(30, BattleRoarActive);
            foreach (Thing victim in HostilesInRadius(Pawn.Position, 2.8f).ToList())
            {
                victim.TakeDamage(new DamageInfo(DamageDefOf.Blunt, 12f, 1.2f, -1f, Pawn));
                if (Rand.Chance(.35f)) FleckMaker.ThrowDustPuff(victim.Position, Pawn.Map, 1.2f);
            }
        }

        private void TryBackMissile()
        {
            int now = Find.TickManager.TicksGame;
            if (now < missileReadyTick) return;
            Thing target = HostilesInRadius(Pawn.Position, 32f).OrderBy(t => t.Position.DistanceToSquared(Pawn.Position)).FirstOrDefault();
            if (target == null) { missileReadyTick = now + 15; return; }
            missileReadyTick = now + QilinBattleRoarState.AttackInterval(180, BattleRoarActive);
            YunjianziPurpleLightning.Glow(Pawn.DrawPos + new Vector3(0f, 0f, .65f), Pawn.Map, 1.4f);
            GenExplosion.DoExplosion(target.Position, Pawn.Map, 2.4f, DamageDefOf.Bomb, Pawn, 32, .55f);
        }

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            if (!dinfo.Def.harmsHealth || shield <= 0f) return;
            combatUntilTick = Find.TickManager.TicksGame + 600;
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
            if (Pawn?.Spawned == true) YunjianziPurpleLightning.Glow(Pawn.DrawPos, Pawn.Map, 1.2f + ShieldRadius * .06f);
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
            combatUntilTick = Find.TickManager.TicksGame + 600;
            float cost = YunjianziProjectileShieldUtility.InterceptionCost(projectile, Stage);
            shield -= cost;
            YunjianziPurpleLightning.Glow(projectile.ExactPosition, pawn.Map, 1f);
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
                defaultDesc = $"云麒麟引来云中积雷，自灵角一息吐尽。雷光会麻痹血肉、扰乱机械，并席卷落点周围。\n\n当前蓄雷：{breathCharges}/{BreathMax}",
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
                t => GenDraw.DrawRadiusRing(t.Cell, 3.9f, new Color(.72f, .4f, 1f)), null, Pawn, null,
                YunjianziAbilityTextures.SummonSkyWyvern, true);
        }

        private void CastBreath(IntVec3 cell)
        {
            if (breathCharges <= 0 || Pawn?.Spawned != true || cell.DistanceTo(Pawn.Position) > 15.9f) return;
            breathCharges--;
            if (breathCharges < BreathMax && nextBreathChargeTick <= Find.TickManager.TicksGame)
                nextBreathChargeTick = Find.TickManager.TicksGame + BreathRecharge;
            YunjianziPurpleLightning.Arc(Pawn.DrawPos, cell.ToVector3Shifted(), Pawn.Map, 1.6f);
            int bolts = Stage >= 5 ? 7 : 4;
            for (int i = 0; i < bolts; i++)
            {
                IntVec3 strike = CellFinder.RandomClosewalkCellNear(cell, Pawn.Map, 4);
                if (!strike.IsValid) strike = cell;
                YunjianziPurpleLightning.Strike(strike, Pawn.Map, Stage >= 5 ? 1.6f : 1.3f, Pawn);
                YunjianziCombatEffects.ChargeSwordWardFromLightning(Owner);
            }
            float damage = Stage >= 5 ? 120f : 72f;
            YunjianziPurpleLightning.EmpPulse(cell.ToVector3Shifted(), Pawn.Map, 3.9f);
            foreach (Thing victim in HostilesInRadius(cell, 3.9f).ToList())
            {
                victim.TakeDamage(new DamageInfo(DamageDefOf.Burn, damage, Stage >= 5 ? 2.2f : 1.1f, -1f, Pawn));
                YunjianziEmp.Apply(victim, Pawn, damage * .5f);
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
            // Our side art is already horizontal. Vanilla's animal resting
            // angle would turn it head-first into the bed instead of lying down.
            bool resting = pawn.GetPosture() != PawnPosture.Standing
                && !pawn.Downed && !pawn.Dead && pawn.Spawned;
            if (resting)
            {
                DrawSleepingBody(pawn);
                if (CombatShieldVisible) DrawCombatShield(pawn);
                return;
            }
            Graphic groundGraphic = GroundGraphicFor(pawn.Rotation);
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
            // Healthy rest was drawn above; retain vanilla downed/dead angles.
            float bodyAngle = pawn.Drawer?.renderer?.BodyAngle(PawnRenderFlags.None) ?? 0f;
            // Every direction is already authored in its final orientation.
            // Drawing the selected Graphic_Single on an unrotated mesh also
            // permits the side sprites to use a wide canvas: the long tail can
            // extend horizontally without shrinking the torso and neck.
            groundGraphic.DrawWorker(drawPos, Rot4.North, pawn.def, pawn, bodyAngle);
            if (CombatShieldVisible) DrawCombatShield(pawn);
        }

        private Graphic GroundGraphicFor(Rot4 rotation)
        {
            if (rotation == Rot4.North)
                return groundNorthGraphic ?? (groundNorthGraphic = LoadGroundGraphic("north", false));
            if (rotation == Rot4.East)
                return groundEastGraphic ?? (groundEastGraphic = LoadGroundGraphic("east", true));
            if (rotation == Rot4.West)
                return groundWestGraphic ?? (groundWestGraphic = LoadGroundGraphic("west", true));
            return groundSouthGraphic ?? (groundSouthGraphic = LoadGroundGraphic("south", false));
        }

        private void DrawSleepingBody(Pawn pawn)
        {
            if (sleepingMaterial == null)
                sleepingMaterial = MaterialPool.MatFrom(
                    "Yunjianzi/Pawn/SkyWyvern/Sleeping", ShaderDatabase.Cutout);
            // The approved alpha texture is 2:1 and tightly framed. Match its
            // visible width to the standing animal, not the padded ground canvas.
            // Mirror UVs via vanilla MeshPool instead of rotating the sleeping body.
            Matrix4x4 matrix = Matrix4x4.TRS(pawn.DrawPos, Quaternion.identity,
                new Vector3(Props.drawSize * 1.12f, 1f, Props.drawSize * .56f));
            Graphics.DrawMesh(pawn.Rotation == Rot4.West ? MeshPool.plane10Flip : MeshPool.plane10,
                matrix, sleepingMaterial, 0);
        }

        private Graphic LoadGroundGraphic(string direction, bool wide)
        {
            // Runtime PNGs are 1536x512 for sides and 768x512 for front/back.
            // Matching those aspect ratios preserves the same vertical scale
            // while retaining transparent space for the perspective tail.
            Vector2 size = new Vector2(
                Props.drawSize * (wide ? 3f : 1.5f),
                Props.drawSize);
            return GraphicDatabase.Get<Graphic_Single>(
                Props.groundTexPath + "_" + direction,
                ShaderDatabase.Cutout,
                size,
                Color.white);
        }

        private void DrawCombatShield(Pawn pawn)
        {
            float ratio = Mathf.Clamp01(shield / Mathf.Max(1f, ShieldMax));
            float pulse = 1f + Mathf.Sin((Find.TickManager.TicksGame + pawn.thingIDNumber) * .035f) * .018f;
            float diameter = ShieldRadius * 2f * pulse * Mathf.Lerp(.96f, 1f, ratio);
            Vector3 drawPos = pawn.DrawPos;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            float angle = (Find.TickManager.TicksGame * .08f + pawn.thingIDNumber * 17f) % 360f;
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(drawPos, Quaternion.AngleAxis(angle, Vector3.up),
                new Vector3(diameter, 1f, diameter));
            Graphics.DrawMesh(MeshPool.plane10, matrix, ShieldMaterial, 0);
        }

        private bool EnsureAnimalBond()
        {
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

                TrainableDef attack = DefDatabase<TrainableDef>.GetNamedSilentFail("AttackTarget");
                if (attack != null && !pawn.training.HasLearned(attack))
                    pawn.training.Train(attack, owner, true);

                // Vanilla Pawn_PlayerSettings.Master rejects a non-obedient
                // animal. Completing Obedience above normally assigns the
                // trainer automatically; retain this guarded assignment for
                // migrated saves whose existing Master points elsewhere.
                if (pawn.training.HasLearned(TrainableDefOf.Obedience) &&
                    pawn.playerSettings.Master != owner)
                    pawn.playerSettings.Master = owner;
                pawn.playerSettings.followDrafted = true;
                pawn.playerSettings.followFieldwork = true;

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
                Messages.Message("袭击来临，云麒麟自行解放并扑向敌群。", pawn,
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
                Log.ErrorOnce("[Yunjianzi] 云麒麟羁绊初始化失败：" + ex, pawn.thingIDNumber ^ 0x71D23);
                return false;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ownerThingId, "yjz_wyvernOwner", -1);
            ExposeQilinLossData();
            bool legacyFlying = false;
            Scribe_Values.Look(ref legacyFlying, "yjz_wyvernFlying", false);
            Scribe_Values.Look(ref combatUntilTick, "yjz_wyvernCombatUntil", 0);
            Scribe_Values.Look(ref battleRoar.WasInCombat, "yjz_qilinRoarWasInCombat", false);
            Scribe_Values.Look(ref battleRoar.ReadyTick, "yjz_qilinRoarReadyTick", 0);
            Scribe_Values.Look(ref shield, "yjz_wyvernShield", 0f);
            Scribe_Values.Look(ref shieldBrokenUntil, "yjz_wyvernShieldBrokenUntil", 0);
            Scribe_Values.Look(ref clawReadyTick, "yjz_wyvernClawReady", 0);
            Scribe_Values.Look(ref lastClawVariant, "yjz_wyvernLastClawVariant", -1);
            Scribe_Values.Look(ref missileReadyTick, "yjz_wyvernMissileReady", 0);
            Scribe_Values.Look(ref crushingFieldReadyTick, "yjz_wyvernCrushingFieldReady", 0);
            Scribe_Values.Look(ref breathCharges, "yjz_wyvernBreathCharges", 0);
            Scribe_Values.Look(ref nextBreathChargeTick, "yjz_wyvernNextBreath", 0);
            Scribe_Values.Look(ref nativeControlBound, "yjz_wyvernNativeControl", false);
            Scribe_Values.Look(ref animalBonded, "yjz_wyvernAnimalBonded", false);
            Scribe_Values.Look(ref raidReleased, "yjz_wyvernRaidReleased", false);
            Scribe_Values.Look(ref legacyAgeHealthRepaired, "yjz_wyvernAgeHealthRepaired", false);
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
    /// FilthRate=0 prevents newly generated animal filth, while this prefix also
    /// prevents the vanilla tracker from picking up and later dropping filth.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_FilthTracker), nameof(Pawn_FilthTracker.Notify_EnteredNewCell))]
    internal static class Patch_SkyWyvernNeverTracksFilth
    {
        private static bool Prefix(Pawn ___pawn)
        {
            return ___pawn?.TryGetComp<CompYunjianziSkyWyvern>() == null;
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
            // A home-map threat can be sealed behind walls or live in a
            // disconnected pocket.  Reject it here instead of repeatedly
            // issuing an attack job which the animal pather cannot start.
            return GenHostility.IsActiveThreatToPlayer(attackTarget) &&
                target.Spawned && target.Map == pawn.Map &&
                pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly);
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
            if (target.IsValid) GenDraw.DrawRadiusRing(target.Cell, 3.9f, new Color(.72f, .4f, 1f));
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
            if (!CanCreateOrAscendSkyWyvern(BoundSkyWyverns())) yield break;
            int cooldown = SkyWyvernCooldownRemaining;
            Command_Action summon = new Command_Action
            {
                defaultLabel = "瑞麟践约",
                defaultDesc = "循着道书末页的旧约，呼唤世间唯一的云麒麟。形态按当前境界决定：紫府为云中灵麟，金丹为负霆瑞兽，化神为九天麟主；突破后可再次施展使其进阶。已有当前境界的麒麟时隐藏此技能。\n\n麒麟被消灭时散为云气，不留下尸体或死亡产物；此技能重新显示并进入10天冷却，冷却结束后可再次呼唤。正常进阶不触发冷却。\n\n消耗100点灵力。" +
                    (cooldown > 0 ? "\n当前冷却：" + SkyWyvernCooldownText : ""),
                icon = YunjianziAbilityTextures.SummonSkyWyvern,
                action = SummonSkyWyvern
            };
            if (cooldown > 0)
                summon.Disable("瑞麟正在重聚云气，还需" + SkyWyvernCooldownText + "。");
            else if (SummoningBlockedByCombat)
                summon.Disable("战斗中无法唤起云麒麟。请先解除征召并脱离交战。");
            else if (Energy < 100f)
                summon.Disable("灵力不足：需要100点。");
            yield return summon;
        }

        private List<Pawn> BoundSkyWyverns()
        {
            // Include caravans, transporters and nested holders: leaving the
            // current map does not free the world's unique companion slot.
            HashSet<Pawn> result = new HashSet<Pawn>();
            List<Thing> held = new List<Thing>();
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawns) result.Add(pawn);
                ThingOwnerUtility.GetAllThingsRecursively(map, held, true);
                foreach (Pawn pawn in held.OfType<Pawn>()) result.Add(pawn);
            }
            if (Find.World != null)
            {
                ThingOwnerUtility.GetAllThingsRecursively(Find.World, held, true);
                foreach (Pawn pawn in held.OfType<Pawn>()) result.Add(pawn);
            }
            if (Find.WorldPawns != null)
                foreach (Pawn pawn in Find.WorldPawns.AllPawnsAlive) result.Add(pawn);
            return result.Where(p => !p.Dead && !p.Destroyed && p.TryGetComp<CompYunjianziSkyWyvern>() != null).ToList();
        }

        private PawnKindDef DesiredSkyWyvernKind()
        {
            if (Realm >= 5) return YJZDefOf.YJZ_SkyWyvern_Spirit;
            if (Realm >= 4) return YJZDefOf.YJZ_SkyWyvern_Golden;
            return YJZDefOf.YJZ_SkyWyvern_Purple;
        }

        private void SummonSkyWyvern()
        {
            if (Pawn == null || !Pawn.Spawned) return;
            List<Pawn> existing = BoundSkyWyverns();
            // Recheck at activation so an old command cannot bypass quota/CD.
            if (!CanCreateOrAscendSkyWyvern(existing) || SkyWyvernCooldownRemaining > 0 ||
                RejectSummoningDuringCombat() || !TrySpend(100f)) return;
            PawnKindDef desired = DesiredSkyWyvernKind();

            // Ascension replaces the pawn. Preserve the gathered-resource cycle
            // just as recalling the same pawn preserves it.
            float salivaFullness = existing.Where(p => p.TryGetComp<CompYunjianziSkyWyvern>()?.IsBoundTo(Pawn) == true)
                .Select(p => p.TryGetComp<CompQilinSaliva>()?.Fullness ?? 0f).DefaultIfEmpty(0f).Max();
            Pawn wyvern = PawnGenerator.GeneratePawn(desired, Faction.OfPlayer);
            wyvern.TryGetComp<CompQilinSaliva>()?.RestoreFullness(salivaFullness);
            IntVec3 cell = CellFinder.RandomClosewalkCellNear(Pawn.Position, Pawn.Map, 4);
            if (!cell.IsValid) cell = Pawn.Position;
            GenSpawn.Spawn(wyvern, cell, Pawn.Map);
            CompYunjianziSkyWyvern comp = wyvern.TryGetComp<CompYunjianziSkyWyvern>();
            if (comp == null || !comp.BindTo(Pawn))
            {
                if (comp != null) comp.DismissWithoutLossCooldown();
                else wyvern.Destroy(DestroyMode.Vanish);
                Energy += 100f;
                Messages.Message("旧约未能应声，散去的灵力已复归经脉。", Pawn, MessageTypeDefOf.RejectInput, false);
                return;
            }
            // Retire the old form only after the replacement is fully bound.
            foreach (Pawn old in existing)
            {
                if (old.Spawned) FleckMaker.Static(old.Position, old.Map, FleckDefOf.PsycastSkipFlashEntry, 2f);
                old.TryGetComp<CompYunjianziSkyWyvern>()?.DismissWithoutLossCooldown();
            }
            FleckMaker.Static(cell, Pawn.Map, FleckDefOf.PsycastSkipOuterRingExit, 2.4f);
            Messages.Message("云气自四野归来。云麒麟踏出云门，将额角轻抵在新传人的掌中。旧约已续。", wyvern, MessageTypeDefOf.PositiveEvent, false);
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

    [HarmonyPatch(typeof(MassUtility), nameof(MassUtility.Capacity))]
    internal static class Patch_MassUtility_Capacity_YunjianziSkyWyvern
    {
        private const float CaravanCapacity = 1000f;

        private static bool Prefix(Pawn p, StringBuilder explanation, ref float __result)
        {
            if (p?.TryGetComp<CompYunjianziSkyWyvern>() == null) return true;

            __result = CaravanCapacity;
            if (explanation != null)
            {
                if (explanation.Length > 0) explanation.AppendLine();
                explanation.Append("  - " + p.LabelShortCap + ": " + CaravanCapacity.ToStringMassOffset());
            }
            return false;
        }
    }
}
