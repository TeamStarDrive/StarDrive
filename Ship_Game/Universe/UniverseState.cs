using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SDUtils;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.ExtensionMethods;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using BoundingFrustum = Microsoft.Xna.Framework.BoundingFrustum;

namespace Ship_Game.Universe
{
    /// <summary>
    /// Holds the invisible state of the universe, manages all Ships, Projectiles, Systems, Planets
    /// and UniverseState reference be used to search for those objects.
    ///
    /// </summary>
    public class UniverseState
    {
        /// <summary>
        /// This is the RADIUS of the universe
        /// Stars are generated within XY range [-Size, +Size],
        /// so {0,0} is center of the universe
        /// </summary>
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
        public DebugModes DebugMode;
        DebugModes PrevDebugMode;

        public bool GameOver = false;
        public bool NoEliminationVictory;
        public float GamePace = 1f;
        public float GameSpeed = 1f;
        public float StarDate = 1000f;

        // generated once during universe generation
        // allows us to define consistent backgrounds between savegames
        public int BackgroundSeed;

        /// <summary>
        /// Manages universe objects in a thread-safe manner
        /// </summary>
        public readonly UniverseObjectManager Objects;

        /// <summary>
        /// Spatial search interface for Universe Objects, updated once per frame
        /// </summary>
        public readonly SpatialManager Spatial;

        /// <summary>
        /// Global influence tree for fast influence checks, updated every time
        /// a ship is created or dies
        /// </summary>
        public readonly InfluenceTree Influence;

        /// <summary>
        /// Invoked when a Ship is removed from the universe
        /// </summary>
        public event Action<Ship> EvtOnShipRemoved;

        readonly Array<Empire> EmpireList = new();

        readonly Map<int, SolarSystem> SolarSystemDict = new();
        readonly Array<SolarSystem> SolarSystemList = new();
        
        readonly Map<int, Planet> PlanetsDict = new();
        readonly Array<Planet> AllPlanetsList = new();
        
        // @return All Empires in the Universe
        public IReadOnlyList<Empire> Empires => EmpireList;

        public int NumEmpires => EmpireList.Count;

        // @return All SolarSystems in the Universe
        public IReadOnlyList<SolarSystem> Systems => SolarSystemList;

        // @return All Planets in the Universe
        public IReadOnlyList<Planet> Planets => AllPlanetsList;
        
        /// <summary>
        /// Thread unsafe view of all ships.
        /// It's only safe to use from simulation thread or when sim is paused
        /// </summary>
        public IReadOnlyList<Ship> Ships => Objects.GetShips();

        // TODO: attempt to stop relying on visual state
        public readonly UniverseScreen Screen;
        
        // TODO: Encapsulate
        public BatchRemovalCollection<SpaceJunk> JunkList = new();

        public DebugInfoScreen DebugWin => Screen.DebugWin;
        public NotificationManager Notifications => Screen.NotificationManager;

        public readonly float DefaultProjectorRadius;

        public UniverseState(UniverseScreen screen, float universeRadius)
        {
            Screen = screen;
            Size = universeRadius;
            if (Size < 1f)
                throw new ArgumentException("UniverseSize not set!");

            Spatial = new SpatialManager();
            Spatial.Setup(universeRadius);

            DefaultProjectorRadius = (float)Math.Round(universeRadius * 0.04f);
            Influence = new InfluenceTree(universeRadius, DefaultProjectorRadius);

            Objects = new UniverseObjectManager(screen, this, Spatial);
        }

        public void SetDebugMode(bool debug)
        {
            Debug = debug;
            // if not in debug, we set DebugMode to invalid value
            DebugMode = debug ? PrevDebugMode : DebugModes.Last;
            if (!debug)
                Screen.DebugWin = null;
        }

        public void SetDebugMode(DebugModes mode)
        {
            PrevDebugMode = mode;
            SetDebugMode(Debug);
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
                if (planet.ParentSystem != system)
                    throw new InvalidOperationException($"AddSolarSystem Planet.ParentSystem must be valid: {planet.ParentSystem} != {system}");
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

        public Empire CreateTestEmpire(string name)
        {
            var e = new Empire(this)
            {
                data = new EmpireData
                {
                    Traits = new RacialTrait { Name = name }
                }
            };
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

        public Empire GetEmpire(int empireId)
        {
            for (int i = 0; i < EmpireList.Count; ++i)
                if (EmpireList[i].Id == empireId)
                    return EmpireList[i];
            return null;
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

        // Returns all solar systems within frustum
        public SolarSystem[] GetVisibleSystems()
        {
            return SolarSystemList.Filter(s => Screen.IsInFrustum(s.Position, s.Radius));
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

        public GameObject GetObject(int id)
        {
            return Objects.FindObject(id);
        }

        public bool GetObject(int id, out GameObject found)
        {
            return Objects.FindObject(id, out found);
        }

        public void AddShip(Ship ship)
        {
            Objects.Add(ship);
        }

        public void OnShipAdded(Ship ship)
        {
            Empire owner = ship.Loyalty;
            owner.AddBorderNode(ship);

            if (ship.IsSubspaceProjector)
            {
                Influence.Insert(owner, ship);
            }
        }

        public void OnShipRemoved(Ship ship)
        {
            if (ship.IsSubspaceProjector)
            {
                ship.Loyalty.RemoveBorderNode(ship);
                Influence.Remove(ship.Loyalty, ship);
            }
            EvtOnShipRemoved?.Invoke(ship);
        }
        
        public void OnPlanetOwnerAdded(Empire owner, Planet planet)
        {
            owner.AddBorderNode(planet);
            Influence.Insert(owner, planet);
        }

        public void OnPlanetOwnerRemoved(Empire owner, Planet planet)
        {
            owner.RemoveBorderNode(planet);
            Influence.Remove(owner, planet);
        }
    }
}
