using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;

namespace UnitTests.Graphics;

[TestClass]
public class ScreenTargetTests
{
    [TestMethod]
    public void ALowerResolutionFillsTheDisplayAndKeepsItsShape()
    {
        Assert.AreEqual(new XnaRectangle(128, 0, 2304, 1440), GameBase.FitScreen(2560, 1440, 1680, 1050));
        Assert.AreEqual(new XnaRectangle(380, 0, 1800, 1440), GameBase.FitScreen(2560, 1440, 1280, 1024));
        Assert.AreEqual(new XnaRectangle(0, 0, 3840, 2160), GameBase.FitScreen(3840, 2160, 1920, 1080));
        Assert.AreEqual(new XnaRectangle(0, 180, 2560, 1080), GameBase.FitScreen(2560, 1440, 2560, 1080));
    }

    [TestMethod]
    [DataRow(2560, 1440, 1680, 1050)]
    [DataRow(2560, 1440, 1920, 1080)]
    [DataRow(2560, 1440, 1280, 1024)]
    [DataRow(3840, 2160, 1920, 1080)]
    [DataRow(1920, 1080, 1366, 768)]
    [DataRow(2560, 1440, 2560, 1080)]
    public void PlacingTheMouseReadsBackTheSamePixel(int displayW, int displayH, int screenW, int screenH)
    {
        XnaRectangle r = GameBase.FitScreen(displayW, displayH, screenW, screenH);
        for (int x = 0; x < screenW; ++x)
        {
            (int wx, int wy) = GameBase.ScreenToWindow(x, screenH / 2, r, screenW, screenH);
            Assert.AreEqual((x, screenH / 2), GameBase.WindowToScreen(wx, wy, r, screenW, screenH), $"x={x}");
        }
        for (int y = 0; y < screenH; ++y)
        {
            (int wx, int wy) = GameBase.ScreenToWindow(screenW / 2, y, r, screenW, screenH);
            Assert.AreEqual((screenW / 2, y), GameBase.WindowToScreen(wx, wy, r, screenW, screenH), $"y={y}");
        }
    }

    [TestMethod]
    public void TheMouseOverTheBlackBarsIsAtTheScreensEdge()
    {
        XnaRectangle r = GameBase.FitScreen(2560, 1440, 1680, 1050);
        Assert.AreEqual((0, 0), GameBase.WindowToScreen(0, 0, r, 1680, 1050));
        Assert.AreEqual((0, 525), GameBase.WindowToScreen(127, 720, r, 1680, 1050));
        Assert.AreEqual((1679, 1049), GameBase.WindowToScreen(2559, 1439, r, 1680, 1050));
        Assert.AreEqual((840, 525), GameBase.WindowToScreen(1280, 720, r, 1680, 1050));
    }
}
