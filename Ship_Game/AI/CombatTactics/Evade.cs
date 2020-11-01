using Microsoft.Xna.Framework;

namespace Ship_Game.AI.CombatTactics
{
    internal sealed class Evade : ShipAIPlan
    {
        public Evade(ShipAI ai) : base(ai)
        {
        }
        public override void Execute(FixedSimTime timeStep, ShipAI.ShipGoal g)
        {
            Vector2 avgDir = Vector2.Zero;
            int count = 0;
            foreach (ShipAI.ShipWeight ship in AI.NearByShips)
            {
                if (ship.Ship.loyalty == Owner.loyalty ||
                    !ship.Ship.loyalty.isFaction && !Owner.loyalty.IsAtWarWith(ship.Ship.loyalty))
                    continue;
                avgDir += Owner.Center.DirectionToTarget(ship.Ship.Center);
                count += 1;
            }
            if (count != 0)
            {
                avgDir /= count;
                Vector2 evadeOffset = avgDir.Normalized() * -7500f;
                AI.ThrustOrWarpToPos(Owner.Center + evadeOffset, timeStep);

            }
        }
    }
}
