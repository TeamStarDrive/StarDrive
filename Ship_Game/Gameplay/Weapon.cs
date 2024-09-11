using Ship_Game.AI;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.ExtensionMethods;
using Vector2 = SDGraphics.Vector2;
using Ship_Game.Utils;
using Ship_Game.Universe;
using Ship_Game.Universe.SolarBodies;

namespace Ship_Game.Gameplay
{
    // WeaponState
    // Holds required state of a Weapon used in ShipModules or planetary Buildings
    public class Weapon : WeaponTemplateWrapper, IDisposable, IDamageModifier
    {
        public Ship Owner { get; set; }
        public UniverseState Universe { get; private set; }
        AudioHandle ToggleCue = new();
        // Separate because Weapons attached to Planetary Buildings, don't have a ShipModule Center
        public Vector2 PlanetOrigin;
        public ShipModule Module;
        public float CooldownTimer;
        public GameObject FireTarget { get; private set; }
        float TargetChangeTimer;

        // these are set during install to ShipModule
        public bool IsTurret;
        public bool IsMainGun;

        // Currently pending salvos to be fired
        public int SalvosToFire { get; private set; }
        float SalvoDirection;
        float SalvoFireTimer; // while SalvosToFire > 0, use this timer to count when to fire next shot
        GameObject SalvoTarget;

        public new float FireDelay { get; set; }

        public RandomBase Random => Owner?.Loyalty.Random ?? Universe?.Random;

        public Weapon(UniverseState us, IWeaponTemplate t, Ship owner, ShipModule m, ShipHull hull) : base(t)
        {
            Universe = us;
            Owner = owner;
            Module = m;
            if (m != null)
            {
                IsTurret  = m.ModuleType == ShipModuleType.Turret;
                IsMainGun = m.ModuleType == ShipModuleType.MainGun;
            }
            FireDelay = base.FireDelay;

            if (hull != null)
            {
                FireDelay = (base.FireDelay * (1f - hull.Bonuses.FireRateBonus));
            }
        }

        public void PlayToggleAndFireSfx(Audio.AudioEmitter emitter = null)
        {
            if (ToggleCue.IsPlaying)
                return;

            Audio.AudioEmitter soundEmitter = emitter ?? Owner?.SoundEmitter;
            GameAudio.PlaySfxAsync(FireCueName, soundEmitter);
            ToggleCue.PlaySfxAsync(ToggleSoundName, soundEmitter);
        }

        public void FireDrone(Vector2 direction)
        {
            if (CanFireWeaponCooldown())
            {
                PrepareToFire();
                Projectile.Create(this, Owner, Module.Position, direction, null, playSound: true);
            }
        }

