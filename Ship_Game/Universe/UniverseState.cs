using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        readonly Map<Guid, SolarSystem> SolarSystemDict = new Map<Guid, SolarSystem>();
        readonly Array<SolarSystem> SolarSystemList = new Array<SolarSystem>();
        
        readonly Map<Guid, Planet> PlanetsDict = new Map<Guid, Planet>();
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
            system.Universe = this;
            SolarSystemDict.Add(system.Guid, system);
            SolarSystemList.Add(system);
            foreach (Planet planet in system.PlanetList)
            {
                planet.ParentSystem = system;
                planet.UpdatePositionOnly();
                PlanetsDict.Add(planet.Guid, planet);
                AllPlanetsList.Add(planet);
            }
        }

        public Empire CreateEmpire(IEmpireData readOnlyData, bool isPlayer)
        {
            if (EmpireManager.GetEmpireByName(readOnlyData.Name) != null)
                throw new InvalidOperationException($"BUG: Empire already created! {readOnlyData.Name}");
            Empire e = EmpireManager.CreateEmpireFromEmpireData(readOnlyData, isPlayer);
            return AddEmpire(e);
        }

        public Empire AddEmpire(Empire e)
        {
            e.Universum = this;
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

        public SolarSystem GetSystem(in Guid guid)
        {
            if (guid == Guid.Empty) return null;
            if (SolarSystemDict.TryGetValue(guid, out SolarSystem system))
                return system;
            Log.Error($"System not found: {guid}");
            return null;
        }

        public Planet GetPlanet(in Guid guid)
        {
            if (guid == Guid.Empty) return null;
            if (PlanetsDict.TryGetValue(guid, out Planet planet))
                return planet;
            Log.Error($"Planet not found: {guid}");
            return null;
        }

        public bool GetPlanet(in Guid guid, out Planet found)
        {
            return (found = GetPlanet(guid)) != null;
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

        public bool FindSystem(in Guid systemGuid, out SolarSystem foundSystem)
        {
            return SolarSystemDict.TryGetValue(systemGuid, out foundSystem);
        }

        public SolarSystem FindSystem(in Guid systemGuid)
        {
            return SolarSystemDict.TryGetValue(systemGuid, out SolarSystem system) ? system : null;
        }

        public Array<SolarSystem> GetFiveClosestSystems(SolarSystem system)
        {
            return SolarSystemList.FindMinItemsFiltered(5, filter => filter != system,
                                                           select => select.Position.SqDist(system.Position));
        }

        public Array<SolarSystem> GetSolarSystemsFromGuids(Array<Guid> guids)
        {
            var systems = new Array<SolarSystem>();
            for (int i = 0; i < guids.Count; i++)
            {
                if (SolarSystemDict.TryGetValue(guids[i], out SolarSystem s))
                    systems.Add(s);
            }
            return systems;
        }

        public Ship GetShip(in Guid guid)
        {
            return Objects.FindShip(guid);
        }

        public bool GetShip(in Guid guid, out Ship found)
        {
            return Objects.FindShip(guid, out found);
        }

        public GameplayObject GetObject(in Guid guid)
        {
            return Objects.FindObject(guid);
        }

        public bool GetObject(in Guid guid, out GameplayObject found)
        {
            return Objects.FindObject(guid, out found);
        }
    }
}
