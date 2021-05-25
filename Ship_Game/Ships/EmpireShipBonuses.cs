using System;
using System.Collections.Generic;

namespace Ship_Game.Ships
{

    // Bonuses for each ship hull type for each empire
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

        public void Update(Empire empire, HullBonus hullBonus)
        {
            float EmpireDataMod(float mod)
            {
                return Math.Max(0.0f, (1.0f + mod));
            }

            FuelCellMod   = EmpireDataMod(empire.data.FuelCellModifier);
            PowerFlowMod  = EmpireDataMod(empire.data.PowerFlowMod);
            RepairRateMod = EmpireDataMod(empire.data.Traits.RepairMod) * hullBonus.RepairModifier;
            ShieldMod     = EmpireDataMod(empire.data.ShieldPowerMod)   * hullBonus.ShieldModifier;
            HealthMod     = EmpireDataMod(empire.data.Traits.ModHpModifier);
        }

        public static readonly EmpireShipBonuses Default = new EmpireShipBonuses();

        static readonly Map<Empire, EmpireBonusData> EmpireBonuses = new Map<Empire, EmpireBonusData>();

        class EmpireBonusData
        {
            readonly Empire Empire;
            static int NextRevisionId; // to make each revision globally unique
            public int RevisionId { get; private set; } = ++NextRevisionId;
            readonly Map<HullBonus, EmpireShipBonuses> HullBonuses = new Map<HullBonus, EmpireShipBonuses>();

            public EmpireBonusData(Empire empire)
            {
                Empire = empire;
            }
            public void Update()
            {
                foreach (KeyValuePair<HullBonus, EmpireShipBonuses> kv in HullBonuses)
                    kv.Value.Update(Empire, kv.Key);
                RevisionId = ++NextRevisionId;
            }
            public EmpireShipBonuses GetOrCreateShipBonus(ShipData shipData)
            {
                lock (this)
                {
                    HullBonus hullBonus = shipData.Bonuses;
                    if (!HullBonuses.TryGetValue(hullBonus, out EmpireShipBonuses shipBonuses))
                    {
                        shipBonuses = new EmpireShipBonuses();
                        HullBonuses[hullBonus] = shipBonuses;
                        shipBonuses.Update(Empire, hullBonus);
                    }
                    return shipBonuses;
                }
            }
        }

        static EmpireBonusData GetOrCreateBonusData(Empire empire)
        {
            // @note Loading is multi-threaded, so cache init must be thread safe
            lock (EmpireBonuses)
            {
                if (!EmpireBonuses.TryGetValue(empire, out EmpireBonusData bonusData))
                {
                    bonusData = new EmpireBonusData(empire);
                    EmpireBonuses[empire] = bonusData;
                }
                return bonusData;
            }
        }

        /// <summary>
        /// Gets bonuses for a specific empire and a specific ship hull type
        /// </summary>
        public static EmpireShipBonuses Get(Empire empire, ShipData shipData)
        {
            if (empire   == null) throw new InvalidOperationException("Empire must not be null");
            if (shipData == null) throw new InvalidOperationException("ShipData must not be null");

            return GetOrCreateBonusData(empire).GetOrCreateShipBonus(shipData);
        }

        /// <summary>
        /// Clears all cached Ship hull bonuses
        /// </summary>
        public static void Clear()
        {
            lock (EmpireBonuses)
            {
                EmpireBonuses.Clear();
            }
        }

        /// <summary>
        /// Refreshes bonuses for the specific empire.
        /// This should be called when an Empire unlocks a new tech modifier
        /// </summary>
        public static void RefreshBonuses(Empire empire)
        {
            lock (EmpireBonuses)
            {
                if (EmpireBonuses.TryGetValue(empire, out EmpireBonusData bonusData))
                    bonusData.Update();
            }
        }

        public static void RefreshBonuses()
        {
            Log.Info("Refreshing all bonuses");
            lock (EmpireBonuses)
            {
                foreach (KeyValuePair<Empire, EmpireBonusData> empireBonuses in EmpireBonuses)
                    empireBonuses.Value.Update();
            }
        }

        // this helps us keep track whether bonuses changed or not
        public static int GetBonusRevisionId(Empire empire)
        {
            if (EmpireBonuses.TryGetValue(empire, out EmpireBonusData bonusData))
                return bonusData.RevisionId;
            return 0;
        }
    }
}
