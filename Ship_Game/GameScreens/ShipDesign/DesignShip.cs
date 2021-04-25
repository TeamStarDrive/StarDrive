using Ship_Game.Gameplay;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game.GameScreens.ShipDesign
{
    public class DesignShip : Ship
    {
        ModuleSlotData[] PrevSlots;

        public ShipDesignStats DesignStats;

        public DesignShip(ShipData designHull)
            : base(EmpireManager.Player, designHull, fromSave:false, 
                    isTemplate:true, shipyardDesign:true)
        {
            DesignStats = new ShipDesignStats(this);
        }

        public void UpdateDesign(ModuleSlotData[] placedSlots)
        {
            if (PrevSlots != null && AreEqual(PrevSlots, placedSlots))
                return;

            PrevSlots = placedSlots;
            CreateModuleSlotsFromData(placedSlots, fromSave:false, 
                                      isTemplate:true, shipyardDesign:true);
            InitializeShip();
            DesignStats.Update();
        }

        static bool AreEqual(ModuleSlotData[] slotsA, ModuleSlotData[] slotsB)
        {
            if (slotsA.Length != slotsB.Length)
                return false;
            for (int i = 0; i < slotsA.Length; ++i)
                if (!slotsA[i].Equals(slotsB[i]))
                    return false;
            return true;
        }
    }
}
