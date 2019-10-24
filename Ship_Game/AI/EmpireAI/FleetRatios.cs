using Ship_Game.Ships;
using System;

namespace Ship_Game.AI
{
    public struct FleetRatios
    {
        public float TotalCount     { get; set; }
        public float MinFighters { get; set; }
        public float MinCorvettes { get; set; }
        public float MinFrigates { get; set; }
        public float MinCruisers { get; set; }
        public float MinCapitals { get; set; }
        public float MinTroopShip { get; set; }
        public float MinBombers { get; set; }
        public float MinCarriers { get; set; }
        public float MinSupport { get; set; }

        public Empire OwnerEmpire { get; }

        public FleetRatios(Empire empire)
        {
            OwnerEmpire    = empire;

            TotalCount     = 0;
            MinFighters  = 0;
            MinCorvettes = 0;
            MinFrigates  = 0;
            MinCruisers  = 0;
            MinCapitals  = 0;
            MinTroopShip = 0;
            MinBombers   = 0;
            MinCarriers  = 0;
            MinSupport   = 0;
            SetFleetRatios();

        }

        public void SetFleetRatios()
        {
            //fighters: 1, corvettes: 4, frigates: 8, cruisers: 6, capitals: 1, bombers: 1f, carriers: 1f, support: 1f, troopShip: 1f
            if (OwnerEmpire.canBuildCapitals)
                SetCounts(new[] { 1, 20, 10, 3, 1, 1, 1, 1, 1 });

            else if (OwnerEmpire.canBuildCruisers)
                SetCounts(new[] { 6, 16, 8, 2, 0, 1, 1, 1, 1 });

            else if (OwnerEmpire.canBuildFrigates)
                SetCounts(new[] { 10, 15, 5, 0, 0, 1, 1, 1, 1 });

            else if (OwnerEmpire.canBuildCorvettes)
                SetCounts(new[] { 10, 5, 0, 0, 0, 1, 1, 1, 1 });

            else
                SetCounts(new[] { 20, 0, 0, 0, 0, 1, 1, 1, 1 });
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

            float totalRatio = MinFighters + MinCorvettes + MinFrigates + MinCruisers
                               + MinCapitals;
            TotalCount = totalRatio + MinSupport + MinCarriers + MinBombers + MinTroopShip;
        }


        public int ApplyFighterRatio(float roleCount, float roleUpkeep, float capacity) => ApplyRatio(roleCount, roleUpkeep, capacity, MinFighters);
        public int ApplyRatioCorvettes(float roleCount, float roleUpkeep, float capacity) => ApplyRatio(roleCount, roleUpkeep, capacity, MinCorvettes);
        public int ApplyRatioFrigates(float roleCount, float roleUpkeep, float capacity) => ApplyRatio(roleCount, roleUpkeep, capacity, MinFrigates);
        public int ApplyRatioCruisers(float roleCount, float roleUpkeep, float capacity) => ApplyRatio(roleCount, roleUpkeep, capacity, MinCruisers);
        public int ApplyRatioCapitals(float roleCount, float roleUpkeep, float capacity) => ApplyRatio(roleCount, roleUpkeep, capacity, MinCapitals);
        public int ApplyRatioTroopShip(float roleCount, float roleUpkeep, float capacity) => ApplyRatio(roleCount, roleUpkeep, capacity, MinTroopShip);
        public int ApplyRatioBombers(float roleCount, float roleUpkeep, float capacity) => ApplyRatio(roleCount, roleUpkeep, capacity, MinBombers);
        public int ApplyRatioCarriers(float roleCount, float roleUpkeep, float capacity) => ApplyRatio(roleCount, roleUpkeep, capacity, MinCarriers);
        public int ApplyRatioSupport(float roleCount, float roleUpkeep, float capacity) => ApplyRatio(roleCount, roleUpkeep, capacity, MinSupport);

