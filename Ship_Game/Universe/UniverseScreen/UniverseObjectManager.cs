using System;
using System.Collections.Generic;
using SDUtils;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Ship_Game.Utils;

namespace Ship_Game
{
    /// <summary>
    /// Encapsulates universe object and entity management
    /// </summary>
    public class UniverseObjectManager
    {
        readonly UniverseScreen Universe;
        readonly UniverseState UState;
        readonly SpatialManager Spatial;

        /// <summary>
        /// Should be TRUE by default. Can be used to detect threading issues.
        /// </summary>
        public bool EnableParallelUpdate = true;

        /// <summary>
        /// All objects: ships, projectiles, beams
        /// </summary>
        readonly GameObjectList<GameObject> Objects = new();

        /// <summary>
        /// All ships
        /// </summary>
        readonly GameObjectList<Ship> Ships = new();

        /// <summary>
        /// All projectiles: projectiles, beams
        /// </summary>
        readonly GameObjectList<Projectile> Projectiles = new();

        public readonly AggregatePerfTimer TotalTime = new();
        public readonly AggregatePerfTimer ListTime = new();
        public readonly AggregatePerfTimer SysShipsPerf = new();
        public readonly AggregatePerfTimer SysPerf   = new();
        public readonly AggregatePerfTimer ShipsPerf = new();
        public readonly AggregatePerfTimer ProjPerf  = new();
        public readonly AggregatePerfTimer SensorPerf = new();
        public readonly AggregatePerfTimer VisPerf   = new();

        public Ship[] VisibleShips { get; private set; } = Empty<Ship>.Array;
        public Projectile[] VisibleProjectiles { get; private set; } = Empty<Projectile>.Array;
        public Beam[] VisibleBeams { get; private set; } = Empty<Beam>.Array;

        /// <summary>
        /// Number of currently tracked ships
        /// </summary>
        public int NumShips => Ships.NumBackingItems;

        /// <summary>
        /// Number of currently tracked projectiles
        /// </summary>
        public int NumProjectiles => Projectiles.NumBackingItems;

        /// <summary>
        /// Currently submitted objects (excludes pending)
        /// </summary>
        public IReadOnlyList<Ship> GetShips() => Ships.GetItems();

        public UniverseObjectManager(UniverseScreen uScreen, UniverseState uState, SpatialManager spatial)
        {
            Universe = uScreen;
            UState = uState;
            Spatial = spatial;
        }

        public Ship FindShip(int id) => Ships.Find(id);
        public bool FindShip(int id, out Ship found) => Ships.Find(id, out found);
        public bool ContainsShip(int id) => Ships.Contains(id);

        public GameObject FindObject(int id) => Objects.Find(id);
        public bool FindObject(int id, out GameObject found) => Objects.Find(id, out found);
        public bool ContainsObject(int id) => Objects.Contains(id);

        public SavedGame.ProjectileSaveData[] GetProjectileSaveData()
        {
            var projectiles = Projectiles.GetItems();
            return projectiles.FilterSelect(
                p => p.Active && p.Type == GameObjectType.Proj && (p.Owner != null || p.Planet != null),
                p => new SavedGame.ProjectileSaveData
                {
                    Id = p.Id,
                    OwnerId  = p.Owner?.Id ?? p.Planet.Id,
                    Weapon   = p.Weapon.UID,
                    Duration = p.Duration,
                    Rotation = p.Rotation,
                    Velocity = p.Velocity,
                    Position = p.Position,
                    Loyalty  = p.Loyalty.Id,
                });
        }

        public SavedGame.BeamSaveData[] GetBeamSaveData()
        {
            var projectiles = Projectiles.GetItems();
            return projectiles.FilterSelect(
                p => p.Active && p.Type == GameObjectType.Beam && (p.Owner != null || p.Planet != null),
                p =>
                {
                    var beam = (Beam)p;
                    return new SavedGame.BeamSaveData
                    {
                        Id = beam.Id,
                        OwnerId  = p.Owner?.Id ?? p.Planet.Id,
                        Weapon   = p.Weapon.UID,
                        Duration = p.Duration,
                        Source   = beam.Source,
                        Destination = beam.Destination,
                        ActualHitDestination = beam.ActualHitDestination,
                        TargetId  = beam.Target is Ship ship ? ship.Id : 0,
                        Loyalty = p.Loyalty.Id,
                    };
                });
        }
        
