using System;

namespace Yunjianzi
{
    // Coordinates are on the existing 512px elemental accessory canvas.
    // Both particle lanes use one path and one clock; reflection is exact.
    internal static class XianxuParticleMotion
    {
        internal const int SparksPerLane = 7;
        private static readonly float[,] Frontal = {
            {256,25}, {224,46}, {190,87}, {158,92}, {105,75},
            {89,109}, {133,146}, {180,182}, {153,216}, {158,260},
            {105,302}, {63,337}, {96,368}, {158,411}, {124,458}, {84,486}
        };
        private static readonly float[,] Side = {
            {184,30}, {175,57}, {209,93}, {257,133}, {279,178},
            {265,217}, {261,248}, {283,288}, {287,321},
            {271,361}, {283,412}, {316,440}, {329,423}
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
