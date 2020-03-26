using System;
using System.Linq;

namespace Ship_Game.AI.Research
{
    public struct ResearchPriorities
    {
        public float ResearchDebt { get; }
        public float Wars { get; }
        public float Economics { get; }
        public float BuildCapacity { get; }
        public string Command { get; }
        public string Command2 { get; }
        private readonly float FoodNeeds;
        private readonly float Industry;
        public readonly string TechCategoryPrioritized;
        private readonly Empire OwnerEmpire;

        public ResearchPriorities(Empire empire, float buildCapacity, string command, string command2) : this()
        {
            OwnerEmpire          = empire;
            BuildCapacity        = buildCapacity;
            Command              = command;
            Command2             = command2;
            ResearchDebt         = CalcReserchDebt(empire, out Array<TechEntry> availableTechs);
            Wars                 = CalcWars(empire, availableTechs);
            Economics            = CalcEconomics(empire);

            CalcFoodAndIndustry(empire, out FoodNeeds, out Industry);
            Map<string, int> priority = CreatePriorityMap(empire);
            TechCategoryPrioritized   = CreateTechString(priority);
            AddDebugLog(priority);
        }

        string CreateTechString(Map<string, int> priority)
        {
            string techCategoryPrioritized = "TECH";
            int max = 0;
            foreach (var pWeighted in priority.OrderByDescending(weight => weight.Value))
            {
                if (max > 6)
                    break;
                if (pWeighted.Value < 0)
                    continue;

                techCategoryPrioritized += ":";
                if (pWeighted.Key == "SHIPTECH")
                {
                    techCategoryPrioritized += GetShipTechString();
                    max += 3;
                }
                else
                {
                    techCategoryPrioritized += pWeighted.Key;
                    max++;
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
                { "SHIPTECH",     Randomizer(strat.MilitaryRatio,  Wars)        },
                { "Research",     Randomizer(strat.ResearchRatio,  ResearchDebt)},
                { "Colonization", Randomizer(strat.ExpansionRatio, FoodNeeds)   },
                { "Economic",     Randomizer(strat.ExpansionRatio, Economics)   },
                { "Industry",     Randomizer(strat.IndustryRatio,  Industry)    },
                { "General",      Randomizer(strat.ResearchRatio,  0)           },
                { "GroundCombat", Randomizer(strat.MilitaryRatio,  Wars * .5f)  },
            };

            return priority;
        }

        float CalcWars(Empire empire, Array<TechEntry> availableTechs)
        {
            float enemyThreats = empire.GetEmpireAI().ThreatMatrix.HighestStrengthOfAllEmpireThreats(empire);
            enemyThreats      += empire.TotalRemnantStrTryingToClear();
            float wars         = enemyThreats / empire.CurrentMilitaryStrength.LowerBound(1);
            wars              += OwnerEmpire.GetEmpireAI().TechChooser.LineFocus.BestShipNeedsHull(availableTechs) ? 0.5f
                                                                                                                   : 0;

            return wars.Clamped(0, 1);
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

        float CalcReserchDebt(Empire empire, out Array<TechEntry> availableTechs)
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
            needs       = (needs * p.Level / 5).LowerBound(0);
            if (p.IsStarving)
                needs += 0.5f;

            return needs;
        }

        float CalcPlanetProdNeeds(Planet p)
        {
            float productionNeedToComplete = p.TurnsUntilQueueCompleted * p.Prod.NetIncome;
            float needs = 1 - p.EstimatedAverageProduction / (productionNeedToComplete.LowerBound(1));

            return (needs * p.Level / 5).LowerBound(0);
        }

        string GetShipTechString()
        {
            string shipTechToAdd = "";
            int shipTechs = RandomMath.IntBetween(1, 4);
            switch (shipTechs)
            {
                case 1: shipTechToAdd = "ShipHull:ShipDefense:ShipWeapons:ShipGeneral"; break;
                case 2: shipTechToAdd = "ShipDefense:ShipWeapons:ShipHull";             break;
                case 3: shipTechToAdd = "ShipWeapons:ShipHull:ShipDefense";             break;
                case 4: shipTechToAdd = "ShipHull:ShipGeneral:ShipDefense:ShipWeapons"; break;
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
