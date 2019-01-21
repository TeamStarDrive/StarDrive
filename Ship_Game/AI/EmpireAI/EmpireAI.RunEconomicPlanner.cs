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
            float risk = GetRisk();
            float money = OwnerEmpire.Money;
            money = money < 1 ? 1 : money;
            float treasuryGoal = TreasuryGoal();

            float goal = money / treasuryGoal;
            AutoSetTaxes(treasuryGoal, goal);

            float goalClamped = goal.Clamped(0, 1);
            var resStrat = OwnerEmpire.GetResStrat();
            float buildRatio = (resStrat.MilitaryRatio + resStrat.IndustryRatio + resStrat.ExpansionRatio) /2f;
            
            SetBudgetForeArea(goalClamped * .01f, ref OwnerEmpire.data.DefenseBudget, Math.Max(risk, resStrat.MilitaryRatio));            
            SetBudgetForeArea(goalClamped * .01f, ref OwnerEmpire.data.SSPBudget, resStrat.IndustryRatio + resStrat.ExpansionRatio);
            SetBudgetForeArea(goalClamped * .01f, ref BuildCapacity, Math.Max(risk, buildRatio));           
            SetBudgetForeArea(goalClamped * .1f, ref OwnerEmpire.data.SpyBudget, Math.Max(risk, resStrat.MilitaryRatio));
            SetBudgetForeArea(goalClamped * .01f, ref OwnerEmpire.data.ColonyBudget, resStrat.IndustryRatio + resStrat.ExpansionRatio);
#if DEBUG
            var pBudgets = new Array<PlanetBudget>();
            foreach (var empire in EmpireManager.Empires)
            {
             
                foreach (var planet in empire.GetPlanets())
                {
                    var pinfo = new PlanetBudget(planet);
                    pBudgets.Add(pinfo);
                }
            }
            PlanetBudgets = pBudgets;
#endif
        }

        private float TreasuryGoal()
        {
            float treasuryGoal = OwnerEmpire.data.treasuryGoal;
            if (!OwnerEmpire.isPlayer || OwnerEmpire.data.AutoTaxes)
            {
                //gremlin: Use self adjusting tax rate based on wanted treasury of 10(1 full year) of total income.
                treasuryGoal = OwnerEmpire.NetPlanetIncomes 
                               + OwnerEmpire.TradeMoneyAddedThisTurn 
                               + OwnerEmpire.data.FlatMoneyBonus
                               + OwnerEmpire.TotalShipMaintenance; //more savings than GDP 
            }
            treasuryGoal *= (OwnerEmpire.data.treasuryGoal * 1000);
            treasuryGoal = Math.Max(1000, treasuryGoal);
            return treasuryGoal;
        }

        private void AutoSetTaxes(float treasuryGoal, float goalClamped)
        {
            if (OwnerEmpire.isPlayer && !OwnerEmpire.data.AutoTaxes) return;
            float treasuryGoalRatio = 1 - goalClamped;
            treasuryGoal *= treasuryGoalRatio;
            float tempTax = FindTaxRateToReturnAmount(treasuryGoal);
            if (tempTax - OwnerEmpire.data.TaxRate > .02f)
                OwnerEmpire.data.TaxRate += .02f;
            else
                OwnerEmpire.data.TaxRate = tempTax;

            OwnerEmpire.data.TaxRate = OwnerEmpire.data.TaxRate.Clamped(0.25f, 0.75f); // FB - temp hack until this code is refactored. no chance tax can be 0%
        }
#if DEBUG
        public Array<PlanetBudget> PlanetBudgets;
#endif 
        private float SetBudgetForeArea(float percentOfIncome, ref float area, float risk)
        {
            float budget = OwnerEmpire.Money * percentOfIncome * risk;
            
            budget = Math.Max(1, budget);
            area = budget;
            return budget;
        }
        public float GetRisk(float riskLimit =.75f)
        {
            float risk = 0;
            foreach (var kv in OwnerEmpire.AllRelations)
            {
                risk += kv.Value.Risk.MaxRisk >  riskLimit ? 0 :kv.Value.Risk.MaxRisk;
            }
            return risk;
        }

        public PlanetBudget PlanetBudget(Planet planet) => new PlanetBudget(planet);        

    }
}