using Microsoft.Xna.Framework;
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
        readonly float Speed; // interception speed (projectile speed)
        readonly Vector2 TargetPos; // Target position
        readonly Vector2 TargetVel; // Target velocity
        readonly Vector2 TargetAcc; // Target acceleration

        // This sets the maximum lookahead time in seconds
        // Any impact time predictions beyond this are clamped
        // @note This should be == max projectile health
        readonly float MaxPredictionTime; 

        // For weapon arc prediction
        public ImpactPredictor(Vector2 pos, Vector2 vel, float speed, float range, GameplayObject target)
        {
            Pos = pos;
            Vel = vel;
            Speed = speed;
            TargetInfo info = GetTargetInfo(target);
            TargetPos = info.Pos;
            TargetVel = info.Vel;
            TargetAcc = info.Acc;

            // this will limit the pip from moving further than would be possible
            float approxLifetime = range / speed;
            MaxPredictionTime = approxLifetime * 1.1f;
        }

        // For generic ship movement prediction
        public ImpactPredictor(Vector2 pos, Vector2 vel, float speed, float range, Vector2 targetPos)
        {
            Pos = pos;
            Vel = vel;
            Speed = speed;
            TargetPos = targetPos;
            TargetVel = Vector2.Zero;
            TargetAcc = Vector2.Zero;

            // this will limit the pip from moving further than would be possible
            float approxLifetime = range / speed;
            MaxPredictionTime = approxLifetime * 1.1f;
        }

        // This is used during interception / attack runs
        public ImpactPredictor(Ship ourShip, GameplayObject target)
        {
            Pos = ourShip.Center;
            Vel = ourShip.Velocity;
            Speed = ourShip.AvgProjectileSpeed;
            TargetInfo info = GetTargetInfo(target);
            TargetPos = info.Pos;
            TargetVel = info.Vel;
            TargetAcc = info.Acc;
            MaxPredictionTime = 0f;
            MaxPredictionTime = TimeToTarget(target.Center) * 1.5f;
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
            MaxPredictionTime = Max(0.1f, proj.Duration);
        }

        static TargetInfo GetTargetInfo(GameplayObject target)
        {
            if (target is Ship ship || target is ShipModule sm && (ship = sm.GetParent()) != null)
            {
                return new TargetInfo { Pos = target.Center, Vel = ship.Velocity, Acc = ship.Acceleration };
            }
            return new TargetInfo { Pos = target.Center, Vel = target.Velocity };
        }

        Vector2 PredictImpactOld()
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
        float PredictImpactTime(Vector2 deltaV)
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

        float TimeToTarget(Vector2 target) => Pos.Distance(target) / Speed;

        float PredictImpactTimeAdjusted(Vector2 deltaV)
        {
            float impactTime = PredictImpactTime(deltaV);
            if (impactTime > MaxPredictionTime)
                impactTime = MaxPredictionTime;
            else if (impactTime <= 0f)
                impactTime = TimeToTarget(TargetPos); // default fallback
            return impactTime;
        }

        Vector2 PredictImpactQuad()
        {
            if (Speed.AlmostEqual(0f, 0.01f))
                return TargetPos;

            Vector2 deltaV = TargetVel - Vel;
            float impactTime = PredictImpactTimeAdjusted(deltaV);
            return ProjectPosition(TargetPos, deltaV, impactTime);
        }

        Vector2 PredictImpactQuad(Vector2 targetAccel)
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

        Vector2 PredictImpactIter(Vector2 targetAccel)
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

        Vector2 PredictImpactIter()
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
