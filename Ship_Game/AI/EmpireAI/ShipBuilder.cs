using System;
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

        public static float GetModifiedStrength(int shipSize, int numWeaponSlots, float offense, float defense,
                                                ShipData.RoleName role, float velocity)
        {
            float weaponRatio = (float)numWeaponSlots / shipSize;
            float modifiedStrength;
            if (defense > offense && weaponRatio < 0.2f)
                modifiedStrength = offense * 2;
            else
                modifiedStrength = offense + defense;

            float speedModifier = 1f;
            switch (role)
            {
                case ShipData.RoleName.fighter when velocity > 575:
                    speedModifier = 2f;
                    break;
                case ShipData.RoleName.corvette when velocity > 475:
                    speedModifier = 2.2f;
                    break;
                case ShipData.RoleName.frigate when velocity > 375:
                    speedModifier = 2.4f;
                    break;
                case ShipData.RoleName.cruiser when velocity > 250:
                    speedModifier = 2.6f;
                    break;
            }

            return modifiedStrength * speedModifier;
        }
    }
}
