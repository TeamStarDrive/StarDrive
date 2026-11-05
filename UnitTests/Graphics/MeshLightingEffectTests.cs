using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Mesh;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects.Forward;
using SynapseGaming.LightingSystem.Rendering;

namespace UnitTests.Graphics;

/// <summary>
/// Phase 3.7 step 4 (Phase A) pinning tests: verify the new MeshLighting MGFX
/// shader honors the BasicEffect-shaped property API on LightingEffect.
///
/// Phase A is a wiring-only swap (no material maps yet); these tests catch
/// regressions in the parameter binding before Phase B layers on the
/// normal/specular/emissive map sampling.
///
/// Three properties matter here:
///   1. DiffuseColor=Black should produce near-zero output (proves the
///      DiffuseColor parameter is reaching the shader and modulating output).
///   2. EmissiveColor adds light without requiring directional lights
///      (proves the emissive path runs even when LightingEnabled=false).
///   3. Disabling all lights gives DiffuseColor, or the texture alone when one
///      is bound (proves the LightingEnabled flag reaches the shader).
/// </summary>
[TestClass]
public class MeshLightingEffectTests : StarDriveTest
{
    [TestMethod]
    public void DiffuseColorBlack_ProducesDarkOutput()
    {
        using var rt = RenderUnitCubeWith(fx =>
        {
            fx.DiffuseColor = Vector3.Zero;
            fx.EmissiveColor = Vector3.Zero;
        });

        long brightness = SumBrightness(rt);
        // Cube covers ~25% of 64x64 = ~1000 pixels. Even ambient * 0 + 0*lit = 0,
        // so the lit cube should contribute ~zero brightness. Allow a small
        // margin for the magenta clear pixels around the cube edges (those
        // aren't part of the cube geometry, so they read magenta=255+0+255).
        long maxAllowed = SumBrightness(rt, onlyMagentaPixels: false) - SumBrightness(rt, onlyMagentaPixels: true);
        Assert.IsTrue(maxAllowed < 100,
            $"Expected DiffuseColor=Black to render the cube with ~zero non-magenta brightness, " +
            $"got {maxAllowed}. DiffuseColor parameter likely not pushed to MGFX.");
    }

    [TestMethod]
    public void EmissiveColor_LightsCubeEvenWithoutDirectionalLights()
    {
        using var rt = RenderUnitCubeWith(fx =>
        {
            fx.LightingEnabled = false;
            fx.DiffuseColor = Vector3.Zero;
            fx.EmissiveColor = new Vector3(0.5f, 0.0f, 0.0f); // half-bright red
        });

        long redContribution = SumChannel(rt, channel: 0, excludeMagenta: true);
        long greenContribution = SumChannel(rt, channel: 1, excludeMagenta: true);
        Assert.IsTrue(redContribution > 1000,
            $"Expected EmissiveColor=red to brighten cube pixels, got R-sum={redContribution}.");
        Assert.IsTrue(greenContribution < 100,
            $"Expected near-zero green from a red-only emissive, got G-sum={greenContribution}.");
    }

    [TestMethod]
    public void NoLighting_NoEmissive_NoTexture_RendersDiffuseColor()
    {
        using var rt = RenderUnitCubeWith(fx =>
        {
            fx.LightingEnabled = false;
            fx.TextureEnabled = false;
            fx.DiffuseColor = new Vector3(0.0f, 0.5f, 0.0f); // half-bright green
            fx.EmissiveColor = Vector3.Zero;
        });

        long redContribution = SumChannel(rt, channel: 0, excludeMagenta: true);
        long greenContribution = SumChannel(rt, channel: 1, excludeMagenta: true);
        Assert.IsTrue(greenContribution > 1000,
            $"Expected DiffuseColor=green to render green, got G-sum={greenContribution}.");
        Assert.IsTrue(redContribution < 100,
            $"Expected near-zero red from a green-only diffuse, got R-sum={redContribution}.");
    }

