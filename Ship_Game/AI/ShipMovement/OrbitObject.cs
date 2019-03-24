using Microsoft.Xna.Framework;
using Ship_Game.Ships;
using System;

namespace Ship_Game.AI.ShipMovement
{
    internal class OrbitObject : ShipAIMovement
    {
        public Vector2 OrbitPos { get; private set; }
        float OrbitalAngle;
        const float OrbitalSpeedLimit = 500f;
        public enum OrbitDirection { Left, Right }

        public OrbitObject(ShipAI ai) : base(ai)
        {
            AI = ai;
        }

        internal void UpdateOrbitPos(Vector2 orbitAround, float orbitRadius, OrbitDirection direction, float orbitAccuracy)
        {

            OrbitPos = orbitAround.PointOnCircle(OrbitalAngle, orbitRadius);
            //These calculations are to check if the orbit target is behind the ship
            //This prevents the ship from chasing an orbit position that it cant reach due to the orbit target
            //speed being faster or the same speed as the ship.
            //also helps get a smoother orbit start on planets.
            Owner.RotationNeededForTarget(OrbitPos, 0f, out float angleToOrbitPos, out float rotationDirToOrbitPos);
            Owner.RotationNeededForTarget(orbitAround, 0f, out float angleToTarget, out float rotationDirToTarget);

            bool inSideNewOrbitPoint = Owner.Center.Distance(OrbitPos) < orbitAccuracy;

            if (inSideNewOrbitPoint || angleToTarget > 2 && angleToOrbitPos < 1.25f)
            {
                float deltaAngle = direction == OrbitDirection.Left ? -15f : +15f;
                OrbitalAngle = (OrbitalAngle + deltaAngle).NormalizedAngle();

                OrbitPos = orbitAround.PointOnCircle(OrbitalAngle, orbitRadius);
            }

            if (Empire.Universe.SelectedShip == Owner)
            {
                Empire.Universe.DebugWin?.DrawCircle(Debug.DebugModes.PathFinder, OrbitPos, orbitAccuracy, .5f);
                Empire.Universe.DebugWin?.DrawCircle(Debug.DebugModes.PathFinder, orbitAround, orbitRadius, 1);
            }
        }

        // orbit around a planet
        internal void Orbit(Planet orbitTarget, float elapsedTime, OrbitDirection orbitDirection = OrbitDirection.Right)
        {
            if (Owner.velocityMaximum < 1 || Owner.EnginesKnockedOut)
                return;

            float distance = orbitTarget.Center.Distance(Owner.Center);
            if (distance > 15000f)
            {
                AI.ThrustOrWarpToPosCorrected(orbitTarget.Center, elapsedTime);
                OrbitPos = orbitTarget.Center;
                return;
            }

            if (Owner.engineState == Ship.MoveState.Warp && distance < 7500f) // exit warp if we're getting close
                Owner.HyperspaceReturn();

            float radius = orbitTarget.ObjectRadius + Owner.Radius;

            //This sets the threshold to where a new Orbit point is created.
            float orbitAccuracy = Owner.Velocity.Length() + Owner.Radius * 2;

            UpdateOrbitPos(orbitTarget.Center, radius, OrbitDirection.Right, orbitAccuracy);

            // precision move, this fixes uneven thrusting while orbiting
            float precisionSpeed = Owner.velocityMaximum;

            // We are within orbit radius, so do actual orbiting:
            if (OrbitPos.Distance(Owner.Center) <= radius * 1.2f)
            {
                if (AI.State != AIState.Bombard)
                    AI.HasPriorityOrder = false;

                precisionSpeed = Math.Min(Owner.velocityMaximum, 200);
                Vector2 direction = Owner.Center.DirectionToTarget(OrbitPos);
                AI.RotateToDirection(direction, elapsedTime, 0.01f);
                Owner.SubLightAccelerate(elapsedTime, precisionSpeed);
                Owner.RestoreYBankRotation();

                if (AI.State != AIState.Bombard)
                    AI.HasPriorityOrder = false;
            }
            else // we are still not there yet, so find a meaningful orbit position
            {
                if (distance < radius * 2) //We are very near the planet start to slow down.
                {
                    float minVelocity = Math.Min(200, Owner.velocityMaximum);
                    precisionSpeed *= distance / (distance + radius * 2);
                    precisionSpeed = precisionSpeed.Clamped(minVelocity, Owner.velocityMaximum);
                }

                AI.ThrustOrWarpToPosCorrected(OrbitPos, elapsedTime, precisionSpeed);
            }
        }

        internal void Orbit(Ship ship, float elapsedTime, OrbitDirection direction = OrbitDirection.Right)
        {
            float accuracy = (Owner.Velocity.Length() + ship.Velocity.Length()) * 3;
            Vector2 predictedSpot = AI.PredictThrustPosition(ship.Center);
            UpdateOrbitPos(ship.Center, 1500f, direction, accuracy);
            AI.ThrustOrWarpToPosCorrected(predictedSpot, elapsedTime, OrbitalSpeedLimit);
        }
    }
}
