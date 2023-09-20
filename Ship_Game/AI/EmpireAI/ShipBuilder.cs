using System;
using Ship_Game.Ships;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;

namespace Ship_Game.AI
{
    public static class ShipBuilder  // Created by Fat Bastard to support ship picking for build
    {
        public const int OrbitalsLimit  = 27; // FB - Maximum of 27 stations or platforms (or shipyards)
        public const int ShipYardsLimit = 3; // FB - Maximum of 3 shipyards

        public static IShipDesign PickFromCandidates(
            RoleName role, Empire empire, int maxSize = 0,
            HangarOptions designation = HangarOptions.General)
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

        static Array<IShipDesign> ShipsWeCanBuild(Empire empire, Predicate<IShipDesign> filter)
        {
            var ships = new Array<IShipDesign>();
            foreach (IShipDesign design in empire.ShipsWeCanBuild)
            {
                if (filter(design))
                    ships.Add(design);
            }
            return ships;
        }

        // Pick the strongest ship to build with a cost limit and a role
        public static IShipDesign PickCostEffectiveShipToBuild(RoleName role, Empire empire, 
            float maxCost, float maintBudget)
        {
            Array<IShipDesign> potentialShips = ShipsWeCanBuild(empire,
                s => s.Role == role && s.GetCost(empire).LessOrEqual(maxCost) 
                                          && s.GetMaintenanceCost(empire).Less(maintBudget)
                                          && !s.IsShipyard
                                          && !s.IsSubspaceProjector
                                          && !s.IsResearchStation);

            if (potentialShips.Count == 0)
            {
                if (role == RoleName.drone)
                    return GetDefaultEventDrone();
                return null;
            }

            return potentialShips.FindMax(s => s.BaseStrength);
        }

        // Try to get a pre-defined default drone for event buildings which can launch drones
        static IShipDesign GetDefaultEventDrone()
        {
            IShipDesign drone = ResourceManager.Ships.GetDesign(GlobalStats.Defaults.DefaultEventDrone, false);
            if (drone == null)
                Log.Warning($"Could not find default drone - {GlobalStats.Defaults.DefaultEventDrone} - in Vanilla SavedDesigns folder");

            return drone;
        }
        
        static IShipDesign PickFromCandidatesByStrength(RoleName role, Empire empire,
            int maxSize, HangarOptions designation)
        {
            Array<IShipDesign> potentialShips = ShipsWeCanBuild(empire, design => design.Role == role
                && (maxSize == 0 || design.SurfaceArea <= maxSize)
                && (designation == HangarOptions.General || designation == design.HangarDesignation)
            );

            if (potentialShips.Count == 0)
                return null;

            if (potentialShips.Count == 1)
                return potentialShips.First;

            float maxStrength = potentialShips.FindMax(ship => ship.BaseStrength).BaseStrength;
            var levelAdjust   = new MinMaxStrength(maxStrength, empire);
            var bestShips     = potentialShips.Filter(ship => levelAdjust.InRange(ship.BaseStrength));

            if (bestShips.Length == 0)
            {
                // If ther are ships which not in the level adjust range, take them all instead of
                // returning nothing. Better to get something and maybe refit later.
                if (potentialShips.Count > 0) 
                    bestShips = potentialShips.ToArray(); 
                else
                    return null;
            }

            // We choose the first item for the player to overcome edge case where several ships have the same str
            // because for the player - min str and max str is set to be the same to get the best ship)
            IShipDesign pickedShip = empire.isPlayer ? bestShips[0] : empire.Random.Item(bestShips);

            if (false && empire.Universe?.Debug == true)
            {
                Debug($"    Sorted Ship List ({bestShips.Length})");
                foreach (IShipDesign loggedShip in bestShips)
                {
                    Debug($"    -- Name: {loggedShip.Name}, Strength: {loggedShip.BaseStrength}");
                }
                Debug($"    Chosen Role: {pickedShip.Role}  Chosen Hull: {pickedShip.Hull}\n" +
                      $"    Strength: {pickedShip.BaseStrength}\n" +
                      $"    Name: {pickedShip.Name}. Range: {levelAdjust}");
            }
            return pickedShip;
        }

        static float GetColonyShipScore(IShipDesign s, Empire empire)
        {
            float maxFTL = ShipStats.GetFTLSpeed(s, empire);
            return s.StartingColonyGoods + s.NumBuildingsDeployed * 20 + maxFTL / 1000;
        }

