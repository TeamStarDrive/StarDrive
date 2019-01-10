using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Universe.SolarBodies;

namespace Ship_Game
{
    public partial class Planet
    {
        public void InitializeWorkerDistribution(Empire o)
        {
            if (o.IsCybernetic || Type == "Barren")
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

        void AssignCoreWorldWorkers()
        {
            if (IsCybernetic)
            {
                AssignCoreWorldProduction(1f);
                Res.AutoBalanceWorkers(); // rest goes to research
            }
            else
            {
                AssignCoreWorldFarmers();
                AssignCoreWorldProduction(1f - Food.Percent);
                Res.AutoBalanceWorkers(); // rest goes to research
            }
        }

        float DesiredNetIncomeFromStorage(float storage)
        {
            float ratio = storage / Storage.Max;
            if (ratio > 0.8f)
                return +1.0f; // by default we want +1.0 income



            return +5.0f;
        }

        // Core World aims for +1 NetIncome
        void AssignCoreWorldFarmers()
        {

            float minFarmers = Food.EstPercentForNetIncome(+1.0f);


            float storageFill = (1f - Storage.FoodRatio).Clamped(0f, 0.5f);
            float farmers = (1f - Storage.FoodRatio).Clamped(minFarmers, 0.9f);

            if (farmers > 0 && farmers < 0.1f)
                farmers = 0.1f; // avoid crazy small percentage of labor

            Food.Percent = farmers;
        }

        void AssignCoreWorldProduction(float labor)
        {
            if (labor <= 0f) return;

            float minWorkers = Prod.EstPercentForNetIncome(+1.0f);
            float workers = (1f - Storage.ProdRatio).Clamped(minWorkers, 0.9f);

            if (workers > 0 && workers < 0.1f)
                workers = 0.1f; // avoid crazy small percentage of labor

            if (ConstructionQueue.Count > 1 && workers < 0.75f)
                workers = 0.75f; // minimum value if construction is going on

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
    }
}
