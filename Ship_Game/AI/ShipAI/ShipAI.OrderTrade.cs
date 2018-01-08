using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI {
    public sealed partial class ShipAI
    {
        public Planet start;
        public Planet end;
        public string FoodOrProd;

        private void DropoffGoods()
        {
            if (end != null)
            {
                if (Owner.loyalty.data.Traits.Mercantile > 0f)
                    Owner.loyalty.AddTradeMoney(Owner.CargoSpaceUsed * Owner.loyalty.data.Traits.Mercantile);

                end.FoodHere += Owner.UnloadFood(end.MaxStorage - end.FoodHere);
                end.ProductionHere += Owner.UnloadProduction(end.MaxStorage - end.ProductionHere);
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

            float maxPopulation = end.MaxPopulation + end.MaxPopBonus;
            end.Population += Owner.UnloadColonists(maxPopulation - end.Population);

            OrderQueue.RemoveFirst();
            start = null;
            end = null;
            OrderTransportPassengers(5f);
        }

        private float TradeSort(Ship ship, Planet PlanetCheck, string ResourceType, float cargoCount, bool Delivery)
        {
            /*here I am trying to predict the planets need versus the ships speed.
             * I am returning a weighted value that is based on this but primarily the returned value is the time it takes the freighter to get to the target in a straight line
             * 
             * 
             */
            //cargoCount = cargoCount > PlanetCheck.MAX_STORAGE ? PlanetCheck.MAX_STORAGE : cargoCount;
            float resourceRecharge = 0;
            float resourceAmount = 0;
            if (ResourceType == "Food")
            {
                resourceRecharge = PlanetCheck.NetFoodPerTurn;
                resourceAmount = PlanetCheck.FoodHere;
            }
            else if (ResourceType == "Production")
            {
                resourceRecharge = PlanetCheck.NetProductionPerTurn;
                resourceAmount = PlanetCheck.ProductionHere;
            }
            float timeTotarget = ship.AI.TimeToTarget(PlanetCheck);
            float Effeciency = resourceRecharge * timeTotarget;

            // return PlanetCheck.MAX_STORAGE / (PlanetCheck.MAX_STORAGE -(Effeciency + resourceAmount));

            if (Delivery)
            {
                bool badCargo = Effeciency + resourceAmount > PlanetCheck.MaxStorage;             
                if (!badCargo)
                    return timeTotarget * (badCargo
                               ? PlanetCheck.MaxStorage / (Effeciency + resourceAmount)
                               : 1);
            }
            else
            {
                Effeciency = PlanetCheck.MaxStorage * .5f < ship.CargoSpaceMax
                    ? resourceAmount + Effeciency < ship.CargoSpaceMax * .5f
                        ? ship.CargoSpaceMax * .5f / (resourceAmount + Effeciency)
                        : 1
                    : 1;             
                return timeTotarget * Effeciency; 
            }
            return timeTotarget + UniverseScreen.UniverseSize;
        }

        public void OrderTrade(float elapsedTime)
        {
            //trade timer is sent but uses arbitrary timer just to delay the routine.

            Owner.TradeTimer -= elapsedTime;
            if (Owner.TradeTimer > 0f)
                return;


            PlayerManualTrade();

            if (Owner.GetColonists() > 0.0f) return;
          
            if (Owner.loyalty.TradeBlocked)
            {
                Owner.TradeTimer = 5;
                return;
            }
            if (IsAlreadyTrading()) return;
            if (!IsReadyForTrade) return;

            if (FoodOrProd == "")
            {
                end = null;
                start = null;
                return;
            }

                               
           
            if (Owner.loyalty.data.Traits.Cybernetic < 1 && FoodOrProd == "Food" && (end == null || Owner.GetFood() > 0))
            {
                if ( DeliverShipment("Food"))
                {                    
                    FoodOrProd = "Food";
                    Owner.TradingFood = true;
                    Owner.TradingProd = false;
                    //return;
                }
                else
                {
                    FoodOrProd = "";
                    Owner.TradingFood = false;
                    Owner.TradingProd = false;
                }
            }
            
            if (FoodOrProd == "Prod" && end == null || Owner.GetProduction() >0)
            {
                if(DeliverShipment("Production"))
                {
                    
                    FoodOrProd = "Prod";
                    Owner.TradingProd = true;
                    Owner.TradingFood = false;
                    //return;
                }
                else
                {
                    FoodOrProd = "";
                    Owner.TradingFood = false;
                    Owner.TradingProd = false;
                }
            }
            GetShipment(Owner.TradingFood ? "Food" : "Production");

            if (start != null && end != null && start != end)
            {
                OrderMoveTowardsPosition(start.Center + RandomMath.RandomDirection() * 500f, 0f,
                    new Vector2(0f, -1f), true, start);

                AddShipGoal(Plan.PickupGoods, Vector2.Zero, 0f);
            }
            else
            {
                AwaitClosest = start ?? end ?? Owner.loyalty.FindNearestRallyPoint(Owner.Center);
                start = null;
                end = null;                
                if (Owner.CargoSpaceUsed > 0)
                    Owner.ClearCargo();
                State = AIState.SystemTrader;
                return;
            }
            State = AIState.SystemTrader;
            Owner.TradeTimer = 5f;
            if (FoodOrProd.IsEmpty())
                FoodOrProd = Owner.TradingFood ? "Food" : "Prod";
            //catch { }
        }

        private void PlayerManualTrade()
        {
            if (Owner.loyalty.isPlayer && !Owner.loyalty.AutoFreighters && start == null && end == null)
            {
                if (FoodOrProd == "")
                {
                    FoodOrProd = "Food";
                }
                if (Owner.CargoSpaceUsed < 1)
                    if (FoodOrProd == "Prod")
                    {
                        FoodOrProd = "Food";
                        Owner.TradingFood = true;
                    }
                    else
                    {
                        Owner.TradingProd = true;
                        FoodOrProd = "Prod";
                    }
            }
        }

        private void GetShipment(string goodType)
        {
            var prodOrFood = goodType.Substring(0, 4);

            if (start != null || end == null || (FoodOrProd != null && FoodOrProd != prodOrFood) ||
                (Owner.CargoSpaceUsed > 0 && !(Owner.CargoSpaceUsed / Owner.CargoSpaceMax < .2f))) return;

            var flag = false;
            var tradePlanets = GetTradePlanets(goodType, Planet.GoodState.EXPORT);
            var planets = tradePlanets.Planets;

            if (planets.Count <= 0) return;

            var sortPlanets = planets.OrderBy(PlanetCheck =>
            {
                return TradeSort(Owner, PlanetCheck, goodType, Owner.CargoSpaceMax, false);                        
            });
            foreach (Planet p in sortPlanets)
            {
                if (p.ParentSystem.CombatInSystem) continue;
                flag = false;
                float cargoSpaceMax = p.GetGoodHere(goodType);

                float mySpeed = TradeSort(Owner, p, goodType, Owner.CargoSpaceMax, false);
                     
                ShipGoal plan;
                using (Owner.loyalty.GetShips().AcquireReadLock())
                {
                    for (var k = 0; k < Owner.loyalty.GetShips().Count; k++)
                    {
                        Ship s = Owner.loyalty.GetShips()[k];
                        if (s != null &&
                            (s.shipData.Role == ShipData.RoleName.freighter || s.shipData.ShipCategory ==
                             ShipData.Category.Civilian) && s != Owner && !s.isConstructor)
                        {
                            plan = s.AI.OrderQueue.PeekLast;
                            if (plan != null && s.AI.State == AIState.SystemTrader && s.AI.start == p &&
                                plan.Plan == Plan.PickupGoods && s.AI.FoodOrProd == prodOrFood)
                            {
                                if (p.ImportPriority().ToString() != goodType)
                                {
                                    break;
                                }
                                float currenTrade = TradeSort(s, p, goodType, s.CargoSpaceMax, false);
                                if (currenTrade > 1000)
                                    continue;

                                float efficiency = Math.Abs(currenTrade - mySpeed);
                                efficiency = s.CargoSpaceMax - efficiency * p.GetNetGoodProd(goodType);
                                if (efficiency > 0)
                                    cargoSpaceMax = cargoSpaceMax - efficiency;
                            }

                            if (cargoSpaceMax <= 0 + p.MaxStorage * .1f) 
                            {
                                flag = true;
                                break;
                            }
                        }
                    }
                }
                if (!flag)
                {
                    start = p;
                    break;
                }
            }
        }

        private bool DeliverShipment(string goodType)
        {
            var planets = GetTradePlanets(goodType, Planet.GoodState.IMPORT);

            if (planets.Planets.Count <= 0) return false;
            var goodSwitch = goodType.Substring(0, 4);
            
            var cargoSpace = Owner.GetCargo(goodType);
            cargoSpace = cargoSpace > 0 ? cargoSpace : Owner.CargoSpaceMax;
            var sortPlanets = planets.Planets.OrderBy(planetCheck => TradeSort(Owner, planetCheck, goodType, cargoSpace, true));
            

            foreach (Planet p in sortPlanets)
            {
                if (p.ParentSystem.CombatInSystem) continue;
                var flag = false;
                float cargoSpaceMax = p.MaxStorage - p.GetGoodHere(goodType);
                var faster = true;
                float thisTradeStr = TradeSort(Owner, p, goodType, Owner.CargoSpaceMax, true);
                if (thisTradeStr >= UniverseScreen.UniverseSize && p.GetGoodHere(goodType) >= 0)
                    continue;
                if (goodType == "Food" && p.ImportPriority().ToString() != goodType)
                    continue;
                using (Owner.loyalty.GetShips().AcquireReadLock())
                {
                    for (var k = 0; k < Owner.loyalty.GetShips().Count; k++)
                    {
                        Ship s = Owner.loyalty.GetShips()[k];
                        if (s == null ||
                            (s.shipData.Role != ShipData.RoleName.freighter &&
                             s.shipData.ShipCategory != ShipData.Category.Civilian) || s == Owner ||
                            s.isConstructor) continue;
                        if (s.AI.State == AIState.SystemTrader && s.AI.end == p && s.AI.FoodOrProd == goodSwitch)
                        {                          
                            float currenTrade = TradeSort(s, p, goodType, s.CargoSpaceMax, true);
                            if (currenTrade < thisTradeStr)
                                faster = false;
                            if (currenTrade > UniverseData.UniverseWidth && !faster)
                            {
                                flag = true;
                                break;
                            }
                            cargoSpaceMax = cargoSpaceMax - s.CargoSpaceMax;
                        }

                        if (cargoSpaceMax <= 0f)
                        {
                            flag = true;
                            break;
                        }

                    }
                }
               
                if (!flag)
                {
                    end = p;
                    break;
                }
                //if (faster)
                //    potential = p;
            }
            if (end == null) return false;

            FoodOrProd = goodSwitch;
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }

            OrderQueue.Clear();
            //if (Owner.CargoSpaceFree / Owner.CargoSpaceMax > .25f) return true;
            OrderMoveTowardsPosition(end.Center, 0f, new Vector2(0f, -1f), true, end);
            AddShipGoal(Plan.DropOffGoods, Vector2.Zero, 0f);
            State = AIState.SystemTrader;
            return end != null;
        }

        private TradePlanets GetTradePlanets(string goodType, Planet.GoodState goodState)
        {
            var planets = new TradePlanets(true);            
            foreach (Planet planet in Owner.loyalty.GetPlanets())
                if (planet.ParentSystem.combatTimer <= 0)
                {
                    float distanceWeight = TradeSort(Owner, planet, goodType, Owner.CargoSpaceMax, false);

                    var exportWeight = planet.GetExportWeight(goodType);

                    planet.SetExportWeight(goodType, distanceWeight < exportWeight
                        ? distanceWeight
                        : exportWeight);

                    if (planet.GetGoodState(goodType) == goodState && InsideAreaOfOperation(planet))
                        planets.Planets.Add(planet);
                    else if (planet.MaxStorage - planet.GetGoodHere(goodType) > 0)
                        planets.SecondaryPlanets.Add(planet);
                }
            return planets;
        }


        private bool IsReadyForTrade => Math.Abs(Owner.CargoSpaceMax) > 0 && State != AIState.Flee && !Owner.isConstructor &&
                   !Owner.isColonyShip && Owner.DesignRole == ShipData.RoleName.freighter;

        private bool IsAlreadyTrading()
        {
            if (start == null || end == null) return false;
            Owner.TradeTimer = 5f;
            if (Owner.GetFood() > 0f || Owner.GetProduction() > 0f)
            {
                OrderMoveTowardsPosition(end.Center, 0f, new Vector2(0f, -1f), true, end);

                AddShipGoal(Plan.DropOffGoods, Vector2.Zero, 0f);

                State = AIState.SystemTrader;
                return true;
            }
            if (FoodOrProd == "Pass" || FoodOrProd == "")
            {
                Owner.TradingFood = false;
                Owner.TradingProd = false;
                end = null;
                start = null;
                FoodOrProd = "";
                return false;
            }
            OrderMoveTowardsPosition(start.Center, 0f, new Vector2(0f, -1f), true, start);

            AddShipGoal(Plan.PickupGoods, Vector2.Zero, 0f);

            State = AIState.SystemTrader;
            return true;
        }

        private bool ShouldSuspendTradeDueToCombat()
        {
            return Owner.loyalty.GetOwnedSystems().All(combat => combat.combatTimer > 0);
        }

        public void OrderTradeFromSave(bool hasCargo, Guid startGUID, Guid endGUID)
        {
            if (Owner.CargoSpaceMax <= 0 || State == AIState.Flee || ShouldSuspendTradeDueToCombat())
                return;

            if (start == null && end == null)
                foreach (Planet p in Owner.loyalty.GetPlanets())
                {
                    if (p.guid == startGUID)
                        start = p;
                    if (p.guid != endGUID)
                        continue;
                    end = p;
                }
            if (!hasCargo && start != null)
            {
                OrderMoveTowardsPosition(start.Center + RandomMath.RandomDirection() * 500f, 0f, new Vector2(0f, -1f),
                    true, start);
                AddShipGoal(Plan.PickupGoods, Vector2.Zero, 0f);
                State = AIState.SystemTrader;
            }
            if (!hasCargo || end == null)
            {
                if (!hasCargo && (start == null || end == null))
                    OrderTrade(5f);
                return;
            }
            OrderMoveTowardsPosition(end.Center + RandomMath.RandomDirection() * 500f, 0f, new Vector2(0f, -1f), true,
                end);
            AddShipGoal(Plan.DropOffGoods, Vector2.Zero, 0f);
            State = AIState.SystemTrader;
        }

        private bool PassengerDropOffTarget(Planet p)
        {
            return p != start && p.MaxPopulation > 2000f && (p.Population + p.IncomingColonists) / p.MaxPopulation < 0.75f
                   && !p.NeedsFood();
        }

        private bool PassengerPickUpTarget(Planet p)
        {
            return p != start && p.NeedsFood() || p.MaxPopulation > 2000f 
                && (p.Population + p.IncomingColonists) / p.MaxPopulation > 0.75f;
        }

        public void OrderTransportPassengers(float elapsedTime)
        {
            Owner.TradeTimer -= elapsedTime;
            if (Owner.TradeTimer > 0f || Owner.CargoSpaceMax <= 0f || State == AIState.Flee || Owner.isConstructor)
                return;

            if (ShouldSuspendTradeDueToCombat())
            {
                Owner.TradeTimer = 5f;
                return;
            }
      
            Planet[] safePlanets = Owner.loyalty.GetPlanets().ToArray().FilterBy
                (combat => combat.ParentSystem.combatTimer <= 0f);
            OrderQueue.Clear();

            // RedFox: Where to drop nearest Population
            if (Owner.GetColonists() > 0f)
            {
                if (SelectPlanetByFilter(safePlanets, out end, PassengerDropOffTarget))
                {
                    OrderMoveTowardsPosition(end.Center, 0f, new Vector2(0f, -1f), true, end);
                    State = AIState.PassengerTransport;
                    FoodOrProd = "Pass";
                    AddShipGoal(Plan.DropoffPassengers, Vector2.Zero, 0f);
                    end.IncomingColonists += Owner.GetColonists();
                }
                else if (SelectPlanetByFilter(safePlanets, out end, p => p.Population < p.MaxPopulation))
                {
                    OrderMoveTowardsPosition(end.Center, 0f, new Vector2(0f, -1f), true, end);
                    State = AIState.PassengerTransport;
                    FoodOrProd = "Pass";
                    AddShipGoal(Plan.DropoffPassengers, Vector2.Zero, 0f);
                    end.IncomingColonists += Owner.GetColonists();
                }
                else
                    Owner.ClearCargo();
                return;
            }

            // RedFox: Where to load & drop nearest Population
            SelectPlanetByFilter(safePlanets, out start, PassengerPickUpTarget);
            SelectPlanetByFilter(safePlanets, out end, PassengerDropOffTarget);

            if (start != null && end != null)
            {
                OrderMoveTowardsPosition(start.Center + RandomMath.RandomDirection() * 500f, 0f, new Vector2(0f, -1f),
                    true, start);
                end.IncomingColonists += Owner.CargoSpaceMax;
                start.IncomingColonists -= Owner.CargoSpaceMax;
                AddShipGoal(Plan.PickupPassengers, Vector2.Zero, 0f);
            }
            else
            {
                AwaitClosest = start ?? end;
                start = null;
                end = null;
            }
            Owner.TradeTimer = 5f;
            State = AIState.PassengerTransport;
            FoodOrProd = "Pass";
        }

        public void OrderTransportPassengersFromSave()
        {
            OrderTransportPassengers(0.33f);
        }

        public void OrderTroopToBoardShip(Ship s)
        {
            HasPriorityOrder = true;
            EscortTarget = s;
            OrderQueue.Clear();
            AddShipGoal(Plan.BoardShip, Vector2.Zero, 0f);
        }

        public void OrderTroopToShip(Ship s)
        {
            EscortTarget = s;
            OrderQueue.Clear();
            AddShipGoal(Plan.TroopToShip, Vector2.Zero, 0f);
        }

        private void PickupGoods()
        {
            if (start == null)
            {
                OrderTrade(0.1f);
                return;
            }

            if (FoodOrProd == "Food")
            {
                start.ProductionHere += Owner.UnloadProduction();
                start.Population += Owner.UnloadColonists();

                float maxFoodLoad = (start.FoodHere).Clamp(0f, start.MaxStorage * 0.10f);
                start.FoodHere -= Owner.LoadFood(maxFoodLoad);

                OrderQueue.RemoveFirst();
                OrderMoveTowardsPosition(end.Center + UniverseRandom.RandomDirection() * 500f, 0f,
                    new Vector2(0f, -1f), true, end);
                AddShipGoal(Plan.DropOffGoods, Vector2.Zero, 0f);
            }
            else if (FoodOrProd == "Prod")
            {
                start.FoodHere += Owner.UnloadFood();
                start.Population += Owner.UnloadColonists();

                float maxProdLoad = (start.ProductionHere).Clamp(0f, start.MaxStorage * 10f);
                start.ProductionHere -= Owner.LoadProduction(maxProdLoad);

                OrderQueue.RemoveFirst();
                OrderMoveTowardsPosition(end.Center + UniverseRandom.RandomDirection() * 500f, 0f,
                    new Vector2(0f, -1f), true, end);
                AddShipGoal(Plan.DropOffGoods, Vector2.Zero, 0f);
            }
            else
            {
                OrderTrade(0.1f);
            }
            State = AIState.SystemTrader;
        }

        private void PickupPassengers()
        {
            start.ProductionHere += Owner.UnloadProduction();
            start.FoodHere += Owner.UnloadFood();

            // load everyone we can :P
            start.Population -= Owner.LoadColonists(start.Population * 0.2f);

            OrderQueue.RemoveFirst();
            OrderMoveTowardsPosition(end.Center, 0f, new Vector2(0f, -1f), true, end);
            State = AIState.PassengerTransport;
            AddShipGoal(Plan.DropoffPassengers, Vector2.Zero, 0f);
        }

        private enum transportState
        {
            ChoosePickup,
            GoToPickup,
            ChooseDropDestination,
            GotoDrop,
            DoDrop
        }

        struct TradePlanets
        {
            public Array<Planet> Planets;
            public Array<Planet> SecondaryPlanets;
            public TradePlanets(bool init)
            {
                
                Planets = new Array<Planet>();
                SecondaryPlanets = new Array<Planet>();
                
            }
        }
    }
}