using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System.Linq;
using Microsoft.Xna.Framework;

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

        public Vector2 ModuleCenter()
        {
            if (Module?.UID.IsEmpty() ?? true) return Vector2.Zero;
            return  new Vector2(PQ.enclosingRect.X + 16 * Module.XSIZE / 2
                , PQ.enclosingRect.Y + 16 * Module.YSIZE / 2);
        }

        public Rectangle ModuleRectangle()
        {
            return new Rectangle(PQ.enclosingRect.X,
                PQ.enclosingRect.Y
                , 16 * Module.XSIZE, 16 * Module.YSIZE);
        }

        public void SetValidity(ShipModule module = null)
        {
            ShowValid = CanSlotSupportModule(module);
        }
    }
}