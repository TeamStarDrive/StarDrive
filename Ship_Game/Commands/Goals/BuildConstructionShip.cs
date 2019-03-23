using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class BuildConstructionShip : Goal
    {
        public const string ID = "BuildConstructionShip";
        public override string UID => ID;

        public BuildConstructionShip() : base(GoalType.DeepSpaceConstruction)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitMainGoalCompletion,
                OrderDeepSpaceBuild,
                WaitForDeployment
            };
        }

        public BuildConstructionShip(Vector2 buildPosition, string platformUid, Empire owner) : this()
        {
            BuildPosition = buildPosition;
            ToBuildUID = platformUid;
            empire = owner;
            Evaluate();
        }

        GoalStep FindPlanetToBuildAt()
        {
            if (!ResourceManager.GetShipTemplate(ToBuildUID, out Ship toBuild))
            {
                Log.Error($"BuildConstructionShip: no ship to build with uid={ToBuildUID ?? "null"}");
                return GoalStep.GoalFailed;
            }

            // ShipToBuild will be the constructor ship -- usually a freighter
            // once the freighter is deployed, it will mutate into ToBuildUID
            string constructorId = empire.data.ConstructorShip;
            if (!ResourceManager.GetShipTemplate(constructorId, out ShipToBuild))
            {
                Log.Error($"BuildConstructionShip: no construction ship with uid={constructorId}");
                return GoalStep.GoalFailed;
            }

            if (!empire.FindClosestSpacePort(BuildPosition, out Planet planet))
                return GoalStep.TryAgain;

            // toBuild is only used for cost calculation
            planet.Construction.AddPlatform(toBuild, ShipToBuild, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep OrderDeepSpaceBuild()
        {
            FinishedShip.AI.OrderDeepSpaceBuild(this);
            FinishedShip.isConstructor = true;
            FinishedShip.VanityName = "Construction Ship";
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForDeployment()
        {
            if (FinishedShip == null)
                return GoalStep.RestartGoal;

            return FinishedShip.Active ? GoalStep.TryAgain : GoalStep.GoalComplete;
        }

    }
}
