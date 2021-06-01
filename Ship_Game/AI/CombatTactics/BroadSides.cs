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
        Vector2 InitialMovePoint(float desiredTargetRange) => OwnerTarget.Center + SetInitialOrbitEntryPoint(desiredTargetRange);
       
        protected override void OverrideCombatValues(FixedSimTime timeStep)
        {
            DesiredCombatRange = AI.Owner.DesiredCombatRange;//  - AI.Owner.Radius - AI.Target.Radius;
        }

        protected override CombatMoveState ExecuteAttack(FixedSimTime timeStep)
        {
            CombatMoveState moveState = CombatMoveState.Error;

            float maxCombatRange = DesiredCombatRange + Owner.Radius;

            if (DistanceToTarget > maxCombatRange)
            {
                AI.SubLightMoveTowardsPosition(InitialMovePoint(DesiredCombatRange), timeStep);
                moveState = CombatMoveState.Approach;
            }
            else if (TargetIsMighty(0.5f))
            {
                Vector2 initialMovePoint = InitialMovePoint(DesiredCombatRange * 0.75f);
                AI.SubLightMoveTowardsPosition(initialMovePoint, timeStep);
                moveState = CombatMoveState.Approach;
            }
            else
            {
                moveState  = ExecuteBroadside(timeStep);
            }

            return moveState;
        }

        CombatMoveState ExecuteBroadside(FixedSimTime timeStep)
        {
            Vector2 dir = Owner.Center.DirectionToTarget(AI.Target.Center);
            // when doing broadside to Right, wanted forward dir is 90 degrees left
            // when doing broadside to Left, wanted forward dir is 90 degrees right
            dir = (Direction == OrbitDirection.Right) ? dir.LeftVector() : dir.RightVector();
            AI.ReverseThrustUntilStopped(timeStep);
            AI.RotateTowardsPosition(Owner.Center + dir, timeStep, 0.02f);
            return CombatMoveState.Maintain;
        }
        /// <summary>
        /// Creates a point that allows a turn friendly orbit injection point. 
        /// </summary>
        Vector2 SetInitialOrbitEntryPoint(float wantedRangeToTarget)
        {
            Vector2 initialOffset = Vectors.Up * wantedRangeToTarget;
            if (OwnerTarget != null)
            {
                Vector2 initialDirection = OwnerTarget.Center.DirectionToTarget(Owner.Center);
                initialOffset = initialDirection * DesiredCombatRange;

                // desiredCombatRange * 0.25... need a turn friendly value here. 
                if (Direction == OrbitDirection.Left)
                    initialOffset += initialDirection.LeftVector() * (wantedRangeToTarget * 0.25f);
                else
                    initialOffset += initialDirection.RightVector() * (wantedRangeToTarget * 0.25f);

                ForceOrbitOffSet(initialOffset);
            }
            return initialOffset;
        }
    }
}
