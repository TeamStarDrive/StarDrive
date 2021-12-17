using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.GameScreens.Scene
{
    [StarDataType]
    public class ObjectGroupInfo
    {
        #pragma warning disable 649
        /// <summary>
        /// @see ObjectSpawnInfo.Type
        /// </summary>
        [StarData] public string Type = "fighter";
        [StarData] public int Count = 1;
        [StarData] public Range Scale = new Range(0.8f);
        [StarData] public Range Speed = new Range(10f);
        [StarData] public Vector3? MinPos;
        [StarData] public Vector3? MaxPos;
        // if Orbit state exists, use Orbit [radius, min_angle, max_angle]
        [StarData] public Vector3? Orbit;
        // if Orbit state exists, use Offset RandVec3[X, Y, Z]
        [StarData] public Vector3 Offset;
        [StarData] public Vector3? Rotation;
        [StarData] public Vector3? RandRot;
        #pragma warning restore 649
    }
}
