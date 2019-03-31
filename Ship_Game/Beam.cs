using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Xml.Serialization;

namespace Ship_Game
{
    public class Beam : Projectile
    {
        public float PowerCost;
        public Vector2 Source;
        public Vector2 Destination;
        public Vector2 ActualHitDestination; // actual location where beam hits another ship
        public int Thickness { get; private set; }
        public static Effect BeamEffect;
        public VertexPositionNormalTexture[] Vertices = new VertexPositionNormalTexture[4];
        public int[] Indexes = new int[6];
        private readonly float BeamZ = RandomMath2.RandomBetween(-1f, 1f);
        public bool Infinite;
        private VertexDeclaration QuadVertexDecl;
        private float Displacement = 1f;
        [XmlIgnore][JsonIgnore] public bool BeamCollidedThisFrame;
        [XmlIgnore][JsonIgnore] public GameplayObject Target { get; }

        // Create a beam with an initial destination position that optionally follows GameplayObject [target]
        public Beam(Weapon weapon, Vector2 source, Vector2 destination, GameplayObject target = null) : base(GameObjectType.Beam)
        {
            // there is an error here in beam creation where the weapon has no module.
            // i am setting these values in the weapon CreateDroneBeam where possible.
            Weapon                  = weapon;
            Target                  = target;
            Module                  = weapon.Module;
            DamageAmount            = weapon.GetDamageWithBonuses(weapon.Owner);
            PowerCost               = weapon.BeamPowerCostPerSecond;
            Range                   = weapon.Range;
            Duration                = weapon.BeamDuration;
            Thickness               = weapon.BeamThickness;
            WeaponEffectType        = weapon.WeaponEffectType;
            WeaponType              = weapon.WeaponType;
            // for repair weapons, we ignore all collisions
            DisableSpatialCollision = DamageAmount < 0f;
            Emitter.Position        = new Vector3(source, 0f);
            Owner                   = weapon.Owner;
            Source                  = source;
            Destination             = destination;
            ActualHitDestination = Destination;
            BeamCollidedThisFrame = true; //

            Initialize();
            weapon.ModifyProjectile(this);

            if (Owner != null && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView && Owner.InFrustum)
            {
                weapon.PlayToggleAndFireSfx(Emitter);
            }
        }

        // Create a spatially fixed beam spawned from a ship center
        // Used by DIMENSIONAL PRISON
        public Beam(Ship ship, Vector2 destination, int thickness) : base(GameObjectType.Beam)
        {
            Owner       = ship;
            Source      = ship.Center;
            Destination = destination;
            Thickness   = thickness;

            Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();
            if (Owner != null)
            {
                Loyalty = Owner?.loyalty ?? DroneAI?.Drone?.Loyalty; // set loyalty before adding to spatial manager
                SetSystem(Owner?.System ?? DroneAI?.Drone?.System);
            }
            InitBeamMeshIndices();
            UpdateBeamMesh();

            QuadVertexDecl = new VertexDeclaration(Empire.Universe.ScreenManager.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
        }

        public override void Die(GameplayObject source, bool cleanupOnly)
        {
            Weapon.ResetToggleSound();
            base.Die(source, cleanupOnly);
        }

        public void Draw(GameScreen screen)
        {
            if (!Source.InRadius(ActualHitDestination, Range + 10.0f))
                return;

            GraphicsDevice device = screen.Device;
            lock (GlobalStats.BeamEffectLocker)
            {
                Empire.Universe.beamflashes.AddParticleThreadA(new Vector3(Source, BeamZ), Vector3.Zero);

                var hit = new Vector3(ActualHitDestination, BeamZ);
                if (BeamCollidedThisFrame) // a cool hit effect
                {
                    Empire.Universe.sparks.AddParticleThreadA(hit, new Vector3(50f));
                    Empire.Universe.fireTrailParticles.AddParticleThreadA(hit, new Vector3(50f));
                    Empire.Universe.fireTrailParticles.AddParticleThreadA(hit, Vector3.Zero);
                }
                else // dispersion effect
                {
                    Empire.Universe.sparks.AddParticleThreadA(hit, Vector3.Zero);
                    Empire.Universe.sparks.AddParticleThreadA(hit, Vector3.Zero);
                }

                device.VertexDeclaration = QuadVertexDecl;
                BeamEffect.CurrentTechnique = BeamEffect.Techniques["Technique1"];
                BeamEffect.Parameters["World"].SetValue(Matrix.Identity);
                string beamTexPath = "Beams/" + Weapon.BeamTexture;
                BeamEffect.Parameters["tex"].SetValue(ResourceManager.Texture(beamTexPath).Texture);
                Displacement -= 0.05f;
                if (Displacement < 0f)
                    Displacement = 1f;

                BeamEffect.Parameters["displacement"].SetValue(new Vector2(0f, Displacement));
                BeamEffect.Begin();
                var rs = device.RenderState;
                rs.AlphaTestEnable = true;
                rs.AlphaFunction   = CompareFunction.GreaterEqual;
                rs.ReferenceAlpha  = 200;
                foreach (EffectPass pass in BeamEffect.CurrentTechnique.Passes)
                {
                    pass.Begin();
                    device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Vertices, 0, 4, Indexes, 0, 2);
                    pass.End();
                }
                rs.DepthBufferWriteEnable = false;
                rs.AlphaBlendEnable       = true;
                rs.SourceBlend            = Blend.SourceAlpha;
                rs.DestinationBlend       = Blend.InverseSourceAlpha;
                rs.AlphaTestEnable        = true;
                rs.AlphaFunction          = CompareFunction.Less;
                rs.ReferenceAlpha         = 200;
                foreach (EffectPass pass in BeamEffect.CurrentTechnique.Passes)
                {
                    pass.Begin();
                    device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Vertices, 0, 4, Indexes, 0, 2);
                    pass.End();
                }
                rs.AlphaBlendEnable = false;
                rs.DepthBufferWriteEnable = true;
                rs.AlphaTestEnable = false;
                BeamEffect.End();
            }
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

