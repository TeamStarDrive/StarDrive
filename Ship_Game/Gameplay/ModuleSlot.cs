using Microsoft.Xna.Framework;
using System;
using System.Xml.Serialization;

namespace Ship_Game.Gameplay
{
    public sealed class ModuleSlotData
    {
        public Vector2 Position;
        public string InstalledModuleUID;
        public Guid HangarshipGuid;
        public float Health;
        public float ShieldPower;
        public float Facing;
        public Restrictions Restrictions;
        public string SlotOptions;
    }
}