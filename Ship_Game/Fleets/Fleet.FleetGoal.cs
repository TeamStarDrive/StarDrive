using Microsoft.Xna.Framework;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    public sealed partial class Fleet
    {
        public class FleetGoal
        {
            public FleetGoalType Type;
            public Vector2 Velocity = new Vector2();
            public Vector2 MovePosition;
            public Vector2 PositionLast = new Vector2();
            public Vector2 FinalFacingVector;
            readonly ShipGroup Fleet;
            public float FinalFacing;

            public FleetGoal(ShipGroup fleet, Vector2 movePosition, float facing, Vector2 fVec, FleetGoalType t)
            {
                Type              = t;
                Fleet             = fleet;
                FinalFacingVector = fVec;
                FinalFacing       = facing;
                MovePosition      = movePosition;
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

            private void DoAttackMove(float elapsedTime)
            {
                Fleet.Position += Fleet.Position.DirectionToTarget(MovePosition) * Fleet.Speed * elapsedTime;
                Fleet.AssembleFleet(FinalFacing.RadiansToDirection());
                if (Vector2.Distance(Fleet.Position, MovePosition) >= 100.0f)
                    return;
                Fleet.Position = MovePosition;
                Fleet.PopGoalStack();
            }

            private void DoMove(float elapsedTime)
            {
                Vector2 dir = Fleet.Position.DirectionToTarget(MovePosition);
                Fleet.Position += dir * (Fleet.Speed + 75f) * elapsedTime;
                Fleet.AssembleFleet(FinalFacing.RadiansToDirection());
                if (Fleet.Position.InRadius(MovePosition, 100f))
                {
                    Fleet.Position = MovePosition;
                    Fleet.PopGoalStack();
                }
            }
        }
    }
}