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
                ReportGoalCompleteToEmpireAndStartTrading
            };
        }
        public IncreaseFreighters(Empire empire) : this()
        {
            this.empire = empire;
        }

        bool PickFreighter(out Ship freighter)
        {
            if (empire.isPlayer && empire.AutoFreighters &&
                ResourceManager.GetShipTemplate(empire.data.CurrentAutoFreighter, out freighter))
                return true;

            var freighters = new Array<Ship>();
            foreach (string shipId in empire.ShipsWeCanBuild)
            {
                Ship ship = ResourceManager.GetShipTemplate(shipId);
                if (ship.shipData.Role != ShipData.RoleName.freighter || ship.CargoSpaceMax < 1f)
                    continue; // definitely not a freighter

                if (ship.isColonyShip || ship.isConstructor)
                    continue; // ignore colony ships and constructors

                if (ship.shipData.ShipCategory == ShipData.Category.Civilian ||
                    ship.shipData.ShipCategory == ShipData.Category.Unclassified)
                    freighters.Add(ship); // only consider civilian/unclassified as freighters
            }

            freighter = freighters
                .OrderByDescending(ship => ship.CargoSpaceMax <= empire.cargoNeed * 0.5f ? ship.CargoSpaceMax : 0)
                .ThenByDescending(ship => (int)(ship.WarpThrust / ship.Mass / 1000f))
                .ThenByDescending(ship => ship.Thrust / ship.Mass)
                .FirstOrDefault();
            return freighter != null;
        }

        private GoalStep FindPlanetToBuildAt()
        {
            if (!PickFreighter(out Ship freighter))
                return GoalStep.GoalFailed;

            if (!empire.FindPlanetToBuildAt(empire.BestBuildPlanets, null, out Planet planet))
                return GoalStep.TryAgain;

            QueueItem qi = planet.Construction.AddShip(freighter, this);
            qi.NotifyOnEmpty = false;
            PlanetBuildingAt = planet;
            return GoalStep.GoToNextStep;
        }

        private GoalStep ReportGoalCompleteToEmpireAndStartTrading()
        {
            empire.ReportGoalComplete(this);
            FinishedShip.DoTrading();
            return GoalStep.GoalComplete;
        }
    }
}
