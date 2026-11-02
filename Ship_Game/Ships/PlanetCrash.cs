using SDGraphics;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;
using Ship_Game.Graphics.Particles;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game.Ships
{
    [StarDataType]
    public class PlanetCrash
    {
        [StarData] readonly Planet P;
        [StarData] readonly float Distance;
        [StarData] readonly Vector2 CrashPos;
        [StarData] readonly Ship Owner;
        [StarData] float Seconds;
        [StarData] public float Scale { get; private set; } = 2;
        ParticleEmitter TrailEmitter;
        ParticleEmitter FireTrailEmitter;
        ParticleEmitter FlameTrail;

        public PlanetCrash(Planet p, Ship owner)
        {
            P        = p;
            Owner    = owner;
            CrashPos = P.Position.GenerateRandomPointInsideCircle(P.Radius, p.Random);
            Distance = Owner.Position.Distance(CrashPos).LowerBound(1);
            if (Owner.Velocity.Length() > Owner.MaxSTLSpeed) // a ship hit at warp falls from its top sublight speed
                Owner.Velocity = Owner.Velocity.Normalized(Owner.MaxSTLSpeed);
            Owner.Acceleration = Vector2.Zero; // a falling ship has no thrust

            Owner.SetDieTimer(2);
            if (Owner.GetTether() != null) // a tethered ship would be held in place and never reach the planet
            {
                Owner.GetTether().RemoveFromOrbitalStations(Owner);
                Owner.RemoveTether();
            }
        }

        public PlanetCrash()
        {
        }

        public void Update(ParticleManager particles, FixedSimTime timeStep)
        {
            Owner.Dying = true;
            Vector2 dir = Owner.Position.DirectionToTarget(CrashPos);
            float speed = (Owner.MaxSTLSpeed * 0.8f).LowerBound(200);
            PullTowardCrash(dir * speed, speed, timeStep.FixedTime);
            Scale = (Owner.Position.Distance(CrashPos) / Distance).UpperBound(Scale.UpperBound(1)); // the ship only sinks toward the planet

            if (Owner.Position.InRadius(CrashPos, 200f))
            {
                P.TryCrashOn(Owner);
                Owner.SetReallyDie();
                Owner.Velocity = Vector2.Zero;
                Owner.Die(Owner.LastDamagedBy, true);
                return;
            }

            Owner.SetDieTimer(2); // the ship dies when it reaches the crash point

            // Fiery trail atmospheric entry
            if (!P.PType.Clouds || !Owner.Position.InRadius(P.Position, P.Radius + 1000f))
                return;

            float z = Owner.GetSO()?.World.Translation.Z - 20 ?? 0;
            Vector3 trailPos = (Owner.Position + Owner.Velocity.Normalized() * Owner.Radius * Scale * 0.5f).ToVec3(z);

            if (Owner.Position.InRadius(P.Position, P.Radius + 1000f) &&
                !Owner.Position.InRadius(P.Position, P.Radius))
            {
                if (FireTrailEmitter == null)
                {
                    FireTrailEmitter = particles.FireTrail.NewEmitter(500f, trailPos);
                    FlameTrail       = particles.Flame.NewEmitter(300, trailPos);
                }

                FireTrailEmitter.Update(timeStep.FixedTime, trailPos);
                FlameTrail.Update(timeStep.FixedTime, trailPos);
            }

            if (Owner.Position.InRadius(P.Position, P.Radius))
            {
                if (TrailEmitter == null)
                    TrailEmitter = particles.ProjectileTrail.NewEmitter(500, trailPos);

                TrailEmitter.Update(timeStep.FixedTime, trailPos);
            }
        }

        // the velocity at death turns evenly into the fall velocity over the time a straight fall would take, 2 to 5 seconds
        void PullTowardCrash(Vector2 fallVelocity, float speed, float dt)
        {
            float remaining = (Distance / speed).Clamped(2f, 5f) - Seconds;
            Seconds += dt;
            Owner.Velocity = remaining > dt ? Owner.Velocity + (fallVelocity - Owner.Velocity) * (dt / remaining) : fallVelocity;
        }

        public static bool GetPlanetToCrashOn(Ship ship, out Planet planet)
        {
            planet = null;
            if (ship.System == null)
                return false;

            for (int i = 0; i < ship.System.PlanetList.Count; i++)
            {
                Planet p = ship.System.PlanetList[i];
                if (ship.Position.InRadius(p.Position, p.GravityWellRadius * 0.5f)
                   || ship.IsPlatformOrStation && ship.GetTether() == p)
                {
                    planet = p;
                    return true;
                }
            }

            return false;
        }
    }
}