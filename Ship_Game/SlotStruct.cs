using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class SlotStruct
    {
        // Integer position in the local grid, top-left is [0,0]
        public Point GridPos;

        // Center of the design grid
        public Point GridCenter;

        // Position of this slot in the world, where center of the Grid
        // is at world [0,0]
        public Vector2 WorldPos;

        public int TurretAngle;
        public Restrictions Restrictions;
        public ModuleOrientation ModuleRot; // Orientation controls the visual 4-dir rotation of module
        public SlotStruct Parent;
        public string ModuleUID;
        public ShipModule Module;
        public string SlotOptions;
        public SubTexture Tex;

        public SlotStruct()
        {
        }

        public SlotStruct(HullSlot slot, ShipHull hull)
        {
            GridPos = slot.Pos;
            Restrictions = slot.R;

            GridCenter = hull.GridCenter;
            WorldPos = new Vector2(slot.Pos.X - GridCenter.X, slot.Pos.Y - GridCenter.Y) * 16f;
        }

        [XmlIgnore][JsonIgnore]
        public Point OffsetFromGridCenter => new Point(GridPos.X - GridCenter.X, GridPos.Y - GridCenter.Y);

        public override string ToString()
        {
            if (Parent == null)
                return $"{Module?.UID} {GridPos} R:{Restrictions} TA:{TurretAngle} O:{ModuleRot}";

            // @note Don't call Parent.ToString(), or we might get a stack overflow
            string parent = $"{Parent.GridPos} R:{Parent.Restrictions} TA:{Parent.TurretAngle} O:{ModuleRot}";
            return $"{GridPos} R:{Restrictions} TA:{TurretAngle} O:{ModuleRot}   Parent={{{parent}}}";
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
            if (module == null || module.Restrictions == Restrictions.IOE || module.Restrictions == Restrictions)
                return true;

            if (module.Restrictions <= Restrictions.IOE)
                return IsPartialMatch(Restrictions, module.Restrictions);

            switch (module.Restrictions) // exclusive restrictions
            {
                case Restrictions.xI:  return Restrictions == Restrictions.I;
                case Restrictions.xIO: return Restrictions == Restrictions.IO;
                case Restrictions.xO:  return Restrictions == Restrictions.O;
            }
            return false;
        }

        public void Draw(SpriteBatch sb, GameScreen screen, SubTexture texture, Color tint)
        {
            screen.ProjectToScreenCoords(WorldPos, WorldSize, out Vector2 posOnScreen, out Vector2 sizeOnScreen);
            Rectangle rect = new RectF(posOnScreen, sizeOnScreen);
            sb.Draw(texture, rect, tint);
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
            ModuleUID   = null;
            Tex         = null;
            Module      = null;
            Parent      = null;
            ModuleRot = ModuleOrientation.Normal;
        }

        [XmlIgnore][JsonIgnore]
        public SlotStruct Root => Parent ?? this;

        public bool IsModuleReplaceableWith(ShipModule other)
        {
            return Module              != null
                && ModuleUID           != null
                && Module.XSIZE        == other.XSIZE
                && Module.YSIZE        == other.YSIZE
                && Module.Restrictions == other.Restrictions;
        }

        public bool IsSame(ShipModule module, ModuleOrientation orientation, int turretAngle)
        {
            return Module != null
                && Module.UID == module.UID
                && Module.HangarShipUID == module.HangarShipUID
                && ModuleRot == orientation
                && TurretAngle == turretAngle;
        }
    }
}