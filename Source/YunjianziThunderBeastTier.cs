using Verse;

namespace Yunjianzi
{
    internal static class ThunderBeastTier
    {
        internal static PawnKindDef ForRealm(int realm) => (realm <= 2
            ? YJZDefOf.YJZ_ThunderBeast_Weak
            : realm == 3 ? YJZDefOf.YJZ_ThunderBeast : YJZDefOf.YJZ_ThunderBeast_Strong) ?? YJZDefOf.YJZ_ThunderBeast;

        internal static bool IsElemental(PawnKindDef kind) => kind != null &&
            (kind == YJZDefOf.YJZ_ThunderBeast_Weak || kind == YJZDefOf.YJZ_ThunderBeast ||
             kind == YJZDefOf.YJZ_ThunderBeast_Strong);
    }
}
