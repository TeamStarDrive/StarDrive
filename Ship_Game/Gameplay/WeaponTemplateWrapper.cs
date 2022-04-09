using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Ships;

namespace Ship_Game.Gameplay
{
    // An adapter which provides a view to a WeaponTemplate
    public class WeaponTemplateWrapper : IWeaponTemplate
    {
        public readonly IWeaponTemplate T;

        public WeaponTemplateWrapper(IWeaponTemplate t)
        {
            T = t;
        }

        // This is the WeaponTemplate UID string
        public string UID => T.UID;

        public WeaponTag[] ActiveWeaponTags => T.ActiveWeaponTags;

        // @note These are initialized from XML during serialization
        public bool Tag_Kinetic => T.Tag_Kinetic;
        public bool Tag_Energy => T.Tag_Energy;
        public bool Tag_Guided => T.Tag_Guided;
        public bool Tag_Missile => T.Tag_Missile;
        public bool Tag_Plasma => T.Tag_Plasma;
        public bool Tag_Beam => T.Tag_Beam;
        public bool Tag_Intercept => T.Tag_Intercept;
        public bool Tag_Bomb => T.Tag_Bomb;
        public bool Tag_SpaceBomb => T.Tag_SpaceBomb;
        public bool Tag_BioWeapon => T.Tag_BioWeapon;
        public bool Tag_Drone => T.Tag_Drone;
        public bool Tag_Torpedo => T.Tag_Torpedo;
        public bool Tag_Cannon => T.Tag_Cannon;
        public virtual bool Tag_PD => T.Tag_PD;

        public virtual float HitPoints => T.HitPoints;
        public bool IsBeam => T.IsBeam;
        public virtual bool TruePD => T.TruePD;
        public float EffectVsArmor => T.EffectVsArmor;
        public float EffectVsShields => T.EffectVsShields;
        public float TroopDamageChance => T.TroopDamageChance;
        public float TractorDamage => T.TractorDamage;
        public float BombPopulationKillPerHit => T.BombPopulationKillPerHit;
        public int BombTroopDamageMin => T.BombTroopDamageMin;
        public int BombTroopDamageMax => T.BombTroopDamageMax;
        public int BombHardDamageMin => T.BombHardDamageMin;
        public int BombHardDamageMax => T.BombHardDamageMax;
        public string HardCodedAction => T.HardCodedAction;
        public float RepulsionDamage => T.RepulsionDamage;
        public float EMPDamage => T.EMPDamage;
        public float ShieldPenChance => T.ShieldPenChance;
        public float PowerDamage => T.PowerDamage;
        public float SiphonDamage => T.SiphonDamage;
        public int BeamThickness => T.BeamThickness;
        public float BeamDuration => T.BeamDuration;
        public int BeamPowerCostPerSecond => T.BeamPowerCostPerSecond;
        public string BeamTexture => T.BeamTexture;
        public int Animated => T.Animated;
        public int Frames => T.Frames;
        public string AnimationPath => T.AnimationPath;
        public ExplosionType ExplosionType => T.ExplosionType;
        public string DieCue => T.DieCue;
        public string ToggleSoundName => T.ToggleSoundName;
        public string Light => T.Light;
        public virtual float OrdinanceRequiredToFire => T.OrdinanceRequiredToFire;

        // This is the weapons base unaltered range. In addition to this, many bonuses could be applied.
        // Use GetActualRange() to the true range with bonuses
        public virtual float BaseRange => T.BaseRange;

        public virtual float DamageAmount => T.DamageAmount;
        public virtual float ProjectileSpeed => T.ProjectileSpeed;

        // The number of projectiles spawned from a single "shot".
        // Most commonly it is 1. AFAIK, only some FLAK cannons spawn buck-shots.
        public virtual int ProjectileCount => T.ProjectileCount;

        // for cannons that have ProjectileCount > 1, this spreads projectiles out in an arc
        // this spreading out has a very strict pattern
        public int FireDispersionArc => T.FireDispersionArc;

        // this is the fire imprecision angle, direction spread is randomized [-FireCone,+FireCone]
        // the cannon simply has random variability in its shots
        public int FireImprecisionAngle => T.FireImprecisionAngle;

        public string ProjectileTexturePath => T.ProjectileTexturePath;
        public string ModelPath => T.ModelPath;
        public string WeaponType => T.WeaponType;

        // Determines Hit, ShieldHit, Death and Trail effects from ParticleEffects.yaml
        public string WeaponHitEffect => T.WeaponHitEffect;
        public string WeaponShieldHitEffect => T.WeaponShieldHitEffect;
        public string WeaponDeathEffect => T.WeaponDeathEffect;
        public string WeaponTrailEffect => T.WeaponTrailEffect;