        public static bool PickColonyShip(Empire empire, out IShipDesign colonyShip)
        {
            if (empire.isPlayer && !empire.AutoPickBestColonizer)
            {
                ResourceManager.Ships.GetDesign(empire.data.CurrentAutoColony, out colonyShip);
            }
            else
            {
                colonyShip = ShipsWeCanBuild(empire, s => s.IsColonyShip).FindMax(s => GetColonyShipScore(s, empire));
            }

            if (colonyShip == null)
            {
                if (!ResourceManager.Ships.GetDesign(empire.data.DefaultColonyShip, out colonyShip))
                {
                    Log.Error($"{empire} failed to find a ColonyShip template! AutoColony:{empire.data.CurrentAutoColony}" +
                              $"  Default:{empire.data.DefaultColonyShip}");

                    return false;
                }
            }
            return true;
        }

        public static IShipDesign PickShipToRefit(Ship oldShip, Empire empire)
        {
            Array<IShipDesign> ships = ShipsWeCanBuild(empire, s => s.Hull == oldShip.ShipData.Hull
                                                            && s.Role == oldShip.DesignRole
                                                            && s.BaseStrength.Greater(oldShip.BaseStrength * 1.1f)
                                                            && s.Name != oldShip.Name);
            if (ships.Count == 0)
                return null;

            IShipDesign picked = empire.Random.Item(ships);
            Log.Info(ConsoleColor.DarkCyan, $"{empire.Name} Refit: {oldShip.Name}, Strength: {oldShip.BaseStrength}" +
                                            $" refit to --> {picked.Name}, Strength: {picked.BaseStrength}");
            return picked;
        }

        
        public static IShipDesign PickResearchStation(Empire empire)
        {
            if (empire.isPlayer && !empire.AutoPickBestResearchStation)
            {
                if (!empire.CanBuildResearchStations)
                    return null;

                ResourceManager.Ships.GetDesign(empire.data.CurrentResearchStation, out IShipDesign reseaechStation);
                return reseaechStation;
            }

            var potentialResearchStations = new Array<IShipDesign>();
            float maxResearchPerTurn = 0;
            foreach (IShipDesign design in empire.ShipsWeCanBuild)
            {
                if (design.IsResearchStation)
                {
                    potentialResearchStations.Add(design);
                    if (design.BaseResearchPerTurn > maxResearchPerTurn)
                        maxResearchPerTurn = design.BaseResearchPerTurn;
                }
            }

            IShipDesign bestResearchStation = null;
            if (potentialResearchStations.Count > 0)
            {
                var researchStations = potentialResearchStations.
                    Filter(s  => s.BaseResearchPerTurn.InRange(maxResearchPerTurn * 0.8f, maxResearchPerTurn));

                bestResearchStation = researchStations.FindMax(s => s.BaseStrength / s.SurfaceArea);
            }

            if (empire.Universe?.Debug == true)
                Log.Info(ConsoleColor.Cyan, $"----- Picked {bestResearchStation?.Name ?? empire.data.ResearchStation}");

            return bestResearchStation ?? ResourceManager.Ships.GetDesign(empire.data.ResearchStation, throwIfError: true);
        }

        public static IShipDesign PickMiningStation(Empire empire)
        {
            if (empire.isPlayer && !empire.AutoPickBestMiningStation)
            {
                if (!empire.CanBuildMiningStations)
                    return null;

                ResourceManager.Ships.GetDesign(empire.data.CurrentMiningStation, out IShipDesign miningStation);
                return miningStation;
            }

            var potentialMiningStations = new Array<IShipDesign>();
            float maxProcessingPerTurn = 0;
            foreach (IShipDesign design in empire.ShipsWeCanBuild)
            {
                if (design.IsMiningStation)
                {
                    potentialMiningStations.Add(design);
                    if (design.BaseProcessingPerTurn > maxProcessingPerTurn)
                        maxProcessingPerTurn = design.BaseProcessingPerTurn;
                }
            }

            IShipDesign bestMiningStation = null;
            if (potentialMiningStations.Count > 0)
            {
                var miningStations = potentialMiningStations.
                    Filter(s => s.BaseProcessingPerTurn.InRange(maxProcessingPerTurn * 0.8f, maxProcessingPerTurn));

                bestMiningStation = miningStations.FindMax(s => s.BaseCargoSpace / s.BaseProcessingPerTurn);
            }

            if (empire.Universe?.Debug == true)
                Log.Info(ConsoleColor.Cyan, $"----- Picked {bestMiningStation?.Name ?? empire.data.ResearchStation}");

            return bestMiningStation ?? ResourceManager.Ships.GetDesign(empire.data.ResearchStation, throwIfError: true);
        }

