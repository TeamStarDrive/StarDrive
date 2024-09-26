using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game;

namespace UnitTests.Data
{
    [TestClass]
    public class TestLocalizedText : StarDriveTest
    {
        public TestLocalizedText()
        {
        }

        // restore default language settings after these tests
        [ClassCleanup]
        public static void ClassTeardown()
        {
            ResourceManager.LoadLanguage(Language.English);
            Fonts.LoadFonts(ResourceManager.RootContent, Language.English);
        }

        [TestMethod]
        public void LocalizedIdIsDynamic()
        {
            var textNewGame = new LocalizedText(id:1);
            Assert.IsTrue(textNewGame.NotEmpty);

            ResourceManager.LoadLanguage(Language.English);
            AssertEqual("New Game", textNewGame.Text);

            ResourceManager.LoadLanguage(Language.Spanish);
            AssertEqual("Nueva partida", textNewGame.Text);

            ResourceManager.LoadLanguage(Language.Ukrainian);
            AssertEqual("Нова гра", textNewGame.Text);

            ResourceManager.LoadLanguage(Language.German);
            AssertEqual("Neues Spiel", textNewGame.Text);
        }

        [TestMethod]
        public void GameTextEnumIsDynamic()
        {
            LocalizedText textNewGame = GameText.NewGame;
            Assert.IsTrue(textNewGame.NotEmpty);

            ResourceManager.LoadLanguage(Language.English);
            AssertEqual("New Game", textNewGame.Text);

            ResourceManager.LoadLanguage(Language.Spanish);
            AssertEqual("Nueva partida", textNewGame.Text);

            ResourceManager.LoadLanguage(Language.Ukrainian);
            AssertEqual("Нова гра", textNewGame.Text);

            ResourceManager.LoadLanguage(Language.German);
            AssertEqual("Neues Spiel", textNewGame.Text);
        }

        [TestMethod]
        public void RawTextIsConstant()
        {
            var rawText = new LocalizedText("RawText: {1}", LocalizationMethod.RawText);
            Assert.IsTrue(rawText.NotEmpty);

            ResourceManager.LoadLanguage(Language.English);
            AssertEqual("RawText: {1}", rawText.Text);

            ResourceManager.LoadLanguage(Language.Spanish);
            AssertEqual("RawText: {1}", rawText.Text); // should not change

            ResourceManager.LoadLanguage(Language.Ukrainian);
            AssertEqual("RawText: {1}", rawText.Text); // should not change

            ResourceManager.LoadLanguage(Language.German);
            AssertEqual("RawText: {1}", rawText.Text); // should not change
        }

        [TestMethod]
        public void ParsedTextIsDynamic()
        {
            var parsed1 = new LocalizedText("Parsed: {1} ", LocalizationMethod.Parse);
            var parsed2 = new LocalizedText("Parsed: {1} {2} ", LocalizationMethod.Parse);
            Assert.IsTrue(parsed1.NotEmpty);

            ResourceManager.LoadLanguage(Language.English);
            AssertEqual("Parsed: New Game ", parsed1.Text);
            AssertEqual("Parsed: New Game Load Game ", parsed2.Text);

            ResourceManager.LoadLanguage(Language.Spanish);
            AssertEqual("Parsed: Nueva partida ", parsed1.Text);
            AssertEqual("Parsed: Nueva partida Cargar partida ", parsed2.Text);

            ResourceManager.LoadLanguage(Language.Ukrainian);
            AssertEqual("Parsed: Нова гра ", parsed1.Text);
            AssertEqual("Parsed: Нова гра Завантажити гру ", parsed2.Text);

            ResourceManager.LoadLanguage(Language.German);
            AssertEqual("Parsed: Neues Spiel ", parsed1.Text);
            AssertEqual("Parsed: Neues Spiel Spiel laden ", parsed2.Text);
        }

