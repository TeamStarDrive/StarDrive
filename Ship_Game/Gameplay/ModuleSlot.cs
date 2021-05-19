using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Gameplay
{
    public sealed class ModuleSlotData
    {
        public Vector2 Position;
        public Restrictions Restrictions;

        [XmlElement(ElementName = "InstalledModuleUID")]
        [JsonProperty("InstalledModuleUID")]
        public string ModuleUID;

        [XmlElement(ElementName = "facing")]
        public float Facing;

        [XmlElement(ElementName = "state")]
        public string Orientation;

        public string SlotOptions;

        /// --- Saved ShipModule states ---
        public float Health;

        [XmlElement(ElementName = "Shield_Power")]
        public float ShieldPower;

        public Guid HangarshipGuid;

        public override string ToString() => $"{ModuleUID} {Position} {Facing} {Restrictions} T={ModuleOrNull}";

        [XmlIgnore] [JsonIgnore]
        public bool IsDummy => ModuleUID == null || ModuleUID == "Dummy";

        [XmlIgnore] [JsonIgnore]
        public ShipModule ModuleOrNull => ModuleUID != null && ModuleUID != "Dummy" && 
                                          ResourceManager.GetModuleTemplate(ModuleUID, out ShipModule m) ? m : null;

        [XmlIgnore] [JsonIgnore]
        public Point PosAsPoint => new Point((int)Position.X, (int)Position.Y);

        public ModuleSlotData()
        {
        }

        // New Empty Slot
        public ModuleSlotData(Vector2 position, Restrictions restrictions)
        {
            Position = position;
            Restrictions = restrictions;
        }

        // Save ShipModule as ModuleSlotData for Savegame
        public ModuleSlotData(ShipModule module)
        {
            Position     = module.XMLPosition;
            Restrictions = module.Restrictions;
            ModuleUID    = module.UID;
            Health       = module.Health;
            ShieldPower  = module.ShieldPower;
            Facing       = module.FacingDegrees;
            Orientation  = module.Orientation.ToString();

            if (module.TryGetHangarShip(out Ship hangarShip))
                HangarshipGuid = hangarShip.guid;

            if (module.ModuleType == ShipModuleType.Hangar)
                SlotOptions = module.DynamicHangar == DynamicHangarOptions.Static
                            ? module.hangarShipUID
                            : module.DynamicHangar.ToString();
        }

        // Save SlotStruct as ModuleSlotData in ShipDesignScreen
        public ModuleSlotData(SlotStruct slot)
        {
            Position     = slot.XMLPos;
            Restrictions = slot.Restrictions;
            ModuleUID    = slot.ModuleUID;
            Orientation  = slot.Orientation.ToString();
            if (slot.Module != null)
            {
                slot.Facing = slot.Module.FacingDegrees;
                if (slot.Module.ModuleType == ShipModuleType.Hangar)
                    slot.SlotOptions = slot.Module.hangarShipUID;
            }
        }

        public ModuleOrientation GetOrientation()
        {
            if (Orientation.NotEmpty() && Orientation != "Normal")
                return (ModuleOrientation)Enum.Parse(typeof(ModuleOrientation), Orientation);
            return ModuleOrientation.Normal;
        }

        public bool Equals(ModuleSlotData s)
        {
            return Position == s.Position
                && Restrictions == s.Restrictions
                && ModuleUID == s.ModuleUID
                && Facing == s.Facing
                && Orientation == s.Orientation
                && SlotOptions == s.SlotOptions
                && HangarshipGuid == s.HangarshipGuid;
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

        /// <summary>
        /// Similar to GetClone(), however it discards Ship Save state variables
        /// </summary>
        public ModuleSlotData GetStatelessClone()
        {
            return new ModuleSlotData
            {
                Position     = Position,
                Restrictions = Restrictions,
                ModuleUID    = ModuleUID,
                Facing       = Facing,
                Orientation  = Orientation,
                SlotOptions  = SlotOptions
            };
        }
    }
}