using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class Beam : Projectile
	{
		public bool InFrustumWhenFired;

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

		private BasicEffect quadEffect;

		private float displacement = 1f;

		public Beam()
		{
		}

		public Beam(Vector2 srcCenter, Vector2 destination, int Thickness, Ship Owner, GameplayObject target)
		{
			this.Target = target;
			this.owner = Owner;
			if (Owner.InFrustum)
			{
				this.DamageToggleSound = AudioManager.GetCue("sd_shield_static_1");
				this.InFrustumWhenFired = true;
			}
			if (this.owner.isInDeepSpace || this.owner.GetSystem() == null)
			{
				UniverseScreen.DeepSpaceManager.BeamList.Add(this);
			}
			else
			{
				this.system = this.owner.GetSystem();
				this.system.spatialManager.BeamList.Add(this);
			}
			this.Source = srcCenter;
			this.BeamOffsetAngle = Owner.Rotation - MathHelper.ToRadians(HelperFunctions.findAngleToTarget(srcCenter, destination));
			this.Destination = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(srcCenter, Owner.Rotation + this.BeamOffsetAngle, this.range);
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

		public Beam(Vector2 srcCenter, Vector2 destination, int Thickness, Projectile Owner, GameplayObject target)
		{
			this.Target = target;
			this.DamageToggleSound = AudioManager.GetCue("sd_shield_static_1");
			if (Owner.GetSystem() == null)
			{
				UniverseScreen.DeepSpaceManager.BeamList.Add(this);
			}
			else
			{
				this.system = Owner.GetSystem();
				this.system.spatialManager.BeamList.Add(this);
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

		public Beam(Vector2 srcCenter, Vector2 destination, int Thickness, Ship Owner)
		{
			this.owner = Owner;
			if (Owner.InFrustum)
			{
				this.DamageToggleSound = AudioManager.GetCue("sd_shield_static_1");
				this.InFrustumWhenFired = true;
			}
			if (this.owner.isInDeepSpace || this.owner.GetSystem() == null)
			{
				UniverseScreen.DeepSpaceManager.BeamList.Add(this);
			}
			else
			{
				this.system = this.owner.GetSystem();
				this.system.spatialManager.BeamList.Add(this);
			}
			this.Source = srcCenter;
			this.BeamOffsetAngle = Owner.Rotation - MathHelper.ToRadians(HelperFunctions.findAngleToTarget(srcCenter, destination));
			this.Destination = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(srcCenter, Owner.Rotation + this.BeamOffsetAngle, this.range);
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
				this.DamageToggleSound = null;
			}
			if (this.owner != null)
			{
				this.owner.Beams.QueuePendingRemoval(this);
				if (this.owner.GetSystem() == null)
				{
					UniverseScreen.DeepSpaceManager.BeamList.QueuePendingRemoval(this);
				}
				else
				{
					this.system = this.owner.GetSystem();
					this.system.spatialManager.BeamList.QueuePendingRemoval(this);
				}
			}
			else if (this.weapon.drowner != null)
			{
				(this.weapon.drowner as Projectile).GetDroneAI().Beams.QueuePendingRemoval(this);
				if (this.weapon.drowner.GetSystem() == null)
				{
					UniverseScreen.DeepSpaceManager.BeamList.QueuePendingRemoval(this);
				}
				else
				{
					this.system = this.weapon.drowner.GetSystem();
					this.system.spatialManager.BeamList.QueuePendingRemoval(this);
				}
			}
			this.weapon.ResetToggleSound();
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager)
		{
			lock (GlobalStats.BeamEffectLocker)
			{
				Projectile.universeScreen.beamflashes.AddParticleThreadA(new Vector3(this.Source, this.BeamZ), Vector3.Zero);
				ScreenManager.GraphicsDevice.VertexDeclaration = this.quadVertexDecl;
				Beam.BeamEffect.CurrentTechnique = Beam.BeamEffect.Techniques["Technique1"];
				Beam.BeamEffect.Parameters["World"].SetValue(Matrix.Identity);
				Beam.BeamEffect.Parameters["tex"].SetValue(ResourceManager.TextureDict[string.Concat("Beams/", ResourceManager.WeaponsDict[this.weapon.UID].BeamTexture)]);
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

		public void LoadContent(Ship_Game.ScreenManager ScreenManager, Matrix view, Matrix projection)
		{
			lock (GlobalStats.BeamEffectLocker)
			{
				this.quadEffect = new BasicEffect(ScreenManager.GraphicsDevice, (EffectPool)null)
				{
					World = Matrix.Identity,
					View = view,
					Projection = projection,
					TextureEnabled = true,
					Texture = ResourceManager.TextureDict[string.Concat("Beams/", ResourceManager.WeaponsDict[this.weapon.UID].BeamTexture)]
				};
				this.quadVertexDecl = new VertexDeclaration(ScreenManager.GraphicsDevice, VertexPositionNormalTexture.VertexElements);
				Beam.BeamEffect.Parameters["tex"].SetValue(ResourceManager.TextureDict[string.Concat("Beams/", ResourceManager.WeaponsDict[this.weapon.UID].BeamTexture)]);
			}
		}

		public override bool Touch(GameplayObject target)
		{
			if (target != null)
			{
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
				if (this.damageAmount < 0f && target is ShipModule && (target as ShipModule).shield_power > 0f)
				{
					return false;
				}
				if (!this.DamageToggleOn && target is ShipModule)
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
				if (!this.InFrustumWhenFired )
				{
					target.Damage(this, this.damageAmount * 90f);
					this.Die(null, true);
				}
				else
				{
                    (target as ShipModule).Damage(this, this.damageAmount);
				}
			}
			return true;
		}

		public void Update(Vector2 srcCenter, Vector2 dstCenter, int Thickness, Matrix view, Matrix projection, float elapsedTime)
		{
			if (!this.collidedThisFrame && this.DamageToggleOn)
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
            this.Duration -= elapsedTime;
			this.Source = srcCenter;
			if (this.Target == null)
			{
				this.Destination = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(this.Source, this.owner.Rotation - this.BeamOffsetAngle, this.range);
			}
			else if (!this.Owner.CheckIfInsideFireArc(this.weapon, this.Target.Center, base.Owner.Rotation))
			{
				float angle = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(srcCenter, this.Target.Center));
				if (angle > base.Owner.Rotation + this.weapon.moduleAttachedTo.facing + MathHelper.ToRadians(this.weapon.moduleAttachedTo.FieldOfFire) / 2f)
				{
					angle = base.Owner.Rotation + this.weapon.moduleAttachedTo.facing + MathHelper.ToRadians(this.weapon.moduleAttachedTo.FieldOfFire) / 2f;
				}
				else if (angle < base.Owner.Rotation + this.weapon.moduleAttachedTo.facing - MathHelper.ToRadians(this.weapon.moduleAttachedTo.FieldOfFire) / 2f)
				{
					angle = base.Owner.Rotation + this.weapon.moduleAttachedTo.facing - MathHelper.ToRadians(this.weapon.moduleAttachedTo.FieldOfFire) / 2f;
				}
				this.Destination = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(srcCenter, angle, this.range);
			}
			else
			{
				this.Destination = this.Target.Center;
			}
			this.quadEffect.View = view;
			this.quadEffect.Projection = projection;
			Vector3[] points = HelperFunctions.BeamPoints(srcCenter, this.ActualHitDestination, (float)Thickness, new Vector2[4], 0, this.BeamZ);
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

		public void UpdateDroneBeam(Vector2 srcCenter, Vector2 dstCenter, int Thickness, Matrix view, Matrix projection, float elapsedTime)
		{
			if (!this.collidedThisFrame && this.DamageToggleOn)
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
			this.quadEffect.View = view;
			this.quadEffect.Projection = projection;
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
	}
}