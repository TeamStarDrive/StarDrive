using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;


namespace Ship_Game
{
    public sealed class Beam : Projectile
    {
        public Vector3 Origin;
        public Vector3 UpperLeft;
        public Vector3 LowerLeft;
        public Vector3 UpperRight;
        public Vector3 LowerRight;
        public Vector3 Normal;
        public Vector3 Up;
        public Vector3 Left;
        public float PowerCost;
        public ShipModule HitLast;
        public Vector2 Source;
        public int Thickness { get; private set; }
        public Vector2 Destination;
        public static Effect BeamEffect;
        public Vector2 ActualHitDestination;
        public bool FollowMouse;
        public float BeamOffsetAngle;
        public VertexPositionNormalTexture[] Vertices;
        public int[] Indexes;
        private float BeamZ;
        private GameplayObject Target;
        public bool Infinite;
        private VertexDeclaration QuadVertexDecl;
        private float Displacement = 1f;

        private AudioHandle DamageToggleSound;
        private float DamageSoundTimer;

        public Beam()
        {
            Duration = 2f;
        }

        public Beam(Vector2 srcCenter, int thickness, Ship owner, GameplayObject target) : this()
        {
            Thickness = thickness;
            Target = target;
            Owner = owner;
            Vector2 targetDir = Vector2.Normalize(target.Center);
            SetSystem(owner.System);
            Source = srcCenter;
            BeamOffsetAngle = owner.Rotation - srcCenter.AngleToTarget(targetDir).ToRadians();
            Destination = srcCenter.PointFromRadians(owner.Rotation + BeamOffsetAngle, Range);
            ActualHitDestination = Destination;
            Vertices = new VertexPositionNormalTexture[4];
            Indexes = new int[6];
            BeamZ = RandomMath2.RandomBetween(-1f, 1f);
            Vector3[] points = HelperFunctions.BeamPoints(srcCenter, targetDir, thickness, new Vector2[4], 0, BeamZ);
            UpperLeft  = points[0];
            UpperRight = points[1];
            LowerLeft  = points[2];
            LowerRight = points[3];
            FillVertices();
        }

