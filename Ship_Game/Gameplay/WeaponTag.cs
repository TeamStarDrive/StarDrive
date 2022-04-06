using System;

namespace Ship_Game.Gameplay
{
    [Flags]
    public enum WeaponTag
    {
        Kinetic   = (1 << 0),
        Energy    = (1 << 1),
        Guided    = (1 << 2),
        Missile   = (1 << 3),
        Plasma    = (1 << 4),
        Beam      = (1 << 5),
        Intercept = (1 << 6),
        Bomb      = (1 << 7),
        SpaceBomb = (1 << 8),
        BioWeapon = (1 << 9),
        Drone     = (1 << 10),
        Torpedo   = (1 << 11),
        Cannon    = (1 << 12),
        PD        = (1 << 13),
    }
}
