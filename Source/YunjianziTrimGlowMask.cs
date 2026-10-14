using System;

namespace Yunjianzi
{
    // Color selection for a runtime emission pass, never a replacement sprite.
    internal static class YunjianziTrimGlowMask
    {
        private static float Smooth(float low, float high, float value)
        {
            float t = Math.Max(0f, Math.Min(1f, (value - low) / (high - low)));
            return t * t * (3f - 2f * t);
        }

        internal static byte Alpha(byte red, byte green, byte blue, byte alpha, bool hair)
        {
            if (alpha == 0) return 0;
            float r = red / 255f, g = green / 255f, b = blue / 255f;
            float gold = 0f, cyan = 0f;
            if (r >= g && g > b && g >= r * .55f)
                gold = Smooth(.14f, .38f, r - b) * Smooth(.05f, .18f, g - b)
                    * Smooth(.38f, .72f, r) * .20f;
            if (g > r && b > r && g >= b * .65f && b >= g * .62f)
            {
                float saturation = (Math.Max(g, b) - r) / Math.Max(g, b);
                // Stronger saturation gate on hair leaves pale blue-white locks
                // alone while retaining the gold fittings and cyan gemstones.
                cyan = Smooth(hair ? .56f : .30f, hair ? .78f : .64f, saturation)
                    * Smooth(.30f, .62f, Math.Min(g, b)) * .26f;
            }
            return (byte)Math.Round(alpha * Math.Max(gold, cyan));
        }
    }
}
