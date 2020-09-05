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

        protected override void OverrideCombatValues(FixedSimTime timeStep)
        {
        }

        protected override CombatMoveState ExecuteAttack(FixedSimTime timeStep)
        {
            
            if (DistanceToTarget > DesiredCombatRange)
            {
                AI.SubLightMoveTowardsPosition(AI.Target.Center, timeStep);
                return CombatMoveState.Approach; // we're still way far away from target
            }

            if (DistanceToTarget < (DesiredCombatRange * 0.70f)) // within suitable range
            {
                UpdateOrbitPos(AI.Target.Center, DesiredCombatRange * 0.95f, timeStep);
                AI.SubLightMoveTowardsPosition(OrbitPos, timeStep);
                return CombatMoveState.OrbitInjection;
            }

            Vector2 dir = Owner.Center.DirectionToTarget(AI.Target.Center);
            // when doing broadside to Right, wanted forward dir is 90 degrees left
            // when doing broadside to Left, wanted forward dir is 90 degrees right
            dir = (Direction == OrbitDirection.Right) ? dir.LeftVector() : dir.RightVector();
            AI.ReverseThrustUntilStopped(timeStep);
            AI.RotateTowardsPosition(Owner.Center + dir, timeStep, 0.02f);
            return CombatMoveState.Maintain;
        }
    }
}
