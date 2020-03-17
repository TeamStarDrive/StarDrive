using System;

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

        public static Power Calculate(ShipModule[] modules, Empire empire, bool designModule = false)
        {
            float nonShieldPowerDraw = 0f;
            float shieldPowerDraw    = 0f;
            float warpPowerDrawBonus = 0f;
            float powerFlowMax       = 0f;
            float powerStoreMax      = 0f;

            if (modules == null)
                return new Power();

            foreach (ShipModule module in modules)
            {

                if (!module.Active || (!module.Powered && module.PowerDraw > 0f) && !designModule)
                    continue;
                
                powerFlowMax  += module.ActualPowerFlowMax;
                powerStoreMax += module.ActualPowerStoreMax;

                if (module.Is(ShipModuleType.Shield))
                {
                    shieldPowerDraw    += module.PowerDraw;
                    warpPowerDrawBonus += module.PowerDrawAtWarp; // FB: include bonuses to warp if shields are on at warp
                }
                else
                {
                    nonShieldPowerDraw += module.PowerDraw;
                    warpPowerDrawBonus += module.PowerDrawAtWarp;
                }
            }

            float subLightPowerDraw      = shieldPowerDraw + nonShieldPowerDraw;
            float warpPowerDrainModifier = empire.data.FTLPowerDrainModifier;
            float warpPowerDraw          = (shieldPowerDraw + nonShieldPowerDraw) * warpPowerDrainModifier + (warpPowerDrawBonus * warpPowerDrainModifier / 2);
            float subLightPowerDuration  = PowerDuration(powerFlowMax, subLightPowerDraw);
            float warpPowerDuration      = PowerDuration(powerFlowMax, warpPowerDraw);
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
        public static float PowerDuration(float powerFlowMax, float powerDraw)
        {
            powerDraw = powerDraw.ClampMin(1);
            float powerRatio = (powerFlowMax / powerDraw).Clamped(.01f, 1f);
            if (powerRatio < 1)
                return powerFlowMax * powerRatio;

            return float.MaxValue;
        }
        public float PowerDuration(Ship.MoveState moveState)
        {
            switch (moveState)
            {
                case Ship.MoveState.Warp: return WarpPowerDuration;
                default:                  return SubLightPowerDuration;
            }
        }
    }
}