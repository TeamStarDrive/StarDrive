using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using Ship_Game.Audio;
using System.Runtime.InteropServices.WindowsRuntime;
using static Ship_Game.EmpireManager;

namespace Ship_Game.Gameplay
{
    [Flags]
    public enum WeaponTag
    {
        Kinetic   = (1 << 0),
        Energy    = (1 << 1),
        Guided    = (1 << 2),
        Missile   = (1 << 3),
        Hybrid    = (1 << 4),
        Beam      = (1 << 5),
        Explosive = (1 << 6),
        Intercept = (1 << 7),
        Railgun   = (1 << 8),
        Bomb      = (1 << 9),
        SpaceBomb = (1 << 10),
        BioWeapon = (1 << 11),
        Drone     = (1 << 12),
        Warp      = (1 << 13),
        Torpedo   = (1 << 14),
        Cannon    = (1 << 15),
        Subspace  = (1 << 16),
        PD        = (1 << 17),
        Flak      = (1 << 18),
        Array     = (1 << 19),
        Tractor   = (1 << 20)
    }

    public sealed class Weapon : IDisposable, IDamageModifier
    {
        WeaponTag TagBits;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Tag(WeaponTag tag, bool value) => TagBits = value ? TagBits|tag : TagBits & ~tag;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Tag(WeaponTag tag) => (TagBits & tag) != 0;

        public static readonly WeaponTag[] TagValues = (WeaponTag[])typeof(WeaponTag).GetEnumValues();

        [XmlIgnore][JsonIgnore] 
        public WeaponTag[] ActiveWeaponTags;

        // @note These are initialized from XML during serialization
        public bool Tag_Kinetic   { get => Tag(WeaponTag.Kinetic);   set => Tag(WeaponTag.Kinetic, value);   }
        public bool Tag_Energy    { get => Tag(WeaponTag.Energy);    set => Tag(WeaponTag.Energy, value);    }
        public bool Tag_Guided    { get => Tag(WeaponTag.Guided);    set => Tag(WeaponTag.Guided, value);    }
        public bool Tag_Missile   { get => Tag(WeaponTag.Missile);   set => Tag(WeaponTag.Missile, value);   }
        public bool Tag_Hybrid    { get => Tag(WeaponTag.Hybrid);    set => Tag(WeaponTag.Hybrid, value);    }
        public bool Tag_Beam      { get => Tag(WeaponTag.Beam);      set => Tag(WeaponTag.Beam, value);      }
        public bool Tag_Explosive { get => Tag(WeaponTag.Explosive); set => Tag(WeaponTag.Explosive, value); }
        public bool Tag_Intercept { get => Tag(WeaponTag.Intercept); set => Tag(WeaponTag.Intercept, value); }
        public bool Tag_Railgun   { get => Tag(WeaponTag.Railgun);   set => Tag(WeaponTag.Railgun, value);   }
        public bool Tag_Bomb      { get => Tag(WeaponTag.Bomb);      set => Tag(WeaponTag.Bomb, value);      }
        public bool Tag_SpaceBomb { get => Tag(WeaponTag.SpaceBomb); set => Tag(WeaponTag.SpaceBomb, value); }
        public bool Tag_BioWeapon { get => Tag(WeaponTag.BioWeapon); set => Tag(WeaponTag.BioWeapon, value); }
        public bool Tag_Drone     { get => Tag(WeaponTag.Drone);     set => Tag(WeaponTag.Drone, value);     }
        public bool Tag_Warp      { get => Tag(WeaponTag.Warp);      set => Tag(WeaponTag.Warp, value);      }
        public bool Tag_Torpedo   { get => Tag(WeaponTag.Torpedo);   set => Tag(WeaponTag.Torpedo, value);   }
        public bool Tag_Cannon    { get => Tag(WeaponTag.Cannon);    set => Tag(WeaponTag.Cannon, value);    }
        public bool Tag_Subspace  { get => Tag(WeaponTag.Subspace);  set => Tag(WeaponTag.Subspace, value);  }
        public bool Tag_PD        { get => Tag(WeaponTag.PD);        set => Tag(WeaponTag.PD, value);        }
        public bool Tag_Flak      { get => Tag(WeaponTag.Flak);      set => Tag(WeaponTag.Flak, value);      }
        public bool Tag_Array     { get => Tag(WeaponTag.Array);     set => Tag(WeaponTag.Array, value);     }
        public bool Tag_Tractor   { get => Tag(WeaponTag.Tractor);   set => Tag(WeaponTag.Tractor, value);   }

        [XmlIgnore][JsonIgnore] public Ship Owner { get; set; }

