using System;
using Microsoft.Xna.Framework;
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

        public readonly AggregatePerfTimer TotalTime = new AggregatePerfTimer();
        public readonly AggregatePerfTimer ListTime = new AggregatePerfTimer();
        public readonly AggregatePerfTimer SysPerf   = new AggregatePerfTimer();
        public readonly AggregatePerfTimer ShipsPerf = new AggregatePerfTimer();
        public readonly AggregatePerfTimer ProjPerf  = new AggregatePerfTimer();
        public readonly AggregatePerfTimer VisPerf   = new AggregatePerfTimer();

        /// <summary>
        /// Invoked when a Ship is removed from the universe
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
            if (guid == Guid.Empty) return null;
            lock (Ships)
            {
                for (int i = 0; i < Ships.Count; ++i)
                    if (Ships[i].guid == guid)
                        return Ships[i];
                return null;
            }
        }

        public GameplayObject FindObject(in Guid guid)
        {
            // TODO: Currently only Ships are supported,
            //       but in the future we will use GameplayObject.Id instead of the slow Guid
            return FindShip(guid);
        }

        public SavedGame.ProjectileSaveData[] GetProjectileSaveData()
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
                Position = p.Center,
                Loyalty  = p.Loyalty.Id,
            });
        }
        
        public SavedGame.BeamSaveData[] GetBeamSaveData()
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

        // NOTE: SLOW !! Should only be used for UNIT TESTS
        // Only returns projectiles NOT BEAMS
        public Array<Projectile> GetProjectiles(Ship ship)
        {
            var projectiles = new Array<Projectile>();
            for (int i = 0; i < Projectiles.Count; ++i)
            {
                Projectile p = Projectiles[i];
                if (p.Type == GameObjectType.Proj && p.Owner == ship)
                    projectiles.Add(p);
            }
            lock (PendingProjectiles)
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

        /// <summary> Only for test </summary>
        public Beam[] GetBeams(Ship ship) => Projectiles.FilterSelect(
            p => p.Active && p.Type == GameObjectType.Beam && p.Owner == ship,
            p => (Beam)p);

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
            lock (PendingShips)
                PendingShips.Add(ship);
        }
        /// <summary>Thread-safely Adds a new PROJECTILE or BEAM to the Universe</summary>
        public void Add(Projectile projectile)
        {
            lock (PendingProjectiles)
                PendingProjectiles.Add(projectile);
        }

        public void Clear()
        {
            lock (PendingShips)
                PendingShips.Clear();
            
            lock (PendingProjectiles)
                PendingProjectiles.Clear();

            for (int i = 0; i < Ships.Count; ++i)
                Ships[i]?.RemoveFromUniverseUnsafe();

            Ships.Clear();
            Projectiles.Clear();
            Objects.Clear();
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
            if (Empire.Universe.GameOver) return;

            TotalTime.Start();

            UpdateLists(timeStep);
            UpdateAllSystems(timeStep, Ships);
            UpdateAllShips(timeStep, Ships);
            UpdateAllProjectiles(timeStep, Projectiles);

            // spatial update will automatically:
            //   add objects with no spatial Id
            //   remove objects that are !Active
            //   update objects that have spatial Id
            Spatial.Update(Objects);
            // remove inactive objects only after Spatial has seen them as inactive
            Objects.RemoveInActiveObjects(); 

            // trigger all Hit events, but only if we are not paused!
            if (timeStep.FixedTime > 0f)
                Spatial.CollideAll(timeStep);

            UpdateVisibleObjects();

            TotalTime.Stop();
        }

        /// <summary>Updates master objects lists, removing inactive objects</summary>
        void UpdateLists(FixedSimTime timeStep)
        {
            ListTime.Start();

            lock (PendingShips)
            {
                Ships.AddRange(PendingShips);
                Objects.AddRange(PendingShips);
                PendingShips.Clear();
            }
            lock (PendingProjectiles)
            {
                Projectiles.AddRange(PendingProjectiles);
                Objects.AddRange(PendingProjectiles);
                PendingProjectiles.Clear();
            }

            // only remove and kill objects if game is not paused
            if (timeStep.FixedTime > 0)
            {
                for (int i = 0; i < Ships.Count; ++i)
                {
                    Ship ship = Ships[i];
                    if (!ship.Active)
                    {
                        Log.Info($"Removing inactive ship: {ship}");
                        OnShipRemoved?.Invoke(ship);
                        ship.RemoveFromUniverseUnsafe();
                    }
                }
                Ships.RemoveInActiveObjects();

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

            ListTime.Stop();
        }

        void UpdateAllSystems(FixedSimTime timeStep, Array<Ship> ships)
        {
            if (Universe.IsExiting)
                return;
            
            SysPerf.Start();

            for (int i = 0; i < ships.Count; ++i)
                ships[i].SetSystem(null);

            void UpdateSystems(int start, int end)
            {
                for (int i = start; i < end; ++i)
                {
                    SolarSystem system = UniverseScreen.SolarSystemList[i];
                    system.ShipList.Clear();

                    //int debugId = (system.Name == "Opteris") ? 11 : 0;
                    GameplayObject[] shipsInSystem = Spatial.FindNearby(GameObjectType.Ship,
                                                    system.Position, system.Radius,
                                                    maxResults:ships.Count/*, debugId:debugId*/);
                    for (int j = 0; j < shipsInSystem.Length; ++j)
                    {
                        var ship = (Ship)shipsInSystem[j];
                        system.ShipList.Add(ship);
                        ship.SetSystem(system);
                        system.SetExploredBy(ship.loyalty);
                    }
                }
            }

            UpdateSystems(0, UniverseScreen.SolarSystemList.Count);

            //Parallel.For(UniverseScreen.SolarSystemList.Count, (start, end) =>
            //{
            //    UpdateSystems(start, end);
            //}, Universe.MaxTaskCores);

            // TODO: SolarSystem.Update is not thread safe because of resource loading
            for (int i = 0; i < UniverseScreen.SolarSystemList.Count; ++i)
            {
                SolarSystem system = UniverseScreen.SolarSystemList[i];
                system.Update(timeStep, Universe);
            }

            SysPerf.Stop();
        }

        void UpdateAllShips(FixedSimTime timeStep, Array<Ship> ships)
        {
            ShipsPerf.Start();

            bool isSystemView = Universe.IsSystemViewOrCloser;
            Ship[] allShips = ships.GetInternalArrayItems();

            Parallel.For(ships.Count, (start, end) =>
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
            }, Universe.MaxTaskCores);

            ShipsPerf.Stop();
        }

        void UpdateAllProjectiles(FixedSimTime timeStep, Array<Projectile> projectiles)
        {
            ProjPerf.Start();

            if (timeStep.FixedTime > 0)
            {
                Projectile[] allProjectiles = projectiles.GetInternalArrayItems();

                Parallel.For(projectiles.Count, (start, end) =>
                {
                    for (int i = start; i < end; ++i)
                    {
                        Projectile proj = allProjectiles[i];
                        proj.Update(timeStep);
                    }
                }, Universe.MaxTaskCores);
            }

            ProjPerf.Stop();
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
