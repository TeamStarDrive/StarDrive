using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.Ships;

namespace Ship_Game.Gameplay
{
    // Mutable view of WeaponTemplate
    // This should not be used outside of WeaponTemplates manager
    [XmlType(TypeName = "Weapon")] // for Weapons/*.xml compatibility
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


        [XmlIgnore][JsonIgnore]
        public float NetFireDelay => IsBeam ? FireDelay+BeamDuration : FireDelay+SalvoDuration;

        [XmlIgnore][JsonIgnore]
        public float AverageOrdnanceUsagePerSecond => OrdinanceRequiredToFire * ProjectileCount * SalvoCount / NetFireDelay;

        [XmlIgnore][JsonIgnore]
        public float BurstOrdnanceUsagePerSecond => OrdinanceRequiredToFire * ProjectileCount * SalvoProjectilesPerSecond;

        [XmlIgnore][JsonIgnore] // 3 salvos with salvo duration of 2 seconds will give  1.5 salvos per second 
        public float SalvoProjectilesPerSecond => SalvoDuration.Greater(0) ? SalvoCount / SalvoDuration : 1;

        [XmlIgnore][JsonIgnore]
        public bool Explodes => ExplosionRadius > 0;

        [XmlIgnore][JsonIgnore] // only usage during fire, not power maintenance
        public float PowerFireUsagePerSecond => (BeamPowerCostPerSecond * BeamDuration + PowerRequiredToFire * ProjectileCount * SalvoCount) / NetFireDelay;


        public void InitializeTemplate()
        {
            ActiveWeaponTags = TagValues.Where(tag => (TagBits & tag) != 0).ToArray();

            BeamDuration = BeamDuration > 0 ? BeamDuration : 2f;
            FireDelay = Math.Max(0.016f, FireDelay);
            SalvoDuration = Math.Max(0, SalvoDuration);

            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo != null)
                ExplosionRadiusVisual *= GlobalStats.ActiveModInfo.GlobalExplosionVisualIncreaser;

            if (PlaySoundOncePerSalvo) // @note Backwards compatibility
                SalvoSoundInterval = 9999;

