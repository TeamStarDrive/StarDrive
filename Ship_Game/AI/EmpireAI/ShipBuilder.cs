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


        public static string PickFromCandidates(ShipData.RoleName role, Empire empire, int maxSize = 0, ShipModuleType targetModule = ShipModuleType.Dummy)
        {
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.AiPickShipsByStrength)
                return PickFromCandidatesByStrength(role, empire, maxSize, targetModule);

            return PickFromCandidatesByTechsNeeded(role, empire, maxSize, targetModule);
        }

        private static string PickFromCandidatesByTechsNeeded(ShipData.RoleName role, Empire empire, int maxSize, ShipModuleType targetModule)
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

        private static string PickFromCandidatesByStrength(ShipData.RoleName role, Empire empire, int maxSize, ShipModuleType targetModule)
        {
            var potentialShips = new Array<Ship>();
            bool specificModuleWanted = targetModule != ShipModuleType.Dummy;
            string name = "";
            float bestModuleRatio = 0;
            float highestStrength = 0;

            foreach (string shipsWeCanBuild in empire.ShipsWeCanBuild)
            {
                Ship ship;
                if ((ship = ResourceManager.GetShipTemplate(shipsWeCanBuild, false)) == null)
                    continue;

                if (role != ship.DesignRole)
                    continue;

                if (maxSize > 0 && ship.Size > maxSize)
                    continue;

                potentialShips.Add(ship);
                int shipSize = ship.shipData.ModuleSlots.Length;
                if (specificModuleWanted)
                {
                    bestModuleRatio = Math.Max(bestModuleRatio, ship.PercentageOfShipByModules(targetModule));
                    highestStrength = Math.Max(highestStrength, ship.BaseStrength / shipSize);
                }
                else
                    highestStrength = Math.Max(highestStrength, ship.BaseStrength / shipSize);
            }

            if (potentialShips.Count <= 0)
                return name;

            bestModuleRatio *= 0.8f;
            MinMaxStrength levelAdjust = new MinMaxStrength(highestStrength);
            Ship[] bestShips = potentialShips.FilterBy(ships =>
            {
                float shipStrength = ships.BaseStrength / ships.shipData.ModuleSlots.Length;
                if (specificModuleWanted)
                    return shipStrength >= levelAdjust.MinStrength
                           && shipStrength <= levelAdjust.MaxStrength
                           && ships.PercentageOfShipByModules(targetModule) >= bestModuleRatio;

                return shipStrength >= levelAdjust.MinStrength
                       && shipStrength <= levelAdjust.MaxStrength;
            });

            if (bestShips.Length == 0)
                return name;

            Ship pickedShip = RandomMath.RandItem(bestShips);
            name = pickedShip.Name;

            if (Empire.Universe?.showdebugwindow ?? false)
            {
                Log.Info($"Sorted Ship List ({bestShips.Length})");
                int i = 0;
                foreach (Ship loggedShip in bestShips)
                {
                    i++;
                    Log.Info(ConsoleColor.Magenta, $"{i}) Name: {loggedShip.Name}, Stength: {loggedShip.BaseStrength / loggedShip.shipData.ModuleSlots.Length}");
                }
                Log.Info(ConsoleColor.Magenta, $"Chosen Role: {pickedShip.DesignRole}  Chosen Hull: {pickedShip.shipData.Hull}  " +
                                    $"Strength: {pickedShip.BaseStrength / pickedShip.shipData.ModuleSlots.Length} " +
                                    $"Name: {pickedShip.Name} . Min STR: {levelAdjust.MinStrength}, Max STR: {levelAdjust.MaxStrength}.");
            }
            return name;
        }

        private struct MinMaxStrength
        {
            public readonly float MinStrength;
            public readonly float MaxStrength;

            public MinMaxStrength(float inputStrength)
            {
                float maxStrength = 0;
                float minStrength = 0;
                switch (Empire.Universe.GameDifficulty)
                {
                    case UniverseData.GameDifficulty.Easy:
                        maxStrength = inputStrength * 0.6f;
                        break;
                    case UniverseData.GameDifficulty.Normal:
                        minStrength = inputStrength * 0.5f;
                        maxStrength = inputStrength;
                        break;
                    case UniverseData.GameDifficulty.Hard:
                        minStrength = inputStrength * 0.7f;
                        maxStrength = inputStrength;
                        break;
                    case UniverseData.GameDifficulty.Brutal:
                        minStrength = inputStrength * 0.9f;
                        maxStrength = inputStrength;
                        break;
                }
                MinStrength = minStrength;
                MaxStrength = maxStrength;
            }
        }

        public static string PickShipToRefit(Ship oldShip, Empire empire)
        {
            var potentialShips = new Array<Ship>();
            float highestStrength = 0;
            string name = "";

            foreach (string shipsWeCanBuild in empire.ShipsWeCanBuild)
            {
                Ship ship;
                if ((ship = ResourceManager.GetShipTemplate(shipsWeCanBuild, false)) == null)
                    continue;

                if (oldShip.shipData.Hull != ship.shipData.Hull
                    || oldShip.BaseStrength >= ship.BaseStrength
                    || oldShip.Name == ship.Name)
                    continue;

                potentialShips.Add(ship);
                highestStrength = Math.Max(highestStrength, ship.shipData.BaseStrength);
            }
            if (potentialShips.Count <= 0)
                return name;

            Ship pickedShip = RandomMath.RandItem(potentialShips);
            name = pickedShip.Name;
            Log.Info(ConsoleColor.DarkCyan, $"{empire.Name} Refit: {oldShip.Name}, Stength: {oldShip.BaseStrength} refit to --> {name}, Strength: {pickedShip.BaseStrength}");
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

        public static bool IsDynamicLaunch(string compare)
        {
            if (Enum.TryParse(compare, out DynamicHangarLaunch result))
                return result == DynamicHangarLaunch.DynamicLaunch;

            return false;
        }
    }
    public enum DynamicHangarLaunch
    {
        DynamicLaunch
    }
}
