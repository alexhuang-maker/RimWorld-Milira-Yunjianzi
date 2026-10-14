using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Yunjianzi
{
    internal enum PurpleLightningStyle { Lance, Fork, Crown, Coil }

    // A shuffled bag guarantees variety, without consuming the game's combat RNG.
    // Advance only for admitted bolts, not decorative arcs or merged multihits.
    internal sealed class PurpleLightningStyleCycle
    {
        private readonly PurpleLightningStyle[] bag = {
            PurpleLightningStyle.Lance, PurpleLightningStyle.Fork,
            PurpleLightningStyle.Crown, PurpleLightningStyle.Coil };
        private uint state = 0x91E10DA5u;
        private int cursor = 4;
        private PurpleLightningStyle last = (PurpleLightningStyle)(-1);

        internal PurpleLightningStyle Next()
        {
            if (cursor == bag.Length)
            {
                for (int i = bag.Length - 1; i > 0; i--)
                {
                    state ^= state << 13;
                    state ^= state >> 17;
                    state ^= state << 5;
                    int j = (int)(state % (uint)(i + 1));
                    PurpleLightningStyle swap = bag[i]; bag[i] = bag[j]; bag[j] = swap;
                }
                if (bag[0] == last)
                {
                    PurpleLightningStyle swap = bag[0]; bag[0] = bag[3]; bag[3] = swap;
                }
                cursor = 0;
            }
            return last = bag[cursor++];
        }
    }

    // Visual budgets use elapsed seconds, never game ticks: 3x/4x speed must not
    // multiply brightness or sound density. None of these limits gate gameplay.
    internal sealed class PurpleLightningBudget
    {
        internal const int MaxBolts = 8;
        internal const int MaxChains = 12;
        internal const int MaxGlows = 24;
        internal const int MaxEmpPulses = 4;
        internal const int MaxParticleExplosions = 3;
        internal const float SoundInterval = .35f;
        private float nextSound = -1f;
        private float windowStart = -1f;
        private int boltsInWindow;

        internal bool AllowBolt(float now, int active)
        {
            if (now - windowStart >= .12f || now < windowStart)
            {
                windowStart = now;
                boltsInWindow = 0;
            }
            if (active >= MaxBolts || boltsInWindow >= 6) return false;
            boltsInWindow++;
            return true;
        }

        internal bool AllowSound(float now)
        {
            if (now < nextSound) return false;
            nextSound = now + SoundInterval;
            return true;
        }
    }

    internal static class YunjianziPurpleLightning
    {
        // Gameplay AOE is independent of visual admission and density budgets.
        internal static void Strike(IntVec3 cell, Map map, float strength = 1f,
            Pawn caster = null, float damage = -1f, float armorPenetration = .75f,
            float radius = YunjianziLightningDamage.MinimumRadius,
            float empDamage = YunjianziEmp.LightningDamage, float empRadius = -1f)
        {
            if (map == null || !cell.InBounds(map)) return;
            YunjianziLightningDamage.Apply(cell, map, caster, damage, armorPenetration, radius, empDamage, empRadius);
            MapComponent_YunjianziPurpleLightning visuals = Visuals(map, cell);
            visuals?.AddBolt(cell.ToVector3Shifted(), strength);
            visuals?.AddEmpPulse(cell.ToVector3Shifted(), Mathf.Max(YunjianziLightningDamage.MinimumRadius, Mathf.Max(radius, empRadius)));
        }

        internal static void Glow(Vector3 position, Map map, float size)
        {
            Visuals(map, position.ToIntVec3())?.AddGlow(position, size);
        }

        internal static void Chain(List<Vector3> points, Map map, float strength = 1f)
        {
            if (points == null || points.Count == 0) return;
            Visuals(map, points[0].ToIntVec3())?.AddChain(points, strength);
        }

        internal static void Arc(Vector3 start, Vector3 end, Map map, float strength = 1f)
        {
            Chain(new List<Vector3> { start, end }, map, strength);
        }

        internal static void Burst(Vector3 position, Map map, float strength = 1f)
        {
            Visuals(map, position.ToIntVec3())?.AddBurst(position, strength);
        }

        internal static void ImpactWave(Vector3 position, Map map, float radius, float strength = 1f)
        {
            Visuals(map, position.ToIntVec3())?.AddImpactWave(position, radius, strength);
        }

        // Visual-only EMP feedback. The caller owns all damage / stun rules.
        internal static void EmpPulse(Vector3 position, Map map, float radius)
        {
            Visuals(map, position.ToIntVec3())?.AddEmpPulse(position, radius);
        }

        private static MapComponent_YunjianziPurpleLightning Visuals(Map map, IntVec3 cell)
        {
            // Hidden maps never accumulate VFX to replay on return. Damage above
            // still runs on every map, even when no visual is admitted.
            return map != null && map == Find.CurrentMap && cell.InBounds(map) && !cell.Fogged(map)
                ? map.GetComponent<MapComponent_YunjianziPurpleLightning>() : null;
        }
    }

    public sealed class MapComponent_YunjianziPurpleLightning : MapComponent
    {
        private sealed class Flash
        {
            internal Vector3[] points;
            internal float born, duration, strength;
            internal int seed;
            internal bool bolt;
            internal bool burst;
            internal float impactRadius;
            internal float empRadius;
            internal bool emp;
            internal bool particleExplosion;
            internal PurpleLightningStyle style;
            internal PurpleLightningRibbon ribbon;
        }

        private sealed class Glow
        {
            internal Vector3 position;
            internal float born, size;
        }

        private readonly List<Flash> flashes = new List<Flash>();
        private readonly List<Glow> glows = new List<Glow>();
        private readonly Stack<PurpleLightningRibbon> ribbonPool = new Stack<PurpleLightningRibbon>();
        private readonly PurpleLightningBudget budget = new PurpleLightningBudget();
        private readonly PurpleLightningStyleCycle styles = new PurpleLightningStyleCycle();
        private float visualTime;
        private int serial;
        private int boltCount;
        private int chainCount;
        private int empCount;
        private int explosionCount;
        private const float GlowDuration = .32f;

        public MapComponent_YunjianziPurpleLightning(Map map) : base(map) { }

        internal void AddBolt(Vector3 position, float strength)
        {
            strength = Mathf.Clamp(strength, .6f, 1.65f);
            AddGlow(position, 1.05f + strength * .7f);
            for (int i = 0; i < flashes.Count; i++)
            {
                Flash existing = flashes[i];
                if (existing.bolt && visualTime - existing.born < .12f &&
                    (existing.points[0] - position).sqrMagnitude < 1.44f)
                {
                    // Same-cell multihits brighten one bolt, without restarting
                    // its envelope or adding multiple screen-height white cores.
                    existing.strength = Mathf.Max(existing.strength, strength);
                    return;
                }
            }
            if (!budget.AllowBolt(visualTime, boltCount)) return;
            int seed = ++serial;
            Vector3 top = position + new Vector3(Noise(seed, 0) * 3f, 0f, 16f + strength * 5f);
            AddFlash(new[] { position, top }, strength, true, .30f + strength * .065f, seed);
            flashes[flashes.Count - 1].style = styles.Next();
            flashes[flashes.Count - 1].empRadius = 1.1f + strength * .45f;
            if (budget.AllowSound(Time.realtimeSinceStartup))
            {
                SoundInfo info = SoundInfo.InMap(new TargetInfo(position.ToIntVec3(), map));
                info.volumeFactor = .48f;
                SoundDefOf.Thunder_OnMap.PlayOneShot(info);
            }
        }

        internal void AddChain(List<Vector3> points, float strength)
        {
            if (chainCount >= PurpleLightningBudget.MaxChains) return;
            // Snapshot impact positions before dead/despawned targets disappear.
            // The arc does not rescan targets, deal damage, or summon anything.
            int count = Math.Min(points.Count, 5);
            if (count == 0) return;
            Vector3[] snapshot = new Vector3[count];
            for (int i = 0; i < count; i++) snapshot[i] = points[i];
            AddGlow(snapshot[0], .75f);
            if (count == 1)
            {
                snapshot = new[] { snapshot[0] + new Vector3(-.3f, 0f, .25f), snapshot[0],
                    snapshot[0] + new Vector3(.25f, 0f, -.2f) };
            }
            AddFlash(snapshot, Mathf.Clamp(strength, .45f, 1.65f), false, .38f, ++serial);
        }

        private void AddFlash(Vector3[] points, float strength, bool bolt, float duration, int seed)
        {
            flashes.Add(new Flash
            {
                points = points, strength = strength, bolt = bolt, duration = duration,
                born = visualTime, seed = seed,
                ribbon = ribbonPool.Count > 0 ? ribbonPool.Pop() : new PurpleLightningRibbon()
            });
            if (bolt) boltCount++; else chainCount++;
        }

        internal void AddBurst(Vector3 position, float strength)
        {
            AddGlow(position, strength);
            if (chainCount >= PurpleLightningBudget.MaxChains) return;
            AddFlash(new[] { position }, Mathf.Clamp(strength, .5f, 1.65f), false, .32f, ++serial);
            flashes[flashes.Count - 1].burst = true;
        }

        internal void AddImpactWave(Vector3 position, float radius, float strength)
        {
            radius = Mathf.Clamp(radius, .8f, 8f);
            for (int i = 0; i < flashes.Count; i++)
            {
                Flash existing = flashes[i];
                if (existing.particleExplosion && visualTime - existing.born < .12f &&
                    (existing.points[0] - position).sqrMagnitude < 1.44f)
                {
                    existing.impactRadius = Mathf.Max(existing.impactRadius, radius);
                    return; // Never refresh the flash envelope on stacked impacts.
                }
            }
            // Landing must stay readable even when the spear filled all chain
            // slots on its approach. This capacity admits only transient VFX.
            if (explosionCount >= PurpleLightningBudget.MaxParticleExplosions) return;
            AddGlow(position, Mathf.Clamp(1.15f + strength * .55f, .8f, 2.4f));
            AddFlash(new[] { position }, Mathf.Clamp(strength, .65f, 1.65f), false,
                PurpleLightningRibbon.ParticleExplosionDuration, ++serial);
            Flash wave = flashes[flashes.Count - 1];
            wave.particleExplosion = true;
            wave.impactRadius = radius;
            chainCount--;
            explosionCount++;
        }

        internal void AddGlow(Vector3 position, float size)
        {
            size = Mathf.Clamp(size, .35f, 2.4f);
            for (int i = 0; i < glows.Count; i++)
            {
                Glow existing = glows[i];
                if ((existing.position - position).sqrMagnitude < 1.44f)
                {
                    existing.size = Mathf.Max(existing.size, size);
                    return; // No lifetime refresh: repeated hits cannot sustain a white patch.
                }
            }
            if (glows.Count < PurpleLightningBudget.MaxGlows)
                glows.Add(new Glow { position = position, size = size, born = visualTime });
        }

        internal void AddEmpPulse(Vector3 position, float radius)
        {
            radius = Mathf.Clamp(radius, .35f, 6f);
            for (int i = 0; i < flashes.Count; i++)
            {
                Flash existing = flashes[i];
                if ((existing.bolt || existing.emp) && visualTime - existing.born < .12f &&
                    (existing.points[0] - position).sqrMagnitude < .36f)
                {
                    existing.empRadius = Mathf.Max(existing.empRadius, radius);
                    return; // Merge with the real strike, without restarting its flash.
                }
            }
            // Reserve a small independent budget so sword-chain saturation cannot
            // hide an actual EMP ability's pulse. Never evict damage-bearing objects.
            if (empCount >= PurpleLightningBudget.MaxEmpPulses) return;
            AddFlash(new[] { position }, 1f, false, .48f, ++serial);
            Flash pulse = flashes[flashes.Count - 1];
            pulse.emp = true;
            pulse.empRadius = radius;
            chainCount--; // Dedicated visual capacity, separate from chain/burst slots.
            empCount++;
        }

        public override void MapComponentUpdate()
        {
            if (map != Find.CurrentMap) { Clear(); return; }
            // Use real elapsed time for readable effects at high game speed,
            // but freeze both geometry and lifetime while the player is paused.
            if (Find.TickManager.Paused) return;
            visualTime += Mathf.Min(Time.unscaledDeltaTime, .1f);
            for (int i = flashes.Count - 1; i >= 0; i--)
            {
                if (visualTime - flashes[i].born < flashes[i].duration) continue;
                if (flashes[i].bolt) boltCount--;
                else if (flashes[i].emp) empCount--;
                else if (flashes[i].particleExplosion) explosionCount--;
                else chainCount--;
                ribbonPool.Push(flashes[i].ribbon);
                flashes.RemoveAt(i);
            }
            for (int i = glows.Count - 1; i >= 0; i--)
                if (visualTime - glows[i].born >= GlowDuration) glows.RemoveAt(i);
        }

        public override void MapComponentDraw()
        {
            if (map != Find.CurrentMap) return;
            float density = 1f / Mathf.Sqrt(Mathf.Max(1f,
                boltCount * .55f + chainCount * .15f + empCount * .45f + explosionCount * .8f));
            for (int i = 0; i < flashes.Count; i++)
            {
                Flash flash = flashes[i];
                float age = visualTime - flash.born;
                float progress = Mathf.Clamp01(age / flash.duration);
                float alpha = (1f - progress) * density;
                flash.ribbon.Begin();
                if (flash.particleExplosion)
                {
                    flash.ribbon.ParticleExplosion(flash.points[0], flash.impactRadius,
                        flash.strength, flash.seed, age);
                    alpha *= .78f;
                }
                else if (flash.emp)
                {
                    flash.ribbon.EmpPulse(flash.points[0], flash.empRadius, flash.seed, progress);
                    alpha *= .7f;
                }
                else if (flash.burst)
                {
                    flash.ribbon.Impact(flash.points[0], flash.strength, flash.seed, age, progress,
                        true, flash.impactRadius);
                }
                else if (flash.bolt)
                {
                    flash.ribbon.Bolt(flash.points[0], flash.points[1], flash.strength,
                        flash.seed, age, progress, flash.style);
                    // Split shapes share the existing light budget; more branches
                    // never mean more white-screen brightness or extra impact FX.
                    alpha *= PurpleLightningRibbon.StyleOpacity(flash.style);
                    flash.ribbon.Impact(flash.points[0], flash.strength, flash.seed, age, progress, false);
                    flash.ribbon.EmpPulse(flash.points[0], flash.empRadius, flash.seed, progress);
                }
                else
                {
                    flash.ribbon.EmpPulse(flash.points[0], .5f, flash.seed,
                        Mathf.Clamp01(age / .18f), false);
                    for (int jump = 0; jump < flash.points.Length - 1; jump++)
                    {
                        float pulse = Mathf.Exp(-Mathf.Pow((age - .045f * (jump + 1)) / .045f, 2f));
                        float width = (.28f + .16f * pulse) * flash.strength;
                        flash.ribbon.Arc(flash.points[jump], flash.points[jump + 1], width,
                            flash.seed + jump * 31, age, false);
                        if (pulse > .2f)
                        {
                            flash.ribbon.Impact(flash.points[jump + 1], .4f * flash.strength,
                                flash.seed + jump, age, progress, false);
                            float nodeProgress = Mathf.Clamp01((age - .045f * jump) / .18f);
                            flash.ribbon.EmpPulse(flash.points[jump + 1], .5f,
                                flash.seed + jump, nodeProgress, false);
                            PurpleLightningRibbon.DrawGlow(flash.points[jump + 1], .7f,
                                pulse * alpha * .32f);
                        }
                    }
                }
                flash.ribbon.Draw(alpha);
            }
            float glowDensity = .72f / Mathf.Sqrt(Mathf.Max(1f, glows.Count * .22f));
            for (int i = 0; i < glows.Count; i++)
            {
                Glow glow = glows[i];
                float progress = Mathf.Clamp01((visualTime - glow.born) / GlowDuration);
                PurpleLightningRibbon.DrawGlow(glow.position, glow.size * (1f + .25f * progress),
                    (1f - progress) * glowDensity);
            }
        }

        private void Clear()
        {
            for (int i = 0; i < flashes.Count; i++) ribbonPool.Push(flashes[i].ribbon);
            flashes.Clear();
            glows.Clear();
            boltCount = chainCount = empCount = explosionCount = 0;
        }

        public override void MapRemoved()
        {
            Clear();
            while (ribbonPool.Count > 0) ribbonPool.Pop().Dispose();
        }

        // No gameplay Rand calls: changing VFX quality cannot change hit rolls.
        internal static float Noise(int seed, int index)
        {
            unchecked
            {
                uint n = (uint)(seed * 374761393 + index * 668265263);
                n = (n ^ (n >> 13)) * 1274126177u;
                return ((n ^ (n >> 16)) & 65535) / 32767.5f - 1f;
            }
        }
    }

    // A whole branching flash is one ribbon mesh/draw call. Meshes and buffers
    // are reused, and the soft cross-section is generated once at startup.
    // This keeps a bounded workload even with six array hits plus eight swords.
    [StaticConstructorOnStartup]
    internal sealed class PurpleLightningRibbon
    {
        internal const float ParticleExplosionDuration = 1.44f;
        private static readonly Material RibbonMaterial = CreateRibbonMaterial();
        private static readonly Material GlowMaterial = MaterialPool.MatFrom(
            "Things/Mote/LightningGlow", ShaderDatabase.MoteGlow);
        private static readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();
        private readonly Mesh mesh = new Mesh { name = "YJZ_PurpleLightning" };
        private readonly List<Vector3> vertices = new List<Vector3>(768);
        private readonly List<Vector2> uv = new List<Vector2>(768);
        private readonly List<int> triangles = new List<int>(2304);
        private readonly Vector3[] knots = new Vector3[49];
        private static readonly float Altitude = AltitudeLayer.MoteOverhead.AltitudeFor();

        internal PurpleLightningRibbon() { mesh.MarkDynamic(); }

        private static Material CreateRibbonMaterial()
        {
            Texture2D texture = new Texture2D(64, 1, TextureFormat.RGBA32, false);
            texture.name = "YJZ_PurpleLightningCrossSection";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++)
            {
                float distance = Mathf.Abs(i / 63f * 2f - 1f);
                // A readable white-lavender core and brighter violet rim, without
                // widening geometry or raising the simultaneous-flash budget.
                Color colour = Color.Lerp(new Color(1f, .97f, 1f), new Color(.67f, .34f, 1f),
                    Mathf.Clamp01(distance * 2.4f));
                colour.a = Mathf.Pow(1f - distance, 1.25f);
                pixels[i] = colour;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return MaterialPool.MatFrom(texture, ShaderDatabase.MoteGlow, Color.white);
        }

        internal void Begin() { vertices.Clear(); uv.Clear(); triangles.Clear(); }

        internal static float StyleOpacity(PurpleLightningStyle style)
        {
            switch (style)
            {
                case PurpleLightningStyle.Fork: return .88f;
                case PurpleLightningStyle.Crown: return .82f;
                case PurpleLightningStyle.Coil: return .80f;
                default: return 1f;
            }
        }

        internal void Bolt(Vector3 root, Vector3 top, float strength, int seed,
            float age, float progress, PurpleLightningStyle style)
        {
            Vector3 direction = top - root;
            direction.y = 0f;
            Vector3 side = new Vector3(-direction.z, 0f, direction.x).normalized;
            float width = .38f + strength * .25f;
            switch (style)
            {
                case PurpleLightningStyle.Fork:
                {
                    // Two asymmetric sky channels join a single grounded trunk.
                    Vector3 junction = Vector3.Lerp(root, top, .27f);
                    float exchange = .12f * Mathf.Sin(age * 19f);
                    Arc(root, junction, width, seed, age, true, 1, 0f, .7f);
                    Arc(junction, top + side * 2.5f, width * (.57f + exchange), seed + 11, age, true, 2);
                    Arc(junction, top - side * 2.1f, width * (.57f - exchange), seed + 23, age, true, 2);
                    break;
                }
                case PurpleLightningStyle.Crown:
                {
                    // A branched canopy, with sequentially swelling limbs flowing
                    // into the same impact point. The canopy never creates hits.
                    Vector3 junction = Vector3.Lerp(root, top, .38f);
                    Arc(root, junction, width * .95f, seed, age, true, 2);
                    for (int i = -1; i <= 1; i++)
                    {
                        Vector3 tip = top + side * (i * 3.5f) - direction * (Math.Abs(i) * .12f);
                        float pulse = Mathf.Exp(-Mathf.Pow((progress - (.17f + (i + 1) * .15f)) * 9f, 2f));
                        Arc(junction, tip, width * (.32f + pulse * .13f), seed + (i + 2) * 19,
                            age, true, 3, 0f, 1.3f);
                    }
                    break;
                }
                case PurpleLightningStyle.Coil:
                {
                    // Opposite curved channels braid around the centerline;
                    // the moving wave runs downward and both ends stay pinned.
                    float sway = 1.05f + strength * .18f;
                    Arc(root, top, width * .54f, seed, age, true, 1, sway, .4f);
                    Arc(root, top, width * .54f, seed + 37, age, true, 1, -sway, .4f);
                    break;
                }
                default:
                {
                    // A heavy single stroke with one restrained return pulse.
                    float pulse = 1f + .18f * Mathf.Exp(-Mathf.Pow((progress - .35f) * 12f, 2f));
                    Arc(root, top, width * pulse, seed, age, true, 3, 0f, .8f);
                    break;
                }
            }
        }

        internal void Arc(Vector3 start, Vector3 end, float width, int seed, float age, bool sky,
            int branchCount = -1, float sway = 0f, float jitterScale = 1f)
        {
            Vector3 direction = end - start;
            direction.y = 0f;
            float length = direction.magnitude;
            if (length < .015f) return;
            Vector3 side = new Vector3(-direction.z, 0f, direction.x) / length;
            int pieces = Mathf.Clamp(Mathf.CeilToInt(length * (sky ? 1.2f : 2.6f)), 5, 40);
            float phase = age * 22f;
            int frame = Mathf.FloorToInt(phase);
            float blend = Mathf.SmoothStep(0f, 1f, phase - frame);
            float amplitude = Mathf.Min(sky ? .7f : .38f, length * .14f);
            for (int i = 0; i <= pieces; i++)
            {
                float t = i / (float)pieces;
                float fixedNoise = MapComponent_YunjianziPurpleLightning.Noise(seed, i);
                float motion = Mathf.Lerp(MapComponent_YunjianziPurpleLightning.Noise(seed + frame * 7, i),
                    MapComponent_YunjianziPurpleLightning.Noise(seed + (frame + 1) * 7, i), blend);
                float offset = ((fixedNoise * .7f + motion * .3f) * amplitude * jitterScale +
                    sway * Mathf.Sin(t * Mathf.PI * 5f + age * 9f)) * Mathf.Sin(t * Mathf.PI);
                knots[i] = Vector3.Lerp(start, end, t) + side * offset;
            }
            for (int i = 0; i < pieces; i++)
                Segment(knots[i], knots[i + 1], width * (sky ? Mathf.Lerp(1f, .48f, i / (float)pieces) : 1f));
            int branches = branchCount >= 0 ? branchCount : sky ? 4 : 2;
            for (int i = 0; i < branches; i++)
            {
                int knot = 1 + (i + 1) * (pieces - 2) / (branches + 1);
                float sign = i % 2 == 0 ? 1f : -1f;
                Vector3 root = knots[knot];
                Vector3 tip = root + side * sign * (sky ? 1.1f : .45f) + direction.normalized * (sky ? -1.3f : .22f);
                Vector3 elbow = Vector3.Lerp(root, tip, .5f) + side * .16f * Mathf.Sin(phase + i);
                Segment(root, elbow, width * .45f);
                Segment(elbow, tip, width * .22f);
            }
        }

        internal void Impact(Vector3 center, float strength, int seed, float age, float progress,
            bool ring = true, float ringRadius = 0f)
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = (i * 90f + seed * 37f) * Mathf.Deg2Rad;
                Vector3 ray = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 tip = center + ray * strength * (.6f + .45f * progress);
                Vector3 elbow = Vector3.Lerp(center, tip, .55f) + new Vector3(-ray.z, 0f, ray.x) * .12f;
                Segment(center, elbow, .13f * strength);
                Segment(elbow, tip, .075f * strength);
            }
            if (!ring || strength < 1.25f) return;
            float radius = ringRadius > 0f
                ? ringRadius * (.12f + progress * .88f)
                : strength * (.35f + progress * .8f);
            Vector3 previous = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= 24; i++)
            {
                float angle = i * Mathf.PI / 12f;
                Vector3 next = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                Segment(previous, next, .09f * (1f - progress));
                previous = next;
            }
        }

        internal void EmpPulse(Vector3 center, float radius, int seed, float progress, bool sparks = true)
        {
            // Thin expanding fronts, not a filled ExplosionFlash. The trailing
            // ring and short electrical teeth make EMP readable at low brightness.
            progress = Mathf.Clamp01(progress);
            if (progress >= 1f) return;
            for (int layer = 0; layer < 2; layer++)
            {
                float phase = Mathf.Clamp01((progress - layer * .14f) / (1f - layer * .14f));
                if (layer > 0 && phase <= 0f) continue;
                float reach = radius * (.12f + .88f * Mathf.Sqrt(phase));
                // Keep the front readable while the whole effect fades; fading
                // both width and alpha to zero made the EMP disappear too early.
                float width = (layer == 0 ? .38f : .18f) * (.85f - .25f * progress) * Mathf.Min(1f, radius);
                Vector3 previous = center + new Vector3(reach, 0f, 0f);
                for (int i = 1; i <= 32; i++)
                {
                    float angle = i * Mathf.PI / 16f;
                    Vector3 next = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * reach;
                    Segment(previous, next, width);
                    previous = next;
                }
                if (!sparks || layer > 0) continue;
                for (int i = 0; i < 6; i++)
                {
                    float angle = (i * 60f + seed * 23f + progress * 25f) * Mathf.Deg2Rad;
                    Vector3 ray = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    Vector3 tangent = new Vector3(-ray.z, 0f, ray.x);
                    float length = Mathf.Min(.45f, radius * .18f) * (1f - progress);
                    Vector3 start = center + ray * reach;
                    Vector3 bend = start + ray * length * .55f + tangent * length * .4f;
                    Segment(start, bend, width * .65f);
                    Segment(bend, start + ray * length, width * .4f);
                }
            }
        }

        internal void ParticleExplosion(Vector3 center, float radius, float strength, int seed, float age)
        {
            // One mesh: 24 analytic motes with short jagged trails, two thin EMP
            // fronts and a brief central discharge. No new Things or gameplay RNG.
            if (age < 0f || age >= ParticleExplosionDuration) return;
            // Slow outward motion and the afterglow, not the bright initial discharge.
            // Shared lifetime keeps the effect alive until the last particles fade.
            float motionAge = age * .5f;
            EmpPulse(center, radius, seed, Mathf.Clamp01(motionAge / .42f), false);
            if (age < .18f)
                Impact(center, strength * (1f - age / .18f), seed, age, age / .18f, false);
            for (int i = 0; i < 24; i++)
            {
                float noise = MapComponent_YunjianziPurpleLightning.Noise(seed, i * 3);
                float variation = MapComponent_YunjianziPurpleLightning.Noise(seed, i * 3 + 1);
                float delay = (variation + 1f) * .022f;
                float lifetime = .46f + (noise + 1f) * .10f;
                float phase = (motionAge - delay) / lifetime;
                if (phase <= 0f || phase >= 1f) continue;
                float angle = (i + noise * .32f) * Mathf.PI / 12f;
                Vector3 ray = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 side = new Vector3(-ray.z, 0f, ray.x);
                float reach = radius * (.68f + variation * .23f);
                Vector3 head = ParticlePosition(center, ray, side, reach, noise, phase);
                Vector3 tail = ParticlePosition(center, ray, side, reach, noise,
                    Mathf.Max(0f, phase - .085f / lifetime));
                Vector3 bend = Vector3.Lerp(tail, head, .55f) + side * (.12f * noise * (1f - phase));
                float width = (i % 3 == 0 ? .20f : .12f) * strength * (1f - phase * .8f);
                Segment(tail, bend, width * .55f);
                Segment(bend, head, width * .8f);
                // A small diamond-like spark at the moving tip; no filled glow disc.
                Segment(head - ray * width, head + ray * width, width);
                Segment(head - side * width * .55f, head + side * width * .55f, width * .55f);
            }
        }

        private static Vector3 ParticlePosition(Vector3 center, Vector3 ray, Vector3 side,
            float reach, float noise, float phase)
        {
            float travel = 1f - Mathf.Pow(1f - phase, 3f);
            float arc = Mathf.Sin(phase * Mathf.PI);
            return center + ray * (reach * travel) + side * (noise * .22f * arc) +
                new Vector3(0f, 0f, (.3f + .3f * (noise + 1f)) * arc);
        }

        private void Segment(Vector3 a, Vector3 b, float width)
        {
            Vector3 direction = b - a;
            direction.y = 0f;
            if (direction.sqrMagnitude < .00001f) return;
            Vector3 side = new Vector3(-direction.z, 0f, direction.x).normalized * width * .5f;
            // Tiny overlap conceals seams between the short jagged quads.
            Vector3 overlap = direction.normalized * width * .12f;
            a -= overlap;
            b += overlap;
            a.y = b.y = Altitude;
            int index = vertices.Count;
            vertices.Add(a - side); vertices.Add(a + side);
            vertices.Add(b - side); vertices.Add(b + side);
            uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(1f, 0f));
            uv.Add(new Vector2(0f, 1f)); uv.Add(new Vector2(1f, 1f));
            triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
            triangles.Add(index + 2); triangles.Add(index + 1); triangles.Add(index + 3);
        }

        internal void Draw(float alpha)
        {
            if (vertices.Count == 0) return;
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            Properties.SetColor(ShaderPropertyIDs.Color, new Color(1.12f, 1.12f, 1.12f, Mathf.Clamp01(alpha)));
            Graphics.DrawMesh(mesh, Matrix4x4.identity, RibbonMaterial, 0, null, 0, Properties);
        }

        internal static void DrawGlow(Vector3 position, float size, float alpha)
        {
            position.y = Altitude - .005f;
            Properties.SetColor(ShaderPropertyIDs.Color, new Color(.78f, .46f, 1f, Mathf.Clamp01(alpha)));
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(position, Quaternion.identity,
                new Vector3(size, 1f, size)), GlowMaterial, 0, null, 0, Properties);
        }

        internal void Dispose() { UnityEngine.Object.Destroy(mesh); }
    }
}