            if (Tag_Missile)
            {
                if (WeaponType.IsEmpty()) WeaponType = "Missile";
                else if (WeaponType != "Missile")
                {
                    Log.Warning($"Weapon '{UID}' has 'tag_missile' but Weapontype is '{WeaponType}' instead of missile. This Causes invisible projectiles.");
                }
            }
        }

        public float GetDamagePerSecond() // FB: todo - do this also when new tech is unlocked (bonuses)
        {
            IWeaponTemplate wOrMirv = this;
            if (MirvWarheads > 0 && MirvWeapon.NotEmpty())
            {
                wOrMirv = ResourceManager.GetWeaponTemplate(MirvWeapon);
            }

            // TODO: This is all duplicated in `ModuleSelection.cs` and needs to be rewritten!
            float dps;
            if (IsBeam)
            {
                float beamMultiplier = !IsRepairBeam ? BeamDuration * 60f : 0f;
                float damage = DamageAmount != 0 ? DamageAmount : PowerDamage;
                dps = (damage * beamMultiplier) / NetFireDelay;
            }
            else
            {
                dps = (SalvoCount.LowerBound(1) / NetFireDelay)
                    * wOrMirv.ProjectileCount
                    * wOrMirv.DamageAmount * MirvWarheads.LowerBound(1);
            }
            return dps;
        }

        public void InitDamagePerSecond()
        {
            DamagePerSecond = GetDamagePerSecond();
        }

        
        public float CalculateOffense(ShipModule m)
        {
            float off = 0f;
            if (IsBeam)
            {
                off += DamageAmount * 60 * BeamDuration * (1f / NetFireDelay);
                off += TractorDamage * 30 * (1f / NetFireDelay);
                off += PowerDamage * 45 * (1f / NetFireDelay);
                off += RepulsionDamage * 45 * (1f / NetFireDelay);
                off += SiphonDamage * 45 * (1f / NetFireDelay);
                off += TroopDamageChance * (1f / NetFireDelay);
            }
            else
            {
                off += DamageAmount * SalvoCount * ProjectileCount * (1f / NetFireDelay);
                off += EMPDamage * SalvoCount * ProjectileCount * (1f / NetFireDelay) * .5f;
            }

            //Doctor: Guided weapons attract better offensive rating than unguided - more likely to hit
            off *= Tag_Guided ? 3f : 1f;

            off *= 1 + ArmorPen * 0.2f;
            // FB: simpler calcs for these.
            off *= EffectVsArmor > 1 ? 1f + (EffectVsArmor - 1f) / 2f : 1f;
            off *= EffectVsArmor < 1 ? 1f - (1f - EffectVsArmor) / 2f : 1f;
            off *= EffectVsShields > 1 ? 1f + (EffectVsShields - 1f) / 2f : 1f;
            off *= EffectVsShields < 1 ? 1f - (1f - EffectVsShields) / 2f : 1f;

            off *= TruePD ? 0.2f : 1f;
            off *= Tag_Intercept && (Tag_Missile || Tag_Torpedo) ? 0.8f : 1f;
            off *= ProjectileSpeed > 1 ? ProjectileSpeed / BaseRange : 1f;

            // FB: Missiles which can be intercepted might get str modifiers
            off *= Tag_Intercept && RotationRadsPerSecond > 1 ? 1 + HitPoints / 50 / ProjectileRadius.LowerBound(2) : 1;

            // FB: offense calcs for damage radius
            off *= ExplosionRadius > 32 && !TruePD ? ExplosionRadius / 32 : 1f;

            // FB: Added shield pen chance
            off *= 1 + ShieldPenChance / 100;

            if (TerminalPhaseAttack)
            {
                if (TerminalPhaseSpeedMod > 1)
                    off *= 1 + TerminalPhaseDistance * TerminalPhaseSpeedMod / 50000;
                else
                    off *= TerminalPhaseSpeedMod / 2;
            }

            if (DelayedIgnition.Greater(0))
                off *= 1 - (DelayedIgnition / 10).UpperBound(0.95f); 


            // FB: Added correct exclusion offense calcs
            float exclusionMultiplier = 1;
            if (ExcludesFighters)  exclusionMultiplier -= 0.15f;
            if (ExcludesCorvettes) exclusionMultiplier -= 0.15f;
            if (ExcludesCapitals)  exclusionMultiplier -= 0.45f;
            if (ExcludesStations)  exclusionMultiplier -= 0.25f;
            off *= exclusionMultiplier;

            // Imprecision gets worse when range gets higher
            off *= !Tag_Guided ? (1 - FireImprecisionAngle*0.01f * (BaseRange/2000)).LowerBound(0.1f) : 1f;
            
            // Multiple warheads
            if (MirvWarheads > 0 && MirvWeapon.NotEmpty())
            {
                off *= 0.25f; // Warheads mostly do the damage
                IWeaponTemplate warhead = ResourceManager.GetWeaponTemplate(MirvWeapon);
                float warheadOff = warhead.CalculateOffense(m) * MirvWarheads;
                off += warheadOff;
            }

            // FB: Range margins are less steep for missiles
            off *= (!Tag_Guided ? (BaseRange / 4000) * (BaseRange / 4000) : (BaseRange / 4000)) * MirvWarheads.LowerBound(1);

            if (m == null)
                return off * OffPowerMod;

            // FB: Kinetics which does also require more than minimal power to shoot is less effective
            off *= Tag_Kinetic && PowerRequiredToFire > 10 * m.Area ? 0.5f : 1f;

            // FB: Kinetics which does also require more than minimal power to maintain is less effective
            off *= Tag_Kinetic && m.PowerDraw > 2 * m.Area ? 0.5f : 1f;
            // FB: Turrets get some off
            off *= m.ModuleType == ShipModuleType.Turret ? 1.25f : 1f;

            // FB: Field of Fire is also important
            off *= (m.FieldOfFire > RadMath.PI/3) ? (m.FieldOfFire/3) : 1f;

            // Doctor: If there are manual XML override modifiers to a weapon for manual balancing, apply them.
            return off * OffPowerMod;
        }
    }
}
