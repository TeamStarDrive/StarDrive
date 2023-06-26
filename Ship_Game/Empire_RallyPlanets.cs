using System;
using System.Collections.Generic;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Ships;
using static Ship_Game.Planet;

namespace Ship_Game;

public sealed partial class Empire
{
    public Planet[] SafeRallyPoints = Empty<Planet>.Array;
    public Planet[] UnsafeRallyPoints = Empty<Planet>.Array;

    public Planet[] SafePlanets = Empty<Planet>.Array; // p.Safe == true
    public Planet[] UnsafePlanets = Empty<Planet>.Array; // p.Safe == false
    public Planet[] MilitaryOutposts = Empty<Planet>.Array;
    
    public Planet[] SpacePorts = Empty<Planet>.Array;
    public Planet[] SafeSpacePorts = Empty<Planet>.Array; // p.Safe == true
    public Planet[] UnsafeSpacePorts = Empty<Planet>.Array; // p.Safe == false

    /// <summary>
    /// This is important to cache RallyPoints for different retreat decisions.
    /// All the validation should be done here, so AI related code can be faster.
    /// </summary>
    public void UpdateRallyPoints()
    {
        SafePlanets = OwnedPlanets.Filter(p => p.Safe);
        UnsafePlanets = OwnedPlanets.Filter(p => !p.Safe);
        MilitaryOutposts = OwnedPlanets.Filter(p => p.AllowInfantry); // Capitals allow Infantry as well

        SpacePorts = OwnedPlanets.Filter(p => p.HasSpacePort);
        SafeSpacePorts = SafePlanets.Filter(p => p.HasSpacePort);
        UnsafeSpacePorts = UnsafePlanets.Filter(p => p.HasSpacePort);

        Array<Planet> safeRallies = new(SafeSpacePorts);
        Array<Planet> unsafeRallies = new(UnsafeSpacePorts);

        // defeated empires and factions can use rally points now.
        if (OwnedPlanets.Count == 0)
        {
            HashSet<Planet> rallies = new();
            GetSafePlanetsNearOurStations(rallies);
            
            HashSet<SolarSystem> systemsWithFriends = new();
            foreach (Ship s in EmpireShips.OwnedShips)
                if (s.System is { PlanetList.NotEmpty: true } sys)
                    systemsWithFriends.Add(sys);

            foreach (SolarSystem sys in systemsWithFriends)
            {
                // find planet with the most friendly ships orbiting it
                Planet p = sys.PlanetList.FindMax(p => CountNearbyFriendlyShips(sys, p));
                rallies.Add(p);
            }

            if (rallies.Count == 0)
            {
                // find any solar system with no owners
                SolarSystem s = Universe.Systems.Find(s => s.OwnerList.Count == 0 && s.PlanetList.NotEmpty);
                if (s != null) rallies.Add(s.PlanetList.First);
            }

            safeRallies.AddRange(rallies);
            safeRallies.Sort(rp => rp.System.OwnerList.Count > 1);
        }
        else
        {
            if (safeRallies.Count == 0)
            {
                // Could not find any planet with space port and with no enemies in sensor range
                // So get the most producing planet and hope for the best
                Planet best = OwnedPlanets.FindMax(planet => planet.Prod.GrossIncome);
                if (best != null) safeRallies.Add(best);
            }

            // if we didn't find any spaceports, just add all regular colonies
            if (unsafeRallies.Count == 0)
                unsafeRallies.AddRange(OwnedPlanets.Filter(p => !p.HasSpacePort));
        }

        // super failSafe, just take any planet.
        if (safeRallies.Count == 0)
        {
            if (OwnedPlanets.NotEmpty)
                safeRallies.Add(OwnedPlanets[0]);
            else if (Universe.Planets.Count != 0)
                safeRallies.Add(Universe.Planets[0]);
        }

        SafeRallyPoints = safeRallies.ToArray();
        UnsafeRallyPoints = unsafeRallies.ToArray();
    }
    
