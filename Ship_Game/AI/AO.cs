using System;
using System.Collections;
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
        [XmlIgnore][JsonIgnore] Fleet CoreFleet                               ;
        [XmlIgnore][JsonIgnore] Array<Ship> ShipsWaitingForCoreFleet = new Array<Ship>();
        [XmlIgnore][JsonIgnore] Planet[] PlanetsInAo                          = NoPlanets;
        [XmlIgnore][JsonIgnore] Planet[] OurPlanetsInAo                       = NoPlanets;
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
        [Serialize(9)] public Guid AOGuid = Guid.NewGuid();
        [Serialize(10)] public Vector2 Center;
        public Fleet GetCoreFleet()                        => CoreFleet;
        public Planet GetPlanet() => CoreWorld;

        public Planet[] GetPlanets()                       => OurPlanetsInAo;
        public IReadOnlyList<Ship> GetOffensiveForcePool() => OffensiveForcePool;
        [XmlIgnore][JsonIgnore] public bool AOFull { get; private set; }           = true;

        [XmlIgnore][JsonIgnore]
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
        }

        public int GetNumOffensiveForcePoolShips() => OffensiveForcePool.Count;
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
            CoreFleet                           = new Fleet();
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

        public void AddPlanet(Planet p)
        {
            PlanetsInAo.Add(p, out var ps);
            PlanetsInAo = ps;
        }

        public void AddPlanets(IList<Planet> ps)
        {
            var planets = PlanetsInAo.Union(ps);
            PlanetsInAo = planets.ToArray();
        }

        private bool IsCoreFleetFull()
        {
            float str = 0;
            foreach (Ship ship in ShipsWaitingForCoreFleet)
                str += ship.GetStrength();

            return ThreatLevel + str.LowerBound(100) < CoreFleet.GetStrength();
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

            OffensiveForcePool.AddUniqueRef(ship);
            return true;
        }

        public void AddAnyShips(Array<Ship> ships)
        {
            OffensiveForcePool.AddUniqueRef(ships);
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

        public void InitFromSave(Empire owner)
        {
            SetPlanet(Planet.GetPlanetFromGuid(CoreWorldGuid));
            PlanetsInAo              = Empire.Universe.PlanetsDict.Values.Filter(p => p.Center.InRadius(this));
            OffensiveForcePool       = Ship.GetShipsFromGuids(OffensiveForceGuids);
            ShipsWaitingForCoreFleet = Ship.GetShipsFromGuids(ShipsWaitingGuids);

            var fleet = owner.GetFleetsDict().FilterValues(f => f.Guid == FleetGuid).FirstOrDefault();

            if (fleet != null)
            {
                SetFleet(fleet);
            }
            else
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
            if (p == null && CoreWorld == null) return;
            CoreWorld = p;
            Center    = p?.Center ?? Vector2.Zero;
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

                ShipsWaitingForCoreFleet.RemoveAt(i);
                OffensiveForcePool.AddUnique(ship);


                //if (ship.fleet != null)
                //{
                //    ShipsWaitingForCoreFleet.RemoveAtSwapLast(i);
                //    Log.Error("ship {0} in fleet {1}", ship.Name, ship.fleet.Name);
                //}

                //if (OffensiveForcePool.ContainsRef(ship))
                //{
                //    Log.Error("warning. Ship in offensive and waiting {0} ", CoreWorld.Name);
                //    ShipsWaitingForCoreFleet.RemoveAtSwapLast(i); // remove it
                //}
                
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

            for (int i = 0; i < CoreFleet.Ships.Count; i++)
            {
                var ship = CoreFleet.Ships[i];
                CoreFleet.Ships.RemoveAt(i);
                OffensiveForcePool.AddUniqueRef(ship);
            }

            //if (CoreFleet.FleetTask == null && ShipsWaitingForCoreFleet.Count > 0)
            //{
            //    while (ShipsWaitingForCoreFleet.Count > 0)
            //    {
            //        Ship waiting = ShipsWaitingForCoreFleet.PopLast();
            //        if (!waiting.Active)
            //            continue;

            //        if (IsCoreFleetFull())
            //        {
            //            OffensiveForcePool.AddUniqueRef(waiting);
            //        }
            //        else
            //        {
            //            if (waiting.fleet != null)
            //            {
            //                if (waiting.fleet == CoreFleet)
            //                    Log.Warning("Ship already in CoreFleet (duplication bug)");
            //                else
            //                    Log.Error("Ship already in another fleet");
            //                continue;
            //            }
            //            CoreFleetAddShip(waiting);
            //        }
            //    }
            //    if (CoreFleet.Ships.Count > 0)
            //    {
            //        CoreFleet.FinalPosition = CoreWorld.Center;
            //        CoreFleet.AutoArrange();
            //        CoreFleet.MoveToNow(Center, Vectors.Up);
            //    }
            //    TurnsToRelax +=  1;
            //}
            //else
            //{
            //    foreach(Ship ship in ShipsWaitingForCoreFleet)
            //    {
            //        OffensiveForcePool.AddUniqueRef(ship);
            //    }
            //    ShipsWaitingForCoreFleet.Clear();
            //}

            //if (ThreatLevel > 0 && ThreatLevel * (1 - (TurnsToRelax / 10)) < CoreFleet.GetStrength())
            //{
            //    if (CoreFleet.FleetTask == null && !CoreWorld.Owner.isPlayer)
            //    {
            //        var clearArea = new MilitaryTask(this);
            //        CoreFleet.FleetTask = clearArea;
            //        CoreFleet.TaskStep  = 1;
            //        if (CoreFleet.Owner == null)
            //        {
            //            CoreFleet.Owner = CoreWorld.Owner;
            //        }
            //        CoreFleet.Owner.GetEmpireAI().AddPendingTask(clearArea);
            //    }
            //    TurnsToRelax = 1;
            //}
            AOFull = ThreatLevel < CoreFleet.GetStrength() && OffensiveForcePool.Count > 0;
        }

        public void ClearOut()
        {
            if (CoreFleet != null)
            {
                if (CoreFleet?.Owner != null)
                {
                    foreach (var kv in CoreFleet.Owner.GetFleetsDict())
                    {
                        if (kv.Value != CoreFleet)
                            continue;
                        CoreFleet.Owner.GetFleetsDict().Remove(kv.Key);
                        break;
                    }
                    CoreFleet.Reset();
                }
                ReassignShips(CoreFleet.Ships);
            }
            if (OffensiveForcePool?.NotEmpty == true)
                ReassignShips(OffensiveForcePool);
            if (ShipsWaitingForCoreFleet?.NotEmpty == true)
                ReassignShips(ShipsWaitingForCoreFleet);

            OffensiveForcePool?.Clear();
            ShipsWaitingForCoreFleet?.Clear();
            CoreFleet?.Reset();
            CoreWorld      = null;
            PlanetsInAo    = null;
            OurPlanetsInAo = null;
        }

        void ReassignShips(Array<Ship> ships)
        {
            foreach(var ship in ships)
            {
                ship.loyalty.Pool.AddShipNextFame(ship);
            }
        }

        public float GetWarValueOfSystemsInAOTo(Empire empire)
        {
            var systems = Empire.Universe.SolarSystemDict.Values.Filter(s=> s.Position.InRadius(this));
            return systems.Sum(s => s.WarValueTo(empire));
        }

        public float StrengthOpposing(Empire empire)
        {
            return empire.GetEmpireAI().ThreatMatrix.GetEnemyPinsInAO(this, empire).Sum(p=> p.Strength);
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
        }
       
    }
}