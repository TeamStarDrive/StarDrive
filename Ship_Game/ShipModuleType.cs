namespace Ship_Game
{
    public enum ShipModuleType
    {
        Dummy, // here for backwards compatibility... we should remove this if possible. Repurposing as a default non value
        Turret,
        Command,
        MainGun,
        Armor,
        PowerPlant,
        PowerConduit,
        Engine,
        Shield,
        MissileLauncher,
        Storage,
        Colony,
        FuelCell,
        Hangar,
        Sensors,
        Bomb,
        Special,
        Drone,
        Spacebomb,
        Countermeasure,
        Transporter,
        Troop,
        Ordnance,
        Construction
    }
}