    /// <summary>
    /// Finds the nearest rally point or any closest planet
    /// </summary>
    public Planet FindNearestRallyPoint(Vector2 location)
    {
        return SafeRallyPoints.FindClosestTo(location)
            ?? UnsafeRallyPoints.FindClosestTo(location);
    }

    public Planet FindNearestSafeRallyPoint(Vector2 location)
    {
        return SafePlanets.FindClosestTo(location)
            ?? UnsafePlanets.FindClosestTo(location);
    }

    public Planet FindNearestSpacePort(Vector2 position)
    {
        return SafeSpacePorts.FindClosestTo(position)
            ?? UnsafeSpacePorts.FindClosestTo(position)
            ?? FindNearestRallyPoint(position);
    }

    /// <summary>
    /// Finds the closest Planet inside a solar system which belongs to US.
    /// The planet must belong to THIS empire.
    /// </summary>
    public Planet FindNearestRallyPlanetInSystem(Vector2 position, SolarSystem sys)
    {
        return sys.PlanetList.FindClosestTo(position, p => p.Owner == this);
    }

    void GetSafePlanetsNearOurStations(HashSet<Planet> planets)
    {
        foreach (Ship station in OwnedShips)
        {
            if (station.IsPlatformOrStation)
            {
                if (station.IsTethered)
                {
                    planets.Add(station.GetTether());
                }
                else if (station.System is { } s)
                {
                    foreach (Planet planet in s.PlanetList)
                        if (planet.Owner == null)
                            planets.Add(planet);
                }
            }
        }
    }

    static int CountNearbyFriendlyShips(SolarSystem sys, Planet p)
    {
        int nearbyFriends = 0;
        float inRadius = p.GravityWellRadius;
        for (int i = 0; i < sys.ShipList.Count; ++i)
        {
            Ship s = sys.ShipList[i];
            if (s.Loyalty == p.Owner && s.Position.InRadius(p.Position, inRadius))
                ++nearbyFriends;
        }
        return nearbyFriends;
    }

    /// <summary>
    /// checks planets for shortest time to build.
    /// </summary>
    public bool FindPlanetToBuildShipAt(IReadOnlyList<Planet> ports, IShipDesign ship, out Planet chosen, 
        float priority = 1f, float portQuality = 0.5f)
    {
        if (ports.Count != 0)
        {
            float cost = ship.GetCost(this);
            chosen = FindPlanetToBuildAt(ports, cost, ship, portQuality, priority);
            return chosen != null;
        }

        if (NumPlanets != 0)
            Log.Info(ConsoleColor.Red, $"{this} could not find planet to build {ship} at! Candidates:{ports.Count}");
        chosen = null;
        return false;
    }

    public bool FindPlanetToBuildTroopAt(IReadOnlyList<Planet> ports, Troop troop, float priority, out Planet chosen)
    {
        if (ports.Count != 0)
        {
            float cost = troop.ActualCost(this);
            chosen = FindPlanetToBuildAt(ports, cost, sData: null, 0.2f, priority);
            return chosen != null;
        }

        if (NumPlanets != 0)
            Log.Info(ConsoleColor.Red, $"{this} could not find planet to build {troop} at! Candidates:{ports.Count}");
        chosen = null;
        return false;
    }

    Planet FindPlanetToBuildAt(IReadOnlyList<Planet> ports, float cost, IShipDesign sData, float portQuality, float priority)
    {
        // focus on the best producing planets (number depends on the empire size)
        if (GetBestPorts(ports, out Planet[] bestPorts, portQuality))
            return bestPorts.FindMin(p => p.TurnsUntilQueueComplete(cost, priority, sData));

        return null;
    }

    public bool FindPlanetToRefitAt(IReadOnlyList<Planet> ports, float cost, Ship ship, IShipDesign newShip, bool travelBack, out Planet planet)
    {
        planet = null;
        int travelMultiplier = travelBack ? 2 : 1;

        if (ports.Count == 0)
            return false;

        planet = ports.FindMin(p => p.TurnsUntilQueueComplete(cost, 1f, newShip)
                                  + ship.GetAstrogateTimeTo(p) * travelMultiplier);
        return planet != null;
    }

