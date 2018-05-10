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
        public float HealthMod     { get; private set; } = 1.0f;

        private void Update(Empire empire, ShipData shipData)
        {
            float EmpireDataMod(float mod)
            {
                return Math.Max(0.0f, (1.0f + mod));
            }

            FuelCellMod   = EmpireDataMod(empire.data.FuelCellModifier);
            PowerFlowMod  = EmpireDataMod(empire.data.PowerFlowMod);
            RepairRateMod = EmpireDataMod(empire.data.Traits.RepairMod) * shipData.Bonuses.RepairModifier;
            ShieldMod     = EmpireDataMod(empire.data.ShieldPowerMod)   * shipData.Bonuses.ShieldModifier;
            HealthMod     = EmpireDataMod(empire.data.Traits.ModHpModifier);
        }

        public static readonly EmpireShipBonuses Default = new EmpireShipBonuses();

        private static readonly Map<Empire, Map<ShipData, EmpireShipBonuses>> EmpireShips
                          = new Map<Empire, Map<ShipData, EmpireShipBonuses>>();

        /// <summary>
        /// Gets bonuses for a specific empire and a specific ship hull type
        /// </summary>
        public static EmpireShipBonuses Get(Empire empire, ShipData shipData)
        {
            if (empire   == null) throw new InvalidOperationException("Empire must not be null");
            if (shipData == null) throw new InvalidOperationException("ShipData must not be null");

            Map<ShipData, EmpireShipBonuses> hullBonuses;
            EmpireShipBonuses bonuses;

            // @note Loading is multi-threaded, so cache init must be thread safe

            lock (EmpireShips)
            {
                if (!EmpireShips.TryGetValue(empire, out hullBonuses))
                {
                    hullBonuses = new Map<ShipData, EmpireShipBonuses>();
                    EmpireShips[empire] = hullBonuses;
                }
            }

            lock (hullBonuses)
            {
                if (!hullBonuses.TryGetValue(shipData, out bonuses))
                {
                    bonuses = new EmpireShipBonuses();
                    hullBonuses[shipData] = bonuses;
                    bonuses.Update(empire, shipData);
                }
            }

            return bonuses;
        }

        /// <summary>
        /// Refreshes bonuses for the specific empire.
        /// This should be called when an Empire unlocks a new tech modifier
        /// </summary>
        public static void RefreshBonuses(Empire empire)
        {
            lock (EmpireShips)
            {
                if (EmpireShips.TryGetValue(empire, out Map<ShipData, EmpireShipBonuses> hullBonuses))
                {
                    foreach (KeyValuePair<ShipData, EmpireShipBonuses> kv in hullBonuses)
                        kv.Value.Update(empire, kv.Key);
                }
            }
        }

        public static void RefreshBonuses()
        {
            Log.Info("Refreshing all bonuses");
            lock (EmpireShips)
            {
                foreach (KeyValuePair<Empire, Map<ShipData, EmpireShipBonuses>> empireShips in EmpireShips)
                {
                    foreach (KeyValuePair<ShipData, EmpireShipBonuses> kv in empireShips.Value)
                    {
                        kv.Value.Update(empireShips.Key, kv.Key);
                    }
                }
            }
        }
    }
}
