﻿using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.AI.ShipMovement.CombatManeuvers;
using Ship_Game.Debug;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI.CombatTactics
{
    internal sealed class AttackRun : CombatMovement
    {
        public enum RunState
        {
            None,
            Strafing,
            Disengage1,
            Disengage2,
        }

        RunState State;
        Vector2 DisengageStart;
        Vector2 DisengagePos1;
        Vector2 DisengagePos2;

        public AttackRun(ShipAI ai) : base(ai)
        {
            State = RunState.Strafing;
        }
        
        // CombatState.AttackRuns: fighters / corvettes / frigates performing attack run to target
        // @note We are guaranteed to be within 2~3x maxWeaponsRange by DoCombat
        protected override void OverrideCombatValues(FixedSimTime timeStep)
        {
            DesiredCombatRange = Owner.WeaponsMaxRange * 0.8f; // maybe change to desiredCombatRange.
            if (SpacerDistance > DesiredCombatRange)
                SpacerDistance = DesiredCombatRange;
        }

        protected override CombatMoveState ExecuteAttack(FixedSimTime timeStep)
        {
            if (State == RunState.Disengage1 || State == RunState.Disengage2)
            {
                ExecuteDisengage(timeStep);
                return CombatMoveState.Disengage;
            }

            // we are really close to attackPos?
            if (ShouldDisengage(DistanceToTarget, SpacerDistance))
            {
                float engageDistance = (DesiredCombatRange * 0.25f) + SpacerDistance;
                PrepareToDisengage(engageDistance);
                return CombatMoveState.Disengage;
            }

            if (DistanceToTarget < 500f) 
            {
                // stop applying thrust when we get really close, and focus on aiming at Target.Center:
                AI.RotateTowardsPosition(AI.Target.Position, timeStep, 0.05f);
                DrawDebugTarget(AI.Target.Position, Owner.Radius);
                return CombatMoveState.Face;
            }

            Vector2 attackPos = AI.Target.Position;
            Vector2 localQuadrant = AI.Target.Direction.RotateDirection(TargetQuadrant);
            attackPos += localQuadrant * OwnerTarget.Radius;

            StrafeTowardsTarget(timeStep, DistanceToTarget, attackPos);
            return CombatMoveState.Approach;
        }

        // Strafe: repeatedly with weapons within weapons range
        void StrafeTowardsTarget(FixedSimTime timeStep, float distanceToAttack, Vector2 attackPos)
        {
            float speed = GetStrafeSpeed(distanceToAttack, out string debugStatus);

            if (WeAreChasingAndCantCatchThem)
            {
                // we can't catch these bastards! use warp
                Vector2 pip = Owner.FastestWeapon?.ProjectedImpactPointNoError(AI.Target)?? AI.Target.Position;
                DrawDebugTarget(pip, Owner.Radius);
                AI.ThrustOrWarpToPos(pip, timeStep);
                return;
            }

            if (distanceToAttack < 500f)
            {
                // stop applying thrust when we get really close, and focus on aiming at Target.Center:
                DrawDebugTarget(AI.Target.Position, Owner.Radius);
                AI.RotateTowardsPosition(AI.Target.Position, timeStep, 0.05f);
                DrawDebugText("TerminalStrafe");
            }
            else
            {
                // fly simply towards the offset attack position
                DrawDebugTarget(attackPos, Owner.Radius);
                AI.SubLightMoveTowardsPosition(attackPos, timeStep, speed, predictPos: true, autoSlowDown: false);
                DrawDebugText($"{debugStatus} {(int)speed}");
            }
        }

        float GetStrafeSpeed(float distance, out string debugStatus)
        {
            float targetSpeed = AI.Target.CurrentVelocity;
            if (targetSpeed > 50f)
            {
                // figure out if the target ship is drifting towards our facing or if it's drifting away
                ChaseState chase = ChaseStates;
                if (chase.IsSet(ChaseState.WeAreChasing)) // they are trying to escape us
                {
                    // we can't catch these bastards, so we need some jump drive assistance
                    if (chase.IsSet(ChaseState.CantCatch))
                    {
                        debugStatus = "";
                        return 0f;
                    }
                    
                    debugStatus = "Chase";
                    return (distance - Owner.DesiredCombatRange*0.6f)
                        .Clamped(targetSpeed + Owner.VelocityMax*0.05f, Owner.VelocityMax);
                }
                
                // they are coming towards us or just flew past us
                debugStatus = "Strafe";
                return Owner.STLSpeedLimit * 0.75f;
            }

            // enemy is really slow, so we're not in a hurry
            // using distance gives a nice slow-down effect when we get closer to the target
            debugStatus = "SlowStrafe";
            return (distance - Owner.VelocityMax*0.4f)
                .Clamped(Owner.VelocityMax*0.15f, Owner.VelocityMax*0.9f);
        }

        bool ShouldDisengage(float distanceToAttack, float spacerDistance)
        {
            if (OwnerTarget == null || AI.IsFiringAtMainTarget && !AI.Target.EnginesKnockedOut)
                return false;

            if (distanceToAttack <= spacerDistance)
                return true;

            if (ChaseStates.IsSet(ChaseState.WeAreChasing))
                return false;

            float distanceToDesiredCombatRangeRatio = distanceToAttack / Owner.DesiredCombatRange;
            if (distanceToDesiredCombatRangeRatio < 0.25f) 
                return true;

            float cooldownTime = Owner.Weapons.IsEmpty ? 0 : Owner.Weapons.Average(w => w.CooldownTimer);
            if (cooldownTime <= 0f)
                return false;

            float distanceBeforeFire = Owner.InterceptSpeed * cooldownTime;
            return distanceBeforeFire > distanceToAttack;
        }

        void PrepareToDisengage(float disengageDistance)
        {
            State = RunState.Disengage1;

            var random = Random;
            float dot = OwnerTarget.VelocityDirection.Dot(Owner.Direction);
            float rotation = dot > 0f // we are chasing them, so only disengage left or right
                ? (random.RollDice(50) ? RadMath.RadiansLeft : RadMath.RadiansRight)
                : random.Float(-1.57f, 1.57f); // from -90 to +90 degrees

            float cooldownTime = Owner.Weapons.IsEmpty ? 0 : Owner.Weapons.Max(w => w.CooldownTimer);

            //disengageDistance = disengageDistance.UpperBound(cooldownTime * disengageDistance);

            DisengageStart = AI.Target.Position;

            Vector2 direction = (Owner.Rotation + rotation).RadiansToDirection();
            DisengagePos1 = DisengageStart + direction * (disengageDistance + SpacerDistance);

            Vector2 leftOrRight = random.RollDice(50) ? direction.LeftVector() : direction.RightVector();
            DisengagePos2 = DisengagePos1 + (Owner.MaxSTLSpeed * cooldownTime + SpacerDistance) * (direction + leftOrRight);
        }

        public void ExecuteDisengage(FixedSimTime timeStep)
        {
            Vector2 disengagePos = (State == RunState.Disengage1) ? DisengagePos1 : DisengagePos2;
            float disengageLimit = DesiredCombatRange * 0.25f + SpacerDistance;
            float distance = DisengageStart.Distance(Owner.Position).LowerBound(SpacerDistance);

            float disengageSpeed = (Owner.VelocityMax * 0.8f).Clamped(200f, 1000f);
            if (State == RunState.Disengage1)
            {
                if (distance > disengageLimit) // Disengage1 success
                {
                    State = RunState.Disengage2;
                    switch (Random.InRange(2)) // and pick new attack quadrant on enemy ship:
                    {
                        default:
                        case 0: TargetQuadrant = Vectors.Left; break;
                        case 1: TargetQuadrant = Vectors.Right; break;
                    }
                }
                else
                {
                    AI.SubLightContinuousMoveInDirection(Owner.Position.DirectionToTarget(DisengagePos1), timeStep, disengageSpeed);
                }
            }
            else if (State == RunState.Disengage2)
            {
                if (DistanceToTarget > Owner.DesiredCombatRange || DirectionToTarget.Dot(Owner.Direction) > 0.7f) // Disengage2 success
                {
                    State = RunState.Strafing;
                }
                else
                {
                    AI.SubLightContinuousMoveInDirection(Owner.Position.DirectionToTarget(DisengagePos2), timeStep, disengageSpeed);
                }
            }

            if (Owner.Universe.DebugMode == DebugModes.Targeting &&
                Owner.Universe.DebugWin?.Visible == true &&
                Owner.Universe.Screen.SelectedShip == Owner)
            {
                DebugInfoScreen debug = Owner.Universe.DebugWin;
                debug.DrawCircle(DebugModes.Targeting, DisengagePos1, 30f, Owner.Loyalty.EmpireColor, 0f);
                debug.DrawCircle(DebugModes.Targeting, DisengagePos2, 30f, Owner.Loyalty.EmpireColor, 0f);
                debug.DrawCircle(DebugModes.Targeting, DisengageStart, disengageLimit, Color.Bisque, 0f);
                debug.DrawLine(DebugModes.Targeting, Owner.Position, disengagePos, 1f, Owner.Loyalty.EmpireColor, 0f);
                DrawDebugText(State.ToString());
            }
        }

    }
}
