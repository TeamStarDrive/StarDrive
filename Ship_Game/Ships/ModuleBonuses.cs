using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Ships
{
    public class ModuleBonuses
    {
        public static float GetPowerStoreMax(ShipModule module, Empire empire = null)
        {
            float max = module.PowerStoreMax;
            if (empire == null)
                return max;

            // @note This is unlocked with "Fuel Cell Bonus" tech
            max *= 1.0f + empire.data.FuelCellModifier;
            return Math.Max(0, max);
        }

        public static float GetPowerFlowMax(ShipModule module, Empire empire = null)
        {
            float max = module.PowerFlowMax;
            if (empire == null)
                return max;

            max *= 1.0f + empire.data.PowerFlowMod;
            return Math.Max(0, max);
        }

        public static float GetBonusRepairRate(ShipModule module, Empire empire = null, ShipData shipData = null)
        {
            float rate = module.BonusRepairRate;
            if (empire != null)   rate *= empire.data.Traits.RepairMod;
            if (shipData != null) rate *= shipData.Bonuses.RepairModifier;
            return rate;
        }

        public static float GetShieldPowerMax(ShipModule module, Empire empire = null, ShipData shipData = null)
        {
            float shieldMax = module.shield_power_max;
            if (empire != null)   shieldMax *= 1.0f + empire.data.ShieldPowerMod;
            if (shipData != null) shieldMax *= shipData.Bonuses.ShieldModifier;
            return shieldMax;
        }
    }
}
