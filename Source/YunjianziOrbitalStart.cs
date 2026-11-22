using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Yunjianzi
{
    // Only the new-game generator opts into shrine placement. Old saves retain
    // their existing scroll and never acquire a second ship or memorial.
    public sealed class MapComponent_YunjianziMemorial : MapComponent
    {
        public IntVec3 altar = IntVec3.Invalid;
        private bool environmentInitialized;
        public MapComponent_YunjianziMemorial(Map map) : base(map) { }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref altar, "yjz_memorialAltar", IntVec3.Invalid);
            Scribe_Values.Look(ref environmentInitialized, "yjz_memorialEnvironmentInitialized");
        }
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (!altar.IsValid || environmentInitialized) return;
            // A maintained station starts pressurized. This runs once, not on
            // subsequent loads or after the player breaches the room shell.
            foreach (Room room in map.regionGrid.AllRooms)
            {
                if (room.ExposedToSpace || room.UsesOutdoorTemperature) continue;
                room.Temperature = 21f;
                room.Vacuum = 0f;
            }
            environmentInitialized = true;
            LongEventHandler.ExecuteWhenFinished(EnsureDockingHatch);
        }

        private void EnsureDockingHatch()
        {
            // In some rotations the original side exits are enclosed by the
            // exhaust wells. Add a docking hatch between the ship interior and
            // pressurized berth, never a shortcut through an open vacuum well.
            // Run on the main thread after assembly and room initialization.
            Pawn pawn=map.mapPawns.FreeColonistsSpawned.FirstOrDefault(YunjianziSoleColonistUtility.IsYunjianzi);
            if(pawn==null || pawn.CanReach(altar,PathEndMode.Touch,Danger.Deadly))return;
            var parms=TraverseParms.For(pawn,Danger.Deadly);
            var hulls=map.listerThings.AllThings.Where(t=>t is Building &&
                t.def.defName.Contains("GravshipHull") && t.def.size==IntVec2.One).ToList();
            foreach(Thing hull in hulls.OrderBy(t=>t.Position.DistanceToSquared(altar)))
                foreach(IntVec3 offset in GenAdj.CardinalDirections)
                {
                    IntVec3 inner=hull.Position+offset,outer=hull.Position-offset;
                    // Furnished ship cells can be walked through without being
                    // Standable (beds, tables, workbenches). Require traversable
                    // cells and an actual path, not empty furniture-free tiles.
                    if(!inner.InBounds(map) || !outer.InBounds(map) || !inner.Walkable(map) || !outer.Walkable(map))continue;
                    if(inner.GetRoom(map).Vacuum>0.2f || outer.GetRoom(map).Vacuum>0.2f)continue;
                    if(!map.reachability.CanReach(pawn.Position,inner,PathEndMode.OnCell,parms) ||
                        !map.reachability.CanReach(outer,altar,PathEndMode.Touch,parms))continue;
                    IntVec3 position=hull.Position;
                    hull.Destroy(DestroyMode.Vanish);
                    Thing hatch=ThingMaker.MakeThing(ThingDefOf.Door,ThingDefOf.Steel);
                    hatch.SetFaction(Faction.OfPlayer);
                    GenSpawn.Spawn(hatch,position,map);
                    return;
                }
            Log.Error("[Yunjianzi] No pressurized ship-to-memorial docking route could be created.");
        }
    }

    public sealed class CompProperties_YunjianziMemorialHeating : CompProperties
    {
        public float heatEnergy = 16000f;
        public CompProperties_YunjianziMemorialHeating() { compClass=typeof(CompYunjianziMemorialHeating); }
    }

    public sealed class CompYunjianziMemorialHeating : ThingComp
    {
        public override void CompTickRare()
        {
            Room room = parent.GetRoom();
            if (room == null || room.ExposedToSpace || room.UsesOutdoorTemperature) return;
            float energy=(props as CompProperties_YunjianziMemorialHeating)?.heatEnergy??16000f;
            room.Temperature += GenTemperature.ControlTemperatureTempChange(parent.Position, parent.Map, energy, 21f);
        }
    }

    public sealed class GenStep_YunjianziOrbitalFoundation : GenStep
    {
        public override int SeedPart => 9137711;
        internal static IntVec3 Dock(Map map) => map.Center + new IntVec3(0, 0, -18);
        internal static IntVec3 Shrine(Map map) => map.Center + new IntVec3(0, 0, 20);
        public override void Generate(Map map, GenStepParams parms)
        {
            TerrainDef floor = DefDatabase<TerrainDef>.GetNamed("MechanoidPlatform");
            // Solid station foundation, not gravship substructure.
            LayPlatform(map, CellRect.CenteredOn(Dock(map), 27, 27), floor);
            LayPlatform(map, new CellRect(map.Center.x-3, Dock(map).z+12, 7, 14), floor);
            YunjianziMemorialLayout.LayFoundation(map,Shrine(map));
            // Broken side wings and detached wreck islands frame the intact
            // sanctuary. The ship apron is open; the mausoleum passage is sealed.
            foreach (int side in new[]{-1,1})
            {
                foreach (int z in new[]{-10,13,32})
                {
                    IntVec3 root=map.Center+new IntVec3(side*30,0,z);
                    var rect=CellRect.CenteredOn(root,15,13);
                    foreach (IntVec3 c in rect.ClipInsideMap(map))
                        if ((c-root).LengthHorizontalSquared < 64 &&
                            ((c-root).LengthHorizontalSquared < 16 || Rand.Value>0.08f))
                            map.terrainGrid.SetTerrain(c,floor);
                }
            }
            MapGenerator.PlayerStartSpot = Dock(map);
            MapGenerator.rootsToUnfog.Add(Dock(map));
            MapGenerator.rootsToUnfog.Add(Shrine(map));
            map.GetComponent<MapComponent_YunjianziMemorial>().altar = Shrine(map) + YunjianziMemorialLayout.AltarOffset;
        }
        private static void LayPlatform(Map map,CellRect rect,TerrainDef floor)
        {
            foreach(IntVec3 c in rect.ClipInsideMap(map))map.terrainGrid.SetTerrain(c,floor);
        }
    }

    public sealed class GenStep_YunjianziOrbitalMemorial : GenStep
    {
        public override int SeedPart => 9137712;
        public override void Generate(Map map, GenStepParams parms)
        {
            IntVec3 dock = GenStep_YunjianziOrbitalFoundation.Dock(map);
            IntVec3 shrine = GenStep_YunjianziOrbitalFoundation.Shrine(map);
            // The landing apron is outdoors: no surrounding walls, station
            // roof, or exhaust wells. Keep the prefab's own ship hull/roof.
            // Only the double-door passage into the mausoleum is pressurized.
            RoomShell(map, new CellRect(dock.x-2, dock.z+12, 5, 12), true, true);
            YunjianziMemorialLayout.Build(map,shrine);
            Spawn(map, "YJZ_PredecessorCenotaph", shrine + YunjianziMemorialLayout.SteleOffset);
            Spawn(map, "YJZ_MemorialOffering", shrine + YunjianziMemorialLayout.AltarOffset);
            Spawn(map, "YJZ_Building_MeditationCushion", shrine + YunjianziMemorialLayout.AltarOffset + new IntVec3(0,0,-2), ThingDefOf.Cloth);
            var chest = (Building_YunjianziInheritanceChest)Spawn(map, "YJZ_InheritanceChest", shrine + YunjianziMemorialLayout.AltarOffset + new IntVec3(-3,0,0));
            chest.BindTo(map.mapPawns.FreeColonistsSpawned.FirstOrDefault(YunjianziSoleColonistUtility.IsYunjianzi));
            YunjianziMemorialSupplies.Distribute(map, shrine);
            // The arrival scenpart spawns the pawn (and its one bound scroll)
            // before this step constructs the altar. Move that existing item;
            // never create another scroll or run this operation on save load.
            IntVec3 altar = map.GetComponent<MapComponent_YunjianziMemorial>().altar;
            foreach (Thing scroll in map.listerThings.ThingsOfDef(YJZDefOf.YJZ_Item_BoundImmortalScroll).ToList())
            {
                scroll.DeSpawn();
                GenSpawn.Spawn(scroll, altar, map);
                if(scroll.def.CanHaveFaction)scroll.SetFaction(null);
                scroll.SetForbidden(true,false);
            }
            ThingDef arkDef=DefDatabase<ThingDef>.GetNamed("YJZ_StartingShenjiArk");
            var arkOffsets=YunjianziMemorialLayout.ArkOffsets().ToList();
            int occupiedArk=Rand.Range(0,arkOffsets.Count);
            Pawn inheritor=map.mapPawns.FreeColonistsSpawned.FirstOrDefault(YunjianziSoleColonistUtility.IsYunjianzi);
            for(int i=0;i<arkOffsets.Count;i++)
            {
                var ark=(Building_YunjianziStartingShenjiArk)Spawn(map,arkDef.defName,shrine+arkOffsets[i]);
                ark.BindTo(inheritor,i==occupiedArk);
            }
            Spawn(map, "YJZ_MemorialLifeSupport", dock + new IntVec3(-1,0,17));
            PlaceBattleRemains(map, dock, shrine);

            // Break up the outer platform into salvage clusters; the sealed
            // mausoleum passage and landing apron are excluded from debris.
            for(int i=0;i<70;i++)
            {
                int dx = Rand.RangeInclusive(16,29)*(Rand.Bool?1:-1);
                IntVec3 c=map.Center+new IntVec3(dx,0,Rand.RangeInclusive(-30,30));
                if(!c.InBounds(map) || !c.Standable(map) || c.GetEdifice(map)!=null) continue;
                if(CellRect.CenteredOn(dock,27,27).Contains(c)) continue;
                if(CellRect.CenteredOn(shrine,39,35).Contains(c)) continue;
                string debris=i%4==0?"ShipChunk":"ChunkSlagSteel";
                ThingDef debrisDef=DefDatabase<ThingDef>.GetNamed(debris);
                // Check the entire footprint: vanilla GenSpawn may wipe an
                // existing building even when the anchor itself is empty.
                if(GenAdj.OccupiedRect(c,Rot4.North,debrisDef.size).ExpandedBy(1).Cells.Any(p=>
                    !p.InBounds(map) || !p.Standable(map) || p.GetEdifice(map)!=null))continue;
                Spawn(map,debris,c);
            }
            map.regionAndRoomUpdater.Enabled=true;
        }

        private static void PlaceBattleRemains(Map map,IntVec3 dock,IntVec3 shrine)
        {
            // Called exclusively by this starting-map genstep. These are inert
            // buildings, never pawn corpses, raid entries or recurring spawns.
            string[] names={"AncientRing","AncientBell","AncientCrawler","AncientCoil",
                "DarkSkitter","DarkWatcher","DarkReaper","DarkCore"};
            var reserved=new HashSet<IntVec3>();
            var islandVariants=new HashSet<string>();
            var candidates=new List<IntVec3>();
            foreach(int side in new[]{-1,1})
                foreach(int z in new[]{-10,13,32})
                    candidates.AddRange(CellRect.CenteredOn(map.Center+new IntVec3(side*30,0,z),13,11).ClipInsideMap(map));
            candidates.Shuffle();
            for(int repeat=0;repeat<3;repeat++)
                foreach(string name in names)
                {
                    candidates.Shuffle();
                    foreach(IntVec3 c in candidates)
                        if(Place(name,c))break;
                }

            bool Place(string name,IntVec3 c)
            {
                ThingDef def=DefDatabase<ThingDef>.GetNamed("YJZ_Relic_"+name);
                int dz=c.z-map.Center.z;
                string islandKey=System.Math.Abs(c.x-map.Center.x)<16?null:
                    name+":"+(c.x<map.Center.x?"W":"E")+(dz<0?0:dz<23?1:2);
                if(islandKey!=null && islandVariants.Contains(islandKey))return false;
                CellRect footprint=GenAdj.OccupiedRect(c,Rot4.North,def.size);
                if(footprint.Cells.Any(p=>!p.InBounds(map) || !p.Standable(map) || p.GetEdifice(map)!=null || reserved.Contains(p)))return false;
                if(footprint.ExpandedBy(1).Cells.Any(p=>!p.InBounds(map) || p.GetEdifice(map)!=null || reserved.Contains(p)))return false;
                Spawn(map,def.defName,c);
                if(islandKey!=null)islandVariants.Add(islandKey);
                foreach(IntVec3 p in footprint.ExpandedBy(1))reserved.Add(p);
                return true;
            }
        }

        private static void RoomShell(Map map,CellRect rect,bool northDoor,bool southDoor)
        {
            foreach(IntVec3 c in rect.ClipInsideMap(map))
            {
                bool edge=c.x==rect.minX || c.x==rect.maxX || c.z==rect.minZ || c.z==rect.maxZ;
                if(edge && c.GetEdifice(map)==null)
                {
                    bool door=c.x==rect.CenterCell.x && ((northDoor && c.z==rect.maxZ)||(southDoor && c.z==rect.minZ));
                    Spawn(map,door?"Door":"Wall",c,DefDatabase<ThingDef>.GetNamed("BlocksMarble"));
                }
                map.roofGrid.SetRoof(c,RoofDefOf.RoofConstructed);
            }
        }

        private static Thing Spawn(Map map,string name,IntVec3 c,ThingDef stuff=null)
        {
            Thing thing=ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(name),stuff);
            if(thing.def.CanHaveFaction) thing.SetFaction(Faction.OfPlayer);
            return GenSpawn.Spawn(thing,c,map);
        }
    }
}
