using Microsoft.Xna.Framework;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.AI.CombatTactics
{
    internal sealed class BroadSides : ShipAIPlan
    {
        readonly ShipAI.Orbit OrbitDirection;
        public BroadSides(ShipAI ai, ShipAI.Orbit direction ) : base(ai)
        {
            OrbitDirection = direction;
        }
        public override void Execute(float elapsedTime, ShipAI.ShipGoal g)
        {
            float distance = Owner.Center.Distance(AI.Target.Center);
            if (distance > Owner.maxWeaponsRange)
            {
                AI.SubLightMoveTowardsPosition(AI.Target.Center, elapsedTime, 0, true);
                return; // we're still way far away from target
            }

            if (distance < (Owner.maxWeaponsRange * 0.70f)) // within suitable range
            {
                //Vector2 ourPosNextFrame = Owner.Center + Owner.Velocity * elapsedTime;
                //if (ourPosNextFrame.InRadius(AI.Target.Center, distance))
                {
                    Vector2 nextOrbitPoint = AI.SetNextOrbitPoint(AI.Target.Center, OrbitDirection, Owner.maxWeaponsRange * .9f) ;
                    AI.SubLightMoveTowardsPosition(AI.Owner.Center.DirectionToTarget(nextOrbitPoint), elapsedTime, 0, true);
                    return;
                }

            }

            Vector2 dir = Owner.Center.DirectionToTarget(AI.Target.Center);
            // when doing broadside to Right, wanted forward dir is 90 degrees left
            // when doing broadside to Left, wanted forward dir is 90 degrees right
            dir = (OrbitDirection == ShipAI.Orbit.Right) ? dir.LeftVector() : dir.RightVector();
            AI.SublightSlowToStop(elapsedTime);
            AI.RotateTowardsPosition(AI.Owner.Center + dir, elapsedTime, 0.02f);

        }
    }
}
