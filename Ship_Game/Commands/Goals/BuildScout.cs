using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class BuildScout : Goal
    {
        public const string ID = "Build Scout";
        public override string UID => ID;

        public BuildScout() : base(GoalType.BuildScout)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitForShipBuilt,
                OrderExplore,
                ReportGoalCompleteToEmpire
            };
        }
        public BuildScout(Empire empire) : this()
        {
            this.empire = empire;
        }

        bool ChooseScoutShipToBuild(out Ship scout)
        {
            if (EmpireManager.Player == empire
                && ResourceManager.ShipsDict.TryGetValue(EmpireManager.Player.data.CurrentAutoScout, out scout))
                return true;
            var scoutShipsWeCanBuild = new Array<Ship>();
            foreach (string shipUid in empire.ShipsWeCanBuild)
            {
                Ship ship = ResourceManager.ShipsDict[shipUid];
                if (ship.shipData.Role == ShipData.RoleName.scout)
                    scoutShipsWeCanBuild.Add(ship);
            }
            if (scoutShipsWeCanBuild.IsEmpty)
            {
                scout = null;
                return false;
            }
            // pick most power efficient scout
            scout = scoutShipsWeCanBuild.FindMax(s => s.PowerFlowMax - s.NetPower.NetSubLightPowerDraw);
            return scout != null;
        }

        GoalStep FindPlanetToBuildAt()
        {
            if (!ChooseScoutShipToBuild(out Ship scout))
                return GoalStep.GoalFailed;

            if (!empire.FindPlanetToBuildAt(empire.SafeSpacePorts, scout, out Planet planet))
                return GoalStep.TryAgain;

            planet.Construction.Enqueue(scout, this, notifyOnEmpty: false);
            planet.Construction.PrioritizeShip(scout, 1, 2);

            return GoalStep.GoToNextStep;
        }
       
        GoalStep OrderExplore()
        {
            if (FinishedShip == null)
            {
                Log.Error($"BuildScout {ToBuildUID} failed: BuiltShip is null!");
                return GoalStep.GoalFailed;
            }
            FinishedShip.AI.OrderExplore();
            return GoalStep.GoalComplete;
        }

        GoalStep ReportGoalCompleteToEmpire() // FB - Not used: remove this in Mars, when we can break saves
        {
            return GoalStep.GoalComplete;
        }
    }
}
