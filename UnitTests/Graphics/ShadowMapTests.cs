using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using Ship_Game.Data;
using Ship_Game.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects.Forward;

namespace UnitTests.Graphics;

/// <summary>
/// Phase 3.8.A — depth-pass infrastructure verification.
///
/// Renders two cubes from a known sun direction into a small shadow map
/// via ShadowMapComponent, GetData()'s the R32F target, and asserts the
/// near-cube's depth samples are closer to the light than the far-cube's
/// depth samples at the corresponding texels. Catches:
///   - Wrong matrix-mul order in Shadow.fx (clip-space depth out of range)
///   - LightView / LightProjection swapped (depth comes out near-constant)
///   - RT not bound or not cleared (samples come back as last-write
///     garbage / pre-existing memory)
///   - Sign flip in the orthographic depth packing (front cube reads
///     greater depth than back cube)
///
/// §3.8.B will extend this fixture with a "receiver gets darkened where
/// the occluder shadow falls" assertion once the lit shader is wired up.
/// </summary>
[TestClass]
public class ShadowMapTests : StarDriveTest
{
    [TestMethod]
    public void DepthPass_TwoCubes_FrontReadsCloserDepthThanBack()
    {
        GraphicsDevice device = Game.GraphicsDevice;
        GameContentManager content = StarDriveTestContext.Content;

        // Small RT keeps the GetData() round-trip cheap. 64² is plenty to
        // place two non-overlapping cubes on opposite halves and sample
        // distinct texels for each.
        const int Size = 64;

        using var shadow = new ShadowMapComponent(device, content, Size);
        shadow.LoadContent();
        Assert.IsNotNull(shadow.ShadowMap, "Shadow.mgfxo missing — sibling .mgfxo fallback in GameContentManager.LoadAsset broken?");

        // Sun shines down -Z. Light camera ends up at sceneCenter + (0,0,2r),
        // so a cube at world Z=+5 sits closer to the camera (smaller depth)
        // than one at Z=-5.
        Vector3 lightDir = -Vector3.UnitZ;
        var sceneBounds = new BoundingSphere(Vector3.Zero, radius: 10f);

        // Two unit cubes, scale=2 so each lights ~16 pixels of the RT.
        // Front cube on the left half; back cube on the right half. They
        // do not overlap projectively, so the front-cube depth samples and
        // back-cube depth samples come from disjoint texel regions.
        Matrix frontWorld = Matrix.CreateScale(2f) * Matrix.CreateTranslation(-2.5f, 0f,  5f);
        Matrix backWorld  = Matrix.CreateScale(2f) * Matrix.CreateTranslation( 2.5f, 0f, -5f);

        using VertexBuffer cubeVB = BuildUnitCubeVertexBuffer(device);
        using IndexBuffer  cubeIB = BuildUnitCubeIndexBuffer(device);

        RenderTargetBinding[] prev = device.GetRenderTargets();
        try
        {
            shadow.BeginShadowPass(lightDir, sceneBounds);
            shadow.DrawCaster(frontWorld, cubeVB, cubeIB,
                              PrimitiveType.TriangleList,
                              baseVertex: 0, startIndex: 0, primitiveCount: 12);
            shadow.DrawCaster(backWorld, cubeVB, cubeIB,
                              PrimitiveType.TriangleList,
                              baseVertex: 0, startIndex: 0, primitiveCount: 12);
            shadow.EndShadowPass();
        }
        finally
        {
            device.SetRenderTargets(prev);
        }

        // R32F → single floats. Cleared value was 1.0 (far plane); cubes
        // overwrite their footprint with their own sub-1.0 depth.
        var depths = new float[Size * Size];
        shadow.ShadowMap.GetData(depths);

        // Cube footprints in viewport space (Size=64, ortho width=20):
        //   front cube center world X = -2.5 → NDC X = -0.25 → col = (1-0.25)/2*64 = 24
        //   back  cube center world X = +2.5 → NDC X = +0.25 → col = (1+0.25)/2*64 = 40
        // Each cube spans 2 world units → ~6.4 viewport columns wide; pick the
        // exact center to stay clear of the rasterisation edge.
        int rowMid   = Size / 2;             // 32 (world Y = 0)
        int frontCol = 24;
        int backCol  = 40;

        float frontDepth = depths[rowMid * Size + frontCol];
        float backDepth  = depths[rowMid * Size + backCol];

        // Both samples must have come from a cube (not the cleared white
        // background). Cleared value is 1.0; cube samples at z=±5 inside
        // a far=40 ortho range come back well below that.
        Assert.IsTrue(frontDepth < 0.95f,
            $"Front-cube texel ({frontCol},{rowMid}) read depth {frontDepth:F4}; expected a cube sample, " +
            "got the clear value. Either the cube didn't rasterise (bad VertexDeclaration -> POSITION0 binding) " +
            "or the projection landed it off-screen.");
        Assert.IsTrue(backDepth < 0.95f,
            $"Back-cube texel ({backCol},{rowMid}) read depth {backDepth:F4}; expected a cube sample, " +
            "got the clear value. See front-cube assertion for likely causes.");

        // Front cube must read CLOSER to the light (smaller depth) than back.
        Assert.IsTrue(frontDepth < backDepth - 0.05f,
            $"Front cube depth ({frontDepth:F4}) was not measurably closer than back cube depth " +
            $"({backDepth:F4}). Either the LightView / LightProjection are swapped, or the depth " +
            "encoding in Shadow.fx ended up sign-flipped.");

        // Sanity: sample a corner that no cube projects into — should
        // still be the far-plane clear value.
        float clearDepth = depths[2 * Size + 2];
        Assert.IsTrue(clearDepth > 0.99f,
            $"Corner texel expected far-plane clear (~1.0), got {clearDepth:F4}. " +
            "Either Begin/EndShadowPass clear semantics regressed, or one of the cubes covered the corner.");
    }

