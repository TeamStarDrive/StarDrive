using System;
using System.Collections.Generic;

namespace Ship_Game.Ships
{
    public class EmpireShipBonuses
    {
        /// <summary>
        /// All modifiers are 1.0 by default and can be safely used as a multiplier
        /// </summary>
        public float FuelCellMod   { get; private set; } = 1.0f;
        public float PowerFlowMod  { get; private set; } = 1.0f;
        public float RepairRateMod { get; private set; } = 1.0f;
        public float ShieldMod     { get; private set; } = 1.0f;
        public float HealthMod { get; private set; } = 1.0f;

        private void Update(Empire empire, ShipData shipData)
        {
            FuelCellMod   = Math.Max(0.0f, (1.0f + empire.data.FuelCellModifier));
            PowerFlowMod  = Math.Max(0.0f, (1.0f + empire.data.PowerFlowMod));
            RepairRateMod = empire.data.Traits.RepairMod * shipData.Bonuses.RepairModifier;
            ShieldMod     = (1.0f + empire.data.ShieldPowerMod) * shipData.Bonuses.ShieldModifier;
            HealthMod     = (1.0f + empire.data.Traits.ModHpModifier);
        }

        public static readonly EmpireShipBonuses Default = new EmpireShipBonuses();

        private static Map<Empire, Map<ShipData, EmpireShipBonuses>> BonusCache
                 = new Map<Empire, Map<ShipData, EmpireShipBonuses>>();

        /// <summary>
        /// Gets bonuses for a specific empire and a specific ship hull type
        /// </summary>
        public static EmpireShipBonuses Get(Empire empire, ShipData shipData)
        {
            if (empire   == null) throw new InvalidOperationException("Empire must not be null");
            if (shipData == null) throw new InvalidOperationException("ShipData must not be null");

            if (!BonusCache.TryGetValue(empire, out Map<ShipData, EmpireShipBonuses> hullBonuses))
            {
                hullBonuses = new Map<ShipData, EmpireShipBonuses>();
                BonusCache[empire] = hullBonuses;
            }

            if (!hullBonuses.TryGetValue(shipData, out EmpireShipBonuses bonuses))
            {
                bonuses = new EmpireShipBonuses();
                hullBonuses[shipData] = bonuses;
                bonuses.Update(empire, shipData);
            }

            return bonuses;
        }

        /// <summary>
        /// Refreshes bonuses for the specific empire.
        /// This should be called when an Empire unlocks a new tech modifier
        /// </summary>
        public static void RefreshBonuses(Empire empire)
        {
            if (BonusCache.TryGetValue(empire, out Map<ShipData, EmpireShipBonuses> hullBonuses))
            {
                foreach (KeyValuePair<ShipData, EmpireShipBonuses> kv in hullBonuses)
                    kv.Value.Update(empire, kv.Key);
            }
        }

    }
}
