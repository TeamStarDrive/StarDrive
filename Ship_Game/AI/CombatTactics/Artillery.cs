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

            float distanceToTarget = Owner.Center.Distance(Target.Center);
            float adjustedRange = (Owner.maxWeaponsRange - Owner.Radius);

            if (distanceToTarget > adjustedRange)
            {
                if (distanceToTarget > 7500f)
                    AI.SubLightContinuousMoveInDirection(Owner.Center.DirectionToTarget(Target.Center), elapsedTime);
                else
                    AI.SubLightContinuousMoveInDirection(wantedDirection, elapsedTime);
            }
            else
            {
                float minDistance = Math.Max(adjustedRange * 0.25f + Target.Radius, adjustedRange * 0.5f);

                // slow down
                if (distanceToTarget < minDistance)
                    Owner.Velocity -= Owner.Direction * elapsedTime * Owner.GetSTLSpeed();
                else
                    Owner.Velocity *= 0.95f; // Small propensity to not drift
                AI.RotateTowardsPosition(predictedImpact, elapsedTime,0.05f);
            }
        }
    }
}
