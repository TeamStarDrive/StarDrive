using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.AI;

namespace Ship_Game.Gameplay
{
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
        Tractor   = (1 << 20),
    }

    public sealed class Weapon : IDisposable
    {
        private int TagBits;
        public bool this[WeaponTag tag]
        {
            get => (TagBits & (int)tag) != 0;
            set => TagBits = value ? TagBits|(int)tag : TagBits & ~(int)tag;
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
        public bool PlaySoundOncePerSalvo;
        public int SalvoCount = 1;
        public readonly float SalvoTimer;
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
        public float BeamDuration=2f;
        public int BeamPowerCostPerSecond;
        public string BeamTexture;
        public int Animated;
        public int Frames;
        public string AnimationPath;
        public string ExpColor;
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
        public ShipModule moduleAttachedTo;
        public float timeToNextFire;
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
        [XmlIgnore][JsonIgnore]
        private AudioEmitter planetEmitter;
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
        public float OffPowerMod = 1f;

        public bool RangeVariance;
        public float ExplosionRadiusVisual = 4.5f;
        [XmlIgnore][JsonIgnore]
        public GameplayObject fireTarget;
        public float TargetChangeTimer;
        public bool PrimaryTarget = false;

        [XmlIgnore][JsonIgnore]
        private int SalvosToFire;
        private Vector2 SalvoDirection;
        private float SalvoFireTimer; // while SalvosToFire, use this timer to count when to fire next shot
        public GameplayObject SalvoTarget;


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
      

        public Weapon(Ship owner, ShipModule moduleAttachedTo)
        {
            Owner = owner;
            this.moduleAttachedTo = moduleAttachedTo;
        }

        public Weapon()
        {
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo != null)
            {
                ExplosionRadiusVisual *= GlobalStats.ActiveModInfo.GlobalExplosionVisualIncreaser;
            }
        }

        public Weapon Clone()
        {
            Weapon wep = (Weapon)MemberwiseClone();
            // @todo Remove SalvoList
            wep.SalvoTarget      = null;
            wep.fireTarget       = null;
            wep.planetEmitter    = null;
            wep.moduleAttachedTo = null;
            wep.Owner            = null;
            wep.drowner          = null;
            return wep;
        }

        private void CreateDrone(Vector2 direction)
        {
            var projectile = new Projectile(Owner, direction, moduleAttachedTo)
            {
                Range        = Range,
                Weapon       = this,
                Explodes     = explodes,
                DamageAmount = DamageAmount
            };
            projectile.Explodes              = explodes;
            projectile.DamageRadius          = DamageRadius;
            projectile.ExplosionRadiusMod    = ExplosionRadiusVisual;
            projectile.Speed                 = ProjectileSpeed;
            projectile.Health                = HitPoints;
            projectile.WeaponEffectType      = WeaponEffectType;
            projectile.WeaponType            = WeaponType;
            projectile.RotationRadsPerSecond = RotationRadsPerSecond;
            projectile.LoadContent(ProjectileTexturePath, ModelPath);
            ModifyProjectile(projectile);
            projectile.InitializeDrone(projectile.Speed, direction);
            projectile.Radius = ProjectileRadius;
            Owner.AddProjectile(projectile);

            if (Owner.InFrustum)
            {
                PlayToggleAndFireSfx();
                projectile.DieSound = true;

                string dieCueName = ResourceManager.GetWeaponTemplate(UID).dieCue;
                if (dieCueName.NotEmpty())  projectile.DieCueName  = dieCueName;
                if (InFlightCue.NotEmpty()) projectile.InFlightCue = InFlightCue;
            }
        }
        /// <summary>
        /// modify damageamount utilizing tech bonus. Currently this is only ordnance bonus.
        /// </summary>
        /// <returns>float amount to add</returns>
        private float AdjustDamage()
        {
            if (Owner?.loyalty?.data == null) return 0;
            if (OrdinanceRequiredToFire >0 && DamageAmount >0)
            {
                return Owner.loyalty.data.OrdnanceEffectivenessBonus * DamageAmount;
            }
            return 0;            
        }
        private void PlayToggleAndFireSfx(AudioEmitter emitter = null)
        {
            if (ToggleCue.IsPlaying)
                return;

            AudioEmitter soundEmitter = Owner?.PlayerShip == true ? null : (emitter ?? Owner?.SoundEmitter);

            GameAudio.PlaySfxAsync(fireCueName, soundEmitter);
            ToggleCue.PlaySfxAsync(ToggleSoundName, soundEmitter);
        }

        private void CreateDroneBeam(GameplayObject target, DroneAI source)
        {
            // repair drone beams with negative damage have their collision disabled
            var beam = new Beam(this, target)
            {
                DisableSpatialCollision = DamageAmount < 0f
            };
            source.Beams.Add(beam);

            if (Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
                PlayToggleAndFireSfx(source.Owner.Emitter);
        }

        private void CreateTargetedBeam(GameplayObject target)
        {
            var beam = new Beam(this, target);

            //damage increase by level
            if (Owner.Level > 0)
            {
                beam.DamageAmount += beam.DamageAmount * Owner.Level * 0.05f;
            }
            //Hull bonus damage increase
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.useHullBonuses)
            {
                if (ResourceManager.HullBonuses.TryGetValue(Owner.shipData.Hull, out HullBonus mod))
                    beam.DamageAmount += beam.DamageAmount * mod.DamageBonus;
            }
            ModifyProjectile(beam);

            moduleAttachedTo.GetParent().AddBeam(beam);
            if (Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView && moduleAttachedTo.GetParent().InFrustum)
                PlayToggleAndFireSfx(Owner.SoundEmitter);
        }

        private void CreateMouseBeam(Vector2 direction)
        {
            var beam = new Beam(this, moduleAttachedTo.Center + direction*Range) { FollowMouse = true };
            moduleAttachedTo.GetParent().AddBeam(beam);

            if ((Owner.System?.isVisible == true || Owner.InDeepSpace) && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
                PlayToggleAndFireSfx();
        }

        private Projectile CreateProjectile(Ship owner, Vector2 direction, ShipModule attachedTo, GameplayObject target, bool playSound = true)
        {
            var projectile = new Projectile(owner, direction, attachedTo)
            {
                Range = Range,
                Weapon = this,
                Explodes = explodes,
                DamageAmount = DamageAmount + AdjustDamage(),
                DamageRadius = DamageRadius,
                ExplosionRadiusMod = ExplosionRadiusVisual,
                Health = HitPoints,
                Speed = ProjectileSpeed,
                WeaponEffectType = WeaponEffectType,
                WeaponType = WeaponType,
                RotationRadsPerSecond = RotationRadsPerSecond,
                ArmorPiercing = (int) ArmourPen
            };

            if (owner.Level > 0)
                projectile.DamageAmount += projectile.DamageAmount * owner.Level * 0.05f;
            if (RangeVariance)
                projectile.Range *= RandomMath.RandomBetween(0.9f, 1.1f);

            //Hull bonus damage increase
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.useHullBonuses)
            {
                if (ResourceManager.HullBonuses.TryGetValue(Owner.shipData.Hull, out HullBonus mod))
                    projectile.DamageAmount += projectile.DamageAmount * mod.DamageBonus;
            }
            projectile.LoadContent(ProjectileTexturePath, ModelPath);
            ModifyProjectile(projectile);

            if (Tag_Guided) projectile.InitializeMissile(projectile.Speed, direction, target);
            else            projectile.Initialize(projectile.Speed, direction, attachedTo.Center);
            projectile.Radius = ProjectileRadius;

            if (Animated == 1)
                projectile.TexturePath = AnimationPath + 0.ToString("00000.##");

            if (Empire.Universe.viewState <= UniverseScreen.UnivScreenState.ShipView && Owner.InFrustum && playSound)
            {
                PlayToggleAndFireSfx();
                projectile.DieSound = true;

                string dieCueName = ResourceManager.WeaponsDict[UID].dieCue;
                if (dieCueName.NotEmpty())  projectile.DieCueName  = dieCueName;
                if (InFlightCue.NotEmpty()) projectile.InFlightCue = InFlightCue;
            }

            Owner.AddProjectile(projectile);
            return projectile;
        }

        private void CreateProjectiles(Vector2 direction, GameplayObject target, bool playSound)
        {
            if (SecondaryFire != null && AltFireTriggerFighter && AltFireMode &&
                target is ShipModule shipModule && shipModule.GetParent().shipData.Role == ShipData.RoleName.fighter)
            {
                Weapon altFire = ResourceManager.CreateWeapon(SecondaryFire);
                Projectile projectile = altFire.CreateProjectile(Owner, direction, moduleAttachedTo, shipModule, playSound);
                projectile.IsSecondary = true;
            }
            else
            {
                CreateProjectile(Owner, direction, moduleAttachedTo, target);
            }
        }

        private void CreateProjectilesFromPlanet(Vector2 direction, Planet p, GameplayObject target)
        {
            var projectile = new Projectile(p, direction)
            {
                Range = Range,
                Weapon = this,
                Explodes = explodes,
                DamageAmount = DamageAmount + AdjustDamage()
            };
            if (RangeVariance)
            {
                projectile.Range *= RandomMath.RandomBetween(0.9f, 1.1f);
            }
            projectile.Explodes              = explodes;
            projectile.DamageRadius          = DamageRadius;
            projectile.ExplosionRadiusMod    = ExplosionRadiusVisual;
            projectile.Health                = HitPoints;
            projectile.Speed                 = ProjectileSpeed;
            projectile.WeaponEffectType      = WeaponEffectType;
            projectile.WeaponType            = WeaponType;
            projectile.LoadContent(ProjectileTexturePath, ModelPath);
            projectile.RotationRadsPerSecond = RotationRadsPerSecond;
            projectile.ArmorPiercing         = (int)ArmourPen;

            ModifyProjectile(projectile);
            if (Tag_Guided) projectile.InitializeMissilePlanet(projectile.Speed, direction, target, p);
            else            projectile.InitializePlanet(projectile.Speed, direction, p.Position);
            projectile.Radius = ProjectileRadius;
            if (Animated == 1)
            {
                projectile.TexturePath = AnimationPath + 0.ToString("00000.##");
            }
            p.Projectiles.Add(projectile);
            planetEmitter = new AudioEmitter
            {
                Position = new Vector3(p.Position, 2500f)
            };
            if (Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
            {
                planetEmitter.Position = new Vector3(p.Position, -2500f);
                PlayToggleAndFireSfx(planetEmitter);

                projectile.DieSound = true;

                string dieCueName = ResourceManager.WeaponsDict[UID].dieCue;
                if (dieCueName.NotEmpty())  projectile.DieCueName  = dieCueName;
                if (InFlightCue.NotEmpty()) projectile.InFlightCue = InFlightCue;
            }
        }

        public void Fire(Vector2 direction, GameplayObject target)
        {
            if (Owner.engineState == Ship.MoveState.Warp || timeToNextFire > 0f || !Owner.CheckRangeToTarget(this, target))
                return;
            Owner.InCombatTimer = 15f;

            timeToNextFire = fireDelay + (RandomMath.InRange(10)*0.016f + -0.008f);

            if (moduleAttachedTo.Active && Owner.PowerCurrent > PowerRequiredToFire && OrdinanceRequiredToFire <= Owner.Ordinance)
            {
                Owner.Ordinance -= OrdinanceRequiredToFire;
                Owner.PowerCurrent -= PowerRequiredToFire;

                if (FireArc != 0)
                {
                    foreach (Vector2 fireDir in EnumFireArc(direction, ProjectileCount))
                        CreateProjectiles(fireDir, target, true);
                }
                else
                {
                    for (int i = 0; i < ProjectileCount; ++i)
                        CreateProjectiles(GetFireConeVector(direction), target, true);
                }
                if (SalvoCount > 1)
                {
                    SalvosToFire   = SalvoCount - 1;
                    SalvoDirection = direction;
                    SalvoFireTimer = 0f;
                    SalvoTarget = target;
                }
            }
        }

        public void FireDrone(Vector2 direction)
        {
            if (timeToNextFire > 0f)
            {
                return;
            }
            Owner.InCombatTimer = 15f;
            timeToNextFire = fireDelay;
            if (moduleAttachedTo.Active && Owner.PowerCurrent > PowerRequiredToFire && OrdinanceRequiredToFire <= Owner.Ordinance)
            {
                Owner.Ordinance    -= OrdinanceRequiredToFire;
                Owner.PowerCurrent -= PowerRequiredToFire;
                CreateDrone(Vector2.Normalize(direction));
            }
        }

        public void FireDroneBeam(GameplayObject target, DroneAI source)
        {
            drowner = source.Owner;
            if (timeToNextFire > 0f)
                return;
            timeToNextFire = fireDelay;
            CreateDroneBeam(target, source);
        }

        public void FireFromPlanet(Vector2 direction, Planet p, GameplayObject target)
        {
            if (target is ShipModule shipModule)
                shipModule.GetParent().InCombatTimer = 15f;

            if (FireArc != 0)
            {
                foreach (Vector2 fireDir in EnumFireArc(direction, ProjectileCount))
                    CreateProjectilesFromPlanet(fireDir, p, target);
            }
            else if (FireCone <= 0)
            {
                if (!isBeam)
                {
                    Vector2 dir = WeaponType != "Missile" ? direction : Vector2.Normalize(direction);
                    for (int i = 0; i < ProjectileCount; i++)
                        CreateProjectilesFromPlanet(dir, p, target);
                }
            }
            else
            {
                CreateProjectilesFromPlanet(GetFireConeVector(direction), p, target);
            }
        }

        public void FireSalvo(Vector2 direction, GameplayObject target)
        {
            if (Owner.engineState == Ship.MoveState.Warp)
                return;
            Owner.InCombatTimer = 15f;
            if (moduleAttachedTo.Active && Owner.PowerCurrent > PowerRequiredToFire && OrdinanceRequiredToFire <= Owner.Ordinance)
            {
                Owner.Ordinance -= OrdinanceRequiredToFire;
                Owner.PowerCurrent -= PowerRequiredToFire;
                if (FireArc != 0)
                {
                    foreach (Vector2 fireDir in EnumFireArc(direction, ProjectileCount))
                        CreateProjectiles(fireDir, target, !PlaySoundOncePerSalvo);
                }
                else
                {
                    for (int i = 0; i < ProjectileCount; ++i)
                        CreateProjectiles(GetFireConeVector(direction), target, !PlaySoundOncePerSalvo);
                }
            }
        }

        public void FireTargetedBeam(GameplayObject target)
        {
            if (timeToNextFire > 0f )
                return;
            Owner.InCombatTimer = 15f;
            timeToNextFire = fireDelay + (RandomMath.InRange(10) * 0.016f + -0.008f);
            if (moduleAttachedTo.Active && Owner.PowerCurrent > PowerRequiredToFire && OrdinanceRequiredToFire <= Owner.Ordinance)
            {
                Owner.Ordinance    -= OrdinanceRequiredToFire;                
                Owner.PowerCurrent -= PowerRequiredToFire;
                CreateTargetedBeam(target);
            }
        }

        public void FireMouseBeam(Vector2 direction)
        {
            if (timeToNextFire > 0f)
                return;
            Owner.InCombatTimer = 15f;
            timeToNextFire = fireDelay;
            if (moduleAttachedTo.Active && Owner.PowerCurrent > PowerRequiredToFire && OrdinanceRequiredToFire <= Owner.Ordinance)
            {
                Owner.Ordinance    -= OrdinanceRequiredToFire;
                Owner.PowerCurrent -= PowerRequiredToFire;
                CreateMouseBeam(direction);
            }
        }

        private Vector2 GetFireConeVector(Vector2 direction)
        {
            if (FireCone <= 0)
                return direction;
            float spread = RandomMath2.RandomBetween(-FireCone, FireCone) * 0.5f;
            return (direction.ToDegrees() + spread).AngleToDirection();
        }

        private IEnumerable<Vector2> EnumFireArc(Vector2 direction, int projectileCount)
        {
            float degreesBetweenShots = FireArc / (float)projectileCount;
            float angleToTarget = direction.ToDegrees() - FireArc * 0.5f;
            for (int i = 0; i < projectileCount; ++i)
            {
                Vector2 dir = angleToTarget.AngleToDirection();
                angleToTarget += degreesBetweenShots;
                yield return dir;
            }
        }

        public void FireMouse(Vector2 direction)
        {
            if (Owner.engineState == Ship.MoveState.Warp || timeToNextFire > 0f)
                return;
            Owner.InCombatTimer = 15f;
            timeToNextFire = fireDelay;
            if (moduleAttachedTo.Active && Owner.PowerCurrent > PowerRequiredToFire && OrdinanceRequiredToFire <= Owner.Ordinance)
            {
                Owner.Ordinance -= OrdinanceRequiredToFire;
                Owner.PowerCurrent -= PowerRequiredToFire;

                if (FireArc != 0)
                {
                    foreach (Vector2 fireDir in EnumFireArc(direction, ProjectileCount))
                        CreateProjectiles(fireDir, null, true);
                }
                else
                {
                    for (int i = 0; i < ProjectileCount; i++)
                        CreateProjectiles(GetFireConeVector(direction), null, true);
                }

                if (SalvoCount > 1) // queue the rest of the salvo to follow later
                {
                    SalvosToFire   = SalvoCount - 1;
                    SalvoDirection = direction;
                    SalvoFireTimer = 0f;
                    SalvoTarget    = null; // untargeted salvo... well whatever
                }
            }
        }

        public Projectile LoadProjectiles(Vector2 direction, Ship owner)
        {
            var projectile = new Projectile(owner, direction)
            {
                Range        = Range,
                Weapon       = this,
                Explodes     = explodes,
                DamageAmount = DamageAmount
            };
            projectile.Explodes           = explodes;
            projectile.DamageRadius       = DamageRadius;
            projectile.ExplosionRadiusMod = ExplosionRadiusVisual;
            projectile.Speed              = ProjectileSpeed;
            projectile.WeaponEffectType   = WeaponEffectType;
            projectile.WeaponType         = WeaponType;
            projectile.Initialize(ProjectileSpeed, direction, owner.Center);
            projectile.Radius = ProjectileRadius;
            projectile.LoadContent(ProjectileTexturePath, ModelPath);
            if (owner.System != null && owner.System.isVisible || owner.InDeepSpace)
            {
                projectile.DieSound = true;
                if (!string.IsNullOrEmpty(ResourceManager.WeaponsDict[UID].dieCue))
                    projectile.DieCueName = ResourceManager.WeaponsDict[UID].dieCue;
                if (!string.IsNullOrEmpty(InFlightCue))
                    projectile.InFlightCue = InFlightCue;
            }
            return projectile;
        }

        private void ModifyProjectile(Projectile projectile)
        {
            if (Owner == null)
                return;
            if (Owner.loyalty.data.Traits.Pack)
            {
                projectile.DamageAmount += projectile.DamageAmount * Owner.DamageModifier;
            }
            //Added by McShooterz: Check if mod uses weapon modifiers
            if (GlobalStats.HasMod && !GlobalStats.ActiveModInfo.useWeaponModifiers)
                return;
            if (Tag_Missile)   AddModifiers("Missile", projectile);
            if (Tag_Energy)    AddModifiers("Energy", projectile);
            if (Tag_Torpedo)   AddModifiers("Torpedo", projectile);
            if (Tag_Kinetic)   AddModifiers("Kinetic", projectile);
            if (Tag_Hybrid)    AddModifiers("Hybrid", projectile);
            if (Tag_Railgun)   AddModifiers("Railgun", projectile);
            if (Tag_Explosive) AddModifiers("Explosive", projectile);
            if (Tag_Guided)    AddModifiers("Guided", projectile);
            if (Tag_Intercept) AddModifiers("Intercept", projectile);
            if (Tag_PD)        AddModifiers("PD", projectile);
            if (Tag_SpaceBomb) AddModifiers("Spacebomb", projectile);
            if (Tag_BioWeapon) AddModifiers("BioWeapon", projectile);
            if (Tag_Drone)     AddModifiers("Drone", projectile);
            if (Tag_Subspace)  AddModifiers("Subspace", projectile);
            if (Tag_Warp)      AddModifiers("Warp", projectile);
            if (Tag_Cannon)    AddModifiers("Cannon", projectile);
            if (Tag_Beam)      AddModifiers("Beam", projectile);
            if (Tag_Bomb)      AddModifiers("Bomb", projectile);
            if (Tag_Array)     AddModifiers("Array", projectile);
            if (Tag_Flak)      AddModifiers("Flak", projectile);
            if (Tag_Tractor)   AddModifiers("Tractor", projectile);
        }
        
        private void AddModifiers(string tag, Projectile projectile)
        {
            var wepTags = Owner.loyalty.data.WeaponTags;
            projectile.DamageAmount      += wepTags[tag].Damage * projectile.DamageAmount;
            projectile.ShieldDamageBonus += wepTags[tag].ShieldDamage;
            projectile.ArmorDamageBonus  += wepTags[tag].ArmorDamage;
            // Shield Penetration
            float actualShieldPenChance = moduleAttachedTo.GetParent().loyalty.data.ShieldPenBonusChance;
            actualShieldPenChance += wepTags[tag].ShieldPenetration;
            actualShieldPenChance += ShieldPenChance;
            if (actualShieldPenChance > 0f && RandomMath2.InRange(100) < actualShieldPenChance)
            {
                projectile.IgnoresShields = true;
            }
            if (!isBeam)
            {
                projectile.ArmorPiercing         += (int)wepTags[tag].ArmourPenetration;
                projectile.Health                += HitPoints * wepTags[tag].HitPoints;
                projectile.RotationRadsPerSecond += wepTags[tag].Turn * RotationRadsPerSecond;
                projectile.Speed                 += wepTags[tag].Speed * ProjectileSpeed;
                projectile.DamageRadius          += wepTags[tag].ExplosionRadius * DamageRadius;
            }
        }

        public void ResetToggleSound()
        {
            if (ToggleCue.IsPlaying)
                ToggleCue.Stop();
        }

        public void Update(float elapsedTime)
        {
            if (timeToNextFire > 0f)
            {
                if (WeaponType != "Drone") timeToNextFire = MathHelper.Max(timeToNextFire - elapsedTime, 0f);
                //Gretman -- To fix broken Repair Drones, I moved updating the cooldown for drone weapons to the ArtificialIntelligence update function.
            }

            if (SalvosToFire > 0)
            {
                float timeBetweenShots = SalvoTimer / SalvoCount;
                SalvoFireTimer += elapsedTime;
                if (SalvoFireTimer >= timeBetweenShots)
                {
                    SalvoFireTimer -= timeBetweenShots;
                    --SalvosToFire;

                    if (SalvoTarget == null)
                    {
                        FireSalvo(SalvoDirection, null);
                    }
                    else if (Owner.CheckIfInsideFireArc(this, SalvoTarget))
                    {
                        if (Tag_Guided)
                            FireSalvo(SalvoDirection, SalvoTarget);
                        else
                            Owner.AI.CalculateAndFire(this, SalvoTarget, true);
                    }
                }
            }
            else SalvoTarget = null;
            Center = moduleAttachedTo.Center;
        }

        private float CachedModifiedRange;
        public float GetModifiedRange()
        {
            if (Owner == null || GlobalStats.ActiveModInfo == null || !GlobalStats.ActiveModInfo.useWeaponModifiers)
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

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        ~Weapon() { Destroy(); }

        private void Destroy()
        {
        }

        public override string ToString() => $"Weapon {WeaponType} {WeaponEffectType} {Name}";
    }

    public sealed class ProjectileTracker
    {
        public float Timer = 1f;
    }

}
