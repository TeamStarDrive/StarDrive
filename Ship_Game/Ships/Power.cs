using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Ship_Game.Ships
{
    public struct Power
    {
        public float NetSubLightPowerDraw;
        public float NetWarpPowerDraw;
        public float WarpPowerDuration;
        public float SubLightPowerDuration;
        public float PowerFlowMax;
        public float PowerStoreMax;

        public static Power Calculate(IReadOnlyList<ShipModule> modules, Empire empire, bool designModule = false)
        {
            float nonShieldPowerDraw = 0f;
            float shieldPowerDraw    = 0f;
            float warpPowerDrawBonus = 0f;
            float powerFlowMax       = 0f;
            float powerStoreMax      = 0f;

            if (modules == null)
                return new Power();

            for (int i = 0; i < modules.Count; i++)
            {
                ShipModule module = modules[i];
                if (designModule || (module.Active && (module.Powered || module.PowerDraw <= 0f)))
                {
                    powerFlowMax += module.ActualPowerFlowMax;
                    powerStoreMax += module.ActualPowerStoreMax;

                    if (module.Is(ShipModuleType.Shield))
                    {
                        shieldPowerDraw += module.PowerDraw;
                        warpPowerDrawBonus += module.PowerDrawAtWarp; // FB: include bonuses to warp if shields are on at warp
                    }
                    else
                    {
                        nonShieldPowerDraw += module.PowerDraw;
                        warpPowerDrawBonus += module.PowerDrawAtWarp;
                    }
                }
            }

            float subLightPowerDraw      = shieldPowerDraw + nonShieldPowerDraw;
            float warpPowerDrainModifier = empire?.data.FTLPowerDrainModifier ?? 1f;
            float warpPowerDraw          = (shieldPowerDraw + nonShieldPowerDraw) * warpPowerDrainModifier + (warpPowerDrawBonus * warpPowerDrainModifier / 2);
            float subLightPowerDuration  = PowerDuration(powerFlowMax, subLightPowerDraw, powerStoreMax);
            float warpPowerDuration      = PowerDuration(powerFlowMax, warpPowerDraw, powerStoreMax);
            // calculate after maxes

            return new Power
            {
                NetSubLightPowerDraw  = subLightPowerDraw,
                NetWarpPowerDraw      = warpPowerDraw,
                SubLightPowerDuration = subLightPowerDuration,
                WarpPowerDuration     = warpPowerDuration,
                PowerFlowMax          = powerFlowMax,
                PowerStoreMax         = powerStoreMax
            };
        }

        /// <summary>
        /// Returns the number of updates of power available.
        /// </summary>
        public static float PowerDuration(float powerFlowMax, float powerDraw, float powerStore)
        {
            float duration = float.MaxValue;
            float netPower = powerFlowMax - powerDraw;
            netPower *= -1;
            if (netPower > 0) 
                duration = powerStore / netPower;
            return duration;
        }

        /// <summary>
        /// Returns the numbers of updates of power depending on the move state.
        /// </summary>
        [Pure] public float PowerDuration(Ship.MoveState moveState, float currentPower)
        {
            if (PowerStoreMax == 0f)
                return 0f;

            float powerSupplyRatio = currentPower / PowerStoreMax;
            switch (moveState)
            {
                case Ship.MoveState.Warp: return WarpPowerDuration * powerSupplyRatio;
                default:                  return SubLightPowerDuration * powerSupplyRatio;
            }
        }
    }
}