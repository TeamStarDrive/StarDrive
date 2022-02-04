using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game.Debug;
using Ship_Game.Empires;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Universe
{
    /// <summary>
    /// Holds the invisible state of the universe, manages all Ships, Projectiles, Systems, Planets
    /// and UniverseState reference be used to search for those objects.
    ///
    /// </summary>
    public class UniverseState
    {
        public readonly float Size;
        public float FTLModifier = 1f;
        public float EnemyFTLModifier = 1f;
        public bool FTLInNeutralSystems = true;
        public GameDifficulty Difficulty;
        public GalSize GalaxySize;
        public bool GravityWells;
        public Empire Player;

        // Global unique ID counter for this Universe
        // Can be used to assign ID-s for any kind of object
        // Id <= 0 is always invalid, valid ID-s start at 1
        public int UniqueObjectIds;
        
        public bool Paused = true; // always start paused
        public bool Debug;
        public bool GameOver = false;
        public bool NoEliminationVictory;
        public float GamePace = 1f;
        public float GameSpeed = 1f;
        public float StarDate = 1000f;

        /// <summary>
        /// Manages universe objects in a thread-safe manner
        /// </summary>
        public readonly UniverseObjectManager Objects;

        /// <summary>
        /// Spatial search interface for Universe Objects, updated once per frame
        /// </summary>
        public readonly SpatialManager Spatial;

        public readonly SubSpaceProjectors Projectors;

        readonly Array<Empire> EmpireList = new Array<Empire>();

        readonly Map<int, SolarSystem> SolarSystemDict = new Map<int, SolarSystem>();
        readonly Array<SolarSystem> SolarSystemList = new Array<SolarSystem>();
        
        readonly Map<int, Planet> PlanetsDict = new Map<int, Planet>();
        readonly Array<Planet> AllPlanetsList = new Array<Planet>();
        
        // @return All Empires in the Universe
        public IReadOnlyList<Empire> Empires => EmpireList;

        // @return All SolarSystems in the Universe
        public IReadOnlyList<SolarSystem> Systems => SolarSystemList;

        // @return All Planets in the Universe
        public IReadOnlyList<Planet> Planets => AllPlanetsList;
        
        /// <summary>
        /// Thread unsafe view of all ships.
        /// It's only safe to use from simulation thread or when sim is paused
        /// </summary>
        public IReadOnlyList<Ship> Ships => Objects.Ships;

        // TODO: attempt to stop relying on visual state
        public readonly UniverseScreen Screen;
        
        // TODO: Encapsulate
        public BatchRemovalCollection<SpaceJunk> JunkList = new BatchRemovalCollection<SpaceJunk>();

        public DebugInfoScreen DebugWin => Screen.DebugWin;
        public NotificationManager Notifications => Screen.NotificationManager;

        public UniverseState(UniverseScreen screen, float universeSize)
        {
            Screen = screen;
            Size = universeSize;
            if (Size < 1f)
                throw new ArgumentException("UniverseSize not set!");

            Spatial = new SpatialManager();
            Spatial.Setup(universeSize);

            Projectors = new SubSpaceProjectors(universeSize);
            Objects = new UniverseObjectManager(screen, this, Spatial);
        }

        // @return New Unique ID in this Universe
        //         The ID-s are ever increasing, and persistent between saves
        public int CreateId()
        {
            return Interlocked.Increment(ref UniqueObjectIds);
        }

        public void Clear()
        {
            Objects.Clear();
            ClearSystems();
            ClearSpaceJunk();
            PlanetsDict.Clear();
            SolarSystemDict.Clear();
            EmpireManager.Clear();
            Spatial.Destroy();
        }

        void ClearSystems()
        {
            foreach (SolarSystem solarSystem in SolarSystemList)
            {
                // TODO: Move this into SolarSystem.cs
                solarSystem.FiveClosestSystems.Clear();
                foreach (Planet planet in solarSystem.PlanetList)
                {
                    planet.TilesList = new Array<PlanetGridSquare>();
                    if (planet.SO != null)
                    {
                        Screen.RemoveObject(planet.SO);
                        planet.SO = null;
                    }
                }

                foreach (Asteroid asteroid in solarSystem.AsteroidsList)
                {
                    asteroid.DestroySceneObject();
                }
                solarSystem.AsteroidsList.Clear();

                foreach (Moon moon in solarSystem.MoonList)
                {
                    moon.DestroySceneObject();
                }
                solarSystem.MoonList.Clear();
            }
            SolarSystemList.Clear();
        }

        void ClearSpaceJunk()
        {
            JunkList.ApplyPendingRemovals();
            foreach (SpaceJunk spaceJunk in JunkList)
                spaceJunk.DestroySceneObject();
            JunkList.Clear();
        }

        // Adds a new solar system to the universe
        // and registers all planets as unique entries in AllPlanetsList
        public void AddSolarSystem(SolarSystem system)
        {
            if (system.Universe != this)
                throw new InvalidOperationException($"AddSolarSystem System was not created for this Universe: {system}");
            if (system.Id <= 0)
                throw new InvalidOperationException($"AddSolarSystem System.Id must be valid: {system}");
            SolarSystemDict.Add(system.Id, system);
            SolarSystemList.Add(system);
            foreach (Planet planet in system.PlanetList)
            {
                if (planet.Id <= 0)
                    throw new InvalidOperationException($"AddSolarSystem Planet.Id must be valid: {planet}");
                planet.ParentSystem = system;
                planet.UpdatePositionOnly();
                PlanetsDict.Add(planet.Id, planet);
                AllPlanetsList.Add(planet);
            }
        }

        public Empire CreateEmpire(IEmpireData readOnlyData, bool isPlayer)
        {
            if (EmpireManager.GetEmpireByName(readOnlyData.Name) != null)
                throw new InvalidOperationException($"BUG: Empire already created! {readOnlyData.Name}");
            Empire e = EmpireManager.CreateEmpireFromEmpireData(this, readOnlyData, isPlayer);
            return AddEmpire(e);
        }

        public Empire AddEmpire(Empire e)
        {
            if (e.Universum == null)
                throw new ArgumentNullException("Empire.Universum cannot be null");

            EmpireList.Add(e);
            EmpireManager.Add(e);

            if (e.isPlayer)
            {
                if (Player != null)
                    throw new InvalidOperationException($"Duplicate Player empire! previous={Player}  new={e}");
                Player = e;
            }
            return e;
        }

        public Empire GetEmpire(string loyalty)
        {
            return EmpireList.Find(e => e.data.Traits.Name == loyalty);
        }

        public SolarSystem GetSystem(int id)
        {
            if (id <= 0) return null;
            if (SolarSystemDict.TryGetValue(id, out SolarSystem system))
                return system;
            Log.Error($"System not found: {id}");
            return null;
        }

        public Planet GetPlanet(int id)
        {
            if (id <= 0) return null;
            if (PlanetsDict.TryGetValue(id, out Planet planet))
                return planet;
            Log.Error($"Planet not found: {id}");
            return null;
        }

        public bool GetPlanet(int id, out Planet found)
        {
            return (found = GetPlanet(id)) != null;
        }

        public SolarSystem FindClosestSystem(Vector2 pos)
        {
            return SolarSystemList.FindClosestTo(pos);
        }

        public Planet FindClosestPlanet(Vector2 pos)
        {
            return AllPlanetsList.FindClosestTo(pos);
        }

        public SolarSystem FindSolarSystemAt(Vector2 point)
        {
            foreach (SolarSystem s in SolarSystemList)
            {
                if (point.InRadius(s.Position, s.Radius*2))
                    return s;
            }
            return null;
        }

        public bool FindSystem(int id, out SolarSystem foundSystem)
        {
            return SolarSystemDict.TryGetValue(id, out foundSystem);
        }

        public SolarSystem FindSystem(int id)
        {
            return SolarSystemDict.TryGetValue(id, out SolarSystem system) ? system : null;
        }

        public Array<SolarSystem> GetFiveClosestSystems(SolarSystem system)
        {
            return SolarSystemList.FindMinItemsFiltered(5, filter => filter != system,
                                                           select => select.Position.SqDist(system.Position));
        }

        public Array<SolarSystem> GetSolarSystemsFromIds(Array<int> ids)
        {
            var systems = new Array<SolarSystem>();
            for (int i = 0; i < ids.Count; i++)
            {
                if (SolarSystemDict.TryGetValue(ids[i], out SolarSystem s))
                    systems.Add(s);
            }
            return systems;
        }

        public Array<Ship> GetShipsFromIds(Array<int> ids)
        {
            var ships = new Array<Ship>();
            for (int i = 0; i < ids.Count; i++)
            {
                Ship ship = Objects.FindShip(ids[i]);
                if (ship != null)
                    ships.AddUnique(ship);
            }
            return ships;
        }

        public Ship GetShip(int id)
        {
            return Objects.FindShip(id);
        }

        public bool GetShip(int id, out Ship found)
        {
            return Objects.FindShip(id, out found);
        }

        public GameplayObject GetObject(int id)
        {
            return Objects.FindObject(id);
        }

        public bool GetObject(int id, out GameplayObject found)
        {
            return Objects.FindObject(id, out found);
        }
    }
}
