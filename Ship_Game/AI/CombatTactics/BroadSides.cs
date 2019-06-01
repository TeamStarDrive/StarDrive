using Microsoft.Xna.Framework;
using Ship_Game.AI.ShipMovement;


namespace Ship_Game.AI.CombatTactics
{
    internal sealed class BroadSides : OrbitPlan
    {
        public BroadSides(ShipAI ai, OrbitDirection direction) : base(ai, direction)
        {
        }

        public override void Execute(float elapsedTime, ShipAI.ShipGoal g)
        {
            float distance = Owner.Center.Distance(AI.Target.Center);
            if (distance > Owner.maxWeaponsRange)
            {
                AI.SubLightMoveTowardsPosition(AI.Target.Center, elapsedTime);
                return; // we're still way far away from target
            }

            if (distance < (Owner.maxWeaponsRange * 0.70f)) // within suitable range
            {
                UpdateOrbitPos(AI.Target.Center, Owner.MaxWeaponRange * 0.95f, elapsedTime);
                AI.SubLightMoveTowardsPosition(OrbitPos, elapsedTime);
                return;
            }

            Vector2 dir = Owner.Center.DirectionToTarget(AI.Target.Center);
            // when doing broadside to Right, wanted forward dir is 90 degrees left
            // when doing broadside to Left, wanted forward dir is 90 degrees right
            dir = (Direction == OrbitDirection.Right) ? dir.LeftVector() : dir.RightVector();
            AI.ReverseThrustUntilStopped(elapsedTime);
            AI.RotateTowardsPosition(AI.Owner.Center + dir, elapsedTime, 0.02f);
        }
    }
}
