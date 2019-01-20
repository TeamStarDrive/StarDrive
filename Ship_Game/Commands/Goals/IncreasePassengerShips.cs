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
        Ship IdleTransport;

        public IncreasePassengerShips() : base(GoalType.BuildShips)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitMainGoalCompletion,
                OrderLastIdleTransportToTransportPassengers
            };
        }

        bool FindIdleTransport(out Ship transport)
        {
            foreach (Ship ship in empire.GetShips())
            {
                if (!ship.IsFreighter || !ship.IsIdleFreighter) continue;
                transport = ship;
                return true;
            }
            transport = null;
            return false;
        }

        bool GetNewFreighterType(out Ship freighter)
        {
            if (empire.isPlayer && empire.AutoFreighters && 
                ResourceManager.GetShipTemplate(empire.data.CurrentAutoFreighter, out freighter))
                return true;

            var freighters = new Array<Ship>();
            foreach (string uid in empire.ShipsWeCanBuild)
            {
                Ship ship = ResourceManager.GetShipTemplate(uid);
                if (ship.IsFreighter) freighters.Add(ship);
            }
            if (freighters.IsEmpty)
            {
                freighter = null;
                return false;
            }

            Ship[] orderedByCargoSpace = freighters.OrderByDescending(ship => ship.CargoSpaceMax).ToArray();

            // best cargo ships with equal cargo space value
            var bestCargoShips = new Array<Ship>();
            foreach (Ship ship in orderedByCargoSpace)
            {
                if (ship.CargoSpaceMax >= orderedByCargoSpace[0].CargoSpaceMax)
                    bestCargoShips.Add(ship);
            }

            // pick the one with best thrust
            freighter = bestCargoShips.FindMax(ship => ship.WarpThrust / ship.Mass);
            return freighter != null;
        }

        GoalStep FindPlanetToBuildAt()
        {
            if (FindIdleTransport(out IdleTransport))
            {
                AdvanceToNextStep(); // jump to final step
                return GoalStep.GoToNextStep;
            }

            if (!GetNewFreighterType(out Ship freighter))
            {
                Log.Warning("IncreasePassengerShips failed: no freighter types");
                return GoalStep.GoalFailed;
            }

            if (!empire.TryFindSpaceportToBuildShipAt(freighter, out Planet planet))
                return GoalStep.TryAgain;

            planet.Construction.AddShip(freighter, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep OrderLastIdleTransportToTransportPassengers()
        {
            Ship transport = FinishedShip ?? IdleTransport;

            if (transport == null && !FindIdleTransport(out transport))
                return GoalStep.RestartGoal;

            transport.AI.OrderTransportPassengers(0.1f);
            empire.ReportGoalComplete(this);
            return GoalStep.GoalComplete;
        }
    }
}
