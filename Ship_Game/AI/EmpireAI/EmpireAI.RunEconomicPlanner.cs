// ReSharper disable once CheckNamespace
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
            float defBudget = OwnerEmpire.Money * OwnerEmpire.GetPlanets().Count * .001f *
                              (1 - OwnerEmpire.data.TaxRate);
            if (defBudget < 0 || OwnerEmpire.data.DefenseBudget >
                OwnerEmpire.Money * .01 * OwnerEmpire.GetPlanets().Count)
                defBudget                        = 0;
            OwnerEmpire.Money              -= defBudget;
            OwnerEmpire.data.DefenseBudget += defBudget;
        }
    }
}