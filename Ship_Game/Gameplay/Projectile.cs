using System;
using System.IO;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Ships;
using Ship_Game.Audio;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Graphics.Particles;
using Ship_Game.Universe;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;

using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using Vector2d = SDGraphics.Vector2d;
using Vector3d = SDGraphics.Vector3d;

namespace Ship_Game.Gameplay
{
    public class Projectile : PhysicsObject, IDisposable
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
        public bool FakeExplode;
        public ShipModule Module;

        // projectile has a trail (like missile trail)
        ParticleEffect TrailEffect;
        float TrailOffset;

        // the particle itself explodes
        ParticleEffect DeathEffect;

        protected class HitEffectState
        {
            public ParticleEffect Fx;
            public float Timer;
            public Vector3 HitPos;
            public Vector3 Normal;
            public HitEffectState(ParticleEffect fx)  { Fx = fx; }
            public void Update(in Vector3 hitPos, in Vector3 normal, float timer)
            {
                Timer = timer;
                HitPos = hitPos;
                Normal = normal;
            }
        }

        // currently active HitEffect (if any)
        // beams have different hit effects, and hitting shields or armor has different effects as well
        protected HitEffectState HitEffect;

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
        float ZPos = -25f;
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
        public Vector2 FixedError;
        public bool ErrorSet = false;
        public bool FlashExplode;
        bool Deflected;
        public bool TrailTurnedOn { get; protected set; } = true;

        public Ship Owner { get; protected set; }
        public Planet Planet { get; protected set; }

        // Only Guided Missiles can slow down, but lower the acceleration
        const float DecelThrustPower = 0.1f;

        public UniverseState Universe;

        public override IDamageModifier DamageMod => Weapon;

        public Projectile(int id, Empire loyalty, GameObjectType type = GameObjectType.Proj)
            : base(id, type)
        {
            Loyalty = loyalty;
        }

        // new projectile from a ship
        public static Projectile Create(Weapon weapon, Ship ship, Vector2 origin, Vector2 direction,
                                        GameplayObject target, bool playSound)
        {
            // Need to check here for better debug experience, since these crashes sneak in from time to time
            if (ship == null) throw new NullReferenceException(nameof(ship));
            if (ship.Universe == null) throw new NullReferenceException(nameof(ship.Universe));
            if (ship.Loyalty == null) throw new NullReferenceException(nameof(ship.Loyalty));

            var projectile = new Projectile(ship.Universe.CreateId(), ship.Loyalty, GameObjectType.Proj)
            {
                Owner = ship,
                Weapon = weapon,
                Module = weapon.Module
            };
            projectile.Initialize(origin, direction, target, playSound, Vector2.Zero);
            return projectile;
        }

        // new Mirv cluster warhead
        // the original missile will be destroyed, but was launched by a Ship or by a Planet
        static void CreateMirvWarhead(Weapon warhead, Vector2 origin, Vector2 direction, GameplayObject target, 
                                      bool playSound, Vector2 inheritedVelocity, Empire loyalty, Planet planet)
        {
            // Loyalty cannot be null, otherwise kill events will not work correctly
            if (loyalty == null) throw new NullReferenceException(nameof(loyalty));

            UniverseState universe = (planet?.Universe ?? loyalty.Universum);
            if (universe == null) throw new NullReferenceException(nameof(universe));

            var projectile = new Projectile(universe.CreateId(), loyalty)
            {
                Weapon = warhead,
                Owner = warhead.Owner,
                Module = warhead.Module,
                Planet = planet,
                FirstRun = false,
            };

            projectile.Initialize(origin, direction, target, playSound, inheritedVelocity, isMirv: true);
        }

        // new projectile from planet
        public static Projectile Create(Weapon weapon, Planet planet, Empire loyalty, Vector2 direction, GameplayObject target)
        {
            if (loyalty == null) throw new NullReferenceException(nameof(loyalty));
            if (planet.Universe == null) throw new NullReferenceException(nameof(planet.Universe));

            var projectile = new Projectile(planet.Universe.CreateId(), loyalty, GameObjectType.Proj)
            {
                Weapon  = weapon,
                Planet  = planet,
                ZPos  = 2500f, // +Z: deep in background, away from camera
            };
            projectile.Initialize(weapon.Origin, direction, target, playSound: true, Vector2.Zero);
            return projectile;
        }

