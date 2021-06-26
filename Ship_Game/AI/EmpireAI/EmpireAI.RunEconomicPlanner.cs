// ReSharper disable once CheckNamespace

using Ship_Game.AI.Budget;
using System;
using System.Linq;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
    /// <summary>
    /// Economic process in brief.
    /// set up treasury goal and tax rate.
    /// calculate threat to empire.
    /// set budgets for areas.
    /// some areas like civilian freighter budget is not restrictive. It is used to allow the empire to track the money spent in that area.
    /// most budget areas are not hard restricted. There will be some wiggle room or outright relying on stored money. 
    /// </summary>
    public sealed partial class EmpireAI
    {
        /// <summary>
        /// value from 0 to 1+
        /// This represents the overall threat to the empire. It is calculated from the EmpireRiskAssessment class.
        /// it currently looks at expansion threat, border threat, and general threat from each each empire. 
        /// </summary>
        public float ThreatLevel { get; private set; } = 0;
        /// <summary>
        /// This is the budgeted amount of money that will be available to empire looking over 20 years.  
        /// </summary>
        public float ProjectedMoney { get; private set; } = 0;
        /// <summary>
        /// This a ratio of projectedMoney and the normalized money. It should be fairly stable. 
        /// </summary>
        public float FinancialStability
        {
            get
            {
                float normalMoney = OwnerEmpire.NormalizedMoney;
                float treasury    = ProjectedMoney / 2;
                return normalMoney / treasury;
            }
        }
        /// <summary>
        /// This is a quick set check to see if we are financially able to rush production
        /// </summary>
        public bool SafeToRush => FinancialStability > 0.75f; 

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
            float treasuryGoal             = ProjectedMoney = TreasuryGoal(normalizedBudget);

            // gamestate attempts to increase the budget if there are wars or lack of some resources.  
            // its primarily geared at ship building. 
            float riskLimit = (normalizedBudget * 3 / treasuryGoal).Clamped(0.1f, 2);
            float gameState = ThreatLevel = GetRisk(riskLimit);

            AutoSetTaxes(treasuryGoal, normalizedBudget);
            // the commented numbers are for debugging to compare the current values to the previous ones. 
            // the values below are now weights to adjust the budget areas. 
            float defense                  = 5;
            float SSP                      = 1;
            float build                    = 7;
            float spy                      = 25;
            float colony                   = 10f;
            float freight                  = 2f;
            float savings                  = 500;

            float total = (defense + SSP + build + spy + colony + freight + savings);

            defense       /= total;
            SSP           /= total;
            build         /= total;
            spy           /= total;
            colony        /= total;
            freight       /= total;

            // for the player they don't use some budgets. so distribute them to areas they do
            // spy budget is a special case currently and is not distributed. 
            // dont set the build cap to zero. the build cap is used for potential economic strength. 
            // and setting to 0 doesnt do anything past this point. 
            if (OwnerEmpire.isPlayer)
            {
                float budgetBalance = (build) / 3f;
                defense            += budgetBalance;
                colony             += budgetBalance;
                SSP                += budgetBalance;
            }

            OwnerEmpire.data.DefenseBudget = DetermineDefenseBudget(treasuryGoal, defense, gameState);
            OwnerEmpire.data.SSPBudget     = DetermineSSPBudget(treasuryGoal, SSP);
            BuildCapacity                  = DetermineBuildCapacity(treasuryGoal, gameState, build);
            OwnerEmpire.data.SpyBudget     = DetermineSpyBudget(treasuryGoal, spy);
            OwnerEmpire.data.ColonyBudget  = DetermineColonyBudget(treasuryGoal * 0.5f, colony);
            OwnerEmpire.data.FreightBudget = SetBudgetForeArea(freight, 1, treasuryGoal);
            PlanetBudgetDebugInfo();
            float allianceBudget = 0;
            foreach (var ally in EmpireManager.GetAllies(OwnerEmpire)) allianceBudget += ally.GetEmpireAI().BuildCapacity;
            AllianceBuildCapacity = BuildCapacity + allianceBudget;
        }

        float DetermineDefenseBudget(float money, float percentOfMoney, float risk)
        {
            float budget                   = SetBudgetForeArea(percentOfMoney, 1, money);
            return budget;
        }

        float DetermineSSPBudget(float money, float percentOfMoney)
        {
            var strat  = OwnerEmpire.Research.Strategy;
            float risk = 1 + (strat.IndustryRatio + strat.ExpansionRatio);
            risk      /= 5;
            float debt = TreasuryProtection(money, 0.1f);
            return SetBudgetForeArea(percentOfMoney, risk, money) * debt;
        }

        float DetermineBuildCapacity(float money, float risk, float percentOfMoney)
        {
            float buildBudget    = SetBudgetForeArea(percentOfMoney, risk, money);
            float troopMaintenance = OwnerEmpire.TotalTroopShipMaintenance;
            return buildBudget - troopMaintenance;
        }

        float DetermineColonyBudget(float money, float percentOfMoney)
        {
            var budget = SetBudgetForeArea(percentOfMoney, 1, money);
            return budget;
        }

        float DetermineSpyBudget(float money, float percentOfMoney)
        {
            if (OwnerEmpire.isPlayer)
                return 0;

            bool notKnown = !OwnerEmpire.AllRelations.Any(r => r.Rel.Known && !r.Them.isFaction);
            if (notKnown) return 0;

            float trustworthiness = OwnerEmpire.data.DiplomaticPersonality?.Trustworthiness ?? 100;
            trustworthiness      /= 100f;
            float militaryRatio   = OwnerEmpire.Research.Strategy.MilitaryRatio;
            
            // it is possible that the number of agents can exceed the agent limit. That needs a whole other pr. So this hack to make things work. 
            float agentRatio      =  OwnerEmpire.data.AgentList.Count.UpperBound(EmpireSpyLimit) / (float)EmpireSpyLimit;
            
            // here we want to make sure that even if they arent trust worthy that the value they put on war machines will 
            // get more money.
            float treasuryToSave  = ((0.5f + agentRatio + trustworthiness + militaryRatio) * 0.6f);
            float numAgents       = OwnerEmpire.data.AgentList.Count;
            float spyNeeds        = 1 + EmpireSpyLimit - numAgents.UpperBound(EmpireSpyLimit);
            spyNeeds              = spyNeeds.LowerBound(1);
            float overSpend       = OverSpendRatio(money, treasuryToSave, spyNeeds);
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

        public float TreasuryGoal(float normalizedMoney)
        {
            //gremlin: Use self adjusting tax rate based on wanted treasury of 10(1 full years) of total income.
            float treasuryGoal = Math.Max(OwnerEmpire.PotentialIncome, 0)
                                 + OwnerEmpire.data.FlatMoneyBonus;
            
            float timeSpan = (200 - normalizedMoney / 500).Clamped(100,200) * OwnerEmpire.data.treasuryGoal;
            treasuryGoal *= timeSpan;

            return treasuryGoal.LowerBound(Empire.StartingMoney);
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
            float money    = OwnerEmpire.NormalizedMoney * 2;
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

        private void AutoSetTaxes(float treasuryGoal, float money)
        {
            if (OwnerEmpire.isPlayer && !OwnerEmpire.data.AutoTaxes)
                return;

            float timeSpan = 200 * OwnerEmpire.data.treasuryGoal;

            float totalMaintenance = OwnerEmpire.TotalShipMaintenance + OwnerEmpire.TotalBuildingMaintenance;

            if (!OwnerEmpire.isPlayer)
            {
                float max = Math.Max(totalMaintenance * timeSpan, treasuryGoal / 2);
                float min = Math.Min(totalMaintenance * timeSpan, treasuryGoal);
                treasuryGoal = treasuryGoal.Clamped(min, max);
            }

            float treasuryGap = treasuryGoal - money;
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
            return budget.LowerBound(1);
        }
        public float GetRisk(float riskLimit = 2f)
        {
            float risk = 0;
            float maxRisk = 0;
            int totalRels = 0;
            foreach ((Empire other, Relationship rel) in OwnerEmpire.AllRelations)
            {
                if (other.data.Defeated || !rel.Known) continue;
                if (rel.Risk.Risk <= 0) 
                    continue;
                maxRisk = Math.Max(maxRisk, rel.Risk.Risk);
                risk += rel.Risk.Risk;
                totalRels++;
            }

            risk /= totalRels.LowerBound(1);

            return Math.Min(maxRisk, riskLimit);
        }

        public PlanetBudget PlanetBudget(Planet planet) => new PlanetBudget(planet);
    }
}