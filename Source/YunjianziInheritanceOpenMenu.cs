using HarmonyLib;
using RimWorld;
using Verse;

namespace Yunjianzi
{
    // IOpenable also feeds vanilla's OpenThing provider. These two buildings
    // already supply one named single/multiselect action and a sealed-state
    // explanation; suppress only their duplicate generic opening action.
    [HarmonyPatch(typeof(FloatMenuOptionProvider_OpenThing), "GetSingleOptionFor")]
    internal static class Patch_InheritanceOpenMenu
    {
        static bool Prefix(Thing clickedThing, ref FloatMenuOption __result)
        {
            if (!(clickedThing is Building_YunjianziInheritanceChest) &&
                !(clickedThing is Building_YunjianziStartingShenjiArk)) return true;
            __result = null;
            return false;
        }
    }
}
