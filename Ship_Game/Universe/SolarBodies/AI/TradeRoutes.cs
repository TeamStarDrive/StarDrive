using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Ships;

namespace Ship_Game.Universe.SolarBodies.AI
{
    public struct TradeAI
    {     
        public Array<DebugTextBlock> DebugText()
        {

            if (IncomingFreight == null) return null;
            Array<DebugTextBlock> blocks = new Array<DebugTextBlock>();
            string food = $"F:{TradePlanet.FS} {(int)TradePlanet.FoodHere} %{(100 * TradePlanet.FoodHere / TradePlanet.MaxStorage).ToString("0.#")}";
            string prod = $"P:{TradePlanet.PS} {(int)TradePlanet.ProductionHere} %{(100 * TradePlanet.ProductionHere / TradePlanet.MaxStorage).ToString("0.#")}";
            Array<string> lines = new Array<string>{$"Incoming {food} {prod} " };
            foreach(var kv in IncomingFreight.OrderBy(k=> k.Key))
            {                
                int foodt = (int)kv.Value.Cargo.Sum(t => t.Good == Goods.Food ? t.Amount : 0);
                int prodt = (int)kv.Value.Cargo.Sum(t => t.Good == Goods.Production ? t.Amount : 0);
                int colt = (int)kv.Value.Cargo.Sum(t => t.Good == Goods.Colonists ? t.Amount : 0);
                int totalC = (int)kv.Value.Cargo.Count;
                lines.Add($"Time: {kv.Key} food: {foodt} prod: {prodt} Colo: {colt}  T:{totalC}");
            }
            
            blocks.Add(new DebugTextBlock{Lines = lines});
            food = $"F:{TradePlanet.FS} {(int)TradePlanet.FoodHere} %{(100 * TradePlanet.FoodHere / TradePlanet.MaxStorage).ToString("0.#")}";
            prod = $"P:{TradePlanet.PS} {(int)TradePlanet.ProductionHere} %{(100 * TradePlanet.ProductionHere / TradePlanet.MaxStorage).ToString("0.#")}";
            
            lines = new Array<string>{ $"OutGoing {food} {prod} " };
            int foodT2 = 0;
            int prodT2 = 0;
            int coltT2 = 0;
            int totalT2 = 0;
            foreach (var kv in OutGoingFreight.OrderBy(k => k.Key))
            {
                
                int foodt = (int)kv.Value.Cargo.Sum(t => t.Good == Goods.Food ? t.Amount : 0);
                int prodt = (int)kv.Value.Cargo.Sum(t => t.Good == Goods.Production ? t.Amount : 0);
                int colt = (int)kv.Value.Cargo.Sum(t => t.Good == Goods.Colonists ? t.Amount : 0);
                int totalC = (int)kv.Value.Cargo.Count;
                lines.Add($"Time: {kv.Key} food: {foodt} prod: {prodt} Colo: {colt}  T:{totalC}");
                foodT2 += foodt;
                prodT2 += prodT2;
                coltT2 += colt;
                totalT2 += totalC;
            }
            lines.Add($" food: {foodT2} prod: {prodT2} Colo: {coltT2}  T:{totalT2}");
            blocks.Add(new DebugTextBlock { Lines = lines });
            lines = new Array<string>{"Suppliers"};
            foreach (var p in ImportTargets)
            {
                lines.Add($"{p.Name}: F:{(int)p.FoodHere} P:{(int)p.ProductionHere}"); 
            }
            blocks.Add(new DebugTextBlock { Lines = lines });
            return blocks;
        }

        public override string ToString()
        {
            return $"{TradePlanet.Name} - IF:{IncomingFreight.Count} OF:{OutGoingFreight.Count}";
        }
        public struct Entry
        {
            public Array<Cargo> Cargo;
            public void AddCargo(Cargo cargo) => Cargo.Add(cargo);
        }                                

        private readonly Planet TradePlanet;
        
        private Dictionary<int, Entry> IncomingFreight;
        private Dictionary<int, Entry> OutGoingFreight;
        private Planet[] ImportTargets;

        public TradeAI(Planet planet)
        {
            IncomingFreight = new Dictionary<int, Entry>();
            OutGoingFreight = new Dictionary<int, Entry>();
            TradePlanet = planet;
            AvgTradingFood = 0;
            AvgTradingProduction = 0;
            AvgTradingColonists = 0;
            ImportTargets = planet.Owner.GetPlanets().FilterBy(p => p.IsExporting());
            ImportTargets.Sort(p => p.Center.SqDist(planet.Center));

        }

        public float AvgTradingFood {get; private set;}
        public float AvgTradingProduction { get; private set; }
        public float AvgTradingColonists { get; private set; }

