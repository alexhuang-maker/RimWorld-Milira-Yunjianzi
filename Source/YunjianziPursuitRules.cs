using System;
namespace Yunjianzi
{
    // Deterministic transition rules shared with the regression harness.
    public static class YunjianziPursuitRules
    {
        public static int RemainingHealth(int health, float damage, bool defending, bool complete)
        {
            if(defending || complete || damage<=0 || float.IsNaN(damage))return health;
            return damage>=health?0:Math.Max(0,health-(int)Math.Ceiling(damage));
        }
        public static bool DefenseComplete(int now,int until,bool playerPresent,bool allWavesSucceeded,bool activeThreat)
            => playerPresent && now>=until && allWavesSucceeded && !activeThreat;
        public static int LockTick(int start,bool surfaceDone) => start+(surfaceDone?20:10)*60000;
        public static int WarningTick(int start,bool surfaceDone) => LockTick(start,surfaceDone)-60000;
    }
}
