using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Mesh;
using Ship_Game.Data.Texture;
using SynapseGaming.LightingSystem.Effects.Forward;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace UnitTests.Graphics;

/// <summary>
/// A model's DXT1 textures stay compressed on the GPU and draw like their decoded copies, each texel as transparent and
/// within a few levels of its color, while its DXT5 textures, and a DXT1 file the other loaders load, are decoded as
/// before.
/// </summary>
[TestClass]
public class ModelTextureTests : StarDriveTest
{
    // each GPU rounds a block's in-between colors its own way: up to 5 levels off the decoder on an RTX 3060 Ti
    const int ColorTolerance = 8;

    [TestMethod]
    public void AModelKeepsItsDxt1TexturesCompressedAndDecodesItsDxt5Ones()
    {
        StaticMesh mesh = Content.LoadStaticMesh("Model/Stations/Station_Small.fbx");
        LightingEffect fx = mesh.GetFirstEffect<LightingEffect>();
        Assert.IsNotNull(fx?.DiffuseMapTexture, "setup: the small station loads with its diffuse map");
        Assert.AreEqual(SurfaceFormat.Dxt1, fx.DiffuseMapTexture.Format, $"the DXT1 diffuse map {fx.DiffuseMapFile}");
        Assert.AreEqual(SurfaceFormat.Dxt1, fx.SpecularColorMapTexture.Format, $"the DXT1 specular map {fx.SpecularColorMapFile}");
        Assert.AreEqual(SurfaceFormat.Dxt1, fx.TintMapTexture.Format, $"the DXT1 tint map {fx.TintMapFile}");
        Assert.AreEqual(SurfaceFormat.Color, fx.EmissiveMapTexture.Format, $"the DXT5 glow map {fx.EmissiveMapFile}");
        Assert.AreEqual(SurfaceFormat.Color, fx.NormalMapTexture.Format, $"the DXT5 normal map {fx.NormalMapFile}");
    }

    [TestMethod]
    public void TheTextureLoaderDecodesADxt1FileUnlessAskedToKeepIt()
    {
        using Texture2D tex = ImageUtils.LoadDds(Game.GraphicsDevice, "Content/Model/Stations/spacestation01_d.dds");
        Assert.AreEqual(SurfaceFormat.Color, tex.Format);
    }

    [TestMethod]
    [DataRow("Content/Model/Ships/Cordrazine/ship16_d_0.dds")]
    [DataRow("Content/Model/Ships/Remnant/ship09_n.dds")]
    [DataRow("Content/Model/Ships/Vulfen/ship14_t.dds")]
    [DataRow("Content/Model/Stations/spacestation01_d.dds")]
    public void ACompressedDxt1TextureDrawsLikeItsDecodedOne(string file)
    {
        DrawBothWays(file);
    }

    // the fourth texel of a three-color block is transparent, and a transparent texel draws as transparent black either way
    [TestMethod]
    public void ADxt1BlockOfThreeColorsAndATransparentTexelDrawsLikeItsDecodedOne()
    {
        string file = Path.Combine(Path.GetTempPath(), $"ModelTextureTests_{Guid.NewGuid():N}.dds");
        try
        {
            File.WriteAllBytes(file, Dxt1(8, 8, new (ushort, ushort)[]
            {
                (0xF800, 0x001F), // four colors: red to blue
                (0x07E0, 0xFFE0), // three colors and transparent: green to yellow
                (0x7BEF, 0x0861), // four colors: gray to dark green
                (0x1082, 0xC618), // three colors and transparent: dark to light gray
            }));
            XnaColor[] compressed = DrawBothWays(file);
            Assert.AreEqual(XnaColor.Transparent, compressed[7], "setup: the second block's fourth texel is transparent");
        }
        finally
        {
            File.Delete(file);
        }
    }

    // draws the file kept compressed and decoded, and returns the compressed drawing once the two draw alike
    XnaColor[] DrawBothWays(string file)
    {
        GraphicsDevice device = Game.GraphicsDevice;
        using Texture2D compressed = ImageUtils.LoadDds(device, file, keepDxt1: true);
        using Texture2D decoded = ImageUtils.LoadDds(device, file);
        Assert.AreEqual(SurfaceFormat.Dxt1, compressed.Format, $"setup: {file} stays compressed");
        Assert.AreEqual(SurfaceFormat.Color, decoded.Format, $"setup: {file} is decoded");

        XnaColor[] a = Draw(device, compressed);
        XnaColor[] b = Draw(device, decoded);
        for (int i = 0; i < a.Length; ++i)
        {
            int diff = Math.Max(Math.Abs(a[i].R - b[i].R), Math.Max(Math.Abs(a[i].G - b[i].G), Math.Abs(a[i].B - b[i].B)));
            if (diff > ColorTolerance || a[i].A != b[i].A)
                Assert.Fail($"{file} texel ({i % compressed.Width},{i / compressed.Width}) draws {a[i]} compressed and {b[i]} decoded");
        }
        return a;
    }

    static XnaColor[] Draw(GraphicsDevice device, Texture2D tex)
    {
        using var rt = new RenderTarget2D(device, tex.Width, tex.Height, mipMap: false, SurfaceFormat.Color, DepthFormat.None);
        RenderTargetBinding[] prev = device.GetRenderTargets();
        try
        {
            device.SetRenderTarget(rt);
            device.Clear(XnaColor.Magenta);
            using var batch = new SpriteBatch(device);
            batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
            batch.Draw(tex, Microsoft.Xna.Framework.Vector2.Zero, XnaColor.White);
            batch.End();
        }
        finally
        {
            device.SetRenderTargets(prev);
        }
        var pixels = new XnaColor[tex.Width * tex.Height];
        rt.GetData(pixels);
        return pixels;
    }

    // a DXT1 image of 4x4 blocks row by row, each block's texels taking its four codes in turn along every row
    static byte[] Dxt1(uint width, uint height, (ushort Color0, ushort Color1)[] blocks)
    {
        using var stream = new MemoryStream();
        using var w = new BinaryWriter(stream);
        w.Write(0x20534444u); // "DDS "
        w.Write(124u);
        w.Write(0x1007u); // caps, height, width and pixel format
        w.Write(height);
        w.Write(width);
        w.Write(0u);
        for (int i = 0; i < 13; ++i)
            w.Write(0u);
        w.Write(32u);
        w.Write(4u); // four-character code
        w.Write(0x31545844u); // "DXT1"
        for (int i = 0; i < 5; ++i)
            w.Write(0u);
        w.Write(0x1000u); // texture caps
        for (int i = 0; i < 4; ++i)
            w.Write(0u);
        foreach ((ushort color0, ushort color1) in blocks)
        {
            w.Write(color0);
            w.Write(color1);
            w.Write(0xE4E4E4E4u); // codes 0, 1, 2, 3 along each row
        }
        w.Flush();
        return stream.ToArray();
    }
}
