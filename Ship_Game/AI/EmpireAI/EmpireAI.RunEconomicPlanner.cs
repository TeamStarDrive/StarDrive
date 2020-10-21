// ReSharper disable once CheckNamespace

using Ship_Game.AI.Budget;
using System;
using System.Linq;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        private float FindTaxRateToReturnAmount(float amount)
        {
            if (amount <= 0) return 0f;
            for (int i = 1; i < 100; i++)
            {
                float taxRate = i / 100f;
                //float amountMade = OwnerEmpire.EstimateNetIncomeAtTaxRate(taxRate);

                float amountMade = taxRate * OwnerEmpire.GrossIncome;

                if (amountMade >= amount)
                {
                    return taxRate;
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
            // the commented numbers are for debugging to compare the current values to the previous ones. 
            // the values below are now weights to adjust the budget areas. 
            float defense                  = 6;  // 0.015f;
            float SSP                      = 2;  // 0.003f;
            float build                    = 15; // 0.02f;
            float spy                      = 100;
            float colony                   = 10;  // 0.015f;
            float savings                  = 350;
            float troopShuttles = 6;

            float total = (defense + SSP + build + spy + colony + savings + troopShuttles) ;

            defense       /= total;
            SSP           /= total;
            build         /= total;
            spy           /= total;
            colony        /= total;
            troopShuttles /= total;

            // for the player they don't use some budgets. so distribute them to areas they do
            // spy budget is a special case currently and is not distributed. 
            if (OwnerEmpire.isPlayer)
            {
                float budgetBalance = (build + troopShuttles) / 3f;
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
            OwnerEmpire.data.DefenseBudget = DetermineDefenseBudget(treasuryGoal, defense);
            OwnerEmpire.data.SSPBudget     = DetermineSSPBudget(treasuryGoal, SSP);
            BuildCapacity                  = DetermineBuildCapacity(treasuryGoal, gameState, build);
            OwnerEmpire.data.SpyBudget     = DetermineSpyBudget(treasuryGoal, spy);
            OwnerEmpire.data.ColonyBudget  = DetermineColonyBudget(treasuryGoal, colony);
            TroopShuttleCapacity           = DetermineBuildCapacity(treasuryGoal, gameState, troopShuttles) - OwnerEmpire.TotalTroopShipMaintenance;
            PlanetBudgetDebugInfo();
        }

        float DetermineDefenseBudget(float money, float percentOfMoney)
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            float territorialism           = (OwnerEmpire.data.DiplomaticPersonality?.Territorialism ?? 100) /100f;
            float buildRatio               = (1 + territorialism + strat.MilitaryRatio) /3;
            float budget                   = SetBudgetForeArea(percentOfMoney, buildRatio, money);
            float debt                     = TreasuryProtection(money, 0.1f);
            return (budget * debt).LowerBound(buildRatio);
        }

        float DetermineSSPBudget(float money, float percentOfMoney)
        {
            var strat  = OwnerEmpire.Research.Strategy;
            float risk = 1 + strat.IndustryRatio + strat.ExpansionRatio;
            risk      /= 2;
            float debt = TreasuryProtection(money, 0.1f);
            return SetBudgetForeArea(percentOfMoney, risk, money) * debt;
        }

        float DetermineBuildCapacity(float money, float risk, float percentOfMoney)
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;
            float personality              = OwnerEmpire.data.DiplomaticPersonality?.Opportunism ?? 1;
            
            risk                           = risk.LowerBound(Math.Max(strat.MilitaryRatio, 0.1f));
            float personalityRatio         = (0.5f + personality + strat.MilitaryRatio + strat.ExpansionRatio) / 3f;
            float buildRatio               = personalityRatio;
            float buildBudget              = SetBudgetForeArea(percentOfMoney, buildRatio, money);
            float extraBudget              = OverSpendRatio(money, 1, 1.25f).LowerBound(1);
            return (buildBudget * risk * extraBudget).LowerBound(1);

        }

        float DetermineColonyBudget(float money, float percentOfMoney)
        {
            EconomicResearchStrategy strat = OwnerEmpire.Research.Strategy;

            float buildRatio               = strat.ExpansionRatio + strat.IndustryRatio;
            var budget                     = SetBudgetForeArea(percentOfMoney, buildRatio, money);
            return budget - OwnerEmpire.TotalCivShipMaintenance;
        }

        float DetermineSpyBudget(float money, float percentOfMoney)
        {
            float trustworthiness = OwnerEmpire.data.DiplomaticPersonality?.Trustworthiness ?? 0;
            trustworthiness      /= 100f;
            float militaryRatio   = OwnerEmpire.Research.Strategy.MilitaryRatio;
            float agentRatio =  OwnerEmpire.data.AgentList.Count / (float)EmpireSpyLimit;
            // here we want to make sure that even if they arent trust worthy that the value they put on war machines will 
            // get more money.
            float treasuryToSave  = (1 + agentRatio + trustworthiness + militaryRatio) / 2;
            float numAgents       = OwnerEmpire.data.AgentList.Count;
            float spyNeeds        = 1 + EmpireSpyLimit - numAgents;
            spyNeeds              = spyNeeds.LowerBound(0);
            float overSpend       = OverSpendRatio(money, treasuryToSave,  spyNeeds);

            float budget          = money * percentOfMoney * overSpend;

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
            float timeSpan = 200 * OwnerEmpire.data.treasuryGoal;
            treasuryGoal *= timeSpan;
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

        /// <summary>
        /// calculate treasury to money ratio as debt. if debt is under threshold then the ratio of debt to threshold.
        /// eg debt under threshold return debt / threshold. else return 1
        /// </summary>
        public float TreasuryProtection(float treasury, float threshold, float minRatio = 0.5f)
        {
            float debt       = OverSpendRatio(treasury, 1.5f, 1);
            float protection = debt < threshold ? (debt + minRatio) / (threshold + minRatio): 1;
            return protection;
        }

        private void AutoSetTaxes(float treasuryGoal)
        {
            if (OwnerEmpire.isPlayer && !OwnerEmpire.data.AutoTaxes)
                return;
            float money    = OwnerEmpire.Money;
            float timeSpan = 200 * OwnerEmpire.data.treasuryGoal;

            float treasuryGap = treasuryGoal - money;
            float totalMaintenance = OwnerEmpire.TotalShipMaintenance + OwnerEmpire.TotalBuildingMaintenance;
            float neededPerTurn = treasuryGap / timeSpan;
            float treasuryToMoneyRatio = (money / treasuryGoal).Clamped(0.01f, 1);
            float taxesNeeded = FindTaxRateToReturnAmount(neededPerTurn).Clamped(0.0f, 0.95f);
            float increase = taxesNeeded - OwnerEmpire.data.TaxRate;
            if (!increase.AlmostZero())
            {
                float wanted = OwnerEmpire.data.TaxRate + (increase ) * treasuryToMoneyRatio;
                //float wanted = OwnerEmpire.data.TaxRate + (increase > 0 ? 0.05f : -0.05f) * treasuryToMoneyRatio;

                OwnerEmpire.data.TaxRate = wanted.Clamped(0.01f, 0.95f);
            }
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
            foreach ((Empire other, Relationship rel) in OwnerEmpire.AllRelations)
            {
                var totalRisk = rel.Risk.Risk;
                var maxRisk   = rel.Risk.Risk;

                if (rel.AtWar || rel.PreparingForWar)
                {
                    risk += totalRisk;
                }
                //else if(kv.Value.PreparingForWar)
                //{
                //    risk = Math.Max(totalRisk, risk);
                //}
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