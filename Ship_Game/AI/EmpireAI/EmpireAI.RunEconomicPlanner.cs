// ReSharper disable once CheckNamespace

using System;


namespace Ship_Game.AI {
    public sealed partial class EmpireAI
    {        
        private float FindTaxRateToReturnAmount(float amount)
        {
            for (int i = 0; i < 100; i++)
            {
                if (OwnerEmpire.EstimateIncomeAtTaxRate(i / 100f) >= amount)
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
            var resStrat = OwnerEmpire.getResStrat();
            float buildRatio = (resStrat.MilitaryRatio + resStrat.IndustryRatio + resStrat.ExpansionRatio) /2f;
            
            SetBudgetForeArea(goalClamped * .01f, ref OwnerEmpire.data.DefenseBudget, Math.Max(risk, resStrat.MilitaryRatio));            
            SetBudgetForeArea(goalClamped * .01f, ref OwnerEmpire.data.SSPBudget, resStrat.IndustryRatio + resStrat.ExpansionRatio);
            SetBudgetForeArea(goalClamped * .01f, ref BuildCapacity, Math.Max(risk, buildRatio));           
            SetBudgetForeArea(goalClamped * .1f, ref OwnerEmpire.data.SpyBudget, Math.Max(risk, resStrat.MilitaryRatio));
            SetBudgetForeArea(goalClamped * .01f, ref OwnerEmpire.data.ColonyBudget, resStrat.IndustryRatio + resStrat.ExpansionRatio);
#if DEBUG
            var pBudgets = new Array<Budget.PlanetBudget>();
            foreach (var empire in EmpireManager.Empires)
            {
             
                foreach (var planet in empire.GetPlanets())
                {
                    var pinfo = new Budget.PlanetBudget(planet);
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
            {//gremlin: Use self adjusting tax rate based on wanted treasury of 10(1 full year) of total income.
                treasuryGoal = OwnerEmpire.GrossTaxes + OwnerEmpire.OtherIncome +
                                     OwnerEmpire.TradeMoneyAddedThisTurn +
                                     OwnerEmpire.data.FlatMoneyBonus; //mmore savings than GDP 
            }
            treasuryGoal *= (OwnerEmpire.data.treasuryGoal * 100);
            treasuryGoal = Math.Max(1, treasuryGoal);
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
        }
#if DEBUG
        public Array<Budget.PlanetBudget> PlanetBudgets;
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

        public Budget.PlanetBudget PlanetBudget(Planet planet) => new Budget.PlanetBudget(planet);        

    }
}