using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class DimensionalPrison : Anomaly, IDisposable
	{
		public Vector2 p1;

		public Vector2 p2;

		public Vector2 p3;

		public string PlatformName = "Mysterious Platform";

		new public Vector2 Position;

		public string PrisonerID;

		private BackgroundItem Prison;

		private Beam b1;

		private Beam b2;

		private Beam b3;

		private Ship s1;

		private Ship s2;

		private Ship s3;

		private int numCreated;

		private int numToCreate = 9;

		private float timer;

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

		public DimensionalPrison(Vector2 Position)
		{
			this.p1 = Position + new Vector2(0f, -400f);
			this.p2 = Position + new Vector2(-400f, 400f);
			this.p3 = Position + new Vector2(400f, 400f);
			this.s1 = ResourceManager.CreateShipAtPoint(this.PlatformName, EmpireManager.GetEmpireByName("Unknown"), this.p1);
			this.s2 = ResourceManager.CreateShipAtPoint(this.PlatformName, EmpireManager.GetEmpireByName("Unknown"), this.p2);
			this.s3 = ResourceManager.CreateShipAtPoint(this.PlatformName, EmpireManager.GetEmpireByName("Unknown"), this.p3);
			this.Position = Position;
			Rectangle r = new Rectangle((int)Position.X - 200, (int)Position.Y - 200, 400, 400);
			this.Prison = new BackgroundItem();
			this.Prison.LoadContent(Anomaly.screen.ScreenManager, Anomaly.screen.view, Anomaly.screen.projection);
			this.Prison.UpperLeft = new Vector3((float)r.X, (float)r.Y, 0f);
			this.Prison.LowerLeft = this.Prison.UpperLeft + new Vector3(0f, (float)r.Height, 0f);
			this.Prison.UpperRight = this.Prison.UpperLeft + new Vector3((float)r.Width, 0f, 0f);
			this.Prison.LowerRight = this.Prison.UpperLeft + new Vector3((float)r.Width, (float)r.Height, 0f);
			this.Prison.Texture = ResourceManager.TextureDict["Textures/star_neutron"];
			this.Prison.FillVertices();
			this.b1 = new Beam(this.p1, Position, 50, this.s1)
			{
				weapon = ResourceManager.WeaponsDict["AncientRepulsor"]
			};
			this.b1.LoadContent(Anomaly.screen.ScreenManager, Anomaly.screen.view, Anomaly.screen.projection);
			this.s1.Beams.Add(this.b1);
			this.b1.infinite = true;
			this.b1.range = 2500f;
			this.b1.thickness = 75;
			this.b1.PowerCost = 0f;
			this.b1.damageAmount = 0f;
			this.b2 = new Beam(this.p2, Position, 50, this.s2)
			{
				weapon = ResourceManager.WeaponsDict["AncientRepulsor"]
			};
			this.b2.LoadContent(Anomaly.screen.ScreenManager, Anomaly.screen.view, Anomaly.screen.projection);
			this.b2.infinite = true;
			this.s2.Beams.Add(this.b2);
			this.b2.range = 2500f;
			this.b2.thickness = 75;
			this.b2.PowerCost = 0f;
			this.b2.damageAmount = 0f;
			this.b3 = new Beam(this.p3, Position, 50, this.s3)
			{
				weapon = ResourceManager.WeaponsDict["AncientRepulsor"]
			};
			this.b3.LoadContent(Anomaly.screen.ScreenManager, Anomaly.screen.view, Anomaly.screen.projection);
			this.b3.infinite = true;
			this.s3.Beams.Add(this.b3);
			this.b3.range = 2500f;
			this.b3.thickness = 75;
			this.b3.PowerCost = 0f;
			this.b3.damageAmount = 0f;
		}

		public override void Draw()
		{
			Anomaly.screen.ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
			Anomaly.screen.ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
			Anomaly.screen.ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = true;
			Anomaly.screen.ScreenManager.GraphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
			Anomaly.screen.ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
			Anomaly.screen.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
			Anomaly.screen.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;
			Anomaly.screen.ScreenManager.GraphicsDevice.RenderState.CullMode = CullMode.None;
			for (int i = 0; i < 20; i++)
			{
				Anomaly.screen.sparks.AddParticleThreadA(new Vector3(this.Position, 0f) + this.GenerateRandomWithin(100f), this.GenerateRandomWithin(25f));
			}
			if (RandomMath.RandomBetween(0f, 100f) > 97f)
			{
				Anomaly.screen.flash.AddParticleThreadA(new Vector3(this.Position, 0f), Vector3.Zero);
			}
			this.Prison.Draw(Anomaly.screen.ScreenManager, Anomaly.screen.view, Anomaly.screen.projection, 1f);
		}

		private Vector2 GenerateRandomV2(float radius)
		{
			return new Vector2(RandomMath.RandomBetween(-radius, radius), RandomMath.RandomBetween(-radius, radius));
		}

		private Vector3 GenerateRandomWithin(float radius)
		{
			return new Vector3(RandomMath.RandomBetween(-radius, radius), RandomMath.RandomBetween(-radius, radius), RandomMath.RandomBetween(-radius, radius));
		}

		public override void Update(float elapsedTime)
		{
			if (!this.s1.Active && !this.s2.Active && !this.s3.Active)
			{
				DimensionalPrison dimensionalPrison = this;
				dimensionalPrison.timer = dimensionalPrison.timer - elapsedTime;
				if (this.timer <= 0f)
				{
					Ship enemy = ResourceManager.CreateShipAtPoint("Heavy Drone", EmpireManager.GetEmpireByName("The Remnant"), this.Position);
					enemy.Velocity = this.GenerateRandomV2(100f);
					enemy.GetAI().State = AIState.AwaitingOrders;
					this.timer = 2f;
					DimensionalPrison dimensionalPrison1 = this;
					dimensionalPrison1.numCreated = dimensionalPrison1.numCreated + 1;
				}
				if (this.numCreated == this.numToCreate)
				{
					Anomaly.screen.anomalyManager.AnomaliesList.QueuePendingRemoval(this);
				}
			}
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.Prison != null)
                        this.Prison.Dispose();

                }
                this.Prison = null;
                this.disposed = true;
            }
        }
	}
}