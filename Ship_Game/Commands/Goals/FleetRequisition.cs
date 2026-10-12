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
                WaitForTheBuild,
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

            PlanetBuildingAt.Construction.Enqueue(Build.Template, QueueItemType.CombatShip, this, notifyOnEmpty: false);
            if (Build.Rush)
                PlanetBuildingAt.Construction.MoveToAndContinuousRushFirstItem();

            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForTheBuild()
        {
            if (PlanetBuildingAt.Owner == Owner)
            {
                var queue = PlanetBuildingAt.ConstructionQueue;
                for (int i = 0; i < queue.Count; ++i)
                    if (queue[i].Goal == this)
                        return GoalStep.TryAgain;
            }

            Fleet?.RemoveGoal(this);
            return GoalStep.GoalFailed;
        }

        GoalStep AddShipToFleetAndMoveToPosition()
        {
            if (Fleet == null)
                return GoalStep.GoalComplete;
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
                node.Goal = null;
                if (ship.Fleet != null)
                    return GoalStep.GoalComplete;

                FleetDataNode slot = node.Ship == null ? node
                    : Fleet.DataNodes.Find(n => n.Ship == null && n.Goal == null && n.ShipName == ship.Name);
                if (slot == null)
                    return GoalStep.GoalComplete;

                if (Fleet.Ships.Count == 0)
                    Fleet.FinalPosition = ship.Position + Owner.Random.Vector2D(3000f);
                if (Fleet.FinalPosition == Vector2.Zero)
                    Fleet.FinalPosition = Owner.FindNearestRallyPoint(ship.Position).Position;

                Fleet.AddExistingShip(ship, slot);
                ship.AI.ResetPriorityOrder(false);
                ship.AI.OrderMoveTo(Fleet.GetFinalPos(ship), ship.Fleet.FinalDirection);

                return GoalStep.GoalComplete;
            } 
            return GoalStep.GoalComplete;
        }
    }
}
