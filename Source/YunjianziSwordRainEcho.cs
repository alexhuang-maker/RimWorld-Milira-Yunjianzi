using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Yunjianzi
{
    public sealed class ThingYunjianziSwordRainEcho : Thing
    {
        private Pawn owner;
        private int totalDamage = 84;
        private float radius = 3.9f;
        private int age;
        private Vector3 exactPosition;
        private Vector3 strokeFrom;
        private Vector3 strokeTo;
        private List<int> hitThisStroke = new List<int>();
        private RenderTexture appearance;
        private Material ghostMaterial;
        private Material weaponMaterial;
        private bool drawWeapon;
        private MaterialPropertyBlock ghostProperties;

        public override int UpdateRateTicks => 1;
        public override Vector3 DrawPos => exactPosition;

        public void Initialize(Pawn caster, int damage, float areaRadius, Vector3 center)
        {
            owner = caster;
            totalDamage = Mathf.Max(1, damage);
            radius = Mathf.Max(1f, areaRadius);
            exactPosition = strokeFrom = strokeTo = center;
        }

        protected override void TickInterval(int delta)
        {
            if (Destroyed) return;
            // Retire expired echoes from existing saves before rebuilding their render textures.
            if (age >= SwordRainEchoMotion.Duration || owner == null || owner.Destroyed || owner.Dead || owner.MapHeld != Map || !Spawned)
            {
                Destroy();
                return;
            }
            if (appearance == null && age % SwordRainEchoMotion.StrokeTicks != 0) CaptureAppearance();
            for (int tick = 0; tick < delta && age < SwordRainEchoMotion.Duration; tick++)
            {
                int phase = age % SwordRainEchoMotion.StrokeTicks;
                if (phase == 0) BeginStroke();
                Vector3 before = exactPosition;
                exactPosition = Vector3.Lerp(strokeFrom, strokeTo, SwordRainEchoMotion.Progress(phase + 1));
                HitAlongSegment(before, exactPosition);
                if (phase == SwordRainEchoMotion.StrokeTicks / 2) PlaySlash(exactPosition, 1.6f);
                age++;
            }
            if (age >= SwordRainEchoMotion.Duration) Destroy();
        }

        private bool ValidTarget(Thing thing)
        {
            return thing != null && thing != owner && thing.Spawned && !thing.Destroyed && thing.Map == Map &&
                thing.HostileTo(owner) && ((thing is Pawn pawn && !pawn.Dead) || (thing is Building && thing.def.useHitPoints)) &&
                thing.Position.DistanceToSquared(Position) <= radius * radius &&
                GenSight.LineOfSight(Position, thing.Position, Map, true);
        }

        private List<Thing> Targets()
        {
            return GenRadial.RadialDistinctThingsAround(Position, Map, radius, true).Where(ValidTarget).ToList();
        }

        private void BeginStroke()
        {
            hitThisStroke.Clear();
            strokeFrom = exactPosition;
            Vector3 center = Position.ToVector3Shifted();
            List<Thing> targets = Targets();
            targets.Sort((a, b) => a.thingIDNumber.CompareTo(b.thingIDNumber));
            int stroke = age / SwordRainEchoMotion.StrokeTicks;
            Vector3 pivot = targets.Count > 0 ? targets[stroke % targets.Count].Position.ToVector3Shifted() : center;
            float angle = (thingIDNumber % 360 + stroke * 137.5f) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            if (stroke == 0) direction = pivot - center;
            if (direction.sqrMagnitude < .001f) direction = Vector3.right;
            float x, z;
            // Each subsequent echo fades out then reappears on a new chord.
            // Repositioning is visual only and must never sweep damage between chords.
            if (stroke > 0)
            {
                SwordRainEchoMotion.EndPoint(pivot.x - center.x, pivot.z - center.z,
                    -direction.x, -direction.z, radius, out x, out z);
                strokeFrom = ClampToMap(center + new Vector3(x, 0f, z));
            }
            SwordRainEchoMotion.EndPoint(pivot.x - center.x, pivot.z - center.z,
                direction.x, direction.z, radius, out x, out z);
            strokeTo = ClampToMap(center + new Vector3(x, 0f, z));
            exactPosition = strokeFrom;
            CaptureAppearance();
        }

        private Vector3 ClampToMap(Vector3 point)
        {
            point.x = Mathf.Clamp(point.x, .5f, Map.Size.x - .5f);
            point.z = Mathf.Clamp(point.z, .5f, Map.Size.z - .5f);
            return point;
        }

        private void HitAlongSegment(Vector3 from, Vector3 to)
        {
            float halfWidth = SwordRainEchoMotion.HitHalfWidth(radius);
            foreach (Thing target in Targets())
            {
                if (hitThisStroke.Contains(target.thingIDNumber)) continue;
                Vector3 point = target.Position.ToVector3Shifted();
                if (SwordRainEchoMotion.DistanceSquared(point.x, point.z, from.x, from.z, to.x, to.z) > halfWidth * halfWidth) continue;
                if (!GenSight.LineOfSight(to.ToIntVec3(), target.Position, Map, true)) continue;
                hitThisStroke.Add(target.thingIDNumber);
                target.TakeDamage(new DamageInfo(DamageDefOf.Cut, SwordRainEchoMotion.DamagePerStroke(totalDamage),
                    .62f, (strokeTo - strokeFrom).AngleFlat(), owner));
                PlaySlash(point, 2f);
                SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(point.ToIntVec3(), Map));
            }
        }

        private void PlaySlash(Vector3 point, float size)
        {
            if (!point.ToIntVec3().InBounds(Map)) return;
            size *= SwordRainEchoMotion.SlashScale(radius);
            int variant = age / SwordRainEchoMotion.StrokeTicks % 4;
            FleckCreationData slash = FleckMaker.GetDataStatic(point, Map,
                variant % 2 == 0 ? YJZDefOf.YJZ_Fleck_ZhuxieSlashA : YJZDefOf.YJZ_Fleck_ZhuxieSlashB, size);
            slash.exactScale = new Vector3(variant < 2 ? size : -size, 1f, size);
            slash.rotation = (strokeTo - strokeFrom).AngleFlat() + (variant % 2 == 0 ? 135f : 90f);
            slash.instanceColor = new Color(.65f, .9f, 1.15f, .7f);
            Map.flecks.CreateFleck(slash);
        }

        private void CaptureAppearance()
        {
            Vector3 direction = strokeTo - strokeFrom;
            Rot4 facing = Mathf.Abs(direction.x) > Mathf.Abs(direction.z)
                ? (direction.x >= 0 ? Rot4.East : Rot4.West)
                : (direction.z >= 0 ? Rot4.North : Rot4.South);
            // Render current world appearance before the map's parallel draw phase.
            // The texture belongs to this effect, so UI portrait cache eviction or
            // pausing cannot replace it with another pawn's portrait.
            if (appearance == null)
            {
                appearance = new RenderTexture(256, 256, 24) { name = "Yunjianzi sword echo", filterMode = FilterMode.Bilinear };
                appearance.Create();
            }
            Find.PawnCacheRenderer.RenderPawn(owner, appearance, Vector3.zero, 1f, 0f, facing,
                renderHead: owner.health.hediffSet.HasHead, renderHeadgear: true, renderClothes: true, portrait: false);
            if (ghostMaterial == null) ghostMaterial = new Material(ShaderDatabase.MoteGlow);
            ghostMaterial.mainTexture = appearance;
            ThingWithComps weapon = owner.equipment?.Primary;
            drawWeapon = weapon != null;
            if (drawWeapon)
            {
                if (weaponMaterial == null) weaponMaterial = new Material(ShaderDatabase.MoteGlow);
                weaponMaterial.mainTexture = weapon.Graphic.MatSingle.mainTexture;
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (owner == null || owner.Destroyed || owner.Dead || !Spawned || ghostMaterial == null) return;
            if (ghostProperties == null) ghostProperties = new MaterialPropertyBlock();
            int phase = age <= 0 ? 0 : (age - 1) % SwordRainEchoMotion.StrokeTicks + 1;
            float fade = SwordRainEchoMotion.Opacity(phase);
            for (int trail = 2; trail >= 0; trail--)
            {
                Vector3 point = trail == 0 ? exactPosition : Vector3.Lerp(strokeFrom, strokeTo,
                    SwordRainEchoMotion.Progress(Mathf.Max(0, phase - trail * 2)));
                point.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead) + (2 - trail) * .001f;
                float alpha = (trail == 0 ? .58f : trail == 1 ? .22f : .1f) * fade;
                ghostProperties.SetColor(ShaderPropertyIDs.Color, new Color(.55f, .82f, 1.15f, alpha));
                Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(point, Quaternion.identity, new Vector3(2f, 1f, 2f)),
                    ghostMaterial, 0, null, 0, ghostProperties);
            }
            if (drawWeapon && weaponMaterial != null)
            {
                Vector3 direction = (strokeTo - strokeFrom).normalized;
                Vector3 point = exactPosition + direction * .55f;
                point.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead) + .004f;
                float swing = Mathf.Lerp(-55f, 65f, SwordRainEchoMotion.Progress(phase));
                ghostProperties.SetColor(ShaderPropertyIDs.Color, new Color(.6f, .86f, 1.2f, .65f * fade));
                Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(point,
                    Quaternion.AngleAxis(direction.AngleFlat() + swing, Vector3.up), new Vector3(1.3f, 1f, 1.3f)),
                    weaponMaterial, 0, null, 0, ghostProperties);
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (appearance != null) { appearance.Release(); UnityEngine.Object.Destroy(appearance); appearance = null; }
            if (ghostMaterial != null) { UnityEngine.Object.Destroy(ghostMaterial); ghostMaterial = null; }
            if (weaponMaterial != null) { UnityEngine.Object.Destroy(weaponMaterial); weaponMaterial = null; }
            base.Destroy(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref owner, "swordRainEchoOwner");
            Scribe_Values.Look(ref totalDamage, "totalDamage", 84);
            Scribe_Values.Look(ref radius, "radius", 3.9f);
            Scribe_Values.Look(ref age, "age", 0);
            Scribe_Values.Look(ref exactPosition, "exactPosition");
            Scribe_Values.Look(ref strokeFrom, "strokeFrom");
            Scribe_Values.Look(ref strokeTo, "strokeTo");
            Scribe_Collections.Look(ref hitThisStroke, "hitThisStroke", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && hitThisStroke == null) hitThisStroke = new List<int>();
        }
    }
}