        // loading from savegame
        public static Projectile CreateFromSave(in SavedGame.ProjectileSaveData pdata, UniverseState us)
        {
            if (!GetOwners(pdata.OwnerId, pdata.Loyalty, pdata.Weapon, false, us, out ProjectileOwnership o))
                return null; // this owner or weapon no longer exists

            var p = new Projectile(pdata.Id, o.Loyalty)
            {
                Weapon = o.Weapon,
                Module = o.Weapon.Module,
                Owner = o.Owner,
                Planet = o.Planet,
                Universe = us,
            };

            p.Initialize(pdata.Position, pdata.Velocity, null, playSound: false, Vector2.Zero);
            p.Duration = pdata.Duration; // apply duration from save data
            p.FirstRun = false;
            return p;
        }

        public struct ProjectileOwnership
        {
            public Ship Owner;
            public Planet Planet;
            public Weapon Weapon;
            public Empire Loyalty;
        }

        public static bool GetOwners(int shipOrPlanetId, int loyaltyId, string weaponUID, bool isBeam,
                                     UniverseState us, out ProjectileOwnership o)
        {
            o = default;
            o.Owner = us.GetShip(shipOrPlanetId);
            if (o.Owner != null)
            {
                // TODO: this is a buggy fallback because it always returns the first Weapon match,
                // TODO: leading to incorrect weapon reference
                // TODO: however, this is really hard to fix, because we don't save Weapon instances
                if (weaponUID.NotEmpty())
                    o.Weapon = o.Owner.Weapons.Find(w => w.UID == weaponUID);
            }
            else
            {
                o.Planet = us.GetPlanet(shipOrPlanetId);
                Building building = o.Planet?.BuildingList.Find(b => b.Weapon == weaponUID);
                o.Weapon = building?.TheWeapon;
            }

            if (loyaltyId > 0) // Older saves don't have loyalty ID, so this is for compatibility
                o.Loyalty = EmpireManager.GetEmpireById(loyaltyId);
            else
                o.Loyalty = o.Owner?.Loyalty ?? o.Planet?.Owner;

            if (o.Loyalty == null || o.Owner == null && o.Planet == null)
            {
                Log.Warning($"Projectile Owner not found! Owner.Id={shipOrPlanetId} weaponUID={weaponUID} loyalty={o.Loyalty}");
                return false;
            }

            // fallback, the owner has died, or this is a Mirv warhead (owner is a projectile)
            if (o.Weapon == null && !isBeam)
            {
                // This can fail if `weaponUID` no longer exists in game data
                // in which case we abandon this projectile
                if (ResourceManager.GetWeaponTemplate(weaponUID, out IWeaponTemplate t))
                {
                    o.Weapon = new Weapon(t, o.Owner, null, null);
                }
            }

            if (o.Weapon == null)
                return false; // abandon it

            return true;
        }

