using System;
using Microsoft.Xna.Framework;
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
        private readonly Vector2 Pos; // Our Position
        private readonly Vector2 Vel; // Our Velocity
        private readonly float Speed; // interception speed (projectile speed)
        private readonly Vector2 TargetPos; // Target position
        private readonly Vector2 TargetVel; // Target velocity
        private readonly Vector2 TargetAcc; // Target acceleration

        public ImpactPredictor(Vector2 pos, Vector2 vel, float speed, Vector2 targetPos, Vector2 targetVel)
        {
            Pos = pos;
            Vel = vel;
            Speed = speed;
            TargetPos = targetPos;
            TargetVel = targetVel;
            TargetAcc = Vector2.Zero;
        }

        public ImpactPredictor(Vector2 pos, Vector2 vel, float speed, GameplayObject target)
        {
            Pos = pos;
            Vel = vel;
            Speed = speed;
            TargetInfo info = GetTargetInfo(target);
            TargetPos = info.Pos;
            TargetVel = info.Vel;
            TargetAcc = info.Acc;
        }

        public ImpactPredictor(Ships.Ship ourShip, GameplayObject target)
        {
            Pos = ourShip.Center;
            Vel = ourShip.Velocity;
            Speed = ourShip.AvgProjectileSpeed;
            TargetInfo info = GetTargetInfo(target);
            TargetPos = info.Pos;
            TargetVel = info.Vel;
            TargetAcc = info.Acc;
        }

        public ImpactPredictor(Projectile proj, GameplayObject target)
        {
            Pos = proj.Center;
            Vel = proj.Velocity;
            Speed = proj.Speed;
            // guided missiles should not account for speed, since they are
            // ramming devices and always have more velocity
            //if (proj.Weapon.Tag_Guided)
            //    Speed = 1.0f;
            TargetInfo info = GetTargetInfo(target);
            TargetPos = info.Pos;
            TargetVel = info.Vel;
            TargetAcc = info.Acc;
        }

        private static TargetInfo GetTargetInfo(GameplayObject target)
        {
            if (target is Ships.Ship ship || target is Ships.ShipModule sm && (ship = sm.GetParent()) != null)
            {
                return new TargetInfo { Pos = target.Center, Vel = ship.Velocity, Acc = ship.Acceleration };
            }
            return new TargetInfo { Pos = target.Center, Vel = target.Velocity };
        }

        private Vector2 PredictImpactOld()
        {
            Vector2 vectorToTarget = TargetPos - Pos;
            Vector2 projectileVelocity = Vel + Vel.Normalized() * Speed;
            float distance = vectorToTarget.Length();
            float time = distance / projectileVelocity.Length();
            return TargetPos + TargetVel * time;
        }

        // assume we have a relative reference frame and weaponPos is stationary
        // use additional calculations to set up correct interceptSpeed and deltaVel
        // https://stackoverflow.com/a/2249237
        private float PredictImpactTime(Vector2 deltaV)
        {
            Vector2 distance = TargetPos - Pos;

            float a = deltaV.Dot(deltaV) - (Speed * Speed);
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

            // If any of them are negative, discard them, because you can't send the target back in time to hit it.  
            // Take any of the remaining positive values (probably the smaller one).
            if (timeToImpact1 < 0f) return Max(0f, timeToImpact2);
            if (timeToImpact2 < 0f) return timeToImpact1;
            return Min(timeToImpact1, timeToImpact2);
        }

        // http://www.dummies.com/education/science/physics/finding-distance-using-initial-velocity-time-and-acceleration/
        private static Vector2 ProjectPosition(Vector2 pos, Vector2 vel, Vector2 accel, float time)
        {
            // s = v0*t + (a*t^2)/2
            Vector2 dist = vel * time + accel * (time * time * 0.5f);
            return pos + dist;
        }
        private static Vector2 ProjectPosition(Vector2 pos, Vector2 vel, float time)
        {
            return pos + vel * time;
        }

        private float TimeToTarget(Vector2 target) => Pos.Distance(target) / Speed;

        private float PredictImpactTimeAdjusted(Vector2 deltaV)
        {
            float impactTime = PredictImpactTime(deltaV);
            if (impactTime > 20f) // projectile will probably never catch up to target
                impactTime = 20f;
            else if (impactTime <= 0f)
                impactTime = TimeToTarget(TargetPos);
            return impactTime;
        }

        private Vector2 PredictImpactQuad()
        {
            if (Speed.AlmostEqual(0f, 0.01f))
                return TargetPos;

            Vector2 deltaV = TargetVel - Vel;
            float impactTime = PredictImpactTimeAdjusted(deltaV);
            return ProjectPosition(TargetPos, deltaV, impactTime);
        }

        private Vector2 PredictImpactQuad(Vector2 targetAccel)
        {
            if (Speed.AlmostEqual(0f, 0.01f))
                return TargetPos;

            Vector2 deltaV = TargetVel - Vel;
            float impactTime = PredictImpactTimeAdjusted(deltaV);

            // project target position at impactTime
            Vector2 pip = ProjectPosition(TargetPos, deltaV, targetAccel, impactTime);

            float impactTime2 = TimeToTarget(pip); // t = s/v
            if (impactTime2 <= 0f)
                return TargetPos; // incase of head-on collision

            // this is the final corrected PIP:
            Vector2 pip2 = ProjectPosition(TargetPos, deltaV, targetAccel, impactTime2);
            return pip2;
        }

        private Vector2 PredictImpactIter(Vector2 targetAccel)
        {
            if (Speed.AlmostEqual(0f, 0.01f))
                return TargetPos;

            float time = TimeToTarget(TargetPos);
            Vector2 deltaV = TargetVel - Vel;

            // objects are separating faster than projectile can catch up, so this means
            // the projectile might never hit?
            if (deltaV.Length() > Speed)
                return ProjectPosition(TargetPos, deltaV, targetAccel, time);

            Vector2 predictedPos = default(Vector2);

            for (int i = 0; i < 20; ++i)
            {
                predictedPos = ProjectPosition(TargetPos, deltaV, targetAccel, time);
                float newTime = TimeToTarget(predictedPos);
                if (newTime > 20f || time.AlmostEqual(newTime, 0.1f))
                    return predictedPos;
                time = newTime;
            }
            return predictedPos;
        }

        private Vector2 PredictImpactIter()
        {
            if (Speed.AlmostEqual(0f, 0.01f))
                return TargetPos;

            float time = TimeToTarget(TargetPos);
            Vector2 deltaV = TargetVel - Vel;

            // objects are separating faster than projectile can catch up, so this means
            // the projectile might never hit?
            if (deltaV.Length() > Speed)
                return ProjectPosition(TargetPos, deltaV, time);

            Vector2 predictedPos = default(Vector2);

            for (int i = 0; i < 20; ++i)
            {
                predictedPos = ProjectPosition(TargetPos, deltaV, time);
                float newTime = TimeToTarget(predictedPos);
                if (newTime > 20f || time.AlmostEqual(newTime, 0.1f))
                    return predictedPos;
                time = newTime;
            }
            return predictedPos;
        }

        public Vector2 Predict()
        {
            //Vector2 quad = PredictImpactQuad();
            Vector2 iter = PredictImpactIter();
            //Log.Info("PIP quad: {0}", quad);
            //Log.Info("PIP iter: {0}", iter);

            //Vector2 error = quad-iter;
            //if (error.Length() > 100000)
            //    return iter;
            return iter;
        }
    }
}
