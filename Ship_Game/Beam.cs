using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class Beam : Projectile
    {
        public float PowerCost;
        public Vector2 Source;
        public Vector2 Destination;
        public Vector2 ActualHitDestination; // actual location where beam hits another ship
        private Vector2 TargetPosistion;
        public int Thickness { get; private set; }
        public static Effect BeamEffect;
        public bool FollowMouse;
        public VertexPositionNormalTexture[] Vertices = new VertexPositionNormalTexture[4];
        public int[] Indexes = new int[6];
        private readonly float BeamZ = RandomMath2.RandomBetween(-1f, 1f);
        public bool Infinite;
        private VertexDeclaration QuadVertexDecl;
        private float Displacement = 1f;
        [XmlIgnore][JsonIgnore] public bool BeamCollidedThisFrame;
        private float JitterRadius;
        private readonly Vector2 Jitter;
        private Vector2 WanderPath = Vector2.Zero;
        private AudioHandle DamageToggleSound = default(AudioHandle);

        [XmlIgnore][JsonIgnore]
        public GameplayObject Target { get; }

        // Create a beam with an initial destination position that optionally follows GameplayObject [target]
        public Beam(Weapon weapon, Vector2 source, Vector2 destination, GameplayObject target = null, bool followMouse = false) : base(GameObjectType.Beam)
        {
            //there is an error here in beam creation where the weapon has no module. 
            // i am setting these values in the weapon CreateDroneBeam where possible. 
            FollowMouse             = followMouse;
            Weapon                  = weapon;
            Target                  = target;
            TargetPosistion         = destination;
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
            Jitter                  = destination;
            JitterRadius            = 0;
            Emitter.Position        = new Vector3(source, 0f);
            Owner                   = weapon.Owner ;
            Source                  = source;
            Destination             = destination;
            //WanderPath = Owner?.Center.RightVector() ?? source.RightVector();
            // if (target != null && destination.OutsideRadius(target.Center, 32))

            TargetPosistion = Target?.Center.NearestPointOnFiniteLine(Source, destination) ?? destination;
            JitterRadius = target?.Center.Distance(Jitter) ?? 0 ;
            if (JitterRadius > 0)
                WanderPath = Vector2.Normalize(destination - target.Center) * 8f;
            if (float.IsNaN(WanderPath.X))
                WanderPath = Vector2.Zero;
            
            ActualHitDestination    = Destination;                        
            Initialize();
            weapon.ModifyProjectile(this);

            if (Owner != null && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView && Owner.InFrustum)
            {
                weapon.PlayToggleAndFireSfx(Emitter);
            }
        }

        // Create a spatially fixed beam spawned from a ship center
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

        private void SetDestination(Vector2 destination, float range =-1)
        {
            range = range < 0 ? Range : range;
            Vector2 deltaVec = destination - Source;            
            if (!DisableSpatialCollision)
            {
                TargetPosistion = Target?.Center.NearestPointOnFiniteLine(Source, destination) ?? destination;
            }
            else
                TargetPosistion = destination;
            Destination = Source + deltaVec.Normalized() * range;
        }

        public override void Die(GameplayObject source, bool cleanupOnly)
        {
            DamageToggleSound.Stop();

            if (Owner != null)
            {
                Owner.RemoveBeam(this);
            }
            else if (Weapon.drowner != null)
            {
                (Weapon.drowner as Projectile)?.DroneAI.Beams.QueuePendingRemoval(this);
                SetSystem(Weapon.drowner.System);
            }
            Weapon.ResetToggleSound();
            base.Die(source, cleanupOnly);
        }

        public void Draw(ScreenManager screenMgr)
        {
            lock (GlobalStats.BeamEffectLocker)
            {
                Empire.Universe.beamflashes.AddParticleThreadA(new Vector3(Source, BeamZ), Vector3.Zero);
                screenMgr.GraphicsDevice.VertexDeclaration = QuadVertexDecl;
                BeamEffect.CurrentTechnique = BeamEffect.Techniques["Technique1"];
                BeamEffect.Parameters["World"].SetValue(Matrix.Identity);
                string beamTexPath = "Beams/" + Weapon.BeamTexture;
                BeamEffect.Parameters["tex"].SetValue(ResourceManager.Texture(beamTexPath));
                Displacement -= 0.05f;
                if (Displacement < 0f)
                {
                    Displacement = 1f;
                }
                BeamEffect.Parameters["displacement"].SetValue(new Vector2(0f, Displacement));
                BeamEffect.Begin();
                var rs = screenMgr.GraphicsDevice.RenderState;
                rs.AlphaTestEnable = true;
                rs.AlphaFunction   = CompareFunction.GreaterEqual;
                rs.ReferenceAlpha  = 200;
                foreach (EffectPass pass in BeamEffect.CurrentTechnique.Passes)
                {
                    pass.Begin();
                    screenMgr.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Vertices, 0, 4, Indexes, 0, 2);
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
                    screenMgr.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Vertices, 0, 4, Indexes, 0, 2);
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

        private void UpdateBeamMesh()
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

            // @todo Why are we always doing this extra work??
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

        public override bool Touch(GameplayObject target)
        {
            if (target == null || target == Owner || target is Ship)
                return false;
            if (target is Projectile projectile)
            {
                if (!Weapon.Tag_PD && !Weapon.TruePD) return false;
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
            if (DamageAmount < 0f && targetModule?.ShieldPower >= 1f) // @todo Repair beam??
                return false;

            targetModule?.Damage(this, DamageAmount);
            return true;
        }

        public void Update(Vector2 srcCenter, int thickness, float elapsedTime)
        {
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
            Source = srcCenter;
            if (ship != null && ship.Active && !DisableSpatialCollision)
            {
                float sweep = ((Module?.WeaponRotationSpeed ?? 1f)) * 4f; //* .25f);                

                if (Destination.OutsideRadius(Target.Center, JitterRadius * .5f))
                    WanderPath = Vector2.Normalize(Target.Center - Destination) * sweep;
                if (float.IsNaN(WanderPath.X))
                    WanderPath = Vector2.Normalize(Target.Center - ActualHitDestination) * sweep;
            }

            if (FollowMouse)
            {
                float sweep = ((Module?.WeaponRotationSpeed ?? 1f)) *
                              16f; //* .25f);                                           
                WanderPath = Vector2.Normalize(Empire.Universe.mouseWorldPos - Destination) * sweep;
            }

            // always update Destination to ensure beam stays in range
            SetDestination( //FollowMouse
                //? Empire.Universe.mouseWorldPos
                DisableSpatialCollision
                    ? Target?.Center ?? Destination
                    : Destination + WanderPath);


            if (!BeamCollidedThisFrame) ActualHitDestination = Destination;

            BeamCollidedThisFrame = false;
           
            if (!Owner.CheckIfInsideFireArc(Weapon, Destination, Owner.Rotation, skipRangeCheck: true))
            {
                if (ship != null)
                    Empire.Universe.DebugWin?.DrawCircle(DebugModes.Targeting, Destination, ship.Radius, Color.Yellow);
                Log.Info("Beam killed because of angle");
                Die(null, true);
                return;
            }

            UpdateBeamMesh();
            if (Duration < 0f && !Infinite)
                Die(null, true);
        }

        public void UpdateDroneBeam(Vector2 srcCenter, Vector2 dstCenter, int thickness, float elapsedTime)
        {
            Duration -= elapsedTime;
            Thickness = thickness;
            Source    = srcCenter;
            ActualHitDestination = dstCenter;
            // apply drone repair effect, 5 times more if not in combat
            if (DamageAmount < 0f && Source.InRadius(Destination, Range + 10f) && Target is Ship targetShip)
            {
                float beamRepairMuliplier = targetShip.InCombat ? 1 : 5;
                targetShip.ApplyRepairOnce(-DamageAmount * beamRepairMuliplier * elapsedTime, Owner?.Level ?? 0);
            }

            UpdateBeamMesh();
            if (Duration < 0f && !Infinite)
                Die(null, true);
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
}