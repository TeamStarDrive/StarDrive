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
            InfraStructure
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
                TryScrapBuilding(); // We must scrap something to bring us above of our debt tolerance
            }
            else
            {
                TryBuildTerraformers(budget); // Build Terraformers if needed
                BuildOrReplaceBuilding(budget);
                if (!TryBuildBiospheres(budget, out bool shouldScrapBiospheres) && shouldScrapBiospheres)
                    TryScrapBiospheres(); // Build or scrap Biospheres if needed
            }
        }

        void BuildOrReplaceBuilding(float budget)
        {
            if (FreeHabitableTiles > 0)
            {
                SimpleBuild(budget); // Let's try to build something within our budget
                if (TryPrioritizeColonyBuilding())
                    return;
            }
            else
            {
                ReplaceBuilding(budget); // We don't have room for expansion. Let's see if we can replace to a better value building
            }

            PrioritizeCriticalFoodBuildings();
            PrioritizeCriticalProductionBuildings();
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
            CalcInfrastructurePriority();

            if (IsPlanetExtraDebugTarget())
            {
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

            float foodToFeedAll     = FoodConsumptionPerColonist * PopulationBillion * 1.5f;
            float flatFoodToFeedAll = foodToFeedAll - Food.NetFlatBonus;
            float fertilityBonus    = Fertility.InRange(0.1f, 0.99f) ? 1 : Fertility;

            float flat   = (flatFoodToFeedAll - EstimatedAverageFood).LowerBound(0);
            float perCol = (foodToFeedAll - EstimatedAverageFood).LowerBound(0) * fertilityBonus;
            if (IsStarving)
            {
                perCol += 3 * fertilityBonus;
                flat   += (3 - Fertility).LowerBound(0);
            }

            perCol += (1 - Storage.FoodRatio) * fertilityBonus;
            flat   += 1 - Storage.FoodRatio;

            if (colonyType == ColonyType.Agricultural)
                perCol *= fertilityBonus;


            flat   = ApplyGovernorBonus(flat, 0.5f, 2f, 2f, 2.5f, 1f);
            perCol = ApplyGovernorBonus(perCol, 1.75f, 0.25f, 0.25f, 3f, 0.25f);
            Priorities[ColonyPriority.FoodFlat]   = flat;
            Priorities[ColonyPriority.FoodPerCol] = perCol;
        }

        void CalcProductionPriorities()
        {
            float netProdPerColonist = Prod.NetYieldPerColonist - ProdConsumptionPerColonist;
            float flatProdToFeedAll  = IsCybernetic ? ConsumptionPerColonist * PopulationBillion - Prod.NetFlatBonus : 0;
            float richnessBonus      = MineralRichness > 0 ? 1 / MineralRichness : 0;

            float flat = NonCybernetic ? (10 - netProdPerColonist - Prod.NetFlatBonus).LowerBound(0)
                                       : (flatProdToFeedAll - netProdPerColonist - Prod.NetFlatBonus).LowerBound(0);

            float richnessMultiplier = NonCybernetic ? 5 : 10;
            float perRichness        = (MineralRichness * richnessMultiplier - Prod.NetFlatBonus / 2).LowerBound(0);
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

            flat        = ApplyGovernorBonus(flat, 1f, 2f, 0.5f, 0.5f, 1.5f);
            perRichness = ApplyGovernorBonus(perRichness, 1f, 2f, 0.5f, 0.5f, 1.5f);
            perCol      = ApplyGovernorBonus(perCol, 1f, 2f, 0.5f, 0.5f, 1.5f);
            Priorities[ColonyPriority.ProdFlat]        = flat;
            Priorities[ColonyPriority.ProdPerRichness] = perRichness;
            Priorities[ColonyPriority.ProdPerCol]      = perCol;
        }

        void CalcInfrastructurePriority()
        {
            float infra = PopulationBillion / 2 - BuildingList.Sum(b => b.Infrastructure);
            infra       = ApplyGovernorBonus(infra, 2f, 2.5f, 0.25f, 0.25f, 1.5f);
            Priorities[ColonyPriority.InfraStructure] = infra;
        }

        void CalcPopulationPriorities()
        {
            float eatableRatio = IsCybernetic ? Storage.ProdRatio : Storage.FoodRatio;
            float popGrowth    = (10 - PopulationRatio * 10).LowerBound(0);
            popGrowth   *= eatableRatio;
            float popCap = FreeHabitableTiles > 0 ? (PopulationRatio * 10).Clamped(0, 10) : 0;
            popGrowth    = ApplyGovernorBonus(popGrowth, 1f, 1f, 1f, 1f, 1f);
            popCap       = ApplyGovernorBonus(popCap, 1f, 1f, 1f, 1f, 1f);
            Priorities[ColonyPriority.PopGrowth] = popGrowth;
            Priorities[ColonyPriority.PopCap]    = popCap;
        }

        void CalcStoragePriorities()
        {
            float storage = NonCybernetic ? (Storage.FoodRatio + Storage.ProdRatio) * 2.5f
                                          : Storage.ProdRatio * 5f;
            storage += (Level / 2f).LowerBound(0.5f);
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
                flat   = ApplyGovernorBonus(flat, 0.8f, 0.2f, 2f, 0.2f, 0.25f);
                perCol = ApplyGovernorBonus(perCol, 1f, 0.1f, 2f, 0.1f, 0.1f);
            }

            Priorities[ColonyPriority.ResearchFlat]   = flat;
            Priorities[ColonyPriority.ResearchPerCol] = perCol;
        }

        void CalcMoneyPriorities()
        {
            float ratio   = 1 - MoneyBuildingRatio;
            float tax     = PopulationBillion * Owner.data.TaxRate * 4 * PopulationRatio * ratio;
            float credits = PopulationBillion.LowerBound(2) * PopulationRatio * ratio;

            tax     = ApplyGovernorBonus(tax, 1f, 1f, 0.8f, 1f, 1f);
            credits = ApplyGovernorBonus(credits, 1.5f, 1f, 1f, 1f, 1f);
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
            float spacePort = PopulationBillion - 2;
            spacePort       = ApplyGovernorBonus(spacePort, 1.5f, 1f, 1f, 1f, 2f);
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

            return value;
        }

        bool SimpleBuild(float budget) // build a building with a positive value
        {
            if (CivilianBuildingInTheWorks)
                return false;

            ChooseBestBuilding(BuildingsCanBuild, budget, out Building bestBuilding);
            return bestBuilding != null && Construction.Enqueue(bestBuilding);
        }

        bool TryScrapBuilding(bool scrapZeroMaintenance = false)
        {
            if (GovernorShouldNotScrapBuilding)
                return false;  // Player decided not to allow governors to scrap buildings

            ChooseWorstBuilding(BuildingList, scrapZeroMaintenance, false, out Building toScrap);

            if (toScrap == null)
                return false;

            Log.Info(ConsoleColor.Blue, $"{Owner.PortraitName} SCRAPPED {toScrap.Name} on planet {Name}");
            ScrapBuilding(toScrap); // scrap the worst building we have on the planet
            return true;
        }

        void ReplaceBuilding(float budget)
        {
            if (BuildingsHereCanBeBuiltAnywhere || BuildingsCanBuild.Count == 0)
                return;

            // Replace works even if the governor is not scrapping buildings, unless they are player built
            float worstBuildingScore = ChooseWorstBuilding(BuildingList, scrapZeroMaintenance: true, true, out Building worstBuilding);
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
                Construction.Enqueue(bestBuilding);

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
            float totalProd    = Storage.Prod + IncomingProd + Prod.NetIncome.LowerBound(0);

            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                if (!SuitableForBuild(b, budget))
                    continue;

                float buildingScore = EvaluateBuilding(b, totalProd);
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

        float ChooseWorstBuilding(Array<Building> buildings, bool scrapZeroMaintenance, bool replacing, out Building worst)
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
                if (!SuitableForScrap(b, storageInUse, scrapZeroMaintenance, replacing))
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

        bool SuitableForBuild(Building b, float budget)
        {
            if (b.IsMilitary
                || b.IsTerraformer
                || b.IsBiospheres  // Different logic for the above
                || Owner.isPlayer && b.BuildOnlyOnce
                || !Owner.isPlayer && b.BuildOnlyOnce && Level < (int)DevelopmentLevel.MegaWorld
                // If starving and this buildings does not produce food while we have food buildings available for build, filter it
                || NonCybernetic && IsStarving && !b.ProducesFood && BuildingsCanBuild.Any(f => f.ProducesFood))
            {
                return false;
            }

            float maintenance = b.ActualMaintenance(this);
            if (maintenance < budget || b.IsMoneyBuilding && b.MoneyBuildingAndProfitable(maintenance, PopulationBillion))
                return true;

            return false; // Too expensive for us and its not getting profitable juice from the population
        }

        bool SuitableForScrap(Building b, bool replacing)
        {
            if (b.IsBiospheres
                || !b.Scrappable
                || b.IsPlayerAdded && Owner.isPlayer
                || b.IsTerraformer
                || b.IsMilitary
                || b.BuildOnlyOnce
                || b.MoneyBuildingAndProfitable(b.ActualMaintenance(this), PopulationBillion)
                || IsStarving && b.ProducesFood && NonCybernetic // Dont scrap food buildings when starving
                || b.IsSpacePort && Owner.GetPlanets().Count == 1 // Dont scrap our last spaceport
                || !IsBuildingOnHabitableTile(b) && replacing)  // Dont allow buildings on non habitable tiles to be scrapped when replacing
            {
                return false;
            }

            return true;
        }

        bool SuitableForScrap(Building b, float storageInUse, bool scrapZeroMaintenance, bool replacing)
        {
            if (!SuitableForScrap(b, replacing) || !scrapZeroMaintenance && b.ActualMaintenance(this).AlmostZero())
                return false;

            return !IsStorageWasted(storageInUse, b.StorageAdded);
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
        float EvaluateBuilding(Building b, float totalProd, bool chooseBest = true)
        {
            float score = 0;
            if (IsLimitedByPerCol(b))
            {
                if (IsPlanetExtraDebugTarget())
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
            score += EvalTraits(Priorities[ColonyPriority.FoodPerCol],      b.PlusFoodPerColonist);
            score += EvalTraits(Priorities[ColonyPriority.ProdFlat],        b.PlusFlatProductionAmount);
            score += EvalTraits(Priorities[ColonyPriority.ProdPerCol],      b.PlusProdPerColonist);
            score += EvalTraits(Priorities[ColonyPriority.ProdPerRichness], b.PlusProdPerRichness);
            score += EvalTraits(Priorities[ColonyPriority.PopGrowth],       b.PlusFlatPopulation / 10);
            score += EvalTraits(Priorities[ColonyPriority.PopCap],          b.MaxPopIncrease / 200);
            score += EvalTraits(Priorities[ColonyPriority.StorageNeeds],    b.StorageAdded / 50f);
            score += EvalTraits(Priorities[ColonyPriority.ResearchFlat],    b.PlusFlatResearchAmount * 3);
            score += EvalTraits(Priorities[ColonyPriority.ResearchPerCol],  b.PlusResearchPerColonist * 10);
            score += EvalTraits(Priorities[ColonyPriority.CreditsPerCol],   b.CreditsPerColonist * 3);
            score += EvalTraits(Priorities[ColonyPriority.TaxPercent],      b.PlusTaxPercentage * 20);
            score += EvalTraits(Priorities[ColonyPriority.Fertility],       b.MaxFertilityOnBuildFor(Owner, Category) * 2);
            score += EvalTraits(Priorities[ColonyPriority.SpacePort],       b.IsSpacePort ? 5 : 0);
            score += EvalTraits(Priorities[ColonyPriority.InfraStructure],  b.Infrastructure * 3);

            score *= FertilityMultiplier(b);
            float effectiveness = chooseBest ? CostEffectivenessMultiplier(b.Cost, totalProd) : 1;
            score *= effectiveness;

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
                return b.MaxFertilityOnBuild > 0 ? 0.25f : 1;

            if (b.MaxFertilityOnBuild < 0 && colonyType == ColonyType.Agricultural)
                return 0; // Never build fertility reducers on Agricultural colonies

            float projectedMaxFertility = MaxFertility + b.MaxFertilityOnBuildFor(Owner, Category);
            if (projectedMaxFertility < 1)
            {
                // Multiplier will be smaller in direct relation to its effect if not Core
                if (colonyType == ColonyType.Core)
                    return colonyType == ColonyType.Core ? 0 : projectedMaxFertility.LowerBound(0);
            }

            return 1;
        }

        // Hard limits on mulfi functional buildings which are useless due to per col needs being 0.
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
            if (expectedProd >= cost)
                return 1;

            // This will allow the colony to slowly build more expensive buildings as it grows
            float multiplier = expectedProd / cost.LowerBound(1);
            return multiplier.Clamped(0, 1);
        }

        void TryBuildTerraformers(float budget)
        {
            if (PopulationRatio < 0.95f
                || HabitableBuiltCoverage < 0.5f
                || !AreTerraformersNeeded
                || IsStarving
                || TerraformerInTheWorks)
            {
                return;
            }

            Building terraformer = ResourceManager.GetBuildingTemplate(Building.TerraformerId);
            if (terraformer.ActualMaintenance(this) > budget)
                return;

            var unHabitableTiles = TilesList.Filter(t => !t.Habitable && !t.BuildingOnTile);
            if (unHabitableTiles.Length > 0) // try to build a terraformer on an unhabitable tile first
            {
                PlanetGridSquare tile = TilesList.First(t => !t.Habitable && !t.BuildingOnTile);
                Construction.Enqueue(terraformer, tile);
            }
            else if (!Construction.Enqueue(terraformer))
            {
                // If could not add a terraformer anywhere due to planet being full
                // try to scrap a building and then retry construction
                if (TryScrapBuilding(scrapZeroMaintenance: true))
                    Construction.Enqueue(terraformer);
            }
        }

        bool AreTerraformersNeeded
        {
            get
            {
                if (!Owner.IsBuildingUnlocked(Building.TerraformerId) || TerraformersHere >= TerraformerLimit)
                    return false;

                if (TilesList.Any(t => t.CanTerraform)
                    || TilesList.Any(t => t.BioCanTerraform || t.VolcanoHere)
                    || Category != Owner.data.PreferredEnv
                    || NonCybernetic && BaseMaxFertility.Less(1 / Empire.RacialEnvModifer(Category, Owner)))
                {
                    return true;
                }

                return false;
            }
        }

        void TryScrapBiospheres()
        {
            var potentialBio = TilesList.Filter(t => t.Biosphere 
                                                     && (t.NoBuildingOnTile 
                                                         || t.Building.IsMilitary && !t.Building.IsPlayerAdded));
            if (potentialBio.Length == 0)
                return;

            PlanetGridSquare tile = potentialBio.Sorted(t => t.Building?.ActualCost ?? 0).RandItem();
            if (!tile.Building?.CanBuildAnywhere == true)
                ScrapBuilding(tile.Building, tile);

            DestroyBioSpheres(tile, false);
        }

        bool TryBuildBiospheres(float budget, out bool shouldScrapBioSpheres)
        {
            shouldScrapBioSpheres = false;

            if (!Owner.IsBuildingUnlocked(Building.BiospheresId)
                || PopulationRatio < 0.9f && HabitableBuiltCoverage.Less(1)
                || BiosphereInTheWorks
                || IsStarving
                || HabitablePercentage.AlmostEqual(1)) // all tiles are habitable
            {
                return false;
            }

            Building bio = ResourceManager.GetBuildingTemplate(Building.BiospheresId);
            if (bio == null || bio.ActualMaintenance(this) > budget)
                return false; // not within budget or not profitable and more than 5

            if  (!BioSphereProfitable(bio))
            {
                int numBio = BuildingList.Filter(b => b.IsBiospheres).Length;
                if (numBio > 5)
                    shouldScrapBioSpheres = true;

                if (numBio >= 5)
                    return false;
            }

            if (IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Green, $"{Owner.PortraitName} BUILT {bio.Name} on planet {Name}");


            PlanetGridSquare preferred = Owner.IsBuildingUnlocked(Building.TerraformerId) 
                ? TilesList.Find(t => !t.Habitable && !t.Terraformable)
                : null;

            return Construction.Enqueue(bio, preferred); // Preferred is null safe in this call
        }

        bool BioSphereProfitable(Building bio)
        {
            return Money.NetRevenueGain(bio).GreaterOrEqual(0);
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
            Construction.Enqueue(ResourceManager.CreateBuilding(Building.OutpostId));

            // Move Outpost to the top of the list
            for (int i = 1; i < ConstructionQueue.Count; ++i)
            {
                QueueItem q = ConstructionQueue[i];
                if (q.isBuilding && q.Building.IsOutpost)
                {
                    Construction.MoveTo(0, i);
                    break;
                }
            }
        }

        void PrioritizeCriticalFoodBuildings()
        {
            if (IsCybernetic || !IsStarving)
                return;

            if (PlayerAddedFirstConstructionItem)
                return; // Do not prioritize over player added queue

            for (int i = 1; i < ConstructionQueue.Count; ++i)
            {
                QueueItem q = ConstructionQueue[i];
                if (q.isBuilding)
                {
                    if (q.Building.ProducesFood)
                    {
                        Construction.MoveTo(0, i);
                        Construction.RushProduction(0, 10, true);
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

        bool TryPrioritizeColonyBuilding()
        {
            // This will prioritize important buildings based on colony type to be 2nd in queue, if possible.
            if (ConstructionQueue.Count <= 2 || PlayerAddedFirstConstructionItem)
                return false;

            for (int i = ConstructionQueue.Count - 1; i > 1; --i)
            {
                QueueItem q = ConstructionQueue[i];
                if (q.isBuilding && !q.Building.IsBiospheres)
                {
                    switch (colonyType)
                    {
                        case ColonyType.Core:
                        case ColonyType.Agricultural when q.Building.ProducesFood:
                        case ColonyType.Industrial   when q.Building.ProducesProduction:
                        case ColonyType.Research     when q.Building.ProducesResearch:
                        case ColonyType.Military     when q.Building.IsMilitary: Construction.MoveTo(1, i); return true;
                    }
                }
            }

            return false;
        }

        void PrioritizeCriticalProductionBuildings()
        {
            if (Prod.NetIncome > 1 && ConstructionQueue.Count <= Level)
                return;

            if (PlayerAddedFirstConstructionItem)
                return; // Do not prioritize over player added queue

            for (int i = 0; i < ConstructionQueue.Count; ++i)
            {
                QueueItem q = ConstructionQueue[i];
                if (q.isBuilding && q.Building.ProducesProduction)
                {
                    Construction.MoveTo(0, i);
                    Construction.RushProduction(0, 10, true);
                    break;
                }
            }
        }
    }
}