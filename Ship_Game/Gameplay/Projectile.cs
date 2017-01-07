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
using Ship_Game.AI;

namespace Ship_Game.Gameplay
{
	public class Projectile : GameplayObject, IDisposable
	{
		public float ShieldDamageBonus;
		public float ArmorDamageBonus;
		public byte ArmorPiercing;
		public static GameContentManager contentManager;
		public Ship owner;
		public bool IgnoresShields;
		public string WeaponType;
		private MissileAI missileAI;
		private Planet planet;
		public float velocityMaximum;
		public float speed;
		public float range;
		public float damageAmount;
		public float damageRadius;
        public float explosionradiusmod;
		public float duration;
		public bool explodes;
        public ShipModule moduleAttachedTo;
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
		private DroneAI droneAI;
		public Weapon weapon;
		public string texturePath;
		public string modelPath;
		private float zStart = -25f;
		private float particleDelay;
		private PointLight light;
		public bool firstRun = true;
		private MuzzleFlash flash;
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
        private bool wasAddedToSceneGraph;
		private bool LightWasAddedToSceneGraph;
		private bool muzzleFlashAdded;
        public Vector2 FixedError;
        public bool ErrorSet = false;
        public bool flashExplode;
        public bool isSecondary;

		public Ship   Owner  => owner;
	    public Planet Planet => planet;

