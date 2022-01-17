using Microsoft.Xna.Framework;
using Ship_Game.AI.Tasks;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static Ship_Game.Ships.ShipDesign;

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
        private Empire OwnerEmpire;
        private FleetRatios Ratios;
        private Array<Ship> Ships = new Array<Ship>();
        public float WantedFleetCompletePercentage = 0.25f;
        public int InvasionTroops { get; private set; }
        public float InvasionTroopStrength { get; private set; }
        public int BombSecsAvailable { get; private set; }
        readonly int[] RoleCount;
        readonly float[] RoleStrength;
        public int ShipSetsExtracted;
        public int TotalShips => Ships.Count;

        public FleetShips(Empire ownerEmpire)
        {
            OwnerEmpire  = ownerEmpire;
            Ratios       = new FleetRatios(OwnerEmpire);
            int items    = Enum.GetNames(typeof(EmpireAI.RoleBuildInfo.RoleCounts.CombatRole)).Length;
            RoleCount    = new int[items];
            RoleStrength = new float[items];
        }

        public FleetShips(Empire ownerEmpire, Array<Ship> ships) : this(ownerEmpire)
        {
            AddShips(ships);
        }

        void AddShips(Array<Ship> ships)
        {
            foreach (Ship ship in ships)
                AddShip(ship);
        }

        public bool AddShip(Ship ship)
        {
            if (!ship.ShipIsGoodForGoals())
                return false;

            if (ship.Fleet != null)
            {
                Log.Error($"FleetRatios: attempting to add a ship already in a fleet '{ship.Fleet.Name}'. removing from fleet");
                ship.ClearFleet(returnToManagedPools: false, clearOrders: false);
            }

            if (ship.IsPlatformOrStation
                || ship.Fleet != null
                || ship.IsHangarShip
                || ship.AI.State == AIState.Scrap
                || ship.AI.State == AIState.Resupply
                || ship.AI.State == AIState.Refit
                || ship.IsHomeDefense)
            {
                return false;
            }

            int roleIndex = (int)EmpireAI.RoleBuildInfo.RoleCounts.ShipRoleToCombatRole(ship.DesignRole);
            RoleCount[roleIndex] += 1;
            RoleStrength[roleIndex] += ship.GetStrength();
            AccumulatedStrength += ship.GetStrength();

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
            if (OwnerEmpire.isFaction || OwnerEmpire.isPlayer)
            {
                strength = 0;
                return 0;
            }

            float filledRoles = float.MaxValue;
            strength = 0;
            float completionWanted = 0.1f;
            foreach(EmpireAI.RoleBuildInfo.RoleCounts.CombatRole item in Enum.GetValues(typeof(EmpireAI.RoleBuildInfo.RoleCounts.CombatRole)))
            {
                float wanted = Ratios.GetWanted(item);
                

                if (wanted > 0)
                {
                    wanted = (Ratios.GetWanted(item) * completionWanted).LowerBound(1);
                    int index          = (int)item;

                    float have = RoleCount[index];

                    if (have > 0)
                    {
                        float filled = have / wanted;
                        float roleStrength = RoleStrength[index] * wanted / have;
                        strength += (filled > 1 ? roleStrength : 0);

                        filledRoles = Math.Min(filled, filledRoles);
                    }
                }
            }
            if (filledRoles >= float.MaxValue)
                return 0;
            return (int)filledRoles;
        }

        public int ExtractFleetShipsUpToStrength(float strengthNeeded, int wantedFleetCount,
            out Array<Ship> ships)
        {
            float extractedShipStrength = 0;
            int completeFleets          = 0;
            ships                       = new Array<Ship>();
            int neededFleets            = wantedFleetCount.LowerBound(1);
            var utilityShips            = new Array<Ship>();
            do
            {
                var gatheredShips = GetCoreFleet(out bool goodSet);
                if (gatheredShips.IsEmpty)
                    break;

                extractedShipStrength += gatheredShips.Sum(s => s.GetStrength());
                ships.AddRange(gatheredShips);

                if (goodSet && ships.Count > 0)
                {
                    completeFleets++;
                    neededFleets--;
                    ShipSetsExtracted++;
                }
     
            }
            while (neededFleets > 0 || extractedShipStrength < strengthNeeded);

            // we have enough strength so its good.
            if (extractedShipStrength >= strengthNeeded)
                completeFleets = wantedFleetCount + 1;
            else
                return 0; // bail on failed fleet creation

            for (int x =0; x< (wantedFleetCount + completeFleets).LowerBound(1); x++)
                utilityShips.AddRange(GetSupplementalFleet());

            ships.AddRange(utilityShips);
            return completeFleets;
        }

        // core combat section of a fleet
        public Array<Ship> GetCoreFleet(out bool goodSet)
        {
            var ships = new Array<Ship>();
            goodSet   = false;
            bool roleGood = false;
            ships.AddRange(ExtractCoreFleetRole(RoleName.fighter,  out roleGood));   goodSet |= roleGood;
            ships.AddRange(ExtractCoreFleetRole(RoleName.corvette, out roleGood));   goodSet |= roleGood;
            ships.AddRange(ExtractCoreFleetRole(RoleName.frigate, out roleGood));    goodSet |= roleGood;
            ships.AddRange(ExtractCoreFleetRole(RoleName.cruiser, out roleGood));    goodSet |= roleGood;
            ships.AddRange(ExtractCoreFleetRole(RoleName.battleship, out roleGood)); goodSet |= roleGood;
            ships.AddRange(ExtractCoreFleetRole(RoleName.capital, out roleGood));    goodSet |= roleGood;
            goodSet = roleGood;
            return ships;
        }

        public Array<Ship> ExtractCoreFleetRole(RoleName role, out bool fullFilled)
        {
            float wanted    = Ratios.GetWanted(role);
            var ships = ExtractShips(Ships, wanted.LowerBound(1), role, required: false);
            fullFilled      = wanted == 0 || ships.NotEmpty;
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
                ships.AddRange(fleetShips);
            else
                AddShips(fleetShips);

            return ships;
        }

        public Array<Ship> ExtractTroops(int planetAssaultTroopsStrWanted)
        {
            int troopsWanted = 1 + planetAssaultTroopsStrWanted / OwnerEmpire.GetTypicalTroopStrength();
            var ships = ExtractShipsByFeatures(Ships, troopsWanted, 1, 1
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
            Ships.Sort(s =>
            {
                if (s.System?.HostileForcesPresent(OwnerEmpire) ?? false)
                    return s.Position.SqDist(point) + OwnerEmpire.Universum.UniverseSize;

                return s.Position.SqDist(point);
            });
        }

        static void CheckForShipErrors(Array<Ship> ships)
        {
            if (Debugger.IsAttached)
            {
                foreach (var ship in ships)
                {
                    if (ship.Fleet != null)
                        throw new Exception("Fleet should be null here.");

                    int n = ships.CountRef(ship);
                    //if (n > 1) 
                    //    throw new Exception($"Fleet ships contain duplicates({n}): {ship}.");
                }
            }
        }

        private void LaunchTroopsAndAddToShipList(int wantedTroopStrength, Array<Troop> planetTroops)
        {
            foreach (Troop troop in planetTroops.Filter(delegate(Troop t)
            {
                if (t.HostPlanet != null
                    && t.Loyalty != null
                    && t.CanLaunch // save some iterations to find tiles for irrelevant troops
                    && !t.HostPlanet.RecentCombat
                    && !t.HostPlanet.ParentSystem.DangerousForcesPresent(t.Loyalty)) 
                    return true;
                return false;
            }))
            {
                if (InvasionTroopStrength > wantedTroopStrength)
                    break;

                Ship launched = troop.Launch();
                if (launched != null)
                    AddShip(launched);
            }
        }

        public void Clear()
        {
            Ships.Clear();
            OwnerEmpire = null;
        }
    }
}