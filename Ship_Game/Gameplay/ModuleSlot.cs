using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Ships;

namespace Ship_Game.Gameplay
{
    public sealed class ModuleSlotData
    {
        public Vector2 Position;
        [XmlElement(ElementName = "InstalledModuleUID")]
        [JsonProperty("InstalledModuleUID")]
        public string ModuleUID;
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

        public override string ToString() => $"{ModuleUID} {Position} {Facing} {Restrictions} T={ModuleOrNull}";

        [XmlIgnore] [JsonIgnore]
        public bool IsDummy => ModuleUID == null || ModuleUID == "Dummy";

        [XmlIgnore] [JsonIgnore]
        public ShipModule ModuleOrNull => ModuleUID != null && ModuleUID != "Dummy" && 
                                          ResourceManager.GetModuleTemplate(ModuleUID, out ShipModule m) ? m : null;

        public ModuleOrientation GetOrientation()
        {
            if (Orientation.NotEmpty() && Orientation != "Normal")
                return (ModuleOrientation)Enum.Parse(typeof(ModuleOrientation), Orientation);
            return ModuleOrientation.Normal;
        }

        public bool Equals(ModuleSlotData s)
        {
            return Position == s.Position
                && ModuleUID == s.ModuleUID
                && HangarshipGuid == s.HangarshipGuid
                && Facing == s.Facing
                && Orientation == s.Orientation
                && Restrictions == s.Restrictions
                && SlotOptions == s.SlotOptions;
        }

        public Point GetModuleSize()
        {
            ShipModule m = ModuleOrNull;
            if (m != null)
            {
                switch (GetOrientation())
                {
                    case ModuleOrientation.Normal:
                    case ModuleOrientation.Rear:
                        return new Point(m.XSIZE, m.YSIZE);
                    case ModuleOrientation.Left:
                    case ModuleOrientation.Right:
                        return new Point(m.YSIZE, m.XSIZE);
                }
            }
            return new Point(1, 1);
        }

        public Vector2 GetModuleSizeF()
        {
            Point size = GetModuleSize();
            return new Vector2(size.X * 16f, size.Y * 16f);
        }

        public ModuleSlotData GetClone()
        {
            return (ModuleSlotData)MemberwiseClone();
        }
    }
}