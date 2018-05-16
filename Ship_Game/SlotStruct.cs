using System;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class SlotStruct
    {
        public Restrictions Restrictions;
        public PrimitiveQuad PQ;
        public float Facing;
        public bool PowerChecked; // this conduit or power plant already checked?
        public bool InPowerRadius; // is this slot covered by a power radius?
        public SlotStruct Parent;
        public ShipDesignScreen.ActiveModuleState State;
        public ModuleSlotData SlotReference;
        public string ModuleUID;
        public ShipModule Module;
        public string SlotOptions;
        public Texture2D Tex;
        public bool ShowValid = true;

        public SlotStruct()
        {
        }

        public SlotStruct(ModuleSlotData slot, Vector2 offset)
        {
            Enum.TryParse(slot.Orientation, out ShipDesignScreen.ActiveModuleState slotState);
            Vector2 pos = slot.Position;
            PQ            = new PrimitiveQuad(pos.X + offset.X - 8f, pos.Y + offset.Y - 8f, 16f, 16f);
            Restrictions  = slot.Restrictions;
            Facing        = slot.Facing;
            ModuleUID     = slot.InstalledModuleUID;
            SlotReference = slot;
            State         = slotState;
            SlotOptions   = slot.SlotOptions;
        }

        public SlotStruct(SlotStruct parent)
        {
            PQ            = parent.PQ;
            Restrictions  = parent.Restrictions;
            Facing        = parent.Facing;
            ModuleUID     = parent.ModuleUID;
            Module        = parent.Module;
            State         = parent.State;
            SlotReference = parent.SlotReference;
        }

        public override string ToString() => $"UID={ModuleUID} {Position} {Facing} {Restrictions}";

        private bool CanSlotSupportModule(ShipModule module)
        {
            if (module == null || module.Restrictions == Restrictions.IOE || module.Restrictions == Restrictions)
                return true;

            if (module.Restrictions <= Restrictions.IOE )
            {
                string moduleFitsToSlots = module.Restrictions.ToString();
                return Restrictions.ToString().Any(slotCapability => moduleFitsToSlots.Any(res => res == slotCapability));
            }

            switch (module.Restrictions)
            {
                case Restrictions.xI:  return Restrictions == Restrictions.I;
                case Restrictions.xIO: return Restrictions == Restrictions.IO;
                case Restrictions.xO:  return Restrictions == Restrictions.O;
                default:               return false;
            }
        }

        public void Draw(SpriteBatch sb, Texture2D texture, Color tint)
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
        [XmlIgnore][JsonIgnore] public Point IntSize => new Point(PQ.W/16, PQ.H/16);
        [XmlIgnore][JsonIgnore] public Point IntPos  => new Point(PQ.X/16, PQ.Y/16);
        [XmlIgnore][JsonIgnore] public Rectangle IntRect => new Rectangle(PQ.X/16, PQ.Y/16, PQ.W/16, PQ.H/16);

        public Vector2 Center()
        {
            if (Module?.UID.IsEmpty() ?? true)
                return Vector2.Zero;
            return new Vector2(PQ.X + Module.XSIZE*8, PQ.Y + Module.YSIZE*8);
        }

        public Rectangle ModuleRect => new Rectangle(PQ.X, PQ.Y, Module.XSIZE * 16, Module.YSIZE * 16);

        public bool Intersects(Rectangle r) => PQ.Rect.Intersects(r);

        public void SetValidity(ShipModule module = null)
        {
            ShowValid = CanSlotSupportModule(module);
        }

        public void Clear()
        {
            ModuleUID = null;
            Tex       = null;
            Module    = null;
            Parent    = null;
            State     = ShipDesignScreen.ActiveModuleState.Normal;
        }
    }
}