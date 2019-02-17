using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Debug;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    internal sealed class AttackRun : ShipAIPlan
    {
        // direction offset from target ship, so our attack runs go over the side of the enemy ship
        Vector2 TargetQuadrant = Vectors.Right;
        Vector2 DisengagePos1;
        Vector2 DisengagePos2;
        enum RunState
        {
            Strafing,
            Disengage1,
            Disengage2,
        }
        RunState State = RunState.Strafing;


        public AttackRun(ShipAI ai) : base(ai)
        {
        }

        // CombatState.AttackRuns: fighters / corvettes / frigates performing attack run to target
        // @note We are guaranteed to be within 2~3x maxWeaponsRange by DoCombat
        public override void Execute(float elapsedTime)
        {
            if (Owner.IsPlatformOrStation) // platforms can't do attack runs
                return;

            float spacerDistance = Owner.Radius + AI.Target.Radius;
            float adjustedWeaponRange = Owner.maxWeaponsRange * 0.8f;
            if (spacerDistance > adjustedWeaponRange)
                spacerDistance = adjustedWeaponRange;

            if (State == RunState.Disengage1 ||
                State == RunState.Disengage2)
            {
                ExecuteDisengage(elapsedTime);
                return;
            }

            //Empire.Universe.DebugWin?.DrawCircle(DebugModes.Targeting, Target.Center, spacerDistance, Owner.loyalty.EmpireColor, 0f);
            
            Vector2 localQuadrant = AI.Target.Direction.RotateDirection(TargetQuadrant);
            Vector2 attackPos = AI.Target.Position + localQuadrant*AI.Target.Radius;

            // we are really close to attackPos?
            float distanceToAttack = Owner.Center.Distance(attackPos);

            if (ShouldDisengage(distanceToAttack, spacerDistance))
            {
                PrepareToDisengage(600f);
            }
            else if (distanceToAttack < 500f)
            {
                // stop applying thrust when we get really close, and focus on aiming at Target.Center:
                AI.RotateTowardsPosition(AI.Target.Center, elapsedTime, 0.05f);
                DrawDebugTarget(AI.Target.Center, Owner.Radius);
            }
            else
            {
                StrafeTowardsTarget(elapsedTime, distanceToAttack, attackPos);
            }
        }

        void PrepareToDisengage(float disengageDistance)
        {
            State = RunState.Disengage1;

            // pick a new disengage position to make some distance for another run
            Vector2 disengageDir = (Owner.Rotation + RandomMath.RandomBetween(-1f, +1f)).RadiansToDirection();
            DisengagePos1 = AI.Target.Center + disengageDir * disengageDistance;

            Vector2 leftOrRight = RandomMath.RollDice(50) ? disengageDir.LeftVector() : disengageDir.RightVector();
            DisengagePos2 = DisengagePos1 + disengageDistance*(disengageDir+leftOrRight);
        }

        void ExecuteDisengage(float elapsedTime)
        {
            Vector2 disengagePos = (State == RunState.Disengage1) ? DisengagePos1 : DisengagePos2;
            float disengageLimit = AI.Target.Center.Distance(disengagePos) + 20f;
            float distance = Owner.Center.Distance(AI.Target.Center);

            if (State == RunState.Disengage1)
            {
                if (distance > disengageLimit) // Disengage1 success
                {
                    State = RunState.Disengage2;
                    switch (RandomMath.InRange(2)) // and pick new attack quadrant on enemy ship:
                    {
                        default:
                        case 0: TargetQuadrant = Vectors.Left; break;
                        case 1: TargetQuadrant = Vectors.Right; break;
                    }
                    return;
                }
                
                AI.SubLightContinuousMoveInDirection(Owner.Center.DirectionToTarget(DisengagePos1), elapsedTime, Owner.Speed);
            }
            else // if (State == RunState.Disengage2)
            {
                if (distance > disengageLimit) // Disengage2 success
                {
                    State = RunState.Strafing;
                    return;
                }
                AI.SubLightContinuousMoveInDirection(Owner.Center.DirectionToTarget(DisengagePos2), elapsedTime, Owner.Speed*0.8f);
            }

            if (DebugInfoScreen.Mode == DebugModes.Targeting &&
                Empire.Universe.DebugWin?.Visible == true &&
                Empire.Universe.SelectedShip == Owner)
            {
                DebugInfoScreen debug = Empire.Universe.DebugWin;
                debug.DrawCircle(DebugModes.Targeting, DisengagePos1, 30f, Owner.loyalty.EmpireColor, 0f);
                debug.DrawCircle(DebugModes.Targeting, DisengagePos2, 30f, Owner.loyalty.EmpireColor, 0f);
                debug.DrawCircle(DebugModes.Targeting, AI.Target.Center, disengageLimit, Color.Bisque, 0f);
                debug.DrawLine(DebugModes.Targeting, Owner.Center, disengagePos, 1f, Owner.loyalty.EmpireColor, 0f);
                DrawDebugText(State.ToString());
            }
        }

        bool ShouldDisengage(float distanceToAttack, float spacerDistance)
        {
            if (distanceToAttack <= spacerDistance)
                return true;

            int salvosRemaining = Owner.Weapons.Sum(w => w.SalvosToFire);
            if (salvosRemaining != 0) return false;

            float cooldownTime = Owner.Weapons.Average(w => w.CooldownTimer);
            if (cooldownTime <= 0f) return false;

            float averageSpeed = Owner.Weapons.Average(w => w.ProjectileSpeed);
            return (distanceToAttack*2f) <= (averageSpeed * cooldownTime);
        }

        // Strafe: repeatedly with weapons within weapons range
        void StrafeTowardsTarget(float elapsedTime, float distanceToAttack, Vector2 attackPos)
        {
            float speed = GetStrafeSpeed(distanceToAttack, out bool cantCatchUp);
            if (cantCatchUp)
            {
                // we can't catch these bastards! use warp
                if (Owner.FastestWeapon.ProjectedImpactPointNoError(AI.Target, out Vector2 pip))
                {
                    DrawDebugTarget(pip, Owner.Radius);
                    AI.ThrustOrWarpToPosCorrected(pip, elapsedTime);
                    return;
                }
            }

            if (distanceToAttack < 500f)
            {
                // stop applying thrust when we get really close, and focus on aiming at Target.Center:
                DrawDebugTarget(AI.Target.Center, Owner.Radius);
                AI.RotateTowardsPosition(AI.Target.Center, elapsedTime, 0.05f);
                DrawDebugText("StrafeTerminal");
            }
            else
            {
                // fly simply towards the offset attack position
                DrawDebugTarget(attackPos, Owner.Radius);
                AI.SubLightMoveTowardsPosition(attackPos, elapsedTime, speed, predictPos: true, autoSlowDown: false);
                DrawDebugText($"Strafe {(int)speed}");
            }
        }

        float GetStrafeSpeed(float distance, out bool cantCatchUp)
        {
            float targetSpeed = AI.Target.Velocity.Length();
            cantCatchUp = false;
            if (targetSpeed > 50f)
            {
                // figure out if the target ship is drifting towards our facing or if it's drifting away
                float dot = Owner.Direction.Dot(AI.Target.Velocity.Normalized());
                if (dot > -0.25f) // they are trying to escape us
                {
                    // we can't catch these bastards, so we need some jump drive assistance
                    if (targetSpeed > Owner.velocityMaximum && distance > Owner.maxWeaponsRange)
                    {
                        cantCatchUp = true;
                        return 0f;
                    }

                    if (distance > Owner.maxWeaponsRange * 0.75f)
                        return Owner.velocityMaximum; // catch up with max speed

                    if (distance > Owner.maxWeaponsRange * 0.25f)
                        return targetSpeed + Owner.velocityMaximum * 0.05f;

                    return targetSpeed + 5f; // ~ match their speed
                }

                // they are coming towards us or just flew past us
                return Owner.Speed * 0.75f;
            }

            // enemy is really slow, so we're not in a hurry
            // this gives a nice slow-down effect when we get close to the target
            return (distance - Owner.velocityMaximum*0.4f)
                .Clamped(Owner.velocityMaximum*0.15f, Owner.velocityMaximum*0.9f);
        }

        void DrawDebugTarget(Vector2 pip, float radius)
        {
            if (DebugInfoScreen.Mode == DebugModes.Targeting &&
                Empire.Universe.DebugWin?.Visible == true &&
                Empire.Universe.SelectedShip == Owner)
            {
                Empire.Universe.DebugWin?.DrawCircle(DebugModes.Targeting, pip, radius, Owner.loyalty.EmpireColor, 0f);
                Empire.Universe.DebugWin?.DrawLine(DebugModes.Targeting, AI.Target.Center, pip, 1f, Owner.loyalty.EmpireColor, 0f);
            }
        }

        void DrawDebugText(string text)
        {
            if (DebugInfoScreen.Mode == DebugModes.Targeting &&
                Empire.Universe.DebugWin?.Visible == true &&
                Empire.Universe.SelectedShip == Owner)
            {
                Empire.Universe.DebugWin?.DrawText(DebugModes.Targeting, 
                    Owner.Center + new Vector2(Owner.Radius), text, Color.Red, 0f);
            }
        }
    }
}
