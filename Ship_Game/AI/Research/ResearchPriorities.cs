using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI.Research
{
    public struct ResearchPriorities
    {
        public float ResearchDebt { get; }
        public float Wars { get; }
        public float Economics { get; }
        public float BuildCapacity { get; }
        private readonly float FoodNeeds;
        private readonly float Industry;
        private readonly float ShipHulls;
        public readonly string TechCategoryPrioritized;
        private readonly Empire OwnerEmpire;

        public ResearchPriorities(Empire empire, float buildCapacity) : this()
        {
            OwnerEmpire          = empire;
            BuildCapacity        = buildCapacity;
            ResearchDebt         = CalcResearchDebt(empire, out Array<TechEntry> availableTechs);
            Wars                 = CalcWars(empire, availableTechs);
            Economics            = CalcEconomics(empire);
            ShipHulls            = CalcShipHulls(Wars);

            CalcFoodAndIndustry(empire, out FoodNeeds, out Industry);
            Map<string, int> priority = CreatePriorityMap(empire);
            TechCategoryPrioritized   = CreateTechString(priority, availableTechs);
            AddDebugLog(priority);
        }

        string CreateTechString(Map<string, int> priority, Array<TechEntry> availableTechs)
        {
            string techCategoryPrioritized = "TECH";
            int maxStrings                 = 7;
            int numStrings                 = 0;
            foreach (var pWeighted in priority.OrderByDescending(weight => weight.Value))
            {
                if (numStrings > maxStrings)
                    break;

                if (pWeighted.Value < 0)
                    continue;

                // Filter out ship techs and lower num tech types if ship hull is wanted the most.
                if (techCategoryPrioritized == "TECH"
                    && pWeighted.Key == "ShipHull"
                    && availableTechs.Any(t => t.IsTechnologyType(ChooseTech.ConvertTechStringTechType(pWeighted.Key)))
                    && GlobalStats.HasMod && GlobalStats.ActiveModInfo.UseResearchableShipTechs)
                {
                    maxStrings = 1;
                }

                if (pWeighted.Key == "SHIPTECH")
                {
                    techCategoryPrioritized += GetShipTechString(availableTechs);
                    numStrings += 3;
                }
                else if (availableTechs.Any(t => t.IsTechnologyType(ChooseTech.ConvertTechStringTechType(pWeighted.Key))))
                {
                    techCategoryPrioritized += ":";
                    techCategoryPrioritized += pWeighted.Key;
                    numStrings++;
                }
            }

            return techCategoryPrioritized;
        }

        void AddDebugLog(Map<string, int> priority)
        {
            int maxNameLength = priority.Keys.Max(name => name.Length);
            maxNameLength    += 5;
            foreach (var kv in priority) 
                DebugLog($"{kv.Key.PadRight(maxNameLength, '.')} {kv.Value}");
        }

        Map<string, int> CreatePriorityMap(Empire empire)
        {
            EconomicResearchStrategy strat = empire.Research.Strategy;

            var priority = new Map<string, int>
            {
                { "SHIPTECH",     Randomizer(strat.MilitaryRatio,  Wars * 2)    },
                { "ShipHull",     Randomizer(strat.MilitaryRatio,  ShipHulls)   },
                { "Research",     Randomizer(strat.ResearchRatio,  ResearchDebt)},
                { "Colonization", Randomizer(strat.ExpansionRatio, FoodNeeds)   },
                { "Economic",     Randomizer(strat.ExpansionRatio, Economics)   },
                { "Industry",     Randomizer(strat.IndustryRatio,  Industry)    },
                { "General",      Randomizer(strat.ResearchRatio,  0)           },
                { "GroundCombat", Randomizer(strat.MilitaryRatio,  Wars * 0.5f) },
            };

            return priority;
        }

        float CalcWars(Empire empire, Array<TechEntry> availableTechs)
        {
            float enemyThreats = empire.GetEmpireAI().ThreatMatrix.HighestStrengthOfAllEmpireThreats(empire);
            enemyThreats      += empire.TotalRemnantStrTryingToClear();
            float wars         = enemyThreats / empire.OffensiveStrength.LowerBound(1);
            wars              += OwnerEmpire.GetEmpireAI().TechChooser.LineFocus.BestShipNeedsHull(availableTechs) ? 0.5f
                                                                                                                   : 0;

            return wars.Clamped(0, 1);
        }

        float CalcShipHulls(float wars)
        {
            float maxBonus = 0;
            foreach ((Empire them, Relationship rel) in OwnerEmpire.AllRelations)
            {
                if (!rel.Known && them.isFaction)
                    continue;

                float empireBonus = CalcCanBuildHulls(them);
                if (empireBonus > maxBonus)
                    maxBonus = empireBonus;
            }

            maxBonus = (maxBonus - CalcCanBuildHulls(OwnerEmpire)).LowerBound(0);
            return maxBonus + wars * 2;
        }

        float CalcCanBuildHulls(Empire empire)
        {
            float canBuildBonus = 0;
            if (empire.canBuildCorvettes) canBuildBonus += 0.5f;
            if (empire.canBuildFrigates)  canBuildBonus += 0.5f;
            if (empire.canBuildCruisers)  canBuildBonus += 0.5f;
            if (empire.canBuildCapitals)  canBuildBonus += 0.5f;

            return canBuildBonus;
        }

        void CalcFoodAndIndustry(Empire empire, out float foodNeeds, out float industry)
        {
            foodNeeds = 0;
            industry = 0;
            int planets = empire.GetPlanets().Count;
            if (planets > 0)
            {
                foreach (Planet planet in empire.GetPlanets())
                {
                    foodNeeds += CalcPlanetFoodNeeds(planet);
                    industry  += CalcPlanetProdNeeds(planet);
                }

                if (empire.IsCybernetic)
                {
                    industry = ((industry + foodNeeds) / 2).Clamped(0, 1.5f);
                    foodNeeds = 0;
                }

                industry  /= planets;
                foodNeeds /= planets;

                int totalFreighters     = empire.TotalFreighters.LowerBound(1);
                int totalFoodFreighters = empire.GetPlanets().Sum(planet => planet.IncomingFoodFreighters);
                int totalProdFreighters = empire.GetPlanets().Sum(planet => planet.IncomingProdFreighters);

                foodNeeds = (foodNeeds + (float)totalFoodFreighters / totalFreighters) / 2;
                industry  = (industry  + (float)totalProdFreighters / totalFreighters) / 2;
            }
        }

        float CalcEconomics(Empire empire)
        {
            float workerEfficiency = empire.Research.NetResearch / empire.Research.MaxResearchPotential.LowerBound(1);
            return empire.data.TaxRate + workerEfficiency;
        }

        float CalcResearchDebt(Empire empire, out Array<TechEntry> availableTechs)
        {
            float researchDebt = 0;
            availableTechs = OwnerEmpire.CurrentTechsResearchable();

            if (availableTechs.NotEmpty)
            {
                // Calculate standard deviation of tech costs. remove extreme highs and lows in average
                float avgTechCost = availableTechs.Average(cost => cost.TechCost);
                avgTechCost       = availableTechs.Sum(cost => (float)Math.Pow(cost.TechCost - avgTechCost, 2));
                avgTechCost       /= availableTechs.Count;
                avgTechCost       = (float)Math.Sqrt(avgTechCost);
                // Use stddev of tech cost to determine how behind we are in tech

                float techCostRatio = avgTechCost / empire.Research.NetResearch.LowerBound(1);
                researchDebt = techCostRatio / 50f; //divide by 50 turns.
                researchDebt = researchDebt.Clamped(0, 1);
            }

            return researchDebt;
        }

        float CalcPlanetFoodNeeds(Planet p)
        {
            float ratio = p.NonCybernetic ? p.Storage.FoodRatio : p.Storage.Prod;
            float needs = 1 - ratio;
            needs       = (needs * p.Level).LowerBound(0);
            if (p.IsStarving)
                needs += 0.5f;

            return needs;
        }

        float CalcPlanetProdNeeds(Planet p)
        {
            float productionNeedToComplete = p.TotalProdNeededInQueue();
            float needs = 1 - p.EstimatedAverageProduction / (productionNeedToComplete.LowerBound(1));

            return (needs * p.Level / 2).LowerBound(0);
        }

        string GetShipTechString(Array<TechEntry> availableTech)
        {
            string shipTechToAdd = "";
            Array<string> shipTech = new Array<string>();
            if (availableTech.Any(t => t.IsTechnologyType(ChooseTech.ConvertTechStringTechType("ShipWeapons"))))
                shipTech.Add("ShipWeapons");

            if (availableTech.Any(t => t.IsTechnologyType(ChooseTech.ConvertTechStringTechType("ShipDefense"))))
                shipTech.Add("ShipDefense");

            if (availableTech.Any(t => t.IsTechnologyType(ChooseTech.ConvertTechStringTechType("ShipGeneral"))))
                shipTech.Add("ShipGeneral");

            while (shipTech.Count > 0)
            {
                string techToAdd = shipTech.RandItem();
                shipTechToAdd   += $":{techToAdd}";
                shipTech.Remove(techToAdd);
            }

            return shipTechToAdd;
        }

        int Randomizer(float priority, float bonus)
        {
            int b = (int)(bonus * 100);
            int p = (int)(priority * 100);
            return RandomMath.AvgRandomBetween(b, p + b);

        }

        void DebugLog(string text) => Empire.Universe?.DebugWin?.ResearchLog(text, OwnerEmpire);
    }

}
