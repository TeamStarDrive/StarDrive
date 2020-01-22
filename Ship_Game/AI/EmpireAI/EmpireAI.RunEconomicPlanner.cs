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

        public void RunEconomicPlanner()
        {
            float money                    = OwnerEmpire.Money.ClampMin(1.0f);
            float treasuryGoal             = TreasuryGoal();
            AutoSetTaxes(treasuryGoal);

            float gameState                = GetRisk().Clamped(0,1);
            OwnerEmpire.data.DefenseBudget = DetermineDefenseBudget(gameState, treasuryGoal);
            OwnerEmpire.data.SSPBudget     = DetermineSSPBudget(treasuryGoal);
            BuildCapacity                  = DetermineBuildCapacity(gameState, treasuryGoal);
            OwnerEmpire.data.SpyBudget     = DetermineSpyBudget(gameState, treasuryGoal);
            OwnerEmpire.data.ColonyBudget  = DetermineColonyBudget(treasuryGoal);

            PlanetBudgetDebugInfo();
        }
        float DetermineDefenseBudget(float risk, float money)
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            float adjustor = risk + strat.MilitaryRatio;
            return SetBudgetForeArea(0.01f, adjustor, money);
        }
        float DetermineSSPBudget(float money)
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            float risk = strat.IndustryRatio + strat.ExpansionRatio;
            return SetBudgetForeArea(0.0025f, risk, money);
        }

        float DetermineBuildCapacity(float risk, float money)
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            float buildRatio = strat.MilitaryRatio + strat.IndustryRatio + strat.ExpansionRatio;
            buildRatio /= 2;
            return  SetBudgetForeArea(0.01f, buildRatio + risk, money);

        }

        float DetermineColonyBudget(float money)
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            return SetBudgetForeArea(0.01f, strat.IndustryRatio + strat.ExpansionRatio, money);
        }

        float DetermineSpyBudget(float risk, float money)
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            return SetBudgetForeArea(0.15f, Math.Max(risk, strat.MilitaryRatio), money);
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

        public float TreasuryGoal()
        {
            //gremlin: Use self adjusting tax rate based on wanted treasury of 10(1 full year) of total income.
            float treasuryGoal = Math.Max(OwnerEmpire.PotentialIncome, 0)
                                 + OwnerEmpire.data.FlatMoneyBonus
                                 + OwnerEmpire.BuildingAndShipMaint / 5;

            treasuryGoal *= OwnerEmpire.data.treasuryGoal * 200;
            float minGoal = OwnerEmpire.isPlayer ? 100 : 1000;
            treasuryGoal = Math.Max(minGoal, treasuryGoal);
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

            OwnerEmpire.data.TaxRate  = (normalTaxRate + desiredTaxRate).Clamped(0.01f,0.95f);
        }

        public Array<PlanetBudget> PlanetBudgets;

        private float SetBudgetForeArea(float percentOfIncome, float risk, float money)
        {
            risk = OwnerEmpire.isPlayer ? 1 : risk;
            float budget = money * percentOfIncome * risk;
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