using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Debug;

namespace Ship_Game.AI.CombatTactics
{
    internal sealed class AttackRun : ShipAIPlan
    {
        // direction offset from target ship, so our attack runs go over the side of the enemy ship
        Vector2 TargetQuadrant = Vectors.Right;
        Vector2 DisengageStart;
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
        public override void Execute(float elapsedTime, ShipAI.ShipGoal g)
        {
            if (Owner.IsPlatformOrStation) // platforms can't do attack runs
                return;

            float spacerDistance = Owner.Radius + AI.Target.Radius;
            float adjustedWeaponRange = Owner.WeaponsMaxRange * 0.8f; // maybe change to desiredCombatRange.
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
                float distance = (distanceToAttack*0.75f) + 50.0f;
                PrepareToDisengage(distance);
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

        // pick a new disengage position to make some distance for another run
        void PrepareToDisengage(float disengageDistance)
        {
            State = RunState.Disengage1;

            float turnAboutTime = (float)Math.PI / Owner.rotationRadiansPerSecond;

            float dot = Owner.Direction.Dot(AI.Target.Velocity.Normalized());
            float rotation = dot > -0.25f // we are chasing them, so only disengage left or right
                ? (RandomMath.RollDice(50) ? RadMath.RadiansLeft : RadMath.RadiansRight)
                : RandomMath.RandomBetween(-1.57f, 1.57f); // from -90 to +90 degrees

            DisengageStart = AI.Target.Center;

            Vector2 direction = (Owner.Rotation + rotation).RadiansToDirection();
            DisengagePos1 = DisengageStart + direction * disengageDistance;

            Vector2 leftOrRight = RandomMath.RollDice(50) ? direction.LeftVector() : direction.RightVector();
            DisengagePos2 = DisengagePos1 + disengageDistance*(direction+leftOrRight);
        }

        void ExecuteDisengage(float elapsedTime)
        {
            Vector2 disengagePos = (State == RunState.Disengage1) ? DisengagePos1 : DisengagePos2;
            float disengageLimit = DisengageStart.Distance(disengagePos) + 20f;
            float distance = DisengageStart.Distance(Owner.Center);

            float disengageSpeed = (Owner.velocityMaximum*0.8f).Clamped(200f, 1000f);
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
                
                AI.SubLightContinuousMoveInDirection(Owner.Center.DirectionToTarget(DisengagePos1),
                    elapsedTime, disengageSpeed);
            }
            else // if (State == RunState.Disengage2)
            {
                if (distance > disengageLimit) // Disengage2 success
                {
                    State = RunState.Strafing;
                    return;
                }
                AI.SubLightContinuousMoveInDirection(Owner.Center.DirectionToTarget(DisengagePos2), 
                    elapsedTime, disengageSpeed);
            }

            if (DebugInfoScreen.Mode == DebugModes.Targeting &&
                Empire.Universe.DebugWin?.Visible == true &&
                Empire.Universe.SelectedShip == Owner)
            {
                DebugInfoScreen debug = Empire.Universe.DebugWin;
                debug.DrawCircle(DebugModes.Targeting, DisengagePos1, 30f, Owner.loyalty.EmpireColor, 0f);
                debug.DrawCircle(DebugModes.Targeting, DisengagePos2, 30f, Owner.loyalty.EmpireColor, 0f);
                debug.DrawCircle(DebugModes.Targeting, DisengageStart, disengageLimit, Color.Bisque, 0f);
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

            float cooldownTime = Owner.Weapons.IsEmpty ? 0 : Owner.Weapons.Average(w => w.CooldownTimer);
            if (cooldownTime <= 0f) return false;

            float averageSpeed = Owner.Weapons.Average(w => w.ProjectileSpeed);
            return (distanceToAttack*2f) <= (averageSpeed * cooldownTime);
        }

        // Strafe: repeatedly with weapons within weapons range
        void StrafeTowardsTarget(float elapsedTime, float distanceToAttack, Vector2 attackPos)
        {
            float speed = GetStrafeSpeed(distanceToAttack, out bool cantCatchUp, out string debugStatus);
            if (cantCatchUp)
            {
                // we can't catch these bastards! use warp
                if (Owner.FastestWeapon.ProjectedImpactPointNoError(AI.Target, out Vector2 pip))
                {
                    DrawDebugTarget(pip, Owner.Radius);
                    AI.ThrustOrWarpToPosCorrected(pip, elapsedTime);
                    DrawDebugText("CatchUp");
                    return;
                }
            }

            if (distanceToAttack < 500f)
            {
                // stop applying thrust when we get really close, and focus on aiming at Target.Center:
                DrawDebugTarget(AI.Target.Center, Owner.Radius);
                AI.RotateTowardsPosition(AI.Target.Center, elapsedTime, 0.05f);
                DrawDebugText("TerminalStrafe");
            }
            else
            {
                // fly simply towards the offset attack position
                DrawDebugTarget(attackPos, Owner.Radius);
                AI.SubLightMoveTowardsPosition(attackPos, elapsedTime, speed, predictPos: true, autoSlowDown: false);
                DrawDebugText($"{debugStatus} {(int)speed}");
            }
        }

        float GetStrafeSpeed(float distance, out bool cantCatchUp, out string debugStatus)
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
                    if (targetSpeed > Owner.velocityMaximum && distance > Owner.DesiredCombatRange)
                    {
                        cantCatchUp = true;
                        debugStatus = "";
                        return 0f;
                    }
                    
                    debugStatus = "Chase";
                    return (distance - Owner.DesiredCombatRange*0.6f)
                        .Clamped(targetSpeed + Owner.velocityMaximum*0.05f, Owner.velocityMaximum);
                }
                
                // they are coming towards us or just flew past us
                debugStatus = "Strafe";
                return Owner.Speed * 0.75f;
            }

            // enemy is really slow, so we're not in a hurry
            // using distance gives a nice slow-down effect when we get closer to the target
            debugStatus = "SlowStrafe";
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