        public new bool Touch(GameplayObject target)
        {
            if (target == null || target == Owner || target is Ship)
                return false;

            if (target is Projectile projectile)
            {
                if (!Weapon.Tag_PD && !Weapon.TruePD)
                    return false;
                if (projectile.Weapon?.Tag_Intercept != true || projectile.Weapon?.Tag_PD == true)
                    return false;
                if (!Loyalty.IsEmpireAttackable(projectile.Loyalty))
                    return false;
                if (projectile.Loyalty?.data.MissileDodgeChance > UniverseRandom.RandomBetween(0f, 1f))
                    return false;

                projectile.DamageMissile(this, DamageAmount);
                return true;
            }

            var targetModule = target as ShipModule;
            if (DamageAmount < 0f && targetModule?.ShieldsAreActive == true) // @todo Repair beam??
                return false;

            targetModule?.Damage(this, DamageAmount);
            return true;
        }

        public override void Update(float elapsedTime)
        {
            if (Module == null)
            {
                Die(null, false);
                return;
            }

            Vector2 slotForward  = (Owner.Rotation + Module.Rotation.ToRadians()).RadiansToDirection();
            Vector2 muzzleOrigin = Module.Center + slotForward * (Module.YSIZE * 8f);

            // @todo Varying beam width
            //int thickness = (int)UniverseRandom.RandomBetween(Thickness*0.75f, Thickness*1.1f);


            Owner.PowerCurrent -= PowerCost * elapsedTime;
            if (Owner.PowerCurrent < 0f)
            {
                Owner.PowerCurrent = 0f;
                Die(null, false);
                Duration = 0f;
                return;
            }

            var ship = (Target as Ship) ?? (Target as ShipModule)?.GetParent();

            if (Owner.engineState == Ship.MoveState.Warp || ship != null && ship.engineState == Ship.MoveState.Warp)
            {
                Die(null, false);
                Duration = 0f;
                return;
            }

            Duration -= elapsedTime;
            Source = muzzleOrigin;

            // always update Destination to ensure beam stays in range
            Vector2 newDestination = (Target?.Center ?? Destination);

            // old destination adjusted to same distance as newDestination,
            // so we get two equal length lines \/
            Vector2 oldAdjusted = Source.OffsetTowards(Destination, Source.Distance(newDestination));
            float sweepSpeed = Module.WeaponRotationSpeed * 48f * elapsedTime;
            Vector2 newPosition = oldAdjusted.OffsetTowards(newDestination, sweepSpeed);

            if (Owner.IsInsideFiringArc(Weapon, newPosition))
            {
                Destination = Source.OffsetTowards(newPosition, Range);
            }

            if (!BeamCollidedThisFrame)
                ActualHitDestination = Destination;
            else
                BeamCollidedThisFrame = false;

            UpdateBeamMesh();
            if (Duration < 0f && !Infinite)
            {
                Die(null, true);
            }
        }

        protected override void Dispose(bool disposing)
        {
            QuadVertexDecl?.Dispose(ref QuadVertexDecl);
            base.Dispose(disposing);
        }

        public override string ToString() => $"Beam[{WeaponType}] Wep={Weapon?.Name} Src={Source} Dst={Destination} Loy=[{Loyalty}]";

        public void CreateHitParticles(float centerAxisZ)
        {
            if (!HasParticleHitEffect(10f)) return;
            Vector2 impactNormal = (Source - Center).Normalized();
            var pos = ActualHitDestination.ToVec3(centerAxisZ);
            Empire.Universe.flash.AddParticleThreadB(pos, Vector3.Zero);
            for (int i = 0; i < 20; i++)
            {
                var vel = new Vector3(impactNormal * RandomMath.RandomBetween(40f, 80f), RandomMath.RandomBetween(-25f, 25f));
                Empire.Universe.sparks.AddParticleThreadB(pos, vel);
            }
        }

        private static bool HasParticleHitEffect(float chance) => RandomMath.RandomBetween(0f, 100f) <= chance;
    }
    public sealed class DroneBeam : Beam
    {
        readonly DroneAI Drone;
        public DroneBeam(DroneAI drone) :
            base(drone.DroneWeapon, drone.Drone.Center, drone.DroneTarget.Center, drone.DroneTarget)
        {
            Drone = drone;
            Owner = drone.Drone.Owner;
        }

        public override void Update(float elapsedTime)
        {
            Duration -= elapsedTime;
            Source = Drone.Drone.Center;
            ActualHitDestination = Drone.DroneTarget.Center;
            // apply drone repair effect, 5 times more if not in combat
            if (DamageAmount < 0f && Source.InRadius(Destination, Range + 10f) && Target is Ship targetShip)
            {
                float repairMultiplier = targetShip.InCombat ? 1 : 5;
                targetShip.ApplyRepairOnce(-DamageAmount * repairMultiplier * elapsedTime, Owner?.Level ?? 0);
            }

            UpdateBeamMesh();
            if (Duration < 0f && !Infinite)
                Die(null, true);
        }
        public override void Die(GameplayObject source, bool cleanupOnly)
        {
            //im confused on this setsystem. why are we doing this?
            //pretty sure its going to be set to null later int he die process.
            SetSystem(Drone.Drone.Owner.System);
            base.Die(source, cleanupOnly);
        }
    }
}