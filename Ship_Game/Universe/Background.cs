using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDGraphics.Rendering;
using SDGraphics.Sprites;
using SDUtils;
using Ship_Game.Graphics;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game;

public sealed class Background : IDisposable
{
    readonly UniverseScreen Universe;
    StarField StarField;
    Texture2D BackgroundNebula;
    Texture2D BackgroundStars;
        
    SubTexture CloudTex;
    SpriteShader SpriteCloudShader;
    EffectParameter SpriteCloudShaderPos;
    Vector2 LastCamPos;
    Vector2 CameraPos;
    Vector2 CloudPos;

    public Background(UniverseScreen universe, GraphicsDevice device)
    {
        Universe = universe;

        // support unit tests
        if (!ResourceManager.HasLoadedNebulae)
            return;

        StarField = new StarField(Universe.UState);

        var nebulas = Dir.GetFiles("Content/Textures/BackgroundNebulas");
        if (nebulas.Length > 0)
        {
            int nebulaIdx = Universe.UState.BackgroundSeed % nebulas.Length;
            BackgroundNebula = universe.TransientContent.LoadUncachedTexture(nebulas[nebulaIdx]);
        }

        BackgroundStars = universe.TransientContent.LoadUncachedTexture(
            ResourceManager.GetModOrVanillaFile("Textures/hqstarfield1.dds")
        );
            
        CloudTex = ResourceManager.Texture("clouds");
        FileInfo cloudsShader = ResourceManager.GetModOrVanillaFile("Effects/Clouds.fx");
        OnCloudShaderModified(cloudsShader);
        universe.ScreenManager.AddHotLoadTarget(universe, cloudsShader, OnCloudShaderModified);
    }

    ~Background() { Destroy(); }

    public void Dispose()
    {
        Destroy();
        GC.SuppressFinalize(this);
    }

    void Destroy()
    {
        StarField = null;
        Mem.Dispose(ref BackgroundNebula);
        Mem.Dispose(ref BackgroundStars);

        CloudTex = null;
        SpriteCloudShader = null;
        SpriteCloudShaderPos = null;
    }
        
    void OnCloudShaderModified(FileInfo shaderFile)
    {
        // free existing resource, this will force content manager to reload it
        SpriteCloudShader?.Shader.Dispose();

        // reload the shader
        SpriteCloudShader = new(Universe.TransientContent.LoadShader(shaderFile));
        SpriteCloudShaderPos = SpriteCloudShader.Shader["Position"];
    }

    public void Draw(SpriteRenderer sr)
    {
        RenderStates.BasicBlendMode(Universe.Device, additive: false, depthWrite: false);
        sr.Begin(Matrix.Identity);
        // with Matrix.Identity, screen spans from [-1.0; +1.0] in both X and Y
        sr.FillRect(new RectF(-1.01f, -1.01f, 2.02f, 2.02f), Color.Black); // fill the screen
        sr.End();

        DrawBackgroundNebulaWithStars(sr);

        // draw nebulous clouds overlay effect
        // the clouds scroll across the universe in two different layers
        // and complement the nebular colors
        LastCamPos = CameraPos;
        CameraPos = new(
            (float)(Universe.CamPos.X / 500000.0) * (Universe.ScreenWidth / 10.0f),
            (float)(Universe.CamPos.Y / 500000.0) * (Universe.ScreenHeight / 10.0f)
        );

        if (SpriteCloudShader != null)
        {
            // draw using a custom sprite shader
            sr.Begin(Universe.OrthographicProjection, SpriteCloudShader);
            RenderStates.BasicBlendMode(Universe.Device, additive: false, depthWrite: true);

            Vector2 movement = (CameraPos - LastCamPos);
            CloudPos += ((movement * 0.3f) * 2f);
            SpriteCloudShaderPos.SetValue(CloudPos);

            RectF rect = new(0, 0, Universe.ScreenWidth, Universe.ScreenHeight);

            // this allows us to tweak the cloud effect color tones
            Color filterColor = new(0, 255, 255, 255);
            sr.Draw(CloudTex, new Quad3D(rect, 0), filterColor);
            sr.End();
        }

        // draw some extra colorful stars, scattered across the background
        StarField.Draw(sr, Universe);
    }

    void DrawBackgroundNebulaWithStars(SpriteRenderer sr)
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

        Vector3d backgroundPos = new(
            Universe.CamPos.X * (1.0 - movementSensitivity),
            Universe.CamPos.Y * (1.0 - movementSensitivity),
            backgroundDepth
        );

        Texture2D nebula = BackgroundNebula;
        if (nebula != null)
        {
            sr.Begin(Universe.ViewProjection);
            RenderStates.BasicBlendMode(Universe.Device, additive: false, depthWrite: false);

            Vector2d nebulaSize = SubTexture.GetAspectFill(nebula.Width, nebula.Height, 20_000_000.0);
            sr.Draw(nebula, backgroundPos, nebulaSize, Color.White);
            sr.End();
        }

        sr.Begin(Universe.ViewProjection);
        RenderStates.BasicBlendMode(Universe.Device, additive: true, depthWrite: false);
        Texture2D stars = BackgroundStars;
        Vector2d starsSize = SubTexture.GetAspectFill(stars.Width, stars.Height, 12_000_000.0);

        // for stars we draw it twice, on the left and on the right side to fill the background
        Vector3d starsTopLeft = backgroundPos - new Vector3d(starsSize.X * 0.2, 0, 0);
        sr.Draw(stars, starsTopLeft, starsSize, Color.White);
        sr.Draw(stars, starsTopLeft + new Vector3d(starsSize.X, 0, 0), starsSize, Color.White);
        sr.End();
    }
}