using System.Collections.Generic;
using System.Reflection;

namespace Ship_Game.Ships
{
    public struct Power
    {
        public float NetSubLightPowerDraw;
        public float NetWarpPowerDraw;

        public static Power Calculate(ShipModule[] modules, Empire empire, ShieldsWarpBehavior behavior = ShieldsWarpBehavior.OnFullChargeAtWarpExit)
        {
            float nonShieldPowerDraw = 0f;
            float shieldPowerDraw = 0f;
            float warpPowerDrawBonus = 0f;
            if (modules == null)
                return new Power();

            foreach (ShipModule module in modules)
            {
                if (!module.Active || (!module.Powered && module.PowerDraw > 0f))
                    continue;

                if (module.Is(ShipModuleType.Shield))
                {
                    shieldPowerDraw += module.PowerDraw;
                    if (behavior == ShieldsWarpBehavior.OnFullChargeAtWarpExit)
                        warpPowerDrawBonus += module.PowerDrawAtWarp; // FB: include bonuses to warp if shields are on at warp
                }
                else
                {
                    nonShieldPowerDraw += module.PowerDraw;
                    warpPowerDrawBonus += module.PowerDrawAtWarp;
                }
            }
            float subLightPowerDraw = shieldPowerDraw + nonShieldPowerDraw;
            float warpPowerDrainModifier = empire?.data.FTLPowerDrainModifier ?? 1;
            float warpPowerDraw = 0f;
            switch (behavior)
            {
                case ShieldsWarpBehavior.OnFullChargeAtWarpExit:
                    {
                        warpPowerDraw = (shieldPowerDraw + nonShieldPowerDraw) * warpPowerDrainModifier + (warpPowerDrawBonus * warpPowerDrainModifier / 2);
                        break;
                    }
                case ShieldsWarpBehavior.LowDischargeDownTo50Percent:
                    {
                        warpPowerDraw = nonShieldPowerDraw * warpPowerDrainModifier + shieldPowerDraw;
                        break;
                    }
                case ShieldsWarpBehavior.MediumDischargeDownTo25Percent:
                {
                    warpPowerDraw = nonShieldPowerDraw * warpPowerDrainModifier + shieldPowerDraw / 2;
                        break;
                }
                case ShieldsWarpBehavior.HighDischargeDownTo0Percent:
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
    }
    public enum ShieldsWarpBehavior
    {
        OnFullChargeAtWarpExit = 1,
        LowDischargeDownTo50Percent = 2,
        MediumDischargeDownTo25Percent = 4,
        HighDischargeDownTo0Percent = 5
    }
}