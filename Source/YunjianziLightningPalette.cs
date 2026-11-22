using UnityEngine;

namespace Yunjianzi
{
    internal static class LightningPalette
    {
        internal static int Band(int realm) => realm <= 2 ? 0 : realm == 3 ? 1 : 2;
        internal static Color Edge(int realm) => realm <= 2 ? new Color(.12f,.9f,1f) :
            realm == 3 ? new Color(.67f,.34f,1f) : new Color(1f,.76f,.12f);
        internal static Color Core(int realm) => realm <= 2 ? new Color(.9f,1f,1f) :
            realm == 3 ? new Color(1f,.97f,1f) : new Color(1f,.98f,.86f);
        internal static Color Glow(int realm, float alpha)
        {
            Color c = realm == 3 ? new Color(.78f,.46f,1f) : Edge(realm);
            c.a = alpha;
            return c;
        }
        // Multiplicative tint for existing purple spear/claw/warp artwork.
        // No extra particles, exposure or additive full-screen layers.
        internal static Color TextureTint(int realm) => realm <= 2 ? new Color(.25f,1.45f,1.12f) :
            realm == 3 ? new Color(1.12f,1.12f,1.12f) : new Color(1.35f,1.65f,.14f);
    }
}
