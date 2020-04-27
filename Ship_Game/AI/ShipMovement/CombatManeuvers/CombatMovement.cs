using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Debug;
using Ship_Game.Ships;

namespace Ship_Game.AI.ShipMovement.CombatManeuvers
{
    internal abstract class CombatMovement : ShipAIPlan
    {
        [Flags]
        public enum ChaseState
        {
            None = 0,
            WeAreChasing = 1,
            TheyAreChasing = 2,
            CantCatch = 4,
            ChasingCantCatch = WeAreChasing | CantCatch
        }

        public enum CombatMoveState
        {
            None,
            Error,
            Disengage,
            Hold,
            Face,
            Maintain,
            Retrograde,
            Approach,
            Flank,
            OrbitInjection
        }

        protected CombatMoveState MoveState = CombatMoveState.None;
        // direction offset from target ship, so our attack runs go over the side of the enemy ship
        int DebugTextIndex;
        protected float DistanceToTarget;
        protected Vector2 TargetQuadrant = Vectors.Right;
        protected Vector2 ZigZag;
        protected float SpacerDistance;
        protected float RadiansToTargetByOurVelocity;
        protected float RadiansToUsByTargetVelocity;
        protected float RadiansDifferenceToTargetFacingAndDirection;
        protected float RadiansDifferenceToUsFacingAndDirection;
        protected float RadiansDifferenceToOurFacingAndVelocity;
        protected float DesiredCombatRange;
        Vector2 DisengageDirection;
        //AttackPosition OwnerTarget;
        
        protected bool WeAreFacingThem => RadiansDifferenceToTargetFacingAndDirection > 0.7f;
        protected Ship OwnerTarget           => AI.Target;

        protected bool WeAreChasingAndCantCatchThem => ChaseStates.HasFlag(ChaseState.ChasingCantCatch) && !WeAreRetrograding;
        protected bool WeAreRetrograding => RadiansDifferenceToOurFacingAndVelocity < 0;

        protected  ChaseState ChaseStates;
        protected CombatMovement(ShipAI ai) : base(ai)
        {
            DesiredCombatRange = ai.Owner.DesiredCombatRange;
           // OwnerTarget = new AttackPosition(target: ai.Target, owner: ai.Owner);
        }

        protected abstract CombatMoveState ExecuteAttack(float elapsedTime);
        protected abstract void OverrideCombatValues(float elapsedTime);

        public override void Execute(float elapsedTime, ShipAI.ShipGoal goal)
        {
            // Bail on invalid combat situations. 
            if (Owner.IsPlatformOrStation || OwnerTarget == null || (Owner.AI.HasPriorityOrder && !Owner.AI.HasPriorityTarget)) 
                return;

            Initialize(DesiredCombatRange);
            OverrideCombatValues(elapsedTime);

            if (MoveState == CombatMoveState.Approach && OwnerTarget.AI.Target == Owner && CantGetInRange()) 
            {
                DisengageDirection = RandomFlankTo(Owner.Center.DirectionToTarget(OwnerTarget.Center));
            }
            else if (DisengageDirection != Vector2.Zero)
            {
                ExecuteAntiChaseDisengage(elapsedTime);
            }
            else
            {
                MoveState = ExecuteAttack(elapsedTime);
            }

            if (Empire.Universe.Debug && Empire.Universe.SelectedShip != null)
            {
                DrawDebugText($"Chase: {ChaseStates}");
                DrawDebugText($"MoveState: { MoveState }");
                DrawDebugText($"Velocity Radians To Target: R {(int)(RadiansToTargetByOurVelocity * 100)}");
                DrawDebugText($"Facing Radians To Target: R {(int)(RadiansDifferenceToTargetFacingAndDirection * 100)}");
            }
        }

        protected virtual bool CantGetInRange()
        {
            if (WeAreChasingAndCantCatchThem && MoveState != CombatMoveState.Disengage && !Owner.AI.HasPriorityTarget)
            {
                MoveState = CombatMoveState.Disengage;
                return true;
            }
            return false;
        }

        protected virtual void ExecuteAntiChaseDisengage(float elapsedTime)
        {
            Owner.AI.SubLightContinuousMoveInDirection(DisengageDirection, elapsedTime);
        }

