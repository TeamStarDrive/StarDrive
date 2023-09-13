using System;
using System.Collections.Generic;
using System.IO;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Ships;
using Ship_Game.Audio;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Data.Serialization;
using Ship_Game.Graphics.Particles;
using Ship_Game.Universe;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;

using Matrix = SDGraphics.Matrix;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using Vector2d = SDGraphics.Vector2d;
using Ship_Game.Data.Mesh;

namespace Ship_Game.Gameplay
{
    [StarDataType]
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

        public UniverseState Universe;
        [StarData] public Ship Owner { get; protected set; }
        [StarData] public Planet Planet { get; protected set; }
        [StarData] public Empire Loyalty;
        [StarData] public float Duration;
        [StarData] protected string WeaponUID;
        [StarData] public bool DieNextFrame { get; private set; }
        public Weapon Weapon;

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
        float InitialDuration;
        public float RotationRadsPerSecond;
        public DroneAI DroneAI { get; private set; }
        public string ModelPath;
        float ZPos = -25f;
        float ParticleDelay;
        PointLight Light;
        SpriteAnimation Animation;
        SubTexture ProjectileTexture;

        readonly AudioHandle InFlightSfx = new AudioHandle();
        public string DieCueName = "";
        bool LightWasAddedToSceneGraph;
        bool UsesVisibleMesh;
        public Vector2 FixedError;
        public bool ErrorSet = false;
        public bool FlashExplode;
        bool Deflected;
        public bool TrailTurnedOn { get; protected set; } = true;

        // Only Guided Missiles can slow down, but lower the acceleration
        const float DecelThrustPower = 0.1f;

        public override IDamageModifier DamageMod => Weapon;

        [StarDataConstructor]
        protected Projectile() : base(0, GameObjectType.Proj) {}

        // Create a NEW projectile
        protected Projectile(int id, Weapon weapon, Ship owner, Planet planet, Empire loyalty, GameObjectType type)
            : base(id, type)
        {
            Active = true;
            Owner = owner ?? weapon.Owner;
            Loyalty = loyalty ?? weapon.Owner?.Loyalty ?? planet?.Owner;
            Planet = planet;
            Module = weapon.Module;
            Weapon = weapon;
            WeaponUID = weapon.UID;

            Universe = Owner?.Universe ?? Planet?.Universe ?? Loyalty?.Universe;

            // Add to universe during creation
            Universe!.Objects.Add(this);
        }

        // new projectile from a ship
        public static Projectile Create(Weapon weapon, Ship ship, Vector2 origin, Vector2 direction,
                                        GameObject target, bool playSound)
        {
            // Need to check here for better debug experience, since these crashes sneak in from time to time
            if (ship == null) throw new NullReferenceException(nameof(ship));
            if (ship.Universe == null) throw new NullReferenceException(nameof(ship.Universe));
            if (ship.Loyalty == null) throw new NullReferenceException(nameof(ship.Loyalty));

            var p = new Projectile(ship.Universe.CreateId(), weapon, ship, null, ship.Loyalty, GameObjectType.Proj);
            p.Initialize(origin, direction, target, playSound, Vector2.Zero);
            return p;
        }

        // new Mirv cluster warhead
        // the original missile will be destroyed, but was launched by a Ship or by a Planet
        static void CreateMirvWarhead(Weapon warhead, Vector2 origin, Vector2 direction, GameObject target,
                                      bool playSound, Vector2 inheritedVelocity, Empire loyalty, Planet planet)
        {
            // Loyalty cannot be null, otherwise kill events will not work correctly
            if (loyalty == null) throw new NullReferenceException(nameof(loyalty));

            UniverseState universe = (planet?.Universe ?? loyalty.Universe);
            if (universe == null) throw new NullReferenceException(nameof(universe));

            var p = new Projectile(universe.CreateId(), warhead, warhead.Owner, planet, loyalty, GameObjectType.Proj);
            p.Initialize(origin, direction, target, playSound, inheritedVelocity, isMirv: true);
        }

