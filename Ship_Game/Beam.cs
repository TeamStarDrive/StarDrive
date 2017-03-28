using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Runtime;


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
        public int thickness;
        public float PowerCost;
        public ShipModule hitLast;
        public Vector2 Source;
        private readonly int Thickness;
        public Vector2 Destination;
        public static Effect BeamEffect;
        public Vector2 ActualHitDestination;
        public bool followMouse;
        public float Duration = 2f;
        public float BeamOffsetAngle;
        public VertexPositionNormalTexture[] Vertices;
        public int[] Indexes;
        private float BeamZ;
        private GameplayObject Target;
        public bool infinite;
        private Cue DamageToggleSound;
        private bool DamageToggleOn;
        private VertexDeclaration quadVertexDecl;
        private float displacement = 1f;

        public Beam()
        {
        }

        public Beam(Vector2 srcCenter, int Thickness, Ship Owner, GameplayObject target)
        {
            Target = target;
            owner = Owner;
            Vector2 TargetPosition = Vector2.Normalize(target.Center);
            if (Owner.InFrustum)
            {
                DamageToggleSound = AudioManager.GetCue("sd_shield_static_1");
            }

            SetSystem(owner.System);
            Source = srcCenter;
            BeamOffsetAngle = Owner.Rotation - srcCenter.AngleToTarget(TargetPosition).ToRadians();
            Destination = MathExt.PointFromRadians(srcCenter, Owner.Rotation + BeamOffsetAngle, range);
            ActualHitDestination = Destination;
            Vertices = new VertexPositionNormalTexture[4];
            Indexes = new int[6];
            BeamZ = RandomMath2.RandomBetween(-1f, 1f);
            Vector3[] points = HelperFunctions.BeamPoints(srcCenter, TargetPosition, (float)Thickness, new Vector2[4], 0, BeamZ);
            UpperLeft = points[0];
            UpperRight = points[1];
            LowerLeft = points[2];
            LowerRight = points[3];
            FillVertices();
        }

        public Beam(Vector2 srcCenter, Vector2 destination, int thickness, Projectile Owner, GameplayObject target)
        {
            Target = target;
            DamageToggleSound = AudioManager.GetCue("sd_shield_static_1");
            SetSystem(Owner.System);
            Source          = srcCenter;
            BeamOffsetAngle = 0f;
            Vertices        = new VertexPositionNormalTexture[4];
            Indexes         = new int[6];
            BeamZ           = RandomMath2.RandomBetween(-1f, 1f);
            ActualHitDestination = destination;
            Vector3[] points = HelperFunctions.BeamPoints(srcCenter, destination, (float)thickness, new Vector2[4], 0, BeamZ);
            UpperLeft  = points[0];
            UpperRight = points[1];
            LowerLeft  = points[2];
            LowerRight = points[3];
            FillVertices();
        }
        public void BeamRecreate(Vector2 srcCenter, int Thickness, Ship Owner, GameplayObject target)
        {
            Indexes = new int[6];
            ActualHitDestination = Vector2.Zero;
            followMouse     = false;
            Duration        = 2f;
            BeamOffsetAngle = 0f;
            BeamOffsetAngle = 0f;
            Indexes.Initialize();
            BeamZ             = 0f;
            Target            = null;
            infinite          = false;
            DamageToggleSound = null;
            DamageToggleOn    = false;

            moduleAttachedTo = weapon.moduleAttachedTo;
            PowerCost        = weapon.BeamPowerCostPerSecond;
            range            = weapon.Range;
            thickness        = weapon.BeamThickness;
            Duration         = weapon.BeamDuration > 0 ? weapon.BeamDuration : 2f;
            damageAmount     = weapon.DamageAmount;
            Destination      = target.Center;
            Active           = true;
            
            Target = target;
            owner = Owner;
            Vector2 TargetPosition = Vector2.Normalize(target.Center);
            if (Owner.InFrustum)
            {
                DamageToggleSound = AudioManager.GetCue("sd_shield_static_1");
            }
            SetSystem(owner.System);
            Source          = srcCenter;
            BeamOffsetAngle = Owner.Rotation - srcCenter.RadiansToTarget(TargetPosition);
            Destination     = MathExt.PointFromRadians(srcCenter, Owner.Rotation + BeamOffsetAngle, range);
            Vertices        = new VertexPositionNormalTexture[4];
            Indexes         = new int[6];
            BeamZ           = RandomMath2.RandomBetween(-1f, 1f);
            ActualHitDestination = Destination;
            Vector3[] points = HelperFunctions.BeamPoints(srcCenter, TargetPosition, Thickness, new Vector2[4], 0, BeamZ);
            UpperLeft  = points[0];
            UpperRight = points[1];                                 
            LowerLeft  = points[2];
            LowerRight = points[3];
            FillVertices();
            Active = true;
        }
        public Beam(Vector2 srcCenter, Vector2 destination, int thickness, Ship shipOwner)
        {
            owner = shipOwner;
            if (shipOwner.InFrustum)
            {
                DamageToggleSound = AudioManager.GetCue("sd_shield_static_1");
            }
            SetSystem(owner.System);
            Source = srcCenter;
            Thickness       = thickness;
            BeamOffsetAngle = shipOwner.Rotation - srcCenter.RadiansToTarget(destination);
            Destination     = srcCenter.PointFromRadians(shipOwner.Rotation + BeamOffsetAngle, range);
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
            if (DamageToggleSound != null)
            {
                DamageToggleSound.Stop(AudioStopOptions.Immediate);
                DamageToggleSound = null;
            }
            if (owner != null)
            {
                owner.RemoveBeam(this);
                SetSystem(owner.System);
            }
            else if (weapon.drowner != null)
            {
                (weapon.drowner as Projectile)?.GetDroneAI().Beams.QueuePendingRemoval(this);
                SetSystem(weapon.drowner.System);
            }
            weapon.ResetToggleSound();
        }

        public void Draw(ScreenManager screenMgr)
        {
            lock (GlobalStats.BeamEffectLocker)
            {
                Empire.Universe.beamflashes.AddParticleThreadA(new Vector3(Source, BeamZ), Vector3.Zero);
                screenMgr.GraphicsDevice.VertexDeclaration = quadVertexDecl;
                BeamEffect.CurrentTechnique = BeamEffect.Techniques["Technique1"];
                BeamEffect.Parameters["World"].SetValue(Matrix.Identity);
                string beamTexPath = "Beams/" + ResourceManager.WeaponsDict[weapon.UID].BeamTexture;
                BeamEffect.Parameters["tex"].SetValue(ResourceManager.Texture(beamTexPath));
                displacement -= 0.05f;
                if (displacement < 0f)
                {
                    displacement = 1f;
                }
                BeamEffect.Parameters["displacement"].SetValue(new Vector2(0f, displacement));
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
            quadVertexDecl = new VertexDeclaration(screenMgr.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
            return true;
        }

        public override bool Touch(GameplayObject target)
        {
            if (target == null)
                return true;
            if (target == owner && !weapon.HitsFriendlies)
                return false;
            if (target is Projectile && WeaponType != "Missile")
                return false;
            if (target is Ship)
                return false;

            var targetModule = target as ShipModule;
            if (damageAmount < 0f && targetModule?.ShieldPower > 0f)
                return false;

            if (!DamageToggleOn && targetModule != null)
            {
                DamageToggleOn = true;
            }
            else if (DamageToggleOn && DamageToggleSound != null && DamageToggleSound.IsPrepared)
            {
                // @todo What's going on here?
            }

            targetModule?.Damage(this, damageAmount);
            return true;
        }

        public void Update(Vector2 srcCenter, Vector2 dstCenter, int Thickness, Matrix view, Matrix projection, float elapsedTime)
        {
            if (!CollidedThisFrame && DamageToggleOn)
            {
                DamageToggleOn = false;
                if (DamageToggleSound != null && DamageToggleSound.IsPlaying)
                {
                    DamageToggleSound.Stop(AudioStopOptions.Immediate);
                    if (base.Owner.InFrustum)
                    {
                        DamageToggleSound = AudioManager.GetCue("sd_shield_static_1");
                    }
                }
            }
            Ship owner = base.Owner;
            owner.PowerCurrent = owner.PowerCurrent - PowerCost * elapsedTime;
            if (base.Owner.PowerCurrent < 0f)
            {
                base.Owner.PowerCurrent = 0f;
                Die(null, false);
                Duration = 0f;
                return;
            }
            Ship ship = Target as Ship;
            if (owner.engineState == Ship.MoveState.Warp || ship != null && ship.engineState == Ship.MoveState.Warp )
            {
                Die(null, false);
                Duration = 0f;
                return;
            }
            Duration -= elapsedTime;
            Source = srcCenter;

            //Modified by Gretman
            if (Target == null)// If current target sucks, use "destination" instead
            {
                Log.Info("Beam assigned alternate destination at update");
                Destination = Source.PointFromRadians(owner.Rotation - BeamOffsetAngle, range);
            }
            else if (!owner.isPlayerShip() && Vector2.Distance(Destination, Source) > range + owner.Radius) //So beams at the back of a ship can hit too!
            {
                Log.Info("Beam killed because of distance: Dist = " + Vector2.Distance(Destination, Source).ToString() + "  Beam Range = " + (range).ToString());
                Die(null, true);
                return;
            }
            else if (!owner.isPlayerShip() && !Owner.CheckIfInsideFireArc(weapon, Destination, base.Owner.Rotation))
            {
                Log.Info("Beam killed because of angle");
                Die(null, true);
                return;
            }
            else
            {
                Destination = Target.Center;
            }// Done messing with stuff - Gretman

            //if (quadEffect != null)
            //{
            //    quadEffect.View = view;
            //    quadEffect.Projection = projection;
            //}
            Vector3[] points = HelperFunctions.BeamPoints(srcCenter, ActualHitDestination, (float)Thickness, new Vector2[4], 0, BeamZ);
            UpperLeft = points[0];
            UpperRight = points[1];
            LowerLeft = points[2];
            LowerRight = points[3];
            FillVertices();

            if ((Duration < 0f && !infinite ))// ||Vector2.Distance(Destination, owner.Center) > range)
            {
                Die(null, true);
            }
        }

        public void UpdateDroneBeam(Vector2 srcCenter, Vector2 dstCenter, int Thickness, Matrix view, Matrix projection, float elapsedTime)
        {
        
            if (!CollidedThisFrame && DamageToggleOn)
            {
                DamageToggleOn = false;
                if (DamageToggleSound != null && DamageToggleSound.IsPlaying)
                {
                    DamageToggleSound.Stop(AudioStopOptions.Immediate);
                    if (base.Owner.InFrustum)
                    {
                        DamageToggleSound = AudioManager.GetCue("sd_shield_static_1");
                    }
                }
            }
            Duration -= elapsedTime;
            Source = srcCenter;
            Destination = dstCenter;
            Vector3[] points = HelperFunctions.BeamPoints(srcCenter, Destination, (float)Thickness, new Vector2[4], 0, BeamZ);
            UpperLeft = points[0];
            UpperRight = points[1];
            LowerLeft = points[2];
            LowerRight = points[3];
            FillVertices();
            if (Duration < 0f && !infinite)
            {
                Die(null, true);
            }
        }


        protected override void Dispose(bool disposing)
        {
            quadVertexDecl?.Dispose(ref quadVertexDecl);
            base.Dispose(disposing);
        }
    }
}