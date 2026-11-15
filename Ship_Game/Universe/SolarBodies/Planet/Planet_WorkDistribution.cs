using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDGraphics;

namespace Ship_Game
{
    public partial class Planet
    {
        public void InitializeWorkerDistribution(Empire o)
        {
            if (o.IsCybernetic || IsBarrenType)
            {
                Food.Percent = 0.0f;
                Prod.Percent = 0.5f;
                Res.Percent  = 0.5f;
            }
            else
            {
                Food.Percent = 0.55f;
                Prod.Percent = 0.25f;
                Res.Percent  = 0.20f;
            }
        }

        static float CalculateMod(float desiredPercent, float storageRatio)
        {
            float mod = (desiredPercent - storageRatio) * 2; // Percentage currently over or under desired storage
            if (mod > 0f && mod < +0.05f) mod = 0.05f;	// Avoid crazy small percentage
            if (mod < 0f && mod > -0.05f) mod = 0.00f;	// Avoid bounce (stop if slightly over)
            return mod;
        }

        // calculate farmers up to percent
        float FarmToPercentage(float percent, float wantedIncome) // Production
        {
            if (percent <= 0f || Food.YieldPerColonist <= 0.1f || IsCybernetic)
                return 0; // No farming here, so never mind

            float farmers = Food.EstPercentForNetIncome(wantedIncome);

            // modify nominal farmers by overage or underage
            farmers += CalculateMod(percent, Storage.FoodRatioWithIncoming).UpperBound(0.5f);
            float maxPercentage = IsStarving || ConstructionQueue.Count == 0 ? 1f : 0.9f;
            return farmers.Clamped(0f, maxPercentage);
        }

        float WorkToPercentage(float percent, float wantedIncome) // Production
        {
            if (percent <= 0f || Prod.YieldPerColonist <= 0.1f) 
                return 0;

            float workers = Prod.EstPercentForNetIncome(wantedIncome);

            workers += CalculateMod(percent, Storage.ProdRatioWithIncoming).Clamped(-0.35f, 0.5f);
            return workers.Clamped(0f, 1f);
        }


        // Core world aims to balance everything, without maximizing food/prod/res
        internal void AssignCoreWorldWorkers()
        {
            float remainingWork;
            if (IsCybernetic) // Filthy Opteris
            {
                AssignCoreWorldProduction(1f);
                remainingWork = 1 - Prod.Percent;
            }
            else // Strategy for Flesh-bags:
            {
                AssignCoreWorldFarmers(1f);
                remainingWork = 1 - Food.Percent;
            }

            if (NonCybernetic)
            {
                // Sabotage freezes the queue, so the queue-biased over-investment
                // path would just pile prod into storage and get taxed away. Route
                // to the storage-driven sizing instead — modest production keeps
                // exports/Dyson/scrap-refund paths working without over-spending.
                if (ConstructionQueue.Count > 0 && !IsSabotaged)
                    Prod.Percent = (remainingWork * EvaluateProductionQueue()).UpperBound(remainingWork);
                else
                    AssignCoreWorldProduction(remainingWork - MinimumResearchNoQueue(remainingWork, 0.5f));
            }

            Res.AutoBalanceWorkers(); // rest goes to research
        }

        // Work for Anything that is not a Core World or Trade Hub is dealt here
        internal void AssignOtherWorldsWorkers(float percentFood, float percentProd, float wantedFoodIncome, float wantedProdIncome)
        {
            Food.Percent        = FarmToPercentage(percentFood, wantedFoodIncome);
            Food.CalculateAveragePercentage();
            float remainingWork = 1 - Food.Percent;
            Prod.Percent        = WorkToPercentage(percentProd, wantedProdIncome).UpperBound(remainingWork);
            if (NonCybernetic)
            {
                if (ConstructionQueue.Count > 0 && !IsSabotaged)
                {
                    // Same freezes queue as AssignCoreWorldWorkers
                    Prod.Percent = CType is ColonyType.Agricultural && Storage.FoodRatio < percentFood * 0.9f
                        ? remainingWork
                        : (remainingWork * EvaluateProductionQueue()).UpperBound(remainingWork);
                }
                else
                {
                    Prod.Percent = remainingWork - MinimumResearchNoQueue(remainingWork, percentProd);
                }
            }

            Res.AutoBalanceWorkers(); // rest goes to research
        }
        
        float MinimumResearchNoQueue(float availableWork, float wantedStoragePercent)
        {
            if (Res.YieldPerColonist.AlmostZero() || availableWork.AlmostZero() || IsCybernetic || Owner.Research.NoTopic)
                return 0; // No need to use researchers

            if (Storage.ProdRatio.AlmostEqual(1))
                return availableWork; // Use all research possible

            float minCut; // Minimum cut the research can take from remaining work
            float maxCut; // Maximum cut the research can take from remaining work
            switch (CType)
            {
                default:
                case ColonyType.Core:         minCut = 0.4f;  maxCut = 0.9f;  break;
                case ColonyType.Research:     minCut = 0.75f; maxCut = 1f;    break;
                case ColonyType.Agricultural: minCut = 0f;    maxCut = 0.4f;  break;
                case ColonyType.Military:
                case ColonyType.Industrial:   minCut = 0f;    maxCut = 0.25f; break;
            }

            return (Storage.ProdRatio / wantedStoragePercent).Clamped(minCut, maxCut) * availableWork;
            
        }

