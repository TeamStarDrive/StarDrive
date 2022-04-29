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

        Texture2D BackgroundNebula;

        public Background(UniverseScreen universe, GraphicsDevice device)
        {
            Universe = universe;

            // support unit tests
            if (!ResourceManager.HasLoadedNebulae)
                return;

            StarField = new StarField(universe);

            var nebulas = Dir.GetFiles("Content/Textures/BackgroundNebulas");
            if (nebulas.Length > 0)
            {
                int nebulaIdx = Universe.UState.BackgroundSeed % nebulas.Length;
                BackgroundNebula = universe.TransientContent.LoadTexture(nebulas[nebulaIdx]);
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
            BackgroundNebula?.Dispose(ref BackgroundNebula);
        }

        public void Draw(SpriteBatch batch)
        {
            Camera.Pos = new Vector2(
                (float)(Universe.CamPos.X / 500000.0) * (Width / 10.0f),
                (float)(Universe.CamPos.Y / 500000.0) * (Height / 10.0f)
            );

            RenderStates.BasicBlendMode(Universe.Device, additive: false, depthWrite: true);
            batch.Begin();
            {
                Color backgroundFill = Color.Black; // new Color(12, 17, 24);
                batch.FillRectangle(ScreenRect, backgroundFill);
            }
            batch.End();

            RenderStates.BasicBlendMode(Universe.Device, additive: true, depthWrite: true);
            batch.Begin();
            {
                DrawBackgroundNebulaWithStars(batch);
                //batch.Draw(BackgroundTexture, ScreenRect);
            }
            batch.End();

            RenderStates.BasicBlendMode(Universe.Device, additive: false, depthWrite: true);
            // draw some extra colorful stars, scattered across the background
            StarField.Draw(Camera.Pos, batch);
        }

        void DrawBackgroundNebulaWithStars(SpriteBatch batch)
        {
            Texture2D nebula = BackgroundNebula;
            if (nebula == null)
                return;

            // scale of the background nebula
            double sizeMod = 8.0;

            // distance of the nebula in the background
            double backgroundDepth = 25_000_000;

            // inherit the universe camera pos by a certain fraction
            // this will make the background nebula move slightly with the camera
            double movementSensitivity = 0.05;

            var backgroundPos = new Vector3d(
                Universe.CamPos.X * (1.0 - movementSensitivity),
                Universe.CamPos.Y * (1.0 - movementSensitivity),
                backgroundDepth
            );

            double uSize = Universe.UState.Size * sizeMod;
            Vector2d nebulaSize = SubTexture.GetAspectFill(nebula.Width, nebula.Height, uSize);

            RectF nebulaScreenRect = Universe.ProjectToScreenRectF(backgroundPos, nebulaSize);
            batch.Draw(nebula, nebulaScreenRect);

            var starsTex = ResourceManager.Texture("hqstarfield1");
            Vector2d starsSize = SubTexture.GetAspectFill(starsTex.Width, starsTex.Height, uSize);

            RectF starsScreenRect = Universe.ProjectToScreenRectF(backgroundPos, starsSize);
            batch.Draw(starsTex, starsScreenRect, Color.White);
        }
    }
}