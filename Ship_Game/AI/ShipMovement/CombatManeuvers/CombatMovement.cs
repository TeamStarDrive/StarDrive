using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Debug;
using Ship_Game.Ships;

namespace Ship_Game.AI.ShipMovement.CombatManeuvers
{
    /// <summary>
    /// This is a movement layer that lays under the specific combat stances.
    /// This allows common movement routines and behaviors between all stances.
    /// The execute method is called and this calls the stances combatexecute.
    /// </summary>
    internal abstract class CombatMovement : ShipAIPlan
    {
        /// <summary>
        /// describes the state of how ships are moving in relation to their targets. 
        /// </summary>
        [Flags]
        public enum ChaseState
        {
            None             = 0,
            WeAreChasing     = 1 << 0,
            TheyAreChasing   = 1 << 1,
            CantCatch        = 1 << 2,
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
        protected Vector2 DirectionToTarget;
        protected Vector2 TargetQuadrant = Vectors.Right;
        protected Vector2 ZigZag;
        protected float SpacerDistance;
        protected float DeltaOurFacingTheirMovement;
        protected float DeltaOurMovmentTheirFacing;
        protected float DeltaDirectionToTargetAndOurFacing;
        protected float DeltaTargetDirectionToUsAndOurFacing;
        protected float DeltaOurFacingAndOurMovement;
        protected float DeltaOurMovementToTheirMovement;
        protected float DeltaOurMovementToOurDirectionToTarget;
        protected float DesiredCombatRange;
        protected bool OutSideDesiredWeaponsRang;
        Vector2 DisengageDirection;
        protected float ErraticTimer =0;
        private float ThinkTimer =0;
        
        protected bool WeAreFacingThem => DeltaDirectionToTargetAndOurFacing > 0.7f;
        protected Ship OwnerTarget           => AI.Target;

        protected bool WeAreChasingAndCantCatchThem => ChaseStates.HasFlag(ChaseState.ChasingCantCatch) && !WeAreRetrograding;
        protected bool WeAreRetrograding => DeltaOurFacingAndOurMovement < 0;

        protected  ChaseState ChaseStates;
        protected CombatMovement(ShipAI ai) : base(ai)
        {
            DesiredCombatRange = ai.Owner.DesiredCombatRange;
           // OwnerTarget = new AttackPosition(target: ai.Target, owner: ai.Owner);
        }

        /// <summary>
        /// Executes the attack from combat stance
        /// </summary>
        protected abstract CombatMoveState ExecuteAttack(float elapsedTime);

        /// <summary>
        /// Allows the stance to change the default combat parameters in the Initialize method. 
        /// </summary>
        protected abstract void OverrideCombatValues(float elapsedTime);

        /// <summary>
        /// Executes combat movement.
        /// calls initialize/override, chase behavior, and combat stance movement. 
        /// </summary>
        public override void Execute(float elapsedTime, ShipAI.ShipGoal goal)
        {
            // Bail on invalid combat situations. 
            if (Owner.IsPlatformOrStation || OwnerTarget == null || (Owner.AI.HasPriorityOrder && !Owner.AI.HasPriorityTarget) 
                || Owner.engineState == Ship.MoveState.Warp) 
                return;

            if (ThinkTimer.LessOrEqual(0))
            {
                ThinkTimer = 1f / Owner.Level.LowerBound(1);
                Initialize(DesiredCombatRange);
                OverrideCombatValues(elapsedTime);
            }

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
                ErraticMovement(DistanceToTarget, elapsedTime);
                MoveState = ExecuteAttack(elapsedTime);
            }

            if (Empire.Universe.Debug && Empire.Universe.SelectedShip != null)
            {
                DrawDebugText($"Chase: {ChaseStates}");
                DrawDebugText($"MoveState: { MoveState }");
                DrawDebugText($"Movement angle To Target: {(int)DeltaOurFacingTheirMovement}");
                DrawDebugText($"Facing angle To Target  : {(int)DeltaDirectionToTargetAndOurFacing}");
                DrawDebugText($"Movement angle To Target Movement  : {(int)DeltaOurMovementToTheirMovement}");
                DrawDebugText($"Movement angle to their facing  : {(int)DeltaOurMovmentTheirFacing}");
            }
        }