        void Initialize(Vector2 origin, Vector2 direction, GameplayObject target, bool playSound, Vector2 inheritedVelocity, bool isMirv = false)
        {
            ++DebugInfoScreen.ProjCreated;
            Position = origin;
            Emitter.Position = new Vector3(origin, 0f);

            Range                 = Weapon.BaseRange;
            Radius                = Weapon.ProjectileRadius;
            Explodes              = Weapon.Explodes;
            FakeExplode           = Weapon.FakeExplode;
            DamageAmount          = Weapon.GetDamageWithBonuses(Owner);
            DamageRadius          = Weapon.ExplosionRadius;
            ExplosionRadiusMod    = Weapon.ExplosionRadiusVisual;
            Health                = Weapon.HitPoints;
            Speed                 = Weapon.ProjectileSpeed;
            TrailOffset           = Weapon.TrailOffset;
            WeaponType            = Weapon.WeaponType;
            RotationRadsPerSecond = Weapon.RotationRadsPerSecond;
            ArmorPiercing         = Weapon.ArmorPen;
            TrailTurnedOn         = !Weapon.Tag_Guided || Weapon.DelayedIgnition.AlmostZero();

            Weapon.ApplyDamageModifiers(this); // apply all buffs before initializing
            if (Weapon.RangeVariance)
                Range *= RandomMath.Float(0.9f, 1.1f);

            float durationMod = 1.2f;
            if (Weapon.IsRepairDrone)    durationMod = 1.25f;
            else if (Weapon.Tag_Missile) durationMod = 2.0f;
            else if (Planet != null)     durationMod = 2.0f;

            // @todo Do not inherit parent velocity until we fix target prediction code
            // it is passed as vector zero parameter for now, unless it is MIRV
            // Vector2 inheritedVelocity = Vector2.Zero; // (Owner?.Velocity ?? Vector2.Zero);
            VelocityMax = Speed;
            SetInitialVelocity(Speed * direction + inheritedVelocity);

            InitialDuration = Duration = (Range/Speed + Weapon.DelayedIgnition) * durationMod;
            ParticleDelay  += Weapon.ParticleDelay;

            if (Owner?.Loyalty.data.ArmorPiercingBonus > 0 && Weapon.Tag_Kinetic)
                ArmorPiercing += Owner.Loyalty.data.ArmorPiercingBonus;

            if (Weapon.IsRepairDrone)
            {
                DroneAI = new DroneAI(this);
            }
            else if (Weapon.Tag_Guided)
            {
                if (!Weapon.IsTurret && !isMirv)
                    Rotation = Owner?.Rotation + Weapon.Module?.TurretAngleRads ?? Rotation;

                Vector2 missileVelocity = inheritedVelocity != Vector2.Zero ? inheritedVelocity : Weapon.Owner?.Velocity ?? Vector2.Zero;
                MissileAI = new MissileAI(this, target, missileVelocity);
            }

            Universe = Owner?.Universe ?? Planet.Universe;
            Universe.Objects.Add(this);

            LoadContent();

            if (Owner != null)
            {
                SetSystem(Owner.System);
            }
            else if (Planet != null)
            {
                SetSystem(Planet.ParentSystem);
            }

            bool inFrustum = IsInFrustum(Universe.Screen);
            if (playSound && inFrustum)
            {
                Weapon.PlayToggleAndFireSfx(Emitter);
                string cueName = ResourceManager.GetWeaponTemplate(Weapon.UID)?.DieCue;
                if (cueName.NotEmpty())     DieCueName  = cueName;
                if (InFlightCue.NotEmpty()) InFlightCue = Weapon.InFlightCue;
            }

            if (inFrustum && Module?.InstalledWeapon?.MuzzleFlash != null)
            {
                Universe.Screen.Particles.BeamFlash.AddParticle(new Vector3(origin, ZPos), 0.5f);
            }

            // TODO:
            //if (inFrustum)
            {
                UpdateWorldMatrix();
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
                float startAt = Animation.Looping ? UniverseRandom.Float(0f, loopDuration) : 0f;
                Animation.Start(loopDuration, startAt);
            }
            else
            {
                ProjectileTexture = ResourceManager.ProjTexture(Weapon.ProjectileTexturePath);
            }

            if (Weapon.WeaponTrailEffect.NotEmpty() && Universe.Screen.Particles != null)
            {
                var pos3D = new Vector3(Position, ZPos);
                TrailEffect = Universe.Screen.Particles.CreateEffect(Weapon.WeaponTrailEffect, pos3D, context: this);
            }
        }

        public void Deflect(Empire empire, Vector3 deflectionPoint)
        {
            Loyalty = empire;
            Deflected = true;
            Vector2 newDirection = RandomMath.RandomDirection();
            float momentumLoss = 9 - RandomMath.RollDie(6);
            Duration *= momentumLoss / 10;
            Speed    *= momentumLoss / 10;
            SetInitialVelocity(Speed * newDirection);
            if (RandomMath.RollDie(2) == 2)
            {
                var foregroundPos = new Vector3(deflectionPoint.X, deflectionPoint.Y, deflectionPoint.Z - 50f);
                Universe.Screen.Particles.BeamFlash.AddParticle(foregroundPos, Vector3.Zero);
            }
        }

