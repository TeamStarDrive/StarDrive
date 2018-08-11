using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Universe.SolarBodies.AI
{
    public struct TradeTracking
    {
        public void DebugText()
        {
            if (Empire.Universe?.DebugWin == null) return;
            if (TimeAndCargo == null) return;
            Array<string> lines = new Array<string>();
            foreach(var kv in TimeAndCargo)
            {
                string line = ($"Time: {kv.Key.ToString()} Amount: {kv.Value.Cargo.Sum(a => a.Amount)}");
                Debug.DebugInfoScreen.LogSelected(TradePlanet, line, Debug.DebugModes.Trade);
            }            
        }


        public struct Entry
        {
            public Array<Cargo> Cargo;
            public void AddCargo(Cargo cargo) => Cargo.Add(cargo);
        }                                

        private readonly Planet TradePlanet;
        public ShipAI.RouteType Type;
        private Dictionary<int, Entry> TimeAndCargo;

        public TradeTracking(Planet planet, ShipAI.RouteType routeType)
        {
            TimeAndCargo = new Dictionary<int, Entry>();
            TradePlanet = planet;
            Type = routeType;
            AvgTradingFood = 0;
            AvgTradingProduction = 0;
            AvgTradingColonists = 0;
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
                    switch (Type)
                    {                        
                        case ShipAI.RouteType.Delivery:
                            if (ship.AI.end != TradePlanet) return false;
                            eta = (int)ship.AI.TimeToTarget(ship.AI.end);
                            eta += (int)ship.AI.TimeToTarget(ship.AI.start);
                            break;
                        case ShipAI.RouteType.Pickup:
                            eta = (int)ship.AI.TimeToTarget(ship.AI.start);
                            break;
                        default:
                            return false;
                    }
                    break;

                case ShipAI.Plan.DropOffGoods:                    
                    if (ship.AI.end != TradePlanet) return false;                    
                    eta = (int)ship.AI.TimeToTarget(ship.AI.end);
                    break;
                default:
                    return false;
            }                                  

            Cargo cargo = ship.GetCargo();
            cargo.Amount = cargo.Amount > 0 ? cargo.Amount : ship.CargoSpaceMax;

            TimeAndCargo.TryGetValue(eta, out Entry entry);
            entry.Cargo = entry.Cargo ?? new Array<Cargo>();
            entry.AddCargo(cargo);
            TimeAndCargo[eta] = entry;
            return true;
        }
        public Dictionary<int, float> GetGoodsEta(Goods good)
        {
            var data = new Dictionary<int, float>();
            foreach (var entry in TimeAndCargo)
            {
                float amount = 0;
                foreach (var cargo in entry.Value.Cargo)
                    if (cargo.Good == good) amount += cargo.Amount;
                if (amount <= 0) continue;
                data[entry.Key] = amount;
            }
            return data;
        }
        public float GetAverageTradeFor(Goods good)
        {
            var goods = GetGoodsEta(good);
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
        

    }
}