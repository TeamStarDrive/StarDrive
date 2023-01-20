using System;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using SDGraphics;
using Ship_Game.Data.Serialization;
using Ship_Game.Graphics;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using SDUtils;

namespace Ship_Game
{
    [StarDataType]
    public class Beam : Projectile
    {
        [StarData] public Vector2 Source;
        [StarData] public Vector2 Destination;
        [StarData] public Vector2 ActualHitDestination; // actual location where beam hits another ship
        [StarData] public Ship TargetShip { get; private set; } // setter used by serializer
        [StarData] int TargetModIdx; // TODO: can be removed if we support ShipModule serialization

        public GameObject Target
        {
            get
            {
                if (TargetShip == null) return null;
                if (TargetModIdx == 0) return TargetShip;
                if (TargetShip.Modules.Length == 0) return null;
                return TargetShip.Modules[TargetModIdx - 1];
            }
        }

        public ShipModule TargetModule
        {
            get
            {
                if (TargetShip == null || TargetModIdx == 0)
                    return null;

                var modules = TargetShip.Modules;
                if (modules.Length == 0) // when a ship dies, its modules are set to Empty Array
                    return null;
                if (TargetModIdx > modules.Length) // wtf? but somehow this can happen
                    return null;
                return modules[TargetModIdx - 1];
            }
            set
            {
                if (value != null && TargetShip != null)
                    TargetModIdx = TargetShip.Modules.IndexOf(value) + 1;
                else
                    TargetModIdx = 0;
            }
        }

        // for spatial manager:
        public int RadiusX;
        public int RadiusY;

        public int Thickness { get; private set; }
        public float PowerCost;
        public static Effect BeamEffect;
        public VertexPositionNormalTexture[] Vertices = new VertexPositionNormalTexture[4];
        public int[] Indexes = new int[6];
        readonly float BeamZ = RandomMath2.Float(-1f, 1f);
        public bool Infinite;
        VertexDeclaration QuadVertexDecl;
        float Displacement = 1f;
        public bool BeamCollidedThisFrame;

        [StarDataConstructor] protected Beam() {}

        // Create a beam with an initial destination position that optionally follows GameplayObject [target]
        public Beam(int id, Weapon weapon, Vector2 source, Vector2 destination, GameObject target = null)
            : base(id, weapon, weapon.Owner, null, weapon.Owner.Loyalty, GameObjectType.Beam)
        {
            var targetModule = (target as ShipModule);
            TargetShip = targetModule?.GetParent() ?? target as Ship;
            if (targetModule != null && TargetShip != null)
                TargetModIdx = TargetShip.Modules.IndexOf(targetModule) + 1;

            BeamInit(source, destination);
        }

        // Create a spatially fixed beam spawned from a ship center
        // Used by DIMENSIONAL PRISON
        public Beam(int id, Weapon weapon, Ship ship, Vector2 destination, int thickness)
            : base(id, weapon, ship, null, ship.Loyalty, GameObjectType.Beam)
        {
            Source = ship.Position;
            Destination = destination;
            Thickness = thickness;
        }

        // loading from savegame
        [StarDataDeserialized]
        public override void OnDeserialized(UniverseState us)
        {
            Universe = us;
            if (!GetWeapon(Owner, Planet, WeaponUID, false, out Weapon))
            {
                Log.Error($"Beam.Weapon not found UID={WeaponUID} Owner={Owner} Planet={Planet}");
                return; // this owner or weapon no longer exists
            }

            float savedDuration = Duration;
            BeamInit(Source, Destination);
            Duration = savedDuration;
        }

