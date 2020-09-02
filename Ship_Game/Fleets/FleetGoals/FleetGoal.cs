﻿using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Fleets;

namespace Ship_Game.Fleets.FleetGoals
{
    public class FleetGoal
    {
        readonly Fleet.FleetGoalType Type;
        public readonly Vector2 MovePosition; // final position of the goal
        public readonly Vector2 FinalDirection; // desired final direction at goal position
        readonly ShipGroup Fleet;

        public FleetGoal(ShipGroup fleet, Vector2 movePosition, Vector2 finalDirection, Fleet.FleetGoalType t)
        {
            Type           = t;
            Fleet          = fleet;
            FinalDirection = finalDirection;
            MovePosition   = movePosition;
        }

        public void Evaluate(FixedSimTime timeStep)
        {
            switch (Type)
            {
                case Fleets.Fleet.FleetGoalType.AttackMoveTo:
                    AttackMoveTo(timeStep);
                    break;
                case Fleets.Fleet.FleetGoalType.MoveTo:
                    MoveTo(timeStep);
                    break;
            }
        }

        void AttackMoveTo(FixedSimTime timeStep)
        {
            Vector2 fleetPos = Fleet.AveragePosition();
            Vector2 towardsFleetGoal = fleetPos.DirectionToTarget(MovePosition);
            Vector2 finalPos = fleetPos + towardsFleetGoal * Fleet.SpeedLimit * timeStep.FixedTime;

            if (finalPos.InRadius(MovePosition, 100f))
            {
                finalPos = MovePosition;
                Fleet.PopGoalStack();
            }

            Fleet.AssembleFleet(finalPos, FinalDirection);
        }

        void MoveTo(FixedSimTime timeStep)
        {
            Vector2 fleetPos = Fleet.AveragePosition();
            Vector2 towardsFleetGoal = fleetPos.DirectionToTarget(MovePosition);
            Vector2 finalPos = fleetPos + towardsFleetGoal * (Fleet.SpeedLimit + 75f) * timeStep.FixedTime;

            if (finalPos.InRadius(MovePosition, 100f))
            {
                finalPos = MovePosition;
                Fleet.PopGoalStack();
            }

            Fleet.AssembleFleet(finalPos, FinalDirection);
        }
    }
}