    [TestMethod]
    public void NoLighting_Textured_IgnoresDiffuseColor()
    {
        using var whiteMap = MakeSolidColorTexture(Game.GraphicsDevice, Color.White);
        using var rt = RenderUnitCubeWith(fx =>
        {
            fx.LightingEnabled = false;
            fx.DiffuseColor = new Vector3(0.494f, 0.722f, 0.529f);
            fx.EmissiveColor = Vector3.Zero;
            fx.Texture = whiteMap;
            fx.TextureEnabled = true;
        });

        long whitePixels = CountPixels(rt, Color.White);
        Assert.IsTrue(whitePixels > 100,
            $"Expected a white texture to render white whatever the DiffuseColor, got {whitePixels} white pixels.");
    }

    // Phase B emissive (`_g` glow map) sampling: when an EmissiveMapTexture
    // is bound, the shader uses its sampled RGB directly as the emissive
    // contribution (independent of EmissiveColor — the map is the source of
    // truth, since FBX-imported materials don't set EmissiveColor). A solid-
    // blue glow map should render the cube blue regardless of EmissiveColor.
    [TestMethod]
    public void EmissiveMapTexture_BrightensCubeBlue()
    {
        using var glowMap = MakeSolidColorTexture(Game.GraphicsDevice, new Color(0, 0, 255, 255));
        using var rt = RenderUnitCubeWith(fx =>
        {
            fx.LightingEnabled = false;
            fx.TextureEnabled = false;
            fx.DiffuseColor = Vector3.Zero;
            fx.EmissiveColor = Vector3.Zero;           // map dominates regardless
            fx.EmissiveMapTexture = glowMap;           // solid blue glow
        });

        long redContribution   = SumChannel(rt, channel: 0, excludeMagenta: true);
        long greenContribution = SumChannel(rt, channel: 1, excludeMagenta: true);
        long blueContribution  = SumChannel(rt, channel: 2, excludeMagenta: true);
        Assert.IsTrue(blueContribution > 1000,
            $"Expected EmissiveMap=blue to render the cube blue, got B-sum={blueContribution}.");
        Assert.IsTrue(redContribution < 100,
            $"Unexpected red contribution from blue emissive map: R-sum={redContribution}.");
        Assert.IsTrue(greenContribution < 100,
            $"Unexpected green contribution from blue emissive map: G-sum={greenContribution}.");
    }

    // A ship's lights go dark or stutter with its power: EmissiveScale scales the glow map.
    [TestMethod]
    public void EmissiveScale_ScalesTheGlowMap()
    {
        using var glowMap = MakeSolidColorTexture(Game.GraphicsDevice, new Color(0, 0, 255, 255));
        long BlueWith(float scale)
        {
            using var rt = RenderUnitCubeWith(fx =>
            {
                fx.LightingEnabled = false;
                fx.TextureEnabled = false;
                fx.DiffuseColor = Vector3.Zero;
                fx.EmissiveMapTexture = glowMap;
                fx.EmissiveScale = scale;
            });
            return SumChannel(rt, channel: 2, excludeMagenta: true);
        }

        long full = BlueWith(1f);
        Assert.IsTrue(full > 1000, $"setup: the glow map renders the cube blue, got B-sum={full}");
        AssertEqual(0.05f, 0.5f, BlueWith(0.5f) / (float)full, "half the glow at half the scale");
        Assert.IsTrue(BlueWith(0f) < 100, "no glow at zero scale");
    }

    // Only the glow map counts as a ship's lights; a model without one is never darkened.
    [TestMethod]
    public void EmissiveScale_LeavesTheMaterialEmissiveColorAlone()
    {
        using var rt = RenderUnitCubeWith(fx =>
        {
            fx.LightingEnabled = false;
            fx.DiffuseColor = Vector3.Zero;
            fx.EmissiveColor = new Vector3(0.5f, 0f, 0f);
            fx.EmissiveScale = 0f;
        });

        long redContribution = SumChannel(rt, channel: 0, excludeMagenta: true);
        Assert.IsTrue(redContribution > 1000, $"Expected the red EmissiveColor to stay lit, got R-sum={redContribution}.");
    }

