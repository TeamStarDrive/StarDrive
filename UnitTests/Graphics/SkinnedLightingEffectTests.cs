using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Effects.Forward;

namespace UnitTests.Graphics;

/// <summary>
/// Phase 3.10.B.5 close-out: verify SkinnedEffect.mgfxo loaded, exposes
/// the matrix-palette parameter the renderer (B.6) will push, AND retains
/// the full LightingEffect parameter surface so the binder can target it
/// the same way it targets the static MeshLighting effect.
/// </summary>
[TestClass]
public class SkinnedLightingEffectTests : StarDriveTest
{
    [TestMethod]
    public void SkinnedLightingEffect_ExposesBonesParameter()
    {
        GraphicsDevice device = Game.GraphicsDevice;
        using var fx = new SkinnedLightingEffect(device);
        Assert.IsNotNull(fx.Parameters["Bones"], "SkinnedEffect.mgfxo missing the Bones[] palette parameter");
    }

    [TestMethod]
    public void SkinnedLightingEffect_PreservesLightingEffectSurface()
    {
        // Renderer (B.6) wires SkinnedLightingEffect through the same
        // LightingEffectBinder.Apply path it uses for LightingEffect, so the
        // material + light + shadow uniforms must survive. Mirrors the
        // ShadowMapTests guard for MeshLighting.mgfxo.
        GraphicsDevice device = Game.GraphicsDevice;
        using var fx = new SkinnedLightingEffect(device);
        Assert.IsNotNull(fx.Parameters["World"]);
        Assert.IsNotNull(fx.Parameters["View"]);
        Assert.IsNotNull(fx.Parameters["Projection"]);
        Assert.IsNotNull(fx.Parameters["DiffuseColor"]);
        Assert.IsNotNull(fx.Parameters["AmbientLightColor"]);
        Assert.IsNotNull(fx.Parameters["DirLight0Direction"]);
        Assert.IsNotNull(fx.Parameters["PointLight0PositionAndRadius"]);
        Assert.IsNotNull(fx.Parameters["DynamicLight0PositionAndRadius"]);
        Assert.IsNotNull(fx.Parameters["ShadowParams"], "ShadowParams missing — receiver shadow path won't apply on skinned hulls");
        Assert.IsNotNull(fx.Parameters["ShadowMap"]);
        Assert.IsNotNull(fx.Parameters["LightViewProjection"]);
        Assert.IsNotNull(fx.Parameters["EmissiveScale"], "EmissiveScale missing — animated hulls' lights won't follow their power");
    }

    [TestMethod]
    public void SkinnedLightingEffect_SetBoneTransforms_PushesPalette()
    {
        GraphicsDevice device = Game.GraphicsDevice;
        using var fx = new SkinnedLightingEffect(device);

        var palette = new Matrix[3];
        palette[0] = Matrix.Identity;
        palette[1] = Matrix.CreateTranslation(1f, 2f, 3f);
        palette[2] = Matrix.CreateScale(2f);

        fx.SetBoneTransforms(palette); // must not throw
    }

    [TestMethod]
    public void SkinnedLightingEffect_SetBoneTransforms_TruncatesOversizedPalette()
    {
        GraphicsDevice device = Game.GraphicsDevice;
        using var fx = new SkinnedLightingEffect(device);

        // A future mod with 200 deformer bones would overflow the shader's
        // Bones[64] array; SetBoneTransforms silently clamps to the cap
        // rather than throwing inside MonoGame's SetValue.
        var palette = new Matrix[200];
        for (int i = 0; i < palette.Length; i++) palette[i] = Matrix.Identity;
        fx.SetBoneTransforms(palette); // must not throw
    }

    [TestMethod]
    public void SkinnedLightingEffect_NullOrEmptyPalette_IsSafeNoOp()
    {
        GraphicsDevice device = Game.GraphicsDevice;
        using var fx = new SkinnedLightingEffect(device);
        fx.SetBoneTransforms(null);
        fx.SetBoneTransforms(new Matrix[0]);
    }

