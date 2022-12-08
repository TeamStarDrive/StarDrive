using System;
using System.Collections.Generic;
using SDUtils;
using Ship_Game.Universe;

namespace Ship_Game.Ships
{
    // Bonuses for each ship hull type for each empire
    public class EmpireHullBonuses
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
            static float EmpireDataMod(float mod)
            {
                return Math.Max(0.0f, (1.0f + mod));
            }

            FuelCellMod   = EmpireDataMod(empire.data.FuelCellModifier);
            PowerFlowMod  = EmpireDataMod(empire.data.PowerFlowMod);
            RepairRateMod = EmpireDataMod(empire.data.Traits.RepairMod) * hullBonus.RepairModifier;
            ShieldMod     = EmpireDataMod(empire.data.ShieldPowerMod)   * hullBonus.ShieldModifier;
            HealthMod     = EmpireDataMod(empire.data.Traits.ModHpModifier);
        }

        public static readonly EmpireHullBonuses Default = new();

        static readonly Array<EmpireBonusData> EmpireBonuses = new(128, 128);

        class EmpireBonusData
        {
            static int NextRevisionId; // to make each revision globally unique
            public int RevisionId { get; private set; } = ++NextRevisionId;
            readonly Map<HullBonus, EmpireHullBonuses> HullBonuses = new();

            public void Update(Empire empire)
            {
                lock (HullBonuses)
                {
                    foreach (KeyValuePair<HullBonus, EmpireHullBonuses> kv in HullBonuses)
                        kv.Value.Update(empire, kv.Key);
                }
                RevisionId = ++NextRevisionId;
            }
            public EmpireHullBonuses GetOrCreateShipBonus(Empire empire, ShipHull hull)
            {
                lock (HullBonuses)
                {
                    HullBonus hullBonus = hull.Bonuses;
                    if (!HullBonuses.TryGetValue(hullBonus, out EmpireHullBonuses shipBonuses))
                    {
                        shipBonuses = new EmpireHullBonuses();
                        HullBonuses[hullBonus] = shipBonuses;
                        shipBonuses.Update(empire, hullBonus);
                    }
                    return shipBonuses;
                }
            }
        }

        static EmpireBonusData GetOrCreateBonusData(Empire empire)
        {
            int empireIdx = empire.Id + 1;
            EmpireBonusData bonusData = EmpireBonuses[empireIdx];
            if (bonusData == null)
            {
                // @note Loading is multi-threaded, so cache init must be thread safe
                lock (EmpireBonuses)
                {
                    bonusData = EmpireBonuses[empireIdx];
                    if (bonusData == null)
                    {
                        EmpireBonuses[empireIdx] = bonusData = new EmpireBonusData();
                    }
                }
            }
            return bonusData;
        }

        /// <summary>
        /// Gets bonuses for a specific empire and a specific ship hull type
        /// </summary>
        public static EmpireHullBonuses Get(Empire empire, ShipHull hull)
        {
            if (empire == null) throw new InvalidOperationException("Empire must not be null");
            if (hull == null) throw new InvalidOperationException("ShipHull must not be null");

            return GetOrCreateBonusData(empire).GetOrCreateShipBonus(empire, hull);
        }

        /// <summary>
        /// Clears all cached Ship hull bonuses
        /// </summary>
        public static void Clear()
        {
            EmpireBonuses.Clear();
            EmpireBonuses.Resize(128);
        }

        /// <summary>
        /// Refreshes bonuses for the specific empire.
        /// This should be called when an Empire unlocks a new tech modifier
        /// </summary>
        public static void RefreshBonuses(Empire empire)
        {
            EmpireBonusData bonusData = EmpireBonuses[empire.Id + 1];
            if (bonusData != null)
                bonusData.Update(empire);
        }

        public static void RefreshBonuses(UniverseState us)
        {
            Log.Info("Refreshing all bonuses");
            for (int id = 0; id < EmpireBonuses.Count; ++id)
            {
                EmpireBonusData bonusData = EmpireBonuses[id];
                if (bonusData != null)
                {
                    Empire empire = null;
                    if (id == 0)
                        empire = Empire.Void;
                    else if (id > 1)
                        empire = us.GetEmpireById(id - 1);
                    bonusData.Update(empire);
                }
            }
        }

        // this helps us keep track whether bonuses changed or not
        public static int GetBonusRevisionId(Empire empire)
        {
            EmpireBonusData bonusData = EmpireBonuses[empire.Id + 1];
            if (bonusData != null)
                return bonusData.RevisionId;
            return 0;
        }
    }
}
