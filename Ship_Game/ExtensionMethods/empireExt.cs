using System;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public static class GamePlayExtensions
    {
        public static Array<Ship> GetTroopShips(this Empire empire)
        {
            Array<Ship> ships = empire.GetShips(ship =>
                ship.shipData.Role == ShipData.RoleName.troop 
                && ship.fleet == null 
                && ship.Mothership == null 
                && !ship.AI.HasPriorityOrder
                && ship.AI.State != AI.AIState.Scrap
            );
            return ships;
        }

        public static Array<Ship> GetShips(this Empire empire, Predicate<Ship> filter)
        {
            Array<Ship> ships = new Array<Ship>();
            foreach (Ship ship in empire.GetShips())
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
                    if (p.TroopsHere[i].Strength > 0 && p.TroopsHere[i].GetOwner() == empire)
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