        // new projectile from planet
        public static Projectile Create(Weapon weapon, Planet planet, Empire loyalty, Vector2 direction, GameObject target)
        {
            if (loyalty == null) throw new NullReferenceException(nameof(loyalty));
            if (planet.Universe == null) throw new NullReferenceException(nameof(planet.Universe));

            var p = new Projectile(planet.Universe.CreateId(), weapon, null, planet, loyalty, GameObjectType.Proj)
            {
                ZPos  = 2500f, // +Z: deep in background, away from camera
            };
            p.Initialize(weapon.Origin, direction, target, playSound: true, Vector2.Zero);
            return p;
        }

        // loading from savegame
        [StarDataDeserialized]
        public virtual void OnDeserialized(UniverseState us)
        {
            Universe = us;
            if (!GetWeapon(us, Owner, Planet, WeaponUID, false, out Weapon))
            {
                Log.Error($"Projectile.Weapon not found UID={WeaponUID} Owner={Owner} Planet={Planet}");
                return; // this owner or weapon no longer exists
            }

            float savedDuration = Duration;
            Initialize(Position, Velocity, null, playSound: false, Vector2.Zero);
            Duration = savedDuration; // apply duration from save data
        }

        protected static bool GetWeapon(UniverseState us, Ship ship, Planet planet, 
                                        string weaponUID, bool isBeam, out Weapon weapon)
        {
            weapon = null;
            if (ship == null && planet == null)
                return false;

            if (ship != null)
            {
                // TODO: this is a buggy fallback because it always returns the first Weapon match,
                // TODO: leading to incorrect weapon reference
                // TODO: however, this is really hard to fix, because we don't save Weapon instances
                if (weaponUID.NotEmpty())
                    weapon = ship.Weapons.Find(w => w.UID == weaponUID);
            }
            else
            {
                Building building = planet.FindBuilding(b => b.Weapon == weaponUID);
                weapon = building?.TheWeapon;
            }

            // fallback, the owner has died, or this is a Mirv warhead (owner is a projectile)
            if (weapon == null && !isBeam)
            {
                // This can fail if `weaponUID` no longer exists in game data
                // in which case we abandon this projectile
                if (ResourceManager.GetWeaponTemplate(weaponUID, out IWeaponTemplate t))
                {
                    weapon = new(us, t, ship, null, null);
                }
            }

            return weapon != null;
        }

