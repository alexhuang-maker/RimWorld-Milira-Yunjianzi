using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    [StaticConstructorOnStartup]
    internal static class YunjianziTrimGlow
    {
        private static readonly Dictionary<Texture, Material> Materials = new Dictionary<Texture, Material>();

        static YunjianziTrimGlow()
        {
            foreach (string direction in new[] { "south", "east", "north" })
                Register("Yunjianzi/Apparel/YunjianziRobe_Female_" + direction, false);
            foreach (string direction in new[] { "south", "east", "north", "west" })
            {
                Register("Yunjianzi/Pawn/HairFront/YunjianziHairFront_" + direction, true);
                Register("Yunjianzi/Pawn/HairBack/YunjianziHairBack_" + direction, true);
            }
            foreach (string suffix in new[] { "", "_Front_east", "_Front_north", "_Front_west" })
                Register("Yunjianzi/Apparel/JiuxiaoXianxu/Normal/JiuxiaoXianxu" + suffix, false);
        }

        private static void Register(string path, bool hair)
        {
            Texture2D source = ContentFinder<Texture2D>.Get(path, false);
            if (source == null || Materials.ContainsKey(source)) return;
            Texture2D mask = null;
            try
            {
                mask = CreateMask(source, p =>
                {
                    p.a = YunjianziTrimGlowMask.Alpha(p.r, p.g, p.b, p.a, hair);
                    return p;
                });
                if (mask != null)
                    Materials.Add(source, MaterialPool.MatFrom(mask, ShaderDatabase.MoteGlow, Color.white));
            }
            catch (Exception e)
            {
                if (mask != null) UnityEngine.Object.Destroy(mask);
                Log.Warning("[Yunjianzi] Could not prepare subtle trim glow for " + path + ": " + e.Message);
            }
        }

        internal static Texture2D CreateMask(Texture2D source, Func<Color32, Color32> select)
        {
            Texture2D mask = null;
            try
            {
                // Imported textures can be compressed or non-readable. Read a GPU
                // copy once on startup, without touching the source texture/files.
                RenderTexture previous = RenderTexture.active;
                RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height,
                    0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                try
                {
                    Graphics.Blit(source, temporary);
                    RenderTexture.active = temporary;
                    mask = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                    mask.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                }
                finally
                {
                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(temporary);
                }
                Color32[] pixels = mask.GetPixels32();
                bool hasEmission = false;
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 p = select(pixels[i]);
                    hasEmission |= p.a != 0;
                    pixels[i] = p;
                }
                if (!hasEmission)
                {
                    UnityEngine.Object.Destroy(mask);
                    return null;
                }
                mask.name = source.name + "_SoftTrimEmission";
                mask.wrapMode = TextureWrapMode.Clamp;
                mask.filterMode = source.filterMode;
                mask.SetPixels32(pixels);
                mask.Apply(false, true);
                return mask;
            }
            catch
            {
                if (mask != null) UnityEngine.Object.Destroy(mask);
                throw;
            }
        }

        internal static void Draw(Mesh mesh, Matrix4x4 matrix, Material source, bool drawNow)
        {
            if (source == null || source.mainTexture == null || mesh == null ||
                !Materials.TryGetValue(source.mainTexture, out Material glow)) return;
            // Same mesh, UVs and matrix: no layer offset, enlargement, reordered
            // pawn nodes or atlas bypass. Opaque clothing/hair still occludes it.
            GenDraw.DrawMeshNowOrLater(mesh, matrix, glow, drawNow);
        }
    }

    [HarmonyPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.Draw))]
    internal static class Patch_Yunjianzi_SubtleTrimGlow
    {
        private static void Postfix(PawnDrawParms parms, List<PawnGraphicDrawRequest> ___drawRequests)
        {
            if (parms.pawn == null || parms.pawn.Dead || parms.Portrait || parms.Statue ||
                (parms.flags & PawnRenderFlags.Invisible) != 0) return;
            // DrawNow also handles vanilla atlas generation. Steady highlights
            // need no ticking, recache requests or changes to vanilla zoom caching.
            for (int i = 0; i < ___drawRequests.Count; i++)
            {
                PawnGraphicDrawRequest request = ___drawRequests[i];
                YunjianziTrimGlow.Draw(request.mesh, request.preDrawnComputedMatrix,
                    request.material, parms.DrawNow);
            }
        }
    }
}
