using System;

namespace Ship_Game.Ships
{
    public static partial class ShipUtils
    {
        // This will also update shield max power of modules if there are amplifiers
        public static float UpdateShieldAmplification(ShipModule[] amplifiers, ShipModule[] shields)
        {
            var mainShields = shields.Filter(s => s.ModuleType == ShipModuleType.Shield);
            int numShields  = shields.Length;
            if (numShields == 0)
                return 0;

            float shieldAmplify = GetShieldAmplification(amplifiers, shields);
            float shieldMax     = 0;
            for (int i = 0; i < shields.Length; i++)
            {
                ShipModule shield = shields[i];
                if (shield.Active && shield.Powered)
                {
                    shield.UpdateShieldPowerMax(shieldAmplify);
                    shieldMax += shield.ActualShieldPowerMax;
                }
            }

            return shieldMax;
        }

        public static float GetShieldAmplification(ShipModule[] amplifiers, ShipModule[] shields)
        {
            var mainShields   = shields.Filter(s => s.ModuleType == ShipModuleType.Shield);
            int numShields    = mainShields.Length;
            int numAmplifiers = amplifiers.Length;

            if (numAmplifiers == 0 || numShields == 0)
                return 0;

            float totalAmplifyPower = 0;

            for (int i = 0; i < amplifiers.Length; i++)
            {
                ShipModule amplifier = amplifiers[i];
                if (amplifier.Active && amplifier.Powered)
                    totalAmplifyPower += amplifier.AmplifyShields;
            }

            return totalAmplifyPower / numShields;
        }
    }
}