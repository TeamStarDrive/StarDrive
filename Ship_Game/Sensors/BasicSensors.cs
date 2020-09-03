using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Sensors
{
    public class BasicSensors : TimedScanner
    {
        public float TimerInterVal {get;set;} = 2f;

        public Ship[] EnemiesNear = Empty<Ship>.Array;
        public Ship[] FriendliesNearBy = Empty<Ship>.Array;
        public Projectile[] ProjectilesNearby = Empty<Projectile>.Array;
        public Empire Owner;

        public BasicSensors(float timeInterval, Empire empire) : base(timeInterval)
        {
            Owner = empire;
            ScanFilter = GameObjectType.Any;
        }

        public override GameplayObject[] Scan(float elapsedTime, Vector2 position, Empire empireToFind = null)
        {
            var results = base.Scan(elapsedTime, position, empireToFind);

            foreach (var item in results)
            {
                if (!item.Active || item.Velocity.Length() > 1000f) continue;

            }

            return results;
        }
    }
}