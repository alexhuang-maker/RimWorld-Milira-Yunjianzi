using System.Linq;
using RimWorld;
using Verse;

namespace Yunjianzi
{
    internal static class YunjianziMemorialSupplies
    {
        private static readonly string[] MaterialNames = {
            "Steel", "Plasteel", "ComponentIndustrial", "Jade", "YJZ_TrueJadeSteel", "ComponentSpacer", "GravlitePanel"
        };

        // Runs only during initial map generation. Move the scenario's actual
        // stacks, so loading a save never recreates or redistributes supplies.
        internal static void Distribute(Map map, IntVec3 shrine)
        {
            var shelves = YunjianziMemorialLayout.Plan.furniture.Where(f => f.def.defName == "Shelf")
                .Select(f => (shrine + f.offset).GetEdifice(map)).OfType<Building_Storage>().ToList();
            if (shelves.Count == 0) { Log.Error("[Yunjianzi] Memorial supply shelves missing."); return; }
            var apron = CellRect.CenteredOn(GenStep_YunjianziOrbitalFoundation.Dock(map), 27, 27);
            var supplies = map.listerThings.AllThings.Where(t => t.def.category == ThingCategory.Item &&
                apron.Contains(t.Position) && MaterialNames.Contains(t.def.defName))
                .OrderBy(t => t.def.defName).ThenBy(t => t.thingIDNumber).ToList();
            int next = 0;
            foreach (var item in supplies)
            {
                var shelf = shelves[next++ % shelves.Count];
                var cells = shelf.OccupiedRect().Cells.OrderBy(c =>
                    c.GetThingList(map).Count(t => t.def.category == ThingCategory.Item)).ToList();
                var original = item.Position;
                shelf.GetStoreSettings().filter.SetAllow(item.def, true);
                item.DeSpawn();
                bool placed = false;
                foreach (var cell in cells)
                    if (GenPlace.TryPlaceThing(item, cell, map, ThingPlaceMode.Direct)) { placed = true; break; }
                if (!placed)
                {
                    GenPlace.TryPlaceThing(item, original, map, ThingPlaceMode.Near);
                    Log.Error("[Yunjianzi] Could not store starting material on memorial shelf: " + item.def.defName);
                }
            }
            foreach (var shelf in shelves) shelf.Notify_SettingsChanged();
        }
    }
}
