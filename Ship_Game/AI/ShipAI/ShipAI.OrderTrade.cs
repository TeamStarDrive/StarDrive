using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    public sealed partial class ShipAI
    {
        public Planet start;
        public Planet end;
        public Goods FoodOrProd;

        public bool NoGoods => FoodOrProd == Goods.None;
        public bool IsFood => FoodOrProd == Goods.Food;
        public bool IsProd => FoodOrProd == Goods.Production;
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
        private static float UniverseSize => UniverseScreen?.UniverseSize ?? 5000000f;

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

        private static float TradeSort(Ship ship, Planet planetCheck, Goods good, float cargoCount, bool delivery)
        {
            /*here I am trying to predict the planets need versus the ships speed.
             * I am returning a weighted value that is based on this but primarily the returned value is the time it takes the freighter to get to the target in a straight line
             * 
             * 
             */
            //cargoCount = cargoCount > PlanetCheck.MAX_STORAGE ? PlanetCheck.MAX_STORAGE : cargoCount;
            float resourceRecharge = 0;
            float resourceAmount = 0;
            if (good == Goods.Food)
            {
                resourceRecharge = planetCheck.NetFoodPerTurn;
                resourceAmount = planetCheck.FoodHere;
            }
            else if (good == Goods.Production)
            {
                resourceRecharge = planetCheck.NetProductionPerTurn;
                resourceAmount = planetCheck.ProductionHere;
            }
            float timeTotarget = ship.AI.TimeToTarget(planetCheck);
            float efficiency = resourceRecharge * timeTotarget;

            // return PlanetCheck.MAX_STORAGE / (PlanetCheck.MAX_STORAGE -(Effeciency + resourceAmount));

            if (delivery)
            {
                bool badCargo = efficiency + resourceAmount > planetCheck.MaxStorage;             
                if (!badCargo)
                    return timeTotarget;
            }
            else
            {
                if (planetCheck.MaxStorage * 0.5f < ship.CargoSpaceMax)
                {
                    if (resourceAmount + efficiency < ship.CargoSpaceMax * 0.5f)
                        efficiency = ship.CargoSpaceMax * 0.5f / (resourceAmount + efficiency);
                    else
                        efficiency = 1f;
                }
                else efficiency = 1f;
                return timeTotarget * efficiency; 
            }
            return timeTotarget + UniverseSize;
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

            if (NoGoods)
            {
                end = null;
                start = null;
                return;
            }

            if (Owner.loyalty.data.Traits.Cybernetic < 1 && IsFood && (end == null || Owner.GetFood() > 0))
            {
                if (DeliverShipment(Goods.Food))
                {                    
                    FoodOrProd = Goods.Food;
                    Owner.TradingFood = true;
                    Owner.TradingProd = false;
                }
                else
                {
                    FoodOrProd = Goods.None;
                    Owner.TradingFood = false;
                    Owner.TradingProd = false;
                }
            }
            
            if (IsProd && (end == null || Owner.GetProduction() >0))
            {
                if (DeliverShipment(Goods.Production))
                {
                    
                    FoodOrProd = Goods.Production;
                    Owner.TradingProd = true;
                    Owner.TradingFood = false;
                }
                else
                {
                    FoodOrProd = Goods.None;
                    Owner.TradingFood = false;
                    Owner.TradingProd = false;
                }
            }
            GetShipment(Owner.TradingFood ? Goods.Food : Goods.Production);

            if (start != null && end != null && start != end)
            {
                OrderMoveTowardsPosition(start.Center + RandomMath.RandomDirection() * 500f, 0f,
                    new Vector2(0f, -1f), true, start);

                AddShipGoal(Plan.PickupGoods, Vector2.Zero, 0f);
            }
            else
            {
                OrderQueue.Clear();
                FoodOrProd = Goods.None;
                Owner.TradingFood = false;
                Owner.TradingProd = false;
                AwaitClosest = start ?? Owner.loyalty.FindNearestRallyPoint(Owner.Center); //?? end 
                start = null;
                end = null;                
                if (Owner.CargoSpaceUsed > 0)
                    Owner.ClearCargo();
                //State = AIState.SystemTrader;
                //return;
            }
            State = AIState.SystemTrader;
            Owner.TradeTimer = 5f;
            //if (FoodOrProd.IsEmpty())
            //    FoodOrProd = Owner.TradingFood ? "Food" : "Prod";
            //catch { }
        }

        private void PlayerManualTrade()
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

        private void GetShipment(Goods good)
        {
            if (end == null)
                return;

            TradePlanets tradePlanets = GetTradePlanets(good, Planet.GoodState.EXPORT);
            Array<Planet> planets = tradePlanets.Planets;
            if (planets.Count <= 0)
                return;

            Planet[] sortedPlanets = planets.Sorted(p => TradeSort(Owner, p, good, Owner.CargoSpaceMax, false));
            foreach (Planet p in sortedPlanets)
            {
                if (IsEfficientPickupPlanet(good, p))
                {
                    start = p;
                    break;
                }
            }
        }

        private bool IsEfficientPickupPlanet(Goods good, Planet p)
        {
            if (p.ParentSystem.CombatInSystem)
                return false;

            float cargoSpaceMax = p.GetGoodHere(good);
            float mySpeed = TradeSort(Owner, p, good, Owner.CargoSpaceMax, false);

            using (Owner.loyalty.GetShips().AcquireReadLock())
            {
                foreach (Ship s in Owner.loyalty.GetShips())
                {
                    if (s != Owner && !s.isConstructor &&
                        (s.shipData.Role == ShipData.RoleName.freighter ||
                         s.shipData.ShipCategory == ShipData.Category.Civilian))
                    {
                        ShipGoal plan = s.AI.OrderQueue.PeekLast;
                        if (s.AI.State == AIState.SystemTrader && s.AI.start == p &&
                            plan.Plan == Plan.PickupGoods && s.AI.FoodOrProd == good)
                        {
                            if (p.ImportPriority() != good)
                                break;

                            float currentTrade = TradeSort(s, p, good, s.CargoSpaceMax, false);
                            if (currentTrade > 1000)
                                continue;

                            float efficiency = Math.Abs(currentTrade - mySpeed);
                            efficiency = s.CargoSpaceMax - efficiency * p.GetNetGoodProd(good);
                            if (efficiency > 0)
                                cargoSpaceMax -= efficiency;
                        }

                        if (cargoSpaceMax <= (p.MaxStorage * 0.1f))
                            return false;
                    }
                }
            }
            return true; // Yep, it's ok!
        }

        private bool DeliverShipment(Goods good)
        {
            TradePlanets planets = GetTradePlanets(good, Planet.GoodState.IMPORT);
            if (planets.Planets.Count <= 0)
                return false;

            float cargoSpace = Owner.GetCargo(good);
            cargoSpace = cargoSpace > 0 ? cargoSpace : Owner.CargoSpaceMax;
            var sortPlanets = planets.Planets.OrderBy(planetCheck => TradeSort(Owner, planetCheck, good, cargoSpace, true));
            

            foreach (Planet p in sortPlanets)
            {
                if (p.ParentSystem.CombatInSystem) continue;
                var flag = false;
                float cargoSpaceMax = p.MaxStorage - p.GetGoodHere(good);
                var faster = true;
                float thisTradeStr = TradeSort(Owner, p, good, Owner.CargoSpaceMax, true);
                if (thisTradeStr >= UniverseSize && p.GetGoodHere(good) >= 0)
                    continue;
                if (good == Goods.Food && p.ImportPriority() != Goods.Food)
                    continue;
                using (Owner.loyalty.GetShips().AcquireReadLock())
                {
                    for (var k = 0; k < Owner.loyalty.GetShips().Count; k++)
                    {
                        Ship s = Owner.loyalty.GetShips()[k];
                        if (s == null ||
                            (s.DesignRole != ShipData.RoleName.freighter &&
                             s.shipData.ShipCategory != ShipData.Category.Civilian) || s == Owner ||
                            s.isConstructor) continue;
                        if (s.AI.State == AIState.SystemTrader && s.AI.end == p && s.AI.FoodOrProd == good)
                        {                          
                            float currenTrade = TradeSort(s, p, good, s.CargoSpaceMax, true);
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

            //FoodOrProd = goodSwitch;
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

        private TradePlanets GetTradePlanets(Goods good, Planet.GoodState goodState)
        {
            var planets = new TradePlanets(true);            
            foreach (Planet planet in Owner.loyalty.GetPlanets())
            {
                if (!(planet.ParentSystem.combatTimer <= 0)) continue;
                float distanceWeight = TradeSort(Owner, planet, good, Owner.CargoSpaceMax, false);

                var exportWeight = planet.GetExportWeight(good);
                
                planet.SetExportWeight(good, distanceWeight < exportWeight ? distanceWeight : exportWeight);

                if (planet.GetGoodState(good) == goodState && InsideAreaOfOperation(planet))
                    planets.Planets.Add(planet);
                else if (planet.MaxStorage - planet.GetGoodHere(good) > 0)
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
            if (IsPassengers || NoGoods)
            {
                Owner.TradingFood = false;
                Owner.TradingProd = false;
                end = null;
                start = null;
                FoodOrProd = Goods.None;
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
                OrderMoveTowardsPosition(start.Center.RandomOffset(500f), 0f, Vector.Up(), true, start);
                AddShipGoal(Plan.PickupGoods, Vector2.Zero, 0f);
                State = AIState.SystemTrader;
            }
            if (!hasCargo || end == null)
            {
                if (!hasCargo && (start == null || end == null))
                    OrderTrade(5f);
                return;
            }
            OrderMoveTowardsPosition(end.Center + RandomMath.RandomDirection() * 500f, 0f, Vector.Up(), true, end);
            AddShipGoal(Plan.DropOffGoods, Vector2.Zero, 0f);
            State = AIState.SystemTrader;
        }

        private bool PassengerDropOffTarget(Planet p)
        {
            return p != start && !p.NeedsFood() && p.MaxPopulation > 2000f
                   && (p.Population + p.IncomingColonists) / p.MaxPopulation < 0.75f;
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
      
            Planet[] safePlanets = Owner.loyalty.GetPlanets().FilterBy(combat => combat.ParentSystem.combatTimer <= 0f);
            OrderQueue.Clear();

            // RedFox: We have already have colonists, so find a drop-off planet
            if (Owner.GetColonists() > 0f)
            {
                // try to find a reasonable drop-off planet
                if (SelectPlanetByFilter(safePlanets, out end, PassengerDropOffTarget))
                {
                    OrderMoveTowardsPosition(end.Center, 0f, Vector.Up(), true, end);
                    State = AIState.PassengerTransport;
                    FoodOrProd = Goods.Colonists;
                    AddShipGoal(Plan.DropoffPassengers, Vector2.Zero, 0f);
                    end.IncomingColonists += Owner.GetColonists();
                }
                // try to find ANY safe planet
                else if (SelectPlanetByFilter(safePlanets, out end, p => p.Population < p.MaxPopulation))
                {
                    OrderMoveTowardsPosition(end.Center, 0f, Vector.Up(), true, end);
                    State = AIState.PassengerTransport;
                    FoodOrProd = Goods.Colonists;
                    AddShipGoal(Plan.DropoffPassengers, Vector2.Zero, 0f);
                    end.IncomingColonists += Owner.GetColonists();
                }
                else
                {
                    Owner.ClearCargo(); // Space the colonists
                }
                return;
            }

            // RedFox: Where to load & drop nearest Population
            SelectPlanetByFilter(safePlanets, out start, PassengerPickUpTarget);
            SelectPlanetByFilter(safePlanets, out end, PassengerDropOffTarget);

            // go pick them up!
            if (start != null && end != null)
            {
                OrderMoveTowardsPosition(start.Center.RandomOffset(500f), 0f, Vector.Up(), true, start);
                end.IncomingColonists += Owner.CargoSpaceMax; // resort colony adverts for suckers
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
            FoodOrProd = Goods.Colonists;
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

            if (IsFood)
            {
                start.ProductionHere += Owner.UnloadProduction();
                start.Population += Owner.UnloadColonists();

                float maxFoodLoad = start.FoodHere.Clamped(0f, start.MaxStorage * 0.10f);
                start.FoodHere -= Owner.LoadFood(maxFoodLoad);

                OrderQueue.RemoveFirst();
                OrderMoveTowardsPosition(end.Center + UniverseRandom.RandomDirection() * 500f, 0f,
                    new Vector2(0f, -1f), true, end);
                AddShipGoal(Plan.DropOffGoods, Vector2.Zero, 0f);
            }
            else if (IsProd)
            {
                start.FoodHere += Owner.UnloadFood();
                start.Population += Owner.UnloadColonists();

                float maxProdLoad = start.ProductionHere.Clamped(0f, start.MaxStorage * 10f);
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