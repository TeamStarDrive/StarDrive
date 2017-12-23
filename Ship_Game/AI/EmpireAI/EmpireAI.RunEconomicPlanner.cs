// ReSharper disable once CheckNamespace

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Ship_Game.Gameplay;

namespace Ship_Game.AI {
    public sealed partial class EmpireAI
    {
        public EmpireRiskAssessment RiskAssessment = new EmpireRiskAssessment();
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
            SetBudgetForeArea(.10f, ref OwnerEmpire.data.DefenseBudget);
        }
        private float SetBudgetForeArea(float percentOfIncome, ref float area)
        {
            float budget = OwnerEmpire.PercentageOfIncome(percentOfIncome);
            if (budget < 0 || OwnerEmpire.data.DefenseBudget / OwnerEmpire.Money > .10f)
                budget = 0;
                
            OwnerEmpire.Money -= budget;
            area += budget;
            return budget;
        }
       
        public class EmpireRiskAssessment
        {
            private readonly float[] Elements;
            public float Expansion   { get => Elements[1]; private set => Elements[1] = value; }
            public float Border      { get => Elements[2]; private set => Elements[2] = value; }
            public float KnownThreat { get => Elements[3]; private set => Elements[3] = value; }

            public float Risk { get; private set; }
            public float MaxRisk { get; private set; }

            public EmpireRiskAssessment()
            {
                Elements = new float[4];
            }
            
            public ReadOnlyCollection<float> GetRiskElementsArray()
            {
                return Array.AsReadOnly(Elements);
            }

            private void UpdateRiskAssessment(Empire empire)
            {                
                Expansion = 0;
                Border = 0;
                KnownThreat = 0;
                Risk = 0;
                foreach (KeyValuePair<Empire, Relationship> rel in empire.AllRelations)
                {
                    KnownThreat = Math.Max(KnownThreat, rel.Value.RiskAssesment(empire, rel.Key));
                    Border = Math.Max(Border, rel.Value.BorderRiskAssesment(empire, rel.Key));
                    Expansion = Math.Max(Expansion, rel.Value.ExpansionRiskAssement(empire, rel.Key));
                }
                Risk = Elements.Sum();
                MaxRisk = Elements.MultiMax();

            }


        }
        
    }
}