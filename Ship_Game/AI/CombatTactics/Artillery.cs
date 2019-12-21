using Microsoft.Xna.Framework;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.AI.CombatTactics
{
    /**
     * Artillery plan in a nutshell:
     *      If out of reach, approach. 
     *      When in reach, maneuver to keep a range that is close to the limit, while facing towards enemy.
     */
    internal sealed class Artillery : ShipAIPlan
    {
        public Artillery(ShipAI ai) : base(ai)
        {
        }

        // @note We don't cache min/max distance, because combat state and target can change most of the dynamics
        public override void Execute(float elapsedTime, ShipAI.ShipGoal g)
        {
            float maxDistance = Owner.DesiredCombatRange;
            float minDistance = Math.Max(Owner.DesiredCombatRange - 500f, Owner.DesiredCombatRange * 0.9f);

            // in general, arty stance is what you use for long range ships.
            // This is fail safe distance logic for large ships with super short range going up against other large ships.

            Ship target = AI.Target;
            float collisionRange = Owner.Radius + target.Radius;
            if (minDistance <= collisionRange)       minDistance = collisionRange;
            if (maxDistance < collisionRange + 150f) maxDistance = collisionRange + 150f;

            float distanceToTarget = Owner.Center.Distance(target.Center) + 0.5f * target.Radius;
            if (distanceToTarget > maxDistance)
            {
                // if more than <interceptBuffer> out of reach, move on a intercept course. Does not have to be 1500.
                // something like closingSpeed * 2..3 seconds would be fine, so the there is time for optimal align.

                // maybe (Owner.Velocity - Target.Velocity).Length * 2.0f; to make it adaptable to ship speeds.
                const float interceptBuffer = 1500f;

                if (distanceToTarget > maxDistance + interceptBuffer) 
                {
                    // This move will keep the ship aligned with the intercept point (for fastest closing of distance).
                    // You won't notice when charging head to head / chasing directly behind, but there are cases 
                    // where this is quite bad for weapons alignment. That is the reason for the buffer.                    
                    AI.SubLightMoveTowardsPosition(target.Center, elapsedTime);
                }
                else 
                {
                    // spend the last bit of the firing gap on a shot impact course, for optimal alignment.
                    AI.SubLightMoveTowardsPosition(Owner.PredictImpact(target), elapsedTime);
                }
            }
            else
            {
                // adjust to keep facing in intended firing direction.
                AI.RotateTowardsPosition(Owner.PredictImpact(target), elapsedTime, 0.05f);

                if (distanceToTarget > minDistance)
                {
                    // stop, we are close enough.
                    AI.ReverseThrustUntilStopped(elapsedTime);
                }
                else 
                {
                    // we are too close, back away.
                    Owner.Velocity -= Owner.Direction * elapsedTime * Owner.MaxSTLSpeed * 0.50f;
                }
            }
        }
    }
}
 