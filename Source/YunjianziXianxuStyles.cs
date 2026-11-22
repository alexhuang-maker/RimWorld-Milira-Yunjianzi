using UnityEngine;
using Verse;

namespace Yunjianzi
{
    internal static class XianxuAppearanceRules
    {
        private static readonly string[] Styles = { "Hidden", "A", "C" };
        private static readonly string[] Colours = { "Cyan", "Violet", "Gold" };

        internal static int StyleIndex(bool cloudLattice, bool alwaysVisible, bool drafted)
            => !alwaysVisible && !drafted ? 0 : cloudLattice ? 2 : 1;

        internal static string TexturePath(int style, int realm)
            => "Yunjianzi/Apparel/JiuxiaoXianxu/Styles/Realm/" + Styles[style] + "_" + Colours[LightningPalette.Band(realm)];

        internal static Color GauzePixel(Color c, bool gemstone)
        {
            if (gemstone || c.a <= 0f) return c;
            float maximum = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float minimum = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            float saturation = maximum > .001f ? (maximum - minimum) / maximum : 0f;
            // Neutral silver stays crisp. Colored spiritual fibers remain see-through,
            // even when a source sprite's pigment is dark but its alpha is opaque.
            float spiritual = Mathf.Clamp01((saturation - .06f) / .14f);
            c.a = Mathf.Lerp(c.a, Mathf.Min(c.a * 1.12f, .34f), spiritual);
            return c;
        }

        internal static bool IsGemstone(int style, float x, float y)
        {
            float side = Mathf.Min(x, 1f - x);
            if (style == 2)
                return (Mathf.Abs(side - .35f) < .022f && Mathf.Abs(y - .277f) < .025f) ||
                    (Mathf.Abs(side - .30f) < .025f && Mathf.Abs(y - .615f) < .028f);
            return (Mathf.Abs(side - .28f) < .023f && Mathf.Abs(y - .285f) < .025f) ||
                (style == 1 && Mathf.Abs(side - .22f) < .020f && Mathf.Abs(y - .92f) < .025f);
        }
    }

    [StaticConstructorOnStartup]
    internal static class XianxuStyleDrawer
    {
        // Each texture keeps its physical frame white; tinting the entire old
        // purple sprite would also stain the metal and muddy golden lightning.
        private static readonly Material[,] Materials = LoadMaterials();
        private static readonly Mesh[] Halves = { Half(false), Half(true) };
        private static readonly MaterialPropertyBlock Tint = new MaterialPropertyBlock();

        private static Material[,] LoadMaterials()
        {
            var materials = new Material[3, 3];
            int[] representativeRealms = { 2, 3, 5 };
            for (int style = 0; style < 3; style++)
                for (int band = 0; band < 3; band++)
                    materials[style, band] = GauzeMaterial(style, representativeRealms[band]);
            return materials;
        }

        private static Material GauzeMaterial(int style, int realm)
        {
            string path = XianxuAppearanceRules.TexturePath(style, realm);
            // The user approved the golden gauze as-is; only correct cyan/violet.
            if (LightningPalette.Band(realm) == 2)
                return MaterialPool.MatFrom(path, ShaderDatabase.TransparentPostLight);
            Texture2D source = ContentFinder<Texture2D>.Get(path);
            // Imported textures need not be CPU-readable. Copy through a render target
            // once at material creation; never read pixels or allocate textures per frame.
            RenderTexture previous = RenderTexture.active;
            RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            var gauze = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                gauze.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                Color[] pixels = gauze.GetPixels();
                for (int y = 0; y < source.height; y++)
                    for (int x = 0; x < source.width; x++)
                    {
                        int index = y * source.width + x;
                        pixels[index] = XianxuAppearanceRules.GauzePixel(pixels[index],
                            XianxuAppearanceRules.IsGemstone(style, (x + .5f) / source.width, 1f - (y + .5f) / source.height));
                    }
                gauze.SetPixels(pixels);
                gauze.Apply();
                gauze.name = path + "_Gauze";
                return MaterialPool.MatFrom(gauze, ShaderDatabase.TransparentPostLight, Color.white);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
            }
        }

        private static Mesh Half(bool right)
        {
            float x0 = right ? 0f : -.5f, x1 = right ? .5f : 0f;
            float u0 = right ? .5f : 0f, u1 = right ? 1f : .5f;
            var mesh = new Mesh { name = right ? "XianxuRight" : "XianxuLeft" };
            mesh.vertices = new[] { new Vector3(x0, 0, -.5f), new Vector3(x0, 0, .5f),
                new Vector3(x1, 0, .5f), new Vector3(x1, 0, -.5f) };
            mesh.uv = new[] { new Vector2(u0, 0), new Vector2(u0, 1), new Vector2(u1, 1), new Vector2(u1, 0) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        internal static void Draw(Pawn pawn, bool cloudLattice, bool alwaysVisible, float scale, float offsetZ)
        {
            int style = XianxuAppearanceRules.StyleIndex(cloudLattice, alwaysVisible, pawn.Drafted);
            // Resolve the current wearer every draw: advancement, save loading,
            // or handing the artifact to another pawn must never retain a stale tint.
            int band = LightningPalette.Band(LightningRealm.Of(pawn));
            Rot4 facing = pawn.Rotation;
            float phase = ((Find.TickManager?.TicksGame ?? 0) % 36000) * .025f + pawn.thingIDNumber;
            // Anchor each sprite's upper gemstone to the garment shoulder. A/C and
            // the folded clasps have different UV positions but share one attachment.
            for (int i = 0; i < 2; i++)
            {
                bool far = facing == Rot4.East ? i == 0 : facing == Rot4.West && i == 1;
                // In profile the body occludes the opposite clasp and its entire veil.
                if (far) continue;
                float side = i == 0 ? -1f : 1f;
                float width = facing.IsHorizontal ? .48f : 1f;
                float gemX = side * (style == 2 ? .15f : .22f);
                float gemZ = .5f - (style == 2 ? .277f : .285f);
                Vector3 loc = pawn.DrawPos;
                loc.y += .045f;
                float shoulderX = side * .29f;
                float shoulderZ = .04f + offsetZ + .08f;
                if (facing.IsHorizontal)
                {
                    shoulderX = (facing == Rot4.East ? -1f : 1f) * .03f;
                    shoulderZ -= .025f;
                }
                loc.x += shoulderX - gemX * scale * width;
                loc.z += shoulderZ - gemZ * scale;
                // Sway around the clasp so the physical attachment never floats off the robe.
                float sway = style == 0 ? 0f : Mathf.Sin(phase + i * .45f) * .018f;
                var matrix = Matrix4x4.TRS(loc, Quaternion.identity, new Vector3(scale * width, 1f, scale));
                matrix *= Matrix4x4.Translate(new Vector3(gemX, 0f, gemZ)) *
                    Matrix4x4.Rotate(Quaternion.AngleAxis(sway * Mathf.Rad2Deg, Vector3.up)) *
                    Matrix4x4.Translate(new Vector3(-gemX, 0f, -gemZ));
                Tint.SetColor(ShaderPropertyIDs.Color, new Color(1.16f, 1.16f, 1.16f, 1.08f));
                Graphics.DrawMesh(Halves[i], matrix, Materials[style, band], 0, null, 0, Tint);
            }
        }
    }
}
