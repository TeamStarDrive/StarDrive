using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game.Universe.SolarBodies.AI
{
    public class TradeAI
    {


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
            IncomingFreight = new Map<int, Entry>();
            OutGoingFreight = new Map<int, Entry>();
            TradePlanet = planet;
            AvgTradingFood = 0;
            AvgTradingProduction = 0;
            AvgTradingColonists = 0;
            CreateSortedGoodSources(planet);
        }
        public void ClearHistory()
        {
            IncomingFreight.Clear();
            OutGoingFreight.Clear();
            CreateSortedGoodSources(TradePlanet);

        }
        public void CreateSortedGoodSources(Planet planet)
        {
            if (planet.Owner == null)
            {
                ImportTargets = Empty<Planet>.Array;
                return;
            }
            ImportTargets = planet.Owner.GetPlanets().Filter(p => p.IsExporting());
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
            Cargo cargo  = ship.GetCargo();
            Goods type   = Goods.Colonists;
            if (ship.TradingFood || ship.TradingProd)
                type = ship.TradingProd ? Goods.Production : Goods.Food;
            cargo.Good   = type;
            cargo.Amount = cargo.Amount <=0 ? ship.CargoSpaceMax : cargo.Amount;
            freight      = plan == ShipAI.Plan.PickupGoods ? OutGoingFreight : IncomingFreight;

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
            var goods       = GetGoodsEtaDict(good);
            if (goods.Count == 0) return 0;
            float time      = goods.Keys.Sum();
            float amount    = goods.Values.Sum();
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
            var goods       = GetGoodsEtaDict(good,route);
            if (goods.Count == 0) return 0;
            float time      = goods.Keys.Sum();
            float amount    = goods.Values.Sum();
            if (amount <= 0 || time <= 0)
            {
                Log.Info("TradeTracking: avg trade amount was 0");
                return 0;
            }
            return amount;
        }
        public struct TradeRoute
        {
            public Planet End;
            public Planet Start;
            public int Eta;
        }

        private float GetMaxAmount(Goods good)
        {
            switch(good)
            {
                case Goods.Food:
                case Goods.Production:
                    return TradePlanet.MaxStorage - TradePlanet.GetGoodHere(good);
                case Goods.Colonists:
                    return TradePlanet.MaxPopulation - TradePlanet.Population;

            }
            return 0;
        }

        //wip
        public TradeRoute GetTradeRoute(Goods good, Ship ship)
        {
            float incoming = PredictedTradeFor(good, ShipAI.Plan.DropOffGoods);
            TradeRoute route = new TradeRoute { Eta = int.MaxValue };
            if (incoming >= GetMaxAmount(good)) return route;
            if (ship.loyalty != TradePlanet.Owner) return route;
            Planet[] potentialSources = ImportTargets.Filter(exporter =>
            {
                if (exporter.GetGoodState(good) != Planet.GoodState.EXPORT) return false;
                if (exporter == TradePlanet) return false;
                if (exporter.Owner != TradePlanet.Owner) return false;
                return exporter.TradeAI.PredictedTradeFor(good, ShipAI.Plan.PickupGoods)
                       < exporter.SbCommodities.GetGoodAmount(good);
            });
            if (potentialSources.Length == 0) return route;
            Planet startPlanet = potentialSources.FindMin(start =>
            {
                float etaToEnd = start.Center.SqDist(TradePlanet.Center);
                float etaToStart = ship.Center.SqDist(start.Center);
                return etaToEnd + etaToStart;
            });
            if (startPlanet == null) return route;

            float eta = TradePlanet.Center.Distance(startPlanet.Center) + ship.Center.Distance(startPlanet.Center);
            eta      /= Math.Max(ship.GetmaxFTLSpeed,1);
            route     = new TradeRoute
            {
                End   = TradePlanet,
                Start = startPlanet,
                Eta   = (int)Math.Max(1, eta)
            };

            return route;

        }



       //All of the below is debug information


        public DebugSummaryTotal DebugSummarizeIncomingFreight(Array<string> lines) =>
            DebugSummarizeFreight(lines, IncomingFreight);
        public DebugSummaryTotal DebugSummarizeOutgoingFreight(Array<string> lines) =>
            DebugSummarizeFreight(lines, OutGoingFreight);
        public DebugSummaryTotal DebugSummarizeFreight(Array<string> lines, Dictionary<int, Entry> freight)
        {
            if (freight == null) return new DebugSummaryTotal();
            int foodT2 = 0;
            int prodT2 = 0;
            int coltT2 = 0;
            int totalT2 = 0;
            foreach (var kv in freight.OrderBy(k => k.Key))
            {
                int foodSum = (int)kv.Value.Cargo.Sum(t => t.Good == Goods.Food ? t.Amount : 0);
                int prodt   = (int)kv.Value.Cargo.Sum(t => t.Good == Goods.Production ? t.Amount : 0);
                int colt    = (int)kv.Value.Cargo.Sum(t => t.Good == Goods.Colonists ? t.Amount : 0);
                int totalC  = kv.Value.Cargo.Count;
                lines.Add($"ETA: {kv.Key} - food: {foodSum} - prod: {prodt} - Colo: {colt}  T:{totalC}");

                foodT2  += foodSum;
                prodT2  += prodt;
                coltT2  += colt;
                totalT2 += totalC;
            }
            return new DebugSummaryTotal
            {
                Food      = foodT2,
                Prod      = prodT2,
                Colonists = coltT2,
                Total     = totalT2
            };

        }

        public struct DebugSummaryTotal
        {
            public int Food;
            public int Prod;
            public int Colonists;
            public int Total;
        }

        public DebugTextBlock DebugFormatTradeBlock(string header)
        {
            float foodHere = TradePlanet.FoodHere;
            float prodHere = TradePlanet.ProductionHere;
            float foodStorPerc = 100 * foodHere / TradePlanet.MaxStorage;
            float prodStorPerc = 100 * prodHere / TradePlanet.MaxStorage;
            string food = $"{(int)foodHere}(%{foodStorPerc:00.0}) {TradePlanet.FS}";
            string prod = $"{(int)prodHere}(%{prodStorPerc:00.0}) {TradePlanet.PS}";
            DebugTextBlock block = new DebugTextBlock { Header = header };
            block.AddLine($"FoodHere: {food} ", Color.White);
            block.AddLine($"ProdHere: {prod}");
            return block;
        }

        public Array<DebugTextBlock> DebugText()
        {
            if (IncomingFreight == null) return null;
            var blocks = new Array<DebugTextBlock>();

            var lines = new Array<string>();
            DebugSummarizeIncomingFreight(lines);
            var block = DebugFormatTradeBlock($"{TradePlanet.Name} Incoming Cargo");
            block.AddRange(lines);
            blocks.Add(block);

            lines.Clear();
            block = DebugFormatTradeBlock($"{TradePlanet.Name} Outgoing Cargo");
            var totals = DebugSummarizeOutgoingFreight(lines);
            block.AddRange(lines);
            block.AddLine($" food: {totals.Food} prod: {totals.Prod} Colo: {totals.Colonists}  T:{totals.Total}");
            blocks.Add(block);

            block = new DebugTextBlock { Header = "Suppliers" };
            foreach (var p in ImportTargets)
                block.AddLine($"{p.Name}: F:{(int)p.FoodHere} P:{(int)p.ProductionHere}");

            blocks.Add(block);
            return blocks;
        }
    }
}