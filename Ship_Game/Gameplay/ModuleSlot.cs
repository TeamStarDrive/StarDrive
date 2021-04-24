using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;

namespace Ship_Game.Gameplay
{
    public sealed class ModuleSlotData
    {
        public Vector2 Position;
        public string InstalledModuleUID;
        public Guid HangarshipGuid;
        public float Health;
        [XmlElement(ElementName = "Shield_Power")]
        public float ShieldPower;

        [XmlElement(ElementName = "facing")]
        public float Facing;
        [XmlElement(ElementName = "state")]
        public string Orientation;
        public Restrictions Restrictions;
        public string SlotOptions;
        

        public override string ToString() => $"{InstalledModuleUID} {Position} {Facing} {Restrictions}";

        public ModuleOrientation GetOrientation()
        {
            if (Orientation.NotEmpty() && Orientation != "Normal")
                return (ModuleOrientation)Enum.Parse(typeof(ModuleOrientation), Orientation);
            return ModuleOrientation.Normal;
        }

        public bool Equals(ModuleSlotData s)
        {
            return Position == s.Position
                && InstalledModuleUID == s.InstalledModuleUID
                && HangarshipGuid == s.HangarshipGuid
                && Facing == s.Facing
                && Orientation == s.Orientation
                && Restrictions == s.Restrictions
                && SlotOptions == s.SlotOptions;
        }
    }
}