        // The trail offset behind missile center
        public float TrailOffset => T.TrailOffset;

        public float FireDelay => T.FireDelay;
        public virtual float PowerRequiredToFire => T.PowerRequiredToFire;
        public virtual float ExplosionRadius => T.ExplosionRadius;
        public string FireCueName => T.FireCueName;
        public string MuzzleFlash => T.MuzzleFlash;
        public bool IsRepairDrone => T.IsRepairDrone;
        public bool FakeExplode => T.FakeExplode;
        public virtual float ProjectileRadius => T.ProjectileRadius;
        public string Name => T.Name;
        public byte LoopAnimation => T.LoopAnimation;
        public float Scale => T.Scale;
        public float RotationRadsPerSecond => T.RotationRadsPerSecond;
        public string InFlightCue => T.InFlightCue;
        public float ParticleDelay => T.ParticleDelay;
        public float ECMResist => T.ECMResist;
        public bool ExcludesFighters => T.ExcludesFighters;
        public bool ExcludesCorvettes => T.ExcludesCorvettes;
        public bool ExcludesCapitals => T.ExcludesCapitals;
        public bool ExcludesStations => T.ExcludesStations;
        public bool IsRepairBeam => T.IsRepairBeam;
        public bool TerminalPhaseAttack => T.TerminalPhaseAttack;
        public float TerminalPhaseDistance => T.TerminalPhaseDistance;
        public float TerminalPhaseSpeedMod => T.TerminalPhaseSpeedMod;
        public float DelayedIgnition => T.DelayedIgnition;
        public float MirvWarheads => T.MirvWarheads;
        public float MirvSeparationDistance => T.MirvSeparationDistance;
        public string MirvWeapon => T.MirvWeapon;
        public int ArmorPen => T.ArmorPen;
        public float OffPowerMod => T.OffPowerMod;
        public float FertilityDamage => T.FertilityDamage;
        public bool RangeVariance => T.RangeVariance;
        public float ExplosionRadiusVisual => T.ExplosionRadiusVisual;
        public bool UseVisibleMesh => T.UseVisibleMesh;
        public bool PlaySoundOncePerSalvo => T.PlaySoundOncePerSalvo;
        public int SalvoSoundInterval => T.SalvoSoundInterval;

        // STAT Generated automatically after all weapons are loaded
        public float DamagePerSecond => T.DamagePerSecond;

        // Number of salvos that will be sequentially spawned.
        // For example, Vulcan Cannon fires a salvo of 20
        public virtual int SalvoCount => T.SalvoCount;

        // This is the total salvo duration
        // TimeBetweenShots = SalvoTimer / SalvoCount;
        public float SalvoDuration => T.SalvoDuration;

        public float NetFireDelay => T.NetFireDelay;
        public float AverageOrdnanceUsagePerSecond => T.AverageOrdnanceUsagePerSecond;
        public float BurstOrdnanceUsagePerSecond => T.BurstOrdnanceUsagePerSecond;
        public float SalvoProjectilesPerSecond => T.SalvoProjectilesPerSecond;
        public bool Explodes => T.Explodes;
        public float PowerFireUsagePerSecond => T.PowerFireUsagePerSecond;

        // TODO: this CalculateOffense is wrong when `virtual` overrides are touched
        public float CalculateOffense(ShipModule m) => T.CalculateOffense(m);

        // modify damage amount utilizing tech bonus. Currently this is only ordnance bonus.
        public float GetDamageWithBonuses(Ship owner)
        {
            float damageAmount = DamageAmount;
            if (owner?.Loyalty.data != null && OrdinanceRequiredToFire > 0)
                damageAmount += damageAmount * owner.Loyalty.data.OrdnanceEffectivenessBonus;

            if (owner?.Level > 0)
                damageAmount += damageAmount * owner.Level * 0.05f;

            // Hull bonus damage increase
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.UseHullBonuses && owner != null &&
                ResourceManager.HullBonuses.TryGetValue(owner.ShipData.Hull, out HullBonus mod))
            {
                damageAmount += damageAmount * mod.DamageBonus;
            }

            return damageAmount;
        }

        public float GetActualRange(Empire owner)
        {
            float range = BaseRange;

            // apply extra range bonus based on weapon tag type:
            for (int i = 0; i < ActiveWeaponTags.Length; ++i)
            {
                WeaponTagModifier mod = owner.data.WeaponTags[ActiveWeaponTags[i]];
                range += mod.Range * BaseRange;
            }
            return range;
        }
    }
}
