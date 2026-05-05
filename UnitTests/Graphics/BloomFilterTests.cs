using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using Ship_Game.Data;

namespace UnitTests.Graphics;

/// <summary>
/// Phase 3.7 step 1: 4-pass bloom pipeline correctness signal. Renders a
/// small ring of bright pixels into a clear-black RT, runs BloomComponent
/// over it, and asserts that:
///   1. The bloom output's total brightness is strictly greater than the
///      source's (bloom adds energy by glowing bright pixels outward).
///   2. Pixels that were black-and-adjacent to the bright ring become
///      non-black (the blur stages spread bright energy spatially).
/// Catches: missing .mgfxo siblings, broken parameter names, an inverted
/// extract pass that drops bright pixels instead of keeping them, a
/// degenerate combine pass that returns only the base scene unmodified.
/// </summary>
[TestClass]
public class BloomFilterTests : StarDriveTest
{
    [TestMethod]
    public void RenderTexturedScene_BloomCombines_ProducesBrightening()
    {
        GraphicsDevice device = Game.GraphicsDevice;
        GameContentManager content = StarDriveTestContext.Content;

        const int W = 128, H = 128;

        // Source RT: clear black, paint a small bright cross in the middle.
        using var sourceRT = new RenderTarget2D(device, W, H, mipMap: false,
                                                SurfaceFormat.Color, DepthFormat.None);
        using var destRT   = new RenderTarget2D(device, W, H, mipMap: false,
                                                SurfaceFormat.Color, DepthFormat.None);

        // Use a 1x1 white texture and stretch it to draw the bright pixels.
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
                // 8x8 bright square at the center.
                batch.Draw(white, new Rectangle(W/2 - 4, H/2 - 4, 8, 8), Color.White);
                batch.End();
            }
        }
        finally
        {
            device.SetRenderTargets(prevTargets);
        }

        // Snapshot source pixels before running bloom.
        var sourcePixels = new Color[W * H];
        sourceRT.GetData(sourcePixels);
        long sourceBrightness = 0;
        foreach (Color c in sourcePixels) sourceBrightness += c.R + c.G + c.B;

        // Run the bloom pipeline.
        using var bloom = new BloomComponent(device, content);
        bloom.LoadContent();
        // Use a low threshold so the bright square actually exceeds it after blur.
        bloom.Settings = BloomComponent.BloomSettings.PresetSettings[2]; // "Soft" — threshold 0, blur 3

        // Sanity check: required parameters exist on the loaded .mgfxo
        // (catches shader-source / parameter-name regressions).
        var combine = content.Load<Microsoft.Xna.Framework.Graphics.Effect>("Effects/BloomCombine");
        Assert.IsNotNull(combine.Parameters["BaseTexture"],     "Missing BaseTexture parameter");
        Assert.IsNotNull(combine.Parameters["BloomIntensity"],  "Missing BloomIntensity");
        Assert.IsNotNull(combine.Parameters["MatrixTransform"], "Missing MatrixTransform — VS won't get a clip-space projection");

        using (var batch = new SpriteBatch(device))
        {
            bloom.Draw(batch, sourceRT, destRT);
        }
        // Restore whatever the harness expects.
        device.SetRenderTargets(prevTargets);

        var destPixels = new Color[W * H];
        destRT.GetData(destPixels);
        long destBrightness = 0;
        foreach (Color c in destPixels) destBrightness += c.R + c.G + c.B;

        // Assertion 1: output brightness > source brightness.
        // Bloom = base + bloom, where bloom is a non-negative blurred copy of
        // the bright pass. The "Soft" preset's BaseIntensity=1 keeps the base
        // at its original brightness, and the bloom pass adds energy on top.
        Assert.IsTrue(destBrightness > sourceBrightness,
            $"Expected bloom output brightness ({destBrightness}) to exceed " +
            $"source brightness ({sourceBrightness}). Bloom pipeline likely " +
            "did not add any glow — check that .mgfxo siblings loaded and " +
            "that BloomCombine's BaseTexture parameter binds correctly.");

        // Assertion 2: pixels just outside the original bright square should
        // now be non-black. The bright square was at [W/2-4..W/2+3]^2; sample
        // at offset 6 from center (i.e. 2 pixels outside the bright edge),
        // well within the 15-tap blur radius even with the conservative
        // BlurAmount=3 of the "Soft" preset.
        int ringOffset = 6;
        int ringHits = 0;
        int[] dx = { -ringOffset, ringOffset, 0, 0 };
        int[] dy = { 0, 0, -ringOffset, ringOffset };
        for (int i = 0; i < 4; i++)
        {
            int x = W/2 + dx[i], y = H/2 + dy[i];
            Color c = destPixels[y * W + x];
            if (c.R + c.G + c.B > 0) ringHits++;
        }
        Assert.IsTrue(ringHits >= 2,
            $"Expected at least 2 of 4 ring-sample pixels (offset {ringOffset} " +
            $"from center) to receive blurred bloom energy, got {ringHits}. Blur " +
            "passes may not be spreading energy spatially — check SampleOffsets " +
            "/ SampleWeights wiring.");
    }
}
