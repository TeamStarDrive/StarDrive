using Ship_Game.AI.ShipMovement;
using Ship_Game.AI.ShipMovement.CombatManeuvers;
using Vector2 = SDGraphics.Vector2;


namespace Ship_Game.AI.CombatTactics
{
    internal sealed class BroadSides : OrbitPlan
    {
        public BroadSides(ShipAI ai, OrbitPlan.OrbitDirection direction) : base(ai, direction)
        {
        }
        Vector2 InitialMovePoint(float desiredTargetRange) => OwnerTarget.Position + SetInitialOrbitEntryPoint(desiredTargetRange);
       
        protected override void OverrideCombatValues(FixedSimTime timeStep)
        {
            DesiredCombatRange = AI.Owner.DesiredCombatRange;
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
            else if (TargetIsMighty(ratioToOurDefense: 0.5f))
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
            Vector2 dir = Owner.Position.DirectionToTarget(AI.Target.Position);
            // when doing broadside to Right, wanted forward dir is 90 degrees left
            // when doing broadside to Left, wanted forward dir is 90 degrees right
            dir = (Direction == OrbitDirection.Right) ? dir.LeftVector() : dir.RightVector();
            AI.ReverseThrustUntilStopped(timeStep);
            AI.RotateTowardsPosition(Owner.Position + dir, timeStep, 0.02f);
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
                Vector2 initialDirection = OwnerTarget.Position.DirectionToTarget(Owner.Position);
                initialOffset = initialDirection * DesiredCombatRange;

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
