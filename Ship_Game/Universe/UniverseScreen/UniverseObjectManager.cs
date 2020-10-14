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

        public readonly AggregatePerfTimer SysPerf   = new AggregatePerfTimer();
        public readonly AggregatePerfTimer ShipsPerf = new AggregatePerfTimer();
        public readonly AggregatePerfTimer ProjPerf  = new AggregatePerfTimer();

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
        }

        public Ship FindShip(in Guid guid)
        {
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
            lock (Projectiles)
            {
                return Projectiles.FilterSelect(
                p => p.Type == GameObjectType.Proj,
                p => new SavedGame.ProjectileSaveData
                {
                    Owner = p.Owner?.guid ?? p.Planet.guid,
                    Weapon   = p.Weapon.UID,
                    Duration = p.Duration,
                    Rotation = p.Rotation,
                    Velocity = p.Velocity,
                    Position = p.Center,
                });
            }
        }
        
        public SavedGame.BeamSaveData[] GetBeamSaveData()
        {
            lock (Projectiles)
            {
                return Projectiles.FilterSelect(
                p => p.Type == GameObjectType.Beam && (p.Owner != null || p.Planet != null),
                p =>
                {
                    var beam = (Beam)p;
                    return new SavedGame.BeamSaveData
                    {
                        Owner = p.Owner?.guid ?? p.Planet.guid,
                        Weapon = p.Weapon.UID,
                        Duration = p.Duration,
                        Source = beam.Source,
                        Destination = beam.Destination,
                        ActualHitDestination = beam.ActualHitDestination,
                        Target = beam.Target is Ship ship ? ship.guid : Guid.Empty,
                    };
                });
            }
        }

        // NOTE: SLOW !! Should only be used for UNIT TESTS
        // Only returns projectiles NOT BEAMS
        public Array<Projectile> GetProjectiles(Ship ship)
        {
            var projectiles = new Array<Projectile>();
            lock (Projectiles)
            {
                for (int i = 0; i < Projectiles.Count; ++i)
                {
                    Projectile p = Projectiles[i];
                    if (p.Type == GameObjectType.Proj && p.Owner == ship)
                        projectiles.Add(p);
                }
            }
            return projectiles;
        }

        public void Add(GameplayObject go)
        {
            if (go.Type == GameObjectType.Ship)
            {
                lock (Ships)
                {
                    Ships.Add((Ship)go);
                }
                lock (Objects)
                {
                    Objects.Add(go);
                }
            }
            else if (go.Type == GameObjectType.Beam || go.Type == GameObjectType.Proj)
            {
                lock (Projectiles)
                {
                    Projectiles.Add((Projectile)go);
                }
                lock (Objects)
                {
                    Objects.Add(go);
                }
            }
        }

        public void Clear()
        {
            lock (Ships)
            {
                for (int i = 0; i < Ships.Count; ++i)
                    Ships[i]?.RemoveFromUniverseUnsafe();
                Ships.Clear();
            }
            lock (Projectiles)
            {
                Projectiles.Clear();
            }
            lock (Objects)
            {
                Objects.Clear();
            }
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
            UpdateLists();

            UpdateAllSystems(timeStep);
            UpdateAllShips(timeStep);
            UpdateAllProjectiles(timeStep);

            lock (Objects)
            {
                // spatial update will automatically:
                //   add objects with no spatial Id
                //   remove objects that are !Active
                //   update objects that have spatial Id
                Spatial.Update(Objects);
                // remove inactive objects only after Spatial has seen them as inactive
                Objects.RemoveInActiveObjects(); 
            }

            // trigger all Hit events, but only if we are not paused!
            if (timeStep.FixedTime > 0f)
                Spatial.CollideAll(timeStep);

            UpdateVisibleObjects();
        }

        /// <summary>Updates master objects lists, removing inactive objects</summary>
        void UpdateLists()
        {
            lock (Ships)
            {
                for (int i = 0; i < Ships.Count; ++i)
                {
                    Ship ship = Ships[i];
                    if (!ship.Active)
                    {
                        OnShipRemoved?.Invoke(ship);
                        ship.RemoveFromUniverseUnsafe();
                    }
                }
                Ships.RemoveInActiveObjects();
            }

            lock (Projectiles)
            {

                for (int i = 0; i < Projectiles.Count; ++i)
                {
                    Projectile proj = Projectiles[i];
                    if (proj.DieNextFrame)
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

        void UpdateAllSystems(FixedSimTime timeStep)
        {
            if (Universe.IsExiting)
                return;
            
            SysPerf.Start();

            lock (Ships)
            {
                for (int i = 0; i < Ships.Count; ++i)
                    Ships[i].SetSystem(null);
            }

            void UpdateSystems(int start, int end)
            {
                for (int i = start; i < end; ++i)
                {
                    SolarSystem system = UniverseScreen.SolarSystemList[i];
                    system.ShipList.Clear();

                    //int debugId = (system.Name == "Opteris") ? 11 : 0;
                    GameplayObject[] shipsInSystem = Spatial.FindNearby(GameObjectType.Ship,
                                                    system.Position, system.Radius,
                                                    maxResults:10_000/*, debugId:debugId*/);
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

        void UpdateAllShips(FixedSimTime timeStep)
        {
            ShipsPerf.Start();
            bool isSystemView = (Universe.viewState <= UniverseScreen.UnivScreenState.SystemView);
            lock (Ships)
            {
                Ship[] allShips = Ships.GetInternalArrayItems();
                Parallel.For(Ships.Count, (start, end) =>
                {
                    for (int i = start; i < end; ++i)
                    {
                        Ship ship = allShips[i];
                        ship.Update(timeStep);
                        ship.UpdateModulePositions(timeStep, isSystemView);

                        // make sure dead and dying ships can be seen.
                        if (!ship.Active && ship.KnownByEmpires.KnownByPlayer)
                            ship.KnownByEmpires.SetSeenByPlayer();
                    }
                }, Universe.MaxTaskCores);
            }
            ShipsPerf.Stop();
        }

        void UpdateAllProjectiles(FixedSimTime timeStep)
        {
            ProjPerf.Start();
            lock (Projectiles)
            {
                Parallel.For(Projectiles.Count, (start, end) =>
                {
                    for (int i = start; i < end; ++i)
                    {
                        Projectile proj = Projectiles[i];
                        proj.Update(timeStep);
                    }
                }, Universe.MaxTaskCores);
            }
            ProjPerf.Stop();
        }

        void UpdateVisibleObjects()
        {
            RectF worldRect = Universe.GetVisibleWorldRect();
            float radius = Math.Max(worldRect.W, worldRect.H) * 0.5f;
            Vector2 center = worldRect.Center;

            Projectile[] projs = Empty<Projectile>.Array;
            Beam[] beams = Empty<Beam>.Array;

            if (Universe.viewState <= UniverseScreen.UnivScreenState.PlanetView)
            {
                projs = Spatial.FindNearby(GameObjectType.Proj, center, radius, 1024)
                               .FastCast<GameplayObject, Projectile>();

                beams = Spatial.FindNearby(GameObjectType.Beam, center, radius, 1024)
                               .FastCast<GameplayObject, Beam>();
            }

            Ship[] ships = Spatial.FindNearby(GameObjectType.Ship, center, radius, 1024)
                                  .FastCast<GameplayObject, Ship>();

            VisibleProjectiles = projs;
            VisibleBeams = beams;
            VisibleShips = ships;
        }
    }
}
