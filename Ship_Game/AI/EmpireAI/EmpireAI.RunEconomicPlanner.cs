// ReSharper disable once CheckNamespace

using Ship_Game.AI.Budget;
using System;
using System.Linq;

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
            // FB Get normalized money to smooth fluctuations - until we get a better treasury goal calc
            float money                    = OwnerEmpire.NormalizeBudget(OwnerEmpire.Money).LowerBound(1);
            float treasuryGoal             = TreasuryGoal();
            AutoSetTaxes(treasuryGoal);

            // gamestate attempts to increase the budget if there are wars or lack of some resources. 
            // its primarily geared at ship building. 
            float risklimit = (money * 6 / treasuryGoal).Clamped(0.01f,2);
            float gameState = GetRisk(risklimit);
            OwnerEmpire.data.DefenseBudget = DetermineDefenseBudget(1, treasuryGoal);
            OwnerEmpire.data.SSPBudget     = DetermineSSPBudget(treasuryGoal);
            BuildCapacity                  = DetermineBuildCapacity(gameState, treasuryGoal);
            OwnerEmpire.data.SpyBudget     = DetermineSpyBudget(1, treasuryGoal);
            OwnerEmpire.data.ColonyBudget  = DetermineColonyBudget(treasuryGoal);

            PlanetBudgetDebugInfo();
        }

        float DetermineDefenseBudget(float risk, float money)
        {
            risk = risk.LowerBound(0.01f);
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            float territorialism           = OwnerEmpire.data.DiplomaticPersonality?.Territorialism ?? 100;
            float buildRatio               = territorialism / 100 + strat.MilitaryRatio;
            float overSpend = OverSpendRatio(money, 0.25f, 0.25f);
            buildRatio = Math.Max(buildRatio, overSpend);

            float budget                   = SetBudgetForeArea(0.015f, buildRatio, money);
            float buildMod = BuildModifier() / 7;

            return budget * buildMod * risk;
        }

        float BuildModifier()
        {
            float buildModifier = 1;
            buildModifier += OwnerEmpire.canBuildCorvettes ? 1 : 0;
            buildModifier += OwnerEmpire.canBuildFrigates ? 2 : 0;
            buildModifier += OwnerEmpire.canBuildCruisers ? 3 : 0;
            return buildModifier;
        }

        float DetermineSSPBudget(float money)
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            float risk                     = strat.IndustryRatio + strat.ExpansionRatio;
            return SetBudgetForeArea(0.003f, risk, money);
        }

        float DetermineBuildCapacity(float risk, float money)
        {
            risk = risk.LowerBound(0.01f);
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            float buildRatio               = MathExt.Max3(strat.MilitaryRatio, strat.IndustryRatio , strat.ExpansionRatio);
            float overSpend                = OverSpendRatio(money, 0.15f, 0.75f);
            buildRatio                     = Math.Max(buildRatio, overSpend);
            float buildBudget              = SetBudgetForeArea(0.02f, buildRatio, money);
            float buildMod = BuildModifier() /7 ;
            return (buildBudget * buildMod * risk).LowerBound(1);

        }

        float DetermineColonyBudget(float money)
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;

            float buildRatio               = strat.ExpansionRatio + strat.IndustryRatio;
            float overSpend = OverSpendRatio(money, 0.15f, 0.75f);
            buildRatio                     = Math.Max(buildRatio, overSpend);
            var budget                     = SetBudgetForeArea(0.015f,buildRatio, money);
            return budget - OwnerEmpire.TotalCivShipMaintenance;
        }

        float DetermineSpyBudget(float risk, float money)
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            float overSpend = OverSpendRatio(money,  1 - strat.MilitaryRatio, 1f);

            float buildMod = BuildModifier() / 7;

            return SetBudgetForeArea(0.1f, Math.Max(risk, overSpend), money) * buildMod;
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
                                 + OwnerEmpire.data.FlatMoneyBonus;

            treasuryGoal *= OwnerEmpire.data.treasuryGoal * 150;
            float minGoal = OwnerEmpire.isPlayer ? 100 : 1000;
            treasuryGoal  = Math.Max(minGoal, treasuryGoal);
            return treasuryGoal;
        }

        /// <summary>
        /// creates a ratio starting from x percent of money.
        ///  if treasury goal is 100 and percentage of treasury is 0.8 
        ///  the result should be a ratio between  treasury and money starting at a money value of 80.
        /// 80 is .1 and 100 is 1. <para></para>
        /// maximum ratio creates an upper bound for the return value. 
        /// </summary>
        public float OverSpendRatio(float treasuryGoal, float percentageOfTreasury, float upperRatioRange)
        {
            float money    = OwnerEmpire.Money;
            float treasury = treasuryGoal.LowerBound(1);

            //if (money < 1000 || treasury < 1000) return 0;

            float reducer = (treasury * percentageOfTreasury);
            float ratio   = (money - reducer) / (treasury - reducer).LowerBound(1);
            return ratio.LowerBound(0) * upperRatioRange;
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
            risk         = OwnerEmpire.isPlayer ? 1 : risk;
            float budget = money * percentOfIncome * risk;
            budget       = Math.Max(1, budget);
            return budget;
        }
        public float GetRisk(float riskLimit = 2f)
        {
            float risk = 0;
            foreach (var kv in OwnerEmpire.AllRelations)
            {
                var totalRisk = kv.Value.Risk.Risk;
                var maxRisk = kv.Value.Risk.Risk;
                if (kv.Value.AtWar)
                {
                    risk += totalRisk;
                }
                else if(kv.Value.PreparingForWar)
                {
                    risk = Math.Max(totalRisk, risk);
                }
                else
                {
                    risk = Math.Max(maxRisk, risk);
                }
            }
            return Math.Min(risk, riskLimit) ;
        }

        public PlanetBudget PlanetBudget(Planet planet) => new PlanetBudget(planet);
    }
}