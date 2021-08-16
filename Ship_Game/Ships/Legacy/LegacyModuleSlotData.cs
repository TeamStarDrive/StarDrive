using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships.Legacy
{
    /// <summary>
    /// Legacy copy of the old ModuleSlotData - for backwards compatibility with old Ship XML-s
    /// </summary>
    public sealed class LegacyModuleSlotData : IEquatable<LegacyModuleSlotData>
    {
        public Vector2 Position;
        public Restrictions Restrictions;

        [XmlElement(ElementName = "InstalledModuleUID")]
        [JsonProperty("InstalledModuleUID"/*, ItemConverterType = typeof(InterningStringConverter)*/)]
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
        public LegacyModuleSlotData()
        {
        }

        // New Empty Slot
        public LegacyModuleSlotData(Vector2 xmlPos, Restrictions restrictions)
        {
            Position = xmlPos;
            Restrictions = restrictions;
        }
        
        // This is the most correct way to create a new ModuleSlotData instance
        // With initial position, restrictions, etc.
        // 
        // NOTE: In ShipData when these are parsed, the strings will be Interned
        public LegacyModuleSlotData(Vector2 xmlPos,
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

        public override string ToString() => $"{Position} {ModuleUID} {Facing} {Orientation} {SlotOptions} {Restrictions} T={ModuleOrNull}";

        [XmlIgnore] [JsonIgnore]
        public bool IsDummy => ModuleUID == null || ModuleUID == "Dummy";

        [XmlIgnore] [JsonIgnore]
        public ShipModule ModuleOrNull => ModuleUID != null && ModuleUID != "Dummy" && 
                                          ResourceManager.GetModuleTemplate(ModuleUID, out ShipModule m) ? m : null;

        [XmlIgnore] [JsonIgnore]
        public Point PosAsPoint => new Point((int)Position.X, (int)Position.Y);

        // Gets the size of this slot, correctly oriented
        public Point GetSize()
        {
            ShipModule m = ModuleOrNull;
            return m?.GetOrientedSize(Orientation) ?? new Point(1, 1);
        }

        static string[] Orientations;

        public static string GetOrientationString(ModuleOrientation orientation)
        {
            if (Orientations == null)
                Orientations = new []{ "Normal", "Left", "Right", "Rear" };
            return Orientations[(int)orientation];
        }

        public ModuleOrientation GetOrientation()
        {
            if (Orientation.NotEmpty() && Orientation != "Normal")
                return (ModuleOrientation)Enum.Parse(typeof(ModuleOrientation), Orientation);
            return ModuleOrientation.Normal;
        }

        public bool Equals(LegacyModuleSlotData s)
        {
            if (s == null) return false;
            return Position == s.Position
                && Restrictions == s.Restrictions
                && ModuleUID == s.ModuleUID
                && Facing == s.Facing
                && Orientation == s.Orientation
                && SlotOptions == s.SlotOptions
                && HangarshipGuid == s.HangarshipGuid;
        }

        public override bool Equals(object obj)
        {
            return this.Equals((LegacyModuleSlotData)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Restrictions;
                hashCode = (hashCode * 397) ^ (ModuleUID != null ? ModuleUID.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Facing.GetHashCode();
                hashCode = (hashCode * 397) ^ (Orientation != null ? Orientation.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SlotOptions != null ? SlotOptions.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Health.GetHashCode();
                hashCode = (hashCode * 397) ^ ShieldPower.GetHashCode();
                hashCode = (hashCode * 397) ^ HangarshipGuid.GetHashCode();
                return hashCode;
            }
        }

        public LegacyModuleSlotData GetClone()
        {
            return (LegacyModuleSlotData)MemberwiseClone();
        }

        /// <summary>
        /// Similar to GetClone(), however it discards Ship Save state variables
        /// </summary>
        public LegacyModuleSlotData GetStatelessClone()
        {
            var slot = new LegacyModuleSlotData(
                xmlPos: Position,
                restrictions: Restrictions,
                moduleUid: ModuleUID,
                facing: Facing,
                orientation: Orientation,
                slotOptions: SlotOptions
            );
            return slot;
        }

        /// <summary>
        /// Sorter for ModuleSlotData[], orders ModuleSlots grid in scanline order:
        /// 0 1 2 3
        /// 4 5 6 7
        /// </summary>
        public static int Sorter(LegacyModuleSlotData a, LegacyModuleSlotData b)
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
