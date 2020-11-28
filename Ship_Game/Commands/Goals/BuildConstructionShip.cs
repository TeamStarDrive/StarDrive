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
            string constructorId = empire.data.CurrentConstructor;
            constructorId = empire.data.ConstructorShip;//ConstructorShip;
            constructorId = empire.data.DefaultConstructor;

            if (!ResourceManager.GetShipTemplate(constructorId, out ShipToBuild))
            {
                Log.Warning($"BuildConstructionShip: no construction ship with uid={constructorId}");
                constructorId = empire.data.DefaultConstructor;
                if (!ResourceManager.GetShipTemplate(constructorId, out ShipToBuild))
                {
                    Log.Warning($"BuildConstructionShip: no construction ship with uid={constructorId}");
                    return GoalStep.GoalFailed;
                }
            }

            Planet planet = empire.FindPlanetToBuildAt(empire.SafeSpacePorts, toBuild.GetCost(empire));
            if (planet == null)
                return GoalStep.TryAgain;

            // toBuild is only used for cost calculation
            planet.Construction.Enqueue(toBuild, ShipToBuild, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep OrderDeepSpaceBuild()
        {
            if (FinishedShip == null) return GoalStep.GoalFailed;
            FinishedShip.AI.OrderDeepSpaceBuild(this);
            FinishedShip.IsConstructor = true;
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForDeployment()
        {
            // FB - must keep this goal until the ship deployed it's structure. 
            // If the goal is not kept, load game construction ships loses the empire goal and get stuck
            return FinishedShip == null ? GoalStep.GoalComplete : GoalStep.TryAgain;
        }

    }
}
