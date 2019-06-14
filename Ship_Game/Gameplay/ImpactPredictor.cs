using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;
using static System.Math;

namespace Ship_Game.Gameplay
{
    public struct TargetInfo
    {
        public Vector2 Pos;
        public Vector2 Vel;
        public Vector2 Acc;
    }

    public struct ImpactPredictor
    {
        readonly Vector2 Pos; // Our Position
        readonly Vector2 Vel; // Our Velocity
        readonly float InterceptSpeed; // interception speed (projectile speed)
        readonly Vector2 TargetPos; // Target position
        readonly Vector2 TargetVel; // Target velocity
        readonly Vector2 TargetAcc; // Target acceleration

        // For generic ship movement prediction
        public ImpactPredictor(Vector2 pos, Vector2 vel, float interceptSpeed,
                               Vector2 targetPos, Vector2 targetVel, Vector2 targetAcc)
        {
            Pos = pos;
            Vel = vel;
            InterceptSpeed = interceptSpeed;
            TargetPos = targetPos;
            TargetVel = targetVel;
            TargetAcc = targetAcc;
        }

        // For weapon arc prediction
        public ImpactPredictor(Vector2 pos, Vector2 vel, float interceptSpeed, 
                               GameplayObject target)
        {
            Pos = pos;
            Vel = vel;
            InterceptSpeed = interceptSpeed;
            TargetInfo info = GetTargetInfo(target);
            TargetPos = info.Pos;
            TargetVel = info.Vel;
            TargetAcc = info.Acc;
        }

        // For generic ship movement prediction
        public ImpactPredictor(Vector2 pos, Vector2 vel, Vector2 targetPos)
        {
            Pos = pos;
            Vel = vel;
            InterceptSpeed = 0f;
            TargetPos = targetPos;
            TargetVel = Vector2.Zero;
            TargetAcc = Vector2.Zero;
        }

        // This is used during interception / attack runs
        public ImpactPredictor(Ship ourShip, GameplayObject target)
        {
            Pos = ourShip.Center;
            Vel = ourShip.Velocity;
            InterceptSpeed = ourShip.InterceptSpeed;
            TargetInfo info = GetTargetInfo(target);
            TargetPos = info.Pos;
            TargetVel = info.Vel;
            TargetAcc = info.Acc;
        }

        public ImpactPredictor(Projectile proj, GameplayObject target)
        {
            Pos = proj.Center;
            Vel = proj.Velocity;
            InterceptSpeed = proj.Speed;
            // guided missiles should not account for speed, since they are
            // ramming devices and always have more velocity
            //if (proj.Weapon.Tag_Guided)
            //    Speed = 1.0f;
            TargetInfo info = GetTargetInfo(target);
            TargetPos = info.Pos;
            TargetVel = info.Vel;
            TargetAcc = info.Acc;
        }

        static TargetInfo GetTargetInfo(GameplayObject target)
        {
            if (target is Ship ship || target is ShipModule sm && (ship = sm.GetParent()) != null)
            {
                return new TargetInfo
                {
                    Pos = target.Center,
                    Vel = ship.Velocity,
                    Acc = ship.Acceleration
                };
            }
            return new TargetInfo
            {
                Pos = target.Center,
                Vel = target.Velocity
            };
        }

        // http://www.dummies.com/education/science/physics/finding-distance-using-initial-velocity-time-and-acceleration/
        static Vector2 ProjectPosition(Vector2 pos, Vector2 vel, Vector2 accel, float time)
        {
            // s = v0*t + (a*t^2)/2
            Vector2 dist = vel * time + accel * (time * time * 0.5f);
            return pos + dist;
        }

        static Vector2 ProjectPosition(Vector2 pos, Vector2 vel, float time)
        {
            return pos + vel * time;
        }

        public static float TimeToTarget(Vector2 pos, Vector2 target, float speed)
        {
            if (speed.AlmostZero())
                return 0f;
            return pos.Distance(target) / speed;
        }

        void DebugPip(string text, Vector2 predicted, float spd, float t, in Color color)
        {
            //float d = Pos.Distance(TargetPos);
            //Console.WriteLine($"PIP {text} ({predicted.X.String(1)} {predicted.Y.String(1)}) d:{d.String()}m {t.String(2)}s|{spd.String()}m/s");
            Empire.Universe?.DebugWin?.DrawText(Debug.DebugModes.Targeting, Pos, $"{text}: {t.String(2)}", color, 0f);
        }

        Vector2 PredictProjectileImpact(Vector2 targetAcc)
        {
            float interceptSpeed = InterceptSpeed.NotZero() ? InterceptSpeed : Vel.Length();

            // due to how StarDrive handles projectile speed limit, we don't use deltaV,
            // just TargetVel. In fully newtonian model we would require deltaV.
            //Vector2 deltaV = Vel - TargetVel;
            float time = PredictImpactTime(Pos, TargetPos, -TargetVel, interceptSpeed);

            Vector2 predicted;
            if (time > 0f)
            {
                predicted = ProjectPosition(TargetPos, TargetVel, targetAcc, time);
                //DebugPip("PERFECT", predicted, interceptSpeed, time, Color.Green);
            }
            // intercept is behind us in time, which means we should have fired the projectile X seconds ago
            else if (time < 0f) 
            {
                predicted = ProjectPosition(TargetPos, TargetVel, TargetAcc, -time);
                //DebugPip("BEHIND", predicted, interceptSpeed, time, Color.Orange);
            }
            else // no solution, fall back to default time estimate
            {
                time = TimeToTarget(Pos, TargetPos, interceptSpeed);
                predicted = ProjectPosition(TargetPos, TargetVel, targetAcc, time);

                // edge case: Pos == TargetPos, they will collide with us head on
                if (Pos.Distance(predicted) <= 8f)
                {
                    predicted = TargetPos;
                    //DebugPip("COLLIDE", predicted, interceptSpeed, time, Color.Yellow);
                }
                else
                {
                    //DebugPip("NOSOLT", predicted, interceptSpeed, time, Color.Red);
                }
            }
            return predicted;
        }

