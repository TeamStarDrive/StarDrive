using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game.Codex;

namespace UnitTests.Serialization
{
    [TestClass]
    public class StyledTextParserTests : StarDriveTest
    {
        [TestMethod]
        public void PlainTextRoundTrips()
        {
            StyledRun[] runs = StyledTextParser.Parse("just plain text");
            AssertEqual(1, runs.Length);
            AssertEqual("just plain text", runs[0].Text);
            Assert.IsFalse(runs[0].Bold);
            Assert.IsNull(runs[0].Url);
            Assert.IsNull(runs[0].ImagePath);
        }

        [TestMethod]
        public void ColorTagAppliesNamedColor()
        {
            // Use Warning (not Caption) so the auto double-break doesn't fire.
            StyledRun[] runs = StyledTextParser.Parse("a <color=Warning>b</color> c");
            AssertEqual(3, runs.Length);
            AssertEqual(CodexStyles.Default,  runs[0].Color);
            AssertEqual(CodexStyles.Warning,  runs[1].Color);
            AssertEqual(CodexStyles.Default,  runs[2].Color);
            AssertEqual("b", runs[1].Text);
        }

        [TestMethod]
        public void CaptionColorMarksRunAndInsertsDoubleBreak()
        {
            StyledRun[] runs = StyledTextParser.Parse("<color=Caption>Heading</color>body");
            // Caption run, then two "\n" runs, then body — four total.
            AssertEqual(4, runs.Length);
            Assert.IsTrue(runs[0].IsCaption);
            AssertEqual(CodexStyles.Caption, runs[0].Color);
            Assert.IsTrue(runs[1].IsLineBreak);
            Assert.IsTrue(runs[2].IsLineBreak);
            AssertEqual("body", runs[3].Text);
            Assert.IsFalse(runs[3].IsCaption);
        }

        [TestMethod]
        public void TrailingCaptionSkipsDoubleBreak()
        {
            StyledRun[] runs = StyledTextParser.Parse("body <color=Caption>End</color>");
            // No content after caption → no trailing break inserted.
            int breakCount = 0;
            foreach (StyledRun r in runs)
                if (r.IsLineBreak) breakCount++;
            AssertEqual(0, breakCount);
        }

        [TestMethod]
        public void UnknownColorFallsBackToDefault()
        {
            StyledRun[] runs = StyledTextParser.Parse("<color=Bogus>x</color>");
            AssertEqual(1, runs.Length);
            AssertEqual(CodexStyles.Default, runs[0].Color);
            AssertEqual("x", runs[0].Text);
        }

        [TestMethod]
        public void BoldTagAppliesBoldFlag()
        {
            StyledRun[] runs = StyledTextParser.Parse("warning: <b>hot</b> coffee");
            AssertEqual(3, runs.Length);
            Assert.IsFalse(runs[0].Bold);
            Assert.IsTrue(runs[1].Bold);
            Assert.IsFalse(runs[2].Bold);
            AssertEqual("hot", runs[1].Text);
        }

        [TestMethod]
        public void UrlTagCarriesHref()
        {
            StyledRun[] runs = StyledTextParser.Parse("see <url=https://x.com>x</url>");
            AssertEqual(2, runs.Length);
            Assert.IsNull(runs[0].Url);
            AssertEqual("https://x.com", runs[1].Url);
            AssertEqual("x", runs[1].Text);
        }

        [TestMethod]
        public void UrlForcesUrlColorAndRestoresAfterClose()
        {
            StyledRun[] runs = StyledTextParser.Parse("<color=Warning>red <url=https://x.com>link</url> red</color>");
            // Walk runs; the url-span run uses CodexStyles.Url; surrounding spans use Warning.
            bool sawUrlColor = false;
            bool sawWarningAfter = false;
            for (int i = 0; i < runs.Length; ++i)
            {
                if (runs[i].Url != null)
                {
                    AssertEqual(CodexStyles.Url, runs[i].Color);
                    sawUrlColor = true;
                }
                else if (sawUrlColor && runs[i].Text != null && runs[i].Text.Contains("red"))
                {
                    AssertEqual(CodexStyles.Warning, runs[i].Color);
                    sawWarningAfter = true;
                }
            }
            Assert.IsTrue(sawUrlColor && sawWarningAfter, "url color must apply, then restore outer Warning");
        }

