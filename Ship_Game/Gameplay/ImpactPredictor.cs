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
            InterceptSpeed = ourShip.AvgProjectileSpeed;
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

        Vector2 PredictProjectileImpact(Vector2 targetAcc)
        {
            Vector2 lookAt = Pos.DirectionToTarget(TargetPos);
            Vector2 deltaV = TargetVel - Vel;
            float dot = deltaV.Normalized().Dot(lookAt); // deltaV intersect with lookAt

            // this is the current time to target if we shot the projectile right now
            float time = TimeToTarget(Pos, TargetPos, InterceptSpeed);

            // targets are on collision course
            if (dot < -0.95f)
            {
                Vector2 left = lookAt.LeftVector(); // perpendicular to forward vector
                float dot2 = left.Dot(deltaV.Normalized());
                float speed = deltaV.Length();

                // only place movePos on the same axis as left vector
                Vector2 movePos = TargetPos + left*(dot2*speed) + lookAt*(speed*0.2f);

                //Console.WriteLine($"PIP lookAt:{lookAt} dv:{deltaV} dot:{dot.String(2)} time:{time.String(2)}");
                Empire.Universe?.DebugWin?.DrawText(Debug.DebugModes.Targeting, Pos, $"TTT head-on: {time.String(2)}", Color.Green, 0f);
                return movePos;
            }

            // objects are SEPARATING faster than projectile can catch up, so this means
            // the projectile might never hit?
            if (deltaV.Length().GreaterOrEqual(InterceptSpeed))
            {
                //Console.WriteLine($"PIP lookAt:{lookAt} dv:{deltaV} dot:{dot.String(2)} time:{time.String(2)}");
                Empire.Universe?.DebugWin?.DrawText(Debug.DebugModes.Targeting, Pos, $"TTT never: {time.String(2)}", Color.Red, 0f);
                return ProjectPosition(TargetPos, deltaV, targetAcc, time);
            }

            Vector2 predictedPos = default;

            for (int i = 0; i < 20; ++i)
            {
                predictedPos = ProjectPosition(TargetPos, deltaV, targetAcc, time);
                float newTime = TimeToTarget(Pos, predictedPos, InterceptSpeed);
                if (time.AlmostEqual(newTime, 0.01666f))
                    break;
                if (/*i == 19 || */newTime > 20f)
                    break;
                    //throw new Exception($"Prediction probably out of range: {newTime}");
                time = newTime;
            }
            
            //Console.WriteLine($"PIP lookAt:{lookAt} dv:{deltaV} dot:{dot.String(2)} time:{time.String(2)}");
            Empire.Universe?.DebugWin?.DrawText(Debug.DebugModes.Targeting, Pos, $"TTT ok: {time.String(2)}", Color.Orange, 0f);
            return predictedPos;
        }

        // @param advancedTargeting If TRUE, targeting will account for Acceleration
        //                          which yields even more accurate target prediction!
        public Vector2 Predict(bool advancedTargeting)
        {
            // @note Validated via DeveloperSandbox DebugPlatform simulations
            // Quad is very accurate if speed is constant
            // Quad has a tendency to over predict when accelerating
            //Vector2 quad = PredictImpactQuad();

            //Empire.Universe.DebugWin?.DrawCircle(Debug.DebugModes.Targeting, PredictImpactQuad(), 50f, Color.Cyan, 0f);
            //Empire.Universe.DebugWin?.DrawCircle(Debug.DebugModes.Targeting, PredictImpactIter(), 60f, Color.LawnGreen, 0f);
            //Empire.Universe.DebugWin?.DrawCircle(Debug.DebugModes.Targeting, PredictImpactIter(TargetAcc), 70f, Color.HotPink, 0f);

            // Iter is just as accurate when speed is constant
            // Iter is quite accurate even when accelerating
            // For this reason, we will favor ITER
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


        
        // @todo Figure out if this could be useful
        // assume we have a relative reference frame and weaponPos is stationary
        // use additional calculations to set up correct interceptSpeed and deltaVel
        // https://stackoverflow.com/a/2249237
        float PredictImpactTime(Vector2 deltaV, float speed)
        {
            Vector2 distance = TargetPos - Pos;

            float a = deltaV.Dot(deltaV) - (speed * speed);
            float bm = 2f * distance.Dot(deltaV);
            float c = distance.Dot(distance);

            // Then solve the quadratic equation for a, b, and c.That is, time = (-b + -sqrt(b * b - 4 * a * c)) / 2a.
            if (Abs(a) < 0.0001f)
                return 0f; // no solution

            float sqrt = bm * bm - 4 * a * c;
            if (sqrt < 0.0f)
                return 0f; // no solution
            sqrt = (float)Sqrt(sqrt);

            // Those values are the time values at which point you can hit the target.
            float timeToImpact1 = (bm - sqrt) / (2 * a);
            float timeToImpact2 = (bm + sqrt) / (2 * a);

            #if DEBUG
            if (float.IsNaN(timeToImpact1) || float.IsNaN(timeToImpact2))
                Log.Error("timeToImpact was NaN!");
            #endif

            // If any of them are negative, discard them, because you can't send the target back in time to hit it.  
            // Take any of the remaining positive values (probably the smaller one).
            if (timeToImpact1 < 0f) return Max(0f, timeToImpact2);
            if (timeToImpact2 < 0f) return timeToImpact1;
            return Min(timeToImpact1, timeToImpact2);
        }

    }
}
