using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    [StaticConstructorOnStartup]
    public sealed class ThingYunjianziThunderSpear : Thing
    {
        // UF's Lorentz Spear shell: 1200 Bomb damage, 10 AP, 3-tile sweep,
        // 5-tile blast. Keep that damage baseline, but widen our landing to 7.5 tiles.
        // Realm IV reaches that per-hit baseline, V adds 25%.
        internal const float SweepRadius = 3f;
        internal const float BlastRadius = 7.5f;
        internal const float ArmorPenetration = 10f;
        internal static float DamageAtRealm(int level) => 300f * Mathf.Clamp(level, 1, 5);

        private static readonly Material SpearMaterial = MaterialPool.MatFrom(
            "Yunjianzi/Effects/ThunderSpear/ThunderSpear", ShaderDatabase.MoteGlow);

        private Pawn caster;
        private Vector3 origin;
        private IntVec3 destination;
        private int realm;
        private int age;
        private int flightTicks = 30;
        private HashSet<int> struckThingIds = new HashSet<int>();

        public void Initialize(Pawn source, IntVec3 target, int sourceRealm)
        {
            caster = source;
            origin = source.DrawPos;
            destination = target;
            realm = Mathf.Clamp(sourceRealm, 1, 5);
            flightTicks = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(origin, target.ToVector3Shifted()) * 1.35f), 14, 54);
        }

        protected override void Tick()
        {
            base.Tick();
            if (Map == null || caster == null || caster.Dead || !destination.InBounds(Map))
            {
                if (!Destroyed) Destroy(DestroyMode.Vanish);
                return;
            }
            Vector3 previousPosition = Vector3.Lerp(origin, destination.ToVector3Shifted(),
                Mathf.Clamp01(age / (float)flightTicks));
            age++;
            Vector3 exact = Vector3.Lerp(origin, destination.ToVector3Shifted(), Mathf.Clamp01(age / (float)flightTicks));
            if (age % 8 == 0)
            {
                Vector3 previous = Vector3.Lerp(origin, destination.ToVector3Shifted(),
                    Mathf.Clamp01((age - 6f) / flightTicks));
                YunjianziPurpleLightning.Arc(previous, exact, Map, .75f);
            }
            IntVec3 cell = exact.ToIntVec3();
            if (cell.InBounds(Map) && Position != cell) Position = cell;
            DamageAlongPath(previousPosition, exact);
            if (age >= flightTicks) Impact();
        }

        private void DamageAlongPath(Vector3 start, Vector3 end)
        {
            int sparkBursts = 0;
            foreach (Thing thing in TargetsInSweep(start, end, SweepRadius))
            {
                if (!CanDamage(thing) || !struckThingIds.Add(thing.thingIDNumber)) continue;
                Vector3 hit = thing.DrawPos;
                ApplySpearDamage(thing);
                Vector3 incoming = origin - destination.ToVector3Shifted();
                incoming.y = 0f;
                if (incoming.sqrMagnitude < .001f) incoming = Vector3.forward;
                incoming.Normalize();
                YunjianziPurpleLightning.Arc(hit + incoming * .75f, hit, Map, .72f);
                YunjianziPurpleLightning.Burst(hit, Map, .92f);
                YunjianziPurpleLightning.Glow(hit, Map, 1.25f);
                if (sparkBursts++ < 3) FleckMaker.ThrowMicroSparks(hit, Map);
            }
        }

        private void Impact()
        {
            IntVec3 cell = destination;
            float radius = BlastRadius;
            Vector3 center = cell.ToVector3Shifted();
            foreach (Thing thing in TargetsInSweep(center, center, radius))
            {
                if (CanDamage(thing)) ApplySpearDamage(thing);
            }
            YunjianziPurpleLightning.ImpactWave(cell.ToVector3Shifted(), Map, radius, 1.45f);
            for (int i = 0; i < 3; i++)
            {
                float angle = (thingIDNumber * 47f + i * 120f) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius * .28f;
                FleckMaker.ThrowMicroSparks(cell.ToVector3Shifted() + offset, Map);
            }
            YunjianziCombatEffects.ShowLightningAt(cell, Map, caster, visualStrength: 1.65f);
            CompYunjianziCultivation cultivation = caster.TryGetComp<CompYunjianziCultivation>();
            if (cultivation != null) cultivation.NotifyThunderSpearImpact(cell, destination.x >= origin.x);
            Destroy(DestroyMode.Vanish);
        }

        private bool CanDamage(Thing thing)
        {
            return thing != null && !thing.Destroyed && thing.Spawned && thing.Map == Map &&
                thing.HostileTo(caster) && ((thing is Pawn pawn && !pawn.Dead) || thing is Building);
        }

        private List<Thing> TargetsInSweep(Vector3 start, Vector3 end, float radius)
        {
            // Snapshot before damage destroys/despawns things. Check every occupied
            // cell so large buildings can be hit at their edge; deduplicate their IDs.
            List<Thing> targets = new List<Thing>();
            HashSet<int> seen = new HashSet<int>();
            foreach (IntVec3 cell in SweptCells(start, end, radius, Map.Size.x, Map.Size.z))
                foreach (Thing thing in cell.GetThingList(Map))
                    if (CanDamage(thing) && seen.Add(thing.thingIDNumber)) targets.Add(thing);
            return targets;
        }

        internal static IEnumerable<IntVec3> SweptCells(Vector3 start, Vector3 end,
            float radius, int mapWidth, int mapHeight)
        {
            int minX = Math.Max(0, (int)Math.Floor(Math.Min(start.x, end.x) - radius));
            int maxX = Math.Min(mapWidth - 1, (int)Math.Floor(Math.Max(start.x, end.x) + radius));
            int minZ = Math.Max(0, (int)Math.Floor(Math.Min(start.z, end.z) - radius));
            int maxZ = Math.Min(mapHeight - 1, (int)Math.Floor(Math.Max(start.z, end.z) + radius));
            float dx = end.x - start.x, dz = end.z - start.z;
            float lengthSquared = dx * dx + dz * dz;
            for (int x = minX; x <= maxX; x++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    float px = x + .5f - start.x, pz = z + .5f - start.z;
                    float t = lengthSquared > .000001f
                        ? Mathf.Clamp01((px * dx + pz * dz) / lengthSquared) : 0f;
                    float ex = px - t * dx, ez = pz - t * dz;
                    if (ex * ex + ez * ez <= radius * radius + .00001f)
                        yield return new IntVec3(x, 0, z);
                }
        }

        private void ApplySpearDamage(Thing thing)
        {
            YunjianziEmp.Apply(thing, caster, YunjianziEmp.SpearDamage);
            if (thing.Destroyed || !thing.Spawned || (thing is Pawn pawn && pawn.Dead)) return;
            // A separate Bomb-based Def retains heavy-shell injury propagation and
            // building multipliers, without altering ordinary Shenxiao lightning.
            thing.TakeDamage(new DamageInfo(YJZDefOf.YJZ_ThunderSpearBlast, DamageAtRealm(realm),
                ArmorPenetration, -1f, caster, weapon: def, intendedTarget: thing));
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float progress = Mathf.Clamp01(age / (float)Mathf.Max(1, flightTicks));
            Vector3 loc = Vector3.Lerp(origin, destination.ToVector3Shifted(), progress);
            loc.y = Altitudes.AltitudeFor(AltitudeLayer.Projectile);
            Vector3 delta = destination.ToVector3Shifted() - origin;
            float angle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            float pulse = 1f + Mathf.Sin((Find.TickManager.TicksGame + thingIDNumber) * .45f) * .06f;
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.AngleAxis(angle, Vector3.up),
                new Vector3(2.4f * pulse, 1f, 4.8f * pulse));
            Graphics.DrawMesh(MeshPool.plane10, matrix, SpearMaterial, 0, null, 0, ThunderSpearGlow.Properties);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "yjz_thunderSpearCaster");
            Scribe_Values.Look(ref origin, "yjz_thunderSpearOrigin");
            Scribe_Values.Look(ref destination, "yjz_thunderSpearDestination");
            Scribe_Values.Look(ref realm, "yjz_thunderSpearRealm", 4);
            Scribe_Values.Look(ref age, "yjz_thunderSpearAge", 0);
            Scribe_Values.Look(ref flightTicks, "yjz_thunderSpearFlightTicks", 30);
            Scribe_Collections.Look(ref struckThingIds, "yjz_thunderSpearStruckIds", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && struckThingIds == null)
                struckThingIds = new HashSet<int>();
            if (Scribe.mode == LoadSaveMode.PostLoadInit) flightTicks = Mathf.Max(1, flightTicks);
        }
    }

}
