using System;
using Ship_Game.Ships;
using Microsoft.Xna.Framework;

namespace Ship_Game.Gameplay
{
    // Used only in Hull definitions
    public sealed class HullSlot
    {
        public readonly Point Pos; // integer position in the design, such as [0, 1]
        public readonly Restrictions R; // this slots restrictions
        
        // Required by ModuleGrid
        public Point GetSize() => new Point(1, 1);

        public override string ToString() => $"{R} {Pos}";

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
        
        // Required by ModuleGrid
        public Point GetSize() => Size;

        public override string ToString() => $"{Pos} {ModuleUID} {Size} TA:{TurretAngle} MR:{ModuleRot} HS:{HangarShipUID}";
        
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
        public DesignSlot(ShipModule s)
        {
            Pos = s.Pos;
            ModuleUID = s.UID;
            Size = s.GetSize();
            TurretAngle = s.TurretAngle;
            ModuleRot = s.ModuleRot;
            HangarShipUID = s.HangarShipUID;
        }

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

        public static DesignSlot[] FromModules(Array<ShipModule> modules)
        {
            var slots = new Array<DesignSlot>();
            for (int i = 0; i < modules.Count; ++i)
                slots.Add(new DesignSlot(modules[i]));
            return slots.ToArray();
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
        public int HangarShipId;

        public ModuleSaveData(ShipModule m)
            : base(m.Pos, m.UID, m.GetSize(), m.TurretAngle, m.ModuleRot, m.HangarShipUID)
        {
            Health = m.Health;
            ShieldPower = m.ShieldPower;
            HangarShipId = m.HangarShipId;
        }

        public ModuleSaveData(DesignSlot s, float health, float shieldPower, int hangarShipId)
            : base(s)
        {
            Health = health;
            ShieldPower = shieldPower;
            HangarShipId = hangarShipId;
        }

        public DesignSlot ToDesignSlot() // abandon the state
        {
            return new DesignSlot(Pos, ModuleUID, Size, TurretAngle, ModuleRot, HangarShipUID);
        }
    }
}
