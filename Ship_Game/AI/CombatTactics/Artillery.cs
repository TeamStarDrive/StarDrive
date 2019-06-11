using Microsoft.Xna.Framework;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.AI.CombatTactics
{
    /* artillery plan in a nutshell:
            If out of reach, approach. 
            When in reach, maneuver to keep a range that is close to the limit, while facing towards enemy.
    */
    internal sealed class Artillery : ShipAIPlan
    {
        GameplayObject Target;
        private float largestArtiDistance, smallestArtiDistance, collisionRange;

        public Artillery(ShipAI ai) : base(ai)
        {
            // init stuff.
            largestArtiDistance = Owner.DesiredCombatRange;
            smallestArtiDistance = Math.Max(Owner.DesiredCombatRange - 500f, Owner.DesiredCombatRange * 0.9f);

            Setup();
        }

        /* Set up things that only need to be changed when target changes */
        private void Setup()
        {
            Target = AI.Target; if (Target == null) return;
            // in general, arty stance is what you use for longrange ships.
            // This is failsafe distance logic for large ships with super short range going up against other large ships.

            collisionRange = Owner.Radius + Target.Radius;
            if (smallestArtiDistance <= collisionRange) smallestArtiDistance = collisionRange;
            if (largestArtiDistance < collisionRange + 150f) largestArtiDistance = collisionRange + 150f;
            
            // If something needs this protection, it would probably benefit from using shortrange or strafe instead.
        }

        public override void Execute(float elapsedTime, ShipAI.ShipGoal g)
        {
            if(Target!=AI.Target) // catch target switches.
            {
                if (Target == null) return;
                Setup(); 
            }

            Vector2 predictedImpact = Owner.PredictImpact(Target);

            float distanceToTarget = Owner.Center.Distance(Target.Center)+0.5f*Target.Radius;

            if (distanceToTarget > largestArtiDistance)
            {
                if (distanceToTarget > largestArtiDistance + 1500f)
                {
                    AI.SubLightMoveTowardsPosition(Target.Center, elapsedTime); // built in prediction for intercept course.
                }
                else
                {
                    AI.SubLightMoveTowardsPosition(predictedImpact, elapsedTime);
                }
            }
            else
            {
                // adjust to keep facing in intended firing direction, specially important for narrow arc weapons.
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
 