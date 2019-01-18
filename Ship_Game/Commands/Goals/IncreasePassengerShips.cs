using System;
using System.Linq;
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
                    passTran = ship;
                    flag1 = true;
                }
            }
            if (flag1)
            {
                AdvanceToNextStep();
                return GoalStep.GoToNextStep;
            }

            Array<Planet> list1 = new Array<Planet>();
            foreach (Planet planet in empire.GetPlanets())
            {
                if (planet.HasSpacePort && planet.ParentSystem.combatTimer <= 0)  //fbedard: do not build freighter if combat in system
                    list1.Add(planet);
            }
            Planet planet1 = null;
            int num1 = 9999999;
            foreach (Planet planet2 in list1)
            {
                int num2 = 0;
                foreach (QueueItem queueItem in planet2.ConstructionQueue)
                    num2 += (int)((queueItem.Cost - (double)queueItem.productionTowards) / planet2.Prod.NetIncome);
                if (num2 < num1)
                {
                    num1 = num2;
                    planet1 = planet2;
                }
            }
            if (planet1 == null)
                return GoalStep.TryAgain;
            if (empire.isPlayer && empire.AutoFreighters && ResourceManager.ShipsDict.ContainsKey(empire.data.CurrentAutoFreighter))
            {
                planet1.ConstructionQueue.Add(new QueueItem(planet1)
                {
                    isShip = true,
                    QueueNumber = planet1.ConstructionQueue.Count,
                    sData = ResourceManager.ShipsDict[empire.data.CurrentAutoFreighter].shipData,
                    Goal = this,
                    Cost = ResourceManager.ShipsDict[empire.data.CurrentAutoFreighter].GetCost(empire),
                    NotifyOnEmpty = false
                });
                return GoalStep.GoToNextStep;
            }

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
            planet1.ConstructionQueue.Add(new QueueItem(planet1)
            {
                isShip = true,
                QueueNumber = planet1.ConstructionQueue.Count,
                sData = fastestWarpSpeed.shipData,
                Goal = this,
                Cost = fastestWarpSpeed.GetCost(empire)
            });
            return GoalStep.GoToNextStep;
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
                    passTran = ship;
                    flag2 = true;
                }
            }
            if (flag2)
            {
                passTran.AI.OrderTransportPassengers(0.1f);
                empire.ReportGoalComplete(this);
                return GoalStep.GoalComplete;
            }

            return GoalStep.RestartGoal;
        }
    }
}
