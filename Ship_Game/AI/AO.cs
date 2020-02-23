using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.AI.Tasks;
using Ship_Game.Debug;
using Ship_Game.Fleets;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    // ReSharper disable once InconsistentNaming
    public sealed class AO : IDisposable
    {
        public static readonly Planet[] NoPlanets = new Planet[0];

        [XmlIgnore][JsonIgnore] public Planet CoreWorld { get; private set; }
        [XmlIgnore][JsonIgnore] Array<Ship> OffensiveForcePool                = new Array<Ship>();
        [XmlIgnore][JsonIgnore] Fleet CoreFleet                               = new Fleet();
        [XmlIgnore][JsonIgnore] readonly Array<Ship> ShipsWaitingForCoreFleet = new Array<Ship>();
        [XmlIgnore][JsonIgnore] Planet[] PlanetsInAo                          = NoPlanets;
        [XmlIgnore][JsonIgnore] Planet[] OurPlanetsInAo                       = NoPlanets;
        [XmlIgnore] [JsonIgnore] public Vector2 Center;
        [XmlIgnore][JsonIgnore] Empire Owner                                  => CoreWorld.Owner;

        [Serialize(0)] public int ThreatLevel;
        [Serialize(1)] public Guid CoreWorldGuid;
        [Serialize(2)] public Array<Guid> OffensiveForceGuids = new Array<Guid>();
        [Serialize(3)] public Array<Guid> ShipsWaitingGuids   = new Array<Guid>();
        [Serialize(4)] public Guid FleetGuid;
        [Serialize(5)] public int WhichFleet = -1;
        //[Serialize(6)] private bool Flip; // @todo Change savegame version before reassigning Serialize indices
        [Serialize(7)] public float Radius;
        [Serialize(8)] public int TurnsToRelax;
        public Fleet GetCoreFleet()                        => CoreFleet;
        public Planet GetPlanet()                          => CoreWorld;
        public Planet[] GetPlanets()                       => OurPlanetsInAo;
        public IReadOnlyList<Ship> GetOffensiveForcePool() => OffensiveForcePool;
        [XmlIgnore][JsonIgnore] public bool AOFull { get; private set; }           = true;
        public float OffensiveForcePoolStrength
        {
            get
            {
                float strength = 0f;
                for (int i = 0; i < OffensiveForcePool.Count -1; ++i)
                    strength += OffensiveForcePool[i].GetStrength();
                return strength;
            }
        }

        public void SetThreatLevel()
        {
            ThreatLevel = (int)Owner.GetEmpireAI().ThreatMatrix.
                PingRadarStrengthLargestCluster(Center, Radius, Owner, 50000);
            ThreatLevel = ThreatLevel;
        }
        public int NumOffensiveForcePoolShips => OffensiveForcePool.Count;
        public bool OffensiveForcePoolContains(Ship s) => OffensiveForcePool.ContainsRef(s);
        public bool WaitingShipsContains(Ship s)       => ShipsWaitingForCoreFleet.ContainsRef(s);

        public AO()
        {
        }

        public AO(Vector2 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public AO(Empire empire)
        {
            Center = empire.GetWeightedCenter();
            Radius = Empire.Universe.UniverseSize / 4;
        }

        public AO(Planet p, float radius)
        {
            Radius                              = radius;
            CoreWorld                           = p;
            CoreWorldGuid                       = p.guid;
            WhichFleet                          = p.Owner.CreateFleetKey();
            p.Owner.GetFleetsDict()[WhichFleet] = CoreFleet;
            CoreFleet.Name                      = "Core Fleet";
            CoreFleet.FinalPosition             = p.Center;
            CoreFleet.Owner                     = p.Owner;
            CoreFleet.IsCoreFleet               = true;
            Center                              = CoreWorld.Center;
            var tempPlanet                      = new Array<Planet>();
            foreach (Planet planet in Empire.Universe.PlanetsDict.Values)
                if (planet.Center.InRadius(CoreWorld.Center, radius))
                    tempPlanet.Add(planet);
            PlanetsInAo                         = tempPlanet.ToArray();
        }

        private bool IsCoreFleetFull()
        {
            float str = 0;
            foreach (Ship ship in ShipsWaitingForCoreFleet)
                str += ship.GetStrength();

            return ThreatLevel + str.ClampMin(100) < CoreFleet.GetStrength();
        }

        private void CoreFleetAddShip(Ship ship)
        {
            ship.AI.ClearOrders();
            CoreFleet.AddShip(ship);
        }

        public bool AddShip(Ship ship)
        {
            if (ship.BaseStrength < 1f 
                || ship.DesignRole == ShipData.RoleName.bomber 
                || ship.DesignRole == ShipData.RoleName.troopShip 
                || ship.DesignRole == ShipData.RoleName.support)
                return false;
            

            if (IsCoreFleetFull() || GetPoolStrength() < Owner.CurrentMilitaryStrength * .05f) 
            {
                OffensiveForcePool.AddUniqueRef(ship);
            }
            else
            {
                ShipsWaitingForCoreFleet.AddUniqueRef(ship);
            }
            return true;
        }


        public bool RemoveShip(Ship ship)
        {
            if (ship.fleet?.IsCoreFleet ?? false)
            {
                CoreFleet.RemoveShip(ship);
                Log.Error("Ship was in core fleet");
            }
            ShipsWaitingForCoreFleet.RemoveRef(ship);            
            return OffensiveForcePool.RemoveRef(ship);
        }

        public void InitFromSave(UniverseData data, Empire owner)
        {
            foreach (SolarSystem sys in data.SolarSystemsList)
            {
                foreach (Planet p in sys.PlanetList)
                    if (p.guid == CoreWorldGuid)
                        SetPlanet(p);
            }

            var tempPlanet = new Array<Planet>();
            foreach (SolarSystem sys in data.SolarSystemsList)
            {
                foreach (Planet p in sys.PlanetList)
                    if (p.Center.InRadius(Center, Radius))
                        tempPlanet.Add(p);
            }
            PlanetsInAo = tempPlanet.ToArray();

            OffensiveForceGuids.RemoveDuplicates();
            foreach (Guid guid in OffensiveForceGuids)
            {
                foreach (Ship ship in data.MasterShipList)
                {
                    if (ship.guid != guid) continue;
                    OffensiveForcePool.Add(ship);
                    ship.AddedOnLoad = true;
                }
            }

            ShipsWaitingGuids.RemoveDuplicates();
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
            {
                string fleetName = WhichFleet != -1 ? owner.GetFleetsDict()[WhichFleet].Name : "";
                Log.Warning($"Savegame FleetGuid {FleetGuid} ({owner.Name} fleetIdx:{WhichFleet} [{fleetName}]) not found in owner FleetsDict!!");
            }
        }

        public void PrepareForSave()
        {
            OffensiveForceGuids.Clear();
            ShipsWaitingGuids.Clear();
            foreach (Ship ship in OffensiveForcePool)
                OffensiveForceGuids.Add(ship.guid);
            foreach (Ship ship in ShipsWaitingForCoreFleet)
                ShipsWaitingGuids.Add(ship.guid);
            FleetGuid = CoreFleet.Guid;
        }

        public float GetPoolStrength()
        {
            float str = 0;
            for (int i = 0; i < OffensiveForcePool.Count; i++)
            {
                Ship ship = OffensiveForcePool[i];
                if (ship.AI.State == AIState.Scrap
                    || ship.AI.State == AIState.Refit
                    || ship.AI.State == AIState.Resupply
                    || !ship.ShipIsGoodForGoals()
                ) continue;
                str += ship.GetStrength();
            }

            return str;
        }

        public void SetFleet(Fleet f)
        {
            CoreFleet = f;
        }

        public void SetPlanet(Planet p)
        {
            CoreWorld = p;
            Center    = p.Center;
        }

        public void Update()
        {
            Empire.Universe?.DebugWin?.DrawCircle(DebugModes.AO, Center, Radius, Owner.EmpireColor, 1);
            if (OurPlanetsInAo.Length == 0 && Owner != null && PlanetsInAo.Length > 0)
            {
                OurPlanetsInAo = PlanetsInAo.Filter(p => p.Owner == Owner);
            }

            for (int i = ShipsWaitingForCoreFleet.Count - 1; i >= 0; --i)
            {
                Ship ship = ShipsWaitingForCoreFleet[i];
                if (ship.fleet != null)
                {
                    ShipsWaitingForCoreFleet.RemoveAtSwapLast(i);
                    Log.Error("ship {0} in fleet {1}", ship.Name, ship.fleet.Name);
                }

                if (OffensiveForcePool.ContainsRef(ship))
                {
                    Log.Error("warning. Ship in offensive and waiting {0} ", CoreWorld.Name);
                    ShipsWaitingForCoreFleet.RemoveAtSwapLast(i); // remove it
                }
                
            }
            for (int i = OffensiveForcePool.Count-1; i >= 0; --i)
            {
                Ship ship = OffensiveForcePool[i];
                if (!ship.Active || ship.fleet != null ||
                    ship.shipData.Role == ShipData.RoleName.troop ||
                    ship.GetStrength() <= 0)
                {
                    OffensiveForcePool.RemoveAtSwapLast(i);
                }
            }

            if (CoreFleet.FleetTask == null && ShipsWaitingForCoreFleet.Count > 0)
            {
                while (ShipsWaitingForCoreFleet.Count > 0)
                {
                    Ship waiting = ShipsWaitingForCoreFleet.PopLast();
                    if (!waiting.Active)
                        continue;

                    if (IsCoreFleetFull())
                    {
                        OffensiveForcePool.AddUniqueRef(waiting);
                    }
                    else
                    {
                        if (waiting.fleet != null)
                        {
                            if (waiting.fleet == CoreFleet)
                                Log.Warning("Ship already in CoreFleet (duplication bug)");
                            else
                                Log.Error("Ship already in another fleet");
                            continue;
                        }
                        CoreFleetAddShip(waiting);
                    }
                }
                if (CoreFleet.Ships.Count > 0)
                {
                    CoreFleet.FinalPosition = CoreWorld.Center;
                    CoreFleet.AutoArrange();
                    CoreFleet.MoveToNow(Center, Vectors.Up);
                }
                TurnsToRelax +=  1;
            }
            else
            {
                foreach(Ship ship in ShipsWaitingForCoreFleet)
                {
                    OffensiveForcePool.AddUniqueRef(ship);
                }
                ShipsWaitingForCoreFleet.Clear();
            }

            if (ThreatLevel > 0 && ThreatLevel * (1 - (TurnsToRelax / 10)) < CoreFleet.GetStrength())
            {
                if (CoreFleet.FleetTask == null && !CoreWorld.Owner.isPlayer)
                {
                    var clearArea = new MilitaryTask(this);
                    CoreFleet.FleetTask = clearArea;
                    CoreFleet.TaskStep  = 1;
                    if (CoreFleet.Owner == null)
                    {
                        CoreFleet.Owner = CoreWorld.Owner;
                    }
                    CoreFleet.Owner.GetEmpireAI().AddPendingTask(clearArea);
                }
                TurnsToRelax = 1;
            }
            AOFull = ThreatLevel < CoreFleet.GetStrength() && OffensiveForcePool.Count > 0;
        }

        public void ClearOut()
        {
            if (CoreFleet != null)
                Owner?.AddShipNextFrame(CoreFleet.Ships);
            if (OffensiveForcePool?.NotEmpty == true)
                Owner.AddShipNextFrame(OffensiveForcePool);
            if (ShipsWaitingForCoreFleet?.NotEmpty == true)
                Owner.AddShipNextFrame(ShipsWaitingForCoreFleet);

            OffensiveForcePool?.Clear();
            ShipsWaitingForCoreFleet?.Clear();
            CoreFleet?.Reset();
            CoreWorld      = null;
            PlanetsInAo    = null;
            OurPlanetsInAo = null;
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
        }
       
    }
}