using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

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
        public bool ShowInvalid = true;

        public void SetValidity(ShipModule module = null)
        {
            if (module == null)
            {
                ShowInvalid = true;
                ShowValid   = true;
                return;
            }

            ShowInvalid               = false;
            ShowValid                 = false;
            Restrictions restrictions = module.Restrictions;
            if (restrictions          == Restrictions.I && (Restrictions != Restrictions.E 
                && Restrictions      != Restrictions.O && Restrictions != Restrictions.OE)) ShowValid = true;
            else if (restrictions     == Restrictions.O && (Restrictions != Restrictions.E 
                && Restrictions      != Restrictions.I && Restrictions != Restrictions.IE)) ShowValid = true;
            else if (restrictions     == Restrictions.E && (Restrictions != Restrictions.I 
                && Restrictions      != Restrictions.O && Restrictions != Restrictions.IO)) ShowValid = true;
            else if (restrictions     == Restrictions.IO && Restrictions != Restrictions.E) ShowValid = true;
            else if (restrictions     == Restrictions.IE && Restrictions != Restrictions.O) ShowValid = true;
            else if (restrictions     == Restrictions.OE && Restrictions != Restrictions.I) ShowValid = true;
            else if (restrictions     == Restrictions.IOE) ShowValid = true;
            else ShowInvalid          = true;
        }
    }
}