using System.Collections.Generic;
using System.Xml.Serialization;
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
            if (BuildingCount != OwnerPlanet.BuildingList.Count)
            {
                BuildingCount = OwnerPlanet.BuildingList.Count;
                WarValue = OwnerPlanet.ColonyWarValueTo(DataOwner);
            }
        }
    }

    [StarDataType]
    public sealed class AO : IShipPool
    {
        public static readonly Planet[] NoPlanets = new Planet[0];

        [StarData] public Planet CoreWorld { get; private set; }
        [StarData] public Fleet CoreFleet { get; private set; }
        [StarData] Array<Ship> OffensiveForcePool = new();
        [StarData] Array<Ship> ShipsWaitingForCoreFleet = new();
        Planet[] PlanetsInAo = NoPlanets;
        AOPlanetData[] PlanetData;
        [StarData] Planet[] OurPlanetsInAo = NoPlanets;
        [StarData] Empire Owner;
        [StarData] int ThreatTimer;

        [StarData] public int Id { get; set; }
        [StarData] public string Name { get; set; }
        [StarData] public int ThreatLevel;
        [StarData] public int WhichFleet = -1;
        [StarData] public float Radius;
        [StarData] public Vector2 Center;
        public float WarValueOfPlanets;

        public Empire OwnerEmpire => Owner;
        public Array<Ship> Ships => OffensiveForcePool;

        public Fleet GetCoreFleet() => CoreFleet;
        public Planet GetPlanet() => CoreWorld;
        public Planet[] GetOurPlanets() => OurPlanetsInAo;

        public IReadOnlyList<Ship> GetOffensiveForcePool() => OffensiveForcePool;
        public bool AOFull { get; private set; } = true;

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
            ThreatLevel = (int)Owner.AI.ThreatMatrix.GetHostileStrengthAt(Center, Radius);
            // arbitrary performance consideration.
            ThreatTimer = 5;
        }

        public int GetNumOffensiveForcePoolShips() => OffensiveForcePool.Count;
        public bool OffensiveForcePoolContains(Ship s) => OffensiveForcePool.ContainsRef(s);
        public bool WaitingShipsContains(Ship s)       => ShipsWaitingForCoreFleet.ContainsRef(s);

        // for deserialization only
        public AO()
        {
        }

        public AO(UniverseState us)
        {
            Id = us.CreateId();
        }

        public AO(UniverseState us, Vector2 center, float radius) : this(us)
        {
            Center = center;
            Radius = radius;
        }

        public AO(UniverseState us, Empire empire) : this(us)
        {
            Center = empire.WeightedCenter;
            Radius = empire.Universe.Size / 4;
        }

        public AO(UniverseState us, Planet p, float radius) : this(us)
        {
            Radius        = radius;
            CoreWorld     = p;
            Owner         = p.Owner;
            Center        = p.Position;
            WhichFleet    = p.Owner.CreateFleetKey();

            CoreFleet = new(p.Universe.CreateId(), p.Owner)
            {
                Name = "Core Fleet",
                FinalPosition = p.Position,
                IsCoreFleet = true
            };
            p.Owner.SetFleet(WhichFleet, CoreFleet);

            SetupPlanetsInAO();
        }

        public AO(UniverseState us, Planet p, float radius, int whichFleet, Fleet coreFleet) : this(us)
        {
            Radius        = radius;
            CoreWorld     = p;
            Owner         = p.Owner;
            Center        = p.Position;
            WhichFleet    = whichFleet;

            if (coreFleet == null)
            {
                CoreFleet = new(p.Universe.CreateId(), p.Owner)
                {
                    Name = "Core Fleet",
                    FinalPosition = p.Position,
                    IsCoreFleet = true
                };
                p.Owner.SetFleet(WhichFleet, CoreFleet);
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

            PlanetsInAo = planets.ToArray();
            PlanetData  = new AOPlanetData[PlanetsInAo.Length];

            for (int i = 0; i < PlanetsInAo.Length; i++)
            {
                var p  = PlanetsInAo[i];
                var pd = new AOPlanetData(p, Owner);
                PlanetData[i] = pd;
                WarValueOfPlanets += pd.WarValue;
            }
        }

        public void Update()
        {
            //Empire.Universe?.DebugWin?.DrawCircle(DebugModes.AO, Center, Radius, Owner.EmpireColor, 1);
            if (PlanetsInAo.Length == 0 && Owner != null) SetupPlanetsInAO();

            if (OurPlanetsInAo.Length == 0 && Owner != null && PlanetsInAo.Length > 0) OurPlanetsInAo = PlanetsInAo.Filter(p => p.Owner == Owner);

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

        public void ClearOut()
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
            CoreFleet      = null;
            CoreWorld      = null;
            PlanetsInAo    = null;
            OurPlanetsInAo = null;
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