using System;
using Ship_Game.Ships;
using SDGraphics;
using Ship_Game.AI.ShipMovement.CombatManeuvers;
using Ship_Game.ExtensionMethods;

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

        // Artillery holds a fixed facing to the target and reverse-thrusts to keep range; the
        // anti-chase Disengage (turn-and-burn) would thrash against that face-the-target logic
        // and drive the ship forward into the enemy. So Artillery never disengages.
        protected override bool AllowDisengage => false;

        protected override void OverrideCombatValues(FixedSimTime timeStep)
        {
            Ship target = OwnerTarget;
            if (target != null)
                DistanceToTarget = Owner.Position.Distance(target.Position) + 0.5f * target.Radius;
        }

        // @note We don't cache min/max distance, because combat state and target can change most of the dynamics
        protected override CombatMoveState ExecuteAttack(FixedSimTime timeStep)
        {
            Ship target = OwnerTarget;
            if (target == null)
                return CombatMoveState.Error;
            
            float maxDistance = Owner.DesiredCombatRange - ((int)Owner.Radius).RoundUpToMultipleOf(10);
            float minDistance = Math.Max(Owner.DesiredCombatRange - 500f, Owner.DesiredCombatRange * 0.9f);
            // in general, arty stance is what you use for long range ships.
            // This is fail safe distance logic for large ships with super short range going up against other large ships.
           
            float collisionRange = Owner.Radius + target.Radius;
            if (minDistance <= collisionRange)       minDistance = collisionRange;
            if (maxDistance < collisionRange + 150f) maxDistance = collisionRange + 150f;
            minDistance = minDistance.UpperBound(maxDistance - 150f);

            
            if (DistanceToTarget > maxDistance)
            {
                // if more than <interceptBuffer> out of reach, move on a intercept course. Does not have to be 1500.
                // something like closingSpeed * 2..3 seconds would be fine, so the there is time for optimal align.

                // maybe (Owner.Velocity - Target.Velocity).Length * 2.0f; to make it adaptable to ship speeds.
                const float interceptBuffer = 1500f;

                // Start decelerating EARLY so we coast to a stop AT the standoff range instead of
                // carrying our full approach momentum straight through it to point-blank (the
                // artillery "overshoot" that makes long-range ships fight up close). Once the
                // standoff range is within the stopping distance for the speed we close on the target
                // (plus a 1.2 margin), start reverse-thrusting now while staying faced on the target so
                // we can keep firing. Driving the thrust directly avoids the late-braking we'd get from
                // SubLightMoveTowardsPosition, which skips its speed cap on any frame it's rotating.
                float distToStandoff = DistanceToTarget - maxDistance;
                float closingSpeed = (Owner.Velocity - target.Velocity).Dot(DirectionToTarget);
                float brakingDistance = Owner.GetMinDecelerationDistance(closingSpeed.LowerBound(0f)) * 1.2f;

                if (distToStandoff <= brakingDistance)
                {
                    AI.RotateTowardsPosition(Owner.PredictAim(target), timeStep, 0.05f);
                    Owner.SubLightAccelerate(stlSpeedLimit: 0f, Thrust.Reverse);
                    return CombatMoveState.Approach;
                }

                if (DistanceToTarget > maxDistance + interceptBuffer)
                {
                    // This move will keep the ship aligned with the intercept point (for fastest closing of distance).
                    AI.SubLightMoveTowardsPosition(target.Position, timeStep);
                }
                else
                {
                    // spend the last bit of the firing gap on a shot impact course, for optimal alignment.
                    AI.SubLightMoveTowardsPosition(Owner.PredictImpact(target), timeStep);
                }
                return CombatMoveState.Approach;
            }

            // adjust to keep facing in intended firing direction.
            AI.RotateTowardsPosition(Owner.PredictAim(target), timeStep, 0.05f);

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
                Owner.SubLightAccelerate(stlSpeedLimit: distanceToBackPedal, Thrust.Reverse);
                return CombatMoveState.Retrograde;
            }
            return CombatMoveState.Error;
        }
    }
}
 