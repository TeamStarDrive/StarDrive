﻿using System;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    /// <summary>
    /// Encapsulates universe object and entity management
    /// </summary>
    public class UniverseObjectManager
    {
        readonly UniverseScreen Universe;
        readonly SpatialManager Spatial;

        /// <summary>
        /// Should be TRUE by default. Can be used to detect threading issues.
        /// </summary>
        static bool EnableParallelUpdate = true;

        /// <summary>
        /// All objects: ships, projectiles, beams
        /// </summary>
        public readonly Array<GameplayObject> Objects = new Array<GameplayObject>();

        /// <summary>
        /// All ships
        /// </summary>
        public readonly Array<Ship> Ships = new Array<Ship>();

        /// <summary>
        /// All projectiles: projectiles, beams
        /// </summary>
        public readonly Array<Projectile> Projectiles = new Array<Projectile>();

        // pending ships and projectiles submitted to object manager
        readonly Array<Ship> PendingShips = new Array<Ship>();
        readonly Array<Projectile> PendingProjectiles = new Array<Projectile>();

        readonly object PendingShipLocker = new object();
        readonly object PendingProjectileLocker = new object();
        // locks down any modification of object lists
        readonly object ShipsLocker = new object();
        readonly object ProjectilesLocker = new object();
        readonly object AllObjectsLocker = new object();

        public readonly AggregatePerfTimer TotalTime = new AggregatePerfTimer();
        public readonly AggregatePerfTimer ListTime = new AggregatePerfTimer();
        public readonly AggregatePerfTimer SysPerf   = new AggregatePerfTimer();
        public readonly AggregatePerfTimer ShipsPerf = new AggregatePerfTimer();
        public readonly AggregatePerfTimer ProjPerf  = new AggregatePerfTimer();
        public readonly AggregatePerfTimer SensorPerf = new AggregatePerfTimer();
        public readonly AggregatePerfTimer VisPerf   = new AggregatePerfTimer();

        /// <summary>
        /// Invoked when a Ship is removed from the universe
        /// OnShipRemove(Ship ship, bool onUIThread);
        /// </summary>
        public event Action<Ship> OnShipRemoved;


        public Ship[] VisibleShips { get; private set; } = Empty<Ship>.Array;
        public Projectile[] VisibleProjectiles { get; private set; } = Empty<Projectile>.Array;
        public Beam[] VisibleBeams { get; private set; } = Empty<Beam>.Array;


        public UniverseObjectManager(UniverseScreen universe, SpatialManager spatial, UniverseData data)
        {
            Universe = universe;
            Spatial = spatial;

            Ships.AddRange(data.MasterShipList);
            Projectiles.AddRange(data.MasterProjectileList);

            Objects.AddRange(Ships);
            Objects.AddRange(Projectiles);

            foreach (GameplayObject go in Objects)
                if (!go.Active)
                    Log.Warning($"Inactive object added from savegame: {go}");
        }

        public Ship FindShip(in Guid guid)
        {
            if (guid == Guid.Empty)
                return null;
            lock (ShipsLocker)
            {
                for (int i = 0; i < Ships.Count; ++i)
                    if (Ships[i].guid == guid)
                        return Ships[i];
            }
            lock (PendingShipLocker)
            {
                for (int i = 0; i < PendingShips.Count; ++i)
                    if (PendingShips[i].guid == guid)
                        return PendingShips[i];
            }
            return null;
        }

        public GameplayObject FindObject(in Guid guid)
        {
            // TODO: Currently only Ships are supported,
            //       but in the future we will use GameplayObject.Id instead of the slow Guid
            return FindShip(guid);
        }

        public SavedGame.ProjectileSaveData[] GetProjectileSaveData()
        {
            lock (ProjectilesLocker)
            {
                return Projectiles.FilterSelect(
                p => p.Active && p.Type == GameObjectType.Proj && (p.Owner != null || p.Planet != null),
                p => new SavedGame.ProjectileSaveData
                {
                    Owner    = p.Owner?.guid ?? p.Planet.guid,
                    Weapon   = p.Weapon.UID,
                    Duration = p.Duration,
                    Rotation = p.Rotation,
                    Velocity = p.Velocity,
                    Position = p.Position,
                    Loyalty  = p.Loyalty.Id,
                });
            }
        }

        public SavedGame.BeamSaveData[] GetBeamSaveData()
        {
            lock (ProjectilesLocker)
            {
                return Projectiles.FilterSelect(
                p => p.Active && p.Type == GameObjectType.Beam && (p.Owner != null || p.Planet != null),
                p =>
                {
                    var beam = (Beam)p;
                    return new SavedGame.BeamSaveData
                    {
                        Owner    = p.Owner?.guid ?? p.Planet.guid,
                        Weapon   = p.Weapon.UID,
                        Duration = p.Duration,
                        Source   = beam.Source,
                        Destination = beam.Destination,
                        ActualHitDestination = beam.ActualHitDestination,
                        Target  = beam.Target is Ship ship ? ship.guid : Guid.Empty,
                        Loyalty = p.Loyalty.Id,
                    };
                });
            }
        }

        // NOTE: SLOW !! Should only be used for UNIT TESTS
        // Only returns projectiles NOT BEAMS
        public Array<Projectile> GetProjectiles(Ship ship)
        {
            var projectiles = new Array<Projectile>();
            lock (ProjectilesLocker)
            {
                for (int i = 0; i < Projectiles.Count; ++i)
                {
                    Projectile p = Projectiles[i];
                    if (p.Type == GameObjectType.Proj && p.Owner == ship)
                        projectiles.Add(p);
                }
            }
            lock (PendingProjectileLocker)
            {
                for (int i = 0; i < PendingProjectiles.Count; ++i)
                {
                    Projectile p = PendingProjectiles[i];
                    if (p.Type == GameObjectType.Proj && p.Owner == ship)
                        projectiles.Add(p);
                }
            }
            return projectiles;
        }

        /// <summary>SLOW !! Only for UNIT TESTS</summary>
        public Beam[] GetBeams(Ship ship)
        {
            lock (ProjectilesLocker)
            {
                return Projectiles.FilterSelect(
                    p => p.Active && p.Type == GameObjectType.Beam && p.Owner == ship,
                    p => (Beam)p);
            }
        }

        /// <summary>Thread-safely Adds a new Object to the Universe</summary>
        public void Add(GameplayObject go)
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
            lock (PendingShipLocker)
                PendingShips.Add(ship);
        }
        /// <summary>Thread-safely Adds a new PROJECTILE or BEAM to the Universe</summary>
        public void Add(Projectile projectile)
        {
            lock (PendingProjectileLocker)
                PendingProjectiles.Add(projectile);
        }

        public void Clear()
        {
            lock (PendingShipLocker)
                PendingShips.Clear();
            
            lock (PendingProjectileLocker)
                PendingProjectiles.Clear();

            lock (ShipsLocker)
            {
                for (int i = 0; i < Ships.Count; ++i)
                    Ships[i]?.RemoveFromUniverseUnsafe();
                Ships.Clear();
            }
            lock (ProjectilesLocker)
                Projectiles.Clear();
            lock (AllObjectsLocker)
                Objects.Clear();

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
            if (Universe.GameOver || Universe.IsExiting)
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
                lock (AllObjectsLocker)
                {
                    Spatial.Update(Objects);
                    // remove inactive objects only after Spatial has seen them as inactive
                    Objects.RemoveInActiveObjects();
                }
                
                // trigger all Hit events
                Spatial.CollideAll(timeStep);

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
            lock (AllObjectsLocker)
            {
                Spatial.Update(Objects);
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

            lock (PendingShipLocker)
            {
                lock (ShipsLocker)
                    Ships.AddRange(PendingShips);
                lock (AllObjectsLocker)
                    Objects.AddRange(PendingShips);
                PendingShips.Clear();
            }
            lock (PendingProjectileLocker)
            {
                lock (ProjectilesLocker)
                    Projectiles.AddRange(PendingProjectiles);
                lock (AllObjectsLocker)
                    Objects.AddRange(PendingProjectiles);
                PendingProjectiles.Clear();
            }

            if (removeInactiveObjects)
            {
                lock (ShipsLocker)
                {
                    for (int i = 0; i < Ships.Count; ++i)
                    {
                        Ship ship = Ships[i];
                        if (!ship.Active)
                        {
                            OnShipRemoved?.Invoke(ship);
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
                    Ships.RemoveInActiveObjects();
                }

                lock (ProjectilesLocker)
                {
                    for (int i = 0; i < Projectiles.Count; ++i)
                    {
                        Projectile proj = Projectiles[i];
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
                    Projectiles.RemoveInActiveObjects();
                }
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

            SysPerf.Start();

            void UpdateSystems(int start, int end)
            {
                for (int i = start; i < end; ++i)
                {
                    SolarSystem system = UniverseScreen.SolarSystemList[i];
                    system.ShipList.Clear();

                    //int debugId = (system.Name == "Opteris") ? 11 : 0;
                    GameplayObject[] shipsInSystem = Spatial.FindNearby(GameObjectType.Ship,
                                                                        system.Position, system.Radius,
                                                                        maxResults:Ships.Count/*, debugId:debugId*/);
                    for (int j = 0; j < shipsInSystem.Length; ++j)
                    {
                        var ship = (Ship)shipsInSystem[j];
                        system.ShipList.Add(ship);
                        ship.SetSystemBackBuffer(system);
                        system.SetExploredBy(ship.loyalty);
                    }
                }
            }

            lock (ShipsLocker)
            {
                for (int i = 0; i < Ships.Count; ++i)
                    Ships[i].SetSystemFromBackBuffer();
            }

            UpdateSystems(0, UniverseScreen.SolarSystemList.Count);
            //Parallel.For(UniverseScreen.SolarSystemList.Count, UpdateSystems, Universe.MaxTaskCores);

            // TODO: SolarSystem.Update is not thread safe because of resource loading
            for (int i = 0; i < UniverseScreen.SolarSystemList.Count; ++i)
            {
                SolarSystem system = UniverseScreen.SolarSystemList[i];
                system.Update(timeStep, Universe);
            }

            SysPerf.Stop();
        }

        void UpdateAllShips(FixedSimTime timeStep)
        {
            ShipsPerf.Start();

            lock (ShipsLocker)
            {
                bool isSystemView = Universe.IsSystemViewOrCloser;
                Ship[] allShips = Ships.GetInternalArrayItems();

                void UpdateShips(int start, int end)
                {
                    bool debug = Universe.Debug;
                    for (int i = start; i < end; ++i)
                    {
                        Ship ship = allShips[i];
                        ship.Update(timeStep);
                        ship.UpdateModulePositions(timeStep, isSystemView);

                        // make sure dying ships can be seen. and show all ships in DEBUG
                        if ((ship.dying && ship.KnownByEmpires.KnownByPlayer) || debug)
                            ship.KnownByEmpires.SetSeenByPlayer();
                    }
                }

                if (EnableParallelUpdate)
                    Parallel.For(Ships.Count, UpdateShips, Universe.MaxTaskCores);
                else
                    UpdateShips(0, Ships.Count);
            }

            ShipsPerf.Stop();
        }

        void UpdateAllProjectiles(FixedSimTime timeStep)
        {
            ProjPerf.Start();

            if (timeStep.FixedTime > 0)
            {
                lock (ProjectilesLocker)
                {
                    Projectile[] allProjectiles = Projectiles.GetInternalArrayItems();
                    void UpdateProjectiles(int start, int end)
                    {
                        for (int i = start; i < end; ++i)
                        {
                            Projectile proj = allProjectiles[i];
                            proj.Update(timeStep);
                        }
                    }

                    if (EnableParallelUpdate)
                        Parallel.For(Projectiles.Count, UpdateProjectiles, Universe.MaxTaskCores);
                    else
                        UpdateProjectiles(0, Projectiles.Count);
                }
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

            lock (ShipsLocker)
            {
                Ship[] allShips = Ships.GetInternalArrayItems();
                void UpdateSensors(int start, int end)
                {
                    for (int i = start; i < end; ++i)
                    {
                        Ship ship = allShips[i];
                        if (ship.Active && !ship.dying)
                            ship.UpdateSensorsAndInfluence(timeStep);
                    }
                }

                if (EnableParallelUpdate)
                    Parallel.For(Ships.Count, UpdateSensors, Universe.MaxTaskCores);
                else
                    UpdateSensors(0, Ships.Count);
            }

            ScansAcc += Scans;

            if (SensorPerf.Stop())
            {
                ScansPerSec = ScansAcc;
                ScansAcc = 0;
            }
        }

        void UpdateAllShipAI(FixedSimTime timeStep)
        {
            lock (ShipsLocker)
            {
                Ship[] allShips = Ships.GetInternalArrayItems();
                void UpdateAI(int start, int end)
                {
                    for (int i = start; i < end; ++i)
                    {
                        Ship ship = allShips[i];
                        if (ship.Active && !ship.dying && !ship.EMPdisabled)
                            ship.AI.Update(timeStep);
                    }
                }

                if (EnableParallelUpdate)
                    Parallel.For(Ships.Count, UpdateAI, Universe.MaxTaskCores);
                else
                    UpdateAI(0, Ships.Count);
            }
        }

        void UpdateVisibleObjects()
        {
            VisPerf.Start();

            AABoundingBox2D visibleWorld = Universe.GetVisibleWorldRect();

            Projectile[] projs = Empty<Projectile>.Array;
            Beam[] beams = Empty<Beam>.Array;

            if (Universe.IsPlanetViewOrCloser)
            {
                projs = Spatial.FindNearby(GameObjectType.Proj, visibleWorld, 2048)
                               .FastCast<GameplayObject, Projectile>();

                beams = Spatial.FindNearby(GameObjectType.Beam, visibleWorld, 2048)
                               .FastCast<GameplayObject, Beam>();
            }

            Ship[] ships = Spatial.FindNearby(GameObjectType.Ship, visibleWorld, 1024)
                                  .FastCast<GameplayObject, Ship>();

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

        static void SetInFrustum<T>(T[] objects, bool inFrustum) where T : GameplayObject
        {
            for (int i = 0; i < objects.Length; ++i)
                objects[i].InFrustum = inFrustum;
        }
    }
}