        private int ApplyRatio(float roleCount, float roleUpkeep, float totalCapacity, float wantedMin)
        {
            if (wantedMin < .01f) return 0;
            float possible = totalCapacity / roleUpkeep;
            float ratio = wantedMin / TotalCount;
            float desired = possible * ratio;
            //get an average of maintainence used for the role.
            //float shipUpkeep = Math.Max(roleUpkeep, .01f) / Math.Max(roleCount, 1);
            //figure the possible number of ships in role based on capacity and upkeep
            //float possible   = totalCapacity / shipUpkeep;



            return (int)desired;
        }
    }

    public struct FleetShips
    {
        public float AccumulatedStrength { get; private set; }
        public Empire OwnerEmpire { get; }
        private FleetRatios Ratios;
        private Array<Ship> Ships;

        public FleetShips(Empire ownerEmpire)
        {
            AccumulatedStrength = 0;
            OwnerEmpire = ownerEmpire;
            Ratios = new FleetRatios(OwnerEmpire);

            Ships = new Array<Ship>();
        }

        public bool AddShip(Ship ship)
        {
            if (!ship.ShipIsGoodForGoals())
                return false;

            if (ship.fleet != null)
            {
                Log.Warning($"FleetRatios: attempting to add a ship already in a fleet '{ship.fleet.Name}'. removing from fleet");
                ship.ClearFleet();
                foreach(var fleet in OwnerEmpire.GetFleetsDict())
                {
                    fleet.Value.RemoveShip(ship);
                }
            }

            if (ship.IsPlatformOrStation
                || !ship.ShipIsGoodForGoals()
                || ship.InCombat
                || ship.fleet != null
                || ship.Mothership != null
                || ship.AI.State == AIState.Scrap
                || ship.AI.State == AIState.Resupply
                || ship.AI.State == AIState.Refit)
                return false;
            Ships.Add(ship);
            return true;
        }

        public Array<Ship> GetFleetByStrength(float strength)
        {
            Array<Ship> ships = new Array<Ship>();
            do
            {
                var gatheredShips = GetBasicFleet();
                if (gatheredShips.IsEmpty)
                    break;
                ships.AddRange(gatheredShips);
            }
            while (AccumulatedStrength < strength);
            return ships;
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

        public Array<Ship> GetInvasionFleet(int bombs, int troops)
        {
            Array<Ship> ships = GetBasicFleet();
            ships.AddRange(GetBombers(bombs));
            ships.AddRange(GetTroops(troops));
            return ships;
        }

        public Array<Ship> GetTroops(int assaultCount) =>
            GetShipsByFeaturesAndRole(Ships, assaultCount, s => s.Carrier.PlanetAssaultCount,
                r=> r.DesignRole == ShipData.RoleName.troop || r.DesignRole == ShipData.RoleName.troopShip);

        public Array<Ship> GetBombers(int bombCount)
        {
            //Array<Ship> ships = new Array<Ship>();
            if (bombCount < 1 || Ratios.MinBombers < 1)
                return new Array<Ship>();
            return GetShipsByFeaturesAndRole(Ships, bombCount, s => s.BombsUseful,
                r => r?.DesignRole == ShipData.RoleName.bomber);
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

        private Array<Ship> GetShipsByFeaturesAndRole(Array<Ship> ships, float wanted, Func<Ship, int> featureCount, Func<Ship, bool> roleFilter)
        {
            var shipSet = new Array<Ship>();
            if (wanted < 1) return shipSet;

            for (int x = ships.Count - 1; x >= 0; x--)
            {
                Ship ship = ships[x];
                if (!roleFilter(ship))
                    continue;
                int count = featureCount(ship);
                if (count <= 0)
                    continue;
                Ships.RemoveSwapLast(ship);
                shipSet.Add(ship);
                AccumulatedStrength += ship.GetStrength();
                wanted -= count;
                if (wanted <= 0)
                    break;
            }

            return shipSet;
        }
    }
}