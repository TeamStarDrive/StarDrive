using System;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public static class EmpireExtensions
    {
        public static Array<Ship> GetTroopShips(this Empire empire, ref float troopStrength)
        {
            Array<Ship> troopShips = new Array<Ship>();
            foreach (Ship ship in empire.GetShips())
            {
                if (ship.shipData.Role != ShipData.RoleName.troop
                    || ship.fleet != null
                    || ship.Mothership != null
                    || ship.AI.HasPriorityOrder)
                    continue;
                troopShips.Add(ship);
                for (int i = 0; i < ship.TroopList.Count; i++)
                    if (ship.TroopList[i].GetOwner() == empire)
                        troopStrength += ship.TroopList[i].Strength;
            }
            return troopShips;
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

    }
}