        void BeamInit(Vector2 source, Vector2 destination)
        {
            Module = Weapon.Module;
            DamageAmount = Weapon.GetDamageWithBonuses(Owner);
            PowerCost  = Weapon.BeamPowerCostPerSecond;
            Range      = Weapon.BaseRange;
            Duration   = Weapon.BeamDuration;
            Thickness  = Weapon.BeamThickness;
            WeaponType = Weapon.WeaponType;
            // for repair weapons, we ignore all collisions
            DisableSpatialCollision = DamageAmount < 0f;
            Source  = source;

            Destination = destination;
            SetActualHitDestination(Destination);
            BeamCollidedThisFrame = true; 

            Weapon.ApplyDamageModifiers(this);

            if (Owner != null
                && Owner.InFrustum
                && Owner.Universe.IsSystemViewOrCloser
                && (Owner.InPlayerSensorRange || TargetShip?.InPlayerSensorRange == true))
            {
                Emitter.Position = new Vector3(source, 0f);
                Weapon.PlayToggleAndFireSfx(Emitter);
            }
        }

        void InitMesh()
        {
            InitBeamMeshIndices();
            UpdateBeamMesh();
            UpdatePosition();
            QuadVertexDecl = new(GameBase.Base.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
        }

        // cleanupOnly: just delete the projectile without showing visual death effects
        public override void Die(GameObject source, bool cleanupOnly)
        {
            Weapon?.ResetToggleSound();
            base.Die(source, cleanupOnly);
        }

        public static void UpdateBeamEffect(UniverseScreen u)
        {
            BeamEffect.Parameters["View"].SetValue(u.View);
            BeamEffect.Parameters["Projection"].SetValue(u.Projection);
        }

        public void Draw(UniverseScreen u)
        {
            if (!Source.InRadius(ActualHitDestination, Range + 10.0f))
                return;

            GraphicsDevice device = u.Device;
            u.Particles.BeamFlash.AddParticle(new Vector3(Source, BeamZ), Vector3.Zero);

            var hit = new Vector3(ActualHitDestination, BeamZ);
            if (BeamCollidedThisFrame) // a cool hit effect
            {
                u.Particles.Sparks.AddParticle(hit, new Vector3(20f));
                u.Particles.SmallFire.AddParticle(hit, new Vector3(10f));
                u.Particles.SmallFire.AddParticle(hit, Vector3.Zero);
            }
            else // dispersion effect
            {
                u.Particles.Lightning.AddParticle(hit, Vector3.Zero);
                u.Particles.Lightning.AddParticle(hit, Vector3.Zero);
            }

            if (QuadVertexDecl == null)
                InitMesh();

            device.VertexDeclaration = QuadVertexDecl;
            BeamEffect.CurrentTechnique = BeamEffect.Techniques["Technique1"];
            BeamEffect.Parameters["World"].SetValue(Matrix.XnaIdentity);
            string beamTexPath = "Beams/" + Weapon.BeamTexture;
            BeamEffect.Parameters["tex"].SetValue(ResourceManager.Texture(beamTexPath).Texture);
            Displacement -= 0.05f;
            if (Displacement < 0f)
                Displacement = 1f;

            BeamEffect.Parameters["displacement"].SetValue(new Vector2(0f, Displacement));
            BeamEffect.Begin();

            RenderStates.EnableAlphaTest(device, CompareFunction.GreaterEqual, 200);

            foreach (EffectPass pass in BeamEffect.CurrentTechnique.Passes)
            {
                pass.Begin();
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Vertices, 0, 4, Indexes, 0, 2);
                pass.End();
            }

            RenderStates.DisableDepthWrite(device);
            RenderStates.EnableAlphaBlend(device, Blend.SourceAlpha, Blend.InverseSourceAlpha);
            RenderStates.EnableAlphaTest(device, CompareFunction.Less, 200);

            foreach (EffectPass pass in BeamEffect.CurrentTechnique.Passes)
            {
                pass.Begin();
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Vertices, 0, 4, Indexes, 0, 2);
                pass.End();
            }
            
            RenderStates.DisableAlphaTest(device);
            RenderStates.EnableClassicAlphaBlend(device); // restore default
            RenderStates.EnableDepthWrite(device);

            BeamEffect.End();
        }

