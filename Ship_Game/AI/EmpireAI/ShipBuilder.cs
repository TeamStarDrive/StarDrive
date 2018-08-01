using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    public static class ShipBuilder
    {
        public static void PickRoles(ref float numShips, float desiredShips, ShipData.RoleName role, Map<ShipData.RoleName, float>
            rolesPicked)
        {
            if (numShips >= desiredShips)
                return;
            rolesPicked.Add(role, numShips / desiredShips);
        }

        public static string PickFromCandidates(ShipData.RoleName role, Empire empire, ShipModuleType targetModule = ShipModuleType.Dummy, int maxSize = 0)
        {
            var potentialShips = new Array<Ship>();
            bool efficiency = targetModule != ShipModuleType.Dummy;
            string name = "";
            Ship ship;
            int maxTech = 0;
            float bestEfficiency = 0;
            foreach (string shipsWeCanBuild in empire.ShipsWeCanBuild)
            {
                if ((ship = ResourceManager.GetShipTemplate(shipsWeCanBuild, false)) == null) continue;

                if (role != ship.DesignRole)
                    continue;
                if (maxSize > 0 && ship.Size > maxSize)
                    continue;

                maxTech = Math.Max(maxTech, ship.shipData.TechsNeeded.Count);

                potentialShips.Add(ship);
                if (efficiency)
                    bestEfficiency = Math.Max(bestEfficiency, ship.PercentageOfShipByModules(targetModule));

            }
            float nearmax = maxTech * .80f;
            bestEfficiency *= .80f;
            if (potentialShips.Count <= 0)
                return name;

            Ship[] bestShips = potentialShips.FilterBy(ships =>
            {
                if (efficiency)
                    return ships.PercentageOfShipByModules(targetModule) >= bestEfficiency;
                return ships.shipData.TechsNeeded.Count >= nearmax;
            });

            if (bestShips.Length == 0)
                return name;

            ship = RandomMath.RandItem(bestShips);
            name = ship.Name;
            if (Empire.Universe?.showdebugwindow ?? false)
                Log.Info($"Chosen Role: {ship.DesignRole}  Chosen Hull: {ship.shipData.Hull}  " +
                         $"Strength: {ship.BaseStrength} Name: {ship.Name} ");

            return name;
        }
    }
}
