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

    public sealed class AO : IShipPool
    {
        public static readonly Planet[] NoPlanets = new Planet[0];
        public static readonly SolarSystem[] NoSystem = new SolarSystem[0];

        [XmlIgnore][JsonIgnore] public Planet CoreWorld { get; private set; }
        [XmlIgnore][JsonIgnore] public Fleet CoreFleet { get; private set; }
        [XmlIgnore][JsonIgnore] Array<Ship> OffensiveForcePool = new Array<Ship>();
        [XmlIgnore][JsonIgnore] Array<Ship> ShipsWaitingForCoreFleet = new Array<Ship>();
        [XmlIgnore][JsonIgnore] Planet[] PlanetsInAo = NoPlanets;
        AOPlanetData[] PlanetData;
        [XmlIgnore][JsonIgnore] Planet[] OurPlanetsInAo = NoPlanets;
        [XmlIgnore][JsonIgnore] SolarSystem[] SystemsInAo = NoSystem;
        [XmlIgnore][JsonIgnore] Empire Owner;
        [XmlIgnore][JsonIgnore] int ThreatTimer;

        [Serialize(0)] public Guid Guid { get; set; } = Guid.NewGuid();
        [Serialize(1)] public string Name { get; set; }
        [Serialize(2)] public int ThreatLevel;
        [Serialize(3)] public Guid CoreWorldGuid;
        [Serialize(4)] public Array<Guid> OffensiveForceGuids = new Array<Guid>();
        [Serialize(5)] public Array<Guid> ShipsWaitingGuids   = new Array<Guid>();
        [Serialize(6)] public Guid FleetGuid;
        [Serialize(7)] public int WhichFleet = -1;
        [Serialize(8)] public float Radius;
        [Serialize(9)] public Vector2 Center;
        [Serialize(10)] public float WarValueOfPlanets;
        
        [XmlIgnore][JsonIgnore] public Empire OwnerEmpire => Owner;
        [XmlIgnore][JsonIgnore] public Array<Ship> Ships => OffensiveForcePool;

        public Fleet GetCoreFleet() => CoreFleet;
        public Planet GetPlanet() => CoreWorld;
        public Planet[] GetOurPlanets() => OurPlanetsInAo;

        public IReadOnlyList<Ship> GetOffensiveForcePool() => OffensiveForcePool;
        [XmlIgnore][JsonIgnore] public bool AOFull { get; private set; } = true;
        
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
            Radius        = radius;
            CoreWorld     = p;
            CoreWorldGuid = p.guid;
            Owner         = p.Owner;
            Center        = p.Center;
            WhichFleet    = p.Owner.CreateFleetKey();

            CoreFleet = new Fleet();
            CoreFleet.Name = "Core Fleet";
            CoreFleet.FinalPosition = p.Center;
            CoreFleet.Owner         = p.Owner;
            CoreFleet.IsCoreFleet   = true;
            p.Owner.GetFleetsDict()[WhichFleet] = CoreFleet;

            SetupPlanetsInAO();
        }

        public AO(Planet p, float radius, int whichFleet, Fleet coreFleet)
        {
            Radius        = radius;
            CoreWorld     = p;
            CoreWorldGuid = p.guid;
            Owner         = p.Owner;
            Center        = p.Center;
            WhichFleet    = whichFleet;

            if (coreFleet == null)
            {
                CoreFleet = new Fleet();
                CoreFleet.Name          = "Core Fleet";
                CoreFleet.FinalPosition = p.Center;
                CoreFleet.Owner         = p.Owner;
                CoreFleet.IsCoreFleet   = true;
                p.Owner.GetFleetsDict()[WhichFleet] = CoreFleet;
            }
            else
            {
                CoreFleet = coreFleet;
            }

            SetupPlanetsInAO();
        }

        public AO(Empire empire, Vector2 center, float radius)
        {
            Center = center;
            Radius = radius;
            Owner  = empire;
            SetupPlanetsInAO();
        }

        public bool Add(Ship ship)
        {
            if (ship.BaseStrength < 1f 
                || ship.DesignRole == ShipData.RoleName.bomber 
                || ship.DesignRole == ShipData.RoleName.troopShip 
                || ship.DesignRole == ShipData.RoleName.support)
                return false;

            if (ship.Pool == this)
                return true;

            ship.Pool?.Remove(ship);
            ship.Pool = this;
            OffensiveForcePool.Add(ship);
            return true;
        }

        public bool Remove(Ship ship)
        {
            if (ship.Pool != this)
                return false;

            ship.Pool = null;

            if (ship.fleet?.IsCoreFleet ?? false)
            {
                CoreFleet.RemoveShip(ship, returnToEmpireAI: true);
            }
            ShipsWaitingForCoreFleet.RemoveRef(ship);
            OffensiveForcePool.RemoveRef(ship);
            return true;
        }

        public bool Contains(Ship ship) => ship.Pool == this;

        public void InitFromSave(Empire owner)
        {
            SetPlanet(Planet.GetPlanetFromGuid(CoreWorldGuid));
            Owner = owner;
            SetupPlanetsInAO();
            OffensiveForcePool = Ship.GetShipsFromGuids(OffensiveForceGuids);
            ShipsWaitingForCoreFleet = Ship.GetShipsFromGuids(ShipsWaitingGuids);
            
            var fleet = owner.GetFleetsDict().FilterValues(f => f.Guid == FleetGuid).FirstOrDefault();

            if (fleet != null)
            {
                SetFleet(fleet);
            }
            else if (FleetGuid != Guid.Empty)
            {
                if (WhichFleet == -1)
                    return;

                if (owner.GetFleetsDict().TryGetValue(WhichFleet, out Fleet f))
                {
                    string fleetName = WhichFleet != -1 ? f.Name : "";
                    Log.Warning($"Savegame FleetGuid {FleetGuid} ({owner.Name} fleetIdx:{WhichFleet} [{fleetName}]) not found in owner FleetsDict!!");
                }
                else
                {
                    Log.Warning($"Savegame FleetGuid {FleetGuid} ({owner.Name} fleetIdx:{WhichFleet}) not found in owner FleetsDict!!");
                }
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
                ship?.loyalty.AddShipToManagedPools(ship);
            }
        }
    }
}