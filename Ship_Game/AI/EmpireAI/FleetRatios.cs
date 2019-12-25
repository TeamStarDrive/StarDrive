using Ship_Game.Ships;
using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Ship_Game.AI
{
    public struct FleetRatios
    {
        public float TotalCount { get; set; }
        public float MinFighters { get; set; }
        public float MinCorvettes { get; set; }
        public float MinFrigates { get; set; }
        public float MinCruisers { get; set; }
        public float MinCapitals { get; set; }
        public float MinTroopShip { get; set; }
        public float MinBombers { get; set; }
        public float MinCarriers { get; set; }
        public float MinSupport { get; set; }
        public int MinCombatFleet { get; set; }

        public Empire OwnerEmpire { get; }

        public FleetRatios(Empire empire)
        {
            OwnerEmpire    = empire;

            TotalCount     = 0;
            MinFighters    = 0;
            MinCorvettes   = 0;
            MinFrigates    = 0;
            MinCruisers    = 0;
            MinCapitals    = 0;
            MinTroopShip   = 0;
            MinBombers     = 0;
            MinCarriers    = 0;
            MinSupport     = 0;
            MinCombatFleet = 0;
            SetFleetRatios();


        }

        public void SetFleetRatios()
        {
            //fighters, corvettes, frigate, cruisers, capitals, troopShip,bombers,carriers,support
            if (OwnerEmpire.canBuildCapitals)
                SetCounts(new[] { 6, 3, 2, 2, 1, 1, 2, 3, 2 });

            else if (OwnerEmpire.canBuildCruisers)
                SetCounts(new[] { 6, 3, 2, 1, 0, 1, 2, 3, 2 });

            else if (OwnerEmpire.canBuildFrigates)
                SetCounts(new[] { 6, 3, 1, 0, 0, 1, 2, 3, 1 });

            else if (OwnerEmpire.canBuildCorvettes)
                SetCounts(new[] { 6, 3, 0, 0, 0, 1, 2, 1, 1 });

            else
                SetCounts(new[] { 6, 0, 0, 0, 0, 1, 2, 1, 1 });
        }

        private void SetCounts(int[] counts)
        {
            MinFighters  = counts[0];
            MinCorvettes = counts[1];
            MinFrigates  = counts[2];
            MinCruisers  = counts[3];
            MinCapitals  = counts[4];
            MinTroopShip = counts[5];
            MinBombers   = counts[6];
            MinCarriers  = counts[7];
            MinSupport   = counts[8];

            if (!OwnerEmpire.canBuildTroopShips)
                MinTroopShip = 0;

            if (!OwnerEmpire.canBuildBombers)
                MinBombers = 0;

            if (OwnerEmpire.canBuildCarriers)
                MinFighters = 0;
            else
                MinCarriers = 0;

            if (!OwnerEmpire.canBuildSupportShips)
                MinSupport = 0;

            MinCombatFleet = (int)(MinFighters + MinCorvettes + MinFrigates + MinCruisers
                               + MinCapitals + MinSupport + MinCarriers);
            TotalCount = MinCombatFleet + MinBombers + MinTroopShip;

        }
    }
    /// <summary>
    /// used to classify a group of ships into fleets according to the fleet ratios.
    /// usage: create class and give it ships. it will talley their fleet characteristics
    /// and provide methods for extracting ship sets used for fleets.
    /// in general before extracting make sure that the tallies at least match what is wanted. 
    /// </summary>
    public class FleetShips
    {
        public float AccumulatedStrength { get; private set; }
        private readonly Empire OwnerEmpire;
        private readonly FleetRatios Ratios;
        private Array<Ship> Ships = new Array<Ship>();
        public float WantedFleetCompletePercentage = 0.25f;
        public int InvasionTroops { get; private set; }
        public float InvasionTroopStrength { get; private set; }
        public int BombSecsAvailable { get; private set; }

        public FleetShips(Empire ownerEmpire)
        {
            OwnerEmpire           = ownerEmpire;
            Ratios                = new FleetRatios(OwnerEmpire);
        }

        public FleetShips(Empire ownerEmpire, Array<Ship> ships)
        {
            OwnerEmpire             = ownerEmpire;
            Ratios                  = new FleetRatios(OwnerEmpire);

            foreach (var ship in ships) AddShip(ship);
        }

        public void AddShips(Array<Ship> ships)
        {
            for (int x = 0; x < ships.Count; x++)
            {
                var ship = ships[x];
                AddShip(ship);
            }
        }
        public bool AddShip(Ship ship)
        {
            if (!ship.ShipIsGoodForGoals())
                return false;

            if (ship.fleet != null)
            {
                Log.Error($"FleetRatios: attempting to add a ship already in a fleet '{ship.fleet.Name}'. removing from fleet");
                ship.ClearFleet();
                foreach(var fleet in OwnerEmpire.GetFleetsDict())
                {
                    fleet.Value.RemoveShip(ship);
                }
            }

            if (ship.IsPlatformOrStation
                || ship.AI.BadGuysNear
                || ship.Inhibited
                || ship.engineState         == Ship.MoveState.Warp
                || ship.fleet               != null
                || ship.Mothership          != null
                || ship.AI.State            == AIState.Scrap
                || ship.AI.State            == AIState.Resupply
                || ship.AI.State            == AIState.Refit
                || ship.fleet               != null
                || ship.ShipIsGoodForGoals()== false)
                return false;

            AccumulatedStrength += ship.GetStrength();
            Ships.AddUnique(ship);

            if (ShipData.ShipRoleToRoleType(ship.DesignRole) == ShipData.RoleType.Troop)
            {
                InvasionTroops += ship.Carrier.PlanetAssaultCount;
                InvasionTroopStrength += ship.Carrier.PlanetAssaultStrength;
            }
            if (ship.DesignRole == ShipData.RoleName.bomber)
                BombSecsAvailable += ship.BombsGoodFor60Secs;

            return true;
        }

        public int GatherSetsOfFleetShipsUpToStrength(float strength, float setCompletePercent,
                                                      out Array<Ship> ships)
        {
            float accumulatedStrength;
            int completeFleets = 0;
            ships = new Array<Ship>();
            do
            {
                var gatheredShips = GetBasicFleet();
                if (gatheredShips.IsEmpty)
                    break;
                if (gatheredShips.Count >= Ratios.MinCombatFleet * setCompletePercent)
                    completeFleets++;
                accumulatedStrength = gatheredShips.Sum(s=> s.GetStrength());
                ships.AddRange(gatheredShips);
            }
            while (accumulatedStrength < strength);
            return completeFleets;
        }

        public Array<Ship> GetBasicFleet()
        {
            var ships = new Array<Ship>();
            ships.AddRange(GetShipByCounts(Ships, Ratios.MinFighters, ShipData.RoleName.fighter));
            ships.AddRange(GetShipByCounts(Ships, Ratios.MinCorvettes, ShipData.RoleName.corvette));
            ships.AddRange(GetShipByCounts(Ships, Ratios.MinFrigates, ShipData.RoleName.frigate));
            ships.AddRange(GetShipByCounts(Ships, Ratios.MinCruisers, ShipData.RoleName.cruiser));
            ships.AddRange(GetShipByCounts(Ships, Ratios.MinCapitals, ShipData.RoleName.capital));
            ships.AddRange(GetShipByCounts(Ships, Ratios.MinCarriers, ShipData.RoleName.carrier));
            ships.AddRange(GetShipByCounts(Ships, Ratios.MinSupport, ShipData.RoleName.support));
            return ships;
        }

        public Array<Ship> GatherSetsOfCombatShips(float strength, float setCompletePercentage)
        {
            Array<Ship> ships = new Array<Ship>();
            int fleetCount = GatherSetsOfFleetShipsUpToStrength(strength, setCompletePercentage,
                                                                out Array<Ship> fleetShips);
            ships.AddRange(fleetShips);
            if (fleetCount < 1)
            {
                GatherSetsOfFleetShipsUpToStrength(strength, setCompletePercentage,
                    out Array<Ship> extraFleetShips);
                ships.AddRange(extraFleetShips);
            }
            
            return ships;
        }

        public Array<Ship> GetTroops(int planetAssaultTroopsWanted) =>
            GetShipsByFeatures(Ships, planetAssaultTroopsWanted, 1, s =>
            {
                if (ShipData.ShipRoleToRoleType(s.DesignRole) == ShipData.RoleType.Troop)
                    return s.Carrier.PlanetAssaultCount;
                return 0;
            });

        public Array<Ship> GetBombers(int bombSecsWanted)
        {
            if (bombSecsWanted < 1 || Ratios.MinBombers < 1)
                return new Array<Ship>();
            return GetShipsByFeatures(Ships, bombSecsWanted, 1,
                s =>
                {
                    if (s.DesignRole == ShipData.RoleName.bomber)
                        return s.BombsGoodFor60Secs;
                    return 0;
                });
        }

        private Array<Ship> GetShipByCounts(Array<Ship> ships, float wanted, ShipData.RoleName role)
            => GetShipByCounts(ships, wanted, s => s.DesignRole == role);

        private Array<Ship> GetShipByCounts(Array<Ship> ships, float wanted, Func<Ship,bool> func)
        {
            var shipSet = new Array<Ship>();
            if (wanted < 1) return shipSet;

            for (int x = ships.Count - 1; x >= 0; x--)
            {
                Ship ship = ships[x];
                if (!func(ship))
                    continue;

                shipSet.Add(ship);
                Ships.RemoveSwapLast(ship);
                AccumulatedStrength += ship.GetStrength();
                if (shipSet.Count >= wanted)
                    break;
            }

            return shipSet;
        }

        /// <summary>
        /// returns ships with wanted feature up to the count of the wanted feature.
        /// ex. I want 10 bombs. give me as many ships as it takes to get 10 bombs.
        /// </summary>
        /// <param name="ships">Ships to be processed</param>
        /// /// <param name="totalFeatureWanted">max of total features found on all matching ships</param>
        /// <param name="minWantedFeature">min count of feature the ship should have</param>
        /// <param name="shipFeatureCount">Number of times ship matches the feature.
        /// like ship.BombsGoodFor60Secs</param>
        /// <returns></returns>
        private Array<Ship> GetShipsByFeatures(Array<Ship> ships, int totalFeatureWanted, float minWantedFeature
            , Func<Ship, int> shipFeatureCount)
        {
            var shipSet = new Array<Ship>();
            if (minWantedFeature < 1) return shipSet;

            for (int x = ships.Count - 1; x >= 0; x--)
            {
                if (totalFeatureWanted <= 0)
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
/// Returns a ship set with wanted characteristics. 
/// </summary>
/// <param name="minStrength">Combat strength of fleet ships</param>
/// <param name="bombingSecs">Time fleet should be able to bomb</param>
/// <param name="wantedTroopStrength">Troop strength to invade with</param>
/// <param name="planetTroops">Troops still on planets</param>
/// <returns></returns>
        public Array<Ship> CollectShipSet(float minStrength, int bombingSecs,
                                         int wantedTroopStrength, Array<Troop> planetTroops)
        {
            Array<Ship> ships = GatherSetsOfCombatShips(minStrength, WantedFleetCompletePercentage);
            if (ships.IsEmpty)
                return new Array<Ship>();

            LaunchTroopsAndAddToShipList(wantedTroopStrength, planetTroops);
            ships.AddRange(GetTroops(wantedTroopStrength));
            ships.AddRange(GetBombers(bombingSecs));

            if (Debugger.IsAttached)
                foreach (var ship in ships)
                {
                    if (ship.fleet != null)
                    {
                        throw new Exception("Fleet should be null here.");
                        break;
                    }

                    int dupes = 0;
                    foreach (var dupe in ships)
                    {
                        if (dupe == ship)
                            dupes++;
                    }

                    if (dupes > 1)
                        throw new Exception("Fleet should be null here.");
                }

            return ships;
        }

        private void LaunchTroopsAndAddToShipList(int wantedTroopStrength, Array<Troop> planetTroops)
        {
            foreach (Troop troop in planetTroops.Filter(t => t.HostPlanet != null &&
                                                             !t.HostPlanet.RecentCombat))
            {
                if (InvasionTroopStrength > wantedTroopStrength)
                    break;

                if (troop.Loyalty == null || !troop.CanMove)
                    continue;
                Ship launched = troop.Launch();
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