        public float HitPoints;
        public bool isBeam;
        public float EffectVsArmor = 1f;
        public float EffectVSShields = 1f;
        public bool TruePD;
        public float TroopDamageChance;
        public float MassDamage;
        public float BombPopulationKillPerHit;
        public int BombTroopDamage_Min;
        public int BombTroopDamage_Max;
        public int BombHardDamageMin;
        public int BombHardDamageMax;
        public string HardCodedAction;
        public float RepulsionDamage;
        public float EMPDamage;
        public float ShieldPenChance;
        public float PowerDamage;
        public float SiphonDamage;
        public int BeamThickness;
        public float BeamDuration;
        public int BeamPowerCostPerSecond;
        public string BeamTexture;
        public int Animated;
        public int Frames;
        public string AnimationPath;
        public ExplosionType ExplosionType = ExplosionType.Projectile;
        public string dieCue;
        public string ToggleSoundName = "";
        [XmlIgnore][JsonIgnore] readonly AudioHandle ToggleCue = new AudioHandle();
        public string Light;
        public bool isTurret;
        public bool isMainGun;
        public float OrdinanceRequiredToFire;
        // Separate because Weapons attached to Planetary Buildings, don't have a ShipModule Center
        public Vector2 PlanetOrigin;
        
        // This is the weapons base unaltered range. In addition to this, many bonuses could be applied.
        // Use GetActualRange() to the true range with bonuses
        [XmlElement(ElementName = "Range")]
        public float BaseRange;

        public float DamageAmount;
        public float ProjectileSpeed;

        // The number of projectiles spawned from a single "shot".
        // Most commonly it is 1. AFAIK, only some FLAK cannons spawn buck-shots.
        public int ProjectileCount = 1;

        // for cannons that have ProjectileCount > 1, this spreads projectiles out in an arc
        // this spreading out has a very strict pattern
        [XmlElement(ElementName = "FireArc")]
        public int FireDispersionArc;
        
        // this is the fire imprecision angle, direction spread is randomized [-FireCone,+FireCone]
        // the cannon simply has random variability in its shots
        [XmlElement(ElementName = "FireCone")]
        public int FireImprecisionAngle; 
        
        public string ProjectileTexturePath;
        public string ModelPath;
        public string WeaponType;
        public string WeaponEffectType;
        public string UID;
        [XmlIgnore][JsonIgnore] public ShipModule Module;
        [XmlIgnore][JsonIgnore] public float CooldownTimer;
        public float fireDelay;
        public float PowerRequiredToFire;
        public bool explodes;
        public float DamageRadius;
        public string fireCueName;
        public string MuzzleFlash;
        public bool IsRepairDrone;
        public bool FakeExplode;
        public float ProjectileRadius = 4f;
        public string Name;
        public byte LoopAnimation;
        public float Scale = 1f;
        public float RotationRadsPerSecond = 2f;
        public string InFlightCue = "";
        public float particleDelay;
        public float ECMResist;
        public bool Excludes_Fighters;
        public bool Excludes_Corvettes;
        public bool Excludes_Capitals;
        public bool Excludes_Stations;
        public bool isRepairBeam;
        public bool TerminalPhaseAttack;
        public float TerminalPhaseDistance;
        public float TerminalPhaseSpeedMod = 2f;
        public float DelayedIgnition;
        public float MirvWarheads;
        public float MirvSeparationDistance;
        public string MirvWeapon;
        public int ArmorPen = 0;
        public float OffPowerMod = 1f;
        public float FertilityDamage;
        public bool RangeVariance;
        public float ExplosionRadiusVisual = 4.5f;
        [XmlIgnore][JsonIgnore] public GameplayObject FireTarget { get; private set; }
        float TargetChangeTimer;
        public bool UseVisibleMesh;
        public bool PlaySoundOncePerSalvo; // @todo DEPRECATED
        public int SalvoSoundInterval = 1; // play sound effect every N salvos
        [XmlIgnore][JsonIgnore] public int DamagePerSecond { get; private set; }

        // Number of salvos that will be sequentially spawned.
        // For example, Vulcan Cannon fires a salvo of 20
        public int SalvoCount = 1;

        // This is the total salvo duration
        // TimeBetweenShots = SalvoTimer / SalvoCount;
        [XmlElement(ElementName = "SalvoTimer")]
        public float SalvoDuration;

        [XmlIgnore][JsonIgnore] public int SalvosToFire { get; private set; }
        float SalvoDirection;
        float SalvoFireTimer; // while SalvosToFire, use this timer to count when to fire next shot
        GameplayObject SalvoTarget;
        public float ECM = 0;

