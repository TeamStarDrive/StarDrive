using Microsoft.Xna.Framework;


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
                float accuracy = (Owner.Velocity.Length() + AI.Owner.Velocity.Length()) * 3;
                AI.UpdateOrbitPos(AI.Target.Center, Owner.MaxWeaponRange * .95f, OrbitDirection, accuracy);
                    AI.SubLightMoveTowardsPosition(AI.Owner.Center.DirectionToTarget(AI.GetOrbitPos), elapsedTime, 0, true);
                    return;
            }

            Vector2 dir = Owner.Center.DirectionToTarget(AI.Target.Center);
            // when doing broadside to Right, wanted forward dir is 90 degrees left
            // when doing broadside to Left, wanted forward dir is 90 degrees right
            dir = (OrbitDirection == ShipAI.Orbit.Right) ? dir.LeftVector() : dir.RightVector();
            AI.ReverseThrustUntilStopped(elapsedTime);
            AI.RotateTowardsPosition(AI.Owner.Center + dir, elapsedTime, 0.02f);

        }
    }
}
