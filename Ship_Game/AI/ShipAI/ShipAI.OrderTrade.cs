using Microsoft.Xna.Framework;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies.AI;
using System;
using System.Linq;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        public Planet start;
        public Planet end;
        public Goods FoodOrProd;

        public bool NoGoods      => FoodOrProd == Goods.None;
        public bool IsFood       => FoodOrProd == Goods.Food;
        public bool IsProd       => FoodOrProd == Goods.Production;
        public bool IsPassengers => FoodOrProd == Goods.Colonists;

        public void SetTradeType(string savedString)
        {
            switch (savedString)
            {
                case "Food": FoodOrProd = Goods.Food; break;
                case "Prod": FoodOrProd = Goods.Production; break;
                case "Pass": FoodOrProd = Goods.Colonists; break;
                default:     FoodOrProd = Goods.None; break;
            }
        }

        public string GetTradeTypeString()
        {
            switch (FoodOrProd)
            {
                default:
                case Goods.None:       return "";
                case Goods.Food:       return "Food";
                case Goods.Production: return "Prod";
                case Goods.Colonists:  return "Pass";
            }
        }

        private void DropOffGoods()
        {
            if (end != null)
            {
                Owner.loyalty.TaxGoodsIfMercantile(Owner.CargoSpaceUsed);
                end.FoodHere += Owner.UnloadFood(end.Storage.Max - end.FoodHere);
                end.ProdHere += Owner.UnloadProduction(end.Storage.Max - end.ProdHere);
                end = null;
            }
            start = null;
            OrderQueue.RemoveFirst();
            OrderTrade(5f);
        }

        private void DropoffPassengers()
        {
            if (end == null)
            {
                OrderQueue.RemoveFirst();
                OrderTransportPassengers(0.1f);
                return;
            }

            end.Population += Owner.UnloadColonists(end.MaxPopulation - end.Population);

            OrderQueue.RemoveFirst();
            start = null;
            end = null;
            OrderTransportPassengers(5f);
        }

        public void OrderTrade(float elapsedTime)
        {
            //trade timer is sent but uses arbitrary timer just to delay the routine.
            Owner.TradeTimer -= elapsedTime;
            if (Owner.TradeTimer > 0f)
                return;

            PlayerManualTrade();

            if (Owner.GetColonists() > 0.0f)
                return;
            if (Owner.loyalty.TradeBlocked)
            {
                Owner.TradeTimer = 5;
                return;
            }
            if (UpdateTradeStatus() || !IsReadyForTrade)
                return;

            if (NoGoods)
            {
                end   = null;
                start = null;
                return;
            }

            // @note Cybernetic factions never touch Food trade. Filthy Opteris are disgusted by protein-bugs. Ironic.
            if (Owner.loyalty.NonCybernetic && IsFood && (end == null || Owner.GetFood() > 0))
            {
                if (DeliverShipment(Goods.Food))
                {
                    FoodOrProd          = Goods.Food;
                    Owner.TradingFood   = true;
                    Owner.TradingProd   = false;
                }
                else
                {
                    FoodOrProd        = Goods.None;
                    Owner.TradingFood = false;
                    Owner.TradingProd = false;
                }
            }

            if (IsProd && (end == null || Owner.GetProduction() >0))
            {
                 if (DeliverShipment(Goods.Production))
                {

                    FoodOrProd        = Goods.Production;
                    Owner.TradingProd = true;
                    Owner.TradingFood = false;
                }
                else
                {
                    FoodOrProd        = Goods.None;
                    Owner.TradingFood = false;
                    Owner.TradingProd = false;
                }
            }
            Goods trading = Owner.TradingFood ? Goods.Food : Goods.Production;

            if (start != null && end != null && start != end)
            {
                OrderMoveTowardsPosition(start.Center + RandomMath.RandomDirection() * 500f, Vectors.Up, true, start);
                AddShipGoal(GoodToPlan.Pickup(trading));
            }
            else if(end == null || Owner.GetCargo().Good != trading)
            {
                OrderQueue.Clear();
                FoodOrProd        = Goods.None;
                Owner.TradingFood = false;
                Owner.TradingProd = false;
                AwaitClosest      = start ?? Owner.loyalty.FindNearestRallyPoint(Owner.Center); //?? end
                start             = null;
                end               = null;
                if (Owner.CargoSpaceUsed > 0)
                    Owner.ClearCargo();
            }
            State            = AIState.SystemTrader;
            Owner.TradeTimer = 5f;
            end?.TradeAI.AddTrade(Owner);
            start?.TradeAI.AddTrade(Owner);
        }

        void PlayerManualTrade()
        {
            if (!Owner.loyalty.isPlayer || Owner.loyalty.AutoFreighters || start != null || end != null) return;
            State = AIState.SystemTrader;
            if (start != null || end != null) return;
            if (Owner.TransportingFood)
            {
                if (Owner.TransportingProduction)
                {
                    int rnd = RandomMath.IntBetween(1, 100);
                    if (rnd > 50)
                    {
                        FoodOrProd = Goods.Production;
                        return;
                    }
                }
                FoodOrProd = Goods.Food;
                return;
            }
            FoodOrProd = Goods.Production;
        }

        private TradeAI.TradeRoute[] GetTradeRoutes(Goods good, Planet[] tradePlanets)
        {
            var routes = new TradeAI.TradeRoute[tradePlanets.Length];
            for (int i = 0; i < tradePlanets.Length; i++)
            {
                routes[i] = tradePlanets[i].TradeAI.GetTradeRoute(good, Owner);
            }
            return routes;
        }

        private bool DeliverShipment(Goods good)
        {
            Planet[] planets = GetTradePlanets(good, Planet.GoodState.IMPORT);
            if (planets.Length <= 0)
                return false;
            TradeAI.TradeRoute[] tradeRoutes = GetTradeRoutes(good, planets);
            if (tradeRoutes.Length == 0) return false;
            tradeRoutes.Sort(tr => tr.Eta);
            var route = tradeRoutes[0];
            if (route.End == null)
                return false;
            end = route.End;

            if (Owner.GetCargo(good) <= 0)
                start = route.Start;

            WayPoints.Clear();
            OrderQueue.Clear();
            OrderMoveTowardsPosition(end.Center, Vectors.Up, true, end);
            AddShipGoal(GoodToPlan.DropOff(good));
            State = AIState.SystemTrader;
            return end != null;
        }

        private Planet[] GetTradePlanets(Goods good, Planet.GoodState goodState)
        {
            var planets = new Array<Planet>();
            foreach (Planet planet in Owner.loyalty.GetPlanets())
            {
                if (planet.ParentSystem.ShipList.Any(s => s.GetStrength() >0 && Owner.loyalty.IsEmpireAttackable(s.loyalty, s))) continue;

                if (planet.GetGoodState(good) != goodState) continue;
                if (goodState == Planet.GoodState.IMPORT && good != Goods.Colonists)
                {
                    if (planet.ImportPriority() != good) continue;
                }

                if (InsideAreaOfOperation(planet))
                    planets.Add(planet);
            }
            return planets.ToArray();
        }


        bool IsReadyForTrade => Math.Abs(Owner.CargoSpaceMax) > 0 
                            && State != AIState.Flee
                            && !Owner.isConstructor && !Owner.isColonyShip
                            && Owner.DesignRole == ShipData.RoleName.freighter;

        bool UpdateTradeStatus()
        {
            if (start == null || end == null) 
                return false;

            Owner.TradeTimer = 5f;
            if (OrderQueue.NotEmpty) return true;
            if (Owner.GetFood() > 0f || Owner.GetProduction() > 0f)
            {
                OrderMoveTowardsPosition(end.Center, Vectors.Up, true, end);
                AddShipGoal(Plan.DropOffGoods);
                State = AIState.SystemTrader;
                return true;
            }
            if (IsPassengers || NoGoods)
            {
                Owner.TradingFood = false;
                Owner.TradingProd = false;
                end   = null;
                start = null;
                FoodOrProd = Goods.None;
                return false;
            }
            OrderMoveTowardsPosition(start.Center, Vectors.Up, true, start);

            AddShipGoal(Plan.PickupGoods);
            State = AIState.SystemTrader;
            return true;
        }

        bool ShouldSuspendTradeDueToCombat()
        {
            return Owner.loyalty.GetOwnedSystems().All(combat => combat.combatTimer > 0);
        }

        public void OrderTransportPassengers(float elapsedTime)
        {
            Owner.TradeTimer -= elapsedTime;
            if ( Owner.CargoSpaceMax <= 0f || State == AIState.Flee || Owner.isConstructor) //Owner.TradeTimer > 0f ||
                return;

            if (ShouldSuspendTradeDueToCombat())
            {
                Owner.TradeTimer = 5f;
                return;
            }

            if (DeliverShipment(Goods.Colonists))
            {
                State             = AIState.PassengerTransport;
                Owner.TradingProd = false;
                Owner.TradingFood = false;
                FoodOrProd        = Goods.Colonists;
            }
            else
            {
                Owner.TradingProd = false;
                Owner.TradingFood = false;
                FoodOrProd        = Goods.None;
            }

            // go pick them up!
            if (start != null && end != null && start != end)
            {
                OrderMoveTowardsPosition(start.Center.RandomOffset(500f), Vectors.Up, true, start);
                AddShipGoal(GoodToPlan.Pickup(FoodOrProd));
            }
            else if (end == null || Owner.GetCargo().Good != Goods.Colonists)
            {
                AwaitClosest = start ?? end ?? Owner.loyalty.FindNearestRallyPoint(Owner.Center);
                start        = null;
                end          = null;
                FoodOrProd   = Goods.None;
                OrderQueue.Clear();
                if (Owner.CargoSpaceUsed > 0)
                    Owner.ClearCargo();
            }
            Owner.TradeTimer = 5f;
            State            = AIState.PassengerTransport;
            FoodOrProd       = Goods.Colonists;
            end?.TradeAI.AddTrade(Owner);
            start?.TradeAI.AddTrade(Owner);
        }

        public void OrderTroopToBoardShip(Ship s)
        {
            HasPriorityOrder = true;
            EscortTarget     = s;
            OrderQueue.Clear();
            AddShipGoal(Plan.BoardShip);
        }

        public void OrderTroopToShip(Ship s)
        {
            EscortTarget = s;
            OrderQueue.Clear();
            AddShipGoal(Plan.TroopToShip);
        }

        private void PickupGoods()
        {
            if (start == null)
            {
                OrderTrade(0.1f);
                return;
            }

            if (IsFood)
            {
                start.ProdHere   += Owner.UnloadProduction();
                start.Population += Owner.UnloadColonists();

                float maxFoodLoad = start.FoodHere.Clamped(0f, start.Storage.Max * 0.10f);
                start.FoodHere   -= Owner.LoadFood(maxFoodLoad);

                OrderQueue.RemoveFirst();
                OrderMoveTowardsPosition(end.Center + UniverseRandom.RandomDirection() * 500f, Vectors.Up, true, end);
                AddShipGoal(Plan.DropOffGoods);
            }
            else if (IsProd)
            {
                start.FoodHere   += Owner.UnloadFood();
                start.Population += Owner.UnloadColonists();

                float maxProdLoad = start.ProdHere.Clamped(0f, start.Storage.Max * 10f);
                start.ProdHere -= Owner.LoadProduction(maxProdLoad);

                OrderQueue.RemoveFirst();
                OrderMoveTowardsPosition(end.Center + UniverseRandom.RandomDirection() * 500f, Vectors.Up, true, end);
                AddShipGoal(Plan.DropOffGoods);
            }
            else
            {
                OrderTrade(0.1f);
            }
            State = AIState.SystemTrader;
        }

        private void PickupPassengers()
        {
            start.ProdHere += Owner.UnloadProduction();
            start.FoodHere += Owner.UnloadFood();

            // load everyone we can :P
            start.Population -= Owner.LoadColonists(start.Population * 0.2f);

            OrderQueue.RemoveFirst();
            OrderMoveTowardsPosition(end.Center, Vectors.Up, true, end);
            AddShipGoal(Plan.DropoffPassengers);
            State = AIState.PassengerTransport;
        }
    }
}