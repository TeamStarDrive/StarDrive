using System;

namespace Ship_Game.Gameplay
{
    // ReadOnly interface for WeaponTemplate
    public interface IWeaponTemplate
    {
        // This is the WeaponTemplate UID string
        string UID { get; }

        WeaponTag[] ActiveWeaponTags { get; }

        // @note These are initialized from XML during serialization
        bool Tag_Kinetic { get; }
        bool Tag_Energy { get; }
        bool Tag_Guided { get; }
        bool Tag_Missile { get; }
        bool Tag_Plasma { get; }
        bool Tag_Beam { get; }
        bool Tag_Intercept { get; }
        bool Tag_Bomb { get; }
        bool Tag_SpaceBomb { get; }
        bool Tag_BioWeapon { get; }
        bool Tag_Drone { get; }
        bool Tag_Torpedo { get; }
        bool Tag_Cannon { get; }
        bool Tag_PD { get; }

        float HitPoints { get; }
        bool IsBeam { get; }
        bool TruePD { get; }
        float EffectVsArmor { get; }
        float EffectVsShields { get; }
        float TroopDamageChance { get; }
        float TractorDamage { get; }
        float BombPopulationKillPerHit { get; }
        int BombTroopDamageMin { get; }
        int BombTroopDamageMax { get; }
        int BombHardDamageMin { get; }
        int BombHardDamageMax { get; }
        string HardCodedAction { get; }
        float RepulsionDamage { get; }
        float EMPDamage { get; }
        float ShieldPenChance { get; }
        float PowerDamage { get; }
        float SiphonDamage { get; }
        int BeamThickness { get; }
        float BeamDuration { get; }
        int BeamPowerCostPerSecond { get; }
        string BeamTexture { get; }
        int Animated { get; }
        int Frames { get; }
        string AnimationPath { get; }
        ExplosionType ExplosionType { get; }
        string DieCue { get; }
        string ToggleSoundName { get; }
        string Light { get; }
        bool IsTurret { get; }
        bool IsMainGun { get; }
        float OrdinanceRequiredToFire { get; }

        // This is the weapons base unaltered range. In addition to this, many bonuses could be applied.
        // Use GetActualRange() to the true range with bonuses
        float BaseRange { get; }

        float DamageAmount { get; }
        float ProjectileSpeed { get; }

        // The number of projectiles spawned from a single "shot".
        // Most commonly it is 1. AFAIK, only some FLAK cannons spawn buck-shots.
        int ProjectileCount { get; }

        // for cannons that have ProjectileCount > 1, this spreads projectiles out in an arc
        // this spreading out has a very strict pattern
        int FireDispersionArc { get; }
        
        // this is the fire imprecision angle, direction spread is randomized [-FireCone,+FireCone]
        // the cannon simply has random variability in its shots
        int FireImprecisionAngle { get; } 
        
        string ProjectileTexturePath { get; }
        string ModelPath { get; }
        string WeaponType { get; }

        // Determines Hit, ShieldHit, Death and Trail effects from ParticleEffects.yaml
        string WeaponHitEffect { get; }
        string WeaponShieldHitEffect { get; }
        string WeaponDeathEffect { get; }
        string WeaponTrailEffect { get; }

        // The trail offset behind missile center
        float TrailOffset { get; }

        float FireDelay { get; }
        float PowerRequiredToFire { get; }
        float ExplosionRadius { get; } // If > 0 it means the projectile wil explode
        string FireCueName { get; }
        string MuzzleFlash { get; }
        bool IsRepairDrone { get; }
        bool FakeExplode { get; }
        float ProjectileRadius { get; }
        string Name { get; }
        byte LoopAnimation { get; }
        float Scale { get; }
        float RotationRadsPerSecond { get; }
        string InFlightCue { get; }
        float ParticleDelay { get; }
        float ECMResist { get; }
        bool ExcludesFighters { get; }
        bool ExcludesCorvettes { get; }
        bool ExcludesCapitals { get; }
        bool ExcludesStations { get; }
        bool IsRepairBeam { get; }
        bool TerminalPhaseAttack { get; }
        float TerminalPhaseDistance { get; }
        float TerminalPhaseSpeedMod { get; }
        float DelayedIgnition { get; }
        float MirvWarheads { get; }
        float MirvSeparationDistance { get; }
        string MirvWeapon { get; }
        int ArmorPen { get; }
        float OffPowerMod { get; }
        float FertilityDamage { get; }
        bool RangeVariance { get; }
        float ExplosionRadiusVisual { get; }
        bool UseVisibleMesh { get; }
        bool PlaySoundOncePerSalvo { get; } // @todo DEPRECATED
        int SalvoSoundInterval { get; } // play sound effect every N salvos

        // STAT Generated automatically after all weapons are loaded
        float DamagePerSecond { get; }

        // Number of salvos that will be sequentially spawned.
        // For example, Vulcan Cannon fires a salvo of 20
        int SalvoCount { get; }

        // This is the total salvo duration
        // TimeBetweenShots = SalvoTimer / SalvoCount;
        float SalvoDuration { get; }
    }
}
