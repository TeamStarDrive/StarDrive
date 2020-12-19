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

        enum DisengageTypes
        {
            None,
            Left,
            Right,
            Away,
            Total
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
        protected float DeltaOurMovementTheirFacing;
        protected float DeltaDirectionToTargetAndOurFacing;
        protected float DeltaTargetDirectionToUsAndTheirFacing;
        protected float DeltaOurFacingAndOurMovement;
        protected float DeltaOurMovementToTheirMovement;
        protected float DeltaOurMovementToOurDirectionToTarget;
        protected float DeltaDirectionToTargetToTheirFacing;
        protected float DesiredCombatRange;
        protected bool OutSideDesiredWeaponsRange;
        Vector2 DisengageDirection;
        DisengageTypes DisengageType; 
        protected float ErraticTimer =0;
        private float ThinkTimer =0;
        
        protected bool WeAreFacingThem => DeltaDirectionToTargetAndOurFacing < 10f;
        protected Ship OwnerTarget           => AI.Target;

        protected bool WeAreChasingAndCantCatchThem => ChaseStates.HasFlag(ChaseState.ChasingCantCatch) && !WeAreRetrograding;

        /// <summary>
        /// If movement direction is greater than 90 degrees different from facing we must be moving backwards
        /// </summary>
        protected bool WeAreRetrograding => DeltaOurFacingAndOurMovement > 90;
        protected bool HeadOnAttack => DeltaOurMovementTheirFacing > 175;

        protected  ChaseState ChaseStates;
        protected CombatMovement(ShipAI ai) : base(ai)
        {
            DesiredCombatRange = ai.Owner.DesiredCombatRange;
            DesiredCombatRange = DesiredCombatRange > 0 ? DesiredCombatRange : Owner.WeaponsMaxRange;
           // OwnerTarget = new AttackPosition(target: ai.Target, owner: ai.Owner);
        }

        /// <summary>
        /// Executes the attack from combat stance
        /// </summary>
        protected abstract CombatMoveState ExecuteAttack(FixedSimTime timeStep);

        /// <summary>
        /// Allows the stance to change the default combat parameters in the Initialize method. 
        /// </summary>
        protected abstract void OverrideCombatValues(FixedSimTime timeStep);

        /// <summary>
        /// Executes combat movement.
        /// calls initialize/override, chase behavior, and combat stance movement. 
        /// </summary>
        public override void Execute(FixedSimTime timeStep, ShipAI.ShipGoal goal)
        {
            // Bail on invalid combat situations. 
            if (Owner.IsPlatformOrStation || OwnerTarget == null || (Owner.AI.HasPriorityOrder && !Owner.AI.HasPriorityTarget) 
                || Owner.engineState == Ship.MoveState.Warp) 
                return;
            // ThinkTimer delays the recalculation of angle differences. 
            ThinkTimer -= timeStep.FixedTime;
            Initialize(DesiredCombatRange);
            OverrideCombatValues(timeStep);

            if (MoveState == CombatMoveState.Approach && ShouldDisengage())
            {
                DisengageType = RandomDisengageType(DirectionToTarget);
                ExecuteAntiChaseDisengage(timeStep);
            }
            else if (DisengageType != DisengageTypes.None)
            {
                ExecuteAntiChaseDisengage(timeStep);
            }
            else
            {
                DisengageType = DisengageTypes.None;
                DisengageDirection = Vector2.Zero;
                ErraticMovement(DistanceToTarget, timeStep);
                MoveState = ExecuteAttack(timeStep);
            }

            if (Empire.Universe.Debug && Empire.Universe.SelectedShip != null)
            {
                DrawDebugText($"Chase: {ChaseStates}");
                DrawDebugText($"MoveState: { MoveState }");
                DrawDebugText($"Movement angle To Target: {(int)DeltaOurFacingTheirMovement}");
                DrawDebugText($"Facing angle To Target  : {(int)DeltaDirectionToTargetAndOurFacing}");
                DrawDebugText($"Movement angle To Target Movement  : {(int)DeltaOurMovementToTheirMovement}");
                DrawDebugText($"Movement angle to their facing  : {(int)DeltaOurMovementTheirFacing}");
            }
        }

        /// <summary>
        /// Returns true if already disengaging or chasing a faster ship. if a player gives the command dont disengage. 
        /// can be overriden. 
        /// </summary>
        protected virtual bool ShouldDisengage()
        {
            return !Owner.AI.HasPriorityTarget && (MoveState == CombatMoveState.Disengage || WeAreChasingAndCantCatchThem || TheyAreMighty());
        }

        protected bool TheyAreMighty()
        {
            if (!HeadOnAttack) return false;
            float theirDps = OwnerTarget.TotalDps;
            float ourDefense = (Owner.shield_max + Owner.armor_max) * Owner.HealthPercent;
            return theirDps * 5 > ourDefense;
        }

        /// <summary>
        /// Executes the anti chase disengage. ship will try to maintain a left or right facing to target.
        /// </summary>
        protected virtual void ExecuteAntiChaseDisengage(FixedSimTime timeStep)
        {
            switch(DisengageType)
            {
                case DisengageTypes.Left:  DisengageDirection = DirectionToTarget.LeftVector(); break;
                case DisengageTypes.Right: DisengageDirection = DirectionToTarget.RightVector(); break;
                default: DisengageDirection = Vector2.Zero; break;
            }
            
            Owner.AI.SubLightContinuousMoveInDirection(DisengageDirection, timeStep);
        }

        /// <summary>
        /// Initializes basic combat movement parameters. Chase status. distance to target.
        /// ship target orientation.
        /// </summary>
        void Initialize(float desiredWeaponsRange)
        {
            DistanceToTarget = Owner.Center.Distance(OwnerTarget.Center);
            SpacerDistance = Owner.Radius + AI.Target.Radius;
            OutSideDesiredWeaponsRange = DistanceToTarget > Owner.DesiredCombatRange;
            DebugTextIndex = 0;


            if (OutSideDesiredWeaponsRange)
            {
                if (DistanceToTarget < OwnerTarget.WeaponsMaxRange * 2)
                {
                    if (ShouldApproach())
                    {
                        MoveState = CombatMoveState.Approach;
                        DisengageType = DisengageTypes.None;
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
                    DisengageType      = DisengageTypes.None;
                    MoveState          = CombatMoveState.Approach;
                }
            }
            else
            {
                DisengageDirection = Vector2.Zero;
                DisengageType = DisengageTypes.None;
            }


            if (ThinkTimer.LessOrEqual(0))
            {
                
                ThinkTimer = 1f / Owner.Level.LowerBound(1);

                DirectionToTarget = Owner.Center.DirectionToTarget(OwnerTarget.Center);

                DeltaOurFacingTheirMovement =
                    DegreeDifferenceBetweenVectorDirection(OwnerTarget.VelocityDirection, Owner.Direction);

                DeltaOurMovementTheirFacing =
                    DegreeDifferenceBetweenVectorDirection(Owner.VelocityDirection, OwnerTarget.Direction);

                DeltaOurMovementToTheirMovement =
                    DegreeDifferenceBetweenVectorDirection(Owner.VelocityDirection, OwnerTarget.VelocityDirection);

                DeltaDirectionToTargetAndOurFacing =
                    DegreeDifferenceBetweenVectorDirection(DirectionToTarget, Owner.Direction);

                DeltaTargetDirectionToUsAndTheirFacing =
                    DegreeDifferenceBetweenVectorDirection(OwnerTarget.Center.DirectionToTarget(Owner.Center), OwnerTarget.Direction);

                DeltaOurFacingAndOurMovement =
                    DegreeDifferenceBetweenVectorDirection(Owner.VelocityDirection, Owner.Direction);

                DeltaOurMovementToOurDirectionToTarget =
                    DegreeDifferenceBetweenVectorDirection(Owner.VelocityDirection, DirectionToTarget);
                
                DeltaDirectionToTargetToTheirFacing =
                    DegreeDifferenceBetweenVectorDirection(OwnerTarget.Direction, DirectionToTarget);
            }

            ChaseStates = ChaseStatus(DistanceToTarget);
        }

        /// <summary>
        /// Approach target if it cant fight back. Its not withing 5 degrees of the targets facing.The ship should not be disengaging
        /// </summary>
        /// <returns></returns>
        bool ShouldApproach()
        {
            if (OwnerTarget.CombatDisabled || DeltaTargetDirectionToUsAndTheirFacing > 5f) return true;
            return !ShouldDisengage();
        }

        protected float DegreeDifferenceBetweenVectorDirection(Vector2 velocityDirection, Vector2 direction)
        {
            if (OwnerTarget == null) return 0;
            float dot = direction.Dot(velocityDirection);
            return Math.Abs(dot * 90 - 90);
        }
        protected Vector2 RandomFlankTo(Vector2 direction) => RandomMath.RollDice(50) ? direction.LeftVector() : direction.RightVector();
        DisengageTypes RandomDisengageType(Vector2 disenageFrom)
        {
            float rng = RandomMath.RollDie(100);
            rng = OwnerTarget.AI.Target == Owner ? rng -30 : rng + 30;

            return rng > 50 ? DisengageTypes.Left : DisengageTypes.Right;
        }

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
            if (OwnerTarget.CombatDisabled) return false;
            
            float distanceRatio = DistanceToTarget / DesiredCombatRange;
            //float maxSpeedRatio = Owner.VelocityMaximum / OwnerTarget.VelocityMaximum.LowerBound(0.1f);
            float currentSpeedRatio = Owner.MaxSTLSpeed.LowerBound(75) / OwnerTarget.CurrentVelocity.LowerBound(75);
            return currentSpeedRatio < 1f && distanceRatio > 0.9f; //maxSpeedRatio < 1f && 
        }
        protected bool TheyCantCatchUs(float distanceToTarget)
        {
            return Owner.CurrentVelocity > OwnerTarget.VelocityMaximum && distanceToTarget > Owner.DesiredCombatRange;
        }

        protected ChaseState ChaseStatus(float distance)
        {
            ChaseState chase = ChaseState.None;
            if (Owner.AI.HasPriorityOrder || Owner.AI.HasPriorityTarget) return chase;
            if (OwnerTarget.CombatDisabled || Owner.CurrentVelocity + OwnerTarget.CurrentVelocity < 100) return chase;
            
            if (DeltaOurMovementToTheirMovement > 5f)        return ChaseState.None;

            if (DeltaOurMovementToOurDirectionToTarget < 20)  chase |= ChaseState.WeAreChasing;
            else
            if (DeltaOurMovementToOurDirectionToTarget > 160) chase |= ChaseState.TheyAreChasing;

            chase |= CanCatchState(chase, distance);

            return chase;
        }

        /// <summary>
        /// TO BE EXPANDED. be a harder target on approach. 
        /// </summary>
        public void ErraticMovement(float distanceToTarget, FixedSimTime timeStep)
        {
            ErraticTimer -= timeStep.FixedTime;
            if (AI.IsFiringAtMainTarget || Owner.AI.HasPriorityOrder)
            {
                ZigZag = Vector2.Zero;
                return;
            }

            if (ErraticTimer > 0)
            {
                return;
            }
             
            int rng       = RandomMath.RollAvgPercentVarianceFrom50();
            int racialMod = Owner.loyalty.data.Traits.PhysicalTraitPonderous ? -1 : 0;
            racialMod    += Owner.loyalty.data.Traits.PhysicalTraitReflexes ? 1 : 0;
            float mod     = 5 * (Owner.RotationRadiansPerSecond * (Owner.Level + racialMod)).Clamped(0, 10);

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
                    ZigZag = dir.RightVector() * rng * 10 * Owner.RotationRadiansPerSecond;
                }
                else
                {
                    ZigZag = dir.LeftVector() * rng * 10 * Owner.RotationRadiansPerSecond;
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