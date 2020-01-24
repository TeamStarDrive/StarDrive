namespace Ship_Game.Ships
{
    public static class ShipUtils
    {
        // This will also update shield max power of modules if there are amplifiers
        public static float CalcShieldAmplification(float shieldMax, ShipData data, Empire empire, 
            float totalShieldAmplify, ShipModule[] activeShields)
        {
            if (activeShields.Length == 0)
                return 0; // no active shields

            var bonuses         = EmpireShipBonuses.Get(empire, data);
            float shieldAmplify = totalShieldAmplify / activeShields.Length;

            if (shieldAmplify > 0)
            {
                for (int i = 0; i < activeShields.Length; i++)
                {
                    ShipModule shield = activeShields[i];
                    shield.UpdateAmplification(shieldAmplify);
                }
            }

            return (shieldMax + totalShieldAmplify) * bonuses.ShieldMod;
        }
    }
}