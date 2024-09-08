using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
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

        public BuildConstructionShip(Vector2 buildPos, string platformUid, Empire owner, bool rush = false)
            : this(owner)
        {
            Initialize(platformUid, buildPos, null);
            Build.Rush = rush;

            // try catch multipler projector build on same place
            if (ToBuild.IsSubspaceProjector)
            {
                if (owner.FindProjectorAt(buildPos, 100, out Ship _))
                    Log.Error($"Build pos of projector is near {buildPos}");
            }
        }

        /// <summary>
        ///  build Dyson Swarm Controller
        /// </summary>
        public BuildConstructionShip(Vector2 buildPos, Empire owner, Planet planetToBuildAt)
            : this(owner)
        {
            PlanetBuildingAt = planetToBuildAt;
            Initialize(DysonSwarm.DysonSwarmControllerName, buildPos, null);
        }

        public BuildConstructionShip(Vector2 buildPos, string platformUid, Empire owner, Planet tetherPlanet, Vector2 tetherOffset)
            : this(owner)
        {
            Initialize(platformUid, buildPos, tetherPlanet, tetherOffset);
        }

        GoalStep FindPlanetToBuildAt()
        {
            float structureCost = ToBuild.GetCost(Owner);
            IShipDesign constructor = BuildableShip.GetConstructor(Owner, TetherPlanet?.System ?? TargetSystem, structureCost);

            if (PlanetBuildingAt == null)
            {
                if (!Owner.FindPlanetToBuildShipAt(Owner.SafeSpacePorts, ToBuild, out Planet planet, priority: 0.25f))
                    return GoalStep.TryAgain;

                PlanetBuildingAt = planet;
            }

            QueueItemType itemType = ToBuild.IsSubspaceProjector ? QueueItemType.RoadNode 
                                                                 : ToBuild.IsDysonSwarmController ? QueueItemType.SwarmController 
                                                                                                  : QueueItemType.Orbital;
            if (Build.Rush && !ToBuild.IsDysonSwarmController)
                itemType = QueueItemType.OrbitalUrgent;

            PlanetBuildingAt.Construction.Enqueue(itemType, ToBuild, constructor, structureCost, Build.Rush, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep OrderDeepSpaceBuild()
        {
            if (!ConstructionShipOk) 
                return GoalStep.GoalFailed;

            FinishedShip.AI.OrderDeepSpaceBuild(this, ToBuild.GetCost(Owner), ToBuild.Grid.Radius);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForDeployment()
        {
            if (!ConstructionShipOk)
                return GoalStep.GoalComplete;

            if (FinishedShip.Construction.TryConstruct(BuildPosition) && FinishedShip.System != null)
                FinishedShip.System.TryLaunchBuilderShip(FinishedShip, Owner);

            return GoalStep.TryAgain;
        }

        bool ConstructionShipOk => FinishedShip?.Active == true;
    }
}
