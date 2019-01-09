using System;
using System.Linq;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class IncreaseFreighters : Goal
    {
        public const string ID = "IncreaseFreighters";
        public override string UID => ID;

        public IncreaseFreighters() : base(GoalType.BuildShips)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitMainGoalCompletion,
                ReportGoalCompleteToEmpire
            };
        }
        public IncreaseFreighters(Empire empire) : this()
        {
            this.empire = empire;
        }

        private GoalStep FindPlanetToBuildAt()
        {
            Planet planet1 = null;
            int num1 = 9999999;

            foreach (Planet planet2 in empire.BestBuildPlanets)
            {
                int num2 = 0;
                int finCon = 0;
                foreach (QueueItem queueItem in planet2.ConstructionQueue)
                {
                    num2 += (int)((queueItem.Cost - queueItem.productionTowards) / planet2.Prod.NetIncome);
                    if (queueItem.Goal is IncreaseFreighters)
                        finCon++;
                }
                if (finCon > 2)
                    continue;
                if (num2 < num1)
                {
                    num1 = num2;
                    planet1 = planet2;
                }
            }
            if (planet1 == null)
            {
                return GoalStep.TryAgain;
            }
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
            Array<Ship> list2 = new Array<Ship>();
            foreach (string index in empire.ShipsWeCanBuild)
            {
                Ship ship = ResourceManager.GetShipTemplate(index);
                if (!ship.isColonyShip && !ship.isConstructor && ship.CargoSpaceMax > 0
                    && (ship.shipData.Role == ShipData.RoleName.freighter
                        && (ship.shipData.ShipCategory == ShipData.Category.Civilian || ship.shipData.ShipCategory == ShipData.Category.Unclassified)))
                    list2.Add(ship);
            }
            Ship toBuild = list2
                .OrderByDescending(ship => ship.CargoSpaceMax <= empire.cargoNeed * .5f ? ship.CargoSpaceMax : 0)
                .ThenByDescending(ship => (int)(ship.WarpThrust / ship.Mass / 1000f))
                .ThenByDescending(ship => ship.Thrust / ship.Mass)
                .FirstOrDefault();

            if (toBuild == null)
            {
                return GoalStep.TryAgain;
            }
            PlanetBuildingAt = planet1;
            planet1.ConstructionQueue.Add(new QueueItem(planet1)
            {
                isShip = true,
                QueueNumber = planet1.ConstructionQueue.Count,
                //sData = ResourceManager.ShipsDict[Enumerable.First<Ship>((IEnumerable<Ship>)orderedEnumerable2).Name].GetShipData(),
                sData = toBuild.shipData,
                Goal = this,
                //Cost = ResourceManager.ShipsDict[Enumerable.First<Ship>((IEnumerable<Ship>)orderedEnumerable2).Name].GetCost(this.empire)
                Cost = toBuild.GetCost(empire)
            });
            return GoalStep.GoToNextStep;
        }

        private GoalStep ReportGoalCompleteToEmpire()
        {
            empire.ReportGoalComplete(this);
            return GoalStep.GoalComplete;
        }
    }
}
