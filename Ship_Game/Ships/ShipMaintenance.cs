using System;

namespace Ship_Game.Ships
{
    public static class ShipMaintenance // Created by Fat Bastard - to unify and provide a baseline for future maintenance features
    {
        private const float MaintModifierByCost  = 0.004f;
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
            bool hullUpkeep = GlobalStats.UseUpkeepByHullSize;
            float maint;
            if (ship.shipData.FixedUpkeep > 0)
            {
                maint = ship.shipData.FixedUpkeep;
            }
            else
            {
                float shipCost    = ship.GetCost(empire);
                float hangarsArea = ship.Carrier.AllFighterHangars.Sum(m => m.MaximumHangarShipSize);
                float surfaceArea = ship.SurfaceArea + hangarsArea;
                maint = hullUpkeep ? surfaceArea * MaintModifierBySize : shipCost * MaintModifierByCost;
            }

            RoleName role = ship.shipData.HullRole;
            switch (role)
            {
                case RoleName.station    when !hullUpkeep:
                case RoleName.platform   when !hullUpkeep: maint *= 0.35f; break;
                case RoleName.corvette   when !hullUpkeep: maint *= 0.9f;  break;
                case RoleName.frigate    when !hullUpkeep: maint *= 0.8f;  break;
                case RoleName.cruiser    when !hullUpkeep: maint *= 0.7f;  break;
                case RoleName.battleship when !hullUpkeep: maint *= 0.6f;  break;
                case RoleName.capital    when !hullUpkeep: maint *= 0.5f;  break;
                case RoleName.station:
                case RoleName.platform:                    maint *= 0.7f; break;
                case RoleName.troop:                       maint *= 0.5f; break;
            }

            if (role == RoleName.freighter ||
                role == RoleName.platform ||
                role == RoleName.station)
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