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
            string moduleFitsToSlots = module.Restrictions.ToString();

            if (module.Restrictions <= Restrictions.IOE )
                return Restrictions.ToString().Any(slotCapability => moduleFitsToSlots.Any(res => res == slotCapability));
            switch (module.Restrictions)
            {
                case Restrictions.xI:
                    return Restrictions == Restrictions.I;
                case Restrictions.xIO:
                    return Restrictions == Restrictions.IO;
                case Restrictions.xO:
                    return Restrictions == Restrictions.O;
                default:
                    return false;
            }

        }

        public void Draw(SpriteBatch sb, Texture2D texture, Color tint)
        {
            Rectangle rect = Module == null ? PQ.Rect : ModuleRect;
            sb.Draw(texture, rect, tint);
        }

        [XmlIgnore][JsonIgnore] public Vector2 Position     => new Vector2(PQ.X, PQ.Y);
        [XmlIgnore][JsonIgnore] public Vector2 ModuleCenter => new Vector2(PQ.X + PQ.W/2, PQ.Y + PQ.H/2);

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
    }
}