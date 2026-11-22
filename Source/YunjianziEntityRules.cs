using System;
using System.Collections.Generic;

namespace Yunjianzi
{
    // Pure policy shared by runtime generation and the boundary regression checks.
    public static class YunjianziEntityRules
    {
        public static bool Eligible(bool scenario, bool alive, int realm, int tier, bool finale)
            => scenario && alive && (tier == 1 ? realm >= 3 : tier == 2 ? realm >= 4 : tier == 3 && realm >= 4 && finale);

        public static List<int> Compose(float points, int realm, bool boss, Func<int,int> choose)
        {
            var result = new List<int>();
            if (float.IsNaN(points) || float.IsInfinity(points) || points < 0 || realm < 3) return result;
            // Boss budget belongs to the explicit quest wave, never a random group.
            if (boss && realm >= 4) result.Add(7);
            float remaining = points;
            int mediums = 0;
            while (remaining >= MinimumCost && result.Count < 40)
            {
                if (realm >= 4 && remaining >= 1500 && mediums < (realm >= 5 ? 2 : 1))
                {
                    int id = 5 + choose(2);
                    if (result.Contains(id)) id = id == 5 ? 6 : 5;
                    result.Add(id); remaining -= Costs[id]; mediums++; continue;
                }
                int small = choose(5);
                int cost = Costs[small];
                if (remaining < cost) { small = 0; cost = MinimumCost; }
                result.Add(small); remaining -= cost;
            }
            return result;
        }
        public const int MinimumCost=250;
        public static readonly int[] Costs = {250,320,280,300,260,1000,1100,2400};
    }
}
