using Microsoft.Xna.Framework;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game.GameScreens.ShipDesign
{
    public class DesignShip : Ship
    {
        Array<ShipModule> PrevModules;

        public ShipDesignStats DesignStats;

        public DesignShip(Ships.ShipDesign designHull)
            : base(EmpireManager.Player, designHull, isTemplate:true, shipyardDesign:true)
        {
            DesignStats = new ShipDesignStats(this);
            Position = new Vector2(0, 0);
        }

        public void UpdateDesign(Array<ShipModule> placedModules, bool forceUpdate = false)
        {
            if (!forceUpdate && PrevModules != null && AreEqual(PrevModules, placedModules))
                return;

            PrevModules = placedModules;
            CreateModuleSlotsFromShipyardModules(placedModules);
            InitializeShip();
            DesignStats.Update();
        }

        static bool AreEqual(Array<ShipModule> a, Array<ShipModule> b)
        {
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; ++i)
                if (!a[i].Equals(b[i]))
                    return false;
            return true;
        }
    }
}
