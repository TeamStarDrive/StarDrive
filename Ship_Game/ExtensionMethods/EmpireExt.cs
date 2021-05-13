using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game
{
    public static class GamePlayExtensions
    {
        /// <summary>
        /// Empire extension for getting available troops ships
        /// </summary>
        public static Array<Ship> GetAvailableTroopShips(this Empire empire, out int troopsInFleets)
        {
            var ships      = new Array<Ship>();
            troopsInFleets = 0;
            var collection = empire.OwnedShips;
            for (int x = 0; x < collection.Count; x++)
            {
                Ship ship = collection[x];
                if (!ship.Active
                    || ship.shipData.Role != ShipData.RoleName.troop
                    || ship.IsHangarShip
                    || ship.IsHomeDefense
                    || ship.AI.State == AIState.Scrap
                    || ship.AI.State == AIState.RebaseToShip
                    || ship.AI.HasPriorityOrder)
                {
                    continue;
                }

                if (ship.fleet != null)
                    troopsInFleets += ship.GetOurTroops().Count;
                else
                    ships.Add(ship);
            }

            return ships;
        }
        /// <summary>
        /// Generic empire ship filter. Use this to help remember to move filter needs to extensions when needed.
        /// </summary>
        public static Array<Ship> GetShips(this Empire empire, Predicate<Ship> filter)
        {
            Array<Ship> ships = new Array<Ship>();
            foreach (Ship ship in empire.OwnedShips)
            {
                if (!filter(ship)) continue;
                
                ships.Add(ship);
            }
            return ships;
        }

        public static float CalculateShipsValue(this Array<Ship> ships, Func<Ship,float> calculator)
        {
            float value =0;
            foreach (Ship ship in ships)            
                value += calculator(ship);
            return value;
        }




        public static Array<Troop> GetTroopUnits(this Empire empire, ref float TotalTroopStrength)
        {
            Array<Troop> troops = new Array<Troop>();
            foreach (Planet p in empire.GetPlanets())
                for (int i = 0; i < p.TroopsHere.Count; i++)
                    if (p.TroopsHere[i].Strength > 0 && p.TroopsHere[i].Loyalty == empire)
                    {
                        Troop troop = p.TroopsHere[i];
                        troops.Add(troop);
                        TotalTroopStrength += troop.Strength;
                    }
            return troops;
        }
        public static bool AddModuleTypeToList(this ShipModule module, ShipModuleType moduleType, bool isTrue = true, Array<ShipModule> addToList = null )
        {
            if (module.ModuleType != moduleType || !isTrue)
                return false;
            addToList?.Add(module);
            return true;           
        }

    }
}
