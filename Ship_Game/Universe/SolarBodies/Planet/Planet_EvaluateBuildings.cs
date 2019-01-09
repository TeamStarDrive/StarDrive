using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public partial class Planet
    {
        float EvaluateBuildingScrapWeight(Building building, float income)
        {
            float score = 0;
            if (building.Maintenance != 0)
            {
                score += building.Maintenance;
                score -= Owner.data.FlatMoneyBonus * 0.015f;      //Acceptible loss (Note what this will do at high Difficulty)

                //This is where the logic for how bad the planet is doing will go + the value of this planet to the empire and all that.
                //For now, just testing with base of just being able to justify its own Maintenance cost

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} Maintenance : Score was {-score}");
            }
            return score;
        }

        float EvaluateBuildingMaintenance(Building building, float income)
        {
            float score = 0;
            if (building.Maintenance != 0)
            {
                score += building.Maintenance * 2;  //Base of 2x maintenance -- Also, I realize I am not calculating MaintMod here. It throws the algorithm off too much
                if (income < building.Maintenance + building.Maintenance * Owner.data.Traits.MaintMod)
                    score += score + (building.Maintenance + building.Maintenance * Owner.data.Traits.MaintMod);   //Really dont want this if we cant afford it
                score -= Owner.data.FlatMoneyBonus * 0.015f;      //Acceptible loss (Note what this will do at high Difficulty)

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} Maintenance : Score was {-score}");
            }

            return score;
        }

        float EvaluateBuildingFlatFood(Building building)
        {
            float score = 0;
            if (building.PlusFlatFoodAmount != 0 && NonCybernetic)
            {
                if (building.PlusFlatFoodAmount < 0) score = building.PlusFlatFoodAmount * 2;   //For negative Flat Food (those crazy modders...)
                else
                {
                    float farmers = CalculateFoodWorkers();
                    score += ((building.PlusFlatFoodAmount / MaxPopulationBillion) * 1.5f).Clamped(0.0f, 1.5f);   //Percentage of population this will feed, weighted
                    score += 1.5f - (Food.YieldPerColonist/2);//Bonus for low Effective Fertility
                    if (farmers == 0) score += building.PlusFlatFoodAmount; //Bonus if planet is relying on flat food
                    if (farmers > 0.5f) score += farmers - 0.5f;            //Bonus if planet is spending a lot of labor feeding itself
                    if (score < building.PlusFlatFoodAmount * 0.1f) score = building.PlusFlatFoodAmount * 0.1f; //A little flat food is always useful
                    if (building.PlusFlatFoodAmount + Food.FlatBonus - 0.5f > MaxPopulationBillion) score = 0;   //Dont want this if a lot would go to waste
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} FlatFood : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingScrapFlatFood(Building building)
        {
            float score = 0;
            if (building.PlusFlatFoodAmount != 0 && NonCybernetic)
            {
                if (building.PlusFlatFoodAmount < 0) score = building.PlusFlatFoodAmount * 2;   //For negative Flat Food (those crazy modders...)
                else
                {
                    float projectedFarmers = CalculateFoodWorkers(pFlatFood: -building.PlusFlatFoodAmount);
                    score += ((building.PlusFlatFoodAmount / MaxPopulationBillion) * 1.5f).Clamped(0.0f, 1.5f);   //Percentage of population this is feeding, weighted
                    score += 1.5f - (Food.YieldPerColonist/2);         //Bonus for low Effective Fertility
                    if (projectedFarmers == 0) score += building.PlusFlatFoodAmount; //Bonus if planet is relying on flat food
                    if (projectedFarmers > 0.5f) score += projectedFarmers - 0.5f;   //Bonus if planet would be spending a lot of labor feeding itself
                    if (score < 0) score = 0;                                        //No penalty for extra food
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated SCRAP of {building.Name} FlatFood : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingFoodPerCol(Building building)
        {
            float score = 0;
            if (building.PlusFoodPerColonist != 0 && NonCybernetic)
            {
                score = 0;
                if (building.PlusFoodPerColonist < 0) score = building.PlusFoodPerColonist * MaxPopulationBillion * 2; //for negative value
                else
                {
                    float projectedFarmers = CalculateFoodWorkers(pFoodPerCol: building.PlusFoodPerColonist);
                    score += building.PlusFoodPerColonist * projectedFarmers * MaxPopulationBillion;  //Food this would create if constructed
                    if (score < building.PlusFoodPerColonist * 0.1f) score = building.PlusFoodPerColonist * 0.1f; //A little food production is always useful
                    if (building.PlusFoodPerColonist + Food.YieldPerColonist <= 1.0f) score = 0;     //Dont try to add farming to a planet without enough to sustain itself
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} FoodPerCol : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingScrapFoodPerCol(Building building)
        {
            float score = 0;
            if (building.PlusFoodPerColonist != 0 && NonCybernetic)
            {
                score = 0;
                if (building.PlusFoodPerColonist < 0) score = building.PlusFoodPerColonist * MaxPopulationBillion * 2; //for negative value
                else
                {
                    score += building.PlusFoodPerColonist * CalculateFoodWorkers() * MaxPopulationBillion;  //Food this is producing
                    if (score < building.PlusFoodPerColonist * 0.1f) score = building.PlusFoodPerColonist * 0.1f; //A little food production is always useful
                    if (Food.YieldPerColonist - building.PlusFoodPerColonist <= 1.0f) score = building.PlusFoodPerColonist;     //Dont scrap this if it would drop effective fertility below 1.0
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} FoodPerCol : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingFlatProd(Building building)
        {
            float score = 0;
            if (building.PlusFlatProductionAmount != 0)
            {
                if (building.PlusFlatProductionAmount < 0) score = building.PlusFlatProductionAmount * 2; //for negative value
                else
                {
                    if (IsCybernetic)
                        score += building.PlusFlatProductionAmount / MaxPopulationBillion;     //Percentage of the filthy Opteris population this will feed
                    score += (0.5f - (PopulationBillion / MaxPopulationBillion)).Clamped(0.0f, 0.5f);   //Bonus if population is currently less than half of max population
                    score += 1.5f - (Prod.YieldPerColonist);      //Bonus for low richness planets
                    score += (0.66f - MineralRichness).Clamped(0.0f, 0.66f);      //More Bonus for really low richness planets
                    float currentOutput = Prod.YieldPerColonist * LeftoverWorkers() + Prod.FlatBonus;  //Current Prod Output
                    score += (building.PlusFlatProductionAmount / currentOutput).Clamped(0.0f, 2.0f);         //How much more this building will produce compared to labor prod
                    if (score < building.PlusFlatProductionAmount * 0.1f) score = building.PlusFlatProductionAmount * 0.1f; //A little production is always useful
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} FlatProd : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingProdPerCol(Building building)
        {
            float score = 0;
            if (building.PlusProdPerColonist != 0 && PopulationBillion / MaxPopulationBillion >= 0.8f)
            {
                if (building.PlusProdPerColonist < 0) score = building.PlusProdPerColonist * MaxPopulationBillion * 2;
                else
                {
                    score += building.PlusProdPerColonist * LeftoverWorkers();    //Prod this building is contributing
                    if (score < building.PlusProdPerColonist * 0.1f) score = building.PlusProdPerColonist * 0.1f; //A little production is always useful
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} ProdPerCol : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingProdPerRichness(Building building)
        {
            float score = 0;
            if (building.PlusProdPerRichness != 0)  //This one can produce a pretty high building value, which is normally offset by its huge maintenance cost and Fertility loss
            {
                if (building.PlusProdPerRichness < 0) score = building.PlusProdPerRichness * MineralRichness * 2;
                else
                {
                    score += building.PlusProdPerRichness * MineralRichness;        //Production this would generate
                    if (!HasShipyard) score *= 0.75f;       //Do we have a use for all this production?
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} ProdPerRich : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingStorage(Building building)
        {
            float score = 0;
            if (building.StorageAdded != 0)
            {
                float desiredStorage = 70.0f;
                if (Food.YieldPerColonist >= 2.5f || Prod.YieldPerColonist >= 2.5f || Prod.FlatBonus > 5) desiredStorage += 100.0f;  //Potential high output
                if (HasShipyard) desiredStorage += 100.0f;      //For buildin' ships 'n shit
                if (Storage.Max < desiredStorage) score += (building.StorageAdded * 0.002f);  //If we need more storage, rate this building
                if (building.Maintenance > 0) score *= 0.25f;       //Prefer free storage

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} StorageAdd : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingPopulationGrowth(Building building)
        {
            float score = 0;
            if (building.PlusFlatPopulation != 0)
            {
                if (building.PlusFlatPopulation < 0) score = building.PlusFlatPopulation * 0.02f;  //Which is sorta like     0.01f * 2
                else
                {
                    score += (MaxPopulationBillion * 0.02f - 1.0f) + (building.PlusFlatPopulation * 0.01f);        //More desireable on high pop planets
                    if (score < 0) score = 0;     //Dont let this cause a penalty to other building properties
                }
                if (Owner.data.Traits.PhysicalTraitLessFertile) score *= 2;     //These are calculated outside the else, so they will affect negative flatpop too
                if (Owner.data.Traits.PhysicalTraitFertile) score *= 0.5f;

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} PopGrowth : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingPlusMaxPopulation(Building building)
        {
            float score = 0;
            if (building.MaxPopIncrease != 0)
            {
                if (building.MaxPopIncrease < 0) score = building.MaxPopIncrease * 0.002f;      //Which is sorta like     0.001f * 2
                else
                {
                    //Basically, only add to the score if we would be able to feed the extra people
                    if ((Food.YieldPerColonist + building.PlusFoodPerColonist) * (MaxPopulationBillion + (building.MaxPopIncrease / 1000))
                        >= (MaxPopulationBillion + (building.MaxPopIncrease / 1000) - Food.FlatBonus - building.PlusFlatFoodAmount))
                        score += building.MaxPopIncrease * 0.001f;
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} MaxPop : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingFlatResearch(Building building, float income)
        {
            float score = 0;
            if (building.PlusFlatResearchAmount != 0)
            {
                score = 0.001f;
                if (building.PlusFlatResearchAmount < 0)            //Surly no one would make a negative research building
                {
                    if (Res.Percent > 0 || Res.FlatBonus > 0) score += building.PlusFlatResearchAmount * 2;
                    else score += building.PlusFlatResearchAmount;
                }
                else
                {                   //Can we easily afford this
                    if ((building.Maintenance + building.Maintenance * Owner.data.Traits.MaintMod) * 1.5 <= income) score += building.PlusFlatResearchAmount * 2;
                    if (score < building.PlusFlatResearchAmount * 0.1f) score = building.PlusFlatResearchAmount * 0.1f; //A little extra research is always useful
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} FlatResearch : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingScrapFlatResearch(Building building, float income)
        {
            float score = 0;
            if (building.PlusFlatResearchAmount != 0)
            {
                if (building.PlusFlatResearchAmount < 0) score += building.PlusFlatResearchAmount * 2;
                else score += building.PlusFlatResearchAmount;


                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated SCRAP of {building.Name} FlatResearch : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingResearchPerCol(Building building)
        {
            float score = 0;
            if (building.PlusResearchPerColonist != 0 && PopulationBillion / MaxPopulationBillion >= 0.8f)
            {
                if (building.PlusResearchPerColonist < 0) score += building.PlusResearchPerColonist * 2;
                else score += building.PlusResearchPerColonist * (LeftoverWorkers() / 2);    //Reasonable extrapolation of how much research this will reliably produce

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} ResPerCol : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingCreditsPerCol(Building building)
        {
            float score = 0;
            if (building.CreditsPerColonist != 0)
            {
                if (building.CreditsPerColonist < 0) score += building.CreditsPerColonist * MaxPopulationBillion * 2;
                else score += (building.CreditsPerColonist * PopulationBillion) / 2;        //Dont want to cause this to have building preference over infrastructure buildings

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} CredsPerCol : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingScrapCreditsPerCol(Building building)
        {
            float score = 0;
            if (building.CreditsPerColonist != 0)
            {
                if (building.CreditsPerColonist < 0) score += building.CreditsPerColonist * MaxPopulationBillion;
                else score += (building.CreditsPerColonist * PopulationBillion) / 2;

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} CredsPerCol : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingPlusTaxPercent(Building building, float income)
        {
            float score = 0;
            if (building.PlusTaxPercentage != 0)
            {
                float assumedIncome = MaxPopulationBillion * 0.20f;     //This is an assumed tax value, used only for determining how useful a PlusTaxPercentage building is
                if (building.PlusTaxPercentage < 0) score += building.PlusTaxPercentage * assumedIncome * 2;
                else score += building.PlusTaxPercentage * assumedIncome / 2;

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} PlusTaxPercent : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingAllowShipBuilding(Building building)
        {
            float score = 0;
            if (building.AllowShipBuilding || building.Name == "Space Port" && PopulationBillion / MaxPopulationBillion >= 0.75f)
            {
                float prodFromLabor = LeftoverWorkers() * (Prod.YieldPerColonist + building.PlusProdPerColonist);
                float prodFromFlat = Prod.FlatBonus + building.PlusFlatProductionAmount + (building.PlusProdPerRichness * MineralRichness);
                //Do we have enough production capability to really justify trying to build ships
                if (prodFromLabor + prodFromFlat > 10.0f) score += ((prodFromLabor + prodFromFlat) / 10).Clamped(0.0f, 2.0f);

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} AllowShipBuilding : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingTerraforming(Building building)
        {
            float score = 0;
            if (building.PlusTerraformPoints != 0)
            {
                //Still working on this one...
                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} Terraform : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingScrapTerraforming(Building building)
        {
            float score = 0;
            if (building.PlusTerraformPoints != 0)
            {
                if (Fertility >= 1.0f)  score -= building.Maintenance;     //Are we done yet?
                else                    score += building.Maintenance;

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated SCRAP of {building.Name} Terraform : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingFertilityLoss(Building building)
        {
            float score = 0;
            if (building.MinusFertilityOnBuild != 0 && NonCybernetic)       //Cybernetic dont care.
            {
                if (building.MinusFertilityOnBuild < 0) score += building.MinusFertilityOnBuild * 2;    //Negative loss means positive gain!!
                else
                {                                   //How much fertility will actually be lost
                    float fertLost = Math.Min(Fertility, building.MinusFertilityOnBuild);
                    float foodFromLabor = MaxPopulationBillion * ((Fertility - fertLost) + Food.YieldPerColonist + building.PlusFoodPerColonist);
                    float foodFromFlat = Food.FlatBonus + building.PlusFlatFoodAmount;
                    //Will we still be able to feed ourselves?
                    if (foodFromFlat + foodFromLabor < Consumption) score += fertLost * 10;
                    else score += fertLost * 4;
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} FertLossOnBuild : Score was {score}");
            }

            return score;
        }

        float EvaluateBuildingScrapFertilityLoss(Building building)
        {
            float score = 0;
            if (building.MinusFertilityOnBuild != 0 && NonCybernetic)
            {
                if (building.MinusFertilityOnBuild < 0) score += building.MinusFertilityOnBuild * 2;    //Negative MinusFertilityOnBuild is reversed if the building is removed.

                //There is no logic for a score penalty due to loss of Fertility... because the damage has already been done  =(

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated {building.Name} FertLossOnBuild : Score was {score}");
            }

            return score;
        }

        int ExistingMilitaryBuildings()
        {
            return BuildingList.Count(building =>
                        building.CombatStrength > 0 &&
                        building.Name != "Outpost" &&
                        building.Name != "Capital City" &&
                        building.MaxPopIncrease == 0);
        }

        int DesiredMilitaryBuildings()
        {
            //This is a temporary quality rating. This will be replaced by an "importance to the Empire" calculation from the military planner at some point (hopefully soon). -Gretman
            float quality = Fertility + MineralRichness + MaxPopulationBillion / 2;
            if (Fertility > 1.6) quality += 1.5f;
            if (MineralRichness > 1.6) quality += 1.5f;

            return (int)(quality / 5); //Return just the whole number by truncating the decimal
        }

        float FactorForConstructionCost(float score, float cost, float highestCost)
        {
            //1 minus cost divided by highestCost gives a decimal value that is higher for smaller construction cost. This will make buildings with lower cost more desirable,
            //but never disqualify a building that had a positive score to begin with. -Gretman
            highestCost = highestCost.Clamped(50, 250);
            return score * (1f - cost / highestCost).Clamped(0.001f, 1.0f);
        }

        float EvaluateBuilding(Building building, float income, float highestCost)     //Gretman function, to support DoGoverning()
        {
            if (Name == "Drell VIfI") Debugger.Break();

            float buildingValue = 0.0f;    //End result value for entire building

            buildingValue -= EvaluateBuildingMaintenance(building, income);
            buildingValue += EvaluateBuildingFlatFood(building);
            buildingValue += EvaluateBuildingFoodPerCol(building);
            buildingValue += EvaluateBuildingFlatProd(building);
            buildingValue += EvaluateBuildingProdPerCol(building);
            buildingValue += EvaluateBuildingProdPerRichness(building);
            buildingValue += EvaluateBuildingStorage(building);
            buildingValue += EvaluateBuildingPopulationGrowth(building);
            buildingValue += EvaluateBuildingPlusMaxPopulation(building);
            buildingValue += EvaluateBuildingFlatResearch(building, income);
            buildingValue += EvaluateBuildingResearchPerCol(building);
            buildingValue += EvaluateBuildingCreditsPerCol(building);
            buildingValue += EvaluateBuildingPlusTaxPercent(building, income);
            buildingValue += EvaluateBuildingAllowShipBuilding(building);
            buildingValue += EvaluateBuildingTerraforming(building);
            buildingValue -= EvaluateBuildingFertilityLoss(building);

            if (buildingValue > 0) buildingValue = FactorForConstructionCost(buildingValue, building.Cost, highestCost);

            if (Name == ExtraInfoOnPlanet) Log.Info(ConsoleColor.Cyan, $"Evaluated {building.Name} Final Score was : {buildingValue}");

            return buildingValue;
        }

        float EvaluateMilitaryBuilding(Building building, float income)
        {
            float combatScore = (building.Strength + building.Defense + building.CombatStrength + building.SoftAttack + building.HardAttack) / 100f;

            float weaponDPS = 0;
            if (building.isWeapon && !String.IsNullOrEmpty(building.Weapon))
            {
                Weapon theWeapon = ResourceManager.WeaponsDict[building.Weapon];
                weaponDPS = (theWeapon.DamageAmount / theWeapon.fireDelay) / 500;
            }

            float shieldScore = building.PlanetaryShieldStrengthAdded / 1000;

            float allowTroops = 0;
            if (building.AllowInfantry)
            {
                if (colonyType == ColonyType.Military) allowTroops = 1.0f;
                else allowTroops = 0.5f;
            }

            //Shield, weapon, and/or allowtroop weighting go here (which is why they are all seperate values)

            float ratingFactor = (((PopulationBillion / MaxPopulationBillion) - 0.5f) * 2.0f).Clamped(0.0f, 1.0f);  //Factor by current population, so military buildings will be delayed
            float finalRating = (combatScore + weaponDPS + shieldScore + allowTroops) * ratingFactor;

            if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated military building {building.Name} : Score was {finalRating}");
            return finalRating;
        }

        void ChooseAndBuild(float budget)
        {
            if (BuildingsCanBuild.Count == 0) return;
            Building bestBuilding = null;
            float bestValue = 0.0f;     //So a building with a value of 0 will not be built.
            float buildingScore = 0.0f;
            float highestCost = BuildingsCanBuild.FindMax(building => building.Cost).Cost;
            for (int i = 0; i < BuildingsCanBuild.Count; i++)
            {
                //Find the building with the highest score
                buildingScore = EvaluateBuilding(BuildingsCanBuild[i], budget, highestCost);
                if (buildingScore > bestValue)
                {
                    bestBuilding = BuildingsCanBuild[i];
                    bestValue = buildingScore;
                }
            }
            if (bestBuilding != null) AddBuildingToCQ(bestBuilding);
            else ChooseAndBuildMilitary(budget);
        }

        void ChooseAndBuildMilitary(float budget)
        {
            if (BuildingsCanBuild.Count == 0) return;    //Discourage building military buildings too early
            if (ExistingMilitaryBuildings() < DesiredMilitaryBuildings())
            {
                Building bestMBuilding = null;
                float bestValue = 0.0f;
                float highestCost = BuildingsCanBuild.FindMax(building => building.Cost).Cost;
                for (int i = 0; i < BuildingsCanBuild.Count; i++)
                {
                    Building bldg = BuildingsCanBuild[i];
                    if (bldg.CombatStrength == 0 || bldg.MaxPopIncrease > 0) continue;
                    if (bldg.Name == "Outpost" || bldg.Name == "Capital City") continue;

                    float mBuildingScore = -EvaluateBuildingMaintenance(bldg, budget);
                    mBuildingScore += EvaluateMilitaryBuilding(bldg, budget);
                    mBuildingScore = FactorForConstructionCost(mBuildingScore, bldg.Cost, highestCost);
                    if (mBuildingScore > bestValue)
                    {
                        bestMBuilding = bldg;
                        bestValue = mBuildingScore;
                    }
                }
                if (bestMBuilding != null) AddBuildingToCQ(bestMBuilding);
            }
        }

        void BuildBuildings(float budget)
        {
            //Do some existing bulding recon
            int openTiles      = TilesList.Count(tile => tile.Habitable && tile.building == null);
            int totalbuildings = TilesList.Count(tile => tile.building != null && tile.building.Name != "Biospheres");

            //Construction queue recon
            bool buildingInTheWorks  = SbProduction.ConstructionQueue.Any(building => building.isBuilding);
            bool militaryBInTheWorks = SbProduction.ConstructionQueue.Any(building => building.isBuilding && building.Building.CombatStrength > 0);
            bool lotsInQueueToBuild  = ConstructionQueue.Count >= 4;


            //New Build Logic by Gretman
            if (!lotsInQueueToBuild) BuildShipyardifAble(); //If we can build a shipyard but dont have one, build it

            if (openTiles > 0)
            {
                if (!buildingInTheWorks) ChooseAndBuild(budget);
            }
            else
            {
                bool biosphereInTheWorks = SbProduction.ConstructionQueue.Find(building => building.isBuilding && building.Building.Name == "Biospheres") != null;
                Building bioSphere = BuildingsCanBuild.Find(building => building.Name == "Biospheres");

                if (bioSphere != null && !biosphereInTheWorks && totalbuildings < 35 && bioSphere.Maintenance < budget + 0.3f) //No habitable tiles, and not too much in debt
                    AddBuildingToCQ(bioSphere);
            }

            ScrapBuildings(budget);
        }

        void BuildShipyardifAble()
        {
            if (RecentCombat || !HasShipyard) return;
            if (Owner != Empire.Universe.PlayerEmpire
                && !Shipyards.Any(ship => ship.Value.shipData.IsShipyard)
                && Owner.ShipsWeCanBuild.Contains(Owner.data.DefaultShipyard))
            {
                bool hasShipyard = false;
                foreach (QueueItem queueItem in ConstructionQueue)
                {
                    if (queueItem.isShip && queueItem.sData.IsShipyard)
                    {
                        hasShipyard = true;
                        break;
                    }
                }
                if (!hasShipyard && DevelopmentLevel > 2)
                    ConstructionQueue.Add(new QueueItem(this)
                    {
                        isShip = true,
                        sData = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].shipData,
                        Cost = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].GetCost(Owner) *
                               UniverseScreen.GamePaceStatic
                    });
            }
        }

        void ScrapBuildings(float income)
        {
            if (Name == "Cordron Vf") Debugger.Break();

            for (int i = 0; i < BuildingList.Count; i++)
            {
                float buildingValue = 0;
                float costWeight    = 0;
                Building bldg       = BuildingList[i];
                if (bldg.Name == "Biospheres" || !bldg.Scrappable || bldg.IsPlayerAdded) continue;

                costWeight     = EvaluateBuildingScrapWeight(bldg, income);

                buildingValue += EvaluateBuildingScrapFlatFood(bldg);
                buildingValue += EvaluateBuildingScrapFoodPerCol(bldg);
                buildingValue += EvaluateBuildingFlatProd(bldg);
                buildingValue += EvaluateBuildingProdPerCol(bldg);
                buildingValue += EvaluateBuildingProdPerRichness(bldg);
                buildingValue += EvaluateBuildingStorage(bldg);
                buildingValue += EvaluateBuildingPopulationGrowth(bldg);
                buildingValue += EvaluateBuildingPlusMaxPopulation(bldg);
                buildingValue += EvaluateBuildingScrapFlatResearch(bldg, income);
                buildingValue += EvaluateBuildingResearchPerCol(bldg);
                buildingValue += EvaluateBuildingScrapCreditsPerCol(bldg);
                buildingValue += EvaluateBuildingPlusTaxPercent(bldg, income);
                buildingValue += EvaluateBuildingAllowShipBuilding(bldg);
                buildingValue += EvaluateBuildingScrapTerraforming(bldg);
                buildingValue -= EvaluateBuildingScrapFertilityLoss(bldg);  //Yes, -= because it is calculated as negative in the function
                if (bldg.CombatStrength > 0) buildingValue += EvaluateMilitaryBuilding(bldg, income);

                if (buildingValue < costWeight)
                {
                    Log.Info(ConsoleColor.Blue, $"{Owner.PortraitName} SCRAPPED {bldg.Name} on planet {Name}     buildingValue: {buildingValue}    costWeight: {costWeight}");
                    bldg.ScrapBuilding(this);
                    return;     //No mass scrappings
                }

                if (Name == ExtraInfoOnPlanet) Log.Info($"Evaluated SCRAP of {bldg.Name}  buildingValue: {buildingValue}    costWeight: {costWeight}");
            }
        }

        float BuildingBudget()
        {
            //Empire budget will go here instead of planet budget

            float income = MaxPopulationBillion * (Owner.data.TaxRate).Clamped(0.1f, 0.4f);    //If taxes go way up, dont want the governors to go too crazy
            income += income * PlusTaxPercentage;
            income += income * Owner.data.Traits.TaxMod;
            income -= SbProduction.GetTotalConstructionQueueMaintenance();

            return income;
        }



        bool FindOutpost()
        {
            //First check the existing buildings
            foreach (Building building in BuildingList)
            {
                if (building.Name == "Outpost" || building.Name == "Capital City")
                {
                    return true;
                }
            }

            //Then check the queue
            foreach (QueueItem queueItem in ConstructionQueue)
            {
                if (queueItem.isBuilding && queueItem.Building.Name == "Outpost")
                {
                    return true;
                }
            }
            return false;
        }

        void BuildOutpostifAble() //A Gretman function to support DoGoverning()
        {
            //Check Existing Buildings and the queue
            if (FindOutpost()) return;

            //Build it!
            AddBuildingToCQ(ResourceManager.CreateBuilding("Outpost"), false);

            //Move Outpost to the top of the list, and rush production
            for (int index = 0; index < ConstructionQueue.Count; ++index)
            {
                QueueItem queueItem1 = ConstructionQueue[index];
                if (index == 0 && queueItem1.isBuilding)
                {
                    if (queueItem1.Building.Name == "Outpost")
                    {
                        SbProduction.ApplyAllStoredProduction(0);
                    }
                    break;
                }

                if (queueItem1.isBuilding && queueItem1.Building.Name == "Outpost")
                {
                    ConstructionQueue.Remove(queueItem1);
                    ConstructionQueue.Insert(0, queueItem1);
                    break;
                }
            }
        }
    }
}
