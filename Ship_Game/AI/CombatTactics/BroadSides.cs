using Microsoft.Xna.Framework;
using Ship_Game.AI.ShipMovement;


namespace Ship_Game.AI.CombatTactics
{
    internal sealed class BroadSides : ShipAIPlan
    {
        readonly OrbitObject.OrbitDirection OrbitDirection;
        readonly OrbitObject DoOrbit;

        public BroadSides(ShipAI ai, OrbitObject.OrbitDirection direction ) : base(ai)
        {
            OrbitDirection = direction;
            DoOrbit = new OrbitObject(ai);
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
                DoOrbit.UpdateOrbitPos(AI.Target.Center, Owner.MaxWeaponRange * .95f, OrbitDirection, accuracy);
                AI.SubLightMoveTowardsPosition(AI.Owner.Center.DirectionToTarget(DoOrbit.OrbitPos), elapsedTime, 0, true);
                return;
            }

            Vector2 dir = Owner.Center.DirectionToTarget(AI.Target.Center);
            // when doing broadside to Right, wanted forward dir is 90 degrees left
            // when doing broadside to Left, wanted forward dir is 90 degrees right
            dir = (OrbitDirection == OrbitObject.OrbitDirection.Right) ? dir.LeftVector() : dir.RightVector();
            AI.ReverseThrustUntilStopped(elapsedTime);
            AI.RotateTowardsPosition(AI.Owner.Center + dir, elapsedTime, 0.02f);

        }
    }
}