        [TestMethod]
        public void InnerColorInsideUrlRestoresUrlColorOnClose()
        {
            // A nested <color> inside <url> must restore CodexStyles.Url on close,
            // not the global Default — otherwise the tail of the url renders white.
            StyledRun[] runs = StyledTextParser.Parse("<url=https://x.com>blue <color=Warning>warn</color> blue tail</url>");
            // Walk runs; tokens before/after the inner color stay url-blue,
            // and the url is still active for the tail.
            bool sawWarnInside = false, sawBlueTail = false;
            for (int i = 0; i < runs.Length; ++i)
            {
                if (runs[i].Url == null) continue;
                if (runs[i].Text != null && runs[i].Text.Contains("warn"))
                {
                    AssertEqual(CodexStyles.Warning, runs[i].Color);
                    sawWarnInside = true;
                }
                else if (runs[i].Text != null && runs[i].Text.Contains("tail"))
                {
                    AssertEqual(CodexStyles.Url, runs[i].Color);
                    sawBlueTail = true;
                }
            }
            Assert.IsTrue(sawWarnInside && sawBlueTail, "warn span uses Warning; tail restores Url, not Default");
        }

        [TestMethod]
        public void ColorAndBoldCanCombine()
        {
            // Different tag classes are allowed to overlap; same class cannot nest.
            StyledRun[] runs = StyledTextParser.Parse("<color=Warning><b>x</b></color>");
            AssertEqual(1, runs.Length);
            AssertEqual(CodexStyles.Warning, runs[0].Color);
            Assert.IsTrue(runs[0].Bold);
        }

        [TestMethod]
        public void SameClassNestingTreatsInnerOpenAsLiteral()
        {
            // The inner <color=...> open is refused → falls through as literal text.
            StyledRun[] runs = StyledTextParser.Parse("<color=Caption>outer <color=Warning>x</color></color>");
            // outer caption span, inner open as literal, inner close pops the outer.
            // Expect: "outer <color=Warning>x" with caption color, then "" tail.
            bool sawLiteralOpen = false;
            foreach (StyledRun r in runs)
                if (r.Text != null && r.Text.Contains("<color=Warning>"))
                    sawLiteralOpen = true;
            Assert.IsTrue(sawLiteralOpen, "inner same-class open should be emitted as literal text");
        }

        [TestMethod]
        public void UnknownTagPassesThroughLiterally()
        {
            StyledRun[] runs = StyledTextParser.Parse("a <foo>b</foo> c");
            // All literal — no recognized tag was consumed.
            var sb = new System.Text.StringBuilder();
            foreach (StyledRun r in runs) sb.Append(r.Text);
            AssertEqual("a <foo>b</foo> c", sb.ToString());
        }

        [TestMethod]
        public void ImageRunEmittedAsAtom()
        {
            StyledRun[] runs = StyledTextParser.Parse("press <img>UI/icon_research</img> to research");
            // Expect: "press ", img, "\n" (group-break), " to research"
            int imgCount = 0, breakCount = 0;
            foreach (StyledRun r in runs)
            {
                if (r.IsImage) imgCount++;
                else if (r.IsLineBreak) breakCount++;
            }
            AssertEqual(1, imgCount);
            AssertEqual(1, breakCount);
            // The break should come AFTER the image
            int imgIdx = -1, breakIdx = -1;
            for (int i = 0; i < runs.Length; ++i)
            {
                if (runs[i].IsImage) imgIdx = i;
                else if (runs[i].IsLineBreak) breakIdx = i;
            }
            Assert.IsTrue(imgIdx < breakIdx, "line break must follow the image");
        }

