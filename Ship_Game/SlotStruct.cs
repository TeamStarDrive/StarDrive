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
        public bool ShowValid = true; //These values are not being set correctly in the shipyard so they are always default. false
        public bool ShowInvalid = true;
    }
}