        public bool AddTrade(Ship ship)
        {            
            if (ship.AI.end != TradePlanet && ship.AI.start != TradePlanet) return false;
            int eta;            
            switch (ship.AI.OrderQueue.PeekLast.Plan)
            {
                case ShipAI.Plan.PickupGoods:
                case ShipAI.Plan.PickupPassengers:
                    if (ship.AI.start == TradePlanet)
                    {
                        eta = (int)ship.AI.TimeToTarget(ship.AI.start);
                        AddToOutGoingFreight(ship, eta);
                        return true;
                    }

                    eta = (int) ship.AI.TimeToTarget(ship.AI.end);
                    eta += (int) ship.AI.TimeToTarget(ship.AI.start);
                    AddToIncomingFreight(ship, eta);

                    break;
                case ShipAI.Plan.DropOffGoods:
                case ShipAI.Plan.DropoffPassengers:
                    if (ship.AI.end != TradePlanet) return false;                    
                    eta = (int)ship.AI.TimeToTarget(ship.AI.end);
                    AddToIncomingFreight(ship, eta);
                    break;
                default:
                    return false;
            }                                  
            return true;
        }
        private void AddToIncomingFreight(Ship ship, int eta)=> AddToFreight(ship, eta, ShipAI.Plan.DropOffGoods);
        private void AddToOutGoingFreight(Ship ship, int eta)=> AddToFreight(ship, eta, ShipAI.Plan.PickupGoods);
        private void AddToFreight(Ship ship, int eta, ShipAI.Plan plan)
        {
            Dictionary<int, Entry> freight;
            Cargo cargo = ship.GetCargo();
            Goods type = Goods.Colonists;            
            if (ship.TradingFood || ship.TradingProd)
                type = ship.TradingProd ? Goods.Production : Goods.Food;
            cargo.Good = type;
            cargo.Amount = cargo.Amount <=0 ? ship.CargoSpaceMax : cargo.Amount;
            freight = plan == ShipAI.Plan.PickupGoods ? OutGoingFreight : IncomingFreight;
                        
            freight.TryGetValue(eta, out Entry entry);
            entry.Cargo = entry.Cargo ?? new Array<Cargo>();
            entry.AddCargo(cargo);
            freight[eta] = entry;
        }

        public Dictionary<int, float> GetGoodsEtaDict(Goods good, ShipAI.Plan type = ShipAI.Plan.DropOffGoods)
        {
            var data = new Dictionary<int, float>();
            Dictionary<int, Entry> sourceDictionary = 
                type == ShipAI.Plan.PickupGoods ? OutGoingFreight : IncomingFreight;

            foreach (var entry in sourceDictionary)
            {
                float amount = 0;
                foreach (Cargo cargo in entry.Value.Cargo)
                    if (cargo.Good == good) amount += cargo.Amount;
                if (amount <= 0) continue;
                data[entry.Key] = amount;
            }
            return data;
        }
        public float GetAverageTradeFor(Goods good)
        {
            var goods = GetGoodsEtaDict(good);
            if (goods.Count == 0) return 0;
            float time = goods.Keys.Sum();            
            float amount = goods.Values.Sum();
            if (amount <= 0 || time <= 0)
            {
                Log.Info("TradeTracking: avg trade amount was 0");
                return 0;
            }
            return amount / time;
        }

        public void ComputeAverages()
        {
            AvgTradingColonists  = GetAverageTradeFor(Goods.Colonists);
            AvgTradingFood       = GetAverageTradeFor(Goods.Food);
            AvgTradingProduction = GetAverageTradeFor(Goods.Production);
        }
        
        public bool NeedsMore(Goods good)
        {
            float incoming = PredictedTradeFor(good, ShipAI.Plan.DropOffGoods);
            return incoming < TradePlanet.MaxStorage;
        }

        public float PredictedTradeFor(Goods good, ShipAI.Plan route)
        {
            var goods = GetGoodsEtaDict(good,route);
            if (goods.Count == 0) return 0;
            float time = goods.Keys.Sum();
            float amount = goods.Values.Sum();
            if (amount <= 0 || time <= 0)
            {
                Log.Info("TradeTracking: avg trade amount was 0");
                return 0;
            }
            return amount;
        }

        public Planet GetNearestSupplierFor(Goods good)
        {            
            return ImportTargets.Find(exporter =>
            {                
                if (exporter.GetGoodState(good) != Planet.GoodState.EXPORT) return false;
                return exporter.TradeAI.PredictedTradeFor(good, ShipAI.Plan.PickupGoods) 
                       < exporter.SbCommodities.GetGoodAmount(good);
            });

        }

    }
}