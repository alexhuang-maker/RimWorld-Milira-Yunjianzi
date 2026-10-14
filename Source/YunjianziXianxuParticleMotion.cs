using System;

namespace Yunjianzi
{
    // Coordinates are on the existing 512px elemental accessory canvas.
    // Both particle lanes use one path and one clock; reflection is exact.
    internal static class XianxuParticleMotion
    {
        internal const int SparksPerLane = 10;
        internal const int FlowsPerLane = 2;
        internal const int SamplesPerFlow = 5;

        internal static float GlowAlpha(int ticks, int pawnId)
        {
            long clock = (long)ticks + pawnId;
            return .34f + .10f * (float)Math.Sin((clock % 120) * (Math.PI * 2.0 / 120.0));
        }

        internal static float FlowProgress(int ticks, int pawnId, int flow)
        {
            long clock = (long)ticks + (long)pawnId * 13 + flow * 60;
            // Upstream: trails run from the hanging ends toward the lotus.
            return 1f - (clock % 120) / 120f;
        }
        private static readonly float[,] Frontal = {
            {256,25}, {243,49}, {228,73}, {225,99}, {195,107},
            {151,117}, {105,113}, {110,149}, {151,204}, {168,250},
            {140,286}, {105,327}, {85,370}, {94,406}, {128,451}, {137,482}
        };
        private static readonly float[,] Side = {
            {257,29}, {235,50}, {214,77}, {221,105}, {238,135},
            {259,159}, {279,188}, {281,221}, {272,255}, {281,295},
            {309,331}, {332,368}, {337,396}, {327,423}, {305,453}
        };

        internal static void Point(int rotation, bool mirrored, float progress, out float x, out float y)
        {
            bool side = rotation == 1 || rotation == 3;
            float[,] path = side ? Side : Frontal;
            float index = Math.Max(0f, Math.Min(1f, progress)) * (path.GetLength(0) - 1);
            int segment = Math.Min((int)index, path.GetLength(0) - 2);
            float t = index - segment;
            x = path[segment,0] + (path[segment+1,0] - path[segment,0]) * t;
            y = path[segment,1] + (path[segment+1,1] - path[segment,1]) * t;
            if (side ? rotation == 3 : mirrored) x = 512f - x;
        }

        internal static void Spark(int ticks, int pawnId, int slot, out float progress, out float age)
        {
            long clock = (long)ticks + (long)pawnId * 13 + slot * 11;
            age = (clock % 54) / 54f;
            progress = (float)((slot * .173 + (clock / 54) * .191) % 1.0);
        }

        internal static void Drift(int rotation, bool mirrored, int slot, float age, out float x, out float y)
        {
            bool side = rotation == 1 || rotation == 3;
            float outward = side ? (rotation == 1 ? 1f : -1f) : (mirrored ? 1f : -1f);
            float fan = (slot % 3) / 2f;
            // Canvas y decreases upwards. Different slopes make a fan, with
            // identical timing and reflected displacement on the opposite lane.
            x = outward * age * (24f + 20f * fan);
            y = -age * (58f - 18f * fan);
        }
    }
}
