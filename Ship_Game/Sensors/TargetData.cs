using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Windows.Forms;

namespace Ship_Game.Sensors
{
    public class TargetData
    {
        public GameplayObject[] Nearby =Empty<GameplayObject>.Array;

        public float ScanRange {get; set;} = 1000;
        public GameObjectType ScanFilter {get; set;} = GameObjectType.Ship;

        public virtual GameplayObject[] Scan(float elapsedTime, Vector2 position, Empire empire = null)
        {
            Nearby = UniverseScreen.SpaceManager.FindNearby(position, ScanRange, ScanFilter, empire) ?? Empty<GameplayObject>.Array;
            return Nearby;
        }
    }
}