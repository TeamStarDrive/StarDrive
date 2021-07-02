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
        public float ThreatLevel { get; private set; }    = 0;
        public float EconomicThreat { get; private set; } = 0;
        public float BorderThreat { get; private set; }   = 0;
        public float EnemyThreat { get; private set; }    = 0;
        /// <summary>
        /// This is the budgeted amount of money that will be available to empire looking over 20 years.  
        /// </summary>
        public float ProjectedMoney { get; private set; } = 0;
        /// <summary>
        /// This a ratio of projectedMoney and the normalized money then multiplied by 2 then add 1 - taxrate
        /// then the whole thing divided by 3. This puts a large emphasis on money goal to money ratio
        /// but a high tax will greatly effect the result. 
        /// </summary>
        public float CreditRating
        {
            get
            {
                float normalMoney = OwnerEmpire.NormalizedMoney;
                float goalRatio = normalMoney / ProjectedMoney;

                return (goalRatio.UpperBound(1) * 3 + (1 - OwnerEmpire.data.TaxRate)) / 4f;
            }
        }
        /// <summary>
        /// This is a quick set check to see if we are financially able to rush production
        /// </summary>
        public bool SafeToRush => CreditRating > 0.75f;

        // Stores Maintenance saved by scrapping this turn;
        public float MaintSavedByBuildingScrappedThisTurn = 0;

        // Empire spaceDefensive Reserves high enough to support fractional build budgets
        public bool EmpireCanSupportSpcDefense => OwnerEmpire.data.DefenseBudget > OwnerEmpire.TotalOrbitalMaintenance && CreditRating > 0.90f;

        private float FindTaxRateToReturnAmount(float amount)
        {
            if (amount <= 0) return 0f;
            for (int i = 1; i < 100; i++)
            {
                float taxRate = i / 100f;
                //float amountMade = OwnerEmpire.EstimateNetIncomeAtTaxRate(taxRate);

                float amountMade = taxRate * OwnerEmpire.GrossIncomeBeforeTax;

                if (amountMade >= amount)
                {
                    return taxRate;
                }
            }
            return 1;
        }

        public void RunEconomicPlanner(bool fromSave = false)
        {
            MaintSavedByBuildingScrappedThisTurn = 0;

            float money = OwnerEmpire.Money;
            if (!fromSave)
            {
                OwnerEmpire.UpdateNormalizedMoney(money);
            }

            float normalizedBudget = OwnerEmpire.NormalizedMoney;
            normalizedBudget = money < 0 ? money : normalizedBudget;

            float treasuryGoal = TreasuryGoal(normalizedBudget);
            ProjectedMoney = treasuryGoal / 2;

            // gamestate attempts to increase the budget if there are wars or lack of some resources.  
            // its primarily geared at ship building. 
            float riskLimit = (CreditRating * 2).Clamped(0.1f, 2);
            float gameState = ThreatLevel = GetRisk(riskLimit);

            AutoSetTaxes(treasuryGoal, normalizedBudget);
            // the values below are now weights to adjust the budget areas. 
            float defense = 5;
            float SSP     = 1;
            float build   = 7;
            float spy     = 25;
            float colony  = 5;
            float freight = 2f;
            float savings = 550;

            float total = (defense + SSP + build + spy + colony + freight + savings);

            defense       /= total;
            SSP           /= total;
            build         /= total;
            spy           /= total;
            colony        /= total;
            freight       /= total;

            // for the player they don't use some budgets. so distribute them to areas they do
            // spy budget is a special case currently and is not distributed.
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
            return buildBudget;
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
            float gross = OwnerEmpire.GrossIncomeBeforeTax - OwnerEmpire.TotalCivShipMaintenance -
                          OwnerEmpire.TroopCostOnPlanets - OwnerEmpire.TotalTroopShipMaintenance; ;
            float treasuryGoal = Math.Max(gross, 0);
            
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

            if (!OwnerEmpire.isPlayer)
            {
                // set the money to the min maintenance or the treasury goal
                float totalMaintenance = OwnerEmpire.TotalShipMaintenance + OwnerEmpire.TotalBuildingMaintenance;
                float max              = Math.Max(totalMaintenance * timeSpan, treasuryGoal / 2);
                float min              = Math.Min(totalMaintenance * timeSpan, treasuryGoal);
                treasuryGoal           = treasuryGoal.Clamped(min, max);
            }

            // figure how much is needed for treasury
            float treasuryGap          = treasuryGoal - money;
            //figure how much is needed to fulfill treasury in timespan and cover current costs
            float neededPerTurn        = treasuryGap / timeSpan + OwnerEmpire.AllSpending;
            float taxesNeeded          = FindTaxRateToReturnAmount(neededPerTurn).Clamped(0.0f, 0.95f);
            float increase             = taxesNeeded - OwnerEmpire.data.TaxRate;
            float treasuryToMoneyRatio = (money / treasuryGoal).Clamped(0.01f, 1);

            // try to control the amount of change of the tax so that it can not wildly change to low. 
            if (!increase.AlmostZero())
            {
                float wanted             = OwnerEmpire.data.TaxRate + (increase ) * treasuryToMoneyRatio;
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
            float maxRisk    = 0;
            float econRisk   = 0;
            float borderRisk = 0;
            float enemyRisk  = 0;

            foreach ((Empire other, Relationship rel) in OwnerEmpire.AllRelations)
            {
                if (other.data.Defeated || !rel.Known) continue;
                if (rel.Risk.Risk <= 0)
                    continue;
                maxRisk    = Math.Max(maxRisk, rel.Risk.Risk);
                econRisk   = Math.Max(econRisk, rel.Risk.Expansion);
                borderRisk = Math.Max(borderRisk, rel.Risk.Border);
                enemyRisk  = Math.Max(enemyRisk, rel.Risk.KnownThreat);
            }

            ThreatLevel    = maxRisk;
            EconomicThreat = econRisk;
            BorderThreat   = borderRisk;
            EnemyThreat    = enemyRisk;

            return Math.Min(maxRisk, riskLimit);
        }

        public PlanetBudget PlanetBudget(Planet planet) => new PlanetBudget(planet);
    }
}