        public Beam(Vector2 srcCenter, Vector2 destination, int thickness, Projectile owner, GameplayObject target) : this()
        {
            Target = target;
            SetSystem(owner.System);
            Source          = srcCenter;
            BeamOffsetAngle = 0f;
            Vertices        = new VertexPositionNormalTexture[4];
            Indexes         = new int[6];
            BeamZ           = RandomMath2.RandomBetween(-1f, 1f);
            ActualHitDestination = destination;
            Vector3[] points = HelperFunctions.BeamPoints(srcCenter, destination, thickness, new Vector2[4], 0, BeamZ);
            UpperLeft  = points[0];
            UpperRight = points[1];
            LowerLeft  = points[2];
            LowerRight = points[3];
            FillVertices();
        }
        public void BeamRecreate(Vector2 srcCenter, int thickness, Ship owner, GameplayObject target)
        {
            Indexes = new int[6];
            ActualHitDestination = Vector2.Zero;
            FollowMouse     = false;
            Duration        = 2f;
            BeamOffsetAngle = 0f;
            BeamOffsetAngle = 0f;
            Indexes.Initialize();
            BeamZ             = 0f;
            Target            = null;
            Infinite          = false;
            DamageToggleSound.Stop();
            DamageSoundTimer = 0f;

            ModuleAttachedTo = Weapon.moduleAttachedTo;
            PowerCost        = Weapon.BeamPowerCostPerSecond;
            Range            = Weapon.Range;
            Thickness        = Weapon.BeamThickness;
            Duration         = Weapon.BeamDuration > 0 ? Weapon.BeamDuration : 2f;
            DamageAmount     = Weapon.DamageAmount;
            Destination      = target.Center;
            Active           = true;
            
            Target = target;
            Owner = owner;
            Vector2 targetDir = Vector2.Normalize(target.Center);
            SetSystem(Owner.System);
            Source          = srcCenter;
            BeamOffsetAngle = owner.Rotation - srcCenter.RadiansToTarget(targetDir);
            Destination     = srcCenter.PointFromRadians(owner.Rotation + BeamOffsetAngle, Range);
            Vertices        = new VertexPositionNormalTexture[4];
            Indexes         = new int[6];
            BeamZ           = RandomMath2.RandomBetween(-1f, 1f);
            ActualHitDestination = Destination;
            Vector3[] points = HelperFunctions.BeamPoints(srcCenter, targetDir, Thickness, new Vector2[4], 0, BeamZ);
            UpperLeft  = points[0];
            UpperRight = points[1];                                 
            LowerLeft  = points[2];
            LowerRight = points[3];
            FillVertices();
            Active = true;
        }
        public Beam(Vector2 srcCenter, Vector2 destination, int thickness, Ship shipOwner)
        {
            Owner = shipOwner;
            SetSystem(Owner.System);
            Source = srcCenter;
            Thickness       = thickness;
            BeamOffsetAngle = shipOwner.Rotation - srcCenter.RadiansToTarget(destination);
            Destination     = srcCenter.PointFromRadians(shipOwner.Rotation + BeamOffsetAngle, Range);
            Vertices        = new VertexPositionNormalTexture[4];
            Indexes         = new int[6];
            BeamZ           = RandomMath2.RandomBetween(-1f, 1f);
            ActualHitDestination = Destination;
            Vector3[] points = HelperFunctions.BeamPoints(srcCenter, destination, thickness, new Vector2[4], 0, BeamZ);
            UpperLeft  = points[0];
            UpperRight = points[1];
            LowerLeft  = points[2];
            LowerRight = points[3];
            FillVertices();
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
                (Weapon.drowner as Projectile)?.GetDroneAI().Beams.QueuePendingRemoval(this);
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
                string beamTexPath = "Beams/" + ResourceManager.WeaponsDict[Weapon.UID].BeamTexture;
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

        private void FillVertices()
        {
            Vertices[0].Position = LowerLeft;
            Vertices[1].Position = UpperLeft;
            Vertices[2].Position = LowerRight;
            Vertices[3].Position = UpperRight;
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

        public GameplayObject GetTarget()
        {
            return Target;
        }

        public bool LoadContent(ScreenManager screenMgr, Matrix view, Matrix projection)
        {
            QuadVertexDecl = new VertexDeclaration(screenMgr.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
            return true;
        }

        public override bool Touch(GameplayObject target)
        {
            if (target == null || target == Owner && !Weapon.HitsFriendlies || target is Ship)
                return false;
            if (target is Projectile && WeaponType != "Missile")
                return false;

            var targetModule = target as ShipModule;
            if (DamageAmount < 0f && targetModule?.ShieldPower > 0f) // @todo Repair beam??
                return false;

            //// trigger shield static sfx.... @todo BUT WHY?
            //if (targetModule != null && targetModule.ShieldPower > 0f)
            //{
            //    if (Owner.InFrustum && DamageToggleSound.NotPlaying)
            //        DamageToggleSound.PlaySfxAsync("sd_shield_static_1");
            //}

            targetModule?.Damage(this, DamageAmount);
            return true;
        }

        public void Update(Vector2 srcCenter, Vector2 dstCenter, int thickness, Matrix view, Matrix projection, float elapsedTime)
        {
            Owner.PowerCurrent = Owner.PowerCurrent - PowerCost * elapsedTime;
            if (Owner.PowerCurrent < 0f)
            {
                Owner.PowerCurrent = 0f;
                Die(null, false);
                Duration = 0f;
                return;
            }
            var ship = Target as Ship;
            if (Owner.engineState == Ship.MoveState.Warp || ship != null && ship.engineState == Ship.MoveState.Warp )
            {
                Die(null, false);
                Duration = 0f;
                return;
            }
            Duration -= elapsedTime;
            Source = srcCenter;

            // Modified by Gretman
            if (Target == null) // If current target sucks, use "destination" instead
            {
                Log.Info("Beam assigned alternate destination at update");
                Destination = Source.PointFromRadians(Owner.Rotation - BeamOffsetAngle, Range);
            }
            else if (!Owner.isPlayerShip() && Destination.OutsideRadius(Source, Range + Owner.Radius)) // So beams at the back of a ship can hit too!
            {
                Log.Info($"Beam killed because of distance: Dist = {Destination.Distance(Source)}  Beam Range = {Range}");
                Die(null, true);
                return;
            }
            else if (!Owner.isPlayerShip() && !Owner.CheckIfInsideFireArc(Weapon, Destination, Owner.Rotation))
            {
                Log.Info("Beam killed because of angle");
                Die(null, true);
                return;
            }
            else
            {
                Destination = Target.Center;
            }// Done messing with stuff - Gretman

            Vector3[] points = HelperFunctions.BeamPoints(srcCenter, ActualHitDestination, thickness, new Vector2[4], 0, BeamZ);
            UpperLeft = points[0];
            UpperRight = points[1];
            LowerLeft = points[2];
            LowerRight = points[3];
            FillVertices();

            if ((Duration < 0f && !Infinite ))// ||Vector2.Distance(Destination, owner.Center) > range)
            {
                Die(null, true);
            }
        }

        public void UpdateDroneBeam(Vector2 srcCenter, Vector2 dstCenter, int thickness, Matrix view, Matrix projection, float elapsedTime)
        {
            Duration -= elapsedTime;
            Source = srcCenter;
            Destination = dstCenter;
            Vector3[] points = HelperFunctions.BeamPoints(srcCenter, Destination, thickness, new Vector2[4], 0, BeamZ);
            UpperLeft = points[0];
            UpperRight = points[1];
            LowerLeft = points[2];
            LowerRight = points[3];
            FillVertices();
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
    }
}