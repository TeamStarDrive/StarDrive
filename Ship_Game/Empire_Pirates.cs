using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public partial class Empire
    {
        bool GetCorsairOrbitals(out Array<Ship> orbitals, Array<string> orbitalNames)
        {
            orbitals = new Array<Ship>();
            for (int i = 0; i < OwnedShips.Count; i++)
            {
                Ship ship = OwnedShips[i];
                if (orbitalNames.Contains(ship.Name))
                    orbitals.Add(ship);
            }

            return orbitals.Count > 0;
        }

        public bool GetCorsairBases(out Array<Ship> bases)    => GetCorsairOrbitals(out bases, CorsairBases());
        public bool GetCorsairStations(out Array<Ship> bases) => GetCorsairOrbitals(out bases, CorsairStations());

        Array<string> CorsairBases()
        {
            Array<string> bases = new Array<string>();
            if (this != EmpireManager.Corsairs)
                return bases; // Only for pirates

            bases.Add(data.PirateBaseBasic);
            bases.Add(data.PirateBaseImproved);
            bases.Add(data.PirateBaseAdvanced);

            return bases;
        }

        Array<string> CorsairStations()
        {
            Array<string> stations = new Array<string>();
            if (this != EmpireManager.Corsairs)
                return stations; // Only for pirates

            stations.Add(data.PirateStationBasic);
            stations.Add(data.PirateStationImproved);
            stations.Add(data.PirateStationAdvanced);

            return stations;
        }

        bool GetCorsairOrbitalsOrbitingPlanets(out Array<Ship> planetBases)
        {
            planetBases = new Array<Ship>();
            GetCorsairBases(out Array<Ship> bases);
            GetCorsairStations(out Array<Ship> stations);
            bases.AddRange(stations);

            for (int i = 0; i < bases.Count; i++)
            {
                Ship pirateBase = bases[i];
                if (pirateBase.GetTether() != null)
                    planetBases.AddUnique(pirateBase);
            }

            return planetBases.Count > 0;
        }

        public bool GetClosestCorsairBasePlanet(Vector2 fromPos, out Planet planet)
        {
            planet = null;
            if (!GetCorsairOrbitalsOrbitingPlanets(out Array<Ship> bases))
                return false;

            Ship pirateBase = bases.FindMin(b => b.Center.Distance(fromPos));
            planet          = pirateBase.GetTether();

            return planet != null;
        }

        public void CorsairsTryLevelUp()
        {
            if (this != EmpireManager.Corsairs)
                return; // Only for pirates

            if (RandomMath.RollDie(20) > PirateThreatLevel)
                IncreasePirateThreatLevel();
        }

        public void ReduceOverallPirateThreatLevel()
        {
            var empires = EmpireManager.Empires.Filter(e => !e.isFaction);
            for (int i = 0; i < empires.Length; i++)
            {
                Empire empire = empires[i];
                empire.SetPirateThreatLevel((empire.PirateThreatLevel - 1).LowerBound(1));
            }

            SetPirateThreatLevel(PirateThreatLevel - 1);
            if (PirateThreatLevel < 1)
            {
                EmpireAI.Goals.Clear();
                SetAsDefeated();
            }
        }

        public void IncreasePirateThreatLevel()
        {
            SetPirateThreatLevel((PirateThreatLevel + 1).UpperBound(20));
            if (this == EmpireManager.Corsairs)
            {
                PirateNewLevelOps(PirateThreatLevel);
            }
        }

        void PirateNewLevelOps(int level)
        {
            NewPirateBaseSpot spotType = (NewPirateBaseSpot)RandomMath.IntBetween(0, 3);
            switch (spotType)
            {
                case NewPirateBaseSpot.GasGiant:
                case NewPirateBaseSpot.Habitable:    BuildPirateBaseOrbitingPlanet(spotType); break;
                case NewPirateBaseSpot.AsteroidBelt: BuildPirateBaseInAsteroids();            break;
            }

        }

        void BuildPirateBaseInAsteroids()
        {
            if (!GetPirateBaseAsteroidsSpot(out Vector2 pos))
                return;  // build in deep space

            SpawnPirateShip(PirateShipType.Base, pos, out _);
        }

        void BuildPirateBaseOrbitingPlanet(NewPirateBaseSpot spot)
        {
            if (!GetPirateBasePlanet(spot, out Planet planet))
                return; // build in deep space

            Vector2 pos        = planet.Center.GenerateRandomPointInsideCircle(2000);
            if (SpawnPirateShip(PirateShipType.Base, pos, out Ship pirateBase))
                pirateBase.TetherToPlanet(planet);
        }


        bool GetPirateBaseAsteroidsSpot(out Vector2 position)
        {
            position = Vector2.Zero;
            if (!GetUnownedSystems(out SolarSystem[] systems))
                return false;

            var systemsWithAsteroids = systems.Filter(s => s.RingList.Any(r => r.Asteroids));
            if (systemsWithAsteroids.Length == 0)
                return false;

            SolarSystem selectedSystem = systemsWithAsteroids.RandItem();
            var asteroidRings          = selectedSystem.RingList.Filter(r => r.Asteroids);

            SolarSystem.Ring selectedRing = asteroidRings.RandItem();
            position = selectedSystem.Position.GenerateRandomPointOnCircle(selectedRing.OrbitalDistance);

            return position != Vector2.Zero;
        }
        
        bool GetPirateBasePlanet(NewPirateBaseSpot spot, out Planet selectedPlanet)
        {
            selectedPlanet = null;
            if (!GetUnownedSystems(out SolarSystem[] systems))
                return false;

            Array<Planet> planets = new Array<Planet>();
            for (int i = 0; i < systems.Length; i++)
            {
                SolarSystem system = systems[i];
                switch (spot)
                {
                    case NewPirateBaseSpot.Habitable: 
                        planets.AddRange(system.PlanetList.Filter(p => p.Habitable)); 
                        break;
                    case NewPirateBaseSpot.GasGiant: 
                        planets.AddRange(system.PlanetList.Filter(p => p.Category == PlanetCategory.GasGiant)); 
                        break;
                }
            }
            if (planets.Count == 0)
                return false;

            selectedPlanet = planets.RandItem();
            return selectedPlanet != null;
        }

        bool GetUnownedSystems(out SolarSystem[] systems)
        {
            systems = UniverseScreen.SolarSystemList.Filter(s => s.OwnerList.Count == 0 && s.RingList.Count > 0);
            return systems.Length > 0;
        }

        public struct PirateForces
        {
            public readonly string Fighter;
            public readonly string Frigate;
            public readonly string BoardingShip;
            public readonly string Base;
            public readonly string Station;

            public PirateForces(Empire pirates)
            {
                switch (pirates.PirateThreatLevel)
                {
                    case 1:
                    case 2:
                    case 3: 
                        Fighter      = pirates.data.PirateFighterBasic;
                        Frigate      = pirates.data.PirateFrigateBasic;
                        BoardingShip = pirates.data.PirateSlaverBasic;
                        Base         = pirates.data.PirateBaseBasic;
                        Station      = pirates.data.PirateStationBasic;
                        break;
                    case 4:
                    case 5:
                    case 6:
                        Fighter      = pirates.data.PirateFighterImproved;
                        Frigate      = pirates.data.PirateFrigateImproved;
                        BoardingShip = pirates.data.PirateSlaverImproved;
                        Base         = pirates.data.PirateBaseImproved;
                        Station      = pirates.data.PirateStationImproved;
                        break;
                    default:
                        Fighter      = pirates.data.PirateFighterAdvanced;
                        Frigate      = pirates.data.PirateFrigateAdvanced;
                        BoardingShip = pirates.data.PirateSlaverAdvanced;
                        Base         = pirates.data.PirateBaseAdvanced;
                        Station      = pirates.data.PirateStationAdvanced;
                        break;
                }
            }
        }

        public bool SpawnPirateShip(PirateShipType shipType, Vector2 where, out Ship pirateShip)
        {
            PirateForces forces = new PirateForces(this);
            string shipName = "";
            switch (shipType)
            {
                case PirateShipType.Fighter:  shipName = forces.Fighter;      break;
                case PirateShipType.Frigate:  shipName = forces.Frigate;      break;
                case PirateShipType.Boarding: shipName = forces.BoardingShip; break;
                case PirateShipType.Base:     shipName = forces.Base;         break;
                case PirateShipType.Station:  shipName = forces.Station;      break;
            }

            pirateShip = Ship.CreateShipAtPoint(forces.BoardingShip, this, where);
            return shipName.NotEmpty() && pirateShip != null;
        }

        enum NewPirateBaseSpot
        {
            AsteroidBelt,
            GasGiant,
            Habitable,
            DeepSpace
        }
    }

    public enum PirateShipType
    {
        Fighter,
        Frigate,
        Boarding,
        Base,
        Station
    }
}
