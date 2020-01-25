namespace Ship_Game.Ships
{
    public static class ShipUtils
    {
        // This will also update shield max power of modules if there are amplifiers
        public static float UpdateShieldAmplification(float totalShieldAmplify, ShipModule[] shields)
        {
            int numShields = shields.Length;
            if (numShields == 0)
                return 0; 

            float shieldAmplify = GetShieldAmplification(totalShieldAmplify, shields);
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
        
        public static float GetShieldAmplification(float totalShieldAmplifyPower, ShipModule[] shields)
        {
            return shields.Length > 0 ? totalShieldAmplifyPower / shields.Length : 0;
        }
    }
}