        public void InitializeTemplate()
        {
            ActiveWeaponTags = TagValues.Where(tag => (TagBits & tag) != 0).ToArray();

            BeamDuration = BeamDuration > 0 ? BeamDuration : 2f;
            fireDelay = Math.Max(0.016f, fireDelay);
            SalvoDuration = Math.Max(0, SalvoDuration);

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

        public Weapon(Ship owner, ShipModule module)
        {
            Owner  = owner;
            Module = module;
        }

        public Weapon()
        {
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo != null)
                ExplosionRadiusVisual *= GlobalStats.ActiveModInfo.GlobalExplosionVisualIncreaser;
        }

        public Weapon Clone()
        {
            var wep = (Weapon)MemberwiseClone();
            wep.SalvoTarget = null;
            wep.FireTarget  = null;
            wep.Module      = null;
            wep.Owner       = null;
            return wep;
        }

        [XmlIgnore][JsonIgnore]
        public float NetFireDelay => isBeam ? fireDelay+BeamDuration : fireDelay+SalvoDuration;

        [XmlIgnore][JsonIgnore]
        public float AverageOrdnanceUsagePerSecond => OrdinanceRequiredToFire * ProjectileCount * SalvoCount / NetFireDelay;

        [XmlIgnore][JsonIgnore]
        public float BurstOrdnanceUsagePerSecond => OrdinanceRequiredToFire * ProjectileCount * SalvoProjectilesPerSecond;

        [XmlIgnore][JsonIgnore] // 3 salvos with salvo duration of 2 seconds will give  1.5 salvos per second 
        float SalvoProjectilesPerSecond => SalvoDuration.Greater(0) ? SalvoCount / SalvoDuration : 1;

        [XmlIgnore][JsonIgnore] // only usage during fire, not power maintenance
        public float PowerFireUsagePerSecond => (BeamPowerCostPerSecond * BeamDuration + PowerRequiredToFire * ProjectileCount * SalvoCount) / NetFireDelay;

        public void CalcDamagePerSecond() // FB: todo - do this also when new tech is unlocked (bonuses)
        {
            Weapon wOrMirv = this; 
            if (MirvWarheads > 0 && MirvWeapon.NotEmpty())
            {
                Weapon warhead = ResourceManager.CreateWeapon(MirvWeapon);
                wOrMirv        = warhead;
            }

            int salvos           = SalvoCount.LowerBound(1);
            float beamMultiplier = isBeam && !isRepairBeam ? BeamDuration * 60f : 0f;
            float dps            = isBeam 
                ? DamageAmount * beamMultiplier / NetFireDelay
                : (salvos / NetFireDelay) * wOrMirv.ProjectileCount * wOrMirv.DamageAmount * MirvWarheads.LowerBound(1);

            DamagePerSecond = (int)dps;
        }

        // modify damage amount utilizing tech bonus. Currently this is only ordnance bonus.
        public float GetDamageWithBonuses(Ship owner)
        {
            float damageAmount = DamageAmount;
            if (owner?.loyalty.data != null && OrdinanceRequiredToFire > 0)
                damageAmount += damageAmount * owner.loyalty.data.OrdnanceEffectivenessBonus;

            if (owner?.Level > 0)
                damageAmount += damageAmount * owner.Level * 0.05f;

            // Hull bonus damage increase
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.UseHullBonuses && owner != null &&
                ResourceManager.HullBonuses.TryGetValue(owner.shipData.Hull, out HullBonus mod))
            {
                damageAmount += damageAmount * mod.DamageBonus;
            }

            return damageAmount;
        }

        public void PlayToggleAndFireSfx(AudioEmitter emitter = null)
        {
            if (ToggleCue.IsPlaying)
                return;

            AudioEmitter soundEmitter = emitter ?? Owner?.SoundEmitter;
            GameAudio.PlaySfxAsync(fireCueName, soundEmitter);
            ToggleCue.PlaySfxAsync(ToggleSoundName, soundEmitter);
        }

        public void FireDrone(Vector2 direction)
        {
            if (CanFireWeaponCooldown())
            {
                PrepareToFire();
                Projectile.Create(this, Module.Center, direction, null, playSound: true);
            }
        }

        Vector2 ApplyFireImprecisionAngle(Vector2 direction)
        {
            if (FireImprecisionAngle <= 0)
                return direction;
            float spread = RandomMath2.RandomBetween(-FireImprecisionAngle, FireImprecisionAngle) * 0.5f;
            return (direction.ToDegrees() + spread).AngleToDirection();
        }

        struct FireSource
        {
            public readonly Vector2 Origin;
            public readonly Vector2 Direction;
            public FireSource(Vector2 origin, Vector2 direction)
            {
                Origin    = origin;
                Direction = direction;
            }
        }

