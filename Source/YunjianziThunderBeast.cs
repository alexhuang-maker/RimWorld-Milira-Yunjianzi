using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Yunjianzi
{
    public sealed class CompProperties_ThunderBeast : CompProperties
    {
        public int lifetimeTicks = 1800;

        public CompProperties_ThunderBeast()
        {
            compClass = typeof(CompThunderBeast);
        }
    }

    public sealed class CompThunderBeast : ThingComp
    {
        private int vanishTick = -1;
        private IntVec3 lastGlowCell = IntVec3.Invalid;
        private Pawn energyOwner;
        private float refundableEnergy;
        private bool energyRefunded;
        private bool populationChecked;

        private Pawn Pawn => parent as Pawn;

        public void Initialize(Pawn caster = null, float paidLightningCost = 0f)
        {
            vanishTick = Find.TickManager.TicksGame + ((CompProperties_ThunderBeast)props).lifetimeTicks;
            energyOwner = caster;
            refundableEnergy = Mathf.Max(0f, paidLightningCost * .8f);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            RefundLightningEnergy();
        }

        private void RefundLightningEnergy()
        {
            if (energyRefunded || refundableEnergy <= 0f)
                return;
            energyRefunded = true;
            CompYunjianziCultivation cultivation = energyOwner?.TryGetComp<CompYunjianziCultivation>();
            if (cultivation != null)
                cultivation.Energy += refundableEnergy;
        }

        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = Pawn;
            if (pawn == null || pawn.Destroyed) return;
            if (pawn.Spawned && (!populationChecked || pawn.IsHashIntervalTick(60)))
            {
                populationChecked = true;
                ThunderBeastLimit.Enforce(pawn.Map);
                if (pawn.Destroyed) return;
            }
            if (vanishTick < 0) Initialize();

            // CompGlower does not relocate itself when a pawn walks into another cell.
            // Re-register it only on cell changes so the pool of light follows the elemental.
            if (pawn.Spawned && pawn.Position != lastGlowCell)
            {
                pawn.TryGetComp<CompGlower>()?.ForceRegister(pawn.Map);
                lastGlowCell = pawn.Position;
            }

            if (Find.TickManager.TicksGame >= vanishTick)
            {
                YunjianziPurpleLightning.Glow(pawn.DrawPos, pawn.Map, 1.1f);
                pawn.Destroy(DestroyMode.Vanish);
                return;
            }

            if (pawn.Spawned && pawn.IsHashIntervalTick(30))
                EnsureAttacking(pawn);
        }

        private static void EnsureAttacking(Pawn pawn)
        {
            LocalTargetInfo current = pawn.CurJob?.targetA ?? LocalTargetInfo.Invalid;
            Thing currentThing = current.Thing;
            if (pawn.CurJobDef == JobDefOf.AttackMelee && IsValidEnemy(pawn, currentThing)) return;

            IAttackTarget target = AttackTargetFinder.BestAttackTarget(
                pawn,
                TargetScanFlags.NeedReachable,
                thing => IsValidEnemy(pawn, thing),
                0f,
                60f,
                canBashDoors: true,
                canBashFences: true);
            if (target == null)
            {
                if (pawn.CurJobDef == JobDefOf.Flee || pawn.CurJobDef == JobDefOf.FleeAndCower ||
                    pawn.CurJobDef == JobDefOf.FleeAndCowerShort)
                {
                    Job hold = JobMaker.MakeJob(JobDefOf.Wait_Combat);
                    hold.expiryInterval = 60;
                    pawn.jobs.StartJob(hold, JobCondition.InterruptForced);
                }
                return;
            }

            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target.Thing);
            job.expiryInterval = 300;
            job.checkOverrideOnExpire = true;
            pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, false, true, null, JobTag.Misc);
        }

        private static bool IsValidEnemy(Pawn pawn, Thing thing)
        {
            if (thing == null || thing.Destroyed || !thing.Spawned || !thing.HostileTo(pawn)) return false;
            Pawn enemyPawn = thing as Pawn;
            if (enemyPawn != null) return !enemyPawn.Dead;
            return thing is Building_Turret;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref vanishTick, "yjz_thunderBeastVanishTick", -1);
            Scribe_References.Look(ref energyOwner, "yjz_thunderBeastEnergyOwner");
            Scribe_Values.Look(ref refundableEnergy, "yjz_thunderBeastRefundableEnergy", 0f);
            Scribe_Values.Look(ref energyRefunded, "yjz_thunderBeastEnergyRefunded", false);
        }
    }

    internal static class ThunderBeastUtility
    {
        public static void TrySummon(Pawn caster, IntVec3 impactCell, Map map, bool exactCell = false,
            float paidLightningCost = 0f)
        {
            if (caster == null || map == null || !caster.Spawned || caster.Map != map) return;
            CompYunjianziCultivation cultivation = caster.TryGetComp<CompYunjianziCultivation>();
            if (cultivation?.WearingJiuxiaoXianxu != true || YJZDefOf.YJZ_ThunderBeast == null) return;
            // Reject before searching for a cell or generating a Pawn.
            if (ThunderBeastLimit.Enforce(map) >= ThunderBeastLimit.MaximumPerMap) return;

            IntVec3 spawnCell = impactCell;
            if (!exactCell || !spawnCell.InBounds(map) || !spawnCell.Standable(map))
            {
                if (!CellFinder.TryRandomClosewalkCellNear(impactCell, map, 2, out spawnCell,
                    cell => cell.Standable(map) && cell.GetFirstPawn(map) == null)) return;
            }

            Pawn beast = PawnGenerator.GeneratePawn(YJZDefOf.YJZ_ThunderBeast);
            if (caster.Faction != null) beast.SetFaction(caster.Faction);
            GenSpawn.Spawn(beast, spawnCell, map);
            beast.TryGetComp<CompThunderBeast>()?.Initialize(caster, paidLightningCost);
            beast.mindState.mentalStateHandler.Reset();
            YunjianziPurpleLightning.Glow(spawnCell.ToVector3Shifted(), map, 1.6f);
        }
    }
}