        Vector2 ApplyFireImprecisionAngle(Vector2 direction)
        {
            if (FireImprecisionAngle <= 0)
                return direction;
            float spread = Random.Float(-FireImprecisionAngle, FireImprecisionAngle) * 0.5f;
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

        public void SpawnMirvSalvo(Vector2 direction, GameObject target, Vector2 origin)
        {
            SpawnSalvo(direction, target, origin, playSound: true);
        }

        void SpawnSalvo(Vector2 direction, GameObject target, Vector2 origin, bool playSound)
        {
            foreach (FireSource fireSource in EnumFireSources(origin, direction))
            {
                Projectile.Create(this, Owner, fireSource.Origin, fireSource.Direction, target, playSound);
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
            CooldownTimer = NetFireDelay + Random.Float(-10f, +10f) * 0.008f;

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
                if (PrepareFirePos(SalvoTarget, SalvoTarget.Position, out Vector2 firePos))
                    SalvoDirection = (firePos - Module.Position).ToRadians() - Owner.Rotation;
            }

            bool playSound = salvoIndex % SalvoSoundInterval == 0;
            SpawnSalvo((SalvoDirection + Owner.Rotation).RadiansToDirection(), SalvoTarget, Origin, playSound);
        }

        bool PrepareFirePos(GameObject target, Vector2 targetPos, out Vector2 firePos)
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

        bool FireAtTarget(Vector2 targetPos, GameObject target = null)
        {
            if (!PrepareFirePos(target, targetPos, out Vector2 firePos))
                return false;

            PrepareToFire();
            Vector2 direction = Origin.DirectionToTarget(firePos);
            SpawnSalvo(direction, target, Origin, playSound: true);

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
        public bool CanUseAdvancedTargeting
        {
            get
            {
                ShipModule m = Module;
                Ship o = Owner;
                return (m != null && m.AccuracyPercent > 0.66f) ||
                       (o != null && o.CanUseAdvancedTargeting);
            }
        }

        public float BaseTargetError(float level, float range = 0, Empire loyalty = null)
        {
            if (Module == null || Tag_Bomb)
                return 0;

            // base error is based on module size or accuracyPercent.
            float baseError;
            if (Module.AccuracyPercent >= 0 || !TruePD)
                baseError = Module.WeaponInaccuracyBase;
            else
                baseError = 0;

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
            if (Tag_Kinetic) adjust *= (1f - (Owner?.Loyalty?.data.OrdnanceEffectivenessBonus ?? 0));
            if (IsTurret) adjust    *= 0.5f;
            if (Tag_PD) adjust      *= 0.5f;
            if (TruePD) adjust      *= 0.5f;
            return adjust * (1f - (Owner?.Loyalty?.data.Traits.TargetingModifier ?? 0));
        }

        // @note This is used for debugging
        [XmlIgnore]
        public Vector2 DebugLastImpactPredict { get; private set; }

        public Vector2 GetTargetError(RandomBase random, GameObject target, int level = -1)
        {
            // base error from crew level/targeting bonuses
            float adjust = BaseTargetError(level);
            Vector2 error = random.Vector2D(adjust);
            if (target != null)
            {
                error += target.JammingError(); // if target has ECM, they can scramble their position
                error *= target.DodgeMultiplier();
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
                Ship parent = ((ShipModule)FireTarget).GetParent();
                if (parent != null) // the parent can be null
                {
                    float speed = parent.CurrentVelocity;
                    errorMagnitude *= speed < 150 ? speed / 150 : 1;
                }
            }
            
            Vector2 adjusted = target + error*errorMagnitude;
            DebugLastImpactPredict = adjusted;
            return adjusted;
        }

        public Vector2 Origin => Module?.Position ?? PlanetOrigin;
        public Vector2 OwnerVelocity => Owner?.Velocity ?? Module?.GetParent()?.Velocity ?? Vector2.Zero;

        Vector2 Predict(Vector2 origin, GameObject target, bool advancedTargeting)
        {
            Vector2 pip = new ImpactPredictor(origin, OwnerVelocity, ProjectileSpeed, target)
                .Predict(advancedTargeting);

            float distance = origin.Distance(pip);
            float maxPredictionRange = BaseRange*2;
            if (distance > maxPredictionRange)
            {
                Vector2 predictionVector = target.Position.DirectionToTarget(pip);
                pip = target.Position + predictionVector*maxPredictionRange;
            }

            return pip;
        }

        public Vector2 ProjectedImpactPointNoError(GameObject target)
        {
            return Predict(Origin, target, advancedTargeting: true);
        }

        bool ProjectedImpactPoint(GameObject target, out Vector2 pip)
        {
            Vector2 origin = Origin;
            pip = Predict(origin, target, CanUseAdvancedTargeting);
            if (pip == Vector2.Zero)
                return false;

            Vector2 error = GetTargetError(Random, target);
            pip = AdjustedImpactPoint(origin, pip, error);
            return true;
        }
        
        // @note This is the main firing and targeting method
        // Prioritizes mainTarget
        // But if mainTarget is not viable, will pick a new Target using ships/projectiles lists
        // @return TRUE if target was fired at
        public bool UpdateAndFireAtTarget(Ship mainTarget,
            Projectile[] enemyProjectiles, Ship[] enemyShips)
        {
            TargetChangeTimer -= 0.0167f;
            if (!CanFireWeaponCooldown())
                return false; // we can't fire, so don't bother checking targets

            if (ShouldPickNewTarget())
            {
                TargetChangeTimer = 0.1f * Math.Max(Module.XSize, Module.YSize);
                // cumulative bonuses for turrets, PD and true PD weapons
                if (IsTurret) TargetChangeTimer *= 0.5f;
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
                if (IsBeam)
                    return FireBeam(Module.Position, FireTarget.Position, FireTarget);
                else
                    return FireAtTarget(FireTarget.Position, FireTarget);
            }
            return false; // no target to fire at
        }

        bool IsTargetAliveAndInRange(GameObject target)
        {
            return target != null && target.Active && target.Health > 0.0f &&
                   (!(target is Ship ship) || !ship.Dying)
                   && Owner.IsTargetInFireArcRange(this, target);
        }

        // @return TRUE if firing logic should pick a new firing target
        bool ShouldPickNewTarget()
        {
            // Reasons for this weapon not to choose a new target
            return TargetChangeTimer <= 0f // ready to change targets
                && !IsRepairDrone // TODO: is this correct?
                && !IsRepairBeam // TODO: repair beams are managed by repair drone ai?
                && !IsTargetAliveAndInRange(FireTarget); // Target is dead or out of range
        }

        public bool TargetValid(GameObject fireTarget)
        {
            if (fireTarget.Type == GameObjectType.ShipModule)
                return ShipTargetValid(((ShipModule)fireTarget).GetParent());
            if (fireTarget.Type == GameObjectType.Ship)
                return ShipTargetValid((Ship)fireTarget);
            return true;
        }

        bool PickProjectileTarget(Projectile[] enemyProjectiles)
        {
            if (Tag_PD && enemyProjectiles.Length != 0)
            {
                int maxTracking = 1 + Owner.TrackingPower + Owner.Level;
                for (int i = 0; i < maxTracking && i < enemyProjectiles.Length; i++)
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

        void PickShipTarget(Ship mainTarget, Ship[] potentialTargets)
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
                for (int i = 0; i < potentialTargets.Length && i < maxTracking; ++i)
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

        public Vector2 ProjectedBeamPoint(Vector2 source, Vector2 destination, GameObject target = null)
        {
            if (DamageAmount < 1)
                return destination;

            if (target != null && !Owner.Loyalty.IsEmpireAttackable(target.GetLoyalty()))
                return destination;

            if (IsRepairBeam)
                return destination;

            Vector2 error = GetTargetError(Random, target);
            Vector2 beamDestination = AdjustedImpactPoint(source, destination, error);
            return beamDestination;
        }

        bool FireBeam(Vector2 source, Vector2 destination, GameObject target = null)
        {
            destination = ProjectedBeamPoint(source, destination, target);
            if (!Owner.IsInsideFiringArc(this, destination))
                return false;

            PrepareToFire();

            // NOTE: beam is automatically added to this.Owner.Universe
            var _ = new Beam(Owner.Universe.CreateId(), this, source, destination, target);
            return true;
        }

        public DroneBeam FireDroneBeam(DroneAI droneAI)
        {
            return new DroneBeam(Owner.Universe.CreateId(), droneAI);
        }

        public void FireTargetedBeam(GameObject target)
        {
            FireBeam(Module.Position, target.Position, target);
        }

        public bool ManualFireTowardsPos(Vector2 targetPos)
        {
            if (CanFireWeaponCooldown())
            {
                if (IsBeam)
                {
                    return FireBeam(Module.Position, targetPos);
                }
                return FireAtTarget(targetPos);
            }
            return false;
        }

        public void FireFromPlanet(Planet planet, Ship targetShip, int randomRadiusFromTarget = 0)
        {
            PlanetOrigin = planet.Position.GenerateRandomPointInsideCircle(planet.Radius * 0.66f, planet.Random);
            GameObject target = targetShip.GetRandomInternalModule(this) ?? (GameObject) targetShip;

            if (IsBeam)
            {
                FireBeam(PlanetOrigin, target.Position, target);
                return;
            }

            Vector2 pip;
            if (randomRadiusFromTarget != 0)
            {
                pip = target.Position.GenerateRandomPointInsideCircle(randomRadiusFromTarget, planet.Random);
                CreateProjectile(pip);
            }
            else if (ProjectedImpactPoint(target, out pip))
            {
                CreateProjectile(pip);
            }

            void CreateProjectile(Vector2 pip)
            {
                Vector2 direction = (pip - Origin).Normalized();
                foreach (FireSource fireSource in EnumFireSources(PlanetOrigin, direction))
                    Projectile.Create(this, planet, planet.Owner, fireSource.Direction, target, IsSwarmSat: randomRadiusFromTarget != 0);
            }
        }

        public void ApplyDamageModifiers(Projectile projectile)
        {
            if (Owner == null)
                return;

            if (Owner.Loyalty.HavePackMentality)
                projectile.DamageAmount += projectile.DamageAmount * Owner.PackDamageModifier;

            float actualShieldPenChance = Module?.GetParent().Loyalty.data.ShieldPenBonusChance * 100 ?? 0;
            for (int i = 0; i < ActiveWeaponTags.Length; ++i)
            {
                AddModifiers(ActiveWeaponTags[i], projectile, ref actualShieldPenChance);
            }

            projectile.IgnoresShields = Random.RollDice(actualShieldPenChance);
        }

        void AddModifiers(WeaponTag tag, Projectile p, ref float actualShieldPenChance)
        {
            WeaponTagModifier weaponTag = Owner.Loyalty.data.WeaponTags[tag];

            p.RotationRadsPerSecond += weaponTag.Turn * RotationRadsPerSecond;
            p.DamageAmount          += weaponTag.Damage * p.DamageAmount;
            p.Range                 += weaponTag.Range * BaseRange;
            p.Speed                 += weaponTag.Speed * ProjectileSpeed;
            p.Health                += weaponTag.HitPoints * HitPoints;
            p.DamageRadius          += weaponTag.ExplosionRadius * ExplosionRadius;
            p.ArmorPiercing         += (int)weaponTag.ArmourPenetration;
            p.ArmorDamageBonus      += weaponTag.ArmorDamage;
            p.ShieldDamageBonus     += weaponTag.ShieldDamage;
            
            float shieldPenChance  = weaponTag.ShieldPenetration * 100 + ShieldPenChance;
            actualShieldPenChance  = shieldPenChance.LowerBound(actualShieldPenChance);
        }

        public void ResetToggleSound()
        {
            if (ToggleCue?.IsPlaying == true)
                ToggleCue.Stop();
        }

        public void Update(FixedSimTime timeStep)
        {
            if (CooldownTimer > 0f)
            {
                if (WeaponType != "Drone")
                    CooldownTimer = Math.Max(CooldownTimer - timeStep.FixedTime, 0f);
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
            float damageModifier = EffectVsShields;
            if (Explodes) // Exploding projectiles supercede any other resistances 
            {
                damageModifier *= 1f - module.ShieldExplosiveResist;
            }
            else
            {
                if (Tag_Plasma)  damageModifier *= 1f - module.ShieldPlasmaResist;
                if (Tag_Kinetic) damageModifier *= 1f - module.ShieldKineticResist;
                if (Tag_Beam)    damageModifier *= 1f - module.ShieldBeamResist;
                if (Tag_Energy)  damageModifier *= 1f - module.ShieldEnergyResist;
            }
            
            return damageModifier;
        }

        public float GetArmorDamageMod(ShipModule module)
        {
            float damageModifier = 1f;
            if (module.Is(ShipModuleType.Armor)) 
                damageModifier *= EffectVsArmor;

            if (Explodes) // Exploding projectiles supercede any other resistances        
            {
                damageModifier *= 1f - module.ExplosiveResist;
            }
            else 
            {
                if (Tag_Plasma)  damageModifier *= 1f - module.PlasmaResist;
                if (Tag_Kinetic) damageModifier *= 1f - module.KineticResist;
                if (Tag_Beam)    damageModifier *= 1f - module.BeamResist;
                if (Tag_Energy)  damageModifier *= 1f - module.EnergyResist;
            }

            return damageModifier;
        }

        public bool ShipTargetValid(Ship ship)
        {
            if (ship.TroopsAreBoardingShip)
                return false;

            switch (ship.ShipData.HullRole)
            {
                case RoleName.fighter    when ExcludesFighters:
                case RoleName.scout      when ExcludesFighters:
                case RoleName.drone      when ExcludesFighters:
                case RoleName.corvette   when ExcludesCorvettes:
                case RoleName.gunboat    when ExcludesCorvettes:
                case RoleName.frigate    when ExcludesCapitals:
                case RoleName.destroyer  when ExcludesCapitals:
                case RoleName.cruiser    when ExcludesCapitals:
                case RoleName.battleship when ExcludesCapitals:
                case RoleName.capital    when ExcludesCapitals:
                case RoleName.platform   when ExcludesStations:
                case RoleName.station    when ExcludesStations: return false;
                default: return true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Weapon() { Dispose(false); }

        protected virtual void Dispose(bool disposing)
        {
            Mem.Dispose(ref ToggleCue);
            Owner = null;
            Module = null;
            FireTarget = null;
            SalvoTarget = null;
        }

        public override string ToString() => $"Weapon Type={WeaponType} UID={UID} Name={Name}";
    }
}
