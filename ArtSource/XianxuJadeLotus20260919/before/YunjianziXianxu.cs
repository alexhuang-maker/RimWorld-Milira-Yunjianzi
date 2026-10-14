using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    public sealed class CompProperties_JiuxiaoXianxu : CompProperties
    {
        public float drawScale = 1.50f;
        public float drawOffsetZ = -0.08f;

        public CompProperties_JiuxiaoXianxu()
        {
            compClass = typeof(CompJiuxiaoXianxu);
        }
    }

    /// <summary>Draws a floating celestial silk without replacing the robe or wings.</summary>
    [StaticConstructorOnStartup]
    public sealed class CompJiuxiaoXianxu : ThingComp
    {
        private CompProperties_JiuxiaoXianxu Props => (CompProperties_JiuxiaoXianxu)props;

        public override void CompDrawWornExtras()
        {
            Pawn pawn = (parent as Apparel)?.Wearer;
            if (pawn == null || !pawn.Spawned || pawn.Dead) return;

            Rot4 rot = pawn.Rotation;
            JiuxiaoXianxuDrawer.Draw(pawn, pawn.DrawPos, rot,
                Props.drawScale, Props.drawOffsetZ);
        }
    }

    [StaticConstructorOnStartup]
    internal static class JiuxiaoXianxuDrawer
    {
        private const string ThunderRoot = "Yunjianzi/Apparel/JiuxiaoXianxu/JiuxiaoXianxu";
        private const string NormalRoot = "Yunjianzi/Apparel/JiuxiaoXianxu/Normal/JiuxiaoXianxu";
        private static readonly Material[] Normal = Load(NormalRoot, ShaderDatabase.Transparent);
        private static readonly Material[] Thunder = Load(ThunderRoot, ShaderDatabase.TransparentPostLight);
        private static readonly Material[] ThunderGlow = Load(ThunderRoot, ShaderDatabase.MoteGlow);
        private static readonly Mesh[] Drapes = Enumerable.Range(0, 4).Select(CreateDrape).ToArray();
        private static readonly MaterialPropertyBlock Dimmed = CreatePropertyBlock();
        private static readonly MaterialPropertyBlock Radiance = new MaterialPropertyBlock();

        private static MaterialPropertyBlock CreatePropertyBlock()
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor(ShaderPropertyIDs.Color, new Color(.94f, .96f, .95f, .92f));
            return block;
        }

        private static Material[] Load(string root, Shader shader)
        {
            // Rot4.AsInt is North, East, South, West. The final side source is
            // authored in world-facing orientation, so use the matching material.
            // The south item image is the full, uncut composition. All other
            // directions already have complete images in their Front files.
            return new[] { "_Front_north", "_Front_east", "", "_Front_west" }
                .Select(suffix => MaterialPool.MatFrom(root + suffix, shader)).ToArray();
        }

        private static Mesh CreateDrape(int rotation)
        {
            var vertices = new Vector3[(XianxuDrape.Rows + 1) * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[XianxuDrape.Rows * 6];
            for (int row = 0; row <= XianxuDrape.Rows; row++)
            {
                float canvasY = row * 512f / XianxuDrape.Rows;
                for (int side = 0; side < 2; side++)
                {
                    int i = row * 2 + side;
                    XianxuDrape.Position(rotation, side * 512f, canvasY, 1f, out float x, out float depth, out float z);
                    vertices[i] = new Vector3(x, depth, z);
                    uv[i] = new Vector2(side, 1f - canvasY / 512f);
                }
                if (row == XianxuDrape.Rows) continue;
                int v = row * 2, t = row * 6;
                // Same upward winding and UV orientation as MeshMakerPlanes.
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                triangles[t + 3] = v + 1; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
            }
            var mesh = new Mesh { name = "JiuxiaoXianxuDrape_" + rotation };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        internal static void Draw(Pawn pawn, Vector3 drawLoc, Rot4 rot, float scale, float offsetZ)
        {
            int ticks = Find.TickManager?.TicksGame ?? 0;
            float phase = ticks * .035f + pawn.thingIDNumber * .17f;
            float animatedScale = scale + Mathf.Sin(phase) * .035f;
            Vector3 loc = drawLoc + new Vector3(0f, 0f,
                offsetZ + Mathf.Cos(phase * .52f) * .018f);
            // Depth changes run along the camera ray, avoiding projected seams
            // even when a camera mod tilts the usual overhead view.
            Vector3 forward = Find.Camera != null ? Find.Camera.transform.forward : Vector3.down;
            Vector3 depthAxis = Mathf.Abs(forward.y) > .01f ? forward / forward.y : Vector3.up;
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.identity,
                new Vector3(animatedScale, 1f, animatedScale));
            matrix.m01 = depthAxis.x;
            matrix.m21 = depthAxis.z;
            // Live draft state also changes immediately while paused.
            bool thunder = pawn.Drafted;
            Graphics.DrawMesh(Drapes[rot.AsInt], matrix, (thunder ? Thunder : Normal)[rot.AsInt],
                0, null, 0, thunder ? null : Dimmed);
            if (thunder)
            {
                // Same post-light + additive pair as the sword wheel. Reuse the
                // exact drape and silhouette rather than enlarging a flat halo
                // through the head or moving the shoulder transition.
                Radiance.SetColor(ShaderPropertyIDs.Color, new Color(.72f,.82f,1f,
                    XianxuParticleMotion.GlowAlpha(ticks, pawn.thingIDNumber)));
                Matrix4x4 glowMatrix = matrix;
                Vector3 glowOffset = depthAxis * .001f;
                glowMatrix.m03 += glowOffset.x;
                glowMatrix.m13 += glowOffset.y;
                glowMatrix.m23 += glowOffset.z;
                Graphics.DrawMesh(Drapes[rot.AsInt], glowMatrix, ThunderGlow[rot.AsInt], 0, null, 0, Radiance);
                XianxuParticles.Draw(pawn, loc, rot, animatedScale, ticks, depthAxis);
            }
        }
    }
}
