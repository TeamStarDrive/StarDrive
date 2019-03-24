

using Microsoft.Xna.Framework;

namespace Ship_Game.AI.CombatTactics
{
    internal sealed class OrbitTarget : ShipAIPlan
    {
        readonly ShipAI.Orbit OrbitDirection;
        public OrbitTarget(ShipAI ai, ShipAI.Orbit direction ) : base(ai)
        {
            OrbitDirection = direction;
        }
        public override void Execute(float elapsedTime, ShipAI.ShipGoal g)
        {
            float radius = AI.Owner.MaxWeaponRange - AI.Owner.Radius - AI.Target.Radius;
            float velocityCheck = (Owner.Velocity.Length() + AI.Target.Velocity.Length()) * 3 ;
            Vector2 predictedOrbitPoint = AI.Owner.PredictImpact(AI.Target);
            AI.UpdateOrbitPos(predictedOrbitPoint, radius, OrbitDirection, velocityCheck);
            AI.ThrustOrWarpToPosCorrected(AI.GetOrbitPos, elapsedTime);
        }
    }
}
