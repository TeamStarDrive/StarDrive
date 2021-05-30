using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI.ShipMovement;
using Ship_Game.AI.ShipMovement.CombatManeuvers;

namespace Ship_Game.AI.CombatTactics
{
    internal sealed class OrbitTarget : OrbitPlan
    {
        public OrbitTarget(ShipAI ai, OrbitDirection direction) : base(ai, direction)
        {
            SetInitialOrbitEntryPoint();
        }

        /// <summary>
        /// Creates a point that allows a turn friendly orbit injection point. 
        /// </summary>
        Vector2 SetInitialOrbitEntryPoint()
        {
            Vector2 initialOffset = Vectors.Up * DesiredCombatRange;
            if (OwnerTarget != null)
            {
                Vector2 initialDirection = OwnerTarget.Center.DirectionToTarget(Owner.Center);
                initialOffset = initialDirection * DesiredCombatRange;

                // desiredCombatRange * 0.25... need a turn friendly value here. 
                if (Direction == OrbitDirection.Left)
                    initialOffset += initialDirection.LeftVector() * (DesiredCombatRange * 0.25f);
                else
                    initialOffset += initialDirection.RightVector() * (DesiredCombatRange * 0.25f);

                ForceOrbitOffSet(initialOffset);
            }
            return initialOffset;
        }

        protected override void OverrideCombatValues(FixedSimTime timeStep)
        {
            DesiredCombatRange = AI.Owner.DesiredCombatRange;//  - AI.Owner.Radius - AI.Target.Radius;
        }

        protected override CombatMoveState ExecuteAttack(FixedSimTime timeStep)
        {
            CombatMoveState moveState = CombatMoveState.Error;

            if (DistanceToTarget > DesiredCombatRange + Owner.Radius)
            {
                Vector2 initialMovePoint = OwnerTarget.Center + SetInitialOrbitEntryPoint();
                AI.SubLightMoveTowardsPosition(initialMovePoint, timeStep);
                moveState = CombatMoveState.Approach;
            }
            else
            {
                Vector2 predictedTarget = AI.Owner.PredictImpact(AI.Target);
                
                UpdateOrbitPos(predictedTarget, DesiredCombatRange, timeStep);

                // speed 300... the speed should some calculation based on rotationPerSec and max Velocity compared to angle of orbit. 
                // 300 is a safe limit. ships wont get stuck spinning a point that they cant turn to. 
                AI.ThrustOrWarpToPos(OrbitPos, timeStep, 300);
                moveState = CombatMoveState.OrbitInjection;
            }

            return moveState;
        }
    }
}
