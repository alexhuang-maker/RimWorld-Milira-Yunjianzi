using System;

namespace Yunjianzi
{
    // One unbroken surface above the wing render layers. The rearward position
    // is expressed in the silhouette, not by sinking it under the whole pawn.
    // Coordinates use the same 512px canvas as both existing texture sets.
    internal static class XianxuDrape
    {
        internal const int Rows = 64;

        private static float Shoulder(int rotation, float canvasY)
        {
            float start = rotation == 2 || rotation == 0 ? 100f : 130f;
            float end = rotation == 2 || rotation == 0 ? 280f : 310f;
            float t = Math.Max(0f, Math.Min(1f, (canvasY - start) / (end - start)));
            return t * t * (3f - 2f * t);
        }

        private static void Row(int rotation, float canvasY, out float shift, out float lift)
        {
            float shoulder = Shoulder(rotation, canvasY);
            // Restore the higher crown from the accepted reference version.
            // Fade into the original hem instead of moving the complete accessory.
            lift = .30f * (1f - shoulder);
            float behind = .18f + (.045f - .18f) * shoulder;
            shift = rotation == 1 ? -behind : rotation == 3 ? behind : 0f;
        }

        internal static void Position(int rotation, float canvasX, float canvasY, float scale,
            out float x, out float depth, out float z)
        {
            // Interpolate the exact rows of the cached mesh, so sparks follow
            // its surface, including the raised crown and rearward side offset.
            float row = Math.Max(0f, Math.Min(Rows, canvasY * Rows / 512f));
            int lower = Math.Min((int)row, Rows - 1);
            float t = row - lower;
            float shift0, lift0, shift1, lift1;
            Row(rotation, lower * 512f / Rows, out shift0, out lift0);
            Row(rotation, (lower + 1) * 512f / Rows, out shift1, out lift1);
            float shift = shift0 + (shift1 - shift0) * t;
            // Wings use render-tree layer 60 (about .022 world altitude).
            // Retain the proven tail height for the whole mesh: a slope from
            // negative to positive depth intersects their flat mesh and makes
            // a visible horizontal cut even with continuous UVs.
            depth = .285f;
            x = (canvasX / 512f - .5f + shift) * scale;
            z = (.5f - canvasY / 512f + lift0 + (lift1 - lift0) * t) * scale;
        }
    }
}
