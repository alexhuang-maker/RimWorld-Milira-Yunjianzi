using UnityEngine;
using Verse;

namespace Yunjianzi
{
    [StaticConstructorOnStartup]
    internal static class ThunderSpearGlow
    {
        // Modest, uniform enlargement for the charging and planted silhouettes.
        internal const float ChargeAndAnchorScale = 1.15f;
        internal static readonly MaterialPropertyBlock Properties = CreateProperties();

        private static MaterialPropertyBlock CreateProperties()
        {
            // MaterialPool rounds colours through Color32, so apply the modest
            // emissive gain per draw instead of clipping it or mutating pooled materials.
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetColor(ShaderPropertyIDs.Color, new Color(1.12f, 1.12f, 1.12f, 1f));
            return properties;
        }
    }
}
