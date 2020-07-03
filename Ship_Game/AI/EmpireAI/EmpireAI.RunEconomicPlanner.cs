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
            float money                    = OwnerEmpire.Money;
            float normalizedBudget         = OwnerEmpire.NormalizedMoney = money;
            float treasuryGoal             = TreasuryGoal();
            AutoSetTaxes(treasuryGoal);
            float defense                  = 6; // 0.015f;
            float SSP                      = 2; // 0.003f;
            float build                    = 10;// 0.02f;
            float spy                      = 10;
            float colony                   = 7;// 0.015f;
            float savings = 350;

            float total = (defense + SSP + build + spy + colony + savings) ;

            defense /= total;
            SSP     /= total;
            build   /= total;
            spy      = SpyCost / ( total);
            colony  /= total;


            //SpyCost / (SpyCost * 6);
            if (OwnerEmpire.isPlayer)
            {
                float budgetBalance = (spy + build) / 2f / 3f;
                defense            += budgetBalance;
                colony             += budgetBalance;
                SSP                += budgetBalance;
                build               = 0;
                spy                 = 0;
            }

            // gamestate attempts to increase the budget if there are wars or lack of some resources. 
            // its primarily geared at ship building. 
            float riskLimit                = (normalizedBudget * 6 / treasuryGoal).Clamped(0.01f,2);
            float gameState                = GetRisk(riskLimit);
            OwnerEmpire.data.DefenseBudget = DetermineDefenseBudget(treasuryGoal, 1, defense);
            OwnerEmpire.data.SSPBudget     = DetermineSSPBudget(treasuryGoal, SSP);
            BuildCapacity                  = DetermineBuildCapacity(treasuryGoal, gameState, build);
            OwnerEmpire.data.SpyBudget     = DetermineSpyBudget(treasuryGoal, gameState, spy);
            OwnerEmpire.data.ColonyBudget  = DetermineColonyBudget(treasuryGoal, colony);

            PlanetBudgetDebugInfo();
        }

        float DetermineDefenseBudget(float money, float risk, float percentOfMoney)
        {
            risk                           = risk.Clamped(0.1f,1);
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            float territorialism           = OwnerEmpire.data.DiplomaticPersonality?.Territorialism ?? 100;
            float buildRatio               = (1 + territorialism / 100f + strat.MilitaryRatio) /3;
            float budget                   = SetBudgetForeArea(0.015f, buildRatio, money);
            float overSpend                = OverSpendRatio(money, percentOfMoney, 1f);
            return budget * risk * overSpend;
        }

        float DetermineSSPBudget(float money, float percentOfMoney)
        {
            var strat  = OwnerEmpire.Research.Strategy;
            float risk = 1 + strat.IndustryRatio + strat.ExpansionRatio;
            risk      /= 2;
            return SetBudgetForeArea(percentOfMoney, risk, money);
        }

        float DetermineBuildCapacity(float money, float risk, float percentOfMoney)
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            DTrait personality = OwnerEmpire.data?.DiplomaticPersonality;
            
            risk                           = risk.LowerBound(Math.Max(strat.MilitaryRatio, 0.1f));
            float personalityRatio         = strat.MilitaryRatio + strat.ExpansionRatio;
            float buildRatio               = personalityRatio;
            float buildBudget              = SetBudgetForeArea(percentOfMoney, buildRatio, money);
            float overSpend                = OverSpendRatio(money, 0.75f, 1.75f);
            return (buildBudget * risk * overSpend).LowerBound(1);

        }

        float DetermineColonyBudget(float money, float percentOfMoney)
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;

            float buildRatio               = strat.ExpansionRatio + strat.IndustryRatio;
            float overSpend                = OverSpendRatio(money, 0.85f, 1.75f);
            buildRatio                     = Math.Max(buildRatio, overSpend);
            var budget                     = SetBudgetForeArea(percentOfMoney, buildRatio, money);
            return budget - OwnerEmpire.TotalCivShipMaintenance;
        }

        float DetermineSpyBudget(float money, float risk, float percentOfMoney)
        {
            float trustworthiness = OwnerEmpire.data.DiplomaticPersonality?.Trustworthiness ?? 0;
            trustworthiness      /= 100f;
            float militaryRatio   = OwnerEmpire.Research.Strategy.MilitaryRatio;
            // here we want to make sure that even if they arent trust worthy that the value they put on war machines will 
            // get more money.
            float treasuryToSave  = (trustworthiness + militaryRatio) / 2;
            float covertness      = 1 - trustworthiness;
            float numAgents       = OwnerEmpire.data.AgentList.Count;
            float spyNeeds        = 1 + EmpireSpyLimit - numAgents;
            spyNeeds              = spyNeeds.LowerBound(0);
            float overSpend       = OverSpendRatio(money, treasuryToSave, risk + spyNeeds);
            risk                  = risk.LowerBound(covertness); // * agent threat from empires

            // we are tuning to a spy cost of 250. if that changes the budget should adjust to it. 
            float spyBudgetPercent = SpyCost / (SpyCost * 6);
            float budget           = money * percentOfMoney * risk * overSpend;

            return budget;
        }

        private void PlanetBudgetDebugInfo()
        {
            if (!Empire.Universe.Debug) 
                return;

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
            //gremlin: Use self adjusting tax rate based on wanted treasury of 10(1 full years) of total income.
            float treasuryGoal = Math.Max(OwnerEmpire.PotentialIncome, 0)
                                 + OwnerEmpire.data.FlatMoneyBonus;

            treasuryGoal *= OwnerEmpire.data.treasuryGoal * 200;
            return treasuryGoal.LowerBound(100);
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
            float money    = OwnerEmpire.NormalizedMoney;
            float treasury = treasuryGoal.LowerBound(1);
            float minMoney = money - treasury * percentageOfTreasuryToSave;
            float ratio    = (money + minMoney) / treasury.LowerBound(1);
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
                var maxRisk   = kv.Value.Risk.Risk;

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

            float expansionTasks = GetAvgStrengthNeededByExpansionTasks();
            if (expansionTasks > 0)
            {
                float expansionRatio                         = OwnerEmpire.Research.Strategy.ExpansionRatio.LowerBound(0.1f);
                float riskFromExpansion                      = expansionTasks / OwnerEmpire.CurrentMilitaryStrength.LowerBound(1);

                // TOO RISKY check. Prevent trying to budget a venture right now when waiting for better tech 
                // or a more powerful economy might be better. 
                if (riskFromExpansion > 1) riskFromExpansion = 0.5f;
                riskFromExpansion                           *= expansionRatio;
                risk                                         = Math.Max(risk, riskFromExpansion);
            }

            return Math.Min(risk, riskLimit) ;
        }

        public PlanetBudget PlanetBudget(Planet planet) => new PlanetBudget(planet);
    }
}