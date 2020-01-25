namespace Ship_Game.Ships
{
    public static class ShipUtils
    {
        // This will also update shield max power of modules if there are amplifiers
        public static float UpdateShieldAmplification(ShipModule[] amplifiers, ShipModule[] shields)
        {
            int numShields = shields.Length;
            if (numShields == 0)
                return 0;

            float shieldAmplify = GetShieldAmplification(amplifiers, shields);
            float shieldMax     = 0;
            for (int i = 0; i < shields.Length; i++)
            {
                ShipModule shield = shields[i];
                if (shield.Active)
                {
                    shield.UpdateShieldPowerMax(shieldAmplify);
                    shieldMax += shield.ActualShieldPowerMax;
                }
            }

            return shieldMax;
        }

        public static float GetShieldAmplification(ShipModule[] amplifiers, ShipModule[] shields)
        {
            int numShields          = shields.Length;
            int numAmplifiers       = amplifiers.Length;

            if (numAmplifiers == 0 || numShields == 0)
                return 0;

            float totalAmplifyPower = 0;

            for (int i = 0; i < amplifiers.Length; i++)
            {
                ShipModule amplifier = amplifiers[i];
                if (amplifier.Active)
                    totalAmplifyPower += amplifier.AmplifyShields;
            }

            return totalAmplifyPower / numShields;
        }

        public static ShipModule[] GetShields(ShipModule[] moduleList)
        {
            Array<ShipModule> shields = new Array<ShipModule>();
            for (int i = 0; i<moduleList.Length; ++i)
            {
                ShipModule shield = moduleList[i];
                if (shield.shield_power_max > 0f)
                    shields.Add(shield);
            }

            return shields.ToArray();
        }

        public static ShipModule[] GetAmplifiers(ShipModule[] moduleList)
        {
            Array<ShipModule> amplifiers = new Array<ShipModule>();
            for (int i = 0; i < moduleList.Length; ++i)
            {
                ShipModule amplifier = moduleList[i];
                if (amplifier.AmplifyShields > 0f)
                    amplifiers.Add(amplifier);
            }

            return amplifiers.ToArray();
        }
    }
}