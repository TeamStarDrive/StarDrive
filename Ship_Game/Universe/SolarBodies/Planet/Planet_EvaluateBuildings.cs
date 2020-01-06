using System;
using System.Diagnostics;
using System.Linq;
using Ship_Game.AI.Budget;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;

namespace Ship_Game
{
    public partial class Planet
    {
        EnumFlatMap<ColonyPriority, float> Priorities = new EnumFlatMap<ColonyPriority, float>();

        private enum ColonyPriority
        {
            FoodFlat,
            FoodPerCol,
            ProdFlat,
            ProdPerCol,
            ProdPerRichness,
            StorageNeeds,
            PopGrowth,
            PopCap,
            ResearchFlat,
            ResearchPerCol,
            CreditsPerCol,
            TaxPercent,
            Fertility
        }

        bool IsPlanetExtraDebugTarget()
        {
            if (Name == ExtraInfoOnPlanet)
                return true;

            // Debug eval planet if we have colony screen open
            return Debugger.IsAttached
                   && Empire.Universe.LookingAtPlanet
                   && Empire.Universe.workersPanel is ColonyScreen colony
                   && colony.P == this;
        }

        void DebugEvalBuild(Building b, string what, float score)
        {
            if (IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.DarkGray,
                    $"Eval VALUE  {b.Name,-20}  {what,-16} {(+score).SignString()}");
        }

        void BuildAndScrapCivilianBuildings(float budget)
        {
            UpdateGovernorPriorities();
            if (budget < 0f)
            {
                TryScrapBuilding(); // we must scrap something to bring us above of our debt tolerance
            }
            else
            {
                TryBuildTerraformers(); // Build Terraformers if needed
                if (FreeHabitableTiles > 0)
                {
                    SimpleBuild(budget); // lets try to build something within our budget
                }
                else
                {
                    if (BuildBiospheres(budget))
                        return;

                    TryScrapBuilding(); // we don't have room for expansion. Let's see if we can replace to a better value building
                }
            }
        }

        // Fat Bastard - This will create a map with Governor priorities per building trait
        void UpdateGovernorPriorities()
        {
            Priorities.Clear();
            CalcFoodPriorities();
            CalcProductionPriorities();
            CalcPopulationPriorities();
            CalcResearchPriorities();
            CalcFertilityPriorities();
            CalcMoneyPriorities();
            CalcStoragePriorities();

            if (IsPlanetExtraDebugTarget())
            {
                Log.Info(ConsoleColor.Green,$"**** {Name} - Governor Priorities        ****");
                foreach ((ColonyPriority key, float value) in Priorities.Values)
                    Log.Info($"{key,-16} = {value}");
                Log.Info(ConsoleColor.Green, "---------------------------------------------");
            }
        }


        void CalcFoodPriorities()
        {
            if (IsCybernetic)
                return;

            float netFoodPerColonist = Food.NetYieldPerColonist - FoodConsumptionPerColonist;
            float flatFoodToFeedAll  = FoodConsumptionPerColonist * PopulationBillion - Food.NetFlatBonus;
            float fertilityBonus     = Fertility > 0 ? 1 / Fertility : 0;

            float flat   = (flatFoodToFeedAll - netFoodPerColonist - Food.NetFlatBonus).ClampMin(0);
            float perCol = 10 - netFoodPerColonist - Food.NetFlatBonus*fertilityBonus;
            perCol       = (perCol * Fertility).ClampMin(0);
            if (IsStarving)
            {
                perCol += 2 * Fertility;
                flat   += (2 - Fertility).ClampMin(0);
            }

            perCol += (1 - Storage.FoodRatio) * Fertility;
            flat   += 1 - Storage.FoodRatio;

            flat   = ApplyGovernorBonus(flat, 1f, 0.5f, 0.5f, 2f, 0.5f);
            perCol = ApplyGovernorBonus(perCol, 1f, 0.5f, 0.5f, 2f, 0.5f);
            Priorities[ColonyPriority.FoodFlat]   = flat;
            Priorities[ColonyPriority.FoodPerCol] = perCol;
        }

