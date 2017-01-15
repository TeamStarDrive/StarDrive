using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.AI;

namespace Ship_Game.Gameplay
{
	public class Weapon : IDisposable
	{
		public bool Tag_Kinetic;
		public bool Tag_Energy;
		public bool Tag_Guided;
		public bool Tag_Missile;
		public bool Tag_Hybrid;
		public bool Tag_Beam;
		public bool Tag_Explosive;
		public bool Tag_Intercept;
		public bool Tag_Railgun;
		public bool Tag_Bomb;
		public bool Tag_SpaceBomb;
		public bool Tag_BioWeapon;
		public bool Tag_Drone;
		public bool Tag_Warp;
		public bool Tag_Torpedo;
		public bool Tag_Cannon;
		public bool Tag_Subspace;
		public bool Tag_PD;
        public bool Tag_Flak;
        public bool Tag_Array;
        public bool Tag_Tractor;

        [XmlIgnore][JsonIgnore]
        private Ship Owner;
		public GameplayObject drowner;
		public float HitPoints;
		public bool isBeam;
		public float EffectVsArmor = 1f;
		public float EffectVSShields = 1f;
		public bool PlaySoundOncePerSalvo;
		public int SalvoCount = 1;
		public float SalvoTimer;
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
		protected Cue fireCue;
		public string ToggleSoundName = "";
		private bool ToggleSoundOn;
		private Cue ToggleCue;
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
		public ShipModule moduleAttachedTo;
		public float timeToNextFire;
		public float fireDelay;
		public float PowerRequiredToFire;
		public bool explodes;
		public float DamageRadius;
		public string fireCueName;
		public string MuzzleFlash;
		public bool IsRepairDrone;
		private BatchRemovalCollection<Salvo> SalvoList = new BatchRemovalCollection<Salvo>();
		public bool FakeExplode;
		public float ProjectileRadius = 4f;
		public string Name;
		public byte LoopAnimation;
		public float Scale = 1f;
		public float RotationRadsPerSecond = 2f;
		private AudioEmitter planetEmitter;
		public bool HitsFriendlies;
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

        public GameplayObject SalvoTarget;
        public float ExplosionRadiusVisual = 4.5f;
        public GameplayObject fireTarget;
        public float TargetChangeTimer;
        public bool PrimaryTarget = false;
        [XmlIgnore][JsonIgnore]
        public Array<ModuleSlot> AttackerTargetting;// = new Array<ModuleSlot>();

		public static AudioListener audioListener { get; set; }

		public Weapon(Ship owner, ShipModule moduleAttachedTo)
		{
			Owner = owner;
			this.moduleAttachedTo = moduleAttachedTo;
		}

		public Weapon()
		{
            if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi !=null)
            {
                ExplosionRadiusVisual *= GlobalStats.ActiveMod.mi.GlobalExplosionVisualIncreaser;
            }
		}

        public Weapon Clone()
        {
            Weapon wep = (Weapon)MemberwiseClone();
            // @todo Remove SalvoList
            wep.SalvoList        = new BatchRemovalCollection<Salvo>();
            wep.SalvoTarget      = null;
            wep.fireTarget       = null;
            wep.planetEmitter    = null;
            wep.moduleAttachedTo = null;
            wep.Owner            = null;
            wep.drowner          = null;
            return wep;
        }

        private void AddModifiers(string tag, Projectile projectile)
        {
            var wepTags = Owner.loyalty.data.WeaponTags;
            projectile.damageAmount      += wepTags[tag].Damage * projectile.damageAmount;
            projectile.ShieldDamageBonus += wepTags[tag].ShieldDamage;
            projectile.ArmorDamageBonus  += wepTags[tag].ArmorDamage;
            //Shield Penetration
            float actualShieldPenChance = moduleAttachedTo.GetParent().loyalty.data.ShieldPenBonusChance;
            actualShieldPenChance += wepTags[tag].ShieldPenetration;
            actualShieldPenChance += ShieldPenChance;
            if (actualShieldPenChance > 0f && RandomMath2.InRange(100) < actualShieldPenChance)
            {
                projectile.IgnoresShields = true;
            }
            if (!isBeam)
            {
                projectile.ArmorPiercing         += (byte)wepTags[tag].ArmourPenetration;
                projectile.Health                += HitPoints * wepTags[tag].HitPoints;
                projectile.RotationRadsPerSecond += wepTags[tag].Turn * RotationRadsPerSecond;
                projectile.speed                 += wepTags[tag].Speed * ProjectileSpeed;
                projectile.damageRadius          += wepTags[tag].ExplosionRadius * DamageRadius;
            }
        }