    public bool FindPlanetToRefitAt(IReadOnlyList<Planet> ports, float cost, IShipDesign newShip, out Planet planet)
    {
        planet = null;
        if (ports.Count == 0)
            return false;

        ports  = ports.Filter(p => !p.IsCrippled);
        if (ports.Count == 0)
            return false;

        planet = ports.FindMin(p => p.TurnsUntilQueueComplete(cost, 1f, newShip));
        return planet != null;
    }

    public IReadOnlyCollection<Planet> GetBestPortsForShipBuilding(float portQuality)
        => GetBestPortsForShipBuilding(OwnedPlanets, portQuality);
    
    public IReadOnlyCollection<Planet> GetBestPortsForShipBuilding(IReadOnlyList<Planet> ports, float portQuality)
    {
        if (ports == null) return Empty<Planet>.Array;
        GetBestPorts(ports, out Planet[] bestPorts, portQuality);
        return bestPorts?.Filter(p => p.HasSpacePort) ?? Empty<Planet>.Array;
    }

    // Port quality is the number to multiply the average max production potential limit.
    // only planets above this multiplier will be nominated.
    bool GetBestPorts(IReadOnlyList<Planet> ports, out Planet[] bestPorts, float portQuality)
    {
        bestPorts = null;
        // If all the ports are research colonies, do not filter them
        bool filterResearchPorts = ports.Any(p => p.CType != ColonyType.Research);
        if (ports.Count > 0)
        {
            float averageMaxProd = ports.Average(ModifiedNetMaxProductionPotential);
            bestPorts = ports.Filter(p => !p.IsCrippled
                                     && (p.CType != ColonyType.Research || !filterResearchPorts)
                                     && p.Prod.NetMaxPotential.GreaterOrEqual(averageMaxProd * portQuality));
        }


        float ModifiedNetMaxProductionPotential(Planet planet)
        {
            float maxPotential = planet.Prod.NetMaxPotential;
            switch (planet.CType)
            {
                case ColonyType.TradeHub:
                case ColonyType.Core:         maxPotential *= 0.5f;                  break;
                case ColonyType.Agricultural: maxPotential *= 0.25f;                 break;
                case ColonyType.Military:     maxPotential *= 0.75f;                 break;
                case ColonyType.Research:     maxPotential *= 0.1f;                  break;
                case ColonyType.Colony:       maxPotential  = planet.Prod.NetIncome; break;
            }

            return maxPotential;
        }

        return bestPorts?.Length > 0;
    }

    public Planet GetOrbitPlanetAfterBuild(Planet builtAt)
    {
        if (GetBestPorts(SafeSpacePorts, out Planet[] bestPorts, portQuality: 1) && !bestPorts.Contains(builtAt))
        {
            return bestPorts.Sorted(p => p.Position.Distance(builtAt.Position)).First();
        }

        return builtAt;
    }

    public bool FindPlanetToScrapIn(Ship ship, out Planet planet)
    {
        planet = null;
        if (OwnedPlanets.Count == 0)
            return false;

        if (!ship.BaseCanWarp)
        {
            planet = FindNearestRallyPoint(ship.Position);
            if (planet == null || planet.Position.Distance(ship.Position) > 50000)
                ship.ScuttleTimer = 5;

            return planet != null;
        }

        var scrapGoals = AI.FindGoals(g => g.Type == GoalType.ScrapShip);
        var potentialPlanets = OwnedPlanets.SortedDescending(p => p.MissingProdHereForScrap(scrapGoals)).TakeItems(5);
        if (potentialPlanets.Length == 0)
            return false;

        planet = potentialPlanets.FindMin(p => p.Position.Distance(ship.Position));
        return planet != null;
    }

}
