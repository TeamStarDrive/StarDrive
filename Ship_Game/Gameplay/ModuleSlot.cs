using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Gameplay
{
    // Used only in Hull definitions
    public sealed class HullSlot
    {
        public Point Pos; // integer position in the design, such as [0, 1]
        public Restrictions R; // this slots restrictions

        public HullSlot() {}
        public HullSlot(int x, int y, Restrictions r)
        {
            Pos = new Point(x, y);
            R = r;
        }
        public HullSlot(HullSlot slot)
        {
            Pos = slot.Pos;
            R = slot.R;
        }
        public Point GetGridPos() => Pos;
        public Point GetSize() => new Point(1, 1);
        public override string ToString() => $"{R} {Pos}";

        /// <summary>
        /// Sorter for HullSlot[], orders HullSlot grid in scanline order:
        /// 0 1 2 3
        /// 4 5 6 7
        /// </summary>
        public static int Sorter(HullSlot a, HullSlot b)
        {
            // Array.Sort bug in .NET 4.5.2:
            if (object.ReferenceEquals(a, b))
                return 0;

            // first by scanline (Y axis)
            if (a.Pos.Y < b.Pos.Y) return -1;
            if (a.Pos.Y > b.Pos.Y) return +1;

            // and then sort by column (X axis)
            if (a.Pos.X < b.Pos.X) return -1;
            if (a.Pos.X > b.Pos.X) return +1;
            return 0;
        }
    }

    // Used only in new Ship designs
    // # gridX,gridY; moduleUIDIndex; sizeX,sizeY; turretAngle; moduleRotation; slotOptions
    public class DesignSlot : IEquatable<DesignSlot>
    {
        public Point Pos; // integer position in the design, such as [0, 1]
        public string ModuleUID; // module UID, must be interned during parsing
        public Point Size; // integer size, default is 1,1, for a 1x2 module with ModuleRot.Left it is [2,1]
        public int TurretAngle; // angle 0..360 of a mounted turret
        public ModuleOrientation ModuleRot; // module's orientation/rotation: Normal,Left,Right,Rear
        public string HangarShipUID; // null by default, only set if there are any options
        
        public DesignSlot() {}
        public DesignSlot(Point pos, string uid, Point size, int turretAngle,
                          ModuleOrientation moduleRot, string hangarShipUID)
        {
            Pos = pos;
            ModuleUID = uid;
            Size = size;
            TurretAngle = turretAngle;
            ModuleRot = moduleRot;
            HangarShipUID = hangarShipUID;
        }
        public DesignSlot(DesignSlot s)
        {
            Pos = s.Pos;
            ModuleUID = s.ModuleUID;
            Size = s.Size;
            TurretAngle = s.TurretAngle;
            ModuleRot = s.ModuleRot;
            HangarShipUID = s.HangarShipUID;
        }

        public Point GetGridPos() => Pos;
        public Point GetSize() => new Point(1, 1);
        public override string ToString() => $"{Pos} {ModuleUID} {Size} TA:{TurretAngle} MR:{ModuleRot} HS:{HangarShipUID}";

        public bool Equals(DesignSlot s)
        {
            if (s == null) return false;
            return Pos == s.Pos
                && ModuleUID == s.ModuleUID
                && Size == s.Size
                && TurretAngle == s.TurretAngle
                && ModuleRot == s.ModuleRot
                && HangarShipUID == s.HangarShipUID;
        }

        /// <summary>
        /// Sorter for DesignSlot[], orders DesignSlot grid in scanline order:
        /// 0 1 2 3
        /// 4 5 6 7
        /// </summary>
        public static int Sorter(DesignSlot a, DesignSlot b)
        {
            // Array.Sort bug in .NET 4.5.2:
            if (object.ReferenceEquals(a, b))
                return 0;

            // first by scanline (Y axis)
            if (a.Pos.Y < b.Pos.Y) return -1;
            if (a.Pos.Y > b.Pos.Y) return +1;

            // and then sort by column (X axis)
            if (a.Pos.X < b.Pos.X) return -1;
            if (a.Pos.X > b.Pos.X) return +1;
            return 0;
        }
    }

    /// <summary>
    /// Active ships must be saved with all of their DesignSlot data
    /// and their ShipModule state data.
    ///
    /// This is because players can modify an existing ship .design,
    /// making old designs obsolete.
    /// </summary>
    public sealed class ModuleSaveData : DesignSlot
    {
        /// --- Saved ShipModule state ---
        public float Health;
        public float ShieldPower;
        public Guid HangarShip;

        public ModuleSaveData(ShipModule m)
            : base(m.GridPos, m.UID, m.GetSize(), m.TurretAngle, m.ModuleRot, m.HangarShipUID)
        {
            Health = m.Health;
            ShieldPower = m.ShieldPower;
            HangarShip = m.HangarShipGuid;
        }

        public ModuleSaveData(DesignSlot s, float health, float shieldPower, in Guid hangarShip)
            : base(s)
        {
            Health = health;
            ShieldPower = shieldPower;
            HangarShip = hangarShip;
        }

        public DesignSlot ToDesignSlot() // abandon the state
        {
            return new DesignSlot(Pos, ModuleUID, Size, TurretAngle, ModuleRot, HangarShipUID);
        }
    }

    public sealed class ModuleSlotData : IEquatable<ModuleSlotData>
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
        // 
        // NOTE: In ShipData when these are parsed, the strings will be Interned
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

        // Save SlotStruct as ModuleSlotData in ShipDesignScreen
        public ModuleSlotData(SlotStruct slot)
        {
            Position     = slot.WorldPos + new Vector2(ShipModule.ModuleSlotOffset);
            Restrictions = slot.Restrictions;
            ModuleUID    = slot.ModuleUID != null ? string.Intern(slot.ModuleUID) : null;
            Orientation  = GetOrientationString(slot.ModuleRot);
            if (slot.Module != null)
            {
                Facing = slot.Module.TurretAngle;
                if (slot.Module.ModuleType == ShipModuleType.Hangar)
                {
                    SlotOptions = string.Intern(slot.Module.HangarShipUID);
                }
            }
        }

        // Convert from new coordinates to Legacy
        public ModuleSlotData(DesignSlot slot)
        {

        }

        public override string ToString() => $"{ModuleUID} {Position} {Facing} {Orientation} {SlotOptions} {Restrictions} T={ModuleOrNull}";

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

        public bool Equals(ModuleSlotData s)
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
            return this.Equals((ModuleSlotData)obj);
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

        public ModuleSlotData GetClone()
        {
            return (ModuleSlotData)MemberwiseClone();
        }

        /// <summary>
        /// Similar to GetClone(), however it discards Ship Save state variables
        /// </summary>
        public ModuleSlotData GetStatelessClone()
        {
            var slot = new ModuleSlotData(
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