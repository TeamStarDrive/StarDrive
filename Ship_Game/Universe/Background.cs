using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Graphics;

namespace Ship_Game
{
    public sealed class Background : IDisposable
    {
        struct Nebula
        {
            public Vector2 Position;
            public Vector2 Clearance;
        }

        RectF BkgRect = new RectF(0, 0, 15000, 15000);
        readonly Camera2D Camera = new Camera2D();
        readonly Array<Nebula> Nebulas = new Array<Nebula>();
        const int ItAmount = 512;

        StarField StarField;
        //Texture2D BackgroundTexture;

        public Background(UniverseScreen universe, GraphicsDevice device)
        {
            // support unit tests
            if (!ResourceManager.HasLoadedNebulae)
                return;

            StarField = new StarField(universe);

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

            for (int x = 0; x < (int)BkgRect.W; x += ItAmount)
            {
                for (int y = 0; y < (int)BkgRect.H; y += ItAmount)
                {
                    AddNebula(x, y, ResourceManager.NebulaBigRandom());
                }
            }
            for (int x = 0; x < (int)BkgRect.W; x += ItAmount)
            {
                for (int y = 0; y < (int)BkgRect.H; y += ItAmount)
                {
                    AddNebula(x, y, ResourceManager.NebulaMedRandom());
                    AddNebula(x, y, ResourceManager.SmallNebulaRandom());
                }
            }

            //using (RenderTarget2D rt = RenderTargets.Create(device))
            //{
            //    device.SetRenderTarget(0, rt);
            //    RenderStates.BasicBlendMode(device, additive:true, depthWrite:true);
            //    DrawBackgroundStars(universe, universe.ScreenManager.SpriteBatch);
            //    device.SetRenderTarget(0, null);

            //    BackgroundTexture = rt.GetTexture();
            //}
        }

        ~Background() { Destroy(); }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        void Destroy()
        {
            StarField?.Dispose(ref StarField);
        }

        void DrawBackgroundStars(UniverseScreen u, SpriteBatch batch)
        {
            var blackRect = new Rectangle(0, 0, u.ScreenWidth, u.ScreenHeight);

            // these are drawn with RenderState Additive
            batch.Begin();
            batch.FillRectangle(blackRect, Color.Black); // new Color(12, 17, 24));

            // dynamic 3d backgrop galaxy, it doesn't really work that well :(
            //float uSize = u.UniverseSize;
            //Viewport vp = u.Viewport;
            //Vector3 topLeft = vp.Project(new Vector3(-uSize * 1.5f, -uSize * 1.5f, 0f), u.Projection, u.View, Matrix.Identity);
            //Vector3 botRight = vp.Project(new Vector3(uSize * 1.5f, uSize * 1.5f, 0f), u.Projection, u.View, Matrix.Identity);
            //Rectangle galaxyRect = RectF.FromPoints(topLeft.X, botRight.X, topLeft.Y, botRight.Y);
            //batch.Draw(ResourceManager.Texture("Galaxy/galaxy3.dds"), galaxyRect, Color.White);

            // static background texture
            //if (true)
            //{
            //    var galaxy = ResourceManager.Texture("Galaxy/galaxy3.dds");
            //    float srcHeight = Math.Min(galaxy.Height, galaxy.Width / u.Viewport.AspectRatio);
            //    Rectangle srcRect = new RectF(0, 0, galaxy.Width, srcHeight);
            //    batch.Draw(galaxy, blackRect, srcRect, Color.White);
            //}

            var c = new Color(255, 255, 255, 160);
            if (u.ScreenWidth > 2048)
                batch.Draw(ResourceManager.Texture("hqstarfield1"), blackRect, c);
            else
                batch.Draw(ResourceManager.Texture("hqstarfield1"), blackRect, blackRect, c);
            batch.End();
        }

        public void Draw(UniverseScreen u, SpriteBatch batch)
        {
            RenderStates.BasicBlendMode(u.ScreenManager.GraphicsDevice, additive:false, depthWrite:true);

            float width  = u.ScreenWidth;
            float height = u.ScreenHeight;
            DrawBackgroundStars(u, batch);
            //batch.Begin();
            //batch.FillRectangle(new RectF(0, 0, width, height), Color.Black); // new Color(12, 17, 24));
            //batch.Draw(BackgroundTexture, new RectF(0, 0, width, height));
            //batch.End();

            Vector2 camPos = u.CamPos.ToVec2f();
            float percentX = camPos.X / 500000f;
            float percentY = camPos.Y / 500000f;
            float xDiff = width / 10f;
            float yDiff = height / 10f;
            Camera.Pos = new Vector2(percentX * xDiff, percentY * yDiff);
            // draw some extra stars
            StarField.Draw(Camera.Pos, batch);

            BkgRect = new RectF(Camera.Pos.X - width*0.4f  - (Camera.Pos.X / 20f) - 600f,
                                Camera.Pos.Y - height*0.4f - (Camera.Pos.Y / 20f) - 600f, 2048, 2048);
            if (width > 2048)
                BkgRect.W = BkgRect.H = 2600;

            ///// blends 3 main background nebulas, best if 1 of those 3 would be picked at random on game start
            ///// Drawing the BigNebula causes less visibility in the universe, often mistaking game stars(solar systems) at max zoom with start in the nebula texture.

            batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, Camera.Transform);
            batch.Draw(ResourceManager.BigNebula(1), BkgRect, new Color(255, 255, 255, 60));
            batch.Draw(ResourceManager.BigNebula(2), BkgRect, new Color(255, 255, 255, 160));
            //batch.Draw(ResourceManager.BigNebula(3), BkgRect, new Color(255, 255, 255, 90)); //dissabled to prevent muddiness
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