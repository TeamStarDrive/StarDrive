using System;
using System.Collections.Generic;
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
    public partial class UniverseState
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

        // TODO: This was too hard to fix, so added this placeholder until code is fixed
        public static float DummyPacePlaceholder = 1f;
        public static float DummySettingsResearchModifier = 1f;
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
        public Qtree PlanetsTree;

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
        public IReadOnlyList<Ship> Ships => Objects.GetShips();

        [StarData] public RandomEventManager Events;
        [StarData] public StatTracker Stats;

        // TODO: attempt to stop relying on visual state
        public UniverseScreen Screen;
        
        // TODO: Encapsulate
        public BatchRemovalCollection<SpaceJunk> JunkList = new();

        public DebugInfoScreen DebugWin => Screen.DebugWin;
        public NotificationManager Notifications => Screen.NotificationManager;

        public ShieldManager Shields => Screen.Shields;

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
                    designs.Add((ShipDesign)s.ShipData);
                foreach (Empire e in us.EmpireList)
                {
                    foreach (IShipDesign s in e.ShipsWeCanBuild)
                        designs.Add((ShipDesign)s);
                    foreach (IShipDesign s in e.SpaceStationsWeCanBuild)
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
            }

            void RemapShipDesigns(UniverseState us, ShipDesign fromSave, IShipDesign replaceWith)
            {
                // NOTE: individual ships are updated inside Ship.OnDeserialized()

                foreach (Empire e in us.EmpireList)
                {
                    if (e.CanBuildShip(fromSave))
                    {
                        e.RemoveBuildableShip(fromSave);
                        e.AddBuildableShip(replaceWith);
                    }
                    if (e.CanBuildStation(fromSave)) // a station will appear in both lists
                    {
                        e.RemoveBuildableStation(fromSave);
                        e.AddBuildableStation(replaceWith);
                    }
                }
            }
        }

        [StarData] public SaveState Save;
        [StarData] public byte[] FogMapBytes;

        [StarDataSerialize]
        StarDataDynamicField[] OnSerialize()
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
        void OnDeserialized()
        {
            Initialize(UniverseWidth);

            SaveState save = Save;
            Save = null;
            save.UpdateAllDesignsFromSave(this);

            P.UpdateGlobalStats();
            SettingsResearchModifier = GetResearchMultiplier();
            RemnantPaceModifier = CalcRemnantPace();
            
            // NOTE: This will automatically call AddShipInfluence() to update InfluenceTree
            Objects.AddRange(save.Ships);
            Objects.AddRange(save.Projectiles);

            // updated InfluenceTree with all universe planets
            foreach (Planet planet in AllPlanetsList)
            {
                if (planet.Owner != null)
                    OnPlanetOwnerAdded(planet.Owner, planet);
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

        public void Clear()
        {
            Objects.Clear();
            ClearSystems();
            ClearSpaceJunk();
            ClearEmpires();
            PlanetsDict.Clear();
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

            SolarSystemList.Add(system);
            SystemsTree.Insert(system);

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

        public Planet GetPlanet(int id)
        {
            if (id <= 0) return null;
            if (PlanetsDict.TryGetValue(id, out Planet planet))
                return planet;
            Log.Error($"Planet not found: {id}");
            return null;
        }

        public SolarSystem FindClosestSystem(Vector2 pos)
        {
            return SolarSystemList.FindClosestTo(pos);
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

        public Array<SolarSystem> GetFiveClosestSystems(SolarSystem system)
        {
            return SolarSystemList.FindMinItemsFiltered(5, filter => filter != system,
                                                           select => select.Position.SqDist(system.Position));
        }

        // Returns all solar systems within frustum
        public SolarSystem[] GetVisibleSystems()
        {
            return SolarSystemList.Filter(s => Screen.IsInFrustum(s.Position, s.Radius));
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
            float stars = P.StarsModifier * 4; // 1-8
            float size = (int)P.GalaxySize + 1; // 1-7
            int extra = P.ExtraPlanets; // 1-3
            int numMajorEmpires = EmpireList.Count(e => !e.IsFaction);
            float numEmpires = numMajorEmpires / 2f; // 1-4.5

            float pace = 20 - stars - size - extra - numEmpires;
            return pace.LowerBound(1);
        }

        float GetResearchMultiplier()
        {
            if (!GlobalStats.Settings.ChangeResearchCostBasedOnSize)
                return 1f;

            int idealNumPlayers   = (int)P.GalaxySize + 3;
            float galSizeModifier = P.GalaxySize <= GalSize.Medium 
                ? ((int)P.GalaxySize / 2f).LowerBound(0.25f) // 0.25, 0.5 or 1
                : 1 + ((int)P.GalaxySize - (int)GalSize.Medium) * 0.25f; // 1.25, 1.5, 1.75, 2

            float extraPlanetsMod = 1 + P.ExtraPlanets * 0.25f;
            int numMajorEmpires = EmpireList.Count(e => !e.IsFaction);
            float playerRatio     = (float)idealNumPlayers / numMajorEmpires;
            float settingsRatio   = galSizeModifier * extraPlanetsMod * playerRatio * P.StarsModifier;

            return settingsRatio;
        }

        public float ProductionPace => 1 + (P.Pace - 1) * 0.5f;
    }
}