        void CalcProductionPriorities()
        {
            float netProdPerColonist = Prod.NetYieldPerColonist - ProdConsumptionPerColonist;
            float flatProdToFeedAll  = IsCybernetic ? ConsumptionPerColonist * PopulationBillion - Prod.NetFlatBonus : 0;
            float richnessBonus      = MineralRichness > 0 ? 1 / MineralRichness : 0;

            float flat = NonCybernetic ? (10 - netProdPerColonist - Prod.NetFlatBonus).ClampMin(0) 
                                       : (flatProdToFeedAll - netProdPerColonist - Prod.NetFlatBonus).ClampMin(0);

            float perRichness = (MineralRichness * 10 - Prod.NetFlatBonus).ClampMin(0);
            float perCol      = 10 - netProdPerColonist - flatProdToFeedAll*richnessBonus;
            perCol            = (perCol * MineralRichness).ClampMin(0);
            if (IsCybernetic && IsStarving)
            {
                perCol      += 2 * MineralRichness;
                flat        += (2 - MineralRichness).ClampMin(0);
                perRichness += 1.5f * MineralRichness;
            }

            flat   += 1 - Storage.ProdRatio;
            perCol += (1 - Storage.ProdRatio) * MineralRichness;

            flat        = ApplyGovernorBonus(flat, 1f, 2f, 0.5f, 0.5f, 1.5f);
            perRichness = ApplyGovernorBonus(perRichness, 1f, 2f, 0.5f, 0.5f, 1.5f);
            perCol      = ApplyGovernorBonus(perCol, 1f, 2f, 0.5f, 0.5f, 1.5f);
            Priorities[ColonyPriority.ProdFlat]        = flat;
            Priorities[ColonyPriority.ProdPerRichness] = perRichness;
            Priorities[ColonyPriority.ProdPerCol]      = perCol;
        }

        void CalcPopulationPriorities()
        {
            float popGrowth = (10 - PopulationRatio*10).ClampMin(0);
            float popCap    = FreeHabitableTiles > 0 ? (PopulationRatio*10).Clamped(0, 10) : 0;
            popGrowth       = ApplyGovernorBonus(popGrowth, 1f, 1f, 1f, 1f, 1f);
            popCap          = ApplyGovernorBonus(popCap, 1f, 1f, 1f, 1f, 1f);
            Priorities[ColonyPriority.PopGrowth] = popGrowth;
            Priorities[ColonyPriority.PopCap]    = popCap;
        }

        void CalcStoragePriorities()
        {
            float storage = NonCybernetic ? (Storage.FoodRatio + Storage.ProdRatio) * 5
                                          : Storage.ProdRatio * 10;
            storage += Level;
            storage  = ApplyGovernorBonus(storage, 1f, 1f, 0.5f, 1.25f, 1f);
            Priorities[ColonyPriority.StorageNeeds] = storage;
        }

        void CalcResearchPriorities()
        {
            float flat   = 10 - Res.NetFlatBonus;
            float perCol = 10 * PopulationBillion;
            flat         = ApplyGovernorBonus(flat, 1f, 0.25f, 2f, 0.25f, 0.25f);
            perCol       = ApplyGovernorBonus(perCol, 1f, 0.25f, 2f, 0.25f, 0.25f);
            Priorities[ColonyPriority.ResearchFlat]   = flat;
            Priorities[ColonyPriority.ResearchPerCol] = perCol;
        }

        void CalcMoneyPriorities()
        {
            float tax     = PopulationBillion;
            float credits = PopulationBillion;
            tax           = ApplyGovernorBonus(tax, 1f, 0.5f, 0.75f, 0.5f, 0.5f);
            credits       = ApplyGovernorBonus(credits, 1f, 0.2f, 0.75f, 0.5f, 0.5f);
            Priorities[ColonyPriority.TaxPercent]    = tax;
            Priorities[ColonyPriority.CreditsPerCol] = credits;
        }

        void CalcFertilityPriorities()
        {
            float fertility = NonCybernetic ? 5 - MaxFertility : 0;
            fertility       = ApplyGovernorBonus(fertility, 1.5f, 0.5f, 1, 2, 1f);
            Priorities[ColonyPriority.Fertility] = fertility;
        }

        float ApplyGovernorBonus(float value, float core, float industrial, float research, float agricultural, float military)
        {
            float multiplier;
            switch (colonyType)
            {
                case ColonyType.Core:         multiplier = core;         break;
                case ColonyType.Industrial:   multiplier = industrial;   break;
                case ColonyType.Research:     multiplier = research;     break;
                case ColonyType.Agricultural: multiplier = agricultural; break;
                case ColonyType.Military:     multiplier = military;     break;
                default:                      multiplier = 1;            break;
            }

            value = (value * multiplier).Clamped(0, 10);
            return (float)Math.Round(value, 0);
        }

