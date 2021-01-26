using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.AI.Tasks;
using Ship_Game.Empires.ShipPools;
using Ship_Game.Ships;
using static Ship_Game.Ships.ShipData;

namespace Ship_Game.AI
{
    /// <summary>
    /// used to classify a group of ships into fleets according to the fleet ratios.
    /// usage: create class and give it ships. it will talley their fleet characteristics
    /// and provide methods for extracting ship sets used for fleets.
    /// in general before extracting make sure that the tallies at least match what is wanted. 
    /// </summary>
    public class FleetShips
    {
        // need to add a way to prefer ships near to a point
        public float AccumulatedStrength { get; private set; }
        private readonly Empire OwnerEmpire;
        private readonly FleetRatios Ratios;
        private readonly Array<Ship> Ships = new Array<Ship>();
        public float WantedFleetCompletePercentage = 0.25f;
        public int InvasionTroops { get; private set; }
        public float InvasionTroopStrength { get; private set; }
        public int BombSecsAvailable { get; private set; }
        readonly int[] RoleCount;
        readonly float[] RoleStrength;
        public int ShipSetsExtracted = 0;

        public FleetShips(Empire ownerEmpire)
        {
            OwnerEmpire  = ownerEmpire;
            Ratios       = new FleetRatios(OwnerEmpire);
            int items    = Enum.GetNames(typeof(EmpireAI.RoleBuildInfo.RoleCounts.CombatRole)).Length;
            RoleCount    = new int[items];
            RoleStrength = new float[items];
        }

        public FleetShips(Empire ownerEmpire, Ship[] ships) : this(ownerEmpire)
        {
            AddShips(ships);
        }

        void AddShips(IEnumerable<Ship> ships)
        {
            foreach (var ship in ships) AddShip(ship);
        }

        public bool AddShip(Ship ship)
        {
            if (!ship.ShipIsGoodForGoals())
                return false;

            if (ship.fleet != null)
            {
                Log.Error($"FleetRatios: attempting to add a ship already in a fleet '{ship.fleet.Name}'. removing from fleet");
                ship.ClearFleet();
                foreach (var fleet in OwnerEmpire.GetFleetsDict())
                {
                    fleet.Value.RemoveShip(ship);
                }
            }

            if (ship.IsPlatformOrStation
                || ship.fleet != null
                || ship.IsHangarShip
                || ship.AI.State == AIState.Scrap
                || ship.AI.State == AIState.Resupply
                || ship.AI.State == AIState.Refit
                || ship.IsHomeDefense)
            {
                return false;
            }

            int roleIndex            = (int)EmpireAI.RoleBuildInfo.RoleCounts.ShipRoleToCombatRole(ship.DesignRole);
            RoleCount[roleIndex]    += 1;
            RoleStrength[roleIndex] += ship.GetStrength(); 
            AccumulatedStrength     += ship.GetStrength();
            
            Ships.Add(ship);

            if (ShipRoleToRoleType(ship.DesignRole) == RoleType.Troop)
            {
                InvasionTroops += ship.Carrier.PlanetAssaultCount;
                InvasionTroopStrength += ship.Carrier.PlanetAssaultStrength;
            }
            if (ship.DesignRole == RoleName.bomber)
                BombSecsAvailable += ship.BombsGoodFor60Secs;

            return true;
        }

        public float FleetsStrength()
        {
            CountFleets(out var strength);
            return strength;
        }

        public int CountFleets(out float strength)
        {
            float filledRoles = float.MaxValue;
            strength = 0;
            int wantedRoles = 0;
            float completionWanted = 0.1f;
            int maxCombatIndex = Ratios.MaxCombatRoleIndex();
            foreach(EmpireAI.RoleBuildInfo.RoleCounts.CombatRole item in Enum.GetValues(typeof(EmpireAI.RoleBuildInfo.RoleCounts.CombatRole)))
            {
                float wanted       = (Ratios.GetWanted(item) * completionWanted).LowerBound(1);// > 0 ? 1 : 0;
                if (wanted        <= 0) continue;
                wantedRoles++;
                int index          = (int)item;
                float have         = RoleCount[index];
                float filled       = have / wanted;
                float roleStrength = RoleStrength[index];
                strength          += (filled > 1 ? roleStrength : 0);
                bool requiredRole = index > maxCombatIndex - 1 && index < maxCombatIndex + 1;
                if (!requiredRole) continue;
                filledRoles       = Math.Min(filled, filledRoles);
            }
            return (int)filledRoles;
        }

