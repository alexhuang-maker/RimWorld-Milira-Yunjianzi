using System;

namespace Yunjianzi
{
    internal static class SwordRainEchoMotion
    {
        internal const int StrokeCount = 8;
        internal const int StrokeTicks = 18;
        internal const int Duration = StrokeCount * StrokeTicks;

        internal static float RadiusForRealm(int realm, bool goldenUpgrade) =>
            3.9f + Math.Max(0, Math.Min(5, realm) - 2) + (goldenUpgrade ? 1.5f : 0f);
        internal static float SlashScale(float radius) => Math.Max(1f, radius / 3.9f);
        internal static float HitHalfWidth(float radius) => .85f * SlashScale(radius);

        internal static float Progress(int ticks) => Math.Max(0f, Math.Min(1f, ticks / (float)StrokeTicks));
        internal static float DamagePerStroke(int total) => Math.Max(1, total) / (float)StrokeCount;
        internal static float Opacity(int ticks) => Math.Max(0f, Math.Min(1f, Math.Min(ticks / 2f, (StrokeTicks - ticks) / 3f)));

        // Intersect a ray from inside the arena with its circular boundary.
        internal static void EndPoint(float x, float z, float dx, float dz, float radius, out float endX, out float endZ)
        {
            float length = (float)Math.Sqrt(dx * dx + dz * dz);
            if (length < .0001f) { dx = 1f; dz = 0f; length = 1f; }
            dx /= length;
            dz /= length;
            float dot = x * dx + z * dz;
            float distance = -dot + (float)Math.Sqrt(Math.Max(0f, dot * dot + radius * radius - x * x - z * z));
            endX = x + dx * distance;
            endZ = z + dz * distance;
        }

        internal static float DistanceSquared(float x, float z, float ax, float az, float bx, float bz)
        {
            float dx = bx - ax, dz = bz - az;
            float length = dx * dx + dz * dz;
            float t = length < .000001f ? 0f : Math.Max(0f, Math.Min(1f, ((x - ax) * dx + (z - az) * dz) / length));
            float ex = x - ax - t * dx, ez = z - az - t * dz;
            return ex * ex + ez * ez;
        }
    }
}
