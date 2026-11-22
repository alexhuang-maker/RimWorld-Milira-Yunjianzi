using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    // Recolour the existing purple artwork once per graphic, preserving its
    // alpha and shading. Multiplying purple by gold alone makes red edges.
    public sealed class Graphic_ThunderElemental : Graphic_Multi
    {
        private Material[] elementalMaterials;
        public override Material MatNorth => elementalMaterials?[0] ?? base.MatNorth;
        public override Material MatEast => elementalMaterials?[1] ?? base.MatEast;
        public override Material MatSouth => elementalMaterials?[2] ?? base.MatSouth;
        public override Material MatWest => elementalMaterials?[3] ?? base.MatWest;

        public override void Init(GraphicRequest request)
        {
            base.Init(request);
            var originals = new[] { base.MatNorth, base.MatEast, base.MatSouth, base.MatWest };
            elementalMaterials = new Material[4];
            var cache = new Dictionary<Texture2D, Material>();
            for (int i = 0; i < originals.Length; i++)
            {
                var source = originals[i].mainTexture as Texture2D;
                if (source == null || originals[i] == BaseContent.BadMat)
                {
                    elementalMaterials[i] = originals[i];
                    continue;
                }
                if (!cache.TryGetValue(source, out Material material))
                {
                    var texture = YunjianziTrimGlow.CreateMask(source, pixel =>
                    {
                        Color original = pixel;
                        float value = Mathf.Max(original.r, Mathf.Max(original.g, original.b));
                        float whiteCore = Mathf.Min(original.r, Mathf.Min(original.g, original.b));
                        Color result = Color.Lerp(request.color, Color.white, Mathf.Pow(whiteCore, 2f));
                        result.r *= value;
                        result.g *= value;
                        result.b *= value;
                        result.a = original.a;
                        return result;
                    });
                    material = texture == null ? originals[i] : MaterialPool.MatFrom(texture, request.shader, Color.white);
                    cache.Add(source, material);
                }
                elementalMaterials[i] = material;
            }
        }

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo) =>
            GraphicDatabase.Get<Graphic_ThunderElemental>(path, newShader, drawSize, newColor, newColorTwo, data, maskPath);

        public override void TryInsertIntoAtlas(TextureAtlasGroup groupKey)
        {
            foreach (Material material in elementalMaterials)
                GlobalTextureAtlasManager.TryInsertStatic(groupKey, (Texture2D)material.mainTexture, null);
        }
    }
}
