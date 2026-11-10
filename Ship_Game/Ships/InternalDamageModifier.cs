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

        // damage from no weapon, such as a ship's death blast or a star's radiation, meets deflection with its raw damage
        public float GetShieldEffect(ShipModule module) => 1f;
        public float GetArmorEffect(ShipModule module) => 1f;
    }
}