        // NOTE: SLOW !! Should only be used for UNIT TESTS
        public Projectile[] GetAllProjectilesAndBeams()
        {
            return Projectiles.GetBackItemsSlow();
        }

        // NOTE: SLOW !! Should only be used for UNIT TESTS
        // Only returns projectiles NOT BEAMS
        public Projectile[] GetProjectiles(Ship ship)
        {
            var projectiles = Projectiles.GetBackItemsSlow();
            return projectiles.Filter(
                p => p.Active && p.Type == GameObjectType.Proj && p.Owner == ship);
        }

        /// <summary>SLOW !! Only for UNIT TESTS</summary>
        public Beam[] GetBeams(Ship ship)
        {
            var projectiles = Projectiles.GetBackItemsSlow();
            return projectiles.FilterSelect(
                p => p.Active && p.Type == GameObjectType.Beam && p.Owner == ship,
                p => (Beam)p);
        }

        /// <summary>DEFERRED: Thread-safely Adds a new Object to the Universe</summary>
        public void Add(GameObject go)
        {
            if (go.Type == GameObjectType.Ship)
            {
                Add((Ship)go);
            }
            else if (go.Type == GameObjectType.Beam || go.Type == GameObjectType.Proj)
            {
                Add((Projectile)go);
            }
        }
        /// <summary>Thread-safely Adds a new Ship to the Universe</summary>
        public void Add(Ship ship)
        {
            ship.ReinsertSpatial = true;
            Ships.Add(ship);
            Objects.Add(ship);
        }
        /// <summary>Thread-safely Adds a new PROJECTILE or BEAM to the Universe</summary>
        public void Add(Projectile projectile)
        {
            projectile.ReinsertSpatial = true;
            Projectiles.Add(projectile);
            Objects.Add(projectile);
        }

        public void Clear()
        {
            Ships.ApplyChanges();

            var ships = Ships.GetItems();
            for (int i = 0; i < ships.Length; ++i)
                ships[i].RemoveFromUniverseUnsafe();

            Ships.ClearAndApplyChanges();
            Projectiles.ClearAndApplyChanges();
            Objects.ClearAndApplyChanges();

            Spatial.Clear();
        }
        
        /// <summary>
        /// Perform multiple steps to update all universe objects
        /// -) Updates object lists, removing dead objects
        /// -) Updates all systems, assigning ships to systems
        /// -) Updates all ships and modules
        /// -) Updates spatial subdivision information for world objects
        /// -) Resolves collisions between all world objects
        /// -) Updates list of visible Ships, Projectiles, Beams
        /// </summary>
        /// <param name="timeStep"></param>
        public void Update(FixedSimTime timeStep)
        {
            // crash in findnearby when on game over screen
            if (UState.GameOver || Universe.IsExiting)
                return;

            TotalTime.Start();
            
            bool isRunning = timeStep.FixedTime > 0f;
            bool isUIThread = Universe.IsUIThread;

            // only remove and kill objects if game is not paused
            UpdateLists(removeInactiveObjects: isRunning);
            UpdateAllSystems(timeStep);
            UpdateAllShips(timeStep);
            UpdateAllProjectiles(timeStep);

            if (isRunning)
            {
                // spatial update will automatically:
                //   add objects with no spatial Id
                //   remove objects that are !Active
                //   update objects that have spatial Id
                {
                    var objects = Objects.GetItems();
                    Spatial.Update(objects);
                    // remove inactive objects only after Spatial has seen them as inactive
                    Objects.RemoveInActiveAndApplyChanges();
                }

                // trigger all Hit events
                Spatial.CollideAll(timeStep, showCollisions: Universe.Debug);

                // update sensors AFTER spatial update, but only if we are not paused!
                UpdateAllSensors(timeStep);

                // now that we have a complete view of the universe
                // allow ships to make decisions
                UpdateAllShipAI(timeStep);
            }

            UpdateVisibleObjects();

            TotalTime.Stop();
        }

        /// <summary>
        /// Run once after save is loaded to restore object visibility
        /// </summary>
        public void InitializeFromSave()
        {
            UpdateLists(removeInactiveObjects: true);
            UpdateAllSystems(FixedSimTime.Zero);
            {
                var objects = Objects.GetItems();
                Spatial.Update(objects);
            }
            UpdateAllSensors(FixedSimTime.Zero);
            UpdateVisibleObjects();
        }

