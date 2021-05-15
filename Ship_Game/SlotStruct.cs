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
        public Restrictions Restrictions;
        public PrimitiveQuad PQ;
        public float Facing; // Facing is the turret aiming dir
        public ModuleOrientation Orientation; // Orientation controls the visual 4-dir rotation of module
        public bool PowerChecked; // this conduit or power plant already checked?
        public bool InPowerRadius; // is this slot covered by a power radius?
        public SlotStruct Parent;
        public ModuleSlotData SlotReference;
        public string ModuleUID;
        public ShipModule Module;
        public string SlotOptions;
        public SubTexture Tex;

        public SlotStruct()
        {
        }

        public SlotStruct(ModuleSlotData slot, Vector2 offset)
        {
            Enum.TryParse(slot.Orientation, out ModuleOrientation slotState);
            Vector2 pos = slot.Position;
            PQ            = new PrimitiveQuad(pos.X + offset.X - 8f, pos.Y + offset.Y - 8f, 16f, 16f);
            Restrictions  = slot.Restrictions;
            Facing        = slot.Facing;
            ModuleUID     = slot.InstalledModuleUID;
            SlotReference = slot;
            Orientation   = slotState;
            SlotOptions   = slot.SlotOptions;
        }

        public SlotStruct(SlotStruct parent)
        {
            PQ            = parent.PQ;
            Restrictions  = parent.Restrictions;
            Facing        = parent.Facing;
            ModuleUID     = parent.ModuleUID;
            Module        = parent.Module;
            Orientation         = parent.Orientation;
            SlotReference = parent.SlotReference;
        }

        public override string ToString()
        {
            if (Parent == null)
                return $"{Module?.UID} {Position} F:{Facing} R:{Restrictions}";

            // @note Don't call Parent.ToString(), or we might get a stack overflow
            string parent = $"{Parent.Position} F:{Parent.Facing} R:{Parent.Restrictions}";
            return $"{Position} F:{Facing} R:{Restrictions}   Parent={{{parent}}}";
        }


        private static bool MatchI(Restrictions b) => b == Restrictions.I || b == Restrictions.IO || b == Restrictions.IE;
        private static bool MatchO(Restrictions b) => b == Restrictions.O || b == Restrictions.IO || b == Restrictions.OE;
        private static bool MatchE(Restrictions b) => b == Restrictions.E || b == Restrictions.IE || b == Restrictions.OE;

        private static bool IsPartialMatch(Restrictions a, Restrictions b)
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

        public void Draw(SpriteBatch sb, SubTexture texture, Color tint)
        {
            Rectangle rect = Module == null ? PQ.Rect : ModuleRect;
            sb.Draw(texture, rect, tint);
        }

        [XmlIgnore][JsonIgnore] public Vector2 PosVec2      => new Vector2(PQ.X, PQ.Y);
        [XmlIgnore][JsonIgnore] public Vector2 ModuleCenter => new Vector2(PQ.X + PQ.W/2, PQ.Y + PQ.H/2);

        [XmlIgnore][JsonIgnore] public Point Position     => new Point(PQ.X, PQ.Y);
        [XmlIgnore][JsonIgnore] public Point ModuleSize   => new Point(PQ.W, PQ.H);

        // Width and Height in 1x1, 2x2, etc
        [XmlIgnore][JsonIgnore] public int Width  => PQ.W/16;
        [XmlIgnore][JsonIgnore] public int Height => PQ.H/16;
        [XmlIgnore][JsonIgnore] public Point IntPos  => new Point(PQ.X/16, PQ.Y/16);
        [XmlIgnore][JsonIgnore] public Point IntSize => new Point(PQ.W/16, PQ.H/16);
        [XmlIgnore][JsonIgnore] public Rectangle IntRect => new Rectangle(PQ.X/16, PQ.Y/16, PQ.W/16, PQ.H/16);

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