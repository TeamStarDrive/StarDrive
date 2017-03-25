using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;
using System;
using Ship_Game.AI;
using Ship_Game.Debug;

namespace Ship_Game.Gameplay
{
    public class Projectile : GameplayObject, IDisposable
    {
        public float ShieldDamageBonus;
        public float ArmorDamageBonus;
        public int ArmorPiercing;
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
        public string TexturePath;
        public string ModelPath;
        private float zStart = -25f;
        private float particleDelay;
        private PointLight light;
        public bool firstRun = true;
        private MuzzleFlash flash;
        private PointLight MuzzleFlash;
        private float flashTimer = 0.142f;
        public float Scale = 1f;
        private int AnimationFrame;
        private const string fmt = "00000.##";
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

        public Projectile()
        {
        }

        public Projectile(Ship owner, Vector2 direction, ShipModule moduleAttachedTo)
        {
            Init(owner, direction, moduleAttachedTo);
        }
        public void Init(Ship owner, Vector2 direction, ShipModule moduleAttachedTo)
        {
            loyalty = owner.loyalty;
            this.owner = owner;
            SetSystem(owner.System);
            this.moduleAttachedTo = moduleAttachedTo;
            Center = Position = moduleAttachedTo.Center;
            emitter.Position = new Vector3(Position, 0f);
        }

        public Projectile(Planet p, Vector2 direction)
        {
            SetSystem(p.system);
            Position = p.Position;
            loyalty  = p.Owner;
            Velocity = direction;
            Rotation = p.Position.RadiansToTarget(p.Position + Velocity);
            Center   = p.Position;
            emitter.Position = new Vector3(p.Position, 0f);
        }

