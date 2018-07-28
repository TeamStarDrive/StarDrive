using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Gameplay
{
    [Flags]
    public enum WeaponTag : int
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
        Tractor   = (1 << 20),
    }

    public sealed class Weapon : IDisposable, IDamageModifier
    {
        private WeaponTag TagBits;
        public bool this[WeaponTag tag]
        {
            get => (TagBits & tag) != 0;
            set => TagBits = value ? TagBits|tag : TagBits & ~tag;
        }
        private static readonly char[] TagBitsSeparator = {',', ' '};
        public string[] GetActiveTagIds()
        {
            string[] ids = TagBits.ToString().Split(TagBitsSeparator, StringSplitOptions.RemoveEmptyEntries);
            return ids;
        }
        public bool Tag_Kinetic   { get => this[WeaponTag.Kinetic];   set => this[WeaponTag.Kinetic]   = value; }
        public bool Tag_Energy    { get => this[WeaponTag.Energy];    set => this[WeaponTag.Energy]    = value; }
        public bool Tag_Guided    { get => this[WeaponTag.Guided];    set => this[WeaponTag.Guided]    = value; }
        public bool Tag_Missile   { get => this[WeaponTag.Missile];   set => this[WeaponTag.Missile]   = value; }
        public bool Tag_Hybrid    { get => this[WeaponTag.Hybrid];    set => this[WeaponTag.Hybrid]    = value; }
        public bool Tag_Beam      { get => this[WeaponTag.Beam];      set => this[WeaponTag.Beam]      = value; }
        public bool Tag_Explosive { get => this[WeaponTag.Explosive]; set => this[WeaponTag.Explosive] = value; }
        public bool Tag_Intercept { get => this[WeaponTag.Intercept]; set => this[WeaponTag.Intercept] = value; }
        public bool Tag_Railgun   { get => this[WeaponTag.Railgun];   set => this[WeaponTag.Railgun]   = value; }
        public bool Tag_Bomb      { get => this[WeaponTag.Bomb];      set => this[WeaponTag.Bomb]      = value; }
        public bool Tag_SpaceBomb { get => this[WeaponTag.SpaceBomb]; set => this[WeaponTag.SpaceBomb] = value; }
        public bool Tag_BioWeapon { get => this[WeaponTag.BioWeapon]; set => this[WeaponTag.BioWeapon] = value; }
        public bool Tag_Drone     { get => this[WeaponTag.Drone];     set => this[WeaponTag.Drone]     = value; }
        public bool Tag_Warp      { get => this[WeaponTag.Warp];      set => this[WeaponTag.Warp]      = value; }
        public bool Tag_Torpedo   { get => this[WeaponTag.Torpedo];   set => this[WeaponTag.Torpedo]   = value; }
        public bool Tag_Cannon    { get => this[WeaponTag.Cannon];    set => this[WeaponTag.Cannon]    = value; }
        public bool Tag_Subspace  { get => this[WeaponTag.Subspace];  set => this[WeaponTag.Subspace]  = value; }
        public bool Tag_PD        { get => this[WeaponTag.PD];        set => this[WeaponTag.PD]        = value; }
        public bool Tag_Flak      { get => this[WeaponTag.Flak];      set => this[WeaponTag.Flak]      = value; }
        public bool Tag_Array     { get => this[WeaponTag.Array];     set => this[WeaponTag.Array]     = value; }
        public bool Tag_Tractor   { get => this[WeaponTag.Tractor];   set => this[WeaponTag.Tractor]   = value; }

        [XmlIgnore][JsonIgnore]
        public Ship Owner { get; set; }
        [XmlIgnore][JsonIgnore]
        public GameplayObject drowner; // drone owner
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
        public string ExpColor = null;
        public string dieCue;
        public string ToggleSoundName = "";
        [XmlIgnore][JsonIgnore]
        private AudioHandle ToggleCue;
        public string Light;
        public bool isTurret;
        public bool isMainGun;
        public float OrdinanceRequiredToFire;
        public Vector2 Center;
        public float Range;
        public float DamageAmount;        
        public float ProjectileSpeed;
        public int ProjectileCount = 1;
        public int FireArc;
        public int FireCone;
        public string ProjectileTexturePath;
        public string ModelPath;
        public string WeaponType;
        public string WeaponEffectType;
        public string UID;
        [XmlIgnore][JsonIgnore]
        public ShipModule Module;

        [XmlIgnore][JsonIgnore]
        public float CooldownTimer;
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
        public float TerminalPhaseSpeedMod;
        public float ArmourPen = 0f;
        public string SecondaryFire;
        public bool AltFireMode;
        public bool AltFireTriggerFighter;
        private Weapon AltFireWeapon;
        public float OffPowerMod = 1f;        
        public bool RangeVariance;
        public float ExplosionRadiusVisual = 4.5f;
        [XmlIgnore][JsonIgnore]
        public GameplayObject FireTarget { get; private set; }
        private float TargetChangeTimer;
        public bool UseVisibleMesh;
        public bool PlaySoundOncePerSalvo;
        public int SalvoCount = 1;
        public float SalvoTimer;
        [XmlIgnore][JsonIgnore]
        private int SalvosToFire;
        private float SalvoDirection;
        private float SalvoFireTimer; // while SalvosToFire, use this timer to count when to fire next shot
        private GameplayObject SalvoTarget;
        public string ExplosionPath = "";
        public string ExplosionAnimation = "";
        public float ECM = 0;
        // When ships are off-screen, we do cheap and dirty invisible damage calculation
        [XmlIgnore][JsonIgnore]
        public float InvisibleDamageAmount
        {
            get
            {
                float damage = DamageAmount;
                return damage + damage*SalvoCount + damage*(isBeam ? 90f : 0f);
            }
        }
        public void InitializeTemplate()
        {
            BeamDuration = BeamDuration > 0 ? BeamDuration : 2f;
            fireDelay = Math.Max(0.016f, fireDelay);
            SalvoTimer = Math.Max(0, SalvoTimer);
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
            Weapon wep = (Weapon)MemberwiseClone();
            wep.SalvoTarget      = null;
            wep.FireTarget       = null;
            wep.Module           = null;
            wep.Owner            = null;
            wep.drowner          = null;
            return wep;
        }

        [XmlIgnore][JsonIgnore]
        public float NetFireDelay
        {
            get
            {
                return fireDelay + SalvoTimer;
            }
        }

        [XmlIgnore][JsonIgnore]
        public float OrdnanceUsagePerSecond
        {
            get
            { 
                return OrdinanceRequiredToFire * ProjectileCount * SalvoCount / NetFireDelay;
            }
        }

        [XmlIgnore][JsonIgnore]
        public float PowerFireUsagePerSecond // only usage during fire, not power maintenance 
        {
            get
            {
                return (BeamPowerCostPerSecond * BeamDuration + PowerRequiredToFire * ProjectileCount * SalvoCount) / NetFireDelay;
            }
        }

        // modify damageamount utilizing tech bonus. Currently this is only ordnance bonus.
        public float GetDamageWithBonuses(Ship owner)
        {
            float damageAmount = DamageAmount;
            if (owner?.loyalty?.data != null && OrdinanceRequiredToFire > 0)
                damageAmount += damageAmount * owner.loyalty.data.OrdnanceEffectivenessBonus;

            if (owner?.Level > 0)
                damageAmount += damageAmount * owner.Level * 0.05f;

            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.useHullBonuses) // Hull bonus damage increase
                if (ResourceManager.HullBonuses.TryGetValue(owner.shipData.Hull, out HullBonus mod))
                    damageAmount += damageAmount * mod.DamageBonus;

            return damageAmount;            
        }

 
        public void PlayToggleAndFireSfx(AudioEmitter emitter = null)
        {
            if (ToggleCue.IsPlaying)
                return;

            AudioEmitter soundEmitter = Owner?.PlayerShip == true ? null : (emitter ?? Owner?.SoundEmitter);

            GameAudio.PlaySfxAsync(fireCueName, soundEmitter);
            ToggleCue.PlaySfxAsync(ToggleSoundName, soundEmitter);
        }

        public void FireDrone(Vector2 direction)
        {
            if (PrepareToFire())
                Projectile.Create(this, Module.Center, direction, null, playSound: true);
        }

        public Vector2 GetFireConeSpread(Vector2 direction)
        {
            if (FireCone <= 0)
                return direction;
            float spread = RandomMath2.RandomBetween(-FireCone, FireCone) * 0.5f;
            return (direction.ToDegrees() + spread).AngleToDirection();
        }

        private struct FireSource
        {
            public readonly Vector2 Origin;
            public readonly Vector2 Direction;
            public FireSource(Vector2 origin, Vector2 direction)
            {
                Origin    = origin;
                Direction = direction;
            }
        }

        private IEnumerable<FireSource> EnumFireSources(Vector2 origin, Vector2 direction)
        {
            if (FireArc != 0)
            {
                float degreesBetweenShots = FireArc / (float)ProjectileCount;
                float angleToTarget = direction.ToDegrees() - FireArc * 0.5f;
                for (int i = 0; i < ProjectileCount; ++i)
                {
                    Vector2 dir = angleToTarget.AngleToDirection();
                    angleToTarget += degreesBetweenShots;
                    yield return new FireSource(origin, GetFireConeSpread(dir));
                }
            }
            else
            {
                for (int i = 0; i < ProjectileCount; ++i)
                {
                    yield return new FireSource(origin, GetFireConeSpread(direction));
                }
            }
        }

        private void SpawnSalvo(Vector2 direction, GameplayObject target)
        {
            bool secondary = SecondaryFire != null && AltFireTriggerFighter && AltFireMode 
                && target is ShipModule shipModule && shipModule.GetParent().shipData.Role == ShipData.RoleName.fighter;

            if (secondary && AltFireWeapon == null)
                AltFireWeapon = ResourceManager.CreateWeapon(SecondaryFire);

            Weapon  weapon = secondary ? AltFireWeapon : this;
            Vector2 origin = Module.Center;
            bool playSound = true;

            foreach (FireSource fireSource in EnumFireSources(origin, direction))
            {
                Projectile.Create(weapon, fireSource.Origin, fireSource.Direction, target, playSound);
                if (PlaySoundOncePerSalvo) playSound = false;
            }
        }

        private bool CanFireWeapon()
        {
            return Module.Active
                && Owner.engineState  != Ship.MoveState.Warp
                && Owner.PowerCurrent >= PowerRequiredToFire
                && Owner.Ordinance    >= OrdinanceRequiredToFire;
        }

        private bool PrepareToFire()
        {
            if (CooldownTimer > 0f || !CanFireWeapon())
                return false; 

            // cooldown should start after all salvos have finished, so
            // increase the cooldown by SalvoTimer
            CooldownTimer = NetFireDelay + RandomMath.RandomBetween(-10f, +10f) * 0.008f;

            Owner.InCombatTimer = 15f;
            Owner.Ordinance    -= OrdinanceRequiredToFire;
            Owner.PowerCurrent -= PowerRequiredToFire;
            return true;
        }

        private bool PrepareToFireSalvo()
        {
            float timeBetweenShots = SalvoTimer / SalvoCount;
            if (SalvoFireTimer < timeBetweenShots || !CanFireWeapon())
                return false; // not ready to fire salvo

            SalvoFireTimer -= timeBetweenShots;
            --SalvosToFire;

            Owner.InCombatTimer = 15f;
            Owner.Ordinance    -= OrdinanceRequiredToFire;
            Owner.PowerCurrent -= PowerRequiredToFire;
            return true;
        }

        private void ContinueSalvo()
        {
            if (SalvoTarget != null && !Owner.CheckRangeToTarget(this, SalvoTarget))
                return;
            if (!PrepareToFireSalvo())
                return;
            
            if (SalvoTarget != null) // check for new direction
            {
                // update direction only if we have a new valid pip
                if (PrepareFirePos(SalvoTarget, SalvoTarget.Center, out Vector2 firePos))
                    SalvoDirection = (firePos - Module.Center).ToRadians() - Owner.Rotation;
            }
            
            SpawnSalvo((SalvoDirection + Owner.Rotation).RadiansToDirection(), SalvoTarget);
        }

        private bool PrepareFirePos(GameplayObject target, Vector2 targetPos, out Vector2 firePos)
        {
            if (target != null)
            {
                if (ProjectedImpactPoint(target, out Vector2 pip) && CheckFireArc(pip))
                {
                    firePos = pip;
                    return PrepareToFire();
                }
            }
            if (CheckFireArc(targetPos))
            {
                firePos = targetPos;
                return PrepareToFire();
            }
            firePos = targetPos;
            return false;
        }

        private void FireAtTarget(Vector2 targetPos, GameplayObject target = null)
        {
            if (!PrepareFirePos(target, targetPos, out Vector2 firePos))
                return;

            Vector2 direction = (firePos - Module.Center).Normalized();

            SpawnSalvo(direction, target);

            if (SalvoCount > 1)  // queue the rest of the salvo to follow later
            {
                SalvosToFire   = SalvoCount - 1;
                SalvoDirection = direction.ToRadians() - Owner.Rotation; //keep direction relative to source
                SalvoFireTimer = 0f;
                SalvoTarget    = target;
            }
        }

        public void ClearFireTarget()
        {
            FireTarget  = null;
            SalvoTarget = null;
        }

        public Vector2 AdjustTargetting(int level = -1)
        {
            if (Module == null || Module.AccuracyPercent > 0.9999f || TruePD) 
                return Vector2.Zero; //|| Tag_PD 

            //calaculate level. 
            int trackingPower = (int)(Owner?.TrackingPower ?? 1);                        
            if(level == -1)
                level = (Owner?.Level ?? level) 
                    + trackingPower //(Owner?.TrackingPower  ?? 0)
                    + (Owner?.loyalty?.data.Traits.Militaristic ?? 0);
            
            //reduce jitter by level cubed. if jitter is less than a module radius stop.
            float baseJitter = 178f + 8 * Module.XSIZE * Module.YSIZE; 
            float adjust     = Math.Max(0, baseJitter - level * level * level);
            if (adjust < 8) return Vector2.Zero;


            //reduce or increase jitter based on weapon and trait characteristics. 
            
            if (isBeam)      adjust *= (1f - (Owner?.loyalty?.data.Traits.EnergyDamageMod ?? 0));    
            if (Tag_Kinetic) adjust *= (1f - (Owner?.loyalty?.data.OrdnanceEffectivenessBonus ?? 0));

            if (Owner?.loyalty?.data.Traits.Blind > 0) adjust *= 2f;
            adjust *= CalculateBaseAccuracy();
            
            return RandomMath2.Vector2D(adjust);
        }

        private float CalculateBaseAccuracy()
        {
             float adjust =(Module?.AccuracyPercent ?? 0);
            if (adjust == -1)
            {
                adjust = 1;
                if (isTurret) adjust *= .5f;
                if (Tag_PD) adjust   *= .5f;
                if (TruePD) adjust   *= .5f;
                //if (isBeam) adjust *= 2f;
            }
            else
                adjust = 1 - adjust;
            return adjust;
        }
        public Vector2 SetDestination(Vector2 target, Vector2 source, float range )
        {            
            Vector2 deltaVec = target - source;            
            return source + deltaVec.Normalized() * range;
        }
        public bool ProjectedImpactPoint(GameplayObject target, out Vector2 pip)
        {
            Vector2 weaponOrigin = Module?.Center ?? Center;
            Vector2 ownerVel     = Owner?.Velocity ?? Vector2.Zero;
            Vector2 ownerCenter  = Owner?.Center ?? Vector2.Zero;
            Vector2 jitter = target.JitterPosition() + AdjustTargetting();

            pip = new ImpactPredictor(weaponOrigin, ownerVel, ProjectileSpeed, target).Predict();
            Vector2 jitteredtarget = SetDestination(pip, ownerCenter, 4000) + jitter;
            pip = SetDestination(jitteredtarget, ownerCenter, ownerCenter.Distance(pip));

            //Log.Info($"FindPIP center:{center}  pip:{pip}");
            return pip != Vector2.Zero;
        }

        public void UpdatePrimaryFireTarget(GameplayObject prevTarget, 
            Array<Projectile> enemyProjectiles, Array<Ship> enemyShips)
        {
            TargetChangeTimer -= 0.0167f;
            if (!CanTargetWeapon(prevTarget)) return;
            if (!PickProjectileTarget(enemyProjectiles))
                PickShipTarget(prevTarget, enemyShips);
        }

        private bool CanTargetWeapon(GameplayObject prevTarget)
        {
            // Reasons for this weapon not to fire 
            if (TargetChangeTimer > 0f 
                || !Module.Active
                || CooldownTimer > 0f
                || !Module.Powered || IsRepairDrone || isRepairBeam
                || PowerRequiredToFire > Owner.PowerCurrent)
                return false;
            if ((!TruePD || !Tag_PD) && Owner.PlayerShip)
                return false;

            var projTarget = FireTarget as Projectile;

            // check if weapon target as a gameplay object is still a valid target
            // and if the weapon can still fire on main target.       
            if (FireTarget != null && !Owner.CheckIfInsideFireArc(this, FireTarget)
                || prevTarget != null && SalvoTimer <= 0f && BeamDuration <= 0f
                && projTarget == null && Owner.CheckIfInsideFireArc(this, prevTarget)
            )
            {
                TargetChangeTimer = 0.1f * Module.XSIZE * Module.YSIZE;
                FireTarget        = null;
                if (isTurret) TargetChangeTimer *= .5f;
                if (Tag_PD) TargetChangeTimer   *= .5f;
                if (TruePD) TargetChangeTimer   *= .25f;
            }

            // Reasons for this weapon not to fire                    
            return FireTarget != null || TargetChangeTimer <= 0f;
        }

        private bool PickProjectileTarget(Array<Projectile> enemyProjectiles)
        {
            if (enemyProjectiles.NotEmpty && Tag_PD)
            {
                int maxTrackable = (int)(Owner.TrackingPower + Owner.Level *.05f);
                for (int i = 0; i < maxTrackable && i < enemyProjectiles.Count; i++)
                {
                    Projectile proj = enemyProjectiles[i];
                    if (proj == null || !proj.Active || proj.Health <= 0f || 
                        !proj.Weapon.Tag_Intercept || !Owner.CheckIfInsideFireArc(this, proj))
                        continue;
                    FireTarget = proj;
                    return true;
                }
            }
            return false;
        }

        private void PickShipTarget(GameplayObject prevTarget, Array<Ship> potentialTargets)
        {
            if (potentialTargets.IsEmpty || TruePD) // true PD weapons can't target ships
                return;

            // Is prev target still valid?
            if (Owner.CheckIfInsideFireArc(this, prevTarget))
            {
                FireTarget = prevTarget; // then continue using primary target
            }
            else if (Owner.TrackingPower > 0)
            {
                // limit to one target per level.
                int tracking = (int)(Owner.TrackingPower + Owner.Level * 0.05f);
                for (int i = 0; i < potentialTargets.Count && i < tracking; i++) //
                {
                    Ship potentialTarget = potentialTargets[i];
                    if (potentialTarget == prevTarget)
                    {
                        tracking++;
                        continue;
                    }
                    if (!Owner.CheckIfInsideFireArc(this, potentialTarget))
                        continue;
                    FireTarget = potentialTarget;
                    break;
                }
            }

            // If a ship was found to fire on, change to target an internal module if target is visible  || weapon.Tag_Intercept
            if (FireTarget is Ship targetShip
                && (GlobalStats.ForceFullSim || Owner.InFrustum || targetShip.InFrustum))
            {
                FireTarget = targetShip.GetRandomInternalModule(this);
            }
        }

        private bool CheckFireArc(Vector2 targetPos, GameplayObject maybeTarget = null)
        {
            return maybeTarget != null
                ? Owner.CheckIfInsideFireArc(this, maybeTarget)
                : Owner.CheckIfInsideFireArc(this, targetPos);
        }
        public Vector2 ProjectedBeamPoint(Vector2 source, Vector2 destination, GameplayObject target = null)
        {
            if (DamageAmount < 1) return destination;
            if (target != null)
            {
                if (!Owner.loyalty.IsEmpireAttackable(target.GetLoyalty()))
                    return destination;
            }
            if (Tag_Tractor || isRepairBeam) return destination;

            Vector2 ownerCenter = Owner?.Center ?? Vector2.Zero;
            Vector2 jitter = target?.JitterPosition() ?? Vector2.Zero;            
            jitter += AdjustTargetting();
            jitter += SetDestination(destination, ownerCenter, 4000);
            jitter = SetDestination(jitter, ownerCenter, ownerCenter.Distance(destination));
            return jitter;
        }

        private void FireBeam(Vector2 source, Vector2 destination, GameplayObject target = null, bool followMouse = false)
        {
            destination = ProjectedBeamPoint(source, destination, target);
            if (!CheckFireArc(destination) || !PrepareToFire())
                return;

            var beam = new Beam(this, source, destination, target, followMouse);
            Module.GetParent().AddBeam(beam);
        }

        public void FireDroneBeam(GameplayObject target, DroneAI droneAI)
        {
            drowner = droneAI.Drone;
            var beam = new Beam(this, drowner.Center, target.Center, target);
           
            droneAI.Beams.Add(beam);
        }

        public void FireTargetedBeam(GameplayObject target)
        {
            FireBeam(Module.Center, target.Center, target);
        }

        public void MouseFireAtTarget(Vector2 targetPos)
        {
            if (!CanFireWeapon())
                return;
            if (isBeam) FireBeam(Module.Center, targetPos, null, true);
            else        FireAtTarget(targetPos, null);
        }

        public void FireFromPlanet(Planet planet, Ship targetShip)
        {
            if (!TargetValid(targetShip)) return;
            targetShip.InCombatTimer = 15f;
            GameplayObject target = targetShip.GetRandomInternalModule(this) ?? (GameplayObject) targetShip;

            if (isBeam)
            {
                FireBeam(planet.Center, target.Center, target);
                return;
            }

            if (!ProjectedImpactPoint(target, out Vector2 pip))
                return;
            Vector2 direction = (pip - Center).Normalized();

            foreach (FireSource fireSource in EnumFireSources(planet.Center, direction))
                Projectile.Create(this, planet, fireSource.Direction, target);
        }

        public void FireAtAssignedTarget()
        {
            if (!CanFireWeapon())
                return;
            if (!TargetValid(FireTarget)) return;
            if (FireTarget is Ship targetShip)
            {          
                FireAtAssignedTargetNonVisible(targetShip);
                return;
            }
            if (isBeam) FireBeam(Module.Center, FireTarget.Center, FireTarget);
            else        FireAtTarget(FireTarget.Center, FireTarget);
        }

        private void FireAtAssignedTargetNonVisible(Ship targetShip)
        {
            if (!CanFireWeapon())
                return;
            CooldownTimer = fireDelay;
            if (IsRepairDrone)
                return;
            if (targetShip == null || !targetShip.Active || targetShip.dying //|| !TargetValid(targetShip.shipData.HullRole)
                || targetShip.engineState == Ship.MoveState.Warp || !Owner.CheckIfInsideFireArc(this, targetShip))
                return;

            Owner.Ordinance    -= OrdinanceRequiredToFire;
            Owner.PowerCurrent -= PowerRequiredToFire;
            Owner.PowerCurrent -= BeamPowerCostPerSecond * BeamDuration;
            Owner.InCombatTimer = 15f;

            if (FireTarget is Projectile)
            {
                FireTarget.Damage(Owner, DamageAmount);
                return;
            }

            CooldownTimer = fireDelay;
            if (targetShip.NumExternalSlots == 0)
            {
                targetShip.Die(null, true);
                return;
            } //@todo invisible ecm and such should match visible


            if (targetShip.AI.CombatState == CombatState.Evade) // fbedard: firing on evading ship can miss !
                if (RandomMath.RandomBetween(0f, 100f) < 5f + targetShip.experience)
                    return;

            if (targetShip.shield_power > 0f)
                targetShip.DamageShieldInvisible(Owner, InvisibleDamageAmount);
            else
                targetShip.FindClosestUnshieldedModule(Owner.Center)?.Damage(Owner, InvisibleDamageAmount);
        }

        // @todo This requires optimiziation
        public void ModifyProjectile(Projectile projectile)
        {
            if (Owner == null)
                return;
            if (Owner.loyalty.data.Traits.Pack)
                projectile.DamageAmount += projectile.DamageAmount * Owner.DamageModifier;

            //if (GlobalStats.HasMod && !GlobalStats.ActiveModInfo.useWeaponModifiers)
            //    return;

            float actualshieldpenchance = 0;

            if (Tag_Missile)   AddModifiers("Missile", projectile, ref actualshieldpenchance);
            if (Tag_Energy)    AddModifiers("Energy", projectile, ref actualshieldpenchance);
            if (Tag_Torpedo)   AddModifiers("Torpedo", projectile, ref actualshieldpenchance);
            if (Tag_Kinetic)   AddModifiers("Kinetic", projectile, ref actualshieldpenchance);
            if (Tag_Hybrid)    AddModifiers("Hybrid", projectile, ref actualshieldpenchance);
            if (Tag_Railgun)   AddModifiers("Railgun", projectile, ref actualshieldpenchance);
            if (Tag_Explosive) AddModifiers("Explosive", projectile, ref actualshieldpenchance);
            if (Tag_Guided)    AddModifiers("Guided", projectile, ref actualshieldpenchance);
            if (Tag_Intercept) AddModifiers("Intercept", projectile, ref actualshieldpenchance);
            if (Tag_PD)        AddModifiers("PD", projectile, ref actualshieldpenchance);
            if (Tag_SpaceBomb) AddModifiers("Spacebomb", projectile, ref actualshieldpenchance);
            if (Tag_BioWeapon) AddModifiers("BioWeapon", projectile, ref actualshieldpenchance);
            if (Tag_Drone)     AddModifiers("Drone", projectile, ref actualshieldpenchance);
            if (Tag_Subspace)  AddModifiers("Subspace", projectile, ref actualshieldpenchance);
            if (Tag_Warp)      AddModifiers("Warp", projectile, ref actualshieldpenchance);
            if (Tag_Cannon)    AddModifiers("Cannon", projectile, ref actualshieldpenchance);
            if (Tag_Beam)      AddModifiers("Beam", projectile, ref actualshieldpenchance);
            if (Tag_Bomb)      AddModifiers("Bomb", projectile, ref actualshieldpenchance);
            if (Tag_Array)     AddModifiers("Array", projectile, ref actualshieldpenchance);
            if (Tag_Flak)      AddModifiers("Flak", projectile, ref actualshieldpenchance);
            if (Tag_Tractor)   AddModifiers("Tractor", projectile, ref actualshieldpenchance);

            projectile.IgnoresShields = actualshieldpenchance > 0f && RandomMath2.InRange(100) <= actualshieldpenchance;
        }

        private void AddModifiers(string tag, Projectile projectile, ref float actualShieldPenChance)
        {
            SerializableDictionary<string, WeaponTagModifier> wepTags = Owner.loyalty.data.WeaponTags;
            projectile.DamageAmount      += wepTags[tag].Damage * projectile.DamageAmount;
            projectile.ShieldDamageBonus += wepTags[tag].ShieldDamage;
            projectile.ArmorDamageBonus  += wepTags[tag].ArmorDamage;
            // Shield Penetration
            float currenshieldpenchance   = Module.GetParent().loyalty.data.ShieldPenBonusChance; // check the old calcs first
            currenshieldpenchance        += wepTags[tag].ShieldPenetration * 100; // new calcs
            currenshieldpenchance        += ShieldPenChance;
            actualShieldPenChance = Math.Max(currenshieldpenchance, actualShieldPenChance);
            if (isBeam) return;
            projectile.ArmorPiercing         += (int)wepTags[tag].ArmourPenetration;
            projectile.Health                += HitPoints * wepTags[tag].HitPoints;
            projectile.RotationRadsPerSecond += wepTags[tag].Turn * RotationRadsPerSecond;
            projectile.Speed                 += wepTags[tag].Speed * ProjectileSpeed;
            projectile.DamageRadius          += wepTags[tag].ExplosionRadius * DamageRadius;
        }

        public void ResetToggleSound()
        {
            if (ToggleCue.IsPlaying)
                ToggleCue.Stop();
        }

        public void Update(float elapsedTime)
        {
            if (CooldownTimer > 0f)
            {
                if (WeaponType != "Drone")
                    CooldownTimer = MathHelper.Max(CooldownTimer - elapsedTime, 0f);
            }

            if (SalvosToFire > 0)
            {
                SalvoFireTimer += elapsedTime;
                ContinueSalvo();
            }
            else SalvoTarget = null;
            Center = Module.Center;
        }

        private float CachedModifiedRange;
        public float GetModifiedRange()
        {
            if (Owner?.loyalty == null || GlobalStats.ActiveModInfo == null || !GlobalStats.ActiveModInfo.useWeaponModifiers)
                return Range;

            if (CachedModifiedRange > 0f)
                return CachedModifiedRange;

            float modifier = 1.0f;
            EmpireData loyaltyData = Owner.loyalty.data;
            if (Tag_Beam)      modifier *= loyaltyData.WeaponTags["Beam"].Range;
            if (Tag_Energy)    modifier *= loyaltyData.WeaponTags["Energy"].Range;
            if (Tag_Explosive) modifier *= loyaltyData.WeaponTags["Explosive"].Range;
            if (Tag_Guided)    modifier *= loyaltyData.WeaponTags["Guided"].Range;
            if (Tag_Hybrid)    modifier *= loyaltyData.WeaponTags["Hybrid"].Range;
            if (Tag_Intercept) modifier *= loyaltyData.WeaponTags["Intercept"].Range;
            if (Tag_Kinetic)   modifier *= loyaltyData.WeaponTags["Kinetic"].Range;
            if (Tag_Missile)   modifier *= loyaltyData.WeaponTags["Missile"].Range;
            if (Tag_Railgun)   modifier *= loyaltyData.WeaponTags["Railgun"].Range;
            if (Tag_Cannon)    modifier *= loyaltyData.WeaponTags["Cannon"].Range;
            if (Tag_PD)        modifier *= loyaltyData.WeaponTags["PD"].Range;
            if (Tag_SpaceBomb) modifier *= loyaltyData.WeaponTags["Spacebomb"].Range;
            if (Tag_BioWeapon) modifier *= loyaltyData.WeaponTags["BioWeapon"].Range;
            if (Tag_Drone)     modifier *= loyaltyData.WeaponTags["Drone"].Range;
            if (Tag_Subspace)  modifier *= loyaltyData.WeaponTags["Subspace"].Range;
            if (Tag_Warp)      modifier *= loyaltyData.WeaponTags["Warp"].Range;
            if (Tag_Array)     modifier *= loyaltyData.WeaponTags["Array"].Range;
            if (Tag_Flak)      modifier *= loyaltyData.WeaponTags["Flak"].Range;
            if (Tag_Tractor)   modifier *= loyaltyData.WeaponTags["Tractor"].Range;

            CachedModifiedRange = modifier * Range;
            return CachedModifiedRange;            
        }


        public float GetShieldDamageMod(ShipModule module)
        {
            float damageModifier = EffectVSShields;
            if      (Tag_Kinetic) damageModifier *= (1f - module.shield_kinetic_resist);
            else if (Tag_Energy)  damageModifier *= (1f - module.shield_energy_resist);
            else if (Tag_Beam)    damageModifier *= (1f - module.shield_beam_resist);
            else if (Tag_Missile) damageModifier *= (1f - module.shield_missile_resist);
            return damageModifier;
        }

        public float GetArmorDamageMod(ShipModule module)
        {
            float damageModifier = 1f;
            if (module.Is(ShipModuleType.Armor)) damageModifier *= EffectVsArmor;
            if (Tag_Explosive)                   damageModifier *= (1f - module.ExplosiveResist);
            if (Tag_Kinetic)                     damageModifier *= (1f - module.KineticResist);
            else if (Tag_Beam)                   damageModifier *= (1f - module.BeamResist);
            else if (Tag_Energy)                 damageModifier *= (1f - module.EnergyResist);
            else if (Tag_Missile)                damageModifier *= (1f - module.MissileResist);
            else if (Tag_Torpedo)                damageModifier *= (1f - module.TorpedoResist);
            return damageModifier;
        }

        //How much total resource required to use weapon. 
        public float PowerUseMax    => isBeam ? BeamPowerCostPerSecond * BeamDuration : PowerRequiredToFire;
        public float OrdnanceUseMax => OrdinanceRequiredToFire * SalvoCount;


        public bool TargetValid(ShipData.RoleName role)
        {
            if (Excludes_Fighters && (role == ShipData.RoleName.fighter || role == ShipData.RoleName.scout || role == ShipData.RoleName.drone))
                return false;
            if (Excludes_Corvettes && (role == ShipData.RoleName.corvette || role == ShipData.RoleName.gunboat))
                return false;
            if (Excludes_Capitals && (role == ShipData.RoleName.frigate || role == ShipData.RoleName.destroyer || role == ShipData.RoleName.cruiser || role == ShipData.RoleName.carrier || role == ShipData.RoleName.capital))
                return false;
            if (Excludes_Stations && (role == ShipData.RoleName.platform || role == ShipData.RoleName.station))
                return false;
            return true;
        }
        public bool TargetValid(GameplayObject fireTarget)
        {
            if (fireTarget.Type == GameObjectType.ShipModule)
                return TargetValid(((ShipModule)fireTarget).GetParent().shipData.HullRole);
            if (fireTarget.Type == GameObjectType.Ship)
                return TargetValid(((Ship)fireTarget).shipData.HullRole);
            return true;
        }
        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        ~Weapon() { Destroy(); }

        private void Destroy()
        {
            ToggleCue.Destroy();
            Owner         = null;
            drowner       = null;
            Module        = null;
            AltFireWeapon = null;
            FireTarget    = null;
            SalvoTarget   = null;
        }

        public override string ToString() => $"Weapon {WeaponType} {WeaponEffectType} {Name}";
    }
}
