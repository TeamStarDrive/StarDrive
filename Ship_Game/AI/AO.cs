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
    public sealed class AOPlanetData
    {
        public Guid PlanetGuid;
        readonly Planet OwnerPlanet;
        public int BuildingCount;
        public float WarValue;
        float LastUpdate;
        readonly Empire DataOwner;

        public AOPlanetData(Planet p, Empire e)
        {
            OwnerPlanet = p;
            PlanetGuid  = p.guid;
            DataOwner   = e;
        }

        public void Update(float stardate)
        {
            if (stardate.LessOrEqual(stardate)) return;
            if (BuildingCount != OwnerPlanet.BuildingList.Count)
            {
                BuildingCount = OwnerPlanet.BuildingList.Count;
                WarValue = OwnerPlanet.ColonyWarValueTo(DataOwner);
            }
        }
    }

    public sealed class AO : IDisposable
    {
        public static readonly Planet[] NoPlanets     = new Planet[0];
        public static readonly SolarSystem[] NoSystem = new SolarSystem[0];

        [XmlIgnore][JsonIgnore] public Planet CoreWorld { get; private set; }
        [XmlIgnore][JsonIgnore] Array<Ship> OffensiveForcePool                     = new Array<Ship>();
        [XmlIgnore][JsonIgnore] Fleet CoreFleet;
        [XmlIgnore][JsonIgnore] Array<Ship> ShipsWaitingForCoreFleet               = new Array<Ship>();
        [XmlIgnore][JsonIgnore] Planet[] PlanetsInAo                               = NoPlanets;
        AOPlanetData[] PlanetData;
        [XmlIgnore][JsonIgnore] Planet[] OurPlanetsInAo                            = NoPlanets;
        [XmlIgnore][JsonIgnore] SolarSystem[] SystemsInAo                          = NoSystem;
        [XmlIgnore][JsonIgnore] Empire Owner;
        [XmlIgnore][JsonIgnore] int ThreatTimer;

        [Serialize(0)] public int ThreatLevel;
        [Serialize(1)] public Guid CoreWorldGuid;
        [Serialize(2)] public Array<Guid> OffensiveForceGuids = new Array<Guid>();
        [Serialize(3)] public Array<Guid> ShipsWaitingGuids   = new Array<Guid>();
        [Serialize(4)] public Guid FleetGuid;
        [Serialize(5)] public int WhichFleet                  = -1;
        //[Serialize(6)] private bool Flip; // @todo Change savegame version before reassigning Serialize indices
        [Serialize(7)] public float Radius;
        [Serialize(8)] public int TurnsToRelax;
        [Serialize(9)] public Guid AOGuid                     = Guid.NewGuid();
        [Serialize(10)] public Vector2 Center;
        [Serialize(11)] public float WarValueOfPlanets;
        
        public Fleet GetCoreFleet()                           => CoreFleet;
        public Planet GetPlanet()                             => CoreWorld;

        public Planet[] GetOurPlanets()                       => OurPlanetsInAo;
        public Planet[] GetAllPlanets()                       => PlanetsInAo;

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

        public Array<SolarSystem> GetAoSystems() => new Array<SolarSystem>(SystemsInAo);

        public void UpdateThreatLevel()
        {
            if (--ThreatTimer > 0) return;
            ThreatLevel = (int)Owner.GetEmpireAI().ThreatMatrix.PingRadarStrengthLargestCluster(Center, Radius, Owner, 50000);
            // arbitrary performance consideration.
            ThreatTimer = 5;
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
            Center = empire.WeightedCenter;
            Radius = Empire.Universe.UniverseSize / 4;
        }

        public AO(Planet p, float radius)
        {
            Radius                              = radius;
            CoreWorld                           = p;
            CoreWorldGuid                       = p.guid;
            Owner                               = p.Owner;
            WhichFleet                          = p.Owner.CreateFleetKey();
            CoreFleet                           = new Fleet();
            p.Owner.GetFleetsDict()[WhichFleet] = CoreFleet;
            CoreFleet.Name                      = "Core Fleet";
            CoreFleet.FinalPosition             = p.Center;
            CoreFleet.Owner                     = p.Owner;
            CoreFleet.IsCoreFleet               = true;
            Center                              = CoreWorld.Center;
            SetupPlanetsInAO();
        }

        public AO(Empire empire, Vector2 center, float radius)
        {
            Center = center;
            Radius = radius;
            Owner  = empire;
            SetupPlanetsInAO();
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
                Log.Warning(ConsoleColor.Red, "Ship was in core fleet");
            }
            ShipsWaitingForCoreFleet.RemoveRef(ship);            
            return OffensiveForcePool.RemoveRef(ship);
        }

        public void InitFromSave(Empire owner)
        {
            SetPlanet(Planet.GetPlanetFromGuid(CoreWorldGuid));
            Owner = owner;
            SetupPlanetsInAO();
            OffensiveForcePool       = Ship.GetShipsFromGuids(OffensiveForceGuids);
            ShipsWaitingForCoreFleet = Ship.GetShipsFromGuids(ShipsWaitingGuids);
            
            var fleet = owner.GetFleetsDict().FilterValues(f => f.Guid == FleetGuid).FirstOrDefault();

            if (fleet != null)
            {
                SetFleet(fleet);
            }
            else if (FleetGuid != Guid.Empty)
            {
                string fleetName = WhichFleet != -1 ? owner.GetFleetsDict()[WhichFleet].Name : "";
                Log.Warning($"Savegame FleetGuid {FleetGuid} ({owner.Name} fleetIdx:{WhichFleet} [{fleetName}]) not found in owner FleetsDict!!");
            }
        }

        public void SetupPlanetsInAO()
        {
            WarValueOfPlanets = 0;
            var planets       = new Array<Planet>();
            var systems       = new Array<SolarSystem>();
            foreach(var planet in Empire.Universe.PlanetsDict.Values)
            {
                if (!planet.Center.InRadius(this)) continue;
                WarValueOfPlanets += planet.ColonyWarValueTo(Owner);
                planets.AddUniqueRef(planet);
                systems.AddUniqueRef(planet.ParentSystem);
            }

            PlanetsInAo = planets.ToArray();
            SystemsInAo = systems.ToArray();
            PlanetData  = new AOPlanetData[PlanetsInAo.Length];

            for (int i = 0; i < PlanetsInAo.Length; i++)
            {
                var p  = PlanetsInAo[i];
                var pd = new AOPlanetData(p, Owner);
                PlanetData[i] = pd;
                WarValueOfPlanets += pd.WarValue;
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
            FleetGuid = CoreFleet?.Guid ?? Guid.Empty; 
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
            //Empire.Universe?.DebugWin?.DrawCircle(DebugModes.AO, Center, Radius, Owner.EmpireColor, 1);
            if (PlanetsInAo.Length == 0 && Owner != null) SetupPlanetsInAO();

            if (OurPlanetsInAo.Length == 0 && Owner != null && PlanetsInAo.Length > 0) OurPlanetsInAo = PlanetsInAo.Filter(p => p.Owner == Owner);

            UpdateThreatLevel();

            for (int i = 0; i < PlanetData.Length; i++) PlanetData[i].Update(Empire.Universe?.StarDate ?? 0);

            for (int i = ShipsWaitingForCoreFleet.Count - 1; i >= 0; --i)
            {
                Ship ship = ShipsWaitingForCoreFleet[i];
                ShipsWaitingForCoreFleet.RemoveAt(i);
                OffensiveForcePool.AddUnique(ship);
            }

            for (int i = OffensiveForcePool.Count-1; i >= 0; --i)
            {
                Ship ship = OffensiveForcePool[i];
                if (ship?.Active != true || ship.fleet != null || ship.shipData.Role == ShipData.RoleName.troop || ship.GetStrength() <= 0)
                {
                    OffensiveForcePool.RemoveAtSwapLast(i);
                }
            }

            if (CoreFleet != null)
            {
                for (int i = 0; i < CoreFleet.Ships.Count; i++)
                {
                    var ship = CoreFleet.Ships[i];
                    CoreFleet.Ships.RemoveAt(i);
                    OffensiveForcePool.AddUniqueRef(ship);
                }
                AOFull = ThreatLevel < CoreFleet.GetStrength() && OffensiveForcePool.Count > 0;
            }
            else
                AOFull = false;
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
            CoreFleet      = null;
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

        public float GetWarAttackValueOfSystemsInAOTo(Empire empire)
        {
            if (Owner == empire) return WarValueOfPlanets;
            return SystemsInAo.Sum(s => s.WarValueTo(empire));
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