    // Animated hulls' lights follow their ship's power like static hulls' do.
    [TestMethod]
    public void SkinnedLightingEffect_EmissiveScaleScalesTheGlowMap()
    {
        long full = BlueGlowWith(1f);
        Assert.IsTrue(full > 1000, $"setup: the glow map renders the skinned cube blue, got B-sum={full}");
        long none = BlueGlowWith(0f);
        Assert.IsTrue(none < 100, $"no glow at zero scale, got B-sum={none}");
    }

    // Animated hulls take their owner's color on their tint-mapped paint like static hulls do.
    [TestMethod]
    [DataRow(1f, 112, 255)]
    [DataRow(0f, 128, 128)]
    public void SkinnedLightingEffect_HullTintColorsTheTintMappedPaint(float tintWeight, int red, int blue)
    {
        GraphicsDevice device = Game.GraphicsDevice;
        using var paint = new Texture2D(device, 1, 1);
        paint.SetData(new[] { new Color(128, 128, 128, 255) });
        using var mask = new Texture2D(device, 1, 1);
        mask.SetData(new[] { Color.White });
        Color[] pixels = RenderSkinnedCube(fx =>
        {
            fx.Texture = paint;
            fx.TextureEnabled = true;
            fx.TintMapTexture = mask;
            fx.HullTint = new Vector4(0f, 0f, 1f, tintWeight);
        });
        Color px = pixels[32 * 64 + 32];
        AssertEqual(3f, red, px.R, "red");
        AssertEqual(3f, red, px.G, "green");
        AssertEqual(3f, blue, px.B, "blue");
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct SkinnedVertex : IVertexType
    {
        public Vector3 Position;
        public byte Bone0, Bone1, Bone2, Bone3;
        public Vector4 Weights;
        public Vector3 Normal;
        public Vector2 TexCoord;
        public Vector3 Tangent;
        public Vector3 Binormal;

        public static readonly VertexDeclaration Declaration = new(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Byte4, VertexElementUsage.BlendIndices, 0),
            new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
            new VertexElement(32, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(44, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(52, VertexElementFormat.Vector3, VertexElementUsage.Tangent, 0),
            new VertexElement(64, VertexElementFormat.Vector3, VertexElementUsage.Binormal, 0));

        VertexDeclaration IVertexType.VertexDeclaration => Declaration;
    }

    // a unit cube bound whole to bone 0, drawn with no lights and a solid blue glow map
    static long BlueGlowWith(float emissiveScale)
    {
        using var glowMap = new Texture2D(Game.GraphicsDevice, 1, 1);
        glowMap.SetData(new[] { new Color(0, 0, 255, 255) });
        Color[] pixels = RenderSkinnedCube(fx =>
        {
            fx.TextureEnabled = false;
            fx.DiffuseColor = Vector3.Zero;
            fx.EmissiveMapTexture = glowMap;
            fx.EmissiveScale = emissiveScale;
        });
        return pixels.Where(px => px != Color.Magenta).Sum(px => (long)px.B);
    }

    // a cube seen from its center, drawn white from both sides, its insides burnt or not
    static System.Action<SkinnedLightingEffect> InsideTheCube(bool burnt) => fx =>
    {
        fx.World = Matrix.CreateScale(1f, 1f, -1f); // mirrored, so its faces wind as a ship model's
        fx.View = Matrix.CreateLookAt(Vector3.Zero, -Vector3.UnitZ, Vector3.Up);
        fx.TextureEnabled = false;
        fx.DiffuseColor = Vector3.One;
        fx.InsidesBurnt = burnt;
    };

    // Animated hulls with a part off show their insides burnt like static hulls do.
    [TestMethod]
    public void SkinnedLightingEffect_DrawsTheInsidesBurnt()
    {
        Color[] plain = RenderSkinnedCube(InsideTheCube(burnt: false), bothSides: true, size: 40f);
        Assert.IsTrue(plain.All(px => px.R > 200 && px.G > 200 && px.B > 200), "setup: the insides drawn plain white");

        Color[] burnt = RenderSkinnedCube(InsideTheCube(burnt: true), bothSides: true, size: 40f);
        int embers = burnt.Count(px => px.R > 120 && px.R > px.B * 2 + 40);
        int charred = burnt.Count(px => px.R + px.G + px.B < 90);
        Assert.IsTrue(embers > 0, $"embers glowing red and orange: {embers} of {burnt.Length}");
        Assert.IsTrue(charred > burnt.Length / 2, $"on charred metal: {charred} of {burnt.Length}");
    }

    // Animated hulls draw a part coming back in specks like static hulls do.
    [TestMethod]
    public void SkinnedLightingEffect_DrawsAPartComingBackInSpecks()
    {
        int Drawn(float share) => RenderSkinnedCube(fx =>
        {
            fx.World = Matrix.CreateScale(1f, 1f, -1f); // mirrored, so its faces wind as a ship model's
            fx.View = Matrix.CreateLookAt(new Vector3(0f, 0f, 90f), Vector3.Zero, Vector3.Up);
            fx.CurrentTechnique = fx.DissolveTechnique;
            fx.ShareShown = share;
        }, size: 40f).Count(px => px != Color.Magenta);
        using (var fx = new SkinnedLightingEffect(Game.GraphicsDevice))
            Assert.IsNotNull(fx.DissolveTechnique, "setup: the skinned effect can dissolve");
        int whole = Drawn(1f);
        int half = Drawn(0.5f);
        Assert.IsTrue(whole > 500, $"setup: drawn whole it fills much of the view: {whole}");
        Assert.IsTrue(half > whole / 4 && half < whole * 3 / 4, $"about half at half repaired: {half} of {whole}");
    }

    // a cube of the given size bound whole to bone 0, drawn with no lights onto a 64x64 magenta target
    static Color[] RenderSkinnedCube(System.Action<SkinnedLightingEffect> configure, bool bothSides = false, float size = 1f)
    {
        GraphicsDevice device = Game.GraphicsDevice;
        SkinnedVertex[] vertices = ForwardRendererTests.BuildCubeVertices().Select(v => new SkinnedVertex
        {
            Position = v.Position * size, Weights = new Vector4(1f, 0f, 0f, 0f),
            Normal = v.Normal, TexCoord = v.TextureCoordinate,
        }).ToArray();
        short[] indices = ForwardRendererTests.BuildCubeIndices();

        using var vb = new VertexBuffer(device, SkinnedVertex.Declaration, vertices.Length, BufferUsage.WriteOnly);
        vb.SetData(vertices);
        using var ib = new IndexBuffer(device, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
        ib.SetData(indices);
        using var fx = new SkinnedLightingEffect(device)
        {
            LightingEnabled = false,
            View = Matrix.CreateLookAt(new Vector3(0, 0, 3), Vector3.Zero, Vector3.Up),
            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 0.1f, 100f),
        };
        configure(fx);
        fx.SetBoneTransforms(new[] { Matrix.Identity });

        using var rt = new RenderTarget2D(device, 64, 64, mipMap: false, SurfaceFormat.Color, DepthFormat.Depth24);
        RenderTargetBinding[] previousTargets = device.GetRenderTargets();
        RasterizerState prevRaster = device.RasterizerState;
        BlendState prevBlend = device.BlendState;
        DepthStencilState prevDepth = device.DepthStencilState;
        try
        {
            device.SetRenderTarget(rt);
            device.Clear(Color.Magenta);
            device.BlendState = BlendState.Opaque;
            device.DepthStencilState = DepthStencilState.Default;
            device.RasterizerState = bothSides ? RasterizerState.CullNone : RasterizerState.CullCounterClockwise;
            device.SetVertexBuffer(vb);
            device.Indices = ib;
            foreach (EffectPass pass in fx.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, indices.Length / 3);
            }
        }
        finally
        {
            device.SetRenderTargets(previousTargets);
            device.RasterizerState = prevRaster;
            device.BlendState = prevBlend;
            device.DepthStencilState = prevDepth;
        }

        var pixels = new Color[rt.Width * rt.Height];
        rt.GetData(pixels);
        return pixels;
    }
}
