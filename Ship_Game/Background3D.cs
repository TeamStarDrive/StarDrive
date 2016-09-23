using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class Background3D
	{
		private UniverseScreen screen;

		public List<BackgroundItem> BGItems = new List<BackgroundItem>();

		private List<BackgroundItem> NonAdditiveList = new List<BackgroundItem>();

		public Background3D(UniverseScreen screen)
		{
			this.screen = screen;
            int nebsize = (int)RandomMath.RandomBetween(screen.Size.X / 8f, screen.Size.X);
            if (nebsize < 1000000) nebsize = 1000000;
			this.CreateRandomLargeNebula(new Rectangle(
                                (int)RandomMath.RandomBetween(-screen.Size.X * 1.5f, 0),
                                (int)RandomMath.RandomBetween(-screen.Size.Y * 1.5f, 0),
                                nebsize, nebsize)); //Updated to take full advantage of the bigger maps -Gretman
			for (int i = 0; i < 4 + (int)(this.screen.Size.X / 2000000) ; i++)     //And add more nedulas and stuff
			{
				this.CreateRandomSmallObject();
			}
		}

		private void CreateBGItem(Rectangle r, float zdepth, ref BackgroundItem neb)
		{
			neb.UpperLeft = new Vector3((float)r.X, (float)r.Y, zdepth);
			neb.LowerLeft = neb.UpperLeft + new Vector3(0f, (float)r.Height, 0f);
			neb.UpperRight = neb.UpperLeft + new Vector3((float)r.Width, 0f, 0f);
			neb.LowerRight = neb.UpperLeft + new Vector3((float)r.Width, (float)r.Height, 0f);
			neb.FillVertices();
		}

		private void CreateBlackClouds()
		{
			BackgroundItem BigNeb_1 = new BackgroundItem()
			{
				Texture = ResourceManager.TextureDict["hqspace/neb_black_1"]
			};
			Vector2 nebUpperLeft = Vector2.Zero;
			float zPos = 50000f;
			float xSize = RandomMath.RandomBetween(1000000f, 1100000f);
			float ySize = (float)(BigNeb_1.Texture.Height / BigNeb_1.Texture.Width) * xSize;
			BigNeb_1.UpperLeft = new Vector3(nebUpperLeft, zPos);
			BigNeb_1.LowerLeft = BigNeb_1.UpperLeft + new Vector3(0f, ySize, 0f);
			BigNeb_1.UpperRight = BigNeb_1.UpperLeft + new Vector3(xSize, 0f, 0f);
			BigNeb_1.LowerRight = BigNeb_1.UpperLeft + new Vector3(xSize, ySize, 0f);
			BigNeb_1.FillVertices();
			BigNeb_1.LoadContent(this.screen.ScreenManager, this.screen.view, this.screen.projection);
			this.NonAdditiveList.Add(BigNeb_1);
		}

		private void CreateRandomLargeNebula(Rectangle r)
		{
			float startz = 3500000f;
			float zPos = 3500000f;
			Rectangle r1 = r;
            //r1.X = r1.X + 1500000;
            r1.X = r1.X + (int)RandomMath.RandomBetween(0, this.screen.Size.X * 1.5f);       //Added some random here, so the nebulas are more varied -Gretman
            //r1.Y = r1.Y + 1000000;
            r1.Y = r1.Y + (int)RandomMath.RandomBetween(0, this.screen.Size.Y * 1.5f);
            //r1.Width = r1.Width - 2500000;
            //r1.Height = r1.Height - 2500000;
            //float scaler = RandomMath.RandomBetween(1, this.screen.Size.X / 1000000);
            //r1.Width = (int)((float)r1.Width * scaler);
            //r1.Height = (int)((float)r1.Height * scaler);
            BackgroundItem pointy = new BackgroundItem()
			{
				Texture = ResourceManager.TextureDict["hqspace/neb_pointy"]
			};
			this.CreateBGItem(r1, zPos, ref pointy);
			pointy.LoadContent(this.screen.ScreenManager, this.screen.view, this.screen.projection);
			this.BGItems.Add(pointy);
            //zPos = zPos + 400000f;
            zPos = zPos - RandomMath.RandomBetween(200000, 600000);
            pointy = new BackgroundItem()
			{
				Texture = ResourceManager.BigNebulas[2]
			};
			this.CreateBGItem(r1, zPos, ref pointy);
			pointy.LoadContent(this.screen.ScreenManager, this.screen.view, this.screen.projection);
			this.BGItems.Add(pointy);
            //r1.X = r1.X - 750000;
            //r1.Y = r1.Y + 500000;
            //zPos = zPos - 400000f;
            //r1.X = r1.X - (int)RandomMath.RandomBetween(400000, 750000);
            //r1.Y = r1.Y + (int)RandomMath.RandomBetween(250000, 500000);
            zPos = zPos - RandomMath.RandomBetween(200000, 600000);
            BackgroundItem floaty = new BackgroundItem()
			{
				Texture = ResourceManager.TextureDict["hqspace/neb_floaty"]
			};
			this.CreateBGItem(r1, zPos, ref floaty);
			floaty.LoadContent(this.screen.ScreenManager, this.screen.view, this.screen.projection);
			this.BGItems.Add(floaty);
			BackgroundItem BigNeb_1 = new BackgroundItem()
			{
				Texture = ResourceManager.MedNebulas[0]
			};
			this.CreateBGItem(r1, zPos, ref BigNeb_1);
			BigNeb_1.LoadContent(this.screen.ScreenManager, this.screen.view, this.screen.projection);
			this.BGItems.Add(BigNeb_1);
            //zPos = zPos + 500000f;
            zPos = zPos + RandomMath.RandomBetween(250000, 800000);

            BackgroundItem BigNeb_2 = new BackgroundItem()
			{
				Texture = ResourceManager.MedNebulas[1]
			};
			this.CreateBGItem(r1, zPos, ref BigNeb_2);
			BigNeb_2.LoadContent(this.screen.ScreenManager, this.screen.view, this.screen.projection);
			this.BGItems.Add(BigNeb_2);
			//zPos = zPos + 500000f;
            zPos = zPos + RandomMath.RandomBetween(250000, 800000);
            BackgroundItem BigNeb_3 = new BackgroundItem()
			{
				Texture = ResourceManager.BigNebulas[1]
			};
			this.CreateBGItem(r1, zPos, ref BigNeb_3);
			BigNeb_3.LoadContent(this.screen.ScreenManager, this.screen.view, this.screen.projection);
			this.BGItems.Add(BigNeb_3);
			for (int i = 0; i < 50; i++)
			{
				BackgroundItem BigNeb_4 = new BackgroundItem()
				{
					Texture = ResourceManager.TextureDict["Textures/smoke"]
				};
				float rw = RandomMath.RandomBetween(150000f, 800000f);
				Rectangle b = new Rectangle((int)RandomMath.RandomBetween((float)r1.X + (float)r1.Width * 0.2f, (float)r1.X + (float)r1.Width * 0.6f), (int)RandomMath.RandomBetween((float)r1.Y + (float)r1.Height * 0.2f, (float)r1.Y + (float)r1.Height * 0.6f), (int)rw, (int)rw);
                float zed = RandomMath.RandomBetween(startz + 200000f, zPos + 250000f);
                this.CreateBGItem(b, zed, ref BigNeb_4);
				BigNeb_4.LoadContent(this.screen.ScreenManager, this.screen.view, this.screen.projection);
				this.BGItems.Add(BigNeb_4);
				this.screen.star_particles.AddParticleThreadB(new Vector3((float)(b.X + b.Width / 2), (float)(b.Y + b.Height / 2), zed), Vector3.Zero);
			}
		}

		private void CreateRandomSmallObject()
		{
			BackgroundItem BigNeb_1 = new BackgroundItem()
			{
				Texture = ResourceManager.SmallNebulas[(int)RandomMath.RandomBetween(0f, (float)ResourceManager.SmallNebulas.Count)]
			};
			Vector2 nebUpperLeft = new Vector2(RandomMath.RandomBetween(-this.screen.Size.X * 1.5f, this.screen.Size.X * 0.75f), RandomMath.RandomBetween(-this.screen.Size.Y * 1.5f, this.screen.Size.Y * 0.75f));   //More Random Here -Gretman
			//float zPos = (float)RandomMath.RandomBetween(200000f, 2500000f);
			//float xSize = RandomMath.RandomBetween(800000f, 1800000f);
            float zPos = RandomMath.RandomBetween(100000f, 5000000f);
            float xSize = RandomMath.RandomBetween(800000f, this.screen.Size.X * 0.75f);
            float ySize = (float)(BigNeb_1.Texture.Height / BigNeb_1.Texture.Width) * xSize;
			BigNeb_1.UpperLeft = new Vector3(nebUpperLeft, zPos);
			BigNeb_1.LowerLeft = BigNeb_1.UpperLeft + new Vector3(0f, ySize, 0f);
			BigNeb_1.UpperRight = BigNeb_1.UpperLeft + new Vector3(xSize, 0f, 0f);
			BigNeb_1.LowerRight = BigNeb_1.UpperLeft + new Vector3(xSize, ySize, 0f);
			BigNeb_1.FillVertices();
			BigNeb_1.LoadContent(this.screen.ScreenManager, this.screen.view, this.screen.projection);
			this.BGItems.Add(BigNeb_1);

			BackgroundItem BigNeb_2 = new BackgroundItem()
			{
				Texture = ResourceManager.SmallNebulas[(int)RandomMath.RandomBetween(0f, (float)ResourceManager.SmallNebulas.Count)]
			};
            //zPos = zPos + 200000f;
            zPos = zPos + RandomMath.RandomBetween(100000f, 400000f);
            //xSize = RandomMath.RandomBetween(800000f, 1800000f);
            xSize = RandomMath.RandomBetween(800000f, this.screen.Size.X * 0.75f);
            ySize = (float)(BigNeb_2.Texture.Height / BigNeb_2.Texture.Width) * xSize;
			BigNeb_2.UpperLeft = new Vector3(nebUpperLeft, zPos);
			BigNeb_2.LowerLeft = BigNeb_2.UpperLeft + new Vector3(0f, ySize, 0f);
			BigNeb_2.UpperRight = BigNeb_2.UpperLeft + new Vector3(xSize, 0f, 0f);
			BigNeb_2.LowerRight = BigNeb_2.UpperLeft + new Vector3(xSize, ySize, 0f);
			BigNeb_2.FillVertices();
			BigNeb_2.LoadContent(this.screen.ScreenManager, this.screen.view, this.screen.projection);
			this.BGItems.Add(BigNeb_2);

			BackgroundItem BigNeb_3 = new BackgroundItem()
			{
				Texture = ResourceManager.SmallNebulas[(int)RandomMath.RandomBetween(0f, (float)ResourceManager.SmallNebulas.Count)]
			};
			//zPos = zPos + 200000f;
            zPos = zPos + RandomMath.RandomBetween(100000f, 400000f);
            //xSize = RandomMath.RandomBetween(800000f, 1800000f);
            xSize = RandomMath.RandomBetween(800000f, this.screen.Size.X * 0.75f);
            ySize = (float)(BigNeb_3.Texture.Height / BigNeb_3.Texture.Width) * xSize;
			BigNeb_3.UpperLeft = new Vector3(nebUpperLeft, zPos);
			BigNeb_3.LowerLeft = BigNeb_3.UpperLeft + new Vector3(0f, ySize, 0f);
			BigNeb_3.UpperRight = BigNeb_3.UpperLeft + new Vector3(xSize, 0f, 0f);
			BigNeb_3.LowerRight = BigNeb_3.UpperLeft + new Vector3(xSize, ySize, 0f);
			BigNeb_3.FillVertices();
			BigNeb_3.LoadContent(this.screen.ScreenManager, this.screen.view, this.screen.projection);
			this.BGItems.Add(BigNeb_3);
		}

		public void Draw()
		{
			this.screen.ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
			this.screen.ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
			this.screen.ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = true;
			this.screen.ScreenManager.GraphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
			this.screen.ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
			this.screen.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
			this.screen.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;
			this.screen.ScreenManager.GraphicsDevice.RenderState.CullMode = CullMode.None;
			foreach (BackgroundItem bgi in this.BGItems)
			{
				float zpos = bgi.LowerRight.Z;
				if (zpos >= 0f)
				{
					float alpha = (this.screen.camHeight + zpos) / 8000000f;
					if (alpha > 0.4f)
					{
						alpha = 0.4f;
					}
					if (alpha < 0f)
					{
						alpha = 0f;
					}
					bgi.Draw(this.screen.ScreenManager, this.screen.view, this.screen.projection, alpha);
				}
				else
				{
					zpos = zpos * -1f;
					float alpha = (this.screen.camHeight - zpos) / 8000000f;
					if (alpha > 0.8f)
					{
						alpha = 0.8f;
					}
					if (alpha < 0f)
					{
						alpha = 0f;
					}
					bgi.Draw(this.screen.ScreenManager, this.screen.view, this.screen.projection, alpha);
				}
			}
			this.screen.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
			foreach (BackgroundItem bgi in this.NonAdditiveList)
			{
				bgi.Draw(this.screen.ScreenManager, this.screen.view, this.screen.projection, 1f);
			}
		}
	}
}