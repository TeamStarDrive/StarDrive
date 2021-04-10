using Ship_Game.Gameplay;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public class DesignShip : Ship
    {
        public DesignShip(ShipData designHull)
            : base(EmpireManager.Player, designHull, fromSave:false, 
                    isTemplate:true, shipyardDesign:true) {}
        public void UpdateDesign(ModuleSlotData[] placedSlots)
        {
            CreateModuleSlotsFromData(placedSlots, fromSave:false, 
                                      isTemplate:true, shipyardDesign:true);
            InitializeShip();
        }
    }
}
