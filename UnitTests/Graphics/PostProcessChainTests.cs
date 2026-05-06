using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using Ship_Game.Data;

namespace UnitTests.Graphics;

/// <summary>
/// Phase 3.7 step 5: end-to-end post-process chain test. Exercises Bloom →
/// Distortion → BasicFogOfWar in the same order as
/// UniverseScreen.Draw.Universe.DrawUniverseScreen, on a synthetic scene plus
/// a half-visible / half-fogged lights mask, and verifies the composite
/// reflects each pass's contribution:
///
///   1. Bloom-side: the visible (lights=white) half retains bright energy from
///      the source scene + the bloom glow that BloomFilterTests pins on its
///      own. Catches a regression where any of the three passes accidentally
///      zeroes the chain.
///   2. Fog-side: the fogged (lights=black) half is forced to black by the
///      AlphaBlend composite of BasicFogOfWar — checks the LightsTexture
///      sampler binding survives the upstream RT swaps.
///   3. Distortion-active vs distortion-skipped: running the same input twice,
///      once with a full-frame shield source and once with no sources,
///      produces measurably different visible-side pixel patterns. Confirms
///      the distortion pass actually intercepts the chain rather than no-op
///      blitting.
///
/// Catches: chain ordering regressions (e.g. fog applied before bloom would
/// prevent dark-fog pixels from being bloomed brighter — accidentally
/// "correct"-looking under cursory inspection), but here we pin the production
/// order by mirroring it.
/// </summary>
[TestClass]
public class PostProcessChainTests : StarDriveTest
{
    [TestMethod]
    public void BloomDistortFog_FullChain_ProducesPerPassContributions()
    {
        GraphicsDevice device = Game.GraphicsDevice;
        GameContentManager content = StarDriveTestContext.Content;

        const int W = 128, H = 128;

        // Build the synthetic scene: dark background, a bright vertical bar
        // straddling x=W/4 (entirely on the visible half of the lights mask).
        // Bloom should glow it outward; fog should pass it through; distortion
        // should warp the bar's edge.
        using var sceneRT = new RenderTarget2D(device, W, H, mipMap: false,
                                               SurfaceFormat.Color, DepthFormat.None);
        using var bloomedRT   = new RenderTarget2D(device, W, H, mipMap: false,
                                                   SurfaceFormat.Color, DepthFormat.None);
        using var distortedRT = new RenderTarget2D(device, W, H, mipMap: false,
                                                   SurfaceFormat.Color, DepthFormat.None);
        using var finalRT     = new RenderTarget2D(device, W, H, mipMap: false,
                                                   SurfaceFormat.Color, DepthFormat.None);

        using var white = new Texture2D(device, 1, 1);
        white.SetData(new[] { Color.White });

        RenderTargetBinding[] prev = device.GetRenderTargets();
        try
        {
            device.SetRenderTarget(sceneRT);
            device.Clear(Color.Black);
            using var batch = new SpriteBatch(device);
            batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
            // 4-pixel-wide bright bar at x=W/4 (visible half).
            batch.Draw(white, new Rectangle(W / 4 - 2, 0, 4, H), Color.White);
            batch.End();
        }
        finally { device.SetRenderTargets(prev); }

        // Lights mask as a plain Texture2D (more reliable than SetData on a
        // RenderTarget2D under MonoGame 3.8.1.303). Left half = visible.
        using var lightsTex = new Texture2D(device, W, H);
        var lightsPixels = new Color[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                lightsPixels[y * W + x] = (x < W / 2) ? Color.White : Color.Black;
        lightsTex.SetData(lightsPixels);

        // Run the production chain twice — once with a distortion source
        // active, once without — to check that distortion contributes.
        Color[] withDistort   = RunChain(device, content, sceneRT, bloomedRT, distortedRT,
                                         finalRT, lightsTex, withShield: true);
        Color[] withoutDistort = RunChain(device, content, sceneRT, bloomedRT, distortedRT,
                                          finalRT, lightsTex, withShield: false);

        // Assertion 1: visible-side carries the bright-bar energy through the
        // chain; fogged-side is forced dark by the fog composite. Compare
        // total brightness across the two halves rather than per-row counts —
        // bloom-blur intensity and distortion warp can move energy around but
        // not vanish it on the visible side, while the fog AlphaBlend zeroes
        // it on the fogged side.
        long visBright = 0, fogBright = 0;
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W / 2; x++)
            {
                Color c = withDistort[y * W + x];
                visBright += c.R + c.G + c.B;
            }
            for (int x = W / 2; x < W; x++)
            {
                Color c = withDistort[y * W + x];
                fogBright += c.R + c.G + c.B;
            }
        }
        Assert.IsTrue(visBright > fogBright * 10,
            $"Expected visible-side brightness ({visBright}) to dominate fogged-side " +
            $"brightness ({fogBright}) by >10x after the bloom→distort→fog chain. The " +
            "scene's bright bar lives entirely in the visible half; if both halves come " +
            "out comparably bright, BasicFogOfWar isn't masking — its LightsTexture binding " +
            "was likely clobbered by the upstream Bloom/Distortion RT switches. If both " +
            "halves are comparably dark, an upstream pass zeroed the chain.");

