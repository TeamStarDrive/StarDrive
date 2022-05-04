using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using SDGraphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
    public sealed class SlotStruct
    {
        // Integer position in the local grid, top-left is [0,0]
        public readonly Point Pos;

        // Center of the design grid
        public readonly Point GridCenter;

        // Position of this slot in the world, where center of the Grid
        // is at world [0,0]
        public readonly Vector2 WorldPos;

        // HullSlot restriction
        public Restrictions HullRestrict;

        public SlotStruct Parent;
        public string ModuleUID;
        public ShipModule Module;
        public SubTexture Tex;

        public SlotStruct()
        {
        }

        public SlotStruct(HullSlot slot, Point gridCenter)
        {
            Pos = slot.Pos;
            HullRestrict = slot.R;

            GridCenter = gridCenter;
            WorldPos = slot.Pos.Sub(gridCenter).Mul(16f);
        }

        public override string ToString()
        {
            if (Parent == null)
                return $"{Module?.UID} {Pos} R:{HullRestrict}";

            // @note Don't call Parent.ToString(), or we might get a stack overflow
            string parent = $"{Parent.Module?.UID} {Parent.Pos} R:{Parent.HullRestrict}";
            return $"{Pos} R:{HullRestrict} Parent={{{parent}}}";
        }

        static bool MatchI(Restrictions b) => b == Restrictions.I || b == Restrictions.IO || b == Restrictions.IE;
        static bool MatchO(Restrictions b) => b == Restrictions.O || b == Restrictions.IO || b == Restrictions.OE;
        static bool MatchE(Restrictions b) => b == Restrictions.E || b == Restrictions.IE || b == Restrictions.OE;

        static bool IsPartialMatch(Restrictions a, Restrictions b)
        {
            switch (a)
            {
                case Restrictions.I:  return MatchI(b);
                case Restrictions.O:  return MatchO(b);
                case Restrictions.E:  return MatchE(b);
                case Restrictions.IO: return MatchI(b) || MatchO(b);
                case Restrictions.IE: return MatchI(b) || MatchE(b);
                case Restrictions.OE: return MatchO(b) || MatchE(b);
            }
            return false;
        }

        public bool CanSlotSupportModule(ShipModule module)
        {
            if (module == null)
                return true;

            Restrictions r = module.Restrictions;
            if (r == Restrictions.IOE || r == HullRestrict)
                return true;

            if (r <= Restrictions.IOE)
                return IsPartialMatch(HullRestrict, r);

            switch (r) // exclusive restrictions
            {
                case Restrictions.xI:  return HullRestrict == Restrictions.I;
                case Restrictions.xIO: return HullRestrict == Restrictions.IO;
                case Restrictions.xO:  return HullRestrict == Restrictions.O;
            }
            return false;
        }

        // Center of the module in WORLD coordinates
        [XmlIgnore][JsonIgnore]
        public Vector2 Center => WorldPos + WorldSize/2f;

        // Gets the size of the module rectangle in WORLD coordinates
        [XmlIgnore][JsonIgnore]
        public Vector2 WorldSize => Module?.WorldSize ?? new Vector2(16f);

        // Gets the design grid size of the module, such as [1,1] or [2,2]
        [XmlIgnore][JsonIgnore]
        public Point Size => Module?.GetSize() ?? new Point(1,1);

        // Gets the module rectangle in WORLD coordinates
        [XmlIgnore][JsonIgnore]
        public RectF WorldRect => new RectF(WorldPos, WorldSize);
        public RectF GetWorldRectFor(ShipModule m) => new RectF(WorldPos, m.WorldSize);

        public void Clear()
        {
            Parent    = null;
            ModuleUID = null;
            Module    = null;
            Tex       = null;
        }

        [XmlIgnore][JsonIgnore]
        public SlotStruct Root => Parent ?? this;

        public bool IsModuleReplaceableWith(ShipModule other)
        {
            return Module    != null
                && ModuleUID != null
                && Module.XSize == other.XSize
                && Module.YSize == other.YSize
                && Module.Restrictions == other.Restrictions;
        }

        public bool IsSame(ShipModule module, ModuleOrientation orientation, int turretAngle)
        {
            return Module != null
                && Module.UID  == module.UID
                && Module.ModuleRot   == orientation
                && Module.TurretAngle == turretAngle
                && Module.HangarShipUID == module.HangarShipUID;
        }
    }
}