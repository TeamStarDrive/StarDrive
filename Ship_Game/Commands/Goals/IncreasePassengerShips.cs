using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class IncreasePassengerShips : Goal
    {
        public const string ID = "IncreasePassengerShips";
        public override string UID => ID;

        public IncreasePassengerShips() : base(GoalType.BuildShips)
        {
            
        }

        public override void Evaluate()
        {
            if (Held)
                return;
            switch (this.Step)
            {
                case 0:
                    bool flag1 = false;
                    foreach (Ship ship in (Array<Ship>)this.empire.GetShips())
                    {
                        if (!ship.isColonyShip && !ship.isConstructor && ship.CargoSpaceMax > 0
                            && (ship.shipData.Role == ShipData.RoleName.freighter || ship.shipData.ShipCategory == ShipData.Category.Civilian)
                            && (!ship.PlayerShip && ship.AI != null) && (ship.AI.State != AIState.PassengerTransport && ship.AI.State != AIState.SystemTrader))
                        {
                            this.passTran = ship;
                            flag1 = true;
                        }
                    }
                    if (flag1)
                    {
                        this.Step = 2;
                        break;
                    }
                    else
                    {
                        Array<Planet> list1 = new Array<Planet>();
                        foreach (Planet planet in this.empire.GetPlanets())
                        {
                            if (planet.HasShipyard && planet.ParentSystem.combatTimer <= 0)  //fbedard: do not build freighter if combat in system
                                list1.Add(planet);
                        }
                        Planet planet1 = (Planet)null;
                        int num1 = 9999999;
                        foreach (Planet planet2 in list1)
                        {
                            int num2 = 0;
                            foreach (QueueItem queueItem in (Array<QueueItem>)planet2.ConstructionQueue)
                                num2 += (int)(((double)queueItem.Cost - (double)queueItem.productionTowards) / (double)planet2.NetProductionPerTurn);
                            if (num2 < num1)
                            {
                                num1 = num2;
                                planet1 = planet2;
                            }
                        }
                        if (planet1 == null)
                            break;
                        if (this.empire.isPlayer && this.empire.AutoFreighters && ResourceManager.ShipsDict.ContainsKey(this.empire.data.CurrentAutoFreighter))
                        {
                            planet1.ConstructionQueue.Add(new QueueItem()
                            {
                                isShip = true,
                                QueueNumber = planet1.ConstructionQueue.Count,
                                sData = ResourceManager.ShipsDict[this.empire.data.CurrentAutoFreighter].GetShipData(),
                                Goal = this,
                                Cost = ResourceManager.ShipsDict[this.empire.data.CurrentAutoFreighter].GetCost(this.empire),
                                NotifyOnEmpty = false
                            });
                            ++this.Step;
                            break;
                        }
                        else
                        {
                            Array<Ship> list2 = new Array<Ship>();
                            foreach (string index in this.empire.ShipsWeCanBuild)
                            {
                                if (!ResourceManager.ShipsDict[index].isColonyShip && !ResourceManager.ShipsDict[index].isConstructor && (ResourceManager.ShipsDict[index].shipData.Role == ShipData.RoleName.freighter || ResourceManager.ShipsDict[index].shipData.ShipCategory == ShipData.Category.Civilian))
                                    list2.Add(ResourceManager.ShipsDict[index]);
                            }
                            IOrderedEnumerable<Ship> orderedEnumerable1 = ((IEnumerable<Ship>)list2).OrderByDescending<Ship, float>((Func<Ship, float>)(ship => ship.CargoSpaceMax));
                            Array<Ship> list3 = new Array<Ship>();
                            foreach (Ship ship in (IEnumerable<Ship>)orderedEnumerable1)
                            {
                                if (!ship.isColonyShip && (double)ship.CargoSpaceMax >= (double)((IEnumerable<Ship>)orderedEnumerable1).First<Ship>().CargoSpaceMax)
                                    list3.Add(ship);
                            }
                            IOrderedEnumerable<Ship> orderedEnumerable2 = (list2).OrderByDescending<Ship, float>((Func<Ship, float>)(ship => ship.WarpThrust / ship.Mass));
                            if (!(orderedEnumerable2).Any())
                                break;
                            planet1.ConstructionQueue.Add(new QueueItem()
                            {
                                isShip = true,
                                QueueNumber = planet1.ConstructionQueue.Count,
                                sData = ResourceManager.ShipsDict[(orderedEnumerable2).First<Ship>().Name].GetShipData(),
                                Goal = this,
                                Cost = ResourceManager.ShipsDict[(orderedEnumerable2).First<Ship>().Name].GetCost(this.empire)
                            });
                            ++this.Step;
                            break;
                        }
                    }
                case 2:
                    bool flag2 = false;
                    foreach (Ship ship in empire.GetShips())
                    {
                        if (!ship.isColonyShip && !ship.isConstructor
                            && (ship.shipData.Role == ShipData.RoleName.freighter || ship.shipData.ShipCategory == ShipData.Category.Civilian)
                            && (!ship.PlayerShip && ship.AI != null) && (ship.AI.State != AIState.PassengerTransport
                            && ship.AI.State != AIState.SystemTrader && (!ship.AI.HasPriorityOrder && ship.AI.State != AIState.Refit))
                            && ship.AI.State != AIState.Scrap)
                        {
                            this.passTran = ship;
                            flag2 = true;
                        }
                    }
                    if (flag2)
                    {
                        this.passTran.AI.OrderTransportPassengers(0.1f);
                        this.empire.ReportGoalComplete(this);
                        break;
                    }
                    else
                    {
                        this.Step = 0;
                        break;
                    }
            }
        }
    }
}
