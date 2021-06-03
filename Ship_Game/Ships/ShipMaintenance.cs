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

        public static float GetMaintenanceCost(Ship ship, Empire empire, int troopCount)
        {
            if (IsFreeUpkeepShip(empire, ship))
                return 0;

            float maint = GetBaseMainCost(ship, empire, troopCount);

            // Projectors do not get any more modifiers
            if (ship.IsSubspaceProjector)
                 return maint;

            // Reduced maintenance for shipyards (sitting ducks, no offense) Shipyards are limited to 3.
            if (ship.shipData.IsShipyard)
                maint *= 0.4f;

            return maint;
        }

        static float GetBaseMainCost(Ship ship, Empire empire, int numTroops)
        {
            bool realism = GlobalStats.ActiveModInfo?.UseProportionalUpkeep == true;

            float maint;
            if (ship.shipData.FixedUpkeep > 0)
            {
                maint = ship.shipData.FixedUpkeep;
            }
            else
            {
                float shipCost = ship.GetCost(empire);
                float hangarsArea = ship.Carrier.AllFighterHangars.Sum(m => m.MaximumHangarShipSize);
                float surfaceArea = ship.SurfaceArea + hangarsArea;
                maint = realism ? shipCost * MaintModifierRealism : surfaceArea * MaintModifierBySize;
            }

            ShipData.RoleName role = ship.shipData.HullRole;
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

            if (role == ShipData.RoleName.freighter ||
                role == ShipData.RoleName.platform ||
                role == ShipData.RoleName.station)
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