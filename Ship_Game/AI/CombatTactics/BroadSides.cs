using Microsoft.Xna.Framework;
using Ship_Game.AI.ShipMovement;
using Ship_Game.AI.ShipMovement.CombatManeuvers;


namespace Ship_Game.AI.CombatTactics
{
    internal sealed class BroadSides : OrbitPlan
    {
        public BroadSides(ShipAI ai, OrbitPlan.OrbitDirection direction) : base(ai, direction)
        {
        }

        protected override void OverrideCombatValues(float elapsedTime)
        {
        }

        protected override CombatMoveState ExecuteAttack(float elapsedTime)
        {
            
            if (DistanceToTarget > DesiredCombatRange)
            {
                AI.SubLightMoveTowardsPosition(AI.Target.Center, elapsedTime);
                return CombatMoveState.Approach; // we're still way far away from target
            }

            if (DistanceToTarget < (DesiredCombatRange * 0.70f)) // within suitable range
            {
                UpdateOrbitPos(AI.Target.Center, DesiredCombatRange * 0.95f, elapsedTime);
                AI.SubLightMoveTowardsPosition(OrbitPos, elapsedTime);
                return CombatMoveState.OrbitInjection;
            }

            Vector2 dir = Owner.Center.DirectionToTarget(AI.Target.Center);
            // when doing broadside to Right, wanted forward dir is 90 degrees left
            // when doing broadside to Left, wanted forward dir is 90 degrees right
            dir = (Direction == OrbitDirection.Right) ? dir.LeftVector() : dir.RightVector();
            AI.ReverseThrustUntilStopped(elapsedTime);
            AI.RotateTowardsPosition(Owner.Center + dir, elapsedTime, 0.02f);
            return CombatMoveState.Maintain;
        }
    }
}
