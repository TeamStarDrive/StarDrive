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

        public float Facing; // Facing is the turret aiming dir
        public Restrictions Restrictions;
        public ModuleOrientation Orientation; // Orientation controls the visual 4-dir rotation of module
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
            GridPos = slot.P;
            Restrictions = slot.R;

            GridCenter = new Point(hull.Size.X / 2, hull.Size.Y / 2);
            WorldPos = new Vector2(slot.P.X - GridCenter.X, slot.P.Y - GridCenter.Y) * 16f;
        }

        public override string ToString()
        {
            if (Parent == null)
                return $"{Module?.UID} {Position} R:{Restrictions} F:{Facing} O:{Orientation}";

            // @note Don't call Parent.ToString(), or we might get a stack overflow
            string parent = $"{Parent.Position} R:{Parent.Restrictions} F:{Parent.Facing} O:{Orientation}";
            return $"{Position} R:{Restrictions} F:{Facing} O:{Orientation}   Parent={{{parent}}}";
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
            var size = new Vector2(16f);

            if (Module != null)
                size = new Vector2(Module.XSIZE * 16f, Module.YSIZE * 16f);

            screen.ProjectToScreenCoords(WorldPos, size, 
                                         out Vector2 posOnScreen, out Vector2 sizeOnScreen);
            Rectangle rect = new RectF(posOnScreen, sizeOnScreen);
            sb.Draw(texture, rect, tint);
        }

        [XmlIgnore][JsonIgnore] public Vector2 Center
        {
            get
            {
                if (Module?.UID.IsEmpty() ?? true)
                    return Vector2.Zero;
                return new Vector2(PQ.X + Module.XSIZE * 8, PQ.Y + Module.YSIZE * 8);
            }
        }

        public Rectangle ModuleRect => new Rectangle(PQ.X, PQ.Y, Module.XSIZE * 16, Module.YSIZE * 16);
        public Rectangle GetProjectedRect(ShipModule m) => new Rectangle(PQ.X, PQ.Y, m.XSIZE * 16, m.YSIZE * 16);

        public bool Intersects(Rectangle r) => PQ.Rect.Intersects(r);

        public void Clear()
        {
            ModuleUID   = null;
            Tex         = null;
            Module      = null;
            Parent      = null;
            Orientation = ModuleOrientation.Normal;
        }

        public SlotStruct Root => Parent ?? this;

        public bool IsModuleReplaceableWith(ShipModule other)
        {
            return Module              != null
                && ModuleUID           != null
                && Module.XSIZE        == other.XSIZE
                && Module.YSIZE        == other.YSIZE
                && Module.Restrictions == other.Restrictions;
        }

        public bool IsSame(ShipModule module, ModuleOrientation orientation, float facing)
        {
            return Module != null
                && Module.UID == module.UID
                && Module.hangarShipUID == module.hangarShipUID
                && Orientation == orientation
                && Facing.AlmostEqual(facing);
        }
    }
}