namespace Ship_Game.Ships
{
    public interface IDamageModifier
    {
        float GetShieldDamageMod(ShipModule module);
        float GetArmorDamageMod(ShipModule module);
    }
}
