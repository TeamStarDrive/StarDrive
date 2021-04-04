using System;

namespace Ship_Game.Ships
{
    public static class ShipMaintenance // Created by Fat Bastard - to unify and provide a baseline for future maintenance features
    {
        private const float MaintModifierRealism = 0.004f;
        private const float MaintModifierBySize  = 0.01f;
        public static float TroopMaint           = 0.1f;

        private static bool IsFreeUpkeepShip(Empire empire, Ship ship)
        {
            return ship.loyalty.WeAreRemnants
                   || empire?.data == null
                   || ship.Name == ship.loyalty.data.PrototypeShip
                   || !ship.CanBeRefitted; 
        }

        // Note, this is for ship design screen only. So it is always for the Player empire
        public static float GetMaintenanceCost(ShipData ship, float cost, int totalHangarArea, int troopCount)
        {
            float maint = GetBaseMainCost(ship.HullRole, ship.FixedCost > 0 ? ship.FixedCost : cost, 
                ship.ModuleSlots.Length + totalHangarArea, EmpireManager.Player, troopCount);

            return (float)Math.Round(maint, 2);
        }

        public static float GetMaintenanceCost(Ship ship, Empire empire, int troopCount)
        {
            if (IsFreeUpkeepShip(empire, ship))
                return 0;

            float hangarArea = ship.Carrier.AllFighterHangars.Sum(m => m.MaximumHangarShipSize);
            float maint      = GetBaseMainCost(ship.shipData.HullRole, ship.GetCost(empire), ship.SurfaceArea + hangarArea, empire, troopCount);

            // Projectors do not get any more modifiers

            if (ship.IsSubspaceProjector)
                 return maint;

            // Reduced maintenance for shipyards (sitting ducks, no offense) Shipyards are limited to 3.
            if (ship.shipData.IsShipyard)
                maint *= 0.4f;

            return maint;
        }

        private static float GetBaseMainCost(ShipData.RoleName role, float shipCost, float surfaceArea, Empire empire, int numTroops)
        {
            bool realism = GlobalStats.ActiveModInfo != null
                           && GlobalStats.ActiveModInfo.UseProportionalUpkeep;

            float maint = realism ? shipCost * MaintModifierRealism : surfaceArea * MaintModifierBySize;

            switch (role)
            {

                case ShipData.RoleName.station:
                case ShipData.RoleName.platform:                maint *= 0.7f; break;
                case ShipData.RoleName.troop:                   maint *= 0.5f; break;
                case ShipData.RoleName.corvette   when realism: maint *= 0.9f; break;
                case ShipData.RoleName.frigate    when realism: maint *= 0.8f; break;
                case ShipData.RoleName.cruiser    when realism: maint *= 0.7f; break;
                case ShipData.RoleName.battleship when realism: maint *= 0.6f; break;
                case ShipData.RoleName.capital    when realism: maint *= 0.5f; break;
            }

            if (role == ShipData.RoleName.freighter || role == ShipData.RoleName.platform || role == ShipData.RoleName.station)
            {
                maint *= empire.data.CivMaintMod;
                if (empire.data.Privatization)
                    maint *= 0.5f;
            }

            maint += maint * empire.data.Traits.MaintMod + numTroops * TroopMaint;

            return maint * GlobalStats.ShipMaintenanceMulti;
        }
    }
}