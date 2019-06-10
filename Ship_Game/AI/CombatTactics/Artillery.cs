using Microsoft.Xna.Framework;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.AI.CombatTactics
{
    internal sealed class Artillery : ShipAIPlan
    {
        GameplayObject Target;
        public Artillery(ShipAI ai) : base(ai)
        {
        }
        public override void Execute(float elapsedTime, ShipAI.ShipGoal g)
        {
            Target = AI.Target;
            Vector2 predictedImpact = Owner.PredictImpact(Target);

            float distanceToTarget = Owner.Center.Distance(Target.Center)+0.5f*Target.Radius;

            // made up approximation.
            float smallestArtiDistance = Math.Max(Owner.DesiredCombatRange - 500f, Owner.DesiredCombatRange * 0.9f);
            float largestArtiDistance = Owner.DesiredCombatRange;

            // in general, arty stance is what you use for longrange ships.
            // failsafe distance logic for large ships with super short range going up against other large ships.

            // If something needs this protection, it would probably benefit from using shortrange or strafe order instead.
            float collisionRange = Owner.Radius + Target.Radius;
            if (smallestArtiDistance < collisionRange) smallestArtiDistance = collisionRange;
            if (largestArtiDistance < collisionRange + 150f) // +150 to at least give a little room to brake before hitting point blank.
            {
                largestArtiDistance = collisionRange + 150f;
            }

            // if out of reach, approach. 
            // When in reach, maneuver to keep a range close to the limit, while facing towards enemy.
            if (distanceToTarget > largestArtiDistance)
            {
                if (distanceToTarget > largestArtiDistance + 1500f)
                {
                    AI.SubLightMoveTowardsPosition(Target.Center, elapsedTime);
                }
                else
                {
                    // for the last 1500f move straight towards, this helps narrow arc weapons to be on target.
                    // and skips the expensive prediction.
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