using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Yunjianzi
{
    public sealed class YunjianziMemorialFurniture
    {
        public ThingDef def;
        public IntVec3 offset;
        public Rot4 rotation = Rot4.North;
    }
    public sealed class YunjianziMemorialLayoutDef : Def
    {
        public List<string> rows = new List<string>();
        public List<YunjianziMemorialFurniture> furniture = new List<YunjianziMemorialFurniture>();
    }
    internal static class YunjianziMemorialLayout
    {
        // Authored after studying Monument, AddInnerMonuments and AddColumns.
        // XML contains the complete reviewed plan: no broad aisle clearing
        // that erases the nested architecture or its furniture.
        internal static YunjianziMemorialLayoutDef Plan => DefDatabase<YunjianziMemorialLayoutDef>.GetNamed("YJZ_ImperialMemorialPlan");
        internal static readonly IntVec3 SteleOffset=new IntVec3(0,0,11);
        internal static readonly IntVec3 AltarOffset=new IntVec3(0,0,7);
        internal static readonly CellRect Sanctum=new CellRect(-6,3,13,12);
        internal static IEnumerable<IntVec3> ArkOffsets()
        {
            // North-facing 2-cell-wide objects occupy anchor.x and anchor.x+1.
            // These anchors mirror their full footprints about the central tile.
            foreach(int z in new[]{-10,-5,0})
                foreach(int x in new[]{-7,6})yield return new IntVec3(x,0,z);
        }
        internal static IEnumerable<KeyValuePair<IntVec3,char>> Cells()
        {
            var rows=Plan.rows;
            for(int r=0;r<rows.Count;r++)
                for(int x=0;x<rows[r].Length;x++)
                {
                    var offset=new IntVec3(x-rows[r].Length/2,0,rows.Count/2-r);
                    char c=rows[r][x];
                    if(c!='-')yield return new KeyValuePair<IntVec3,char>(offset,c);
                }
        }
        internal static bool OutsidePlan(IntVec3 offset)
        {
            int r=Plan.rows.Count/2-offset.z;
            if(r<0 || r>=Plan.rows.Count)return true;
            int x=offset.x+Plan.rows[r].Length/2;
            return x<0 || x>=Plan.rows[r].Length || Plan.rows[r][x]=='-';
        }
        internal static void LayFoundation(Map map,IntVec3 root)
        {
            var platform=DefDatabase<TerrainDef>.GetNamed("MechanoidPlatform");
            // Follow the quest's stepped cross outline instead of enclosing
            // the whole complex in another rectangular slab.
            foreach(var cell in Cells())
                foreach(var c in CellRect.CenteredOn(root+cell.Key,1))
                    if(c.InBounds(map))map.terrainGrid.SetTerrain(c,platform);
        }
        internal static void Build(Map map,IntVec3 root)
        {
            var marble=DefDatabase<ThingDef>.GetNamed("BlocksMarble");
            var floor=DefDatabase<TerrainDef>.GetNamed("TileMarble");
            foreach(var cell in Cells())
            {
                var c=root+cell.Key;
                map.terrainGrid.SetTerrain(c,floor);
                map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);
                if(cell.Value!='#' && cell.Value!='D')continue;
                if(c.GetEdifice(map)!=null)continue;
                var thing=ThingMaker.MakeThing(cell.Value=='D'?ThingDefOf.Door:ThingDefOf.Wall,marble);
                thing.SetFaction(Faction.OfPlayer);GenSpawn.Spawn(thing,c,map);
            }
            foreach(var item in Plan.furniture)
            {
                var thing=ThingMaker.MakeThing(item.def,item.def.MadeFromStuff?marble:null);
                if(GenAdj.OccupiedRect(root+item.offset,item.rotation,item.def.size).Cells.Any(c=>c.GetEdifice(map)!=null))
                    throw new System.InvalidOperationException("Memorial furnishing overlaps at "+item.offset+": "+item.def.defName);
                thing.SetFaction(Faction.OfPlayer);GenSpawn.Spawn(thing,root+item.offset,map,item.rotation);
                // Supply placement enables only each shelf's assigned materials;
                // ordinary shelf defaults would immediately draw ship supplies.
                if(thing is Building_Storage storage)storage.GetStoreSettings().filter.SetDisallowAll();
            }
        }
    }
}