        /// <summary>
        /// Returns true if too slow to get into combat range.
        /// can be overriden. 
        /// </summary>
        protected virtual bool CantGetInRange()
        {
            if (WeAreChasingAndCantCatchThem && MoveState != CombatMoveState.Disengage) // && !Owner.AI.HasPriorityTarget)
            {
                MoveState = CombatMoveState.Disengage;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Executes the anti chase disengage. Standard behavior is to move to disengage position.
        /// </summary>
        protected virtual void ExecuteAntiChaseDisengage(float elapsedTime)
        {
            Owner.AI.SubLightContinuousMoveInDirection(DisengageDirection, elapsedTime);
        }

        /// <summary>
        /// Initializes basic combat movement parameters. Chase status. distance to target.
        /// ship target orientation.
        /// </summary>
        void Initialize(float desiredWeaponsRange)
        {
            DistanceToTarget = Owner.Center.Distance(OwnerTarget.Center);
            if (DistanceToTarget > desiredWeaponsRange)
            {
                if (DistanceToTarget < OwnerTarget.WeaponsMaxRange * 2)
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

            
            SpacerDistance = Owner.Radius + AI.Target.Radius;

            DebugTextIndex                              = 0;
            DirectionToTarget = Owner.Center.DirectionToTarget(OwnerTarget.Center);

            DeltaOurFacingTheirMovement                = 
                DegreeDifferenceBetweenVectorDirection(OwnerTarget.VelocityDirection, Owner.Direction);

            DeltaOurMovmentTheirFacing                 = 
                DegreeDifferenceBetweenVectorDirection(Owner.VelocityDirection, OwnerTarget.Direction);

            DeltaOurMovementToTheirMovement =
                DegreeDifferenceBetweenVectorDirection(Owner.VelocityDirection, OwnerTarget.VelocityDirection);

            DeltaDirectionToTargetAndOurFacing =
                DegreeDifferenceBetweenVectorDirection(DirectionToTarget, Owner.Direction);

            DeltaTargetDirectionToUsAndOurFacing     =
                DegreeDifferenceBetweenVectorDirection(OwnerTarget.Center.DirectionToTarget(Owner.Center), OwnerTarget.Direction);

            DeltaOurFacingAndOurMovement     =
                DegreeDifferenceBetweenVectorDirection(Owner.VelocityDirection, Owner.Direction);

            DeltaOurMovementToOurDirectionToTarget =
                DegreeDifferenceBetweenVectorDirection(Owner.VelocityDirection, DirectionToTarget);

            DistanceToTarget = Owner.Center.Distance(OwnerTarget.Center);

            ChaseStates = ChaseStatus(DistanceToTarget);
            OutSideDesiredWeaponsRang = DistanceToTarget > Owner.DesiredCombatRange;
        }

        protected float DegreeDifferenceBetweenVectorDirection(Vector2 velocityDirection, Vector2 direction)
        {
            if (OwnerTarget == null) return 0;
            float dot = direction.Dot(velocityDirection);
            return Math.Abs(dot * 90 - 90);
        }
        protected Vector2 RandomFlankTo(Vector2 direction) => RandomMath.RollDice(50) ? direction.LeftVector() : direction.RightVector();

        protected ChaseState CanCatchState(ChaseState chaseState, float distance)
        {
            if (chaseState.HasFlag(ChaseState.WeAreChasing))
                return WeCantCatchThem(distance) ? ChaseState.CantCatch : ChaseState.None;
            
            if (ChaseStates.HasFlag(ChaseState.TheyAreChasing))
                return WeCantCatchThem(distance) ? ChaseState.CantCatch : ChaseState.None;
            return ChaseState.None;
        }

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

            if (DeltaOurMovementToTheirMovement > 45f)        return ChaseState.None;

            if (DeltaOurMovementToOurDirectionToTarget < 20)  chase |= ChaseState.WeAreChasing;
            else
            if (DeltaOurMovementToOurDirectionToTarget > 160) chase |= ChaseState.TheyAreChasing;

            chase |= CanCatchState(chase, distance);

            return chase;
        }

        /// <summary>
        /// TO BE EXPANDED. be a harder target on approach. 
        /// </summary>
        public void ErraticMovement(float distanceToTarget, float deltaTime)
        {
            ErraticTimer -= deltaTime;
            if (AI.IsFiringAtMainTarget)
            {
                ZigZag = Vector2.Zero;
            }

            if (ErraticTimer > 0)
            {
                return;
            }
             
            int rng = RandomMath.RollAvgPercentVarianceFrom50();
            int racialMod = Owner.loyalty.data.Traits.PhysicalTraitPonderous ? -1 : 0;
            racialMod += Owner.loyalty.data.Traits.PhysicalTraitReflexes ? 1 : 0;
            float mod = 5 * (Owner.RotationRadiansPerSecond * (Owner.Level + racialMod)).Clamped(0, 10);

            ErraticTimer = rng * 0.05f;

            if (rng < 25 - mod)
            {
                ZigZag = Vector2.Zero;
            }
            else
            {
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