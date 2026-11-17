using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game;
using Ship_Game.Data.Texture;
using Ship_Game.SpriteSystem;
using XnaColor = Microsoft.Xna.Framework.Color;
using XnaRect = Microsoft.Xna.Framework.Rectangle;

namespace UnitTests.Graphics;

/// <summary>
/// A texture read back from the texture atlas cache draws as the same texture loaded straight from its file:
/// premultiplied once, whether it is kept as its own cache file, packed into a compressed atlas or packed into a PNG atlas.
/// </summary>
[TestClass]
public class TextureCacheTests : StarDriveTest
{
    [TestMethod]
    public void ATextureKeptAsItsOwnCacheFileDrawsItsSoftEdgesAsItsArtDoes()
    {
        int compared = CompareSoftEdgesWithArt("Textures/Arcs", packed: false);
        Assert.IsTrue(compared >= 5, $"setup: the weapon arcs are kept as their own cache files, {compared} compared");
    }

    [TestMethod]
    public void ATexturePackedIntoACompressedAtlasDrawsItsSoftEdgesAsItsArtDoes()
    {
        int compared = CompareSoftEdgesWithArt("Textures/Stars/LargeStars", packed: true);
        Assert.IsTrue(compared >= 10, $"setup: the large background stars are packed into one atlas, {compared} compared");
    }

    [TestMethod]
    public void ATextureInAPngCacheIsExactlyItsArt()
    {
        int packed = CompareExactlyWithArt("Textures/NewUI", packed: true);
        Assert.IsTrue(packed >= 50, $"setup: the main UI icons are packed into a PNG atlas, {packed} compared");
        int own = CompareExactlyWithArt("Textures/UI", packed: false);
        Assert.IsTrue(own >= 3, $"setup: the sensor circle textures are kept as their own PNG cache files, {own} compared");
    }

