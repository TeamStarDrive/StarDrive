using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Ship_Game.Gameplay
{
    // Mutable view of WeaponTemplate
    // This should not be used outside of WeaponTemplates manager
    public class WeaponTemplate : IWeaponTemplate
    {
        // This is the WeaponTemplate UID string
        public string UID { get; set; }

        // Active Tag Bits for this WeaponTemplate
        WeaponTag TagBits;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Tag(WeaponTag tag, bool value) => TagBits = value ? TagBits|tag : TagBits & ~tag;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Tag(WeaponTag tag) => (TagBits & tag) != 0;

        public static readonly WeaponTag[] TagValues = (WeaponTag[])typeof(WeaponTag).GetEnumValues();

        [XmlIgnore][JsonIgnore] public WeaponTag[] ActiveWeaponTags { get; set; }

        // @note These are initialized from XML during serialization
        public bool Tag_Kinetic   { get => Tag(WeaponTag.Kinetic);   set => Tag(WeaponTag.Kinetic, value);   }
        public bool Tag_Energy    { get => Tag(WeaponTag.Energy);    set => Tag(WeaponTag.Energy, value);    }
        public bool Tag_Guided    { get => Tag(WeaponTag.Guided);    set => Tag(WeaponTag.Guided, value);    }
        public bool Tag_Missile   { get => Tag(WeaponTag.Missile);   set => Tag(WeaponTag.Missile, value);   }
        public bool Tag_Plasma    { get => Tag(WeaponTag.Plasma);    set => Tag(WeaponTag.Plasma, value);    }
        public bool Tag_Beam      { get => Tag(WeaponTag.Beam);      set => Tag(WeaponTag.Beam, value);      }
        public bool Tag_Intercept { get => Tag(WeaponTag.Intercept); set => Tag(WeaponTag.Intercept, value); }
        public bool Tag_Bomb      { get => Tag(WeaponTag.Bomb);      set => Tag(WeaponTag.Bomb, value);      }
        public bool Tag_SpaceBomb { get => Tag(WeaponTag.SpaceBomb); set => Tag(WeaponTag.SpaceBomb, value); }
        public bool Tag_BioWeapon { get => Tag(WeaponTag.BioWeapon); set => Tag(WeaponTag.BioWeapon, value); }
        public bool Tag_Drone     { get => Tag(WeaponTag.Drone);     set => Tag(WeaponTag.Drone, value);     }
        public bool Tag_Torpedo   { get => Tag(WeaponTag.Torpedo);   set => Tag(WeaponTag.Torpedo, value);   }
        public bool Tag_Cannon    { get => Tag(WeaponTag.Cannon);    set => Tag(WeaponTag.Cannon, value);    }
        public bool Tag_PD        { get => Tag(WeaponTag.PD);        set => Tag(WeaponTag.PD, value);        }

        public float HitPoints { get; set; }
        public bool IsBeam { get; set; }
        public bool TruePD { get; set; }
        public float EffectVsArmor { get; set; } = 1f;
        public float EffectVsShields { get; set; } = 1f;
        public float TroopDamageChance { get; set; }
        public float TractorDamage { get; set; }
        public float BombPopulationKillPerHit { get; set; }
        public int BombTroopDamageMin { get; set; }
        public int BombTroopDamageMax { get; set; }
        public int BombHardDamageMin { get; set; }
        public int BombHardDamageMax { get; set; }
        public string HardCodedAction { get; set; }
        public float RepulsionDamage { get; set; }
        public float EMPDamage { get; set; }
        public float ShieldPenChance { get; set; }
        public float PowerDamage { get; set; }
        public float SiphonDamage { get; set; }
        public int BeamThickness { get; set; }
        public float BeamDuration { get; set; }
        public int BeamPowerCostPerSecond { get; set; }
        public string BeamTexture { get; set; }
        public int Animated { get; set; }
        public int Frames { get; set; }
        public string AnimationPath { get; set; }
        public ExplosionType ExplosionType { get; set; } = ExplosionType.Projectile;
        public string DieCue { get; set; }
        public string ToggleSoundName { get; set; } = "";
        public string Light { get; set; }
        public bool IsTurret { get; set; }
        public bool IsMainGun { get; set; }
        public float OrdinanceRequiredToFire { get; set; }

        // This is the weapons base unaltered range. In addition to this, many bonuses could be applied.
        // Use GetActualRange() to the true range with bonuses
        [XmlElement(ElementName = "Range")]
        public float BaseRange { get; set; }

        public float DamageAmount { get; set; }
        public float ProjectileSpeed { get; set; }

        // The number of projectiles spawned from a single "shot".
        // Most commonly it is 1. AFAIK, only some FLAK cannons spawn buck-shots.
        public int ProjectileCount { get; set; } = 1;

        // for cannons that have ProjectileCount > 1, this spreads projectiles out in an arc
        // this spreading out has a very strict pattern
        [XmlElement(ElementName = "FireArc")]
        public int FireDispersionArc { get; set; }

        // this is the fire imprecision angle, direction spread is randomized [-FireCone,+FireCone]
        // the cannon simply has random variability in its shots
        [XmlElement(ElementName = "FireCone")]
        public int FireImprecisionAngle { get; set; }

        public string ProjectileTexturePath { get; set; }
        public string ModelPath { get; set; }
        public string WeaponType { get; set; }

        // Determines Hit, ShieldHit, Death and Trail effects from ParticleEffects.yaml
        public string WeaponHitEffect { get; set; }
        public string WeaponShieldHitEffect { get; set; }
        public string WeaponDeathEffect { get; set; }
        public string WeaponTrailEffect { get; set; }

        // The trail offset behind missile center
        public float TrailOffset { get; set; }

        public float FireDelay { get; set; }
        public float PowerRequiredToFire { get; set; }
        public float ExplosionRadius { get; set; } // If > 0 it means the projectile wil explode
        public string FireCueName { get; set; }
        public string MuzzleFlash { get; set; }
        public bool IsRepairDrone { get; set; }
        public bool FakeExplode { get; set; }
        public float ProjectileRadius { get; set; } = 4f;
        public string Name { get; set; }
        public byte LoopAnimation { get; set; }
        public float Scale { get; set; } = 1f;
        public float RotationRadsPerSecond { get; set; } = 2f;
        public string InFlightCue { get; set; } = "";
        public float ParticleDelay { get; set; }
        public float ECMResist { get; set; }
        public bool ExcludesFighters { get; set; }
        public bool ExcludesCorvettes { get; set; }
        public bool ExcludesCapitals { get; set; }
        public bool ExcludesStations { get; set; }
        public bool IsRepairBeam { get; set; }
        public bool TerminalPhaseAttack { get; set; }
        public float TerminalPhaseDistance { get; set; }
        public float TerminalPhaseSpeedMod { get; set; } = 2f;
        public float DelayedIgnition { get; set; }
        public float MirvWarheads { get; set; }
        public float MirvSeparationDistance { get; set; }
        public string MirvWeapon { get; set; }
        public int ArmorPen { get; set; }
        public float OffPowerMod { get; set; } = 1f;
        public float FertilityDamage { get; set; }
        public bool RangeVariance { get; set; }
        public float ExplosionRadiusVisual { get; set; } = 4.5f;
        public bool UseVisibleMesh { get; set; }
        public bool PlaySoundOncePerSalvo { get; set; } // @todo DEPRECATED
        public int SalvoSoundInterval { get; set; } = 1; // play sound effect every N salvos

        // STAT Generated automatically after all weapons are loaded
        [XmlIgnore][JsonIgnore] public float DamagePerSecond { get; private set; }

        // Number of salvos that will be sequentially spawned.
        // For example, Vulcan Cannon fires a salvo of 20
        public int SalvoCount { get; set; } = 1;

        // This is the total salvo duration
        // TimeBetweenShots = SalvoTimer / SalvoCount;
        [XmlElement(ElementName = "SalvoTimer")]
        public float SalvoDuration { get; set; }
    }
}