		protected virtual void CreateDrone(Vector2 direction)
		{
			var projectile = new Projectile(Owner, direction, moduleAttachedTo)
			{
				range = Range,
				weapon = this,
				explodes = explodes,
				damageAmount = DamageAmount
            };
			projectile.explodes = explodes;
			projectile.damageRadius = DamageRadius;
            projectile.explosionradiusmod = ExplosionRadiusVisual;
			projectile.speed = ProjectileSpeed;
			projectile.Health = HitPoints;
			projectile.WeaponEffectType = WeaponEffectType;
			projectile.WeaponType = WeaponType;
			projectile.LoadContent(ProjectileTexturePath, ModelPath);
			projectile.RotationRadsPerSecond = RotationRadsPerSecond;
            ModifyProjectile(projectile);
			projectile.InitializeDrone(projectile.speed, direction);
			projectile.Radius = ProjectileRadius;
            Owner.Projectiles.Add(projectile);
			if (Owner.InFrustum)
			{
				projectile.DieSound = true;
				if (!string.IsNullOrEmpty(ToggleSoundName) && !ToggleSoundOn)
				{
                    ToggleSoundOn = true;
                    ToggleCue = AudioManager.GetCue(ToggleSoundName);
                    ToggleCue.Apply3D(audioListener, Owner.emitter);
                    ToggleCue.Play();
                    PlayFireCue(fireCueName, Owner.emitter, !Owner.isPlayerShip());
				}
				if (ResourceManager.GetWeaponTemplate(UID).dieCue.NotEmpty())
				{
					projectile.dieCueName = ResourceManager.GetWeaponTemplate(UID).dieCue;
				}
				if (!string.IsNullOrEmpty(InFlightCue))
				{
					projectile.InFlightCue = InFlightCue;
				}
				if (ToggleCue == null)
				{
                    fireCue = AudioManager.GetCue(fireCueName);
					if (!Owner.isPlayerShip())
					{
                        fireCue.Apply3D(audioListener, Owner.emitter);
					}
				    fireCue?.Play();
				}
			}
		}

		protected virtual void CreateDroneBeam(Vector2 destination, GameplayObject target, DroneAI source)
		{
            if (source == null)
                return;
		    var beam = new Beam(source.Owner.Center, target.Center, BeamThickness, source.Owner, target)
		    {
		        moduleAttachedTo = moduleAttachedTo,
		        PowerCost        = BeamPowerCostPerSecond,
		        range            = Range,
		        thickness        = BeamThickness,
		        Duration         = BeamDuration > 0 ? BeamDuration : 2f,
		        damageAmount     = DamageAmount,
		        weapon           = this
		    };


		    if (!beam.LoadContent(Empire.Universe.ScreenManager, Empire.Universe.view, Empire.Universe.projection))
            {
                beam.Die(null, true);
                return;
            } 
            source.Beams.Add(beam);
            ToggleSoundOn = false;
			if (Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
			{
                PlayFireCue(fireCueName, source.Owner.emitter);
                if (!string.IsNullOrEmpty(ToggleSoundName))
				{
                    ToggleSoundOn = true;
                    ToggleCue = AudioManager.GetCue(ToggleSoundName);
                    ToggleCue.Apply3D(audioListener, source.Owner.emitter);
                    ToggleCue.Play();
				}
			}
		}

        protected virtual void CreateTargetedBeam(GameplayObject target)
        {
            var beam = new Beam(moduleAttachedTo.Center, BeamThickness, moduleAttachedTo.GetParent(), target)
            {
                moduleAttachedTo = moduleAttachedTo,
                PowerCost        = BeamPowerCostPerSecond,
                range            = Range,
                thickness        = BeamThickness,
                Duration         = BeamDuration > 0 ? BeamDuration : 2f,
                damageAmount     = DamageAmount,
                weapon           = this,
                Destination      = target.Center
            };

            //damage increase by level
            if (Owner.Level > 0)
            {
                beam.damageAmount += beam.damageAmount * (float)Owner.Level * 0.05f;
            }
            //Hull bonus damage increase
			if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.useHullBonuses)
            {
                if (ResourceManager.HullBonuses.TryGetValue(Owner.shipData.Hull, out HullBonus mod))
                    beam.damageAmount += beam.damageAmount * mod.DamageBonus;
            }
            ModifyProjectile(beam);

            if (!beam.LoadContent(Empire.Universe.ScreenManager, Empire.Universe.view, Empire.Universe.projection))
            {
                beam.Die(null, true);
                return;
            }
            moduleAttachedTo.GetParent().Beams.Add(beam);
            ToggleSoundOn = false;
            if (Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView && moduleAttachedTo.GetParent().InFrustum)
            {
                PlayFireCue(fireCueName, Owner.emitter, !Owner.isPlayerShip());

                if (!string.IsNullOrEmpty(ToggleSoundName))
                {
                    ToggleSoundOn = true;
                    ToggleCue = AudioManager.GetCue(ToggleSoundName);
                    ToggleCue.Apply3D(audioListener, Owner.emitter);
                    ToggleCue.Play();
                }
            }
        }

