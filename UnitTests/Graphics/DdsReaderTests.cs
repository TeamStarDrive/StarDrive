using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game.Data.Texture;

namespace UnitTests.Graphics;

/// <summary>
/// A DDS file's base level decodes from its own data whatever its header says of its size: a flagged linear size too
/// small for the image, as some tools write, is not trusted, and a file holding less than its base level is refused.
/// Each color channel decodes from its own bits to its full range.
/// </summary>
[TestClass]
public class DdsReaderTests
{
    const uint LinearSizeFlags = 0x81007;
    const uint PlainFlags = 0x1007;

    [TestMethod]
    [DataRow(LinearSizeFlags, 128u)]
    [DataRow(PlainFlags, 0u)]
    [DataRow(LinearSizeFlags, 32u)]
    public void ADdsDecodesItsWholeBaseLevelWhateverItsHeaderSaysOfItsSize(uint flags, uint linearSize)
    {
        var dds = new DxtReader(Checkerboard(flags, linearSize, blocks: 16));
        Assert.AreEqual(16, dds.Width, "width");
        Assert.AreEqual(16, dds.Height, "height");
        for (int y = 0; y < 16; ++y)
            for (int x = 0; x < 16; ++x)
                Assert.AreEqual(IsWhiteBlock(x / 4, y / 4) ? Color.White : Color.Black, dds.DecodedImage[y * 16 + x], $"pixel ({x},{y})");
    }

    [TestMethod]
    public void ADdsHoldingLessThanItsBaseLevelIsRefused()
    {
        Assert.ThrowsExactly<InvalidDataException>(() => new DxtReader(Checkerboard(PlainFlags, 0u, blocks: 8)));
    }

    // a 5-bit or 6-bit channel repeats its own top bits below them, so full blue is 255 and a green of 32 in 63 is 130
    [TestMethod]
    [DataRow(false, (ushort)0x001F, 0, 0, 255)]
    [DataRow(false, (ushort)0x0400, 0, 130, 0)]
    [DataRow(false, (ushort)0xF800, 255, 0, 0)]
    [DataRow(true, (ushort)0x001F, 0, 0, 255)]
    [DataRow(true, (ushort)0x0400, 0, 130, 0)]
    [DataRow(true, (ushort)0xF800, 255, 0, 0)]
    public void ADxtColorDecodesEachChannelFromItsOwnBits(bool dxt5, ushort color, int red, int green, int blue)
    {
        var dds = new DxtReader(Dxt(dxt5, 4, 4, PlainFlags, 0u, new[] { color }));
        Assert.AreEqual(new Color(red, green, blue), dds.DecodedImage[0], dxt5 ? "DXT5" : "DXT1");
    }

    // a GPU samples DXT1 only in whole 4x4 blocks of one image, so only such an image keeps its blocks undecoded, knowing its base level's size
    [TestMethod]
    [DataRow(false, 8u, 8u, 1u, PlainFlags, 0u, true)]
    [DataRow(false, 8u, 8u, 1u, LinearSizeFlags, 32u, true)]
    [DataRow(false, 6u, 8u, 1u, PlainFlags, 0u, false)]
    [DataRow(false, 8u, 6u, 1u, PlainFlags, 0u, false)]
    [DataRow(false, 8u, 8u, 2u, PlainFlags, 0u, false)]
    [DataRow(true, 8u, 8u, 1u, PlainFlags, 0u, false)]
    public void OnlyADxt1ImageOfWholeBlocksKeepsItsBlocks(bool dxt5, uint width, uint height, uint depth, uint flags, uint linearSize, bool kept)
    {
        // blocks past the four of an 8x8 base level, as smaller levels or a second slice would be
        byte[] file = Dxt(dxt5, width, height, flags, linearSize, new ushort[] { 0x001F, 0x07E0, 0xF800, 0xFFFF, 0x1234, 0x4321, 0x0001, 0x0002, 0x0003 });
        BitConverter.GetBytes(depth).CopyTo(file, 24); // the header's depth
        var dds = new DxtReader(new MemoryStream(file), keepDxt1Blocks: true);
        if (kept)
        {
            Assert.AreEqual(32, dds.Dxt1BaseLevelSize, "the base level's four blocks");
            CollectionAssert.AreEqual(file.AsSpan(128, 32).ToArray(), dds.Dxt1Blocks.AsSpan(0, 32).ToArray(), "as the file holds them");
            Assert.AreEqual(0, dds.DecodedImage.Length, "and nothing decoded");
        }
        else
        {
            Assert.IsNull(dds.Dxt1Blocks, "no blocks kept");
            Assert.AreEqual(new Color(0, 0, 255), dds.DecodedImage[0], "the image decoded instead");
        }
    }

    static bool IsWhiteBlock(int bx, int by) => (bx + by) % 2 == 0;

    // a 16x16 image of 4x4 blocks in a white and black checkerboard, holding only its first blocks
    static byte[] Checkerboard(uint flags, uint linearSize, int blocks)
    {
        var colors = new ushort[blocks];
        for (int b = 0; b < blocks; ++b)
            colors[b] = IsWhiteBlock(b % 4, b / 4) ? (ushort)0xFFFF : (ushort)0;
        return Dxt(dxt5: false, 16, 16, flags, linearSize, colors);
    }

    // a DXT1 or opaque DXT5 image of solid 4x4 blocks, one color each, row by row
    static byte[] Dxt(bool dxt5, uint width, uint height, uint flags, uint linearSize, ushort[] blockColors)
    {
        using var stream = new MemoryStream();
        using var w = new BinaryWriter(stream);
        w.Write(0x20534444u); // "DDS "
        w.Write(124u);
        w.Write(flags);
        w.Write(height);
        w.Write(width);
        w.Write(linearSize);
        for (int i = 0; i < 13; ++i)
            w.Write(0u);
        w.Write(32u);
        w.Write(4u); // four-character code
        w.Write(dxt5 ? 0x35545844u : 0x31545844u); // "DXT5" or "DXT1"
        for (int i = 0; i < 5; ++i)
            w.Write(0u);
        w.Write(0x1000u); // texture caps
        for (int i = 0; i < 4; ++i)
            w.Write(0u);
        foreach (ushort color in blockColors)
        {
            if (dxt5)
                w.Write(0xFFFFul); // both alpha ends 255, every pixel on the first
            w.Write(color);
            w.Write(color);
            w.Write(0u);
        }
        w.Flush();
        return stream.ToArray();
    }
}
