using Ship_Game.Ships;
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
            if (percent <= 0f || Food.YieldPerColonist <= 0.5f || IsCybernetic)
                return 0; // No farming here, so never mind

            float farmers = Food.EstPercentForNetIncome(+1f);

            // modify nominal farmers by overage or underage
            farmers += CalculateMod(percent, Storage.FoodRatio).Clamped(-0.35f, 0.50f);
            return farmers.Clamped(0f, 0.9f);
        }

        float WorkToPercentage(float percent) // Production
        {
            if (percent <= 0f) return 0;

            float workers = Prod.EstPercentForNetIncome(+1f);

            workers += CalculateMod(percent, Storage.ProdRatio).Clamped(-0.35f, 1.00f);
            return workers.Clamped(0f, 1f);
        }


        // Core world aims to balance everything, without maximizing food/prod/res
        void AssignCoreWorldWorkers()
        {
            if (IsCybernetic) // Filthy Opteris
                AssignCoreWorldProduction(1f);
            else // Strategy for Flesh-bags:
            {
                AssignCoreWorldFarmers(1f);
                AssignCoreWorldProduction(1f - Food.Percent); // then we optimize production
            }

            Res.AutoBalanceWorkers(); // rest goes to research
        }

        // Work for Anything that is not a Core World or Trade Hub is dealt here
        void AssignOtherWorldsWorkers(float percentFood, float percentProd)
        {
            Food.Percent = FarmToPercentage(percentFood);
            Prod.Percent = Math.Min(1 - Food.Percent, WorkToPercentage(percentProd));
            if (ConstructionQueue.Count > 0)
                Prod.Percent = Math.Max(Prod.Percent, (1 - Food.Percent) * EvaluateProductionQueue());

            Res.AutoBalanceWorkers(); // rest goes to research
        }

        float MinIncomePerTurn(float storage, ColonyResource res)
        {
            float ratio = storage / Storage.Max;
            if (ratio > 0.8f)
                return 1; // when idling, keep production low to leave room for others

            float minPerTurn = res.NetMaxPotential * 0.1f;
            float maxPerTurn = res.NetMaxPotential * 0.9f; // MAX % for this product

            float shortage = (Storage.Max*0.8f) - storage;
            float resolveInTurns = 20.0f;
            float perTurn = (shortage / resolveInTurns).Clamped(minPerTurn, maxPerTurn);
            return perTurn;
        }

        // Core World aims for +1 NetIncome
        void AssignCoreWorldFarmers(float labor)
        {
            float minPerTurn = MinIncomePerTurn(Storage.Food, Food) * (1-Owner.ResearchStrategy.ResearchRatio);
            float farmers = Food.EstPercentForNetIncome(minPerTurn);

            if (farmers > 0 && farmers < 0.1f)
                farmers = 0.1f; // avoid crazy small percentage of labor

            Food.Percent = farmers * labor;
        }

        void AssignCoreWorldProduction(float labor)
        {
            if (labor <= 0f) return;

            float minPerTurn = MinIncomePerTurn(Storage.Prod, Prod);
            float workers = Prod.EstPercentForNetIncome(minPerTurn);

            workers += EvaluateProductionQueue();
            workers = workers.Clamped(0.1f, 1.0f);
            //    workers = 0.75f; // minimum value if construction is going on

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
            var item = ConstructionQueue.FirstOrDefault();
            if (item == null) return 0;
            if (item.IsPlayerAdded) return 1;

            // colony level ranges from 1 worst to 5 best.
            // this should give a range from 0 - .25f
            float colonyDevelopmentBonus = (5 - Level) * 0.05f;
            float colonyTypeBonus = 0;
            switch (colonyType)
            {
                case ColonyType.Industrial: colonyTypeBonus = 0.05f; break;
                case ColonyType.Military:   colonyTypeBonus = 0.01f; break;
            }
            float workerPercentage = colonyDevelopmentBonus + colonyTypeBonus;

            if (item.isBuilding)
            {
                //set base pri to industry ratio;
                switch (item.Building.Category)
                {
                    case BuildingCategory.General:
                    case BuildingCategory.Storage:
                    case BuildingCategory.Shipyard:
                        workerPercentage += Owner.ResearchStrategy.IndustryRatio;
                        break;
                    case BuildingCategory.Production:
                    case BuildingCategory.Finance:
                    case BuildingCategory.Food:
                        workerPercentage += Owner.ResearchStrategy.IndustryRatio;
                        workerPercentage += Owner.ResearchStrategy.ExpansionRatio;
                        break;
                    case BuildingCategory.Defense:
                    case BuildingCategory.Military:
                    case BuildingCategory.Sensor:
                        workerPercentage += Owner.ResearchStrategy.MilitaryPriority;
                        break;
                    case BuildingCategory.Science:
                        workerPercentage += Owner.ResearchStrategy.ResearchRatio;
                        workerPercentage += Owner.ResearchStrategy.ExpansionRatio;
                        break;
                    case BuildingCategory.Population:
                    case BuildingCategory.Terraforming:
                    case BuildingCategory.Growth:
                    case BuildingCategory.Biosphere:
                        workerPercentage += Owner.ResearchStrategy.ExpansionRatio;
                        break;
                    case BuildingCategory.Victory:
                        workerPercentage = 0.75f;
                        break;
                    default:
                        workerPercentage += 0.2f;
                        break;
                }
                if (item.Building.IsCapitalOrOutpost)
                    workerPercentage = 1.0f;
            }

            if (item.isShip)
            {
                switch (item.sData.Role)
                {
                    case ShipData.RoleName.freighter:
                        workerPercentage += Owner.ResearchStrategy.ExpansionRatio + Owner.ResearchStrategy.IndustryRatio;
                        break;
                    case ShipData.RoleName.station:
                        workerPercentage += Owner.ResearchStrategy.IndustryRatio;
                        break;
                    default:
                        workerPercentage += Owner.ResearchStrategy.MilitaryRatio;
                        break;

                }
            }

            if (item.isTroop)   workerPercentage += Owner.ResearchStrategy.MilitaryRatio + Owner.ResearchStrategy.ExpansionRatio;
            if (item.isOrbital) workerPercentage += 0.1f;

            if (workerPercentage <= 0)
                Log.Error($"Queue Item gave no bonus production. This is likely a bug. item: {item.DisplayName} ");
            return workerPercentage.Clamped(0.0f,1.0f);
        }
    }
}
