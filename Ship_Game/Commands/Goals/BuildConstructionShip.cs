using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class BuildConstructionShip : DeepSpaceBuildGoal
    {
        [StarDataConstructor]
        public BuildConstructionShip(Empire owner) : base(GoalType.DeepSpaceConstruction, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitForShipBuilt,
                OrderDeepSpaceBuild,
                WaitForDeployment
            };
        }

        public BuildConstructionShip(Vector2 buildPos, string platformUid, Empire owner, bool rush = false)
            : this(owner)
        {
            Build = new(platformUid)
            {
                StaticBuildPos = buildPos,
                Rush = rush,
            };
        }

        public BuildConstructionShip(Vector2 buildPos, string platformUid, Empire owner, Planet tetherPlanet, Vector2 tetherOffset)
            : this(owner)
        {
            Build = new(platformUid, tetherPlanet, tetherOffset)
            {
                StaticBuildPos = buildPos,
            };
        }

        GoalStep FindPlanetToBuildAt()
        {
            // ShipToBuild will be the constructor ship -- usually a freighter
            // once the freighter is deployed, it will mutate into ToBuildUID

            IShipDesign constructor = ShipBuilder.PickConstructor(Owner)?.ShipData;
            if (constructor == null)
                throw new($"PickConstructor failed for {Owner.Name}."+
                            "This is a FATAL bug in data files, where Empire is not able to do space construction!");

            if (!Owner.FindPlanetToBuildShipAt(Owner.SafeSpacePorts, Build.Template, out Planet planet, priority: 0.25f))
                return GoalStep.TryAgain;

            // toBuild is only used for cost calculation
            planet.Construction.Enqueue(Build.Template, constructor, this);
            if (Build.Rush)
                planet.Construction.MoveToAndContinuousRushFirstItem();

            return GoalStep.GoToNextStep;
        }

        GoalStep OrderDeepSpaceBuild()
        {
            if (FinishedShip == null) 
                return GoalStep.GoalFailed;

            FinishedShip.AI.OrderDeepSpaceBuild(this);
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
