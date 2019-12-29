using Microsoft.Xna.Framework;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    public sealed partial class Fleet
    {
        public class FleetGoal
        {
            readonly FleetGoalType Type;
            public readonly Vector2 MovePosition; // final position of the goal
            public readonly Vector2 FinalDirection; // desired final direction at goal position
            readonly ShipGroup Fleet;

            public FleetGoal(ShipGroup fleet, Vector2 movePosition, Vector2 finalDirection, FleetGoalType t)
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
                    case FleetGoalType.AttackMoveTo:
                        DoAttackMove(elapsedTime);
                        break;
                    case FleetGoalType.MoveTo:
                        DoMove(elapsedTime);
                        break;
                }
            }

            void DoAttackMove(float elapsedTime)
            {
                Fleet.FinalPosition += Fleet.FinalPosition.DirectionToTarget(MovePosition) * Fleet.SpeedLimit * elapsedTime;
                Fleet.AssembleFleet(FinalDirection);
                if (Vector2.Distance(Fleet.FinalPosition, MovePosition) >= 100.0f)
                    return;
                Fleet.FinalPosition = MovePosition;
                Fleet.PopGoalStack();
            }

            void DoMove(float elapsedTime)
            {
                Vector2 dir = Fleet.FinalPosition.DirectionToTarget(MovePosition);
                Fleet.FinalPosition += dir * (Fleet.SpeedLimit + 75f) * elapsedTime;
                Fleet.AssembleFleet(FinalDirection);
                if (Fleet.FinalPosition.InRadius(MovePosition, 100f))
                {
                    Fleet.FinalPosition = MovePosition;
                    Fleet.PopGoalStack();
                }
            }
        }
    }
}