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

        float EvalMaintenance(Building b, float budget)
        {
            if (b.Maintenance.AlmostZero())
                return 0;

            float score = b.Maintenance * 2;  //Base of 2x maintenance -- Also, I realize I am not calculating MaintMod here. It throws the algorithm off too much
            float maint = b.Maintenance + b.Maintenance * Owner.data.Traits.MaintMod;
            if (budget < maint)
                score += maint * 10;   // Really don't want this if we cant afford it, since the scraping minister might scrap something else next turn

            score -= Owner.data.FlatMoneyBonus * 0.015f; // Acceptable loss (Note what this will do at high Difficulty)
            
            DebugEvalBuild(b, "Maintenance", -score);
            return -score; // FB - This actually returns the negative score
        }

        float EvalFlatFood(Building b)
        {
            float score = 0;
            if (b.PlusFlatFoodAmount.AlmostZero() || IsCybernetic)
                return 0;
            if (b.PlusFlatFoodAmount < 0)
                score = b.PlusFlatFoodAmount * 2;   //For negative Flat Food (those crazy modders...)
            else
            {
                float farmers = Food.WorkersNeededForEquilibrium();
                score += ((b.PlusFlatFoodAmount / MaxPopulationBillion) * 1.5f).Clamped(0.0f, 1.5f);   //Percentage of population this will feed, weighted
                score += 1.5f - (Food.YieldPerColonist/2);//Bonus for low Effective Fertility
                if (farmers.AlmostZero())
                    score += b.PlusFlatFoodAmount; //Bonus if planet is relying on flat food
                if (farmers > 0.5f)
                    score += farmers - 0.5f;            //Bonus if planet is spending a lot of labor feeding itself
                if (score < b.PlusFlatFoodAmount * 0.1f)
                    score = b.PlusFlatFoodAmount * 0.1f; //A little flat food is always useful
                if (b.PlusFlatFoodAmount + Food.FlatBonus - 0.5f > MaxPopulationBillion)
                    score = 0;   //Dont want this if a lot would go to waste
                score += 10 - Food.NetMaxPotential; // FB - jump start a new colony, as the colony grows, it will get negative and scrapped
            }
            DebugEvalBuild(b, "FlatFood", score);
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
            if (b.PlusFoodPerColonist.AlmostZero() || IsCybernetic)
                return 0;

            float score = 0;
            if (b.PlusFoodPerColonist < 0)
                score = MaxFoodGainPerColonist(b, 2.0f);
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

        float EvalFlatProd(Building b, float popRatio)
        {
            if (b.PlusFlatProductionAmount.AlmostZero())
                return 0;

            float score = 0;
            if (b.PlusFlatProductionAmount < 0)
                score = b.PlusFlatProductionAmount * 2;
            else
            {
                if (IsCybernetic)
                    score += b.PlusFlatProductionAmount / MaxPopulationBillion;     //Percentage of the filthy Opteris population this will feed
                
                score += (0.5f - popRatio).Clamped(0.0f, 0.5f);   //Bonus if population is currently less than half of max population
                score += 1.5f - Prod.YieldPerColonist;      //Bonus for low richness planets
                score += (0.66f - MineralRichness).Clamped(0.0f, 0.66f);      //More Bonus for really low richness planets
                
                float currentOutput = Prod.YieldPerColonist * LeftoverWorkerBillions() + Prod.FlatBonus;  //Current Prod Output
                score += (b.PlusFlatProductionAmount / currentOutput).Clamped(0.0f, 2.0f);         //How much more this building will produce compared to labor prod
                if (score < b.PlusFlatProductionAmount * 0.1f)
                    score = b.PlusFlatProductionAmount * 0.1f; //A little production is always useful

                score += 15 - Prod.NetMaxPotential /2; // FB - jump start a new colony, as the colony grows, it will get negative and scrapped
            }

            DebugEvalBuild(b, "FlatProd", score);
            return score;
        }
        float EvalProdPerCol(Building b)
        {
            if (b.PlusProdPerColonist.AlmostZero() || PopulationRatio < 0.8f)
                return 0;

            float score = MaxProdGainPerColonist(b, b.PlusProdPerColonist < 0 ? 2.0f : LeftoverWorkers());
            DebugEvalBuild(b, "ProdPerCol", score);
            return score;
        }

        // This one can produce a pretty high building value,
        // which is normally offset by its huge maintenance cost and Fertility loss
        float EvalProdPerRichness(Building b)
        {
            if (b.PlusProdPerRichness.AlmostZero())
                return 0;

            float score = 0;
            if (b.PlusProdPerRichness < 0)
                score = b.PlusProdPerRichness * MineralRichness * 2;
            else
            {
                score += b.PlusProdPerRichness * MineralRichness; // Production this would generate
                if (!HasSpacePort) score *= 0.75f; // Do we have a use for all this production?
            }
            DebugEvalBuild(b, "ProdPerRich", score);
            return score;
        }

        float EvalStorage(Building b)
        {
            if (b.StorageAdded == 0)
                return 0;

            float desiredStorage = 70.0f;
            if (Food.YieldPerColonist >= 2.5f || Prod.YieldPerColonist >= 2.5f || Prod.FlatBonus > 5)
                desiredStorage += 100.0f;  //Potential high output

            if (HasSpacePort)
                desiredStorage += 200.0f; // For buildin' ships 'n shit

            float score = 0;
            if (Storage.Max < desiredStorage)
                score += (b.StorageAdded * 0.02f); // If we need more storage, rate this building
            else
                score += (b.StorageAdded * 0.01f);

            if (b.Maintenance > 0)
                score *= 0.5f; // Prefer free storage

            DebugEvalBuild(b, "StorageAdd", score);
            return score;
        }

        float EvalPopulationGrowth(Building b)
        {
            if (b.PlusFlatPopulation.AlmostZero())
                return 0;

            float score = 0;
            if (b.PlusFlatPopulation < 0)
                score = b.PlusFlatPopulation * 0.02f;  // Which is sorta like 0.01f * 2
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
            if (b.MaxPopIncrease.AlmostZero())
                return 0;

            float score = 0;
            if (b.MaxPopIncrease < 0)
                score = b.MaxPopIncrease * 0.002f; // Which is sorta like 0.001f * 2
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

        float EvalFlatResearch(Building b, float budget)
        {
            if (b.PlusFlatResearchAmount.AlmostZero())
                return 0;

            float score = 0.001f;
            if (b.PlusFlatResearchAmount < 0) // Surly no one would make a negative research building
            {
                if (Res.Percent > 0 || Res.FlatBonus > 0) score += b.PlusFlatResearchAmount * 2;
                else score += b.PlusFlatResearchAmount;
            }
            else
            {   // Can we easily afford this
                if (b.ActualMaintenance(this) * 1.5f <= budget)
                    score += b.PlusFlatResearchAmount * 2;

                score = Math.Max(score, b.PlusFlatResearchAmount * 0.1f); // A little extra is always useful
            }
            DebugEvalBuild(b, "FlatResearch", score);
            return score;
        }

        float EvalResearchPerCol(Building b)
        {
            if (b.PlusResearchPerColonist.AlmostZero() || PopulationRatio < 0.8f)
                return 0;
            
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
            if (b.CreditsPerColonist.AlmostZero())
                return 0;

            float score;
            if (b.CreditsPerColonist < 0)
                score = b.CreditsPerColonist * MaxPopulationBillion * 2.0f;
            else // Don't want to cause this to have building preference over infrastructure buildings
                score = b.CreditsPerColonist * PopulationBillion * 0.5f;        

            DebugEvalBuild(b, "CredsPerCol", score);
            return score;
        }

        float EvalPlusTaxPercent(Building b)
        {
            if (b.PlusTaxPercentage.AlmostZero())
                return 0;

            // FB - governments really like tax buildings. This power based calc will make to building too valuable
            // not to build or scrap if pop is high
            float score = b.PlusTaxPercentage * 2 * (float)Math.Pow(PopulationBillion / 2 ,2) - b.Maintenance;

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
                score += 4.0f; // we can't be a space-faring species if our capital doesn't have a space-port...

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
            if (b.PlusTerraformPoints.AlmostZero() || Category == PlanetCategory.Terran
                                                   || Category == PlanetCategory.Oceanic)
                return 0; // FB - note that Terafformers are automatically scraped when they finish their job (not here)

            float score = 0;
            if (Owner.Money < 0)
                return score; // lets not commit to stuff which can overthrow our empire

            switch (Category)
            {
                case PlanetCategory.Volcanic:
                case PlanetCategory.Barren: score += 20; break;
                case PlanetCategory.Swamp:  score += 5;  break;
                case PlanetCategory.Steppe: score += 2;  break;
                default:                    score += 10; break;
            }

            DebugEvalBuild(b, "Terraformer", score);
            return score;
        }

        float EvalFertilityLoss(Building b)
        {
            if (b.MinusFertilityOnBuild.AlmostZero() || IsCybernetic) return 0;

            float score = 0;
            if (b.MinusFertilityOnBuild < 0)
                score = b.MinusFertilityOnBuild * 2;    //Negative loss means positive gain!!
            else
            {   
                // How much fertility will actually be lost
                // @todo food calculation is a bit dodgy
                float fertLost = Math.Min(Fertility, b.MinusFertilityOnBuild);
                float foodFromLabor = MaxPopulationBillion * ((Fertility - fertLost));
                float foodFromFlat = Food.FlatBonus + b.PlusFlatFoodAmount;
                // Will we still be able to feed ourselves?
                if (foodFromFlat + foodFromLabor < MaxConsumption)
                    score += fertLost * 20;
                else 
                    score += fertLost * 4;
            }

            DebugEvalBuild(b, "FertLossOnBuild", -score);
            return -score; // FB - this is actually negative
        }

        float ConstructionCostModifier(float score, float cost, float highestCost)
        {
            if (PopulationBillion.GreaterOrEqual(15))
                return score; // no factor is needed for big colonies

            //1 minus cost divided by highestCost gives a decimal value that is higher for smaller construction cost. This will make buildings with lower cost more desirable,
            //but never disqualify a building that had a positive score to begin with. -Gretman
            // FB - so highest cost building will get a factor of 0 (this is a multiplier) and the lowest cost building will get a factor of 1.
            // A building with a cost of 1/4 from the highest cost will get a factor of 0.75 to its score and so on.
            highestCost = highestCost.Clamped(50, 200);
            float factor = (1f - cost / highestCost).Clamped(0.01f, 1.0f); // so highest cost building will get a factor of 0 (this is a multiplier). 
            if (IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.DarkGray,$"--- Construction Factor phase 1: {factor}");

            // FB- this is a very powerful factor. So i added the factor's strength based on population. It will be used fully when the pop is low
            // and it wont used at all when the planet has full pop. So the planet can build expensive buildings as well
            factor = 1 - (1 - (PopulationBillion / 15).Clamped(0,1)) * factor;
            if (IsPlanetExtraDebugTarget())
            {
                Log.Info(ConsoleColor.DarkGray, $"--- Construction Factor phase 2: {factor}");
                Log.Info(ConsoleColor.DarkGray, $"--- TOTAL SCORE after Construction Factor: {score * factor} ---");
            }
            return score * factor;
        }

        float EvalCommon (Building b, float budget, int desiredMilitary, int existingMilitary)
        {
            float score = 0.0f;

            score += EvalbyGovernor(b);
            score += EvalFlatProd(b, PopulationRatio);
            score += EvalProdPerCol(b);
            score += EvalProdPerRichness(b);
            score += EvalStorage(b);
            score += EvalPopulationGrowth(b);
            score += EvalPlusMaxPopulation(b);
            score += EvalResearchPerCol(b);
            score += EvalPlusTaxPercent(b);
            score += EvalSpacePort(b);
            score += EvalMilitaryBuilding(b, desiredMilitary, existingMilitary);
            score += EvalFlatFood(b);
            score += EvalFoodPerCol(b);
            score += EvalFlatResearch(b, budget);
            score += EvalCreditsPerCol(b);
            score += EvalTerraformer(b);
            score += EvalCostVsBuildTime(b);
            score += EvalFertilityLoss(b);

            return score;
        }

        float EvaluateBuilding(Building b, float budget, float highestCost,
            int desiredMilitary, int existingMilitary, bool withMaint = true)     //Gretman function, to support DoGoverning()
        {
            float score = 0;
            if (withMaint)
            {
                float maint = b.Maintenance + b.Maintenance * Owner.data.Traits.MaintMod;
                if (maint.GreaterOrEqual(budget))
                    return -1; // building cannot be built since it has higher maint than budget

                score = EvalMaintenance(b, budget);
            }
            else if (b.ActualMaintenance(this).AlmostEqual(0))
                score += 10; // we are looking worst buildings with actual maintenance cost

            score += EvalCommon(b, budget, desiredMilitary, existingMilitary);
            if (score > 0)
                score = ConstructionCostModifier(score, b.ActualCost, highestCost);

            if (!IsPlanetExtraDebugTarget())
                return score;

            if (score > 0.0f)
                Log.Info(ConsoleColor.Cyan,    $"Eval BUILD  {b.Name,-20}  {"SUITABLE",-16} {score.SignString()}");
            else
                Log.Info(ConsoleColor.DarkRed, $"Eval BUILD  {b.Name,-20}  {"NOT GOOD",-16} {score.SignString()}");
            return score;
        }

        float EvalbyGovernor(Building b) // by Fat Bastard
        {
            float score = -1; // Because governors do not like to spend money on things not close to their hearts.
            if (b.IsMilitary) // military buildings are cool for all governors, even cooler if there was combat here recently
                score += RecentCombat ? 2f : 1f;

            score += b.StorageAdded * 0.01f // but they like a little storage for a rainy day and also money $$$
                  + b.PlusTaxPercentage * 5
                  + b.CreditsPerColonist;
            switch (colonyType)
            {
                case ColonyType.Agricultural:
                    score += b.PlusFlatFoodAmount +2
                             + b.PlusFoodPerColonist * Fertility * 2
                             + b.MaxPopIncrease / 1000 * 0.5f
                             + b.PlusFlatProductionAmount // some flat production as most people will be farming
                             -b.MinusFertilityOnBuild * 20; // we dont want reducing fertility and we really want improving it
                    break;
                case ColonyType.Core:
                    score += 1; // Core governors are open to different building functions
                    score += + b.CreditsPerColonist * 2
                             + b.PlusTaxPercentage * 10
                             + b.MaxPopIncrease / 1000 * 2
                             + b.PlusFlatPopulation / 10
                             - b.MinusFertilityOnBuild * 12;
                    break;
                case ColonyType.Industrial:
                     if (b.PlusProdPerColonist > 0 || b.PlusFlatProductionAmount > 0 || b.PlusProdPerRichness > 0)
                         score += 1f;
                    score += b.PlusProdPerColonist * MineralRichness 
                             + b.PlusFlatProductionAmount +1
                             + b.PlusProdPerRichness * MineralRichness
                             + b.PlusFlatFoodAmount * 0.5f; // some flat food as most people will be in production
                    break;
                case ColonyType.Research:
                    score += b.PlusResearchPerColonist * 4
                             + b.MaxPopIncrease / 1000
                             + b.PlusFlatResearchAmount * 2
                             - b.MinusFertilityOnBuild * 10;
                    break;
                case ColonyType.Military:
                    score += b.IsMilitary ? 2f : 0f; // yes, more military buildings!
                    score += b.AllowInfantry || b.AllowShipBuilding ? 2f : 0f;
                    break;
            }
            // The influence of the Governor increase as the colony increases.
            score *= (PopulationBillion / 10).Clamped(0.5f,2); 

            DebugEvalBuild(b, "Governor", score);
            return score;
        }

        float EvalCostVsBuildTime(Building b)
        {
            if (b.ActualCost.LessOrEqual(0)) return 0;
            float netCost = Math.Max(b.ActualCost - Storage.Prod / 2 * (1 - PopulationRatio), 0);
            if (netCost.AlmostZero())
                return 0;

            float score = 0;

            float ratio = b.ActualCost / Math.Max(Prod.NetMaxPotential / 3, 0.5f);
            if (ratio > 100)
                score = -20;
            else if (ratio > 50)
                score = -10;
            else if (ratio > 20)
                score = -1;

            DebugEvalBuild(b, "Cost VS Time", score);
            return score;
        }

        float EvalMilitaryBuilding(Building b, int desiredMilitary, int existingMilitary)
        {
            if (!b.IsMilitary || b.IsCapitalOrOutpost)
                return 0;

            float safety         = (float)Math.Pow(desiredMilitary - existingMilitary,2) * (RecentCombat ? 1.5f : 0.8f);
            float combatScore    = (b.Strength + b.Defense + b.CombatStrength + b.SoftAttack + b.HardAttack) / 100f;

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
            float shieldScore = b.PlanetaryShieldStrengthAdded / 300;
            float allowTroops = 0;
            if (b.AllowInfantry)
                allowTroops = colonyType == ColonyType.Military ? 1.0f : 0.5f;

            float invadeScore      = b.InvadeInjurePoints;
            float defenseShipScore = CalcDefenseShipScore(b);

            // Shield, weapon, and/or allow troop weighting go here (which is why they are all seperate values)

            // Factor by current population, so military buildings will be delayed
            float ratingFactor = ((PopulationRatio - 0.5f) * 2.0f).Clamped(0.0f, 1.0f);
            float finalRating  = (combatScore + dps + shieldScore + allowTroops + invadeScore + defenseShipScore + safety) * ratingFactor;

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
                case ShipData.RoleName.drone:    defenseShipScore = 0.05f; break;
                case ShipData.RoleName.fighter:  defenseShipScore = 0.1f;  break;
                case ShipData.RoleName.corvette: defenseShipScore = 0.2f;  break;
                case ShipData.RoleName.frigate:  defenseShipScore = 0.4f;  break;
                default:                         defenseShipScore = 1f;    break;
            }
            return defenseShipScore * b.DefenseShipsCapacity;
        }

        Building ChooseBestBuilding(Array<Building> buildings, float budget, float popRatio, out float value)
        {
            if (buildings.Count == 0) 
            {
                value = 0;
                return null;
            }
            if (IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Cyan,$"==== Planet  {Name}  CHOOSE BEST BUILDING, Budget: {budget} ====");

            Building best        = null;
            float bestValue      = 0f; // So a building with a value of 0 will not be built.
            float highestCost    = buildings.FindMax(building => building.ActualCost).ActualCost;
            int desiredMilitary  = DesiredMilitaryBuildings;
            int existingMilitary = ExistingMilitaryBuildings;

            for (int i = 0; i < buildings.Count; i++)
            {
                //Find the building with the highest score
                Building b = buildings[i];
                float buildingScore = EvaluateBuilding(b, budget, highestCost, desiredMilitary, existingMilitary);
                if (buildingScore > bestValue)
                {
                    best      = b;
                    bestValue = buildingScore;
                }
            }
            if (best != null && IsPlanetExtraDebugTarget())
                Log.Info($"-- Planet {Name}: Best Buidling is {best.Name} with score of {bestValue} Budget: {budget} -- ");

            value = bestValue;
            return best;
        }

        Building ChooseWorstBuilding(Array<Building> buildings, float budget, float popRatio, 
            out float value, float threshold)
        {
            if (IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Magenta, $"==== Planet  {Name}  CHOOSE WORST BUILDINGS, Budget: {budget} ====");

            Building worst   = null;
            float worstValue = threshold;
            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                if (b.IsBiospheres || !b.Scrappable || b.IsPlayerAdded) // might remove the isplayeradded
                    continue;

                Building highestCostBuilding = BuildingsCanBuild.FindMax(building => building.ActualCost);
                if (highestCostBuilding == null)
                {
                    value = worstValue;
                    return worst;
                }

                int desiredMilitary  = DesiredMilitaryBuildings;
                int existingMilitary = ExistingMilitaryBuildings;

                if (b.ActualMaintenance(this).AlmostZero())
                    continue; // we are looking to scrap buildings which actually cost maintenance to our Empire
                float buildingScore = EvaluateBuilding(b, budget, highestCostBuilding.ActualCost, desiredMilitary, existingMilitary, withMaint: false);
                if (buildingScore < worstValue)
                {
                    worst      = b;
                    worstValue = buildingScore;
                }
            }
            if (IsPlanetExtraDebugTarget())
                Log.Info(worst == null
                    ? $"-- Planet {Name}: No Worst Buidling was found --"
                    : $"-- Planet {Name}: Worst Buidling is  {worst.Name} with score of {worstValue} -- ");

            value = worstValue;
            return worst;
        }

        bool SimpleBuild(float budget, float popRatio) // build a building with a positive value
        {
            if (BuildingInTheWorks)
                return false;

            Building bestBuilding = ChooseBestBuilding(BuildingsCanBuild, budget, popRatio, out float bestValue);
            if (bestBuilding != null && bestValue > 0)
                AddBuildingToCQ(bestBuilding);

            return bestBuilding != null; ;
        }

        bool BuildBiospheres(float budget, int totalBuildings)
        {
            Building bio = GetBiospheresWeCanBuild;
            if (bio != null && !BiosphereInTheWorks && totalBuildings < MaxBuildings
                && bio.Maintenance < budget + 0.3f) //No habitable tiles, and not too much in debt
            {
                if (IsPlanetExtraDebugTarget())
                    Log.Info(ConsoleColor.Green, $"{Owner.PortraitName} BUILT {bio.Name} on planet {Name}");
                AddBuildingToCQ(bio);
            }
            return bio != null;
        }

        bool ScrapBuilding(float budget, float popRatio, float scoreThreshold = float.MaxValue)
        {
            Building worstBuilding = ChooseWorstBuilding(BuildingList, budget, popRatio, out float worstWeHave, scoreThreshold);
            if (worstBuilding != null)
            {
                Log.Info(ConsoleColor.Blue, $"{Owner.PortraitName} SCRAPPED {worstBuilding.Name} on planet {Name}   value: {worstWeHave}");
                worstBuilding.ScrapBuilding(this); // scrap the worst building  we have on the planet
            }
            return worstBuilding != null;
        }

        void BuildandScrapBuildings(float budget)
        {
            int totalBuildings       = TotalBuildings;
            float popRatio           = PopulationRatio;
            bool lotsInQueueToBuild  = ConstructionQueue.Count >= 4;

            //New Build Logic by Gretman, modified by FB
            if (!lotsInQueueToBuild) BuildShipyardIfAble(); //If we can build a shipyard but dont have one, build it
            // FB the above functio is buggy i think, need to check

            if (budget < 0)
            {
                ScrapBuilding(budget, popRatio); // we must scrap something to bring us above of our debt tolerance
                return; 
            }

            ScrapBuilding(budget, popRatio, scoreThreshold: 0); // scap a negative value building
            if (OpenTiles > 0)
            {
                SimpleBuild(budget, popRatio); // lets try to build something within our debt tolerance
                return;
            }

            BuildBiospheres(budget, totalBuildings); // lets build biospheres if we, can since we have no open tiles
        }

        void BuildShipyardIfAble()
        {
            if (RecentCombat || !HasSpacePort) return;
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
                    sData  = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].shipData,
                    Cost   = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].GetCost(Owner) 
                });
            }
        }

        float BuildingBudget()
        {
            // FB this will give the budget the colony will have for building selection
            float colonyIncome  = Money.NetRevenue;
            colonyIncome       -= SbProduction.GetTotalConstructionQueueMaintenance(); // take into account buildings maint in queue
            float debtTolerance = (5 - PopulationBillion).Clamped(-2,5); // the bigger the colony, the less debt tolerance it has, it should be earning money 
            if (Owner.Money < 0)
                debtTolerance -= 3; // if the empire is in debt, we must scrap staff
            return colonyIncome + debtTolerance;
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