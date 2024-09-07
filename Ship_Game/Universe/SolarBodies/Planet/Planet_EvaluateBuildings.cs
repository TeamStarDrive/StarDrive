using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Universe.SolarBodies;

namespace Ship_Game
{
    public partial class Planet
    {
        static float BuildingScoreThreshold = 1;
        bool LowProdPotential => Prod.GrossMaxPotential < 1;
        bool LowFoodPotential => NonCybernetic && Food.GrossMaxPotential < 1;

        private readonly EnumFlatMap<ColonyPriority, float> Priorities = new EnumFlatMap<ColonyPriority, float>();

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
            SpacePort,
            InfraStructure,
            BuildingIncome
        }

        bool IsPlanetExtraDebugTarget()
        {
            if (Name == ExtraInfoOnPlanet)
                return true;

            // Debug eval planet if we have colony screen open
            return Debugger.IsAttached
                   && Universe.Screen.LookingAtPlanet
                   && Universe.Screen.workersPanel is ColonyScreen colony
                   && colony.P == this;
        }

        void BuildAndScrapCivilianBuildings(float budget)
        {
            UpdateGovernorPriorities();
            bool overBudget = budget < -0.1f;
            if (overBudget || Blueprints?.ShouldScrapNonRequiredBuilding() == true)
            {
                // We must scrap something to bring us above of our debt tolerance
                // or we can try scrap buildings not in blueprints
                TryScrapBuilding(overBudget);
                if (!overBudget) // we can try to build something if we have blueprints
                    BuildOrReplaceBuilding(budget, overBudget);
            }
            else
            {
                BuildOrReplaceBuilding(budget, overBudget);
                if (!TryBuildBiospheres(budget, out bool shouldScrapBiospheres) && shouldScrapBiospheres)
                    TryScrapBiospheres(); // Build or scrap Biospheres if needed
            }
        }

        void BuildOrReplaceBuilding(float budget, bool overBudget)
        {
            if (TryCancelOverBudgetCivilianBuilding(budget))
                return;

            if (FreeHabitableTiles > 0)
                SimpleBuild(budget); // Let's try to build something within our budget
            else
                ReplaceBuilding(budget, overBudget); // We don't have room for expansion. Let's see if we can replace to a better value building

            PrioritizeCriticalFoodBuildings();
            PrioritizeCriticalProductionBuildings();
        }

        // Fat Bastard - This will create a map with Governor priorities per building trait
        void UpdateGovernorPriorities()
        {
            Priorities.Clear();

            CalcProductionPriorities();
            if (ProdInfraStructureNeeded())
            {
                ShowDebugPriorities();
                return; // need to setup minimal flat prod 
            }

            CalcFoodPriorities();
            if (FoodInfraStructureNeeded())
            {
                ShowDebugPriorities();
                return; // need to setup minimal flat food 
            }

            CalcPopulationPriorities();
            CalcResearchPriorities();
            CalcFertilityPriorities();
            CalcMoneyPriorities();
            CalcStoragePriorities();
            CalcSpacePortPriorities();
            CalcInfrastructurePriority();

            ShowDebugPriorities();

            // Local Method
            bool ProdInfraStructureNeeded()
            {
                return LowProdPotential
                       && Prod.FlatBonus < 0.25f
                       && BuildingsCanBuild.Any(b => b.GoodFlatProduction(this));
            }

            // Local Method
            bool FoodInfraStructureNeeded()
            {
                return LowFoodPotential
                       && Food.FlatBonus < 0.25f
                       && BuildingsCanBuild.Any(b => b.GoodFlatFood());
            }

            // Local Method
            void ShowDebugPriorities()
            {
                if (!IsPlanetExtraDebugTarget())
                    return;

                int rank = GetColonyRank();
                Log.Info($"Planet Rank: {rank} ({ColonyValue.String(0)}/{Owner.MaxColonyValue.String(0)})");
                Log.Info(ConsoleColor.Green, $"**** {Name} - Governor Priorities        ****");
                foreach ((ColonyPriority key, float value) in Priorities.Values)
                    Log.Info($"{key,-16} = {value}");

                Log.Info(ConsoleColor.Green, "---------------------------------------------");
            }
        }

        void CalcFoodPriorities()
        {
            if (IsCybernetic)
                return;

            float foodToFeedAll     = FoodConsumptionPerColonist * (MaxPopulationBillion/3).LowerBound(PopulationBillion);
            float flatFoodToFeedAll = foodToFeedAll - Food.NetFlatBonus;

            float flat   = (flatFoodToFeedAll - EstimatedAverageFood).LowerBound(0);
            float perCol = (foodToFeedAll - EstimatedAverageFood).LowerBound(0) * Fertility;
            if (IsStarving)
            {
                perCol += 3 * Fertility;
                flat   += (3 - Fertility).LowerBound(0);
            }

            perCol += (1 - Storage.FoodRatio) * Fertility;
            flat   += 1 - Storage.FoodRatio;

            if (CType == ColonyType.Agricultural)
                perCol *= Fertility;

            float flatMulti = Food.NetMaxPotential <= 0 ? 4 : 1;
            flat   = ApplyGovernorBonus(flat, 1.25f, 2f, 2f, 2.5f, 2f) * flatMulti;
            perCol = ApplyGovernorBonus(perCol, 1.75f, 0.25f, 0.25f, 4f, 0.25f);
            Priorities[ColonyPriority.FoodFlat]   = flat;
            Priorities[ColonyPriority.FoodPerCol] = perCol;
        }

