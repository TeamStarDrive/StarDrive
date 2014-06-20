using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class Background
	{
		private Rectangle bgRect = new Rectangle(0, 0, 15000, 15000);

		private Camera2d cam = new Camera2d();

		private List<Background.Nebula> Nebulas = new List<Background.Nebula>();

		private int itAmount = 512;

		private Vector2 lastCamPos;

		public Background()
		{
			for (int x = 0; x < this.bgRect.Width; x = x + this.itAmount)
			{
				for (int y = 0; y < this.bgRect.Height; y = y + this.itAmount)
				{
					int BigIndex = (int)RandomMath.RandomBetween(0f, (float)ResourceManager.BigNebulas.Count + 0.75f);
					if (BigIndex > ResourceManager.BigNebulas.Count - 1)
					{
						BigIndex = ResourceManager.BigNebulas.Count - 1;
					}
					Background.Nebula n = new Background.Nebula();
					//{
                    n.Position = new Vector2(RandomMath.RandomBetween((float)x, (float)(x + 256)), RandomMath.RandomBetween((float)y, (float)(y + 256)));
						n.index = BigIndex;
						n.size = 3;
						n.xClearanceNeeded = ResourceManager.BigNebulas[n.index].Width / 2;
						n.yClearanceNeeded = ResourceManager.BigNebulas[n.index].Height / 2;
					//};
					if (this.NebulaPosOk(n))
					{
						this.Nebulas.Add(n);
					}
				}
			}
			for (int x = 0; x < this.bgRect.Width; x = x + this.itAmount)
			{
				for (int y = 0; y < this.bgRect.Height; y = y + this.itAmount)
				{
					int MedIndex = (int)RandomMath.RandomBetween(0f, (float)ResourceManager.MedNebulas.Count + 0.75f);
					if (MedIndex > ResourceManager.MedNebulas.Count - 1)
					{
						MedIndex = ResourceManager.MedNebulas.Count - 1;
					}
					Background.Nebula n = new Background.Nebula();
					n = new Background.Nebula()
					{
						Position = new Vector2(RandomMath.RandomBetween((float)x, (float)(x + 256)), RandomMath.RandomBetween((float)y, (float)(y + 256))),
						index = MedIndex,
						size = 1,
						xClearanceNeeded = ResourceManager.MedNebulas[n.index].Width / 2,
						yClearanceNeeded = ResourceManager.MedNebulas[n.index].Height / 2
					};
					if (this.NebulaPosOk(n))
					{
						this.Nebulas.Add(n);
					}
					int SmallIndex = (int)RandomMath.RandomBetween(0f, (float)ResourceManager.SmallNebulas.Count + 0.75f);
					if (SmallIndex > ResourceManager.SmallNebulas.Count - 1)
					{
						SmallIndex = ResourceManager.SmallNebulas.Count - 1;
					}
					n = new Background.Nebula();
					n = new Background.Nebula()
					{
						Position = new Vector2(RandomMath.RandomBetween((float)x, (float)(x + 256)), RandomMath.RandomBetween((float)y, (float)(y + 256))),
						index = SmallIndex,
						size = 1,
						xClearanceNeeded = ResourceManager.SmallNebulas[n.index].Width / 2,
						yClearanceNeeded = ResourceManager.SmallNebulas[n.index].Height / 2
					};
					if (this.NebulaPosOk(n))
					{
						this.Nebulas.Add(n);
					}
				}
			}
		}

		public void Draw(Vector2 camPos, Starfield starfield, Ship_Game.ScreenManager ScreenManager)
		{
			Vector2 vector2 = -1f * (camPos - this.lastCamPos);
			int width = ScreenManager.GraphicsDevice.Viewport.Width;
			Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
			Rectangle blackRect = new Rectangle(0, 0, width, viewport.Height);
			ScreenManager.SpriteBatch.Begin();
			Color c = new Color(255, 255, 255, 160);
			Primitives2D.FillRectangle(ScreenManager.SpriteBatch, blackRect, new Color(12, 17, 24));
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Textures/hqstarfield1"], blackRect, new Rectangle?(blackRect), c);
			Vector2 vector21 = new Vector2(camPos.X, camPos.Y);
			float percentX = camPos.X / 500000f;
			float percentY = camPos.Y / 500000f;
			float xDiff = (float)blackRect.Width / 10f;
			float yDiff = (float)blackRect.Height / 10f;
			float xPerc = percentX * xDiff;
			this.cam.Pos = new Vector2(xPerc, percentY * yDiff);
			starfield.Draw(this.cam.Pos, ScreenManager.SpriteBatch);
			ScreenManager.SpriteBatch.End();
			float x = this.cam.Pos.X;
			Viewport viewport1 = ScreenManager.GraphicsDevice.Viewport;
			Rectangle bgRect = new Rectangle((int)(x - (float)(viewport1.Width / 2) - this.cam.Pos.X / 30f - 200f), (int)(this.cam.Pos.Y - (float)(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2) - this.cam.Pos.Y / 30f) - 200, 2048, 2048);
			Vector2 vector22 = new Vector2(200f, 50f);
			ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, this.cam.get_transformation(ScreenManager.GraphicsDevice));
			ScreenManager.SpriteBatch.Draw(ResourceManager.BigNebulas[1], bgRect, new Color(255, 255, 255, 60));
			ScreenManager.SpriteBatch.Draw(ResourceManager.BigNebulas[3], bgRect, new Color(255, 255, 255, 60));
			float single = this.cam.Pos.X;
			Viewport viewport2 = ScreenManager.GraphicsDevice.Viewport;
			bgRect = new Rectangle((int)(single - (float)(viewport2.Width / 2) - this.cam.Pos.X / 15f - 200f), (int)(this.cam.Pos.Y - (float)(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2) - this.cam.Pos.Y / 15f) - 200, 2500, 2500);
			ScreenManager.SpriteBatch.End();
			this.lastCamPos = camPos;
		}

		public void Draw(UniverseScreen universe, Starfield starfield)
		{
			Vector2 camPos = new Vector2(universe.camPos.X, universe.camPos.Y);
			Vector2 vector2 = -1f * (camPos - this.lastCamPos);
			int width = universe.ScreenManager.GraphicsDevice.Viewport.Width;
			Viewport viewport = universe.ScreenManager.GraphicsDevice.Viewport;
			Rectangle blackRect = new Rectangle(0, 0, width, viewport.Height);
			universe.ScreenManager.SpriteBatch.Begin();
			Color c = new Color(255, 255, 255, 160);
			Primitives2D.FillRectangle(universe.ScreenManager.SpriteBatch, blackRect, new Color(12, 17, 24));
			if (universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth > 2048)
			{
				universe.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Textures/hqstarfield1"], blackRect, c);
			}
			else
			{
				universe.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Textures/hqstarfield1"], blackRect, new Rectangle?(blackRect), c);
			}
			Vector2 vector21 = new Vector2(camPos.X, camPos.Y);
			float percentX = camPos.X / 500000f;
			float percentY = camPos.Y / 500000f;
			float xDiff = (float)blackRect.Width / 10f;
			float yDiff = (float)blackRect.Height / 10f;
			float xPerc = percentX * xDiff;
			this.cam.Pos = new Vector2(xPerc, percentY * yDiff);
			starfield.Draw(this.cam.Pos, universe.ScreenManager.SpriteBatch);
			universe.ScreenManager.SpriteBatch.End();
			Rectangle bgRect = new Rectangle();
			if (universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth > 2048)
			{
				float x = this.cam.Pos.X;
				Viewport viewport1 = universe.ScreenManager.GraphicsDevice.Viewport;
				bgRect = new Rectangle((int)(x - (float)(viewport1.Width / 2) - this.cam.Pos.X / 30f - 200f), (int)(this.cam.Pos.Y - (float)(universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2) - this.cam.Pos.Y / 30f) - 200, 2600, 2600);
			}
			else
			{
				float single = this.cam.Pos.X;
				Viewport viewport2 = universe.ScreenManager.GraphicsDevice.Viewport;
				bgRect = new Rectangle((int)(single - (float)(viewport2.Width / 2) - this.cam.Pos.X / 30f - 200f), (int)(this.cam.Pos.Y - (float)(universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2) - this.cam.Pos.Y / 30f) - 200, 2048, 2048);
			}
			Vector2 vector22 = new Vector2(200f, 50f);
			universe.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, this.cam.get_transformation(universe.ScreenManager.GraphicsDevice));
			universe.ScreenManager.SpriteBatch.Draw(ResourceManager.BigNebulas[1], bgRect, new Color(255, 255, 255, 60));
			universe.ScreenManager.SpriteBatch.Draw(ResourceManager.BigNebulas[3], bgRect, new Color(255, 255, 255, 60));
			if (universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth > 2048)
			{
				float x1 = this.cam.Pos.X;
				Viewport viewport3 = universe.ScreenManager.GraphicsDevice.Viewport;
				bgRect = new Rectangle((int)(x1 - (float)(viewport3.Width / 2) - this.cam.Pos.X / 15f - 200f), (int)(this.cam.Pos.Y - (float)(universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2) - this.cam.Pos.Y / 15f) - 200, 2600, 2600);
			}
			else
			{
				float single1 = this.cam.Pos.X;
				Viewport viewport4 = universe.ScreenManager.GraphicsDevice.Viewport;
				bgRect = new Rectangle((int)(single1 - (float)(viewport4.Width / 2) - this.cam.Pos.X / 15f - 200f), (int)(this.cam.Pos.Y - (float)(universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2) - this.cam.Pos.Y / 15f) - 200, 2500, 2500);
			}
			universe.ScreenManager.SpriteBatch.End();
			this.lastCamPos = camPos;
		}

		public void Draw(GameScreen universe, Starfield starfield)
		{
			Vector2 camPos = Vector2.Zero;
			Vector2 vector2 = -1f * (camPos - this.lastCamPos);
			int width = universe.ScreenManager.GraphicsDevice.Viewport.Width;
			Viewport viewport = universe.ScreenManager.GraphicsDevice.Viewport;
			Rectangle blackRect = new Rectangle(0, 0, width, viewport.Height);
			universe.ScreenManager.SpriteBatch.Begin();
			Color c = new Color(255, 255, 255, 160);
			Primitives2D.FillRectangle(universe.ScreenManager.SpriteBatch, blackRect, new Color(12, 17, 24));
			universe.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Textures/hqstarfield1"], blackRect, new Rectangle?(blackRect), c);
			Vector2 vector21 = new Vector2(camPos.X, camPos.Y);
			float percentX = camPos.X / 500000f;
			float percentY = camPos.Y / 500000f;
			float xDiff = (float)blackRect.Width / 10f;
			float yDiff = (float)blackRect.Height / 10f;
			float xPerc = percentX * xDiff;
			this.cam.Pos = new Vector2(xPerc, percentY * yDiff);
			starfield.Draw(this.cam.Pos, universe.ScreenManager.SpriteBatch);
			universe.ScreenManager.SpriteBatch.End();
			float x = this.cam.Pos.X;
			Viewport viewport1 = universe.ScreenManager.GraphicsDevice.Viewport;
			Rectangle bgRect = new Rectangle((int)(x - (float)(viewport1.Width / 2) - this.cam.Pos.X / 30f - 200f), (int)(this.cam.Pos.Y - (float)(universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2) - this.cam.Pos.Y / 30f) - 200, 2048, 2048);
			Vector2 vector22 = new Vector2(200f, 50f);
			universe.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, this.cam.get_transformation(universe.ScreenManager.GraphicsDevice));
			universe.ScreenManager.SpriteBatch.Draw(ResourceManager.BigNebulas[1], bgRect, new Color(255, 255, 255, 60));
			universe.ScreenManager.SpriteBatch.Draw(ResourceManager.BigNebulas[3], bgRect, new Color(255, 255, 255, 60));
			float single = this.cam.Pos.X;
			Viewport viewport2 = universe.ScreenManager.GraphicsDevice.Viewport;
			bgRect = new Rectangle((int)(single - (float)(viewport2.Width / 2) - this.cam.Pos.X / 15f - 200f), (int)(this.cam.Pos.Y - (float)(universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2) - this.cam.Pos.Y / 15f) - 200, 2500, 2500);
			universe.ScreenManager.SpriteBatch.End();
		}

		public void DrawGalaxyBackdrop(UniverseScreen universe, Starfield starfield)
		{
			Vector2 camPos = new Vector2(universe.camPos.X, universe.camPos.Y);
			Vector2 vector2 = -1f * (camPos - this.lastCamPos);
			int width = universe.ScreenManager.GraphicsDevice.Viewport.Width;
			Viewport viewport = universe.ScreenManager.GraphicsDevice.Viewport;
			Rectangle blackRect = new Rectangle(0, 0, width, viewport.Height);
			Viewport viewport1 = universe.ScreenManager.GraphicsDevice.Viewport;
			Viewport viewport2 = universe.ScreenManager.GraphicsDevice.Viewport;
			Rectangle rectangle = new Rectangle(0, 0, viewport1.Width * 2, viewport2.Height * 2);
			universe.ScreenManager.SpriteBatch.Begin();
			Color color = new Color(255, 255, 255, 160);
			Primitives2D.FillRectangle(universe.ScreenManager.SpriteBatch, blackRect, Color.Black);
			Viewport viewport3 = universe.ScreenManager.GraphicsDevice.Viewport;
			Vector3 UpperLeft = viewport3.Project(Vector3.Zero, universe.projection, universe.view, Matrix.Identity);
			Viewport viewport4 = universe.ScreenManager.GraphicsDevice.Viewport;
			Vector3 LowerRight = viewport4.Project(new Vector3(universe.Size.X, universe.Size.Y, 0f), universe.projection, universe.view, Matrix.Identity);
			Viewport viewport5 = universe.ScreenManager.GraphicsDevice.Viewport;
			viewport5.Project(new Vector3(universe.Size.X / 2f, universe.Size.Y / 2f, 0f), universe.projection, universe.view, Matrix.Identity);
			Rectangle drawRect = new Rectangle((int)UpperLeft.X, (int)UpperLeft.Y, (int)LowerRight.X - (int)UpperLeft.X, (int)LowerRight.Y - (int)UpperLeft.Y);
			universe.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Textures/galaxy"], drawRect, Color.White);
			universe.ScreenManager.SpriteBatch.End();
			this.lastCamPos = camPos;
		}

		private bool NebulaPosOk(Background.Nebula neb)
        {
            foreach (Background.Nebula nebula in this.Nebulas)
            {
                if ((double)Math.Abs(nebula.Position.X - neb.Position.X) < (double)(nebula.xClearanceNeeded + neb.xClearanceNeeded) && (double)Math.Abs(nebula.Position.Y - neb.Position.Y) < (double)(nebula.yClearanceNeeded + neb.yClearanceNeeded))
                    return false;
            }
            return true;
        }

		private struct Nebula
		{
			public Vector2 Position;

			public int index;

			public int xClearanceNeeded;

			public int yClearanceNeeded;

			public int size;
		}
	}
}