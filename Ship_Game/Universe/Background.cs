using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class Background
	{
		private Rectangle bgRect = new Rectangle(0, 0, 15000, 15000);

		private Camera2d cam = new Camera2d();

		private Array<Background.Nebula> Nebulas = new Array<Background.Nebula>();

		private int itAmount = 512;

		private Vector2 lastCamPos;

		public Background()
		{
			for (int x = 0; x < bgRect.Width; x = x + itAmount)
			{
				for (int y = 0; y < bgRect.Height; y = y + itAmount)
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
					if (NebulaPosOk(n))
					{
						Nebulas.Add(n);
					}
				}
			}
			for (int x = 0; x < bgRect.Width; x = x + itAmount)
			{
				for (int y = 0; y < bgRect.Height; y = y + itAmount)
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
					if (NebulaPosOk(n))
					{
						Nebulas.Add(n);
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
					if (NebulaPosOk(n))
					{
						Nebulas.Add(n);
					}
				}
			}
		}

		public void Draw(Vector2 camPos, Starfield starfield, Ship_Game.ScreenManager ScreenManager)
		{
			Rectangle blackRect = new Rectangle(0, 0, Empire.Universe.Viewport.Width, Empire.Universe.Viewport.Height);
			ScreenManager.SpriteBatch.Begin();
			var c = new Color(255, 255, 255, 160);
			ScreenManager.SpriteBatch.FillRectangle(blackRect, new Color(12, 17, 24));
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("hqstarfield1"), blackRect, blackRect, c);
			float percentX = camPos.X / 500000f;
			float percentY = camPos.Y / 500000f;
			float xDiff = (float)blackRect.Width / 10f;
			float yDiff = (float)blackRect.Height / 10f;
			float xPerc = percentX * xDiff;
			cam.Pos = new Vector2(xPerc, percentY * yDiff);
			starfield.Draw(cam.Pos, ScreenManager.SpriteBatch);
			ScreenManager.SpriteBatch.End();
			float x = cam.Pos.X;
			var bgRect = new Rectangle((int)(x - Empire.Universe.Viewport.Width / 2f - cam.Pos.X / 30f - 200f), 
                (int)(cam.Pos.Y - (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2f) - cam.Pos.Y / 30f) - 200, 2048, 2048);
			ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, cam.get_transformation(ScreenManager.GraphicsDevice));
			ScreenManager.SpriteBatch.Draw(ResourceManager.BigNebulas[1], bgRect, new Color(255, 255, 255, 60));
			ScreenManager.SpriteBatch.Draw(ResourceManager.BigNebulas[3], bgRect, new Color(255, 255, 255, 60));
			ScreenManager.SpriteBatch.End();
			lastCamPos = camPos;
		}

	    public void Draw(UniverseScreen universe, Starfield starfield)
	    {
            Vector2 camPos = universe.camPos.ToVec2();	        
	        int width = universe.Viewport.Width;
	        Viewport viewport = universe.Viewport;
	        Rectangle blackRect = new Rectangle(0, 0, width, viewport.Height);
	        universe.ScreenManager.SpriteBatch.Begin();
	        Color c = new Color(255, 255, 255, 160);
	        universe.ScreenManager.SpriteBatch.FillRectangle(blackRect, new Color(12, 17, 24));
	        if (universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth > 2048)
	            universe.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["hqstarfield1"], blackRect, c);
	        else
	            universe.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["hqstarfield1"], blackRect, blackRect,
	                c);
	        float percentX = camPos.X / 500000f;
	        float percentY = camPos.Y / 500000f;
	        float xDiff = blackRect.Width / 10f;
	        float yDiff = blackRect.Height / 10f;
	        float xPerc = percentX * xDiff;
	        cam.Pos = new Vector2(xPerc, percentY * yDiff);
	        starfield.Draw(cam.Pos, universe.ScreenManager.SpriteBatch);
	        universe.ScreenManager.SpriteBatch.End();
	        if (universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth > 2048)
	        {
	            float x = cam.Pos.X;
	            bgRect = new Rectangle((int) (x - universe.Viewport.Width / 2f - cam.Pos.X / 30f - 200f),
	                (int) (cam.Pos.Y - universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight /
	                       2f - cam.Pos.Y / 30f) - 200, 2600, 2600);
	        }
	        else
	        {
	            float single = cam.Pos.X;
	            bgRect = new Rectangle((int) (single - universe.Viewport.Width / 2f - cam.Pos.X / 30f - 200f),
	                (int) (cam.Pos.Y - universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight /
	                       2f - cam.Pos.Y / 30f) - 200, 2048, 2048);
	        }
	        universe.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate,
	            SaveStateMode.None, cam.get_transformation(universe.ScreenManager.GraphicsDevice));
	        universe.ScreenManager.SpriteBatch.Draw(ResourceManager.BigNebulas[1], bgRect, new Color(255, 255, 255, 60));
	        universe.ScreenManager.SpriteBatch.Draw(ResourceManager.BigNebulas[3], bgRect, new Color(255, 255, 255, 60));
	        if (universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth > 2048)
	        {
	            float x1 = cam.Pos.X;
	            Viewport viewport3 = universe.Viewport;
	            bgRect = new Rectangle((int) (x1 - universe.Viewport.Width / 2f - cam.Pos.X / 15f - 200f),
	                (int) (cam.Pos.Y - universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight /
	                       2f - cam.Pos.Y / 15f) - 200, 2600, 2600);
	        }
	        else
	        {
	            float single1 = cam.Pos.X;
	            Viewport viewport4 = universe.Viewport;
	            bgRect = new Rectangle((int) (single1 - viewport4.Width / 2 - cam.Pos.X / 15f - 200f),
	                (int) (cam.Pos.Y - universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight /
	                       2 - cam.Pos.Y / 15f) - 200, 2500, 2500);
	        }
	        universe.ScreenManager.SpriteBatch.End();
	        lastCamPos = camPos;
	    }

		public void Draw(GameScreen universe, Starfield starfield)
		{
			Vector2 camPos = Vector2.Zero;
			Vector2 vector2 = -1f * (camPos - lastCamPos);
			int width = universe.Viewport.Width;
			Viewport viewport = universe.Viewport;
			Rectangle blackRect = new Rectangle(0, 0, width, viewport.Height);
			universe.ScreenManager.SpriteBatch.Begin();
			Color c = new Color(255, 255, 255, 160);
			universe.ScreenManager.SpriteBatch.FillRectangle(blackRect, new Color(12, 17, 24));
			universe.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["hqstarfield1"], blackRect, blackRect, c);
			Vector2 vector21 = new Vector2(camPos.X, camPos.Y);
			float percentX = camPos.X / 500000f;
			float percentY = camPos.Y / 500000f;
			float xDiff = (float)blackRect.Width / 10f;
			float yDiff = (float)blackRect.Height / 10f;
			float xPerc = percentX * xDiff;
			cam.Pos = new Vector2(xPerc, percentY * yDiff);
			starfield.Draw(cam.Pos, universe.ScreenManager.SpriteBatch);
			universe.ScreenManager.SpriteBatch.End();
			float x = cam.Pos.X;
			Viewport viewport1 = universe.Viewport;
			Rectangle bgRect = new Rectangle((int)(x - (float)(viewport1.Width / 2) - cam.Pos.X / 30f - 200f), (int)(cam.Pos.Y - (float)(universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2) - cam.Pos.Y / 30f) - 200, 2048, 2048);
			Vector2 vector22 = new Vector2(200f, 50f);
			universe.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, cam.get_transformation(universe.ScreenManager.GraphicsDevice));
			universe.ScreenManager.SpriteBatch.Draw(ResourceManager.BigNebulas[1], bgRect, new Color(255, 255, 255, 60));
			universe.ScreenManager.SpriteBatch.Draw(ResourceManager.BigNebulas[3], bgRect, new Color(255, 255, 255, 60));
			float single = cam.Pos.X;
			Viewport viewport2 = universe.Viewport;
			bgRect = new Rectangle((int)(single - (float)(viewport2.Width / 2) - cam.Pos.X / 15f - 200f), (int)(cam.Pos.Y - (float)(universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2) - cam.Pos.Y / 15f) - 200, 2500, 2500);
			universe.ScreenManager.SpriteBatch.End();
		}

		public void DrawGalaxyBackdrop(UniverseScreen universe, Starfield starfield)
		{
			Vector2 camPos = new Vector2(universe.camPos.X, universe.camPos.Y);
			Vector2 vector2 = -1f * (camPos - lastCamPos);
			int width = universe.Viewport.Width;
			Viewport viewport = universe.Viewport;
			Rectangle blackRect = new Rectangle(0, 0, width, viewport.Height);
			Viewport viewport1 = universe.Viewport;
			Viewport viewport2 = universe.Viewport;
			Rectangle rectangle = new Rectangle(0, 0, viewport1.Width * 2, viewport2.Height * 2);
			universe.ScreenManager.SpriteBatch.Begin();
			Color color = new Color(255, 255, 255, 160);
			universe.ScreenManager.SpriteBatch.FillRectangle(blackRect, Color.Black);
			Viewport viewport3 = universe.Viewport;
            Vector3 UpperLeft = viewport3.Project(Vector3.Zero, universe.projection, universe.view, Matrix.Identity);
			Viewport viewport4 = universe.Viewport;
			Vector3 LowerRight = viewport4.Project(new Vector3(universe.UniverseRadius, universe.UniverseRadius, 0f), universe.projection, universe.view, Matrix.Identity);
			Viewport viewport5 = universe.Viewport;
			viewport5.Project(new Vector3(universe.UniverseRadius / 2f, universe.UniverseRadius / 2f, 0f), universe.projection, universe.view, Matrix.Identity);
			Rectangle drawRect = new Rectangle((int)UpperLeft.X, (int)UpperLeft.Y, (int)LowerRight.X - (int)UpperLeft.X, (int)LowerRight.Y - (int)UpperLeft.Y);
			universe.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["galaxy"], drawRect, Color.White);
			universe.ScreenManager.SpriteBatch.End();
			lastCamPos = camPos;
		}

		private bool NebulaPosOk(Background.Nebula neb)
        {
            foreach (Background.Nebula nebula in Nebulas)
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