        [TestMethod]
        public void ParseTextSplitsCorrectlyOnSpaces()
        {
            string text = "Big brown fox   jumps over  the gray dog";
            AssertEqual("Big brown fox jumps over the gray dog",
                            Fonts.Arial12.ParseText(text, 300));
        }

        [TestMethod]
        public void ParseTextSplitsCorrectlyOnNewLines()
        {
            string text = "Big brown fox, \n jumps over\nthe gray\n dog";
            AssertEqual(new string[]{"Big brown fox,", "jumps over", "the gray", "dog"},
                              Fonts.Arial12.ParseTextToLines(text, 300));
        }

        [TestMethod]
        public void ParseTextSplitsCorrectlyOnNewLinesThatNeverFit()
        {
            string text = "Terraforming Planet";
            AssertEqual("Terraforming\nPlanet",
                            Fonts.Tahoma10.ParseText(text, 73));
        }
        
        [TestMethod]
        public void ParseTextHandlesTabCharacter()
        {
            string text = "Big brown fox,\n\tjumps over\nthe gray\t\n\tdog";
            AssertEqual(new string[]{"Big brown fox,", "    jumps over",
                                           "the gray    ",   "    dog"},
                              Fonts.Arial12.ParseTextToLines(text, 300));
        }

        [TestMethod]
        public void ParseTextWrapsCorrectly()
        {
            string text = "xxxx xxxx xxxx xxxx xxxx xxxx";
            float wx = Fonts.Arial12.TextWidth("x");

            // 8 chars: "xxxx xxxx" is still too long, so it should split
            //          every block
            AssertEqual("xxxx\nxxxx\nxxxx\nxxxx\nxxxx\nxxxx",
                            Fonts.Arial12.ParseText(text, wx*8));

            // 9 chars: "xxxx xxxx" is 9 chars, it should split perfectly
            //          into 3 chunks
            AssertEqual("xxxx xxxx\nxxxx xxxx\nxxxx xxxx",
                            Fonts.Arial12.ParseText(text, wx*9));
        }

        Array<string> GetTextErrors(string[] tokens)
        {
            var errors = new Array<string>();
            var fonts = new []
            {
                Fonts.Arial8Bold,
                Fonts.Arial10,
                Fonts.Arial11Bold,
                Fonts.Arial12,
                Fonts.Arial12Bold,
                Fonts.Arial14Bold,
                Fonts.Arial20Bold,
                Fonts.Consolas18,
                Fonts.Arial20Bold,
                Fonts.Laserian14,
            };
            foreach (var font in fonts)
            {
                foreach (string text in tokens)
                {
                    try { font.ParseText(text, 120); }
                    catch (Exception e)
                    {
                        errors.Add($"ParseText failed Font={font.Name} Error={e.Message} Text=\"{text}\"");
                        break;
                    }
                    try { font.MeasureString(text); }
                    catch (Exception e)
                    {
                        errors.Add($"MeasureString failed Font={font.Name} Error={e.Message} Text=\"{text}\"");
                        break;
                    }
                }
            }
            return errors;
        }

        [TestMethod]
        public void EnsureEnglishTextIsDrawable()
        {
            ResourceManager.LoadLanguage(Language.English);
            Fonts.LoadFonts(ResourceManager.RootContent, Language.English);
            var err = GetTextErrors(Localizer.EnumerateTokens().ToArr());
            if (err.NotEmpty)
                Assert.Fail(string.Join("\n", err));
        }

        [TestMethod]
        public void EnsureRussianTextIsDrawable()
        {
            ResourceManager.LoadLanguage(Language.Russian);
            Fonts.LoadFonts(ResourceManager.RootContent, Language.Russian);
            var err = GetTextErrors(Localizer.EnumerateTokens().ToArr());
            if (err.NotEmpty)
                Assert.Fail(string.Join("\n", err));
        }

