

using Microsoft.Xna.Framework;
using Ship_Game.AI.ShipMovement;

namespace Ship_Game.AI.CombatTactics
{
    internal sealed class OrbitTarget : ShipAIPlan
    {
        readonly OrbitObject.OrbitDirection OrbitDirection;
        readonly OrbitObject Orbit;
        public OrbitTarget(ShipAI ai, OrbitObject.OrbitDirection direction ) : base(ai)
        {
            OrbitDirection = direction;
            Orbit = new OrbitObject(ai);
        }
        public override void Execute(float elapsedTime, ShipAI.ShipGoal g)
        {
            float radius = AI.Owner.MaxWeaponRange *.8f - AI.Owner.Radius - AI.Target.Radius;
            float velocityCheck = (Owner.Velocity.Length() + AI.Target.Velocity.Length()) * 3 ;
            Vector2 predictedOrbitPoint = AI.Owner.PredictImpact(AI.Target);
            Orbit.UpdateOrbitPos(predictedOrbitPoint, radius, OrbitDirection, velocityCheck);
            AI.ThrustOrWarpToPosCorrected(Orbit.OrbitPos, elapsedTime);
        }
    }
}