        private void InitBeamMeshIndices()
        {
            Vertices[0].TextureCoordinate = new Vector2(0f, 1f);
            Vertices[1].TextureCoordinate = new Vector2(0f, 0f);
            Vertices[2].TextureCoordinate = new Vector2(1f, 1f);
            Vertices[3].TextureCoordinate = new Vector2(1f, 0f);
            Indexes[0] = 0;
            Indexes[1] = 1;
            Indexes[2] = 2;
            Indexes[3] = 2;
            Indexes[4] = 1;
            Indexes[5] = 3;
        }

        protected void UpdateBeamMesh()
        {
            Vector2 src = Source;
            Vector2 dst = ActualHitDestination;
            Vector2 deltaVec = dst - src;
            Vector2 right = new Vector2(deltaVec.Y, -deltaVec.X).Normalized();

            // typical zigzag pattern:  |\|
            Vertices[0].Position = new Vector3(dst - (right * Thickness), BeamZ); // botleft
            Vertices[1].Position = new Vector3(src - (right * Thickness), BeamZ); // topleft
            Vertices[2].Position = new Vector3(dst + (right * Thickness), BeamZ); // botright
            Vertices[3].Position = new Vector3(src + (right * Thickness), BeamZ); // topright
        }

        public bool Touch(FixedSimTime timeStep,GameObject target)
        {
            if (target == null || target == Owner || target is Ship)
                return false;

            // FB - maintain beam damage at different game speeds
            float damageModifier = timeStep.FixedTime * 60;
            if (target is Projectile projectile)
            {
                if (!Weapon.Tag_PD && !Weapon.TruePD)
                    return false;
                if (projectile.Weapon?.Tag_Intercept != true || projectile.Weapon?.Tag_PD == true)
                    return false;
                if (!Loyalty.IsEmpireAttackable(projectile.Loyalty))
                    return false;
                if (projectile.Loyalty?.data.MissileDodgeChance > UniverseRandom.Float(0f, 1f))
                    return false;

                projectile.DamageMissile(this, DamageAmount * damageModifier);
                return true;
            }

            var targetModule = target as ShipModule;
            if (DamageAmount < 0f && targetModule?.ShieldsAreActive == true) // @todo Repair beam??
                return false;

            targetModule?.Damage(this, DamageAmount * damageModifier, damageModifier);
            return true;
        }

        // This is a bugfix for broken Hit Destinations
        public void SetActualHitDestination(Vector2 hit)
        {
            Vector2 delta = hit - Source;
            float distance = delta.Length();
            if (distance > (Range+10)) // if distance is ridiculous, normalize it to something meaningful
            {
                ActualHitDestination = Source + delta.Normalized() * Range;
            }
            else
            {
                ActualHitDestination = hit;
            }
        }

        void UpdatePosition()
        {
            Vector2 source = Source;
            Vector2 target = ActualHitDestination;
            int x1 = (int)Math.Min(source.X, target.X);
            int y1 = (int)Math.Min(source.Y, target.Y);
            int x2 = (int)Math.Max(source.X, target.X);
            int y2 = (int)Math.Max(source.Y, target.Y);

            // These are used by Spatial management
            Position = new Vector2((x1 + x2) >> 1,
                                   (y1 + y2) >> 1);
            RadiusX = (x2 - x1) >> 1;
            RadiusY = (y2 - y1) >> 1;
        }

