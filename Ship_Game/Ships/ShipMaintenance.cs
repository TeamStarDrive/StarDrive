using System;

namespace Ship_Game.Ships
{
    public class ShipMaintenance
    {
        private const float BaseMaintModifier = 0.004f;
        //private readonly ShipData shipData = new ShipData();

        private bool IsFreeUpkeepShip(ShipData.RoleName role, Empire empire, Ship ship)
        {
            return ship.shipData.ShipStyle == "Remnant"
                   || empire?.data == null
                   || ship.loyalty.data.PrototypeShip == ship.Name
                   || (ship.Mothership != null && role >= ShipData.RoleName.fighter && role <= ShipData.RoleName.frigate);
        }

        public float GetMaintenanceCost(Empire empire, Ship ship, float shipCost, bool withModifiers = false, int numShipYards = 0)
        {
            ShipData.RoleName role = ship.shipData.HullRole;
            if (IsFreeUpkeepShip(role, empire, ship))
                return 0;

            float maint = shipCost * BaseMaintModifier;
            if (role == ShipData.RoleName.freighter || role == ShipData.RoleName.platform)
            {
                maint *= empire.data.CivMaintMod;
                if (empire.data.Privatization)
                    maint *= 0.5f;
            }
            if (!withModifiers)
                return  (float)Math.Round(maint, 2);


            // Subspace Projectors do not get any more modifiers
            if (ship.Name == "Subspace Projector")
                return maint;
            //added by gremlin shipyard exploit fix
            if (ship.IsTethered())
            {
                if (role == ShipData.RoleName.platform)
                    return maint * 0.5f;
                if (ship.shipData.IsShipyard)
                {
                    if (numShipYards > 3)
                        maint *= numShipYards - 3;
                }
            }
            float repairMaintModifier = 2 - ship.Health / ship.HealthMax;
            maint *= repairMaintModifier;
            return maint;
        }
    }
}