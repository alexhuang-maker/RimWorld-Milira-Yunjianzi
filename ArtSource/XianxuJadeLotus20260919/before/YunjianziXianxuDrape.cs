using System;

namespace Yunjianzi
{
    // One unbroken surface: rear crown, shoulder turn, foreground tails.
    // Coordinates use the same 512px canvas as both existing texture sets.
    internal static class XianxuDrape
    {
        internal const int Rows = 64;

        private static float Shoulder(int rotation, float canvasY)
        {
            float start = rotation == 2 ? 170f : 205f;
            float end = rotation == 2 ? 245f : 290f;
            float t = Math.Max(0f, Math.Min(1f, (canvasY - start) / (end - start)));
            return t * t * (3f - 2f * t);
        }

        private static void Row(int rotation, float canvasY, out float shift, out float depth)
        {
            float shoulder = Shoulder(rotation, canvasY);
            depth = rotation == 0 ? .285f : -.035f + .320f * shoulder;
            float behind = .18f + (.045f - .18f) * shoulder;
            shift = rotation == 1 ? -behind : rotation == 3 ? behind : 0f;
        }

        internal static void Position(int rotation, float canvasX, float canvasY, float scale,
            out float x, out float depth, out float z)
        {
            // Interpolate the exact rows of the cached mesh, so sparks follow
            // its surface, including the shoulder transition and side offset.
            float row = Math.Max(0f, Math.Min(Rows, canvasY * Rows / 512f));
            int lower = Math.Min((int)row, Rows - 1);
            float t = row - lower;
            Row(rotation, lower * 512f / Rows, out float shift0, out float depth0);
            Row(rotation, (lower + 1) * 512f / Rows, out float shift1, out float depth1);
            float shift = shift0 + (shift1 - shift0) * t;
            depth = depth0 + (depth1 - depth0) * t;
            x = (canvasX / 512f - .5f + shift) * scale;
            z = (.5f - canvasY / 512f) * scale;
        }
    }
}
