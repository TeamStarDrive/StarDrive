using System;
using Ship_Game.Ships;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.AI
{
    public static class ShipBuilder  // Created by Fat Bastard to support ship picking for build
    {
        public const int OrbitalsLimit  = 27; // FB - Maximum of 27 stations or platforms (or shipyards)
        public const int ShipYardsLimit = 2; // FB - Maximum of 2 shipyards

        public static Ship PickFromCandidates(ShipData.RoleName role, Empire empire, int maxSize = 0,
                      ShipModuleType targetModule = ShipModuleType.Dummy,
                      ShipData.HangarOptions designation = ShipData.HangarOptions.General,
                      bool normalizedStrength = true)
        {
            // The AI will pick ships to build based on their Strength and game difficulty level.
            // This allows it to choose the toughest ships to build. This is normalized by ship total slots
            // so ships with more slots of the same role wont get priority (bigger ships also cost more to build and maintain.
            return PickFromCandidatesByStrength(role, empire, maxSize, targetModule, designation, normalizedStrength);
        }

        private struct MinMaxStrength
        {
            private readonly float Min;
            private readonly float Max;

            public MinMaxStrength(float maxStrength, Empire empire)
            {
                Min = empire.DifficultyModifiers.ShipBuildStrMin * maxStrength;
                Max = empire.DifficultyModifiers.ShipBuildStrMax * maxStrength;
            }

            public bool InRange(float strength) => strength.InRange(Min, Max);

            public override string ToString() => $"[{Min.String(2)} .. {Max.String(2)}]";
        }

        private static void Debug(string message)
        {
            Log.DebugInfo(ConsoleColor.Blue, message);
        }

        private static Array<Ship> ShipsWeCanBuild(Empire empire)
        {
            var ships = new Array<Ship>(empire.ShipsWeCanBuild.Count);
            foreach (string shipWeCanBuild in empire.ShipsWeCanBuild)
            {
                if (ResourceManager.GetShipTemplate(shipWeCanBuild, out Ship template))
                    ships.Add(template);
            }
            return ships;
        }

        // Pick the strongest ship to build with a cost limit and a role
        public static Ship PickCostEffectiveShipToBuild(ShipData.RoleName role, Empire empire, float maxCost, float maintBudget)
        {
            Ship[] potentialShips = ShipsWeCanBuild(empire).Filter(
                ship => ship.DesignRole == role && ship.GetCost(empire).LessOrEqual(maxCost) 
                                                && ship.GetMaintCost(empire).Less(maintBudget)
                                                && !ship.shipData.IsShipyard
                                                && !ship.IsSubspaceProjector);

            if (potentialShips.Length == 0)
                return null;

            Ship best = potentialShips.FindMax(orb => orb.NormalizedStrength);
            return best;
        }
        
        static Ship PickFromCandidatesByStrength(ShipData.RoleName role, Empire empire, int maxSize, 
                                                           ShipModuleType targetModule,
                                                           ShipData.HangarOptions designation,
                                                           bool normalizedStrength = true)
        {
            Ship[] potentialShips = ShipsWeCanBuild(empire).Filter(
                ship => ship.DesignRole == role
                && (maxSize <= 0 || ship.SurfaceArea <= maxSize)
                && (designation == ShipData.HangarOptions.General || designation == ship.shipData.HangarDesignation)
            );

            if (targetModule != ShipModuleType.Dummy)
                potentialShips = potentialShips.Filter(ship => ship.AnyModulesOf(targetModule));

            if (potentialShips.Length == 0)
                return null;

            float maxStrength = normalizedStrength ? potentialShips.Max(ship => ship.NormalizedStrength)
                                                   : potentialShips.Max(ship => ship.BaseStrength);

            var levelAdjust = new MinMaxStrength(maxStrength, empire);

            var bestShips = normalizedStrength ? potentialShips.Filter(ship => levelAdjust.InRange(ship.NormalizedStrength)) 
                                               : potentialShips.Filter(ship => levelAdjust.InRange(ship.BaseStrength));


            if (bestShips.Length == 0)
                return null;

            Ship pickedShip = RandomMath.RandItem(bestShips);

            if (false && Empire.Universe?.Debug == true)
            {
                Debug($"    Sorted Ship List ({bestShips.Length})");
                foreach (Ship loggedShip in bestShips)
                {
                    Debug($"    -- Name: {loggedShip.Name}, Strength: {loggedShip.NormalizedStrength}");
                }
                Debug($"    Chosen Role: {pickedShip.DesignRole}  Chosen Hull: {pickedShip.shipData.Hull}\n" +
                      $"    Strength: {pickedShip.NormalizedStrength}\n" +
                      $"    Name: {pickedShip.Name}. Range: {levelAdjust}");
            }
            return pickedShip;
        }

        public static bool PickColonyShip(Empire empire, out Ship colonyShip)
        {
            if (empire.isPlayer && !empire.AutoPickBestColonizer)
            {
                ResourceManager.GetShipTemplate(empire.data.CurrentAutoColony, out colonyShip);
            }
            else
            {
                colonyShip = ShipsWeCanBuild(empire).FindMaxFiltered(s => s.isColonyShip,
                                                                     s => s.StartingColonyGoods() + 
                                                                          s.NumBuildingsDeployedOnColonize() * 20 + 
                                                                          s.MaxFTLSpeed / 1000);
            }

            if (colonyShip == null)
            {
                if (!ResourceManager.GetShipTemplate(empire.data.DefaultColonyShip, out colonyShip))
                {
                    Log.Error($"{empire} failed to find a ColonyShip template! AutoColony:{empire.data.CurrentAutoColony}" +
                              $"  Default:{empire.data.DefaultColonyShip}");

                    return false;
                }
            }
            return true;
        }

        public static Ship PickShipToRefit(Ship oldShip, Empire empire)
        {
            Ship[] ships = ShipsWeCanBuild(empire).Filter(s => s.shipData.Hull == oldShip.shipData.Hull
                                                              && s.DesignRole == oldShip.DesignRole
                                                              && s.NormalizedStrength.Greater(oldShip.NormalizedStrength * 1.1f)
                                                              && s.Name != oldShip.Name);
            if (ships.Length == 0)
                return null;

            Ship picked = RandomMath.RandItem(ships);
            Log.Info(ConsoleColor.DarkCyan, $"{empire.Name} Refit: {oldShip.Name}, Strength: {oldShip.NormalizedStrength}" +
                                            $" refit to --> {picked.Name}, Strength: {picked.NormalizedStrength}");
            return picked;
        }

        public static Ship PickFreighter(Empire empire, float fastVsBig)
        {
            if (empire.isPlayer && empire.AutoFreighters
                                && !EmpireManager.Player.AutoPickBestFreighter
                                && ResourceManager.GetShipTemplate(empire.data.CurrentAutoFreighter, out Ship freighter))
            {
                return freighter;
            }

            var freighters = new Array<Ship>();
            foreach (string shipId in empire.ShipsWeCanBuild)
            {
                if (ResourceManager.GetShipTemplate(shipId, out Ship ship))
                {
                    if (!ship.IsCandidateForTradingBuild)
                        continue;

                    freighters.Add(ship);
                    if (Empire.Universe?.Debug == true)
                    {
                        Log.Info(ConsoleColor.Cyan, $"pick freighter: {ship.Name}: " +
                                                    $"Value: {ship.BestFreighterValue(empire, fastVsBig)}");
                    }
                }
                else
                    Log.Warning($"Could not find shipID '{shipId}' in ship dictionary");
            }

            freighter = freighters
                .FindMax(ship => ship.BestFreighterValue(empire, fastVsBig));

            if (Empire.Universe?.Debug == true)
                Log.Info(ConsoleColor.Cyan, $"----- Picked {freighter.Name}");

            return freighter;
        }

        public static float GetModifiedStrength(int shipSize, int numOffensiveSlots, float offense, float defense)
        {
            float offenseRatio = (float)numOffensiveSlots / shipSize;
            float modifiedStrength;

            if (defense > offense && offenseRatio < 0.2f)
                modifiedStrength = offense * 2;
            else
                modifiedStrength = offense + defense;

            return modifiedStrength;
        }

        public static Color GetHangarTextColor(string shipName)
        {
            DynamicHangarOptions dynamicHangarType = GetDynamicHangarOptions(shipName);
            switch (dynamicHangarType)
            {
                case DynamicHangarOptions.DynamicLaunch:      return Color.Gold;
                case DynamicHangarOptions.DynamicInterceptor: return Color.Cyan;
                case DynamicHangarOptions.DynamicAntiShip:    return Color.OrangeRed;
                default:                                      return Color.Wheat;
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

        public static Ship BestShipWeCanBuild(ShipData.RoleName role, Empire empire)
        {
            Ship bestShip = PickFromCandidates(role, empire);
            if (bestShip == null || bestShip.shipData.IsShipyard || bestShip.IsSubspaceProjector) 
                return null;

            return bestShip;
        }
    }
   
    public enum DynamicHangarOptions
    {
        Static,
        DynamicLaunch,
        DynamicInterceptor,
        DynamicAntiShip
    }
}