        IEnumerable<FireSource> EnumFireSources(Vector2 origin, Vector2 direction)
        {
            if (ProjectileCount == 1) // most common case
            {
                yield return new FireSource(origin, ApplyFireImprecisionAngle(direction));
            }
            else if (FireDispersionArc != 0)
            {
                float degreesBetweenShots = FireDispersionArc / (float)ProjectileCount;
                float angleToTarget = direction.ToDegrees() - FireDispersionArc * 0.5f;
                for (int i = 0; i < ProjectileCount; ++i)
                {
                    Vector2 dir = angleToTarget.AngleToDirection();
                    angleToTarget += degreesBetweenShots;
                    yield return new FireSource(origin, ApplyFireImprecisionAngle(dir));
                }
            }
            else
            {
                for (int i = 0; i < ProjectileCount; ++i)
                {
                    yield return new FireSource(origin, ApplyFireImprecisionAngle(direction));
                }
            }
        }

        void SpawnSalvo(Vector2 direction, GameplayObject target, bool playSound)
        {
            Vector2 origin = Origin;
            foreach (FireSource fireSource in EnumFireSources(origin, direction))
            {
                Projectile.Create(this, fireSource.Origin, fireSource.Direction, target, playSound);
                playSound = false; // only play sound once per fire cone
            }
        }

        bool CanFireWeapon()
        {
            return Module.Active && Module.Powered
                && Owner.engineState  != Ship.MoveState.Warp
                && Owner.PowerCurrent >= PowerRequiredToFire
                && Owner.Ordinance    >= OrdinanceRequiredToFire;
        }

        bool CanFireWeaponCooldown()
        {
            return CooldownTimer <= 0f && CanFireWeapon();
        }

        void PrepareToFire()
        {
            // cooldown should start after all salvos have finished, so
            // increase the cooldown by SalvoTimer
            CooldownTimer = NetFireDelay + RandomMath.RandomBetween(-10f, +10f) * 0.008f;

            Owner.InCombatTimer = 15f;
            Owner.ChangeOrdnance(-OrdinanceRequiredToFire);
            Owner.PowerCurrent -= PowerRequiredToFire;
        }

        bool PrepareToFireSalvo()
        {
            float timeBetweenShots = SalvoDuration / SalvoCount;
            if (SalvoFireTimer < timeBetweenShots)
                return false; // not ready to fire salvo 

            if (!CanFireWeapon())
            {
                // reset salvo and weapon, forgetting any partial salvo that may remain.
                CooldownTimer = NetFireDelay;
                SalvosToFire = 0;
                SalvoFireTimer = 0f;
                return false;
            }

            SalvoFireTimer -= timeBetweenShots;
            --SalvosToFire;

            Owner.InCombatTimer = 15f;
            Owner.ChangeOrdnance(-OrdinanceRequiredToFire);
            Owner.PowerCurrent -= PowerRequiredToFire;
            return true;
        }

        void ContinueSalvo()
        {
            if (SalvoTarget != null && !Owner.CheckRangeToTarget(this, SalvoTarget))
                return;

            int salvoIndex = SalvoCount - SalvosToFire;
            if (!PrepareToFireSalvo())
                return;

            if (SalvoTarget != null) // check for new direction
            {
                // update direction only if we have a new valid pip
                if (PrepareFirePos(SalvoTarget, SalvoTarget.Center, out Vector2 firePos))
                    SalvoDirection = (firePos - Module.Center).ToRadians() - Owner.Rotation;
            }

            bool playSound = salvoIndex % SalvoSoundInterval == 0;
            SpawnSalvo((SalvoDirection + Owner.Rotation).RadiansToDirection(), SalvoTarget, playSound);
        }

        bool PrepareFirePos(GameplayObject target, Vector2 targetPos, out Vector2 firePos)
        {
            if (target != null)
            {
                if (ProjectedImpactPoint(target, out Vector2 pip) && Owner.IsInsideFiringArc(this, pip))
                {
                    firePos = pip;
                    return true;
                }
            }

            // if no target OR pip was not in arc, check if targetPos itself is in arc
            if (Owner.IsInsideFiringArc(this, targetPos))
            {
                firePos = targetPos;
                return true;
            }
            firePos = targetPos;
            return false;
        }

        bool FireAtTarget(Vector2 targetPos, GameplayObject target = null)
        {
            if (!PrepareFirePos(target, targetPos, out Vector2 firePos))
                return false;

            PrepareToFire();
            Vector2 direction = Origin.DirectionToTarget(firePos);
            SpawnSalvo(direction, target, playSound:true);

            if (SalvoCount > 1)  // queue the rest of the salvo to follow later
            {
                SalvosToFire   = SalvoCount - 1;
                SalvoDirection = direction.ToRadians() - Owner.Rotation; // keep direction relative to source
                SalvoFireTimer = 0f;
                SalvoTarget    = target;
            }
            return true;
        }

