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
            Fertility,
            SpacePort
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
                TryScrapBuilding(budget); // we must scrap something to bring us above of our debt tolerance
            }
            else
            {
                TryBuildTerraformers(budget); // Build Terraformers if needed
                if (FreeHabitableTiles > 0)
                {
                    SimpleBuild(budget); // lets try to build something within our budget
                }
                else
                {
                    if (BuildBiospheres(budget))
                        return;

                    ReplaceBuilding(budget); // we don't have room for expansion. Let's see if we can replace to a better value building
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
            CalcSpacePortPriorities();

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

            float foodToFeedAll      = FoodConsumptionPerColonist * PopulationBillion;
            float flatFoodToFeedAll  = foodToFeedAll - Food.NetFlatBonus;
            float fertilityBonus     = Fertility > 0 ? 1 / Fertility : 0;

            float flat   = (flatFoodToFeedAll - EstimatedAverageFood - Food.NetFlatBonus).ClampMin(0);
            float perCol = (foodToFeedAll - EstimatedAverageFood - Food.NetFlatBonus*fertilityBonus).ClampMin(0);
            if (IsStarving)
            {
                perCol += 2 * Fertility;
                flat   += (2 - Fertility).ClampMin(0);
            }

            perCol += (1 - Storage.FoodRatio) * Fertility;
            flat   += 1 - Storage.FoodRatio;

            if (colonyType == ColonyType.Agricultural)
                perCol += Fertility;


            flat   = ApplyGovernorBonus(flat, 0.5f, 2f, 2f, 1.5f, 1f);
            perCol = ApplyGovernorBonus(perCol, 1.25f, 0.25f, 0.25f, 2f, 0.25f);
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

            float richnessMultiplier = NonCybernetic ? 5 : 10;
            float perRichness = (MineralRichness * richnessMultiplier - Prod.NetFlatBonus).ClampMin(0);
            float perCol      = 10 - netProdPerColonist - flatProdToFeedAll*richnessBonus;
            perCol            = (perCol * MineralRichness).ClampMin(0);
            if (IsCybernetic)
            {
                if (colonyType == ColonyType.Industrial)
                    perCol += Fertility;

                if (IsStarving)
                {
                    perCol      += 2 * MineralRichness;
                    flat        += (2 - MineralRichness).ClampMin(0);
                    perRichness += 1.5f * MineralRichness;
                }
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
            float storage = NonCybernetic ? (Storage.FoodRatio + Storage.ProdRatio) * 2.5f
                                          : Storage.ProdRatio * 5f;
            storage += Level;
            storage  = ApplyGovernorBonus(storage, 1f, 1f, 0.5f, 1.25f, 1f);
            Priorities[ColonyPriority.StorageNeeds] = storage;
        }

        void CalcResearchPriorities()
        {
            float flat   = 10 - Res.NetFlatBonus;
            float perCol = PopulationBillion - Res.NetYieldPerColonist;
            flat         = ApplyGovernorBonus(flat, 0.75f, 0.2f, 1.25f, 0.2f, 0.25f);
            perCol       = ApplyGovernorBonus(perCol, 0.8f, 0.1f, 1f, 0.1f, 0.1f);
            Priorities[ColonyPriority.ResearchFlat]   = flat;
            Priorities[ColonyPriority.ResearchPerCol] = perCol;
        }

        void CalcMoneyPriorities()
        {
            float tax     = PopulationBillion - Owner.data.TaxRate*4;
            float credits = (PopulationBillion - Money.IncomePerColonist).ClampMin(2);
            tax           = ApplyGovernorBonus(tax, 1f, 1f, 0.8f, 1f, 1f);
            credits       = ApplyGovernorBonus(credits, 1f, 0.5f, 0.75f, 0.5f, 0.5f);
            Priorities[ColonyPriority.TaxPercent]    = tax;
            Priorities[ColonyPriority.CreditsPerCol] = credits;
        }

        void CalcFertilityPriorities()
        {
            float fertility = NonCybernetic ? 5 - MaxFertility : 0;
            fertility       = ApplyGovernorBonus(fertility, 1.5f, 0.5f, 1f, 5f, 1f);
            Priorities[ColonyPriority.Fertility] = fertility;
        }

        void CalcSpacePortPriorities()
        {
            float spacePort = PopulationBillion;
            spacePort       = ApplyGovernorBonus(spacePort, 1.5f, 0.75f, 0.75f, 0.75f, 2f);
            Priorities[ColonyPriority.SpacePort] = spacePort;
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
            value = value.UpperBound(10);
            value = (value * multiplier).Clamped(0, 20);

            return (float)Math.Round(value, 0);
        }

        bool SimpleBuild(float budget) // build a building with a positive value
        {
            if (CivilianBuildingInTheWorks)
                return false;

            ChooseBestBuilding(BuildingsCanBuild, budget, out Building bestBuilding);
            return bestBuilding != null && Construction.AddBuilding(bestBuilding);
        }

        bool TryScrapBuilding(float budget)
        {
            if (GovernorShouldNotScrapBuilding)
                return false;  // Player decided not to allow governors to scrap buildings

            Building toScrap;
            if (budget > 0) // pick the building with the lowest score
                ChooseWorstBuilding(BuildingList, out toScrap);
            else // we have no budget - get rid of the fattest building
                ChooseMostExpensiveBuilding(BuildingList, out toScrap);

            if (toScrap == null)
                return false;

            Log.Info(ConsoleColor.Blue, $"{Owner.PortraitName} SCRAPPED {toScrap.Name} on planet {Name}");
            ScrapBuilding(toScrap); // scrap the worst/most expensive building we have on the planet
            return true;
        }

        void ReplaceBuilding(float budget)
        {
            if (GovernorShouldNotScrapBuilding)
                return;  // Player decided not to allow governors to scrap buildings

            float worstBuildingScore = ChooseWorstBuilding(BuildingList, out Building worstBuilding);
            if (worstBuilding == null)
                return;

            float replacementBudget = budget + worstBuilding.ActualMaintenance(this);
            float bestBuildingScore = ChooseBestBuilding(BuildingsCanBuild, replacementBudget, out Building bestBuilding);
            if (bestBuilding == null)
                return;

            // the best building score should be at least 10 points better than what we are scrapping
            if (bestBuildingScore > worstBuildingScore + 10)
            {
                ScrapBuilding(worstBuilding);
                Construction.AddBuilding(bestBuilding);

                Log.Info(ConsoleColor.Green, $"{Owner.PortraitName} Replaced {worstBuilding.Name} " +
                                             $"with {bestBuilding.Name} on planet {Name}");
            }
        }

        float ChooseBestBuilding(Array<Building> buildings, float budget, out Building best)
        {
            best = null;
            if (buildings.Count == 0)
                return 0;

            if (IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Cyan, $"==== Planet  {Name}  CHOOSE BEST BUILDING, Budget: {budget} ====");


            float highestScore = 1f; // So a building with a low value of 1 or less will not be built.
            float incomingProd = IncomingFreighters.Sum(f => f.GetProduction());
            float totalProd    = Storage.Prod + incomingProd;
            
            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                if (NotSuitableForBuild(b, budget))
                    continue;

                float constructionMultiplier = ConstructionMultiplier(b, totalProd);
                float buildingScore          = EvaluateBuilding(b, constructionMultiplier);
                if (buildingScore > highestScore)
                {
                    best         = b;
                    highestScore = buildingScore;
                }
            }

            if (best != null && IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Green, $"-- Planet {Name}: Best Building is {best.Name} " +
                                            $"with score of {highestScore}");

            return highestScore;
        }

        float ChooseWorstBuilding(Array<Building> buildings, out Building worst)
        {
            worst = null;
            if (buildings.Count == 0)
                return 0;

            if (IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Red, $"==== Planet  {Name}  CHOOSE WORST BUILDING ====");

            float lowestScore  = float.MaxValue; 
            float storageInUse = Storage.MostGoodsInStorage;
            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                if (NotSuitableForScrap(b, storageInUse))
                    continue;

                // construction multiplier is 1 since there is no need to build anything, we are checking for scrap
                float buildingScore = EvaluateBuilding(b, 1);
                if (buildingScore < lowestScore)
                {
                    worst = b;
                    lowestScore = buildingScore;
                }
            }

            if (worst != null && IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Red, $"-- Planet {Name}: Worst Building is {worst.Name} " +
                                            $"with score of {lowestScore} -- ");

            return lowestScore;
        }

        void ChooseMostExpensiveBuilding(Array<Building> buildings, out Building expensive)
        {
            expensive = null;
            if (buildings.Count == 0)
                return;

            if (IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Magenta, $"==== Planet  {Name}  CHOOSE MOST EXPENSIVE BUILDING ====");

            float maintenance = 0;
            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                if (NotSuitableForScrap(b))
                    continue;

                if (b.ActualMaintenance(this) >  maintenance)
                    expensive = b;
            }

            if (expensive != null && IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Red, $"-- Planet {Name}: most expensive Building is {expensive.Name} " +
                                            $"with maintenance of {expensive.ActualMaintenance(this)} -- ");
        }

        bool NotSuitableForBuild(Building b, float budget)
        {
            if (b.IsMilitary || b.IsTerraformer || b.IsBiospheres) 
                return true; // Different logic for these

            if (b.ActualMaintenance(this) > budget && !b.IsMoneyBuilding)
                return true; // Too expensive for us and its not getting more juice from the population

            return false;
        }

        bool NotSuitableForScrap(Building b)
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

        bool NotSuitableForScrap(Building b, float storageInUse)
        {
            if (NotSuitableForScrap(b))
                return true;

            return IsStorageWasted(storageInUse, b.StorageAdded);
        }


        bool IsStorageWasted(float storageInUse, float storageAdded) => Storage.Max - storageAdded < storageInUse;

        // Gretman function, to support DoGoverning()
        float EvaluateBuilding(Building b, float constructionMultiplier)
        {
            float score = 0;
            score += EvalTraits(Priorities[ColonyPriority.FoodFlat], b.PlusFlatFoodAmount);
            score += EvalTraits(Priorities[ColonyPriority.FoodPerCol], b.PlusFoodPerColonist);
            score += EvalTraits(Priorities[ColonyPriority.ProdFlat], b.PlusFlatProductionAmount);
            score += EvalTraits(Priorities[ColonyPriority.ProdPerCol], b.PlusProdPerColonist);
            score += EvalTraits(Priorities[ColonyPriority.ProdPerRichness], b.PlusProdPerRichness);
            score += EvalTraits(Priorities[ColonyPriority.PopGrowth], b.PlusFlatPopulation / 5);
            score += EvalTraits(Priorities[ColonyPriority.PopCap], b.MaxPopIncrease / 200);
            score += EvalTraits(Priorities[ColonyPriority.StorageNeeds], (float)b.StorageAdded / 50);
            score += EvalTraits(Priorities[ColonyPriority.ResearchFlat], b.PlusFlatResearchAmount * 3);
            score += EvalTraits(Priorities[ColonyPriority.ResearchPerCol], b.PlusResearchPerColonist * 10);
            score += EvalTraits(Priorities[ColonyPriority.CreditsPerCol], b.CreditsPerColonist * 3);
            score += EvalTraits(Priorities[ColonyPriority.TaxPercent], b.PlusTaxPercentage * 10);
            score += EvalTraits(Priorities[ColonyPriority.Fertility], b.MaxFertilityOnBuildFor(Owner, Category) * 2);
            score += EvalTraits(Priorities[ColonyPriority.SpacePort], b.IsSpacePort ? 5 : 0);

            score *= FertilityMultiplier(b);
            score *= constructionMultiplier;

             if (IsPlanetExtraDebugTarget())
            {
                 if (score > 0f)
                     Log.Info(ConsoleColor.Cyan, $"Eval BUILD  {b.Name,-33}  {"SUITABLE",-10} " +
                                                 $"{score.SignString()} {"",3} {"Multiplier:",-10} {constructionMultiplier.String(2)}");
                 else
                     Log.Info(ConsoleColor.DarkRed, $"Eval BUILD  {b.Name,-33}  {"NOT GOOD",-10} " +
                                                    $"{score.SignString()} {"", 3} {"Multiplier:",-10} {constructionMultiplier.String(2)}");
            }

            return score;
        }

        float EvalTraits(float priority, float trait) => priority * trait;

        float FertilityMultiplier(Building b)
        {
            if (b.MaxFertilityOnBuild.AlmostZero())
                return 1;

            if (IsCybernetic && b.MaxFertilityOnBuild > 0)
                return 0.5f; // Fertility increasing buildings score should be very high in order to be worth building by cybernetics

            if (b.MaxFertilityOnBuild < 0 && colonyType == ColonyType.Agricultural)
                return 0; // Never build fertility reducers on Agricultural colonies

            float projectedMaxFertility = MaxFertility + b.MaxFertilityOnBuildFor(Owner, Category);
            if (projectedMaxFertility < 1)
                return projectedMaxFertility.ClampMin(0); // multiplier will be smaller in direct relation to its effect

            return 1;
        }

        float ConstructionMultiplier(Building b, float expectedProd)
        {
            if (expectedProd >= b.Cost)
                return 1;

            // This will allow the colony to slowly build more expensive buildings as it grows
            float missingProd = b.Cost - expectedProd;
            float multiplier  = EstimatedAverageProduction / missingProd;
            return multiplier.Clamped(0,1); 
        }

        void TryBuildTerraformers(float budget)
        {
            if (NumWantedTerraformers <= 0 || TerraformerInTheWorks)
                return;

            Building terraformer = TerraformersWeCanBuild;
            PlanetGridSquare tile = null; 
            if (TilesList.Any(t => !t.Habitable && !t.BuildingOnTile))
                tile = TilesList.First(t => !t.Habitable && !t.BuildingOnTile);

            if (tile != null) // try to build a terraformer on an unhabitable tile first
            {
                Construction.AddBuilding(terraformer, tile);
            }
            else if (!Construction.AddBuilding(terraformer)) 
            {
                // If could not add a terraformer anywhere due to planet being full
                // try to scrap a building and then retry construction
                if (TryScrapBuilding(budget))
                    Construction.AddBuilding(terraformer);
            }}
        
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
                    Construction.RushProduction(0, ProdHere);
                    break;
                }
            }
        }
    }
}