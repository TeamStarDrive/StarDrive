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
     * artillery plan in a nutshell:
     *      If out of reach, approach. 
     *      When in reach, maneuver to keep a range that is close to the limit, while facing towards enemy.
    */
    internal sealed class Artillery : ShipAIPlan
    {
        GameplayObject Target;
        private float largestArtiDistance;
        private float smallestArtiDistance;

        public Artillery(ShipAI ai) : base(ai)
        {
            largestArtiDistance = Owner.DesiredCombatRange;
            smallestArtiDistance = Math.Max(Owner.DesiredCombatRange - 500f, Owner.DesiredCombatRange * 0.9f);

            Setup();
        }

        /* Set up things that only need to be changed when target changes */
        private void Setup()
        {
            float collisionRange;

            Target = AI.Target;
            if (Target == null) return;
            
            // in general, arty stance is what you use for longrange ships.
            // This is failsafe distance logic for large ships with super short range going up against other large ships.

            collisionRange = Owner.Radius + Target.Radius;
            if (smallestArtiDistance <= collisionRange) smallestArtiDistance = collisionRange;
            if (largestArtiDistance < collisionRange + 150f) largestArtiDistance = collisionRange + 150f;
        }

        public override void Execute(float elapsedTime, ShipAI.ShipGoal g)
        {
            if (Target == null) return;
            if (Target!=AI.Target) // catch target switches. Necessary after moving pre-calculable stuff to constructor/setup.
                Setup(); 

            Vector2 predictedImpact = Owner.PredictImpact(Target);

            float distanceToTarget = Owner.Center.Distance(Target.Center)+0.5f*Target.Radius;

            if (distanceToTarget > largestArtiDistance)
            {
                // if more than <interceptBuffer> out of reach, move on a intercept course. Does not have to be 1500.
                // something like closingSpeed * 2..3 seconds would be fine, so the there is time for optimal align.

                // maybe (Owner.Velocity - Target.Velocity).Length * 2.0f; to make it adaptable to ship speeds.
                float interceptBuffer = 1500f;

                if (distanceToTarget > largestArtiDistance + interceptBuffer) 
                {
                    // This move will keep the ship aligned with the intercept point (for fastest closing of distance).
                    // You won't notice when charging head to head / chasing directly behind, but there are cases 
                    // where this is quite bad for weapons alignment. That is the reason for the buffer.                    
                    AI.SubLightMoveTowardsPosition(Target.Center, elapsedTime);
                }
                else 
                {
                    // spend the last bit of the firing gap on a shot impact course, for optimal alignment.
                    AI.SubLightMoveTowardsPosition(predictedImpact, elapsedTime);
                }
            }
            else
            {
                // adjust to keep facing in intended firing direction.
                AI.RotateTowardsPosition(predictedImpact, elapsedTime, 0.05f);

                if (distanceToTarget > smallestArtiDistance)
                {
                    // stop, we are close enough.
                    AI.ReverseThrustUntilStopped(elapsedTime);
                }
                else 
                {
                    // we are too close, back away.
                    Owner.Velocity -= Owner.Direction * elapsedTime * Owner.GetSTLSpeed() * 0.50f;
                }
            }
        }
    }
}
 