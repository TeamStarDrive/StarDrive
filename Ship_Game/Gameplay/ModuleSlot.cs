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

        /// -------------------------------

        // Avoid calling the default constructor
        // This is only for the serializer
        public ModuleSlotData()
        {
        }

        // New Empty Slot
        public ModuleSlotData(Vector2 xmlPos, Restrictions restrictions)
        {
            Position = xmlPos;
            Restrictions = restrictions;
        }
        
        // This is the most correct way to create a new ModuleSlotData instance
        // With initial position, restrictions, etc.
        // Excluding 
        public ModuleSlotData(Vector2 xmlPos,
                              Restrictions restrictions,
                              string moduleUid, float facing,
                              string orientation,
                              string slotOptions = null)
        {
            Position     = xmlPos;
            Restrictions = restrictions;
            ModuleUID    = moduleUid;
            Facing       = facing;
            Orientation  = orientation;
            SlotOptions  = slotOptions;
        }

        // Save ShipModule as ModuleSlotData for Savegame
        public ModuleSlotData(ShipModule module)
            : this(xmlPos:       module.XMLPosition,
                   restrictions: module.Restrictions,
                   moduleUid:    module.UID,
                   facing:       module.FacingDegrees,
                   orientation:  module.Orientation.ToString())
        {
            Health       = module.Health;
            ShieldPower  = module.ShieldPower;

            if (module.TryGetHangarShip(out Ship hangarShip))
                HangarshipGuid = hangarShip.guid;

            if (module.ModuleType == ShipModuleType.Hangar)
            {
                SlotOptions = module.DynamicHangar == DynamicHangarOptions.Static
                            ? module.hangarShipUID
                            : module.DynamicHangar.ToString();
            }
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
                Facing = slot.Module.FacingDegrees;
                if (slot.Module.ModuleType == ShipModuleType.Hangar)
                    SlotOptions = slot.Module.hangarShipUID;
            }
        }

        public override string ToString() => $"{ModuleUID} {Position} {Facing} {Restrictions} T={ModuleOrNull}";

        [XmlIgnore] [JsonIgnore]
        public bool IsDummy => ModuleUID == null || ModuleUID == "Dummy";

        [XmlIgnore] [JsonIgnore]
        public ShipModule ModuleOrNull => ModuleUID != null && ModuleUID != "Dummy" && 
                                          ResourceManager.GetModuleTemplate(ModuleUID, out ShipModule m) ? m : null;

        [XmlIgnore] [JsonIgnore]
        public Point PosAsPoint => new Point((int)Position.X, (int)Position.Y);

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

        public ModuleSlotData GetClone()
        {
            return (ModuleSlotData)MemberwiseClone();
        }

        /// <summary>
        /// Similar to GetClone(), however it discards Ship Save state variables
        /// </summary>
        public ModuleSlotData GetStatelessClone()
        {
            return new ModuleSlotData(
                xmlPos: Position,
                restrictions: Restrictions,
                moduleUid: ModuleUID,
                facing: Facing,
                orientation: Orientation,
                slotOptions: SlotOptions
            );
        }

        /// <summary>
        /// Sorter for ModuleSlotData[], orders ModuleSlots grid in scanline order:
        /// 0 1 2 3
        /// 4 5 6 7
        /// </summary>
        public static int Sorter(ModuleSlotData a, ModuleSlotData b)
        {
            // Array.Sort bug in .NET 4.5.2:
            if (object.ReferenceEquals(a, b))
                return 0;

            // first by scanline (Y axis)
            if (a.Position.Y < b.Position.Y) return -1;
            if (a.Position.Y > b.Position.Y) return +1;

            // and then sort by column (X axis)
            if (a.Position.X < b.Position.X) return -1;
            if (a.Position.X > b.Position.X) return +1;

            // they are equal?? this must not happen for valid designs
            Log.Error($"Slots a={a.Position} {a.ModuleUID} {a.Restrictions} and "+
                      $"b={b.Position} {b.ModuleUID} {b.Restrictions} have overlapping positions");
            return 0;
        }
    }
}