        public void CreateMirv(GameplayObject target)
        {
            // breadcrumbs for easier debugging when we run into these rare bugs
            if (Owner == null && Planet == null)
            {
                Log.Error("CreateMirv: Owner and Planet were null");
                // NOTE: if Owner && Planet are null, this projectile is bugged out and can't spawn Mirv
            }
            else
            {
                // this is the spawned warhead weapon stats
                Weapon warhead = ResourceManager.CreateWeapon(Weapon.MirvWeapon, Owner, Module, null);

                for (int i = 0; i < Weapon.MirvWarheads; i++)
                {
                    // Use separation velocity for mirv non guided, or just Velocity for guided (they will compensate)
                    Vector2 separationVector = Velocity;
                    if (warhead.Tag_Guided)
                    {
                        float launchDir = RandomMath.RollDie(2) == 1 ? -RadMath.Deg90AsRads : RadMath.Deg90AsRads;
                        Vector2 separationVel = (Rotation + launchDir).RadiansToDirection() * (100 + RandomMath.RollDie(40));
                        separationVector += separationVel; // Add it to the initial velocity
                    }

                    bool playSound = i == 0; // play sound once
                    CreateMirvWarhead(warhead, Position, Direction, target, playSound, separationVector, Loyalty, Planet);
                }
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
                    screen.ProjectToScreenCoords(Position, ZPos, 20f * Weapon.ProjectileRadius * Weapon.Scale,
                                                 out Vector2d pos, out double size);

                    Animation.Draw(batch, pos, new Vector2d(size), Rotation, 1f);
                }
                else
                {
                    var projMesh = ResourceManager.ProjectileModelDict[ModelPath];
                    screen.DrawTransparentModel(projMesh, WorldMatrix, ProjectileTexture, Weapon.Scale);
                }
            }

            if (MissileAI != null && MissileAI.Jammed)
            {
                screen.DrawStringProjected(Position + new Vector2(16), 50f, Color.Red, "Jammed");
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
                Universe.Screen.RemoveLight(Light, dynamic:true);

            if (InFlightSfx.IsPlaying)
                InFlightSfx.Stop();

            ExplodeProjectile(cleanupOnly);

            if (ProjSO != null)
            {
                Universe.Screen.RemoveObject(ProjSO);
            }

            SetSystem(null);
            base.Die(source, cleanupOnly);
            Owner = null;
        }

        public bool TerminalPhase;

        void UpdateVelocityAndPos(float dt)
        {
            float maxVel = VelocityMax * (TerminalPhase ? Weapon.TerminalPhaseSpeedMod : 1f);
            var a = new AccelerationState(Velocity, maxVel, Rotation, ThrustAcceleration, DecelThrustPower);

            // standard particles have no acceleration
            if (ThrustThisFrame == Thrust.Coast && Acceleration == Vector2.Zero)
            {
                if (a.Velocity > a.MaxVelocity)
                    Velocity = a.VelocityDir * a.MaxVelocity;

                // constant acceleration can be calculated more easily
                IntegrateExplicitEulerConstantVelocity(dt);
            }
            else
            {
                // update Position Velocity and Acceleration using Velocity Verlet
                Vector2 acc = GetThrustAcceleration(a);
                IntegratePosVelocityVerlet(dt, acc);
            }

            ResetForcesThisFrame();
        }

        public void TestUpdatePhysics(FixedSimTime timeStep)
        {
            if (!Active)
                return;
            UpdateVelocityAndPos(timeStep.FixedTime);
            Duration -= timeStep.FixedTime;
            if (Duration < 0f)
            {
                Health = 0f;
                Die(null, false);
            }
        }

        public override void Update(FixedSimTime timeStep)
        {
            if (!Active)
            {
                Log.Error("Projectile.Update() called when dead!");
                return;
            }

            var pos3d = new Vector3(Position.X, Position.Y, ZPos - 10f);

            // death effect can only trigger once
            if (DeathEffect != null)
            {
                DeathEffect.Update(timeStep, pos3d);
                DeathEffect = null;
            }

            if (HitEffect != null)
            {
                if (InFrustum)
                    HitEffect.Fx?.Update(timeStep, HitEffect.HitPos, HitEffect.Normal);
                HitEffect.Timer -= timeStep.FixedTime;
                if (HitEffect.Timer < 0f)
                    HitEffect = null;
            }

            if (DieNextFrame)
            {
                Die(this, false);
                return;
            }

            if (InFrustum && Weapon.Animated == 1)
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
                Position = Module.Position;
                FirstRun = false;
            }

