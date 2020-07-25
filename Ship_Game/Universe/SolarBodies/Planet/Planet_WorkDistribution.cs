﻿using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        float FarmToPercentage(float percent) // Production
        {
            if (percent <= 0f || Food.YieldPerColonist <= 0.1f || IsCybernetic)
                return 0; // No farming here, so never mind

            float farmers = Food.EstPercentForNetIncome(+1f);

            // modify nominal farmers by overage or underage
            farmers += CalculateMod(percent, Storage.FoodRatio).Clamped(-0.35f, 0.5f);
            return farmers.Clamped(0f, 0.9f);
        }

        float WorkToPercentage(float percent) // Production
        {
            if (percent <= 0f || Prod.YieldPerColonist <= 0.1f) 
                return 0;

            float workers = Prod.EstPercentForNetIncome(+1f, IsCybernetic);

            workers += CalculateMod(percent, Storage.ProdRatio).Clamped(-0.35f, 0.5f);
            return workers.Clamped(0f, 1f);
        }


        // Core world aims to balance everything, without maximizing food/prod/res
        void AssignCoreWorldWorkers()
        {
            float remainingWork;
            if (IsCybernetic) // Filthy Opteris
            {
                AssignCoreWorldProduction(1f);
                //WorkToPercentage(1);
                remainingWork = 1;
            }
            else // Strategy for Flesh-bags:
            {
                AssignCoreWorldFarmers(1f);
                remainingWork = 1 - Food.Percent;
            }

            if (NonCybernetic)
            {
                if (ConstructionQueue.Count > 0)
                    Prod.Percent = (remainingWork * EvaluateProductionQueue()).UpperBound(remainingWork);
                else
                    AssignCoreWorldProduction(remainingWork - MinimumResearch(remainingWork));
            }
            Res.AutoBalanceWorkers(); // rest goes to research
        }

        // Work for Anything that is not a Core World or Trade Hub is dealt here
        void AssignOtherWorldsWorkers(float percentFood, float percentProd)
        {
            Food.Percent        = FarmToPercentage(percentFood);
            float remainingWork = 1 - Food.Percent;
            Prod.Percent        = WorkToPercentage(percentProd).UpperBound(remainingWork);
            if (NonCybernetic)
            {
                if (ConstructionQueue.Count > 0)
                    Prod.Percent = (remainingWork * EvaluateProductionQueue()).UpperBound(Prod.Percent);
                else
                    Prod.Percent = remainingWork - MinimumResearch(remainingWork);
            }

            Res.AutoBalanceWorkers(); // rest goes to research
        }

        float MinimumResearch(float availableWork)
        {
            if (Res.YieldPerColonist.AlmostZero() || availableWork.AlmostZero() || IsCybernetic)
                return 0; // No need to use researchers

            float maximumCut; // Maximum cut the research can take from remaining work
            switch (colonyType)
            {
                default:
                case ColonyType.Core:         maximumCut = 0.3f;  break;
                case ColonyType.Research:     maximumCut = 0.75f; break;
                case ColonyType.Agricultural: maximumCut = 0.2f; break;
                case ColonyType.Military:
                case ColonyType.Industrial:   maximumCut = 0.1f; break;
            }

            float researchRatio = TotalPotentialResearchersYield / Owner.TotalPotentialResearchPerColonist;
            return researchRatio.UpperBound(maximumCut) * availableWork;
            
        }

        public void ResetFoodAfterInvasionSuccess()
        {
            if (Owner == null)
                return;

            Res.Percent = 0;
            if (Owner.IsCybernetic)
                AssignOtherWorldsWorkers(0, 1);
            else
                AssignOtherWorldsWorkers(0.5f, 0.5f);
        }

        float MinIncomePerTurn(float storage, ColonyResource res)
        {
            float ratio = storage / Storage.Max;
            if (ratio.AlmostEqual(1))
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
            float farmers = Food.EstPercentForNetIncome(minPerTurn);

            if (farmers > 0 && farmers < 0.1f)
                farmers = 0.1f; // avoid crazy small percentage of labor

            Food.Percent = farmers * labor;
        }

        void AssignCoreWorldProduction(float labor)
        {
            if (labor <= 0f) return;

            float researchNeed = Level < 3 ? 1 : 1 - (0.25f + Owner.Research.Strategy.ResearchRatio);

            float minPerTurn = MinIncomePerTurn(Storage.Prod, Prod);
            float workers = Prod.EstPercentForNetIncome(minPerTurn, IsCybernetic);

            workers = workers.Clamped(0.1f, 1.0f);
            if (IsCybernetic & ConstructionQueue.Count > 0)
                 workers *= 1.2f;

            Prod.Percent = workers * labor;
        }

        // @return the ratio of workers that are not assigned to farming.
        float LeftoverWorkers()
        {
            return 1f - Food.WorkersNeededForEquilibrium();
        }

        float LeftoverWorkerBillions()
        {
            return LeftoverWorkers() * MaxPopulationBillion;
        }

        float EvaluateProductionQueue()
        {
            if (IsCybernetic)
                return 1;

            var item = ConstructionQueue.FirstOrDefault();
            if (item == null
                || item.IsPlayerAdded
                || Res.YieldPerColonist.AlmostZero())
            {
                return 1;
            }


            float buildDesire = (Owner.data.DiplomaticPersonality?.Opportunism ?? 1) + Owner.Research.Strategy.MilitaryRatio 
                                                                                     + Owner.Research.Strategy.ExpansionRatio 
                                                                                     + Owner.Research.Strategy.IndustryRatio;

            // colony level ranges from 1 worst to 5 best.
            // Gives a base line from .3 to about .1 depending on research wants
            float colonyDevelopmentBonus = ((6 + buildDesire) - Level * Owner.Research.Strategy.ResearchRatio) * 0.05f;
            float colonyTypeBonus        = 0;
            switch (colonyType)
            {
                case ColonyType.Industrial: colonyTypeBonus = 0.2f; break;
                case ColonyType.Military:   colonyTypeBonus = 0.05f; break;
            }

            float workerPercentage = colonyDevelopmentBonus + colonyTypeBonus;

            if (item.isBuilding)
            {
                //set base pri to industry ratio;
                switch (item.Building.Category)
                {
                    case BuildingCategory.General:
                    case BuildingCategory.Storage:
                    case BuildingCategory.Shipyard:   workerPercentage += Owner.Research.Strategy.IndustryRatio;  break;
                    case BuildingCategory.Production: workerPercentage += Owner.Research.Strategy.IndustryRatio;  break;
                    case BuildingCategory.Finance:
                    case BuildingCategory.Food:       workerPercentage += Owner.Research.Strategy.ExpansionRatio; break;
                    case BuildingCategory.Defense:
                    case BuildingCategory.Military:
                    case BuildingCategory.Sensor:     workerPercentage += Owner.Research.Strategy.MilitaryRatio;  break;
                    case BuildingCategory.Science:    workerPercentage += Owner.Research.Strategy.ResearchRatio;  break;
                    case BuildingCategory.Population:
                    case BuildingCategory.Terraforming:
                    case BuildingCategory.Growth:
                    case BuildingCategory.Biosphere:  workerPercentage += Owner.Research.Strategy.ExpansionRatio; break;
                    case BuildingCategory.Victory:    workerPercentage = 0.75f;                                   break;
                    default:                          workerPercentage += 0.2f;                                   break;
                }
                if (item.Building.IsCapitalOrOutpost)
                    workerPercentage = 1.0f;
            }

            if (item.isShip)
            {
                switch (item.sData.Role)
                {
                    case ShipData.RoleName.freighter:    workerPercentage += Owner.Research.Strategy.IndustryRatio;  break;
                    case ShipData.RoleName.station:      workerPercentage += Owner.Research.Strategy.IndustryRatio;  break;
                    case ShipData.RoleName.construction: workerPercentage += Owner.Research.Strategy.ExpansionRatio; break;
                    default:                             workerPercentage += Owner.Research.Strategy.MilitaryRatio;  break;
                }
            }

            if (item.isTroop)   workerPercentage += Owner.Research.Strategy.MilitaryRatio;
            if (item.isOrbital) workerPercentage += 0.1f;

            if (workerPercentage <= 0)
                Log.Error($"Queue Item gave no bonus production. This is likely a bug. item: {item.DisplayName} ");

            return workerPercentage.Clamped(0.0f,1.0f);
        }
    }
}
