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
            AutoSetTaxes(treasuryGoal);

            float goalClamped = goal.Clamped(0, 1);
            var resStrat = OwnerEmpire.ResearchStrategy;
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

        private float Income => OwnerEmpire.NetPlanetIncomes
                                 + OwnerEmpire.TradeMoneyAddedThisTurn
                                 + OwnerEmpire.data.FlatMoneyBonus
                                 - OwnerEmpire.TotalShipMaintenance;

        private float TreasuryGoal()
        {
            float treasuryGoal = OwnerEmpire.data.treasuryGoal;
            if (!OwnerEmpire.isPlayer || OwnerEmpire.data.AutoTaxes)
            {
                //gremlin: Use self adjusting tax rate based on wanted treasury of 10(1 full year) of total income.
                treasuryGoal = Math.Max(OwnerEmpire.NetPlanetIncomes, 0)
                               + OwnerEmpire.TradeMoneyAddedThisTurn 
                               + OwnerEmpire.data.FlatMoneyBonus
                               + OwnerEmpire.TotalShipMaintenance; //more savings than GDP 
            }
            treasuryGoal *= OwnerEmpire.data.treasuryGoal * 1000;
            treasuryGoal = Math.Max(1000, treasuryGoal);
            return treasuryGoal;
        }

        private void AutoSetTaxes(float treasuryGoal)
        {
            if (OwnerEmpire.isPlayer && !OwnerEmpire.data.AutoTaxes)
                return;

            const float normalTaxRate = 0.25f;
            float treasuryGoalRatio   = (Math.Max(OwnerEmpire.Money, 100)) / treasuryGoal;
            float taxRateModifer = 0;
            if (treasuryGoalRatio > 1)
                taxRateModifer = -(float)Math.Round((treasuryGoalRatio -1) / 10, 2); // this will decrease tax based on ratio
            if (treasuryGoalRatio < 1)
                taxRateModifer = (float)Math.Round((1 / treasuryGoalRatio - 1) / 10, 2); // this will decrease tax based on oppsite ratio

            OwnerEmpire.data.TaxRate  = (normalTaxRate + taxRateModifer).Clamped(0.05f,0.95f);
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