        static float FreighterValue(IShipDesign s, Empire empire, float fastVsBig)
        {
            float maxKFTL  = ShipStats.GetFTLSpeed(s, empire) * 0.001f;
            float maxDSTL  = ShipStats.GetSTLSpeed(s, empire) * 0.1f;
            float cargo    = ShipStats.GetCargoSpace(s.BaseCargoSpace, s);
            float turnRate = ShipStats.GetTurnRadsPerSec(s).ToDegrees();
            float area     = s.SurfaceArea;

            float fastVsBigWeight = fastVsBig * 10;
            float costWeight     = s.GetCost(empire) * 0.2f;
            float movementWeight = (maxKFTL + maxDSTL + turnRate) * fastVsBigWeight;
            float cargoWeight    = cargo * (10 - fastVsBigWeight);
            float score          = movementWeight + cargoWeight - costWeight;

            // For faster , cheaper ships vs big and maybe slower ships
            return score;
        }


        public static IShipDesign PickFreighter(Empire empire, float fastVsBig)
        {
            if (empire.isPlayer && empire.AutoFreighters &&
                !empire.Universe.Player.AutoPickBestFreighter &&
                ResourceManager.Ships.GetDesign(empire.data.CurrentAutoFreighter, out IShipDesign freighter))
            {
                return freighter;
            }

            var freighters = new Array<IShipDesign>();
            foreach (IShipDesign design in empire.ShipsWeCanBuild)
            {
                if (!design.IsCandidateForTradingBuild)
                    continue;

                freighters.Add(design);
                if (empire.Universe?.Debug == true)
                {
                    Log.Info(ConsoleColor.Cyan,
                        $"pick freighter: {design.Name}: Value: {FreighterValue(design, empire, fastVsBig)}");
                }
            }

            freighter = freighters.FindMax(ship => FreighterValue(ship, empire, fastVsBig));

            if (empire.Universe?.Debug == true)
                Log.Info(ConsoleColor.Cyan, $"----- Picked {freighter?.Name ?? "null"} (fast vs big: {fastVsBig.String()}/{(1-fastVsBig).String()})");

            return freighter;
        }

        static float GetConstructorValue(IShipDesign s, Empire empire, float buildCost, bool belowDiscountThreshold)
        {
            if (!s.IsConstructor)
                return float.MinValue;

            float cost = s.GetCost(empire);
            if (buildCost < cost *2
                ||  belowDiscountThreshold && cost > GlobalStats.Defaults.ConstructionShipOrbitalDiscount)  
            {
                return -cost;
            }

            float maxFTLValue = ShipStats.GetFTLSpeed(s, empire) * 0.001f;
            float maxSTLValue = ShipStats.GetSTLSpeed(s, empire) * 0.1f;
            float turnRate = ShipStats.GetTurnRadsPerSec(s);
            float score = maxFTLValue + maxSTLValue + turnRate.ToDegrees() 
                + s.NumConstructionModules*GlobalStats.Defaults.ConstructionModuleBuildRate;
            
            return score;
        }

        public static IShipDesign PickConstructor(Empire empire, float buildCost, bool GetCheapest)
        {
            IShipDesign constructor = null;
            if (empire.isPlayer && !empire.AutoPickConstructors)
            {
                string constructorId = empire.data.ConstructorShip;
                if (!ResourceManager.Ships.GetDesign(constructorId, out constructor))
                {
                    Log.Warning($"PickConstructor: no construction ship with uid={constructorId}, falling back to default");
                    constructorId = empire.data.DefaultConstructor;
                    if (!ResourceManager.Ships.GetDesign(constructorId, out constructor))
                    {
                        Log.Warning($"PickConstructor: no construction ship with uid={constructorId}");
                        return null;    
                    }
                }
            }
            else
            {
                var constructors = new Array<IShipDesign>();
                foreach (IShipDesign design in empire.ShipsWeCanBuild)
                    if (design.IsConstructor)
                        constructors.Add(design);

                if (constructors.Count == 0)
                {
                    Log.Warning($"PickConstructor: no construction ship were found for {empire.Name}");
                    return null;
                }

                constructor = constructors.FindMax(s => GetConstructorValue(s, empire, buildCost, GetCheapest));
            }

            return constructor;
        }

        public static float GetModifiedStrength(int shipSize, int numOffensiveSlots, float offense, float defense)
        {
            float offenseRatio = (float)numOffensiveSlots / shipSize;
            float modifiedStrength;

            if (defense > offense && offenseRatio < 0.1f)
                modifiedStrength = offense + offense + defense*0.1f;
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
            if (Enum.TryParse(compare, out DynamicHangarOptions result))
                return result;

            return DynamicHangarOptions.Static;
        }

        public static bool IsDynamicHangar(string compare)
        {
            if (Enum.TryParse(compare, out DynamicHangarOptions result))
                return result != DynamicHangarOptions.Static;

            return false;
        }

        public static IShipDesign BestShipWeCanBuild(RoleName role, Empire empire)
        {
            IShipDesign bestShip = PickFromCandidates(role, empire);
            if (bestShip == null || bestShip.IsShipyard || bestShip.IsSubspaceProjector || bestShip.IsResearchStation) 
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