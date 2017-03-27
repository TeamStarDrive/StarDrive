using System;

namespace Ship_Game
{
    public enum ShipModuleType
    {
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
        Dummy, // here for backwards compatibility... we should remove this if possible
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
        Construction,
    }
}