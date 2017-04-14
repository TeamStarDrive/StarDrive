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
        public bool IgnoresShields;
        public string WeaponType;
        private MissileAI MissileAI;
        public float VelocityMax;
        public float Speed;
        public float Range;
        public float DamageAmount;
        public float DamageRadius;
        public float ExplosionRadiusMod;
        public float Duration;
        public bool Explodes;
        public ShipModule ModuleAttachedTo;
        public string WeaponEffectType;
        private Matrix WorldMatrix;
        private ParticleEmitter TrailEmitter;
        private ParticleEmitter FiretrailEmitter;
        private SceneObject ProjSO;
        public string InFlightCue = "";
        public AudioEmitter Emitter = new AudioEmitter();
        public bool Miss;
        public Empire Loyalty;
        private float InitialDuration;
        private Vector2 Direction;
        private float SwitchFrames;
        private float FrameTimer;
        public float RotationRadsPerSecond;
        private DroneAI DroneAI;
        public Weapon Weapon;
        public string TexturePath;
        public string ModelPath;
        private float ZStart = -25f;
        private float ParticleDelay;
        private PointLight Light;
        public bool FirstRun = true;
        private MuzzleFlash Flash;
        private PointLight MuzzleFlash;
        private float FlashTimer = 0.142f;
        public float Scale = 1f;
        private int AnimationFrame;
        private const string Fmt = "00000.##";
        public bool DieNextFrame { get; private set; }
        public bool DieSound;
        private Cue InFlight;
        public string DieCueName = "";
        private bool WasAddedToSceneGraph;
        private bool LightWasAddedToSceneGraph;
        private bool MuzzleFlashAdded;
        public Vector2 FixedError;
        public bool ErrorSet = false;
        public bool FlashExplode;
        public bool IsSecondary;

        public Ship Owner { get; protected set; }
        public Planet Planet { get; private set; }

        public Projectile()
        {
        }

        public Projectile(Ship owner, Vector2 direction, ShipModule moduleAttachedTo)
        {
            Init(owner, direction, moduleAttachedTo);
        }
        public void Init(Ship parent, Vector2 direction, ShipModule moduleAttachedTo)
        {
            Loyalty = parent.loyalty;
            Owner = parent;
            SetSystem(parent.System);
            ModuleAttachedTo = moduleAttachedTo;
            Center = Position = moduleAttachedTo.Center;
            Emitter.Position = new Vector3(Position, 0f);
        }

        public Projectile(Planet p, Vector2 direction)
        {
            SetSystem(p.system);
            Position = p.Position;
            Loyalty  = p.Owner;
            Velocity = direction;
            Rotation = p.Position.RadiansToTarget(p.Position + Velocity);
            Center   = p.Position;
            Emitter.Position = new Vector3(p.Position, 0f);
        }

        public Projectile(Ship owner, Vector2 direction)
        {
            this.Owner = owner;
            Loyalty = owner.loyalty;
            Rotation = (float)Math.Acos(Vector2.Dot(Vector2.UnitY, direction)) - 3.14159274f;
            if (direction.X > 0f) Rotation = -Rotation;
        }


        public void DamageMissile(GameplayObject source, float damageAmount)
        {
            Health -= damageAmount;
            if (Health <= 0f && Active)
                DieNextFrame = true;
        }

        public override void Die(GameplayObject source, bool cleanupOnly)
        {
            ++DebugInfoScreen.ProjDied;
            if (Active)
            {
                if (Light != null)
                {
                    lock (GlobalStats.ObjectManagerLocker)
                    {
                        Empire.Universe.ScreenManager.inter.LightManager.Remove(Light);
                    }
                }
                if (!InFlightCue.IsEmpty())
                {
                    InFlight?.Stop(AudioStopOptions.Immediate);
                }
                if (Explodes)
                {
                    if (Weapon.OrdinanceRequiredToFire > 0f && Owner != null)
                    {
                        DamageAmount += Owner.loyalty.data.OrdnanceEffectivenessBonus * DamageAmount;
                        DamageRadius += Owner.loyalty.data.OrdnanceEffectivenessBonus * DamageRadius;
                    }

                    if (WeaponType == "Photon")
                    {
                        if (!DieCueName.IsEmpty())
                        {
                            dieCue = AudioManager.GetCue(DieCueName);
                        }
                        if (dieCue != null)
                        {
                            dieCue.Apply3D(audioListener, Emitter);
                            dieCue.Play();
                        }
                        if (!cleanupOnly && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
                        {
                            ExplosionManager.AddProjectileExplosion(new Vector3(Position, -50f), DamageRadius * 4.5f, 2.5f, 0.2f, Weapon.ExpColor);
                            Empire.Universe.flash.AddParticleThreadB(new Vector3(Position, -50f), Vector3.Zero);
                        }
                    }
                    else if (!DieCueName.IsEmpty())
                    {
                        if (Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
                        {
                            dieCue = AudioManager.GetCue(DieCueName);
                            if (dieCue != null)
                            {
                                dieCue.Apply3D(audioListener, Emitter);
                                dieCue.Play();
                            }
                        }
                        if (!cleanupOnly && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
                        {
                            ExplosionManager.AddExplosion(new Vector3(Position, -50f), DamageRadius * ExplosionRadiusMod, 2.5f, 0.2f);
                            if (FlashExplode)
                            {
                                Empire.Universe.flash.AddParticleThreadB(new Vector3(Position, -50f), Vector3.Zero);
                            }
                        }
                    }
                    ActiveSpatialManager.ProjectileExplode(this, DamageAmount, DamageRadius);
                }
                else if (Weapon.FakeExplode && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
                {
                    ExplosionManager.AddExplosion(new Vector3(Position, -50f), DamageRadius * ExplosionRadiusMod, 2.5f, 0.2f);
                    if (FlashExplode)
                    {
                        Empire.Universe.flash.AddParticleThreadB(new Vector3(Position, -50f), Vector3.Zero);
                    }
                }
            }
            if (ProjSO != null && Active)
            {
                lock (GlobalStats.ObjectManagerLocker)
                {
                    Empire.Universe.ScreenManager.inter.ObjectManager.Remove(ProjSO);
                }
                ProjSO.Clear();
            }
            if (DroneAI != null)
            {
                foreach (Beam beam in DroneAI.Beams)
                {
                    beam.Die(this, true);
                }
                DroneAI.Beams.Clear();
            }
            if (MuzzleFlashAdded)
            {
                lock (GlobalStats.ObjectManagerLocker)
                {
                    Empire.Universe.ScreenManager.inter.LightManager.Remove(MuzzleFlash);
                }
            }
            SetSystem(null);
            base.Die(source, cleanupOnly);
            Owner = null;
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
        }

        public DroneAI GetDroneAI()
        {
            return DroneAI;
        }

        public SceneObject GetSO()
        {
            return ProjSO;
        }

        public Matrix GetWorld()
        {
            return WorldMatrix;
        }

        public void Initialize(float initialSpeed, Vector2 dir, Vector2 pos)
        {
            DebugInfoScreen.ProjCreated = DebugInfoScreen.ProjCreated + 1;
            Direction = dir;
            Velocity = initialSpeed * dir;
            if (ModuleAttachedTo == null)
            {
                Center = pos;
                Rotation = Center.RadiansToTarget(Center + Velocity);
            }
            else
            {
                Center = ModuleAttachedTo.Center;
                Rotation = ModuleAttachedTo.Center.RadiansToTarget(ModuleAttachedTo.Center + Velocity);
            }
            Radius          = 1f;
            VelocityMax     = initialSpeed + Owner.Velocity.Length();
            Velocity        = Velocity.Normalized() * VelocityMax;
            Duration        = Range / initialSpeed * 1.2f;
            InitialDuration = Duration;
            if (Weapon.Animated == 1)
            {
                SwitchFrames = InitialDuration / Weapon.Frames;
                if (Weapon.LoopAnimation == 1)
                {
                    AnimationFrame = UniverseRandom.InRange(Weapon.Frames);
                }
            }
            if (Owner.loyalty.data.ArmorPiercingBonus > 0 && (Weapon.WeaponType == "Missile" || Weapon.WeaponType == "Ballistic Cannon"))
            {
                ArmorPiercing += Owner.loyalty.data.ArmorPiercingBonus;
            }
            ParticleDelay += Weapon.particleDelay;
            if (Weapon.Tag_Guided)
            {
                MissileAI = new MissileAI(this);
            }
            if (WeaponType != "Missile" && WeaponType != "Drone" && WeaponType != "Rocket" || (System == null || !System.isVisible) && !InDeepSpace)
            {
                if (Owner != null && Owner.loyalty.data.Traits.Blind > 0)
                {
                    if (UniverseRandom.IntBetween(0, 10) <= 1)
                        Miss = true;
                }
            }
            else if (ProjSO != null)
            {
                WasAddedToSceneGraph = true;
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
            Direction = direction;
            if (ModuleAttachedTo != null)
            {
                Center = ModuleAttachedTo.Center;
            }
            Velocity        = (initialSpeed * direction) + (Owner?.Velocity ?? Vector2.Zero);
            Radius          = 1f;
            VelocityMax     = initialSpeed + (Owner?.Velocity.Length() ?? 0f);
            Duration = Range / initialSpeed;
            Duration += Duration * 0.25f;
            InitialDuration = Duration;
            if (Weapon.Animated == 1)
            {
                SwitchFrames = InitialDuration / Weapon.Frames;
                if (Weapon.LoopAnimation == 1)
                {
                    AnimationFrame = UniverseRandom.InRange(Weapon.Frames);
                }
            }
            ParticleDelay += Weapon.particleDelay;
            if (Weapon.IsRepairDrone)
            {
                DroneAI = new DroneAI(this);
            }
            SetSystem(System);
            base.Initialize();
        }

        public void InitializeMissile(float initialSpeed, Vector2 direction, GameplayObject target)
        {
            Direction = direction;
            if (ModuleAttachedTo != null)
                Center = ModuleAttachedTo.Center;
            Velocity        = (initialSpeed * direction) + (Owner?.Velocity ?? Vector2.Zero);
            Radius          = 1f;
            VelocityMax     = initialSpeed + (Owner?.Velocity.Length() ?? 0f);
            Duration = Range / initialSpeed * 2f;
            InitialDuration = Duration;
            if (ModuleAttachedTo != null)
            {
                Center = ModuleAttachedTo.Center;
                if (ModuleAttachedTo.Facing == 0f)
                {
                    Rotation = Owner?.Rotation ?? 0f;
                }
                else
                {
                    Rotation = ModuleAttachedTo.Center.RadiansToTarget(ModuleAttachedTo.Center + Velocity);
                }
            }
            if (Weapon.Animated == 1)
            {
                SwitchFrames = InitialDuration / Weapon.Frames;
                if (Weapon.LoopAnimation == 1)
                {
                    AnimationFrame = UniverseRandom.InRange(Weapon.Frames);
                }
            }
            ParticleDelay += Weapon.particleDelay;
            if (Weapon.Tag_Guided)
            {
                MissileAI = new MissileAI(this);
                MissileAI.SetTarget(target);
            }
            if (ProjSO != null && (WeaponType == "Missile" || WeaponType == "Drone" || WeaponType == "Rocket") && (System != null && System.isVisible || InDeepSpace))
            {
                WasAddedToSceneGraph = true;
                lock (GlobalStats.ObjectManagerLocker)
                {
                    Empire.Universe.ScreenManager.inter.ObjectManager.Submit(ProjSO);
                }
            }
            SetSystem(System);
            base.Initialize();
        }

        public void InitializeMissilePlanet(float initialSpeed, Vector2 dir, GameplayObject target, Planet p)
        {
            Direction       = dir;
            Center          = p.Position;
            ZStart          = -2500f;
            Velocity        = (initialSpeed * dir) + (Owner?.Velocity ?? Vector2.Zero);
            Radius          = 1f;
            VelocityMax     = initialSpeed + (Owner?.Velocity.Length() ?? 0f);
            Duration = Range / initialSpeed * 2f;
            InitialDuration = Duration;
            Planet          = p;
            if (Weapon.Animated == 1)
            {
                SwitchFrames = InitialDuration / Weapon.Frames;
                if (Weapon.LoopAnimation == 1)
                {
                    AnimationFrame = UniverseRandom.InRange(Weapon.Frames);
                }
            }
            ParticleDelay += Weapon.particleDelay;
            if (Weapon.Tag_Guided)
            {
                MissileAI = new MissileAI(this);
                MissileAI.SetTarget(target);
            }
            if (ProjSO != null && (WeaponType == "Missile" || WeaponType == "Drone" || WeaponType == "Rocket") && (System != null && System.isVisible || InDeepSpace))
            {
                WasAddedToSceneGraph = true;
                lock (GlobalStats.ObjectManagerLocker)
                {
                    Empire.Universe.ScreenManager.inter.ObjectManager.Submit(ProjSO);
                }
            }
            SetSystem(System);
            base.Initialize();
        }

        public void InitializePlanet(float initialSpeed, Vector2 direction, Vector2 pos)
        {
            ZStart          = -2500f;
            Direction       = direction;
            Center          = ModuleAttachedTo?.Center ?? pos;
            Velocity        = (initialSpeed * direction) + (Owner?.Velocity ?? Vector2.Zero);
            Radius          = 1f;
            VelocityMax     = initialSpeed + (Owner?.Velocity.Length() ?? 0f);
            Velocity        = Velocity.Normalized() * VelocityMax;
            Duration = Range / initialSpeed * 1.25f;
            InitialDuration = Duration;
            if (Weapon.Animated == 1)
            {
                SwitchFrames = InitialDuration / Weapon.Frames;
                if (Weapon.LoopAnimation == 1)
                {
                    AnimationFrame = UniverseRandom.InRange(Weapon.Frames);
                }
            }

            ParticleDelay += Weapon.particleDelay;
            if (Weapon.Tag_Guided)
            {
                MissileAI = new MissileAI(this);
            }
            if (ProjSO != null && (WeaponType == "Missile" || WeaponType == "Drone" || WeaponType == "Rocket") && (System != null && System.isVisible || InDeepSpace))
            {
                WasAddedToSceneGraph = true;
                lock (GlobalStats.ObjectManagerLocker)
                {
                    Empire.Universe.ScreenManager.inter.ObjectManager.Submit(ProjSO);
                }
            }
            SetSystem(System);
            base.Initialize();
        }

        public void LoadContent(string texPath, string modelFilePath)
        {
            TexturePath = texPath;
            ModelPath = modelFilePath;

            ProjSO = new SceneObject(ResourceManager.ProjectileMeshDict[modelFilePath])
            {
                Visibility = ObjectVisibility.Rendered,
                ObjectType = ObjectType.Dynamic
            };
            if (Empire.Universe != null && ProjSO !=null)
            {
                switch (Weapon.WeaponEffectType)
                {
                    case "RocketTrail":
                        TrailEmitter     = Empire.Universe.projectileTrailParticles.NewEmitter(500f, Center, -ZStart);
                        FiretrailEmitter = Empire.Universe.fireTrailParticles.NewEmitter(500f, Center, -ZStart);
                        break;
                    case "Plasma":
                        FiretrailEmitter = Empire.Universe.flameParticles.NewEmitter(500f, Center);
                        break;
                    case "SmokeTrail":
                        TrailEmitter     = Empire.Universe.projectileTrailParticles.NewEmitter(500f, Center, -ZStart);
                        break;
                    case "MuzzleSmoke":
                        FiretrailEmitter = Empire.Universe.projectileTrailParticles.NewEmitter(1000f, Center);
                        break;
                    case "MuzzleSmokeFire":
                        FiretrailEmitter = Empire.Universe.projectileTrailParticles.NewEmitter(1000f, Center);
                        TrailEmitter     = Empire.Universe.fireTrailParticles.NewEmitter(750f, Center, -ZStart);
                        break;
                    case "FullSmokeMuzzleFire":
                        TrailEmitter     = Empire.Universe.projectileTrailParticles.NewEmitter(500f, Center, -ZStart);
                        FiretrailEmitter = Empire.Universe.fireTrailParticles.NewEmitter(500f, Center, -ZStart);
                        break;
                }

            }
            if (Weapon.Animated == 1 && ProjSO !=null)
            {
                TexturePath = Weapon.AnimationPath + AnimationFrame.ToString(Fmt);
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
                if (target == Owner && !Weapon.HitsFriendlies)
                {
                    return false;
                }
                var projectile = target as Projectile;
                if (projectile != null)
                {
                    if (Owner != null && projectile.Loyalty == Owner.loyalty)
                    {
                        return false;
                    }
                    if (projectile.WeaponType == "Missile")
                    {
                        float ran = UniverseRandom.RandomBetween(0f, 1f);
                        if (projectile.Loyalty != null && ran >= projectile.Loyalty.data.MissileDodgeChance)
                        {
                            projectile.DamageMissile(this, DamageAmount);
                            return true;
                        }
                    }
                    else if (Weapon.Tag_Intercept || projectile.Weapon.Tag_Intercept)
                    {
                        DieNextFrame = true;
                        projectile.DieNextFrame = true;
                    }
                    return false;
                }
                if (target is Asteroid)
                {
                    if (!Explodes)
                    {
                        target.Damage(this, DamageAmount);
                    }
                    Die(null, false);
                    return true;
                }
                if (target is ShipModule module)
                {
                    Ship parent = module.GetParent();
                    if (parent.loyalty == Loyalty && !Weapon.HitsFriendlies)
                        return false;

                    if (Weapon.TruePD)
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
                    if (!Explodes && module.Active)
                    {
                        //Doc: If module has resistance to Armour Piercing effects, deduct that from the projectile's AP before starting AP and damage checks
                        if (module.APResist > 0)
                            ArmorPiercing = ArmorPiercing - module.APResist;

                        if (ArmorPiercing <= 0 || module.ModuleType != ShipModuleType.Armor)
                            module.Damage(this, DamageAmount, out DamageAmount);
                        else
                            ArmorPiercing -= (module.XSIZE + module.YSIZE) / 2;

                        if (DamageAmount > 0f) // damage passes through to next modules
                        {
                            Vector2 projectileDir = Velocity.Normalized();
                            var projectedModules = parent.RayHitTestModules(Center, projectileDir, 100f, Radius);

                            // now pierce through all of the modules while we can still pierce and damage:
                            foreach (ShipModule impactModule in projectedModules)
                            {
                                if (ArmorPiercing > 0 && impactModule.ModuleType == ShipModuleType.Armor)
                                {
                                    ArmorPiercing -= (impactModule.XSIZE + impactModule.YSIZE) / 2;
                                    continue; // SKIP/Phase through this armor module (yikes!)
                                }

                                impactModule.Damage(this, DamageAmount, out DamageAmount);
                                if (DamageAmount <= 0f)
                                    break;
                            }
                        }
                    }
                    Health = 0f;
                }
                if (WeaponEffectType == "Plasma")
                {
                    var center  = new Vector3(Center.X, Center.Y, -100f);
                    Vector2 forward = Rotation.RadiansToDirection();
                    Vector2 right   = forward.RightVector();
                    for (int i = 0; i < 20; i++)
                    {
                        Vector3 random = UniverseRandom.Vector3D(250f) * new Vector3(right.X, right.Y, 1f);
                        Empire.Universe.flameParticles.AddParticleThreadA(center, random);

                        random = UniverseRandom.Vector3D(150f) + new Vector3(-forward.X, -forward.Y, 0f);
                        Empire.Universe.flameParticles.AddParticleThreadA(center, random);
                    }
                }
                else if (WeaponEffectType == "MuzzleBlast") // currently unused
                {
                    var center  = new Vector3(Center.X, Center.Y, -100f);
                    Vector2 forward = Rotation.RadiansToDirection();
                    Vector2 right   = forward.RightVector();
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
                        AudioManager.PlayCue("sd_impact_bullet_small_01", Empire.Universe.listener, Emitter);
                }
            }
            DieNextFrame = true;
            return true;
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

            Position += Velocity * elapsedTime;
            Scale = Weapon.Scale;
            if (Weapon.Animated == 1)
            {
                FrameTimer += elapsedTime;
                if (Weapon.LoopAnimation == 0 && FrameTimer > SwitchFrames)
                {
                    FrameTimer = 0.0f;
                    ++AnimationFrame;
                    if (AnimationFrame >= Weapon.Frames)
                        AnimationFrame = 0;
                }
                else if (Weapon.LoopAnimation == 1)
                {
                    ++AnimationFrame;
                    if (AnimationFrame >= Weapon.Frames)
                        AnimationFrame = 0;
                }
                TexturePath = Weapon.AnimationPath + AnimationFrame.ToString(Fmt);
            }
            if (!string.IsNullOrEmpty(InFlightCue) && InFlight == null)
            {
                InFlight = AudioManager.GetCue(InFlightCue);
                InFlight.Apply3D(Empire.Universe.listener, Emitter);
                InFlight.Play();
            }
            ParticleDelay -= elapsedTime;
            if (Duration > 0f)
            {
                Duration -= elapsedTime;
                if (Duration < 0f)
                {
                    Health = 0f;
                    Die(null, false);
                    return;
                }
            }
            MissileAI?.Think(elapsedTime);
            DroneAI?.Think(elapsedTime);
            if (ProjSO != null && (WeaponType == "Rocket" || WeaponType == "Drone" || WeaponType == "Missile") && 
                (System != null && System.isVisible && (!WasAddedToSceneGraph && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)))
            {
                WasAddedToSceneGraph = true;
                lock (GlobalStats.ObjectManagerLocker)
                    Empire.Universe.ScreenManager.inter.ObjectManager.Submit(ProjSO);
            }
            if (FirstRun && ModuleAttachedTo != null)
            {
                Position = ModuleAttachedTo.Center;
                Center = ModuleAttachedTo.Center;
                FirstRun = false;
            }
            else
                Center = Position;
            Emitter.Position = new Vector3(Center, 0.0f);
            if (ProjSO != null && (InDeepSpace || System != null && System.isVisible) && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
            {
                if (ZStart < -25.0)
                    ZStart += VelocityMax * elapsedTime;
                else
                    ZStart = -25f;
                ProjSO.World = Matrix.CreateScale(Scale) * Matrix.CreateRotationZ(Rotation) * Matrix.CreateTranslation(Center.X, Center.Y, -ZStart);
                WorldMatrix = ProjSO.World;
            }
            var newPosition = new Vector3(Center.X, Center.Y, -ZStart);

            if (FiretrailEmitter != null)
            {
                float durationLimit = InitialDuration * (WeaponEffectType == "Plasma" ? 0.7f : 0.97f);
                if (ParticleDelay <= 0.0f && Duration > durationLimit)
                {
                    FiretrailEmitter.UpdateProjectileTrail(elapsedTime, newPosition, Velocity + Direction.Normalized() * Speed * 1.75f);
                }
                //FiretrailEmitter.Update(elapsedTime, newPosition);

            }
            if (TrailEmitter != null)
            {
                if (ParticleDelay <= 0.0f && Duration > InitialDuration * 0.96f)
                    TrailEmitter.Update(elapsedTime, newPosition);
            }

            if (System != null && System.isVisible && Light == null && Weapon.Light != null && 
                (Empire.Universe.viewState < UniverseScreen.UnivScreenState.SystemView && !LightWasAddedToSceneGraph))
            {
                LightWasAddedToSceneGraph = true;
                var pos = new Vector3(Center.X, Center.Y, -25f);
                Light = new PointLight
                {
                    Position   = pos,
                    Radius     = 100f,
                    World      = Matrix.CreateTranslation(pos),
                    ObjectType = ObjectType.Dynamic,
                    Intensity  = 1.7f,
                    FillLight  = true,
                    Enabled    = true
                };
                switch (Weapon.Light)
                {
                    case "Green":  Light.DiffuseColor = new Vector3(0.0f, 0.8f, 0.0f);  break;
                    case "Red":    Light.DiffuseColor = new Vector3(1f, 0.0f, 0.0f);    break;
                    case "Orange": Light.DiffuseColor = new Vector3(0.9f, 0.7f, 0.0f);  break;
                    case "Purple": Light.DiffuseColor = new Vector3(0.8f, 0.8f, 0.95f); break;
                    case "Blue":   Light.DiffuseColor = new Vector3(0.0f, 0.8f, 1f);    break;
                }
                Light.AddTo(Empire.Universe);
            }
            else if (Weapon.Light != null && LightWasAddedToSceneGraph)
            {
                Light.Position = new Vector3(Center.X, Center.Y, -25f);
                Light.World = Matrix.CreateTranslation(Light.Position);
            }
            if (ModuleAttachedTo != null)
            {
                if (Owner.ProjectilesFired.Count < 30 && System != null && System.isVisible && MuzzleFlash == null && 
                    ModuleAttachedTo.InstalledWeapon.MuzzleFlash != null && Empire.Universe.viewState < UniverseScreen.UnivScreenState.SystemView && !MuzzleFlashAdded)
                {
                    MuzzleFlashAdded = true;
                    var pos = new Vector3(ModuleAttachedTo.Center.X, ModuleAttachedTo.Center.Y, -45f);
                    MuzzleFlash = new PointLight
                    {
                        Position     = pos,
                        World        = Matrix.CreateTranslation(pos),
                        Radius       = 65f,
                        ObjectType   = ObjectType.Dynamic,
                        DiffuseColor = new Vector3(1f, 0.97f, 0.9f),
                        Intensity    = 1f,
                        FillLight    = false,
                        Enabled      = true
                    };
                    MuzzleFlash.AddTo(Empire.Universe);
                    FlashTimer -= elapsedTime;
                    Flash = new MuzzleFlash
                    {
                        WorldMatrix = Matrix.CreateRotationZ(Rotation) * Matrix.CreateTranslation(pos),
                        Owner = this
                    };
                    lock (GlobalStats.ExplosionLocker)
                        MuzzleFlashManager.FlashList.Add(Flash);
                }
                else if (FlashTimer > 0f && ModuleAttachedTo.InstalledWeapon.MuzzleFlash != null && MuzzleFlashAdded)
                {
                    FlashTimer -= elapsedTime;
                    MuzzleFlash.Position = new Vector3(ModuleAttachedTo.Center.X, ModuleAttachedTo.Center.Y, -45f);
                    Flash.WorldMatrix = Matrix.CreateRotationZ(Rotation) * Matrix.CreateTranslation(MuzzleFlash.Position);
                    MuzzleFlash.World = Matrix.CreateTranslation(MuzzleFlash.Position);
                }
            }
            if (FlashTimer <= 0f && MuzzleFlashAdded)
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
            DroneAI?.Dispose(ref DroneAI);
        }
    }
}