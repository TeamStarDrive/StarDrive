using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Ship_Game;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ship_Game.Gameplay
{
	public class Weapon
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

        // Added by The Doctor: new tags for categorisation for mods who otherwise have large numbers of items in one Shipyard menu node, also new modifiers.

        public bool Tag_Flak;

        public bool Tag_Array;

        public bool Tag_Tractor;

        // End

		private Ship owner;

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

		//private float toggleTimer;

		public bool IsRepairDrone;

		private BatchRemovalCollection<Weapon.Salvo> SalvoList = new BatchRemovalCollection<Weapon.Salvo>();

		public bool FakeExplode;

		public float ProjectileRadius = 4f;

		public string Name;

		public byte LoopAnimation;

		public float Scale = 1f;

		private float lastFireSound;

		public static UniverseScreen universeScreen;

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

        public bool ExplosionFlash;

        public bool RangeVariance;


        public GameplayObject SalvoTarget = null;
        public float ExplosionRadiusVisual = 4.5f;

		public static AudioListener audioListener
		{
			get;
			set;
		}

		public Weapon(Ship owner, ShipModule moduleAttachedTo)
		{
			this.owner = owner;
			this.moduleAttachedTo = moduleAttachedTo;
		}

		public Weapon()
		{
		}

        private void AddModifiers(string Tag, Projectile projectile)
        {
            projectile.damageAmount += this.owner.loyalty.data.WeaponTags[Tag].Damage * projectile.damageAmount;
            projectile.ShieldDamageBonus += this.owner.loyalty.data.WeaponTags[Tag].ShieldDamage;
            projectile.ArmorDamageBonus += this.owner.loyalty.data.WeaponTags[Tag].ArmorDamage;
            //Shield Penetration
            float actualShieldPenChance = this.moduleAttachedTo.GetParent().loyalty.data.ShieldPenBonusChance;
            actualShieldPenChance += this.owner.loyalty.data.WeaponTags[Tag].ShieldPenetration;
            actualShieldPenChance += this.ShieldPenChance;
            if (actualShieldPenChance > 0f && (float)((int)RandomMath2.RandomBetween(0f, 100f)) < actualShieldPenChance)
            {
                projectile.IgnoresShields = true;
            }
            //Projectile specific
            if (!this.isBeam)
            {
                projectile.ArmorPiercing += (byte)this.owner.loyalty.data.WeaponTags[Tag].ArmourPenetration;
                projectile.Health += this.HitPoints * this.owner.loyalty.data.WeaponTags[Tag].HitPoints;
                projectile.RotationRadsPerSecond += this.owner.loyalty.data.WeaponTags[Tag].Turn * this.RotationRadsPerSecond;
                projectile.speed += this.owner.loyalty.data.WeaponTags[Tag].Speed * this.ProjectileSpeed;
                projectile.damageRadius += this.owner.loyalty.data.WeaponTags[Tag].ExplosionRadius * this.DamageRadius;
            }
        }

		protected virtual void CreateDrone(Vector2 direction)
		{
			Projectile projectile = new Projectile(this.owner, direction, this.moduleAttachedTo)
			{
				range = this.Range,
				weapon = this,
				explodes = this.explodes,
				damageAmount = this.DamageAmount
			};
			projectile.explodes = this.explodes;
			projectile.damageRadius = this.DamageRadius;
            projectile.explosionradiusmod = this.ExplosionRadiusVisual;
			projectile.speed = this.ProjectileSpeed;
			projectile.Health = this.HitPoints;
			projectile.WeaponEffectType = this.WeaponEffectType;
			projectile.WeaponType = this.WeaponType;
			projectile.LoadContent(this.ProjectileTexturePath, this.ModelPath);
			projectile.RotationRadsPerSecond = this.RotationRadsPerSecond;
			this.ModifyProjectile(projectile);
			projectile.InitializeDrone(projectile.speed, direction);
			projectile.Radius = this.ProjectileRadius;
			this.owner.Projectiles.Add(projectile);
			if (this.owner.InFrustum)
			{
				projectile.DieSound = true;
				if (this.ToggleSoundName != "" && !this.ToggleSoundOn)
				{
					this.ToggleSoundOn = true;
					this.ToggleCue = AudioManager.GetCue(this.ToggleSoundName);
					this.ToggleCue.Apply3D(Weapon.audioListener, this.owner.emitter);
					this.ToggleCue.Play();
					this.fireCue = AudioManager.GetCue(this.fireCueName);
					if (!this.owner.isPlayerShip())
					{
						this.fireCue.Apply3D(Weapon.audioListener, this.owner.emitter);
					}
					this.lastFireSound = 0f;
					if (this.fireCue != null)
					{
						this.fireCue.Play();
					}
				}
				if (!string.IsNullOrEmpty(ResourceManager.WeaponsDict[this.UID].dieCue))
				{
					projectile.dieCueName = ResourceManager.WeaponsDict[this.UID].dieCue;
				}
				if (this.InFlightCue != "")
				{
					projectile.InFlightCue = this.InFlightCue;
				}
				if (this.ToggleCue == null)
				{
					this.fireCue = AudioManager.GetCue(this.fireCueName);
					if (!this.owner.isPlayerShip())
					{
						this.fireCue.Apply3D(Weapon.audioListener, this.owner.emitter);
					}
					this.lastFireSound = 0f;
					if (this.fireCue != null)
					{
						this.fireCue.Play();
					}
				}
			}
		}

		protected virtual void CreateDroneBeam(Vector2 destination, GameplayObject target, DroneAI source)
		{
			Beam beam = new Beam(source.Owner.Center, target.Center, this.BeamThickness, source.Owner, target);
			beam.moduleAttachedTo = this.moduleAttachedTo;
			beam.PowerCost = (float)this.BeamPowerCostPerSecond;
			beam.range = this.Range;
			beam.thickness = this.BeamThickness;
            beam.Duration = (float)this.BeamDuration > 0 ? this.BeamDuration : 2f;
			beam.damageAmount = this.DamageAmount;
			beam.weapon = this;
			source.Beams.Add(beam);
			beam.LoadContent(Weapon.universeScreen.ScreenManager, Weapon.universeScreen.view, Weapon.universeScreen.projection);
			this.ToggleSoundOn = false;
			if (Weapon.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
			{
                //Added by McShooterz: Use sounds from new sound dictionary
                if (ResourceManager.SoundEffectDict.ContainsKey(this.fireCueName))
                {
                    AudioManager.PlaySoundEffect(ResourceManager.SoundEffectDict[fireCueName], Weapon.audioListener, source.Owner.emitter, 0.5f);
                }
                else
                {
                    if (this.fireCueName != "")
                    {
                        this.fireCue = AudioManager.GetCue(this.fireCueName);
                        this.fireCue.Apply3D(Weapon.audioListener, source.Owner.emitter);
                        this.fireCue.Play();
                    }
                }
				if (this.ToggleSoundName != "")
				{
					this.ToggleSoundOn = true;
					this.ToggleCue = AudioManager.GetCue(this.ToggleSoundName);
					this.ToggleCue.Apply3D(Weapon.audioListener, source.Owner.emitter);
					this.ToggleCue.Play();
				}
			}
		}

        protected virtual void CreateTargetedBeam(GameplayObject target)
        {
            Beam beam = new Beam(this.moduleAttachedTo.Center, this.BeamThickness, this.moduleAttachedTo.GetParent(), target)
            {
                moduleAttachedTo = this.moduleAttachedTo,
                PowerCost = (float)this.BeamPowerCostPerSecond,
                range = this.Range,
                thickness = this.BeamThickness,
                Duration = (float)this.BeamDuration > 0 ? this.BeamDuration : 2f,
                damageAmount = this.DamageAmount,
                weapon = this
            };
            //damage increase by level
            if (this.owner.Level > 0)
            {
                beam.damageAmount += beam.damageAmount * (float)this.owner.Level * 0.05f;
            }
            //Hull bonus damage increase
            if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.useHullBonuses)
            {
                HullBonus mod;
                if (ResourceManager.HullBonuses.TryGetValue(this.owner.shipData.Hull, out mod))
                    beam.damageAmount += beam.damageAmount * mod.DamageBonus;
            }
            this.ModifyProjectile(beam);
            this.moduleAttachedTo.GetParent().Beams.Add(beam);
            beam.LoadContent(Weapon.universeScreen.ScreenManager, Weapon.universeScreen.view, Weapon.universeScreen.projection);
            this.ToggleSoundOn = false;
            if (Weapon.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView && this.moduleAttachedTo.GetParent().InFrustum)
            {
                //Added by McShooterz: Use sounds from new sound dictionary
                if (ResourceManager.SoundEffectDict.ContainsKey(this.fireCueName))
                {
                    AudioManager.PlaySoundEffect(ResourceManager.SoundEffectDict[fireCueName], Weapon.audioListener, this.owner.emitter, 0.5f);
                }
                else
                {
                    if (this.fireCueName != "")
                    {
                        this.fireCue = AudioManager.GetCue(this.fireCueName);
                        if (!this.owner.isPlayerShip())
                        {
                            this.fireCue.Apply3D(Weapon.audioListener, this.owner.emitter);
                        }
                        this.fireCue.Play();
                    }
                }
                if (this.ToggleSoundName != "")
                {
                    this.ToggleSoundOn = true;
                    this.ToggleCue = AudioManager.GetCue(this.ToggleSoundName);
                    this.ToggleCue.Apply3D(Weapon.audioListener, this.owner.emitter);
                    this.ToggleCue.Play();
                }
            }
        }

		protected virtual void CreateMouseBeam(Vector2 destination)
		{
			Beam beam = new Beam(this.moduleAttachedTo.Center, destination, this.BeamThickness, this.moduleAttachedTo.GetParent())
			{
				moduleAttachedTo = this.moduleAttachedTo,
				range = this.Range,
				followMouse = true,
				thickness = this.BeamThickness,
                Duration = (float)this.BeamDuration > 0 ? this.BeamDuration : 2f,
				PowerCost = (float)this.BeamPowerCostPerSecond,
				damageAmount = this.DamageAmount,
				weapon = this
			};
			this.moduleAttachedTo.GetParent().Beams.Add(beam);
			beam.LoadContent(Weapon.universeScreen.ScreenManager, Weapon.universeScreen.view, Weapon.universeScreen.projection);
			this.ToggleSoundOn = false;
			if ((this.owner.GetSystem() != null && this.owner.GetSystem().isVisible || this.owner.isInDeepSpace) && Weapon.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
			{
                //Added by McShooterz: Use sounds from new sound dictionary
                if (ResourceManager.SoundEffectDict.ContainsKey(this.fireCueName))
                {
                    AudioManager.PlaySoundEffect(ResourceManager.SoundEffectDict[fireCueName], Weapon.audioListener, this.owner.emitter, 0.5f);
                }
                else
                {
                    if (this.fireCueName != "")
                    {
                        this.fireCue = AudioManager.GetCue(this.fireCueName);
                        if (!this.owner.isPlayerShip())
                        {
                            this.fireCue.Apply3D(Weapon.audioListener, this.owner.emitter);
                        }
                        this.fireCue.Play();
                    }
                }
                if (this.ToggleSoundName != "" && !this.ToggleSoundOn)
                {
                    this.ToggleSoundOn = true;
                    this.ToggleCue = AudioManager.GetCue(this.ToggleSoundName);
                    this.ToggleCue.Apply3D(Weapon.audioListener, this.owner.emitter);
                    this.ToggleCue.Play();
                }
			}
		}

		protected virtual void CreateProjectiles(Vector2 direction, GameplayObject target, bool playSound)
		{
			Projectile projectile = new Projectile(this.owner, direction, this.moduleAttachedTo)
			{
				range = this.Range,
				weapon = this,
				explodes = this.explodes,
				damageAmount = this.DamageAmount,
                damageRadius = this.DamageRadius,
                explosionradiusmod = this.ExplosionRadiusVisual,
                Health = this.HitPoints,
                speed = this.ProjectileSpeed,
                WeaponEffectType = this.WeaponEffectType,
                WeaponType = this.WeaponType,
                RotationRadsPerSecond = this.RotationRadsPerSecond,
                ArmorPiercing = (byte)this.ArmourPen,
                flashExplode = this.ExplosionFlash
			};
            if (this.RangeVariance)
            {
                projectile.range *= RandomMath.RandomBetween(0.9f, 1.1f);
            }
            //damage increase by level
			if (this.owner.Level > 0)
			{
                projectile.damageAmount += projectile.damageAmount * (float)this.owner.Level * 0.05f;
			}
            //Hull bonus damage increase
            if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.useHullBonuses)
            {
                HullBonus mod;
                if(ResourceManager.HullBonuses.TryGetValue(this.owner.shipData.Hull, out mod))
                    projectile.damageAmount += projectile.damageAmount * mod.DamageBonus;
            }
			projectile.LoadContent(this.ProjectileTexturePath, this.ModelPath);
			this.ModifyProjectile(projectile);
            if(this.Tag_Guided)
                projectile.InitializeMissile(projectile.speed, direction, target);
            else
			    projectile.Initialize(projectile.speed, direction, this.moduleAttachedTo.Center);
			projectile.Radius = this.ProjectileRadius;
			if (this.Animated == 1)
			{
				string remainder = 0.ToString("00000.##");
				projectile.texturePath = string.Concat(this.AnimationPath, remainder);
			}
            if (Weapon.universeScreen.viewState == UniverseScreen.UnivScreenState.ShipView && this.owner.InFrustum && playSound)
			{
				projectile.DieSound = true;
				if (this.ToggleSoundName != "" && (this.ToggleCue == null || this.ToggleCue != null && !this.ToggleCue.IsPlaying))
				{
					this.ToggleSoundOn = true;
					this.ToggleCue = AudioManager.GetCue(this.ToggleSoundName);
					this.ToggleCue.Apply3D(Weapon.audioListener, this.owner.emitter);
					this.ToggleCue.Play();
					if (this.fireCue != null)
					{
                        //Added by McShooterz: Use sounds from new sound dictionary
                        if (ResourceManager.SoundEffectDict.ContainsKey(this.fireCueName))
                        {
                            AudioManager.PlaySoundEffect(ResourceManager.SoundEffectDict[fireCueName], Weapon.audioListener, this.owner.emitter, 0.5f);
                        }
                        else
                        {
                            this.fireCue = AudioManager.GetCue(this.fireCueName);
                            if (!this.owner.isPlayerShip())
                            {
                                this.fireCue.Apply3D(Weapon.audioListener, this.owner.emitter);
                            }
                            this.lastFireSound = 0f;
                            if (this.fireCue != null)
                            {
                                this.fireCue.Play();
                            }
                        }
					}
				}
				if (!string.IsNullOrEmpty(ResourceManager.WeaponsDict[this.UID].dieCue))
				{
					projectile.dieCueName = ResourceManager.WeaponsDict[this.UID].dieCue;
				}
				if (this.InFlightCue != "")
				{
					projectile.InFlightCue = this.InFlightCue;
				}
				if (this.ToggleCue == null && this.owner.ProjectilesFired.Count < 30)
				{
                    this.lastFireSound = 0f;
					this.owner.ProjectilesFired.Add(new ProjectileTracker());
                    //Added by McShooterz: Use sounds from new sound dictionary
                    if (ResourceManager.SoundEffectDict.ContainsKey(this.fireCueName))
                    {
                        AudioManager.PlaySoundEffect(ResourceManager.SoundEffectDict[fireCueName], Weapon.audioListener, this.owner.emitter, 0.5f);
                    }
                    else
                    {
                        this.fireCue = AudioManager.GetCue(this.fireCueName);
                        if (!this.owner.isPlayerShip())
                        {
                            this.fireCue.Apply3D(Weapon.audioListener, this.owner.emitter);
                        }
                        if (this.fireCue != null)
                        {
                            this.fireCue.Play();
                        }
                    }
				}
			}
			this.owner.Projectiles.Add(projectile);
			projectile = null;
		}

		protected virtual void CreateProjectilesFromPlanet(Vector2 direction, Planet p, GameplayObject target)
		{
			Projectile projectile = new Projectile(p, direction)
			{
				range = this.Range,
				weapon = this,
				explodes = this.explodes,
				damageAmount = this.DamageAmount
			};
			projectile.explodes = this.explodes;
			projectile.damageRadius = this.DamageRadius;
            projectile.explosionradiusmod = this.ExplosionRadiusVisual;
            projectile.Health = this.HitPoints;
			projectile.speed = this.ProjectileSpeed;
			projectile.WeaponEffectType = this.WeaponEffectType;
			projectile.WeaponType = this.WeaponType;
			projectile.LoadContent(this.ProjectileTexturePath, this.ModelPath);
			projectile.RotationRadsPerSecond = this.RotationRadsPerSecond;
            projectile.ArmorPiercing = (byte)this.ArmourPen;
            /*
            if (this.ShieldPenChance > 0)
            {
                projectile.IgnoresShields = RandomMath.RandomBetween(0, 100) <= this.ShieldPenChance;
            } */
			this.ModifyProjectile(projectile);
            if(this.Tag_Guided)
                projectile.InitializeMissilePlanet(projectile.speed, direction, target, p);
            else
			    projectile.InitializePlanet(projectile.speed, direction, p.Position);
			projectile.Radius = this.ProjectileRadius;
            if (this.Animated == 1)
            {
                string remainder = 0.ToString("00000.##");
                projectile.texturePath = string.Concat(this.AnimationPath, remainder);
            }
			p.Projectiles.Add(projectile);
			this.planetEmitter = new AudioEmitter()
			{
				Position = new Vector3(p.Position, 2500f)
			};
			if (Weapon.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
			{
				projectile.DieSound = true;
				if (this.ToggleSoundName != "" && !this.ToggleSoundOn)
				{
					this.ToggleSoundOn = true;
					this.ToggleCue = AudioManager.GetCue(this.ToggleSoundName);
					this.ToggleCue.Apply3D(Weapon.audioListener, this.planetEmitter);
					this.ToggleCue.Play();
                    this.lastFireSound = 0f;
                    //Added by McShooterz: Use sounds from new sound dictionary
                    if (ResourceManager.SoundEffectDict.ContainsKey(this.fireCueName))
                    {
                        AudioManager.PlaySoundEffect(ResourceManager.SoundEffectDict[fireCueName], Weapon.audioListener, this.planetEmitter, 0.5f);
                    }
                    else
                    {
                        this.fireCue = AudioManager.GetCue(this.fireCueName);
                        if (!this.owner.isPlayerShip())
                        {
                            this.fireCue.Apply3D(Weapon.audioListener, this.planetEmitter);
                        }
                        if (this.fireCue != null)
                        {
                            this.fireCue.Play();
                        }
                    }
				}
				if (!string.IsNullOrEmpty(ResourceManager.WeaponsDict[this.UID].dieCue))
				{
					projectile.dieCueName = ResourceManager.WeaponsDict[this.UID].dieCue;
				}
				if (this.InFlightCue != "")
				{
					projectile.InFlightCue = this.InFlightCue;
				}
				try
				{
					if (this.ToggleCue == null)
					{
                        this.planetEmitter.Position = new Vector3(p.Position, -2500f);
                        this.lastFireSound = 0f;
                        //Added by McShooterz: Use sounds from new sound dictionary
                        if (ResourceManager.SoundEffectDict.ContainsKey(this.fireCueName))
                        {
                            AudioManager.PlaySoundEffect(ResourceManager.SoundEffectDict[fireCueName], Weapon.audioListener, this.planetEmitter, 0.5f);
                        }
                        else
                        {
                            this.fireCue = AudioManager.GetCue(this.fireCueName);
                            this.fireCue.Apply3D(Weapon.audioListener, this.planetEmitter);
                            if (this.fireCue != null)
                            {
                                this.fireCue.Play();
                            }
                        }
					}
				}
				catch
				{
				}
			}
		}

		private float findAngleToTarget(Vector2 target)
		{
			float theta;
			float tX = target.X;
			float tY = target.Y;
			float centerX = 0f;
			float centerY = 0f;
			float angle_to_target = 0f;
			if (tX > centerX && tY < centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				theta = theta * 180f / 3.14159274f;
				angle_to_target = 90f - Math.Abs(theta);
			}
			else if (tX > centerX && tY > centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				angle_to_target = 90f + theta * 180f / 3.14159274f;
			}
			else if (tX < centerX && tY > centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				theta = theta * 180f / 3.14159274f;
				angle_to_target = 270f - Math.Abs(theta);
			}
			else if (tX < centerX && tY < centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				angle_to_target = 270f + theta * 180f / 3.14159274f;
			}
			else if (tX == centerX && tY < centerY)
			{
				angle_to_target = 0f;
			}
			else if (tX == centerX && tY > centerY)
			{
				angle_to_target = 180f;
			}
			return angle_to_target;
		}

		private Vector2 findTargetFromAngleAndDistance(Vector2 position, float angle, float distance)
		{
			float theta;
			Vector2 TargetPosition = new Vector2(0f, 0f);
			float gamma = angle;
			float D = distance;
			int gammaQuadrant = 0;
			float oppY = 0f;
			float adjX = 0f;
			if (gamma > 360f)
			{
				gamma = gamma - 360f;
			}
			if (gamma < 90f)
			{
				theta = 90f - gamma;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 1;
			}
			else if (gamma > 90f && gamma < 180f)
			{
				theta = gamma - 90f;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 2;
			}
			else if (gamma > 180f && gamma < 270f)
			{
				theta = 270f - gamma;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 3;
			}
			else if (gamma > 270f && gamma < 360f)
			{
				theta = gamma - 270f;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 4;
			}
			if (gamma == 0f)
			{
				TargetPosition.X = position.X;
				TargetPosition.Y = position.Y - D;
			}
			if (gamma == 90f)
			{
				TargetPosition.X = position.X + D;
				TargetPosition.Y = position.Y;
			}
			if (gamma == 180f)
			{
				TargetPosition.X = position.X;
				TargetPosition.Y = position.Y + D;
			}
			if (gamma == 270f)
			{
				TargetPosition.X = position.X - D;
				TargetPosition.Y = position.Y;
			}
			if (gammaQuadrant == 1)
			{
				TargetPosition.X = position.X + adjX;
				TargetPosition.Y = position.Y - oppY;
			}
			else if (gammaQuadrant == 2)
			{
				TargetPosition.X = position.X + adjX;
				TargetPosition.Y = position.Y + oppY;
			}
			else if (gammaQuadrant == 3)
			{
				TargetPosition.X = position.X - adjX;
				TargetPosition.Y = position.Y + oppY;
			}
			else if (gammaQuadrant == 4)
			{
				TargetPosition.X = position.X - adjX;
				TargetPosition.Y = position.Y - oppY;
			}
			return TargetPosition;
		}

		private Vector2 findVectorToTarget(Vector2 OwnerPos, Vector2 TargetPos)
		{
			Vector2 Vec2Target = new Vector2(0f, 0f)
			{
				X = -(OwnerPos.X - TargetPos.X),
				Y = OwnerPos.Y - TargetPos.Y
			};
			return Vec2Target;
		}

		public virtual void Fire(Vector2 direction, GameplayObject target)
		{
            if (this.owner.engineState == Ship.MoveState.Warp || this.timeToNextFire > 0f)
				return;
			this.owner.InCombatTimer = 15f;
            if (target is ShipModule)
                (target as ShipModule).GetParent().InCombatTimer = 15f;
			this.timeToNextFire = this.fireDelay;
			if (this.moduleAttachedTo.Active && this.owner.PowerCurrent > this.PowerRequiredToFire && this.OrdinanceRequiredToFire <= this.owner.Ordinance)
			{
                this.owner.Ordinance -= this.OrdinanceRequiredToFire;
                this.owner.PowerCurrent -= this.PowerRequiredToFire;
				if (this.SalvoCount == 1)
				{
					if (this.FireArc != 0)
					{
						float DegreesBetweenShots = (float)(this.FireArc / this.ProjectileCount);
                        float angleToTarget = this.findAngleToTarget(direction);
						for (int i = 0; i < this.ProjectileCount; i++)
						{
							Vector2 newTarget = this.findTargetFromAngleAndDistance(this.moduleAttachedTo.Center, angleToTarget - (float)(this.FireArc / 2) + DegreesBetweenShots * (float)i, this.Range);
							Vector2 fireDirection = this.findVectorToTarget(this.moduleAttachedTo.Center, newTarget);
							fireDirection.Y = fireDirection.Y * -1f;
							this.CreateProjectiles(Vector2.Normalize(fireDirection), target, true);
						}
						return;
					}
					if (this.FireCone <= 0)
					{
						for (int i = 0; i < this.ProjectileCount; i++)
						{
                            this.CreateProjectiles(direction, target, true);
						}
						return;
					}
					float spread = RandomMath2.RandomBetween((float)(-this.FireCone / 2), (float)(this.FireCone / 2));
                    //renamed angletotarget,newtarget,firedirection
                    float angleToTarget2 = this.findAngleToTarget(direction);
					Vector2 newTarget2 = this.findTargetFromAngleAndDistance(this.moduleAttachedTo.Center, angleToTarget2 + spread, this.Range);
					Vector2 fireDirection2 = this.findVectorToTarget(this.moduleAttachedTo.Center, newTarget2);
					fireDirection2.Y = fireDirection2.Y * -1f;
					this.CreateProjectiles(Vector2.Normalize(fireDirection2), target, true);
					return;
				}
				if (this.SalvoCount > 1)
				{
					float TimeBetweenShots = this.SalvoTimer / (float)this.SalvoCount;
					for (int j = 1; j < this.SalvoCount; j++)
					{
						Weapon.Salvo sal = new Weapon.Salvo()
						{
							Timing = (float)j * TimeBetweenShots,
                            Direction = direction
						};
						this.SalvoList.Add(sal);
					}
                    this.SalvoTarget = target;
					if (this.FireArc != 0)
					{
						float DegreesBetweenShots = (float)(this.FireArc / this.ProjectileCount);
                        float angleToTarget = this.findAngleToTarget(direction);
						for (int i = 0; i < this.ProjectileCount; i++)
						{
							Vector2 newTarget = this.findTargetFromAngleAndDistance(this.moduleAttachedTo.Center, angleToTarget - (float)(this.FireArc / 2) + DegreesBetweenShots * (float)i, this.Range);
							Vector2 fireDirection = this.findVectorToTarget(this.moduleAttachedTo.Center, newTarget);
							fireDirection.Y = fireDirection.Y * -1f;
							this.CreateProjectiles(Vector2.Normalize(fireDirection), target, true);
						}
						return;
					}
					if (this.FireCone > 0)
					{
						float spread = RandomMath2.RandomBetween((float)(-this.FireCone / 2), (float)(this.FireCone / 2));
                        float angleToTarget = this.findAngleToTarget(direction);
						Vector2 newTarget = this.findTargetFromAngleAndDistance(this.moduleAttachedTo.Center, angleToTarget + spread, this.Range);
						Vector2 fireDirection = this.findVectorToTarget(this.moduleAttachedTo.Center, newTarget);
						fireDirection.Y = fireDirection.Y * -1f;
						this.CreateProjectiles(Vector2.Normalize(fireDirection), target, true);
						return;
					}
					for (int i = 0; i < this.ProjectileCount; i++)
					{
                        this.CreateProjectiles(direction, target, true);
					}
					return;
				}
			}
		}

		public virtual void FireDrone(Vector2 direction)
		{
			if (this.timeToNextFire > 0f)
			{
				return;
			}
			this.owner.InCombatTimer = 15f;
			this.timeToNextFire = this.fireDelay;
			if (this.moduleAttachedTo.Active && this.owner.PowerCurrent > this.PowerRequiredToFire && this.OrdinanceRequiredToFire <= this.owner.Ordinance)
			{
				Ship ordinance = this.owner;
				ordinance.Ordinance = ordinance.Ordinance - this.OrdinanceRequiredToFire;
				Ship powerCurrent = this.owner;
				powerCurrent.PowerCurrent = powerCurrent.PowerCurrent - this.PowerRequiredToFire;
				this.CreateDrone(Vector2.Normalize(direction));
			}
		}

		public virtual void FireDroneBeam(Vector2 direction, GameplayObject target, DroneAI source)
		{
			this.drowner = source.Owner;
			if (this.timeToNextFire > 0f)
			{
				return;
			}
			this.timeToNextFire = this.fireDelay;
			this.CreateDroneBeam(direction, target, source);
		}

        public virtual void FireFromPlanet(Vector2 direction, Planet p, GameplayObject target)
        {
            if (target is ShipModule)
                (target as ShipModule).GetParent().InCombatTimer = 15f;
            Vector2 StartPos = p.Position;
            if (this.FireArc != 0)
            {
                float DegreesBetweenShots = (float)(this.FireArc / this.ProjectileCount);
                float angleToTarget = this.findAngleToTarget(direction);
                for (int i = 0; i < this.ProjectileCount; i++)
                {
                    Vector2 newTarget = this.findTargetFromAngleAndDistance(StartPos, angleToTarget - (float)(this.FireArc / 2) + DegreesBetweenShots * (float)i, this.Range);
                    Vector2 fireDirection = this.findVectorToTarget(StartPos, newTarget);
                    fireDirection.Y = fireDirection.Y * -1f;
                    this.CreateProjectilesFromPlanet(Vector2.Normalize(fireDirection), p, target);
                }
                return;
            }
            if (this.FireCone <= 0)
            {
                if (!this.isBeam)
                {
                    for (int i = 0; i < this.ProjectileCount; i++)
                    {
                        if (this.WeaponType != "Missile")
                        {
                            this.CreateProjectilesFromPlanet(direction, p, target);
                        }
                        else
                        {
                            this.CreateProjectilesFromPlanet(Vector2.Normalize(direction), p, target);
                        }
                    }
                }
                return;
            }
            float spread = RandomMath2.RandomBetween((float)(-this.FireCone / 2), (float)(this.FireCone / 2));
            float angleToTarget2 = this.findAngleToTarget(direction);
            Vector2 newTarget2 = this.findTargetFromAngleAndDistance(StartPos, angleToTarget2 + spread, this.Range);
            Vector2 fireDirection2 = this.findVectorToTarget(StartPos, newTarget2);
            fireDirection2.Y = fireDirection2.Y * -1f;
            this.CreateProjectilesFromPlanet(Vector2.Normalize(fireDirection2), p, target);
        }

		public virtual void FireSalvo(Vector2 direction, GameplayObject target)
		{
			if (this.owner.engineState == Ship.MoveState.Warp)
			{
				return;
			}
			this.owner.InCombatTimer = 15f;
			if (this.moduleAttachedTo.Active && this.owner.PowerCurrent > this.PowerRequiredToFire && this.OrdinanceRequiredToFire <= this.owner.Ordinance)
			{
                this.owner.Ordinance -= this.OrdinanceRequiredToFire;
                this.owner.PowerCurrent -= this.PowerRequiredToFire;
				if (this.FireArc != 0)
				{
					float DegreesBetweenShots = (float)(this.FireArc / this.ProjectileCount);
                    float angleToTarget = this.findAngleToTarget(direction);
					for (int i = 0; i < this.ProjectileCount; i++)
					{
						Vector2 newTarget = this.findTargetFromAngleAndDistance(this.moduleAttachedTo.Center, angleToTarget - (float)(this.FireArc / 2) + DegreesBetweenShots * (float)i, this.Range);
						Vector2 fireDirection = this.findVectorToTarget(this.moduleAttachedTo.Center, newTarget);
						fireDirection.Y = fireDirection.Y * -1f;
                        this.CreateProjectiles(Vector2.Normalize(fireDirection), target, !this.PlaySoundOncePerSalvo);
					}
					return;
				}
				if (this.FireCone > 0)
				{
					float spread = RandomMath2.RandomBetween((float)(-this.FireCone / 2), (float)(this.FireCone / 2));
                    float angleToTarget = this.findAngleToTarget(direction);
					Vector2 newTarget = this.findTargetFromAngleAndDistance(this.moduleAttachedTo.Center, angleToTarget + spread, this.Range);
					Vector2 fireDirection = this.findVectorToTarget(this.moduleAttachedTo.Center, newTarget);
					fireDirection.Y = fireDirection.Y * -1f;
                    this.CreateProjectiles(Vector2.Normalize(fireDirection), target, !this.PlaySoundOncePerSalvo);
					return;
				}
				for (int i = 0; i < this.ProjectileCount; i++)
				{
                    this.CreateProjectiles(direction, target, !this.PlaySoundOncePerSalvo);
				}
				return;
			}
		}

		public virtual void FireTargetedBeam(GameplayObject target)
		{
			if (this.timeToNextFire > 0f)
			{
				return;
			}
			this.owner.InCombatTimer = 15f;
			this.timeToNextFire = this.fireDelay;
			if (this.moduleAttachedTo.Active && this.owner.PowerCurrent > this.PowerRequiredToFire && this.OrdinanceRequiredToFire <= this.owner.Ordinance)
			{
                this.owner.Ordinance -= this.OrdinanceRequiredToFire;
                this.owner.PowerCurrent -= this.PowerRequiredToFire;
				this.CreateTargetedBeam(target);
			}
		}

        public virtual void FireMouseBeam(Vector2 direction)
        {
            if (this.timeToNextFire > 0f)
            {
                return;
            }
            this.owner.InCombatTimer = 15f;
            this.timeToNextFire = this.fireDelay;
            if (this.moduleAttachedTo.Active && this.owner.PowerCurrent > this.PowerRequiredToFire && this.OrdinanceRequiredToFire <= this.owner.Ordinance)
            {
                this.owner.Ordinance -= this.OrdinanceRequiredToFire;
                this.owner.PowerCurrent -= this.PowerRequiredToFire;
                this.CreateMouseBeam(direction);
            }
        }

        public virtual void FireMouse(Vector2 direction)
        {
            if (this.owner.engineState == Ship.MoveState.Warp || this.timeToNextFire > 0f)
                return;
            this.owner.InCombatTimer = 15f;
            this.timeToNextFire = this.fireDelay;
            if (this.moduleAttachedTo.Active && this.owner.PowerCurrent > this.PowerRequiredToFire && this.OrdinanceRequiredToFire <= this.owner.Ordinance)
            {
                this.owner.Ordinance -= this.OrdinanceRequiredToFire;
                this.owner.PowerCurrent -= this.PowerRequiredToFire;
                if (this.SalvoCount == 1)
                {
                    if (this.FireArc != 0)
                    {
                        float DegreesBetweenShots = (float)(this.FireArc / this.ProjectileCount);
                        float angleToTarget = this.findAngleToTarget(direction);
                        for (int i = 0; i < this.ProjectileCount; i++)
                        {
                            Vector2 newTarget = this.findTargetFromAngleAndDistance(this.moduleAttachedTo.Center, angleToTarget - (float)(this.FireArc / 2) + DegreesBetweenShots * (float)i, this.Range);
                            Vector2 fireDirection = this.findVectorToTarget(this.moduleAttachedTo.Center, newTarget);
                            fireDirection.Y = fireDirection.Y * -1f;
                            this.CreateProjectiles(Vector2.Normalize(fireDirection), null, true);
                        }
                        return;
                    }
                    if (this.FireCone <= 0)
                    {
                        for (int i = 0; i < this.ProjectileCount; i++)
                        {
                            this.CreateProjectiles(direction, null, true);
                        }
                        return;
                    }
                    float spread = RandomMath2.RandomBetween((float)(-this.FireCone / 2), (float)(this.FireCone / 2));
                    //renamed angletotarget,newtarget,firedirection
                    float angleToTarget2 = this.findAngleToTarget(direction);
                    Vector2 newTarget2 = this.findTargetFromAngleAndDistance(this.moduleAttachedTo.Center, angleToTarget2 + spread, this.Range);
                    Vector2 fireDirection2 = this.findVectorToTarget(this.moduleAttachedTo.Center, newTarget2);
                    fireDirection2.Y = fireDirection2.Y * -1f;
                    this.CreateProjectiles(Vector2.Normalize(fireDirection2), null, true);
                    return;
                }
                if (this.SalvoCount > 1)
                {
                    float TimeBetweenShots = this.SalvoTimer / (float)this.SalvoCount;
                    for (int j = 1; j < this.SalvoCount; j++)
                    {
                        Weapon.Salvo sal = new Weapon.Salvo()
                        {
                            Timing = (float)j * TimeBetweenShots,
                            Direction = direction
                        };
                        this.SalvoList.Add(sal);
                    }
                    if (this.FireArc != 0)
                    {
                        float DegreesBetweenShots = (float)(this.FireArc / this.ProjectileCount);
                        float angleToTarget = this.findAngleToTarget(direction);
                        for (int i = 0; i < this.ProjectileCount; i++)
                        {
                            Vector2 newTarget = this.findTargetFromAngleAndDistance(this.moduleAttachedTo.Center, angleToTarget - (float)(this.FireArc / 2) + DegreesBetweenShots * (float)i, this.Range);
                            Vector2 fireDirection = this.findVectorToTarget(this.moduleAttachedTo.Center, newTarget);
                            fireDirection.Y = fireDirection.Y * -1f;
                            this.CreateProjectiles(Vector2.Normalize(fireDirection), null, true);
                        }
                        return;
                    }
                    if (this.FireCone > 0)
                    {
                        float spread = RandomMath2.RandomBetween((float)(-this.FireCone / 2), (float)(this.FireCone / 2));
                        float angleToTarget = this.findAngleToTarget(direction);
                        Vector2 newTarget = this.findTargetFromAngleAndDistance(this.moduleAttachedTo.Center, angleToTarget + spread, this.Range);
                        Vector2 fireDirection = this.findVectorToTarget(this.moduleAttachedTo.Center, newTarget);
                        fireDirection.Y = fireDirection.Y * -1f;
                        this.CreateProjectiles(Vector2.Normalize(fireDirection), null, true);
                        return;
                    }
                    for (int i = 0; i < this.ProjectileCount; i++)
                    {
                        this.CreateProjectiles(direction, null, true);
                    }
                    return;
                }
            }
        }

		public Ship GetOwner()
		{
			return this.owner;
		}

		public Projectile LoadProjectiles(Vector2 direction, Ship owner)
		{
			Projectile projectile = new Projectile(owner, direction)
			{
				range = this.Range,
				weapon = this,
				explodes = this.explodes,
				damageAmount = this.DamageAmount
			};
			projectile.explodes = this.explodes;
			projectile.damageRadius = this.DamageRadius;
            projectile.explosionradiusmod = this.ExplosionRadiusVisual;
			projectile.speed = this.ProjectileSpeed;
			projectile.WeaponEffectType = this.WeaponEffectType;
			projectile.WeaponType = this.WeaponType;
			projectile.Initialize(this.ProjectileSpeed, direction, owner.Center);
			projectile.Radius = this.ProjectileRadius;
			projectile.LoadContent(this.ProjectileTexturePath, this.ModelPath);
			if (owner.GetSystem() != null && owner.GetSystem().isVisible || owner.isInDeepSpace)
			{
				int numberSameWeapons = 0;
				//this.toggleTimer = 0f;
				foreach (Weapon w in owner.Weapons)
				{
					if (w == this || !(w.Name == this.Name) || w.timeToNextFire > 0f)
					{
						continue;
					}
					numberSameWeapons++;
				}
				projectile.DieSound = true;
				if (!string.IsNullOrEmpty(ResourceManager.WeaponsDict[this.UID].dieCue))
				{
					projectile.dieCueName = ResourceManager.WeaponsDict[this.UID].dieCue;
				}
				if (this.InFlightCue != "")
				{
					projectile.InFlightCue = this.InFlightCue;
				}
			}
			return projectile;
		}

		private void ModifyProjectile(Projectile projectile)
		{
            if (this.owner == null)
			{
				return;
			}
            if (this.owner.loyalty.data.Traits.Pack)
            {
                projectile.damageAmount += projectile.damageAmount * this.owner.DamageModifier;
            }
            //Added by McShooterz: Check if mod uses weapon modifiers
            if (GlobalStats.ActiveMod != null && !GlobalStats.ActiveMod.mi.useWeaponModifiers)
            {
                return;
            }
			if (this.Tag_Missile)
			{
				this.AddModifiers("Missile", projectile);
			}
            if (this.Tag_Energy)
            {
                this.AddModifiers("Energy", projectile);
            }
            if (this.Tag_Torpedo)
            {
                this.AddModifiers("Torpedo", projectile);
            }
            if (this.Tag_Kinetic)
            {
                this.AddModifiers("Kinetic", projectile);
            }
            if (this.Tag_Hybrid)
            {
                this.AddModifiers("Hybrid", projectile);
            }
            if (this.Tag_Railgun)
            {
                this.AddModifiers("Railgun", projectile);
            }
            if (this.Tag_Explosive)
            {
                this.AddModifiers("Explosive", projectile);
            }
            if (this.Tag_Guided)
            {
                this.AddModifiers("Guided", projectile);
            }
            if (this.Tag_Intercept)
            {
                this.AddModifiers("Intercept", projectile);
            }
            if (this.Tag_PD)
            {
                this.AddModifiers("PD", projectile);
            }
            if (this.Tag_SpaceBomb)
            {
                this.AddModifiers("Spacebomb", projectile);
            }
            if (this.Tag_BioWeapon)
            {
                this.AddModifiers("BioWeapon", projectile);
            }
            if (this.Tag_Drone)
            {
                this.AddModifiers("Drone", projectile);
            }
            if (this.Tag_Subspace)
            {
                this.AddModifiers("Subspace", projectile);
            }
            if (this.Tag_Warp)
            {
                this.AddModifiers("Warp", projectile);
            }
            if (this.Tag_Cannon)
            {
                this.AddModifiers("Cannon", projectile);
            }
            if (this.Tag_Beam)
            {
                this.AddModifiers("Beam", projectile);
            }
            if (this.Tag_Bomb)
            {
                this.AddModifiers("Bomb", projectile);
            }
            if (this.Tag_Array)
            {
                this.AddModifiers("Array", projectile);
            }
            if (this.Tag_Flak)
            {
                this.AddModifiers("Flak", projectile);
            }
            if (this.Tag_Tractor)
            {
                this.AddModifiers("Tractor", projectile);
            }
		}

		public void ResetToggleSound()
		{
			if (this.ToggleCue != null)
			{
				this.ToggleCue.Stop(AudioStopOptions.Immediate);
				this.ToggleCue = null;
			}
			this.ToggleSoundOn = false;
		}

		public void SetOwner(Ship owner)
		{
			this.owner = owner;
		}

		public virtual void Update(float elapsedTime)
		{
            this.lastFireSound += elapsedTime;
			if (this.timeToNextFire > 0f)
			{
				this.timeToNextFire = MathHelper.Max(this.timeToNextFire - elapsedTime, 0f);
			}
			foreach (Weapon.Salvo salvo in this.SalvoList)
			{
                salvo.Timing -= elapsedTime;
				if (salvo.Timing > 0f)
				{
					continue;
				}
                //this.FireSalvo(salvo.Direction, this.SalvoTarget);
                if (this.SalvoTarget != null)
                {
                    if (this.Tag_Guided)
                        this.FireSalvo(salvo.Direction, SalvoTarget);
                    else
                        this.GetOwner().GetAI().CalculateAndFire(this, SalvoTarget, true);
                }
                else
                    this.FireSalvo(salvo.Direction, null);
				this.SalvoList.QueuePendingRemoval(salvo);
			}
			this.SalvoList.ApplyPendingRemovals();
            if (this.SalvoList.Count == 0)
                this.SalvoTarget = null;
			this.Center = this.moduleAttachedTo.Center;
		}

		private class Salvo
		{
			public float Timing;

            public Vector2 Direction;

			public Salvo()
			{
			}
		}

        public float GetModifiedRange()
        {
            if (this.GetOwner() == null || GlobalStats.ActiveMod == null || !GlobalStats.ActiveMod.mi.useWeaponModifiers)
                return this.Range;
            float modifiedRange = this.Range;
            EmpireData loyaltyData = this.GetOwner().loyalty.data;
            if (this.Tag_Beam)
                modifiedRange += this.Range * loyaltyData.WeaponTags["Beam"].Range;
            if (this.Tag_Energy)
                modifiedRange += this.Range * loyaltyData.WeaponTags["Energy"].Range;
            if (this.Tag_Explosive)
                modifiedRange += this.Range * loyaltyData.WeaponTags["Explosive"].Range;
            if (this.Tag_Guided)
                modifiedRange += this.Range * loyaltyData.WeaponTags["Guided"].Range;
            if (this.Tag_Hybrid)
                modifiedRange += this.Range * loyaltyData.WeaponTags["Hybrid"].Range;
            if (this.Tag_Intercept)
                modifiedRange += this.Range * loyaltyData.WeaponTags["Intercept"].Range;
            if (this.Tag_Kinetic)
                modifiedRange += this.Range * loyaltyData.WeaponTags["Kinetic"].Range;
            if (this.Tag_Missile)
                modifiedRange += this.Range * loyaltyData.WeaponTags["Missile"].Range;
            if (this.Tag_Railgun)
                modifiedRange += this.Range * loyaltyData.WeaponTags["Railgun"].Range;
            if (this.Tag_Cannon)
                modifiedRange += this.Range * loyaltyData.WeaponTags["Cannon"].Range;
            if (this.Tag_PD)
                modifiedRange += this.Range * loyaltyData.WeaponTags["PD"].Range;
            if (this.Tag_SpaceBomb)
                modifiedRange += this.Range * loyaltyData.WeaponTags["Spacebomb"].Range;
            if (this.Tag_BioWeapon)
                modifiedRange += this.Range * loyaltyData.WeaponTags["BioWeapon"].Range;
            if (this.Tag_Drone)
                modifiedRange += this.Range * loyaltyData.WeaponTags["Drone"].Range;
            if (this.Tag_Subspace)
                modifiedRange += this.Range * loyaltyData.WeaponTags["Subspace"].Range;
            if (this.Tag_Warp)
                modifiedRange += this.Range * loyaltyData.WeaponTags["Warp"].Range;
            if (this.Tag_Array)
                modifiedRange += this.Range * loyaltyData.WeaponTags["Array"].Range;
            if (this.Tag_Flak)
                modifiedRange += this.Range * loyaltyData.WeaponTags["Flak"].Range;
            if (this.Tag_Tractor)
                modifiedRange += this.Range * loyaltyData.WeaponTags["Tractor"].Range;
            return modifiedRange;            
        }

        public bool TargetValid(string Role)
        {
            if (this.Excludes_Fighters && (Role == "fighter" || Role == "scout" || Role == "drone"))
                return false;
            if (this.Excludes_Corvettes && (Role == "corvette"))
                return false;
            if (this.Excludes_Capitals && (Role == "frigate" || Role == "destroyer" || Role == "cruiser" || Role == "carrier" || Role == "capital"))
                return false;
            if (this.Excludes_Stations && (Role == "platform" || Role == "station"))
                return false;
            return true;
        }
	}
}