        // Assertion 2: fogged side is essentially black. With LightsTexture.r=0
        // on the right half, BasicFogOfWar's AlphaBlend composite (PS sets
        // col.a = lights) leaves the cleared-black destination untouched.
        int fogDarkPixels = 0;
        int totalFogPixels = H * (W / 2);
        for (int y = 0; y < H; y++)
        {
            for (int x = W / 2; x < W; x++)
            {
                Color c = withDistort[y * W + x];
                if (c.R < 16 && c.G < 16 && c.B < 16) fogDarkPixels++;
            }
        }
        Assert.IsTrue(fogDarkPixels >= totalFogPixels * 9 / 10,
            $"Expected >= {totalFogPixels * 9 / 10} of {totalFogPixels} fogged-side pixels " +
            $"to be black, got {fogDarkPixels}.");

        // Assertion 3: distortion contributed measurable per-pixel difference
        // on the visible side (where the bright bar is). Sample a 32x32 box
        // centered on the bar at (W/4, H/2) and count differing pixels.
        int distortDelta = 0;
        for (int y = H / 2 - 16; y < H / 2 + 16; y++)
        for (int x = W / 4 - 16; x < W / 4 + 16; x++)
        {
            if (withDistort[y * W + x] != withoutDistort[y * W + x]) distortDelta++;
        }
        Assert.IsTrue(distortDelta > 32,
            $"Expected the distortion pass to perturb >32 pixels in a 32x32 window around " +
            $"the bright bar, got {distortDelta}. Distortion is likely a no-op — verify " +
            "ShieldData/Time uniforms bind and the shield source list is reaching the shader.");
    }

    static Color[] RunChain(GraphicsDevice device, GameContentManager content,
                            RenderTarget2D scene, RenderTarget2D bloomed,
                            RenderTarget2D distorted, RenderTarget2D final,
                            Texture2D lights, bool withShield)
    {
        const int W = 128, H = 128;
        RenderTargetBinding[] prev = device.GetRenderTargets();

        using var bloom = new BloomComponent(device, content);
        bloom.LoadContent();
        // "Soft" preset has BaseIntensity=1 + a non-zero bloom add, so the
        // visible bar's brightness survives end-to-end.
        bloom.Settings = BloomComponent.BloomSettings.PresetSettings[2];

        using var distort = new DistortionComponent(device, content);
        distort.LoadContent();

        using var batch = new SpriteBatch(device);

        // Pass 1: scene → bloomed
        bloom.Draw(batch, scene, bloomed);

        // Pass 2: bloomed → distorted
        var sources = withShield
            ? new List<DistortionComponent.DistortionSource>
              {
                  new() { CenterUV = new Vector2(0.25f, 0.5f), RadiusUV = 0.4f, Intensity = 1.0f },
              }
            : new List<DistortionComponent.DistortionSource>();

        if (sources.Count > 0)
        {
            distort.Draw(batch, bloomed, distorted, sources, time: 0.123f);
        }
        else
        {
            // Mirror UniverseScreen's "no shield → blit-through" path so the
            // chain output is comparable across runs.
            device.SetRenderTarget(distorted);
            device.Clear(Color.Black);
            batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
            batch.Draw((Texture2D)bloomed, new Rectangle(0, 0, W, H), Color.White);
            batch.End();
        }

        // Pass 3: distorted + lights → final, via BasicFogOfWar.
        var fx = content.Load<Effect>("Effects/BasicFogOfWar");
        fx.Parameters["LightsTexture"].SetValue(lights);
        Matrix.CreateOrthographicOffCenter(0, W, H, 0, 0, 1, out Matrix proj);
        fx.Parameters["MatrixTransform"].SetValue(proj);

        device.SetRenderTarget(final);
        device.Clear(Color.Black);
        batch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    RasterizerState.CullNone, fx);
        batch.Draw((Texture2D)distorted, new Rectangle(0, 0, W, H), Color.White);
        batch.End();

        device.SetRenderTargets(prev);

        var got = new Color[W * H];
        final.GetData(got);
        return got;
    }
}
