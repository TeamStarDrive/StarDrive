using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Graphics;

namespace Ship_Game
{
    public sealed class Background : IDisposable
    {
        readonly Camera2D Camera = new Camera2D();

        StarField StarField;
        //Texture2D BackgroundTexture;
        readonly UniverseScreen Universe;

        public int Width => Universe.ScreenWidth;
        public int Height => Universe.ScreenHeight;
        public Rectangle ScreenRect => Universe.Rect;

        public Background(UniverseScreen universe, GraphicsDevice device)
        {
            Universe = universe;

            // support unit tests
            if (!ResourceManager.HasLoadedNebulae)
                return;

            StarField = new StarField(universe);

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

        public void Draw(SpriteBatch batch)
        {
            Camera.Pos = new Vector2(
                (float)(Universe.CamPos.X / 500000.0) * (Width / 10.0f),
                (float)(Universe.CamPos.Y / 500000.0) * (Height / 10.0f)
            );

            RenderStates.BasicBlendMode(Universe.Device, additive:true, depthWrite:true);
            batch.Begin();
            {
                Color backgroundFill = new Color(12, 17, 24); // Color.Black; // 
                batch.FillRectangle(ScreenRect, backgroundFill);
            }
            batch.End();

            batch.Begin(SpriteBlendMode.Additive, SpriteSortMode.Immediate, SaveStateMode.SaveState);
            {
                DrawLargeBackgroundNebula(batch);
                DrawBackgroundStars(batch);
                //batch.Draw(BackgroundTexture, ScreenRect);
            }
            batch.End();

            RenderStates.BasicBlendMode(Universe.Device, additive: false, depthWrite: true);
            // draw some extra colorful stars, scattered across the background
            StarField.Draw(Camera.Pos, batch);
        }

        void DrawBackgroundStars(SpriteBatch batch)
        {
            var c = new Color(255, 255, 255, 160);
            if (Width > 2048)
                batch.Draw(ResourceManager.Texture("hqstarfield1"), ScreenRect, c);
            else
                batch.Draw(ResourceManager.Texture("hqstarfield1"), ScreenRect, ScreenRect, c);
        }

        void DrawLargeBackgroundNebula(SpriteBatch batch)
        {
            int nebulaIdx = Universe.UState.BackgroundSeed % ResourceManager.NumBigNebulae;
            SubTexture nebula = ResourceManager.BigNebula(nebulaIdx);

            // TODO: use camPos to create a nice parallax effect?
            Vector3d camPos = Universe.CamPos;

            float sizeMod = 8.0f;
            float backgroundDepth = 25_000_000f;
            float uSize = Universe.UState.Size;
            var backgroundSize = new Vector2(uSize * sizeMod);
            var backgroundPos = new Vector3(0.0f, 0.0f, backgroundDepth);

            float aspect = nebula.AspectRatio;
            if (aspect > 1.0f)
                backgroundSize.Y /= aspect;
            else
                backgroundSize.X *= aspect;

            (Vector2d pos, Vector2d size) = Universe.ProjectToScreenCoords(backgroundPos, backgroundSize);

            RectF screenRect = RectF.FromCenter(pos, size.X, size.Y);
            batch.Draw(nebula, screenRect, new Color(255,255,255,200));
        }
    }
}