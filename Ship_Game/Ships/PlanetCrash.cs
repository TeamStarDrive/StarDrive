using Microsoft.Xna.Framework;

namespace Ship_Game.Ships
{
    public class PlanetCrash
    {
        readonly Planet P;
        readonly bool IsMeteor;
        readonly float Distance;
        readonly Vector2 CrashPos;
        readonly float Thrust;
        readonly Ship Owner;
        public float Scale = 2;

        public PlanetCrash(Planet p, Ship owner, float thrust, bool isMeteor)
        {
            P        = p;
            Owner    = owner;
            IsMeteor = isMeteor;
            Thrust   = thrust.LowerBound(100);
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