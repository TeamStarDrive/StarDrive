using Microsoft.Xna.Framework;
using Ship_Game.AI.ShipMovement;

namespace Ship_Game.AI.CombatTactics
{
    internal sealed class OrbitTarget : OrbitPlan
    {
        public OrbitTarget(ShipAI ai, OrbitDirection direction) : base(ai, direction)
        {
        }
        
        public override void Execute(float elapsedTime, ShipAI.ShipGoal g)
        {
            float radius = AI.Owner.MaxWeaponRange * 0.8f - AI.Owner.Radius - AI.Target.Radius;

            Vector2 predictedTarget = AI.Owner.PredictImpact(AI.Target);
            UpdateOrbitPos(predictedTarget, radius, elapsedTime);
            AI.ThrustOrWarpToPosCorrected(OrbitPos, elapsedTime);
        }
    }
}
