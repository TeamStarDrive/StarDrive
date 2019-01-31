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
            private readonly ShipGroup Fleet;
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
                Vector2 vector2 = Fleet.Position.DirectionToTarget(MovePosition);
                float num1 = 0.0f;
                int num2 = 0;
                foreach (Ship ship in Fleet.Ships)
                {
                    if (ship.FleetCombatStatus != FleetCombatStatus.Free && !ship.EnginesKnockedOut)
                    {
                        float num3 = Vector2.Distance(Fleet.Position + ship.FleetOffset, ship.Center);
                        num1 += num3;
                        ++num2;
                    }
                }
                Fleet.Position += vector2 * (Fleet.Speed + 75f) * elapsedTime;
                Fleet.AssembleFleet(FinalFacing.RadiansToDirection());
                if (Vector2.Distance(Fleet.Position, MovePosition) >= 100.0f)
                    return;
                Fleet.Position = MovePosition;
                Fleet.PopGoalStack();
            }
        }
    }
}