    static VertexBuffer BuildUnitCubeVertexBuffer(GraphicsDevice device)
    {
        // Color channel is unused — Shadow.fx only reads POSITION0. Using
        // VertexPositionColor avoids declaring a custom VertexDeclaration
        // and keeps the binding format trivially correct (Position is the
        // first element at offset 0, Vector3, usage=Position, index=0).
        var verts = new VertexPositionColor[]
        {
            new(new Vector3(-0.5f, -0.5f, -0.5f), Color.White),
            new(new Vector3( 0.5f, -0.5f, -0.5f), Color.White),
            new(new Vector3( 0.5f,  0.5f, -0.5f), Color.White),
            new(new Vector3(-0.5f,  0.5f, -0.5f), Color.White),
            new(new Vector3(-0.5f, -0.5f,  0.5f), Color.White),
            new(new Vector3( 0.5f, -0.5f,  0.5f), Color.White),
            new(new Vector3( 0.5f,  0.5f,  0.5f), Color.White),
            new(new Vector3(-0.5f,  0.5f,  0.5f), Color.White),
        };
        var vb = new VertexBuffer(device, VertexPositionColor.VertexDeclaration,
                                  verts.Length, BufferUsage.WriteOnly);
        vb.SetData(verts);
        return vb;
    }

    [TestMethod]
    public void LitPass_WithShadowMapBound_DarkensReceiverPixels()
    {
        // Phase 3.8.B integration: drive the depth pre-pass with one cube,
        // then render a SECOND cube as receiver via LightingEffect — first
        // with shadow sampling disabled (baseline), then with the depth RT
        // bound. Assert: the with-shadow render is measurably darker than
        // the baseline. Catches:
        //   - SampleShadowFactor not actually multiplied into diffuse/spec.
        //   - LightViewProjection not propagated through OnApply.
        //   - ShadowMap texture not bound to the sampler.
        //   - Shadow factor 0/1 inverted (shadowed region staying lit).
        GraphicsDevice device = Game.GraphicsDevice;
        GameContentManager content = StarDriveTestContext.Content;

        // Sun shines straight down -Y. Occluder above the receiver, both on
        // the same Y axis so the receiver's top-face center projects onto
        // the occluder's depth footprint in light-clip space.
        Vector3 lightDir = new(0, -1, 0);
        var sceneBounds = new BoundingSphere(Vector3.Zero, radius: 8f);

        // Occluder ~75% the size of the receiver — covers most of the
        // receiver's top face when projected, leaving a thin lit strip on
        // the edges. Big enough that the shadow drop signal clears noise.
        Matrix occluderWorld = Matrix.CreateScale(3f) * Matrix.CreateTranslation(0f, 5f, 0f);
        Matrix receiverWorld = Matrix.CreateScale(4f);            // 4×4×4 at origin

        // Cube vertex format with NORMAL/TEXCOORD/TANGENT/BINORMAL — required
        // by MeshLighting.fx's VS input declaration. Reuses the same builder
        // existing lit-pass tests use (ForwardRendererTests.BuildCubeVertices).
        VertexPositionNormalTextureBump[] verts = ForwardRendererTests.BuildCubeVertices();
        short[] indices = ForwardRendererTests.BuildCubeIndices();
        using var litVB = new VertexBuffer(device, VertexPositionNormalTextureBump.VertexDeclaration,
                                           verts.Length, BufferUsage.WriteOnly);
        litVB.SetData(verts);
        using var litIB = new IndexBuffer(device, IndexElementSize.SixteenBits,
                                          indices.Length, BufferUsage.WriteOnly);
        litIB.SetData(indices);

        // Depth pass uses the same cube geometry (POSITION0 only is read).
        using VertexBuffer occluderVB = BuildUnitCubeVertexBuffer(device);
        using IndexBuffer  occluderIB = BuildUnitCubeIndexBuffer(device);

        using var shadow = new ShadowMapComponent(device, content, 256);
        shadow.LoadContent();

        // Camera positioned to see receiver's top + front faces, so both
        // a shadowed-center region and a lit-edge region show up in the
        // captured frame.
        Matrix camView = Matrix.CreateLookAt(new Vector3(0, 8, 8), Vector3.Zero, Vector3.Up);
        Matrix camProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 0.1f, 100f);

