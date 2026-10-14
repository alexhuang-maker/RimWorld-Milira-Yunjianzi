using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Yunjianzi
{
    /// <summary>
    /// A 3x3 high-capacity storage building. Capacity is governed by the
    /// vanilla maxItemsInCell setting on its ThingDef (112 x 9 cells).
    /// </summary>
    public class Building_TreasureCasket : Building_Storage
    {
        public const int StackCapacity = 1008;

        public int StoredStackCount
        {
            get
            {
                if (!Spawned || Map == null)
                {
                    return 0;
                }

                int count = 0;
                foreach (IntVec3 cell in GenAdj.OccupiedRect(Position, Rotation, def.Size))
                {
                    if (!cell.InBounds(Map))
                    {
                        continue;
                    }

                    var things = cell.GetThingList(Map);
                    for (int i = 0; i < things.Count; i++)
                    {
                        Thing thing = things[i];
                        if (thing != this && thing.def.category == ThingCategory.Item)
                        {
                            count++;
                        }
                    }
                }

                return count;
            }
        }
    }

    /// <summary>
    /// Items stored in the casket remain ordinary spawned map items so vanilla
    /// hauling, selection and storage logic keep working, but their map graphic
    /// and stack-count overlay are suppressed. This makes them appear to be
    /// inside the casket instead of piled on its lid.
    /// </summary>
    public static class TreasureCasketDrawUtility
    {
        public static bool IsStoredInCasket(Thing thing)
        {
            if (thing == null || thing.def.category != ThingCategory.Item || !thing.Spawned)
            {
                return false;
            }

            return thing.GetSlotGroup()?.parent is Building_TreasureCasket;
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.Print))]
    public static class Patch_TreasureCasket_HidePrintedItems
    {
        public static bool Prefix(Thing __instance)
        {
            return !TreasureCasketDrawUtility.IsStoredInCasket(__instance);
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.DynamicDrawPhaseAt))]
    public static class Patch_TreasureCasket_HideRealtimeItems
    {
        public static bool Prefix(Thing __instance)
        {
            return !TreasureCasketDrawUtility.IsStoredInCasket(__instance);
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.DrawGUIOverlay))]
    public static class Patch_TreasureCasket_HideItemLabels
    {
        public static bool Prefix(Thing __instance)
        {
            return !TreasureCasketDrawUtility.IsStoredInCasket(__instance);
        }
    }

    /// <summary>
    // Adaptive Storage Framework replaces Building_Storage.GetInspectString
    // with a prefix. A postfix remains compatible with both that replacement
    // and the unmodified vanilla method, so the true item-count limit is
    // always visible without requiring Adaptive Storage as a dependency.
    [HarmonyPatch(typeof(Building_Storage), nameof(Building_Storage.GetInspectString))]
    public static class Patch_TreasureCasket_InspectString
    {
        public static void Postfix(Building_Storage __instance, ref string __result)
        {
            if (!(__instance is Building_TreasureCasket casket))
            {
                return;
            }

            string capacityText = $"百宝匣堆叠位：{casket.StoredStackCount} / {Building_TreasureCasket.StackCapacity}";
            __result = __result.NullOrEmpty() ? capacityText : __result + "\n" + capacityText;
        }
    }
}
