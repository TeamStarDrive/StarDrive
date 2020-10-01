using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        /// <summary>
        /// All objects: ships, projectiles, beams
        /// </summary>
        readonly Array<GameplayObject> MasterObjectList = new Array<GameplayObject>();
        
        /// <summary>
        /// All ships
        /// </summary>
        readonly Array<Ship> MasterShipList = new Array<Ship>();

        /// <summary>
        /// All projectiles: projectiles, beams
        /// </summary>
        readonly Array<Projectile> MasterProjectileList = new Array<Projectile>();
        
        public Array<Ship> GetMasterShipList() => MasterShipList;
        public Array<GameplayObject> GetMasterObjectList() => MasterObjectList;

        public Ship FindShipByGuid(in Guid guid)
        {
            lock (MasterShipList)
            {
                for (int i = 0; i < MasterShipList.Count; ++i)
                    if (MasterShipList[i].guid == guid)
                        return MasterShipList[i];
                return null;
            }
        }

        public Array<Projectile> GetProjectilesForShip(Ship ship)
        {
            var projectiles = new Array<Projectile>();
            lock (MasterProjectileList)
            {
                for (int i = 0; i < MasterProjectileList.Count; ++i)
                {
                    Projectile p = MasterProjectileList[i];
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
                lock (MasterShipList)
                {
                    MasterShipList.Add((Ship)go);
                }
                lock (MasterObjectList)
                {
                    MasterObjectList.Add(go);
                }
            }
            else if (go.Type == GameObjectType.Beam || go.Type == GameObjectType.Proj)
            {
                lock (MasterProjectileList)
                {
                    MasterProjectileList.Add((Projectile)go);
                }
                lock (MasterObjectList)
                {
                    MasterObjectList.Add(go);
                }
            }
        }

        public void ClearAllObjects()
        {
            lock (MasterShipList)
            {
                for (int i = 0; i < MasterShipList.Count; ++i)
                    MasterShipList[i]?.RemoveFromUniverseUnsafe();
                MasterShipList.Clear();
            }
            lock (MasterProjectileList)
            {
                MasterProjectileList.Clear();
            }
            lock (MasterObjectList)
            {
                MasterObjectList.Clear();
            }
        }
        
        public void UpdateMasterObjectsLists()
        {
            lock (MasterShipList)
            {
                for (int i = 0; i < MasterShipList.Count; ++i)
                {
                    Ship ship = MasterShipList[i];
                    if (!ship.Active)
                    {
                        if (SelectedShip == ship)
                            SelectedShip = null;
                        SelectedShipList.RemoveRef(ship);
                        ship.RemoveFromUniverseUnsafe();
                    }
                }
                MasterShipList.RemoveInActiveObjects();
            }

            lock (MasterProjectileList)
            {

                for (int i = 0; i < MasterProjectileList.Count; ++i)
                {
                    Projectile proj = MasterProjectileList[i];
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
                MasterProjectileList.RemoveInActiveObjects();
            }
            
            lock (MasterObjectList)
            {
                MasterObjectList.RemoveInActiveObjects();
            }
        }

        void UpdateMasterObjects(FixedSimTime timeStep)
        {
            for (int i = 0; i < MasterObjectList.Count; ++i)
            {
                GameplayObject go = MasterObjectList[i];
                go.Update(timeStep);
            }
        }

        void AssignSystemsToShips()
        {
            lock (MasterShipList)
            {
                for (int i = 0; i < MasterShipList.Count; ++i)
                {
                    MasterShipList[i].SetSystem(null);
                }
            }

            Parallel.For(SolarSystemList.Count, (start, end) =>
            {
                for (int i = start; i < end; ++i)
                {
                    SolarSystem system = SolarSystemList[i];
                    system.ShipList.Clear();

                    GameplayObject[] shipsInSystem = Spatial.FindNearby(GameObjectType.Ship, system.Position, system.Radius, 10_000);
                    for (int j = 0; j < shipsInSystem.Length; ++j)
                    {
                        var ship = (Ship)shipsInSystem[j];
                        system.ShipList.Add(ship);
                        ship.SetSystem(system);
                        system.SetExploredBy(ship.loyalty);
                    }
                }
            }, MaxTaskCores);
        }

    }
}