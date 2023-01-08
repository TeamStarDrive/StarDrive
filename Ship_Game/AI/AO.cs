using System;
using System.Collections.Generic;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI
{
    // ReSharper disable once InconsistentNaming
    public sealed class AOPlanetData
    {
        public int PlanetId;
        readonly Planet OwnerPlanet;
        public int BuildingCount;
        public float WarValue;
        readonly Empire DataOwner;

        public AOPlanetData(Planet p, Empire e)
        {
            OwnerPlanet = p;
            PlanetId  = p.Id;
            DataOwner   = e;
        }

        public void Update(float stardate)
        {
            if (stardate.LessOrEqual(stardate)) return;
            if (BuildingCount != OwnerPlanet.NumBuildings)
            {
                BuildingCount = OwnerPlanet.NumBuildings;
                WarValue = OwnerPlanet.ColonyWarValueTo(DataOwner);
            }
        }
    }

    [StarDataType]
    public sealed class AO : IShipPool
    {
        public static readonly Planet[] NoPlanets = Empty<Planet>.Array;

        [StarData] public Planet CoreWorld { get; private set; }
        [StarData] public Fleet CoreFleet { get; private set; }
        [StarData] Array<Ship> OffensiveForcePool = new();
        [StarData] Array<Ship> ShipsWaitingForCoreFleet = new();

        Planet[] AllPlanetsInAO = NoPlanets;
        AOPlanetData[] PlanetData;

        // Planets owned by this Owner
        [StarData] public Planet[] OurPlanets { get; private set; } = NoPlanets;
        [StarData] public Empire Owner { get; private set; }
        [StarData] int ThreatTimer;

        [StarData] public int Id { get; set; }
        [StarData] public string Name { get; set; }
        [StarData] public int ThreatLevel;
        [StarData] public int WhichFleet = -1;
        [StarData] public float Radius;
        [StarData] public Vector2 Center;
        public float WarValueOfPlanets;

        public Empire OwnerEmpire => Owner; // IShipPool
        public Array<Ship> Ships => OffensiveForcePool; // IShipPool

        public IReadOnlyList<Ship> GetOffensiveForcePool() => OffensiveForcePool;
        public int GetNumOffensiveForcePoolShips() => OffensiveForcePool.Count;

        public bool AOFull { get; private set; } = true;

        [StarDataConstructor] AO() {}

        AO(UniverseState us)
        {
            Id = us.CreateId();
        }

        public AO(UniverseState us, Planet planet, Empire empire, float radius) : this(us)
        {
            Owner = empire ?? throw new NullReferenceException(nameof(empire));
            CoreWorld = planet ?? throw new NullReferenceException(nameof(planet));

            Radius = radius;
            Center = planet.Position;
            WhichFleet = planet.Owner.CreateFleetKey();
            CoreFleet = planet.Owner.CreateFleet(WhichFleet, "Core Fleet "+WhichFleet);
            CoreFleet.FinalPosition = planet.Position;
            CoreFleet.IsCoreFleet = true;

            SetupPlanetsInAO();
        }

        public AO(UniverseState us, Planet p, Empire empire, float radius, int whichFleet, Fleet coreFleet) : this(us)
        {
            Owner = empire ?? throw new NullReferenceException(nameof(empire));
            Radius = radius;
            CoreWorld = p;
            Center = p.Position;
            WhichFleet = whichFleet;

            if (coreFleet == null)
            {
                CoreFleet = p.Owner.CreateFleet(WhichFleet, "Core Fleet "+WhichFleet);
                CoreFleet.FinalPosition = p.Position;
                CoreFleet.IsCoreFleet = true;
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

        [StarDataDeserialized]
        void OnDeserialized()
        {
            if (Owner == null)
                return; // BUG: save is corrupted

            SetupPlanetsInAO();
        }

        public bool Add(Ship ship)
        {
            if (ship.BaseStrength < 1f 
                || ship.DesignRole == RoleName.bomber 
                || ship.DesignRole == RoleName.troopShip 
                || ship.DesignRole == RoleName.support)
                return false;

            if (ship.Pool == this)
                return true;

            ship.Pool?.Remove(ship);
            ship.Pool = this;
            OffensiveForcePool.AddUnique(ship);
            return true;
        }

        public bool Remove(Ship ship)
        {
            if (ship.Pool != this)
                return false;

            ship.Pool = null;

            if (ship.Fleet != null && ship.Fleet == CoreFleet)
                CoreFleet.RemoveShip(ship, returnToEmpireAI: true, clearOrders: true);

            ShipsWaitingForCoreFleet.RemoveRef(ship);
            OffensiveForcePool.RemoveRef(ship);
            return true;
        }

        public bool Contains(Ship ship) => ship.Pool == this;

        public void UpdateThreatLevel()
        {
            if (--ThreatTimer > 0) return;
            ThreatLevel = (int)Owner.AI.ThreatMatrix.GetHostileStrengthAt(Center, Radius);
            // arbitrary performance consideration.
            ThreatTimer = 5;
        }

        void SetupPlanetsInAO()
        {
            WarValueOfPlanets = 0;
            var planets = new Array<Planet>();
            var systems = new Array<SolarSystem>();
            foreach(var planet in Owner.Universe.Planets)
            {
                if (!planet.Position.InRadius(this)) continue;
                WarValueOfPlanets += planet.ColonyWarValueTo(Owner);
                planets.AddUniqueRef(planet);
                systems.AddUniqueRef(planet.ParentSystem);
            }

            AllPlanetsInAO = planets.ToArray();
            PlanetData  = new AOPlanetData[AllPlanetsInAO.Length];

            for (int i = 0; i < AllPlanetsInAO.Length; i++)
            {
                var p  = AllPlanetsInAO[i];
                var pd = new AOPlanetData(p, Owner);
                PlanetData[i] = pd;
                WarValueOfPlanets += pd.WarValue;
            }
        }

        public void Update()
        {
            //Empire.Universe?.DebugWin?.DrawCircle(DebugModes.AO, Center, Radius, Owner.EmpireColor, 1);
            if (AllPlanetsInAO.Length == 0 && Owner != null)
                SetupPlanetsInAO();

            if (OurPlanets.Length == 0 && Owner != null && AllPlanetsInAO.Length > 0)
                OurPlanets = AllPlanetsInAO.Filter(p => p.Owner == Owner);

            UpdateThreatLevel();

            for (int i = 0; i < PlanetData.Length; i++)
                PlanetData[i].Update(Owner.Universe?.StarDate ?? 0);

            for (int i = ShipsWaitingForCoreFleet.Count - 1; i >= 0; --i)
            {
                Ship ship = ShipsWaitingForCoreFleet[i];
                ShipsWaitingForCoreFleet.RemoveAt(i);
                OffensiveForcePool.AddUnique(ship);
            }

            for (int i = OffensiveForcePool.Count-1; i >= 0; --i)
            {
                Ship ship = OffensiveForcePool[i];
                if (ship?.Active != true || ship.Fleet != null || ship.ShipData.Role == RoleName.troop || ship.GetStrength() <= 0)
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
        

        // Clears this AO, disbands the fleets, etc
        public void Clear()
        {
            if (CoreFleet != null)
            {
                if (CoreFleet.Owner != null)
                {
                    CoreFleet.Owner.RemoveFleet(CoreFleet);
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
            CoreFleet = null;
            CoreWorld = null;
            AllPlanetsInAO = NoPlanets;
            OurPlanets = NoPlanets;
        }

        void ReassignShips(Array<Ship> ships)
        {
            foreach(var ship in ships)
            {
                ship?.Loyalty.AddShipToManagedPools(ship);
            }
        }
    }
}