using System;

namespace Yunjianzi
{
    // Coordinates are on the existing 512px elemental accessory canvas.
    // Both particle lanes use one path and one clock; reflection is exact.
    internal static class XianxuParticleMotion
    {
        internal const int SparksPerLane = 7;
        private static readonly float[,] Frontal = {
            {256,29}, {230,36}, {202,67}, {172,99}, {137,106},
            {103,103}, {98,126}, {129,164}, {155,211}, {162,250},
            {140,286}, {105,327}, {85,370}, {94,406}, {128,451}, {137,482}
        };
        private static readonly float[,] Side = {
            {201,34}, {187,58}, {196,86}, {226,111}, {249,146},
            {279,189}, {282,223}, {278,259}, {290,298},
            {320,342}, {338,379}, {337,410}, {318,447}
        };

        internal static bool InFront(int rotation, float canvasY)
        {
            return rotation != 2 || canvasY >= 146f;
        }

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
    }
}
