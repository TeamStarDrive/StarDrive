using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using Ship_Game.Data;

namespace UnitTests.Graphics;

/// <summary>
/// Phase 3.7 step 3: BasicFogOfWar effect correctness signal. The PS sets
/// the destination alpha to LightsTexture.r; the host call site uses
/// AlphaBlend with a black-cleared back buffer so dark lights pixels
/// blend toward black (= fog) and bright pixels show full scene.
///
/// The test paints a checker-board lights mask (half white, half black),
/// draws a uniform-color scene through the effect onto a black RT, and
/// asserts:
///   1. Half the output pixels are bright (lights=255 → alpha=255 → scene shows).
///   2. The other half are dark (lights=0 → alpha=0 → black shows through).
///
/// Catches: missing .mgfxo sibling, broken parameter names, the manual
/// Pass.Apply()-after-Begin pattern that produced silent black on the
/// 2026-05-02 attempt, accidental swap of color/lights samplers.
/// </summary>
[TestClass]
public class FogOfWarTests : StarDriveTest
{
    [TestMethod]
    public void BasicFogOfWar_AlphaFromLightsRed_BlackShowsThroughDarkPixels()
    {
        GraphicsDevice device = Game.GraphicsDevice;
        GameContentManager content = StarDriveTestContext.Content;

        const int W = 64, H = 64;

        // Scene RT: uniform red. With AlphaBlend + alpha-from-lights-mask, the
        // visible half should remain red, the fogged half should become black.
        using var sceneRT  = new RenderTarget2D(device, W, H, mipMap: false,
                                                SurfaceFormat.Color, DepthFormat.None);
        using var destRT   = new RenderTarget2D(device, W, H, mipMap: false,
                                                SurfaceFormat.Color, DepthFormat.None);

        // Paint scene = red.
        var scenePixels = new Color[W * H];
        for (int i = 0; i < scenePixels.Length; i++) scenePixels[i] = Color.Red;
        sceneRT.SetData(scenePixels);

        // Lights mask as a plain Texture2D (more reliable than SetData on a
        // RenderTarget2D under MonoGame 3.8.1.303). Left half = white (visible),
        // right half = black (fog).
        using var lightsTex = new Texture2D(device, W, H);
        var lightsPixels = new Color[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                lightsPixels[y * W + x] = (x < W / 2) ? Color.White : Color.Black;
        lightsTex.SetData(lightsPixels);

        var fx = content.Load<Effect>("Effects/BasicFogOfWar");
        Assert.IsNotNull(fx, "BasicFogOfWar.mgfxo failed to load.");
        Assert.IsNotNull(fx.Parameters["LightsTexture"],   "Missing LightsTexture parameter");
        Assert.IsNotNull(fx.Parameters["MatrixTransform"], "Missing MatrixTransform — VS won't get clip-space projection");

        fx.Parameters["LightsTexture"].SetValue(lightsTex);
        Matrix.CreateOrthographicOffCenter(0, W, H, 0, 0, 1, out Matrix proj);
        fx.Parameters["MatrixTransform"].SetValue(proj);

        RenderTargetBinding[] prev = device.GetRenderTargets();
        device.SetRenderTarget(destRT);
        device.Clear(Color.Black);
        using (var batch = new SpriteBatch(device))
        {
            // Pass effect to Begin — manual Pass.Apply() after Begin produces
            // silent black under MGFX 3.8.1.303 / DX11. This test pins the
            // working pattern. NonPremultiplied (not AlphaBlend) because the
            // PS outputs straight non-premul alpha — see the call site comment
            // in UniverseScreen.DrawMainRTWithFogOfWarEffect.
            batch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied,
                        SamplerState.LinearClamp, DepthStencilState.None,
                        RasterizerState.CullNone, fx);
            batch.Draw((Texture2D)sceneRT, new Rectangle(0, 0, W, H), Color.White);
            batch.End();
        }
        device.SetRenderTargets(prev);

        var got = new Color[W * H];
        destRT.GetData(got);

        int redPixels   = 0; // visible half
        int blackPixels = 0; // fogged half
        for (int y = 0; y < H; y++)
        {
            // Sample the column-center to avoid the half-pixel boundary.
            Color l = got[y * W + W / 4];           // visible side
            Color r = got[y * W + W / 4 + W / 2];   // fogged side

            if (l.R > 100 && l.G < 50 && l.B < 50) redPixels++;
            if (r.R < 16 && r.G < 16 && r.B < 16)  blackPixels++;
        }

        Assert.IsTrue(redPixels >= H * 9 / 10,
            $"Expected >= {H*9/10} of {H} visible-side rows to remain ~red, got {redPixels}. " +
            "ColorSampler may not be bound to the scene RT (check that SpriteBatch.Draw passed " +
            "the scene texture as the s0 argument).");
        Assert.IsTrue(blackPixels >= H * 9 / 10,
            $"Expected >= {H*9/10} of {H} fogged-side rows to become black, got {blackPixels}. " +
            "Alpha-from-lights pipeline broken — either the LightsTexture parameter didn't bind, " +
            "or AlphaBlend isn't honoring the per-pixel alpha output.");
    }
}
