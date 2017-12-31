using System;
using System.Collections.Generic;

namespace Ship_Game.Universe.SolarBodies
{
    public class CommodititesManager
    {
        private readonly Planet Ground;

        private Array<PlanetGridSquare> TilesList => Ground.TilesList;
        private Empire Owner => Ground.Owner;
        private BatchRemovalCollection<Troop> TroopsHere => Ground.TroopsHere;
        private Array<Building> BuildingList => Ground.BuildingList;
        private BatchRemovalCollection<Combat> ActiveCombats => Ground.ActiveCombats;
        private SolarSystem ParentSystem => Ground.ParentSystem;        
        private Map<string, float> Commoditites = new Map<string, float>(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyDictionary<string, float> ResourcesDictionary => Commoditites;

        public CommodititesManager (Planet planet)
        {
            Ground = planet;
        }

        public void AddGood(string goodId, float amount)
        {
            float max = float.MaxValue;
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
                        max = Ground.MaxPopulation;
                        break;
                }
                default:
                    break;

            }
            amount = Math.Max(0, amount);
            amount = Math.Min(amount, max);
            Commoditites[goodId] = amount;
        }
        public int GetGoodAmount(string goodId)
        {
            if (Commoditites.TryGetValue(goodId, out float commodity)) return (int)commodity;
            return 0;
        }
        public float CalculateConsumption()
        {
            float unfed = 0.0f;
            if (Owner.data.Traits.Cybernetic > 0)
            {
                Ground.FoodHere = 0.0f;
                Ground.NetProductionPerTurn -= Ground.Consumption;

                if (Ground.NetProductionPerTurn < 0f)
                    Ground.ProductionHere += Ground.NetProductionPerTurn;

                if (Ground.ProductionHere > Ground.MaxStorage)
                {
                    unfed = 0.0f;
                    Ground.ProductionHere = Ground.MaxStorage;
                }
                else if (Ground.ProductionHere < 0)
                {

                    unfed = Ground.ProductionHere;
                    Ground.ProductionHere = 0.0f;
                }
            }
            else
            {
                Ground.NetFoodPerTurn -= Ground.Consumption;
                Ground.FoodHere += Ground.NetFoodPerTurn;
                if (Ground.FoodHere > Ground.MaxStorage)
                {
                    unfed = 0.0f;
                    Ground.FoodHere = Ground.MaxStorage;
                }
                else if (Ground.FoodHere < 0)
                {
                    unfed = Ground.FoodHere;
                    Ground.FoodHere = 0.0f;
                }
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