        bool SimpleBuild(float budget) // build a building with a positive value
        {
            if (CivilianBuildingInTheWorks)
                return false;

            Building bestBuilding = ChooseBestBuilding(BuildingsCanBuild, budget);
            return bestBuilding != null && Construction.AddBuilding(bestBuilding);
        }

        bool TryScrapBuilding()
        {
            if (GovernorShouldNotScrapBuilding)
                return false;  // Player decided not to allow governors to scrap buildings

            Building worstBuilding = ChooseWorstBuilding(BuildingList);
            if (worstBuilding == null)
                return false;

            Log.Info(ConsoleColor.Blue, $"{Owner.PortraitName} SCRAPPED {worstBuilding.Name} on planet {Name}");
            ScrapBuilding(worstBuilding); // scrap the worst building we have on the planet
            return true;
        }

        Building ChooseBestBuilding(Array<Building> buildings, float budget)
        {
            if (buildings.Count == 0)
                return null;

            if (IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Cyan, $"==== Planet  {Name}  CHOOSE BEST BUILDING, Budget: {budget} ====");

            Building best      = null;
            float highestScore = 0f; // So a building with a value of 0 will not be built.

            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                if (NotSuitableForBuildEval(b, budget))
                    continue;

                float buildingScore = EvaluateBuilding(b);
                if (buildingScore > highestScore)
                {
                    best = b;
                    highestScore = buildingScore;
                }
            }

            if (best != null && IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Green, $"-- Planet {Name}: Best Building is {best.Name} " +
                                            $"with score of {highestScore} Budget: {budget} -- ");

            return best;
        }

        Building ChooseWorstBuilding(Array<Building> buildings)
        {
            if (buildings.Count == 0)
                return null;

            if (IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Magenta, $"==== Planet  {Name}  CHOOSE WORST BUILDING ====");

            Building worst = null;
            float lowestScore = float.MaxValue; // So a building with a value of 0 will not be built.

            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                if (NotSuitableForScrapEval(b))
                    continue;

                float buildingScore = EvaluateBuilding(b);
                if (buildingScore < lowestScore)
                {
                    worst = b;
                    lowestScore = buildingScore;
                }
            }

            if (worst != null && IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Red, $"-- Planet {Name}: Best Building is {worst.Name} " +
                                            $"with score of {lowestScore} -- ");

