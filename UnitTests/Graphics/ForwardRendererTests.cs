using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Mesh;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects.Forward;
using SynapseGaming.LightingSystem.Rendering;
using SDUtils;

namespace UnitTests.Graphics;

/// <summary>
/// Phase 2.8 sub-phase A4 smoke signal: prove the new forward-renderer pipeline
/// (VertexBuffer + IndexBuffer + LightingEffect + DrawIndexedPrimitives) can
/// rasterize a hand-built unit cube to a RenderTarget2D and produce non-clear
/// pixels. Self-contained — no content load, no atlas, no model XNB. Catches
/// degenerate buffer ctors, missing world/view/projection, broken effect
/// parameter binding — the cheap-but-decisive correctness signal for the new
/// pipeline.
/// </summary>
[TestClass]
public class ForwardRendererTests : StarDriveTest
{
    [TestMethod]
    public void RenderUnitCube_ProducesNonClearPixels()
    {
        GraphicsDevice device = Game.GraphicsDevice;

        // 24-vertex cube (4 per face) so each face has a clean normal for default lighting.
        // Phase 3.7 step 4 (Phase C): use the bump-bearing format so the new
        // MeshLighting.fx VS gets its required Tangent + Binormal inputs even
        // when the test doesn't actually use normal mapping (NormalMapEnabled
        // stays false; the values are uninitialized zero — never read).
        VertexPositionNormalTextureBump[] vertices = BuildCubeVertices();
        short[] indices = BuildCubeIndices();

        using var vb = new VertexBuffer(device, VertexPositionNormalTextureBump.VertexDeclaration,
                                        vertices.Length, BufferUsage.WriteOnly);
        vb.SetData(vertices);

        using var ib = new IndexBuffer(device, IndexElementSize.SixteenBits,
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

        using var rt = new RenderTarget2D(device, 64, 64, mipMap: false,
            SurfaceFormat.Color, DepthFormat.Depth24);
        using var effect = new LightingEffect(device);
        // EnableDefaultLighting was called in ctor; nothing else needed for the smoke.

        Matrix world = Matrix.Identity;
        Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, 3), Vector3.Zero, Vector3.Up);
        Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 0.1f, 100f);

        // Capture state we'll restore.
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
        }

        var pixels = new Color[64 * 64];
        rt.GetData(pixels);

        int nonClearCount = 0;
        foreach (Color px in pixels)
            if (px != Color.Magenta) ++nonClearCount;

        // A unit cube framed in a 64×64 RT covers ~25–35% of the pixels typically.
        // 5% is a generous floor — anything less means the pipeline is broken.
        const float MinNonClearFraction = 0.05f;
        int minPixels = (int)(64 * 64 * MinNonClearFraction);
        Assert.IsTrue(nonClearCount >= minPixels,
            $"Expected at least {minPixels} non-clear pixels, got {nonClearCount}. " +
            "Forward-renderer pipeline appears broken (degenerate buffers, missing W/V/P, or effect-parameter binding).");
    }

    internal static VertexPositionNormalTextureBump[] BuildCubeVertices()
    {
        // 6 faces × 4 corners = 24 verts. Per-face normal so default lighting
        // works. Tangent + Binormal stay zero — the test cube doesn't use
        // normal mapping (NormalMapEnabled is false), so the shader never
        // reads them, but D3D11 still requires the slots to exist in the
        // declaration to match VS input semantics.
        var v = new VertexPositionNormalTextureBump[24];
        Vector3 nUp = Vector3.Up, nDown = Vector3.Down;
        Vector3 nLeft = Vector3.Left, nRight = Vector3.Right;
        Vector3 nForward = Vector3.Forward, nBack = Vector3.Backward;
        Vector2 uv00 = new(0, 0), uv10 = new(1, 0), uv01 = new(0, 1), uv11 = new(1, 1);
        Vector3 p000 = new(-0.5f, -0.5f, -0.5f);
        Vector3 p100 = new( 0.5f, -0.5f, -0.5f);
        Vector3 p010 = new(-0.5f,  0.5f, -0.5f);
        Vector3 p110 = new( 0.5f,  0.5f, -0.5f);
        Vector3 p001 = new(-0.5f, -0.5f,  0.5f);
        Vector3 p101 = new( 0.5f, -0.5f,  0.5f);
        Vector3 p011 = new(-0.5f,  0.5f,  0.5f);
        Vector3 p111 = new( 0.5f,  0.5f,  0.5f);

        VertexPositionNormalTextureBump V(Vector3 pos, Vector3 normal, Vector2 uv) =>
            new VertexPositionNormalTextureBump
            {
                Position = pos, Normal = normal, TextureCoordinate = uv,
                Tangent = Vector3.Zero, Binormal = Vector3.Zero,
            };

        // +Z (back, facing camera at +Z)
        v[0]  = V(p001, nBack,    uv00); v[1]  = V(p101, nBack,    uv10);
        v[2]  = V(p011, nBack,    uv01); v[3]  = V(p111, nBack,    uv11);
        // -Z (front, away from camera)
        v[4]  = V(p100, nForward, uv00); v[5]  = V(p000, nForward, uv10);
        v[6]  = V(p110, nForward, uv01); v[7]  = V(p010, nForward, uv11);
        // +X (right)
        v[8]  = V(p101, nRight,   uv00); v[9]  = V(p100, nRight,   uv10);
        v[10] = V(p111, nRight,   uv01); v[11] = V(p110, nRight,   uv11);
        // -X (left)
        v[12] = V(p000, nLeft,    uv00); v[13] = V(p001, nLeft,    uv10);
        v[14] = V(p010, nLeft,    uv01); v[15] = V(p011, nLeft,    uv11);
        // +Y (top)
        v[16] = V(p011, nUp,      uv00); v[17] = V(p111, nUp,      uv10);
        v[18] = V(p010, nUp,      uv01); v[19] = V(p110, nUp,      uv11);
        // -Y (bottom)
        v[20] = V(p000, nDown,    uv00); v[21] = V(p100, nDown,    uv10);
        v[22] = V(p001, nDown,    uv01); v[23] = V(p101, nDown,    uv11);
        return v;
    }

    internal static short[] BuildCubeIndices()
    {
        // For each face's 4 verts laid out as { 00, 10, 01, 11 }, two triangles:
        // (00, 10, 01), (10, 11, 01) — counter-clockwise winding when viewed from
        // outside the cube.
        var ix = new short[36];
        for (int face = 0; face < 6; ++face)
        {
            int b = face * 4;
            int o = face * 6;
            ix[o + 0] = (short)(b + 0); ix[o + 1] = (short)(b + 1); ix[o + 2] = (short)(b + 2);
            ix[o + 3] = (short)(b + 1); ix[o + 4] = (short)(b + 3); ix[o + 5] = (short)(b + 2);
        }
        return ix;
    }
}
