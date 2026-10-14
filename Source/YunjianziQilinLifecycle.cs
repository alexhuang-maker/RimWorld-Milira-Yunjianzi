using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    public sealed partial class CompYunjianziCultivation
    {
        internal const int SkyWyvernLossCooldown = 10 * 60000;
        private int skyWyvernReadyTick;
        private int SkyWyvernCooldownRemaining => Mathf.Max(0, skyWyvernReadyTick - Find.TickManager.TicksGame);
        private string SkyWyvernCooldownText => (SkyWyvernCooldownRemaining / 60000f).ToString("0.00") + "天";

        private void ExposeQilinSummonData()
        {
            Scribe_Values.Look(ref skyWyvernReadyTick, "yjz_skyWyvernReadyTick", 0);
        }

        private bool CanCreateOrAscendSkyWyvern(List<Pawn> existing)
        {
            if (existing.Count == 0) return true;
            if (existing.Count != 1) return false;
            CompYunjianziSkyWyvern companion = existing[0].TryGetComp<CompYunjianziSkyWyvern>();
            return companion != null && companion.IsBoundTo(Pawn) && companion.Stage < Mathf.Min(Realm, 5);
        }

        internal void NotifySkyWyvernLost()
        {
            skyWyvernReadyTick = Mathf.Max(skyWyvernReadyTick, Find.TickManager.TicksGame + SkyWyvernLossCooldown);
        }
    }

    public sealed partial class CompYunjianziSkyWyvern
    {
        private bool lossNotified;
        private bool dismissedWithoutLoss;

        private void ExposeQilinLossData()
        {
            Scribe_Values.Look(ref lossNotified, "yjz_qilinLossNotified", false);
        }

        internal void DismissWithoutLossCooldown()
        {
            // Successful ascension or a failed initial binding is not a death.
            dismissedWithoutLoss = true;
            parent.Destroy(DestroyMode.Vanish);
        }

        public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
        {
            base.Notify_Killed(prevMap, dinfo);
            NotifyLossOnce();
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            // Also cover destruction that bypasses Pawn.Kill. Ordinary map
            // departure uses DeSpawn and must not start the loss cooldown.
            NotifyLossOnce();
        }

        private void NotifyLossOnce()
        {
            if (dismissedWithoutLoss || lossNotified) return;
            lossNotified = true;
            Owner?.TryGetComp<CompYunjianziCultivation>()?.NotifySkyWyvernLost();
        }
    }
}
