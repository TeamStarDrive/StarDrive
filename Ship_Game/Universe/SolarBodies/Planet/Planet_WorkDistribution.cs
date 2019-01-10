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


        // Core world aims to balance everything, without maximizing food/prod/res
        void AssignCoreWorldWorkers()
        {
            if (IsCybernetic) // Filthy Opteris
            {
                AssignCoreWorldProduction(1f);
                Res.AutoBalanceWorkers(); // rest goes to research
            }
            else // Strategy for Flesh-bags:
            {
                AssignCoreWorldFarmers(0.8f); 
                AssignCoreWorldProduction(0.8f - Food.Percent); // then we optimize production
                Res.AutoBalanceWorkers(); // and rest goes to research
            }
        }

        float MinIncomePerTurn(float storage, ColonyResource res)
        {
            float ratio = storage / Storage.Max;
            if (ratio > 0.8f)
                return +1.5f; // when idling, keep production low to leave room for others

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
            float minPerTurn = MinIncomePerTurn(Storage.Food, Food);
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