        void Initialize(Vector2 origin, Vector2 direction, GameObject target, bool playSound, Vector2 inheritedVelocity, bool isMirv = false)
        {
            Position = origin;
            Emitter.Position = new Vector3(origin, 0f);

            Range                 = Weapon.BaseRange;
            Radius                = Weapon.ProjectileRadius;
            Explodes              = Weapon.Explodes;
            FakeExplode           = Weapon.FakeExplode;
            DamageAmount          = Weapon.GetDamageWithBonuses(Owner);
            DamageRadius          = Weapon.ExplosionRadius;
            ExplosionRadiusMod    = Weapon.ExplosionRadiusVisual;
            Health                = Weapon.HitPoints * GlobalStats.Defaults.ProjectileHitpointsMultiplier;
            Speed                 = Weapon.ProjectileSpeed;
            TrailOffset           = Weapon.TrailOffset;
            WeaponType            = Weapon.WeaponType;
            RotationRadsPerSecond = Weapon.RotationRadsPerSecond;
            ArmorPiercing         = Weapon.ArmorPen;
            TrailTurnedOn         = !Weapon.Tag_Guided || Weapon.DelayedIgnition.AlmostZero();

            Weapon.ApplyDamageModifiers(this); // apply all buffs before initializing
            if (Weapon.RangeVariance)
                Range *= Owner.Loyalty.Random.Float(0.9f, 1.1f);

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

            ModelPath = Weapon.ModelPath;
            UsesVisibleMesh = Weapon.UseVisibleMesh || WeaponType is "Missile" or "Drone" or "Rocket";

            if (Owner != null)
            {
                SetSystem(Owner.System);
            }
            else if (Planet != null)
            {
                SetSystem(Planet.System);
            }

            bool inFrustum = Universe.Screen != null && IsInFrustum(Universe.Screen);
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

        void LoadTextureAndEffects()
        {
            if (StarDriveGame.Instance == null)
                return; // allow spawning invisible projectiles inside Unit Tests

            if (Weapon.Animated == 1)
            {
                string animFolder = "Textures/" + Path.GetDirectoryName(Weapon.AnimationPath);
                Animation = new SpriteAnimation(ResourceManager.RootContent, animFolder);
                Animation.Looping = Weapon.LoopAnimation == 1;
                float loopDuration = (InitialDuration / Animation.NumFrames);
                float startAt = Animation.Looping ? Loyalty.Random.Float(0f, loopDuration) : 0f;
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
            Vector2 newDirection = empire.Random.Direction2D();
            float momentumLoss = 9 - empire.Random.RollDie(6);
            Duration *= momentumLoss / 10;
            Speed    *= momentumLoss / 10;
            SetInitialVelocity(Speed * newDirection);

            if (InFrustum && empire.Random.RollDie(2) == 2)
            {
                var foregroundPos = new Vector3(deflectionPoint.X, deflectionPoint.Y, deflectionPoint.Z - 50f);
                Universe.Screen.Particles.BeamFlash.AddParticle(foregroundPos, Vector3.Zero);

                if (Universe.DebugMode == DebugModes.Targeting)
                {
                    Universe.DebugWin?.DrawText(DebugModes.Targeting,
                        Position, "deflected", Color.Red, lifeTime:1f);
                }
            }
        }

        public void CreateMirv(GameObject target)
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
                Weapon warhead = ResourceManager.CreateWeapon(Universe, Weapon.MirvWeapon, Owner, Module, null);
                if (warhead.Tag_Guided)
                {
                    for (int i = 0; i < warhead.ProjectileCount; i++)
                    {
                        // Use separation velocity for mirv non guided, or just Velocity for guided (they will compensate)
                        Vector2 separationVector = Velocity;
                        float launchDir = i % 2 == 0 ? -RadMath.Deg90AsRads : RadMath.Deg90AsRads;
                        Vector2 separationVel = (Rotation + launchDir).RadiansToDirection() 
                                                * (100 + Loyalty.Random.RollDie(4*warhead.FireDispersionArc));

                        separationVector += separationVel; // Add it to the initial velocity
                        bool playSound = i == 0; // play sound once
                        CreateMirvWarhead(warhead, Position, Direction, target, playSound, separationVector, Loyalty, Planet);
                    }
                }
                else
                {
                    // use normal fire arc since the new warhead is not a missile
                    warhead.SpawnMirvSalvo(Direction, target, Position);
                }
            }

            Die(null, false);
        }

        public override Vector2 JammingError()
        {
            Vector2 jitter = Vector2.Zero;
            if (!Weapon.Tag_Intercept) return jitter;

            if (MissileAI != null && Loyalty?.data.MissileDodgeChance > 0)
            {
                jitter += Loyalty.Random.Vector2D(Loyalty.data.MissileDodgeChance * 80f);
            }
            if (Weapon?.Module?.WeaponECM > 0)
                jitter += Weapon.Module.Random.Vector2D(Weapon.Module.WeaponECM * 80f);

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
                if (Animation == null && ProjectileTexture == null)
                    LoadTextureAndEffects();

                if (Animation != null)
                {
                    screen.ProjectToScreenCoords(Position, ZPos, 20f * Weapon.ProjectileRadius * Weapon.Scale,
                                                 out Vector2d pos, out double size);

                    Animation.Draw(batch, pos, new Vector2d(size), Rotation, 1f);
                }
                else if (ResourceManager.ProjectileMesh(ModelPath, out StaticMesh projMesh))
                {
                    DrawMesh(screen, projMesh, WorldMatrix, ProjectileTexture.Texture, Weapon.Scale*50f);
                }
            }

