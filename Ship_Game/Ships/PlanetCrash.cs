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

            Owner.SetDieTimer(2);
            if (Owner.IsPlatformOrStation && Owner.GetTether() != null)
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
            Owner.Velocity = dir * (Owner.MaxSTLSpeed * 0.8f).LowerBound(200);
            Scale = Owner.Position.Distance(CrashPos) / Distance;

            if (Owner.Position.InRadius(CrashPos, 200))
            {
                P.TryCrashOn(Owner);
                Owner.SetReallyDie();
                Owner.Velocity = Vector2.Zero;
                Owner.Die(Owner.LastDamagedBy, true);
                return;
            }

            if (Scale < 1.01f)
                Owner.SetDieTimer(2); // If ship shot out of the Atmosphere (scale bigger than 1) - dont update the timer and let it die

            // Fiery trail atmospheric entry
            if (!P.PType.Clouds || !Owner.Position.InRadius(P.Position, P.Radius + 1000f))
                return;

            float z = Owner.GetSO()?.World.Translation.Z - 20 ?? 0;
            Vector3 trailPos = (Owner.Position + dir * Owner.Radius * Scale * 0.5f).ToVec3(z);

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