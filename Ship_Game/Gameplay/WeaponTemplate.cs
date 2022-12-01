using System;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using SDGraphics;
using SDUtils;
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

        public override string ToString() => $"WeaponTemplate Type={WeaponType} UID={UID} Name={Name}";

        // Active Tag Bits for this WeaponTemplate
        WeaponTag TagBits;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Tag(WeaponTag tag, bool value) => TagBits = value ? TagBits|tag : TagBits & ~tag;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Tag(WeaponTag tag) => (TagBits & tag) != 0;

        public static readonly WeaponTag[] TagValues = (WeaponTag[])typeof(WeaponTag).GetEnumValues();

        [XmlIgnore] public WeaponTag[] ActiveWeaponTags { get; set; }

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
        [XmlIgnore] public float DamagePerSecond { get; private set; }

        // Number of salvos that will be sequentially spawned.
        // For example, Vulcan Cannon fires a salvo of 20
        public int SalvoCount { get; set; } = 1;

        // This is the total salvo duration
        // TimeBetweenShots = SalvoTimer / SalvoCount;
        [XmlElement(ElementName = "SalvoTimer")]
        public float SalvoDuration { get; set; }


        [XmlIgnore]
        public float NetFireDelay => IsBeam ? FireDelay+BeamDuration : FireDelay+SalvoDuration;

        [XmlIgnore]
        public float AverageOrdnanceUsagePerSecond => OrdinanceRequiredToFire * ProjectileCount * SalvoCount / NetFireDelay;

        [XmlIgnore]
        public float TotalOrdnanceUsagePerFire => OrdinanceRequiredToFire * ProjectileCount * SalvoCount;

        [XmlIgnore]
        public bool Explodes => ExplosionRadius > 0;

        [XmlIgnore] // only usage during fire, not power maintenance
        public float PowerFireUsagePerSecond => (BeamPowerCostPerSecond * BeamDuration + PowerRequiredToFire * ProjectileCount * SalvoCount) / NetFireDelay;

        [XmlIgnore]
        public bool IsMirv => MirvWeapon.NotEmpty();
        public void InitializeTemplate()
        {
            ActiveWeaponTags = TagValues.Filter(tag => (TagBits & tag) != 0);

            BeamDuration = BeamDuration > 0 ? BeamDuration : 2f;
            FireDelay = Math.Max(0.016f, FireDelay);
            SalvoDuration = Math.Max(0, SalvoDuration);
            ExplosionRadiusVisual *= GlobalStats.Settings.ExplosionVisualIncreaser;

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

            DamagePerSecond = GetDamagePerSecond();
        }

        float GetDamagePerSecond() // FB: todo - do this also when new tech is unlocked (bonuses)
        {
            IWeaponTemplate wOrMirv = this;
            if (IsMirv)
            {
                wOrMirv = ResourceManager.GetWeaponTemplate(MirvWeapon);
                if (wOrMirv == null)
                {
                    Log.Error($"Mirv weapon template {MirvWeapon} not found, using {UID}");
                    wOrMirv = this;
                }
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
                    * ProjectileCount
                    * DamageAmount;

                if (IsMirv && wOrMirv.ProjectileCount > 0)
                    dps *= wOrMirv.ProjectileCount.LowerBound(1);
            }
            return dps;
        }

        public static float GetWeaponInaccuracyBase(float moduleArea, float overridePercent)
        {
            float powerMod;
            float moduleSize;

            if (overridePercent >= 0)
            {
                powerMod = 1 - overridePercent;
                moduleSize = 8 * 8 * 16 + 160;
            }
            else
            {
                powerMod = 1.2f;
                moduleSize = moduleArea * 16f + 160;
            }

            return (float)Math.Pow(moduleSize, powerMod);
        }

        public float CalculateOffense(ShipModule m)
        {
            return CalculateOffense(m, this);
        }

        public static float CalculateOffense(ShipModule m, IWeaponTemplate t)
        {
            float off = 0f;
            float shotsPerSec = (1.0f / t.NetFireDelay);

            if (t.IsBeam)
            {
                off += t.DamageAmount * 60 * t.BeamDuration * shotsPerSec;
                off += t.TractorDamage * 30 * shotsPerSec;
                off += t.PowerDamage * 45 * shotsPerSec;
                off += t.RepulsionDamage * 45 * shotsPerSec;
                off += t.SiphonDamage * 45 * shotsPerSec;
                off += t.TroopDamageChance * shotsPerSec;
            }
            else
            {
                off += t.DamageAmount * t.SalvoCount * t.ProjectileCount * shotsPerSec;
                off += t.EMPDamage * t.SalvoCount * t.ProjectileCount * shotsPerSec * 0.5f;
            }

            //Doctor: Guided weapons attract better offensive rating than unguided - more likely to hit
            off *= t.Tag_Guided ? 3f : 1f;

            off *= 1 + t.ArmorPen * 0.2f;
            // FB: simpler calcs for these.
            off *= t.EffectVsArmor > 1 ? 1f + (t.EffectVsArmor - 1f) / 2f : 1f;
            off *= t.EffectVsArmor < 1 ? 1f - (1f - t.EffectVsArmor) / 2f : 1f;
            off *= t.EffectVsShields > 1 ? 1f + (t.EffectVsShields - 1f) / 2f : 1f;
            off *= t.EffectVsShields < 1 ? 1f - (1f - t.EffectVsShields) / 2f : 1f;

            off *= t.TruePD ? 0.2f : 1f;
            off *= t.Tag_Intercept && (t.Tag_Missile || t.Tag_Torpedo) ? 0.8f : 1f;
            off *= t.ProjectileSpeed > 1 ? t.ProjectileSpeed / t.BaseRange : 1f;

            // FB: Missiles which can be intercepted might get str modifiers
            off *= t.Tag_Intercept && t.RotationRadsPerSecond > 1 ? 1 + t.HitPoints / 50 / t.ProjectileRadius.LowerBound(2) : 1;

            // FB: offense calcs for damage radius
            off *= t.ExplosionRadius > 16 && !t.TruePD ? t.ExplosionRadius / 16 : 1f;

            // FB: Added shield pen chance
            off *= 1 + t.ShieldPenChance / 100;

            if (t.TerminalPhaseAttack)
            {
                if (t.TerminalPhaseSpeedMod > 1)
                    off *= 1 + t.TerminalPhaseDistance * t.TerminalPhaseSpeedMod / 50000;
                else
                    off *= t.TerminalPhaseSpeedMod / 2;
            }

            if (t.DelayedIgnition.Greater(0))
                off *= 1 - (t.DelayedIgnition / 10).UpperBound(0.95f);


            // FB: Added correct exclusion offense calcs
            float exclusionMultiplier = 1;
            if (t.ExcludesFighters) exclusionMultiplier -= 0.15f;
            if (t.ExcludesCorvettes) exclusionMultiplier -= 0.15f;
            if (t.ExcludesCapitals) exclusionMultiplier -= 0.45f;
            if (t.ExcludesStations) exclusionMultiplier -= 0.25f;
            off *= exclusionMultiplier;

            // Imprecision gets worse when range gets higher
            off *= !t.Tag_Guided ? (1 - t.FireImprecisionAngle * 0.01f * (t.BaseRange / 2000)).LowerBound(0.1f) : 1f;

            // FB: Range margins are less steep for missiles
            off *= (!t.Tag_Guided ? (t.BaseRange / 4000) * (t.BaseRange / 4000) : (t.BaseRange / 4000));

            // Multiple warheads
            if (t.IsMirv)
            {
                IWeaponTemplate warhead = ResourceManager.GetWeaponTemplate(t.MirvWeapon);
                float warheadOff = warhead.CalculateOffense(m);
                off += warheadOff;
            }

            if (m == null)
                return off * t.OffPowerMod;

            // FB: Kinetics which does also require more than minimal power to shoot is less effective
            off *= t.Tag_Kinetic && t.PowerRequiredToFire > 10 * m.Area ? 0.5f : 1f;

            // FB: Kinetics which does also require more than minimal power to maintain is less effective
            off *= t.Tag_Kinetic && m.PowerDraw > 2 * m.Area ? 0.5f : 1f;
            // FB: Turrets get some off
            off *= m.ModuleType == ShipModuleType.Turret ? 1.25f : 1f;

            // FB: Field of Fire is also important
            if (!t.IsMirv) // Only for non Mirv since this should be calculated once
                off *= (m.FieldOfFire / (RadMath.PI / 3)).Clamped(1,4);

            // Doctor: If there are manual XML override modifiers to a weapon for manual balancing, apply them.
            return off * t.OffPowerMod;
        }

        // modify damage amount utilizing tech bonus. Currently this is only ordnance bonus.
        public static float GetDamageWithBonuses(Ship owner, IWeaponTemplate t)
        {
            float damageAmount = t.DamageAmount;
            if (owner?.Loyalty.data != null && t.OrdinanceRequiredToFire > 0)
                damageAmount += damageAmount * owner.Loyalty.data.OrdnanceEffectivenessBonus;

            if (owner?.Level > 0)
                damageAmount += damageAmount * owner.Level * 0.05f;

            // Hull bonus damage increase
            if (GlobalStats.Settings.UseHullBonuses && owner != null &&
                ResourceManager.HullBonuses.TryGetValue(owner.ShipData.Hull, out HullBonus mod))
            {
                damageAmount += damageAmount * mod.DamageBonus;
            }

            return damageAmount;
        }

        public static float GetActualRange(Empire owner, IWeaponTemplate t)
        {
            float range = t.BaseRange;

            // apply extra range bonus based on weapon tag type:
            for (int i = 0; i < t.ActiveWeaponTags.Length; ++i)
            {
                WeaponTagModifier mod = owner.data.WeaponTags[t.ActiveWeaponTags[i]];
                range += mod.Range * t.BaseRange;
            }
            return range;
        }
    }
}