        // Confirm the new shadow params survived the mgfxc compile. Without
        // these, OnApply's SetValue calls are silent no-ops and the test
        // would falsely "pass" due to identical-no-shadow brightness in
        // both renders. (We hit exactly that during §3.8.B development —
        // the ShadowMap texture got dead-stripped behind a debug return,
        // and earlier the bool variant didn't reach the cbuffer.)
        using (var dbg = new LightingEffect(device))
        {
            Assert.IsNotNull(dbg.Parameters["ShadowParams"],        "ShadowParams missing from MeshLighting.mgfxo");
            Assert.IsNotNull(dbg.Parameters["ShadowMap"],           "ShadowMap missing from MeshLighting.mgfxo");
            Assert.IsNotNull(dbg.Parameters["LightViewProjection"], "LightViewProjection missing");
        }

        long brightnessNoShadow = RenderLitReceiver(device, lightDir,
            litVB, litIB, indices.Length / 3,
            receiverWorld, camView, camProjection,
            shadowMap: null, lightVP: Matrix.Identity);

        // Now run the real depth pre-pass and re-render with the shadow map bound.
        RenderTargetBinding[] prev = device.GetRenderTargets();
        try
        {
            shadow.BeginShadowPass(lightDir, sceneBounds);
            shadow.DrawCaster(occluderWorld, occluderVB, occluderIB,
                              PrimitiveType.TriangleList, 0, 0, 12);
            shadow.EndShadowPass();
        }
        finally { device.SetRenderTargets(prev); }

        long brightnessWithShadow = RenderLitReceiver(device, lightDir,
            litVB, litIB, indices.Length / 3,
            receiverWorld, camView, camProjection,
            shadowMap: shadow.ShadowMap,
            lightVP: shadow.LightView * shadow.LightProjection);

        // Sanity: the no-shadow render must actually light the cube. If
        // it bottoms out at near-ambient (<50k brightness for a 64² RT),
        // the lit pass isn't running and the comparison is meaningless.
        Assert.IsTrue(brightnessNoShadow > 50_000,
            $"No-shadow render brightness {brightnessNoShadow} suggests the lit pass isn't lighting the cube — " +
            "check VertexBuffer / camera framing / DirectionalLight binding.");

