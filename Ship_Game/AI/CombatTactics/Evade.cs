using Vector2 = SDGraphics.Vector2;

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
                if (ship.Ship.Loyalty == Owner.Loyalty ||
                    !ship.Ship.Loyalty.isFaction && !Owner.Loyalty.IsAtWarWith(ship.Ship.Loyalty))
                    continue;
                avgDir += Owner.Position.DirectionToTarget(ship.Ship.Position);
                count += 1;
            }
            if (count != 0)
            {
                avgDir /= count;
                Vector2 evadeOffset = avgDir.Normalized() * -7500f;
                AI.ThrustOrWarpToPos(Owner.Position + evadeOffset, timeStep);

            }
        }
    }
}
