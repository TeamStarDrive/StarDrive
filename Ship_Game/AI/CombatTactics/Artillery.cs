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
            float smallestArtiDistance = Math.Max(Owner.desiredCombatRange - 500f, Owner.desiredCombatRange * 0.9f);
            float largestArtiDistance = Owner.desiredCombatRange;

            // in general, arty stance is what you use for longrange ships. But since 'users'...
            // failsafe distance logic for large ships with super short range going up against other large ships.
            float collisionRangeBuffer = Owner.Radius + Target.Radius + 150f;
            if (smallestArtiDistance < collisionRangeBuffer) smallestArtiDistance = collisionRangeBuffer;
            if (largestArtiDistance < collisionRangeBuffer + 150f) 
            {
                // houston, we have problem, we can't pee far enough.
                // ship is unsuitable for artillery stance without touching lips with the target.
                largestArtiDistance = collisionRangeBuffer + 150f;
                // let the user learn by being out of range. 
                // Hint: use closerange or strafe if your weapons are that short.
            }

            if (distanceToTarget > largestArtiDistance)
            {
                // approach block
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
                // always adjust to keep facing in intended firing direction, specially important for narrow arc weapons.
                AI.RotateTowardsPosition(predictedImpact, elapsedTime, 0.05f);

                if (distanceToTarget > smallestArtiDistance)
                {
                        // stop, we are close enough.
                        AI.ReverseThrustUntilStopped(elapsedTime); // ignore return value we do not need it.
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