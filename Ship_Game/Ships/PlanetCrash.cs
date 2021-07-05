﻿using Microsoft.Xna.Framework;

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
            Owner.Position += dir.Normalized() * Thrust.UpperBound(200) * timeStep.FixedTime;
            Scale = Owner.Position.Distance(CrashPos) / Distance;

            if (Owner.Position.InRadius(CrashPos, 200))
            {
                P.TryCrashOn(Owner);
                Owner.SetReallyDie();
                Owner.Die(Owner.LastDamagedBy, true);
                return;
            }

            if (Scale < 1.01f)
                Owner.SetDieTimer(2); // If ship shot out of the Atmosphere (scale bigger than 1) - dont update the timer and let it die

            // Fiery trail atmospheric entry
            if (!P.Type.EarthLike || !Owner.Position.InRadius(P.Center, P.ObjectRadius + 1000f))
                return;

            Vector3 trailPos = (Owner.Position + dir.Normalized() * Owner.Radius * (Scale/2)).ToVec3(Owner.GetSO().World.Translation.Z-20);
            if (Owner.Position.InRadius(P.Center, P.ObjectRadius + 1000f)
                && !Owner.Position.InRadius(P.Center, P.ObjectRadius))
            {
                if (FireTrailEmitter == null)
                {
                    FireTrailEmitter = Empire.Universe.Particles.FireTrail.NewEmitter(500f, trailPos);
                    FlameTrail       = Empire.Universe.Particles.Flame.NewEmitter(300, trailPos);
                }

                FireTrailEmitter.Update(timeStep.FixedTime, trailPos);
                FlameTrail.Update(timeStep.FixedTime, trailPos);
            }

            if (Owner.Position.InRadius(P.Center, P.ObjectRadius))
            {
                if (TrailEmitter == null)
                    TrailEmitter = Empire.Universe.Particles.ProjectileTrail.NewEmitter(500, trailPos);

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
                if (ship.Center.InRadius(p.Center, p.GravityWellRadius * 0.5f)
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