        /// <summary>
        /// Updates master objects lists, removing inactive objects.
        /// This can be called multiple times without serious side effects.
        /// It makes sure cached lists are synced to current universe state
        /// </summary>
        public void UpdateLists(bool removeInactiveObjects = true)
        {
            ListTime.Start();

            Ships.ApplyChanges();
            Projectiles.ApplyChanges();
            Objects.ApplyChanges();

            if (removeInactiveObjects)
            {
                var ships = Ships.GetItems();
                for (int i = 0; i < ships.Length; ++i)
                {
                    Ship ship = ships[i];
                    if (!ship.Active)
                    {
                        UState.OnShipRemoved(ship);
                        ship.RemoveFromUniverseUnsafe();
                    }
                    else
                    {
                        // apply loyalty change and make sure it's reinserted to Spatial with new loyalty
                        bool loyaltyChanged = ship.LoyaltyTracker.Update(ship);
                        if (loyaltyChanged)
                            ship.ReinsertSpatial = true;
                    }
                }

                Ships.RemoveInActiveAndApplyChanges();

                var projectiles = Projectiles.GetItems();
                for (int i = 0; i < projectiles.Length; ++i)
                {
                    Projectile proj = projectiles[i];
                    if (proj.Active && proj.DieNextFrame)
                    {
                        proj.Die(proj, false);
                    }
                    else if (proj is Beam beam)
                    {
                        if (beam.Owner?.Active == false)
                        {
                            beam.Die(beam, false);
                        }
                    }
                }

                Projectiles.RemoveInActiveAndApplyChanges();
            }

            for (int x = 0; x < EmpireManager.Empires.Count; x++)
            {
                var empire = EmpireManager.Empires[x];
                empire.EmpireShips.UpdatePublicLists();
            }

            ListTime.Stop();
        }

        void UpdateAllSystems(FixedSimTime timeStep)
        {
            if (Universe.IsExiting)
                return;

            SysShipsPerf.Start();
            UpdateSolarSystemShips();
            SysShipsPerf.Stop();

            SysPerf.Start();
            for (int i = 0; i < UState.Systems.Count; ++i)
            {
                // TODO: SolarSystem.Update is not thread safe because of resource loading
                SolarSystem system = UState.Systems[i];
                system.Update(timeStep, Universe);
            }
            SysPerf.Stop();
        }

        readonly HashSet<int> ShipsInSystems = new(); // by Ship.Id

        void UpdateSolarSystemShips()
        {
            Ship[] allShips = Ships.GetItems();
            ShipsInSystems.Clear();

            void UpdateSystems(int start, int end)
            {
                HashSet<int> shipsInSystems = new();
                for (int i = start; i < end; ++i)
                {
                    SolarSystem system = UState.Systems[i];
                    system.ShipList.Clear();
                    if (allShips.Length == 0)
                        continue; // all ships were killed, nothing to do here

                    //int debugId = (system.Name == "Opteris") ? 11 : 0;
                    GameObject[] shipsInSystem = Spatial.FindNearby(GameObjectType.Ship,
                                                                        system.Position, system.Radius,
                                                                        maxResults:allShips.Length/*, debugId:debugId*/);
                    for (int j = 0; j < shipsInSystem.Length; ++j)
                    {
                        var ship = (Ship)shipsInSystem[j];

                        system.ShipList.Add(ship);
                        shipsInSystems.Add(ship.Id); // this ship was seen in a system

                        ship.SetSystem(system);
                        system.SetExploredBy(ship.Loyalty);
                    }
                }

                lock (ShipsInSystems)
                {
                    foreach (int id in shipsInSystems)
                        ShipsInSystems.Add(id);
                }
            }

            //UpdateSystems(0, UState.Systems.Count);
            Parallel.For(UState.Systems.Count, UpdateSystems, Universe.MaxTaskCores);

            // now set null systems for ships not found in any solar system
            for (int i = 0; i < allShips.Length; ++i)
            {
                Ship ship = allShips[i];
                if (!ShipsInSystems.Contains(ship.Id))
                    ship.SetSystem(null);
            }
        }