            UpdateVelocityAndPos(timeStep.FixedTime);
            Emitter.Position = pos3d;

            if (InFrustum)
            {
                // always put missiles below ships, +25 means away from camera into background
                if (ZPos > 25f)
                    ZPos -= VelocityMax * timeStep.FixedTime; // come closer to camera
                else
                    ZPos = 25f;

                UpdateWorldMatrix();

                if (UsesVisibleMesh) // lazy init rocket projectile meshes
                {
                    UpdateMesh();
                }

                if (TrailTurnedOn && ParticleDelay <= 0f && Duration > 0.5f && TrailEffect != null)
                {
                    UpdateTrailEffect(timeStep);
                }

                if (!LightWasAddedToSceneGraph && Weapon.Light != null && Light == null && Universe.Screen.CanAddDynamicLight)
                {
                    LightWasAddedToSceneGraph = true;
                    Light = CreateLight();
                    Universe.Screen.AddLight(Light, dynamic: true);
                }
                else if (Light != null && Weapon.Light != null && LightWasAddedToSceneGraph)
                {
                    Light.Position = pos3d;
                    Light.World = Matrix.CreateTranslation(Light.Position);
                }
            }

            base.Update(timeStep);
        }

        void UpdateWorldMatrix()
        {
            WorldMatrix = Matrix.CreateScale(Weapon.Scale) 
                        * Matrix.CreateRotationZ(Rotation)
                        * Matrix.CreateTranslation(Position.X, Position.Y, ZPos);
        }

        void UpdateMesh()
        {
            if (ProjSO == null)
            {
                if (ResourceManager.ProjectileMeshDict.TryGetValue(ModelPath, out ModelMesh mesh))
                {
                    ProjSO = new SceneObject(mesh)
                    {
                        Visibility = ObjectVisibility.Rendered,
                        ObjectType = ObjectType.Dynamic,
                        World = WorldMatrix
                    };
                    Universe.Screen.AddObject(ProjSO);
                }
                else
                {
                    Log.Warning($"No such mesh: '{ModelPath}'");
                }
            }
            else
            {
                ProjSO.World = WorldMatrix;
            }
        }

        void UpdateTrailEffect(FixedSimTime timeStep)
        {
            var forward = Direction;

            var pos3d = new Vector3(Position, ZPos);
            var trailPos = new Vector3(pos3d.X + forward.X * TrailOffset,
                                       pos3d.Y + forward.Y * TrailOffset, pos3d.Z);

            // always set trail velocity to negative direction, ignore true velocity
            var trailVel = new Vector3(forward * Speed * -1.75f, 0f);

            if (Universe.Debug && DebugInfoScreen.Mode == DebugModes.Particles)
            {
                Universe.DebugWin.DrawCircle(DebugModes.Particles, trailPos, 8, Color.Red);
                Universe.DebugWin.DrawCircle(DebugModes.Particles, pos3d, 8, Color.Yellow);
            }

            TrailEffect.Update(timeStep, trailPos, trailVel);
        }
        
        protected HitEffectState CreateHitEffect(bool damagingShields, in Vector3 pos)
        {
            string effect = damagingShields ? Weapon.WeaponShieldHitEffect : Weapon.WeaponHitEffect;
            if (effect.IsEmpty())
                return null;
            var fx = Owner.Universe.Screen.Particles?.CreateEffect(effect, pos, this);
            if (fx == null)
                return null; // effect not found
            return new HitEffectState(fx);
        }

        public void CreateHitParticles(in Vector3 center, bool damagingShields)
        {
            if (HitEffect == null)
            {
                HitEffect = CreateHitEffect(damagingShields, center);
            }

            if (HitEffect != null)
            {
                HitEffect?.Update(center, Vector3.One, 0.1f);
            }
        }

        PointLight CreateLight()
        {
            var pos = new Vector3(Position.X, Position.Y, -25f);
            var light = new PointLight
            {
                Position = pos,
                Radius = 100f,
                World = Matrix.CreateTranslation(pos),
                ObjectType = ObjectType.Dynamic,
                Intensity = 1.7f,
                FillLight = true,
                Enabled = true
            };
            switch (Weapon.Light)
            {
                case "Green":  light.DiffuseColor = new Vector3(0.0f, 0.8f, 0.00f); break;
                case "Red":    light.DiffuseColor = new Vector3(1.0f, 0.0f, 0.00f); break;
                case "Orange": light.DiffuseColor = new Vector3(0.9f, 0.7f, 0.00f); break;
                case "Purple": light.DiffuseColor = new Vector3(0.8f, 0.8f, 0.95f); break;
                case "Blue":   light.DiffuseColor = new Vector3(0.0f, 0.8f, 1.00f); break;
            }
            return light;
        }

        bool CloseEnoughForExplosion    => Universe.Screen.IsSectorViewOrCloser;
        bool CloseEnoughForFlashExplode => Universe.Screen.IsSystemViewOrCloser;

        void ExplodeProjectile(bool cleanupOnly, ShipModule atModule = null)
        {
            bool visibleToPlayer = Module?.GetParent().InSensorRange == true;
            Vector3 origin = new Vector3(atModule?.Position ?? Position, -50f);
            if (Explodes)
            {
                if (Weapon.OrdinanceRequiredToFire > 0f && Owner != null)
                {
                    DamageRadius += Owner.Loyalty.data.OrdnanceEffectivenessBonus * DamageRadius;
                }

                if (!cleanupOnly && CloseEnoughForExplosion && visibleToPlayer)
                {
                    ExplosionManager.AddExplosion(Universe.Screen, origin, Velocity*0.1f,
                        DamageRadius * ExplosionRadiusMod, 2.5f, Weapon.ExplosionType);

                    if (FlashExplode && CloseEnoughForFlashExplode)
                    {
                        GameAudio.PlaySfxAsync(DieCueName, Emitter);
                        Universe.Screen.Particles.Flash.AddParticle(origin, Vector3.Zero);
                    }
                }

                // Using explosion at a specific module not to affect other ships which might bypass other modulesfor them , like armor
                if (atModule != null && (IgnoresShields || !atModule.ShieldsAreActive)) 
                    Universe.Spatial.ExplodeAtModule(this, atModule, IgnoresShields, DamageAmount, DamageRadius);
                else
                    Universe.Spatial.ProjectileExplode(this, DamageAmount, DamageRadius, Position);
            }
            else if (FakeExplode && CloseEnoughForExplosion && visibleToPlayer)
            {
                ExplosionManager.AddExplosion(Universe.Screen, origin, Velocity*0.1f, 
                    DamageRadius * ExplosionRadiusMod, 2.5f, Weapon.ExplosionType);
                if (FlashExplode && CloseEnoughForFlashExplode)
                {
                    GameAudio.PlaySfxAsync(DieCueName, Emitter);
                    Universe.Screen.Particles.Flash.AddParticle(origin, Vector3.Zero);
                }
            }
        }

        public void GuidedMoveTowards(FixedSimTime timeStep, Vector2 targetPos, float thrustNozzleRotation)
        {
            float distance = Position.Distance(targetPos);
            bool finalPhase = distance <= 1000f;

            Vector2 adjustedPos = finalPhase ? targetPos // if we get close, then just aim at targetPos
                // if we're still far, apply thrust offset, which will increase our accuracy
                : ImpactPredictor.ThrustOffset(Position, Velocity, targetPos);

            //var debug = Universe?.DebugWin;
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

                SetThrustThisFrame(acceleration, nozzleRotation, Thrust.Forward);
            }
            else if (Velocity.Length() > 200f) // apply magic braking effect, this helps avoid useless rocket spirals
            {
                SetThrustThisFrame(acceleration * 0.1f, 0f, Thrust.Reverse);
            }
        }

        public void MoveStraight()
        {
            if (TrailTurnedOn) // engine is ignited
                SetThrustThisFrame(VelocityMax*0.5f, 0f, Thrust.Forward);
        }
        
        public bool Touch(GameplayObject target, Vector2 hitPos)
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

                    if (projectile.Loyalty == null || Owner?.Loyalty?.IsEmpireAttackable(projectile.Loyalty) == false)
                        return false;                
                    projectile.DamageMissile(this, DamageAmount);
                    DieNextFrame = true;
                    if (InFrustum) // TODO: efficiently check sensor range here
                        CreateWeaponDeathEffect(target);
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
                    if (!Loyalty.IsEmpireAttackable(parent.Loyalty, parent))
                        return false;

                    if (Weapon.TruePD)
                    {
                        DieNextFrame = true;
                        return true;
                    }

                    ArmorPiercingTouch(module, parent, hitPos);
                    Health = 0f;
                    if (parent.InSensorRange)
                        CreateWeaponDeathEffect(target);
                    break;
            }

            DieNextFrame = !Deflected;
            return true;
        }

        void CreateWeaponDeathEffect(GameplayObject target)
        {
            if (Weapon.WeaponDeathEffect.NotEmpty())
            {
                var pos3d = new Vector3(Position.X, Position.Y, ZPos - 10f);
                DeathEffect = Universe.Screen.Particles.CreateEffect(Weapon.WeaponDeathEffect, pos3d, this);
            }

            if (WeaponType == "Ballistic Cannon")
            {
                if (target is ShipModule shipModule && !shipModule.Is(ShipModuleType.Shield))
                    GameAudio.PlaySfxAsync("sd_impact_bullet_small_01", Emitter);
            }
        }

        void RayTracedExplosion(ShipModule module)
        {
            ExplodeProjectile(false, module);
            Explodes = false;
            FakeExplode = false;
        }

        void DebugTargetCircle()
        {
            Universe?.DebugWin?.DrawGameObject(DebugModes.Targeting, this, Universe.Screen);
        }

        void ArmorPiercingTouch(ShipModule module, Ship parent, Vector2 hitPos)
        {
            // Doc: If module has resistance to Armour Piercing effects, 
            // deduct that from the projectile's AP before starting AP and damage checks
            // It is possible for a high AP projectile to pierce 1 armor, damage several modules and
            // them pierce more armor modules as long as it has enough AP left and not exploding, which is cool
            if (IgnoresShields || !module.ShieldsAreActive)
                ArmorPiercing -= module.APResist;

            if (module.Is(ShipModuleType.Armor))
                ArmorPiercing -= module.XSize;

            if (!module.Is(ShipModuleType.Armor) || ArmorPiercing < module.XSize)
            {
                if (Explodes)
                {
                    RayTracedExplosion(module);
                    return;
                }

                module.Damage(this, DamageAmount, out DamageAmount);
            }

            DebugTargetCircle();
            ShipModule moduleToTest = module; // Starting the next modules scan from the hit module
            while (DamageAmount > 0)
            {
                Vector2 pos = moduleToTest.Position;
                float distance = parent.Radius;

                // using hitPos for ray traced hitPos if the module is a shield since shields have bigger radii and we must check
                // the actual hitPos and not continue from the module - since we do not know if the shields were damaged
                // or the actual module.
                if (moduleToTest.Is(ShipModuleType.Shield) && !IgnoresShields)
                {
                    pos = hitPos;
                    distance = distance.LowerBound(moduleToTest.ShieldRadius);
                }

                ShipModule nextModule = parent.RayHitTestNextModules(pos, VelocityDirection, distance, IgnoresShields);

                if (nextModule == null)
                    return;

                moduleToTest = nextModule;
                if (ArmorPiercing > 0 && IgnoresShields || !module.ShieldsAreActive)
                {
                    nextModule.DebugDamageCircle();
                    if (ArmorPiercing >= nextModule.XSize && module.Is(ShipModuleType.Armor)) // armor is always squared anyway.
                    {
                        ArmorPiercing -= nextModule.XSize;
                        continue; // Phase through this armor module (yikes!)
                    }
                }

                nextModule.DebugDamageCircle();
                if (Explodes)
                    RayTracedExplosion(nextModule);
                else
                    nextModule.Damage(this, DamageAmount, out DamageAmount);
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

        public override string ToString() => $"Proj[{WeaponType}] Wep={Weapon?.Name} Pos={Position} Rad={Radius} Loy=[{Loyalty}]";

        public void IgniteEngine()
        {
            TrailTurnedOn = true;
        }
    }
}