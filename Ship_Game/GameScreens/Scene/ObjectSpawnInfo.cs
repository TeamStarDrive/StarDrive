using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data.Serialization;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game.GameScreens.Scene
{
    [StarDataType]
    public class ObjectSpawnInfo
    {
        #pragma warning disable 649
        /// <summary>
        /// Supported values:
        /// All RoleName's such as "fighter", "corvette", "frigate", "cruiser", "capital", "station", etc..
        /// Asteroids: "asteroid"
        /// SpaceJunk: "spacejunk"
        /// </summary>
        [StarData] public string Type = "fighter";
        [StarData] public float Scale = 0.8f;
        [StarData] public float Speed = 10f;
        [StarData] public Vector3 Position;
        [StarData] public Vector3 Rotation;
        public EmpireData Empire;
        public ISceneShipAI AI;
        public bool DisableJumpSfx;
        #pragma warning restore 649
    }
}
