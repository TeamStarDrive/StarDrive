using Ship_Game.Ships;
using System;
using SDGraphics;
using Ship_Game.AI.ShipMovement.CombatManeuvers;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI.ShipMovement
{
    internal class OrbitPlan : CombatMovement
    {
        public enum OrbitDirection { Left, Right }
        protected readonly OrbitDirection Direction;

        Vector2 OrbitOffset = Vectors.Up * 1000f;
        public Vector2 OrbitPos { get; private set; }
        public bool InOrbit { get; private set; }

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

        protected override CombatMoveState ExecuteAttack(FixedSimTime timeStep)
        {
            return CombatMoveState.Error;
        }

        protected override void OverrideCombatValues(FixedSimTime timeStep)
        {
        }

        public void ForceOrbitOffSet(Vector2 position)
        {
            OrbitOffset = position;
        }

        protected void UpdateOrbitPos(Vector2 orbitAround, float orbitRadius, FixedSimTime timeStep)
        {
            // we use a timer here, because proximity update was surprisingly much worse
            // this approach is very fast and simple

            OrbitUpdateTimer -= timeStep.FixedTime;
            if (OrbitUpdateTimer <= 0f || Owner.Position.InRadius(OrbitPos, WayPointProximity))
            {
                OrbitUpdateTimer = OrbitUpdateInterval;

                float deltaAngle = (Direction == OrbitDirection.Left ? OrbitLeft : OrbitRight);
                OrbitOffset = RadMath.OrbitalOffsetRotate(OrbitOffset, orbitRadius, deltaAngle);
            }

            // always update the OrbitPos, because orbitAround is typically moving around
            OrbitPos = orbitAround + OrbitOffset;

            if (Owner.Universe.Screen.SelectedShip == Owner)
            {
                Owner.Universe.DebugWin?.DrawCircle(Debug.DebugModes.PathFinder, OrbitPos, WayPointProximity, 0.5f);
                Owner.Universe.DebugWin?.DrawCircle(Debug.DebugModes.PathFinder, orbitAround, orbitRadius, 1.0f);
            }
        }

        // orbit around a planet
        public void Orbit(Planet orbitTarget, FixedSimTime timeStep)
        {
            InOrbit = false;
            if (Owner.VelocityMax < 1 || Owner.EnginesKnockedOut)
                return;

            if (orbitTarget == null)
            {
                AI.OrderHoldPosition();
                return;
            }
          
            float radius = (orbitTarget.ObjectRadius*2 + Owner.Radius).UpperBound(12000);
            float distance = orbitTarget.Position.Distance(Owner.Position);

            if (distance > 15000f) // we are still far away, thrust towards the planet
            {
                ThrustTowardsPlanet(orbitTarget, timeStep);
                return;
            }

            // we are getting close, exit hyperspace
            if (Owner.engineState == Ship.MoveState.Warp && distance < 7500f)
            {
                Owner.HyperspaceReturn();
                Owner.AI.ClearWayPoints(); // Especially needed for player freighters (check `IsIdleFreighter`)
            }

            // if no enemies near us, then consider the following MAGIC STOP optimization:
            if (!AI.BadGuysNear)
            {
                bool visible = orbitTarget.ParentSystem.IsVisible
                               && Owner.Universe.Screen.IsSystemViewOrCloser;
                if (!visible) // don't update orbits in invisible systems
                {
                    if (distance > 2000f) // we need to get closer to get RESUPPLIED (!)
                    {
                        ThrustTowardsPlanet(orbitTarget, timeStep);
                    }
                    else
                    {
                        // MAGIC STOP ships when orbiting off screen
                        Owner.Velocity = Vector2.Zero;
                        InOrbit = true;
                    }
                    return;
                }
            }

            UpdateOrbitPos(orbitTarget.Position, radius, timeStep);

            // precision move, this fixes uneven thrusting while orbiting
            float maxVel = (float)Math.Floor(Owner.VelocityMax*0.95f);
            float precisionSpeed = Math.Min(OrbitalSpeedLimit, maxVel);

            // We are within orbit radius, so do actual orbiting:
            if (Owner.Position.InRadius(OrbitPos, radius * 1.2f))
            {
                InOrbit = true;
                AI.RotateTowardsPosition(OrbitPos, timeStep, 0.01f);
                Owner.SubLightAccelerate(speedLimit: precisionSpeed);
            }
            else // we are still not there yet, so find a meaningful orbit position
            {
                if (distance < radius*2) // We are very near the planet start to slow down.
                {
                    precisionSpeed *= distance / (radius * 2);
                    precisionSpeed = Math.Max(precisionSpeed, 100);
                }

                AI.ThrustOrWarpToPos(OrbitPos, timeStep, precisionSpeed);
            }
        }

        public void Orbit(Ship ship, FixedSimTime timeStep)
        {
            UpdateOrbitPos(ship.Position, 1500f, timeStep);
            AI.ThrustOrWarpToPos(OrbitPos, timeStep, OrbitalSpeedLimit);
        }

        void ThrustTowardsPlanet(Planet orbitTarget, FixedSimTime timeStep)
        {
            AI.ThrustOrWarpToPos(orbitTarget.Position, timeStep);
            OrbitPos = orbitTarget.Position + OrbitOffset;
            OrbitUpdateTimer = 0f;
        }
    }
}