    [TestMethod]
    public void PixelsALoaderAlreadyPremultipliedComeBackFromTheCachePremultipliedOnce()
    {
        const int size = 32;
        var straight = new XnaColor[size * size];
        for (int y = 0; y < size; ++y)
            for (int x = 0; x < size; ++x)
            {
                float d = MathF.Sqrt((x - 15.5f) * (x - 15.5f) + (y - 15.5f) * (y - 15.5f)) / 16f;
                straight[y * size + x] = new XnaColor(220, 140, 60, (int)(255 * Math.Clamp(1f - d, 0f, 1f)));
            }
        var premultiplied = (XnaColor[])straight.Clone();
        ImageUtils.MultiplyAlpha(premultiplied, premultiplied.Length);

        string dir = Path.Combine(Path.GetTempPath(), "SDTextureCacheTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            XnaColor[] fromPremultiplied = ThroughTheCache(dir, "premultiplied", premultiplied, loaderPremultiplied: true);
            AssertSoftEdgesMatch(premultiplied, fromPremultiplied, "pixels an XNB loader premultiplied");
            XnaColor[] fromStraight = ThroughTheCache(dir, "straight", straight, loaderPremultiplied: false);
            AssertSoftEdgesMatch(premultiplied, fromStraight, "straight pixels");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [TestMethod]
    public void AFullyTransparentTextureWithColorUnderItStaysTransparentThroughTheCache()
    {
        var hidden = new XnaColor[32 * 32];
        for (int i = 0; i < hidden.Length; ++i)
            hidden[i] = new XnaColor(200, 100, 50, 0);

        string dir = Path.Combine(Path.GetTempPath(), "SDTextureCacheTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            XnaColor[] drawn = ThroughTheCache(dir, "hidden", hidden, loaderPremultiplied: false);
            for (int i = 0; i < drawn.Length; ++i)
                if (drawn[i] != XnaColor.Transparent)
                    Assert.Fail($"pixel {i} draws {drawn[i]}, where its art draws nothing");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    XnaColor[] ThroughTheCache(string dir, string name, XnaColor[] pixels, bool loaderPremultiplied)
    {
        int size = (int)Math.Sqrt(pixels.Length);
        using var tex = new Texture2D(Game.GraphicsDevice, size, size, false, SurfaceFormat.Color);
        tex.SetData(pixels);
        var info = new TextureInfo(tex) { Name = name, Premultiplied = loaderPremultiplied };
        string file = info.SaveAsDds(Path.Combine(dir, name + ".dds"));
        Assert.IsTrue(file.EndsWith(".dds"), $"setup: {name} is cached as a DDS file");
        using Texture2D back = ResourceManager.RootContent.LoadUncachedTextureCacheFile(new FileInfo(file));
        var drawn = new XnaColor[size * size];
        back.GetData(drawn);
        return drawn;
    }

    int CompareSoftEdgesWithArt(string folder, bool packed)
    {
        int compared = 0;
        using TextureAtlas atlas = TextureAtlas.FromFolder(folder, useTextureCache: false);
        for (int i = 0; i < atlas.Count; ++i)
        {
            SubTexture sub = atlas[i];
            if (IsPacked(sub) != packed || !TryLoadArt(folder, sub.Name, out XnaColor[] art))
                continue;
            if (CountSoft(art) < 16)
                continue;
            AssertSoftEdgesMatch(art, Drawn(sub), $"{folder}/{sub.Name}");
            ++compared;
        }
        return compared;
    }

    int CompareExactlyWithArt(string folder, bool packed)
    {
        int compared = 0;
        using TextureAtlas atlas = TextureAtlas.FromFolder(folder, useTextureCache: false);
        for (int i = 0; i < atlas.Count; ++i)
        {
            SubTexture sub = atlas[i];
            if (IsPacked(sub) != packed || !TryLoadArt(folder, sub.Name, out XnaColor[] art))
                continue;
            if (!packed && !File.Exists(CacheFile(folder, sub.Name, "png")))
                continue;
            XnaColor[] drawn = Drawn(sub);
            for (int p = 0; p < art.Length; ++p)
                if (art[p] != drawn[p])
                    Assert.Fail($"{folder}/{sub.Name}: pixel {p} draws {drawn[p]}, its art is {art[p]}");
            ++compared;
        }
        return compared;
    }

    static string CacheFile(string folder, string name, string ext)
        => $"{AtlasPath.GetCacheRoot()}/{folder.Replace('/', '_')}/{name}.{ext}";

    static bool IsPacked(SubTexture sub) => sub.Texture.Width != sub.Width || sub.Texture.Height != sub.Height;

    bool TryLoadArt(string folder, string name, out XnaColor[] art)
    {
        art = null;
        Texture2D tex;
        if (File.Exists($"Content/{folder}/{name}.png"))
            tex = ImageUtils.LoadPng(Game.GraphicsDevice, $"Content/{folder}/{name}.png");
        else if (File.Exists($"Content/{folder}/{name}.dds"))
            tex = ImageUtils.LoadDds(Game.GraphicsDevice, $"Content/{folder}/{name}.dds");
        else
            return false;
        art = new XnaColor[tex.Width * tex.Height];
        tex.GetData(art);
        tex.Dispose();
        return true;
    }

    static XnaColor[] Drawn(SubTexture sub)
    {
        var drawn = new XnaColor[sub.Width * sub.Height];
        sub.Texture.GetData(0, new XnaRect(sub.X, sub.Y, sub.Width, sub.Height), drawn, 0, drawn.Length);
        return drawn;
    }

    static int CountSoft(XnaColor[] pixels)
    {
        int soft = 0;
        foreach (XnaColor c in pixels)
            if (c.A is > 0 and < 255)
                ++soft;
        return soft;
    }

    // how bright the half-transparent pixels draw against the art, which DXT rounding moves by a few percent
    static void AssertSoftEdgesMatch(XnaColor[] art, XnaColor[] drawn, string what)
    {
        double artSum = 0, drawnSum = 0;
        for (int i = 0; i < art.Length; ++i)
        {
            if (art[i].A is 0 or 255)
                continue;
            artSum += Math.Max(art[i].R, Math.Max(art[i].G, art[i].B));
            drawnSum += Math.Max(drawn[i].R, Math.Max(drawn[i].G, drawn[i].B));
        }
        Assert.IsTrue(artSum > 0, $"setup: {what} has soft edges");
        double ratio = drawnSum / artSum;
        Assert.IsTrue(ratio is > 0.9 and < 1.1, $"{what}: its soft edges draw at {ratio:P0} of its art");
    }
}
