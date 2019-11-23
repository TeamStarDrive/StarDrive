// ReSharper disable once CheckNamespace

using Ship_Game.AI.Budget;
using System;

namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {        
        private float FindTaxRateToReturnAmount(float amount)
        {
            for (int i = 0; i < 100; i++)
            {
                if (OwnerEmpire.EstimateNetIncomeAtTaxRate(i / 100f) >= amount)
                {
                    return i / 100f;
                }
            }
            return 1;
        }

        private void RunEconomicPlanner()
        {
            float money                    = OwnerEmpire.Money.ClampMin(1.0f);
            float treasuryGoal             = TreasuryGoal();
            AutoSetTaxes(treasuryGoal);

            float risk = GetRisk();
            OwnerEmpire.data.DefenseBudget = DetermineDefenseBudget(risk);
            OwnerEmpire.data.SSPBudget     = DetermineSSPBudget();
            BuildCapacity                  = DetermineBuildCapacity(risk);
            OwnerEmpire.data.SpyBudget     = DetermineSpyBudget(risk);
            OwnerEmpire.data.ColonyBudget  = DetermineColonyBudget();


            PlanetBudgetDebugInfo();
        }
        float DetermineDefenseBudget(float risk)
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            return SetBudgetForeArea(0.0025f, Math.Max(risk, strat.MilitaryRatio));
        }
        float DetermineSSPBudget()
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            float risk = strat.IndustryRatio + strat.ExpansionRatio;
            return SetBudgetForeArea(0.0025f, risk);
        }

        float DetermineBuildCapacity(float risk)
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            float buildRatio = (strat.MilitaryRatio + strat.IndustryRatio + strat.ExpansionRatio) / 2f;

            return  SetBudgetForeArea(0.01f, Math.Max(risk, buildRatio));

        }

        float DetermineColonyBudget()
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            return SetBudgetForeArea(0.01f, strat.IndustryRatio + strat.ExpansionRatio);
        }
        float DetermineSpyBudget(float risk)
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            return SetBudgetForeArea(0.1500f, Math.Max(risk, strat.MilitaryRatio));
        }
        private void PlanetBudgetDebugInfo()
        {
            if (!Empire.Universe.Debug) return;

            var pBudgets = new Array<PlanetBudget>();
            foreach (var planet in OwnerEmpire.GetPlanets())
            {
                var planetBudget = new PlanetBudget(planet);
                pBudgets.Add(planetBudget);
            }

            PlanetBudgets = pBudgets;
        }

        private float TreasuryGoal()
        {
            float treasuryGoal = OwnerEmpire.data.treasuryGoal;
            if (!OwnerEmpire.isPlayer || OwnerEmpire.data.AutoTaxes)
            {
                //gremlin: Use self adjusting tax rate based on wanted treasury of 10(1 full year) of total income.
                treasuryGoal = Math.Max(OwnerEmpire.PotentialIncome, 0)
                               + OwnerEmpire.data.FlatMoneyBonus
                               + OwnerEmpire.TotalShipMaintenance / 5; //more savings than GDP 
            }
            treasuryGoal *= OwnerEmpire.data.treasuryGoal * 200;
            treasuryGoal = Math.Max(1000, treasuryGoal);
            return treasuryGoal;
        }

        private void AutoSetTaxes(float treasuryGoal)
        {
            if (OwnerEmpire.isPlayer && !OwnerEmpire.data.AutoTaxes)
                return;

            const float normalTaxRate = 0.25f;
            float treasuryGoalRatio   = (Math.Max(OwnerEmpire.Money, 100)) / treasuryGoal;
            float desiredTaxRate;
            if (treasuryGoalRatio.Greater(1))
                desiredTaxRate = -(float)Math.Round((treasuryGoalRatio - 1), 2); // this will decrease tax based on ratio
            else
                desiredTaxRate = (float)Math.Round(1 - treasuryGoalRatio, 2); // this will increase tax based on opposite ratio

            OwnerEmpire.data.TaxRate  = (normalTaxRate + desiredTaxRate).Clamped(0.05f,0.95f);
        }

#if DEBUG
        public Array<PlanetBudget> PlanetBudgets;
#endif

        private float SetBudgetForeArea(float percentOfIncome, float risk)
        {
            float budget = OwnerEmpire.Money * percentOfIncome * risk;
            budget = Math.Max(1, budget);
            return budget;
        }
        public float GetRisk(float riskLimit = 2f)
        {
            float risk = 0;
            foreach (var kv in OwnerEmpire.AllRelations)
            {
                var tRisk = kv.Value.Risk.Risk;
                risk      = Math.Max(tRisk, risk);
            }
            return risk;
        }

        public PlanetBudget PlanetBudget(Planet planet) => new PlanetBudget(planet);        
    }
}