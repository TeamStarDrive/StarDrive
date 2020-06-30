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
            OwnerEmpire.data.SpyBudget     = DetermineSpyBudget(gameState, treasuryGoal);
            OwnerEmpire.data.ColonyBudget  = DetermineColonyBudget(treasuryGoal);

            PlanetBudgetDebugInfo();
        }

        float DetermineDefenseBudget(float risk, float money)
        {
            risk = risk.LowerBound(0.01f);
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            float territorialism           = OwnerEmpire.data.DiplomaticPersonality?.Territorialism ?? 100;
            float buildRatio               = territorialism / 100 + strat.MilitaryRatio;
            float overSpend = OverSpendRatio(money, 0.75f, 1.25f);
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
            float overSpend                = OverSpendRatio(money, 0.85f, 1.75f);
            buildRatio                     = Math.Max(buildRatio, overSpend);
            float buildBudget              = SetBudgetForeArea(0.02f, buildRatio, money);
            float buildMod = BuildModifier() /7 ;
            return (buildBudget * buildMod).LowerBound(1);

        }

        float DetermineColonyBudget(float money)
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;

            float buildRatio               = strat.ExpansionRatio + strat.IndustryRatio;
            float overSpend                = OverSpendRatio(money, 0.85f, 1.75f);
            buildRatio                     = Math.Max(buildRatio, overSpend);
            var budget                     = SetBudgetForeArea(0.015f,buildRatio, money);
            return budget - OwnerEmpire.TotalCivShipMaintenance;
        }

        float DetermineSpyBudget(float risk, float money)
        {
            float trustworthiness = OwnerEmpire.data.DiplomaticPersonality?.Trustworthiness ?? 0;
            trustworthiness      /= 100f;
            float militaryRatio   = OwnerEmpire.Research.Strategy.MilitaryRatio;
            // here we want to make sure that even if they arent trust worthy that the value they put on war machines will 
            // get more money.
            float treasuryToSave  = (trustworthiness + militaryRatio) / 2;
            float covertness      = 1 - trustworthiness;
            float spyCost         = 250;
            float numAgents       = OwnerEmpire.data.AgentList.Count;
            float spyNeeds        = 1 + EmpireSpyLimit - numAgents;
            spyNeeds              = spyNeeds.LowerBound(0);
            float overSpend       = OverSpendRatio(money, treasuryToSave, risk + spyNeeds);
            risk                  = risk.LowerBound(covertness); // * agent threat from empires

            // we are tuning to a spy cost of 250. if that changes the budget should adjust to it. 
            float spyBudgetPercent = spyCost / (spyCost * 6); 

            float budget = money * spyBudgetPercent * risk * overSpend;

            return budget;
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
        /// Creates a ratio between cash on hand above what we want on hand and treasury goal
        /// eg. treasury goal is 100, cash on hand is 100, and percent to save is .5
        /// then the result would (100 + 50) / 100 = 1.5
        /// m+m-t*p / t. will always return at least 0.
        /// 
        /// </summary>
        public float OverSpendRatio(float treasuryGoal, float percentageOfTreasuryToSave, float maxRatio)
        {
            float money    = OwnerEmpire.Money;
            float treasury = treasuryGoal.LowerBound(1);
            
            float minMoney = money - treasury * percentageOfTreasuryToSave;
            float ratio   = (money + minMoney) / treasury.LowerBound(1);
            return ratio.Clamped(0f, maxRatio);
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