        public void ClearFireTarget()
        {
            FireTarget  = null;
            SalvoTarget = null;
        }

        // > 66% accurate or >= Level 5 crews can use advanced
        // targeting which even predicts acceleration
        public bool CanUseAdvancedTargeting =>
            (Module != null && Module.AccuracyPercent > 0.66f) ||
            (Owner  != null && Owner.CanUseAdvancedTargeting);

        Vector2 GetLevelBasedError(int level = -1)
        {
            float adjust = BaseTargetError(level);

            return RandomMath2.Vector2D(adjust);
        }

        public float BaseTargetError(float level, float range = 0, Empire loyalty = null)
        {
            if (Module == null)
                return 0;
            
            // base error is based on module size or accuracyPercent.
            float baseError;
            if (Module?.AccuracyPercent.AlmostEqual(-1) == false)
            {
                // should be calculated once. 
                baseError = (float)Math.Pow(1000 , (1 - Module.AccuracyPercent));
            }
            else
            {
                if (TruePD) baseError = 0;
                //else baseError = (Module.Area + 16) * (Module.Area + 16);
                else baseError = (float)Math.Pow(Module.Area * 16f + 160, 1.2f);
            }

            // Skip all accuracy if weapon is 100% accurate
            if (baseError.AlmostZero())
                return 0;

            if (level < 0)
            {
                // calculate at ship update
                level = (Owner?.Level ?? 0) + loyalty?.data.Traits.Militaristic ?? 0;
                level = (float)Math.Pow(level, 2f);
                level += (Owner?.TargetingAccuracy ?? 0);
            }
            
            level += 5;

            // reduce the error by level
            float adjust = (baseError / level -16f).LowerBound(0);
            
            // reduce or increase error based on weapon and trait characteristics.
            // this could be pre-calculated in the flyweight
            if (Tag_Cannon) adjust  *= (1f - (Owner?.loyalty?.data.Traits.EnergyDamageMod ?? 0));
            if (Tag_Kinetic) adjust *= (1f - (Owner?.loyalty?.data.OrdnanceEffectivenessBonus ?? 0));
            if (isTurret) adjust    *= 0.5f;
            if (Tag_PD) adjust      *= 0.5f;
            if (TruePD) adjust      *= 0.5f;
            return adjust;
        }

        // @note This is used for debugging
        [XmlIgnore][JsonIgnore]
        public Vector2 DebugLastImpactPredict { get; private set; }

        public Vector2 GetTargetError(GameplayObject target, int level = -1)
        {
            Vector2 error = GetLevelBasedError(level); // base error from crew level/targeting bonuses
            if (target != null)
            {
                error += target.JammingError(); // if target has ECM, they can scramble their position
            }
            return error;
        }

        // Applies correction to weapon target based on distance to target
        public Vector2 AdjustedImpactPoint(Vector2 source, Vector2 target, Vector2 error)
        {
            // once we get too close the angle error becomes too big, so move the target pos further
            float distance = source.Distance(target);
            const float minDistance = 500f;
            if (distance < minDistance)
            {
                // move pip forward a bit
                target += (minDistance - distance)*source.DirectionToTarget(target);
            }
            
            // total error magnitude should adjust as target is faster or slower than light speed. 
            float errorMagnitude = (float)Math.Pow((distance + minDistance) / Ship.TargetErrorFocalPoint, 0.25f);

            // total error magnitude should get smaller as the target gets slower. 
            if (FireTarget?.Type == GameObjectType.ShipModule)
            {
                float speed = ((ShipModule)FireTarget).GetParent().CurrentVelocity;
                errorMagnitude *= speed < 150 ? speed / 150 : 1;
            }
            
            Vector2 adjusted = target + error*errorMagnitude;
            DebugLastImpactPredict = adjusted;
            return adjusted;
        }

        public float GetSpeedReduction(float targetSpeed)
        {
            return (targetSpeed + 25) / 175;
        }

        public Vector2 Origin => Module?.Center ?? PlanetOrigin;
        public Vector2 OwnerVelocity => Owner?.Velocity ?? Module?.GetParent()?.Velocity ?? Vector2.Zero;

        Vector2 Predict(Vector2 origin, GameplayObject target, bool advancedTargeting)
        {
            Vector2 pip = new ImpactPredictor(origin, OwnerVelocity, ProjectileSpeed, target)
                .Predict(advancedTargeting);

            float distance = origin.Distance(pip);
            float maxPredictionRange = BaseRange*2;
            if (distance > maxPredictionRange)
            {
                Vector2 predictionVector = target.Center.DirectionToTarget(pip);
                pip = target.Center + predictionVector*maxPredictionRange;
            }

            return pip;
        }

