// ReSharper disable once CheckNamespace

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Ship_Game.Gameplay;

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
            //gremlin: Use self adjusting tax rate based on wanted treasury of 10(1 full year) of total income.
            //BuildCapacity = OwnerEmpire.GetTotalShipMaintenance();
            float treasuryGoal = OwnerEmpire.GrossTaxes + OwnerEmpire.OtherIncome +
                                 OwnerEmpire.TradeMoneyAddedThisTurn +
                                 OwnerEmpire.data.FlatMoneyBonus; //mmore savings than GDP 
            treasuryGoal *= (OwnerEmpire.data.treasuryGoal * 100);
            treasuryGoal  = treasuryGoal <= 1 ? 1 : treasuryGoal;
            treasuryGoal *= 1 - money / treasuryGoal;
            treasuryGoal *= .01f;
            float tempTax = FindTaxRateToReturnAmount(treasuryGoal);
            if (tempTax - OwnerEmpire.data.TaxRate > .02f)
                OwnerEmpire.data.TaxRate += .02f;
            else
                OwnerEmpire.data.TaxRate = tempTax;
            float militaryRatio = OwnerEmpire.getResStrat().MilitaryRatio;
            SetBudgetForeArea(.01f, ref OwnerEmpire.data.DefenseBudget, Math.Max(risk, militaryRatio));
            BuildCapacity = OwnerEmpire.EstimateShipCapacityAtTaxRate(Math.Max(GetRisk(), militaryRatio));
        }
        private float SetBudgetForeArea(float percentOfIncome, ref float area, float risk)
        {
            float budget = OwnerEmpire.Money * percentOfIncome * risk;
            if (budget < 0 )
                budget = 0;                            
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
    }
}