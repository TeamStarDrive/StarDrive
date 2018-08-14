using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Particle3DSample;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;
using System;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Ships;

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
        public ShipModule Module;
        public string WeaponEffectType;
        private ParticleEmitter TrailEmitter;
        private ParticleEmitter FiretrailEmitter;
        private SceneObject ProjSO; // this is null for sprite based projectiles
        public Matrix WorldMatrix { get; private set; }
        public string InFlightCue = "";
        public AudioEmitter Emitter = new AudioEmitter();
        public bool Miss;
        public Empire Loyalty;
        private float InitialDuration;
        private float SwitchFrames;
        private float FrameTimer;
        public float RotationRadsPerSecond;
        public DroneAI DroneAI { get; private set; }
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
        //public float Scale = 1f; //Set to Weapon.Scale every frame. Removing...  -Gretman
        private int AnimationFrame;
        public bool DieNextFrame { get; private set; }
        public bool DieSound;
        private AudioHandle InFlightSfx = default(AudioHandle);
        public string DieCueName = "";
        private bool LightWasAddedToSceneGraph;
        private bool UsesVisibleMesh;
        private bool MuzzleFlashAdded;
        public Vector2 FixedError;
        public bool ErrorSet = false;
        public bool FlashExplode;
        private bool InFrustrum = false;
   

        public Ship Owner { get; protected set; }
        public Planet Planet { get; private set; }

        public override IDamageModifier DamageMod => Weapon;

        public Projectile() : base(GameObjectType.Proj)
        {
        }

        public Projectile(GameObjectType typeFlags) : base(typeFlags | GameObjectType.Proj)
        {
        }

        public static Projectile Create(Weapon weapon, Vector2 origin, Vector2 direction, GameplayObject target, bool playSound)
        {
            var projectile = new Projectile
            {
                Weapon  = weapon,
                Owner   = weapon.Owner,
                Loyalty = weapon.Owner.loyalty,
                Module  = weapon.Module
            };
            projectile.Initialize(origin, direction, target, playSound);
            return projectile;
        }

        public static Projectile Create(Weapon weapon, Planet planet, Vector2 direction, GameplayObject target)
        {
            var projectile = new Projectile
            {
                Weapon  = weapon,
                Planet  = planet,
                Loyalty = planet.Owner,
                ZStart  = -2500f
            };
            projectile.Initialize(planet.Center, direction, target, playSound: true);
            return projectile;
        }

        // loading from savegame
        public static Projectile Create(Ship owner, SavedGame.ProjectileSaveData pdata)
        {
            Weapon weapon = ResourceManager.CreateWeapon(pdata.Weapon);
            var projectile = new Projectile
            {
                Weapon  = weapon,
                Owner   = owner,
                Loyalty = owner.loyalty,
                Module  = weapon.Module,
            };
            projectile.Initialize(pdata.Position, pdata.Velocity, null, playSound: false);
            projectile.Duration = pdata.Duration; // apply duration from save data
            return projectile;
        }

        private void Initialize(Vector2 origin, Vector2 direction, GameplayObject target, bool playSound)
        {
            ++DebugInfoScreen.ProjCreated;
            Position = origin;
            Center   = origin;
            Emitter.Position = new Vector3(origin, 0f);

            Range                 = Weapon.Range;
            Radius                = Weapon.ProjectileRadius;
            Explodes              = Weapon.explodes;
            DamageAmount          = Weapon.GetDamageWithBonuses(Owner);
            DamageRadius          = Weapon.DamageRadius;
            ExplosionRadiusMod    = Weapon.ExplosionRadiusVisual;
            Health                = Weapon.HitPoints;
            Speed                 = Weapon.ProjectileSpeed;
            WeaponEffectType      = Weapon.WeaponEffectType;
            WeaponType            = Weapon.WeaponType;
            RotationRadsPerSecond = Weapon.RotationRadsPerSecond;
            ArmorPiercing         = (int)Weapon.ArmourPen;

            Weapon.ModifyProjectile(this); // apply all buffs before initializing
            if (Weapon.RangeVariance)
                Range *= RandomMath.RandomBetween(0.9f, 1.1f);

            float durationMod = 1.2f;
            if (Weapon.IsRepairDrone)    durationMod = 1.25f;
            else if (Weapon.Tag_Missile) durationMod = 2.0f;
            else if (Planet != null)     durationMod = 2.0f;

            Velocity = Speed*direction + (Owner?.Velocity ?? Vector2.Zero);
            Rotation = Velocity.Normalized().ToRadians(); // used for drawing the projectile in correct direction
            VelocityMax = Speed + (Owner?.Velocity.Length() ?? 0f);

            InitialDuration = Duration = (Range/Speed) * durationMod;
            ParticleDelay  += Weapon.particleDelay;

            if (Owner?.loyalty.data.ArmorPiercingBonus > 0
                && (Weapon.Tag_Kinetic  || Weapon.Tag_Missile || Weapon.Tag_Torpedo))
            {
                ArmorPiercing += Owner.loyalty.data.ArmorPiercingBonus;
            }
        
            if (Weapon.IsRepairDrone)   DroneAI   = new DroneAI(this);
            else if (Weapon.Tag_Guided) MissileAI = new MissileAI(this, target);
            
            LoadContent();
            Initialize();

            if (Owner != null)
            {
                Loyalty = Owner.loyalty;
                SetSystem(Owner.System);
                Owner.AddProjectile(this);
            }
            else if (Planet != null)
            {
                Loyalty = Planet.Owner;
                SetSystem(Planet.ParentSystem);
                Planet.AddProjectile(this);
            }

            if (playSound && (System != null && System.isVisible || Owner?.InFrustum == true))
            {
                Weapon.PlayToggleAndFireSfx(Emitter);
                DieSound = true;

                string cueName = ResourceManager.GetWeaponTemplate(Weapon.UID).dieCue;
                if (cueName.NotEmpty())     DieCueName  = cueName;
                if (InFlightCue.NotEmpty()) InFlightCue = Weapon.InFlightCue;
            }
        }

        private void LoadContent()
        {
            if (Weapon.Animated == 1)
            {
                SwitchFrames = InitialDuration / Weapon.Frames;
                if (Weapon.LoopAnimation == 1)
                    AnimationFrame = UniverseRandom.InRange(Weapon.Frames);
                TexturePath = Weapon.AnimationPath + AnimationFrame.ToString("00000.##");
            }
            else
            {
                TexturePath = Weapon.ProjectileTexturePath;
            }

            ModelPath = Weapon.ModelPath;
            UsesVisibleMesh = Weapon.UseVisibleMesh || WeaponType == "Missile" || WeaponType == "Drone" || WeaponType == "Rocket";

            if (Empire.Universe == null)
                return;

            switch (Weapon.WeaponEffectType)
            {
                case "RocketTrail":
                    TrailEmitter     = Empire.Universe.projectileTrailParticles.NewEmitter(100f, Center, -ZStart);
                    FiretrailEmitter = Empire.Universe.fireTrailParticles.NewEmitter(100f, Center, -ZStart);
                    break;
                case "Plasma":
                    FiretrailEmitter = Empire.Universe.flameParticles.NewEmitter(100f, Center);
                    break;
                case "SmokeTrail":
                    TrailEmitter     = Empire.Universe.projectileTrailParticles.NewEmitter(100f, Center, -ZStart);
                    break;
                case "MuzzleSmoke":
                    FiretrailEmitter = Empire.Universe.projectileTrailParticles.NewEmitter(100f, Center);
                    break;
                case "MuzzleSmokeFire":
                    FiretrailEmitter = Empire.Universe.projectileTrailParticles.NewEmitter(100f, Center);
                    TrailEmitter     = Empire.Universe.fireTrailParticles.NewEmitter(100f, Center, -ZStart);
                    break;
                case "FullSmokeMuzzleFire":
                    TrailEmitter     = Empire.Universe.projectileTrailParticles.NewEmitter(100f, Center, -ZStart);
                    FiretrailEmitter = Empire.Universe.fireTrailParticles.NewEmitter(100f, Center, -ZStart);
                    break;
            }
        }
        public override Vector2 JitterPosition()
        {
            Vector2 jitter = Vector2.Zero;
            if (!Weapon.Tag_Intercept) return jitter;

            if (MissileAI != null &&  Loyalty?.data.MissileDodgeChance >0 )
            {
                jitter += RandomMath2.Vector2D(Loyalty.data.MissileDodgeChance * 80f);
                
            }
            if ((Weapon?.Module?.WeaponECM ?? 0) > 0)
                jitter += RandomMath2.Vector2D((Weapon?.Module.WeaponECM ?? 0) * 80f);

            return jitter;
            
        }

        public override bool IsAttackable(Empire attacker, Relationship attackerRelationThis)
        {
            if (MissileAI?.GetTarget.GetLoyalty() == attacker) return true;            
            if (!attackerRelationThis.Treaty_OpenBorders && !attackerRelationThis.Treaty_Trade
                && attacker.GetGSAI().ThreatMatrix.ShipInOurBorders(Owner)) return true;
           
            return false;
        }

        public void DrawProjectile(UniverseScreen screen)
        {
            // if not using visible mesh (rockets, etc), we draw a transparent mesh manually
            InFrustrum = Empire.Universe.viewState < UniverseScreen.UnivScreenState.SystemView 
                         && ((Owner?.InFrustum ?? true) || screen.Frustum.Contains(Center, Radius)) ;
            if (UsesVisibleMesh || !InFrustrum) return;

            var projMesh = ResourceManager.ProjectileModelDict[ModelPath];
            var tex = Weapon.Animated != 0 ? ResourceManager.Texture(TexturePath) : ResourceManager.ProjTexture(TexturePath);
            screen.DrawTransparentModel(projMesh, WorldMatrix, tex, Weapon.Scale);
        }

        public void DamageMissile(GameplayObject source, float damageAmount)
        {
            if (Health < 1)
                Log.Info($"Projectile had no health {Weapon.Name}");
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
                    Empire.Universe.RemoveLight(Light);

                if (InFlightSfx.IsPlaying)
                    InFlightSfx.Stop();

                ExplodeProjectile(cleanupOnly);
                if (ProjSO != null)
                {
                    Empire.Universe.RemoveObject(ProjSO);
                    ProjSO.Clear();
                }
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
                Empire.Universe.RemoveLight(MuzzleFlash);
            }
            SetSystem(null);
            base.Die(source, cleanupOnly);
            Owner = null;
        }

        private void ExplodeProjectile(bool cleanupOnly)
        {
            if (Explodes)
            {
                if (Weapon.OrdinanceRequiredToFire > 0f && Owner != null)
                {
                    DamageRadius += Owner.loyalty.data.OrdnanceEffectivenessBonus * DamageRadius;
                }

                if (!cleanupOnly && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
                {
                    GameAudio.PlaySfxAsync(DieCueName, Emitter);

                    if (Weapon.ExpColor != null)
                        ExplosionManager.AddProjectileExplosion(new Vector3(Position, -50f), DamageRadius * 4.5f, 2.5f,
                            0.2f,
                            Weapon.ExpColor);
                    else
                        ExplosionManager.AddExplosion(new Vector3(Position, -50f), DamageRadius * ExplosionRadiusMod,
                            2.5f,
                            0.2f, Weapon.ExplosionPath, Weapon.ExplosionAnimation);

                    if (FlashExplode)
                        Empire.Universe.flash.AddParticleThreadB(new Vector3(Position, -50f), Vector3.Zero);
                }

                UniverseScreen.SpaceManager.ProjectileExplode(this, DamageAmount, DamageRadius);
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

        public void GuidedMoveTowards(float elapsedTime, Vector2 targetPos)
        {
            
            Vector2 currentForward = Rotation.RadiansToDirection();
            Vector2 desiredForward = Center.DirectionToTarget(targetPos);

            float angleDiff = (float)Math.Acos(desiredForward.Dot(currentForward));
            if (angleDiff > 0.1f)
            {
                Vector2 currentRight = currentForward.RightVector();
                float   rotationDir  = desiredForward.Dot(currentRight) > 0f ? 1f : -1f;

                float rotationRadsPerSec = RotationRadsPerSecond;
                if (rotationRadsPerSec <= 0f)
                    rotationRadsPerSec = Speed / 350f;

                Log.Assert(rotationRadsPerSec > 0f, "Invalid RotationRadsPerSecond");

                Rotation += Math.Min(angleDiff, rotationDir * elapsedTime * rotationRadsPerSec);
            }

            Velocity = Rotation.RadiansToDirection() * (Speed); //elapsedTime * 
            if (Velocity.Length() > VelocityMax)
                Velocity = Velocity.Normalized() * VelocityMax;            
        }
        
        public override bool Touch(GameplayObject target)
        {
            if (Miss || target == Owner)
                return false;
            //
            switch (target) {
                case Projectile projectile:
                    if (!Weapon.Tag_PD && !Weapon.TruePD) return false;
                    if (!projectile.Weapon.Tag_Intercept) return false;
                    if (projectile.Weapon.Tag_PD || projectile.Weapon.TruePD) return false;

                    if (projectile.Loyalty == null || Owner?.loyalty?.IsEmpireAttackable(projectile.Loyalty) == false)
                        return false;                
                    projectile.DamageMissile(this, DamageAmount);
                    DieNextFrame = true;
                    return true;
                case Asteroid _:
                    if (!Explodes)
                    {
                        target.Damage(this, DamageAmount);
                    }
                    Die(null, false);
                    return true;
                case ShipModule module:
                    Ship parent = module.GetParent();
                    if (!Loyalty.IsEmpireAttackable(parent.loyalty))
                        return false;

                    if (Weapon.TruePD)
                    {
                        DieNextFrame = true;
                        return true;
                    }
                    // Non exploding projectiles should go through multiple modules if it has enough damage
                    if (!Explodes && module.Active)
                        ArmourPiercingTouch(module, parent);

                    Health = 0f;
                    break;
            }
            if (WeaponEffectType == "Plasma")
            {
                var center  = new Vector3(Center.X, Center.Y, -100f);
                Vector3 forward  = Rotation.RadiansToDirection3D();
                Vector3 right    = forward.RightVector(z:1f);
                Vector3 backward = -forward;
                for (int i = 0; i < 20; i++)
                {
                    Vector3 random = UniverseRandom.Vector3D(250f) * right;
                    Empire.Universe.flameParticles.AddParticleThreadA(center, random);

                    random = UniverseRandom.Vector3D(150f) + backward;
                    Empire.Universe.flameParticles.AddParticleThreadA(center, random);
                }
            }
            else if (WeaponEffectType == "MuzzleBlast") // currently unused
            {
                var center  = new Vector3(Center.X, Center.Y, -100f);
                Vector3 forward  = Rotation.RadiansToDirection3D();
                Vector3 right    = forward.RightVector(z:1f);
                Vector3 backward = -forward;
                for (int i = 0; i < 20; i++)
                {
                    Vector3 random = UniverseRandom.Vector3D(500f) * right;
                    Empire.Universe.fireTrailParticles.AddParticleThreadA(center, random);

                    random = backward + new Vector3(UniverseRandom.RandomBetween(-500f, 500f), 
                                                    UniverseRandom.RandomBetween(-500f, 500f), 
                                                    UniverseRandom.RandomBetween(-150f, 150f));
                    Empire.Universe.fireTrailParticles.AddParticleThreadA(center, random);
                }
            }
            else if (WeaponType == "Ballistic Cannon")
            {
                if (target is ShipModule shipModule && !shipModule.Is(ShipModuleType.Shield))
                    GameAudio.PlaySfxAsync("sd_impact_bullet_small_01", Emitter);
            }
            
            DieNextFrame = true;
            return true;
        }

        private void DebugTargetCircle()
        {
            Empire.Universe?.DebugWin?.DrawGameObject(DebugModes.Targeting, this);
        }

        private void ArmourPiercingTouch(ShipModule module, Ship parent)
        {
            // Doc: If module has resistance to Armour Piercing effects, 
            // deduct that from the projectile's AP before starting AP and damage checks
            ArmorPiercing -= module.APResist;

            if (ArmorPiercing <= 0 || !module.Is(ShipModuleType.Armor))
                module.Damage(this, DamageAmount, out DamageAmount);

            if (DamageAmount <= 0f)
                return;
            var projectedModules = new Array<ShipModule>();
            projectedModules.Add(module);
            Vector2 projectileDir = Velocity.Normalized();
            projectedModules = parent.RayHitTestModules(module.Center, projectileDir, distance:parent.Radius, rayRadius:Radius);
            if (projectedModules == null)
                return;
            DebugTargetCircle();
            for (int x = 0; x < projectedModules.Count; x++)
            {
                ShipModule impactModule = projectedModules[x];
                if (!impactModule.Active)
                    continue;
                if (ArmorPiercing > 0 && impactModule.Is(ShipModuleType.Armor))
                {
                    ArmorPiercing -= impactModule.XSIZE; // armor is always squared anyway.
                    impactModule.DebugDamageCircle();
                    if (ArmorPiercing >= 0)
                    {
                        continue; // SKIP/Phase through this armor module (yikes!)
                    }
                }
                impactModule.DebugDamageCircle();
                impactModule.Damage(this, DamageAmount, out DamageAmount);
                if (DamageAmount <= 0f)
                    return;
            }
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
            if (Weapon.Animated == 1 && InFrustrum)
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
                TexturePath = Weapon.AnimationPath + AnimationFrame.ToString("00000.##");
            }

            if (InFlightSfx.IsStopped)
                InFlightSfx.PlaySfxAsync(InFlightCue, Emitter);

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
            if (FirstRun && Module != null)
            {
                Position = Module.Center;
                Center = Module.Center;
                FirstRun = false;
            }
            else
                Center = Position;
            Emitter.Position = new Vector3(Center, 0.0f);
            if (InFrustrum)
            {
                if (ZStart < -25.0)
                    ZStart += VelocityMax * elapsedTime;
                else
                    ZStart = -25f;

                WorldMatrix = Matrix.CreateScale(Weapon.Scale) * Matrix.CreateRotationZ(Rotation) * Matrix.CreateTranslation(Center.X, Center.Y, -ZStart);

                if (UsesVisibleMesh) // lazy init rocket projectile meshes
                {
                    if (ProjSO == null)
                    {
                        ProjSO = new SceneObject(ResourceManager.ProjectileMeshDict[ModelPath])
                        {
                            Visibility = ObjectVisibility.Rendered,
                            ObjectType = ObjectType.Dynamic
                        };
                        Empire.Universe.AddObject(ProjSO);
                    }
                    ProjSO.World = WorldMatrix;
                }
            }
            var newPosition = new Vector3(Center.X, Center.Y, -ZStart);

            if (FiretrailEmitter != null && InFrustrum)
            {
                if (ParticleDelay <= 0.0f && Duration > 0.5)
                {
                    FiretrailEmitter.UpdateProjectileTrail(elapsedTime, newPosition, Velocity + Velocity.Normalized() * Speed * 1.75f);
                }
            }
            if (TrailEmitter != null && InFrustrum)
            {
                if (ParticleDelay <= 0.0f && Duration > 0.5)
                {
                    TrailEmitter.Update(elapsedTime, newPosition);
                }
            }

            if (InFrustrum && Light == null && Weapon.Light != null && !LightWasAddedToSceneGraph)
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
                Empire.Universe.AddLight(Light);
            }
            else if (Weapon.Light != null && LightWasAddedToSceneGraph)
            {
                Light.Position = new Vector3(Center.X, Center.Y, -25f);
                Light.World = Matrix.CreateTranslation(Light.Position);
            }
            if (Module != null)
            {
                if (MuzzleFlash == null && Module.InstalledWeapon.MuzzleFlash != null && InFrustrum && !MuzzleFlashAdded)
                {
                    MuzzleFlashAdded = true;
                    var pos = new Vector3(Module.Center.X, Module.Center.Y, -45f);
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
                    Empire.Universe.AddLight(MuzzleFlash);
                    FlashTimer -= elapsedTime;
                    Flash = new MuzzleFlash
                    {
                        WorldMatrix = Matrix.CreateRotationZ(Rotation) * Matrix.CreateTranslation(pos),
                        Owner = this
                    };
                    lock (GlobalStats.ExplosionLocker)
                        MuzzleFlashManager.FlashList.Add(Flash);
                }
                else if (FlashTimer > 0f && Module.InstalledWeapon.MuzzleFlash != null && MuzzleFlashAdded)
                {
                    FlashTimer -= elapsedTime;
                    MuzzleFlash.Position = new Vector3(Module.Center.X, Module.Center.Y, -45f);
                    Flash.WorldMatrix = Matrix.CreateRotationZ(Rotation) * Matrix.CreateTranslation(MuzzleFlash.Position);
                    MuzzleFlash.World = Matrix.CreateTranslation(MuzzleFlash.Position);
                }
            }
            if (FlashTimer <= 0f && MuzzleFlashAdded)
            {
                Empire.Universe.RemoveLight(MuzzleFlash);
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
            DroneAI?.Dispose();
            DroneAI = null;
        }

        public override string ToString() => $"Proj[{WeaponType}] Wep={Weapon?.Name} Pos={Center} Rad={Radius} Loy=[{Loyalty}]";

        public void CreateHitParticles(float damageAmount, Vector3 center)
        {
            AddKineticParticleHitEffects(damageAmount, center);
            AddEnergyParticleHitEffects(damageAmount, center);
        }

        private void AddKineticParticleHitEffects(float damageAmount, Vector3 center)
        {
            if (Weapon?.Tag_Kinetic != true) return;

            float flashChance = GetHitProjectileFlashEmitChance(damageAmount);
            if (HasParticleHitEffect(flashChance))
            {
                Empire.Universe.flash.AddParticleThreadB(GetBackgroundPos(center), Vector3.Zero);
                return;
            }
            float beamFlashChance = GetHitProjectileBeamFlashEmitChance(Weapon.ProjectileSpeed);
            if (HasParticleHitEffect(beamFlashChance))
                Empire.Universe.beamflashes.AddParticleThreadB(GetBackgroundPos(center), Vector3.Zero);
        }

        private void AddEnergyParticleHitEffects(float damageAmount, Vector3 center)
        {
            if (Weapon?.Tag_Energy != true) return;
            float flashChance  = GetHitProjectileFlashEmitChance(damageAmount);
            float sparksChance = GetHitProjectileSparksEmitChance(Weapon.ProjectileSpeed);
            if (HasParticleHitEffect(flashChance))
                Empire.Universe.flash.AddParticleThreadB(GetBackgroundPos(center), Vector3.Zero);
            if (!HasParticleHitEffect(sparksChance)) return;
            int randomEffect = RandomMath2.IntBetween(0, 2);
            switch (randomEffect)
            {
                case 0:
                    for (int i = 0; i < 20; i++)
                        Empire.Universe.fireTrailParticles.AddParticleThreadB(GetBackgroundPos(center), Vector3.Zero);
                    for (int i = 0; i < 5; i++)
                        Empire.Universe.explosionSmokeParticles.AddParticleThreadB(GetBackgroundPos(center), Vector3.Zero);
                    break;
                case 1:
                    for (int i = 0; i < 50; i++)
                        Empire.Universe.sparks.AddParticleThreadB(GetBackgroundPos(center), Vector3.Zero);
                    Empire.Universe.smokePlumeParticles.AddParticleThreadB(GetBackgroundPos(center), Vector3.Zero);
                    break;
                case 2:
                    Empire.Universe.beamflashes.AddParticleThreadB(GetBackgroundPos(center), Vector3.Zero);
                    break;
            }
        }

        private static bool HasParticleHitEffect(float chance) => RandomMath.RandomBetween(0f, 100f) <= chance;

        private static float GetHitProjectileFlashEmitChance(float damage) => damage >= 1000f ? 100f : damage / 10f;

        private static float GetHitProjectileBeamFlashEmitChance(float speed) => speed > 10000f ? 100f : speed / 100f;

        private static float GetHitProjectileSparksEmitChance(float speed) => speed > 10000f ? 100f : speed / 100f;

        private static Vector3 GetBackgroundPos(Vector3 pos) => new Vector3(pos.X, pos.Y, pos.Z - 50f);
    }
}