        public void ResetFoodAfterInvasionSuccess()
        {
            if (Owner == null)
                return;

            Res.Percent = 0;
            if (IsCybernetic)
                AssignOtherWorldsWorkers(0, 1, 0, 1);
            else
                AssignOtherWorldsWorkers(0.5f, 0.5f, 1 ,1);
        }

        float MinIncomePerTurn(float storage, ColonyResource res)
        {
            float ratio = storage / Storage.Max;
            if (ratio > 0.9999f)
                return 0; // when idling, keep production low to leave room for others

            float minPerTurn     = res.NetMaxPotential * 0.1f;
            float maxPerTurn     = res.NetMaxPotential * 0.9f; // MAX % for this product
            float shortage       = (Storage.Max*0.8f) - storage;
            float resolveInTurns = 20.0f;
            float perTurn        = (shortage / resolveInTurns).Clamped(minPerTurn, maxPerTurn);
            return perTurn;
        }

        // Core World aims for +1 NetIncome
        void AssignCoreWorldFarmers(float labor)
        {
            float minPerTurn = MinIncomePerTurn(Storage.Food, Food);
            float farmers    = Food.EstPercentForNetIncome(minPerTurn);

            if (farmers > 0 && farmers < 0.1f)
                farmers = 0.1f; // avoid crazy small percentage of labor

            Food.Percent = farmers * labor;
            Food.CalculateAveragePercentage();
        }

        void AssignCoreWorldProduction(float labor)
        {
            if (labor <= 0f) 
                return;

            float minPerTurn = MinIncomePerTurn(Storage.Prod, Prod); // Todo check this
            float workers    = Prod.EstPercentForNetIncome(minPerTurn);

            workers = workers.Clamped(0.1f, 1.0f);
            if (IsCybernetic && ConstructionQueue.Count > 0 && !IsSabotaged)
                 workers *= 1.2f;

            Prod.Percent = workers * labor;
        }

        // @return the ratio of workers that are not assigned to farming.
        float LeftoverWorkers()
        {
            return 1f - Food.WorkersNeededForEquilibrium();
        }

        float EvaluateProductionQueue(QueueItem qi = null)
        {
            if (IsCybernetic || Owner.Research.NoTopic || CType == ColonyType.Industrial)
                return 1;

            var item = qi ?? ConstructionQueue.FirstOrDefault();
            if (item == null || Res.YieldPerColonist.AlmostZero())
            {
                return 1;
            }


            // the player's governor splits the same way whatever personality the empire drew
            const float evenRatio = 0.25f;
            EconomicResearchStrategy strategy = Owner.Research.Strategy;
            float military    = OwnerIsPlayer ? evenRatio : strategy.MilitaryRatio;
            float expansion   = OwnerIsPlayer ? evenRatio : strategy.ExpansionRatio;
            float research    = OwnerIsPlayer ? evenRatio : strategy.ResearchRatio;
            float industry    = OwnerIsPlayer ? evenRatio : strategy.IndustryRatio;
            float opportunism = OwnerIsPlayer ? 0 : Owner.data.DiplomaticPersonality?.Opportunism ?? 1;
            float buildDesire = opportunism + military + expansion + industry;

            // colony level ranges from 1 to 5, giving a base of about 0.13 to 0.23
            float colonyDevelopmentBonus = ((6 + buildDesire) - Level * research) * 0.03f;
            float colonyTypeBonus        = 0;
            switch (CType)
            {
                case ColonyType.Core:     colonyTypeBonus = Owner.CoreProductionBonus;                  break;
                case ColonyType.Military: colonyTypeBonus = Owner.CoreProductionBonus.LowerBound(0.1f); break;
            }

            float workerPercentage = colonyDevelopmentBonus + colonyTypeBonus;

            if (item.isBuilding)
            {
                //set base pri to industry ratio;
                switch (item.Building.Category)
                {
                    case BuildingCategory.General:
                    case BuildingCategory.Storage:
                    case BuildingCategory.Shipyard:   workerPercentage += industry;  break;
                    case BuildingCategory.Production: workerPercentage += industry;  break;
                    case BuildingCategory.Finance:
                    case BuildingCategory.Food:       workerPercentage += expansion; break;
                    case BuildingCategory.Defense:
                    case BuildingCategory.Military:
                    case BuildingCategory.Sensor:     workerPercentage += military;  break;
                    case BuildingCategory.Science:    workerPercentage += research;  break;
                    case BuildingCategory.Population:
                    case BuildingCategory.Terraforming:
                    case BuildingCategory.Growth:
                    case BuildingCategory.Biosphere:  workerPercentage += expansion; break;
                    case BuildingCategory.Victory:    workerPercentage = 0.75f;      break;
                    default:                          workerPercentage += 0.2f;      break;
                }
                if (item.Building.IsCapitalOrOutpost)
                    workerPercentage = 1.0f;
            }

            if (item.isShip)
            {
                switch (item.ShipData.Role)
                {
                    case RoleName.freighter:    workerPercentage += industry;  break;
                    case RoleName.station:      workerPercentage += industry;  break;
                    case RoleName.construction: workerPercentage += expansion; break;
                    default:                    workerPercentage += military;  break;
                }
            }

            if (item.isTroop)   workerPercentage += military;
            if (item.isOrbital) workerPercentage += 0.1f;

            if (workerPercentage <= 0)
                Log.Error($"Queue Item gave no bonus production. This is likely a bug. item: {item.DisplayName} ");

            return workerPercentage.Clamped(0.0f,1.0f);
        }
    }
}