        public Vector2 ProjectedImpactPointNoError(GameplayObject target)
        {
            return Predict(Origin, target, advancedTargeting: true);
        }

        bool ProjectedImpactPoint(GameplayObject target, out Vector2 pip)
        {
            Vector2 origin = Origin;
            pip = Predict(origin, target, CanUseAdvancedTargeting);
            if (pip == Vector2.Zero)
                return false;
            pip = AdjustedImpactPoint(origin, pip, GetTargetError(target));
            return true;
        }
        
        // @note This is the main firing and targeting method
        // Prioritizes mainTarget
        // But if mainTarget is not viable, will pick a new Target using ships/projectiles lists
        // @return TRUE if target was fired at
        public bool UpdateAndFireAtTarget(Ship mainTarget,
            Array<Projectile> enemyProjectiles, Array<Ship> enemyShips)
        {
            TargetChangeTimer -= 0.0167f;
            if (!CanFireWeaponCooldown())
                return false; // we can't fire, so don't bother checking targets

            if (ShouldPickNewTarget())
            {
                TargetChangeTimer = 0.1f * Math.Max(Module.XSIZE, Module.YSIZE);
                // cumulative bonuses for turrets, PD and true PD weapons
                if (isTurret) TargetChangeTimer *= 0.5f;
                if (Tag_PD)   TargetChangeTimer *= 0.5f;
                if (TruePD)   TargetChangeTimer *= 0.25f;

                ClearFireTarget();

                if (!PickProjectileTarget(enemyProjectiles))
                {
                    PickShipTarget(mainTarget, enemyShips);
                }
            }

            if (FireTarget != null)
            {
                if (isBeam)
                    return FireBeam(Module.Center, FireTarget.Center, FireTarget);
                else
                    return FireAtTarget(FireTarget.Center, FireTarget);
            }
            return false; // no target to fire at
        }

        bool IsTargetAliveAndInRange(GameplayObject target)
        {
            return target != null && target.Active && target.Health > 0.0f &&
                   (!(target is Ship ship) || !ship.dying)
                   && Owner.IsTargetInFireArcRange(this, target);
        }

        // @return TRUE if firing logic should pick a new firing target
        bool ShouldPickNewTarget()
        {
            // Reasons for this weapon not to choose a new target
            return TargetChangeTimer <= 0f // ready to change targets
                && !IsRepairDrone // TODO: is this correct?
                && !isRepairBeam // TODO: repair beams are managed by repair drone ai?
                && !IsTargetAliveAndInRange(FireTarget); // Target is dead or out of range
        }

        public bool TargetValid(GameplayObject fireTarget)
        {
            if (fireTarget.Type == GameObjectType.ShipModule)
                return TargetValid(((ShipModule)fireTarget).GetParent().shipData.HullRole);
            if (fireTarget.Type == GameObjectType.Ship)
                return TargetValid(((Ship)fireTarget).shipData.HullRole);
            return true;
        }

        bool PickProjectileTarget(Array<Projectile> enemyProjectiles)
        {
            if (Tag_PD && enemyProjectiles.NotEmpty)
            {
                int maxTracking = 1 + Owner.TrackingPower + Owner.Level;
                for (int i = 0; i < maxTracking && i < enemyProjectiles.Count; i++)
                {
                    Projectile proj = enemyProjectiles[i];
                    if (proj.Active && proj.Health > 0f
                        && proj.Weapon.Tag_Intercept // projectile can be intercepted?
                        && Owner.IsTargetInFireArcRange(this, proj))
                    {
                        FireTarget = proj;
                        return true;
                    }
                }
            }
            return false;
        }

        void PickShipTarget(Ship mainTarget, Array<Ship> potentialTargets)
        {
            if (TruePD) // true PD weapons can't target ships
                return;

            Ship newTargetShip = null;

            // prefer our main target:
            if (IsTargetAliveAndInRange(mainTarget) && TargetValid(mainTarget))
            {
                newTargetShip = mainTarget;
            }
            else // otherwise track a new target:
            {
                // limit to one target per level.
                int maxTracking = 1 + Owner.TrackingPower + Owner.Level;
                for (int i = 0; i < potentialTargets.Count && i < maxTracking; ++i)
                {
                    Ship potentialTarget = potentialTargets[i];
                    if (TargetValid(potentialTarget) && IsTargetAliveAndInRange(potentialTarget))
                    {
                        newTargetShip = potentialTarget;
                        break;
                    }
                }
            }

            // now choose a random module to target
            if (newTargetShip != null)
            {
                FireTarget = newTargetShip.GetRandomInternalModule(this);
            }
        }