        void CalcProductionPriorities()
        {
            float netProdPerColonist = Prod.NetYieldPerColonist - ProdConsumptionPerColonist;
            float flatProdToFeedAll  = IsCybernetic ? ConsumptionPerColonist * MaxPopulationBillion - Prod.NetFlatBonus : 0;
            float richnessBonus      = MineralRichness > 0 ? 1 / MineralRichness : 0;

            float flat = NonCybernetic ? (10 - netProdPerColonist - Prod.NetFlatBonus).LowerBound(0)
                                       : (flatProdToFeedAll - netProdPerColonist - Prod.NetFlatBonus).LowerBound(0);

            float richnessMultiplier = NonCybernetic ? 5 : 10;
            float perRichness        = (MineralRichness * richnessMultiplier - Prod.NetFlatBonus * 0.5f).LowerBound(0);
            float perCol             = 10 - netProdPerColonist - flatProdToFeedAll * richnessBonus;
            perCol                   = (perCol * MineralRichness).LowerBound(0);

            if (IsCybernetic && IsStarving)
            {
                perCol      += 2 * MineralRichness;
                flat        += (2 - MineralRichness).LowerBound(0);
                perRichness += 1.5f * MineralRichness;
            }


            flat   += 1 - Storage.ProdRatio;
            perCol += (1 - Storage.ProdRatio) * MineralRichness;
            perCol *= PopulationRatio;

            float maxPotentialThreshold = IsCybernetic ? 2 : 1;
            float flatMulti = (IsCybernetic ? maxPotentialThreshold / Prod.NetMaxPotential.LowerBound(0.1f) : 4).Clamped(4, 20);
            flat        = ApplyGovernorBonus(flat, 1f, 2f, 1f, 1f, 1.5f) * flatMulti;
            perRichness = ApplyGovernorBonus(perRichness, 1f, 2f, 0.5f, 0.5f, 1.5f) * flatMulti;
            perCol      = ApplyGovernorBonus(perCol, 1f, 2f, 0.5f, 0.5f, 1.5f);
            Priorities[ColonyPriority.ProdFlat]        = flat;
            Priorities[ColonyPriority.ProdPerRichness] = perRichness;
            Priorities[ColonyPriority.ProdPerCol]      = perCol;
        }

        void CalcInfrastructurePriority()
        {
            float infra = PopulationBillion / 2 - InfraStructure;
            infra       = ApplyGovernorBonus(infra, 2f, 2.5f, 0.25f, 0.25f, 1.5f);
            Priorities[ColonyPriority.InfraStructure] = infra;
        }

        void CalcPopulationPriorities()
        {
            float empirePopRatio = Owner.TotalPopBillion / Owner.MaxPopBillion;
            float popGrowth = 20 - PopulationRatio*10 - empirePopRatio*10;
            float popCap = 0;

            if (FreeHabitableTiles > 0 && PopulationRatio > 0.5f)
                popCap = (PopulationRatio - 0.5f) * 2;

            popGrowth = ApplyGovernorBonus(popGrowth, 1.5f, 1f, 1f, 1.5f, 1f);
            popCap    = ApplyGovernorBonus(popCap, 1.5f, 1f, 1f, 1f, 1f);
            Priorities[ColonyPriority.PopGrowth] = popGrowth;
            Priorities[ColonyPriority.PopCap]    = popCap;
        }

        void CalcStoragePriorities()
        {
            float storage = NonCybernetic ? (Storage.FoodRatio + Storage.ProdRatio)
                                          : Storage.ProdRatio * 2f;
            storage += (Level / 3f).LowerBound(0.5f);
            storage = ApplyGovernorBonus(storage, 1f, 1f, 0.5f, 1.25f, 1f);
            Priorities[ColonyPriority.StorageNeeds] = storage;
        }

        void CalcResearchPriorities()
        {
            float flat   = 10 - Res.NetFlatBonus;
            float perCol = PopulationBillion - Res.NetYieldPerColonist;
            if (Owner.Research.NoTopic) // no research is needed when not researching
            {
                flat   = 0;
                perCol = 0;
            }
            else
            {
                flat   = ApplyGovernorBonus(flat, 0.8f, 0.2f, 3f, 0.2f, 0.5f);
                perCol = ApplyGovernorBonus(perCol, 1f, 0.1f, 3f, 0.1f, 0.25f);
            }

            Priorities[ColonyPriority.ResearchFlat]   = flat;
            Priorities[ColonyPriority.ResearchPerCol] = perCol;
        }

