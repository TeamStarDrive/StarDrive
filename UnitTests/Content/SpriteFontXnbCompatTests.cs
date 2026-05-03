using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;

namespace UnitTests.Content
{
    /// <summary>
    /// Phase 3.3.A regression pin. The 24 SpriteFonts under game/Content/Fonts/ were
    /// restored from the XNA 3.1 baked .xnb files (tag <c>x31_Fonts</c>) to undo the
    /// Phase 2.3 MGCB rebake size regression. Those XNBs embed Texture2D as Dxt3 (BC2),
    /// which MonoGame WindowsDX 3.8's GPU sampling renders as solid white squares
    /// (squares-as-text bug; commit 10b35d779). Xna31Texture2DReader.Read intercepts
    /// that path and decompresses Dxt3 → RGBA8888 in software so the runtime texture
    /// lives as SurfaceFormat.Color. If anyone removes the Dxt3 → Color decode path,
    /// these tests fail before the user sees squares.
    /// </summary>
    [TestClass]
    public class SpriteFontXnbCompatTests : StarDriveTest
    {
        [TestMethod]
        public void RestoredFontXnbs_DecodeDxt3ToColor()
        {
            // Representative sample across the 24 fonts: bold/regular, system/embedded
            // TTF, and a wide glyph atlas (Pirulen20 was the largest pre-migration XNB).
            string[] fonts = { "Fonts/Arial14Bold", "Fonts/Tahoma10", "Fonts/Pirulen20" };

            foreach (string fontPath in fonts)
            {
                SpriteFont font = ResourceManager.RootContent.Load<SpriteFont>(fontPath);
                Assert.IsNotNull(font, $"{fontPath}: load returned null");
                Assert.IsTrue(font.LineSpacing > 0, $"{fontPath}: LineSpacing={font.LineSpacing} (expected > 0)");

                Texture2D tex = (Texture2D)typeof(SpriteFont)
                    .GetField("_texture", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(font);
                Assert.IsNotNull(tex, $"{fontPath}: SpriteFont._texture reflection returned null (MonoGame internals changed?)");

                // The squares-as-text bug fires when this format is Dxt3. Pinning to Color
                // makes the regression explicit if the Xna31Texture2DReader decode path
                // is ever weakened.
                Assert.AreEqual(SurfaceFormat.Color, tex.Format,
                    $"{fontPath}: underlying texture format is {tex.Format} — Dxt3 GPU sampling is broken under MonoGame WindowsDX 3.8 and renders as squares. Decode path in Xna31Texture2DReader.Read was bypassed.");

                Assert.IsTrue(tex.Width > 0 && tex.Height > 0, $"{fontPath}: zero-size atlas ({tex.Width}x{tex.Height})");
            }
        }
    }
}
