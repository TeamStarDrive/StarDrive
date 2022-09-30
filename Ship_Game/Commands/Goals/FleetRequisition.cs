using System;
using Ship_Game.AI;
using Ship_Game.Ships;
using SDGraphics;
using Ship_Game.Data.Serialization;
using Ship_Game.Fleets;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class FleetRequisition : FleetGoal
    {
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; }
        [StarData] public sealed override BuildableShip Build { get; set; }
        public override IShipDesign ToBuild => Build.Template;

        [StarDataConstructor]
        public FleetRequisition(Empire owner) : base(GoalType.FleetRequisition, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetForFleetRequisition,
                DummyStepTryAgain,
                AddShipToFleetAndMoveToPosition
            };
        }

        public FleetRequisition(string shipName, Empire owner, Fleet fleet, bool rush) : this(owner)
        {
            Fleet = fleet;
            Build = new(shipName) { Rush = rush };
        }

        GoalStep FindPlanetForFleetRequisition()
        {
            if (PlanetBuildingAt == null || !PlanetBuildingAt.HasSpacePort)
            {
                if (!Owner.FindPlanetToBuildShipAt(Owner.SpacePorts, Build.Template, out Planet buildAt))
                    return GoalStep.TryAgain;
                PlanetBuildingAt = buildAt;
            }

            PlanetBuildingAt.Construction.Enqueue(Build.Template, this, notifyOnEmpty: false);
            if (Build.Rush)
                PlanetBuildingAt.Construction.MoveToAndContinuousRushFirstItem();

            return GoalStep.GoToNextStep;
        }

        GoalStep AddShipToFleetAndMoveToPosition()
        {
            if (Fleet == null)
            {
                Log.Error($"FleetRequisition {Build.Template.Name} complete but Fleet is null!");
                return GoalStep.GoalComplete;
            }
            if (FinishedShip == null)
            {
                Log.Error($"FleetRequisition {Build.Template.Name} failed: BuiltShip is null!");
                return GoalStep.GoalFailed;
            }

            foreach (FleetDataNode node in Fleet.DataNodes)
            {
                if (node.Goal != this)
                    continue;

                Ship ship = FinishedShip;
                node.Ship = ship;
                node.Goal = null;

                if (Fleet.Ships.Count == 0)
                    Fleet.FinalPosition = ship.Position + RandomMath.Vector2D(3000f);
                if (Fleet.FinalPosition == Vector2.Zero)
                    Fleet.FinalPosition = Owner.FindNearestRallyPoint(ship.Position).Position;

                Fleet.AddExistingShip(ship,node);
                ship.AI.ResetPriorityOrder(false);
                ship.AI.OrderMoveTo(Fleet.GetFinalPos(ship), ship.Fleet.FinalDirection);

                return GoalStep.GoalComplete;
            } 
            return GoalStep.GoalComplete;
        }
    }
}
