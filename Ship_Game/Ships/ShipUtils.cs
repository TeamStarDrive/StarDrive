namespace Ship_Game.Ships
{
    public static class ShipUtils
    {
        // This will also update shield max power of modules if there are amplifiers
        public static float UpdateShieldAmplification(float totalShieldAmplify, ShipModule[] shields, int numActiveShields)
        {
            if (numActiveShields == 0)
                return 0; 

            float shieldAmplify = GetShieldAmplification(totalShieldAmplify, numActiveShields);
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
        
        public static float GetShieldAmplification(float totalShieldAmplifyPower, int numActiveShields)
        {
            return numActiveShields > 0 ? totalShieldAmplifyPower / numActiveShields : 0;
        }
    }
}