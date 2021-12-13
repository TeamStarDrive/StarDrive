using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.GameScreens.Scene
{
    [StarDataType]
    public class ShipSpawnInfo
    {
        #pragma warning disable 649
        [StarData] public RoleName Role = RoleName.fighter;
        [StarData] public Vector3 Position;
        [StarData] public float Speed = 10f;
        public IEmpireData Empire;
        public ISceneShipAI AI;
        public Vector3 Rotation;
        public bool DisableJumpSfx;
        #pragma warning restore 649
    }
}
