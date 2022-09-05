using System;
using System.Collections.Generic;
using System.Threading;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Universe
{
    /// <summary>
    /// Holds the invisible state of the universe, manages all Ships, Projectiles, Systems, Planets
    /// and UniverseState reference be used to search for those objects.
    ///
    /// </summary>
    [StarDataType]
    public class UniverseState
    {
        /// <summary>
        /// This is the RADIUS of the universe
        /// Stars are generated within XY range [-Size, +Size],
        /// so {0,0} is center of the universe
        /// </summary>
        [StarData] public readonly float Size;
        [StarData] public Empire Player;

        [StarData] public float FTLModifier = 1f;
        [StarData] public float EnemyFTLModifier = 1f;
        [StarData] public bool FTLInNeutralSystems = true;
        [StarData] public GameDifficulty Difficulty;
        [StarData] public GalSize GalaxySize;
        [StarData] public bool GravityWells;

        [StarData] public float Pace = 1f;

        // TODO: This was too hard to fix, so added this placeholder until code is fixed
        public static float DummyPacePlaceholder = 1f;
        public static float DummySettingsResearchModifier = 1f;
        public static float DummyProductionPacePlaceholder = 1f;

        [StarData] public int ExtraPlanets;
        [StarData] public float StarsModifier = 1f;
        [StarData] public float SettingsResearchModifier = 1f;
        public float RemnantPaceModifier = 20;

        // Global unique ID counter for this Universe
        // Can be used to assign ID-s for any kind of object
        // Id <= 0 is always invalid, valid ID-s start at 1
        [StarData] int UniqueObjectIds;
        
        public bool Paused = true; // always start paused
        public bool Debug;
        public DebugModes DebugMode;
        DebugModes PrevDebugMode;

        public bool GameOver = false;
        [StarData] public bool NoEliminationVictory;
        [StarData] public float GamePace = 1f;
        [StarData] public float GameSpeed = 1f;
        [StarData] public float StarDate = 1000f;

        [StarData] public Vector3d CamPos;

        [StarData] public byte[] FogMapBytes;

        // generated once during universe generation
        // allows us to define consistent backgrounds between savegames
        [StarData] public int BackgroundSeed;

        /// <summary>
        /// Manages universe objects in a thread-safe manner
        /// </summary>
        public UniverseObjectManager Objects;

        /// <summary>
        /// Spatial search interface for Universe Objects, updated once per frame
        /// </summary>
        public SpatialManager Spatial;

        /// <summary>
        /// Global influence tree for fast influence checks, updated every time
        /// a ship is created or dies
        /// </summary>
        public InfluenceTree Influence;

        /// <summary>
        /// Invoked when a Ship is removed from the universe
        /// </summary>
        public event Action<Ship> EvtOnShipRemoved;

        [StarData] readonly Array<Empire> EmpireList = new();

        [StarData] readonly Map<int, SolarSystem> SolarSystemDict = new();
        [StarData] readonly Array<SolarSystem> SolarSystemList = new();

        [StarData] readonly Map<int, Planet> PlanetsDict = new();
        [StarData] readonly Array<Planet> AllPlanetsList = new();

        // @return All Empires in the Universe
        public IReadOnlyList<Empire> Empires => EmpireList;
        public int NumEmpires => EmpireList.Count;
        public Empire[] NonPlayerMajorEmpires =>
            EmpireList.Filter(empire => !empire.IsFaction && !empire.isPlayer);

        public Empire[] NonPlayerEmpires =>
            EmpireList.Filter(empire => !empire.isPlayer);

        public Empire[] ActiveNonPlayerMajorEmpires =>
            EmpireList.Filter(empire => !empire.IsFaction && !empire.isPlayer && !empire.data.Defeated);

        public Empire[] ActiveMajorEmpires => 
            EmpireList.Filter(empire => !empire.IsFaction && !empire.data.Defeated);

        public Empire[] ActiveEmpires =>
            EmpireList.Filter(empire => !empire.data.Defeated);

        public Empire[] MajorEmpires   => EmpireList.Filter(empire => !empire.IsFaction);
        public Empire[] Factions       => EmpireList.Filter(empire => empire.IsFaction);
        public Empire[] PirateFactions => EmpireList.Filter(empire => empire.WeArePirates);


        // @return All SolarSystems in the Universe
        public IReadOnlyList<SolarSystem> Systems => SolarSystemList;

        // @return All Planets in the Universe
        public IReadOnlyList<Planet> Planets => AllPlanetsList;
        
        /// <summary>
        /// Thread unsafe view of all ships.
        /// It's only safe to use from simulation thread or when sim is paused
        /// </summary>
        public IReadOnlyList<Ship> Ships => Objects.GetShips();

        [StarData] public RandomEventManager Events;
        [StarData] public StatTracker Stats;
        [StarData] public UniverseParams Params;
        [StarData] public SaveState Save;

        // TODO: attempt to stop relying on visual state
        public UniverseScreen Screen;
        
        // TODO: Encapsulate
        public BatchRemovalCollection<SpaceJunk> JunkList = new();

        public DebugInfoScreen DebugWin => Screen.DebugWin;
        public NotificationManager Notifications => Screen.NotificationManager;

        public float DefaultProjectorRadius;

        public readonly RandomBase Random = new ThreadSafeRandom();

        UniverseState()
        {
        }

        public UniverseState(UniverseScreen screen, float universeRadius)
        {
            Screen = screen;
            Size = universeRadius;
            if (Size < 1f)
                throw new ArgumentException("UniverseSize not set!");

            Initialize(universeRadius);

            Events = new RandomEventManager(); // serialized
            Stats = new StatTracker(); // serialized
            Params = new UniverseParams(); // serialized
        }

        void Initialize(float universeRadius)
        {
            Spatial = new SpatialManager();
            Spatial.Setup(universeRadius);
            DefaultProjectorRadius = (float)Math.Round(universeRadius * 0.04f);
            Influence = new InfluenceTree(universeRadius, DefaultProjectorRadius);

            // Screen will be null during deserialization, so it must be set later
            Objects = new UniverseObjectManager(Screen, this, Spatial);
        }

        public void OnUniverseScreenLoaded(UniverseScreen screen)
        {
            Screen = screen;
            Objects.Universe = screen;
        }

        [StarDataType]
        public class SaveState
        {
            // globally stored ship designs
            [StarData] public ShipDesign[] Designs;
            Map<string, IShipDesign> DesignsMap;

            [StarData] public IReadOnlyList<Ship> Ships;
            [StarData] public SavedGame.ProjectileSaveData[] Projectiles;
            [StarData] public SavedGame.BeamSaveData[] Beams;

            public void SetDesigns(HashSet<IShipDesign> designs)
            {
                Designs = designs.Select(d => (ShipDesign)d);
            }

            public IShipDesign GetDesign(string name)
            {
                if (DesignsMap == null)
                {
                    DesignsMap = new();
                    foreach (ShipDesign fromSave in Designs)
                    {
                        fromSave.IsFromSave = true;

                        if (ResourceManager.Ships.GetDesign(fromSave.Name, out IShipDesign existing) &&
                            existing.AreModulesEqual(fromSave))
                            // use the existing one
                            DesignsMap[fromSave.Name] = existing;
                        else
                            // from save only
                            DesignsMap[fromSave.Name] = fromSave;
                    }
                }
                return DesignsMap[name];
            }
        }

        [StarDataSerialize]
        void OnSerialize()
        {
            // clean up and submit objects before saving
            Objects.UpdateLists(removeInactiveObjects: true);

            Save = new SaveState
            {
                Ships = Objects.GetShips(),
                Beams = Objects.GetBeamSaveData(),
                Projectiles = Objects.GetProjectileSaveData()
            };
            Save.SetDesigns(Ships.Select(s => s.ShipData).UniqueSet());

            // FogMap is converted to a Alpha bytes so that it can be included in the savegame
            FogMapBytes = Screen.ContentManager.RawContent.TexExport.ToAlphaBytes(Screen.FogMap);
        }

        // Only call OnDeserialized evt if Empire and Ship have finished their events
        [StarDataDeserialized(typeof(Empire), typeof(Ship))]
        void OnDeserialized()
        {
            Initialize(Size);

            Params.UpdateGlobalStats();
            SettingsResearchModifier = GetResearchMultiplier();
            RemnantPaceModifier      = CalcRemnantPace();

            foreach (Ship ship in Save.Ships)
                Objects.Add(ship);
            foreach (SavedGame.BeamSaveData beamData in Save.Beams)
                Beam.CreateFromSave(beamData, this);
            foreach (SavedGame.ProjectileSaveData projData in Save.Projectiles)
                Projectile.CreateFromSave(projData, this);

            foreach (Empire e in Empires)
                e.ResetTechsUsableByShips(e.GetOurFactionShips(), unlockBonuses: false);

            foreach (Empire e in Empires)
            {
                if (e.data.AbsorbedBy != null)
                {
                    Empire masterEmpire = GetEmpire(e.data.AbsorbedBy);
                    masterEmpire.AssimilateTech(e);
                }
            }

            foreach (Empire empire in MajorEmpires)
                empire.UpdateDefenseShipBuildingOffense();

            foreach (Empire empire in Empires.Filter(e => !e.data.Defeated))
                empire.UpdatePopulation();

            Save = null;
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

        float CalcRemnantPace()
        {
            float stars = StarsModifier * 4; // 1-8
            float size = (int)GalaxySize + 1; // 1-7
            int extra = ExtraPlanets; // 1-3
            int numMajorEmpires = EmpireList.Count(e => !e.IsFaction);
            float numEmpires = numMajorEmpires / 2f; // 1-4.5

            float pace = 20 - stars - size - extra - numEmpires;
            return pace.LowerBound(1);
        }

        float GetResearchMultiplier()
        {
            if (!GlobalStats.ModChangeResearchCost)
                return 1f;

            int idealNumPlayers   = (int)GalaxySize + 3;
            float galSizeModifier = GalaxySize <= GalSize.Medium 
                ? ((int)GalaxySize / 2f).LowerBound(0.25f) // 0.25, 0.5 or 1
                : 1 + ((int)GalaxySize - (int)GalSize.Medium) * 0.25f; // 1.25, 1.5, 1.75, 2

            float extraPlanetsMod = 1 + ExtraPlanets * 0.25f;
            int numMajorEmpires = EmpireList.Count(e => !e.IsFaction);
            float playerRatio     = (float)idealNumPlayers / numMajorEmpires;
            float settingsRatio   = galSizeModifier * extraPlanetsMod * playerRatio * StarsModifier;

            return settingsRatio;
        }

        public float ProductionPace => 1 + (Pace - 1) * 0.5f;
    }
}
