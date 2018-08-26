using System;
using Ship_Game.Ships;
using Microsoft.Xna.Framework.Graphics;

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


        public static string PickFromCandidates(ShipData.RoleName role, Empire empire, int maxSize = 0, 
                      ShipModuleType targetModule = ShipModuleType.Dummy, ShipData.Category shipCategory = ShipData.Category.Unclassified)
        {
            // The AI will pick ships to build based on their Strength and game difficulty level 
            // instead of techs needed. This allows it to choose the toughest ships to build. This is notmalized by ship total slots
            // so ships with more slots of the same role wont get priority (bigger ships also cost more to build and maintain.
            return PickFromCandidatesByStrength(role, empire, maxSize, targetModule, shipCategory);
            //return PickFromCandidatesByTechsNeeded(role, empire, maxSize, targetModule);
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
            if (Empire.Universe?.showdebugwindow == true)
                Log.Info($"Chosen Role: {ship.DesignRole}  Chosen Hull: {ship.shipData.Hull}  " +
                         $"Strength: {ship.BaseStrength} Name: {ship.Name} ");

            return name;
        }

        private static string PickFromCandidatesByStrength(ShipData.RoleName role, Empire empire, int maxSize, 
                                                           ShipModuleType targetModule,
                                                           ShipData.Category shipCategory)
        {
            var potentialShips = new Array<Ship>();
            bool specificModuleWanted = targetModule != ShipModuleType.Dummy;
            float bestModuleRatio = 0.8f;
            float maxStrength = 0;
            
            // @todo This is also a .FilterBy
            foreach (string shipsWeCanBuild in empire.ShipsWeCanBuild)
            {
                Ship ship;
                if ((ship = ResourceManager.GetShipTemplate(shipsWeCanBuild, false)) == null)
                    continue;

                if (role != ship.DesignRole)
                    continue;

                if (maxSize > 0 && ship.Size > maxSize)
                    continue;

                if (shipCategory != ShipData.Category.Unclassified && shipCategory != ship.shipData.ShipCategory)
                    continue;

                potentialShips.Add(ship);
                int shipSize = ship.shipData.ModuleSlots.Length;
                maxStrength = Math.Max(maxStrength, ship.BaseStrength / shipSize);
                if (specificModuleWanted)
                    bestModuleRatio = Math.Max(bestModuleRatio, ship.PercentageOfShipByModules(targetModule));
            }

            if (potentialShips.Count <= 0)
                return "";

            var levelAdjust = new MinMaxStrength(maxStrength, empire);
            Ship[] bestShips = potentialShips.FilterBy(ship =>
            {
                float shipStrength = ship.BaseStrength / ship.shipData.ModuleSlots.Length;
                //Log.Warning($"blabla");
                if (!levelAdjust.InRange(shipStrength))
                    return false; // ignore ships not in adjusted level range

                if (specificModuleWanted)
                    return ship.PercentageOfShipByModules(targetModule) >= bestModuleRatio;

                return true;
            });

            if (bestShips.Length == 0)
            {
                Log.Warning($"No best ships in level range: [{levelAdjust.Min}..{levelAdjust.Max}]");
                return "";
            }

            Ship pickedShip = RandomMath.RandItem(bestShips);

            if (Empire.Universe?.showdebugwindow == true)
            {
                Log.Info($"Sorted Ship List ({bestShips.Length})");
                int i = 0;
                foreach (Ship loggedShip in bestShips)
                {
                    i++;
                    Log.Info(ConsoleColor.Magenta, 
                        $"{i}) Name: {loggedShip.Name}, Strength: {loggedShip.BaseStrength / loggedShip.shipData.ModuleSlots.Length}");
                }
                Log.Info(ConsoleColor.Magenta, 
                    $"Chosen Role: {pickedShip.DesignRole}  Chosen Hull: {pickedShip.shipData.Hull}  " +
                    $"Strength: {pickedShip.BaseStrength / pickedShip.shipData.ModuleSlots.Length} " +
                    $"Name: {pickedShip.Name} . Min STR: {levelAdjust.Min}, Max STR: {levelAdjust.Max}.");
            }
            return pickedShip.Name;
        }

        private struct MinMaxStrength
        {
            public readonly float Min;
            public readonly float Max;

            public MinMaxStrength(float inputStrength, Empire empire)
            {
                if (empire.isPlayer) // always select the best for player
                {
                    Min = inputStrength * 0.9f;
                    Max = inputStrength;
                }
                else // for AI, set the range based on difficulty
                {
                    switch (CurrentGame.Difficulty)
                    {
                        case UniverseData.GameDifficulty.Easy:
                            Min = inputStrength * 0.3f;
                            Max = inputStrength * 0.8f;
                            break;
                        case UniverseData.GameDifficulty.Normal:
                            Min = inputStrength * 0.7f;
                            Max = inputStrength;
                            break;
                        case UniverseData.GameDifficulty.Hard:
                            Min = inputStrength * 0.8f;
                            Max = inputStrength;
                            break;
                        case UniverseData.GameDifficulty.Brutal:
                        default:
                            Min = inputStrength * 0.9f;
                            Max = inputStrength;
                            break;
                    }
                }
            }

            public bool InRange(float strength) => Min <= strength && strength <= Max;
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
            ShipData.RoleName role, float rotationSpeed)
        {
            float weaponRatio = (float)numWeaponSlots / shipSize;
            float modifiedStrength;
            if (defense > offense && weaponRatio < 0.2f)
                modifiedStrength = offense * 2;
            else
                modifiedStrength = offense + defense;

            modifiedStrength += modifiedStrength * rotationSpeed / 100f;
            return modifiedStrength;
        }

        public static Color GetHangarTextColor(string shipName)
        {
            DynamicHangarOptions dynamicHangarType = GetDynamicHangarOptions(shipName);
            switch (dynamicHangarType)
            {
                case DynamicHangarOptions.DynamicLaunch:
                    return Color.Gold;
                case DynamicHangarOptions.DynamicFighter:
                    return Color.Cyan;
                case DynamicHangarOptions.DynamicBomber:
                    return Color.OrangeRed;
                default:
                    return Color.White;
            }
        }

        public static DynamicHangarOptions GetDynamicHangarOptions(string compare)
        {
            return Enum.TryParse(compare, out DynamicHangarOptions result) ? result : DynamicHangarOptions.Static;
        }

        public static bool IsDynamicHangar(string compare)
        {
            if (Enum.TryParse(compare, out DynamicHangarOptions result))
                return result != DynamicHangarOptions.Static;

            return false;
        }
    }
    public enum DynamicHangarOptions
    {
        Static,
        DynamicLaunch,
        DynamicFighter,
        DynamicBomber
    }
}
