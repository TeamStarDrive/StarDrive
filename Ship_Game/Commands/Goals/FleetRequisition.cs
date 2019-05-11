using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class FleetRequisition : Goal
    {
        public const string ID = "FleetRequisition";
        public override string UID => ID;

        public FleetRequisition() : base(GoalType.FleetRequisition)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetForFleetRequisition,
                DummyStepTryAgain,
                AddShipToFleetAndMoveToPosition
            };
        }

        public FleetRequisition(ShipAI.ShipGoal goal, ShipAI ai) : this()
        {
            FleetDataNode node = ai.Owner.fleet.DataNodes.First(n => n.Ship == ai.Owner);
            ToBuildUID = goal.VariableString;
            ShipToBuild = ResourceManager.GetShipTemplate(ToBuildUID);
            ShipToBuild.fleet = ai.Owner.fleet;
            ShipToBuild.RelativeFleetOffset = node.FleetOffset;
            Fleet = ai.Owner.fleet;
            PlanetBuildingAt = ai.OrbitTarget;
        }

        public FleetRequisition(string shipName, Empire owner) : this()
        {
            empire = owner;
            ToBuildUID = shipName;
            ShipToBuild = ResourceManager.GetShipTemplate(shipName);
        }

        GoalStep FindPlanetForFleetRequisition()
        {            
            if (PlanetBuildingAt == null || !PlanetBuildingAt.HasSpacePort)
            {
                empire.FindPlanetToBuildAt(empire.SpacePorts, ShipToBuild, out PlanetBuildingAt);
            }

            if (PlanetBuildingAt == null)
                return GoalStep.TryAgain;
            
            PlanetBuildingAt.Construction.AddShip(ShipToBuild, this, notifyOnEmpty: false);
            return GoalStep.GoToNextStep;
        }

        GoalStep AddShipToFleetAndMoveToPosition()
        {
            if (Fleet == null)
            {
                Log.Error($"FleetRequisition {ToBuildUID} complete but Fleet is null!");
                return GoalStep.GoalComplete;
            }
            if (FinishedShip == null)
            {
                Log.Error($"FleetRequisition {ToBuildUID} failed: BuiltShip is null!");
                return GoalStep.GoalFailed;
            }

            using (Fleet.DataNodes.AcquireWriteLock())
            {
                foreach (FleetDataNode node in Fleet.DataNodes)
                {
                    if (node.GoalGUID != guid)
                        continue;

                    Ship ship = FinishedShip;
                    node.Ship = ship;
                    node.GoalGUID = Guid.Empty;

                    if (Fleet.Ships.Count == 0)
                        Fleet.Position = ship.Position + RandomMath.Vector2D(3000f);        
                    if (Fleet.Position == Vector2.Zero)
                        Fleet.Position = empire.FindNearestRallyPoint(ship.Center).Center;

                    ship.RelativeFleetOffset = node.FleetOffset;

                    Fleet.AddShip(ship);
                    ship.AI.SetPriorityOrder(false);
                    ship.AI.OrderMoveTowardsPosition(Fleet.Position + ship.RelativeFleetOffset, ship.fleet.Direction, true, null);
                    return GoalStep.GoalComplete;
                } 
            }
            return GoalStep.GoalComplete;
        }
    }
}