        // @param advancedTargeting If TRUE, targeting will account for Acceleration
        //                          which yields even more accurate target prediction!
        public Vector2 Predict(bool advancedTargeting)
        {
            //Empire.Universe.DebugWin?.DrawCircle(Debug.DebugModes.Targeting, PredictImpactQuad(), 50f, Color.Cyan, 0f);
            //Empire.Universe.DebugWin?.DrawCircle(Debug.DebugModes.Targeting, PredictImpactIter(), 60f, Color.LawnGreen, 0f);
            //Empire.Universe.DebugWin?.DrawCircle(Debug.DebugModes.Targeting, PredictImpactIter(TargetAcc), 70f, Color.HotPink, 0f);

            // quite accurate even when accelerating
            if (!advancedTargeting)
                return PredictProjectileImpact(Vector2.Zero);

            // by using Target acceleration, we get superhuman target prediction
            // even when ships are accelerating. There is almost no overshooting, even small
            // fighters get shot out of the sky by PD/Flak
            return PredictProjectileImpact(TargetAcc);
        }

        // @note This is different than weapon prediction
        //       The move position is always aligned on an axis intersecting
        //       TargetPos and perpendicular to DirectionToTarget
        public Vector2 PredictMovePos()
        {
            Vector2 forward = Pos.DirectionToTarget(TargetPos);
            Vector2 left = forward.LeftVector(); // perpendicular to forward vector
            float dot = -left.Dot(Vel.Normalized()); // get the velocity negation direction
            float speed = Vel.Length(); // negation magnitude

            // only place movePos on the same axis as left vector
            Vector2 movePos = TargetPos + left*(dot*speed);
            return movePos;
        }

        public static Vector2 ThrustOffset(Vector2 ourPos, Vector2 ourVel, Vector2 targetPos, float magnitude = 1f)
        {
            Vector2 forward = ourPos.DirectionToTarget(targetPos);
            Vector2 left = forward.LeftVector(); // perpendicular to forward vector
            float dot = -left.Dot(ourVel.Normalized()); // get the velocity negation direction
            float speed = ourVel.Length(); // negation magnitude

            // only place movePos on the same axis as left vector
            Vector2 movePos = targetPos + left*(dot*speed);
            if (magnitude.NotEqual(1f))
                movePos = targetPos.LerpTo(movePos, magnitude);
            return movePos;
        }

        // assume we have a relative reference frame and weaponPos is stationary
        // use additional calculations to set up correct interceptSpeed and deltaVel
        // https://stackoverflow.com/a/2249237
        // @param shooter Position of shooter
        // @param target  Position of target
        // @param targetVel Velocity of target
        // @param projectileSpeed Speed of intercepting projectile
        // @return Closest impact time. If time is negative, then impact is somewhere behind us
        public static float PredictImpactTime(Vector2 shooter, Vector2 target, Vector2 targetVel, float projectileSpeed)
        {
            double dx = target.X - shooter.X;
            double dy = target.Y - shooter.Y;
            double vx = targetVel.X;
            double vy = targetVel.Y;

            double a = (vx*vx) + (vy*vy) - (projectileSpeed*projectileSpeed);
            double bm = 2.0 * (vx*dx + vy*dy);
            double c = dx*dx + dy*dy;

            // Then solve the quadratic equation for a, b, and c.That is, time = (-b + -sqrt(b * b - 4 * a * c)) / 2a.
            if (Abs(a) < 0.0001)
                return 0f; // no solution

            double sqrt = bm * bm - 4.0 * a * c;
            if (sqrt < 0.0)
                return 0f; // no solution
            sqrt = (float)Sqrt(sqrt);

            // Those values are the time values at which point you can hit the target.
            double timeToImpact1 = (bm - sqrt) / (2.0 * a);
            double timeToImpact2 = (bm + sqrt) / (2.0 * a);

            timeToImpact1 = timeToImpact1.Clamped(-60.0, +60.0);
            timeToImpact2 = timeToImpact2.Clamped(-60.0, +60.0);

            #if DEBUG
            if (double.IsNaN(timeToImpact1) || double.IsNaN(timeToImpact2))
                Log.Error("timeToImpact was NaN!");
            #endif

            // if time is negative, the impact point is behind us
            if (timeToImpact1 < 0f && timeToImpact2 < 0f) // both times are neg
            {
                // pick the closest time behind us
                return (float)Max(timeToImpact1, timeToImpact2);
            }

            if (timeToImpact1 < 0f) return (float)timeToImpact2;
            if (timeToImpact2 < 0f) return (float)timeToImpact1;
            
            return (float)Min(timeToImpact1, timeToImpact2); // pick closest intersect time
        }

    }
}
