using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    public sealed class CompProperties_Sanyuan : CompProperties
    {
        public CompProperties_Sanyuan() { compClass = typeof(CompSanyuan); }
    }

    public sealed partial class CompYunjianziCultivation
    {
        internal CompSanyuan Sanyuan => Pawn?.apparel?.WornApparel
            .Select(a => a.TryGetComp<CompSanyuan>()).FirstOrDefault(c => c != null);
    }

    public sealed partial class CompYunjianziShenji
    {
        internal int sanyuanCombatTick = -99999;
    }

    internal static class SanyuanRules
    {
        internal const float Capacity = 100f;
        internal const int SweepTicks = 48;
        internal const int SweepCount = 3;
        internal static float Charge(float current, bool combined) => Mathf.Min(Capacity, current + (combined ? 10f : 4f));
        internal static float Feedback(float damage, bool combined) => Mathf.Min(20f, damage * .2f) + (combined ? 10f : 0f);
        internal static Vector3 Closest(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector3 direction = end - start; direction.y = 0;
            float t = direction.sqrMagnitude < .001f ? 0 : Mathf.Clamp01(Vector3.Dot(point - start, direction) / direction.sqrMagnitude);
            return start + direction * t;
        }
        internal static Vector3 ClipPath(Vector3 start, Vector3 end, Func<Vector3, bool> traversable)
        {
            Vector3 safe = start;
            int steps = Mathf.CeilToInt((end - start).magnitude * 3f);
            for (int i = 1; i <= steps; i++)
            {
                Vector3 candidate = Vector3.Lerp(start, end, i / (float)steps);
                if (!traversable(candidate)) break;
                safe = candidate;
            }
            return safe;
        }
    }

    // The inventory object owns charge and volley state; unequipping stops all effects.
    public sealed class CompSanyuan : ThingComp
    {
        private float charge;
        private int volleyAge = -1;
        private Vector3 origin, endpoint;
        private Map volleyMap;
        private List<Pawn> swept = new List<Pawn>();
        private List<Pawn> marked = new List<Pawn>();
        private List<int> markExpiry = new List<int>();
        private readonly Dictionary<int, int> shotTicks = new Dictionary<int, int>();
        private readonly Dictionary<int, int> lastShooter = new Dictionary<int, int>();
        private readonly Dictionary<int, int> targetTicks = new Dictionary<int, int>();
        private readonly Dictionary<int, int> resonanceTicks = new Dictionary<int, int>();
        private readonly List<Tuple<Vector3, Vector3, int>> links = new List<Tuple<Vector3, Vector3, int>>();
        private readonly List<Vector3> beamTrail = new List<Vector3>(8);
        private Pawn Wearer => (parent as Apparel)?.Wearer;
        internal bool Active => Wearer?.Spawned == true && !Wearer.Dead && !Wearer.Downed &&
            Wearer.TryGetComp<CompYunjianziCultivation>()?.IsYunjianzi == true;
        private bool Enemy(Pawn pawn) => pawn != null && pawn.Spawned && !pawn.Dead && pawn.Map == Wearer?.Map && pawn.HostileTo(Wearer);

        internal void OnHit(Pawn shooter, Pawn target, float damage)
        {
            // Killing blows still contribute; the dead pawn may already be despawned.
            if (!Active || target == null || !target.HostileTo(Wearer) || shooter.Map != Wearer.Map || damage <= 0) return;
            int now = Find.TickManager.TicksGame;
            // One credit per machine per half-second, irrespective of pellets/AoE.
            if (!shotTicks.TryGetValue(shooter.thingIDNumber, out int last) || now - last >= 30)
            {
                bool combined = lastShooter.TryGetValue(target.thingIDNumber, out int other) && other != shooter.thingIDNumber &&
                    targetTicks.TryGetValue(target.thingIDNumber, out int hit) && now - hit <= 120 &&
                    (!resonanceTicks.TryGetValue(target.thingIDNumber, out int proc) || now - proc >= 60);
                shotTicks[shooter.thingIDNumber] = now;
                lastShooter[target.thingIDNumber] = shooter.thingIDNumber;
                targetTicks[target.thingIDNumber] = now;
                if (combined) resonanceTicks[target.thingIDNumber] = now;
                charge = SanyuanRules.Charge(charge, combined);
                var cultivation = Wearer.TryGetComp<CompYunjianziCultivation>();
                cultivation.Shield += SanyuanRules.Feedback(damage, combined);
            }
            int index = marked.IndexOf(target);
            if (index < 0 || markExpiry[index] <= now) return;
            // Snapshot before nested damage; share never recursively invokes this method.
            foreach (Pawn neighbor in marked.Where((p, i) => p != target && markExpiry[i] > now && Enemy(p) &&
                p.Position.DistanceTo(target.Position) <= 6f && GenSight.LineOfSight(target.Position, p.Position, Wearer.Map)).ToArray())
            {
                if (links.Count >= 24) links.RemoveAt(0);
                links.Add(Tuple.Create(target.DrawPos, neighbor.DrawPos, now + 12));
                Deal(neighbor, damage * .25f);
            }
        }

        internal void TickWorn()
        {
            if (!Active) { volleyAge = -1; swept.Clear(); beamTrail.Clear(); return; }
            int now = Find.TickManager.TicksGame;
            links.RemoveAll(link => link.Item3 <= now);
            if (volleyAge >= 0)
            {
                if (volleyMap != Wearer.Map || Wearer.Position.DistanceTo(origin.ToIntVec3()) > 6f)
                { volleyAge = -1; swept.Clear(); beamTrail.Clear(); }
                else TickVolley();
            }
            if (volleyAge < 0 && charge >= SanyuanRules.Capacity && now % 15 == 0) TryStartVolley();
            if (now % 60 != 0) return;
            for (int i = marked.Count - 1; i >= 0; i--)
                if (markExpiry[i] <= now || !Enemy(marked[i])) { marked.RemoveAt(i); markExpiry.RemoveAt(i); }
            foreach (var dict in new[] { shotTicks, targetTicks, resonanceTicks })
                foreach (int key in dict.Where(p => now - p.Value > 600).Select(p => p.Key).ToArray()) dict.Remove(key);
            foreach (int key in lastShooter.Keys.Where(k => !targetTicks.ContainsKey(k)).ToArray()) lastShooter.Remove(key);
            RepairAllies(now);
        }

        private void RepairAllies(int now)
        {
            var cultivation = Wearer.TryGetComp<CompYunjianziCultivation>();
            foreach (Pawn ally in Wearer.Map.mapPawns.AllPawnsSpawned)
            {
                var shenji = ally.TryGetComp<CompYunjianziShenji>();
                if (shenji?.Owner != Wearer || ally.Dead || now - shenji.sanyuanCombatTick < 600 || ally.mindState?.enemyTarget != null) continue;
                var wound = ally.health.hediffSet.hediffs.OfType<Hediff_Injury>().FirstOrDefault(h => !h.IsPermanent() && h.Severity > 0);
                if (wound != null && cultivation.TrySpend(2f)) wound.Heal(Mathf.Min(3f, wound.Severity));
            }
        }

        private void TryStartVolley()
        {
            Pawn[] targets = Wearer.Map.mapPawns.AllPawnsSpawned.Where(p => Enemy(p) && !p.Downed &&
                p.Position.DistanceTo(Wearer.Position) <= 28f && GenSight.LineOfSight(Wearer.Position, p.Position, Wearer.Map)).ToArray();
            if (targets.Length == 0) return; // Retain full charge until a valid enemy exists.
            Pawn target = targets.OrderByDescending(p => targets.Count(q => q.Position.DistanceTo(p.Position) <= 5f))
                .ThenBy(p => p.Position.DistanceTo(Wearer.Position)).First();
            origin = Wearer.Position.ToVector3Shifted();
            Vector3 direction = target.Position.ToVector3Shifted() - origin; direction.y = 0;
            if (direction.sqrMagnitude < .1f) return;
            endpoint = origin + direction.normalized * Mathf.Min(30f, direction.magnitude + 6f);
            volleyMap = Wearer.Map; volleyAge = 0; charge = 0; swept.Clear(); beamTrail.Clear();
        }

        private Vector3 BeamEnd(int pass, float progress)
        {
            Vector3 direction = endpoint - origin; direction.y = 0;
            Vector3 side = new Vector3(-direction.z, 0, direction.x).normalized;
            progress = Mathf.Clamp01(progress);
            // The third ray's leading edge advances from the caster to the far
            // end. Damage, displacement and rendering all use this moving tip.
            if (pass == 2) return Vector3.Lerp(origin, endpoint, progress);
            return endpoint + side * (pass == 0 ? 7f : -7f) * (1f - progress);
        }

        private Vector3 Clip(Vector3 start, Vector3 end)
        {
            return SanyuanRules.ClipPath(start, end, candidate =>
            {
                IntVec3 cell = candidate.ToIntVec3();
                return cell.InBounds(Wearer.Map) && cell.Standable(Wearer.Map);
            });
        }

        private void TickVolley()
        {
            int pass = volleyAge / SanyuanRules.SweepTicks;
            int localAge = volleyAge % SanyuanRules.SweepTicks;
            if (localAge == 0) { swept.Clear(); beamTrail.Clear(); }
            if (beamTrail.Count >= 8) beamTrail.RemoveAt(0);
            beamTrail.Add(Clip(origin, BeamEnd(pass, localAge / (float)SanyuanRules.SweepTicks)));
            if (localAge % 3 == 0 || localAge == SanyuanRules.SweepTicks - 1)
            {
                float progress = (localAge + 1f) / SanyuanRules.SweepTicks;
                Vector3 end = Clip(origin, BeamEnd(pass, progress));
                foreach (Pawn target in Wearer.Map.mapPawns.AllPawnsSpawned.Where(Enemy).ToArray())
                {
                    Vector3 point = target.Position.ToVector3Shifted();
                    Vector3 nearest = SanyuanRules.Closest(point, origin, end);
                    if ((point - nearest).sqrMagnitude > 1.25f * 1.25f || swept.Contains(target) ||
                        !GenSight.LineOfSight(origin.ToIntVec3(), target.Position, Wearer.Map)) continue;
                    swept.Add(target);
                    Deal(target, pass == 2 ? 60f : 35f);
                    if (!Enemy(target)) continue;
                    int index = marked.IndexOf(target);
                    if (index < 0) { marked.Add(target); markExpiry.Add(Find.TickManager.TicksGame + 480); }
                    else markExpiry[index] = Find.TickManager.TicksGame + 480;
                }
                foreach (Pawn target in swept.Where(Enemy).OrderByDescending(p => p.Position.DistanceTo(origin.ToIntVec3())).ToArray())
                {
                    Vector3 pushEnd = pass == 2 ? end : Clip(origin, endpoint);
                    Vector3 far = Clip(target.Position.ToVector3Shifted(), pushEnd);
                    Vector3 from = target.Position.ToVector3Shifted();
                    float distance = (far - from).magnitude;
                    if (distance < .5f) continue;
                    Vector3 next = from + (far - from).normalized * Mathf.Min(1.5f, distance);
                    IntVec3 destination = next.ToIntVec3();
                    if (!destination.InBounds(Wearer.Map) || !destination.Standable(Wearer.Map) || destination.GetFirstPawn(Wearer.Map) != null) continue;
                    target.pather?.StopDead(); target.Position = destination; target.Notify_Teleported(true, false);
                }
            }
            volleyAge++;
            if (volleyAge >= SanyuanRules.SweepTicks * SanyuanRules.SweepCount) { volleyAge = -1; swept.Clear(); beamTrail.Clear(); }
        }

        private void Deal(Pawn target, float amount)
        {
            SanyuanDamage.InsideEffect++;
            try { target.TakeDamage(new DamageInfo(DamageDefOf.Burn, amount, 1f, -1f, Wearer)); }
            finally { SanyuanDamage.InsideEffect--; }
        }

        public override string CompInspectStringExtra() => $"秘仪充能：{charge:0} / 100";
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            volleyAge = -1; swept.Clear(); marked.Clear(); markExpiry.Clear(); links.Clear(); beamTrail.Clear();
        }
        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            yield return new Command_Action { defaultLabel = $"三垣秘仪斗 {charge:0}/100", defaultDesc = "神机命中反哺剑罡并充能；满充自动三段扫射。归墟印持续8秒，神机命中会向6格内同印敌人传递25%伤害。脱战10秒后每秒耗2灵力修复神机3点普通损伤。", action = () => { } };
        }

        public override void CompDrawWornExtras()
        {
            if (!Active) return;
            SanyuanAppearance.Draw(Wearer, charge, volleyAge >= 0);
            foreach (var link in links)
            {
                Vector3 a = link.Item1, b = link.Item2;
                a.y = b.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                SanyuanBeamVfx.Line(a, b, .075f, new Color(1f, .82f, .42f), Mathf.Clamp01((link.Item3 - Find.TickManager.TicksGame) / 12f));
            }
            if (volleyAge >= 0 && volleyMap == Wearer.Map)
            {
                int pass = volleyAge / SanyuanRules.SweepTicks;
                Vector3 start = SanyuanAppearance.EmitterPosition(Wearer, pass);
                Vector3 end = Clip(origin, BeamEnd(volleyAge / SanyuanRules.SweepTicks, (volleyAge % SanyuanRules.SweepTicks) / (float)SanyuanRules.SweepTicks));
                start.y = end.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                SanyuanBeamVfx.Draw(start, end, pass, volleyAge % SanyuanRules.SweepTicks, beamTrail);
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref charge, "sanyuanCharge");
            Scribe_Values.Look(ref volleyAge, "sanyuanVolleyAge", -1);
            Scribe_Values.Look(ref origin, "sanyuanOrigin"); Scribe_Values.Look(ref endpoint, "sanyuanEndpoint");
            Scribe_References.Look(ref volleyMap, "sanyuanMap");
            Scribe_Collections.Look(ref swept, "sanyuanSwept", LookMode.Reference);
            Scribe_Collections.Look(ref marked, "sanyuanMarked", LookMode.Reference);
            Scribe_Collections.Look(ref markExpiry, "sanyuanMarkExpiry", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                swept = swept ?? new List<Pawn>(); marked = marked ?? new List<Pawn>(); markExpiry = markExpiry ?? new List<int>();
                if (marked.Count != markExpiry.Count) { marked.Clear(); markExpiry.Clear(); }
                charge = Mathf.Clamp(charge, 0, 100);
                if (volleyAge >= 144 || volleyAge < -1) volleyAge = -1;
            }
        }
    }

    [StaticConstructorOnStartup]
    internal static class SanyuanAppearance
    {
        private static Material Spirit, Glow;
        internal static Vector3 EmitterPosition(Pawn pawn, int pass)
        {
            float angle = (pass == 0 ? 210f : pass == 1 ? 330f : 90f) * Mathf.Deg2Rad;
            Vector3 center = pawn.DrawPos + pawn.Drawer.renderer.BaseHeadOffsetAt(pawn.Rotation);
            float width = pawn.Rotation.IsHorizontal ? .38f : 1f;
            if (pawn.Rotation == Rot4.East) center.x -= .16f;
            else if (pawn.Rotation == Rot4.West) center.x += .16f;
            return center + new Vector3(Mathf.Cos(angle) * .4f * width, 0f, Mathf.Sin(angle) * .4f);
        }
        internal static void Draw(Pawn pawn, float charge, bool firing)
        {
            if (Spirit == null)
            {
                Texture2D texture = ContentFinder<Texture2D>.Get("Yunjianzi/Apparel/SanyuanMiyi/Spirit", false);
                if (texture == null) return; // Art-selection builds have no final sprite yet.
                Spirit = MaterialPool.MatFrom(texture, ShaderDatabase.TransparentPostLight, Color.white);
                Glow = MaterialPool.MatFrom(texture, ShaderDatabase.MoteGlow, Color.white);
            }
            Rot4 facing = pawn.Rotation;
            Vector3 center = pawn.DrawPos + pawn.Drawer.renderer.BaseHeadOffsetAt(facing);
            // Daedalus crown is behind the head in south/east/west views.
            center.y = pawn.DrawPos.y + (facing == Rot4.North ? .04f : -.015f);
            if (facing == Rot4.East) center.x -= .16f;
            else if (facing == Rot4.West) center.x += .16f;
            float time = (Find.TickManager.TicksGame + pawn.thingIDNumber * 17) / 60f;
            float size = 1.02f + Mathf.Sin(time * 1.4f) * .016f;
            float width = facing.IsHorizontal ? .38f : 1f;
            // Keep the selected geometric silhouette steady; the light breathes inside it.
            var matrix = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(size * width, 1f, size));
            Graphics.DrawMesh(MeshPool.plane10, matrix, Spirit, 0);
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", new Color(1f, .93f, .75f, firing ? .65f : .08f + charge * .002f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, Glow, 0, null, 0, block);
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    internal static class SanyuanDamage
    {
        internal static int InsideEffect;
        private static void Prefix(Thing __instance, ref DamageInfo dinfo)
        {
            int now = Find.TickManager?.TicksGame ?? 0;
            var victim = (__instance as Pawn)?.TryGetComp<CompYunjianziShenji>();
            if (victim != null && dinfo.Def.harmsHealth) victim.sanyuanCombatTick = now;
            if (InsideEffect > 0 || !dinfo.Def.harmsHealth) return;
            var shenji = (dinfo.Instigator as Pawn)?.TryGetComp<CompYunjianziShenji>();
            if (shenji == null) return;
            shenji.sanyuanCombatTick = now;
            if (shenji.Cultivation?.Sanyuan?.Active == true && __instance.HostileTo(shenji.Owner)) dinfo.SetAmount(dinfo.Amount * 1.3f);
        }
        private static void Postfix(Thing __instance, DamageInfo dinfo, DamageWorker.DamageResult __result)
        {
            if (InsideEffect > 0 || __result == null || __result.totalDamageDealt <= 0 || !dinfo.Def.harmsHealth) return;
            Pawn attacker = dinfo.Instigator as Pawn;
            attacker?.TryGetComp<CompYunjianziShenji>()?.Cultivation?.Sanyuan?.OnHit(attacker, __instance as Pawn, __result.totalDamageDealt);
        }
    }
}