        [TestMethod]
        public void ConsecutiveImagesShareOneTrailingBreak()
        {
            StyledRun[] runs = StyledTextParser.Parse("<img>a</img><img>b</img><img>c</img>after");
            int imgCount = 0, breakCount = 0;
            foreach (StyledRun r in runs)
            {
                if (r.IsImage) imgCount++;
                else if (r.IsLineBreak) breakCount++;
            }
            AssertEqual(3, imgCount);
            AssertEqual(1, breakCount);
        }

        [TestMethod]
        public void TrailingImageGroupSkipsBreak()
        {
            StyledRun[] runs = StyledTextParser.Parse("text<img>a</img><img>b</img>");
            int breakCount = 0;
            foreach (StyledRun r in runs)
                if (r.IsLineBreak) breakCount++;
            AssertEqual(0, breakCount);
        }

        [TestMethod]
        public void NewlinesPassThroughUnchanged()
        {
            StyledRun[] runs = StyledTextParser.Parse("line1\nline2");
            // Newline stays embedded in the text run; renderer splits it.
            Assert.IsTrue(runs[0].Text.Contains("\n"));
        }

        [TestMethod]
        public void YamlBackslashNRoundTripsAsRealNewline()
        {
            // Verify that double-quoted yaml strings emit real '\n' (LF) so the
            // renderer's line-break handling fires on legacy soft-newline markers.
            using var parser = new Ship_Game.Data.Yaml.YamlParser("x",
                new System.IO.StringReader("Sample:\n ENG: \"line1 \\n line2\""));
            foreach (var node in parser.Root)
            {
                var eng = node.GetSubNode("ENG").Value as string;
                Assert.IsNotNull(eng);
                Assert.IsTrue(eng.Contains("\n"), "YamlParser should decode \\n to real LF");
                return;
            }
            Assert.Fail("no Sample node");
        }

        [TestMethod]
        public void LocalizerPlaceholdersPassThroughUnchanged()
        {
            StyledRun[] runs = StyledTextParser.Parse("hello {0}, your score is {1}");
            AssertEqual("hello {0}, your score is {1}", runs[0].Text);
        }

        [TestMethod]
        public void MalformedTagFallsThrough()
        {
            // Unclosed '<' should not crash; chars after stay literal.
            StyledRun[] runs = StyledTextParser.Parse("price < 100 credits");
            var sb = new System.Text.StringBuilder();
            foreach (StyledRun r in runs) sb.Append(r.Text);
            AssertEqual("price < 100 credits", sb.ToString());
        }

        [TestMethod]
        public void ListItemsBecomeBulletRuns()
        {
            StyledRun[] runs = StyledTextParser.Parse("Lead\n<li>first item</li>\n<li>second");

            // "Lead\n", bullet, "first item", "\n", bullet, "second"
            AssertEqual(6, runs.Length);
            AssertEqual("Lead\n", runs[0].Text);
            Assert.IsTrue(runs[1].IsBullet);
            Assert.IsFalse(runs[1].IsImage, "a bullet run is not an image run");
            AssertEqual("first item", runs[2].Text);
            Assert.IsTrue(runs[3].IsLineBreak);
            Assert.IsTrue(runs[4].IsBullet);
            AssertEqual("second", runs[5].Text);
        }

        [TestMethod]
        public void ListItemsKeepTheirStyle()
        {
            StyledRun[] runs = StyledTextParser.Parse("<li><b>bold</b> plain</li>");

            Assert.IsTrue(runs[0].IsBullet);
            Assert.IsTrue(runs[1].Bold);
            AssertEqual("bold", runs[1].Text);
            Assert.IsFalse(runs[2].Bold);
            AssertEqual(" plain", runs[2].Text);
        }
    }
}
