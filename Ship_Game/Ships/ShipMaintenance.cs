using System;

namespace Ship_Game.Ships
{
    public static class ShipMaintenance // Created by Fat Bastard - to unify and provide a baseline for future maintenance features
    {
        const float MaintModifierByCost = 0.004f;
        const float MaintModifierBySize = 0.01f;
        public const float TroopMaint = 0.1f;

        static bool IsFreeUpkeepShip(Empire empire, Ship ship)
        {
            return empire.WeAreRemnants
                || empire?.data == null
                || ship.Name == empire.data.PrototypeShip
                || !ship.CanBeRefitted;
        }

        static bool IsFreeUpkeepShip(Empire empire, IShipDesign ship)
        {
            return empire.WeAreRemnants
                || empire?.data == null
                || ship.Name == empire.data.PrototypeShip;
        }

        public static float GetMaintenanceCost(Ship ship, Empire empire, int troopCount)
        {
            if (IsFreeUpkeepShip(empire, ship))
                return 0;
            return GetBaseMaintenance(ship.ShipData, empire, troopCount);
        }

        public static float GetBaseMaintenance(IShipDesign ship, Empire empire, int numTroops)
        {
            if (IsFreeUpkeepShip(empire, ship))
                return 0;

            bool hullUpkeep = empire.Universe.P.UseUpkeepByHullSize;
            float maint;
            if (ship.FixedUpkeep > 0)
            {
                maint = ship.FixedUpkeep;
            }
            else
            {
                float shipCost    = ship.GetCost(empire);
                float hangarsArea = ship.AllFighterHangars.Sum(m => m.MaximumHangarShipSize);
                float surfaceArea = ship.BaseHull.SurfaceArea + hangarsArea;
                maint = hullUpkeep ? surfaceArea * MaintModifierBySize : shipCost * MaintModifierByCost;
            }

            RoleName role = ship.HullRole;
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

            // Projectors do not get any more modifiers
            if (ship.IsSubspaceProjector)
                 return maint;

            maint *= empire.Universe.P.ShipMaintenanceMultiplier;

            // Reduced maintenance for shipyards (sitting ducks, no offense) Shipyards are limited to 2.
            if (ship.IsShipyard)
                maint *= 0.4f;

            return maint;
        }
    }
}