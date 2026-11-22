using Verse;

namespace Yunjianzi
{
    internal static class LightningRealm
    {
        internal static int Of(Pawn pawn)
        {
            if (pawn == null) return 3; // Legacy unowned visuals keep their purple.
            var cultivation = pawn.TryGetComp<CompYunjianziCultivation>();
            if (cultivation != null) return cultivation.Realm;
            var qilin = pawn.TryGetComp<CompYunjianziSkyWyvern>();
            if (qilin != null) return qilin.Stage;
            var beast = pawn.TryGetComp<CompThunderBeast>();
            return beast != null ? beast.VisualRealm : 3;
        }
    }
}