        public override void Update(FixedSimTime timeStep)
        {
            if (Module == null)
            {
                Die(null, false);
                return;
            }

            Vector2 slotForward  = (Owner.Rotation + Module.Rotation.ToRadians()).RadiansToDirection();
            Vector2 muzzleOrigin = Module.Position + slotForward * (Module.YSize * 8f);

            // @todo Varying beam width
            //int thickness = (int)UniverseRandom.RandomBetween(Thickness*0.75f, Thickness*1.1f);


            Owner.PowerCurrent -= PowerCost * timeStep.FixedTime;
            if (Owner.PowerCurrent < 0f)
            {
                Owner.PowerCurrent = 0f;
                Die(null, false);
                Duration = 0f;
                return;
            }

            if (Owner.engineState == Ship.MoveState.Warp || TargetShip?.engineState == Ship.MoveState.Warp)
            {
                Die(null, false);
                Duration = 0f;
                return;
            }

            Duration -= timeStep.FixedTime;
            Source = muzzleOrigin;

            // always update Destination to ensure beam stays in range
            Vector2 newDestination = (Target?.Position ?? Destination);

            // old destination adjusted to same distance as newDestination,
            // so we get two equal length lines \/
            Vector2 oldAdjusted = Source.OffsetTowards(Destination, Source.Distance(newDestination));
            float sweepSpeed    = Module.WeaponRotationSpeed * 48 * timeStep.FixedTime;
            Vector2 newPosition = oldAdjusted.OffsetTowards(newDestination, sweepSpeed);

            if (Owner.IsInsideFiringArc(Weapon, newPosition))
            {
                Destination = Source.OffsetTowards(newPosition, Range);
            }

            // only RESET ActualHitDestination if game is unpaused
            if (timeStep.FixedTime > 0f)
            {
                if (!BeamCollidedThisFrame)
                    ActualHitDestination = Destination; // will be validated below
                else
                    BeamCollidedThisFrame = false;
            }

            SetActualHitDestination(ActualHitDestination); // validate hit destination
            UpdatePosition();
            UpdateBeamMesh();
            
            if (Duration < 0f && !Infinite)
            {
                Die(null, true);
            }
        }

        protected override void Dispose(bool disposing)
        {
            Mem.Dispose(ref QuadVertexDecl);
            base.Dispose(disposing);
        }

        public override string ToString() => $"Beam[{WeaponType}] Wep={Weapon?.Name} Src={Source} Dst={Destination} Loy=[{Loyalty}]";

        public void CreateBeamHitParticles(float centerAxisZ, bool damagingShields)
        {
            var impactNormal = new Vector3(ActualHitDestination.DirectionToTarget(Source), 1f);
            var pos = ActualHitDestination.ToVec3(centerAxisZ);

            if (HitEffect == null)
                HitEffect = CreateHitEffect(damagingShields, pos);

            // if effect was created successfully
            HitEffect?.Update(pos, impactNormal, 0.1f);
        }
    }

    [StarDataType]
    public sealed class DroneBeam : Beam
    {
        [StarData] readonly DroneAI AI;

        [StarDataConstructor] DroneBeam() {}

        [StarDataDeserialized]
        public override void OnDeserialized(UniverseState us)
        {
            base.OnDeserialized(us);
        }

        public DroneBeam(int id, DroneAI ai)
            : base(id, ai.DroneWeapon, ai.Drone.Position, ai.DroneTarget.Position, ai.DroneTarget)
        {
            AI = ai;
        }

        public override void Update(FixedSimTime timeStep)
        {
            Duration -= timeStep.FixedTime;
            Source = AI.Drone.Position;
            SetActualHitDestination(AI.DroneTarget?.Position ?? Source);

            if (DamageAmount < 0f && Source.InRadius(Destination, Range + 10f))
            {
                ShipModule moduleToRepair = TargetModule;
                if (moduleToRepair != null)
                {
                    bool inCombat = moduleToRepair.GetParent().IsInRecentCombat;
                    float repairModifier = inCombat ? GlobalStats.Defaults.InCombatSelfRepairModifier : 1f;
                    float repairAmount = -DamageAmount * repairModifier * timeStep.FixedTime;

                    moduleToRepair.Repair(repairAmount);
                    if (moduleToRepair.HealthPercent > 0.999f)
                        TargetModule = null;
                }
                else
                {
                    int repairLevel = Owner?.Level ?? 0;
                    TargetModule = TargetShip.GetModuleToRepair(repairLevel);
                }
            }

            UpdateBeamMesh();
            if (Active && Duration < 0f && !Infinite)
            {
                AI.ClearBeam();
                Die(null, true);
            }
        }
    }
}