        void Initialize(float desiredWeaponsRange)
        {
            if (DistanceToTarget > desiredWeaponsRange)
            {
                if (DistanceToTarget < OwnerTarget.DesiredCombatRange)
                {
                    if (DisengageDirection == Vector2.Zero || OwnerTarget.AI.Target != Owner || OwnerTarget.CombatDisabled)
                    {
                        MoveState = CombatMoveState.Approach;
                    }
                    // if disengage direction was set the ship is disengaging and so we need to set that. 
                    else
                    {
                        MoveState = CombatMoveState.Disengage;
                    }
                }
                else
                {
                    DisengageDirection = Vector2.Zero;
                    MoveState = CombatMoveState.Approach;
                }
            }
            else
            {
                MoveState = CombatMoveState.None;
                DisengageDirection = Vector2.Zero;
            }

            DistanceToTarget = Owner.Center.Distance(OwnerTarget.Center);
            SpacerDistance = Owner.Radius + AI.Target.Radius;

            DebugTextIndex                              = 0;

            RadiansToTargetByOurVelocity                = 
                CompareVelocityToDirection(OwnerTarget.VelocityDirection, Owner.Direction);

            RadiansToUsByTargetVelocity                 = 
                CompareVelocityToDirection(Owner.VelocityDirection, OwnerTarget.Direction);

            RadiansDifferenceToTargetFacingAndDirection =
                CompareVelocityToDirection(Owner.Center.DirectionToTarget(OwnerTarget.Center), Owner.Direction);

            RadiansDifferenceToUsFacingAndDirection     =
                CompareVelocityToDirection(OwnerTarget.Center.DirectionToTarget(Owner.Center), OwnerTarget.Direction);

            RadiansDifferenceToOurFacingAndVelocity     =
                CompareVelocityToDirection(Owner.VelocityDirection, Owner.Direction);

            DistanceToTarget = Owner.Center.Distance(OwnerTarget.Center);
            ChaseStates      = ChaseStatus(DistanceToTarget);
        }

        protected float CompareVelocityToDirection(Vector2 velocityDirection, Vector2 direction)
        {
            if (OwnerTarget == null) return 0;
            return direction.Dot(velocityDirection);
        }
        protected Vector2 RandomFlankTo(Vector2 direction) => RandomMath.RollDice(50) ? direction.LeftVector() : direction.RightVector();

        protected bool WeCantCatchThem(float distanceToTarget)
        {
            return OwnerTarget.CurrentVelocity > Owner.VelocityMaximum && distanceToTarget > Owner.DesiredCombatRange;
        }
        protected bool TheyCantCatchUs(float distanceToTarget)
        {
            return Owner.CurrentVelocity > OwnerTarget.VelocityMaximum && distanceToTarget > Owner.DesiredCombatRange;
        }

        protected ChaseState ChaseStatus(float distance)
        {
            ChaseState chase = ChaseState.None;

            if (RadiansToTargetByOurVelocity > -0.1f && RadiansDifferenceToTargetFacingAndDirection > 0.7f && !WeAreRetrograding) 
            {
                chase |= ChaseState.WeAreChasing;;
                if (WeCantCatchThem(distance)) chase |= ChaseState.CantCatch;
            }
            else if (RadiansToUsByTargetVelocity > -0.1f && RadiansDifferenceToUsFacingAndDirection > 0.7f)
            {
                chase |= ChaseState.TheyAreChasing;
                if (TheyCantCatchUs(distance)) chase |= ChaseState.CantCatch;
            }
            return chase;
        }

        public void ErraticMovement(float distanceToTarget)
        {
            if (AI.IsFiringAtMainTarget)
            {
                ZigZag = Vector2.Zero;
            }

            if (Owner.IsTurning)
            {
                return;
            }

            int rng = RandomMath.RollAvgPercentVarianceFrom50();
            int racialMod = Owner.loyalty.data.Traits.PhysicalTraitPonderous ? -1 : 0;
            racialMod += Owner.loyalty.data.Traits.PhysicalTraitReflexes ? 1 : 0;
            float mod = 5 * (Owner.RotationRadiansPerSecond * (Owner.Level + racialMod)).Clamped(0, 10);

            if (rng < 25 - mod)
            {
                ZigZag = Vector2.Zero;
            }
            else
            {
                if (ZigZag != Vector2.Zero)
                    return;

                Vector2 dir = Owner.Center.DirectionToTarget(AI.Target.Center);
                if (RandomMath.IntBetween(0, 1) == 1)
                {
                    ZigZag = dir.RightVector() * 1000;
                }
                else
                {
                    ZigZag = dir.LeftVector() * 1000;
                }
            }
        }

        public void DrawDebugTarget(Vector2 pip, float radius)
        {
            if (DebugInfoScreen.Mode == DebugModes.Targeting &&
                Empire.Universe.DebugWin?.Visible == true &&
                Empire.Universe.SelectedShip == Owner)
            {
                Empire.Universe.DebugWin?.DrawCircle(DebugModes.Targeting, pip, radius, Owner.loyalty.EmpireColor, 0f);
                Empire.Universe.DebugWin?.DrawLine(DebugModes.Targeting, AI.Target.Center, pip, 1f, Owner.loyalty.EmpireColor, 0f);
            }
        }

        public void DrawDebugText(string text)
        {
            if (DebugInfoScreen.Mode == DebugModes.Targeting &&
                Empire.Universe.DebugWin?.Visible == true &&
                Empire.Universe.SelectedShip == Owner)
            {
                DebugTextIndex++;
                Empire.Universe.DebugWin?.DrawText(DebugModes.Targeting,
                    Owner.Center + new Vector2(Owner.Radius, Owner.Radius + 50 * DebugTextIndex), text, Color.Red, 0f);
            }
        }
    }
}