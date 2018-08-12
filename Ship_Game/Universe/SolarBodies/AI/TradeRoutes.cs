using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Universe.SolarBodies.AI
{
    public struct TradeAI
    {
        public void DebugText()
        {
            if (Empire.Universe?.DebugWin == null) return;
            if (IncomingFreight == null) return;
            Array<string> lines = new Array<string>();
            foreach(var kv in IncomingFreight)
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
            ImportTargets = new Array<Planet>().FilterBy(p => p.IsExporting());
            ImportTargets.Sort(p => p.Center.SqDist(planet.Center));

        }

        public float AvgTradingFood {get; private set;}
        public float AvgTradingProduction { get; private set; }
        public float AvgTradingColonists { get; private set; }

        public bool AddTrade(Ship ship)
        {            
            if (ship.AI.end != TradePlanet && ship.AI.start != TradePlanet) return false;
            int eta;
            ShipAI.RouteType Type;
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
        private void AddToIncomingFreight(Ship ship, int eta) => AddToFreight(ship, eta, IncomingFreight);
        private void AddToOutGoingFreight(Ship ship, int eta) => AddToFreight(ship, eta, OutGoingFreight);
        private void AddToFreight(Ship ship, int eta, Dictionary<int, Entry> freight)
        {
            Cargo cargo = ship.GetCargo();
            cargo.Amount = cargo.Amount > 0 ? cargo.Amount : ship.CargoSpaceMax;

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
        
        public float PredictedGoodOutgoing(Goods good)
        {
            var goods = GetGoodsEtaDict(good,ShipAI.Plan.PickupGoods);
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
                return exporter.TradeAI.PredictedGoodOutgoing(good) < exporter.SbCommodities.GetGoodAmount(good);
            });

        }

    }
}