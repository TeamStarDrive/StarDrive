using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

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

                end.FoodHere += Owner.UnloadFood(end.MAX_STORAGE - end.FoodHere);
                end.ProductionHere += Owner.UnloadProduction(end.MAX_STORAGE - end.ProductionHere);
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
                // return Effeciency;// * ((PlanetCheck.MAX_STORAGE + cargoCount) / ((PlanetCheck.MAX_STORAGE - resourceAmount + 1)));
                // Effeciency =  (PlanetCheck.MAX_STORAGE - cargoCount) / (cargoCount + Effeciency + resourceAmount) ;
                //return timeTotarget * Effeciency;
                bool badCargo = Effeciency + resourceAmount > PlanetCheck.MAX_STORAGE;
                //bool badCargo = (cargoCount + Effeciency + resourceAmount) > PlanetCheck.MAX_STORAGE - cargoCount * .5f; //cargoCount + Effeciency < 0 ||
                if (!badCargo)
                    return timeTotarget * (badCargo
                               ? PlanetCheck.MAX_STORAGE / (Effeciency + resourceAmount)
                               : 1); // (float)Math.Ceiling((double)timeTotarget);                
            }
            else
            {
                //return Effeciency * (PlanetCheck.MAX_STORAGE / ((PlanetCheck.MAX_STORAGE - resourceAmount + 1)));
                // Effeciency = (ship.CargoSpace_Max) / (PlanetCheck.MAX_STORAGE);
                //return timeTotarget * Effeciency;
                Effeciency = PlanetCheck.MAX_STORAGE * .5f < ship.CargoSpaceMax
                    ? resourceAmount + Effeciency < ship.CargoSpaceMax * .5f
                        ? ship.CargoSpaceMax * .5f / (resourceAmount + Effeciency)
                        : 1
                    : 1;
                //bool BadSupply = PlanetCheck.MAX_STORAGE * .5f < ship.CargoSpace_Max && PlanetCheck.FoodHere + Effeciency < ship.CargoSpace_Max * .5f;
                //if (!BadSupply)
                return timeTotarget * Effeciency; // (float)Math.Ceiling((double)timeTotarget);
            }
            return timeTotarget + UniverseScreen.UniverseSize;
        }

        public void OrderTrade(float elapsedTime)
        {
            //trade timer is sent but uses arbitrary timer just to delay the routine.
            Owner.TradeTimer -= elapsedTime;
            if (Owner.TradeTimer > 0f)
                return;

            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }

            OrderQueue.Clear();
            if (Owner.GetColonists() > 0.0f)
                return;

            if (start != null && end != null) //resume trading
            {
                Owner.TradeTimer = 5f;
                if (Owner.GetFood() > 0f || Owner.GetProduction() > 0f)
                {
                    OrderMoveTowardsPosition(end.Center, 0f, new Vector2(0f, -1f), true, end);

                    AddShipGoal(Plan.DropOffGoods, Vector2.Zero, 0f);

                    State = AIState.SystemTrader;
                    return;
                }
                else
                {
                    OrderMoveTowardsPosition(start.Center, 0f, new Vector2(0f, -1f), true, start);

                    AddShipGoal(Plan.PickupGoods, Vector2.Zero, 0f);

                    State = AIState.SystemTrader;
                    return;
                }
            }
            Planet potential = null; //<-unused
            var planets = new Array<Planet>();
            IOrderedEnumerable<Planet> sortPlanets;
            bool flag;
            var secondaryPlanets = new Array<Planet>();
            //added by gremlin if fleeing keep fleeing
            if (Math.Abs(Owner.CargoSpaceMax) < 1 || State == AIState.Flee || Owner.isConstructor || Owner.isColonyShip)
                return;
            
            var allincombat = true;
            var noimport = true;
            foreach (Planet p in Owner.loyalty.GetPlanets())
            {
                if (p.ParentSystem.combatTimer <= 0)
                    allincombat = false;
                if (p.ps == Planet.GoodState.IMPORT || p.fs == Planet.GoodState.IMPORT)
                    noimport = false;
            }

            if (allincombat || noimport && Owner.CargoSpaceUsed > 0)
            {
                Owner.TradeTimer = 5f;
                return;
            }
            if (Owner.loyalty.data.Traits.Cybernetic > 0)
                Owner.TradingFood = false;
            
            if (end == null && Owner.CargoSpaceUsed < 1) 
                foreach (Planet planet in Owner.loyalty.GetPlanets())
                {
                    if (planet.ParentSystem.combatTimer > 0)
                        continue;
                    if (planet.fs == Planet.GoodState.IMPORT && InsideAreaOfOperation(planet))
                        planets.Add(planet);
                }

            if (planets.Count > 0)
            {                
                sortPlanets = planets.OrderBy(PlanetCheck =>
                    {
                        return TradeSort(Owner, PlanetCheck, "Food", Owner.CargoSpaceUsed, true);
                    }
                );
                foreach (Planet p in sortPlanets)
                {
                    flag = false;
                    float cargoSpaceMax = p.MAX_STORAGE - p.FoodHere;
                    var faster = true;
                    float mySpeed = TradeSort(Owner, p, "Food", Owner.CargoSpaceMax, true);
                    cargoSpaceMax += p.NetFoodPerTurn * mySpeed;
                    cargoSpaceMax = cargoSpaceMax > p.MAX_STORAGE ? p.MAX_STORAGE : cargoSpaceMax;
                    cargoSpaceMax = cargoSpaceMax < 0 ? 0 : cargoSpaceMax;
                    //Planet with negative food production need more food:

                    using (Owner.loyalty.GetShips().AcquireReadLock())
                    {
                        for (var k = 0; k < Owner.loyalty.GetShips().Count; k++)
                        {
                            Ship s = Owner.loyalty.GetShips()[k];
                            if (s != null &&
                                (s.shipData.Role == ShipData.RoleName.freighter ||
                                 s.shipData.ShipCategory == ShipData.Category.Civilian) && s != Owner &&
                                !s.isConstructor)
                            {
                                if (s.AI.State == AIState.SystemTrader && s.AI.end == p &&
                                    s.AI.FoodOrProd == "Food" && s.CargoSpaceUsed > 0)
                                {
                                    float currenTrade = TradeSort(s, p, "Food", s.CargoSpaceMax, true);
                                    if (currenTrade < mySpeed)
                                        faster = false;
                                    if (currenTrade != 0)
                                    {
                                        flag = true;
                                        break;
                                    }
                                    float efficiency = currenTrade - mySpeed;
                                    if (mySpeed * p.NetFoodPerTurn < p.FoodHere && faster)
                                        continue;
                                    if (p.NetFoodPerTurn <= 0)
                                        efficiency = s.CargoSpaceMax - efficiency * p.NetFoodPerTurn;
                                    else
                                        efficiency = s.CargoSpaceMax - efficiency * p.NetFoodPerTurn;
                                    if (efficiency > 0)
                                    {
                                        if (efficiency > s.CargoSpaceMax)
                                            efficiency = s.CargoSpaceMax;
                                        cargoSpaceMax = cargoSpaceMax - efficiency;
                                    }
                                }
                                if (cargoSpaceMax <= 0f)
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (!flag)
                    {
                        end = p;
                        break;
                    }
                    if (faster)
                        potential = p;
                }
                if (end != null)
                {
                    FoodOrProd = "Food";
                    if (Owner.GetFood() > 0f)
                    {
                        OrderMoveTowardsPosition(end.Center, 0f, new Vector2(0f, -1f), true, end);
                        AddShipGoal(Plan.DropOffGoods, Vector2.Zero, 0f);
                        State = AIState.SystemTrader;
                        return;
                    }
                }
            }

            #region deliver Production (return if already loaded)

            if (end == null && (Owner.TradingProd || Owner.GetProduction() > 0f))
            {
                planets.Clear();
                secondaryPlanets.Clear();
                foreach (Planet planet in Owner.loyalty.GetPlanets())
                {
                    if (planet.ParentSystem.combatTimer > 0)
                        continue;

                    if (planet.ps == Planet.GoodState.IMPORT && InsideAreaOfOperation(planet))
                        planets.Add(planet);
                    else if (planet.MAX_STORAGE - planet.ProductionHere > 0)
                        secondaryPlanets.Add(planet);
                }

                if (Owner.CargoSpaceUsed > 0.01f && planets.Count == 0)
                    planets.AddRange(secondaryPlanets);

                if (planets.Count > 0)
                {
                    if (Owner.GetProduction() > 0.01f)
                        sortPlanets = planets.OrderBy(PlanetCheck =>
                            {
                                return TradeSort(Owner, PlanetCheck, "Production", Owner.CargoSpaceUsed, true);
                            }
                        ); 
                    else                        
                        sortPlanets = planets.OrderBy(PlanetCheck =>
                            {
                                return TradeSort(Owner, PlanetCheck, "Production", Owner.CargoSpaceMax, true);
                            }
                        ); 
                    foreach (Planet p in sortPlanets)
                    {
                        flag = false;
                        float cargoSpaceMax = p.MAX_STORAGE - p.ProductionHere;
                        var faster = true;
                        float thisTradeStr = TradeSort(Owner, p, "Production", Owner.CargoSpaceMax, true);
                        if (thisTradeStr >= UniverseScreen.UniverseSize && p.ProductionHere >= 0)
                            continue;

                        using (Owner.loyalty.GetShips().AcquireReadLock())
                        {
                            for (var k = 0; k < Owner.loyalty.GetShips().Count; k++)
                            {
                                Ship s = Owner.loyalty.GetShips()[k];
                                if (s != null &&
                                    (s.shipData.Role == ShipData.RoleName.freighter || s.shipData.ShipCategory ==
                                     ShipData.Category.Civilian) && s != Owner && !s.isConstructor)
                                {
                                    if (s.AI.State == AIState.SystemTrader && s.AI.end == p &&
                                        s.AI.FoodOrProd == "Prod")
                                    {
                                        float currenTrade = TradeSort(s, p, "Production", s.CargoSpaceMax, true);
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
                        }
                        if (!flag)
                        {
                            end = p;
                            break;
                        }
                        if (faster)
                            potential = p;
                    }
                    if (end != null)
                    {
                        FoodOrProd = "Prod";
                        if (Owner.GetProduction() > 0f)
                        {
                            OrderMoveTowardsPosition(end.Center, 0f, new Vector2(0f, -1f), true, end);
                            AddShipGoal(Plan.DropOffGoods, Vector2.Zero, 0f);
                            State = AIState.SystemTrader;
                            return;
                        }
                    }
                }
            }

            #endregion

            #region Deliver Food LAST (return if already loaded)

            if (end == null && (Owner.TradingFood || Owner.GetFood() > 0.01f) && Owner.GetProduction() == 0.0f)
            {
                planets.Clear();
                foreach (Planet planet in Owner.loyalty.GetPlanets())
                {
                    if (planet.ParentSystem.combatTimer > 0f)
                        continue;
                    if (planet.fs == Planet.GoodState.IMPORT && InsideAreaOfOperation(planet))
                        planets.Add(planet);
                }

                if (planets.Count > 0)
                {
                    if (Owner.GetFood() > 0.01f)
                        //  sortPlanets = planets.OrderBy(PlanetCheck => (PlanetCheck.MAX_STORAGE - PlanetCheck.FoodHere) >= this.Owner.CargoSpace_Max)
                        sortPlanets = planets.OrderBy(PlanetCheck =>
                            {
                                return TradeSort(Owner, PlanetCheck, "Food", Owner.CargoSpaceUsed, true);
                            }
                        ); //.ThenByDescending(f => f.FoodHere / f.MAX_STORAGE);
                    else
                        //sortPlanets = planets.OrderBy(PlanetCheck => (PlanetCheck.MAX_STORAGE - PlanetCheck.FoodHere) >= this.Owner.CargoSpace_Max)
                        //    .ThenBy(dest => (dest.FoodHere + (dest.NetFoodPerTurn - dest.consumption) * GoodMult));

                        sortPlanets = planets.OrderBy(PlanetCheck =>
                            {
                                return TradeSort(Owner, PlanetCheck, "Food", Owner.CargoSpaceMax, true);
                            }
                        ); //.ThenByDescending(f => f.FoodHere / f.MAX_STORAGE);
                    foreach (Planet p in sortPlanets)
                    {
                        flag = false;
                        float cargoSpaceMax = p.MAX_STORAGE - p.FoodHere;
                        var faster = true;
                        float mySpeed = TradeSort(Owner, p, "Food", Owner.CargoSpaceMax, true);
                        if (mySpeed >= UniverseScreen.UniverseSize)
                            continue;
                        cargoSpaceMax += p.NetFoodPerTurn * mySpeed;
                        cargoSpaceMax = cargoSpaceMax > p.MAX_STORAGE ? p.MAX_STORAGE : cargoSpaceMax;
                        cargoSpaceMax = cargoSpaceMax < 0.0f ? 0.0f : cargoSpaceMax;

                        using (Owner.loyalty.GetShips().AcquireReadLock())
                        {
                            for (var k = 0; k < Owner.loyalty.GetShips().Count; k++)
                            {
                                Ship s = Owner.loyalty.GetShips()[k];
                                if (s != null &&
                                    (s.shipData.Role == ShipData.RoleName.freighter || s.shipData.ShipCategory ==
                                     ShipData.Category.Civilian) && s != Owner && !s.isConstructor)
                                {
                                    if (s.AI.State == AIState.SystemTrader && s.AI.end == p &&
                                        s.AI.FoodOrProd == "Food")
                                    {
                                        float currenTrade = TradeSort(s, p, "Food", s.CargoSpaceMax, true);
                                        if (currenTrade < mySpeed)
                                            faster = false;
                                        if (currenTrade > UniverseData.UniverseWidth && !faster)
                                            continue;
                                        float efficiency = Math.Abs(currenTrade - mySpeed);
                                        if (mySpeed * p.NetFoodPerTurn < p.FoodHere && faster)
                                            continue;
                                        if (p.NetFoodPerTurn == 0.0f)
                                            efficiency = s.CargoSpaceMax + efficiency * p.NetFoodPerTurn;
                                        else if (p.NetFoodPerTurn < 0.0f)
                                            efficiency = s.CargoSpaceMax + efficiency * p.NetFoodPerTurn;
                                        else
                                            efficiency = s.CargoSpaceMax - efficiency * p.NetFoodPerTurn;
                                        if (efficiency > 0.0f)
                                        {
                                            if (efficiency > s.CargoSpaceMax)
                                                efficiency = s.CargoSpaceMax;
                                            cargoSpaceMax = cargoSpaceMax - efficiency;
                                        }
                                        //ca
                                    }
                                    if (cargoSpaceMax <= 0.0f)
                                    {
                                        flag = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (!flag)
                        {
                            end = p;
                            break;
                        }
                    }
                    if (end != null)
                    {
                        FoodOrProd = "Food";
                        if (Owner.GetFood() > 0f)
                        {
                            OrderMoveTowardsPosition(end.Center, 0f, new Vector2(0f, -1f), true, end);
                            AddShipGoal(Plan.DropOffGoods, Vector2.Zero, 0f);
                            State = AIState.SystemTrader;
                            return;
                        }
                    }
                }
            }

            #endregion

            #region Get Food

            if (start == null && end != null && FoodOrProd == "Food"
                && (Owner.CargoSpaceUsed == 0 || Owner.CargoSpaceUsed / Owner.CargoSpaceMax < .2f))
            {
                planets.Clear();
                foreach (Planet planet in Owner.loyalty.GetPlanets())
                {
                    if (planet.ParentSystem.combatTimer > 0)
                        continue;

                    float distanceWeight = TradeSort(Owner, planet, "Food", Owner.CargoSpaceMax, false);
                    planet.ExportFSWeight = distanceWeight < planet.ExportFSWeight
                        ? distanceWeight
                        : planet.ExportFSWeight;
                    if (planet.fs == Planet.GoodState.EXPORT && InsideAreaOfOperation(planet))
                        planets.Add(planet);
                }

                if (planets.Count > 0)
                {
                    sortPlanets = planets.OrderBy(PlanetCheck =>
                    {
                        return TradeSort(Owner, PlanetCheck, "Food", Owner.CargoSpaceMax, false);
                        //+ this.TradeSort(this.Owner, this.end, "Food", this.Owner.CargoSpace_Max)
                        ;
                        //weight += this.Owner.CargoSpace_Max / (PlanetCheck.FoodHere + 1);
                        //weight += Vector2.Distance(PlanetCheck.Position, this.Owner.Position) / this.Owner.GetmaxFTLSpeed;
                        //return weight;
                    });
                    foreach (Planet p in sortPlanets)
                    {
                        float cargoSpaceMax = p.FoodHere;
                        flag = false;
                        float mySpeed = TradeSort(Owner, p, "Food", Owner.CargoSpaceMax, false);
                        //cargoSpaceMax = cargoSpaceMax + p.NetFoodPerTurn * mySpeed;
                        using (Owner.loyalty.GetShips().AcquireReadLock())
                        {
                            for (var k = 0; k < Owner.loyalty.GetShips().Count; k++)
                            {
                                Ship s = Owner.loyalty.GetShips()[k];
                                if (s != null &&
                                    (s.shipData.Role == ShipData.RoleName.freighter || s.shipData.ShipCategory ==
                                     ShipData.Category.Civilian) && s != Owner && !s.isConstructor)
                                {
                                    ShipGoal plan = s.AI.OrderQueue.PeekLast;

                                    if (plan != null && s.AI.State == AIState.SystemTrader && s.AI.start == p &&
                                        plan.Plan == Plan.PickupGoods && s.AI.FoodOrProd == "Food")
                                    {
                                        float currenTrade = TradeSort(s, p, "Food", s.CargoSpaceMax, false);
                                        if (currenTrade > 1000)
                                            continue;

                                        float efficiency = Math.Abs(currenTrade - mySpeed);
                                        efficiency = s.CargoSpaceMax - efficiency * p.NetFoodPerTurn;
                                        if (efficiency > 0)
                                        {
                                            if (efficiency > s.CargoSpaceMax)
                                                efficiency = s.CargoSpaceMax;
                                            cargoSpaceMax = cargoSpaceMax - efficiency;
                                        }
                                        //cargoSpaceMax = cargoSpaceMax - s.CargoSpace_Max;
                                    }

                                    if (cargoSpaceMax <= 0 + p.MAX_STORAGE * .1f) // < this.Owner.CargoSpace_Max)
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
                            //this.Owner.TradingFood = true;
                            //this.Owner.TradingProd = false;
                            break;
                        }
                    }
                }
            }

            #endregion

            #region Get Production

            if (start == null && end != null && FoodOrProd == "Prod"
                && (Owner.CargoSpaceUsed == 0 || Owner.CargoSpaceUsed / Owner.CargoSpaceMax < .2f))
            {
                planets.Clear();
                foreach (Planet planet in Owner.loyalty.GetPlanets())
                    if (planet.ParentSystem.combatTimer <= 0)
                    {
                        float distanceWeight = TradeSort(Owner, planet, "Production", Owner.CargoSpaceMax, false);
                        planet.ExportPSWeight = distanceWeight < planet.ExportPSWeight
                            ? distanceWeight
                            : planet.ExportPSWeight;

                        if (planet.ps == Planet.GoodState.EXPORT && InsideAreaOfOperation(planet))
                            planets.Add(planet);
                    }
                if (planets.Count > 0)
                {
                    sortPlanets = planets.OrderBy(PlanetCheck =>
                    {
//(PlanetCheck.ProductionHere > this.Owner.CargoSpace_Max))
                        //.ThenBy(dest => Vector2.Distance(this.Owner.Position, dest.Position));

                        return TradeSort(Owner, PlanetCheck, "Production", Owner.CargoSpaceMax, false);
                        // + this.TradeSort(this.Owner, this.end, "Production", this.Owner.CargoSpace_Max);
                    });
                    foreach (Planet p in sortPlanets)
                    {
                        flag = false;
                        float cargoSpaceMax = p.ProductionHere;

                        float mySpeed = TradeSort(Owner, p, "Production", Owner.CargoSpaceMax, false);
                        //cargoSpaceMax = cargoSpaceMax + p.NetProductionPerTurn * mySpeed;

                        //+ this.TradeSort(this.Owner, this.end, "Production", this.Owner.CargoSpace_Max);

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
                                        plan.Plan == Plan.PickupGoods && s.AI.FoodOrProd == "Prod")
                                    {
                                        float currenTrade = TradeSort(s, p, "Production", s.CargoSpaceMax, false);
                                        if (currenTrade > 1000)
                                            continue;

                                        float efficiency = Math.Abs(currenTrade - mySpeed);
                                        efficiency = s.CargoSpaceMax - efficiency * p.NetProductionPerTurn;
                                        if (efficiency > 0)
                                            cargoSpaceMax = cargoSpaceMax - efficiency;
                                    }

                                    if (cargoSpaceMax <= 0 + p.MAX_STORAGE * .1f) // this.Owner.CargoSpace_Max)
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
                            //this.Owner.TradingFood = false;
                            //this.Owner.TradingProd = true;
                            break;
                        }
                    }
                }
            }

            #endregion

            if (start != null && end != null && start != end)
            {
                //if (this.Owner.CargoSpace_Used == 00 && this.start.Population / this.start.MaxPopulation < 0.2 && this.end.Population > 2000f && Vector2.Distance(this.Owner.Center, this.end.Position) < 500f)  //fbedard: dont make empty run !
                //    this.PickupAnyPassengers();
                //if (this.Owner.CargoSpace_Used == 00 && Vector2.Distance(this.Owner.Center, this.end.Position) < 500f)  //fbedard: dont make empty run !
                //    this.PickupAnyGoods();
                OrderMoveTowardsPosition(start.Center + RandomMath.RandomDirection() * 500f, 0f,
                    new Vector2(0f, -1f), true, start);

                AddShipGoal(Plan.PickupGoods, Vector2.Zero, 0f);
            }
            else
            {
                AwaitClosest = start ?? end;
                start = null;
                end = null;
                if (Owner.CargoSpaceUsed > 0)
                    Owner.ClearCargo();
            }
            State = AIState.SystemTrader;
            Owner.TradeTimer = 5f;
            if (FoodOrProd.IsEmpty())
                FoodOrProd = Owner.TradingFood ? "Food" : "Prod";
            //catch { }
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
            return p != start && p.MaxPopulation > 2000f && p.Population / p.MaxPopulation < 0.5f
                   && RelativePlanetFertility(p) >= 0.5f;
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

            Planet[] safePlanets = Owner.loyalty.GetPlanets()
                .Where(combat => combat.ParentSystem.combatTimer <= 0f)
                .ToArray();
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
                }
                return;
            }

            // RedFox: Where to load & drop nearest Population
            SelectPlanetByFilter(safePlanets, out start, p => p.MaxPopulation > 1000 && p.Population > 1000);
            SelectPlanetByFilter(safePlanets, out end, PassengerDropOffTarget);

            if (start != null && end != null)
            {
                OrderMoveTowardsPosition(start.Center + RandomMath.RandomDirection() * 500f, 0f, new Vector2(0f, -1f),
                    true, start);
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

                float maxFoodLoad = (start.MAX_STORAGE * 0.10f).Clamp(0f, start.MAX_STORAGE - start.FoodHere);
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

                float maxProdLoad = (start.MAX_STORAGE * .10f).Clamp(0f, start.MAX_STORAGE - start.ProductionHere);
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
    }
}