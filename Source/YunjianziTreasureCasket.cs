using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Yunjianzi
{
    /// <summary>
    /// A one-cell storage building whose capacity is measured in individual
    /// items instead of RimWorld's usual number-of-stacks limit.
    /// </summary>
    public class Building_TreasureCasket : Building_Storage
    {
        public const int ItemCapacity = 1000;

        public int StoredItemCount
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
                            count += thing.stackCount;
                        }
                    }
                }

                return count;
            }
        }

        public int RemainingItemCapacity => Math.Max(0, ItemCapacity - StoredItemCount);
    }

    /// <summary>
    /// Building_Storage.Accepts and SpaceRemainingFor are sealed in RimWorld
    /// 1.6, so the remaining item count is injected at the same two points
    /// used by the vanilla hauling search. No ticking or periodic scan is
    /// needed: the one-cell inventory is counted only when storage is queried.
    /// </summary>
    [HarmonyPatch(typeof(Building_Storage), nameof(Building_Storage.SpaceRemainingFor))]
    public static class Patch_TreasureCasket_SpaceRemainingFor
    {
        public static void Postfix(Building_Storage __instance, ref int __result)
        {
            if (__instance is Building_TreasureCasket casket)
            {
                __result = Math.Min(__result, casket.RemainingItemCapacity);
            }
        }
    }

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

            string capacityText = $"百宝匣容量：{casket.StoredItemCount} / {Building_TreasureCasket.ItemCapacity}";
            __result = __result.NullOrEmpty() ? capacityText : __result + "\n" + capacityText;
        }
    }

    [HarmonyPatch(typeof(StoreUtility), "TryFindBestBetterStoreCellForWorker")]
    public static class Patch_TreasureCasket_StorageSearch
    {
        public static bool Prefix(ISlotGroup slotGroup)
        {
            return !(slotGroup?.Settings?.owner is Building_TreasureCasket casket)
                || casket.RemainingItemCapacity > 0;
        }
    }
}
