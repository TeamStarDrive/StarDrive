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
            Vector2 wantedDirection = Owner.Center.DirectionToTarget(predictedImpact);
            // Vector2 wantedDirection = predictedImpact.DirectionToTarget(Owner.Center); // flipped direction.

            // want intercept point, where target comes into range.
            // Vector2 targetInterceptPos = ?

            float distanceToTarget = Owner.Center.Distance(Target.Center)+0.5f*Target.Radius;
            //float adjustedRange = (Owner.maxWeaponsRange - Owner.Radius);

            // made up approximation.
            float smallestArtiDistance = Math.Max(Owner.desiredCombatRange - 500f, Owner.desiredCombatRange * 0.9f);
            float largestArtiDistance = Owner.desiredCombatRange;

            if (distanceToTarget > largestArtiDistance)
            {
                // approach block
                if (distanceToTarget > largestArtiDistance + 1500f) 
                {
                    // AI.SublightMoveTowardsPosition(targetInterceptPos, elapsedTime);

                    // AI.SubLightContinuousMoveInDirection(Owner.Center.DirectionToTarget(Target.Center), elapsedTime);
                    // AI.SubLightMoveTowardsPosition(Target.Center, elapsedTime);
                    AI.SubLightMoveTowardsPosition(Target.Center, elapsedTime, 0, true);
                }
                else
                {
                    // AI.SubLightContinuousMoveInDirection(wantedDirection, elapsedTime); // if less than 2k out of reach, move towards center of target.
                    AI.SubLightMoveTowardsPosition(predictedImpact, elapsedTime);
                    // AI.RotateTowardsPosition(predictedImpact, elapsedTime, 0.05f);
                }
            }
            else
            {
                // we are in striking range, now we need to try staying close to desired range.

                AI.RotateTowardsPosition(predictedImpact, elapsedTime, 0.05f); // keep facing towards target.

                // if too close, begin backing away. Slow down until down to 90% of arty range, then back off at half speed.
                if (distanceToTarget < smallestArtiDistance)
                {
                    // too close, want to back away.
                    Owner.Velocity -= Owner.Direction * elapsedTime * Owner.GetSTLSpeed() * 0.5f;
                }
                else 
                {
                    /* section blatantly copied from AI.ReverseThrustUntilStopped(float elapsedTime) */
                    if (Owner.Velocity.AlmostZero())
                        return;

                    float deceleration = Owner.velocityMaximum * elapsedTime;
                    if (Owner.Velocity.Length() < deceleration)
                    {
                        Owner.Velocity = Vector2.Zero;
                        return; // stopped
                    }

                    // continue breaking velocity
                    Owner.Velocity -= Owner.Velocity.Normalized() * deceleration;
                    return;
                }
            }
        }
    }
}