        public int ExtractFleetShipsUpToStrength(float strengthNeeded, int wantedFleetCount,
            out Array<Ship> ships)
        {
            float extractedShipStrength = 0;
            int completeFleets          = 0;
            ships                       = new Array<Ship>();
            int neededFleets              = wantedFleetCount.LowerBound(1);
            var utilityShips            = new Array<Ship>();
            do
            {
                var gatheredShips = GetCoreFleet(out bool goodSet);
                if (gatheredShips.IsEmpty)
                    break;

                extractedShipStrength += gatheredShips.Sum(s => s.GetStrength());
                ships.AddRange(gatheredShips);

                if (goodSet)
                {
                    completeFleets++;
                    neededFleets--;
                    ShipSetsExtracted++;
                }
     
            }
            while (neededFleets > 0 || extractedShipStrength < strengthNeeded);

            if (extractedShipStrength >= strengthNeeded && completeFleets > 0)
                completeFleets = wantedFleetCount + 1;

            for (int x =0; x< (wantedFleetCount + completeFleets).LowerBound(1); x++)
                utilityShips.AddRange(GetSupplementalFleet());

            ships.AddRange(utilityShips);
            return completeFleets;
        }

        public Array<Ship> GetCoreFleet(out bool goodSet)
        {
            var ships = new Array<Ship>();
            goodSet   = true;
            bool roleGood;
            ships.AddRange(ExtractCoreFleetRole(RoleName.fighter,  out  roleGood));  bool badSet = !roleGood;
            ships.AddRange(ExtractCoreFleetRole(RoleName.corvette, out roleGood));        badSet = badSet || !roleGood;
            ships.AddRange(ExtractCoreFleetRole(RoleName.frigate, out roleGood));         badSet = badSet || !roleGood;
            ships.AddRange(ExtractCoreFleetRole(RoleName.cruiser, out roleGood));         badSet = badSet || !roleGood;
            ships.AddRange(ExtractCoreFleetRole(RoleName.battleship, out roleGood));      badSet = badSet || !roleGood;
            ships.AddRange(ExtractCoreFleetRole(RoleName.capital, out roleGood));         badSet = badSet || !roleGood;
            goodSet = !badSet;
            return ships;
        }

        public Array<Ship> ExtractCoreFleetRole(RoleName role, out bool fullFilled)
        {
            int combatIndex = FleetRatios.CombatRoleToRatio(role);
            int maxIndex = Ratios.MaxCombatRoleIndex();
            float wanted = Ratios.GetWanted(role);
            int requirementSpread = 0;
            if (maxIndex > 2) requirementSpread += 1;
            bool required =  maxIndex > 0 && wanted >= 1 && combatIndex + requirementSpread >= maxIndex;


            var ships = ExtractShips(Ships,  wanted, role, required);
            fullFilled = !required || ships.NotEmpty;
            return ships;
        }

        public Array<Ship> GetSupplementalFleet()
        {
            var ships = new Array<Ship>();
            ships.AddRange(ExtractShips(Ships, Ratios.MinCarriers, RoleName.carrier, false));
            ships.AddRange(ExtractShips(Ships, Ratios.MinSupport, RoleName.support,false));
            return ships;
        }

        public Array<Ship> ExtractSetsOfCombatShips(float strength, int wantedFleetCount , out int fleetCount)
        {
            Array<Ship> ships = new Array<Ship>();
            fleetCount = ExtractFleetShipsUpToStrength(strength, wantedFleetCount, out Array<Ship> fleetShips);
            
            if (fleetCount > 0 && fleetShips.Sum(s=> s.GetStrength()) >= strength)
            {
                ships.AddRange(fleetShips);
            }
            else
            {
                AddShips(fleetShips);
            }

            return ships;
        }

        public Array<Ship> ExtractTroops(int planetAssaultTroopsWanted)
        {
            var ships = ExtractShipsByFeatures(Ships, planetAssaultTroopsWanted, 1, 1
                                , s =>
                                    {
                                        if (s.DesignRoleType == RoleType.Troop)
                                            return s.Carrier.PlanetAssaultCount;
                                        return 0;
                                    });
            return ships;
        }

        public Array<Ship> ExtractBombers(int bombSecsWanted, int fleetCount)
        {
            var ships = new Array<Ship>();
            if (bombSecsWanted > 0 && Ratios.MinBombers > 0)
                ships = ExtractShipsByFeatures(Ships, bombSecsWanted, 1
                                    , fleetCount * (int)Ratios.MinBombers
                                    , s =>
                                    {
                                        if (s.DesignRole == RoleName.bomber)
                                            return s.BombsGoodFor60Secs;
                                        return 0;
                                    });
            return ships;
        }

        private Array<Ship> ExtractShips(Array<Ship> ships, float wanted, RoleName role, bool required)
            => ExtractShips(ships, wanted, s => s.DesignRole == role, required);

