namespace Ship_Game.Ships
{
    public interface IDamageModifier
    {
        float GetShieldDamageMod(ShipModule module);
        float GetArmorDamageMod(ShipModule module);

        // the weapon's own bonus or penalty, without the module's resistances: deflection is measured against this
        float GetShieldEffect(ShipModule module);
        float GetArmorEffect(ShipModule module);
    }
}
