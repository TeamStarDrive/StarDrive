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

        // Calculate farmers with these adjustments
        float CalculateFoodWorkers(float pFlatFood = 0.0f, float pFoodPerCol = 0.0f)
        {
            if (IsCybernetic || Population <= 0) return 0;
            float foodYield = Food.YieldPerColonist + pFoodPerCol;
            if (foodYield <= 0.5f) return 0;
            float flatBonus = Food.FlatBonus + pFlatFood;
            float workers = (Consumption - flatBonus) / (PopulationBillion * foodYield);
            return workers.Clamped(0.0f, 0.9f); // Don't allow farmers to consume all labor
        }

        float CalculateMod(float desiredPercent, float storageRatio)
        {
            float mod = (desiredPercent - storageRatio) * 2;             //Percentage currently over or under desired storage
            if (mod > 0 && mod < 0.05) mod = 0.05f;	//Avoid crazy small percentage
            if (mod < 0 && mod > -0.05) mod = 0.00f;	//Avoid bounce (stop if slightly over)

            return mod;
        }

        //This will calculate a smooth transition to maintain [percent]% of stored food. It will under-farm if over
        //[percent]% of storage, or over-farm if under it. Returns labor needed
        float FarmToPercentage(float percent)   //Production and Research
        {
            if (percent == 0) return 0;
            if (Food.YieldPerColonist <= 0.5f || IsCybernetic) return 0; //No farming here, so never mind
            float minFarmers = CalculateFoodWorkers();          //Nominal Farmers needed to neither gain nor lose storage
            float storedFoodRatio = Storage.FoodRatio;      //Percentage of Food Storage currently filled

            if (Food.FlatBonus > 0)
            {
                //Stop producing food a little early, since the flat food will continue to pile up
                float maxPop = MaxPopulationBillion;
                if (Food.FlatBonus > maxPop) storedFoodRatio += 0.15f * Math.Min(Food.FlatBonus - maxPop, 3);
                storedFoodRatio = storedFoodRatio.Clamped(0, 1);
            }

            minFarmers += CalculateMod(percent, storedFoodRatio).Clamped(-0.35f, 0.50f);             //modify nominal farmers by overage or underage
            minFarmers = minFarmers.Clamped(0, 0.9f);                  //Tame resulting value, dont let farming completely consume all labor
            return minFarmers;                          //Return labor % of farmers to progress toward goal
        }

        float WorkToPercentage(float percent)   //Production and Research
        {
            if (percent == 0) return 0;
            float minWorkers = 0;
            if (IsCybernetic)
            {											//Nominal workers needed to feed all of the the filthy Opteris
                minWorkers = (Consumption - Prod.FlatBonus) / PopulationBillion / Prod.YieldPerColonist;
                minWorkers = minWorkers.Clamped(0, 1);
            }

            float storedProdRatio = Storage.ProdRatio;      //Percentage of Prod Storage currently filled

            if (Prod.FlatBonus > 0)      //Stop production early, since the flat production will continue to pile up
            {
                if (IsCybernetic)
                {
                    float maxPop = MaxPopulationBillion;
                    if (Prod.FlatBonus > maxPop) storedProdRatio += 0.15f * Math.Min(Prod.FlatBonus - maxPop, 3);
                }
                else
                {
                    storedProdRatio += 0.15f * Math.Min(Prod.FlatBonus, 3);
                }
                storedProdRatio = storedProdRatio.Clamped(0, 1);
            }

            minWorkers += CalculateMod(percent, storedProdRatio).Clamped(-0.35f, 1.00f);
            minWorkers = minWorkers.Clamped(0, 1);
            return minWorkers;                          //Return labor % to progress toward goal
        }

        void FillOrResearch(float labor)    //Core and TradeHub
        {
            FarmOrResearch(labor / 2);
            WorkOrResearch(labor / 2);
        }

        void FarmOrResearch(float labor)   //Agreculture
        {
            if (labor.AlmostZero()) return;
            if (IsCybernetic)
            {
                WorkOrResearch(labor);  //Hand off to Prod instead;
                return;
            }
            float maxPop = MaxPopulationBillion;
            float storedFoodRatio = Storage.FoodRatio;      //How much of Storage is filled
            if (Food.YieldPerColonist <= 0.5f) storedFoodRatio = 1; //No farming here, so skip it

            //Stop producing food a little early, since the flat food will continue to pile up
            if (Food.FlatBonus > maxPop) storedFoodRatio += 0.15f * Math.Min(Food.FlatBonus - maxPop, 3);
            if (storedFoodRatio > 1) storedFoodRatio = 1;

            float farmers = 1 - storedFoodRatio;    //How much storage is left to fill
            if (farmers >= 0.5f) farmers = 1;		//Work out percentage of [labor] to allocate
            else farmers = farmers * 2;
            if (farmers > 0 && farmers < 0.1f) farmers = 0.1f;    //Avoid crazy small percentage of labor

            Food.Percent += farmers * labor;	//Assign Farmers
            Res.Percent  += labor - (farmers * labor);//Leftovers go to Research
        }

        void WorkOrResearch(float labor)    //Industrial
        {
            if (labor.AlmostZero()) return;
            float storedProdRatio = Storage.ProdRatio;      //How much of Storage is filled

            if (IsCybernetic)       //Stop production early, since the flat production will continue to pile up
            {
                float maxPop = MaxPopulationBillion;
                if (Prod.FlatBonus > maxPop) storedProdRatio += 0.15f * Math.Min(Prod.FlatBonus - maxPop, 3);
            }
            else
            {
                if (Prod.FlatBonus > 0) storedProdRatio += 0.15f * Math.Min(Prod.FlatBonus, 3);
            }
            if (storedProdRatio > 1) storedProdRatio = 1;

            float workers = 1 - storedProdRatio;    //How much storage is left to fill
            if (workers >= 0.5f) workers = 1;		//Work out percentage of [labor] to allocate
            else workers = workers * 2;
            if (workers > 0 && workers < 0.1f) workers = 0.1f;    //Avoid crazy small percentage of labor

            if (ConstructionQueue.Count > 1 && workers < 0.75f) workers = 0.75f;  //Minimum value if construction is going on

            Prod.Percent += workers * labor;	//Assign workers
            Res.Percent += labor - (workers * labor);//Leftovers go to Research
        }

        float LeftoverWorkers()
        {
            //Returns the number of workers (in Billions) that are not assigned to farming.
            return ((1 - CalculateFoodWorkers()) * MaxPopulationBillion);
        }

    }
}
