using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        /// <summary>
        /// Invoked when a Ship is removed from the universe
        /// </summary>
        public event Action<Ship> OnShipRemoved;

        public UniverseObjectManager(UniverseScreen universe, SpatialManager spatial, UniverseData data)
        {
            Universe = universe;
            Spatial = spatial;
            Ships.AddRange(data.MasterShipList);
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

        public Array<Projectile> GetProjectiles(Ship ship)
        {
            var projectiles = new Array<Projectile>();
            lock (Projectiles)
            {
                for (int i = 0; i < Projectiles.Count; ++i)
                {
                    Projectile p = Projectiles[i];
                    if (!(p is Beam) && p.Owner == ship)
                        projectiles.Add(p);
                }
            }
            return projectiles;
        }

        public void AddObject(GameplayObject go)
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
        /// </summary>
        /// <param name="timeStep"></param>
        public void Update(FixedSimTime timeStep)
        {
            UpdateLists();

            UpdateAllSystems(timeStep);
            UpdateAllShips(timeStep);

            lock (Objects)
            {
                Spatial.Update(Objects);
            }

            Spatial.CollideAll(timeStep);
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
            
            lock (Objects)
            {
                Objects.RemoveInActiveObjects();
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

            Parallel.For(UniverseScreen.SolarSystemList.Count, (start, end) =>
            {
                for (int i = start; i < end; ++i)
                {
                    SolarSystem system = UniverseScreen.SolarSystemList[i];
                    system.ShipList.Clear();

                    GameplayObject[] shipsInSystem = Spatial.FindNearby(GameObjectType.Ship, system.Position, system.Radius, 10_000);
                    for (int j = 0; j < shipsInSystem.Length; ++j)
                    {
                        var ship = (Ship)shipsInSystem[j];
                        system.ShipList.Add(ship);
                        ship.SetSystem(system);
                        system.SetExploredBy(ship.loyalty);
                    }

                    system.Update(timeStep, Universe);
                }
            }, Universe.MaxTaskCores);

            SysPerf.Stop();
        }

        void UpdateAllShips(FixedSimTime timeStep)
        {
            ShipsPerf.Start();

            bool isSystemView = (Universe.viewState <= UniverseScreen.UnivScreenState.SystemView);
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

            ShipsPerf.Stop();
        }
    }
}
