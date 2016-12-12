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

		public Vector2 Destination;

		public static Effect BeamEffect;

		public Vector2 ActualHitDestination;

		public bool followMouse;

        //added by McShooterz: changed back to default
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

		//private BasicEffect quadEffect;

		private float displacement = 1f;
        //private bool recycled = false;

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

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

		public Beam(Vector2 srcCenter, Vector2 destination, int Thickness, Projectile Owner, GameplayObject target)
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
			Vector3[] points = HelperFunctions.BeamPoints(srcCenter, destination, (float)Thickness, new Vector2[4], 0, this.BeamZ);
			this.UpperLeft = points[0];
			this.UpperRight = points[1];
			this.LowerLeft = points[2];
			this.LowerRight = points[3];
			this.FillVertices();
		}
        public void BeamRecreate(Vector2 srcCenter, int Thickness, Ship Owner, GameplayObject target)
        {


            //Origin = Vector3.Zero;

            //UpperLeft = Vector3.Zero;
            //LowerLeft = Vector3.Zero;

            //UpperRight = Vector3.Zero;

            //LowerRight = Vector3.Zero;

            //Normal = Vector3.Zero;

            //Up = Vector3.Zero;

            //Left = Vector3.Zero;

            //thickness = 0;

            //PowerCost = 0f;

            //hitLast = null;

            //Source = Vector2.Zero;

            Vector2 Destination = Vector2.Zero;

            this.Indexes = new int[6]; ;

           this.ActualHitDestination = Vector2.Zero;

            this.followMouse = false;

            this.Duration = 2f;

            BeamOffsetAngle = 0f;
            this.BeamOffsetAngle = 0f;

            //this.Vertices=new VertexPositionNormalTexture();

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
		public Beam(Vector2 srcCenter, Vector2 destination, int Thickness, Ship Owner)
		{
			this.owner = Owner;
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
			this.BeamOffsetAngle = Owner.Rotation - srcCenter.RadiansToTarget(destination);
			this.Destination = MathExt.PointFromRadians(srcCenter, Owner.Rotation + this.BeamOffsetAngle, this.range);
			this.ActualHitDestination = this.Destination;
			this.Vertices = new VertexPositionNormalTexture[4];
			this.Indexes = new int[6];
			this.BeamZ = RandomMath2.RandomBetween(-1f, 1f);
			Vector3[] points = HelperFunctions.BeamPoints(srcCenter, destination, (float)Thickness, new Vector2[4], 0, this.BeamZ);
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
                    this.System.spatialManager.BeamList.QueuePendingRemoval(this);
                }
                else
                    UniverseScreen.DeepSpaceManager.BeamList.QueuePendingRemoval(this);
            }
            else if (this.weapon.drowner != null)
            {
                (this.weapon.drowner as Projectile).GetDroneAI().Beams.QueuePendingRemoval(this);
                if (this.weapon.drowner.System!= null)
                {
                    this.System = this.weapon.drowner.System;
                    this.System.spatialManager.BeamList.QueuePendingRemoval(this);
                }
                else
                    UniverseScreen.DeepSpaceManager.BeamList.QueuePendingRemoval(this);
            }
            this.weapon.ResetToggleSound();
            //if(this.quadVertexDecl !=null)
            //this.quadVertexDecl.Dispose();
            //if (this.quadEffect != null)
            //    this.quadEffect.Dispose();
        }

		public void Draw(Ship_Game.ScreenManager ScreenManager)
		{
			lock (GlobalStats.BeamEffectLocker)
			{
				Projectile.universeScreen.beamflashes.AddParticleThreadA(new Vector3(this.Source, this.BeamZ), Vector3.Zero);
				ScreenManager.GraphicsDevice.VertexDeclaration = this.quadVertexDecl;
				Beam.BeamEffect.CurrentTechnique = Beam.BeamEffect.Techniques["Technique1"];
				Beam.BeamEffect.Parameters["World"].SetValue(Matrix.Identity);
                string beamTexPath = "Beams/" + ResourceManager.WeaponsDict[weapon.UID].BeamTexture;
                var beamTex = ResourceManager.GetTexture(beamTexPath);
				Beam.BeamEffect.Parameters["tex"].SetValue(beamTex);
				Beam beam = this;
				beam.displacement = beam.displacement - 0.05f;
				if (this.displacement < 0f)
				{
					this.displacement = 1f;
				}
				Beam.BeamEffect.Parameters["displacement"].SetValue(new Vector2(0f, this.displacement));
				Beam.BeamEffect.Begin();
				ScreenManager.GraphicsDevice.RenderState.AlphaTestEnable = true;
				ScreenManager.GraphicsDevice.RenderState.AlphaFunction = CompareFunction.GreaterEqual;
				ScreenManager.GraphicsDevice.RenderState.ReferenceAlpha = 200;
				foreach (EffectPass pass in Beam.BeamEffect.CurrentTechnique.Passes)
				{
					pass.Begin();
					ScreenManager.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, this.Vertices, 0, 4, this.Indexes, 0, 2);
					pass.End();
				}
				ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;
				ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = true;
				ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
				ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
				ScreenManager.GraphicsDevice.RenderState.AlphaTestEnable = true;
				ScreenManager.GraphicsDevice.RenderState.AlphaFunction = CompareFunction.Less;
				ScreenManager.GraphicsDevice.RenderState.ReferenceAlpha = 200;
				foreach (EffectPass pass in Beam.BeamEffect.CurrentTechnique.Passes)
				{
					pass.Begin();
					ScreenManager.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, this.Vertices, 0, 4, this.Indexes, 0, 2);
					pass.End();
				}
				ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = false;
				ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
				ScreenManager.GraphicsDevice.RenderState.AlphaTestEnable = false;
				Beam.BeamEffect.End();
			}
		}

		private void FillVertices()
		{
			Vector2 textureUpperLeft = new Vector2(0f, 0f);
			Vector2 textureUpperRight = new Vector2(1f, 0f);
			Vector2 textureLowerLeft = new Vector2(0f, 1f);
			Vector2 textureLowerRight = new Vector2(1f, 1f);
			this.Vertices[0].Position = this.LowerLeft;
			this.Vertices[0].TextureCoordinate = textureLowerLeft;
			this.Vertices[1].Position = this.UpperLeft;
			this.Vertices[1].TextureCoordinate = textureUpperLeft;
			this.Vertices[2].Position = this.LowerRight;
			this.Vertices[2].TextureCoordinate = textureLowerRight;
			this.Vertices[3].Position = this.UpperRight;
			this.Vertices[3].TextureCoordinate = textureUpperRight;
			this.Indexes[0] = 0;
			this.Indexes[1] = 1;
			this.Indexes[2] = 2;
			this.Indexes[3] = 2;
			this.Indexes[4] = 1;
			this.Indexes[5] = 3;
		}

		public GameplayObject GetTarget()
		{
			return this.Target;
		}

		public bool LoadContent(ScreenManager ScreenManager, Matrix view, Matrix projection)
		{
			//lock (GlobalStats.BeamEffectLocker)
			//{
                
                    //Texture2D texture = ResourceManager.TextureDict[string.Concat("Beams/", ResourceManager.WeaponsDict[this.weapon.UID].BeamTexture)];
                   // Beam beam=null;
                //if(this.owner != null)
                  //  beam = this.owner.Beams.RecycleObject();
                    this.quadVertexDecl = new VertexDeclaration(ScreenManager.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
                    //Beam.BeamEffect.Parameters["tex"].SetValue(texture);
                    return true;
   //            // if (beam != null) // || beam.quadEffect == null)
   //                 {
   //                     try
   //                     {
   //                         this.quadEffect = new BasicEffect(ScreenManager.GraphicsDevice, (EffectPool)null)
   //                             {
   //                                 World = Matrix.Identity,
   //                                 View = view,
   //                                 Projection = projection,
   //                                 TextureEnabled = true,
   //                                // Texture = texture// ResourceManager.TextureDict[string.Concat("Beams/", ResourceManager.WeaponsDict[this.weapon.UID].BeamTexture)]
   //                             };
   //                         this.quadVertexDecl = new VertexDeclaration(ScreenManager.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
   //                         //Beam.BeamEffect.Parameters["tex"].SetValue(texture);
   //                     }
   //                     catch
   //                     {
   //                         GlobalStats.BeamOOM++;
   //                         if (GlobalStats.BeamOOM > 10)
   //                         {
   //                             GC.WaitForPendingFinalizers(); GC.Collect();

   //                             GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
   //                             GC.Collect();
   //                             GlobalStats.BeamOOM = 0;
   //                         }
                            
   //                         System.Diagnostics.Debug.WriteLine("BEAM EXPLODED");


   //                         this.Active = false;
   //                         return false;
   //                     }
   //                 }
   //               //  else
   //                 {
   //                     //this.quadEffect = beam.quadEffect;
                        
   //                     //if(this.quadEffect.World != Matrix.Identity)
   //                     //this.quadEffect.World = Matrix.Identity;
   //                     //if(this.quadEffect.View!=view)
   //                     //this.quadEffect.View = view;
   //                     //if(this.quadEffect.Projection != projection)
   //                     //this.quadEffect.Projection = projection;
   //                     //this.quadEffect.TextureEnabled = true;
                        

   //                     //    this.quadEffect.Texture = texture;
   //                  //       Beam.BeamEffect.Parameters["tex"].SetValue(texture);
                      
                    
                        
   //                 }
                    
                    
   //                    //ResourceManager.TextureDict[string.Concat("Beams/", ResourceManager.WeaponsDict[this.weapon.UID].BeamTexture)]);
     
              
			//}
   //         return true;
		}

		public override bool Touch(GameplayObject target)
		{
			if (target != null)
			{
                bool isShipModule = target is ShipModule;
                ShipModule targetShipmodule = target as ShipModule;
                if (target == this.owner && !this.weapon.HitsFriendlies)
				{
					return false;
				}
				if (target is Projectile && this.WeaponType != "Missile")
				{
					return false;
				}
				if (target is Ship)
				{
					return false;
				}
                if (this.damageAmount < 0f && isShipModule && targetShipmodule.shield_power > 0f)
				{
					return false;
				}
                if (!this.DamageToggleOn && isShipModule)
				{
					this.DamageToggleOn = true;
				}
				else if (this.DamageToggleOn && this.DamageToggleSound != null && this.DamageToggleSound.IsPrepared)
				{
					try
					{
						bool inFrustum = base.Owner.InFrustum;
					}
					catch
					{
					}
				}
                targetShipmodule.Damage(this, this.damageAmount);
			}
			return true;
		}

		public void Update(Vector2 srcCenter, Vector2 dstCenter, int Thickness, Matrix view, Matrix projection, float elapsedTime)
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
                global::System.Diagnostics.Debug.WriteLine("Beam assigned alternate destination at update");
                this.Destination = MathExt.PointFromRadians(this.Source, this.owner.Rotation - this.BeamOffsetAngle, this.range);
            }
            else if (!this.owner.isPlayerShip() && Vector2.Distance(this.Destination, this.Source) > this.range + this.owner.Radius) //So beams at the back of a ship can hit too!
            {
                global::System.Diagnostics.Debug.WriteLine("Beam killed because of distance: Dist = " + Vector2.Distance(this.Destination, this.Source).ToString() + "  Beam Range = " + (this.range).ToString());
                this.Die(null, true);
                return;
            }
            else if (!this.owner.isPlayerShip() && !this.Owner.CheckIfInsideFireArc(this.weapon, this.Destination, base.Owner.Rotation))
            {
                global::System.Diagnostics.Debug.WriteLine("Beam killed because of angle");
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
            //if (this.quadEffect != null)
            ////this.Die(null, true);
            //{
            //    this.quadEffect.View = view;
            //    this.quadEffect.Projection = projection;
            //}
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


        ~Beam() { Dispose(false); }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing )
                {
                    
                    if (this.quadVertexDecl != null)
                        this.quadVertexDecl.Dispose();
                    //if (this.quadEffect != null)
                    //    this.quadEffect.Dispose();
                    

                }
                this.quadVertexDecl = null;
                this.disposed = true;
                base.Dispose(disposing);
            }
        }
	}
}