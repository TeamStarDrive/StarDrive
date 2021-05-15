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
                      ShipData.HangarOptions designation = ShipData.HangarOptions.General)
        {
            // The AI will pick ships to build based on their Strength and game difficulty level.
            // This allows it to choose the toughest ships to build. This is normalized by ship total slots
            // so ships with more slots of the same role wont get priority (bigger ships also cost more to build and maintain.
            return PickFromCandidatesByStrength(role, empire, maxSize, designation);
        }

        private struct MinMaxStrength
        {
            private readonly float Min;
            private readonly float Max;

            public MinMaxStrength(float maxStrength, Empire empire)
            {
                float max = empire.DifficultyModifiers.ShipBuildStrMax;
                float min = empire.isPlayer ? max : empire.DifficultyModifiers.ShipBuildStrMin;
                Min = min * maxStrength;
                Max = max * maxStrength;
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
        public static Ship PickCostEffectiveShipToBuild(ShipData.RoleName role, Empire empire, 
            float maxCost, float maintBudget)
        {
            Ship[] potentialShips = ShipsWeCanBuild(empire).Filter(
                ship => ship.DesignRole == role && ship.GetCost(empire).LessOrEqual(maxCost) 
                                                && ship.GetMaintCost(empire).Less(maintBudget)
                                                && !ship.shipData.IsShipyard
                                                && !ship.IsSubspaceProjector);

            if (potentialShips.Length == 0)
                return null;

            return potentialShips.FindMax(s => s.BaseStrength);
        }
        
        static Ship PickFromCandidatesByStrength(ShipData.RoleName role, Empire empire,
            int maxSize, ShipData.HangarOptions designation)
        {
            Ship[] potentialShips = ShipsWeCanBuild(empire).Filter(
                ship => ship.DesignRole == role
                && (maxSize == 0 || ship.SurfaceArea <= maxSize)
                && (designation == ShipData.HangarOptions.General || designation == ship.shipData.HangarDesignation)
            );

            if (potentialShips.Length == 0)
                return null;

            float maxStrength = potentialShips.Max(ship => ship.BaseStrength);
            var levelAdjust   = new MinMaxStrength(maxStrength, empire);
            var bestShips     = potentialShips.Filter(ship => levelAdjust.InRange(ship.BaseStrength));

            if (bestShips.Length == 0)
                return null;

            Ship pickedShip = RandomMath.RandItem(bestShips);

            if (false && Empire.Universe?.Debug == true)
            {
                Debug($"    Sorted Ship List ({bestShips.Length})");
                foreach (Ship loggedShip in bestShips)
                {
                    Debug($"    -- Name: {loggedShip.Name}, Strength: {loggedShip.BaseStrength}");
                }
                Debug($"    Chosen Role: {pickedShip.DesignRole}  Chosen Hull: {pickedShip.shipData.Hull}\n" +
                      $"    Strength: {pickedShip.BaseStrength}\n" +
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
                                                              && s.BaseStrength.Greater(oldShip.BaseStrength * 1.1f)
                                                              && s.Name != oldShip.Name);
            if (ships.Length == 0)
                return null;

            Ship picked = RandomMath.RandItem(ships);
            Log.Info(ConsoleColor.DarkCyan, $"{empire.Name} Refit: {oldShip.Name}, Strength: {oldShip.BaseStrength}" +
                                            $" refit to --> {picked.Name}, Strength: {picked.BaseStrength}");
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
                                                    $"Value: {ship.FreighterValue(empire, fastVsBig)}");
                    }
                }
                else
                {
                    Log.Warning($"Could not find shipID '{shipId}' in ship dictionary");
                }
            }

            freighter = freighters.FindMax(ship => ship.FreighterValue(empire, fastVsBig));

            if (Empire.Universe?.Debug == true)
                Log.Info(ConsoleColor.Cyan, $"----- Picked {freighter.Name}");

            return freighter;
        }

        public static Ship PickConstructor(Empire empire)
        {
            Ship constructor = null;
            if (empire.isPlayer)
            {
                string constructorId = empire.data.ConstructorShip;
                if (!ResourceManager.GetShipTemplate(constructorId, out constructor))
                {
                    Log.Warning($"PickConstructor: no construction ship with uid={constructorId}, falling back to default");
                    constructorId = empire.data.DefaultConstructor;
                    if (!ResourceManager.GetShipTemplate(constructorId, out constructor))
                    {
                        Log.Warning($"PickConstructor: no construction ship with uid={constructorId}");
                        return null;
                    }
                }
            }
            else
            {
                var constructors = new Array<Ship>();
                foreach (string shipId in empire.ShipsWeCanBuild)
                {
                    if (ResourceManager.GetShipTemplate(shipId, out Ship ship) && ship.IsConstructor)
                        constructors.Add(ship);
                }

                if (constructors.Count == 0)
                {
                    Log.Warning($"PickConstructor: no construction ship were found for {empire.Name}");
                    return null;
                }

                constructor = constructors.FindMax(s => s.ConstructorValue(empire));
            }

            return constructor;
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