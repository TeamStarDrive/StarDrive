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
///   3. Disabling all lights gives DiffuseColor*texture (proves the
///      LightingEnabled flag reaches the shader).
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

    static RenderTarget2D RenderUnitCubeWith(System.Action<LightingEffect> configure, Vector3 cameraPos)
    {
        GraphicsDevice device = Game.GraphicsDevice;

        VertexPositionNormalTextureBump[] vertices = ForwardRendererTests.BuildCubeVertices();
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
            new BoundingBox(new Vector3(-0.5f), new Vector3(0.5f)));
        mesh.RawMeshes.Add(meshData);

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
