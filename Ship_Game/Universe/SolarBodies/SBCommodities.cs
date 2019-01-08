using System;
using Ship_Game.Universe.SolarBodies.AI;

namespace Ship_Game.Universe.SolarBodies
{
    public class SBCommodities
    {
        public TradeAI Trade { get;}
        readonly Planet Ground;
        readonly Map<string, float> Commodities = new Map<string, float>(StringComparer.OrdinalIgnoreCase);

        public SBCommodities(Planet planet)
        {
            Trade = new TradeAI(planet);
            Ground = planet;
        }

        public int CommoditiesCount => Commodities.Count;
        public bool ContainsGood(string goodId) => Commodities.ContainsKey(goodId);
        public void ClearGoods()
        {
            Commodities.Clear();
        }

        // different from Food -- this is based on race
        public float RaceSpecificFood
        {
            get => GetGoodAmount(RacialTrait.GetFoodType(Ground.Owner?.data.Traits));
            set => AddCommodity(RacialTrait.GetFoodType(Ground.Owner?.data.Traits), value);
        }
        
        float FoodValue; // @note These are special fields for perf reasons.
        float ProdValue;
        float PopValue;

        public float Food
        {
            get => FoodValue;
            set => FoodValue = value.Clamped(0f, Ground.MaxStorage);
        }

        public float Production
        {
            get => ProdValue;
            set => ProdValue = value.Clamped(0f, Ground.MaxStorage);
        }

        public float Population
        {
            get => PopValue;
            set => PopValue = value.Clamped(0f, Ground.MaxPopWithBonus);
        }

        public void AddCommodity(string goodId, float amount)
        {
            switch (goodId)
            {
                default:               Commodities[goodId] = GetGoodAmount(goodId) + amount; break;
                case "Food":           Food       += amount; break;
                case "Production":     Production += amount; break;
                case "Colonists_1000": Population += amount; break;
            }
        }

        void SetGoodAmount(string goodId, float amount)
        {
            switch (goodId)
            {
                default:               Commodities[goodId] = amount; break;
                case "Food":           Food       = amount; break;
                case "Production":     Production = amount; break;
                case "Colonists_1000": Population = amount; break;
            }
        }
        public float GetGoodAmount(Goods good)
        {
            switch (good)
            {
                default:               return 0;
                case Goods.Production: return Production;
                case Goods.Food:       return RaceSpecificFood;
                case Goods.Colonists:  return Population;
            }
        }        

        public float GetGoodAmount(string goodId)
        {
            switch (goodId)
            {
                case "Food":           return Food;
                case "Production":     return Production;
                case "Colonists_1000": return Population;
            }
            return Commodities.TryGetValue(goodId, out float commodity) ? commodity : 0;
        }
        
        public float HarvestFood()
         {
            float unfed = 0.0f;     //Pop that did not get any food
            if (Ground.IsCybernetic)
            {
                Food = 0.0f;      //Seems unused
                Ground.NetProductionPerTurn -= Ground.Consumption;  //Reduce production by how much is consumed

                float productionHere = Math.Min(0, Production + Ground.NetProductionPerTurn);

                if (Production >= Ground.MaxStorage)
                {
                    unfed = 0.0f;
                }
                else if (productionHere < 0)
                {
                    unfed = productionHere;
                    Production = 0;
                }
            }
            else
            {
                Ground.FoodPerTurn -= Ground.Consumption;            // Reduce food by how much is consumed
                float foodHere = RaceSpecificFood + Ground.FoodPerTurn;      // Add any remaining to storage
                 
                if (foodHere >= Ground.MaxStorage)
                {
                    unfed = 0.0f;
                }
                else if (foodHere <= 0)
                {
                    unfed = foodHere;                    
                }
                RaceSpecificFood = foodHere;
            }            
            return unfed;
        }

        public void BuildingResources()
        {
            foreach (Building b in Ground.BuildingList)
            {
                if (b.ResourceCreated == null) continue;
                if (b.ResourceConsumed != null)
                {
                    float resource = GetGoodAmount(b.ResourceConsumed);
                    if (resource >= b.ConsumptionPerTurn)
                    {
                        resource -= b.ConsumptionPerTurn;
                        resource += b.OutputPerTurn;
                        SetGoodAmount(b.ResourceConsumed, resource);
                    }
                }
                else if (b.CommodityRequired != null)
                {
                    if (Ground.SbCommodities.ContainsGood(b.CommodityRequired))
                    {
                        foreach (Building other in Ground.BuildingList)
                        {
                            if (other.IsCommodity && other.Name == b.CommodityRequired)
                            {
                                AddCommodity(b.ResourceCreated, b.OutputPerTurn);
                            }
                        }
                    }
                }
                else
                {
                    AddCommodity(b.ResourceCreated, b.OutputPerTurn);
                }
            }
        }
    }
}