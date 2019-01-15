using System;
using System.Diagnostics;
using System.Linq;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class Planet
    {
        bool IsPlanetExtraDebugTarget()
        {
            if (Name == ExtraInfoOnPlanet)
                return true;

            // Debug eval planet if we have colony screen open
            if (Debugger.IsAttached
                && Empire.Universe.LookingAtPlanet
                && Empire.Universe.workersPanel is ColonyScreen colony
                && colony.P == this)
                return true;

            return false;
        }

        void DebugEvalBuild(Building b, string what, float score)
        {
            if (IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.DarkGray,
                    $"Eval BUILD  {b.Name,-20}  {what,-16} {(+score).SignString()}");
        }

        void DebugEvalScrap(Building b, string what, float score)
        {
            if (IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.DarkGray, 
                    $"Eval SCRAP  {b.Name,-20}  {what,-16} {(-score).SignString()}");
        }

        float EvalScrapWeight(Building b, float income)
        {
            if (b.Maintenance.AlmostZero()) return 0;

            float score = b.Maintenance;
            score -= Owner.data.FlatMoneyBonus * 0.015f; // Acceptable loss (Note what this will do at high Difficulty)

            //This is where the logic for how bad the planet is doing will go + the value of this planet to the empire and all that.
            //For now, just testing with base of just being able to justify its own Maintenance cost
            DebugEvalScrap(b, "Maintenance", score);
            return score;
        }

        float EvalMaintenance(Building b, float income)
        {
            if (b.Maintenance.AlmostZero()) return 0;

            float score = b.Maintenance * 2;  //Base of 2x maintenance -- Also, I realize I am not calculating MaintMod here. It throws the algorithm off too much
            float maint = b.Maintenance + b.Maintenance * Owner.data.Traits.MaintMod;
            if (income < maint)
                score += score + maint;   //Really don't want this if we cant afford it
            score -= Owner.data.FlatMoneyBonus * 0.015f; // Acceptable loss (Note what this will do at high Difficulty)
            
            DebugEvalBuild(b, "Maintenance", score);
            return score;
        }

        float EvalFlatFood(Building b)
        {
            float score = 0;
            if (b.PlusFlatFoodAmount.AlmostZero() || IsCybernetic) return 0;
            if (b.PlusFlatFoodAmount < 0)
            {
                score = b.PlusFlatFoodAmount * 2;   //For negative Flat Food (those crazy modders...)
            }
            else
            {
                float farmers = Food.WorkersNeededForEquilibrium();
                score += ((b.PlusFlatFoodAmount / MaxPopulationBillion) * 1.5f).Clamped(0.0f, 1.5f);   //Percentage of population this will feed, weighted
                score += 1.5f - (Food.YieldPerColonist/2);//Bonus for low Effective Fertility
                if (farmers.AlmostZero()) score += b.PlusFlatFoodAmount; //Bonus if planet is relying on flat food
                if (farmers > 0.5f) score += farmers - 0.5f;            //Bonus if planet is spending a lot of labor feeding itself
                if (score < b.PlusFlatFoodAmount * 0.1f) score = b.PlusFlatFoodAmount * 0.1f; //A little flat food is always useful
                if (b.PlusFlatFoodAmount + Food.FlatBonus - 0.5f > MaxPopulationBillion) score = 0;   //Dont want this if a lot would go to waste
            }
            DebugEvalBuild(b, "FlatFood", score);
            return score;
        }

        float EvalFlatFoodScrap(Building b)
        {
            float score = 0;
            if (b.PlusFlatFoodAmount.AlmostZero() || IsCybernetic) return 0;
            if (b.PlusFlatFoodAmount < 0)
            {
                score = b.PlusFlatFoodAmount * 2;   //For negative Flat Food (those crazy modders...)
            }
            else
            {
                float farmers = Food.WorkersNeededForEquilibrium(flat: -b.PlusFlatFoodAmount);
                score += ((b.PlusFlatFoodAmount / MaxPopulationBillion) * 1.5f).Clamped(0.0f, 1.5f);   //Percentage of population this is feeding, weighted
                score += 1.5f - (Food.YieldPerColonist/2);         //Bonus for low Effective Fertility
                if (farmers.AlmostZero()) score += b.PlusFlatFoodAmount; //Bonus if planet is relying on flat food
                if (farmers > 0.5f) score += farmers - 0.5f;   //Bonus if planet would be spending a lot of labor feeding itself
                if (score < 0) score = 0;                                        //No penalty for extra food
            }
            DebugEvalScrap(b, "FlatFood", score);
            return score;
        }

        float MaxFoodGainPerColonist(Building b, float farmers = 1.0f)
        {
            float gain = b.PlusFoodPerColonist * farmers * MaxPopulationBillion;
            return Math.Max(gain, b.PlusFoodPerColonist * 0.1f); // A little gain is always useful
        }

        float MaxProdGainPerColonist(Building b, float workers = 1.0f)
        {
            float gain = b.PlusProdPerColonist * workers * MaxPopulationBillion;
            return Math.Max(gain, b.PlusProdPerColonist * 0.1f); // A little gain is always useful
        }

        float EvalFoodPerCol(Building b)
        {
            if (b.PlusFoodPerColonist.AlmostZero() || IsCybernetic) return 0;
            float score = 0;
            if (b.PlusFoodPerColonist < 0)
            {
                score = MaxFoodGainPerColonist(b, 2.0f);
            }
            else
            {
                float farmers = Food.WorkersNeededForEquilibrium(perCol: b.PlusFoodPerColonist);
                score += MaxFoodGainPerColonist(b, farmers);
                if ((b.PlusFoodPerColonist + Food.YieldPerColonist) <= 1.0f)
                    score = 0; // Don't try to add farming to a planet without enough to sustain itself
            }
            DebugEvalBuild(b, "FoodPerCol", score);
            return score;
        }

        float EvalFoodPerColScrap(Building b)
        {
            if (b.PlusFoodPerColonist.AlmostZero() || IsCybernetic) return 0;
            float score = 0;
            if (b.PlusFoodPerColonist < 0)
            {
                score = MaxFoodGainPerColonist(b, 2.0f);
            }
            else
            {
                score += MaxFoodGainPerColonist(b, Food.WorkersNeededForEquilibrium());
                if ((Food.YieldPerColonist - b.PlusFoodPerColonist) <= 1.0f)
                    score = b.PlusFoodPerColonist; // Don't scrap this if it would drop effective yield below 1.0
            }
            DebugEvalScrap(b, "FoodPerCol", score);
            return score;
        }

        float EvalFlatProd(Building b)
        {
            if (b.PlusFlatProductionAmount.AlmostZero()) return 0;
            float score = 0;
            if (b.PlusFlatProductionAmount < 0)
            {
                score = b.PlusFlatProductionAmount * 2;
            }
            else
            {
                if (IsCybernetic)
                    score += b.PlusFlatProductionAmount / MaxPopulationBillion;     //Percentage of the filthy Opteris population this will feed
                
                score += (0.5f - PopulationRatio).Clamped(0.0f, 0.5f);   //Bonus if population is currently less than half of max population
                score += 1.5f - Prod.YieldPerColonist;      //Bonus for low richness planets
                score += (0.66f - MineralRichness).Clamped(0.0f, 0.66f);      //More Bonus for really low richness planets
                
                float currentOutput = Prod.YieldPerColonist * LeftoverWorkerBillions() + Prod.FlatBonus;  //Current Prod Output
                score += (b.PlusFlatProductionAmount / currentOutput).Clamped(0.0f, 2.0f);         //How much more this building will produce compared to labor prod
                if (score < b.PlusFlatProductionAmount * 0.1f) score = b.PlusFlatProductionAmount * 0.1f; //A little production is always useful
            }

            DebugEvalBuild(b, "FlatProd", score);
            return score;
        }

        float EvalProdPerCol(Building b)
        {
            if (b.PlusProdPerColonist.AlmostZero() || PopulationRatio < 0.8f) return 0;

            float score;
            if (b.PlusProdPerColonist < 0)
                score = MaxProdGainPerColonist(b, 2.0f);
            else
                score = MaxProdGainPerColonist(b, LeftoverWorkers());
            DebugEvalBuild(b, "ProdPerCol", score);
            return score;
        }

        // This one can produce a pretty high building value,
        // which is normally offset by its huge maintenance cost and Fertility loss
        float EvalProdPerRichness(Building b)
        {
            if (b.PlusProdPerRichness.AlmostZero()) return 0;
            float score = 0;
            if (b.PlusProdPerRichness < 0)
            {
                score = b.PlusProdPerRichness * MineralRichness * 2;
            }
            else
            {
                score += b.PlusProdPerRichness * MineralRichness; // Production this would generate
                if (!HasShipyard) score *= 0.75f; // Do we have a use for all this production?
            }
            DebugEvalBuild(b, "ProdPerRich", score);
            return score;
        }

        float EvalStorage(Building b)
        {
            if (b.StorageAdded == 0) return 0;

            float desiredStorage = 70.0f;
            if (Food.YieldPerColonist >= 2.5f || Prod.YieldPerColonist >= 2.5f || Prod.FlatBonus > 5)
                desiredStorage += 100.0f;  //Potential high output
            if (HasShipyard) desiredStorage += 100.0f; // For buildin' ships 'n shit

            float score = 0;
            if (Storage.Max < desiredStorage) score += (b.StorageAdded * 0.002f); // If we need more storage, rate this building
            if (b.Maintenance > 0) score *= 0.25f; // Prefer free storage

            DebugEvalBuild(b, "StorageAdd", score);
            return score;
        }

        float EvalPopulationGrowth(Building b)
        {
            if (b.PlusFlatPopulation.AlmostZero()) return 0;
            float score = 0;
            if (b.PlusFlatPopulation < 0)
            {
                score = b.PlusFlatPopulation * 0.02f;  // Which is sorta like 0.01f * 2
            }
            else
            {
                // More desirable on high pop planets
                score += (MaxPopulationBillion * 0.02f - 1.0f) + (b.PlusFlatPopulation * 0.01f);
                if (score < 0) score = 0; // Don't let this cause a penalty to other building properties
            }

            // These are calculated outside the else, so they will affect negative flatpop too
            if (Owner.data.Traits.PhysicalTraitLessFertile) score *= 2.0f;
            if (Owner.data.Traits.PhysicalTraitFertile)     score *= 0.5f;

            DebugEvalBuild(b, "PopGrowth", score);
            return score;
        }

        float EvalPlusMaxPopulation(Building b)
        {
            if (b.MaxPopIncrease.AlmostZero()) return 0;
            float score = 0;
            if (b.MaxPopIncrease < 0)
            {
                score = b.MaxPopIncrease * 0.002f; // Which is sorta like 0.001f * 2
            }
            else
            {
                // Basically, only add to the score if we would be able to feed the extra people
                if ((Food.YieldPerColonist + b.PlusFoodPerColonist) * (MaxPopulationBillion + (b.MaxPopIncrease / 1000))
                    >= (MaxPopulationBillion + (b.MaxPopIncrease / 1000) - Food.FlatBonus - b.PlusFlatFoodAmount))
                    score += b.MaxPopIncrease * 0.001f;
            }
            DebugEvalBuild(b, "MaxPop", score);
            return score;
        }

        float EvalFlatResearch(Building b, float income)
        {
            if (b.PlusFlatResearchAmount.AlmostZero()) return 0;
            float score = 0.001f;
            if (b.PlusFlatResearchAmount < 0) // Surly no one would make a negative research building
            {
                if (Res.Percent > 0 || Res.FlatBonus > 0) score += b.PlusFlatResearchAmount * 2;
                else score += b.PlusFlatResearchAmount;
            }
            else
            {   // Can we easily afford this
                if (b.ActualMaintenance(this) * 1.5f <= income)
                    score += b.PlusFlatResearchAmount * 2;

                score = Math.Max(score, b.PlusFlatResearchAmount * 0.1f); // A little extra is always useful
            }
            DebugEvalBuild(b, "FlatResearch", score);
            return score;
        }

        float EvalScrapFlatResearch(Building b, float income)
        {
            if (b.PlusFlatResearchAmount.AlmostZero()) return 0;

            float score = 0;
            if (b.PlusFlatResearchAmount < 0)
                score += b.PlusFlatResearchAmount * 2;
            else 
                score += b.PlusFlatResearchAmount;

            DebugEvalScrap(b, "FlatResearch", score);
            return score;
        }

        float EvalResearchPerCol(Building b)
        {
            if (b.PlusResearchPerColonist.AlmostZero() || PopulationRatio < 0.8f) return 0;
            
            float score;
            if (b.PlusResearchPerColonist < 0)
                score = b.PlusResearchPerColonist * 2;
            else // Reasonable extrapolation of how much research this will reliably produce
                score = b.PlusResearchPerColonist * (LeftoverWorkerBillions() / 2);

            DebugEvalBuild(b, "ResPerCol", score);
            return score;
        }

        float EvalCreditsPerCol(Building b)
        {
            if (b.CreditsPerColonist.AlmostZero()) return 0;

            float score;
            if (b.CreditsPerColonist < 0)
                score = b.CreditsPerColonist * MaxPopulationBillion * 2.0f;
            else // Don't want to cause this to have building preference over infrastructure buildings
                score = b.CreditsPerColonist * PopulationBillion * 0.5f;        

            DebugEvalBuild(b, "CredsPerCol", score);
            return score;
        }

        float EvalScrapCreditsPerCol(Building b)
        {
            if (b.CreditsPerColonist.AlmostZero()) return 0;

            float score;
            if (b.CreditsPerColonist < 0)
                score = b.CreditsPerColonist * MaxPopulationBillion;
            else
                score = b.CreditsPerColonist * PopulationBillion * 0.5f;

            DebugEvalScrap(b, "CredsPerCol", score);
            return score;
        }

        float EvalPlusTaxPercent(Building b)
        {
            if (b.PlusTaxPercentage.AlmostZero()) return 0;

            // This is an assumed tax value, used only for determining how useful a PlusTaxPercentage building is
            float maxPotentialIncome = b.PlusTaxPercentage*MaxPopulationBillion * 0.20f;
            
            float score;
            if (maxPotentialIncome < 0)
                score = maxPotentialIncome * 2.0f; // humans perceive negatives more severely
            else
                score = maxPotentialIncome * 0.7f;

            DebugEvalBuild(b, "PlusTaxPercent", score);
            return score;
        }

        float EvalSpacePort(Building b)
        {
            bool spacePort = b.AllowShipBuilding || b.IsSpacePort;
            if (!spacePort || PopulationRatio < 0.5f)
                return 0;

            float score = 0;
            if (BuildingExists(Building.CapitalId))
                score += 1.0f; // we can't be a space-faring species if our capital doesn't have a space-port...

            float prodFromLabor = LeftoverWorkerBillions() * (Prod.YieldPerColonist + b.PlusProdPerColonist);
            float prodFromFlat = Prod.FlatBonus + b.PlusFlatProductionAmount + (b.PlusProdPerRichness * MineralRichness);
            
            // Do we have enough production capability to really justify trying to build ships
            if (prodFromLabor + prodFromFlat > 8.0f)
                score += ((prodFromLabor + prodFromFlat) / 8.0f).Clamped(0.0f, 2.0f);

            DebugEvalBuild(b, "ShipBuilding", score);
            return score;
        }

        float EvalTerraformer(Building b)
        {
            if (b.PlusTerraformPoints.AlmostZero()) return 0;

            // @todo Still working on this one...
            float score = b.PlusTerraformPoints - b.Maintenance*0.5f;

            DebugEvalBuild(b, "Terraform", score);
            return score;
        }

        float EvalTerraformerScrap(Building b)
        {
            if (b.PlusTerraformPoints.AlmostZero()) return 0;

            float score = 0;
            if (Fertility >= 1.0f)  score -= b.Maintenance;
            else                    score += b.Maintenance;

            DebugEvalScrap(b, "Terraform", score);
            return score;
        }

        float EvalFertilityLoss(Building b)
        {
            if (b.MinusFertilityOnBuild.AlmostZero() || IsCybernetic) return 0;

            float score = 0;
            if (b.MinusFertilityOnBuild < 0)
            {
                score = b.MinusFertilityOnBuild * 2;    //Negative loss means positive gain!!
            }
            else
            {   
                // How much fertility will actually be lost
                // @todo food calculation is a bit dodgy
                float fertLost = Math.Min(Fertility, b.MinusFertilityOnBuild);
                float foodFromLabor = MaxPopulationBillion * ((Fertility - fertLost) + Food.YieldPerColonist + b.PlusFoodPerColonist);
                float foodFromFlat = Food.FlatBonus + b.PlusFlatFoodAmount;
                // Will we still be able to feed ourselves?
                if (foodFromFlat + foodFromLabor < Consumption)
                    score += fertLost * 10;
                else 
                    score += fertLost * 4;
            }

            DebugEvalBuild(b, "FertLossOnBuild", score);
            return score;
        }

        float EvalFertilityLossScrap(Building b)
        {
            if (b.MinusFertilityOnBuild.AlmostZero() || IsCybernetic) return 0;

            float score = 0;
            if (b.MinusFertilityOnBuild < 0)
                score = b.MinusFertilityOnBuild * 2; // Negative MinusFertilityOnBuild is reversed if the building is removed.

            // There is no logic for a score penalty due to loss of Fertility... because the damage has already been done  =(
            DebugEvalScrap(b, "FertLossOnBuild", score);
            return score;
        }

        int ExistingMilitaryBuildings()
        {
            return BuildingList.Count(building =>
                        building.CombatStrength > 0 &&
                        !building.IsCapitalOrOutpost &&
                        building.MaxPopIncrease.AlmostZero());
        }

        int DesiredMilitaryBuildings()
        {
            // Importance to our empire:
            float worth = ColonyWorth(toEmpire: Owner);
            int desired = (int)Math.Floor((worth / 15) + 0.1f);
            return desired;
        }

        static float FactorForConstructionCost(float score, float cost, float highestCost)
        {
            //1 minus cost divided by highestCost gives a decimal value that is higher for smaller construction cost. This will make buildings with lower cost more desirable,
            //but never disqualify a building that had a positive score to begin with. -Gretman
            highestCost = highestCost.Clamped(50, 250);
            return score * (1f - cost / highestCost).Clamped(0.001f, 1.0f);
        }

        float EvaluateBuilding(Building b, float income, float highestCost)     //Gretman function, to support DoGoverning()
        {
            float score = 0.0f;    //End result value for entire building

            score -= EvalMaintenance(b, income);
            score += EvalFlatFood(b);
            score += EvalFoodPerCol(b);
            score += EvalFlatProd(b);
            score += EvalProdPerCol(b);
            score += EvalProdPerRichness(b);
            score += EvalStorage(b);
            score += EvalPopulationGrowth(b);
            score += EvalPlusMaxPopulation(b);
            score += EvalFlatResearch(b, income);
            score += EvalResearchPerCol(b);
            score += EvalCreditsPerCol(b);
            score += EvalPlusTaxPercent(b);
            score += EvalSpacePort(b);
            score += EvalTerraformer(b);
            score -= EvalFertilityLoss(b);

            if (score > 0)
                score = FactorForConstructionCost(score, b.Cost, highestCost);

            if (IsPlanetExtraDebugTarget())
            {
                if (score > 0.0f)
                    Log.Info(ConsoleColor.Cyan,    $"Eval BUILD  {b.Name,-20}  {"SUITABLE",-16} {score.SignString()}");
                else
                    Log.Info(ConsoleColor.DarkRed, $"Eval BUILD  {b.Name,-20}  {"NOT GOOD",-16} {score.SignString()}");
            }
            return score;
        }

        float EvalMilitaryBuilding(Building b, float income)
        {
            float combatScore = (b.Strength + b.Defense + b.CombatStrength + b.SoftAttack + b.HardAttack) / 100f;

            float dps = 0;
            if (b.isWeapon && b.Weapon.NotEmpty())
            {
                Weapon w = ResourceManager.WeaponsDict[b.Weapon];
                dps      = (w.DamageAmount * w.ProjectileCount  / w.fireDelay) / 500;
                // FB: Fortunately, salvos and beams dont work for building weapons,
                // otherwise it would be more complicated to calc this
            }

            // Shields are very efficient because they protect from early bombardments
            // Smallest Planetary Shield has strength 500
            // Make sure we give it a fair score.
            float shieldScore = b.PlanetaryShieldStrengthAdded / 500;
            float allowTroops = 0;
            if (b.AllowInfantry)
                allowTroops = colonyType == ColonyType.Military ? 1.0f : 0.5f;

            float invadeScore      = b.InvadeInjurePoints;
            float defenseShipScore = CalcDefenseShipScore(b);

            // Shield, weapon, and/or allow troop weighting go here (which is why they are all seperate values)

            // Factor by current population, so military buildings will be delayed
            float ratingFactor = ((PopulationRatio - 0.5f) * 2.0f).Clamped(0.0f, 1.0f);
            float finalRating  = (combatScore + dps + shieldScore + allowTroops + invadeScore + defenseShipScore) * ratingFactor;

            DebugEvalBuild(b, "Military", finalRating);
            return finalRating;
        }

        static float CalcDefenseShipScore(Building b)
        {
            if (b.DefenseShipsCapacity <= 0 || b.DefenseShipsRole == (ShipData.RoleName) 0)
                return 0;

            float defenseShipScore;
            switch (b.DefenseShipsRole)
            {
                case ShipData.RoleName.drone:
                    defenseShipScore = 0.05f;
                    break;
                case ShipData.RoleName.fighter:
                    defenseShipScore = 0.1f;
                    break;
                case ShipData.RoleName.corvette:
                    defenseShipScore = 0.2f;
                    break;
                case ShipData.RoleName.frigate:
                    defenseShipScore = 0.4f;
                    break;
                default:
                    defenseShipScore = 1f;
                    break;
            }
            return defenseShipScore * b.DefenseShipsCapacity;
        }

        void ChooseAndBuild(float budget)
        {
            if (BuildingsCanBuild.Count == 0) return;

            if (IsPlanetExtraDebugTarget())
                Log.Info($"==== Planet  {Name}  CHOOSE AND BUILD ==== ");

            Building best = null;
            float bestValue = 0.0f; // So a building with a value of 0 will not be built.
            float highestCost = BuildingsCanBuild.FindMax(building => building.Cost).Cost;
            
            for (int i = 0; i < BuildingsCanBuild.Count; i++)
            {
                //Find the building with the highest score
                float buildingScore = EvaluateBuilding(BuildingsCanBuild[i], budget, highestCost);
                if (buildingScore > bestValue)
                {
                    best = BuildingsCanBuild[i];
                    bestValue = buildingScore;
                }
            }

            if (best != null)
                AddBuildingToCQ(best);
            else
                ChooseAndBuildMilitary(budget);
        }

        void ChooseAndBuildMilitary(float budget)
        {
            if (BuildingsCanBuild.Count == 0) return; // Discourage building military buildings too early

            int existingMilitary = ExistingMilitaryBuildings();
            int desiredMilitary  = DesiredMilitaryBuildings();

            if (IsPlanetExtraDebugTarget())
            {
                Log.Info($"==== Planet  {Name}  CHOOSE AND BUILD MILITARY ==== ");
                if (existingMilitary >= desiredMilitary)
                    Log.Info(ConsoleColor.DarkGreen,
                        $"Eval BUILD  MILITARY NOT NEEDED  existing:{existingMilitary} >= desired:{desiredMilitary}");
                else
                    Log.Info(ConsoleColor.DarkRed,
                        $"Eval BUILD  MILITARY IS NEEDED   existing:{existingMilitary} <  desired:{desiredMilitary}");
            }

            if (existingMilitary >= desiredMilitary)
                return;

            Building best = null;
            float bestValue = 0.0f;
            float highestCost = BuildingsCanBuild.FindMax(building => building.Cost).Cost;
         
            for (int i = 0; i < BuildingsCanBuild.Count; i++)
            {
                Building b = BuildingsCanBuild[i];
                if (b.CombatStrength == 0 || b.MaxPopIncrease > 0) continue;
                if (b.IsCapitalOrOutpost) continue;

                float score = -EvalMaintenance(b, budget);
                score += EvalMilitaryBuilding(b, budget);
                score = FactorForConstructionCost(score, b.Cost, highestCost);
                if (score > bestValue)
                {
                    best = b;
                    bestValue = score;
                }
            }
            if (best != null) AddBuildingToCQ(best);
        }

        void BuildBuildings(float budget)
        {
            //Do some existing bulding recon
            int openTiles      = TilesList.Count(tile => tile.Habitable && tile.building == null);
            int totalbuildings = TilesList.Count(tile => tile.building != null && !tile.building.IsBiospheres);

            //Construction queue recon
            bool buildingInTheWorks  = SbProduction.ConstructionQueue.Any(b => b.isBuilding);
            bool militaryBInTheWorks = SbProduction.ConstructionQueue.Any(b => b.isBuilding && b.Building.CombatStrength > 0);
            bool lotsInQueueToBuild  = ConstructionQueue.Count >= 4;


            //New Build Logic by Gretman
            if (!lotsInQueueToBuild) BuildShipyardIfAble(); //If we can build a shipyard but dont have one, build it

            if (openTiles > 0)
            {
                if (!buildingInTheWorks) ChooseAndBuild(budget);
            }
            else
            {
                bool biosphereInTheWorks = SbProduction.ConstructionQueue.Find(b => b.isBuilding && b.Building.IsBiospheres) != null;
                Building bio = BuildingsCanBuild.Find(b => b.IsBiospheres);

                if (bio != null && !biosphereInTheWorks && totalbuildings < 35 && bio.Maintenance < budget + 0.3f) //No habitable tiles, and not too much in debt
                    AddBuildingToCQ(bio);
            }

            ScrapBuildings(budget);
        }

        void BuildShipyardIfAble()
        {
            if (RecentCombat || !HasShipyard) return;
            if (Owner == Empire.Universe.PlayerEmpire
                || Shipyards.Any(ship => ship.Value.shipData.IsShipyard)
                || !Owner.ShipsWeCanBuild.Contains(Owner.data.DefaultShipyard))
                return;

            bool hasShipyard = ConstructionQueue.Any(q => q.isShip && q.sData.IsShipyard);

            if (!hasShipyard && IsVibrant)
            {
                ConstructionQueue.Add(new QueueItem(this)
                {
                    isShip = true,
                    sData = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].shipData,
                    Cost = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].GetCost(Owner) *
                           CurrentGame.Pace
                });
            }
        }

        void ScrapBuildings(float income)
        {
            if (IsPlanetExtraDebugTarget())
                Log.Info($"==== Planet  {Name}  EVALUATE BUILDINGS TO SCRAP ==== ");

            for (int i = 0; i < BuildingList.Count; i++)
            {
                Building b = BuildingList[i];
                if (b.IsBiospheres || !b.Scrappable || b.IsPlayerAdded)
                    continue;
                
                float cost = EvalScrapWeight(b, income);
                float value = 0;

                value += EvalFlatFoodScrap(b);
                value += EvalFoodPerColScrap(b);
                value += EvalFlatProd(b);
                value += EvalProdPerCol(b);
                value += EvalProdPerRichness(b);
                value += EvalStorage(b);
                value += EvalPopulationGrowth(b);
                value += EvalPlusMaxPopulation(b);
                value += EvalScrapFlatResearch(b, income);
                value += EvalResearchPerCol(b);
                value += EvalScrapCreditsPerCol(b);
                value += EvalPlusTaxPercent(b);
                value += EvalSpacePort(b);
                value += EvalTerraformerScrap(b);
                value -= EvalFertilityLossScrap(b);  // Yes, -= because it is calculated as negative in the function
                if (b.CombatStrength > 0) value += EvalMilitaryBuilding(b, income);

                if (value < cost)
                {
                    Log.Info(ConsoleColor.Blue, $"{Owner.PortraitName} SCRAPPED {b.Name} on planet {Name}   value: {value}  <  cost: {cost}");
                    b.ScrapBuilding(this);
                    return; // No mass scrapping
                }

                if (IsPlanetExtraDebugTarget())
                    Log.Info(ConsoleColor.Green, $"Eval SCRAP  {b.Name,-20}  {"KEEP THIS",-16} {value} >= {cost}");
            }
        }

        float BuildingBudget()
        {
            // Empire budget will go here instead of planet budget
            // if taxes go way up, don't want the governors to go too crazy
            float income = MaxPopulationBillion * Money.TaxRate;
            income -= SbProduction.GetTotalConstructionQueueMaintenance();
            return income;
        }

        bool OutpostBuiltOrInQueue()
        {
            // First check the existing buildings
            if (BuildingList.Any(b => b.IsCapitalOrOutpost))
                return true;

            // Then check the queue
            if (ConstructionQueue.Any(q => q.isBuilding && q.Building.IsOutpost))
                return true;

            return false;
        }

        void BuildOutpostIfAble() // A Gretman function to support DoGoverning()
        {
            //Check Existing Buildings and the queue
            if (OutpostBuiltOrInQueue())
                return;

            //Build it!
            AddBuildingToCQ(ResourceManager.CreateBuilding(Building.OutpostId), playerAdded: false);

            // Move Outpost to the top of the list, and rush production
            for (int i = 0; i < ConstructionQueue.Count; ++i)
            {
                QueueItem q = ConstructionQueue[i];
                if (i == 0 && q.isBuilding)
                {
                    if (q.Building.IsOutpost)
                        SbProduction.ApplyAllStoredProduction(0);
                    break;
                }

                if (q.isBuilding && q.Building.IsOutpost)
                {
                    ConstructionQueue.Remove(q);
                    ConstructionQueue.Insert(0, q);
                    break;
                }
            }
        }
    }
}
