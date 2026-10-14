using System;

namespace Yunjianzi
{
    internal static class ZhuxieGlowTuning
    {
        internal static float Progress(int realm)
        {
            // Uninitiated/Qi use the robe's soft glow; Huashen reaches the
            // activated silk's post-light pass plus its average additive glow.
            return (Math.Max(1, Math.Min(5, realm)) - 1) / 4f;
        }

        internal static float LitAlpha(int realm) { return Progress(realm); }
        internal static float GlowAlpha(int realm) { return .26f + .08f * Progress(realm); }

        private static float Smooth(float low, float high, float value)
        {
            float t = Math.Max(0f, Math.Min(1f, (value - low) / (high - low)));
            return t * t * (3f - 2f * t);
        }

        internal static byte MaskAlpha(byte red, byte green, byte blue, byte alpha)
        {
            if (alpha == 0) return 0;
            float r = red / 255f, g = green / 255f, b = blue / 255f;
            // The blade's engraved lines are much paler cyan than the robe's
            // runes. Select their hue contrast, not the neutral silver blade.
            float cyan = 0f;
            if (g > r && b > r && g >= b * .65f && b >= g * .62f)
                cyan = Smooth(.025f, .12f, Math.Min(g, b) - r)
                    * Smooth(.32f, .62f, Math.Min(g, b));
            float gold = 0f;
            if (r >= g && g > b && g >= r * .55f)
                gold = Smooth(.08f, .24f, r - b) * Smooth(.025f, .12f, g - b)
                    * Smooth(.38f, .72f, r) * (.20f / .26f);
            return (byte)Math.Round(alpha * Math.Max(cyan, gold));
        }
    }
}