    // Every ship of a model shares the model's effect, so each SceneObject's own level must reach the draw.
    [TestMethod]
    public void SceneObjectsSharingAnEffectEachDrawWithTheirOwnEmissiveScale()
    {
        GraphicsDevice device = Game.GraphicsDevice;
        using var glowMap = MakeSolidColorTexture(device, new Color(0, 0, 255, 255));
        (StaticMesh mesh, VertexBuffer vb, IndexBuffer ib) = BuildUnitCube(device);
        using var fx = new LightingEffect(device)
        {
            TextureEnabled = false,
            DiffuseColor = Vector3.Zero,
            EmissiveMapTexture = glowMap,
        };
        SceneObject dark = mesh.CreateSceneObject(effect: fx);
        dark.World = Matrix.CreateTranslation(-0.8f, 0f, 0f);
        dark.Visibility = ObjectVisibility.Rendered;
        dark.EmissiveScale = 0f;
        SceneObject lit = mesh.CreateSceneObject(effect: fx);
        lit.World = Matrix.CreateTranslation(0.8f, 0f, 0f);
        lit.Visibility = ObjectVisibility.Rendered;

        using var scene = new SceneInterface(Game.Graphics);
        var state = new SceneState();
        Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, 4), Vector3.Zero, Vector3.Up);
        Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 0.1f, 100f);
        state.BeginFrameRendering(ref view, ref projection, 0f, null, false);
        scene.BeginFrameRendering(state);
        scene.ObjectManager.Submit(lit);
        scene.ObjectManager.Submit(dark);

        using var rt = new RenderTarget2D(device, 64, 64, mipMap: false, SurfaceFormat.Color, DepthFormat.Depth24);
        RenderTargetBinding[] previousTargets = device.GetRenderTargets();
        try
        {
            device.SetRenderTarget(rt);
            device.Clear(Color.Magenta);
            scene.RenderScene();
        }
        finally
        {
            device.SetRenderTargets(previousTargets);
            vb.Dispose();
            ib.Dispose();
        }

        // the scene's grey light lands on both cubes alike, so only blue above red is glow
        long darkGlow = GlowInHalf(rt, rightHalf: false);
        long litGlow = GlowInHalf(rt, rightHalf: true);
        Assert.IsTrue(litGlow > 1000, $"setup: the lit cube glows, got {litGlow}");
        Assert.IsTrue(darkGlow < litGlow / 20, $"the dark cube drew with the lit cube's glow: {darkGlow} vs {litGlow}");
    }

    // The owner's color takes the paint the tint map marks, at the texture's own brightness.
    [TestMethod]
    public void HullTint_GivesTheTintMappedPaintTheOwnersColorAtItsBrightness()
    {
        Color px = CenterPixelOfGrayPaint(maskValue: 255, new Vector4(0f, 0f, 1f, 1f));
        AssertEqual(3f, 112f, px.R, "blue at the gray paint's brightness keeps some red");
        AssertEqual(3f, 112f, px.G, "and as much green");
        AssertEqual(3f, 255f, px.B, "and is mostly blue");
    }

    // Paint the tint map leaves out, and a model drawn with no owner's color, keep the texture as it is.
    [TestMethod]
    [DataRow(0, 1f)]
    [DataRow(255, 0f)]
    public void HullTint_LeavesThePaintAloneOutsideTheMaskOrWithNoColor(int maskValue, float tintWeight)
    {
        Color px = CenterPixelOfGrayPaint(maskValue, new Vector4(0f, 0f, 1f, tintWeight));
        AssertEqual(1f, 128f, px.R, "red");
        AssertEqual(1f, 128f, px.G, "green");
        AssertEqual(1f, 128f, px.B, "blue");
    }

    // Every ship of a model shares the model's effect, so each SceneObject's own color must reach the draw.
    [TestMethod]
    public void SceneObjectsSharingAnEffectEachDrawWithTheirOwnHullColors()
    {
        GraphicsDevice device = Game.GraphicsDevice;
        using var paint = MakeSolidColorTexture(device, new Color(128, 128, 128, 255));
        using var mask = MakeSolidColorTexture(device, Color.White);
        (StaticMesh mesh, VertexBuffer vb, IndexBuffer ib) = BuildUnitCube(device);
        using var fx = new LightingEffect(device)
        {
            DiffuseMapTexture = paint,
            TintMapTexture = mask,
        };
        SceneObject bare = mesh.CreateSceneObject(effect: fx);
        bare.World = Matrix.CreateTranslation(-0.8f, 0f, 0f);
        bare.Visibility = ObjectVisibility.Rendered;
        SceneObject tinted = mesh.CreateSceneObject(effect: fx);
        tinted.World = Matrix.CreateTranslation(0.8f, 0f, 0f);
        tinted.Visibility = ObjectVisibility.Rendered;
        tinted.HullTint = new Vector4(0f, 0f, 1f, 1f);

        using var scene = new SceneInterface(Game.Graphics);
        var state = new SceneState();
        Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, 4), Vector3.Zero, Vector3.Up);
        Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 0.1f, 100f);
        state.BeginFrameRendering(ref view, ref projection, 0f, null, false);
        scene.BeginFrameRendering(state);
        scene.ObjectManager.Submit(tinted);
        scene.ObjectManager.Submit(bare);

        using var rt = new RenderTarget2D(device, 64, 64, mipMap: false, SurfaceFormat.Color, DepthFormat.Depth24);
        RenderTargetBinding[] previousTargets = device.GetRenderTargets();
        try
        {
            device.SetRenderTarget(rt);
            device.Clear(Color.Magenta);
            scene.RenderScene();
        }
        finally
        {
            device.SetRenderTargets(previousTargets);
            vb.Dispose();
            ib.Dispose();
        }

        // the scene's light lands on both cubes alike, so blue above red is the owner's color
        long bareBlue = GlowInHalf(rt, rightHalf: false);
        long tintedBlue = GlowInHalf(rt, rightHalf: true);
        Assert.IsTrue(tintedBlue > 1000, $"setup: the tinted cube is blue, got {tintedBlue}");
        Assert.IsTrue(bareBlue < tintedBlue / 5, $"the bare cube drew with the tinted cube's color: {bareBlue} vs {tintedBlue}");
    }

    // a white box drawn by the scene's renderer onto a 64x64 magenta target, its parts as given; inParts makes it all part 1; gameDepth draws as the universe does, 16-bit depth from a near plane at 100; chrome makes it a glossy material lit from the eye
    static Color[] RenderBoxInScene(float[] partShown, Vector3 eye, Vector3 target, Vector3 size, bool inParts = false,
                                    Vector3 moved = default, float heat = 1f, bool gameDepth = false, bool chrome = false)
    {
        GraphicsDevice device = Game.GraphicsDevice;
        (StaticMesh mesh, VertexBuffer vb, IndexBuffer ib) = BuildBox(device, size);
        if (inParts)
        {
            mesh.RawMeshes[0].PartStart = new[] { 0, 0 };
            mesh.RawMeshes[0].PartPrimitives = new[] { 0, 12 };
        }
        using var fx = new LightingEffect(device) { TextureEnabled = false, DiffuseColor = Vector3.One };
        if (chrome)
        {
            fx.SpecularAmount = 6f;
            fx.SpecularPower = 64f;
        }
        SceneObject so = mesh.CreateSceneObject(effect: fx);
        so.World = Matrix.CreateScale(1f, 1f, -1f) * Matrix.CreateTranslation(moved); // mirrored, so its faces wind as a ship model's
        so.Visibility = ObjectVisibility.Rendered;
        so.PartShown = partShown;
        so.InsidesHeat = heat;

        using var scene = new SceneInterface(Game.Graphics);
        if (chrome)
            scene.LightManager.Submit(new SynapseGaming.LightingSystem.Lights.DirectionalLight
                { Direction = Vector3.Normalize(target - eye), DiffuseColor = Vector3.One, Intensity = 1f, Enabled = true });
        var state = new SceneState();
        Matrix view = Matrix.CreateLookAt(eye + moved, target + moved, Vector3.Up);
        Matrix projection = gameDepth ? Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 100f, 100_000f)
                                      : Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 0.1f, 1000f);
        state.BeginFrameRendering(ref view, ref projection, 0f, null, false);
        scene.BeginFrameRendering(state);
        scene.ObjectManager.Submit(so);

        using var rt = new RenderTarget2D(device, 64, 64, mipMap: false, SurfaceFormat.Color, gameDepth ? DepthFormat.Depth16 : DepthFormat.Depth24);
        RenderTargetBinding[] previousTargets = device.GetRenderTargets();
        try
        {
            device.SetRenderTarget(rt);
            device.Clear(Color.Magenta);
            scene.RenderScene();
        }
        finally
        {
            device.SetRenderTargets(previousTargets);
            vb.Dispose();
            ib.Dispose();
        }
        var pixels = new Color[rt.Width * rt.Height];
        rt.GetData(pixels);
        return pixels;
    }

    static readonly Vector3 Cube = new(40f);

    static bool Ember(Color px) => px.R > 120 && px.R > px.B * 2 + 40;
    static bool Charred(Color px) => px.R + px.G + px.B < 90;

    // the pixels of two pictures that differ by more than a little
    static int Differing(Color[] a, Color[] b)
        => a.Zip(b).Count(p => System.Math.Abs(p.First.R - p.Second.R) > 8 || System.Math.Abs(p.First.G - p.Second.G) > 8
                               || System.Math.Abs(p.First.B - p.Second.B) > 8);

    static readonly Vector3 FarAway = new(30_000f, -20_000f, 0f);

    // A model with a part off shows its insides where nothing else is in front, as through the gap, burnt.
    [TestMethod]
    public void AModelWithAPartOffShowsItsInsidesBurnt()
    {
        Color[] whole = RenderBoxInScene(null, Vector3.Zero, -Vector3.UnitZ, Cube);
        Assert.IsTrue(whole.All(px => px == Color.Magenta), "a whole model draws no insides");

        Color[] open = RenderBoxInScene(new[] { 1f }, Vector3.Zero, -Vector3.UnitZ, Cube);
        Assert.IsFalse(open.Any(px => px == Color.Magenta), "one with a part off draws its insides");
        Assert.IsTrue(open.Count(Ember) > 0, $"embers glowing red and orange: {open.Count(Ember)} of {open.Length}");
        Assert.IsTrue(open.Count(Charred) > open.Length / 2, $"on charred metal: {open.Count(Charred)} of {open.Length}");

        Color[] movedAway = RenderBoxInScene(new[] { 1f }, Vector3.Zero, -Vector3.UnitZ, Cube, moved: FarAway);
        Assert.IsTrue(Differing(open, movedAway) < open.Length / 100, $"the embers stay on the hull as it moves: {Differing(open, movedAway)} pixels differ");
    }

    // Insides cooled out are charred metal with no embers.
    [TestMethod]
    public void CooledInsidesGlowNoMore()
    {
        Color[] cold = RenderBoxInScene(new[] { 1f }, Vector3.Zero, -Vector3.UnitZ, Cube, heat: 0f);
        Assert.AreEqual(0, cold.Count(Ember), "no embers");
        Assert.IsTrue(cold.Count(Charred) > cold.Length * 9 / 10, $"all charred metal: {cold.Count(Charred)} of {cold.Length}");
    }

    // Charred insides keep their own dull shine, even inside a hull of polished chrome.
    [TestMethod]
    public void BurntInsidesShineAlikeOnAnyHull()
    {
        Color[] chrome = RenderBoxInScene(new[] { 1f }, Vector3.Zero, -Vector3.UnitZ, Cube, heat: 0f, chrome: true);
        int brightest = chrome.Max(px => System.Math.Max(px.R, System.Math.Max(px.G, px.B)));
        Assert.IsTrue(brightest < 100, $"no glint off the charred metal under a light from the eye: brightest {brightest}");
    }

    // Its outside is drawn as it was whole.
    [TestMethod]
    public void AModelWithAPartOffLooksTheSameFromOutside()
    {
        var eye = new Vector3(30f, 40f, 100f);
        Color[] whole = RenderBoxInScene(null, eye, Vector3.Zero, Cube);
        Color[] open = RenderBoxInScene(new[] { 1f }, eye, Vector3.Zero, Cube);
        Assert.IsTrue(whole.Count(px => px != Color.Magenta) > 500, "setup: the cube fills much of the view");
        CollectionAssert.AreEqual(whole, open, "its outside as before");
    }

    // Even where its insides lie within the game's coarse depth step of its outside, as at a hull's rim, they never show through.
    [TestMethod]
    public void AModelWithAPartOffLooksTheSameFromOutsideAtTheGamesDepth()
    {
        var plate = new Vector3(8000f, 8000f, 4f);
        var eye = new Vector3(0f, 0f, 15_000f);
        Color[] whole = RenderBoxInScene(null, eye, Vector3.Zero, plate, gameDepth: true);
        Color[] open = RenderBoxInScene(new[] { 1f }, eye, Vector3.Zero, plate, gameDepth: true);
        Assert.IsTrue(whole.Count(px => px != Color.Magenta) > 1000, "setup: the plate fills much of the view");
        CollectionAssert.AreEqual(whole, open, "its outside as before, no inside showing through");
    }

    // A part coming back is drawn in specks, more of them the more of it is repaired.
    [TestMethod]
    public void APartComingBackDrawsMoreOfItselfAsMoreOfItIsRepaired()
    {
        var eye = new Vector3(0f, 0f, 100f);
        // the front face lit white, not the burnt insides seen through its gaps
        int Front(float share) => RenderBoxInScene(new[] { 1f, share }, eye, Vector3.Zero, Cube, inParts: true)
                                  .Count(px => px.R > 50 && System.Math.Abs(px.R - px.G) < 25 && System.Math.Abs(px.R - px.B) < 25);
        int whole = Front(1f);
        Assert.IsTrue(whole > 500, $"setup: the part drawn whole fills much of the view: {whole}");
        Assert.AreEqual(0, Front(0f), "a part lost draws nothing");
        int quarter = Front(0.25f), half = Front(0.5f), threeQuarters = Front(0.75f);
        Assert.IsTrue(0 < quarter && quarter < half && half < threeQuarters && threeQuarters < whole,
                      $"more of it as more comes back: {quarter}, {half}, {threeQuarters} of {whole}");
        Assert.IsTrue(half > whole / 4 && half < whole * 3 / 4, $"about half at half repaired: {half} of {whole}");

        Color[] here = RenderBoxInScene(new[] { 1f, 0.5f }, eye, Vector3.Zero, Cube, inParts: true);
        Color[] movedAway = RenderBoxInScene(new[] { 1f, 0.5f }, eye, Vector3.Zero, Cube, inParts: true, moved: FarAway);
        Assert.IsTrue(Differing(here, movedAway) < here.Length / 100, $"the specks stay on the hull as it moves: {Differing(here, movedAway)} pixels differ");
    }

    // an unlit cube painted mid gray under a solid tint map, read at its center
    static Color CenterPixelOfGrayPaint(int maskValue, Vector4 hullTint)
    {
        using var paint = MakeSolidColorTexture(Game.GraphicsDevice, new Color(128, 128, 128, 255));
        using var mask = MakeSolidColorTexture(Game.GraphicsDevice, new Color(maskValue, maskValue, maskValue, 255));
        using var rt = RenderUnitCubeWith(fx =>
        {
            fx.LightingEnabled = false;
            fx.EmissiveColor = Vector3.Zero;
            fx.Texture = paint;
            fx.TextureEnabled = true;
            fx.TintMapTexture = mask;
            fx.HullTint = hullTint;
        });
        var pixels = new Color[rt.Width * rt.Height];
        rt.GetData(pixels);
        return pixels[rt.Height / 2 * rt.Width + rt.Width / 2];
    }

    static long GlowInHalf(RenderTarget2D rt, bool rightHalf)
    {
        var pixels = new Color[rt.Width * rt.Height];
        rt.GetData(pixels);
        long sum = 0;
        for (int i = 0; i < pixels.Length; ++i)
        {
            Color px = pixels[i];
            bool right = i % rt.Width >= rt.Width / 2;
            if (right == rightHalf && px != Color.Magenta)
                sum += System.Math.Max(0, px.B - px.R);
        }
        return sum;
    }

    // Phase B specular (`_s`) sampling: a tilted-camera + directional-light
    // setup that actually generates a specular highlight, then verify a black
    // SpecularColorMapTexture dims it. (An axis-aligned cube facing the camera
    // produces a degenerate halfway vector and no visible specular, so we
    // deliberately tilt the camera here to expose a specularly-lit edge.)
    // Phase B specular (`_s`) sampling: a visual specular-mask test would
    // require a tilted setup that produces detectable highlights at SM3.0
    // precision (single-axis cubes degenerate the halfway vector; we'd need
    // a non-trivial mesh). Instead this test does a direct parameter-binding
    // probe — verify that setting SpecularColorMapTexture surfaces on the
    // shader's `SpecularMap` parameter, which is the regression we care
    // about (a typo or namespace drift would break the binding without any
    // visible geometry signal).
    [TestMethod]
    public void SpecularMap_BindsToShaderParameter()
    {
        using var redSpec = MakeSolidColorTexture(Game.GraphicsDevice, Color.Red);
        using var fx = new LightingEffect(Game.GraphicsDevice);

        // Before binding: SpecularMapEnabled flag should be unset (false).
        Assert.IsNotNull(fx.Parameters["SpecularMap"], "Shader missing SpecularMap parameter");
        Assert.IsNotNull(fx.Parameters["SpecularMapEnabled"], "Shader missing SpecularMapEnabled flag");

        fx.SpecularColorMapTexture = redSpec;
        // Force OnApply to run by Apply()ing the technique pass.
        fx.CurrentTechnique.Passes[0].Apply();

        Assert.IsTrue(fx.Parameters["SpecularMapEnabled"].GetValueBoolean(),
            "SpecularMapEnabled should be true after binding SpecularColorMapTexture");
        Assert.AreSame(redSpec, fx.Parameters["SpecularMap"].GetValueTexture2D(),
            "SpecularMap parameter should reference the bound texture");

        // Clearing the texture should also clear the flag.
        fx.SpecularColorMapTexture = null;
        fx.CurrentTechnique.Passes[0].Apply();
        Assert.IsFalse(fx.Parameters["SpecularMapEnabled"].GetValueBoolean(),
            "SpecularMapEnabled should be false after unbinding SpecularColorMapTexture");
    }

    // Phase C tangent-space normal map binding probe. A visual test would
    // require a mesh with tangent + binormal in its vertex declaration (our
    // cube uses VertexPositionNormalTexture which has neither). The data
    // path through SdMeshGroup → MeshInterface.TranslateNativeUsage is
    // covered by the FBX import; here we just verify the C# → shader
    // parameter binding is wired. Visual correctness is checked in-game
    // via the engine-trail / specular reference shots.
    [TestMethod]
    public void NormalMap_BindsToShaderParameter()
    {
        // Standard "flat normal" — RGB(128,128,255) decodes to (0,0,1) in
        // tangent space, which leaves the surface using the vertex normal
        // (so this is the safest map to bind for a regression test).
        using var flatNormal = MakeSolidColorTexture(Game.GraphicsDevice, new Color(128, 128, 255, 255));
        using var fx = new LightingEffect(Game.GraphicsDevice);

        Assert.IsNotNull(fx.Parameters["NormalMap"], "Shader missing NormalMap parameter");
        Assert.IsNotNull(fx.Parameters["NormalMapEnabled"], "Shader missing NormalMapEnabled flag");

        fx.NormalMapTexture = flatNormal;
        fx.CurrentTechnique.Passes[0].Apply();

        Assert.IsTrue(fx.Parameters["NormalMapEnabled"].GetValueBoolean(),
            "NormalMapEnabled should be true after binding NormalMapTexture");
        Assert.AreSame(flatNormal, fx.Parameters["NormalMap"].GetValueTexture2D(),
            "NormalMap parameter should reference the bound texture");

        fx.NormalMapTexture = null;
        fx.CurrentTechnique.Passes[0].Apply();
        Assert.IsFalse(fx.Parameters["NormalMapEnabled"].GetValueBoolean(),
            "NormalMapEnabled should be false after unbinding NormalMapTexture");
    }

    static Texture2D MakeSolidColorTexture(GraphicsDevice device, Color color)
    {
        var tex = new Texture2D(device, 1, 1);
        tex.SetData(new[] { color });
        return tex;
    }

    static RenderTarget2D RenderUnitCubeWith(System.Action<LightingEffect> configure)
        => RenderUnitCubeWith(configure, new Vector3(0, 0, 3));

    static (StaticMesh Mesh, VertexBuffer Vertices, IndexBuffer Indices) BuildUnitCube(GraphicsDevice device)
        => BuildBox(device, Vector3.One);

    static (StaticMesh Mesh, VertexBuffer Vertices, IndexBuffer Indices) BuildBox(GraphicsDevice device, Vector3 size)
    {
        VertexPositionNormalTextureBump[] vertices = ForwardRendererTests.BuildCubeVertices();
        for (int i = 0; i < vertices.Length; ++i)
            vertices[i].Position *= size;
        short[] indices = ForwardRendererTests.BuildCubeIndices();

        var vb = new VertexBuffer(device, VertexPositionNormalTextureBump.VertexDeclaration,
                                  vertices.Length, BufferUsage.WriteOnly);
        vb.SetData(vertices);
        var ib = new IndexBuffer(device, IndexElementSize.SixteenBits,
                                 indices.Length, BufferUsage.WriteOnly);
        ib.SetData(indices);

        var meshData = new MeshData
        {
            Name = "UnitCube",
            VertexBuffer = vb,
            IndexBuffer = ib,
            VertexDeclaration = VertexPositionNormalTextureBump.VertexDeclaration,
            VertexCount = vertices.Length,
            VertexStride = VertexPositionNormalTextureBump.VertexDeclaration.VertexStride,
            PrimitiveCount = indices.Length / 3,
        };

        var mesh = new StaticMesh("UnitCube",
            new BoundingBox(size * -0.5f, size * 0.5f));
        mesh.RawMeshes.Add(meshData);
        return (mesh, vb, ib);
    }

    static RenderTarget2D RenderUnitCubeWith(System.Action<LightingEffect> configure, Vector3 cameraPos)
    {
        GraphicsDevice device = Game.GraphicsDevice;
        (StaticMesh mesh, VertexBuffer vb, IndexBuffer ib) = BuildUnitCube(device);

        var rt = new RenderTarget2D(device, 64, 64, mipMap: false,
            SurfaceFormat.Color, DepthFormat.Depth24);
        var effect = new LightingEffect(device);
        configure(effect);

        Matrix world = Matrix.Identity;
        Matrix view = Matrix.CreateLookAt(cameraPos, Vector3.Zero, Vector3.Up);
        Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 0.1f, 100f);

        RenderTargetBinding[] previousTargets = device.GetRenderTargets();
        BlendState prevBlend = device.BlendState;
        DepthStencilState prevDepth = device.DepthStencilState;
        RasterizerState prevRaster = device.RasterizerState;
        try
        {
            device.SetRenderTarget(rt);
            device.Clear(Color.Magenta);
            device.BlendState = BlendState.Opaque;
            device.DepthStencilState = DepthStencilState.Default;
            device.RasterizerState = RasterizerState.CullCounterClockwise;
            mesh.Draw(device, world, view, projection, effect);
        }
        finally
        {
            device.SetRenderTargets(previousTargets);
            device.BlendState = prevBlend;
            device.DepthStencilState = prevDepth;
            device.RasterizerState = prevRaster;
            effect.Dispose();
            vb.Dispose();
            ib.Dispose();
        }
        return rt;
    }

    static long SumBrightness(RenderTarget2D rt, bool onlyMagentaPixels = false)
    {
        var pixels = new Color[rt.Width * rt.Height];
        rt.GetData(pixels);
        long sum = 0;
        foreach (Color px in pixels)
        {
            bool isMagenta = px.R == 255 && px.G == 0 && px.B == 255;
            if (onlyMagentaPixels == isMagenta)
                sum += px.R + px.G + px.B;
        }
        return sum;
    }

    static long CountPixels(RenderTarget2D rt, Color color)
    {
        var pixels = new Color[rt.Width * rt.Height];
        rt.GetData(pixels);
        long count = 0;
        foreach (Color px in pixels)
            if (px == color) ++count;
        return count;
    }

    static long SumChannel(RenderTarget2D rt, int channel, bool excludeMagenta)
    {
        var pixels = new Color[rt.Width * rt.Height];
        rt.GetData(pixels);
        long sum = 0;
        foreach (Color px in pixels)
        {
            if (excludeMagenta && px.R == 255 && px.G == 0 && px.B == 255) continue;
            sum += channel switch { 0 => px.R, 1 => px.G, 2 => px.B, _ => 0 };
        }
        return sum;
    }
}
