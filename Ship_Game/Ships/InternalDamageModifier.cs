namespace Ship_Game.Ships
{
    public class InternalDamageModifier : IDamageModifier
    {
        public static InternalDamageModifier Instance = new InternalDamageModifier();

        public float GetShieldDamageMod(ShipModule module)
        {
            return 1f; // no shield mod for internal component
        }

        public float GetArmorDamageMod(ShipModule module)
        {
            return (1f - module.ExplosiveResist);
        }
    }
}
