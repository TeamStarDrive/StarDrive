using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Particle3DSample
{
	public class ParticleSystem
	{
		private string settingsName;

		private ParticleSettings settings;

		private ContentManager content;

		private Effect particleEffect;

		private EffectParameter effectViewParameter;

		private EffectParameter effectProjectionParameter;

		private EffectParameter effectViewportHeightParameter;

		private EffectParameter effectTimeParameter;

		private ParticleVertex[] particles;

		private DynamicVertexBuffer vertexBuffer;

		private VertexDeclaration vertexDeclaration;

		private int firstActiveParticle;

		private int firstNewParticle;

		private int firstFreeParticle;

		private int firstRetiredParticle;

		private float currentTime;

		private int drawCounter;

		private static Random randomA;

		private static Random randomB;

		private Microsoft.Xna.Framework.Graphics.GraphicsDevice GraphicsDevice;

		static ParticleSystem()
		{
			ParticleSystem.randomA = new Random();
			ParticleSystem.randomB = new Random();
		}

		public ParticleSystem(Game game, ContentManager content, string settingsName, Microsoft.Xna.Framework.Graphics.GraphicsDevice gd)
		{
			this.GraphicsDevice = gd;
			this.content = content;
			this.settingsName = settingsName;
		}

		private void AddNewParticlesToVertexBuffer()
		{
			int stride = 32;
			if (this.firstNewParticle >= this.firstFreeParticle)
			{
				this.vertexBuffer.SetData<ParticleVertex>(this.firstNewParticle * stride, this.particles, this.firstNewParticle, (int)this.particles.Length - this.firstNewParticle, stride, SetDataOptions.NoOverwrite);
				if (this.firstFreeParticle > 0)
				{
					this.vertexBuffer.SetData<ParticleVertex>(0, this.particles, 0, this.firstFreeParticle, stride, SetDataOptions.NoOverwrite);
				}
			}
			else
			{
				this.vertexBuffer.SetData<ParticleVertex>(this.firstNewParticle * stride, this.particles, this.firstNewParticle, this.firstFreeParticle - this.firstNewParticle, stride, SetDataOptions.NoOverwrite);
			}
			this.firstNewParticle = this.firstFreeParticle;
		}

		public void AddParticleThreadA(Vector3 position, Vector3 velocity)
		{
			int nextFreeParticle = this.firstFreeParticle + 1;
			if (nextFreeParticle >= (int)this.particles.Length)
			{
				nextFreeParticle = 0;
			}
			if (nextFreeParticle == this.firstRetiredParticle)
			{
				return;
			}
			velocity = velocity * this.settings.EmitterVelocitySensitivity;
			float horizontalVelocity = MathHelper.Lerp(this.settings.MinHorizontalVelocity, this.settings.MaxHorizontalVelocity, (float)ParticleSystem.randomA.NextDouble());
			double horizontalAngle = ParticleSystem.randomA.NextDouble() * 6.28318548202515;
			velocity.X = velocity.X + horizontalVelocity * (float)Math.Cos(horizontalAngle);
			velocity.Z = velocity.Z + horizontalVelocity * (float)Math.Sin(horizontalAngle);
			velocity.Y = velocity.Y + MathHelper.Lerp(this.settings.MinVerticalVelocity, this.settings.MaxVerticalVelocity, (float)ParticleSystem.randomA.NextDouble());
			Color randomValues = new Color((byte)ParticleSystem.randomA.Next(255), (byte)ParticleSystem.randomA.Next(255), (byte)ParticleSystem.randomA.Next(255), (byte)ParticleSystem.randomA.Next(255));
			this.particles[this.firstFreeParticle].Position = position;
			this.particles[this.firstFreeParticle].Velocity = velocity;
			this.particles[this.firstFreeParticle].Random = randomValues;
			this.particles[this.firstFreeParticle].Time = this.currentTime;
			this.firstFreeParticle = nextFreeParticle;
		}

		public void AddParticleThreadB(Vector3 position, Vector3 velocity)
		{
			int nextFreeParticle = this.firstFreeParticle + 1;
			if (nextFreeParticle >= (int)this.particles.Length)
			{
				nextFreeParticle = 0;
			}
			if (nextFreeParticle == this.firstRetiredParticle)
			{
				return;
			}
			velocity = velocity * this.settings.EmitterVelocitySensitivity;
			float horizontalVelocity = MathHelper.Lerp(this.settings.MinHorizontalVelocity, this.settings.MaxHorizontalVelocity, (float)ParticleSystem.randomB.NextDouble());
			double horizontalAngle = ParticleSystem.randomB.NextDouble() * 6.28318548202515;
			velocity.X = velocity.X + horizontalVelocity * (float)Math.Cos(horizontalAngle);
			velocity.Z = velocity.Z + horizontalVelocity * (float)Math.Sin(horizontalAngle);
			velocity.Y = velocity.Y + MathHelper.Lerp(this.settings.MinVerticalVelocity, this.settings.MaxVerticalVelocity, (float)ParticleSystem.randomB.NextDouble());
			Color randomValues = new Color((byte)ParticleSystem.randomB.Next(255), (byte)ParticleSystem.randomB.Next(255), (byte)ParticleSystem.randomB.Next(255), (byte)ParticleSystem.randomB.Next(255));
			this.particles[this.firstFreeParticle].Position = position;
			this.particles[this.firstFreeParticle].Velocity = velocity;
			this.particles[this.firstFreeParticle].Random = randomValues;
			this.particles[this.firstFreeParticle].Time = this.currentTime;
			this.firstFreeParticle = nextFreeParticle;
		}

		public void Draw(GameTime gameTime)
		{
			Microsoft.Xna.Framework.Graphics.GraphicsDevice device = this.GraphicsDevice;
			if (this.vertexBuffer.IsContentLost)
			{
				this.vertexBuffer.SetData<ParticleVertex>(this.particles);
			}
			if (this.firstNewParticle != this.firstFreeParticle)
			{
				this.AddNewParticlesToVertexBuffer();
			}
			if (this.firstActiveParticle != this.firstFreeParticle)
			{
				this.SetParticleRenderStates(device.RenderState);
				this.effectViewportHeightParameter.SetValue(device.Viewport.Height);
				this.effectTimeParameter.SetValue(this.currentTime);
				device.Vertices[0].SetSource(this.vertexBuffer, 0, 32);
				device.VertexDeclaration = this.vertexDeclaration;
				this.particleEffect.Begin();
				foreach (EffectPass pass in this.particleEffect.CurrentTechnique.Passes)
				{
					pass.Begin();
					if (this.firstActiveParticle >= this.firstFreeParticle)
					{
						device.DrawPrimitives(PrimitiveType.PointList, this.firstActiveParticle, (int)this.particles.Length - this.firstActiveParticle);
						if (this.firstFreeParticle > 0)
						{
							device.DrawPrimitives(PrimitiveType.PointList, 0, this.firstFreeParticle);
						}
					}
					else
					{
						device.DrawPrimitives(PrimitiveType.PointList, this.firstActiveParticle, this.firstFreeParticle - this.firstActiveParticle);
					}
					pass.End();
				}
				this.particleEffect.End();
				device.RenderState.PointSpriteEnable = false;
				device.RenderState.DepthBufferWriteEnable = true;
			}
			ParticleSystem particleSystem = this;
			particleSystem.drawCounter = particleSystem.drawCounter + 1;
		}

		private void FreeRetiredParticles()
		{
			while (this.firstRetiredParticle != this.firstActiveParticle)
			{
				if (this.drawCounter - (int)this.particles[this.firstRetiredParticle].Time < 3)
				{
					return;
				}
				ParticleSystem particleSystem = this;
				particleSystem.firstRetiredParticle = particleSystem.firstRetiredParticle + 1;
				if (this.firstRetiredParticle < (int)this.particles.Length)
				{
					continue;
				}
				this.firstRetiredParticle = 0;
			}
		}

		public void LoadContent()
		{
			this.settings = this.content.Load<ParticleSettings>(this.settingsName);
			this.particles = new ParticleVertex[this.settings.MaxParticles];
			this.LoadParticleEffect();
			this.vertexDeclaration = new VertexDeclaration(this.GraphicsDevice, ParticleVertex.VertexElements);
			int size = 32 * (int)this.particles.Length;
			this.vertexBuffer = new DynamicVertexBuffer(this.GraphicsDevice, size, BufferUsage.WriteOnly | BufferUsage.Points);
		}

		private void LoadParticleEffect()
		{
			string techniqueName;
			Effect effect = this.content.Load<Effect>("3DParticles/ParticleEffect");
			this.particleEffect = effect.Clone(this.GraphicsDevice);
			EffectParameterCollection parameters = this.particleEffect.Parameters;
			this.effectViewParameter = parameters["View"];
			this.effectProjectionParameter = parameters["Projection"];
			this.effectViewportHeightParameter = parameters["ViewportHeight"];
			this.effectTimeParameter = parameters["CurrentTime"];
			parameters["Duration"].SetValue((float)this.settings.Duration.TotalSeconds);
			parameters["DurationRandomness"].SetValue(this.settings.DurationRandomness);
			parameters["Gravity"].SetValue(this.settings.Gravity);
			parameters["EndVelocity"].SetValue(this.settings.EndVelocity);
			parameters["MinColor"].SetValue(this.settings.MinColor.ToVector4());
			parameters["MaxColor"].SetValue(this.settings.MaxColor.ToVector4());
			parameters["RotateSpeed"].SetValue(new Vector2(this.settings.MinRotateSpeed, this.settings.MaxRotateSpeed));
			parameters["StartSize"].SetValue(new Vector2(this.settings.MinStartSize, this.settings.MaxStartSize));
			parameters["EndSize"].SetValue(new Vector2(this.settings.MinEndSize, this.settings.MaxEndSize));
			Texture2D texture = this.content.Load<Texture2D>(string.Concat("3DParticles/", this.settings.TextureName));
			parameters["Texture"].SetValue(texture);
			if ((float)this.settings.Duration.TotalSeconds != 6.66f)
			{
				techniqueName = (this.settings.MinRotateSpeed != 0f || this.settings.MaxRotateSpeed != 0f ? "RotatingParticles" : "NonRotatingParticles");
			}
			else
			{
				techniqueName = "StaticParticles";
			}
			this.particleEffect.CurrentTechnique = this.particleEffect.Techniques[techniqueName];
		}

		private void RetireActiveParticles()
		{
			float particleDuration = (float)this.settings.Duration.TotalSeconds;
			if (particleDuration == 6.66f)
			{
				return;
			}
			while (this.firstActiveParticle != this.firstNewParticle)
			{
				float particleAge = this.currentTime - this.particles[this.firstActiveParticle].Time;
				if (particleAge < particleDuration && particleAge > 0f)
				{
					return;
				}
				this.particles[this.firstActiveParticle].Time = (float)this.drawCounter;
				ParticleSystem particleSystem = this;
				particleSystem.firstActiveParticle = particleSystem.firstActiveParticle + 1;
				if (this.firstActiveParticle < (int)this.particles.Length)
				{
					continue;
				}
				this.firstActiveParticle = 0;
			}
		}

		public void SetCamera(Matrix view, Matrix projection)
		{
			this.effectViewParameter.SetValue(view);
			this.effectProjectionParameter.SetValue(projection);
		}

		private void SetParticleRenderStates(RenderState renderState)
		{
			renderState.PointSpriteEnable = true;
			renderState.PointSizeMax = 256f;
			renderState.AlphaBlendEnable = true;
			renderState.AlphaBlendOperation = BlendFunction.Add;
			renderState.SourceBlend = this.settings.SourceBlend;
			renderState.DestinationBlend = this.settings.DestinationBlend;
			renderState.AlphaTestEnable = true;
			renderState.AlphaFunction = CompareFunction.Greater;
			renderState.ReferenceAlpha = 0;
			renderState.DepthBufferEnable = true;
			renderState.DepthBufferWriteEnable = false;
		}

		public void UnloadContent()
		{
			this.particles = null;
			this.vertexDeclaration.Dispose();
			this.vertexBuffer.Dispose();
		}

		public void Update(GameTime gameTime)
		{
			ParticleSystem totalSeconds = this;
			totalSeconds.currentTime = totalSeconds.currentTime + (float)gameTime.ElapsedGameTime.TotalSeconds;
			this.RetireActiveParticles();
			this.FreeRetiredParticles();
			if (this.firstActiveParticle == this.firstFreeParticle)
			{
				this.currentTime = 0f;
			}
			if (this.firstRetiredParticle == this.firstActiveParticle)
			{
				this.drawCounter = 0;
			}
		}
	}
}