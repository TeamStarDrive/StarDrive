namespace Ship_Game.AI {
    public sealed partial class GSAI
    {
        private float FindTaxRateToReturnAmount(float Amount)
        {
            for (int i = 0; i < 100; i++)
            {
                if (this.empire.EstimateIncomeAtTaxRate((float) i / 100f) >= Amount)
                {
                    return (float) i / 100f;
                }
            }
            //if (this.empire.ActualNetLastTurn < 0 && this.empire.data.TaxRate >=50)
            //{
            //    float tax = this.empire.data.TaxRate + .05f;
            //    tax = tax > 100 ? 100 : tax;
            //}
            return 1; //0.50f;
        }

        private void RunEconomicPlanner()
        {
            float money = this.empire.Money;
            money = money < 1 ? 1 : money;
            //gremlin: Use self adjusting tax rate based on wanted treasury of 10(1 full year) of total income.

            float treasuryGoal = this.empire.GrossTaxes + this.empire.OtherIncome +
                                 this.empire.TradeMoneyAddedThisTurn +
                                 this.empire.data.FlatMoneyBonus; //mmore savings than GDP 
            treasuryGoal *= (this.empire.data.treasuryGoal * 100);
            treasuryGoal = treasuryGoal <= 1 ? 1 : treasuryGoal;
            float zero = this.FindTaxRateToReturnAmount(1);
            float tempTax = this.FindTaxRateToReturnAmount(treasuryGoal * (1 - (money / treasuryGoal)));
            if (tempTax - this.empire.data.TaxRate > .02f)
                this.empire.data.TaxRate += .02f;
            //else if (tempTax - this.empire.data.TaxRate < -.02f)
            //    this.empire.data.TaxRate -= .02f;
            else
                this.empire.data.TaxRate = tempTax;
            //if (!this.empire.isPlayer)  // The AI will NOT build defense platforms ?
            //    return;
            float DefBudget = this.empire.Money * this.empire.GetPlanets().Count * .001f *
                              (1 - this.empire.data.TaxRate);
            if (DefBudget < 0 || this.empire.data.DefenseBudget >
                this.empire.Money * .01 * this.empire.GetPlanets().Count)
                DefBudget = 0;
            this.empire.Money -= DefBudget;
            this.empire.data.DefenseBudget += DefBudget;
            return;
        }
    }
}