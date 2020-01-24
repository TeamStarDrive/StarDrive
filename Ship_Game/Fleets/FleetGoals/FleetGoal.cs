using Microsoft.Xna.Framework;
using Ship_Game.AI;

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

        public void Evaluate(float elapsedTime)
        {
            switch (Type)
            {
                case AI.Fleet.FleetGoalType.AttackMoveTo:
                    AttackMoveTo(elapsedTime);
                    break;
                case AI.Fleet.FleetGoalType.MoveTo:
                    MoveTo(elapsedTime);
                    break;
            }
        }

        void AttackMoveTo(float elapsedTime)
        {
            Vector2 fleetPos = Fleet.AveragePosition();
            Vector2 towardsFleetGoal = fleetPos.DirectionToTarget(MovePosition);
            Vector2 finalPos = fleetPos + towardsFleetGoal * Fleet.SpeedLimit * elapsedTime;

            if (finalPos.InRadius(MovePosition, 100f))
            {
                finalPos = MovePosition;
                Fleet.PopGoalStack();
            }

            Fleet.AssembleFleet(finalPos, FinalDirection);
        }

        void MoveTo(float elapsedTime)
        {
            Vector2 fleetPos = Fleet.AveragePosition();
            Vector2 towardsFleetGoal = fleetPos.DirectionToTarget(MovePosition);
            Vector2 finalPos = fleetPos + towardsFleetGoal * (Fleet.SpeedLimit + 75f) * elapsedTime;

            if (finalPos.InRadius(MovePosition, 100f))
            {
                finalPos = MovePosition;
                Fleet.PopGoalStack();
            }

            Fleet.AssembleFleet(finalPos, FinalDirection);
        }
    }
}