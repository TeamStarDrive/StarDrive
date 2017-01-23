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
			this.Target = target;
			this.owner = Owner;
            Vector2 TargetPosition = Vector2.Normalize(target.Center);
			if (Owner.InFrustum)
			{
				this.DamageToggleSound = AudioManager.GetCue("sd_shield_static_1");
			}
			if (this.owner.isInDeepSpace || this.owner.System== null)
			{
				UniverseScreen.DeepSpaceManager.BeamList.Add(this);
			}
			else
			{
				this.System = this.owner.System;
				this.System.spatialManager.BeamList.Add(this);
			}
			this.Source = srcCenter;
            this.BeamOffsetAngle = Owner.Rotation - srcCenter.AngleToTarget(TargetPosition).ToRadians();
			this.Destination = MathExt.PointFromRadians(srcCenter, Owner.Rotation + this.BeamOffsetAngle, this.range);
			this.ActualHitDestination = this.Destination;
			this.Vertices = new VertexPositionNormalTexture[4];
			this.Indexes = new int[6];
			this.BeamZ = RandomMath2.RandomBetween(-1f, 1f);
            Vector3[] points = HelperFunctions.BeamPoints(srcCenter, TargetPosition, (float)Thickness, new Vector2[4], 0, this.BeamZ);
			this.UpperLeft = points[0];
			this.UpperRight = points[1];
			this.LowerLeft = points[2];
			this.LowerRight = points[3];
			this.FillVertices();
		}

		public Beam(Vector2 srcCenter, Vector2 destination, int thickness, Projectile Owner, GameplayObject target)
		{
			this.Target = target;
			this.DamageToggleSound = AudioManager.GetCue("sd_shield_static_1");
            if (Owner.isInDeepSpace || Owner.System== null)
			{
				UniverseScreen.DeepSpaceManager.BeamList.Add(this);
			}
			else
			{
				this.System = Owner.System;
				this.System.spatialManager.BeamList.Add(this);
			}
			this.Source = srcCenter;
			this.BeamOffsetAngle = 0f;
			this.ActualHitDestination = destination;
			this.Vertices = new VertexPositionNormalTexture[4];
			this.Indexes = new int[6];
			this.BeamZ = RandomMath2.RandomBetween(-1f, 1f);
			Vector3[] points = HelperFunctions.BeamPoints(srcCenter, destination, (float)thickness, new Vector2[4], 0, this.BeamZ);
			this.UpperLeft = points[0];
			this.UpperRight = points[1];
			this.LowerLeft = points[2];
			this.LowerRight = points[3];
			this.FillVertices();
		}
        public void BeamRecreate(Vector2 srcCenter, int Thickness, Ship Owner, GameplayObject target)
        {
            this.Indexes = new int[6]; ;
            ActualHitDestination = Vector2.Zero;
            this.followMouse = false;
            this.Duration = 2f;
            BeamOffsetAngle = 0f;
            this.BeamOffsetAngle = 0f;
            Indexes.Initialize();
            this.BeamZ = 0f;
            this.Target = null;
            this.infinite = false;
            this.DamageToggleSound = null;
            this.DamageToggleOn = false;

            this.moduleAttachedTo = this.weapon.moduleAttachedTo;
            this.PowerCost = this.weapon.BeamPowerCostPerSecond;
            this.range = this.weapon.Range;
            this.thickness = this.weapon.BeamThickness;
            this.Duration = (float)this.weapon.BeamDuration > 0 ? this.weapon.BeamDuration : 2f;
            this.damageAmount = this.weapon.DamageAmount;
            //this.weapon = this;
            this.Destination = target.Center;
            this.Active = true;
            

            this.Target = target;
            this.owner = Owner;
            Vector2 TargetPosition = Vector2.Normalize(target.Center);
            if (Owner.InFrustum)
            {
                this.DamageToggleSound = AudioManager.GetCue("sd_shield_static_1");
            }
            if (this.owner.isInDeepSpace || this.owner.System== null)
            {
                UniverseScreen.DeepSpaceManager.BeamList.Add(this);
            }
            else
            {
                this.System = this.owner.System;
                this.System.spatialManager.BeamList.Add(this);
            }
            this.Source = srcCenter;
            this.BeamOffsetAngle = Owner.Rotation - srcCenter.RadiansToTarget(TargetPosition);
            this.Destination = MathExt.PointFromRadians(srcCenter, Owner.Rotation + this.BeamOffsetAngle, this.range);
            this.ActualHitDestination = this.Destination;
            this.Vertices = new VertexPositionNormalTexture[4];
            this.Indexes = new int[6];
            this.BeamZ = RandomMath2.RandomBetween(-1f, 1f);
            Vector3[] points = HelperFunctions.BeamPoints(srcCenter, TargetPosition, (float)Thickness, new Vector2[4], 0, this.BeamZ);
            this.UpperLeft = points[0];
            this.UpperRight = points[1];                                 
            this.LowerLeft = points[2];
            this.LowerRight = points[3];
            this.FillVertices();
            this.Active = true;
        }
		public Beam(Vector2 srcCenter, Vector2 destination, int thickness, Ship shipOwner)
		{
			this.owner = shipOwner;
			if (shipOwner.InFrustum)
			{
				this.DamageToggleSound = AudioManager.GetCue("sd_shield_static_1");
			}
			if (this.owner.isInDeepSpace || this.owner.System== null)
			{
				UniverseScreen.DeepSpaceManager.BeamList.Add(this);
			}
			else
			{
				this.System = this.owner.System;
				this.System.spatialManager.BeamList.Add(this);
			}
			this.Source = srcCenter;
		    this.Thickness = thickness;
		    this.BeamOffsetAngle = shipOwner.Rotation - srcCenter.RadiansToTarget(destination);
			this.Destination = srcCenter.PointFromRadians(shipOwner.Rotation + this.BeamOffsetAngle, this.range);
			this.ActualHitDestination = this.Destination;
			this.Vertices = new VertexPositionNormalTexture[4];
			this.Indexes = new int[6];
			this.BeamZ = RandomMath2.RandomBetween(-1f, 1f);
			Vector3[] points = HelperFunctions.BeamPoints(srcCenter, destination, (float)thickness, new Vector2[4], 0, this.BeamZ);
			this.UpperLeft = points[0];
			this.UpperRight = points[1];
			this.LowerLeft = points[2];
			this.LowerRight = points[3];
			this.FillVertices();
		}

        public override void Die(GameplayObject source, bool cleanupOnly)
        {
            if (this.DamageToggleSound != null)
            {
                this.DamageToggleSound.Stop(AudioStopOptions.Immediate);
                this.DamageToggleSound = (Cue)null;
            }
            if (this.owner != null)
            {
                this.owner.Beams.QueuePendingRemoval(this);
                if (this.owner.System!= null)
                {
                    this.System = this.owner.System;
                    this.System.spatialManager.BeamList.Remove(this);
                }
                else
                    UniverseScreen.DeepSpaceManager.BeamList.Remove(this);
            }
            else if (this.weapon.drowner != null)
            {
                (this.weapon.drowner as Projectile).GetDroneAI().Beams.QueuePendingRemoval(this);
                if (this.weapon.drowner.System!= null)
                {
                    this.System = this.weapon.drowner.System;
                    this.System.spatialManager.BeamList.Remove(this);
                }
                else
                    UniverseScreen.DeepSpaceManager.BeamList.Remove(this);
            }
            this.weapon.ResetToggleSound();
            //if(this.quadVertexDecl !=null)
            //this.quadVertexDecl.Dispose();
            //if (this.quadEffect != null)
            //    this.quadEffect.Dispose();
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

		    bool isShipModule = target is ShipModule;
		    ShipModule targetModule = target as ShipModule;
		    if (target == owner && !weapon.HitsFriendlies)
		        return false;

		    if (target is Projectile && WeaponType != "Missile")
		        return false;
		    if (target is Ship)
		        return false;
		    if (damageAmount < 0f && isShipModule && targetModule.shield_power > 0f)
		        return false;

		    if (!DamageToggleOn && isShipModule)
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
                this.DamageToggleOn = false;
                if (this.DamageToggleSound != null && this.DamageToggleSound.IsPlaying)
                {
                    this.DamageToggleSound.Stop(AudioStopOptions.Immediate);
                    if (base.Owner.InFrustum)
                    {
                        this.DamageToggleSound = AudioManager.GetCue("sd_shield_static_1");
                    }
                }
            }
            Ship owner = base.Owner;
            owner.PowerCurrent = owner.PowerCurrent - this.PowerCost * elapsedTime;
            if (base.Owner.PowerCurrent < 0f)
            {
                base.Owner.PowerCurrent = 0f;
                this.Die(null, false);
                this.Duration = 0f;
                return;
            }
            Ship ship = this.Target as Ship;
            if (this.owner.engineState == Ship.MoveState.Warp || ship != null && ship.engineState == Ship.MoveState.Warp )
            {
                this.Die(null, false);
                this.Duration = 0f;
                return;
            }
            this.Duration -= elapsedTime;
            this.Source = srcCenter;

            //Modified by Gretman
            if (this.Target == null)// If current target sucks, use "destination" instead
            {
                Log.Info("Beam assigned alternate destination at update");
                this.Destination = Source.PointFromRadians(this.owner.Rotation - this.BeamOffsetAngle, this.range);
            }
            else if (!this.owner.isPlayerShip() && Vector2.Distance(this.Destination, this.Source) > this.range + this.owner.Radius) //So beams at the back of a ship can hit too!
            {
                Log.Info("Beam killed because of distance: Dist = " + Vector2.Distance(this.Destination, this.Source).ToString() + "  Beam Range = " + (this.range).ToString());
                this.Die(null, true);
                return;
            }
            else if (!this.owner.isPlayerShip() && !this.Owner.CheckIfInsideFireArc(this.weapon, this.Destination, base.Owner.Rotation))
            {
                Log.Info("Beam killed because of angle");
                this.Die(null, true);
                return;
            }
            else
            {
                this.Destination = this.Target.Center;
            }// Done messing with stuff - Gretman

            //if (this.quadEffect != null)
            //{
            //    this.quadEffect.View = view;
            //    this.quadEffect.Projection = projection;
            //}
            Vector3[] points = HelperFunctions.BeamPoints(srcCenter, this.ActualHitDestination, (float)Thickness, new Vector2[4], 0, this.BeamZ);
            this.UpperLeft = points[0];
            this.UpperRight = points[1];
            this.LowerLeft = points[2];
            this.LowerRight = points[3];
            this.FillVertices();

            if ((this.Duration < 0f && !this.infinite ))// ||Vector2.Distance(this.Destination, this.owner.Center) > this.range)
            {
                this.Die(null, true);
            }
        }

		public void UpdateDroneBeam(Vector2 srcCenter, Vector2 dstCenter, int Thickness, Matrix view, Matrix projection, float elapsedTime)
		{
        
            if (!this.CollidedThisFrame && this.DamageToggleOn)
			{
				this.DamageToggleOn = false;
				if (this.DamageToggleSound != null && this.DamageToggleSound.IsPlaying)
				{
					this.DamageToggleSound.Stop(AudioStopOptions.Immediate);
					if (base.Owner.InFrustum)
					{
						this.DamageToggleSound = AudioManager.GetCue("sd_shield_static_1");
					}
				}
			}
            this.Duration -= elapsedTime;
			this.Source = srcCenter;
			this.Destination = dstCenter;
			Vector3[] points = HelperFunctions.BeamPoints(srcCenter, this.Destination, (float)Thickness, new Vector2[4], 0, this.BeamZ);
			this.UpperLeft = points[0];
			this.UpperRight = points[1];
			this.LowerLeft = points[2];
			this.LowerRight = points[3];
			this.FillVertices();
			if (this.Duration < 0f && !this.infinite)
			{
				this.Die(null, true);
			}
		}


        protected override void Dispose(bool disposing)
        {
            quadVertexDecl?.Dispose(ref quadVertexDecl);
            base.Dispose(disposing);
        }
	}
}