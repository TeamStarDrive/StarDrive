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
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; }

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

        public BuildConstructionShip(Vector2 buildPos, string platformUid, Empire owner, bool rush = false, SolarSystem system = null)
            : this(owner)
        {
            Initialize(platformUid, buildPos, system);
            Build.Rush = rush;
            var projecors = owner.OwnedProjectors;
            // try catch multipler projector build on same place
            if (platformUid == "Subspace Projector")
            {
                foreach (Ship projector in projecors)
                    if (projector.Position.InRadius(buildPos, 100))
                        Log.Error($"Build pos of projector is near {buildPos}");
            }
        }

        public BuildConstructionShip(Vector2 buildPos, string platformUid, Empire owner, Planet tetherPlanet, Vector2 tetherOffset)
            : this(owner)
        {
            Initialize(platformUid, buildPos, tetherPlanet, tetherOffset);
        }

        GoalStep FindPlanetToBuildAt()
        {
            IShipDesign constructor = BuildableShip.GetConstructor(Owner);

            if (!Owner.FindPlanetToBuildShipAt(Owner.SafeSpacePorts, ToBuild, out Planet planet, priority: 0.25f))
                return GoalStep.TryAgain;

            PlanetBuildingAt = planet;
            QueueItemType itemType = ToBuild.IsSubspaceProjector ? QueueItemType.RoadNode : QueueItemType.Orbital;
            if (Build.Rush)
                itemType = QueueItemType.OrbitalUrgent;
            
            planet.Construction.Enqueue(itemType, ToBuild, constructor, Build.Rush, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep OrderDeepSpaceBuild()
        {
            if (FinishedShip == null) 
                return GoalStep.GoalFailed;

            FinishedShip.AI.OrderDeepSpaceBuild(this, ToBuild.GetCost(Owner));
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
