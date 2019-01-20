using System;
using Ship_Game.Universe.SolarBodies.AI;

namespace Ship_Game.Universe.SolarBodies
{
    public class ColonyStorage
    {
        public TradeAI Trade { get;}
        public float Max { get; set; } = 10f;
        readonly Planet Ground;
        readonly Map<string, float> Commodities = new Map<string, float>(StringComparer.OrdinalIgnoreCase);

        public ColonyStorage(Planet planet)
        {
            Trade = new TradeAI(planet);
            Ground = planet;
        }

        public int CommoditiesCount => Commodities.Count;
        public bool ContainsGood(string goodId) => Commodities.ContainsKey(goodId);

        float FoodValue; // @note These are special fields for perf reasons.
        float ProdValue;
        float PopValue;

        public float Food
        {
            get => FoodValue;
            set => FoodValue = value.Clamped(0f, Max);
        }

        public float Prod
        {
            get => ProdValue;
            set => ProdValue = value.Clamped(0f, Max);
        }

        public float Population
        {
            get => PopValue;
            set => PopValue = value.Clamped(0f, Ground.MaxPopulation);
        }

        // different from Food -- this is based on race
        // cybernetics consume production, organics consume food
        public float RaceFood => Ground.IsCybernetic ? ProdValue : FoodValue;

        public float RaceFoodRatio => RaceFood / Max;
        public float FoodRatio => FoodValue / Max;
        public float ProdRatio => ProdValue / Max;
        public float PopRatio  => PopValue  / Ground.MaxPopulation;

        public void AddCommodity(string goodId, float amount)
        {
            switch (goodId)
            {
                default:               Commodities[goodId] = GetGoodAmount(goodId) + amount; break;
                case "Food":           Food += amount; break;
                case "Production":     Prod += amount; break;
                case "Colonists_1000": Population += amount; break;
            }
        }

        void SetGoodAmount(string goodId, float amount)
        {
            switch (goodId)
            {
                default:               Commodities[goodId] = amount; break;
                case "Food":           Food = amount; break;
                case "Production":     Prod = amount; break;
                case "Colonists_1000": Population = amount; break;
            }
        }

        float AddWithRemainder(ref float oldValue, float amount)
        {
            float remainder = 0f;
            float newValue = oldValue + amount;
            if      (newValue < 0f)  remainder = newValue;
            else if (newValue > Max) remainder = newValue - Max;
            oldValue = newValue.Clamped(0f, Max);
            return remainder;
        }

        public void ConsumeProduction(ref float toConsume)
        {
            MathExt.Consume(ref ProdValue, ref toConsume);
        }

        // @return Surplus (positive) or Shortage (negative)
        public float AddFoodWithRemainder(float amount) => AddWithRemainder(ref FoodValue, amount);

        // @return Surplus (positive) or Shortage (negative)
        public float AddProdWithRemainder(float amount) => AddWithRemainder(ref ProdValue, amount);

        // @note Uses RaceFood for Food. Is this correct??
        public float GetGoodAmount(Goods good)
        {
            switch (good)
            {
                default:               return 0;
                case Goods.Food:       return Food;
                case Goods.Production: return Prod;
                case Goods.Colonists:  return Population;
            }
        }

        public float GetGoodAmount(string goodId)
        {
            switch (goodId)
            {
                case "Food":           return Food;
                case "Production":     return Prod;
                case "Colonists_1000": return Population;
            }
            return Commodities.TryGetValue(goodId, out float commodity) ? commodity : 0;
        }

        public void DistributeSpecialBuildingResources()
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
                // CommodityRequired for ResourceCreated
                else if (b.CommodityRequired != null)
                {
                    if (Ground.Storage.ContainsGood(b.CommodityRequired))
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