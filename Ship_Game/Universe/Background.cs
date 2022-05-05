using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDGraphics.Sprites;
using Ship_Game.Graphics;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed class Background : IDisposable
    {
        StarField StarField;
        //Texture2D BackgroundTexture;
        readonly UniverseScreen Universe;

        public int Width => Universe.ScreenWidth;
        public int Height => Universe.ScreenHeight;
        public Rectangle ScreenRect => Universe.Rect;

        Texture2D BackgroundNebula;
        Texture2D BackgroundStars;

        SpriteRenderer SR;

        public Background(UniverseScreen universe, GraphicsDevice device)
        {
            Universe = universe;

            // support unit tests
            if (!ResourceManager.HasLoadedNebulae)
                return;

            SR = new SpriteRenderer(device);
            StarField = new StarField(universe);

            var nebulas = Dir.GetFiles("Content/Textures/BackgroundNebulas");
            if (nebulas.Length > 0)
            {
                int nebulaIdx = Universe.UState.BackgroundSeed % nebulas.Length;
                BackgroundNebula = universe.TransientContent.LoadTexture(nebulas[nebulaIdx]);
            }

            BackgroundStars = universe.TransientContent.LoadTexture(
                ResourceManager.GetModOrVanillaFile("Textures/hqstarfield1.dds")
            );

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
            SDUtils.Memory.Dispose(ref StarField);
            SDUtils.Memory.Dispose(ref BackgroundNebula);
            SDUtils.Memory.Dispose(ref BackgroundStars);
        }

        public void Draw(SpriteBatch batch)
        {
            var cameraPos = new Vector2(
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
            DrawBackgroundNebulaWithStars();

            RenderStates.BasicBlendMode(Universe.Device, additive: false, depthWrite: true);
            // draw some extra colorful stars, scattered across the background
            StarField.Draw(cameraPos, batch);
        }

        void DrawBackgroundNebulaWithStars()
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

            var backgroundPos = new SDGraphics.Vector3d(
                Universe.CamPos.X * (1.0 - movementSensitivity),
                Universe.CamPos.Y * (1.0 - movementSensitivity),
                backgroundDepth
            );

            double uSize = Universe.UState.Size * sizeMod;
            Vector2d nebulaSize = SubTexture.GetAspectFill(nebula.Width, nebula.Height, uSize);
            Vector2d starsSize = SubTexture.GetAspectFill(BackgroundStars.Width, BackgroundStars.Height, uSize);

            SR.Begin(Universe.View, Universe.Projection);
            SR.Draw(nebula, backgroundPos.ToVec3f(), nebulaSize.ToVec2f(), Color.White);
            SR.Draw(BackgroundStars, backgroundPos.ToVec3f(), starsSize.ToVec2f(), Color.White);
        }
    }
}