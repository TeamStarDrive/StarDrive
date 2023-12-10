using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Spatial;
using Ship_Game.Utils;
using static Ship_Game.UniverseScreen;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Universe
{
    /// <summary>
    /// Holds the invisible state of the universe, manages all Ships, Projectiles, Systems, Planets
    /// and UniverseState reference be used to search for those objects.
    ///
    /// </summary>
    [StarDataType]
    public sealed partial class UniverseState : IDisposable
    {
        /// <summary>
        /// This is the RADIUS of the universe
        /// Stars are generated within XY range [-Size, +Size],
        /// so {0,0} is center of the universe
        /// </summary>
        [StarData] public readonly float Size; // TODO: rename to UniverseRadius?

        [StarData] public UniverseParams P;

        public float UniverseWidth => Size*2f;
        public float UniverseRadius => Size;
        [StarData] public Empire Player;
        [StarData] public Empire Cordrazine;
        [StarData] public Empire Remnants;
        [StarData] public Empire Unknown;
        [StarData] public Empire Corsairs;

        [StarData] public UnivScreenState ViewState;
        public bool IsSectorViewOrCloser => ViewState <= UnivScreenState.SectorView;
        public bool IsSystemViewOrCloser => ViewState <= UnivScreenState.SystemView;
        public bool IsPlanetViewOrCloser => ViewState <= UnivScreenState.PlanetView;
        public bool IsShipViewOrCloser   => ViewState <= UnivScreenState.ShipView;
        public bool ExoticFeaturesDisabled => P.DisableMiningOps && P.DisableResearchStations;

        // TODO: This was too hard to fix, so added this placeholder until code is fixed
        public static float DummyProductionPacePlaceholder = 1f;

        [StarData] public float SettingsResearchModifier = 1f;
        public float RemnantPaceModifier = 20;
        public string ResearchRootUIDToDisplay;

        // Global unique ID counter for this Universe
        // Can be used to assign ID-s for any kind of object
        // Id <= 0 is always invalid, valid ID-s start at 1
        [StarData] int UniqueObjectIds;
        
        public bool CanShowDiplomacyScreen = true;
        public bool Paused = true; // always start paused
        public bool Debug;
        public DebugModes DebugMode;
        DebugModes PrevDebugMode;

        public bool GameOver = false;
        [StarData] public bool NoEliminationVictory;
        [StarData] public float GameSpeed = 1f;
        [StarData] public float StarDate = 1000f;

        [StarData] public Vector3d CamPos;

        // generated once during universe generation
        // allows us to define consistent backgrounds between savegames
        [StarData] public int BackgroundSeed;

        /// <summary>
        /// Manages universe objects in a thread-safe manner
        /// </summary>
        public UniverseObjectManager Objects;

        /// <summary>
        /// Spatial search interface for Universe Ships/Projectiles/Beams, updated once per frame
        /// </summary>
        public SpatialManager Spatial;

        /// <summary>
        /// Spatial search interface for all SolarSystems
        /// </summary>
        public GenericQtree SystemsTree;

        /// <summary>
        /// Spatial search interface for all Planets
        /// </summary>
        public GenericQtree PlanetsTree;

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
        [StarData] readonly Array<SolarSystem> SolarSystemList = new();
        [StarData] readonly Array<Planet> AllPlanetsList = new();
        [StarData] public readonly Map<ExplorableGameObject, HashSet<int>> ResearchableSolarBodies = new();
        [StarData] public readonly Array<Planet> MineablePlanets = new();

        // TODO: remove PlanetsDict
        [StarData] readonly Map<int, Planet> PlanetsDict = new();

        // @return All SolarSystems in the Universe
        public IReadOnlyList<SolarSystem> Systems => SolarSystemList;

        // @return All Planets in the Universe
        public IReadOnlyList<Planet> Planets => AllPlanetsList;

        /// <summary>
        /// Thread unsafe view of all ships.
        /// It's only safe to use from simulation thread or when sim is paused
        /// </summary>
        public Ship[] Ships => Objects.GetShips();

        [StarData] public RandomEventManager Events;
        [StarData] public StatTracker Stats;

        // TODO: attempt to stop relying on visual state
        public UniverseScreen Screen;

        // TODO: Encapsulate
        public Array<SpaceJunk> JunkList = new();

        public DebugInfoScreen DebugWin => Screen.DebugWin;
        public NotificationManager Notifications => Screen.NotificationManager;

        public ShieldManager Shields => Screen.Shields;

        public float ExoticPlanetDivisor => (P.ExtraPlanets * 0.8f).LowerBound(1);
        public float DefaultProjectorRadius;

        public readonly RandomBase Random = new ThreadSafeRandom();

        [StarDataConstructor] UniverseState() {}

        public UniverseState(UniverseScreen screen, UniverseParams settings, float universeRadius)
        {
            Screen = screen;
            Size = universeRadius;
            P = settings ?? throw new NullReferenceException(nameof(settings));
            if (Size < 1f)
                throw new ArgumentException("UniverseSize not set!");

            Initialize(universeRadius*2f);

            Events = new();
            Stats = new(this);
        }

        void Initialize(float universeWidth)
        {
            Spatial = new(universeWidth);
            SystemsTree = new(universeWidth, cellThreshold:8, smallestCell:32_000);
            PlanetsTree = new(universeWidth, smallestCell:16_000);

            DefaultProjectorRadius = (float)Math.Round(universeWidth * 0.02f);
            Influence = new(universeWidth, DefaultProjectorRadius);

            // Screen will be null during deserialization, so it must be set later
            Objects = new(Screen, this, Spatial);
        }

        public void OnUniverseScreenLoaded(UniverseScreen screen)
        {
            Screen = screen;
            Objects.Universe = screen;
            Objects.UpdateLists(removeInactiveObjects: false);
        }

        [StarDataType]
        public class SaveState
        {
            // globally stored ship designs
            [StarData] public ShipDesign[] Designs;

            [StarData] public IReadOnlyList<Ship> Ships;
            [StarData] public IReadOnlyList<Projectile> Projectiles;

            // gather a list of all designs in the universe
            public void SetAllDesigns(UniverseState us)
            {
                HashSet<ShipDesign> designs = new();
                foreach (Ship s in Ships)
                {
                    // double check that these designs are valid, there could be weird anomalies which should not exist in the save
                    if (s.ShipData.IsValidDesign)
                        designs.Add((ShipDesign)s.ShipData);
                }
                foreach (Empire e in us.EmpireList)
                {
                    foreach (IShipDesign s in e.ShipsWeCanBuild)
                        if (s.IsValidDesign)
                            designs.Add((ShipDesign)s);
                    foreach (IShipDesign s in e.SpaceStationsWeCanBuild)
                        if (s.IsValidDesign)
                            designs.Add((ShipDesign)s);
                }
                Designs = designs.ToArr();
            }

            // restored all designs
            public void UpdateAllDesignsFromSave(UniverseState us)
            {
                foreach (ShipDesign fromSave in Designs)
                {
                    if (fromSave.IsAnExistingSavedDesign)
                    {
                        // remap to use the existing one
                        IShipDesign existing = ResourceManager.Ships.GetDesign(fromSave.Name);
                        RemapShipDesigns(us, fromSave, existing);
                    }
                }

                // FIXUP remove duplicates in all Empire build lists
                //       this will fix broken savegames that accidentally suffer from duplicate ship designs
                foreach (Empire e in us.EmpireList)
                {
                    e.RemoveInvalidShipDesigns();
                    e.RemoveDuplicateShipDesigns();
                }
            }

            // replace a design from the savegame with an existing template in all the empire Build lists
            static void RemapShipDesigns(UniverseState us, IShipDesign fromSave, IShipDesign replaceWith)
            {
                // NOTE: individual ships are updated inside Ship.OnDeserialized()

                foreach (Empire e in us.EmpireList)
                {
                    // the buildable ships list contains Ships and Stations/Platforms
                    if (e.CanBuildShip(fromSave))
                    {
                        e.RemoveBuildableShip(fromSave);
                        e.AddBuildableShip(replaceWith); // will also add it as a Station

                        // check all planet construction queues as well
                        foreach (Planet p in e.GetPlanets())
                        {
                            foreach (QueueItem qi in p.ConstructionQueue)
                            {
                                if (qi.isShip && qi.ShipData == fromSave)
                                {
                                    qi.ShipData = replaceWith;
                                }
                            }
                        }
                    }
                }
            }
        }

        [StarData] public SaveState Save;
        [StarData] public byte[] FogMapBytes;

        [StarDataSerialize]
        public StarDataDynamicField[] OnSerialize()
        {
            // clean up and submit objects before saving
            Objects.UpdateLists(removeInactiveObjects: true);

            var save = new SaveState
            {
                Ships = Objects.GetShips(),
                Projectiles = Objects.GetProjectiles()
            };
            save.SetAllDesigns(this);

            // FogMap is converted to a Alpha bytes so that it can be included in the savegame
            var fogMapBytes = Screen.ContentManager.RawContent.TexExport.ToAlphaBytes(Screen.FogMap);

            return new StarDataDynamicField[]
            {
                new (nameof(Save), save),
                new (nameof(FogMapBytes), fogMapBytes)
            };
        }

        // Only call OnDeserialized evt if Empire and Ship have finished their events
        [StarDataDeserialized(typeof(Empire), typeof(Ship), typeof(Projectile),
                              typeof(Beam), typeof(UniverseParams))]
        public void OnDeserialized()
        {
            Initialize(UniverseWidth);

            SaveState save = Save;
            Save = null;
            save.UpdateAllDesignsFromSave(this);

            CalcInitialSettings();

            // NOTE: This will automatically call AddShipInfluence() to update InfluenceTree
            Objects.AddRange(save.Ships);
            Objects.AddRange(save.Projectiles);

            // updated InfluenceTree with all universe planets
            foreach (Planet planet in AllPlanetsList)
            {
                if (planet.Owner != null)
                    OnPlanetOwnerAdded(planet.Owner!, planet);
            }

            // update systems tree and planets tree
            SolarSystemList.ForEach(SystemsTree.Insert);
            PlanetsTree.UpdateAll(AllPlanetsList.ToArr());

            InitializeEmpiresFromSave();
        }

        public void SetDebugMode(bool debug)
        {
            Debug = debug;
            // if not in debug, we set DebugMode to Disabled
            DebugMode = debug ? PrevDebugMode : DebugModes.Disabled;
            if (!debug)
                Screen.HideDebugWindow();
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

        ~UniverseState() { Dispose(false); }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            RemoveSceneObjects();

            Objects.Clear();
            ResearchableSolarBodies.Clear();
            ClearSystems();
            ClearEmpires();
            PlanetsDict.Clear();
            Spatial.Destroy();
            SystemsTree.Dispose();
            PlanetsTree.Dispose();
        }

        // This is for UnloadContent / ReloadContent
        public void RemoveSceneObjects()
        {
            var ships = Objects.GetShips();
            var projectiles = Objects.GetProjectiles();
            foreach (Ship s in ships)
                s?.RemoveSceneObject();
            foreach (Projectile p in projectiles)
                p?.RemoveSceneObject();

            foreach (SolarSystem s in SolarSystemList)
            {
                foreach (Planet p in s.PlanetList)
                    p?.RemoveSceneObject();
                foreach (Asteroid a in s.AsteroidsList)
                    a?.RemoveSceneObject();
                foreach (Moon m in s.MoonList)
                    m?.RemoveSceneObject();
            }

            ClearSpaceJunk();
        }

        void ClearSystems()
        {
            foreach (SolarSystem s in SolarSystemList)
            {
                foreach (Planet planet in s.PlanetList)
                {
                    planet.Dispose();
                }

                s.FiveClosestSystems.Clear();
                s.AsteroidsList.Clear();
                s.MoonList.Clear();
            }
            SolarSystemList.Clear();
        }

        void ClearSpaceJunk()
        {
            for (int i = 0; i < JunkList.Count; ++i)
                JunkList[i]?.RemoveSceneObject();
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

            SolarSystemList.Add(system);
            SystemsTree.Insert(system);

            foreach (Planet planet in system.PlanetList)
            {
                if (planet.Id <= 0)
                    throw new InvalidOperationException($"AddSolarSystem Planet.Id must be valid: {planet}");
                if (planet.System != system)
                    throw new InvalidOperationException($"AddSolarSystem Planet.ParentSystem must be valid: {planet.System} != {system}");

                PlanetsDict.Add(planet.Id, planet);
                AllPlanetsList.Add(planet);
                PlanetsTree.Insert(planet);
            }
        }

        public void AddMineablePlanet(Planet planet)
        {
            MineablePlanets.Add(planet);
        }

        public void AddResearchableSolarBody(ExplorableGameObject solarBody)
        {
            ResearchableSolarBodies[solarBody] = new HashSet<int>();
        }

        public void RemoveResearchableSolarBody(ExplorableGameObject solarBody)
        {
            ResearchableSolarBodies.Remove(solarBody);
        }

        public void AddEmpireToResearchableList(Empire empire, ExplorableGameObject target)
        {
            ResearchableSolarBodies[target].Add(empire.Id);
        }

        public void RemoveEmpireFromResearchableList(Empire empire, ExplorableGameObject target)
        {
            ResearchableSolarBodies[target].Remove(empire.Id);
        }

        public Planet GetPlanet(int id)
        {
            if (id <= 0) return null;
            if (PlanetsDict.TryGetValue(id, out Planet planet))
                return planet;
            Log.Error($"Planet not found: {id}");
            return null;
        }

        /// <summary>
        /// Finds a Planet at worldPos, using an additional searchRadius to increase
        /// hit-test area size
        /// </summary>
        /// <param name="worldPos">Center point of the search in World coordinates</param>
        /// <param name="searchRadius">Additional search radius modifier to increase hit distance</param>
        /// <returns></returns>
        public Planet FindPlanetAt(Vector2 worldPos, float searchRadius = 100f)
        {
            SearchOptions opt = new(worldPos, searchRadius);
            return PlanetsTree.FindOne(ref opt) as Planet;
        }

        /// <summary>
        /// Finds a solar system at worldPos, using a hitRadius parameter
        /// </summary>
        /// <param name="worldPos">Center point of the search in World coordinates</param>
        /// <param name="hitRadius">Size of the solar system hit-test circle</param>
        public SolarSystem FindSolarSystemAt(Vector2 worldPos, float hitRadius)
        {
            SearchOptions opt = new(worldPos, 1)
            {
                FilterFunction = (go) => go.Position.InRadius(worldPos, hitRadius),
                DebugId = 2
            };
            return SystemsTree.FindOne(ref opt) as SolarSystem;
        }
        
        public SolarSystem FindClosestSystem(Vector2 worldPos)
        {
            return SolarSystemList.FindClosestTo(worldPos);
        }

        public Array<SolarSystem> GetFiveClosestSystems(SolarSystem system)
        {
            return SolarSystemList.FindMinItemsFiltered(5, filter => filter != system,
                                                           select => select.Position.SqDist(system.Position));
        }

        // Returns all solar systems within frustum
        public SolarSystem[] GetVisibleSystems()
        {
            SolarSystem[] systems = SystemsTree.Find<SolarSystem>(Screen.VisibleWorldRect);
            return systems;
        }

        public Planet[] GetVisiblePlanets()
        {
            SearchOptions opt = new(Screen.VisibleWorldRect)
            {
                MaxResults = Planets.Count
            };
            Planet[] planets = PlanetsTree.Find<Planet>(ref opt);
            return planets;
        }

        public void AddShip(Ship ship)
        {
            Objects.Add(ship);
        }

        public void OnShipAdded(Ship ship)
        {
            AddShipInfluence(ship, ship.Loyalty);
        }

        public void OnShipRemoved(Ship ship)
        {
            RemoveShipInfluence(ship, ship.Loyalty);
            EvtOnShipRemoved?.Invoke(ship);
        }

        // for loyalty changes, transfers influence from old loyalty to new loyalt
        public void UpdateShipInfluence(Ship ship, Empire oldLoyalty, Empire newLoyalty)
        {
            RemoveShipInfluence(ship, oldLoyalty);
            AddShipInfluence(ship, newLoyalty);
        }

        void AddShipInfluence(Ship ship, Empire newLoyalty)
        {
            if (newLoyalty.AddBorderNode(ship))
                Influence.Insert(newLoyalty, ship);
        }

        void RemoveShipInfluence(Ship ship, Empire oldLoyalty)
        {
            if (oldLoyalty.RemoveBorderNode(ship))
                Influence.Remove(oldLoyalty, ship);
        }

        public void OnPlanetOwnerAdded(Empire owner, Planet planet)
        {
            if (planet.Budget?.Owner != owner)
                planet.CreatePlanetBudget(owner);
            owner!.AddBorderNode(planet);
            Influence.Insert(owner, planet);
        }

        public void OnPlanetOwnerRemoved(Empire owner, Planet planet)
        {
            owner.RemoveBorderNode(planet);
            Influence.Remove(owner, planet);
        }

        public void CalcInitialSettings()
        {
            SettingsResearchModifier = GetResearchMultiplier();
            RemnantPaceModifier = CalcRemnantPace();
        }

        float CalcRemnantPace()
        {
            float stars = P.StarsModifier * 4; // 1-8
            int size = (int)P.GalaxySize + 1; // 1-7
            int extra = P.ExtraPlanets; // 1-3
            int numMajorEmpires = EmpireList.Count(e => !e.IsFaction);
            int numEmpires = numMajorEmpires / 2; // 1-4

            float pace = 20 - stars - size - extra - numEmpires;
            return pace.LowerBound(1);
        }

        float GetResearchMultiplier()
        {
            if (!GlobalStats.Defaults.ChangeResearchCostBasedOnSize)
                return 1f;

            int idealNumPlayers   = (int)P.GalaxySize + 3;
            float galSizeModifier;
            switch (P.GalaxySize)
            {
                case GalSize.Tiny:      galSizeModifier = 0.5f;  break;
                case GalSize.Small:     galSizeModifier = 0.75f; break;
                default:
                case GalSize.Medium:    galSizeModifier = 1f;    break;
                case GalSize.Large:     galSizeModifier = 1.15f; break;
                case GalSize.Huge:      galSizeModifier = 1.35f; break;
                case GalSize.Epic:      galSizeModifier = 1.6f;  break;
                case GalSize.TrulyEpic: galSizeModifier = 1.9f;  break;
            }

            float extraPlanetsMod = 1 + P.ExtraPlanets*0.25f;
            int numMajorEmpires = EmpireList.Count(e => !e.IsFaction);
            float playerRatio     = (float)idealNumPlayers / numMajorEmpires;
            float settingsRatio   = galSizeModifier * extraPlanetsMod * playerRatio * P.StarsModifier;

            return settingsRatio;
        }

        public float ProductionPace => 1 + (P.Pace - 1) * 0.5f;
    }
}