            return worst;
        }

        bool NotSuitableForBuildEval(Building b, float budget)
        {
            if (b.IsMilitary || b.IsTerraformer || b.IsBiospheres) 
                return true; // Different logic for these

            if (b.IsHarmfulToEnv && TerraformingHere)
                return true; // Do not build environmental harmful buildings while terraforming

            if (b.ActualMaintenance(this) > budget && !b.IsMoneyBuilding)
                return true; // Too expensive for us and its not getting more juice from the population

            if ((b.Cost - Storage.Prod) / AverageProductionPercent > 50)
                return true; // Too much time to build it

            return false;
        }

        bool NotSuitableForScrapEval(Building b)
        {
            if (b.IsBiospheres
                || !b.Scrappable
                || b.IsPlayerAdded && Owner.isPlayer
                || b.IsTerraformer
                || b.IsMilitary)
            {
                return true;
            }

            return false;
        }

        // Gretman function, to support DoGoverning()
        float EvaluateBuilding(Building b)
        {
            float score = 0;
            score += EvalTraits(Priorities[ColonyPriority.FoodFlat], b.PlusFlatFoodAmount);
            score += EvalTraits(Priorities[ColonyPriority.FoodPerCol], b.PlusFoodPerColonist);
            score += EvalTraits(Priorities[ColonyPriority.ProdFlat], b.PlusFlatProductionAmount);
            score += EvalTraits(Priorities[ColonyPriority.ProdPerCol], b.PlusProdPerColonist);
            score += EvalTraits(Priorities[ColonyPriority.ProdPerRichness], b.PlusProdPerRichness);
            score += EvalTraits(Priorities[ColonyPriority.PopGrowth], b.PlusFlatPopulation / 5);
            score += EvalTraits(Priorities[ColonyPriority.PopCap], b.MaxPopIncrease / 100);
            score += EvalTraits(Priorities[ColonyPriority.StorageNeeds], (float)b.StorageAdded / 50);
            score += EvalTraits(Priorities[ColonyPriority.ResearchFlat], b.PlusFlatResearchAmount);
            score += EvalTraits(Priorities[ColonyPriority.ResearchPerCol], b.PlusResearchPerColonist);
            score += EvalTraits(Priorities[ColonyPriority.CreditsPerCol], b.CreditsPerColonist);
            score += EvalTraits(Priorities[ColonyPriority.TaxPercent], b.PlusTaxPercentage);
            score += EvalTraits(Priorities[ColonyPriority.Fertility], b.MaxFertilityOnBuildFor(Owner, Category));

             if (IsPlanetExtraDebugTarget())
             {
                 if (score > 0f)
                     Log.Info(ConsoleColor.Cyan, $"Eval BUILD  {b.Name,-20}  {"SUITABLE",-16} {score.SignString()}");
                 else
                     Log.Info(ConsoleColor.DarkRed, $"Eval BUILD  {b.Name,-20}  {"NOT GOOD",-16} {score.SignString()}");
             }

            return score;
        }

        float EvalTraits(float priority, float trait) => priority * trait;

        void TryBuildTerraformers()
        {
            if (TerraformerInTheWorks || NumWantedTerraformers <= 0)
                return;

            PlanetGridSquare tile = null; 
            if (TilesList.Any(t => !t.Habitable && !t.BuildingOnTile))
                tile = TilesList.First(t => !t.Habitable && !t.BuildingOnTile);

            if (tile != null) // try to build a terraformer on a black tile first
            {
                Construction.AddBuilding(TerraformersWeCanBuild, tile);
            }
            else
            {
                if (BuildingList.Count == TileArea)
                    TryScrapBuilding();

                Construction.AddBuilding(TerraformersWeCanBuild);
            }
        }
        
        int NumWantedTerraformers
        {
            get
            {
                if (TerraformersWeCanBuild == null)
                    return 0;

                int num = 0;
                if (TilesList.Any(t => !t.Habitable))
                    ++num;

                if (TilesList.Any(t => t.Biosphere))
                    ++num;

                if (Category != Owner.data.PreferredEnv || NonCybernetic && BaseMaxFertility.Less(1 / Owner.RacialEnvModifer(Category)))
                    ++num;

                num -= Math.Max(BuildingList.Count(b => b.IsTerraformer), 0);
                return num;
            }
        }

        bool BuildBiospheres(float budget)
        {
            if (CivilianBuildingInTheWorks)
                return false;

            if (HabitablePercentage.AlmostEqual(1))
                return false; // All tiles are habitable

            Building bio = BiospheresWeCanBuild;
            if (bio == null || BiosphereInTheWorks && bio.ActualMaintenance(this) > budget)
                return false; // not within budget or Biospheres are being built

            if (PopulationRatio.GreaterOrEqual(0.9f) || BuiltCoverage.AlmostEqual(1))
            {
                if (IsPlanetExtraDebugTarget())
                    Log.Info(ConsoleColor.Green, $"{Owner.PortraitName} BUILT {bio.Name} on planet {Name}");

                return Construction.AddBuilding(bio);
            }

            return false;
        }

        bool OutpostBuiltOrInQueue()
        {
            // First check the existing buildings
            if (BuildingList.Any(b => b.IsCapitalOrOutpost))
                return true;

            // Then check the queue
            return ConstructionQueue.Any(q => q.isBuilding && q.Building.IsOutpost);
        }

        void BuildOutpostIfAble() // A Gretman function to support DoGoverning()
        {
            // Check Existing Buildings and the queue
            if (OutpostBuiltOrInQueue())
                return;

            // Build it!
            Construction.AddBuilding(ResourceManager.CreateBuilding(Building.OutpostId));

            // Move Outpost to the top of the list, and rush production
            for (int i = 0; i < ConstructionQueue.Count; ++i)
            {
                QueueItem q = ConstructionQueue[i];
                if (q.isBuilding && q.Building.IsOutpost)
                {
                    ConstructionQueue.RemoveAt(i);
                    ConstructionQueue.Insert(0, q);
                    Construction.RushProduction(0);
                    break;
                }
            }
        }
    }
}