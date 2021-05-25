using System;
using System.IO;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Ships;
using Ship_Game.Audio;
using Particle3DSample;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Gameplay
{
    public class Projectile : GameplayObject, IDisposable
    {
        public float ShieldDamageBonus;
        public float ArmorDamageBonus;
        public int ArmorPiercing;
        public bool IgnoresShields;
        public string WeaponType;
        MissileAI MissileAI;
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
        ParticleEmitter TrailEmitter;
        ParticleEmitter FiretrailEmitter;
        SceneObject ProjSO; // this is null for sprite based projectiles
        public Matrix WorldMatrix { get; private set; }
        public string InFlightCue = "";
        public AudioEmitter Emitter = new AudioEmitter();
        public bool Miss;
        public Empire Loyalty;
        float InitialDuration;
        public float RotationRadsPerSecond;
        public DroneAI DroneAI { get; private set; }
        public Weapon Weapon;
        public string ModelPath;
        float ZStart = -25f;
        float ParticleDelay;
        PointLight Light;
        public bool FirstRun = true;

        SpriteAnimation Animation;
        SubTexture ProjectileTexture;

        public bool DieNextFrame { get; private set; }
        readonly AudioHandle InFlightSfx = new AudioHandle();
        public string DieCueName = "";
        bool LightWasAddedToSceneGraph;
        bool UsesVisibleMesh;
        bool MuzzleFlashAdded;
        public Vector2 FixedError;
        public bool ErrorSet = false;
        public bool FlashExplode;
        bool Deflected;
        public bool TrailTurnedOn { get; protected set; } = true;
   

        public Ship Owner { get; protected set; }
        public Planet Planet { get; protected set; }

        public override IDamageModifier DamageMod => Weapon;

        public Projectile(Empire loyalty, GameObjectType type = GameObjectType.Proj) : base(type)
        {
            Loyalty = loyalty;
        }

        public static Projectile Create(Weapon weapon, Vector2 origin, Vector2 direction, GameplayObject target, bool playSound)
        {
            var projectile = new Projectile(weapon.Owner.loyalty, GameObjectType.Proj)
            {
                Weapon  = weapon,
                Owner   = weapon.Owner,
                Module  = weapon.Module
            };
            projectile.Initialize(origin, direction, target, playSound, Vector2.Zero);
            return projectile;
        }

        // For Mirv creation
        public static Projectile Create(Weapon weapon, Vector2 origin, Vector2 direction, GameplayObject target, 
            bool playSound, Vector2 inheritedVelocity, Empire loyalty, Planet planet)
        {
            var projectile = new Projectile(loyalty)
            {
                Weapon   = weapon,
                FirstRun = false,
                Planet   = planet
            };

            if (weapon.Owner != null)
                projectile.Owner = weapon.Owner;

            if (weapon.Module != null)
                projectile.Module = weapon.Module;

            projectile.Initialize(origin, direction, target, playSound, inheritedVelocity, isMirv: true);
            return projectile;
        }

        public static Projectile Create(Weapon weapon, Planet planet, Vector2 direction, GameplayObject target)
        {
            var projectile = new Projectile(planet.Owner, GameObjectType.Proj)
            {
                Weapon  = weapon,
                Planet  = planet,
                ZStart  = -2500f
            };
            projectile.Initialize(weapon.Origin, direction, target, playSound: true, Vector2.Zero);
            return projectile;
        }

        public struct ProjectileOwnership
        {
            public Ship Owner;
            public Planet Planet;
            public Weapon Weapon;
            public Empire Loyalty;
        }

        public static bool GetOwners(in Guid ownerGuid, int loyaltyId, string weaponUID, bool isBeam,
                                     UniverseData data, out ProjectileOwnership o)
        {
            o = default;
            o.Owner = data.FindShipOrNull(ownerGuid);
            if (o.Owner != null)
            {
                if (weaponUID.NotEmpty())
                    o.Weapon = o.Owner.Weapons.Find(w => w.UID == weaponUID);
            }
            else
            {
                o.Planet          = data.FindPlanetOrNull(ownerGuid);
                Building building = o.Planet?.BuildingList.Find(b => b.Weapon == weaponUID);
                if (building != null)
                    o.Weapon = building.TheWeapon;
            }

            if (loyaltyId > 0) // Older saves don't have loyalty ID, so this is for compatibility
                o.Loyalty = EmpireManager.GetEmpireById(loyaltyId);
            else
                o.Loyalty = o.Owner?.loyalty ?? o.Planet?.Owner;

            if (o.Loyalty == null || o.Owner == null && o.Planet == null)
            {
                Log.Warning($"Projectile Owner not found! guid={ownerGuid} weaponUid={weaponUID} loyalty={o.Loyalty}");
                return false;
            }

            // fallback, the owner has died, or this is a Mirv warhead (owner is a projectile)
            if (o.Weapon == null && !isBeam)
            {
                // This can fail if `weaponUID` no longer exists in game data
                // in which case we abandon this projectile
                ResourceManager.CreateWeapon(weaponUID, out o.Weapon);
            }

            if (o.Weapon == null)
                return false;

            return true;
        }

        // loading from savegame
        public static Projectile Create(in SavedGame.ProjectileSaveData pdata, UniverseData data)
        {
            if (!GetOwners(pdata.Owner, pdata.Loyalty, pdata.Weapon, false, data, out ProjectileOwnership o))
                return null; // this owner or weapon no longer exists

            var p = new Projectile(o.Loyalty)
            {
                Weapon = o.Weapon,
                Module = o.Weapon.Module,
                Owner = o.Owner,
                Planet = o.Planet
            };

            p.Initialize(pdata.Position, pdata.Velocity, null, playSound: false, Vector2.Zero);
            p.Duration = pdata.Duration; // apply duration from save data
            p.FirstRun = false;
            return p;
        }

        void Initialize(Vector2 origin, Vector2 direction, GameplayObject target, bool playSound, Vector2 inheritedVelocity, bool isMirv = false)
        {
            ++DebugInfoScreen.ProjCreated;
            Position = origin;
            Center   = origin;
            Emitter.Position = new Vector3(origin, 0f);

            Range                 = Weapon.BaseRange;
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
            ArmorPiercing         = Weapon.ArmorPen;
            TrailTurnedOn         = !Weapon.Tag_Guided || Weapon.DelayedIgnition.AlmostZero();

            Weapon.ApplyDamageModifiers(this); // apply all buffs before initializing
            if (Weapon.RangeVariance)
                Range *= RandomMath.RandomBetween(0.9f, 1.1f);

            float durationMod = 1.2f;
            if (Weapon.IsRepairDrone)    durationMod = 1.25f;
            else if (Weapon.Tag_Missile) durationMod = 2.0f;
            else if (Planet != null)     durationMod = 2.0f;

            // @todo Do not inherit parent velocity until we fix target prediction code
            // it is passed as vector zero parameter for now, unless it is MIRV
            // Vector2 inheritedVelocity = Vector2.Zero; // (Owner?.Velocity ?? Vector2.Zero);
            VelocityMax = Speed; // + inheritedVelocity.Length();
            Velocity    = Speed * direction + inheritedVelocity;
            Rotation    = Velocity.Normalized().ToRadians(); // used for drawing the projectile in correct direction

            InitialDuration = Duration = (Range/Speed + Weapon.DelayedIgnition) * durationMod;
            ParticleDelay  += Weapon.particleDelay;

            if (Owner?.loyalty.data.ArmorPiercingBonus > 0 && Weapon.Tag_Kinetic)
                ArmorPiercing += Owner.loyalty.data.ArmorPiercingBonus;

            if (Weapon.IsRepairDrone)
            {
                DroneAI = new DroneAI(this);
            }
            else if (Weapon.Tag_Guided)
            {
                if (!Weapon.isTurret && !isMirv)
                    Rotation = Owner?.Rotation + Weapon.Module?.FacingRadians ?? Rotation;

                Vector2 missileVelocity = inheritedVelocity != Vector2.Zero ? inheritedVelocity : Weapon.Owner?.Velocity ?? Vector2.Zero;
                MissileAI               = new MissileAI(this, target, missileVelocity);
            }
            
            LoadContent();
            Initialize();

            Empire.Universe?.Objects.Add(this);

            if (Owner != null)
            {
                SetSystem(Owner.System);
            }
            else if (Planet != null)
            {
                SetSystem(Planet.ParentSystem);
            }

            if (playSound && (System != null && System.isVisible || Owner?.InFrustum == true))
            {
                Weapon.PlayToggleAndFireSfx(Emitter);

                string cueName = ResourceManager.GetWeaponTemplate(Weapon.UID).dieCue;
                if (cueName.NotEmpty())     DieCueName  = cueName;
                if (InFlightCue.NotEmpty()) InFlightCue = Weapon.InFlightCue;
            }
        }

        void LoadContent()
        {
            ModelPath = Weapon.ModelPath;
            UsesVisibleMesh = Weapon.UseVisibleMesh || WeaponType == "Missile" || WeaponType == "Drone" || WeaponType == "Rocket";

            if (StarDriveGame.Instance == null)
                return; // allow spawning invisible projectiles inside Unit Tests

            if (Weapon.Animated == 1)
            {
                string animFolder = "Textures/" + Path.GetDirectoryName(Weapon.AnimationPath);
                Animation = new SpriteAnimation(ResourceManager.RootContent, animFolder);
                Animation.Looping = Weapon.LoopAnimation == 1;
                float loopDuration = (InitialDuration / Animation.NumFrames);
                float startAt = Animation.Looping ? UniverseRandom.RandomBetween(0f, loopDuration) : 0f;
                Animation.Start(loopDuration, startAt);
            }
            else
            {
                ProjectileTexture = ResourceManager.ProjTexture(Weapon.ProjectileTexturePath);
            }

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

        public void Deflect(Empire empire, Vector3 deflectionPoint)
        {
            Loyalty              = empire;
            Deflected            = true;
            Vector2 newDirection = RandomMath.RandomDirection();
            float momentumLoss   = 9 - RandomMath.RollDie(6);
            Duration            *= momentumLoss / 10;
            Speed               *= momentumLoss / 10;
            Velocity             = Speed * newDirection;
            Rotation             = Velocity.Normalized().ToRadians();
            if (RandomMath.RollDie(2) == 2)
                Empire.Universe.beamflashes.AddParticleThreadB(GetBackgroundPos(deflectionPoint), Vector3.Zero);
        }

        public void CreateMirv(GameplayObject target)
        {
            Weapon mirv = ResourceManager.CreateWeapon(Weapon.MirvWeapon);
            mirv.Owner  = Owner;
            mirv.Module = Module;
            bool playSound = true; // play sound once
            for (int i = 0; i < Weapon.MirvWarheads; i++)
            {
                float launchDir          = RandomMath.RollDie(2) == 1 ? -1.5708f : 1.5708f; // 90 degrees
                Vector2 separationVel    = (Rotation + launchDir).RadiansToDirection() * (100 + RandomMath.RollDie(40));
                Vector2 separationVector = mirv.Tag_Guided ? Velocity : separationVel;
                // Use separation velocity for mirv non guided, or just Velocity for guided (they will compensate)
                Create(mirv, Position, Direction, target, playSound, separationVector, Loyalty, Planet);
                playSound = false;
            }

            Die(null, false);
        }

        public override Vector2 JammingError()
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
            if (MissileAI?.Target.GetLoyalty() == attacker)
                return true;

            if (!attackerRelationThis.Treaty_OpenBorders && !attackerRelationThis.Treaty_Trade && Owner.IsInBordersOf(attacker))
                return true;
            
            return false;
        }

        public void Draw(SpriteBatch batch, GameScreen screen)
        {
            if (!InFrustum)
                return;

            if (!UsesVisibleMesh)
            {
                if (Animation != null)
                {
                    screen.ProjectToScreenCoords(Center, -ZStart, 20f*Weapon.ProjectileRadius*Weapon.Scale,
                                                 out Vector2 pos, out float size);

                    Animation.Draw(batch, pos, new Vector2(size), Rotation, 1f);
                }
                else
                {
                    var projMesh = ResourceManager.ProjectileModelDict[ModelPath];
                    screen.DrawTransparentModel(projMesh, WorldMatrix, ProjectileTexture, Weapon.Scale);
                }
            }

            if (MissileAI != null && MissileAI.Jammed)
            {
                screen.DrawStringProjected(Center + new Vector2(16), 50f, Color.Red, "Jammed");
            }
        }

        public void DamageMissile(GameplayObject source, float damageAmount)
        {
            //if (Health < 0.001f)
            //    Log.Info($"Projectile had no health {Weapon.Name}");
            Health -= damageAmount;
            if (Health <= 0f && Active)
                DieNextFrame = true;
        }

        public override void Die(GameplayObject source, bool cleanupOnly)
        {
            if (!Active)
            {
                Log.Error("Projectile.Die() was called on an already dead object");
                return;
            }

            ++DebugInfoScreen.ProjDied;
            if (Light != null)
                Empire.Universe.RemoveLight(Light);

            if (InFlightSfx.IsPlaying)
                InFlightSfx.Stop();

            ExplodeProjectile(cleanupOnly);

            if (ProjSO != null)
            {
                Empire.Universe.RemoveObject(ProjSO);
            }

            SetSystem(null);
            base.Die(source, cleanupOnly);
            Owner = null;
        }

        public override void Update(FixedSimTime timeStep)
        {
            if (!Active)
            {
                Log.Error("Projectile.Update() called when dead!");
                return;
            }

            if (DieNextFrame)
            {
                Die(this, false);
                return;
            }
            
            Position += Velocity * timeStep.FixedTime;
            if (Weapon.Animated == 1 && InFrustum)
            {
                Animation.Update(timeStep.FixedTime);
            }

            if (InFlightSfx.IsStopped)
                InFlightSfx.PlaySfxAsync(InFlightCue, Emitter);

            ParticleDelay -= timeStep.FixedTime;
            if (Duration > 0f)
            {
                Duration -= timeStep.FixedTime;
                if (Duration < 0f)
                {
                    Health = 0f;
                    Die(null, false);
                    return;
                }
            }
            MissileAI?.Think(timeStep);
            DroneAI?.Think(timeStep);
            if (FirstRun && Module != null)
            {
                Position = Module.Center;
                Center = Module.Center;
                FirstRun = false;
            }
            else Center = Position;
            Emitter.Position = new Vector3(Center, 0.0f);
            if (InFrustum)
            {
                if (ZStart < -25.0)
                    ZStart += VelocityMax * timeStep.FixedTime;
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

            if (FiretrailEmitter != null && InFrustum && TrailTurnedOn)
            {
                if (ParticleDelay <= 0.0f && Duration > 0.5)
                {
                    FiretrailEmitter.UpdateProjectileTrail(timeStep.FixedTime, newPosition, Velocity + VelocityDirection * Speed * 1.75f);
                }
            }
            if (TrailEmitter != null && InFrustum && TrailTurnedOn)
            {
                if (ParticleDelay <= 0.0f && Duration > 0.5)
                {
                    TrailEmitter.Update(timeStep.FixedTime, newPosition);
                }
            }

            if (InFrustum && Light == null && Weapon.Light != null && !LightWasAddedToSceneGraph)
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
            else if (Light != null && Weapon.Light != null && LightWasAddedToSceneGraph)
            {
                Light.Position = new Vector3(Center.X, Center.Y, -25f);
                Light.World = Matrix.CreateTranslation(Light.Position);
            }
            if (Module != null && !MuzzleFlashAdded && Module.InstalledWeapon?.MuzzleFlash != null && InFrustum)
            {
                MuzzleFlashAdded = true;
                MuzzleFlashManager.AddFlash(this);
            }
            base.Update(timeStep);
        }


        bool CloseEnoughForExplosion    => Empire.Universe.IsSectorViewOrCloser;
        bool CloseEnoughForFlashExplode => Empire.Universe.IsSystemViewOrCloser;

        void ExplodeProjectile(bool cleanupOnly, ShipModule atModule = null)
        {
            Vector3 origin = new Vector3(atModule?.Center ?? Center, -50f);
            if (Explodes)
            {
                if (Weapon.OrdinanceRequiredToFire > 0f && Owner != null)
                {
                    DamageRadius += Owner.loyalty.data.OrdnanceEffectivenessBonus * DamageRadius;
                }

                if (!cleanupOnly && CloseEnoughForExplosion)
                {
                    ExplosionManager.AddExplosion(origin, Velocity*0.1f,
                        DamageRadius * ExplosionRadiusMod, 2.5f, Weapon.ExplosionType);

                    if (FlashExplode && CloseEnoughForFlashExplode)
                    {
                        GameAudio.PlaySfxAsync(DieCueName, Emitter);
                        Empire.Universe.flash.AddParticleThreadB(origin, Vector3.Zero);
                    }
                }

                // Using explosion at a specific module not to affect other ships which might bypass other modulesfor them , like armor
                if (atModule != null) 
                    UniverseScreen.Spatial.ExplodeAtModule(this, atModule, IgnoresShields, DamageAmount, DamageRadius);
                else
                    UniverseScreen.Spatial.ProjectileExplode(this, DamageAmount, DamageRadius, Center);
            }
            else if (Weapon.FakeExplode && CloseEnoughForExplosion)
            {
                ExplosionManager.AddExplosion(origin, Velocity*0.1f, 
                    DamageRadius * ExplosionRadiusMod, 2.5f, Weapon.ExplosionType);
                if (FlashExplode && CloseEnoughForFlashExplode)
                {
                    GameAudio.PlaySfxAsync(DieCueName, Emitter);
                    Empire.Universe.flash.AddParticleThreadB(origin, Vector3.Zero);
                }
            }
        }

        public void GuidedMoveTowards(FixedSimTime timeStep, Vector2 targetPos, float thrustNozzleRotation,
                                      bool terminalPhase = false)
        {
            float distance = Center.Distance(targetPos);
            
            bool finalPhase = distance <= 1000f;

            Vector2 adjustedPos = finalPhase ? targetPos // if we get close, then just aim at targetPos
                // if we're still far, apply thrust offset, which will increase our accuracy
                : ImpactPredictor.ThrustOffset(Center, Velocity, targetPos, 1f);

            //var debug = Empire.Universe?.DebugWin;
            //debug?.DrawLine(DebugModes.Targeting, Center, adjustedPos, 1f, Color.DarkOrange.Alpha(0.2f), 0f);
            //debug?.DrawLine(DebugModes.Targeting, targetPos, adjustedPos, 1f, Color.DarkRed.Alpha(0.8f), 0f);
            //debug?.DrawCircle(DebugModes.Targeting, adjustedPos, 5f, Color.DarkRed.Alpha(0.28f), 0f);
            //debug?.DrawCircle(DebugModes.Targeting, targetPos, 5f, Color.DarkOrange.Alpha(0.2f), 0f);

            float acceleration = Speed * 3f;

            if (this.RotationNeededForTarget(adjustedPos, 0.05f, out float angleDiff, out float rotationDir))
            {
                float rotationRadsPerSec = RotationRadsPerSecond;
                if (rotationRadsPerSec <= 0f)
                    rotationRadsPerSec = Speed / 350f;

                Rotation += rotationDir * Math.Min(angleDiff, timeStep.FixedTime*rotationRadsPerSec);
                Rotation = Rotation.AsNormalizedRadians();
            }

            if (angleDiff < 0.3f) // mostly facing our target
            {
                float nozzleRotation;
                if (finalPhase) // correct nozzle towards target
                    nozzleRotation = rotationDir*angleDiff;
                else // apply user provided nozzle rotation
                    nozzleRotation = thrustNozzleRotation;

                // limit max nozzle rotation
                // 0.52 ~ 30 degrees 
                nozzleRotation = nozzleRotation.Clamped(-0.52f, +0.52f);

                Vector2 thrustDirection = (Rotation + nozzleRotation).RadiansToDirection();
                Velocity += thrustDirection * (acceleration * timeStep.FixedTime);
            }
            else if (Velocity.Length() > 200) // apply magic braking effect, this helps avoid useless rocket spirals
            {
                acceleration *= -0.2f;
                Velocity += Velocity.Normalized() * (acceleration * timeStep.FixedTime * 0.5f);
            }

            float maxVel = VelocityMax * (terminalPhase ? Weapon.TerminalPhaseSpeedMod : 1f);
            if (Velocity.Length() > maxVel)
                Velocity = Velocity.Normalized() * maxVel;    
        }

        public void MoveStraight()
        {
            if (TrailTurnedOn) // engine is ignited
                Velocity = Direction * VelocityMax; 
        }
        
        public bool Touch(GameplayObject target)
        {
            if (Miss || target == Owner)
                return false;

            if (!target.Active)
            {
                Log.Error("BUG: touching a DEAD module. No necrophilia allowed!");
                return false;
            }

            switch (target)
            {
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
                    if (!Loyalty.IsEmpireAttackable(parent.loyalty, parent))
                        return false;

                    if (Weapon.TruePD)
                    {
                        DieNextFrame = true;
                        return true;
                    }

                    ArmourPiercingTouch(module, parent);
                    Health = 0f;
                    break;
            }

            switch (WeaponEffectType)
            {
                case "Plasma":
                {
                    var center  = new Vector3(Center.X, Center.Y, -100f);
                    Vector3 forward  = Rotation.RadiansToDirection3D();
                    Vector3 right    = forward.RightVector2D(z:1f);
                    Vector3 backward = -forward;
                    for (int i = 0; i < 20; i++)
                    {
                        Vector3 random = UniverseRandom.Vector3D(250f) * right;
                        Empire.Universe.flameParticles.AddParticleThreadA(center, random);

                        random = UniverseRandom.Vector3D(150f) + backward;
                        Empire.Universe.flameParticles.AddParticleThreadA(center, random);
                    }

                    break;
                }
                // currently unused
                case "MuzzleBlast":
                {
                    var center  = new Vector3(Center.X, Center.Y, -100f);
                    Vector3 forward  = Rotation.RadiansToDirection3D();
                    Vector3 right    = forward.RightVector2D(z:1f);
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

                    break;
                }
                default:
                {
                    if (WeaponType == "Ballistic Cannon")
                    {
                        if (target is ShipModule shipModule && !shipModule.Is(ShipModuleType.Shield))
                            GameAudio.PlaySfxAsync("sd_impact_bullet_small_01", Emitter);
                    }
                    break;
                }
            }

            DieNextFrame = !Deflected;
            return true;
        }

        void RayTracedExplosion(ShipModule module)
        {
            ExplodeProjectile(false, module);
            Explodes = false;
            if (Weapon != null)
                Weapon.FakeExplode = false;
        }

        void DebugTargetCircle()
        {
            Empire.Universe?.DebugWin?.DrawGameObject(DebugModes.Targeting, this);
        }

        void ArmourPiercingTouch(ShipModule module, Ship parent)
        {
            // Doc: If module has resistance to Armour Piercing effects, 
            // deduct that from the projectile's AP before starting AP and damage checks
            ArmorPiercing -= module.APResist;

            if (!module.Is(ShipModuleType.Armor) || ArmorPiercing < module.XSIZE)
            {
                if (Explodes)
                {
                    RayTracedExplosion(module);
                    return;
                }

                module.Damage(this, DamageAmount, out DamageAmount);
            }

            if (DamageAmount <= 0f)
                return;

            ArmorPiercing -= module.XSIZE;
            var projectedModules = parent.RayHitTestModules(module.Center, VelocityDirection, distance:parent.Radius, rayRadius:Radius);

            DebugTargetCircle();
            for (int i = 1; i < projectedModules.Count; i++) // I is 1 since we dealt with the first module above to save performance
            {
                ShipModule impactModule = projectedModules[i];
                if (!impactModule.Active)
                    continue;

                if (ArmorPiercing > 0 && impactModule.Is(ShipModuleType.Armor))
                {
                    ArmorPiercing -= impactModule.APResist; 
                    impactModule.DebugDamageCircle();
                    if (ArmorPiercing >= impactModule.XSIZE) // armor is always squared anyway.
                    {
                        ArmorPiercing -= impactModule.XSIZE;
                        continue; // Phase through this armor module (yikes!)
                    }
                }

                impactModule.DebugDamageCircle();
                if (Explodes)
                {
                    RayTracedExplosion(impactModule);
                    return;
                }

                impactModule.Damage(this, DamageAmount, out DamageAmount);
                // It is possible for a high AP projectile to pierce 1 armor, damage several modules and them pierce more armor modules
                // as long as it has enough AP left and not exploding, which is cool
                if (DamageAmount <= 0f)
                    return;
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
            DroneAI = null;
        }

        public override string ToString() => $"Proj[{WeaponType}] Wep={Weapon?.Name} Pos={Center} Rad={Radius} Loy=[{Loyalty}]";

        public void CreateHitParticles(float damageAmount, Vector3 center)
        {
            AddKineticParticleHitEffects(damageAmount, center);
            AddEnergyParticleHitEffects(damageAmount, center);
        }

        void AddKineticParticleHitEffects(float damageAmount, Vector3 center)
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

        void AddEnergyParticleHitEffects(float damageAmount, Vector3 center)
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

        public void IgniteEngine()
        {
            TrailTurnedOn = true;
        }

        static bool HasParticleHitEffect(float chance) => RandomMath.RandomBetween(0f, 100f) <= chance;

        static float GetHitProjectileFlashEmitChance(float damage) => damage >= 1000f ? 100f : damage / 10f;

        static float GetHitProjectileBeamFlashEmitChance(float speed) => speed > 10000f ? 100f : speed / 100f;

        static float GetHitProjectileSparksEmitChance(float speed) => speed > 10000f ? 100f : speed / 100f;

        static Vector3 GetBackgroundPos(Vector3 pos) => new Vector3(pos.X, pos.Y, pos.Z - 50f);
    }
}