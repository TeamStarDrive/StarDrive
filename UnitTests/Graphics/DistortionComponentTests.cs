using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using Ship_Game.Data;

namespace UnitTests.Graphics;

/// <summary>
/// Phase 3.7 step 2: shield-hit distortion correctness signal. Renders a
/// regular grid pattern to a source RT, runs DistortionComponent with one
/// shield-hit source covering most of the frame, then asserts that:
///   1. With at least one active source, the destination differs from the
///      source — the shader actually warped sample reads.
///   2. With no active sources, the component is a no-op (destination
///      retains its prior contents because we skip Draw entirely).
///   3. The Distort.mgfxo loaded with the expected parameters.
///
/// Catches: missing .mgfxo sibling, broken parameter names, a degenerate
/// PS that returns the unwarped scene, and accidental "always run" that
/// blits the source even when no shields are hit.
/// </summary>
[TestClass]
public class DistortionComponentTests : StarDriveTest
{
    [TestMethod]
    public void Distort_WithActiveShield_WarpsSourceTexture()
    {
        GraphicsDevice device = Game.GraphicsDevice;
        GameContentManager content = StarDriveTestContext.Content;

        const int W = 128, H = 128;

        using var sourceRT = new RenderTarget2D(device, W, H, mipMap: false,
                                                SurfaceFormat.Color, DepthFormat.None);
        using var destRT   = new RenderTarget2D(device, W, H, mipMap: false,
                                                SurfaceFormat.Color, DepthFormat.None);

        // Paint a high-contrast grid into the source so any sub-pixel
        // sample shift produces a measurable color difference.
        using var white = new Texture2D(device, 1, 1);
        white.SetData(new[] { Color.White });

        RenderTargetBinding[] prevTargets = device.GetRenderTargets();
        try
        {
            device.SetRenderTarget(sourceRT);
            device.Clear(Color.Black);
            using (var batch = new SpriteBatch(device))
            {
                batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
                // 8-pixel wide white stripes alternating with black.
                for (int x = 0; x < W; x += 16)
                    batch.Draw(white, new Rectangle(x, 0, 8, H), Color.White);
                batch.End();
            }
        }
        finally
        {
            device.SetRenderTargets(prevTargets);
        }

        var srcPixels = new Color[W * H];
        sourceRT.GetData(srcPixels);

        using var distort = new DistortionComponent(device, content);
        distort.LoadContent();

        var fx = content.Load<Effect>("Effects/Distort");
        Assert.IsNotNull(fx.Parameters["ShieldData"],      "Missing ShieldData parameter — shader changed?");
        Assert.IsNotNull(fx.Parameters["Time"],            "Missing Time parameter");
        Assert.IsNotNull(fx.Parameters["MatrixTransform"], "Missing MatrixTransform — VS won't get clip-space projection");

        // Active source covering the whole frame, max intensity. Time chosen
        // so sin(...) is meaningfully non-zero across the disk.
        var sources = new List<DistortionComponent.DistortionSource>
        {
            new() { CenterUV = new Vector2(0.5f, 0.5f), RadiusUV = 0.6f, Intensity = 1.0f },
        };

        using (var batch = new SpriteBatch(device))
        {
            distort.Draw(batch, sourceRT, destRT, sources, time: 0.123f);
        }
        device.SetRenderTargets(prevTargets);

        var destPixels = new Color[W * H];
        destRT.GetData(destPixels);

        // Count pixels that differ between source and dest. With a 0.05*UV
        // max offset and 8px stripe width (~6.25% of W=128), most of the
        // disk's interior pixels should move across at least one stripe
        // boundary at some non-zero ripple-phase angle.
        int diff = 0;
        for (int i = 0; i < srcPixels.Length; i++)
            if (srcPixels[i] != destPixels[i]) diff++;

        Assert.IsTrue(diff > W * H / 50,
            $"Expected the distortion pass to warp >{W*H/50} pixels with one full-frame " +
            $"shield active, got {diff}. PS likely fell through to a no-op return — " +
            "check that ShieldData / Time uniforms are bound and the [unroll] loop " +
            "actually accumulates per-slot offsets.");
    }

    [TestMethod]
    public void Distort_NoActiveShields_IsNoOp()
    {
        GraphicsDevice device = Game.GraphicsDevice;
        GameContentManager content = StarDriveTestContext.Content;

        const int W = 64, H = 64;
        using var destRT = new RenderTarget2D(device, W, H, mipMap: false,
                                              SurfaceFormat.Color, DepthFormat.None);

        // Pre-paint the destination so we can detect any unwanted writes.
        var sentinel = new Color[W * H];
        for (int i = 0; i < sentinel.Length; i++) sentinel[i] = new Color(7, 11, 13, 255);
        destRT.SetData(sentinel);

        using var distort = new DistortionComponent(device, content);
        distort.LoadContent();

        // Use the dest as a stand-in source — the no-op path should not
        // sample it anyway, so this is safe and avoids extra setup.
        var noSources = new List<DistortionComponent.DistortionSource>();
        using (var batch = new SpriteBatch(device))
        {
            distort.Draw(batch, destRT, destRT, noSources, time: 0f);
        }

        var got = new Color[W * H];
        destRT.GetData(got);
        for (int i = 0; i < got.Length; i++)
        {
            if (got[i] != sentinel[i])
            {
                Assert.Fail($"Distort.Draw with empty sources clobbered the destination at pixel {i}: " +
                            $"expected {sentinel[i]}, got {got[i]}. The component must skip the pass entirely " +
                            "when there are no active shields.");
            }
        }
    }
}