        private Array<Ship> ExtractShips(Array<Ship> ships, float wanted, Func<Ship, bool> shipFilter, bool required)
        {
            var shipSet = new Array<Ship>();
            int setWanted = (int)(wanted * WantedFleetCompletePercentage);
            if (wanted > 0)
                for (int x = ships.Count - 1; x >= 0; x--)
                {
                    Ship ship = ships[x];
                    if (!shipFilter(ship))
                        continue;

                    shipSet.Add(ship);
                    Ships.RemoveSwapLast(ship);
                    AccumulatedStrength += ship.GetStrength();

                    if (shipSet.Count >= setWanted)
                        break;
                }
            if (!required || shipSet.Count >= setWanted)
                return shipSet;
            AddShips(shipSet);
            shipSet.Clear();
            return shipSet;
        }
        /// <summary>
        /// Extracts ships with wanted feature up to the count of the wanted feature.
        /// ex. I want 10 bombs. give me as many ships as it takes to get 10 bombs.
        /// </summary>
        /// <param name="ships">Ships to be processed</param>
        /// /// <param name="totalFeatureWanted">max of total features found on all matching ships</param>
        /// <param name="minWantedFeature">min count of feature the ship should have</param>
        /// <param name="shipFeatureCount">Number of times ship matches the feature.
        /// like ship.BombsGoodFor60Secs</param>
        /// <returns></returns>
        private Array<Ship> ExtractShipsByFeatures(Array<Ship> ships, int totalFeatureWanted, 
                                                   float minWantedFeature, int shipCount, Func<Ship, int> shipFeatureCount)
        {
            var shipSet = new Array<Ship>();
            if (minWantedFeature < 1) return shipSet;

            for (int x = ships.Count - 1; x >= 0; x--)
            {
                if (totalFeatureWanted <= 0 && shipSet.Count >= shipCount)
                    break;
                Ship ship = ships[x];
                int countOfShipFeature = shipFeatureCount(ship);
                if (countOfShipFeature >= minWantedFeature)
                {
                    Ships.RemoveSwapLast(ship);
                    shipSet.Add(ship);
                    totalFeatureWanted -= countOfShipFeature;
                }
            }

            return shipSet;
        }

        /// <summary>
        /// Extracts a ship set with wanted characteristics. 
        /// </summary>
        /// <param name="minStrength">Combat strength of fleet ships</param>
        /// <param name="bombingSecs">Time fleet should be able to bomb</param>
        /// <param name="wantedTroopStrength">Troop strength to invade with</param>
        /// <param name="planetTroops">Troops still on planets</param>
        /// /// <param name="minimumFleetSize">Attempt to get this many fleets</param>
        /// <returns></returns>
        public Array<Ship> ExtractShipSet(float minStrength, Array<Troop> planetTroops, int minimumFleetSize, Vector2 rallyPoint, MilitaryTask task)
        {
            // create static empty ship array.
            if (BombSecsAvailable < task.TaskBombTimeNeeded) return new Array<Ship>();

            SortShipsByDistanceToPoint(task.AO);

            Array<Ship> ships = ExtractSetsOfCombatShips(minStrength, minimumFleetSize, out int fleetCount);
            
            if (ships.IsEmpty)
                return new Array<Ship>();
            
            if (task.NeededTroopStrength > 0)
            {
                if (InvasionTroopStrength < task.NeededTroopStrength)
                    LaunchTroopsAndAddToShipList(task.NeededTroopStrength, planetTroops);
                
                if (InvasionTroopStrength < task.NeededTroopStrength) return new Array<Ship>();

                ships.AddRange(ExtractTroops(task.NeededTroopStrength));
            }

            ships.AddRange(ExtractBombers(task.TaskBombTimeNeeded, fleetCount));

            CheckForShipErrors(ships);

            return ships;
        }

        void SortShipsByDistanceToPoint(Vector2 point)
        {
            bool inWarTheater = OwnerEmpire.AllActiveWarTheaters.Any(t => t.TheaterAO.Center.InRadius(point, t.TheaterAO.Radius * 2));

            Ships.Sort(s =>
            {
                bool shipInTheater =OwnerEmpire.AllActiveWarTheaters.Any(t =>
                {
                    bool inTheater = t.TheaterAO.Center.InRadius(s.Center, t.TheaterAO.Radius * 2);
                    AO rallyAO = t.RallyAO;
                    bool  inTheaterRally = rallyAO?.Center.InRadius(s.Center, rallyAO.Radius * 2) ?? false;

                    return inTheater || inTheaterRally;
                });
                if (!inWarTheater && shipInTheater || (s.System?.HostileForcesPresent(OwnerEmpire) ?? false))
                    return s.Center.SqDist(point) + Empire.Universe.UniverseSize;
                return s.Center.SqDist(point);

            });
        }

        static void CheckForShipErrors(Array<Ship> ships)
        {
            if (Debugger.IsAttached)
            {
                foreach (var ship in ships)
                {
                    if (ship.fleet != null)
                        throw new Exception("Fleet should be null here.");

                    int n = ships.CountRef(ship);
                    //if (n > 1) 
                    //    throw new Exception($"Fleet ships contain duplicates({n}): {ship}.");
                }
            }
        }

        private void LaunchTroopsAndAddToShipList(int wantedTroopStrength, Array<Troop> planetTroops)
        {
            foreach (Troop troop in planetTroops.Filter(t => t.HostPlanet != null &&
                                                             !t.HostPlanet.RecentCombat))
            {
                if (InvasionTroopStrength > wantedTroopStrength)
                    break;

                if (troop.Loyalty == null)
                    continue;
                Ship launched = troop.Launch(true);
                if (launched == null)
                {
                    Log.Warning($"CreateFleet: Troop launched from planet became null");
                    continue;
                }

                AddShip(launched);
            }
        }
    }
}