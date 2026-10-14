using System;

namespace Yunjianzi
{
    // Keep combat-entry detection independent of rendering and pawn APIs.
    internal sealed class QilinBattleRoarState
    {
        internal const int DurationTicks = 900;
        internal const int CooldownTicks = 3600;
        public bool WasInCombat;
        public int ReadyTick;

        internal bool TryEnterCombat(int now, bool inCombat, bool canRoar)
        {
            bool entered = inCombat && !WasInCombat;
            WasInCombat = inCombat;
            if (!entered || !canRoar || now < ReadyTick) return false;
            ReadyTick = now + CooldownTicks;
            return true;
        }

        internal static int AttackInterval(int normalTicks, bool empowered)
        {
            // 50% more attacks means two thirds of the recovery time.
            return Math.Max(1, empowered ? (normalTicks * 2 + 2) / 3 : normalTicks);
        }
    }
}
