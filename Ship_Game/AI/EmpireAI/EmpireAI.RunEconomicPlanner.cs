namespace Ship_Game.AI {
    public sealed partial class EmpireAI
    {
        private float FindTaxRateToReturnAmount(float Amount)
        {
            for (int i = 0; i < 100; i++)
            {
                if (this.OwnerEmpire.EstimateIncomeAtTaxRate((float) i / 100f) >= Amount)
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
            float money = this.OwnerEmpire.Money;
            money = money < 1 ? 1 : money;
            //gremlin: Use self adjusting tax rate based on wanted treasury of 10(1 full year) of total income.

            float treasuryGoal = this.OwnerEmpire.GrossTaxes + this.OwnerEmpire.OtherIncome +
                                 this.OwnerEmpire.TradeMoneyAddedThisTurn +
                                 this.OwnerEmpire.data.FlatMoneyBonus; //mmore savings than GDP 
            treasuryGoal *= (this.OwnerEmpire.data.treasuryGoal * 100);
            treasuryGoal = treasuryGoal <= 1 ? 1 : treasuryGoal;
            float zero = this.FindTaxRateToReturnAmount(1);
            float tempTax = this.FindTaxRateToReturnAmount(treasuryGoal * (1 - (money / treasuryGoal)));
            if (tempTax - this.OwnerEmpire.data.TaxRate > .02f)
                this.OwnerEmpire.data.TaxRate += .02f;
            //else if (tempTax - this.empire.data.TaxRate < -.02f)
            //    this.empire.data.TaxRate -= .02f;
            else
                this.OwnerEmpire.data.TaxRate = tempTax;
            //if (!this.empire.isPlayer)  // The AI will NOT build defense platforms ?
            //    return;
            float DefBudget = this.OwnerEmpire.Money * this.OwnerEmpire.GetPlanets().Count * .001f *
                              (1 - this.OwnerEmpire.data.TaxRate);
            if (DefBudget < 0 || this.OwnerEmpire.data.DefenseBudget >
                this.OwnerEmpire.Money * .01 * this.OwnerEmpire.GetPlanets().Count)
                DefBudget = 0;
            this.OwnerEmpire.Money -= DefBudget;
            this.OwnerEmpire.data.DefenseBudget += DefBudget;
            return;
        }
    }
}