	    public Projectile(Ship owner, Vector2 direction, ShipModule moduleAttachedTo)
		{
			this.loyalty = owner.loyalty;
			this.owner = owner;
			if (!owner.isInDeepSpace)
			{
				this.System = owner.System;
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
        public void ProjectileRecreate(Ship owner, Vector2 direction, ShipModule moduleAttachedTo)
        {
            this.loyalty = owner.loyalty;
            this.owner = owner;
            if (!owner.isInDeepSpace)
            {
                this.System = owner.System;
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
			this.System = p.system;
			base.Position = p.Position;
			this.loyalty = p.Owner;
			this.Velocity = direction;
			this.Rotation = p.Position.RadiansToTarget(p.Position + Velocity);
			this.Center = p.Position;
			this.emitter.Position = new Vector3(p.Position, 0f);
		}

		public Projectile(Ship owner, Vector2 direction)
		{
			this.owner = owner;
			this.loyalty = owner.loyalty;
			this.Rotation = (float)Math.Acos((double)Vector2.Dot(Vector2.UnitY, direction)) - 3.14159274f;
			if (direction.X > 0f)
			{
				Projectile projectile = this;
				projectile.Rotation = projectile.Rotation * -1f;
			}
		}

		public Projectile()
		{
		}

		public void DamageMissile(GameplayObject source, float damageAmount)
		{
            this.Health -= damageAmount;
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
				Vector3 vector31 = new Vector3(this.Velocity.X, base.Velocity.Y, 0f);
				if (this.light != null)
				{
					lock (GlobalStats.ObjectManagerLocker)
					{
						Projectile.universeScreen.ScreenManager.inter.LightManager.Remove(this.light);
					}
				}
				if (!string.IsNullOrEmpty(this.InFlightCue) && this.inFlight != null)
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
						if (!string.IsNullOrEmpty(this.dieCueName))
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
						if (this.System == null)
						{
                            UniverseScreen.DeepSpaceManager.ProjectileExplode(this, this.damageAmount, this.damageRadius);
						}
						else
						{
                            this.System.spatialManager.ProjectileExplode(this, this.damageAmount, this.damageRadius);
						}
					}
					else if (!string.IsNullOrEmpty(this.dieCueName))
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
							ExplosionManager.AddExplosion(new Vector3(base.Position, -50f), this.damageRadius * this.explosionradiusmod, 2.5f, 0.2f);
                            if (this.flashExplode)
                            {
                                Projectile.universeScreen.flash.AddParticleThreadB(new Vector3(base.Position, -50f), Vector3.Zero);
                            }
						}
						if (this.System == null)
						{
                            UniverseScreen.DeepSpaceManager.ProjectileExplode(this, this.damageAmount, this.damageRadius);
						}
						else
						{
                            this.System.spatialManager.ProjectileExplode(this, this.damageAmount, this.damageRadius);
						}
					}
					else if (this.System == null)
					{
                        UniverseScreen.DeepSpaceManager.ProjectileExplode(this, this.damageAmount, this.damageRadius);
					}
					else
					{
                        this.System.spatialManager.ProjectileExplode(this, this.damageAmount, this.damageRadius);
					}
				}
				else if (this.weapon.FakeExplode && Projectile.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
				{
					ExplosionManager.AddExplosion(new Vector3(base.Position, -50f), this.damageRadius * this.explosionradiusmod, 2.5f, 0.2f);
                    if (this.flashExplode)
                    {
                        Projectile.universeScreen.flash.AddParticleThreadB(new Vector3(base.Position, -50f), Vector3.Zero);
                    }
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
			if (this.System == null)
			{
				UniverseScreen.DeepSpaceManager.CollidableProjectiles.QueuePendingRemoval(this);
			}
			else
			{
				this.System.spatialManager.CollidableProjectiles.QueuePendingRemoval(this);
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
            this.Velocity = initialSpeed * direction;
			if (this.moduleAttachedTo == null)
			{
				this.Center = pos;
				this.Rotation = Center.RadiansToTarget(Center + Velocity);
			}
			else
			{
				this.Center = this.moduleAttachedTo.Center;
				this.Rotation = moduleAttachedTo.Center.RadiansToTarget(moduleAttachedTo.Center + Velocity);
			}
			this.Radius = 1f;
            this.velocityMaximum = initialSpeed + (Owner?.Velocity.Length() ?? 0f);
			this.Velocity = Vector2.Normalize(this.Velocity) * this.velocityMaximum;
            this.duration = this.range / initialSpeed * 1.2f;
			this.initialDuration = this.duration;
			if (this.weapon.Animated == 1)
			{
				this.switchFrames = this.initialDuration / (float)this.weapon.Frames;
				if (weapon.LoopAnimation == 1)
				{
					AnimationFrame = UniverseRandom.InRange(weapon.Frames);
				}
			}
			if (this.owner.loyalty.data.ArmorPiercingBonus > 0 && (this.weapon.WeaponType == "Missile" || this.weapon.WeaponType == "Ballistic Cannon"))
			{
				this.ArmorPiercing += (byte)this.owner.loyalty.data.ArmorPiercingBonus;
			}
			Projectile projectile1 = this;
			projectile1.particleDelay = projectile1.particleDelay + this.weapon.particleDelay;
			if (this.weapon.Tag_Guided)
			{
				this.missileAI = new MissileAI(this);
			}
			if (this.WeaponType != "Missile" && this.WeaponType != "Drone" && this.WeaponType != "Rocket" || (this.System == null || !this.System.isVisible) && !this.isInDeepSpace)
			{
				if (owner != null && owner.loyalty.data.Traits.Blind > 0)
				{
					if (UniverseRandom.IntBetween(0, 10) <= 1)
						Miss = true;
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
			if (this.System == null)
			{
				UniverseScreen.DeepSpaceManager.CollidableProjectiles.Add(this);
				if (this.weapon.Tag_Intercept && Projectile.universeScreen != null)
				{
					Projectile.universeScreen.DSProjectilesToAdd.Add(this);
				}
			}
			else
			{
				//lock (GlobalStats.BucketLock)
				{
					this.System.spatialManager.CollidableProjectiles.Add(this);
					if (this.System.spatialManager.CellSize > 0)
					{
						this.System.spatialManager.RegisterObject(this);
					}
					if (this.weapon.Tag_Intercept)
					{
						this.System.spatialManager.CollidableObjects.Add(this);
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
			this.Velocity = (initialSpeed * direction) + (this.owner != null ? this.owner.Velocity : Vector2.Zero);
			this.Radius = 1f;
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
					this.AnimationFrame = UniverseRandom.InRange(weapon.Frames);
				}
			}
			Projectile projectile1 = this;
			projectile1.particleDelay = projectile1.particleDelay + this.weapon.particleDelay;
			if (this.weapon.IsRepairDrone)
			{
				this.droneAI = new DroneAI(this);
			}
			if (this.System == null)
			{
				Projectile.universeScreen.DSProjectilesToAdd.Add(this);
				UniverseScreen.DeepSpaceManager.CollidableProjectiles.Add(this);
			}
			else
			{
                //this.system.spatialManager.CollidableProjectiles.Add(this);
                //this.system.spatialManager.CollidableObjects.Add(this);
                //lock (GlobalStats.BucketLock)
                {
                    this.System.spatialManager.CollidableProjectiles.Add(this);
                    this.System.spatialManager.RegisterObject(this);
                    this.System.spatialManager.CollidableObjects.Add(this);
                }
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
			this.Velocity = (initialSpeed * direction) + (this.owner != null ? this.owner.Velocity : Vector2.Zero);
			this.Radius = 1f;
            this.velocityMaximum = initialSpeed + (this.Owner != null ? this.Owner.Velocity.Length() : 0f);
            this.duration = this.range / initialSpeed * 2f;
			this.initialDuration = this.duration;
			if (this.moduleAttachedTo != null)
			{
				this.Center = this.moduleAttachedTo.Center;
				if (moduleAttachedTo.facing == 0f)
				{
					Rotation = Owner?.Rotation ?? 0f;
				}
				else
				{
					Rotation = moduleAttachedTo.Center.RadiansToTarget(moduleAttachedTo.Center + Velocity);
				}
			}
			if (this.weapon.Animated == 1)
			{
				this.switchFrames = this.initialDuration / (float)this.weapon.Frames;
				if (this.weapon.LoopAnimation == 1)
				{
					AnimationFrame = UniverseRandom.InRange(weapon.Frames);
				}
			}
			Projectile projectile1 = this;
			projectile1.particleDelay = projectile1.particleDelay + this.weapon.particleDelay;
			if (this.weapon.Tag_Guided)
			{
				this.missileAI = new MissileAI(this);
				this.missileAI.SetTarget(Target);
			}
            if (this.ProjSO != null && (this.WeaponType == "Missile" || this.WeaponType == "Drone" || this.WeaponType == "Rocket") && (this.System != null && this.System.isVisible || this.isInDeepSpace))
			{
				this.wasAddedToSceneGraph = true;
				lock (GlobalStats.ObjectManagerLocker)
				{
					Projectile.universeScreen.ScreenManager.inter.ObjectManager.Submit(this.ProjSO);
				}
			}
			if (this.System == null)
			{
				Projectile.universeScreen.DSProjectilesToAdd.Add(this);
				UniverseScreen.DeepSpaceManager.CollidableProjectiles.Add(this);
			}
			else
			{
				//lock (GlobalStats.BucketLock)
				{
					this.System.spatialManager.CollidableProjectiles.Add(this);
					this.System.spatialManager.RegisterObject(this);
					this.System.spatialManager.CollidableObjects.Add(this);
				}
			}
			base.Initialize();
		}

		public void InitializeMissilePlanet(float initialSpeed, Vector2 direction, GameplayObject Target, Ship_Game.Planet p)
		{
			this.direction = direction;
			this.Center = p.Position;
			this.zStart = -2500f;
			this.Velocity = (initialSpeed * direction) + (this.owner != null ? this.owner.Velocity : Vector2.Zero);
			this.Radius = 1f;
			this.velocityMaximum = initialSpeed + (this.owner != null ? this.owner.Velocity.Length() : 0f);
			this.duration = this.range / initialSpeed * 2f;
			this.initialDuration = this.duration;
			this.planet = p;
			if (this.weapon.Animated == 1)
			{
				this.switchFrames = this.initialDuration / (float)this.weapon.Frames;
				if (this.weapon.LoopAnimation == 1)
				{
					AnimationFrame = UniverseRandom.InRange(weapon.Frames);
				}
			}
			Projectile projectile1 = this;
			projectile1.particleDelay = projectile1.particleDelay + this.weapon.particleDelay;
			if (this.weapon.Tag_Guided)
			{
				this.missileAI = new MissileAI(this);
				this.missileAI.SetTarget(Target);
			}
            if (this.ProjSO != null && (this.WeaponType == "Missile" || this.WeaponType == "Drone" || this.WeaponType == "Rocket") && (this.System != null && this.System.isVisible || this.isInDeepSpace))
			{
				this.wasAddedToSceneGraph = true;
				lock (GlobalStats.ObjectManagerLocker)
				{
					Projectile.universeScreen.ScreenManager.inter.ObjectManager.Submit(this.ProjSO);
				}
			}
			if (this.System == null)
			{
				Projectile.universeScreen.DSProjectilesToAdd.Add(this);
				UniverseScreen.DeepSpaceManager.CollidableProjectiles.Add(this);
			}
			else
			{
				//lock (GlobalStats.BucketLock)
				{
					this.System.spatialManager.CollidableProjectiles.Add(this);
					this.System.spatialManager.RegisterObject(this);
					this.System.spatialManager.CollidableObjects.Add(this);
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
			this.Velocity = (initialSpeed * direction) + (this.owner != null ? this.owner.Velocity : Vector2.Zero);
			this.Radius = 1f;
			this.velocityMaximum = initialSpeed + (this.owner != null ? this.owner.Velocity.Length() : 0f);
			this.Velocity = Vector2.Normalize(this.Velocity) * this.velocityMaximum;
			this.duration = this.range / initialSpeed * 1.25f;
			this.initialDuration = this.duration;
			if (this.weapon.Animated == 1)
			{
				this.switchFrames = this.initialDuration / (float)this.weapon.Frames;
				if (this.weapon.LoopAnimation == 1)
				{
					AnimationFrame = UniverseRandom.InRange(weapon.Frames);
				}
			}
			Projectile projectile1 = this;
			projectile1.particleDelay = projectile1.particleDelay + this.weapon.particleDelay;
			if (this.weapon.Tag_Guided)
			{
				this.missileAI = new MissileAI(this);
			}
            if ((this.WeaponType == "Missile" || this.WeaponType == "Drone" || this.WeaponType == "Rocket") && (this.System != null && this.System.isVisible || this.isInDeepSpace) && this.ProjSO != null)
			{
				this.wasAddedToSceneGraph = true;
				lock (GlobalStats.ObjectManagerLocker)
				{
					Projectile.universeScreen.ScreenManager.inter.ObjectManager.Submit(this.ProjSO);
				}
			}
			if (this.System == null)
			{
				Projectile.universeScreen.DSProjectilesToAdd.Add(this);
				UniverseScreen.DeepSpaceManager.CollidableProjectiles.Add(this);
			}
			else
			{
				//lock (GlobalStats.BucketLock)
				{
					this.System.spatialManager.CollidableProjectiles.Add(this);
					this.System.spatialManager.CollidableObjects.Add(this);
				}
			}
			base.Initialize();
		}

		public void LoadContent(string texturePath, string modelPath)
		{
			this.texturePath = texturePath;
			this.modelPath = modelPath;
            //if(this.owner.Projectiles.Count <20)
            //if (Ship.universeScreen !=null && RandomMath.InRange((int)(Ship.universeScreen.Lag *100)) >3 )
            //    return;
            this.ProjSO = new SceneObject(Ship_Game.ResourceManager.ProjectileMeshDict[modelPath])
            {
                Visibility = ObjectVisibility.Rendered,
                ObjectType = ObjectType.Dynamic
            };
			if (Projectile.universeScreen != null && this.ProjSO !=null)
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
                if (this.weapon.WeaponEffectType == "SmokeTrail")
                {
                    this.trailEmitter = new ParticleEmitter(Projectile.universeScreen.projectileTrailParticles, 500f, new Vector3(this.Center, -this.zStart));
                }
                if (this.weapon.WeaponEffectType == "MuzzleSmoke")
                {
                    this.firetrailEmitter = new ParticleEmitter(Projectile.universeScreen.projectileTrailParticles, 1000f, new Vector3(this.Center, 0f));
                }
                if (this.weapon.WeaponEffectType == "MuzzleSmokeFire")
                {
                    this.firetrailEmitter = new ParticleEmitter(Projectile.universeScreen.projectileTrailParticles, 1000f, new Vector3(this.Center, 0f));
                    this.trailEmitter = new ParticleEmitter(Projectile.universeScreen.fireTrailParticles, 750f, new Vector3(this.Center, -this.zStart));
                }
                if (this.weapon.WeaponEffectType == "FullSmokeMuzzleFire")
                {
                    this.trailEmitter = new ParticleEmitter(Projectile.universeScreen.projectileTrailParticles, 500f, new Vector3(this.Center, -this.zStart));
                    this.firetrailEmitter = new ParticleEmitter(Projectile.universeScreen.fireTrailParticles, 500f, new Vector3(this.Center, -this.zStart));
                }

			}
			if (this.weapon.Animated == 1 && this.ProjSO !=null)
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
						float ran = UniverseRandom.RandomBetween(0f, 1f);
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
                    ShipModule module = target as ShipModule;
                    if (module != null && module.GetParent().loyalty == this.loyalty && !this.weapon.HitsFriendlies || module == null)
                        return false;

					if (this.weapon.TruePD)
					{
						this.DieNextFrame = true;
						return true;
					}
                    if (module.GetParent().shipData.Role == ShipData.RoleName.fighter && module.GetParent().loyalty.data.Traits.DodgeMod > 0f)
                    {
                        if (UniverseRandom.RandomBetween(0f, 100f) < module.GetParent().loyalty.data.Traits.DodgeMod * 100f)
                        {
                            this.Miss = true;
                        }
                    }
                    if (this.Miss)
                    {
                        return false;
                    }
                    // Moving this to the Damage function - doesn't seem to be working? Also seems nonsensical.
                    /*
                    if (module.ModuleType == ShipModuleType.Armor || (module.ModuleType == ShipModuleType.Dummy && module.ParentOfDummy.ModuleType == ShipModuleType.Armor))
					{
                        this.damageRadius -= module.GetParent().loyalty.data.ExplosiveRadiusReduction * this.damageRadius;
                        this.damageAmount -= module.GetParent().loyalty.data.ExplosiveRadiusReduction * this.damageAmount;
                        this.damageAmount *= this.weapon.EffectVsArmor;
                        this.damageAmount *= this.damageAmount + this.ArmorDamageBonus;
					}
                    
                    if (module.ModuleType == ShipModuleType.Shield && module.shield_power > 0)
                    {
                        this.damageAmount *= this.weapon.EffectVSShields;
                        this.damageAmount *= this.damageAmount + this.ShieldDamageBonus;
                        //projectiles penetrate weak shields
                        if (this.damageAmount > module.shield_power)
                        {
                            float remainder = 0;
                            module.Damage(this, this.damageAmount, ref remainder);
                            if (remainder > 0)
                            {
                                this.damageAmount = remainder;
                                return false;
                            }
                            else
                            {
                                this.damageAmount = 0;
                                this.explodes = false;
                                this.DieNextFrame = true;
                                return base.Touch(target);
                            }
                        }
                    }
                     */
                    //Non exploding projectiles should go through multiple modules if it has enough damage
                    if (!this.explodes && module.Active)
                    {
                        float remainder;

                        //Doc: If module has resistance to Armour Piercing effects, deduct that from the projectile's AP before starting AP and damage checks
                        if (module.APResist > 0)
                        {
                            this.ArmorPiercing -= (byte)module.APResist;
                            if (this.ArmorPiercing < 0)
                                this.ArmorPiercing = 0;
                        }

                        if (this.ArmorPiercing == 0 || !(module.ModuleType == ShipModuleType.Armor || (module.ModuleType == ShipModuleType.Dummy && module.ParentOfDummy.ModuleType == ShipModuleType.Armor)))
                        {
                            remainder = 0;
                            module.Damage(this, this.damageAmount, ref remainder);
                        }
                        else
                        {
                            this.ArmorPiercing--;
                            remainder = this.damageAmount;
                        }
                        if (remainder > 0)
                        {
                            this.damageAmount = remainder;
                            bool SlotFound;
                            int depth = 10;
                            Vector2 UnitVector = this.Velocity;
                            while (this.damageAmount > 0)
                            {
                                UnitVector.Normalize();
                                UnitVector *= depth;
                                SlotFound = false;
                                foreach (ModuleSlot slot in module.GetParent().ModuleSlotList)
                                {
                                    if (Vector2.Distance(this.Center + UnitVector, slot.module.Center) < 8f)
                                    {
                                        SlotFound = true;
                                        if (slot.module.Active)
                                        {
                                            if (this.ArmorPiercing > 0 && (slot.module.ModuleType == ShipModuleType.Armor || (slot.module.ModuleType == ShipModuleType.Dummy && slot.module.ParentOfDummy.ModuleType == ShipModuleType.Armor)))
                                                break;
                                            else
                                            {
                                                remainder = 0;
                                                slot.module.Damage(this, this.damageAmount, ref remainder);
                                                if (remainder > 0)
                                                    this.damageAmount = remainder;
                                                else
                                                    this.damageAmount = 0f;
                                            }
                                        }
                                        break;
                                    }
                                }
                                //Slot found means it is still in the ship
                                if (SlotFound)
                                {
                                    depth += 8;
                                    this.ArmorPiercing--;
                                }
                                else
                                    break;
                            }
                        }
                    }
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
                        Vector3 random = UniverseRandom.Vector3D(250f) * new Vector3(right.X, right.Y, 1f);
						universeScreen.flameParticles.AddParticleThreadA(center, random);

                        random = UniverseRandom.Vector3D(150f) + new Vector3(-forward.X, -forward.Y, 0f);
						universeScreen.flameParticles.AddParticleThreadA(center, random);
					}
				}
                if (this.WeaponEffectType == "MuzzleBlast") // currently unused
                {
                    Vector3 center = new Vector3(this.Center.X, this.Center.Y, -100f);
                    Vector2 forward = new Vector2((float)Math.Sin((double)base.Rotation), -(float)Math.Cos((double)base.Rotation));
                    Vector2 right = new Vector2(-forward.Y, forward.X);
                    right = Vector2.Normalize(right);
                    for (int i = 0; i < 20; i++)
                    {
                        Vector3 random = UniverseRandom.Vector3D(500f) * new Vector3(right.X, right.Y, 1f);
                        universeScreen.fireTrailParticles.AddParticleThreadA(center, random);

                        random = new Vector3(-forward.X, -forward.Y, 0f) 
                            + new Vector3(UniverseRandom.RandomBetween(-500f, 500f), 
                                        UniverseRandom.RandomBetween(-500f, 500f), 
                                        UniverseRandom.RandomBetween(-150f, 150f));
                        universeScreen.fireTrailParticles.AddParticleThreadA(center, random);
                    }
                }
                else if (this.WeaponType == "Ballistic Cannon")
                {
                    ShipModule shipModule = target as ShipModule;
                    if (shipModule != null && shipModule.ModuleType != ShipModuleType.Shield)
                        AudioManager.PlayCue("sd_impact_bullet_small_01", universeScreen.listener, emitter);
                }
			}
			this.DieNextFrame = true;
			return base.Touch(target);
		}

        public override void Update(float elapsedTime)
        {
            if(this.DieNextFrame && this.Active)
            {
                this.Die(this, false);
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
                    if (this.weapon.LoopAnimation == 0 && this.frameTimer > this.switchFrames)
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
                if (!string.IsNullOrEmpty(this.InFlightCue) && this.inFlight == null)
                {
                    this.inFlight = AudioManager.GetCue(this.InFlightCue);
                    this.inFlight.Apply3D(Projectile.universeScreen.listener, this.emitter);
                    this.inFlight.Play();
                }
                this.particleDelay -= elapsedTime;
                if (this.duration > 0)
                {
                    this.duration -= elapsedTime;
                    if (this.duration < 0)
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
                if (this.ProjSO != null && (this.WeaponType == "Rocket" || this.WeaponType == "Drone" || this.WeaponType == "Missile") && (this.System != null && this.System.isVisible && (!this.wasAddedToSceneGraph && Projectile.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)))
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
                if (this.ProjSO !=null && (this.isInDeepSpace || this.System != null && this.System.isVisible) && Projectile.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
                {
                    if (this.zStart < -25.0)
                        this.zStart += this.velocityMaximum * elapsedTime;
                    else
                        this.zStart = -25f;
                    this.ProjSO.World = Matrix.Identity * Matrix.CreateScale(this.Scale) * Matrix.CreateRotationZ(this.Rotation) * Matrix.CreateTranslation(this.Center.X, this.Center.Y, -this.zStart);
                    this.WorldMatrix = this.ProjSO.World;
                }
                Vector3 newPosition = new Vector3(this.Center.X, this.Center.Y, -this.zStart);
                if (this.firetrailEmitter != null && this.WeaponEffectType == "Plasma" && (this.duration > this.initialDuration * 0.699999988079071 && this.particleDelay <= 0.0))
                    this.firetrailEmitter.UpdateProjectileTrail(elapsedTime, newPosition, new Vector3(this.Velocity, 0.0f) + Vector3.Normalize(new Vector3(this.direction, 0.0f)) * this.speed * 1.75f);

                if (this.firetrailEmitter != null && this.WeaponEffectType == "MuzzleSmoke" && (this.duration > this.initialDuration * 0.97 && this.particleDelay <= 0.0))
                    this.firetrailEmitter.UpdateProjectileTrail(elapsedTime, newPosition, new Vector3(this.Velocity, 0.0f) + Vector3.Normalize(new Vector3(this.direction, 0.0f)) * this.speed * 1.75f);

                if (this.firetrailEmitter != null && this.WeaponEffectType == "MuzzleSmokeFire" && (this.duration > this.initialDuration * 0.97 && this.particleDelay <= 0.0))
                {
                    this.firetrailEmitter.UpdateProjectileTrail(elapsedTime, newPosition, new Vector3(this.Velocity, 0.0f) + Vector3.Normalize(new Vector3(this.direction, 0.0f)) * this.speed * 1.75f);
                }
                if (this.trailEmitter != null && this.WeaponEffectType == "MuzzleSmokeFire" && (this.duration > this.initialDuration * 0.96 && this.particleDelay <= 0.0))
                {
                    this.trailEmitter.Update(elapsedTime, newPosition);
                }

                if (this.firetrailEmitter != null && this.WeaponEffectType == "FullSmokeMuzzleFire")
                {
                    this.trailEmitter.Update(elapsedTime, newPosition);
                }
                if (this.trailEmitter != null && this.WeaponEffectType == "FullSmokeMuzzleFire" && (this.duration > this.initialDuration * 0.96 && this.particleDelay <= 0.0))
                {
                    this.firetrailEmitter.Update(elapsedTime, newPosition);
                }

                if (this.firetrailEmitter != null && this.WeaponEffectType == "RocketTrail")
                    this.firetrailEmitter.Update(elapsedTime, newPosition);

                if (this.firetrailEmitter != null && this.WeaponEffectType == "SmokeTrail")
                    this.firetrailEmitter.Update(elapsedTime, newPosition);

                if (this.trailEmitter != null && this.WeaponEffectType != "MuzzleSmokeFire" && this.WeaponEffectType != "FullSmokeMuzzleFire")
                    this.trailEmitter.Update(elapsedTime, newPosition);

                if (this.System != null && this.System.isVisible && (this.light == null && this.weapon.Light != null) && (Projectile.universeScreen.viewState < UniverseScreen.UnivScreenState.SystemView && !this.LightWasAddedToSceneGraph))
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
                    this.light.AddTo(Empire.Universe);
                }
                else if (this.weapon.Light != null && this.LightWasAddedToSceneGraph)
                {
                    this.light.Position = new Vector3(this.Center.X, this.Center.Y, -25f);
                    this.light.World = Matrix.Identity * Matrix.CreateTranslation(this.light.Position);
                }
                if (this.moduleAttachedTo != null)
                {
                    if (this.owner.ProjectilesFired.Count < 30 && this.System != null && (this.System.isVisible && this.MuzzleFlash == null) && (this.moduleAttachedTo.InstalledWeapon.MuzzleFlash != null && Projectile.universeScreen.viewState < UniverseScreen.UnivScreenState.SystemView && !this.muzzleFlashAdded))
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
                        this.MuzzleFlash.AddTo(Empire.Universe);
                        this.flashTimer -= elapsedTime;
                        this.flash = new MuzzleFlash();
                        this.flash.WorldMatrix = Matrix.Identity * Matrix.CreateRotationZ(this.Rotation) * Matrix.CreateTranslation(this.MuzzleFlash.Position);
                        this.flash.Owner = (GameplayObject)this;
                        lock (GlobalStats.ExplosionLocker)
                            MuzzleFlashManager.FlashList.Add(this.flash);
                    }
                    else if (this.flashTimer > 0 && this.moduleAttachedTo.InstalledWeapon.MuzzleFlash != null && this.muzzleFlashAdded)
                    {
                        this.flashTimer -= elapsedTime;
                        this.MuzzleFlash.Position = new Vector3(this.moduleAttachedTo.Center.X, this.moduleAttachedTo.Center.Y, -45f);
                        this.flash.WorldMatrix = Matrix.Identity * Matrix.CreateRotationZ(this.Rotation) * Matrix.CreateTranslation(this.MuzzleFlash.Position);
                        this.MuzzleFlash.World = Matrix.Identity * Matrix.CreateTranslation(this.MuzzleFlash.Position);
                    }
                }
                if (this.flashTimer <= 0  && this.muzzleFlashAdded)
                {
                    lock (GlobalStats.ObjectManagerLocker)
                        Projectile.universeScreen.ScreenManager.inter.LightManager.Remove(MuzzleFlash);
                }
                base.Update(elapsedTime);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Projectile() { Dispose(false); }

        protected virtual void Dispose(bool disposing)
        {
            droneAI?.Dispose(ref droneAI);
        }
	}
}