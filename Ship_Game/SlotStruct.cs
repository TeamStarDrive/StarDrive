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
        public bool CheckedConduits;
        public SlotStruct Parent;
        public ShipDesignScreen.ActiveModuleState State;
        public bool Powered;
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

        public bool IsNeighbourTo(SlotStruct slot)
        {
            int absDx = Math.Abs(slot.PQ.X - PQ.X) / 16;
            int absDy = Math.Abs(slot.PQ.Y - PQ.Y) / 16;
            return (absDx + absDy) == 1;
        }

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


        [XmlIgnore][JsonIgnore] public Vector2 Position     => new Vector2(PQ.X, PQ.Y);
        [XmlIgnore][JsonIgnore] public Vector2 ModuleCenter => new Vector2(PQ.X + PQ.W/2, PQ.Y + PQ.H/2);
        [XmlIgnore][JsonIgnore] public Vector2 ModuleSize   => new Vector2(PQ.W, PQ.H);

        // Width and Height in 1x1, 2x2, etc
        [XmlIgnore][JsonIgnore] public int Width  => (int)(PQ.W / 16.0f);
        [XmlIgnore][JsonIgnore] public int Height => (int)(PQ.H / 16.0f);
        [XmlIgnore][JsonIgnore] public Point Size => new Point((int)(PQ.W / 16.0f), (int)(PQ.H / 16.0f));

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