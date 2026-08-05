using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    public enum YangfuCombatSpell
    {
        FireTornado,
        ThunderWind,
        BeanSoldiers
    }

    public sealed partial class CompYunjianziCultivation
    {
        private int fireTornadoReadyTick;
        private int thunderWindReadyTick;
        private int beanSoldiersReadyTick;

        private static readonly string[] FireTornadoRanks =
        {
            null, YunjianziSkills.FireTornadoI, YunjianziSkills.FireTornadoII,
            YunjianziSkills.FireTornadoIII, YunjianziSkills.FireTornadoIV, YunjianziSkills.FireTornadoV
        };

        private static readonly string[] ThunderWindRanks =
        {
            null, YunjianziSkills.ThunderWindI, YunjianziSkills.ThunderWindII,
            YunjianziSkills.ThunderWindIII, YunjianziSkills.ThunderWindIV, YunjianziSkills.ThunderWindV
        };

        private static readonly string[] BeanSoldierRanks =
        {
            null, YunjianziSkills.BeanSoldiersI, YunjianziSkills.BeanSoldiersII,
            YunjianziSkills.BeanSoldiersIII, YunjianziSkills.BeanSoldiersIV, YunjianziSkills.BeanSoldiersV
        };

        private int LearnedYangfuRank(string[] ranks)
        {
            for (int i = 5; i >= 1; i--)
                if (HasSkill(ranks[i])) return i;
            return 0;
        }

        private int YangfuCastRange(int rank) { return 10 + 5 * rank; }
        private float YangfuEnergyCost(int rank) { return new[] { 0f, 20f, 40f, 100f, 200f, 400f }[Mathf.Clamp(rank, 1, 5)]; }

        internal IEnumerable<Gizmo> GetYangfuCombatGizmos()
        {
            int fireRank = LearnedYangfuRank(FireTornadoRanks);
            if (fireRank > 0)
                yield return MakeYangfuCommand(YangfuCombatSpell.FireTornado, fireRank, YangfuEnergyCost(fireRank), fireTornadoReadyTick,
                    "阳符诀·火龙卷", "赤符燃尽后生成受控火龙卷，在施法点十格内游走、缩小并持续灼烧敌人。",
                    YunjianziAbilityTextures.FireTornado);

            int thunderRank = LearnedYangfuRank(ThunderWindRanks);
            if (thunderRank > 0)
                yield return MakeYangfuCommand(YangfuCombatSpell.ThunderWind, thunderRank, YangfuEnergyCost(thunderRank), thunderWindReadyTick,
                    "阳符诀·天雷神风", "紫符展开十格聚风场，持续牵引敌人；聚风结束后按境界连续降下强化天雷。",
                    YunjianziAbilityTextures.ThunderWind);

            int beanRank = LearnedYangfuRank(BeanSoldierRanks);
            if (beanRank > 0)
                yield return MakeYangfuCommand(YangfuCombatSpell.BeanSoldiers, beanRank, YangfuEnergyCost(beanRank), beanSoldiersReadyTick,
                    "阳符诀·撒豆成兵", "青符爆开，在目标附近召唤会追踪敌人并以纯爆炸伤害自毁的符兵。",
                    YunjianziAbilityTextures.BeanSoldiers);
        }

        private Command_Action MakeYangfuCommand(YangfuCombatSpell spell, int rank, float cost, int readyTick,
            string label, string description, Texture2D icon)
        {
            int range = YangfuCastRange(rank);
            Command_Action command = new Command_Action
            {
                defaultLabel = label + "·" + new[] { "", "练气", "筑基", "紫府", "金丹", "化神" }[rank],
                defaultDesc = string.Format("{0}\n\n射程：{1}格　消耗：{2:0}灵力", description, range, cost),
                icon = icon,
                action = delegate { BeginYangfuCombatTargeting(spell, rank, cost); }
            };
            int remaining = readyTick - Find.TickManager.TicksGame;
            if (remaining > 0) command.Disable(string.Format("冷却中：{0:0.0}秒", remaining / 60f));
            else if (Energy + .001f < cost) command.Disable(string.Format("灵力不足：需要{0:0}点。", cost));
            return command;
        }

        private void BeginYangfuCombatTargeting(YangfuCombatSpell spell, int rank, float cost)
        {
            if (Pawn == null || !Pawn.Spawned || Pawn.Map == null) return;
            float range = YangfuCastRange(rank) + .9f;
            TargetingParameters parms = new TargetingParameters
            {
                canTargetLocations = true,
                canTargetPawns = true,
                canTargetBuildings = true,
                canTargetItems = false,
                validator = target => target.IsValid && target.Cell.InBounds(Pawn.Map) &&
                    target.Cell.DistanceTo(Pawn.Position) <= range
            };
            Texture2D icon = spell == YangfuCombatSpell.FireTornado ? YunjianziAbilityTextures.FireTornado :
                spell == YangfuCombatSpell.ThunderWind ? YunjianziAbilityTextures.ThunderWind : YunjianziAbilityTextures.BeanSoldiers;
            Find.Targeter.BeginTargeting(parms, target => TryCastYangfuCombat(spell, rank, cost, target.Cell),
                target => GenDraw.DrawRadiusRing(target.Cell, spell == YangfuCombatSpell.BeanSoldiers ? 5f : 10f,
                    spell == YangfuCombatSpell.FireTornado ? new Color(1f, .24f, .12f) :
                    spell == YangfuCombatSpell.ThunderWind ? new Color(.68f, .30f, 1f) : new Color(.22f, 1f, .54f)),
                null, Pawn, null, icon, true);
        }

        private void TryCastYangfuCombat(YangfuCombatSpell spell, int rank, float cost, IntVec3 cell)
        {
            if (Pawn == null || !Pawn.Spawned || !cell.InBounds(Pawn.Map) ||
                cell.DistanceTo(Pawn.Position) > YangfuCastRange(rank) + .9f || !TrySpend(cost)) return;

            // Twelve in-game hours: these are strategic talisman arts, not routine attacks.
            int cooldown = 30000;
            int ready = Find.TickManager.TicksGame + cooldown;
            if (spell == YangfuCombatSpell.FireTornado) fireTornadoReadyTick = ready;
            else if (spell == YangfuCombatSpell.ThunderWind) thunderWindReadyTick = ready;
            else beanSoldiersReadyTick = ready;

            ThingDef markerDef = spell == YangfuCombatSpell.FireTornado ? YJZDefOf.YJZ_Effect_TalismanRed :
                spell == YangfuCombatSpell.ThunderWind ? YJZDefOf.YJZ_Effect_TalismanPurple : YJZDefOf.YJZ_Effect_TalismanGreen;
            ThingYangfuCastMarker marker = (ThingYangfuCastMarker)GenSpawn.Spawn(markerDef, cell, Pawn.Map);
            marker.Initialize(Pawn, spell, rank);
        }

        internal void ExposeYangfuCombatData()
        {
            Scribe_Values.Look(ref fireTornadoReadyTick, "yjz_fireTornadoReadyTick", 0);
            Scribe_Values.Look(ref thunderWindReadyTick, "yjz_thunderWindReadyTick", 0);
            Scribe_Values.Look(ref beanSoldiersReadyTick, "yjz_beanSoldiersReadyTick", 0);
        }
    }

    [StaticConstructorOnStartup]
    public sealed class ThingYangfuCastMarker : ThingWithComps
    {
        private Pawn caster;
        private YangfuCombatSpell spell;
        private int rank;
        private int spawnTick;

        public void Initialize(Pawn source, YangfuCombatSpell castSpell, int castRank)
        {
            caster = source;
            spell = castSpell;
            rank = Mathf.Clamp(castRank, 1, 5);
            spawnTick = Find.TickManager.TicksGame;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad) spawnTick = Find.TickManager.TicksGame;
        }

        protected override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame - spawnTick < 32) return;
            Map map = Map;
            IntVec3 cell = Position;
            Pawn source = caster;
            YangfuCombatSpell castSpell = spell;
            int castRank = rank;
            FleckMaker.ThrowLightningGlow(DrawPos, map, 1.5f);
            FleckMaker.ThrowMicroSparks(DrawPos, map);
            Destroy(DestroyMode.Vanish);
            if (source == null || source.Dead || map == null) return;
            if (castSpell == YangfuCombatSpell.FireTornado)
            {
                ThingYangfuFireTornado tornado = (ThingYangfuFireTornado)GenSpawn.Spawn(YJZDefOf.YJZ_Effect_FireTornado, cell, map);
                tornado.Initialize(source, castRank);
            }
            else if (castSpell == YangfuCombatSpell.ThunderWind)
            {
                ThingYangfuThunderWind wind = (ThingYangfuThunderWind)GenSpawn.Spawn(YJZDefOf.YJZ_Effect_ThunderWind, cell, map);
                wind.Initialize(source, castRank);
            }
            else
            {
                YangfuCombatUtility.SpawnBeanSoldiers(source, cell, castRank);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float age = Mathf.Clamp01((Find.TickManager.TicksGame - spawnTick) / 32f);
            float pulse = 1f + Mathf.Sin(age * Mathf.PI) * .32f;
            Vector3 loc = drawLoc;
            loc.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.Euler(0f, age * 120f - 20f, 0f), new Vector3(pulse, 1f, pulse));
            Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "yjz_yangfuCaster");
            Scribe_Values.Look(ref spell, "yjz_yangfuSpell", YangfuCombatSpell.FireTornado);
            Scribe_Values.Look(ref rank, "yjz_yangfuRank", 1);
            Scribe_Values.Look(ref spawnTick, "yjz_yangfuSpawnTick", 0);
        }
    }

    [StaticConstructorOnStartup]
    public sealed class ThingYangfuFireTornado : ThingWithComps
    {
        private Pawn caster;
        private int rank = 1;
        private int ageTicks;
        private int durationTicks = 1250;
        private Vector2 realPosition;
        private Vector2 destination;
        private IntVec3 origin;
        private int nextDestinationTick;

        public void Initialize(Pawn source, int castRank)
        {
            caster = source;
            rank = Mathf.Clamp(castRank, 1, 5);
            durationTicks = new[] { 0, 1250, 2188, 3125, 4063, 5000 }[rank];
            origin = Position;
            Vector3 p = Position.ToVector3Shifted();
            realPosition = destination = new Vector2(p.x, p.z);
            PickDestination();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                origin = Position;
                Vector3 p = Position.ToVector3Shifted();
                realPosition = destination = new Vector2(p.x, p.z);
            }
        }

        private void PickDestination()
        {
            for (int i = 0; i < 30; i++)
            {
                IntVec3 candidate = origin + GenRadial.RadialPattern[Rand.Range(0, GenRadial.NumCellsInRadius(10f))];
                if (candidate.InBounds(Map))
                {
                    destination = new Vector2(candidate.x + .5f, candidate.z + .5f);
                    break;
                }
            }
            nextDestinationTick = Find.TickManager.TicksGame + Rand.RangeInclusive(90, 180);
        }

        protected override void Tick()
        {
            base.Tick();
            ageTicks++;
            if (ageTicks >= durationTicks) { Destroy(DestroyMode.Vanish); return; }
            Vector2 delta = destination - realPosition;
            if (delta.sqrMagnitude < .16f || Find.TickManager.TicksGame >= nextDestinationTick) PickDestination();
            else realPosition += delta.normalized * .025f;
            IntVec3 next = new IntVec3(Mathf.RoundToInt(realPosition.x - .5f), 0, Mathf.RoundToInt(realPosition.y - .5f));
            if (next.InBounds(Map) && next.DistanceTo(origin) <= 10.5f) Position = next;
            if ((Find.TickManager.TicksGame + thingIDNumber) % 30 == 0) DamageAndBurn();
            if ((Find.TickManager.TicksGame + thingIDNumber) % 5 == 0)
            {
                FleckMaker.ThrowFireGlow(new Vector3(realPosition.x, 0f, realPosition.y), Map, Mathf.Lerp(1.6f, .55f, ageTicks / (float)durationTicks));
                FleckMaker.ThrowSmoke(new Vector3(realPosition.x, 0f, realPosition.y), Map, .7f);
            }
        }

        private void DamageAndBurn()
        {
            if (caster == null) return;
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(Position, Map, 2.4f, true)
                .Where(t => t != this && t.Spawned && t.HostileTo(caster)).ToList())
            {
                thing.TakeDamage(new DamageInfo(DamageDefOf.Flame, 5f + rank * 2.5f, .24f, -1f, caster));
                Pawn pawn = thing as Pawn;
                if (pawn != null && Rand.Chance(.24f + rank * .05f)) pawn.TryAttachFire(.25f + rank * .08f, caster);
            }
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, 1.6f, true).Where(c => c.InBounds(Map)).InRandomOrder().Take(1))
                if (Rand.Chance(.18f)) FireUtility.TryStartFireIn(cell, Map, .12f + rank * .05f, caster);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float life = Mathf.Clamp01(ageTicks / (float)Mathf.Max(1, durationTicks));
            float size = Mathf.Lerp(2.15f, .62f, life);
            Vector3 loc = new Vector3(realPosition.x, AltitudeLayer.Weather.AltitudeFor(), realPosition.y);
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.Euler(0f, ageTicks * 5.5f, 0f), new Vector3(size, 1f, size));
            Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "yjz_fireTornadoCaster");
            Scribe_Values.Look(ref rank, "yjz_fireTornadoRank", 1);
            Scribe_Values.Look(ref ageTicks, "yjz_fireTornadoAge", 0);
            Scribe_Values.Look(ref durationTicks, "yjz_fireTornadoDuration", 1250);
            Scribe_Values.Look(ref realPosition, "yjz_fireTornadoPosition");
            Scribe_Values.Look(ref destination, "yjz_fireTornadoDestination");
            Scribe_Values.Look(ref origin, "yjz_fireTornadoOrigin");
            Scribe_Values.Look(ref nextDestinationTick, "yjz_fireTornadoNextDestination", 0);
        }
    }

    [StaticConstructorOnStartup]
    public sealed class ThingYangfuThunderWind : ThingWithComps
    {
        private Pawn caster;
        private int rank = 1;
        private int ageTicks;
        private int pullTicks = 600;
        private int boltsRemaining;
        private int nextBoltTick;

        public void Initialize(Pawn source, int castRank)
        {
            caster = source;
            rank = Mathf.Clamp(castRank, 1, 5);
            pullTicks = rank * 300 + 300;
            boltsRemaining = rank;
        }

        protected override void Tick()
        {
            base.Tick();
            ageTicks++;
            if (ageTicks <= pullTicks)
            {
                if ((Find.TickManager.TicksGame + thingIDNumber) % 18 == 0) PullEnemies();
                if ((Find.TickManager.TicksGame + thingIDNumber) % 5 == 0)
                {
                    Vector3 point = Position.ToVector3Shifted() + Vector3Utility.RandomHorizontalOffset(Rand.Range(1f, 9f));
                    FleckMaker.ThrowDustPuffThick(point, Map, .8f, new Color(.55f, .38f, .88f));
                }
                return;
            }
            if (nextBoltTick == 0) nextBoltTick = Find.TickManager.TicksGame;
            if (boltsRemaining > 0 && Find.TickManager.TicksGame >= nextBoltTick)
            {
                StrikeLightning();
                boltsRemaining--;
                nextBoltTick = Find.TickManager.TicksGame + 18;
            }
            if (boltsRemaining <= 0 && Find.TickManager.TicksGame > nextBoltTick + 12) Destroy(DestroyMode.Vanish);
        }

        private IEnumerable<Thing> HostileTargets()
        {
            if (caster == null) return Enumerable.Empty<Thing>();
            return GenRadial.RadialDistinctThingsAround(Position, Map, 10f, true)
                .Where(t => t != this && t.Spawned && t.HostileTo(caster) && (t is Pawn || t.def.category == ThingCategory.Building));
        }

        private void PullEnemies()
        {
            foreach (Pawn pawn in HostileTargets().OfType<Pawn>().Where(p => !p.Dead && !p.Downed).ToList())
            {
                IntVec3 next = GenAdj.CardinalDirections
                    .Select(d => pawn.Position + d)
                    .Where(c => c.InBounds(Map) && c.Walkable(Map))
                    .OrderBy(c => c.DistanceToSquared(Position)).FirstOrDefault();
                if (next.IsValid && next.DistanceToSquared(Position) < pawn.Position.DistanceToSquared(Position))
                {
                    pawn.pather.StopDead();
                    pawn.Position = next;
                    pawn.Notify_Teleported(false, true);
                }
            }
        }

        private void StrikeLightning()
        {
            List<Thing> targets = HostileTargets().OrderBy(_ => Rand.Value).ToList();
            IntVec3 cell = targets.Count > 0 ? targets[boltsRemaining % targets.Count].Position : Position;
            Map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(Map, cell));
            FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(), Map, 2.2f + rank * .18f);
            Thing direct = targets.FirstOrDefault(t => t.Position == cell);
            if (direct != null)
            {
                direct.TakeDamage(new DamageInfo(DamageDefOf.Flame, 30f + rank * 12f, .42f, -1f, caster));
                Pawn pawn = direct as Pawn;
                if (pawn != null) pawn.stances.stunner.StunFor(60 + rank * 30, caster);
            }
            GenExplosion.DoExplosion(cell, Map, 2.2f + rank * .18f, DamageDefOf.EMP, caster, 12 + rank * 5, .25f);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float pulse = 9.2f + Mathf.Sin(Find.TickManager.TicksGame / 12f) * .35f;
            Vector3 loc = drawLoc;
            loc.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.Euler(0f, ageTicks * .35f, 0f), new Vector3(pulse, 1f, pulse));
            Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "yjz_thunderWindCaster");
            Scribe_Values.Look(ref rank, "yjz_thunderWindRank", 1);
            Scribe_Values.Look(ref ageTicks, "yjz_thunderWindAge", 0);
            Scribe_Values.Look(ref pullTicks, "yjz_thunderWindPullTicks", 600);
            Scribe_Values.Look(ref boltsRemaining, "yjz_thunderWindBolts", 1);
            Scribe_Values.Look(ref nextBoltTick, "yjz_thunderWindNextBolt", 0);
        }
    }

    [StaticConstructorOnStartup]
    public sealed class ThingYangfuBeanSoldier : ThingWithComps
    {
        private Pawn caster;
        private int rank = 1;
        private int ageTicks;
        private Thing target;
        private bool detonated;

        public void Initialize(Pawn source, int castRank)
        {
            caster = source;
            rank = Mathf.Clamp(castRank, 1, 5);
        }

        protected override void Tick()
        {
            base.Tick();
            ageTicks++;
            if (caster == null || caster.Dead || ageTicks > 3600) { Destroy(DestroyMode.Vanish); return; }
            if (target == null || !target.Spawned || !target.HostileTo(caster) || (Find.TickManager.TicksGame + thingIDNumber) % 60 == 0) FindTarget();
            if (target == null) return;
            if (Position.DistanceTo(target.Position) <= 1.45f) { Detonate(); return; }
            if ((Find.TickManager.TicksGame + thingIDNumber) % 5 == 0) MoveTowardTarget();
        }

        private void FindTarget()
        {
            target = GenRadial.RadialDistinctThingsAround(Position, Map, 35f, true)
                .Where(t => t != this && t.Spawned && t.HostileTo(caster) && (t is Pawn || t.def.category == ThingCategory.Building))
                .OrderBy(t => t.Position.DistanceToSquared(Position)).FirstOrDefault();
        }

        private void MoveTowardTarget()
        {
            if (target == null) return;
            IntVec3 next = GenAdj.CardinalDirections
                .Select(d => Position + d)
                .Where(c => c.InBounds(Map) && c.Walkable(Map))
                .OrderBy(c => c.DistanceToSquared(target.Position)).FirstOrDefault();
            if (next.IsValid && next.DistanceToSquared(target.Position) < Position.DistanceToSquared(target.Position)) Position = next;
        }

        private void Detonate()
        {
            if (detonated || !Spawned) return;
            detonated = true;
            Map map = Map;
            IntVec3 cell = Position;
            Pawn source = caster;
            int damage = Mathf.RoundToInt(50f * (1f + .5f * (rank - 1)));
            base.Destroy(DestroyMode.Vanish);
            GenExplosion.DoExplosion(cell, map, 2.9f, DamageDefOf.Bomb, source, damage, .35f);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (Spawned && mode == DestroyMode.KillFinalize && !detonated) { Detonate(); return; }
            base.Destroy(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "yjz_beanCaster");
            Scribe_References.Look(ref target, "yjz_beanTarget");
            Scribe_Values.Look(ref rank, "yjz_beanRank", 1);
            Scribe_Values.Look(ref ageTicks, "yjz_beanAge", 0);
            Scribe_Values.Look(ref detonated, "yjz_beanDetonated", false);
        }
    }

    internal static class YangfuCombatUtility
    {
        private static readonly int[] Counts = { 0, 2, 5, 8, 11, 15 };

        public static void SpawnBeanSoldiers(Pawn caster, IntVec3 center, int rank)
        {
            if (caster == null || caster.Map == null) return;
            ThingDef def = new[] { null, YJZDefOf.YJZ_BeanSoldier_1, YJZDefOf.YJZ_BeanSoldier_2,
                YJZDefOf.YJZ_BeanSoldier_3, YJZDefOf.YJZ_BeanSoldier_4, YJZDefOf.YJZ_BeanSoldier_5 }[rank];
            List<IntVec3> cells = GenRadial.RadialCellsAround(center, 5f, true)
                .Where(c => c.InBounds(caster.Map) && c.Walkable(caster.Map)).InRandomOrder().ToList();
            for (int i = 0; i < Counts[rank] && cells.Count > 0; i++)
            {
                IntVec3 cell = cells[i % cells.Count];
                ThingYangfuBeanSoldier soldier = (ThingYangfuBeanSoldier)GenSpawn.Spawn(def, cell, caster.Map);
                soldier.Initialize(caster, rank);
                FleckMaker.ThrowMicroSparks(cell.ToVector3Shifted(), caster.Map);
            }
        }
    }
}
