using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{	
    // ReSharper disable once InconsistentNaming
    public sealed class AO : IDisposable
    {
        public static readonly Planet[] NoPlanets = new Planet[0];

        [XmlIgnore][JsonIgnore] private Planet CoreWorld;
        [XmlIgnore][JsonIgnore] private Array<Ship> OffensiveForcePool = new Array<Ship>();
        [XmlIgnore][JsonIgnore] private Fleet CoreFleet = new Fleet();
        [XmlIgnore][JsonIgnore] private readonly Array<Ship> ShipsWaitingForCoreFleet = new Array<Ship>();
        [XmlIgnore][JsonIgnore] private Planet[] PlanetsInAo    = NoPlanets;
        [XmlIgnore][JsonIgnore] private Planet[] OurPlanetsInAo = NoPlanets;
        [XmlIgnore][JsonIgnore] public Vector2 Position => CoreWorld.Position;
        [XmlIgnore][JsonIgnore] private Empire Owner => CoreWorld.Owner;

        [Serialize(0)] public int ThreatLevel;
        [Serialize(1)] public Guid CoreWorldGuid;
        [Serialize(2)] public Array<Guid> OffensiveForceGuids = new Array<Guid>();
        [Serialize(3)] public Array<Guid> ShipsWaitingGuids   = new Array<Guid>();
        [Serialize(4)] public Guid FleetGuid;
        [Serialize(5)] public int WhichFleet = -1;
        //[Serialize(6)] private bool Flip; // @todo Change savegame version before reassigning Serialize indices
        [Serialize(7)] public float Radius;
        [Serialize(8)] public int TurnsToRelax;
        public Fleet GetCoreFleet() => CoreFleet;
        public Planet GetPlanet() => CoreWorld;
        public Planet[] GetPlanets() => OurPlanetsInAo;
        public IReadOnlyList<Ship> GetOffensiveForcePool() => OffensiveForcePool;
        public IReadOnlyList<Ship> GetWaitingShips() => ShipsWaitingForCoreFleet;
        public AO()
        {
        }

        private bool CoreFleetFull()
        {
            return ThreatLevel < CoreFleet.GetStrength();            
        }

        private void CoreFleetAddShip(Ship ship)
        {
            ship.GetAI().OrderQueue.Clear();
            ship.GetAI().HasPriorityOrder = false;
            CoreFleet.AddShip(ship);
        }
        public AO(Planet p, float radius)
        {
            Radius                = radius;
            CoreWorld             = p;
            CoreWorldGuid         = p.guid;
            WhichFleet            = p.Owner.GetUnusedKeyForFleet();
            p.Owner.GetFleetsDict().Add(WhichFleet, CoreFleet);
            CoreFleet.Name        = "Core Fleet";
            CoreFleet.Position    = p.Position;
            CoreFleet.Owner       = p.Owner;
            CoreFleet.IsCoreFleet = true;
            var tempPlanet = new Array<Planet>();
            foreach (Planet planet in Empire.Universe.PlanetsDict.Values)
                if (planet.Position.InRadius(CoreWorld.Position, radius))
                    tempPlanet.Add(planet);
            PlanetsInAo = tempPlanet.ToArray();
        }

        public void AddShip(Ship ship)
        {
            if (ship.BaseStrength <1)
                return ;
            if (OffensiveForcePool.Contains(ship))
            {
                Log.Warning("offensive forcepool already contains this ship. not adding");
                return;
            }
            if (ship.fleet !=null)
                Log.Error("readding corefleet ship");
            //@check for arbitraty comarison threatlevel
            if (CoreFleetFull() || ship.BombBays.Count > 0 || ship.hasAssaultTransporter || ship.HasTroopBay)
            {
               
                OffensiveForcePool.Add(ship);
                return ;
            }
            ShipsWaitingForCoreFleet.Add(ship);
            ////@corefleet speed less than 4k arbitrary logic means a likely problem 
            //if (CoreFleet.FleetTask == null && ship.fleet == null && CoreFleet.Speed < 4000 && !CoreFleetFull())
            //{
            //    CoreFleetAddShip(ship);
            //    float strength = CoreFleet.GetStrength();
            //    foreach (Ship waiting in ShipsWaitingForCoreFleet)
            //    {
            //        if (waiting.fleet != null)
            //            continue;
            //        strength += waiting.GetStrength();
            //        if (ThreatLevel < strength) break;
            //        CoreFleet.AddShip(waiting);
            //        waiting.GetAI().OrderQueue.Clear();
            //        waiting.GetAI().HasPriorityOrder = false;
            //    }
            //    CoreFleet.Position = CoreWorld.Position;
            //    CoreFleet.AutoArrange();
            //    CoreFleet.MoveToNow(Position, 0f, new Vector2(0f, -1f));
            //    ShipsWaitingForCoreFleet.Clear();

            //}
            //else if (ship.fleet == null)
            //{
            //    ShipsWaitingForCoreFleet.Add(ship);
            //    OffensiveForcePool.Add(ship);
            //    return true;
            //}
            //return false;
        }
        public bool RemoveShip(Ship ship)
        {
            if (ship.fleet?.IsCoreFleet ?? false)
                CoreFleet.RemoveShip(ship);
            ShipsWaitingForCoreFleet.Remove(ship);            
            return OffensiveForcePool.Remove(ship);
        }




        public void InitFromSave(UniverseData data, Empire owner)
        {
            foreach (SolarSystem sys in data.SolarSystemsList)
            {
                foreach (Planet p in sys.PlanetList)
                    if (p.guid == CoreWorldGuid)
                        SetPlanet(p);
            }
            Array<Planet> tempPlanet = new Array<Planet>();
            foreach (SolarSystem sys in data.SolarSystemsList)
            {
                foreach (Planet p in sys.PlanetList)
                    if (p.Position.InRadius(Position, Radius))
                        tempPlanet.Add(p);
            }
            PlanetsInAo = tempPlanet.ToArray();            
            foreach (Guid guid in OffensiveForceGuids)
            {
                foreach (Ship ship in data.MasterShipList)
                {
                    if (ship.guid != guid) continue;
                    OffensiveForcePool.Add(ship);
                    ship.AddedOnLoad = true;
                }
            }
            foreach (Guid guid in ShipsWaitingGuids)
            {
                foreach (Ship ship in data.MasterShipList)
                {
                    if (ship.guid != guid) continue;
                    ShipsWaitingForCoreFleet.Add(ship);
                    ship.AddedOnLoad = true;
                }
            }

            bool didSetFleet = false;
            foreach (var kv in owner.GetFleetsDict())
            {
                if (kv.Value.Guid == FleetGuid)
                {
                    didSetFleet = true;
                    SetFleet(kv.Value);
                }
            }

            if (!didSetFleet)
                Log.Warning("Savegame FleetGuid {0} ({1} fleetIdx:{2} [{3}]) not found in owner FleetsDict!!", 
                    FleetGuid, owner.Name, WhichFleet, WhichFleet != -1 ? owner.GetFleetsDict()[WhichFleet].Name : "");
        }

        public void PrepareForSave()
        {
            OffensiveForceGuids.Clear();
            ShipsWaitingGuids.Clear();
            foreach (Ship ship in OffensiveForcePool)
            {
                OffensiveForceGuids.Add(ship.guid);
            }
            foreach (Ship ship in ShipsWaitingForCoreFleet)
            {
                ShipsWaitingGuids.Add(ship.guid);
            }
            FleetGuid = CoreFleet.Guid;
        }

        public void SetFleet(Fleet f)
        {
            CoreFleet = f;
        }

        public void SetPlanet(Planet p)
        {
            CoreWorld = p;
        }

        public void Update()
        {
            Array<Planet> tempP = new Array<Planet>();
            foreach (Planet p in PlanetsInAo)
            {
                if (p.Owner != Owner) continue;
                tempP.Add(p);
            }
            OurPlanetsInAo = tempP.ToArray();
            for (int index = OffensiveForcePool.Count - 1; index >= 0; index--)
            {
                Ship ship = OffensiveForcePool[index];
                if (ship.Active && ship.fleet == null && ship.shipData.Role != ShipData.RoleName.troop && 
                    ship.GetStrength() > 0)
                    continue;
                if(ship.Active)
                Log.Error("invalid ship in offensive force pool");
                OffensiveForcePool.Remove(ship);
            }
                        
            if (CoreFleet.FleetTask ==null && ShipsWaitingForCoreFleet.Count >0)
                //&& (CoreFleet.Ships.Count ==0 || CoreFleet.FleetTask == null))
            {
                bool full = CoreFleetFull();
                for (int index = ShipsWaitingForCoreFleet.Count - 1; index >= 0; index--)
                {
                    Ship waiting = ShipsWaitingForCoreFleet[index];
                    if (!full)
                    {
                        if (waiting.fleet != null)
                            Log.Error("Ship has fleet already");
                        ShipsWaitingForCoreFleet.Remove(waiting);
                        CoreFleetAddShip(waiting);
                        full = CoreFleetFull();
                    }
                    else    
                        AddShip(waiting);
                    
                }
                if (full)
                    ShipsWaitingForCoreFleet.Clear();
                
                CoreFleet.Position = CoreWorld.Position;
                CoreFleet.AutoArrange();
                CoreFleet.MoveToNow(Position, 0f, new Vector2(0f, -1f));
                
            
                TurnsToRelax +=  1;
            }
            if (ThreatLevel * ( 1-(TurnsToRelax / 10)) < CoreFleet.GetStrength())
            {
                if (CoreFleet.FleetTask == null && !CoreWorld.Owner.isPlayer)
                {
                    var clearArea = new MilitaryTask(this);
                    CoreFleet.FleetTask = clearArea;
                    CoreFleet.TaskStep = 1;
                    if (CoreFleet.Owner == null)
                    {
                        CoreFleet.Owner = CoreWorld.Owner;
                        CoreFleet.Owner.GetGSAI().TaskList.Add(clearArea);
                    }
                    else CoreFleet.Owner.GetGSAI().TaskList.Add(clearArea);
                }
                TurnsToRelax = 1;
            }
        }        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AO() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            OffensiveForcePool = null;
            if (CoreFleet?.Owner != null)
            {
                foreach (var kv in CoreFleet.Owner.GetFleetsDict())
                {
                    if (kv.Value != CoreFleet)
                        continue;
                    CoreFleet.Owner.GetFleetsDict().Remove(kv.Key);
                    break;
                }
            }
         

            CoreFleet?.Dispose(ref CoreFleet);
        }
       
    }
}