        public Vector2 ProjectedBeamPoint(Vector2 source, Vector2 destination, GameplayObject target = null)
        {
            if (DamageAmount < 1)
                return destination;

            if (target != null && !Owner.loyalty.IsEmpireAttackable(target.GetLoyalty()))
                return destination;

            if (Tag_Tractor || isRepairBeam)
                return destination;

            Vector2 beamDestination = AdjustedImpactPoint(source, destination, GetTargetError(target));
            return beamDestination;
        }

        bool FireBeam(Vector2 source, Vector2 destination, GameplayObject target = null)
        {
            destination = ProjectedBeamPoint(source, destination, target);
            if (!Owner.IsInsideFiringArc(this, destination))
                return false;

            PrepareToFire();
            var beam = new Beam(this, source, destination, target);
            return true;
        }

        public DroneBeam FireDroneBeam(DroneAI droneAI)
        {
            return new DroneBeam(droneAI);
        }

        public void FireTargetedBeam(GameplayObject target)
        {
            FireBeam(Module.Center, target.Center, target);
        }

        public bool ManualFireTowardsPos(Vector2 targetPos)
        {
            if (CanFireWeaponCooldown())
            {
                if (isBeam)
                {
                    return FireBeam(Module.Center, targetPos, null);
                }
                return FireAtTarget(targetPos, null);
            }
            return false;
        }

        public void FireFromPlanet(Planet planet, Ship targetShip)
        {
            targetShip.InCombatTimer = 15f;
            PlanetOrigin             = planet.Center.GenerateRandomPointInsideCircle(planet.ObjectRadius);
            GameplayObject target    = targetShip.GetRandomInternalModule(this) ?? (GameplayObject) targetShip;

            if (isBeam)
            {
                FireBeam(PlanetOrigin, target.Center, target);
                return;
            }

            if (ProjectedImpactPoint(target, out Vector2 pip))
            {
                Vector2 direction = (pip - Origin).Normalized();

                foreach (FireSource fireSource in EnumFireSources(PlanetOrigin, direction))
                    Projectile.Create(this, planet, fireSource.Direction, target);
            }
        }

        public void ApplyDamageModifiers(Projectile projectile)
        {
            if (Owner == null)
                return;

            if (Owner.loyalty.data.Traits.Pack)
                projectile.DamageAmount += projectile.DamageAmount * Owner.PackDamageModifier;

            float actualShieldPenChance = Module?.GetParent().loyalty.data.ShieldPenBonusChance * 100 ?? 0;
            for (int i = 0; i < ActiveWeaponTags.Length; ++i)
            {
                AddModifiers(ActiveWeaponTags[i], projectile, ref actualShieldPenChance);
            }

            projectile.IgnoresShields = RandomMath.RollDice(actualShieldPenChance);
        }

        void AddModifiers(WeaponTag tag, Projectile p, ref float actualShieldPenChance)
        {
            WeaponTagModifier weaponTag = Owner.loyalty.data.WeaponTags[tag];

            p.RotationRadsPerSecond += weaponTag.Turn * RotationRadsPerSecond;
            p.DamageAmount          += weaponTag.Damage * p.DamageAmount;
            p.Range                 += weaponTag.Range * BaseRange;
            p.Speed                 += weaponTag.Speed * ProjectileSpeed;
            p.Health                += weaponTag.HitPoints * HitPoints;
            p.DamageRadius          += weaponTag.ExplosionRadius * DamageRadius;
            p.ArmorPiercing         += (int)weaponTag.ArmourPenetration;
            p.ArmorDamageBonus      += weaponTag.ArmorDamage;
            p.ShieldDamageBonus     += weaponTag.ShieldDamage;
            
            float shieldPenChance  = weaponTag.ShieldPenetration * 100 + ShieldPenChance;
            actualShieldPenChance  = shieldPenChance.LowerBound(actualShieldPenChance);
        }

        public float GetActualRange(Empire owner = null)
        {
            owner = owner ?? Owner?.loyalty ?? Player;
            float range = BaseRange;
            for (int i = 0; i < ActiveWeaponTags.Length; ++i)
            {
                WeaponTagModifier mod = owner.data.WeaponTags[ ActiveWeaponTags[i] ];
                range += mod.Range * BaseRange;
            }
            return range;
        }

        public void ResetToggleSound()
        {
            if (ToggleCue.IsPlaying)
                ToggleCue.Stop();
        }

        public void Update(FixedSimTime timeStep)
        {
            if (CooldownTimer > 0f)
            {
                if (WeaponType != "Drone")
                    CooldownTimer = MathHelper.Max(CooldownTimer - timeStep.FixedTime, 0f);
            }

            if (SalvosToFire > 0)
            {
                SalvoFireTimer += timeStep.FixedTime;
                ContinueSalvo();
            }
            else
            {
                SalvoTarget = null;
            }
        }