            if (MissileAI != null && MissileAI.Jammed)
            {
                screen.DrawStringProjected(Position + new Vector2(16), 50f, Color.Red, "Jammed");
            }

            if (Universe.DebugMode == DebugModes.Targeting)
            {
                screen.DrawCircleProjectedZ(Position, Radius, Color.LightCyan, ZPos);
            }
        }

        public static void DrawMesh(GameScreen screen, StaticMesh mesh, in Matrix world, Texture2D texture, float scale)
        {
            BasicEffect effect = mesh.GetFirstEffect<BasicEffect>();
            effect.World = Matrix.CreateScale(scale) * world;
            effect.View = screen.View;
            effect.Projection = screen.Projection;
            effect.DiffuseColor = Vector3.One;
            effect.Texture = texture;
            effect.TextureEnabled = true;
            effect.LightingEnabled = false;
            mesh.Draw(effect);
        }

        public void DamageMissile(GameObject source, float damageAmount)
        {
            //if (Health < 0.001f)
            //    Log.Info($"Projectile had no health {Weapon.Name}");
            Health -= damageAmount;
            if (Health <= 0f && Active)
                DieNextFrame = true;
        }
        
        // cleanupOnly: just delete the projectile without showing visual death effects
        public override void Die(GameObject source, bool cleanupOnly)
        {
            if (!Active)
            {
                Log.Error("Projectile.Die() was called on an already dead object");
                return;
            }

            base.Die(source, cleanupOnly);

            if (Light != null)
                Universe.Screen.RemoveLight(Light, dynamic:true);

            if (InFlightSfx.IsPlaying)
                InFlightSfx.Stop();

            // the projectile will explode without hitting anything
            ExplodeProjectile(cleanupOnly, null);

            if (ProjSO != null)
            {
                Universe.Screen.RemoveObject(ProjSO);
            }
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
                Vector2 acc = default;
                GetThrustAcceleration(ref acc, a);
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
                Log.Error($"Projectile.Update() called when dead! Duration={Duration} DieNextFrame={DieNextFrame}");
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

                if (Animation == null && ProjectileTexture == null)
                    LoadTextureAndEffects();

                Animation?.Update(timeStep.FixedTime);

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
                    Light.World = Matrix.CreateTranslation((Vector3)Light.Position);
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
                if (ResourceManager.ProjectileMesh(ModelPath, out StaticMesh mesh))
                {
                    ProjSO = mesh.CreateSceneObject();
                    if (ProjSO != null)
                    {
                        ProjSO.World = WorldMatrix;
                        Universe.Screen.AddObject(ProjSO);
                    }
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

        public void RemoveSceneObject()
        {
            if (ProjSO != null)
            {
                Universe.Screen.RemoveObject(ProjSO);
                ProjSO = null;
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

            if (Universe.DebugMode == DebugModes.Particles)
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

        // cleanupOnly: just delete the projectile without showing visual death effects
        void ExplodeProjectile(bool cleanupOnly, ShipModule victim)
        {
            bool explodes = Explodes;
            if (explodes || FakeExplode)
            {
                bool visibleToPlayer = InFrustum && Module?.GetParent().InPlayerSensorRange == true;
                bool showFx = !cleanupOnly && visibleToPlayer && Universe.IsSectorViewOrCloser;
                bool flashFx = showFx && FlashExplode && Universe.IsSystemViewOrCloser;

                if (explodes)
                {
                    if (Weapon.OrdinanceRequiredToFire > 0f && Owner != null)
                    {
                        DamageRadius += Owner.Loyalty.data.OrdnanceEffectivenessBonus * DamageRadius;
                    }

                    if (showFx)
                        ShowExplosionEffect(flashFx, victim);

                    // the most typical case: projectile has hit a victim module and will now explode
                    if (victim != null)
                        Universe.Spatial.ProjectileExplode(this, victim);
                }
                else if (showFx) // FakeExplode
                {
                    ShowExplosionEffect(flashFx, victim);
                }
            }
        }

        void ShowExplosionEffect(bool flashFx, ShipModule module)
        {
            var origin = new Vector3(Position, -50f);
            float radius = DamageRadius * ExplosionRadiusMod;
            Vector2 explostionVelocity = module?.GetParent()?.Velocity * 1.1f ?? Velocity * 0.1f;
            ExplosionManager.AddExplosion(Universe.Screen, origin, explostionVelocity, radius, 2.5f, Weapon.ExplosionType);
            if (flashFx)
            {
                GameAudio.PlaySfxAsync(DieCueName, Emitter);
                Universe.Screen.Particles.Flash.AddParticle(origin, Vector3.Zero);
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

                float maxRotation = timeStep.FixedTime * rotationRadsPerSec;
                float rotationChange = rotationDir * Math.Min(angleDiff, maxRotation);
                Rotation = (Rotation + rotationChange).AsNormalizedRadians();
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

        public bool Touch(GameObject target, Vector2 hitPos)
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
                    if (InFrustum)
                        CreateWeaponDeathEffect(target);
                    break;
            }

            DieNextFrame = !Deflected;
            return true;
        }

        void CreateWeaponDeathEffect(GameObject target)
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

        // @return TRUE if all Damage was absorbed by victim
        bool Damage(ShipModule victim)
        {
            victim.DebugDamageCircle();
            if (Explodes)
            {
                ExplodeProjectile(false, victim);
                Explodes = false;
                FakeExplode = false;
                return true;
            }

            // for non-explosives try to kill the module
            victim.Damage(this, DamageAmount, out DamageAmount);
            // are we out of juice?
            return DamageAmount <= 0f;
        }

        // For AP Projectiles, can we phase through this victim module?
        bool TryPhaseThroughModule(ShipModule victim)
        {
            // apply resistances to AP phasing
            ArmorPiercing -= (victim.APResist + victim.XSize);
            // if AP was 1 and victim was 1x1 armor, then AP=1-1=0,
            // so we should phase through it
            bool phaseThrough = ArmorPiercing >= 0 && victim.Is(ShipModuleType.Armor);

            if (phaseThrough && Universe.DebugMode == DebugModes.Targeting)
            {
                Universe.DebugWin?.DrawText(DebugModes.Targeting,
                    Position, "phased", Color.IndianRed, lifeTime:1f);
            }
            return phaseThrough;
        }

        // if the projectile is strong enough, keep piercing through modules
        // otherwise just explode and die
        void ArmorPiercingTouch(ShipModule victim, Ship parent, Vector2 hitPos)
        {
            // for visual consistency we want to show Projectile at hitPos
            // if the radius is big - adjust the pos accordingly, since if the explosion radius is smaller than
            // the projectile radius - it will explode with no affect
            Position = Radius <= 8 ? hitPos : hitPos + hitPos.DirectionToTarget(victim.Position)*Radius;
            Universe.DebugWin?.DrawGameObject(DebugModes.Targeting, this, Color.LightCyan, lifeTime:0.25f);

            if (!TryPhaseThroughModule(victim) && Damage(victim))
                return; // all damage absorbed

            // create an enumeration object which will step through the module grid one by one
            IEnumerable<ShipModule> walk = parent.RayHitTestWalkModules(
                hitPos, VelocityDirection, parent.Radius, IgnoresShields);

            foreach (ShipModule nextModule in walk)
            {
                if (!TryPhaseThroughModule(nextModule) && Damage(nextModule))
                    return; // all damage absorbed
                // either phase to next module, or continue crushing through modules
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

        public override string ToString() => $"Proj[{WeaponType}]:{Id} Wep={Weapon?.Name} Pos={Position} Rad={Radius} Loy=[{Loyalty}]";

        public void IgniteEngine()
        {
            TrailTurnedOn = true;
        }
    }
}