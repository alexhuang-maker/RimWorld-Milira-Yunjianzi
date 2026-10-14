using RimWorld;
using Verse;

namespace Yunjianzi
{
    [DefOf]
    public static class YJZGuzhengDefOf
    {
        public static ThingDef YJZ_Building_Guzheng;

        static YJZGuzhengDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(YJZGuzhengDefOf));
        }
    }

    public sealed class ThoughtWorker_ListeningToGuzheng : ThoughtWorker_MusicalInstrumentListeningBase
    {
        protected override ThingDef InstrumentDef => YJZGuzhengDefOf.YJZ_Building_Guzheng;
    }
}
