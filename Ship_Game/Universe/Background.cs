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
            RenderStates.BasicBlendMode(Universe.Device, additive: false, depthWrite: false);
            SR.Begin(Matrix.Identity);
            // with Matrix.Identity, screen spans from [-1.0; +1.0] in both X and Y
            SR.FillRect(new RectF(-1.01f, -1.01f, 2.02f, 2.02f), Color.Black); // fill the screen

            DrawBackgroundNebulaWithStars();

            RenderStates.BasicBlendMode(Universe.Device, additive: false, depthWrite: true);

            var cameraPos = new Vector2(
                (float)(Universe.CamPos.X / 500000.0) * (Universe.ScreenWidth / 10.0f),
                (float)(Universe.CamPos.Y / 500000.0) * (Universe.ScreenHeight / 10.0f)
            );

            // draw some extra colorful stars, scattered across the background
            StarField.Draw(cameraPos, batch);
        }

        void DrawBackgroundNebulaWithStars()
        {
            // distance of the nebula in the background
            // we can't actually set a huge constant distance due to view matrix limitations
            // so we set a constant distance from camera + relative rubber-band distance to give a fake sense of depth
            double constDistFromCamera = 10_000_000.0;
            double rubberBandDistance = 2_500_000.0 * (Universe.CamPos.Z / UniverseScreen.CAM_MAX);
            double backgroundDepth = -Universe.CamPos.Z + constDistFromCamera + rubberBandDistance;

            // inherit the universe camera pos by a certain fraction
            // this will make the background nebula move slightly with the camera
            double movementSensitivity = 0.05;

            var backgroundPos = new Vector3d(
                Universe.CamPos.X * (1.0 - movementSensitivity),
                Universe.CamPos.Y * (1.0 - movementSensitivity),
                backgroundDepth
            );

            double uSize = 20_000_000;

            SR.Begin(Universe.View, Universe.Projection);
            RenderStates.BasicBlendMode(Universe.Device, additive: false, depthWrite: false);

            Texture2D nebula = BackgroundNebula;
            if (nebula != null)
            {
                Vector2d nebulaSize = SubTexture.GetAspectFill(nebula.Width, nebula.Height, uSize);
                SR.Draw(nebula, backgroundPos, nebulaSize, Color.White);
            }

            RenderStates.BasicBlendMode(Universe.Device, additive: true, depthWrite: false);
            Texture2D stars = BackgroundStars;
            Vector2d starsSize = SubTexture.GetAspectFill(stars.Width, stars.Height, uSize);
            SR.Draw(stars, backgroundPos, starsSize, Color.White);
        }
    }
}
