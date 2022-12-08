using Ship_Game.ExtensionMethods;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI.CombatTactics
{
    internal sealed class HoldPosition : ShipAIPlan
    {
        GameObject Target;
        public HoldPosition(ShipAI ai) : base(ai)
        {
        }
        public override void Execute(FixedSimTime timeStep, ShipAI.ShipGoal g)
        {
            Target = AI.Target;
            AI.ReverseThrustUntilStopped(timeStep);
            Vector2 interceptPoint = Owner.PredictImpact(Target);
            AI.RotateTowardsPosition(interceptPoint, timeStep, 0.2f);
        }
    }
}
