using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System.Linq;

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

            // just check if this slot's capabilities match any in the module placement restrictions
            foreach(char c in Restrictions.ToString())
            {
                
            }

            return Restrictions.ToString().Any(slotCapability => moduleFitsToSlots.Any(res => res == slotCapability));
        }

        public void SetValidity(ShipModule module = null)
        {
            ShowValid = CanSlotSupportModule(module);
        }
    }
}