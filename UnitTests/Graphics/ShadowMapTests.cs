using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using Ship_Game.Data;
using Ship_Game.Graphics;

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
