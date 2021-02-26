using Ship_Game.AI;
using Ship_Game.Ships;
using System;

namespace Ship_Game.Commands.Goals
{
    public class StandbyColonyShip : Goal
    {
        public const string ID = "StandbyColonyShip";
        public override string UID => ID;

        public StandbyColonyShip() : base(GoalType.StandbyColonyShip)
        {
            Steps = new Func<GoalStep>[]
            {
                CheckIfStandbyShipNeeded,
                BuildColonyShip,
                EnsureBuildingColonyShip,
                KeepOnStandBy
            };
        }

        public StandbyColonyShip(Empire e) : this()
        {
            empire        = e;
            StarDateAdded = Empire.Universe.StarDate;

            Evaluate();
        }

        GoalStep CheckIfStandbyShipNeeded()
        {
            return empire.GetEmpireAI().Goals.Filter(g => g.type == GoalType.StandbyColonyShip)
                       .Length > empire.DifficultyModifiers.StandByColonyShips.UpperBound(empire.GetPlanets().Count) 

                ? GoalStep.GoalFailed  // reached standby colony ship limit
                : GoalStep.GoToNextStep;
        }

        GoalStep BuildColonyShip()
        {
            if (!ShipBuilder.PickColonyShip(empire, out Ship colonyShip))
                return GoalStep.GoalFailed;

            if (!empire.FindPlanetToBuildAt(empire.SafeSpacePorts, colonyShip, out Planet planet))
                return GoalStep.TryAgain;

            planet.Construction.Enqueue(colonyShip, this);
            planet.Construction.PrioritizeShip(colonyShip, 2);
            return GoalStep.GoToNextStep;
        }

        GoalStep EnsureBuildingColonyShip()
        {
            if (FinishedShip != null) // we already have a ship
                return GoalStep.GoToNextStep;

            if (!IsPlanetBuildingColonyShip())
            {
                PlanetBuildingAt = null;
                return GoalStep.RestartGoal;
            }

            return GoalStep.TryAgain;
        }

        GoalStep KeepOnStandBy()
        {
            if (FinishedShip == null)
                return GoalStep.RestartGoal;

            if (FinishedShip.AI.State == AIState.Colonize)
                return GoalStep.GoalComplete; // Standby ship was picked for colonization

            return GoalStep.TryAgain;
        }

        bool IsPlanetBuildingColonyShip()
        {
            if (PlanetBuildingAt == null)
                return false;

            return PlanetBuildingAt.IsColonyShipInQueue(this);
        }
    }
}
