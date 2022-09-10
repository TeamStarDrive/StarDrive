using Ship_Game.AI;
using Ship_Game.Ships;
using System;
using SDGraphics;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class StandbyColonyShip : Goal
    {
        [StarDataConstructor]
        public StandbyColonyShip(Empire owner) : base(GoalType.StandbyColonyShip, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                CheckIfStandbyShipNeeded,
                BuildColonyShip,
                EnsureBuildingColonyShip,
                KeepOnStandBy
            };
            Evaluate();
        }

        GoalStep CheckIfStandbyShipNeeded()
        {
            return Owner.GetEmpireAI().Goals.Filter(g => g.Type == GoalType.StandbyColonyShip)
                       .Length > Owner.DifficultyModifiers.StandByColonyShips.UpperBound(Owner.GetPlanets().Count) 

                ? GoalStep.GoalFailed  // reached standby colony ship limit
                : GoalStep.GoToNextStep;
        }

        GoalStep BuildColonyShip()
        {
            if (!ShipBuilder.PickColonyShip(Owner, out IShipDesign colonyShip))
                return GoalStep.GoalFailed;

            if (!Owner.FindPlanetToBuildShipAt(Owner.SafeSpacePorts, colonyShip, out Planet planet))
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