        void CalcMoneyPriorities()
        {
            if (PopulationBillion < 1)
                return;
             
            float ratio     = 1 - MoneyBuildingRatio;
            float tax       = PopulationBillion * Owner.data.TaxRate * 4 * PopulationRatio * ratio;
            float credits   = PopulationBillion.LowerBound(2) * PopulationRatio * ratio;
            float buildings = TotalHabitableTiles * ratio;

            tax       = ApplyGovernorBonus(tax, 1f, 1f, 0.8f, 1f, 1f);
            credits   = ApplyGovernorBonus(credits, 1.5f, 1f, 1f, 1f, 1f);
            buildings = ApplyGovernorBonus(buildings, 1.5f, 0.5f, 1f, 0.5f, 1f);

            Priorities[ColonyPriority.TaxPercent]     = tax;
            Priorities[ColonyPriority.CreditsPerCol]  = credits;
            Priorities[ColonyPriority.BuildingIncome] = buildings;
        }

        void CalcFertilityPriorities()
        {
            float fertility = NonCybernetic ? 5 - MaxFertility : 0;
            fertility       = ApplyGovernorBonus(fertility, 3f, 0.5f, 1f, 5f, 1f);
            Priorities[ColonyPriority.Fertility] = fertility;
        }

        void CalcSpacePortPriorities()
        {
            float spacePort = PopulationBillion - 2;
            spacePort       = ApplyGovernorBonus(spacePort, 1.5f, 1f, 1f, 1f, 2f);
            Priorities[ColonyPriority.SpacePort] = spacePort;
        }

        float ApplyGovernorBonus(float value, float core, float industrial, float research, float agricultural, float military)
        {
            float multiplier;
            switch (CType)
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

            return value;
        }

        bool SimpleBuild(float budget) // build a building with a positive value
        {
            if (CivilianBuildingInTheWorks)
                return false;

            ChooseBestBuilding(GetBuildingsListToChooseFrom(BuildingsCanBuild), budget, replacing: false, out Building bestBuilding);
            return bestBuilding != null && Construction.Enqueue(bestBuilding);
        }

        bool TryScrapBuilding(bool overBudget,  bool scrapZeroMaintenance = false, bool terraformerOverride = false)
        {
            if (GovernorShouldNotScrapBuilding && !terraformerOverride)
                return false;  // Player decided not to allow governors to scrap buildings and not terraform related

            ChooseWorstBuilding(overBudget, scrapZeroMaintenance, false, out Building toScrap);

            if (toScrap == null)
                return false;

            Log.Info(ConsoleColor.Blue, $"{Owner.PortraitName} SCRAPPED {toScrap.Name} on planet {Name}");
            ScrapBuilding(toScrap); // scrap the worst building we have on the planet
            return true;
        }

        void ReplaceBuilding(float budget, bool overBudget)
        {
            if (BuildingsHereCanBeBuiltAnywhere || BuildingsCanBuild.Count == 0)
                return;

            // Replace works even if the governor is not scrapping buildings, unless they are player built
            float worstBuildingScore = ChooseWorstBuilding(overBudget, scrapZeroMaintenance: true, true, out Building worstBuilding);
            if (worstBuilding == null)
                return;

            float replacementBudget = budget + worstBuilding.ActualMaintenance(this);
            float bestBuildingScore = ChooseBestBuilding(GetBuildingsListToChooseFrom(BuildingsCanBuild), 
                replacementBudget, replacing: true, out Building bestBuilding);

            if (bestBuilding == null)
                return;

            // the best building score should be at least 10 points better than what we are scrapping
            if (bestBuildingScore > worstBuildingScore + 10)
            {
                ScrapBuilding(worstBuilding);
                Construction.Enqueue(bestBuilding);

                Log.Info(ConsoleColor.Green, $"{Owner.PortraitName} Replaced {worstBuilding.Name} " +
                                             $"with {bestBuilding.Name} on planet {Name}");
            }
        }

        public bool TryCancelOverBudgetCivilianBuilding(float budget)
        {
            if (GovernorShouldNotScrapBuilding)
                return false;

            for (int i = 0; i < ConstructionQueue.Count; i++)
            {
                QueueItem qi = ConstructionQueue[i];
                if (Owner.AutoBuildTerraformers && qi.IsCivilianBuilding && qi.Building.IsTerraformer && TerraformBudget == 0)
                {
                    Log.Info(ConsoleColor.Blue, $"{Owner.PortraitName} CANCELED Terrformer" +
                        $" on planet {Name} since Terraformer Budget was 0.");
                    Construction.Cancel(qi);
                    return true;
                }

                if (qi.IsCivilianBuilding 
                    && !qi.Building.IsTerraformer 
                    && !RequiredInBlueprints(qi.Building) 
                    && qi.Building.ActualMaintenance(this) > budget)
                {
                    Log.Info(ConsoleColor.Blue, $"{Owner.PortraitName} CANCELED {qi.Building.Name}" +
                        $" on planet {Name} since maint. ({qi.Building.ActualMaintenance(this)}) was higher than budget ({budget})");
                    Construction.Cancel(qi);
                    return true;
                }
            }

            return false;
        }

