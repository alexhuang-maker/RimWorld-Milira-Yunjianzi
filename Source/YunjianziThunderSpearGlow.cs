using UnityEngine;
using Verse;

namespace Yunjianzi
{
    [StaticConstructorOnStartup]
    internal static class ThunderSpearGlow
    {
        // Modest, uniform enlargement for the charging and planted silhouettes.
        internal const float ChargeAndAnchorScale = 1.15f;
        private static readonly MaterialPropertyBlock[] Palettes = { CreateProperties(2), CreateProperties(3), CreateProperties(4) };
        internal static MaterialPropertyBlock ForRealm(int realm) => Palettes[LightningPalette.Band(realm)];

        private static MaterialPropertyBlock CreateProperties(int realm)
        {
            // MaterialPool rounds colours through Color32, so apply the modest
            // emissive gain per draw instead of clipping it or mutating pooled materials.
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetColor(ShaderPropertyIDs.Color, LightningPalette.TextureTint(realm));
            return properties;
        }
    }
}