        public float GetShieldDamageMod(ShipModule module)
        {
            float damageModifier = EffectVSShields;
            if      (Tag_Kinetic) damageModifier *= (1f - module.shield_kinetic_resist);
            else if (Tag_Energy)  damageModifier *= (1f - module.shield_energy_resist);
            else if (Tag_Beam)    damageModifier *= (1f - module.shield_beam_resist);
            else if (Tag_Missile) damageModifier *= (1f - module.shield_missile_resist);
            else if (Tag_Hybrid)  damageModifier *= (1f - module.shield_hybrid_resist);
            return damageModifier;
        }

        public float GetArmorDamageMod(ShipModule module)
        {
            float damageModifier = 1f;
            if (module.Is(ShipModuleType.Armor)) damageModifier *= EffectVsArmor;
            if (Tag_Explosive)                   damageModifier *= (1f - module.ExplosiveResist);
            if (Tag_Hybrid)                      damageModifier *= (1f - module.HybridResist);
            if (Tag_Kinetic)                     damageModifier *= (1f - module.KineticResist);
            else if (Tag_Beam)                   damageModifier *= (1f - module.BeamResist);
            else if (Tag_Energy)                 damageModifier *= (1f - module.EnergyResist);
            else if (Tag_Missile)                damageModifier *= (1f - module.MissileResist);
            else if (Tag_Torpedo)                damageModifier *= (1f - module.TorpedoResist);
            return damageModifier;
        }

        public bool TargetValid(ShipData.RoleName role)
        {
            switch (role)
            {
                case ShipData.RoleName.fighter    when Excludes_Fighters:
                case ShipData.RoleName.scout      when Excludes_Fighters:
                case ShipData.RoleName.drone      when Excludes_Fighters:
                case ShipData.RoleName.corvette   when Excludes_Corvettes:
                case ShipData.RoleName.gunboat    when Excludes_Corvettes:
                case ShipData.RoleName.frigate    when Excludes_Capitals:
                case ShipData.RoleName.destroyer  when Excludes_Capitals:
                case ShipData.RoleName.cruiser    when Excludes_Capitals:
                case ShipData.RoleName.battleship when Excludes_Capitals:
                case ShipData.RoleName.capital    when Excludes_Capitals:
                case ShipData.RoleName.platform   when Excludes_Stations:
                case ShipData.RoleName.station    when Excludes_Stations: return false;
                default: return true;
            }
        }

        public float CalculateOffense(ShipModule m = null)
        {
            float off = 0f;
            if (isBeam)
            {
                off += DamageAmount * 60 * BeamDuration * (1f / NetFireDelay);
                off += MassDamage * 30 * (1f / NetFireDelay);
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
            off *= EffectVSShields > 1 ? 1f + (EffectVSShields - 1f) / 2f : 1f;
            off *= EffectVSShields < 1 ? 1f - (1f - EffectVSShields) / 2f : 1f;

            off *= TruePD ? 0.2f : 1f;
            off *= Tag_Intercept && (Tag_Missile || Tag_Torpedo) ? 0.8f : 1f;
            off *= ProjectileSpeed > 1 ? ProjectileSpeed / BaseRange : 1f;

            // FB: Missiles which can be intercepted might get str modifiers
            off *= Tag_Intercept && RotationRadsPerSecond > 1 ? 1 + HitPoints / 50 / ProjectileRadius.LowerBound(2) : 1;

            // FB: offense calcs for damage radius
            off *= DamageRadius > 32 && !TruePD ? DamageRadius / 32 : 1f;

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
            if (Excludes_Fighters)  exclusionMultiplier -= 0.15f;
            if (Excludes_Corvettes) exclusionMultiplier -= 0.15f;
            if (Excludes_Capitals)  exclusionMultiplier -= 0.45f;
            if (Excludes_Stations)  exclusionMultiplier -= 0.25f;
            off *= exclusionMultiplier;

            // Imprecision gets worse when range gets higher
            off *= !Tag_Guided ? (1 - FireImprecisionAngle*0.01f * (BaseRange/2000)).LowerBound(0.1f) : 1f;
            
            // Multiple warheads
            if (MirvWarheads > 0 && MirvWeapon.NotEmpty())
            {
                off             *= 0.25f; // Warheads mostly do the damage
                Weapon warhead   = ResourceManager.CreateWeapon(MirvWeapon);
                float warheadOff = warhead.CalculateOffense() * MirvWarheads;
                off             += warheadOff;
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

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        ~Weapon() { Destroy(); }

        void Destroy()
        {
            ToggleCue.Destroy();
            Owner         = null;
            Module        = null;
            FireTarget    = null;
            SalvoTarget   = null;
        }

        public override string ToString() => $"Weapon {WeaponType} {WeaponEffectType} {Name}";
    }
}