        // With the occluder covering ~75% of the receiver's top-face
        // projection, ~12-20% of total cube brightness should be lost to
        // the shadow factor (top-face center darkened to ambient; lit
        // strip remains around the edges, plus dim non-top faces unchanged).
        // 5% threshold clears post-rasterisation pixel-edge coverage noise.
        double drop = 1.0 - (double)brightnessWithShadow / brightnessNoShadow;
        Assert.IsTrue(drop > 0.05,
            $"With-shadow brightness ({brightnessWithShadow}) was not measurably " +
            $"darker than no-shadow brightness ({brightnessNoShadow}) — drop {drop:P2}. " +
            "Either SampleShadowFactor isn't gating diffuse/specular, the shadow map " +
            "isn't reaching the sampler, or the LightViewProjection projection " +
            "doesn't land the occluder onto the receiver's footprint.");
    }

    static long RenderLitReceiver(GraphicsDevice device, Vector3 sunDir,
                                  VertexBuffer vb, IndexBuffer ib, int primitiveCount,
                                  Matrix receiverWorld, Matrix view, Matrix projection,
                                  Texture2D shadowMap, Matrix lightVP)
    {
        const int Size = 64;
        using var rt = new RenderTarget2D(device, Size, Size, mipMap: false,
                                          SurfaceFormat.Color, DepthFormat.Depth24);
        using var fx = new LightingEffect(device);

        // Use EnableDefaultLighting (3-light setup) so we know the lit
        // path produces meaningful brightness. Specific direction values
        // matter less than ensuring SOME light hits the cube.
        fx.EnableDefaultLighting();
        fx.AmbientLightColor = new Vector3(0.05f, 0.05f, 0.05f);
        fx.DiffuseColor      = Vector3.One;
        fx.EmissiveColor     = Vector3.Zero;
        fx.SpecularColor     = Vector3.Zero;        // ignore specular — diffuse dominates
        fx.TextureEnabled    = false;

        fx.View = view;
        fx.Projection = projection;
        fx.World = receiverWorld;

        fx.ShadowMapEnabled    = shadowMap != null;
        fx.ShadowMap           = shadowMap;
        fx.LightViewProjection = lightVP;

        RenderTargetBinding[] prev = device.GetRenderTargets();
        BlendState        prevBlend = device.BlendState;
        DepthStencilState prevDepth = device.DepthStencilState;
        RasterizerState   prevRast  = device.RasterizerState;
        try
        {
            device.SetRenderTarget(rt);
            device.Clear(Color.Black);
            device.BlendState        = BlendState.Opaque;
            device.DepthStencilState = DepthStencilState.Default;
            device.RasterizerState   = RasterizerState.CullCounterClockwise;
            device.SetVertexBuffer(vb);
            device.Indices = ib;
            foreach (EffectPass pass in fx.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, primitiveCount);
            }
        }
        finally
        {
            device.SetRenderTargets(prev);
            device.BlendState        = prevBlend;
            device.DepthStencilState = prevDepth;
            device.RasterizerState   = prevRast;
        }

        var pixels = new Color[Size * Size];
        rt.GetData(pixels);
        long sum = 0;
        foreach (Color px in pixels)
            sum += px.R + px.G + px.B;
        return sum;
    }

    static IndexBuffer BuildUnitCubeIndexBuffer(GraphicsDevice device)
    {
        // 12 triangles, 36 indices. Counter-clockwise winding when viewed
        // from outside (matches CullCounterClockwise in BeginShadowPass).
        ushort[] indices =
        {
            // -Z
            0, 2, 1,  0, 3, 2,
            // +Z
            4, 5, 6,  4, 6, 7,
            // -X
            0, 4, 7,  0, 7, 3,
            // +X
            1, 2, 6,  1, 6, 5,
            // -Y
            0, 1, 5,  0, 5, 4,
            // +Y
            3, 7, 6,  3, 6, 2,
        };
        var ib = new IndexBuffer(device, IndexElementSize.SixteenBits,
                                 indices.Length, BufferUsage.WriteOnly);
        ib.SetData(indices);
        return ib;
    }
}
