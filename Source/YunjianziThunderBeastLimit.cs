using System.Linq;
using Verse;

namespace Yunjianzi
{
    internal static class ThunderBeastLimit
    {
        internal const int MaximumPerMap = 20;

        // A map-wide limit shared by every summoner. No camera-dependent counts
        // or cached reservations, so deaths/despawns immediately free a slot.
        internal static int Enforce(Map map)
        {
            if (map == null || YJZDefOf.YJZ_ThunderBeast == null) return 0;
            var beasts = map.mapPawns.AllPawnsSpawned
                .Where(p => p.Spawned && p.Map == map && !p.Dead && !p.Destroyed &&
                    p.kindDef == YJZDefOf.YJZ_ThunderBeast)
                .OrderBy(p => p.thingIDNumber).ToList();
            // Stable ordering also repairs old saves and out-of-band spawns.
            // Normal destruction preserves the existing one-time energy refund.
            for (int i = MaximumPerMap; i < beasts.Count; i++)
                beasts[i].Destroy(DestroyMode.Vanish);
            return System.Math.Min(beasts.Count, MaximumPerMap);
        }
    }
}
