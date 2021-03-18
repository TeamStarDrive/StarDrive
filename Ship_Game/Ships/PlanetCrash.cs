using Microsoft.Xna.Framework;
using Particle3DSample;

namespace Ship_Game.Ships
{
    public class PlanetCrash
    {
        readonly Planet P;
        readonly float Distance;
        readonly Vector2 CrashPos;
        readonly float Thrust;
        readonly Ship Owner;
        public float Scale = 2;
        private ParticleEmitter TrailEmitter;
        private ParticleEmitter FireTrailEmitter;
        private ParticleEmitter FlameTrail;

        public PlanetCrash(Planet p, Ship owner, float thrust)
        {
            P        = p;
            Owner    = owner;
            Thrust   = thrust.LowerBound(owner.IsPlatformOrStation ? 100 : 200);
            CrashPos = P.Center.GenerateRandomPointInsideCircle(P.ObjectRadius);
            Distance = Owner.Position.Distance(CrashPos).LowerBound(1);

            Owner.SetDieTimer(2);
            if (Owner.IsPlatformOrStation && Owner.GetTether() != null)
            {
                Owner.GetTether().RemoveFromOrbitalStations(Owner);
                Owner.RemoveTether();
            }
        }

        public void Update(FixedSimTime timeStep)
        {
            Vector2 dir = Owner.Position.DirectionToTarget(CrashPos);
            Owner.Position += dir.Normalized() * Thrust * timeStep.FixedTime;
            Scale = Owner.Position.Distance(CrashPos) / Distance;

            if (Owner.Position.InRadius(CrashPos, 200))
            {
                P.TryCrashOn(Owner);
                Owner.SetReallyDie();
                Owner.Die(Owner.LastDamagedBy, true);
                return;
            }

            Owner.SetDieTimer(2);

            // Fiery trail atmospheric entry
            if (!P.Type.EarthLike || !Owner.Position.InRadius(P.Center, P.ObjectRadius + 1000f))
                return;

            Vector3 trailPos = (Owner.Position + dir.Normalized() * Owner.Radius * (Scale/2)).ToVec3(Owner.GetSO().World.Translation.Z-20);
            if (Owner.Position.InRadius(P.Center, P.ObjectRadius + 1000f)
                && !Owner.Position.InRadius(P.Center, P.ObjectRadius))
            {
                if (FireTrailEmitter == null)
                {
                    FireTrailEmitter = Empire.Universe.fireTrailParticles.NewEmitter(500f, trailPos);
                    FlameTrail       = Empire.Universe.flameParticles.NewEmitter(300, trailPos);
                }

                FireTrailEmitter.Update(timeStep.FixedTime, trailPos);
                FlameTrail.Update(timeStep.FixedTime, trailPos);
            }

            if (Owner.Position.InRadius(P.Center, P.ObjectRadius))
            {
                if (TrailEmitter == null)
                    TrailEmitter = Empire.Universe.projectileTrailParticles.NewEmitter(500, trailPos);

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
                if (ship.Center.InRadius(p.Center, p.GravityWellRadius)
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