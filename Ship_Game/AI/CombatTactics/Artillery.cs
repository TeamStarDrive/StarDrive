using System;
using Ship_Game.Ships;
using Microsoft.Xna.Framework;
using Ship_Game.AI.ShipMovement.CombatManeuvers;

namespace Ship_Game.AI.CombatTactics
{
    /**
     * Artillery plan in a nutshell:
     *      If out of reach, approach. 
     *      When in reach, maneuver to keep a range that is close to the limit, while facing towards enemy.
     */
    internal sealed class Artillery : CombatMovement
    {
        public Artillery(ShipAI ai) : base(ai)
        {
        }

        protected override void OverrideCombatValues(FixedSimTime timeStep)
        {
            DistanceToTarget = Owner.Center.Distance(OwnerTarget.Center) + 0.5f * OwnerTarget.Radius;
        }

        // @note We don't cache min/max distance, because combat state and target can change most of the dynamics
        protected override CombatMoveState ExecuteAttack(FixedSimTime timeStep)
        {
            Ship target = AI.Target;
            
            float maxDistance = Owner.DesiredCombatRange - ((int)Owner.Radius).RoundUpToMultipleOf(10);
            float minDistance = Math.Max(Owner.DesiredCombatRange - 500f, Owner.DesiredCombatRange * 0.9f);
            // in general, arty stance is what you use for long range ships.
            // This is fail safe distance logic for large ships with super short range going up against other large ships.
           
            float collisionRange = Owner.Radius + target.Radius;
            if (minDistance <= collisionRange)       minDistance = collisionRange;
            if (maxDistance < collisionRange + 150f) maxDistance = collisionRange + 150f;

            
            if (DistanceToTarget > maxDistance)
            {
                // if more than <interceptBuffer> out of reach, move on a intercept course. Does not have to be 1500.
                // something like closingSpeed * 2..3 seconds would be fine, so the there is time for optimal align.

                // maybe (Owner.Velocity - Target.Velocity).Length * 2.0f; to make it adaptable to ship speeds.
                const float interceptBuffer = 1500f;

                if (DistanceToTarget > maxDistance + interceptBuffer) 
                {
                    // This move will keep the ship aligned with the intercept point (for fastest closing of distance).
                    // You won't notice when charging head to head / chasing directly behind, but there are cases 
                    // where this is quite bad for weapons alignment. That is the reason for the buffer.                    
                    AI.SubLightMoveTowardsPosition(target.Center, timeStep);
                }
                else 
                {
                    // spend the last bit of the firing gap on a shot impact course, for optimal alignment.
                    AI.SubLightMoveTowardsPosition(Owner.PredictImpact(target), timeStep);
                }
                return CombatMoveState.Approach;
            }

            // adjust to keep facing in intended firing direction.
            AI.RotateTowardsPosition(Owner.PredictImpact(target), timeStep, 0.05f);

            if (DistanceToTarget > minDistance)
            {
                // stop, we are close enough.
                AI.ReverseThrustUntilStopped(timeStep);
                 return CombatMoveState.Hold;
            }

            if (DistanceToTarget < (maxDistance))
            {
                // we are too close, back away.
                float distanceToBackPedal = (maxDistance - 150f) - DistanceToTarget;
                Owner.SubLightAccelerate(speedLimit: distanceToBackPedal, Thrust.Reverse);
                return CombatMoveState.Retrograde;
            }
            return CombatMoveState.Error;
        }
    }
}
 