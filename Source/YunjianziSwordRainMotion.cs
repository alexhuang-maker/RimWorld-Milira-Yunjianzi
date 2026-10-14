using System;

namespace Yunjianzi
{
    // PawnFlyer feeds AdjustedProgress into both horizontal travel and GetHeight.
    // Reserve narrow travel bands for the almost vertical takeoff and final slam.
    internal static class SwordRainMotion
    {
        internal static float Progress(float time)
        {
            float t = Clamp(time);
            if (t <= .3f) return .01f * t / .3f;
            if (t <= .72f) return .01f + .97f * Smooth((t - .3f) / .42f);
            if (t <= .8f) return .98f + .01f * (t - .72f) / .08f;
            return .99f + .01f * (t - .8f) / .2f;
        }

        internal static float Height(float progress)
        {
            float p = Clamp(progress);
            if (p < .01f) return 1f - (1f - p / .01f) * (1f - p / .01f);
            if (p <= .99f) return 1f;
            float fall = (p - .99f) / .01f;
            return Clamp(1f - fall * fall);
        }

        private static float Smooth(float t) { return t * t * (3f - 2f * t); }
        private static float Clamp(float t) { return Math.Max(0f, Math.Min(1f, t)); }
    }
}
