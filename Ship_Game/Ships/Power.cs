using System;

namespace Ship_Game.Ships
{
    public struct Power
    {
        public float NetSubLightPowerDraw;
        public float NetWarpPowerDraw;

        public static Power Calculate(ShipModule[] modules, Empire empire, ShieldsWarpBehavior behavior, bool designModule = false)
        {
            // if warp behaviors are disabled, then force "Full" behavior
            if (!GlobalStats.WarpBehaviorsEnabled)
                behavior = ShieldsWarpBehavior.FullPower;

            float nonShieldPowerDraw = 0f;
            float shieldPowerDraw = 0f;
            float warpPowerDrawBonus = 0f;
            if (modules == null)
                return new Power();

            foreach (ShipModule module in modules)
            {
                if (!module.Active || (!module.Powered && module.PowerDraw > 0f) && !designModule)
                    continue;

                if (module.Is(ShipModuleType.Shield))
                {
                    shieldPowerDraw += module.PowerDraw;
                    if (behavior == ShieldsWarpBehavior.FullPower)
                        warpPowerDrawBonus += module.PowerDrawAtWarp; // FB: include bonuses to warp if shields are on at warp
                }
                else
                {
                    nonShieldPowerDraw += module.PowerDraw;
                    warpPowerDrawBonus += module.PowerDrawAtWarp;
                }
            }
            float subLightPowerDraw = shieldPowerDraw + nonShieldPowerDraw;
            float warpPowerDrainModifier = empire.data.FTLPowerDrainModifier;
            float warpPowerDraw = 0f;
            switch (behavior)
            {
                case ShieldsWarpBehavior.FullPower:
                    {
                        warpPowerDraw = (shieldPowerDraw + nonShieldPowerDraw) * warpPowerDrainModifier + (warpPowerDrawBonus * warpPowerDrainModifier / 2);
                        break;
                    }
                case ShieldsWarpBehavior.Hibernate:
                    {
                        warpPowerDraw = nonShieldPowerDraw * warpPowerDrainModifier + shieldPowerDraw;
                        break;
                    }
                case ShieldsWarpBehavior.ShutDown:
                    {
                        warpPowerDraw = nonShieldPowerDraw * warpPowerDrainModifier;
                        break;
                    }
            }

            return new Power
            {
                NetSubLightPowerDraw = subLightPowerDraw,
                NetWarpPowerDraw = warpPowerDraw
            };
        }
        public float PowerDuration(Ship ship, Ship.MoveState moveState)
        {
            float powerDraw = 0;
            switch (moveState)
            {
                case Ship.MoveState.Sublight:
                    powerDraw = NetSubLightPowerDraw;
                    break;
                case Ship.MoveState.Warp:
                    powerDraw = NetWarpPowerDraw;
                    break;
            }
            powerDraw = powerDraw.Clamped(1, float.MaxValue);
            float powerRatio = (ship.PowerFlowMax / powerDraw).Clamped(.01f, 1f);
            if (powerRatio < 1)
                return ship.PowerStoreMax * powerRatio;
            return float.MaxValue;

        }        
    }
    public enum ShieldsWarpBehavior
    {
        FullPower,
        Hibernate,
        ShutDown
    }
}