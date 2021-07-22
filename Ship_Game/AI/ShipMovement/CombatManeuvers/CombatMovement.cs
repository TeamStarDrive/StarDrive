﻿using System;
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
            Total,
            Erratic
        }

        protected CombatMoveState MoveState = CombatMoveState.None;
        // direction offset from target ship, so our attack runs go over the side of the enemy ship
        int DebugTextIndex;
        protected float DistanceToTarget;
        protected Vector2 DirectionToTarget;
        protected Vector2 TargetQuadrant = Vectors.Right;
        protected Vector2 ZigZag;
        protected float SpacerDistance;
        protected float DesiredCombatRange;
        Vector2 DisengageDirection;
        DisengageTypes DisengageType; 
        protected float ErraticTimer = 0;
        
        protected Ship OwnerTarget => AI.Target;

        protected bool WeAreChasingAndCantCatchThem => ChaseStates.HasFlag(ChaseState.ChasingCantCatch) && !WeAreRetrograding;

        /// <summary>
        /// If dot product is negative, our velocity is opposite of our ship's facing, we are retrograding
        /// </summary>
        bool WeAreRetrograding => Owner.VelocityDirection.Dot(Owner.Direction) < 0f;

        protected ChaseState ChaseStates;

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

            Initialize(DesiredCombatRange);
            OverrideCombatValues(timeStep);

            if (MoveState != CombatMoveState.Disengage && ShouldDisengage())
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
                //ErraticMovement(timeStep); // TODO
                MoveState = ExecuteAttack(timeStep);
            }

            if (Empire.Universe.Debug && Empire.Universe.SelectedShip != null)
            {
                DrawDebugText($"Chase: {ChaseStates}");
                DrawDebugText($"MoveState: {MoveState}");
                DrawDebugText($"Our.Dir to Intercept.Dir: {DirectionToTarget.Dot(Owner.Direction)}");
                DrawDebugText($"Our.Dir to Target.VelDir: {OwnerTarget.VelocityDirection.Dot(Owner.Direction)}");
                DrawDebugText($"Our.VelDir to Target.Dir: {Owner.VelocityDirection.Dot(OwnerTarget.Direction)}");
                DrawDebugText($"Our.VelDir to Target.VelDir: {Owner.VelocityDirection.Dot(OwnerTarget.VelocityDirection)}");
            }
        }

        /// <summary>
        /// Returns true if already disengaging or chasing a faster ship. if a player gives the command dont disengage. 
        /// can be overriden. 
        /// </summary>
        bool ShouldDisengage()
        {
            return !Owner.AI.HasPriorityTarget
                && (MoveState == CombatMoveState.Disengage || WeAreChasingAndCantCatchThem || (Owner.Level > 1 && TargetIsMightyAndChasing(1f)));
        }

        protected bool IsTargetInterceptingUs => Owner.Direction.Dot(OwnerTarget.Direction) < -0.8f
                                    && RadMath.IsTargetInsideArc(OwnerTarget.Position, Owner.Position, OwnerTarget.Rotation, RadMath.HalfPI*0.5f);
        
        // they are intercepting us and they're really strong
        protected bool TargetIsMightyAndChasing(float ratioToOurDefense) => IsTargetInterceptingUs && TargetIsMighty(ratioToOurDefense);

        protected bool TargetIsMighty(float ratioToOurDefense)
        {
            float ourDefense = (Owner.shield_max + Owner.armor_max) * Owner.HealthPercent;
            return (OwnerTarget.TotalDps * 5) / ourDefense >= ratioToOurDefense;
        }

        /// <summary>
        /// Executes the anti chase disengage. ship will try to maintain a left or right facing to target.
        /// </summary>
        void ExecuteAntiChaseDisengage(FixedSimTime timeStep)
        {
            float angle = RandomMath.AvgRandomBetween(1f, 3f);

            switch(DisengageType)
            {
                case DisengageTypes.Left:  DisengageDirection = (DirectionToTarget.LeftVector() / angle).Normalized(); break;
                case DisengageTypes.Right: DisengageDirection = (DirectionToTarget.RightVector() / angle).Normalized(); break;
                default: DisengageDirection = Vector2.Zero; break;
            }
            
            Owner.AI.SubLightContinuousMoveInDirection(DisengageDirection, timeStep);
        }

        /// <summary>
        /// Initializes basic combat movement parameters.
        /// </summary>
        void Initialize(float desiredWeaponsRange)
        {
            DistanceToTarget  = Owner.Position.Distance(OwnerTarget.Position);
            DirectionToTarget = Owner.Position.DirectionToTarget(OwnerTarget.Position);

            SpacerDistance = Owner.Radius + AI.Target.Radius;
            DebugTextIndex = 0;

            if (DistanceToTarget > Owner.DesiredCombatRange && !TargetIsMightyAndChasing(1))
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
                    DisengageType = DisengageTypes.None;
                    MoveState = CombatMoveState.Approach;
                }
            }
            else
            {
                DisengageDirection = Vector2.Zero;
                DisengageType = DisengageTypes.None;
            }

            ChaseStates = ChaseStatus(DistanceToTarget);
        }

        /// <summary>
        /// Approach target if it cant fight back or if it's not intercepting us already
        /// The ship should not be disengaging
        /// </summary>
        bool ShouldApproach()
        {
            if (OwnerTarget.CombatDisabled || !IsTargetInterceptingUs)
                return true;
            return !ShouldDisengage();
        }

        DisengageTypes RandomDisengageType(Vector2 disenageFrom)
        {
            DisengageTypes disengage = DisengageTypes.None;
            if (Owner.AI.CombatState == CombatState.BroadsideLeft || Owner.AI.CombatState == CombatState.OrbitLeft)
            {
                disengage = DisengageTypes.Left;
            }
            else if (Owner.AI.CombatState == CombatState.BroadsideRight || Owner.AI.CombatState == CombatState.OrbitRight)
            {
                disengage = DisengageTypes.Right;
            }
            else
            {
                Vector2 positionOffset = Owner.Position - OwnerTarget.Position;
                Vector2 rightward = OwnerTarget.Direction.RightVector();

                float targetFacingToOwner = positionOffset.Dot(rightward);

                disengage = targetFacingToOwner > 0 ? DisengageTypes.Left : DisengageTypes.Right;
            }
            return disengage;
        }

        protected ChaseState CanCatchState(ChaseState chaseState, float distance)
        {
            if (chaseState.HasFlag(ChaseState.WeAreChasing))
                return WeCantCatchThem(distance) ? ChaseState.CantCatch : ChaseState.None;
            
            if (ChaseStates.HasFlag(ChaseState.TheyAreChasing))
                return WeCantCatchThem(distance) ? ChaseState.CantCatch : ChaseState.None;

            return ChaseState.None;
        }

        bool WeCantCatchThem(float distanceToTarget)
        {
            if (OwnerTarget.CombatDisabled)
                return false;
            
            float distanceRatio = DistanceToTarget / DesiredCombatRange;
            float currentSpeedRatio = Owner.MaxSTLSpeed.LowerBound(75) / OwnerTarget.CurrentVelocity.LowerBound(75);
            return currentSpeedRatio < 1f && distanceRatio > 0.9f; //maxSpeedRatio < 1f && 
        }

        ChaseState ChaseStatus(float distance)
        {
            if (Owner.AI.HasPriorityOrder || Owner.AI.HasPriorityTarget)
                return ChaseState.None;

            if (OwnerTarget.CombatDisabled || Owner.CurrentVelocity + OwnerTarget.CurrentVelocity < 100)
                return ChaseState.None;
            
            // ships are travelling in opposite direction?
            if (Owner.VelocityDirection.Dot(OwnerTarget.VelocityDirection) < 0.5f)
                return ChaseState.None;

            ChaseState chase = ChaseState.None;

            float dotToInterceptDir = Owner.VelocityDirection.Dot(DirectionToTarget);
            if (dotToInterceptDir > 0.35f && RadMath.IsTargetInsideArc(Owner.Position, OwnerTarget.Position, Owner.Rotation, RadMath.Deg20AsRads))
                chase |= ChaseState.WeAreChasing;
            else if (dotToInterceptDir < -0.35f && RadMath.IsTargetInsideArc(OwnerTarget.Position, Owner.Position, OwnerTarget.Rotation, RadMath.Deg20AsRads))
                chase |= ChaseState.TheyAreChasing;

            chase |= CanCatchState(chase, distance);
            return chase;
        }

        /// <summary>
        /// TO BE EXPANDED. be a harder target on approach.
        /// this works badly. disabled until it can be made better
        /// </summary>
        public void ErraticMovement(FixedSimTime timeStep)
        {
            ErraticTimer -= timeStep.FixedTime;

            bool cantMoveErratic =  Owner.Level == 0 || Owner.loyalty.data.Traits.PhysicalTraitPonderous;

            if (cantMoveErratic || MoveState != CombatMoveState.Approach || AI.IsFiringAtMainTarget || Owner.AI.HasPriorityOrder
                || WeAreRetrograding || Owner.CurrentVelocity < 100f)
            {
                return;
            }

            bool inErraticArc = RadMath.IsTargetInsideArc(OwnerTarget.Position, Owner.Position, OwnerTarget.Rotation, RadMath.Deg3AsRads);

            if (inErraticArc || ErraticTimer > 0)
                return;

            int racialMod = Owner.loyalty.data.Traits.PhysicalTraitReflexes ? 1 : 0;

            ErraticTimer = (10f - racialMod) / (Owner.Level + racialMod);

            DisengageType = DisengageTypes.Erratic;

            int rng = RandomMath.RollAvgPercentVarianceFrom50();
            
            float mod = 5 * (Owner.RotationRadiansPerSecond * (1 + Owner.Level + racialMod)).Clamped(0, 10);

            ErraticTimer = 2 / (Owner.RotationRadiansPerSecond + 1);

            if (rng < 25 - mod)
            {
                ZigZag = Vector2.Zero;
            }
            else
            {
                Vector2 dir = Owner.Position.DirectionToTarget(AI.Target.Position);
                if (RandomMath.IntBetween(0, 1) == 1)
                {
                    ZigZag = dir.RightVector() * 100 * Owner.RotationRadiansPerSecond;
                }
                else
                {
                    ZigZag = dir.LeftVector() * 100 * Owner.RotationRadiansPerSecond;
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
                Empire.Universe.DebugWin?.DrawLine(DebugModes.Targeting, AI.Target.Position, pip, 1f, Owner.loyalty.EmpireColor, 0f);
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
                    Owner.Position + new Vector2(Owner.Radius, Owner.Radius + 50 * DebugTextIndex), text, Color.Red, 0f);
            }
        }
    }
}