        [TestMethod]
        public void EnsureSpanishTextIsDrawable()
        {
            ResourceManager.LoadLanguage(Language.Spanish);
            Fonts.LoadFonts(ResourceManager.RootContent, Language.Spanish);
            var err = GetTextErrors(Localizer.EnumerateTokens().ToArr());
            if (err.NotEmpty)
                Assert.Fail(string.Join("\n", err));
        }

        [TestMethod]
        public void EnsureUkrainianTextIsDrawable()
        {
            ResourceManager.LoadLanguage(Language.Ukrainian);
            Fonts.LoadFonts(ResourceManager.RootContent, Language.Ukrainian);
            var err = GetTextErrors(Localizer.EnumerateTokens().ToArr());
            if (err.NotEmpty)
                Assert.Fail(string.Join("\n", err));
        }

        [TestMethod]
        public void EnsureGermanTextIsDrawable()
        {
            ResourceManager.LoadLanguage(Language.German);
            Fonts.LoadFonts(ResourceManager.RootContent, Language.German);
            var err = GetTextErrors(Localizer.EnumerateTokens().ToArr());
            if (err.NotEmpty)
                Assert.Fail(string.Join("\n", err));
        }

        [TestMethod]
        public void ParseTextDoesNotCrashOnInvalidCharacters()
        {
            ResourceManager.LoadLanguage(Language.English);
            Fonts.LoadFonts(ResourceManager.RootContent, Language.English);

            string[] invalidSymbols =
            {
                "$", "€", "`", "@"
            };
            var err = GetTextErrors(invalidSymbols);
            if (err.NotEmpty)
                Assert.Fail(string.Join("\n", err));
        }

        [TestMethod]
        public void ParsedTextSupportsNameIds()
        {
            var parsed1 = new LocalizedText("Parsed: {NewGame} ", LocalizationMethod.Parse);
            var parsed2 = new LocalizedText("Parsed: {NewGame} {LoadGame} ", LocalizationMethod.Parse);
            Assert.IsTrue(parsed1.NotEmpty);

            ResourceManager.LoadLanguage(Language.English);
            AssertEqual("Parsed: New Game ", parsed1.Text);
            AssertEqual("Parsed: New Game Load Game ", parsed2.Text);

            ResourceManager.LoadLanguage(Language.Spanish);
            AssertEqual("Parsed: Nueva partida ", parsed1.Text);
            AssertEqual("Parsed: Nueva partida Cargar partida ", parsed2.Text);

            ResourceManager.LoadLanguage(Language.Ukrainian);
            AssertEqual("Parsed: Нова гра ", parsed1.Text);
            AssertEqual("Parsed: Нова гра Завантажити гру ", parsed2.Text);

            ResourceManager.LoadLanguage(Language.German);
            AssertEqual("Parsed: Neues Spiel ", parsed1.Text);
            AssertEqual("Parsed: Neues Spiel Spiel laden ", parsed2.Text);
        }

        double MB(long mem) => mem / (1024.0*1024.0);

        [TestMethod]
        public void EnsureTextParsePerformance()
        {
            ResourceManager.LoadLanguage(Language.English);
            var texts = new Array<string>();
            for (int id = 1; id <= 8000; ++id)
                if (Localizer.Contains(id))
                    texts.Add($"Parsed: {{{id}}}");

            long mem1 = GC.GetTotalMemory(true);
            var sw = Stopwatch.StartNew();
            long n = 0;
            for (int i = 0; i < 10000; ++i)
            {
                foreach (string text in texts)
                {
                    string parsed = LocalizedText.ParseText(text);
                    ++n;
                }
            }

            double e = sw.Elapsed.TotalSeconds;
            Log.Info($"Elapsed: {e:0.00}s  Total: {n}  Parse:{(e/n)*1000000:0.0000}us");

            long mem2 = GC.GetTotalMemory(false);
            Log.Info($"Mem1: {MB(mem1):0.00}MB  Mem2: {MB(mem2):0.00}MB  D:{MB(mem2-mem1):0.00}MB");
        }
    }
}
