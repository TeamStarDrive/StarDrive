using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class Background
    {
        private Rectangle BkgRect = new Rectangle(0, 0, 15000, 15000);
        private readonly Camera2D Camera = new Camera2D();
        private readonly Array<Nebula> Nebulas = new Array<Nebula>();
        private const int ItAmount = 512;
        private Vector2 LastCamPos;

        public Background()
        {
            for (int x = 0; x < BkgRect.Width; x += ItAmount)
            {
                for (int y = 0; y < BkgRect.Height; y += ItAmount)
                {
                    int bigIndex = RandomMath.InRange(ResourceManager.BigNebulae.Count);
                    var n = new Nebula
                    {
                        Position = new Vector2(RandomMath.RandomBetween(x, x + 256), RandomMath.RandomBetween(y, y + 256)),
                        index = bigIndex,
                        size  = 3,
                        xClearanceNeeded = ResourceManager.BigNebulae[bigIndex].Width / 2,
                        yClearanceNeeded = ResourceManager.BigNebulae[bigIndex].Height / 2
                    };
                    if (NebulaPosOk(n))
                        Nebulas.Add(n);
                }
            }
            for (int x = 0; x < BkgRect.Width; x += ItAmount)
            {
                for (int y = 0; y < BkgRect.Height; y += ItAmount)
                {
                    int medIndex = RandomMath.InRange(ResourceManager.MedNebulae.Count);
                    var n = new Nebula
                    {
                        Position = new Vector2(RandomMath.RandomBetween(x, x + 256), RandomMath.RandomBetween(y, y + 256)),
                        index = medIndex,
                        size = 1,
                        xClearanceNeeded = ResourceManager.MedNebulae[medIndex].Width / 2,
                        yClearanceNeeded = ResourceManager.MedNebulae[medIndex].Height / 2
                    };
                    if (NebulaPosOk(n))
                        Nebulas.Add(n);

                    int smallIndex = RandomMath.InRange(ResourceManager.SmallNebulae.Count);
                    n = new Nebula
                    {
                        Position = new Vector2(RandomMath.RandomBetween(x, x + 256), RandomMath.RandomBetween(y, y + 256)),
                        index = smallIndex,
                        size = 1,
                        xClearanceNeeded = ResourceManager.SmallNebulae[smallIndex].Width / 2,
                        yClearanceNeeded = ResourceManager.SmallNebulae[smallIndex].Height / 2
                    };
                    if (NebulaPosOk(n))
                    {
                        Nebulas.Add(n);
                    }
                }
            }
        }

        public void Draw(Vector2 camPos, Starfield starfield, ScreenManager screenMgr)
        {
            var blackRect = new Rectangle(0, 0, Empire.Universe.Viewport.Width, Empire.Universe.Viewport.Height);
            screenMgr.SpriteBatch.Begin();
            var c = new Color(255, 255, 255, 160);
            screenMgr.SpriteBatch.FillRectangle(blackRect, new Color(12, 17, 24));
            screenMgr.SpriteBatch.Draw(ResourceManager.Texture("hqstarfield1"), blackRect, blackRect, c);
            Camera.Pos = new Vector2((camPos.X / 500000f) * (blackRect.Width / 10f), 
                                     (camPos.Y / 500000f) * (blackRect.Height / 10f));
            starfield.Draw(Camera.Pos, screenMgr.SpriteBatch);
            screenMgr.SpriteBatch.End();
            var bgRect = new Rectangle((int)(Camera.Pos.X - Empire.Universe.Viewport.Width / 2f - Camera.Pos.X / 30f - 200f), 
                                       (int)(Camera.Pos.Y - (screenMgr.GraphicsDevice.PresentationParameters.BackBufferHeight / 2f) - Camera.Pos.Y / 30f) - 200, 2048, 2048);
            screenMgr.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, Camera.Transform);
            screenMgr.SpriteBatch.Draw(ResourceManager.BigNebulae[1], bgRect, new Color(255, 255, 255, 60));
            screenMgr.SpriteBatch.Draw(ResourceManager.BigNebulae[3], bgRect, new Color(255, 255, 255, 60));
            screenMgr.SpriteBatch.End();
            LastCamPos = camPos;
        }

        public void Draw(UniverseScreen universe, Starfield starfield)
        {
            Vector2 camPos = universe.CamPos.ToVec2();	    
            

            int width = universe.Viewport.Width;
            Viewport viewport = universe.Viewport;
            var blackRect = new Rectangle(0, 0, width, viewport.Height);

            var spriteBatch = universe.ScreenManager.SpriteBatch;

            universe.ScreenManager.SpriteBatch.Begin();
            var c = new Color(255, 255, 255, 160);
            universe.ScreenManager.SpriteBatch.FillRectangle(blackRect, Color.Black);// new Color(12, 17, 24));
            if (universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth > 2048)
                universe.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("hqstarfield1"), blackRect, c);
            else
                universe.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("hqstarfield1"), blackRect, blackRect, c);
            float percentX = camPos.X / 500000f;
            float percentY = camPos.Y / 500000f;
            float xDiff = blackRect.Width / 10f;
            float yDiff = blackRect.Height / 10f;
            float xPerc = percentX * xDiff;
            Camera.Pos = new Vector2(xPerc, percentY * yDiff);
            starfield.Draw(Camera.Pos, universe.ScreenManager.SpriteBatch);
            universe.ScreenManager.SpriteBatch.End();
            if (universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth > 2048)
            {
                float x = Camera.Pos.X;
                BkgRect = new Rectangle((int) (x - universe.Viewport.Width / 2f - Camera.Pos.X / 30f - 200f),
                    (int) (Camera.Pos.Y - universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight /
                           2f - Camera.Pos.Y / 30f) - 200, 2600, 2600);
            }
            else
            {
                float single = Camera.Pos.X;
                BkgRect = new Rectangle((int) (single - universe.Viewport.Width / 2f - Camera.Pos.X / 30f - 200f),
                    (int) (Camera.Pos.Y - universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight /
                           2f - Camera.Pos.Y / 30f) - 200, 2048, 2048);
            }
            universe.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate,
                SaveStateMode.None, Camera.Transform);
            universe.ScreenManager.SpriteBatch.Draw(ResourceManager.BigNebulae[1], BkgRect, new Color(255, 255, 255, 60));
            universe.ScreenManager.SpriteBatch.Draw(ResourceManager.BigNebulae[3], BkgRect, new Color(255, 255, 255, 60));
            if (universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth > 2048)
            {
                float x1 = Camera.Pos.X;
                BkgRect = new Rectangle((int) (x1 - universe.Viewport.Width / 2f - Camera.Pos.X / 15f - 200f),
                    (int) (Camera.Pos.Y - universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight /
                           2f - Camera.Pos.Y / 15f) - 200, 2600, 2600);
            }
            else
            {
                float single1 = Camera.Pos.X;
                Viewport viewport4 = universe.Viewport;
                BkgRect = new Rectangle((int) (single1 - viewport4.Width / 2 - Camera.Pos.X / 15f - 200f),
                    (int) (Camera.Pos.Y - universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight /
                           2 - Camera.Pos.Y / 15f) - 200, 2500, 2500);
            }
            universe.ScreenManager.SpriteBatch.End();
            LastCamPos = camPos;
        }

        public void Draw(GameScreen universe, Starfield starfield)
        {
            var blackRect = new Rectangle(0, 0, universe.Viewport.Width, universe.Viewport.Height);
            universe.ScreenManager.SpriteBatch.Begin();
            var c = new Color(255, 255, 255, 160);
            universe.ScreenManager.SpriteBatch.FillRectangle(blackRect, new Color(12, 17, 24));
            universe.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("hqstarfield1"), blackRect, blackRect, c);
            Camera.Pos = Vector2.Zero;
            starfield.Draw(Camera.Pos, universe.ScreenManager.SpriteBatch);
            universe.ScreenManager.SpriteBatch.End();
            var bgRect = new Rectangle((int)((universe.Viewport.Width / -2f) - 200f), 
                (int)(universe.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / -2f) - 200, 2048, 2048);
            universe.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, Camera.Transform);
            universe.ScreenManager.SpriteBatch.Draw(ResourceManager.BigNebulae[1], bgRect, new Color(255, 255, 255, 60));
            universe.ScreenManager.SpriteBatch.Draw(ResourceManager.BigNebulae[3], bgRect, new Color(255, 255, 255, 60));
            universe.ScreenManager.SpriteBatch.End();
        }

        public void DrawGalaxyBackdrop(UniverseScreen universe, Starfield starfield)
        {
            var camPos = new Vector2(universe.CamPos.X, universe.CamPos.Y);
            var blackRect = new Rectangle(0, 0, universe.Viewport.Width, universe.Viewport.Height);
            universe.ScreenManager.SpriteBatch.Begin();
            universe.ScreenManager.SpriteBatch.FillRectangle(blackRect, Color.Black);
            Vector3 upperLeft = universe.Viewport.Project(Vector3.Zero, universe.projection, universe.view, Matrix.Identity);
            Vector3 lowerRight = universe.Viewport.Project(new Vector3(universe.UniverseSize, universe.UniverseSize, 0f), universe.projection, universe.view, Matrix.Identity);
            universe.Viewport.Project(new Vector3(universe.UniverseSize / 2f, universe.UniverseSize / 2f, 0f), universe.projection, universe.view, Matrix.Identity);
            var drawRect = new Rectangle((int)upperLeft.X, (int)upperLeft.Y, (int)lowerRight.X - (int)upperLeft.X, (int)lowerRight.Y - (int)upperLeft.Y);
            universe.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("galaxy"), drawRect, Color.White);
            universe.ScreenManager.SpriteBatch.End();
            LastCamPos = camPos;
        }

        private bool NebulaPosOk(Nebula neb)
        {
            foreach (Nebula nebula in Nebulas)
            {
                if (Math.Abs(nebula.Position.X - neb.Position.X) < (nebula.xClearanceNeeded + neb.xClearanceNeeded) && 
                    Math.Abs(nebula.Position.Y - neb.Position.Y) < (nebula.yClearanceNeeded + neb.yClearanceNeeded))
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