        public Projectile(Ship owner, Vector2 direction)
        {
            this.owner = owner;
            loyalty = owner.loyalty;
            Rotation = (float)Math.Acos(Vector2.Dot(Vector2.UnitY, direction)) - 3.14159274f;
            if (direction.X > 0f) Rotation = -Rotation;
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
                        Empire.Universe.ScreenManager.inter.LightManager.Remove(this.light);
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
                        if (!cleanupOnly && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
                        {
                            ExplosionManager.AddProjectileExplosion(new Vector3(base.Position, -50f), this.damageRadius * 4.5f, 2.5f, 0.2f, this.weapon.ExpColor);
                            Empire.Universe.flash.AddParticleThreadB(new Vector3(base.Position, -50f), Vector3.Zero);
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
                        if (Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
                        {
                            this.dieCue = AudioManager.GetCue(this.dieCueName);
                            if (this.dieCue != null)
                            {
                                this.dieCue.Apply3D(GameplayObject.audioListener, this.emitter);
                                this.dieCue.Play();
                            }
                        }
                        if (!cleanupOnly && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
                        {
                            ExplosionManager.AddExplosion(new Vector3(base.Position, -50f), this.damageRadius * this.explosionradiusmod, 2.5f, 0.2f);
                            if (this.flashExplode)
                            {
                                Empire.Universe.flash.AddParticleThreadB(new Vector3(base.Position, -50f), Vector3.Zero);
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
                else if (this.weapon.FakeExplode && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
                {
                    ExplosionManager.AddExplosion(new Vector3(base.Position, -50f), this.damageRadius * this.explosionradiusmod, 2.5f, 0.2f);
                    if (this.flashExplode)
                    {
                        Empire.Universe.flash.AddParticleThreadB(new Vector3(base.Position, -50f), Vector3.Zero);
                    }
                }
            }
            if (this.ProjSO != null && this.Active)
            {
                lock (GlobalStats.ObjectManagerLocker)
                {
                    Empire.Universe.ScreenManager.inter.ObjectManager.Remove(this.ProjSO);
                }
                this.ProjSO.Clear();
            }
            if (this.droneAI != null)
            {
                foreach (Beam beam in this.droneAI.Beams)
                {
                    beam.Die(this, true);
                }
                droneAI.Beams.Clear();
            }
            if (muzzleFlashAdded)
            {
                lock (GlobalStats.ObjectManagerLocker)
                {
                    Empire.Universe.ScreenManager.inter.LightManager.Remove(this.MuzzleFlash);
                }
            }
            SetSystem(null);
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
            if (owner.loyalty.data.ArmorPiercingBonus > 0 && (weapon.WeaponType == "Missile" || weapon.WeaponType == "Ballistic Cannon"))
            {
                ArmorPiercing += owner.loyalty.data.ArmorPiercingBonus;
            }
            Projectile projectile1 = this;
            projectile1.particleDelay = projectile1.particleDelay + weapon.particleDelay;
            if (weapon.Tag_Guided)
            {
                missileAI = new MissileAI(this);
            }
            if (WeaponType != "Missile" && WeaponType != "Drone" && WeaponType != "Rocket" || (System == null || !System.isVisible) && !InDeepSpace)
            {
                if (owner != null && owner.loyalty.data.Traits.Blind > 0)
                {
                    if (UniverseRandom.IntBetween(0, 10) <= 1)
                        Miss = true;
                }
            }
            else if (ProjSO != null)
            {
                wasAddedToSceneGraph = true;
                lock (GlobalStats.ObjectManagerLocker)
                {
                    if (Empire.Universe != null)
                    {
                        Empire.Universe.ScreenManager.inter.ObjectManager.Submit(ProjSO);
                    }
                }
            }
            SetSystem(System);
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
            SetSystem(System);
            base.Initialize();
        }

        public void InitializeMissile(float initialSpeed, Vector2 direction, GameplayObject Target)
        {
            this.direction = direction;
            if (moduleAttachedTo != null)
            {
                Center = moduleAttachedTo.Center;
            }
            this.Velocity = (initialSpeed * direction) + (owner?.Velocity ?? Vector2.Zero);
            this.Radius = 1f;
            this.velocityMaximum = initialSpeed + (Owner?.Velocity.Length() ?? 0f);
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
            if (ProjSO != null && (WeaponType == "Missile" || WeaponType == "Drone" || WeaponType == "Rocket") && (System != null && System.isVisible || InDeepSpace))
            {
                wasAddedToSceneGraph = true;
                lock (GlobalStats.ObjectManagerLocker)
                {
                    Empire.Universe.ScreenManager.inter.ObjectManager.Submit(ProjSO);
                }
            }
            SetSystem(System);
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
            if (ProjSO != null && (WeaponType == "Missile" || WeaponType == "Drone" || WeaponType == "Rocket") && (System != null && System.isVisible || InDeepSpace))
            {
                this.wasAddedToSceneGraph = true;
                lock (GlobalStats.ObjectManagerLocker)
                {
                    Empire.Universe.ScreenManager.inter.ObjectManager.Submit(this.ProjSO);
                }
            }
            SetSystem(System);
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
            if ((WeaponType == "Missile" || WeaponType == "Drone" || WeaponType == "Rocket") && (System != null && System.isVisible || InDeepSpace) && ProjSO != null)
            {
                wasAddedToSceneGraph = true;
                lock (GlobalStats.ObjectManagerLocker)
                {
                    Empire.Universe.ScreenManager.inter.ObjectManager.Submit(this.ProjSO);
                }
            }
            SetSystem(System);
            base.Initialize();
        }

        public void LoadContent(string texPath, string modelFilePath)
        {
            TexturePath = texPath;
            ModelPath = modelFilePath;

            ProjSO = new SceneObject(Ship_Game.ResourceManager.ProjectileMeshDict[modelFilePath])
            {
                Visibility = ObjectVisibility.Rendered,
                ObjectType = ObjectType.Dynamic
            };
            if (Empire.Universe != null && ProjSO !=null)
            {
                switch (weapon.WeaponEffectType)
                {
                    case "RocketTrail":
                        trailEmitter = new ParticleEmitter(Empire.Universe.projectileTrailParticles, 500f, new Vector3(Center, -zStart));
                        firetrailEmitter = new ParticleEmitter(Empire.Universe.fireTrailParticles, 500f, new Vector3(Center, -zStart));
                        break;
                    case "Plasma":
                        firetrailEmitter = new ParticleEmitter(Empire.Universe.flameParticles, 500f, new Vector3(Center, 0f));
                        break;
                    case "SmokeTrail":
                        trailEmitter = new ParticleEmitter(Empire.Universe.projectileTrailParticles, 500f, new Vector3(Center, -zStart));
                        break;
                    case "MuzzleSmoke":
                        firetrailEmitter = new ParticleEmitter(Empire.Universe.projectileTrailParticles, 1000f, new Vector3(Center, 0f));
                        break;
                    case "MuzzleSmokeFire":
                        firetrailEmitter = new ParticleEmitter(Empire.Universe.projectileTrailParticles, 1000f, new Vector3(Center, 0f));
                        trailEmitter = new ParticleEmitter(Empire.Universe.fireTrailParticles, 750f, new Vector3(Center, -zStart));
                        break;
                    case "FullSmokeMuzzleFire":
                        trailEmitter = new ParticleEmitter(Empire.Universe.projectileTrailParticles, 500f, new Vector3(Center, -zStart));
                        firetrailEmitter = new ParticleEmitter(Empire.Universe.fireTrailParticles, 500f, new Vector3(Center, -zStart));
                        break;
                }

            }
            if (weapon.Animated == 1 && ProjSO !=null)
            {
                TexturePath = weapon.AnimationPath + AnimationFrame.ToString(fmt);
            }
        }

        public override bool Touch(GameplayObject target)
        {
            if (Miss)
            {
                return false;
            }
            if (target != null)
            {
                if (target == owner && !weapon.HitsFriendlies)
                {
                    return false;
                }
                var projectile = target as Projectile;
                if (projectile != null)
                {
                    if (owner != null && projectile.loyalty == owner.loyalty)
                    {
                        return false;
                    }
                    if (projectile.WeaponType == "Missile")
                    {
                        float ran = UniverseRandom.RandomBetween(0f, 1f);
                        if (projectile.loyalty != null && ran >= projectile.loyalty.data.MissileDodgeChance)
                        {
                            projectile.DamageMissile(this, damageAmount);
                            return true;
                        }
                    }
                    else if (weapon.Tag_Intercept || projectile.weapon.Tag_Intercept)
                    {
                        DieNextFrame = true;
                        projectile.DieNextFrame = true;
                    }
                    return false;
                }
                if (target is Asteroid)
                {
                    if (!explodes)
                    {
                        target.Damage(this, damageAmount);
                    }
                    Die(null, false);
                    return true;
                }
                if (target is ShipModule module)
                {
                    Ship parent = module.GetParent();
                    if (parent.loyalty == loyalty && !weapon.HitsFriendlies)
                        return false;

                    if (weapon.TruePD)
                    {
                        DieNextFrame = true;
                        return true;
                    }
                    if (parent.shipData.Role == ShipData.RoleName.fighter && parent.loyalty.data.Traits.DodgeMod > 0f)
                    {
                        if (UniverseRandom.RandomBetween(0f, 100f) < parent.loyalty.data.Traits.DodgeMod * 100f)
                            Miss = true;
                    }
                    if (Miss)
                        return false;

                    //Non exploding projectiles should go through multiple modules if it has enough damage
                    if (!explodes && module.Active)
                    {
                        //Doc: If module has resistance to Armour Piercing effects, deduct that from the projectile's AP before starting AP and damage checks
                        if (module.APResist > 0)
                            ArmorPiercing = ArmorPiercing - module.APResist;

                        if (ArmorPiercing <= 0 || !(module.ModuleType == ShipModuleType.Armor || (module.ModuleType == ShipModuleType.Dummy && module.ParentOfDummy.ModuleType == ShipModuleType.Armor)))
                            module.Damage(this, damageAmount, ref damageAmount);
                        else
                            ArmorPiercing -= (module.XSIZE + module.YSIZE) / 2;

                        if (damageAmount > 0f) // damage passes through to next modules
                        {
                            Vector2 projectileDir = Velocity.Normalized();
                            var projectedModules = parent.RayHitTestModules(Center, projectileDir, 64.0f, Radius, ignoreShields:true);

                            // now pierce through all of the modules while we can still pierce and damage:
                            foreach (ShipModule impactModule in projectedModules)
                            {
                                if (ArmorPiercing > 0 && impactModule.ModuleType == ShipModuleType.Armor)
                                {
                                    ArmorPiercing -= (impactModule.XSIZE + impactModule.YSIZE) / 2;
                                    continue; // SKIP/Phase through this armor module (yikes!)
                                }

                                impactModule.Damage(this, damageAmount, ref damageAmount);
                                if (damageAmount <= 0f)
                                    break;
                            }
                        }
                    }
                    Health = 0f;
                }
                if (WeaponEffectType == "Plasma")
                {
                    var center  = new Vector3(Center.X, Center.Y, -100f);
                    var forward = new Vector2((float)Math.Sin(Rotation), -(float)Math.Cos(Rotation));
                    var right   = new Vector2(-forward.Y, forward.X).Normalized();
                    for (int i = 0; i < 20; i++)
                    {
                        Vector3 random = UniverseRandom.Vector3D(250f) * new Vector3(right.X, right.Y, 1f);
                        Empire.Universe.flameParticles.AddParticleThreadA(center, random);

                        random = UniverseRandom.Vector3D(150f) + new Vector3(-forward.X, -forward.Y, 0f);
                        Empire.Universe.flameParticles.AddParticleThreadA(center, random);
                    }
                }
                if (WeaponEffectType == "MuzzleBlast") // currently unused
                {
                    var center  = new Vector3(Center.X, Center.Y, -100f);
                    var forward = new Vector2((float)Math.Sin(Rotation), -(float)Math.Cos(Rotation));
                    var right   = new Vector2(-forward.Y, forward.X).Normalized();
                    for (int i = 0; i < 20; i++)
                    {
                        Vector3 random = UniverseRandom.Vector3D(500f) * new Vector3(right.X, right.Y, 1f);
                        Empire.Universe.fireTrailParticles.AddParticleThreadA(center, random);

                        random = new Vector3(-forward.X, -forward.Y, 0f) 
                            + new Vector3(UniverseRandom.RandomBetween(-500f, 500f), 
                                        UniverseRandom.RandomBetween(-500f, 500f), 
                                        UniverseRandom.RandomBetween(-150f, 150f));
                        Empire.Universe.fireTrailParticles.AddParticleThreadA(center, random);
                    }
                }
                else if (WeaponType == "Ballistic Cannon")
                {
                    if (target is ShipModule shipModule && shipModule.ModuleType != ShipModuleType.Shield)
                        AudioManager.PlayCue("sd_impact_bullet_small_01", Empire.Universe.listener, emitter);
                }
            }
            DieNextFrame = true;
            return base.Touch(target);
        }

        public override void Update(float elapsedTime)
        {
            if (!Active)
                return;
            if (DieNextFrame)
            {
                Die(this, false);
                return;
            }

            TimeElapsed += elapsedTime;
            Position += Velocity * elapsedTime;
            Scale = weapon.Scale;
            if (weapon.Animated == 1)
            {
                frameTimer += elapsedTime;
                if (weapon.LoopAnimation == 0 && frameTimer > switchFrames)
                {
                    frameTimer = 0.0f;
                    ++AnimationFrame;
                    if (AnimationFrame >= weapon.Frames)
                        AnimationFrame = 0;
                }
                else if (weapon.LoopAnimation == 1)
                {
                    ++AnimationFrame;
                    if (AnimationFrame >= weapon.Frames)
                        AnimationFrame = 0;
                }
                TexturePath = weapon.AnimationPath + AnimationFrame.ToString(fmt);
            }
            if (!string.IsNullOrEmpty(this.InFlightCue) && this.inFlight == null)
            {
                this.inFlight = AudioManager.GetCue(this.InFlightCue);
                this.inFlight.Apply3D(Empire.Universe.listener, this.emitter);
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
            missileAI?.Think(elapsedTime);
            droneAI?.Think(elapsedTime);
            if (this.ProjSO != null && (this.WeaponType == "Rocket" || this.WeaponType == "Drone" || this.WeaponType == "Missile") && (this.System != null && this.System.isVisible && (!this.wasAddedToSceneGraph && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)))
            {
                this.wasAddedToSceneGraph = true;
                lock (GlobalStats.ObjectManagerLocker)
                    Empire.Universe.ScreenManager.inter.ObjectManager.Submit((ISceneObject)this.ProjSO);
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
            if (ProjSO !=null && (InDeepSpace || System != null && System.isVisible) && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
            {
                if (zStart < -25.0)
                    zStart += velocityMaximum * elapsedTime;
                else
                    zStart = -25f;
                ProjSO.World = Matrix.Identity * Matrix.CreateScale(Scale) * Matrix.CreateRotationZ(Rotation) * Matrix.CreateTranslation(Center.X, Center.Y, -zStart);
                WorldMatrix = ProjSO.World;
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

            if (this.System != null && this.System.isVisible && (this.light == null && this.weapon.Light != null) && (Empire.Universe.viewState < UniverseScreen.UnivScreenState.SystemView && !this.LightWasAddedToSceneGraph))
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
                if (this.owner.ProjectilesFired.Count < 30 && this.System != null && (this.System.isVisible && this.MuzzleFlash == null) && (this.moduleAttachedTo.InstalledWeapon.MuzzleFlash != null && Empire.Universe.viewState < UniverseScreen.UnivScreenState.SystemView && !this.muzzleFlashAdded))
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
                    Empire.Universe.ScreenManager.inter.LightManager.Remove(MuzzleFlash);
            }
            base.Update(elapsedTime);
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