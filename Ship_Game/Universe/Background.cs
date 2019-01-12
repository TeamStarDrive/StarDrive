using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class Background
    {
        struct Nebula
        {
            public Vector2 Position;
            public Vector2 Clearance;
        }

        Rectangle BkgRect = new Rectangle(0, 0, 15000, 15000);
        readonly Camera2D Camera = new Camera2D();
        readonly Array<Nebula> Nebulas = new Array<Nebula>();
        const int ItAmount = 512;

        public Background()
        {
            void AddNebula(int x, int y, SubTexture nebulaTex)
            {
                var nebula = new Nebula
                {
                    Position = new Vector2(RandomMath.RandomBetween(x, x + 256), RandomMath.RandomBetween(y, y + 256)),
                    Clearance = nebulaTex.SizeF
                };
                if (NebulaPosOk(nebula))
                    Nebulas.Add(nebula);
            }

            for (int x = 0; x < BkgRect.Width; x += ItAmount)
            {
                for (int y = 0; y < BkgRect.Height; y += ItAmount)
                {
                    AddNebula(x, y, ResourceManager.NebulaBigRandom());
                }
            }
            for (int x = 0; x < BkgRect.Width; x += ItAmount)
            {
                for (int y = 0; y < BkgRect.Height; y += ItAmount)
                {
                    AddNebula(x, y, ResourceManager.NebulaMedRandom());
                    AddNebula(x, y, ResourceManager.SmallNebulaRandom());
                }
            }
        }

        public void Draw(UniverseScreen universe, StarField starField)
        {
            Vector2 camPos = universe.CamPos.ToVec2();	    
            var blackRect = new Rectangle(0, 0, universe.ScreenWidth, universe.ScreenHeight);

            SpriteBatch batch = universe.ScreenManager.SpriteBatch;
            float width  = universe.ScreenWidth;
            float height = universe.ScreenHeight;

            batch.Begin();
            var c = new Color(255, 255, 255, 160);
            batch.FillRectangle(blackRect, Color.Black);// new Color(12, 17, 24));
            if (width > 2048)
                batch.Draw(ResourceManager.Texture("hqstarfield1"), blackRect, c);
            else
                batch.Draw(ResourceManager.Texture("hqstarfield1"), blackRect, blackRect, c);

            float percentX = camPos.X / 500000f;
            float percentY = camPos.Y / 500000f;
            float xDiff = blackRect.Width / 10f;
            float yDiff = blackRect.Height / 10f;
            Camera.Pos = new Vector2(percentX * xDiff, percentY * yDiff);

            starField.Draw(Camera.Pos, batch);
            batch.End();
            
            BkgRect = new Rectangle((int)(Camera.Pos.X - width  / 2f - Camera.Pos.X / 30f - 200f),
                                    (int)(Camera.Pos.Y - height / 2f - Camera.Pos.Y / 30f) - 200, 2048, 2048);
            if (width > 2048)
                BkgRect.Width = BkgRect.Height = 2600;

            batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, Camera.Transform);

            batch.Draw(ResourceManager.BigNebula(1), BkgRect, new Color(255, 255, 255, 60));
            batch.Draw(ResourceManager.BigNebula(3), BkgRect, new Color(255, 255, 255, 60));

            batch.End();
        }

        public void DrawGalaxyBackdrop(UniverseScreen u, StarField field)
        {
            SpriteBatch batch = u.ScreenManager.SpriteBatch;
            Viewport vp = u.Viewport;
            var blackRect = new Rectangle(0, 0, vp.Width, vp.Height);
            batch.Begin();
            batch.FillRectangle(blackRect, Color.Black);
            Vector3 upperLeft = vp.Project(Vector3.Zero, u.projection, u.view, Matrix.Identity);
            Vector3 lowerRight = vp.Project(new Vector3(u.UniverseSize, u.UniverseSize, 0f), u.projection, u.view, Matrix.Identity);
            vp.Project(new Vector3(u.UniverseSize / 2f, u.UniverseSize / 2f, 0f), u.projection, u.view, Matrix.Identity);
            var drawRect = new Rectangle((int)upperLeft.X, (int)upperLeft.Y, (int)lowerRight.X - (int)upperLeft.X, (int)lowerRight.Y - (int)upperLeft.Y);
            batch.Draw(ResourceManager.Texture("galaxy"), drawRect, Color.White);
            batch.End();
        }

        bool NebulaPosOk(Nebula neb)
        {
            foreach (Nebula nebula in Nebulas)
            {
                if (Math.Abs(nebula.Position.X - neb.Position.X) < (nebula.Clearance.X + neb.Clearance.X) && 
                    Math.Abs(nebula.Position.Y - neb.Position.Y) < (nebula.Clearance.Y + neb.Clearance.Y))
                    return false;
            }
            return true;
        }
    }
}