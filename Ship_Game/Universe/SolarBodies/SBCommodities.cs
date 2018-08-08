using System;
using System.Collections.Generic;

namespace Ship_Game.Universe.SolarBodies
{
    public class SBCommodities
    {
        private readonly Planet Ground;

        //private Array<PlanetGridSquare> TilesList => Ground.TilesList;
        private Empire Owner => Ground.Owner;
        //private BatchRemovalCollection<Troop> TroopsHere => Ground.TroopsHere;
        private Array<Building> BuildingList => Ground.BuildingList;
        //private BatchRemovalCollection<Combat> ActiveCombats => Ground.ActiveCombats;
        //private SolarSystem ParentSystem => Ground.ParentSystem;        
        private Map<string, float> Commoditites = new Map<string, float>(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyDictionary<string, float> ResourcesDictionary => Commoditites;
        private float Waste;
        public Array<string> CommoditiesPresent = new Array<string>();
        public SBCommodities (Planet planet)
        {
            Ground = planet;
        }

        public float FoodHere
        {
            get => GetGoodAmount(RacialTrait.GetFoodType(Owner?.data.Traits));
            set => AddGood(RacialTrait.GetFoodType(Owner?.data.Traits), value);
        }
        //actual food becuase food will return production for cybernetics. 
        public float FoodHereActual
        {
            get => GetGoodAmount("Food");
            set => AddGood("Food", value);
        }
        public float ProductionHere
        {
            get => GetGoodAmount("Production");
            set => AddGood("Production", value);
        }

        public float Population
        {
            get => GetGoodAmount("Colonists_1000");
            set => AddGood("Colonists_1000", value);
        }

        public float AddGood(string goodId, float amount, bool clamp = true)
        {
            float max = float.MaxValue;
            if (clamp)
                switch (goodId)
                {
                    case "Food":
                    case "Production":
                        {
                            max = Ground.MaxStorage;
                            break;
                        }
                    case "Colonists_1000":
                        {
                            max = Ground.MaxPopulation + Ground.MaxPopBonus;
                            break;
                        }
                    default:
                        break;

                }
            //clamp by storage capability and return amount not stored. 
            float stored = Math.Max(0, amount);
            stored = Math.Min(stored, max);
            Commoditites[goodId] = stored;
            return amount - stored;
        }
        public float GetGoodAmount(Goods good)
        {
            switch(good)
            {
                case Goods.None:
                    return 0;
                case Goods.Production:
                    return ProductionHere;
                case Goods.Food:
                    return FoodHere;
                case Goods.Colonists:
                    return Population;
                default:
                    throw new ArgumentOutOfRangeException(nameof(good), good, null);
            }
       
        }        

        public float GetGoodAmount(string goodId)
        {
            if (Commoditites.TryGetValue(goodId, out float commodity)) return commodity;
            return 0;
        }
        
        public float HarvestFood()
         {
            float unfed = 0.0f;     //Pop that did not get any food
            if (Owner.data.Traits.Cybernetic > 0)
            {
                FoodHereActual = 0.0f;      //Seems unused
                Ground.NetProductionPerTurn -= Ground.Consumption;  //Reduce production by how much is consumed

                float productionHere = Math.Min(0, ProductionHere + Ground.NetProductionPerTurn);
                

                if (ProductionHere >= Ground.MaxStorage)
                {
                    unfed = 0.0f;
                    
                }
                else if (productionHere < 0)
                {

                    unfed = productionHere;
                    ProductionHere = 0;
                    
                }
            }
            else
            {
                Ground.NetFoodPerTurn -= Ground.Consumption;            //Reduce food by how much is consumed
                float foodHere = FoodHere + Ground.NetFoodPerTurn;      //Add any remaining to storage
                 
                if (foodHere >= Ground.MaxStorage)
                {
                    unfed = 0.0f;
                }
                else if (foodHere <= 0)
                {
                    unfed = foodHere;                    
                }
                FoodHere = foodHere;
            }            
            return unfed;
        }

        public void BuildingResources()
        {
            foreach (Building building1 in BuildingList)
            {
                if (building1.ResourceCreated != null)
                {
                    if (building1.ResourceConsumed != null)
                    {
                        float resource = Commoditites[building1.ResourceConsumed];
                        
                        if (resource >= building1.ConsumptionPerTurn)
                        {
                            resource -= building1.ConsumptionPerTurn;
                            resource += building1.OutputPerTurn;
                            Commoditites[building1.ResourceConsumed] = resource;                            
                        }
                    }
                    else if (building1.CommodityRequired != null)
                    {
                        if (Ground.CommoditiesPresent.Contains(building1.CommodityRequired))
                        {
                            foreach (Building building2 in BuildingList)
                            {
                                if (building2.IsCommodity && building2.Name == building1.CommodityRequired)
                                {
                                    Commoditites[building1.ResourceCreated] += building1.OutputPerTurn;                                    
                                }
                            }
                        }
                    }
                    else
                    {
                        Commoditites[building1.ResourceCreated] += building1.OutputPerTurn;                       
                    }
                }
            }
        }
    }
}