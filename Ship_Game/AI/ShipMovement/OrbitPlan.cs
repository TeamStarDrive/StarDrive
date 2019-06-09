using Microsoft.Xna.Framework;
using Ship_Game.Ships;
using System;

namespace Ship_Game.AI.ShipMovement
{
    internal class OrbitPlan : ShipAIPlan
    {
        public enum OrbitDirection { Left, Right }
        protected readonly OrbitDirection Direction;

        Vector2 OrbitOffset = Vectors.Up * 1000f;
        public Vector2 OrbitPos { get; private set; }

        const float OrbitUpdateInterval = 3f;
        float OrbitUpdateTimer;

        const float WayPointProximity = 500f;
        const float OrbitalSpeedLimit = 300f;
        const float OrbitLeft  = RadMath.PI / -36; // 5 degree steps
        const float OrbitRight = RadMath.PI / +36;

        public OrbitPlan(ShipAI ai, OrbitDirection direction = OrbitDirection.Right) : base(ai)
        {
            Direction = direction;
        }

        public override void Execute(float elapsedTime, ShipAI.ShipGoal g)
        {
        }

        protected void UpdateOrbitPos(Vector2 orbitAround, float orbitRadius, float elapsedTime)
        {
            // we use a timer here, because proximity update was surprisingly much worse
            // this approach is very fast and simple
            OrbitUpdateTimer -= elapsedTime;
            if (OrbitUpdateTimer <= 0f || Owner.Center.InRadius(OrbitPos, WayPointProximity))
            {
                OrbitUpdateTimer = OrbitUpdateInterval;

                float deltaAngle = (Direction == OrbitDirection.Left ? OrbitLeft : OrbitRight);
                OrbitOffset = RadMath.OrbitalOffsetRotate(OrbitOffset, orbitRadius, deltaAngle);
            }

            // always update the OrbitPos, because orbitAround is typically moving around
            OrbitPos = orbitAround + OrbitOffset;

            if (Empire.Universe.SelectedShip == Owner)
            {
                Empire.Universe.DebugWin?.DrawCircle(Debug.DebugModes.PathFinder, OrbitPos, WayPointProximity, 0.5f);
                Empire.Universe.DebugWin?.DrawCircle(Debug.DebugModes.PathFinder, orbitAround, orbitRadius, 1.0f);
            }
        }

        // orbit around a planet
        public void Orbit(Planet orbitTarget, float elapsedTime)
        {
            if (Owner.velocityMaximum < 1 || Owner.EnginesKnockedOut)
                return;
          
            float radius = orbitTarget.ObjectRadius + Owner.Radius;
            float distance = orbitTarget.Center.Distance(Owner.Center);

            if (distance > 15000f) // we are still far away, thrust towards the planet
            {
                AI.ThrustOrWarpToPosCorrected(orbitTarget.Center, elapsedTime);
                OrbitPos = orbitTarget.Center + OrbitOffset;
                OrbitUpdateTimer = 0f;
                return;
            }

            // we are getting close, exit hyperspace
            if (Owner.engineState == Ship.MoveState.Warp && distance < 7500f)
                Owner.HyperspaceReturn();

            // if no enemies near us, then consider the following MAGIC STOP optimization:
            if (!AI.BadGuysNear)
            {
                bool visible = orbitTarget.ParentSystem.isVisible
                               && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView;
                if (!visible) // don't update orbits in invisible systems
                {
                    // MAGIC STOP ships when orbiting off screen
                    Owner.Velocity = Vector2.Zero;
                    OrbitPos = orbitTarget.Center + OrbitOffset;
                    Owner.Center = Owner.Position = OrbitPos;
                    return;
                }
            }

            UpdateOrbitPos(orbitTarget.Center, radius, elapsedTime);

            // precision move, this fixes uneven thrusting while orbiting
            float maxVel = (float)Math.Floor(Owner.velocityMaximum*0.95f);
            float precisionSpeed = Math.Min(OrbitalSpeedLimit, maxVel);

            // We are within orbit radius, so do actual orbiting:
            if (Owner.Center.InRadius(OrbitPos, radius * 1.2f))
            {
                AI.RotateTowardsPosition(OrbitPos, elapsedTime, 0.01f);
                Owner.SubLightAccelerate(elapsedTime, precisionSpeed);
                Owner.RestoreYBankRotation();
            }
            else // we are still not there yet, so find a meaningful orbit position
            {
                if (distance < radius*2) // We are very near the planet start to slow down.
                {
                    precisionSpeed *= distance / (radius * 2);
                    precisionSpeed = Math.Max(precisionSpeed, 100);
                }

                AI.ThrustOrWarpToPosCorrected(OrbitPos, elapsedTime, precisionSpeed);
            }
        }

        public void Orbit(Ship ship, float elapsedTime)
        {
            UpdateOrbitPos(ship.Center, 1500f, elapsedTime);
            AI.ThrustOrWarpToPosCorrected(OrbitPos, elapsedTime, OrbitalSpeedLimit);
        }
    }
}
