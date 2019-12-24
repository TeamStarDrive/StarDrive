using Ship_Game.Ships;
using System;
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

    public struct FleetShips
    {
        public float AccumulatedStrength { get; private set; }
        private readonly Empire OwnerEmpire;
        private readonly FleetRatios Ratios;
        private Array<Ship> Ships;
        public int TroopAsaaultCount { get; private set; }
        public float TroopAssaultStrength { get; private set; }
        public int BombSecsAvailable { get; private set; }

        public FleetShips(Empire ownerEmpire)
        {
            AccumulatedStrength  = 0;
            OwnerEmpire          = ownerEmpire;
            Ratios               = new FleetRatios(OwnerEmpire);
            TroopAsaaultCount    = 0;
            TroopAssaultStrength = 0;
            Ships                = new Array<Ship>();
            BombSecsAvailable    = 0;
        }

        public FleetShips(Empire ownerEmpire, Array<Ship> ships)
        {
            AccumulatedStrength    = ships.Sum(s=> s.GetStrength());
            OwnerEmpire            = ownerEmpire;
            Ratios                 = new FleetRatios(OwnerEmpire);
            Ships                  = new Array<Ship>();
            TroopAsaaultCount      = 0;
            TroopAssaultStrength   = 0;
            BombSecsAvailable      = 0;
            foreach (var ship in ships)
            {
                AddShip(ship);
            }
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
                || ship.ShipIsGoodForGoals()== false)
                return false;

            Ships.Add(ship);
            TroopAsaaultCount               += ship.Carrier.PlanetAssaultCount;
            TroopAssaultStrength            += ship.Carrier.PlanetAssaultStrength;
            BombSecsAvailable               += ship.BombsGoodFor60Secs;

            return true;
        }

        public int GetFleetByStrength(float strength, out Array<Ship> ships)
        {
            float accumulatedStrength = 0;
            int completeFleets = 0;
            ships = new Array<Ship>();
            do
            {
                var gatheredShips = GetBasicFleet();
                if (gatheredShips.IsEmpty)
                    break;
                if (gatheredShips.Count > Ratios.MinCombatFleet * 0.75f)
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

        public Array<Ship> GetInvasionFleet(float strength, int bombs, int troops)
        {
            Array<Ship> ships = GetCombatFleet(strength);
            if (ships.IsEmpty) return ships;


            return ships;
        }

        public Array<Ship> GetCombatFleet(float strength)
        {
            Array<Ship> ships = new Array<Ship>();
            int fleetCount = GetFleetByStrength(strength, out Array<Ship> fleetShips);
            if (fleetCount > 0)
                ships.AddRange(fleetShips);
            return ships;
        }

        public Array<Ship> GetTroops(int assaultCount) =>
            GetShipsByFeaturesAndRole(Ships, assaultCount, s => s.Carrier.PlanetAssaultCount);

        public Array<Ship> GetBombers(int bombSecsWanted)
        {
            //Array<Ship> ships = new Array<Ship>();
            if (bombSecsWanted < 1 || Ratios.MinBombers < 1)
                return new Array<Ship>();
            return GetShipsByFeaturesAndRole(Ships, bombSecsWanted,
                s => s.BombsGoodFor60Secs);
                
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
        /// Wanted is the number of featureCount total wanted.
        /// filter by the role and the want is the count of features wanted. 
        /// </summary>
        private Array<Ship> GetShipsByFeaturesAndRole(Array<Ship> ships, float wanted
            , Func<Ship, int> featureCount)
        {
            var shipSet = new Array<Ship>();
            if (wanted < 1) return shipSet;

            for (int x = ships.Count - 1; x >= 0; x--)
            {
                Ship ship = ships[x];
                int count = featureCount(ship);
                if (count > 0)
                {
                    Ships.RemoveSwapLast(ship);
                    shipSet.Add(ship);
                    AccumulatedStrength += ship.GetStrength();
                    wanted -= count;
                    if (wanted <= 0)
                        break;
                }
            }

            return shipSet;
        }

        /// <summary>
        /// Warning these counts must be verified before calling.
        /// 
        /// </summary>
        public Fleet CreateInvasionFleet(float minStrength, int bombingSecs, int wantedTroopStrength
        ,Array<Troop> planetTroops)
        {
            Array<Ship> ships = GetCombatFleet(minStrength);
            if (ships.IsEmpty)
                return null;

            Fleet newFleet = new Fleet
            {
                Owner = OwnerEmpire,
                Name = "Invasion Fleet"
            };

            foreach (Troop troop in planetTroops.Filter(t=> t.HostPlanet != null &&
                                                            !t.HostPlanet.RecentCombat))
            {
                if (TroopAssaultStrength > wantedTroopStrength)
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
            ships.AddRange(GetTroops(wantedTroopStrength));
            ships.AddRange(GetBombers(bombingSecs));
            newFleet.AddShips(ships);
            
            return newFleet;
        }
    }
}