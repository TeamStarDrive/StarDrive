﻿using System;
using System.Linq;

namespace Ship_Game.AI.Research
{
    public struct ResearchPriorities
    {
        public float ResearchDebt { get; }
        public float Wars { get; }
        public float Economics { get; }
        readonly Empire OwnerEmpire;
        public readonly string TechCategoryPrioritized;
        public float BuildCapacity { get; private set; }
        public string Command { get; private set; }
        public string Command2 { get; private set; }

        public ResearchPriorities(Empire empire, float buildCapacity, string command, string command2) : this()
        {
            OwnerEmpire          = empire;
            Wars                 = 0;
            float shipBuildBonus = 0;
            BuildCapacity        = buildCapacity;
            Command              = command;
            Command2             = command2;

            float enemyThreats = empire.GetEmpireAI().ThreatMatrix.HighestStrengthOfAllEmpireThreats(empire);
            enemyThreats      += empire.TotalRemnantStrTryingToClear();
            Wars               = enemyThreats / empire.CurrentMilitaryStrength.LowerBound(1);
            Wars               = Wars.Clamped(0, 1);

            ResearchDebt = 0;
            var availableTechs = OwnerEmpire.CurrentTechsResearchable();
            float workerEfficiency = empire.Research.NetResearch / empire.Research.MaxResearchPotential.LowerBound(1);
            if (availableTechs.NotEmpty)
            {
                //calculate standard deviation of tech costs. remove extreme highs and lows in average
                float avgTechCost = availableTechs.Average(cost => cost.TechCost);
                avgTechCost       = availableTechs.Sum(cost => (float)Math.Pow(cost.TechCost - avgTechCost, 2));
                avgTechCost      /= availableTechs.Count;
                avgTechCost       = (float)Math.Sqrt(avgTechCost);
                //use stddev of techcost to determine how behind we are in tech

                float techCostRatio = avgTechCost / empire.Research.NetResearch.LowerBound(1);
                ResearchDebt = techCostRatio / 50f; //divide by 50 turns.

                ResearchDebt = ResearchDebt.Clamped(0, 1);
            }

            Economics = empire.data.TaxRate + workerEfficiency;
            float foodNeeds = 0;
            float industry  = 0;
            int planets     = empire.GetPlanets().Count;
            if (planets > 0)
            {
                foreach (Planet planet in empire.GetPlanets())
                {
                    foodNeeds += CalcPlanetFoodNeeds(planet);
                    industry  += CalcPlanetProdNeeds(planet);
                }

                if (empire.IsCybernetic)
                {
                    industry  = ((industry + foodNeeds) / 2).Clamped(0, 1.5f);
                    foodNeeds = 0;
                }

                industry  /= planets;
                foodNeeds /= planets;

                int totalFreighters     = empire.TotalFreighters.LowerBound(1);
                int totalFoodFreighters = empire.GetPlanets().Sum(planet => planet.IncomingFoodFreighters);
                int totalProdFreighters = empire.GetPlanets().Sum(planet => planet.IncomingProdFreighters);

                foodNeeds = (foodNeeds + totalFoodFreighters / totalFreighters) / 2;
                industry  = (industry + totalProdFreighters / totalFreighters) / 2;

            }
            TechCategoryPrioritized = "TECH";

            Wars += OwnerEmpire.GetEmpireAI().TechChooser.LineFocus.BestShipNeedsHull(availableTechs) ? 0.5f : 0;
            EconomicResearchStrategy strat = empire.Research.Strategy;

            var priority = new Map<string, int>
            {
                { "SHIPTECH",     Randomizer(strat.MilitaryRatio, Wars)        },
                { "Research",     Randomizer(strat.ResearchRatio, ResearchDebt)},
                { "Colonization", Randomizer(strat.ExpansionRatio, foodNeeds)  },
                { "Economic",     Randomizer(strat.ExpansionRatio, Economics)  },
                { "Industry",     Randomizer(strat.IndustryRatio, industry)    },
                { "General",      Randomizer(strat.ResearchRatio, 0)           },
                { "GroundCombat", Randomizer(strat.MilitaryRatio, Wars * .5f)  },
            };

            int maxNameLength = priority.Keys.Max(name => name.Length);
            maxNameLength += 5;
            foreach (var kv in priority) DebugLog($"{kv.Key.PadRight(maxNameLength, '.')} {kv.Value}");


            int max = 0;
            foreach (var pWeighted in priority.OrderByDescending(weight => weight.Value))
            {
                if (max > 6)
                    break;
                if (pWeighted.Value < 0)
                    continue;

                TechCategoryPrioritized += ":";
                if (pWeighted.Key == "SHIPTECH")
                {
                    TechCategoryPrioritized += GetShipTechString();
                    max += 3;
                }
                else
                {
                    TechCategoryPrioritized += pWeighted.Key;
                    max++;
                }
            }
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

        private int Randomizer(float priority, float bonus)
        {
            int b = (int)(bonus * 100);
            int p = (int)(priority * 100);
            return RandomMath.AvgRandomBetween(b, p + b);

        }

        private void DebugLog(string text) => Empire.Universe?.DebugWin?.ResearchLog(text, OwnerEmpire);
    }

}
