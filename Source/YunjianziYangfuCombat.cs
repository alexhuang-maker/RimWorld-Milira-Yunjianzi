using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

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
            // One full second of still, readable talisman before it bursts.
            if (Find.TickManager.TicksGame - spawnTick < 75) return;
            Map map = Map;
            IntVec3 cell = Position;
            Pawn source = caster;
            YangfuCombatSpell castSpell = spell;
            int castRank = rank;
            if (castSpell == YangfuCombatSpell.ThunderWind)
                YunjianziPurpleLightning.Glow(DrawPos, map, 2.1f);
            else FleckMaker.ThrowLightningGlow(DrawPos, map, 2.1f);
            for (int i = 0; i < 8; i++)
                FleckMaker.ThrowMicroSparks(DrawPos + Gen.RandomHorizontalVector(.15f + i * .035f), map);
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
            int ticks = Find.TickManager.TicksGame - spawnTick;
            float age = Mathf.Clamp01(ticks / 60f);
            float pulse = 1f + Mathf.Sin(age * Mathf.PI) * .10f;
            Vector3 loc = drawLoc;
            loc.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            if (ticks < 60)
            {
                // Upright and stationary: no flat-plane rotation.
                Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.identity, new Vector3(pulse, 1f, pulse));
                Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0);
                return;
            }
            // Fifteen-tick outward shatter. Reusing the emissive glyph keeps
            // each spell's red/purple/green identity without opaque paper.
            float burst = Mathf.Clamp01((ticks - 60) / 15f);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f * Mathf.Deg2Rad;
                Vector3 shardLoc = loc + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (burst * 1.25f);
                float shardScale = Mathf.Lerp(.34f, .04f, burst);
                Matrix4x4 shard = Matrix4x4.TRS(shardLoc, Quaternion.identity, new Vector3(shardScale * .34f, 1f, shardScale));
                Graphics.DrawMesh(MeshPool.plane10, shard, Graphic.MatSingle, 0);
            }
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
                float visualScale = new[] { 0f, 1.25f, 1.55f, 1.9f, 2.35f, 2.8f }[rank];
                FleckMaker.ThrowFireGlow(new Vector3(realPosition.x, 0f, realPosition.y), Map,
                    Mathf.Lerp(visualScale, visualScale * .32f, ageTicks / (float)durationTicks));
                FleckMaker.ThrowSmoke(new Vector3(realPosition.x, 0f, realPosition.y), Map, .45f + rank * .16f);
            }
        }

        private void DamageAndBurn()
        {
            if (caster == null) return;
            float radius = new[] { 0f, 2.4f, 3.0f, 3.7f, 4.5f, 5.4f }[rank];
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(Position, Map, radius, true)
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
            float[] startSizes = { 0f, 3.0f, 4.0f, 5.2f, 6.6f, 8.2f };
            float[] endSizes = { 0f, .9f, 1.2f, 1.5f, 1.8f, 2.2f };
            float size = Mathf.Lerp(startSizes[rank], endSizes[rank], life);
            Vector3 loc = new Vector3(realPosition.x, AltitudeLayer.Weather.AltitudeFor(), realPosition.y);
            // The texture is a front-readable tornado sprite. Rotating the
            // whole ground plane makes it look like a spinning card, so keep
            // its world orientation fixed and express motion through size,
            // translation, glow and smoke instead.
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.identity, new Vector3(size, 1f, size));
            Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0);
            // 紫府以上叠加固定朝向的内层风壁；化神的视觉尺度接近大型灵能火龙卷，
            // 但实际伤害半径单独受控，避免仅因特效放大而失衡。
            if (rank >= 3)
            {
                float inner = size * (rank >= 5 ? .72f : .62f);
                Vector3 innerLoc = loc + new Vector3(0f, .006f, 0f);
                Matrix4x4 innerMatrix = Matrix4x4.TRS(innerLoc, Quaternion.identity, new Vector3(inner, 1f, inner));
                Graphics.DrawMesh(MeshPool.plane10, innerMatrix, Graphic.MatSingle, 0);
            }
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
                if (ageTicks > pullTicks - 120 && ageTicks % 24 == 0)
                {
                    float angle = ageTicks * .17f + thingIDNumber;
                    Vector3 rim = DrawPos + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2.2f;
                    YunjianziPurpleLightning.Arc(rim, DrawPos, Map, .65f);
                }
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
            Thing direct = targets.FirstOrDefault(t => t.Position == cell);
            if (direct != null)
            {
                Pawn pawn = direct as Pawn;
                if (pawn != null) pawn.stances.stunner.StunFor(60 + rank * 30, caster);
            }
            YunjianziPurpleLightning.Strike(cell, Map, 1f + rank * .1f, caster,
                30f + rank * 12f, .42f,
                empDamage: 12 + rank * 5, empRadius: 2.2f + rank * .18f);
            YunjianziCombatEffects.ChargeSwordWardFromLightning(caster);
            ThunderBeastUtility.TrySummon(caster, cell, Map);
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

    public sealed class CompProperties_YangfuBeanSoldier : CompProperties
    {
        public int defaultRank = 1;
        public int swordShadowCount;

        public CompProperties_YangfuBeanSoldier() { compClass = typeof(CompYangfuBeanSoldier); }
    }

    public sealed class CompYangfuBeanSoldier : ThingComp
    {
        private Pawn caster;
        private int rank = 1;
        private int ageTicks;
        private Thing target;
        private bool detonated;

        private Pawn Pawn { get { return parent as Pawn; } }

        private CompProperties_YangfuBeanSoldier Props
        {
            get { return (CompProperties_YangfuBeanSoldier)props; }
        }

        public int SwordShadowCount { get { return Props.swordShadowCount; } }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Pawn pawn = Pawn;
            if (caster == null) caster = pawn;
            if (!respawningAfterLoad) rank = Mathf.Clamp(Props.defaultRank, 1, 5);
            EnsureSwordShadows();
        }

        public void Initialize(Pawn source, int castRank)
        {
            caster = source;
            rank = Mathf.Clamp(castRank, 1, 5);
        }

        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = Pawn;
            if (pawn == null || pawn.Dead || !pawn.Spawned) return;
            ageTicks++;
            if (SwordShadowCount > 0 && pawn.IsHashIntervalTick(30)) EnsureSwordShadows();
            // RimWorld 一小时为 2500 tick；符兵严格存在两小时后消散。
            if (caster == null || caster.Dead || ageTicks >= 5000)
            {
                detonated = true;
                pawn.Destroy(DestroyMode.Vanish);
                return;
            }
            if (target == null || !target.Spawned || !target.HostileTo(caster) || (Find.TickManager.TicksGame + pawn.thingIDNumber) % 60 == 0) FindTarget();
            if (target == null) return;
            if (pawn.Position.DistanceTo(target.Position) <= 1.45f) { Detonate(); return; }
            if ((Find.TickManager.TicksGame + pawn.thingIDNumber) % 30 == 0) MoveTowardTarget();
        }

        private void FindTarget()
        {
            Pawn pawn = Pawn;
            if (pawn == null || !pawn.Spawned) { target = null; return; }
            target = GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, 35f, true)
                .Where(t => t != Pawn && t.Spawned && t.HostileTo(caster) && (t is Pawn || t.def.category == ThingCategory.Building))
                .OrderBy(t => t.Position.DistanceToSquared(pawn.Position)).FirstOrDefault();
        }

        private void MoveTowardTarget()
        {
            if (target == null) return;
            Pawn pawn = Pawn;
            if (pawn == null || pawn.pather == null || target == null) return;
            if (!pawn.pather.Moving || pawn.pather.Destination.Thing != target)
                pawn.pather.StartPath(target, PathEndMode.Touch);
        }

        private void Detonate()
        {
            Pawn pawn = Pawn;
            if (detonated || pawn == null || !pawn.Spawned) return;
            detonated = true;
            Map map = pawn.Map;
            IntVec3 cell = pawn.Position;
            Pawn source = caster;
            int damage = Mathf.RoundToInt(50f * (1f + .5f * (rank - 1)));
            pawn.Destroy(DestroyMode.Vanish);
            GenExplosion.DoExplosion(cell, map, 2.9f, DamageDefOf.Bomb, source, damage, .35f);
        }

        private void EnsureSwordShadows()
        {
            Pawn pawn = Pawn;
            if (SwordShadowCount <= 0 || pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Dead) return;
            Thing initialTarget = GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, 25.9f, true)
                .Where(t => t != pawn && t.Spawned && t.HostileTo(pawn) &&
                    ((t is Pawn targetPawn && !targetPawn.Dead) || t is Building_Turret))
                .OrderBy(t => t.Position.DistanceToSquared(pawn.Position))
                .FirstOrDefault();
            if (initialTarget == null) return;

            for (int index = 0; index < SwordShadowCount; index++)
            {
                bool exists = pawn.Map.listerThings.ThingsOfDef(YJZDefOf.YJZ_Projectile_SwordShadow)
                    .OfType<ThingYunjianziSwordShadow>()
                    .Any(shadow => shadow.Owner == pawn && shadow.ShadowIndex == index);
                if (exists) continue;
                ThingYunjianziSwordShadow shadow = (ThingYunjianziSwordShadow)ThingMaker.MakeThing(YJZDefOf.YJZ_Projectile_SwordShadow);
                shadow.Initialize(pawn, index, initialTarget);
                GenSpawn.Spawn(shadow, pawn.Position, pawn.Map);
            }
        }

        public void OnSwordShadowHit(Thing hitThing)
        {
            Pawn pawn = Pawn;
            if (hitThing == null || hitThing.Destroyed || pawn == null || pawn.Dead) return;
            float damage = 16f + 7f * rank;
            float armorPenetration = .34f + .06f * rank;
            hitThing.TakeDamage(new DamageInfo(DamageDefOf.Cut, damage, armorPenetration, -1f,
                pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown));
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (detonated || previousMap == null || mode != DestroyMode.KillFinalize) return;
            detonated = true;
            int damage = Mathf.RoundToInt(50f * (1f + .5f * (rank - 1)));
            GenExplosion.DoExplosion(parent.Position, previousMap, 2.9f, DamageDefOf.Bomb, caster, damage, .35f);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
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
            PawnKindDef kind = new[] { null, YJZDefOf.YJZ_BeanSoldier_1, YJZDefOf.YJZ_BeanSoldier_2,
                YJZDefOf.YJZ_BeanSoldier_3, YJZDefOf.YJZ_BeanSoldier_4, YJZDefOf.YJZ_BeanSoldier_5 }[rank];
            List<IntVec3> cells = GenRadial.RadialCellsAround(center, 5f, true)
                .Where(c => c.InBounds(caster.Map) && c.Walkable(caster.Map)).InRandomOrder().ToList();
            for (int i = 0; i < Counts[rank] && cells.Count > 0; i++)
            {
                IntVec3 cell = cells[i % cells.Count];
                Pawn soldier = PawnGenerator.GeneratePawn(kind, caster.Faction ?? Faction.OfPlayer);
                // 符兵是灵力凝成的临时构装体，不参与任何生物性别逻辑。
                // PawnKindDef 继承 AnimalKindBase 时仍可能随机分配性别，因此
                // 在生成完成、入图之前统一锁为 None，并由存档自然持久化。
                soldier.gender = Gender.None;
                GenSpawn.Spawn(soldier, cell, caster.Map);
                CompYangfuBeanSoldier comp = soldier.TryGetComp<CompYangfuBeanSoldier>();
                if (comp != null) comp.Initialize(caster, rank);
                FleckMaker.ThrowMicroSparks(cell.ToVector3Shifted(), caster.Map);
            }
        }
    }
}
