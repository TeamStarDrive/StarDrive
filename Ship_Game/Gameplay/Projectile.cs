using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using Ship_Game;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;

namespace Ship_Game.Gameplay
{
	public class Projectile : GameplayObject
	{
		public float ShieldDamageBonus;

		public float ArmorDamageBonus;

		public byte ArmorPiercing;

		public List<ShipModule> ArmorsPierced = new List<ShipModule>();

		public static ContentManager contentManager;

		public Ship owner;

		public bool IgnoresShields;

		public string WeaponType;

		private MissileAI missileAI;

		public float life;

		private Ship_Game.Planet planet;

		public float velocityMaximum;

		public float speed;

		public float range;

		public float damageAmount;

		public float damageRadius;

		public float duration;

		public bool explodes;

		protected Color[] explosionColors;

		public ShipModule moduleAttachedTo;

		protected string weaponEffect;

		public string WeaponEffectType;

		private Matrix WorldMatrix;

		public static UniverseScreen universeScreen;

		private ParticleEmitter trailEmitter;

		private ParticleEmitter firetrailEmitter;

		private SceneObject ProjSO;

		public string InFlightCue = "";

		public AudioEmitter emitter = new AudioEmitter();

		public bool Miss;

		public Empire loyalty;

		private float initialDuration;

		private Vector2 direction;

		private float switchFrames;

		private float frameTimer;

		public float RotationRadsPerSecond;

		//private GameplayObject Target;

		//private bool isDrone;

		private DroneAI droneAI;

		public Weapon weapon;

		public string texturePath;

		public string modelPath;

		private float zStart = -25f;

		private float particleDelay;

		private PointLight light;

		public bool firstRun = true;

		private Ship_Game.MuzzleFlash flash;

		private PointLight MuzzleFlash;

		private float flashTimer = 0.142f;

		public float Scale = 1f;

		private int AnimationFrame;

		private string fmt = "00000.##";

		private float TimeElapsed;

		private bool DieNextFrame;

		public bool DieSound;

		private Cue inFlight;

		public string dieCueName = "";

		public ShipModule HitModule;

		private bool wasAddedToSceneGraph;

		private bool LightWasAddedToSceneGraph;

		private bool muzzleFlashAdded;

		public Ship Owner
		{
			get
			{
				return this.owner;
			}
		}

		public Ship_Game.Planet Planet
		{
			get
			{
				return this.planet;
			}
		}

		public Projectile(Ship owner, Vector2 direction, ShipModule moduleAttachedTo)
		{
			this.loyalty = owner.loyalty;
			this.owner = owner;
			if (!owner.isInDeepSpace)
			{
				this.system = owner.GetSystem();
			}
			else
			{
				this.isInDeepSpace = true;
			}
			base.Position = moduleAttachedTo.Center;
			this.moduleAttachedTo = moduleAttachedTo;
			this.Center = moduleAttachedTo.Center;
			this.emitter.Position = new Vector3(moduleAttachedTo.Center, 0f);
		}

		public Projectile(Ship_Game.Planet p, Vector2 direction)
		{
			this.system = p.system;
			base.Position = p.Position;
			this.loyalty = p.Owner;
			this.velocity = direction;
			this.rotation = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(p.Position, p.Position + this.velocity));
			this.Center = p.Position;
			this.emitter.Position = new Vector3(p.Position, 0f);
		}

		public Projectile(Ship owner, Vector2 direction)
		{
			this.owner = owner;
			this.loyalty = owner.loyalty;
			this.rotation = (float)Math.Acos((double)Vector2.Dot(Vector2.UnitY, direction)) - 3.14159274f;
			if (direction.X > 0f)
			{
				Projectile projectile = this;
				projectile.rotation = projectile.rotation * -1f;
			}
		}

		public Projectile()
		{
		}

		public void DamageMissile(GameplayObject source, float damageAmount)
		{
			Projectile health = this;
			health.Health = health.Health - damageAmount;
			if (base.Health <= 0f && this.Active)
			{
				this.DieNextFrame = true;
			}
		}

