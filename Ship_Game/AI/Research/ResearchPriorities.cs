using System;
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

        public ResearchPriorities(Empire empire, float buildCapacity, string command, string command2)
        {
            OwnerEmpire    = empire;
            EconomicResearchStrategy resStrat = empire.ResearchStrategy;
            Wars           = 0;
            float shipBuildBonus = 0;
            BuildCapacity = buildCapacity;
            Command = command;
            Command2 = command2;            

            //create a booster for some values when things are slack.
            //so the empire will keep building new ships and researching new science.
            if (empire.data.TechDelayTime % 3 == 0)
                shipBuildBonus = 0.5f;

            float enemyThreats = empire.GetEmpireAI().ThreatMatrix.StrengthOfAllEmpireThreats(empire);
            Wars = enemyThreats / (empire.currentMilitaryStrength + 1);
            Wars = Wars.Clamped(0, 1);
            if (Wars < 0.5f)
                Wars = shipBuildBonus;

            ResearchDebt = 0;
            var availableTechs = OwnerEmpire.CurrentTechsResearchable();
            float workerEfficiency = empire.Research / empire.MaxResearchPotential;
            if (availableTechs.NotEmpty)
            {
                //calculate standard deviation of tech costs. remove extreme highs and lows in average
                float avgTechCost = availableTechs.Average(cost => cost.TechCost);
                avgTechCost = availableTechs.Sum(cost => (float)Math.Pow(cost.TechCost - avgTechCost, 2));
                avgTechCost /= availableTechs.Count;
                avgTechCost = (float)Math.Sqrt(avgTechCost);
                //use stddev of techcost to determine how behind we are in tech

                float techCostRatio = avgTechCost / empire.Research;
                ResearchDebt = techCostRatio / 100f; //divide by 100 turns.

                ResearchDebt = ResearchDebt.Clamped(0, 1);
            }

            Economics = empire.data.TaxRate + workerEfficiency;
            float foodNeeds = 0;
            float industry = 0;
            int planets = 0;

            foreach (Planet planet in empire.GetPlanets())
            {
                if (planet.Level < 2) continue;
                industry += 1 - planet.Storage.ProdRatio;
                if (!empire.IsCybernetic)
                    foodNeeds += planet.Food.Percent;
                else
                    industry += planet.Prod.Percent;
                planets++;
            }

            if (planets > 0)
            {
                if (!empire.IsCybernetic)
                    foodNeeds /= planets;
                industry /= planets;
            }

            TechCategoryPrioritized = "TECH";

           Wars += OwnerEmpire.GetEmpireAI().TechChooser.LineFocus.BestShipNeedsHull(availableTechs) ? 0.5f : 0;

            var priority = new Map<string, float>();
            priority.Add("SHIPTECH"    , Randomizer(resStrat.MilitaryRatio, Wars));
            priority.Add("Research"    , Randomizer(resStrat.ResearchRatio, ResearchDebt));
            priority.Add("Colonization", Randomizer(resStrat.ExpansionRatio, foodNeeds));
            priority.Add("Economic"    , Randomizer(resStrat.ExpansionRatio, Economics));
            priority.Add("Industry"    , Randomizer(resStrat.IndustryRatio, industry));
            priority.Add("General"     , Randomizer(resStrat.ResearchRatio, 0));
            priority.Add("GroundCombat", Randomizer(resStrat.MilitaryRatio, Wars * .5f));

            int maxNameLength = priority.Keys.Max(name => name.Length);
            maxNameLength += 5;
            foreach (var kv in priority) DebugLog($"{kv.Key.PadRight(maxNameLength, '.')} {kv.Value}");


            int max = 0;
            foreach (var pWeighted in priority.OrderByDescending(weight => weight.Value))
            {
                if (max > 4)
                    break;
                if (pWeighted.Value < 0)
                    continue;

                TechCategoryPrioritized += ":";
                if (pWeighted.Key == "SHIPTECH")
                {
                    TechCategoryPrioritized += "ShipWeapons:ShipDefense:ShipHull:ShipGeneral";
                    max += 3;
                }
                else
                {
                    TechCategoryPrioritized += pWeighted.Key;
                    max++;
                }
            }
        }
        private float Randomizer(float priority, float bonus)
        {
            return RandomMath.AvgRandomBetween(0, priority + bonus);

        }
        private void DebugLog(string text) => Empire.Universe?.DebugWin?.ResearchLog(text, OwnerEmpire);

    }

}