        float ChooseBestBuilding(IReadOnlyList<Building> buildings, float budget, bool replacing, out Building best)
        {
            best = null;
            if (buildings.Count == 0)
                return 0;

            if (IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Cyan, $"==== Planet  {Name}  CHOOSE BEST BUILDING, Budget: {budget} ====");


            float highestScore = BuildingScoreThreshold; // Score threshold to build stuff
            float totalProd    = Storage.Prod + IncomingProd;

            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                if (SuitableForBuild(b, budget, buildings))
                {
                    float buildingScore = EvaluateBuilding(b, totalProd, !replacing);
                    if (buildingScore > highestScore)
                    {
                        best = b;
                        highestScore = buildingScore;
                    }
                }
            }

            if (best != null && IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Green, $"-- Planet {Name}: Best Building is {best.Name} " +
                                            $"with score of {highestScore}");

            return highestScore;
        }

        float ChooseWorstBuilding(bool overBudget, bool scrapZeroMaintenance, bool replacing, out Building worst)
        {
            worst = null;
            if (NumBuildings == 0)
                return 0;

            if (IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Red, $"==== Planet  {Name}  CHOOSE WORST BUILDING ====");

            float lowestScore  = float.MaxValue;
            float storageInUse = Storage.MostGoodsInStorage;

            foreach (Building b in Buildings)
            {
                if (!SuitableForScrap(b, overBudget, storageInUse, scrapZeroMaintenance, replacing))
                    continue;

                // Using b.Cost since actually we wont use effectiveness (no production needed)
                float buildingScore = EvaluateBuilding(b, b.Cost, chooseBest: false);
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

        bool SuitableForBuild(Building b, float budget, IReadOnlyList<Building> buildingsCanBuild)
        {
            if (b.IsMilitary
                || b.IsTerraformer
                || b.IsBiospheres  // Different logic for the above
                || OwnerIsPlayer && b.BuildOnlyOnce
                || !OwnerIsPlayer && b.BuildOnlyOnce && Level < (int)DevelopmentLevel.MegaWorld
                // If starving and dont have low prod potential and this building does not produce
                // food while we have food buildings available for build, filter it
                || NonCybernetic && IsStarving && !LowProdPotential && !b.ProducesFood && buildingsCanBuild.Any(f => f.ProducesFood))
            {
                return false;
            }

            float maintenance = b.ActualMaintenance(this);
            return budget > 0 && b.ActualMaintenance(this) <= budget;
        }

        bool SuitableForScrap(Building b, bool overBudget, float storageInUse, bool scrapZeroMaintenance, bool replacing)
        {
            if (b.IsBiospheres
                || b.IsMilitary
                || !b.Scrappable
                || b.IsSpacePort && Owner.GetPlanets().Count == 1 // Dont scrap our last spaceport
                || b.BuildOnlyOnce
                || b.PlusTerraformPoints > 0) // using this instead of IsTerraformer since some event building might also terraform without the terraformer building ID
            {
                return false;
            }

            if (!RequiredInBlueprints(b))
                return true;
            else if (!overBudget)
                return false;

            if (b.IsPlayerAdded && OwnerIsPlayer
                || b.MoneyBuildingAndProfitable(b.ActualMaintenance(this), PopulationBillion)
                || !WillMaintainPositiveFoodOutput(b)
                || !IsBuildingOnHabitableTile(b) && replacing  // Dont allow buildings on non habitable tiles to be scrapped when replacing
                || !scrapZeroMaintenance && b.ActualMaintenance(this).AlmostZero()
                || IsStorageWasted(storageInUse, b.StorageAdded))
            {
                return false;
            }

            return true;
        }

        bool WillMaintainPositiveFoodOutput(Building b)
        {
            if (NonCybernetic && b.ProducesFood && IsStarving
                || IsCybernetic && b.ProducesProduction && IsStarving)
            {
                return false; // Dont scrap food buildings if starving
            }

            if (NonCybernetic && !b.ProducesFood || IsCybernetic && !b.ProducesProduction)
                return true;

            // checking at 80% of max potential considering building fertility or richness changes
            float potential      = 0.8f * (NonCybernetic 
                ? Food.NetMaxPotential * (Fertility - b.MaxFertilityOnBuildFor(Owner, Category) / Fertility.LowerBound(0.01f)) 
                : Prod.NetMaxPotential * MineralRichness - b.IncreaseRichness / MineralRichness.LowerBound(0.01f));

            float pop80          = PopulationBillion * 0.8f;
            float buildingOutput = NonCybernetic 
                ? Food.AfterTax(b.PlusFlatFoodAmount + pop80*ColonyResource.FoodYieldFormula(Fertility, b.PlusFoodPerColonist-1))
                : Prod.AfterTax(b.PlusFlatProductionAmount + pop80 * ColonyResource.ProdYieldFormula(MineralRichness, b.PlusProdPerColonist-1, Owner));

            return potential - buildingOutput > 0;
        }

        bool IsBuildingOnHabitableTile(Building b)
        {
            if (!b.CanBuildAnywhere)
                return true;

            PlanetGridSquare tile = TilesList.Find(t => t.Building == b);
            if (tile == null)
            {
                Log.Warning($"Building {b.Name} was not found on any tile on the planet {Name} when checking if it is on habitable tile");
                return true;
            }

            return tile.Habitable;
        }

        bool IsStorageWasted(float storageInUse, float storageAdded) => Storage.Max - storageAdded < storageInUse;

        // Gretman function, to support DoGoverning()
        float EvaluateBuilding(Building b, float totalProd, bool chooseBest)
        {
            float score = 0;
            if (IsLimitedByPerCol(b) 
                && (!HasBlueprints ||
                    b.IsSuitableForBlueprints && Blueprints.IsNotRequired(b)))
            {
                if (chooseBest && IsPlanetExtraDebugTarget())
                {
                    Log.Info(ConsoleColor.DarkRed, $"Eval BUILD  {b.Name,-33}  {"NOT GOOD",-10} " +
                                                   $"{score.SignString()} {"",3} Has Per Col traits which are not needed.");
                }
            }
            else
            {
                score = CalcBuildingScore(b, totalProd, chooseBest);
            }

            return score;
        }

        float CalcBuildingScore(Building b, float totalProd, bool chooseBest)
        {
            float score = 0;
            score += EvalTraits(Priorities[ColonyPriority.FoodFlat],        b.PlusFlatFoodAmount);
            score += EvalTraits(Priorities[ColonyPriority.FoodPerCol],      b.PlusFoodPerColonist * 3);
            score += EvalTraits(Priorities[ColonyPriority.ProdFlat],        b.PlusFlatProductionAmount * 2);
            score += EvalTraits(Priorities[ColonyPriority.ProdPerCol],      b.PlusProdPerColonist * 2);
            score += EvalTraits(Priorities[ColonyPriority.ProdPerRichness], b.PlusProdPerRichness * 3);
            score += EvalTraits(Priorities[ColonyPriority.ProdPerRichness], b.IncreaseRichness * 50);
            score += EvalTraits(Priorities[ColonyPriority.PopGrowth],       b.PlusFlatPopulation / 10);
            score += EvalTraits(Priorities[ColonyPriority.PopCap],          b.MaxPopIncrease / 200);
            score += EvalTraits(Priorities[ColonyPriority.StorageNeeds],    b.StorageAdded / 100f);
            score += EvalTraits(Priorities[ColonyPriority.ResearchFlat],    b.PlusFlatResearchAmount * 3);
            score += EvalTraits(Priorities[ColonyPriority.ResearchPerCol],  b.PlusResearchPerColonist * 10);
            score += EvalTraits(Priorities[ColonyPriority.CreditsPerCol],   b.CreditsPerColonist * 3);
            score += EvalTraits(Priorities[ColonyPriority.TaxPercent],      b.PlusTaxPercentage * 20);
            score += EvalTraits(Priorities[ColonyPriority.BuildingIncome],  b.Income * 10);
            score += EvalTraits(Priorities[ColonyPriority.Fertility],       b.MaxFertilityOnBuildFor(Owner, Category) * 2);
            score += EvalTraits(Priorities[ColonyPriority.SpacePort],       b.IsSpacePort ? 5 : 0);
            score += EvalTraits(Priorities[ColonyPriority.InfraStructure],  b.Infrastructure * 3);

            score *= FertilityMultiplier(b);

            // When choosing the best building, we all check if that building can be built fast enough
            // If we are evaluating for worst building or choosing a building to replace, we ignore
            // Cost effectiveness, since we want to compare the buildings without cost modifiers
            float effectiveness = chooseBest ? CostEffectivenessMultiplier(b.Cost, totalProd) : 1;
            score *= effectiveness;


            if (RequiredInBlueprints(b))
                score = score.LowerBound(BuildingScoreThreshold + 0.1f);

            if (IsPlanetExtraDebugTarget())
            {
                if (score > 0f)
                    Log.Info(ConsoleColor.Cyan, $"Eval BUILD  {b.Name,-33}  {"SUITABLE",-10} " +
                                                $"{score.SignString()} {"",3} {"Effectiveness:",-13} {effectiveness.String(2)}");
                else
                    Log.Info(ConsoleColor.DarkRed, $"Eval BUILD  {b.Name,-33}  {"NOT GOOD",-10} " +
                                                   $"{score.SignString()} {"",3} {"Effectiveness:",-13} {effectiveness.String(2)}");
            }

            return score;
        }

        float EvalTraits(float priority, float trait) => priority * trait;

        float FertilityMultiplier(Building b)
        {
            if (b.MaxFertilityOnBuild.AlmostZero())
                return 1;

            // Fertility increasing buildings score should be very high in order to be worth building by cybernetics
            if (IsCybernetic)
                return b.MaxFertilityOnBuild > 0 ? 0.2f : 1;

            if (b.MaxFertilityOnBuild < 0 && CType == ColonyType.Agricultural)
                return 0; // Never build fertility reducers on Agricultural colonies

            if (CType == ColonyType.Industrial)
            {
                if (MaxFertility > 1) return 0.5f;
                else                  return 1;
            }

            // Multiplier will be smaller in direct relation to its effect if not Core or not homeworld
            if (b.MaxFertilityOnBuild < 0)
            {
                float threshold;
                switch (CType)
                {
                    case ColonyType.Agricultural: threshold = 3; break;
                    case ColonyType.Core:         threshold = 2; break;
                    default:                      threshold = 1; break;
                }

                if (IsHomeworld)
                    threshold *= 2;

                float projectedMaxFertility = (MaxFertility + b.MaxFertilityOnBuildFor(Owner, Category)).LowerBound(0);
                if (projectedMaxFertility < threshold)
                    return CType == ColonyType.Core || IsHomeworld ? 0 : projectedMaxFertility;
            }

            return 1;
        }

        // Hard limits on multi functional buildings which are useless due to per col needs being 0.
        bool IsLimitedByPerCol(Building b)
        {
            if (Priorities[ColonyPriority.ProdPerCol].AlmostZero() && b.PlusProdPerColonist.Greater(0))         return true;
            if (Priorities[ColonyPriority.FoodPerCol].AlmostZero() && b.PlusFoodPerColonist.Greater(0))         return true;
            if (Priorities[ColonyPriority.ResearchPerCol].AlmostZero() && b.PlusResearchPerColonist.Greater(0)) return true;
            if (Priorities[ColonyPriority.CreditsPerCol].AlmostZero() && b.CreditsPerColonist.Greater(0))       return true;

            return false;
        }

        float CostEffectivenessMultiplier(float cost, float expectedProd)
        {
            // This will allow the colony to slowly build more expensive buildings as it grows
            float multiplier = expectedProd / cost.LowerBound(1);
            return multiplier.LowerBound(0);
        }

        public void UpdateTerraformBudget(ref float currentEmpireBudget, float terraformerMaint)
        {
            TerraformBudget = 0;
            if (HasBlueprints && !Blueprints.OkToBuildTerraformers)
                return;

            if (currentEmpireBudget >= terraformerMaint && Terraformable)
            {
                // This will let each planet to build 1 terraformer at a time and save the budget to other planets
                // As the number of Terraformers here increases, the budget will increase as well.
                float maxLocalBudget = TerraformerLimit * terraformerMaint;
                float budget = ((TerraformersHere + 1) * terraformerMaint).UpperBound(maxLocalBudget);
                TerraformBudget = budget.UpperBound(currentEmpireBudget);
                currentEmpireBudget -= TerraformBudget;
            }
        }

        void TryBuildTerraformers(float budget)
        {
            if (OwnerIsPlayer && !Owner.AutoBuildTerraformers
                || !AreTerraformersNeeded
                || IsStarving
                || TerraformerInTheWorks
                || Owner.data.Traits.TerraformingLevel == 0)
            {
                return;
            }

            Building terraformer   = ResourceManager.GetBuildingTemplate(Building.TerraformerId);
            float terraformerMaint = terraformer.ActualMaintenance(this);
            float remainingBudget  = budget - (TerraformersHere * terraformerMaint);

            if (terraformerMaint <= remainingBudget) // we can build at least 1 terraformer with the budget
            {
                var unHabitableTiles = TilesList.Filter(t => !t.Habitable && t.CanEnqueueBuildingHere(terraformer));
                if (unHabitableTiles.Length > 0) // try to build a terraformer on an uninhabitable tile first
                {
                    PlanetGridSquare tile = PickTileForTerraformer(unHabitableTiles);
                    Construction.Enqueue(terraformer, tile);
                }
                else
                {
                    var freeTiles = TilesList.Filter(t => t.CanEnqueueBuildingHere(terraformer));
                    if (freeTiles.Length > 0) // fall back to free tiles
                    {
                        PlanetGridSquare tile = PickTileForTerraformer(freeTiles);
                        Construction.Enqueue(terraformer, tile);
                    }
                    else if (TryScrapBuilding(true, scrapZeroMaintenance: true, terraformerOverride: true))
                    {
                        // If could not add a terraformer anywhere due to planet being full
                        // try to scrap a building and then retry construction
                        Construction.Enqueue(terraformer);
                    }
                }
            }
        }

        PlanetGridSquare PickTileForTerraformer(PlanetGridSquare[] tileList)
        {
            var potentialTiles = new Array<PlanetGridSquare>();
            for (int i = 0; i < tileList.Length; ++i)
            {
                PlanetGridSquare tile = tileList[i];
                if (NoVolcanosAround(tile))
                    potentialTiles.Add(tile);
            }

            return Random.Item(potentialTiles.Count > 0 ? potentialTiles : tileList.ToArrayList());

            bool NoVolcanosAround(PlanetGridSquare tile)
            {
                PlanetGridSquare.Ping ping = new(tile, 1);
                for (int y = ping.Top; y <= ping.Bottom; ++y)
                {
                    for (int x = ping.Left; x <= ping.Right; ++x)
                    {
                        PlanetGridSquare checkedTile = TilesList[x + y * ping.Width];
                        if (checkedTile.VolcanoHere)
                            return false;
                    }
                }

                return true;
            }
        }

        bool AreTerraformersNeeded
        {
            get
            {
                if (!Terraformable || TerraformersHere >= TerraformerLimit)
                    return false;

                if (TilesList.Any(t => t.CanTerraform)
                    || TilesList.Any(t => t.BioCanTerraform || t.VolcanoHere)
                    || Category != Owner.data.PreferredEnvPlanet
                    || NonCybernetic && BaseMaxFertility.Less(1 / Empire.RacialEnvModifer(Category, Owner)))
                {
                    return true;
                }

                return false;
            }
        }

        void TryScrapBiospheres()
        {
            if (OwnerIsPlayer && GovernorShouldNotScrapBuilding || !HasBuilding(b => b.IsBiospheres && !b.IsPlayerAdded))
                return;

            var potentialBio = TilesList.Filter(
                t => t.Biosphere && (t.NoBuildingOnTile)
            );
            if (potentialBio.Length == 0)
                return;

            PlanetGridSquare tile = Random.Item(potentialBio.Sorted(t => t.Building?.ActualCost(Owner) ?? 0));
            if (!tile.Building?.CanBuildAnywhere == true)
                ScrapBuilding(tile.Building, tile);

            DestroyBioSpheres(tile, false);
        }

        bool TryBuildBiospheres(float budget, out bool shouldScrapBioSpheres)
        {
            shouldScrapBioSpheres = false;
            if (!Owner.IsBuildingUnlocked(Building.BiospheresId)
                || BiosphereInTheWorks
                || IsStarving
                || HabitablePercentage.AlmostEqual(1)) // all tiles are habitable
            {
                return false;
            }

            Building bio = ResourceManager.GetBuildingTemplate(Building.BiospheresId);
            if (bio == null || bio.ActualMaintenance(this) > budget)
                return false; // not within budget or not profitable and more than 5

            if (!BioSphereProfitable(bio))
            {
                int numBuildingsWeCanBuild = GetBuildingsListToChooseFrom(BuildingsCanBuild).Count;
                if (NumFreeBiospheres > 0)
                {
                    // We do not need more than 1 free biospheres if not profitable.
                    // We need only 1 free biosphere if we have anything to built at all
                    shouldScrapBioSpheres = NumFreeBiospheres > 1 
                        || numBuildingsWeCanBuild == 0 && (!HasBlueprints || Blueprints.IsAchievableCompleted);
                    return false;
                }
                else if (numBuildingsWeCanBuild == 0 || HabiableBuiltCoverage.Less(1))
                {
                    // no need to build unprofitable biospheres if we have nothing to build here
                    return false;
                }
            }
            else if (PopulationRatio < 0.95f 
                     && (NumFreeBiospheres > 0 || HabiableBuiltCoverage.Less(1)))
            {
                // dont build even if profitable, if pop is not big enough.
                // but ensure there is at least 1 free biospheres if there are no free tiles
                return false;
            }


            if (IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Green, $"{Owner.PortraitName} BUILT {bio.Name} on planet {Name}");

            return Construction.Enqueue(bio, GetPreferredTile()); // Preferred is null safe in this call

            PlanetGridSquare GetPreferredTile()
            {
                PlanetGridSquare preferred = null;
                if (Owner.IsBuildingUnlocked(Building.TerraformerId))
                {
                    preferred = TilesList.Find(t => !t.Habitable && !t.Terraformable && !t.BuildingOnTile);
                    if (preferred == null)
                        preferred = TilesList.Find(t => !t.Habitable && !t.Terraformable);
                }
                else
                {
                    preferred = TilesList.Find(t => !t.Habitable && !t.BuildingOnTile);
                    if (preferred == null)
                        preferred = TilesList.Find(t => !t.Habitable);
                }

                return preferred;
            }
        }

        bool BioSphereProfitable(Building bio)
        {
            return Money.NetRevenueGain(bio) >= 0;
        }

        void TryBuildDysonSwarmControllers()
        {
            if (System.EmpireOwnsDysonSwarm(Owner)
                && System.DysonSwarm.ShouldBuildMoreSwarmControllers
                && System.DysonSwarm.TryGetAvailablePosForController(out Vector2 pos))
            {
                Owner.AI.AddGoalAndEvaluate(new BuildConstructionShip(pos, DysonSwarm.DysonSwarmControllerName, Owner));
            }
        }

        // FB - For unit tests only!
        public bool TestIsCapitalInQueue() => ConstructionQueue.Any(q => q.isBuilding && q.Building.IsCapital);
        public bool TestIsOutpostInQueue() => ConstructionQueue.Any(q => q.isBuilding && q.Building.IsOutpost);
        
        bool OutpostOrCapitalBuiltOrInQueue(bool checkExisting = true)
        {
            // First check the existing buildings
            if (checkExisting && HasBuilding(b => b.IsCapitalOrOutpost))
                return true;

            // Then check the queue
            return ConstructionQueue.Any(q => q.isBuilding && q.Building.IsCapitalOrOutpost);
        }

        public bool RemoveCapital()
        {
            SetHomeworld(false);
            if (Construction.Cancel(ResourceManager.CreateBuilding(this, Building.CapitalId)))
                return true;

            Building capital = FindBuilding(b => b.IsCapital);
            if (capital != null)
            {
                ScrapBuilding(capital);
                return true;
            }

            return false;
        }

        public void RemoveOutpost()
        {
            QueueItem underConstruction = ConstructionQueue.Find(q => q.isBuilding && q.Building.IsOutpost);
            if (underConstruction != null)
                Construction.Cancel(underConstruction);

            Building outpost = FindBuilding(b => b.IsOutpost);
            if (outpost != null)
                ScrapBuilding(outpost);
        }

        /// <summary>
        /// Sets the homeworld and builds the capital
        /// If its the original empire's capital world, it will try to remove a capital from a non capital planet
        /// to here
        /// </summary>
        public void BuildCapitalHere()
        {
            SetHomeworld(true);
            BuildOutpostOrCapitalIfAble(checkExisting: false);
        }

        void BuildOutpostOrCapitalIfAble(bool checkExisting = true) // A Gretman function to support DoGoverning()
        {
            // Check Existing Buildings and the queue
            if (OutpostOrCapitalBuiltOrInQueue(checkExisting))
                return;

            // Build it!
            int id = IsHomeworld ? Building.CapitalId : Building.OutpostId;
            Construction.Enqueue(ResourceManager.CreateBuilding(this, id));

            // Move Outpost to the top of the list
            for (int i = 1; i < ConstructionQueue.Count; ++i)
            {
                QueueItem q = ConstructionQueue[i];
                if (q.isBuilding && q.Building.IsCapitalOrOutpost)
                {
                    Construction.MoveTo(0, i);
                    break;
                }
            }
        }

        void PrioritizeCriticalFoodBuildings()
        {
            if (IsCybernetic || !IsStarving || PlayerAddedFirstConstructionItem)
                return;

            for (int i = 1; i < ConstructionQueue.Count; ++i)
            {
                QueueItem q = ConstructionQueue[i];
                if (q.isBuilding)
                {
                    if (q.Building.ProducesFood)
                    {
                        Construction.MoveTo(0, i);
                        Construction.RushProduction(0, 10);
                        break;
                    }

                    // Cancel ongoing building if there is a food building available for build and not player added
                    if (!q.IsPlayerAdded && !q.Building.IsMilitary && BuildingsCanBuild.Any(f => f.ProducesFood))
                    {
                        Construction.Cancel(q);
                        break;
                    }
                }
            }
        }

        public float PrioritizeColonyBuilding(Building b)
        {
            if (b == null)
            {
                Log.Warning($"PrioritizeColonyBuilding - building was null in planet{Name}!");
                return 5000;
            }

            switch (CType)
            {
                case ColonyType.Agricultural when b.ProducesFood && b.MaxFertilityOnBuild > 0:
                case ColonyType.Industrial   when b.ProducesProduction:
                case ColonyType.Research     when b.ProducesResearch:
                case ColonyType.Military     when b.IsMilitary:  return 0;
                case ColonyType.Core         when !b.IsMilitary: return NumBuildings * 0.05f;
            }

            return NumBuildings * (b.IsTerraformer ? 0.25f : 0.1f);
        }

        void PrioritizeCriticalProductionBuildings()
        {
            if (Owner.data.TaxRate > 0.4 
                || PlayerAddedFirstConstructionItem 
                || Prod.NetIncome > 1 && ConstructionQueue.Count <= Level)
            {
                return;
            }

            for (int i = 0; i < ConstructionQueue.Count; ++i)
            {
                QueueItem q = ConstructionQueue[i];
                if (q.isBuilding && q.Building.ProducesProduction)
                {
                    Construction.MoveTo(0, i);
                    Construction.RushProduction(0, 10);
                    break;
                }
            }
        }
    }
}