		public override void Die(GameplayObject source, bool cleanupOnly)
		{
			DebugInfoScreen.ProjDied = DebugInfoScreen.ProjDied + 1;
			if (this.Active)
			{
				Vector3 vector3 = new Vector3(this.Center.X, this.Center.Y, -100f);
				Vector3 vector31 = new Vector3(this.velocity.X, base.Velocity.Y, 0f);
				if (this.light != null)
				{
					lock (GlobalStats.ObjectManagerLocker)
					{
						Projectile.universeScreen.ScreenManager.inter.LightManager.Remove(this.light);
					}
				}
				if (this.InFlightCue != "" && this.inFlight != null)
				{
					this.inFlight.Stop(AudioStopOptions.Immediate);
				}
				if (this.explodes)
				{
					if (this.weapon.OrdinanceRequiredToFire > 0f && this.Owner != null)
					{
                        this.damageAmount += this.Owner.loyalty.data.OrdnanceEffectivenessBonus * this.damageAmount;
                        this.damageRadius += this.Owner.loyalty.data.OrdnanceEffectivenessBonus * this.damageRadius;
					}
					if (this.WeaponType == "Photon")
					{
						if (this.dieCueName != "")
						{
							this.dieCue = AudioManager.GetCue(this.dieCueName);
						}
						if (this.dieCue != null)
						{
							this.dieCue.Apply3D(GameplayObject.audioListener, this.emitter);
							this.dieCue.Play();
						}
						if (!cleanupOnly && Projectile.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
						{
							ExplosionManager.AddProjectileExplosion(new Vector3(base.Position, -50f), this.damageRadius * 4.5f, 2.5f, 0.2f, this.weapon.ExpColor);
							Projectile.universeScreen.flash.AddParticleThreadB(new Vector3(base.Position, -50f), Vector3.Zero);
						}
						if (this.system == null)
						{
                            UniverseScreen.DeepSpaceManager.ProjectileExplode(this, this.damageAmount, this.damageRadius);
						}
						else
						{
                            this.system.spatialManager.ProjectileExplode(this, this.damageAmount, this.damageRadius);
						}
					}
					else if (this.dieCueName != "")
					{
						if (Projectile.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
						{
							this.dieCue = AudioManager.GetCue(this.dieCueName);
							if (this.dieCue != null)
							{
								this.dieCue.Apply3D(GameplayObject.audioListener, this.emitter);
								this.dieCue.Play();
							}
						}
						if (!cleanupOnly && Projectile.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
						{
							ExplosionManager.AddExplosion(new Vector3(base.Position, -50f), this.damageRadius * 4.5f, 2.5f, 0.2f);
							Projectile.universeScreen.flash.AddParticleThreadB(new Vector3(base.Position, -50f), Vector3.Zero);
						}
						if (this.system == null)
						{
                            UniverseScreen.DeepSpaceManager.ProjectileExplode(this, this.damageAmount, this.damageRadius);
						}
						else
						{
                            this.system.spatialManager.ProjectileExplode(this, this.damageAmount, this.damageRadius);
						}
					}
					else if (this.system == null)
					{
                        UniverseScreen.DeepSpaceManager.ProjectileExplode(this, this.damageAmount, this.damageRadius);
					}
					else
					{
                        this.system.spatialManager.ProjectileExplode(this, this.damageAmount, this.damageRadius);
					}
				}
				else if (this.weapon.FakeExplode && Projectile.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
				{
					ExplosionManager.AddExplosion(new Vector3(base.Position, -50f), this.damageRadius * 4.5f, 2.5f, 0.2f);
					Projectile.universeScreen.flash.AddParticleThreadB(new Vector3(base.Position, -50f), Vector3.Zero);
				}
			}
			if (this.ProjSO != null && this.Active)
			{
				lock (GlobalStats.ObjectManagerLocker)
				{
					Projectile.universeScreen.ScreenManager.inter.ObjectManager.Remove(this.ProjSO);
				}
				this.ProjSO.Clear();
			}
			if (this.droneAI != null)
			{
				foreach (Beam beam in this.droneAI.Beams)
				{
					beam.Die(this, true);
				}
				this.droneAI.Beams.Clear();
			}
			if (this.muzzleFlashAdded)
			{
				lock (GlobalStats.ObjectManagerLocker)
				{
					Projectile.universeScreen.ScreenManager.inter.LightManager.Remove(this.MuzzleFlash);
				}
			}
			if (this.system == null)
			{
				UniverseScreen.DeepSpaceManager.CollidableProjectiles.QueuePendingRemoval(this);
			}
			else
			{
				this.system.spatialManager.CollidableProjectiles.QueuePendingRemoval(this);
			}
			base.Die(source, cleanupOnly);
		}

		public virtual void Draw(SpriteBatch spriteBatch)
		{
		}

		public DroneAI GetDroneAI()
		{
			return this.droneAI;
		}

		public SceneObject GetSO()
		{
			return this.ProjSO;
		}

		public Matrix GetWorld()
		{
			return this.WorldMatrix;
		}

		public void Initialize(float initialSpeed, Vector2 direction, Vector2 pos)
		{
			DebugInfoScreen.ProjCreated = DebugInfoScreen.ProjCreated + 1;
			this.direction = direction;
			this.velocity = (initialSpeed * direction) + (this.owner != null ? this.owner.Velocity : Vector2.Zero);
			if (this.moduleAttachedTo == null)
			{
				this.Center = pos;
				this.rotation = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.Center, this.Center + this.velocity));
			}
			else
			{
				this.Center = this.moduleAttachedTo.Center;
				this.rotation = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.moduleAttachedTo.Center, this.moduleAttachedTo.Center + this.velocity));
			}
			this.radius = 1f;
			this.velocityMaximum = initialSpeed + (this.owner != null ? this.owner.Velocity.Length() : 0f);
			this.velocity = Vector2.Normalize(this.velocity) * this.velocityMaximum;
			this.duration = this.range / initialSpeed;
			Projectile projectile = this;
			projectile.duration = projectile.duration + this.duration * 0.25f;
			if (this.weapon.Tag_SpaceBomb)
			{
				this.duration = 5f;
			}
			this.initialDuration = this.duration;
			if (this.weapon.Animated == 1)
			{
				this.switchFrames = this.initialDuration / (float)this.weapon.Frames;
				if (this.weapon.LoopAnimation == 1)
				{
					if (Ship.universeScreen != null)
					{
						this.AnimationFrame = (int)((this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(0f, (float)(this.weapon.Frames - 1));
					}
					else
					{
						this.AnimationFrame = (int)RandomMath.RandomBetween(0f, (float)(this.weapon.Frames - 1));
					}
				}
			}
			if (this.owner.loyalty.data.ArmorPiercingBonus > 0 && (this.weapon.WeaponType == "Missile" || this.weapon.WeaponType == "Ballistic Cannon"))
			{
				this.ArmorPiercing = (byte)this.owner.loyalty.data.ArmorPiercingBonus;
			}
			Projectile projectile1 = this;
			projectile1.particleDelay = projectile1.particleDelay + this.weapon.particleDelay;
			if (this.weapon.Tag_Guided)
			{
				this.missileAI = new MissileAI(this);
			}
			if (this.WeaponType != "Missile" && this.WeaponType != "Drone" && this.WeaponType != "Rocket" || (this.system == null || !this.system.isVisible) && !this.isInDeepSpace)
			{
				if (this.owner != null && this.owner.loyalty.data.Traits.Blind > 0)
				{
					int Random = 0;
					Random = (Ship.universeScreen != null ? (int)((this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(0f, 10f) : (int)RandomMath.RandomBetween(0f, 10f));
					if (Random <= 1)
					{
						this.Miss = true;
					}
				}
			}
			else if (this.ProjSO != null)
			{
				this.wasAddedToSceneGraph = true;
				lock (GlobalStats.ObjectManagerLocker)
				{
					if (Projectile.universeScreen != null)
					{
						Projectile.universeScreen.ScreenManager.inter.ObjectManager.Submit(this.ProjSO);
					}
				}
			}
			if (this.system == null)
			{
				UniverseScreen.DeepSpaceManager.CollidableProjectiles.Add(this);
				if (this.weapon.Tag_Intercept && Projectile.universeScreen != null)
				{
					Projectile.universeScreen.DSProjectilesToAdd.Add(this);
				}
			}
			else
			{
				lock (GlobalStats.BucketLock)
				{
					this.system.spatialManager.CollidableProjectiles.Add(this);
					if (this.system.spatialManager.CellSize > 0)
					{
						this.system.spatialManager.RegisterObject(this);
					}
					if (this.weapon.Tag_Intercept)
					{
						this.system.spatialManager.CollidableObjects.Add(this);
					}
				}
			}
			base.Initialize();
		}

		public void InitializeDrone(float initialSpeed, Vector2 direction)
		{
			//this.isDrone = true;
			this.direction = direction;
			if (this.moduleAttachedTo != null)
			{
				this.Center = this.moduleAttachedTo.Center;
			}
			this.velocity = (initialSpeed * direction) + (this.owner != null ? this.owner.Velocity : Vector2.Zero);
			this.radius = 1f;
			this.velocityMaximum = initialSpeed + (this.owner != null ? this.owner.Velocity.Length() : 0f);
			this.duration = this.range / initialSpeed;
			Projectile projectile = this;
			projectile.duration = projectile.duration + this.duration * 0.25f;
			this.initialDuration = this.duration;
			if (this.weapon.Animated == 1)
			{
				this.switchFrames = this.initialDuration / (float)this.weapon.Frames;
				if (this.weapon.LoopAnimation == 1)
				{
					this.AnimationFrame = (int)((this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(0f, (float)(this.weapon.Frames - 1));
				}
			}
			Projectile projectile1 = this;
			projectile1.particleDelay = projectile1.particleDelay + this.weapon.particleDelay;
			if (this.weapon.IsRepairDrone)
			{
				this.droneAI = new DroneAI(this);
			}
			if (this.system == null)
			{
				Projectile.universeScreen.DSProjectilesToAdd.Add(this);
				UniverseScreen.DeepSpaceManager.CollidableProjectiles.Add(this);
			}
			else
			{
				this.system.spatialManager.CollidableProjectiles.Add(this);
				this.system.spatialManager.CollidableObjects.Add(this);
			}
			base.Initialize();
		}

		public void InitializeMissile(float initialSpeed, Vector2 direction, GameplayObject Target)
		{
			this.direction = direction;
			if (this.moduleAttachedTo != null)
			{
				this.Center = this.moduleAttachedTo.Center;
			}
			this.velocity = (initialSpeed * direction) + (this.owner != null ? this.owner.Velocity : Vector2.Zero);
			this.radius = 1f;
			this.velocityMaximum = initialSpeed + (this.owner != null ? this.owner.Velocity.Length() : 0f);
			this.duration = this.range / initialSpeed;
			Projectile projectile = this;
			projectile.duration = projectile.duration + this.duration * 0.25f;
			if (this.weapon.Tag_SpaceBomb)
			{
				this.duration = 5f;
			}
			this.initialDuration = this.duration;
			if (this.moduleAttachedTo != null)
			{
				this.Center = this.moduleAttachedTo.Center;
				if (this.moduleAttachedTo.facing == 0f)
				{
					this.rotation = this.Owner.Rotation;
				}
				else
				{
					this.rotation = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.moduleAttachedTo.Center, this.moduleAttachedTo.Center + this.velocity));
				}
			}
			if (this.weapon.Animated == 1)
			{
				this.switchFrames = this.initialDuration / (float)this.weapon.Frames;
				if (this.weapon.LoopAnimation == 1)
				{
					this.AnimationFrame = (int)((this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(0f, (float)(this.weapon.Frames - 1));
				}
			}
			Projectile projectile1 = this;
			projectile1.particleDelay = projectile1.particleDelay + this.weapon.particleDelay;
			if (this.weapon.Tag_Guided)
			{
				this.missileAI = new MissileAI(this);
				this.missileAI.SetTarget(Target);
			}
			if ((this.WeaponType == "Missile" || this.WeaponType == "Drone" || this.WeaponType == "Rocket") && (this.system != null && this.system.isVisible || this.isInDeepSpace))
			{
				this.wasAddedToSceneGraph = true;
				lock (GlobalStats.ObjectManagerLocker)
				{
					Projectile.universeScreen.ScreenManager.inter.ObjectManager.Submit(this.ProjSO);
				}
			}
			if (this.system == null)
			{
				Projectile.universeScreen.DSProjectilesToAdd.Add(this);
				UniverseScreen.DeepSpaceManager.CollidableProjectiles.Add(this);
			}
			else
			{
				lock (GlobalStats.BucketLock)
				{
					this.system.spatialManager.CollidableProjectiles.Add(this);
					this.system.spatialManager.RegisterObject(this);
					this.system.spatialManager.CollidableObjects.Add(this);
				}
			}
			base.Initialize();
		}

		public void InitializeMissilePlanet(float initialSpeed, Vector2 direction, GameplayObject Target, Ship_Game.Planet p)
		{
			this.direction = direction;
			this.Center = p.Position;
			this.zStart = -2500f;
			this.velocity = (initialSpeed * direction) + (this.owner != null ? this.owner.Velocity : Vector2.Zero);
			this.radius = 1f;
			this.velocityMaximum = initialSpeed + (this.owner != null ? this.owner.Velocity.Length() : 0f);
			this.duration = this.range / initialSpeed;
			Projectile projectile = this;
			projectile.duration = projectile.duration + this.duration * 0.25f;
			this.initialDuration = this.duration;
			this.planet = p;
			if (this.weapon.Animated == 1)
			{
				this.switchFrames = this.initialDuration / (float)this.weapon.Frames;
				if (this.weapon.LoopAnimation == 1)
				{
					this.AnimationFrame = (int)((this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(0f, (float)(this.weapon.Frames - 1));
				}
			}
			Projectile projectile1 = this;
			projectile1.particleDelay = projectile1.particleDelay + this.weapon.particleDelay;
			if (this.weapon.Tag_Guided)
			{
				this.missileAI = new MissileAI(this);
				this.missileAI.SetTarget(Target);
			}
			if ((this.WeaponType == "Missile" || this.WeaponType == "Drone" || this.WeaponType == "Rocket") && (this.system != null && this.system.isVisible || this.isInDeepSpace))
			{
				this.wasAddedToSceneGraph = true;
				lock (GlobalStats.ObjectManagerLocker)
				{
					Projectile.universeScreen.ScreenManager.inter.ObjectManager.Submit(this.ProjSO);
				}
			}
			if (this.system == null)
			{
				Projectile.universeScreen.DSProjectilesToAdd.Add(this);
				UniverseScreen.DeepSpaceManager.CollidableProjectiles.Add(this);
			}
			else
			{
				lock (GlobalStats.BucketLock)
				{
					this.system.spatialManager.CollidableProjectiles.Add(this);
					this.system.spatialManager.RegisterObject(this);
					this.system.spatialManager.CollidableObjects.Add(this);
				}
			}
			base.Initialize();
		}

		public void InitializePlanet(float initialSpeed, Vector2 direction, Vector2 pos)
		{
			this.zStart = -2500f;
			this.direction = direction;
			if (this.moduleAttachedTo == null)
			{
				this.Center = pos;
			}
			else
			{
				this.Center = this.moduleAttachedTo.Center;
			}
			this.velocity = (initialSpeed * direction) + (this.owner != null ? this.owner.Velocity : Vector2.Zero);
			this.radius = 1f;
			this.velocityMaximum = initialSpeed + (this.owner != null ? this.owner.Velocity.Length() : 0f);
			this.velocity = Vector2.Normalize(this.velocity) * this.velocityMaximum;
			this.duration = this.range / initialSpeed;
			Projectile projectile = this;
			projectile.duration = projectile.duration + this.duration * 0.25f;
			this.initialDuration = this.duration;
			if (this.weapon.Animated == 1)
			{
				this.switchFrames = this.initialDuration / (float)this.weapon.Frames;
				if (this.weapon.LoopAnimation == 1)
				{
					this.AnimationFrame = (int)((this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(0f, (float)(this.weapon.Frames - 1));
				}
			}
			Projectile projectile1 = this;
			projectile1.particleDelay = projectile1.particleDelay + this.weapon.particleDelay;
			if (this.weapon.Tag_Guided)
			{
				this.missileAI = new MissileAI(this);
			}
			if ((this.WeaponType == "Missile" || this.WeaponType == "Drone" || this.WeaponType == "Rocket") && (this.system != null && this.system.isVisible || this.isInDeepSpace) && this.ProjSO != null)
			{
				this.wasAddedToSceneGraph = true;
				lock (GlobalStats.ObjectManagerLocker)
				{
					Projectile.universeScreen.ScreenManager.inter.ObjectManager.Submit(this.ProjSO);
				}
			}
			if (this.system == null)
			{
				Projectile.universeScreen.DSProjectilesToAdd.Add(this);
				UniverseScreen.DeepSpaceManager.CollidableProjectiles.Add(this);
			}
			else
			{
				lock (GlobalStats.BucketLock)
				{
					this.system.spatialManager.CollidableProjectiles.Add(this);
					this.system.spatialManager.CollidableObjects.Add(this);
				}
			}
			base.Initialize();
		}

		public void LoadContent(string texturePath, string modelPath)
		{
			this.texturePath = texturePath;
			this.modelPath = modelPath;
			this.ProjSO = new SceneObject(Ship_Game.ResourceManager.ProjectileMeshDict[modelPath])
			{
				Visibility = ObjectVisibility.Rendered,
				ObjectType = ObjectType.Dynamic
			};
			if (Projectile.universeScreen != null)
			{
				if (this.weapon.WeaponEffectType == "RocketTrail")
				{
					this.trailEmitter = new ParticleEmitter(Projectile.universeScreen.projectileTrailParticles, 500f, new Vector3(this.Center, -this.zStart));
					this.firetrailEmitter = new ParticleEmitter(Projectile.universeScreen.fireTrailParticles, 500f, new Vector3(this.Center, -this.zStart));
				}
				if (this.weapon.WeaponEffectType == "Plasma")
				{
					this.firetrailEmitter = new ParticleEmitter(Projectile.universeScreen.flameParticles, 500f, new Vector3(this.Center, 0f));
				}
			}
			if (this.weapon.Animated == 1)
			{
				string remainder = this.AnimationFrame.ToString(this.fmt);
				this.texturePath = string.Concat(this.weapon.AnimationPath, remainder);
			}
		}

		public override bool Touch(GameplayObject target)
		{
			if (this.Miss)
			{
				return false;
			}
			if (target != null)
			{
				if (target == this.owner && !this.weapon.HitsFriendlies)
				{
					return false;
				}
				Projectile projectile = target as Projectile;
				if (projectile != null)
				{
					if (this.owner != null && projectile.loyalty == this.owner.loyalty)
					{
						return false;
					}
					if (projectile.WeaponType == "Missile")
					{
						float ran = ((this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(0f, 1f);
						if (projectile.loyalty != null && ran >= projectile.loyalty.data.MissileDodgeChance)
						{
							projectile.DamageMissile(this, this.damageAmount);
							return true;
						}
					}
					else if (this.weapon.Tag_Intercept || projectile.weapon.Tag_Intercept)
					{
						this.DieNextFrame = true;
						projectile.DieNextFrame = true;
					}
					return false;
				}
				if (target is Asteroid)
				{
					if (!this.explodes)
					{
						target.Damage(this, this.damageAmount);
					}
					this.Die(null, false);
					return true;
				}
				if (target is ShipModule)
				{
					if (this.weapon.TruePD)
					{
						this.DieNextFrame = true;
						return true;
					}
                    if (this.weapon.Tag_Guided && GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.enableECM)
                    {
                        float ECMResist = this.weapon.ECMResist; // check any in-built ECM resistance on the guided weapon itself
                        Ship targetShip = (target as ShipModule).GetParent(); // identify the ship to which the module belongs
                        float rnum = RandomMath.RandomBetween(0.0f, 1.0f); // Random roll 0.0-1.0, i.e. roll 0 to 100
                        if (rnum + ECMResist < targetShip.ECMValue) // Can she hit?
                            this.Miss = true;
                    }
                    if ((target as ShipModule).ModuleType == ShipModuleType.Armor)
					{
						if (!this.ArmorsPierced.Contains(target as ShipModule) && this.ArmorsPierced.Count < this.ArmorPiercing)
						{
							this.ArmorsPierced.Add(target as ShipModule);
							return false;
						}
						if (this.ArmorsPierced.Count > 0 && this.ArmorsPierced.Contains(target as ShipModule))
						{
							return false;
						}
						Projectile explosiveRadiusReduction = this;
						explosiveRadiusReduction.damageRadius = explosiveRadiusReduction.damageRadius - (target as ShipModule).GetParent().loyalty.data.ExplosiveRadiusReduction * this.damageRadius;
						Projectile explosiveRadiusReduction1 = this;
						explosiveRadiusReduction1.damageAmount = explosiveRadiusReduction1.damageAmount - (target as ShipModule).GetParent().loyalty.data.ExplosiveRadiusReduction * this.damageAmount;
						Projectile effectVsArmor = this;
						effectVsArmor.damageAmount = effectVsArmor.damageAmount * (this.weapon.EffectVsArmor + this.ArmorDamageBonus);
					}
					if ((target as ShipModule).ModuleType == ShipModuleType.Shield)
					{
						Projectile effectVSShields = this;
						effectVSShields.damageAmount = effectVSShields.damageAmount * (this.weapon.EffectVSShields + this.ShieldDamageBonus);
					}
					if (this.explodes || this.weapon.Tag_Explosive)
					{
						this.HitModule = target as ShipModule;
						if ((target as ShipModule).ModuleType == ShipModuleType.Shield)
						{
							//Projectile projectile1 = this;
                            //projectile1.damageAmount = projectile1.damageAmount; // 2f;
							this.explodes = false;
						}
					}
					if (this.owner != null && this.owner.loyalty != (target as ShipModule).GetParent().loyalty && (target as ShipModule).GetParent().Role == "fighter" && (target as ShipModule).GetParent().loyalty.data.Traits.DodgeMod > 0f)
					{
						if ((((target as ShipModule).GetParent().GetSystem() != null ? (target as ShipModule).GetParent().GetSystem().RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(0f, 100f) < (target as ShipModule).GetParent().loyalty.data.Traits.DodgeMod * 100f)
						{
							this.Miss = true;
						}
					}
					if (this.Miss)
					{
						return false;
					}
				}
				ShipModule module = target as ShipModule;
				if (module != null && module.GetParent().loyalty == this.loyalty)
				{
					return false;
				}
				if (module != null)
				{
                    if (!this.explodes)
                        target.Damage(this, this.damageAmount);
					base.Health = 0f;
				}
				if (this.WeaponEffectType == "Plasma")
				{
					Vector3 center = new Vector3(this.Center.X, this.Center.Y, -100f);
					Vector2 forward = new Vector2((float)Math.Sin((double)base.Rotation), -(float)Math.Cos((double)base.Rotation));
					Vector2 right = new Vector2(-forward.Y, forward.X);
					right = Vector2.Normalize(right);
					for (int i = 0; i < 20; i++)
					{
						Vector3 random = new Vector3(right.X * ((this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(-250f, 250f), right.Y * ((this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(-250f, 250f), ((this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(-250f, 250f));
						Projectile.universeScreen.flameParticles.AddParticleThreadA(center, random);
						random = new Vector3(-forward.X + ((this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(-150f, 150f), -forward.Y + ((this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(-150f, 150f), ((this.system != null ? this.system.RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(-150f, 150f));
						Projectile.universeScreen.flameParticles.AddParticleThreadA(center, random);
					}
				}
				else if (this.WeaponType == "Ballistic" && target is ShipModule && (target as ShipModule).ModuleType != ShipModuleType.Shield)
				{
					Cue impact = AudioManager.GetCue("sd_impact_bullet_small_01");
					impact.Apply3D(Projectile.universeScreen.listener, this.emitter);
					impact.Play();
				}
			}
			this.DieNextFrame = true;
			return base.Touch(target);
		}

        public override void Update(float elapsedTime)
        {
            if (this.DieNextFrame && this.Active)
            {
                this.Die((GameplayObject)this, false);
            }
            else
            {
                if (!this.Active)
                    return;
                this.TimeElapsed += elapsedTime;
                //Projectile projectile = this;
                Vector2 vector2 = this.Position + this.Velocity * elapsedTime;
                this.Position = vector2;
                this.Scale = this.weapon.Scale;
                if (this.weapon.Animated == 1)
                {
                    this.frameTimer += elapsedTime;
                    if (this.weapon.LoopAnimation == 0 && (double)this.frameTimer > (double)this.switchFrames)
                    {
                        this.frameTimer = 0.0f;
                        ++this.AnimationFrame;
                        if (this.AnimationFrame >= this.weapon.Frames)
                            this.AnimationFrame = 0;
                    }
                    else if (this.weapon.LoopAnimation == 1)
                    {
                        ++this.AnimationFrame;
                        if (this.AnimationFrame >= this.weapon.Frames)
                            this.AnimationFrame = 0;
                    }
                    this.texturePath = this.weapon.AnimationPath + this.AnimationFrame.ToString(this.fmt);
                }
                if (this.InFlightCue != "" && this.inFlight == null)
                {
                    this.inFlight = AudioManager.GetCue(this.InFlightCue);
                    this.inFlight.Apply3D(Projectile.universeScreen.listener, this.emitter);
                    this.inFlight.Play();
                }
                this.particleDelay -= elapsedTime;
                if ((double)this.duration > 0.0)
                {
                    this.duration -= elapsedTime;
                    if ((double)this.duration < 0.0)
                    {
                        this.Health = 0.0f;
                        this.Die((GameplayObject)null, false);
                        return;
                    }
                }
                if (this.missileAI != null)
                    this.missileAI.Think(elapsedTime);
                if (this.droneAI != null)
                    this.droneAI.Think(elapsedTime);
                if ((this.WeaponType == "Rocket" || this.WeaponType == "Drone" || this.WeaponType == "Missile") && (this.system != null && this.system.isVisible && (!this.wasAddedToSceneGraph && Projectile.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)))
                {
                    this.wasAddedToSceneGraph = true;
                    lock (GlobalStats.ObjectManagerLocker)
                        Projectile.universeScreen.ScreenManager.inter.ObjectManager.Submit((ISceneObject)this.ProjSO);
                }
                if (this.firstRun && this.moduleAttachedTo != null)
                {
                    this.Position = this.moduleAttachedTo.Center;
                    this.Center = this.moduleAttachedTo.Center;
                    this.firstRun = false;
                }
                else
                    this.Center = new Vector2(this.Position.X, this.Position.Y);
                this.emitter.Position = new Vector3(this.Center, 0.0f);
                if ((this.isInDeepSpace || this.system != null && this.system.isVisible) && Projectile.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
                {
                    if ((double)this.zStart < -25.0)
                        this.zStart += this.velocityMaximum * elapsedTime;
                    else
                        this.zStart = -25f;
                    this.ProjSO.World = Matrix.Identity * Matrix.CreateScale(this.Scale) * Matrix.CreateRotationZ(this.Rotation) * Matrix.CreateTranslation(this.Center.X, this.Center.Y, -this.zStart);
                    this.WorldMatrix = this.ProjSO.World;
                }
                Vector3 newPosition = new Vector3(this.Center.X, this.Center.Y, -this.zStart);
                if (this.firetrailEmitter != null && this.WeaponEffectType == "Plasma" && ((double)this.duration > (double)this.initialDuration * 0.699999988079071 && (double)this.particleDelay <= 0.0))
                    this.firetrailEmitter.UpdateProjectileTrail(elapsedTime, newPosition, new Vector3(this.Velocity, 0.0f) + Vector3.Normalize(new Vector3(this.direction, 0.0f)) * this.speed * 1.75f);
                if (this.firetrailEmitter != null && this.WeaponEffectType == "RocketTrail")
                    this.firetrailEmitter.Update(elapsedTime, newPosition);
                if (this.trailEmitter != null)
                    this.trailEmitter.Update(elapsedTime, newPosition);
                if (this.system != null && this.system.isVisible && (this.light == null && this.weapon.Light != null) && (Projectile.universeScreen.viewState < UniverseScreen.UnivScreenState.SystemView && !this.LightWasAddedToSceneGraph))
                {
                    this.LightWasAddedToSceneGraph = true;
                    this.light = new PointLight();
                    this.light.Position = new Vector3(this.Center.X, this.Center.Y, -25f);
                    this.light.World = Matrix.Identity * Matrix.CreateTranslation(this.light.Position);
                    this.light.Radius = 100f;
                    this.light.ObjectType = ObjectType.Dynamic;
                    if (this.weapon.Light == "Green")
                        this.light.DiffuseColor = new Vector3(0.0f, 0.8f, 0.0f);
                    else if (this.weapon.Light == "Red")
                        this.light.DiffuseColor = new Vector3(1f, 0.0f, 0.0f);
                    else if (this.weapon.Light == "Orange")
                        this.light.DiffuseColor = new Vector3(0.9f, 0.7f, 0.0f);
                    else if (this.weapon.Light == "Purple")
                        this.light.DiffuseColor = new Vector3(0.8f, 0.8f, 0.95f);
                    else if (this.weapon.Light == "Blue")
                        this.light.DiffuseColor = new Vector3(0.0f, 0.8f, 1f);
                    this.light.Intensity = 1.7f;
                    this.light.FillLight = true;
                    this.light.Enabled = true;
                    lock (GlobalStats.ObjectManagerLocker)
                        Projectile.universeScreen.ScreenManager.inter.LightManager.Submit((ILight)this.light);
                }
                else if (this.weapon.Light != null && this.LightWasAddedToSceneGraph)
                {
                    this.light.Position = new Vector3(this.Center.X, this.Center.Y, -25f);
                    this.light.World = Matrix.Identity * Matrix.CreateTranslation(this.light.Position);
                }
                if (this.moduleAttachedTo != null)
                {
                    if (this.owner.ProjectilesFired.Count < 30 && this.system != null && (this.system.isVisible && this.MuzzleFlash == null) && (this.moduleAttachedTo.InstalledWeapon.MuzzleFlash != null && Projectile.universeScreen.viewState < UniverseScreen.UnivScreenState.SystemView && !this.muzzleFlashAdded))
                    {
                        this.muzzleFlashAdded = true;
                        this.MuzzleFlash = new PointLight();
                        this.MuzzleFlash.Position = new Vector3(this.moduleAttachedTo.Center.X, this.moduleAttachedTo.Center.Y, -45f);
                        this.MuzzleFlash.World = Matrix.Identity * Matrix.CreateTranslation(this.MuzzleFlash.Position);
                        this.MuzzleFlash.Radius = 65f;
                        this.MuzzleFlash.ObjectType = ObjectType.Dynamic;
                        this.MuzzleFlash.DiffuseColor = new Vector3(1f, 0.97f, 0.9f);
                        this.MuzzleFlash.Intensity = 1f;
                        this.MuzzleFlash.FillLight = false;
                        this.MuzzleFlash.Enabled = true;
                        lock (GlobalStats.ObjectManagerLocker)
                            Projectile.universeScreen.ScreenManager.inter.LightManager.Submit((ILight)this.MuzzleFlash);
                        this.flashTimer -= elapsedTime;
                        this.flash = new MuzzleFlash();
                        this.flash.WorldMatrix = Matrix.Identity * Matrix.CreateRotationZ(this.rotation) * Matrix.CreateTranslation(this.MuzzleFlash.Position);
                        this.flash.Owner = (GameplayObject)this;
                        lock (GlobalStats.ExplosionLocker)
                            MuzzleFlashManager.FlashList.Add(this.flash);
                    }
                    else if ((double)this.flashTimer > 0.0 && this.moduleAttachedTo.InstalledWeapon.MuzzleFlash != null && this.muzzleFlashAdded)
                    {
                        this.flashTimer -= elapsedTime;
                        this.MuzzleFlash.Position = new Vector3(this.moduleAttachedTo.Center.X, this.moduleAttachedTo.Center.Y, -45f);
                        this.flash.WorldMatrix = Matrix.Identity * Matrix.CreateRotationZ(this.rotation) * Matrix.CreateTranslation(this.MuzzleFlash.Position);
                        this.MuzzleFlash.World = Matrix.Identity * Matrix.CreateTranslation(this.MuzzleFlash.Position);
                    }
                }
                if ((double)this.flashTimer <= 0.0 && this.muzzleFlashAdded)
                {
                    lock (GlobalStats.ObjectManagerLocker)
                        Projectile.universeScreen.ScreenManager.inter.LightManager.Remove((ILight)this.MuzzleFlash);
                }
                base.Update(elapsedTime);
            }
        }
	}
}