        void UpdateAllShips(FixedSimTime timeStep)
        {
            ShipsPerf.Start();

            bool isSystemView = Universe.IsSystemViewOrCloser;
            Ship[] allShips = Ships.GetItems();

            void UpdateShips(int start, int end)
            {
                bool debug = Universe.Debug;
                for (int i = start; i < end; ++i)
                {
                    Ship ship = allShips[i];
                    ship.Update(timeStep);
                    ship.UpdateModulePositions(timeStep, isSystemView);

                    // make sure dying ships can be seen. and show all ships in DEBUG
                    if ((ship.Dying && ship.KnownByEmpires.KnownByPlayer) || debug)
                        ship.KnownByEmpires.SetSeenByPlayer();
                }
            }

            if (EnableParallelUpdate)
                Parallel.For(allShips.Length, UpdateShips, Universe.MaxTaskCores);
            else
                UpdateShips(0, allShips.Length);

            ShipsPerf.Stop();
        }

        void UpdateAllProjectiles(FixedSimTime timeStep)
        {
            ProjPerf.Start();

            if (timeStep.FixedTime > 0)
            {
                Projectile[] allProjectiles = Projectiles.GetItems();
                void UpdateProjectiles(int start, int end)
                {
                    for (int i = start; i < end; ++i)
                    {
                        Projectile proj = allProjectiles[i];
                        proj.Update(timeStep);
                    }
                }

                if (EnableParallelUpdate)
                    Parallel.For(allProjectiles.Length, UpdateProjectiles, Universe.MaxTaskCores);
                else
                    UpdateProjectiles(0, allProjectiles.Length);
            }

            ProjPerf.Stop();
        }

        public int Scans;
        public int ScansPerSec;
        int ScansAcc;

        void UpdateAllSensors(FixedSimTime timeStep)
        {
            SensorPerf.Start();
            Scans = 0;

            Ship[] allShips = Ships.GetItems();
            void UpdateSensors(int start, int end)
            {
                for (int i = start; i < end; ++i)
                {
                    Ship ship = allShips[i];
                    if (ship.Active && !ship.Dying)
                        ship.UpdateSensors(timeStep);
                }
            }

            if (EnableParallelUpdate)
                Parallel.For(allShips.Length, UpdateSensors, Universe.MaxTaskCores);
            else
                UpdateSensors(0, allShips.Length);

            ScansAcc += Scans;

            if (SensorPerf.Stop())
            {
                ScansPerSec = ScansAcc;
                ScansAcc = 0;
            }
        }

        void UpdateAllShipAI(FixedSimTime timeStep)
        {
            Ship[] allShips = Ships.GetItems();
            void UpdateAI(int start, int end)
            {
                for (int i = start; i < end; ++i)
                {
                    Ship ship = allShips[i];
                    if (ship.Active && !ship.Dying && !ship.EMPDisabled)
                        ship.AI.Update(timeStep);
                }
            }

            if (EnableParallelUpdate)
                Parallel.For(allShips.Length, UpdateAI, Universe.MaxTaskCores);
            else
                UpdateAI(0, allShips.Length);
        }

        void UpdateVisibleObjects()
        {
            VisPerf.Start();

            AABoundingBox2D visibleWorld = Universe.VisibleWorldRect;

            Projectile[] projs = Empty<Projectile>.Array;
            Beam[] beams = Empty<Beam>.Array;

            if (Universe.IsPlanetViewOrCloser)
            {
                projs = Spatial.FindNearby(GameObjectType.Proj, visibleWorld, 2048)
                               .FastCast<GameObject, Projectile>();

                beams = Spatial.FindNearby(GameObjectType.Beam, visibleWorld, 2048)
                               .FastCast<GameObject, Beam>();
            }

            Ship[] ships = Spatial.FindNearby(GameObjectType.Ship, visibleWorld, 1024)
                                  .FastCast<GameObject, Ship>();

            // Reset frustum value for ship visible in previous frame
            SetInFrustum(VisibleProjectiles, false);
            SetInFrustum(VisibleBeams, false);
            SetInFrustum(VisibleShips, false);

            // And now set this frame's objects as visible
            SetInFrustum(projs, true);
            SetInFrustum(beams, true);
            SetInFrustum(ships, true);

            VisibleProjectiles = projs;
            VisibleBeams = beams;
            VisibleShips = ships;

            VisPerf.Stop();
        }

        static void SetInFrustum<T>(T[] objects, bool inFrustum) where T : GameObject
        {
            for (int i = 0; i < objects.Length; ++i)
                objects[i].InFrustum = inFrustum;
        }
    }
}