		protected virtual void CreateMouseBeam(Vector2 destination)
		{
            var beam = new Beam(moduleAttachedTo.Center, destination, BeamThickness, moduleAttachedTo.GetParent())
			{
				moduleAttachedTo = moduleAttachedTo,
				range            = Range,
				followMouse      = true,
				thickness        = BeamThickness,
                Duration         = BeamDuration > 0 ? BeamDuration : 2f,
				PowerCost        = BeamPowerCostPerSecond,
				damageAmount     = DamageAmount,
				weapon           = this
			};
			
			beam.LoadContent(Empire.Universe.ScreenManager, Empire.Universe.view, Empire.Universe.projection);
            if (!beam.Active)
            {
                beam.Die(null, true);
                return;
            }
            moduleAttachedTo.GetParent().Beams.Add(beam);
			ToggleSoundOn = false;
			if ((Owner.System != null && Owner.System.isVisible || Owner.isInDeepSpace) && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
			{
                PlayFireCue(fireCueName, Owner.emitter, !Owner.isPlayerShip());
                if (!string.IsNullOrEmpty(ToggleSoundName) && !ToggleSoundOn)
                {
                    ToggleSoundOn = true;
                    ToggleCue = AudioManager.GetCue(ToggleSoundName);
                    ToggleCue.Apply3D(audioListener, Owner.emitter);
                    ToggleCue.Play();
                }
			}
		}

		protected virtual void CreateProjectiles(Vector2 direction, GameplayObject target, bool playSound)
		{
		    if (SecondaryFire != null && AltFireTriggerFighter && AltFireMode &&
                target is ShipModule shipModule && shipModule.GetParent().shipData.Role == ShipData.RoleName.fighter)
            {
                Weapon altFire = ResourceManager.GetWeapon(SecondaryFire);
                Projectile projectile;
                if (Owner.Projectiles.TryReuseItem(out projectile))
                {
                    projectile.ProjectileRecreate(Owner, direction, moduleAttachedTo);
                    projectile.range                 = altFire.Range;
                    projectile.weapon                = this;
                    projectile.explodes              = altFire.explodes;
                    projectile.damageAmount          = altFire.DamageAmount;
                    projectile.damageRadius          = altFire.DamageRadius;
                    projectile.explosionradiusmod    = altFire.ExplosionRadiusVisual;
                    projectile.Health                = altFire.HitPoints;
                    projectile.speed                 = altFire.ProjectileSpeed;
                    projectile.WeaponEffectType      = altFire.WeaponEffectType;
                    projectile.WeaponType            = altFire.WeaponType;
                    projectile.RotationRadsPerSecond = altFire.RotationRadsPerSecond;
                    projectile.ArmorPiercing         = (byte)altFire.ArmourPen;
                }
                else
                    projectile = new Projectile(Owner, direction, moduleAttachedTo)
                    {
                        range                 = altFire.Range,
                        weapon                = this,
                        explodes              = altFire.explodes,
                        damageAmount          = altFire.DamageAmount,
                        damageRadius          = altFire.DamageRadius,
                        explosionradiusmod    = altFire.ExplosionRadiusVisual,
                        Health                = altFire.HitPoints,
                        speed                 = altFire.ProjectileSpeed,
                        WeaponEffectType      = altFire.WeaponEffectType,
                        WeaponType            = altFire.WeaponType,
                        RotationRadsPerSecond = altFire.RotationRadsPerSecond,
                        ArmorPiercing         = (byte)altFire.ArmourPen,
                    };
                //damage increase by level
                if (Owner.Level > 0)
                {
                    projectile.damageAmount += projectile.damageAmount * Owner.Level * 0.05f;
                }
                if (altFire.RangeVariance)
                {
                    projectile.range *= RandomMath.RandomBetween(0.9f, 1.1f);
                }
                //Hull bonus damage increase
				if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.useHullBonuses)
                {
                    if (ResourceManager.HullBonuses.TryGetValue(Owner.shipData.Hull, out HullBonus mod))
                        projectile.damageAmount += projectile.damageAmount * mod.DamageBonus;
                }
                projectile.LoadContent(altFire.ProjectileTexturePath, altFire.ModelPath);
                altFire.ModifyProjectile(projectile);
                if (altFire.Tag_Guided)
                    projectile.InitializeMissile(projectile.speed, direction, shipModule);
                else
                    projectile.Initialize(projectile.speed, direction, moduleAttachedTo.Center);
                projectile.Radius = ProjectileRadius;
                if (altFire.Animated == 1)
                {
                    string remainder = 0.ToString("00000.##");
                    projectile.texturePath = string.Concat(altFire.AnimationPath, remainder);
                }
                if (Empire.Universe.viewState == UniverseScreen.UnivScreenState.ShipView && Owner.InFrustum && playSound)
                {
                    projectile.DieSound = true;
                    if (!string.IsNullOrEmpty(ToggleSoundName) && (ToggleCue == null || ToggleCue != null && !ToggleCue.IsPlaying))
                    {
                        ToggleSoundOn = true;
                        ToggleCue = AudioManager.GetCue(altFire.ToggleSoundName);
                        ToggleCue.Apply3D(audioListener, Owner.emitter);
                        ToggleCue.Play();
                        PlayFireCue(altFire.fireCueName, Owner.emitter, !Owner.isPlayerShip());
                    }
                    if (!string.IsNullOrEmpty(ResourceManager.WeaponsDict[altFire.UID].dieCue))
                    {
                        projectile.dieCueName = ResourceManager.WeaponsDict[altFire.UID].dieCue;
                    }
                    if (!string.IsNullOrEmpty(InFlightCue))
                    {
                        projectile.InFlightCue = altFire.InFlightCue;
                    }
                    if (ToggleCue == null && Owner.ProjectilesFired.Count < 30)
                    {
                        Owner.ProjectilesFired.Add(new ProjectileTracker());
                        PlayFireCue(altFire.fireCueName, Owner.emitter, !Owner.isPlayerShip());
                    }
                }
                projectile.isSecondary = true;
                Owner.Projectiles.Add(projectile);
            }
            else
            {
                Projectile projectile;
                if (Owner.Projectiles.TryReuseItem(out projectile))
                {
                    projectile.ProjectileRecreate(Owner, direction, moduleAttachedTo);
                    projectile.range = Range;
                    projectile.weapon = this;
                    projectile.explodes = explodes;
                    projectile.damageAmount = DamageAmount;
                    projectile.damageRadius = DamageRadius;
                    projectile.explosionradiusmod = ExplosionRadiusVisual;
                    projectile.Health = HitPoints;
                    projectile.speed = ProjectileSpeed;
                    projectile.WeaponEffectType = WeaponEffectType;
                    projectile.WeaponType = WeaponType;
                    projectile.RotationRadsPerSecond = RotationRadsPerSecond;
                    projectile.ArmorPiercing = (byte)ArmourPen;
                }
                projectile = new Projectile(Owner, direction, moduleAttachedTo)
                {
                    range = Range,
                    weapon = this,
                    explodes = explodes,
                    damageAmount = DamageAmount,
                    damageRadius = DamageRadius,
                    explosionradiusmod = ExplosionRadiusVisual,
                    Health = HitPoints,
                    speed = ProjectileSpeed,
                    WeaponEffectType = WeaponEffectType,
                    WeaponType = WeaponType,
                    RotationRadsPerSecond = RotationRadsPerSecond,
                    ArmorPiercing = (byte)ArmourPen,
                };
                //damage increase by level
                if (Owner.Level > 0)
                {
                    projectile.damageAmount += projectile.damageAmount * (float)Owner.Level * 0.05f;
                }
                if (RangeVariance)
                {
                    projectile.range *= RandomMath.RandomBetween(0.9f, 1.1f);
                }
                //Hull bonus damage increase
				if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses)
                {
                    HullBonus mod;
                    if (ResourceManager.HullBonuses.TryGetValue(Owner.shipData.Hull, out mod))
                        projectile.damageAmount += projectile.damageAmount * mod.DamageBonus;
                }
                projectile.LoadContent(ProjectileTexturePath, ModelPath);
                ModifyProjectile(projectile);
                if (Tag_Guided)
                    projectile.InitializeMissile(projectile.speed, direction, target);
                else
                    projectile.Initialize(projectile.speed, direction, moduleAttachedTo.Center);
                projectile.Radius = ProjectileRadius;
                if (Animated == 1)
                {
                    string remainder = 0.ToString("00000.##");
                    projectile.texturePath = string.Concat(AnimationPath, remainder);
                }
                if (Empire.Universe.viewState == UniverseScreen.UnivScreenState.ShipView && Owner.InFrustum && playSound)
                {
                    projectile.DieSound = true;
                    if (!string.IsNullOrEmpty(ToggleSoundName) && (ToggleCue == null || ToggleCue != null && !ToggleCue.IsPlaying))
                    {
                        ToggleSoundOn = true;
                        ToggleCue = AudioManager.GetCue(ToggleSoundName);
                        ToggleCue.Apply3D(audioListener, Owner.emitter);
                        ToggleCue.Play();
                        PlayFireCue(fireCueName, Owner.emitter, !Owner.isPlayerShip());
                    }
                    if (!string.IsNullOrEmpty(ResourceManager.WeaponsDict[UID].dieCue))
                    {
                        projectile.dieCueName = ResourceManager.WeaponsDict[UID].dieCue;
                    }
                    if (!string.IsNullOrEmpty(InFlightCue))
                    {
                        projectile.InFlightCue = InFlightCue;
                    }
                    if (ToggleCue == null && Owner.ProjectilesFired.Count < 30)
                    {
                        // @todo This is horrible, we must remove it ASAP
                        //  seems to be used for calculating "Projectiles Fired Per Second" or something?
                        Owner.ProjectilesFired.Add(new ProjectileTracker());
                        PlayFireCue(fireCueName, Owner.emitter, !Owner.isPlayerShip());
                    }
                }
                Owner.Projectiles.Add(projectile);
            }
		}

        // Use sounds from new sound dictionary
        private void PlayFireCue(string cueName, AudioEmitter emitter, bool apply3D = true)
        {
            if (ResourceManager.SoundEffectDict.TryGetValue(cueName, out SoundEffect sfx))
            {
                AudioManager.PlaySoundEffect(sfx, audioListener, emitter, 0.5f);
            }
            else if (cueName.NotEmpty() && AudioManager.LimitOk)
            {
                fireCue = AudioManager.GetCue(cueName);
                if (apply3D)
                    fireCue?.Apply3D(audioListener, emitter);
                fireCue?.Play();
            }
        }

        protected virtual void CreateProjectilesFromPlanet(Vector2 direction, Planet p, GameplayObject target)
		{
			var projectile = new Projectile(p, direction)
			{
				range = Range,
				weapon = this,
				explodes = explodes,
				damageAmount = DamageAmount
            };
            if (RangeVariance)
            {
                projectile.range *= RandomMath.RandomBetween(0.9f, 1.1f);
            }
			projectile.explodes              = explodes;
			projectile.damageRadius          = DamageRadius;
            projectile.explosionradiusmod    = ExplosionRadiusVisual;
            projectile.Health                = HitPoints;
			projectile.speed                 = ProjectileSpeed;
			projectile.WeaponEffectType      = WeaponEffectType;
			projectile.WeaponType            = WeaponType;
			projectile.LoadContent(ProjectileTexturePath, ModelPath);
			projectile.RotationRadsPerSecond = RotationRadsPerSecond;
            projectile.ArmorPiercing         = (byte)ArmourPen;

            ModifyProjectile(projectile);
            if (Tag_Guided) projectile.InitializeMissilePlanet(projectile.speed, direction, target, p);
            else            projectile.InitializePlanet(projectile.speed, direction, p.Position);
			projectile.Radius = ProjectileRadius;
            if (Animated == 1)
            {
                projectile.texturePath = AnimationPath + 0.ToString("00000.##");
            }
			p.Projectiles.Add(projectile);
            planetEmitter = new AudioEmitter()
			{
				Position = new Vector3(p.Position, 2500f)
			};
			if (Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
			{
				projectile.DieSound = true;
				if (!string.IsNullOrEmpty(ToggleSoundName) && !ToggleSoundOn)
				{
                    ToggleSoundOn = true;
                    ToggleCue = AudioManager.GetCue(ToggleSoundName);
                    ToggleCue.Apply3D(audioListener, planetEmitter);
                    ToggleCue.Play();
                    PlayFireCue(fireCueName, planetEmitter);
				}
				if (!string.IsNullOrEmpty(ResourceManager.WeaponsDict[UID].dieCue))
				{
					projectile.dieCueName = ResourceManager.WeaponsDict[UID].dieCue;
				}
				if (!string.IsNullOrEmpty(InFlightCue))
				{
					projectile.InFlightCue = InFlightCue;
				}
				if (ToggleCue == null)
				{
                    planetEmitter.Position = new Vector3(p.Position, -2500f);
                    PlayFireCue(fireCueName, planetEmitter);
				}
			}
		}

		public virtual void Fire(Vector2 direction, GameplayObject target)
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
					float timeBetweenShots = SalvoTimer / SalvoCount;
					for (int j = 1; j < SalvoCount; ++j)
					{
						var sal = new Salvo
						{
							Timing    = j * timeBetweenShots,
                            Direction = direction
						};
						SalvoList.Add(sal);
					}
                    SalvoTarget = target;
				}
			}

		}

		public virtual void FireDrone(Vector2 direction)
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

		public virtual void FireDroneBeam(Vector2 direction, GameplayObject target, DroneAI source)
		{
            drowner = source.Owner;
			if (timeToNextFire > 0f)
			{
				return;
			}
            timeToNextFire = fireDelay;
            CreateDroneBeam(direction, target, source);
		}

        public virtual void FireFromPlanet(Vector2 direction, Planet p, GameplayObject target)
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

		public virtual void FireSalvo(Vector2 direction, GameplayObject target)
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

		public virtual void FireTargetedBeam(GameplayObject target)
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

        public virtual void FireMouseBeam(Vector2 direction)
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
            float degreesBetweenShots = FireArc / (float)ProjectileCount;
            float angleToTarget = direction.ToDegrees() - FireArc * 0.5f;
            for (int i = 0; i < ProjectileCount; ++i)
            {
                Vector2 dir = angleToTarget.AngleToDirection();
                angleToTarget += degreesBetweenShots;
                yield return dir;
            }
        }

        public virtual void FireMouse(Vector2 direction)
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
                    float timeBetweenShots = SalvoTimer / SalvoCount;
                    for (int j = 1; j < SalvoCount; j++)
                    {
                        var sal = new Salvo
                        {
                            Timing = j * timeBetweenShots,
                            Direction = direction
                        };
                        SalvoList.Add(sal);
                    }
                }
            }
        }

		public Ship GetOwner()
		{
			return Owner;
		}

		public Projectile LoadProjectiles(Vector2 direction, Ship owner)
		{
			Projectile projectile = new Projectile(owner, direction)
			{
				range = Range,
				weapon = this,
				explodes = explodes,
				damageAmount = DamageAmount
            };
			projectile.explodes = explodes;
			projectile.damageRadius = DamageRadius;
            projectile.explosionradiusmod = ExplosionRadiusVisual;
			projectile.speed = ProjectileSpeed;
			projectile.WeaponEffectType = WeaponEffectType;
			projectile.WeaponType = WeaponType;
			projectile.Initialize(ProjectileSpeed, direction, owner.Center);
			projectile.Radius = ProjectileRadius;
			projectile.LoadContent(ProjectileTexturePath, ModelPath);
			if (owner.System != null && owner.System.isVisible || owner.isInDeepSpace)
			{
				projectile.DieSound = true;
				if (!string.IsNullOrEmpty(ResourceManager.WeaponsDict[UID].dieCue))
					projectile.dieCueName = ResourceManager.WeaponsDict[UID].dieCue;
				if (!string.IsNullOrEmpty(InFlightCue))
					projectile.InFlightCue = InFlightCue;
			}
			return projectile;
		}

		private void ModifyProjectile(Projectile projectile)
		{
            if (Owner == null)
			{
				return;
			}
            if (Owner.loyalty.data.Traits.Pack)
            {
                projectile.damageAmount += projectile.damageAmount * Owner.DamageModifier;
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

		public void ResetToggleSound()
		{
			if (ToggleCue != null)
			{
                ToggleCue.Stop(AudioStopOptions.Immediate);
                ToggleCue = null;
			}
            ToggleSoundOn = false;
		}

		public void SetOwner(Ship newOwner)
		{
			Owner = newOwner;
		}

		public virtual void Update(float elapsedTime)
		{
			if (timeToNextFire > 0f)
			{
                if (WeaponType != "Drone") timeToNextFire = MathHelper.Max(timeToNextFire - elapsedTime, 0f);
                //Gretman -- To fix broken Repair Drones, I moved updating the cooldown for drone weapons to the ArtificialIntelligence update function.
            }
			foreach (Salvo salvo in SalvoList)
			{
                salvo.Timing -= elapsedTime;
				if (salvo.Timing > 0f)
					continue;
                
                if (SalvoTarget != null && Owner.CheckIfInsideFireArc(this,SalvoTarget))
                {
                    if (Tag_Guided)
                        FireSalvo(salvo.Direction, SalvoTarget);
                    else
                        GetOwner().GetAI().CalculateAndFire(this, SalvoTarget, true);
                }
                else if (SalvoTarget != null)
                    FireSalvo(salvo.Direction, null);
                SalvoList.QueuePendingRemoval(salvo);
			}
            SalvoList.ApplyPendingRemovals();
            if (SalvoList.Count == 0)
                SalvoTarget = null;
            Center = moduleAttachedTo.Center;
		}

		private class Salvo
		{
			public float Timing;
            public Vector2 Direction;
		}

        public float GetModifiedRange()
        {
			if (GetOwner() == null || GlobalStats.ActiveModInfo == null || !GlobalStats.ActiveModInfo.useWeaponModifiers)
                return Range;
            float modifiedRange = Range;
            EmpireData loyaltyData = GetOwner().loyalty.data;
            if (Tag_Beam)      modifiedRange += Range * loyaltyData.WeaponTags["Beam"].Range;
            if (Tag_Energy)    modifiedRange += Range * loyaltyData.WeaponTags["Energy"].Range;
            if (Tag_Explosive) modifiedRange += Range * loyaltyData.WeaponTags["Explosive"].Range;
            if (Tag_Guided)    modifiedRange += Range * loyaltyData.WeaponTags["Guided"].Range;
            if (Tag_Hybrid)    modifiedRange += Range * loyaltyData.WeaponTags["Hybrid"].Range;
            if (Tag_Intercept) modifiedRange += Range * loyaltyData.WeaponTags["Intercept"].Range;
            if (Tag_Kinetic)   modifiedRange += Range * loyaltyData.WeaponTags["Kinetic"].Range;
            if (Tag_Missile)   modifiedRange += Range * loyaltyData.WeaponTags["Missile"].Range;
            if (Tag_Railgun)   modifiedRange += Range * loyaltyData.WeaponTags["Railgun"].Range;
            if (Tag_Cannon)    modifiedRange += Range * loyaltyData.WeaponTags["Cannon"].Range;
            if (Tag_PD)        modifiedRange += Range * loyaltyData.WeaponTags["PD"].Range;
            if (Tag_SpaceBomb) modifiedRange += Range * loyaltyData.WeaponTags["Spacebomb"].Range;
            if (Tag_BioWeapon) modifiedRange += Range * loyaltyData.WeaponTags["BioWeapon"].Range;
            if (Tag_Drone)     modifiedRange += Range * loyaltyData.WeaponTags["Drone"].Range;
            if (Tag_Subspace)  modifiedRange += Range * loyaltyData.WeaponTags["Subspace"].Range;
            if (Tag_Warp)      modifiedRange += Range * loyaltyData.WeaponTags["Warp"].Range;
            if (Tag_Array)     modifiedRange += Range * loyaltyData.WeaponTags["Array"].Range;
            if (Tag_Flak)      modifiedRange += Range * loyaltyData.WeaponTags["Flak"].Range;
            if (Tag_Tractor)   modifiedRange += Range * loyaltyData.WeaponTags["Tractor"].Range;
            return modifiedRange;            
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Weapon() { Dispose(false); }

        protected virtual void Dispose(bool disposing)
        {
            SalvoList?.Dispose(ref SalvoList);
        }
    }

    public sealed class ProjectileTracker
    {
        public float Timer = 1f;
    }

}
