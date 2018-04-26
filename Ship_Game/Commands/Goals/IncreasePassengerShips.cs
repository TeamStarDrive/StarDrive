﻿using System;
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
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitMainGoalCompletion,
                OrderLastIdleTransportToTransportPassengers
            };
        }

        private GoalStep FindPlanetToBuildAt()
        {
            bool flag1 = false;
            foreach (Ship ship in empire.GetShips())
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
                AdvanceToNextStep();
                return GoalStep.GoToNextStep;
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
                    foreach (QueueItem queueItem in planet2.ConstructionQueue)
                        num2 += (int)(((double)queueItem.Cost - (double)queueItem.productionTowards) / (double)planet2.NetProductionPerTurn);
                    if (num2 < num1)
                    {
                        num1 = num2;
                        planet1 = planet2;
                    }
                }
                if (planet1 == null)
                    return GoalStep.TryAgain;
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
                    return GoalStep.GoToNextStep;
                }
                else
                {
                    var civilianFreighters = new Array<Ship>();
                    foreach (string uid in empire.ShipsWeCanBuild)
                    {
                        Ship ship = ResourceManager.ShipsDict[uid];
                        if (!ship.isColonyShip && !ship.isConstructor && (ship.shipData.Role == ShipData.RoleName.freighter || ship.shipData.ShipCategory == ShipData.Category.Civilian))
                            civilianFreighters.Add(ship);
                    }
                    if (civilianFreighters.IsEmpty)
                        return GoalStep.TryAgain;

                    Ship[] orderedByCargoSpace = civilianFreighters.OrderByDescending(ship => ship.CargoSpaceMax).ToArray();

                    var bestCargoShips = new Array<Ship>();
                    foreach (Ship ship in orderedByCargoSpace)
                    {
                        if (ship.CargoSpaceMax >= orderedByCargoSpace[0].CargoSpaceMax)
                            bestCargoShips.Add(ship);
                    }

                    Ship fastestWarpSpeed = bestCargoShips.FindMax(ship => ship.WarpThrust / ship.Mass);
                    planet1.ConstructionQueue.Add(new QueueItem()
                    {
                        isShip = true,
                        QueueNumber = planet1.ConstructionQueue.Count,
                        sData = fastestWarpSpeed.GetShipData(),
                        Goal = this,
                        Cost = fastestWarpSpeed.GetCost(empire)
                    });
                    return GoalStep.GoToNextStep;
                }
            }
        }

        private GoalStep OrderLastIdleTransportToTransportPassengers()
        {
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
                return GoalStep.GoalComplete;
            }
            else
            {
                return GoalStep.RestartGoal;
            }
        }
    }
}
