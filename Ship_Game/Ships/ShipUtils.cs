namespace Ship_Game.Ships
{
    public static class ShipUtils
    {
        // This will also update shield max power of modules if there are amplifiers
        public static float UpdateShieldAmplification(float shieldMax, ShipData data, Empire empire, 
            float totalShieldAmplify, ShipModule[] shields)
        {
            ShipModule[] activeShields = shields.Filter(s => s.Active);
            if (activeShields.Length == 0)
                return 0; // no active shields

            var bonuses         = EmpireShipBonuses.Get(empire, data);
            float shieldAmplify = totalShieldAmplify / shields.Length;

            for (int i = 0; i < activeShields.Length; i++)
            {
                ShipModule shield = activeShields[i];
                shield.UpdateAmplification(shieldAmplify);
            }

            return (shieldMax + totalShieldAmplify) * bonuses.ShieldMod;
        }
    }
}