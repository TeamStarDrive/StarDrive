using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;

namespace UnitTests.Data
{
    [TestClass]
    public class TestLocalizedText : StarDriveTest
    {
        public TestLocalizedText()
        {
            CreateGameInstance();
        }

        [TestMethod]
        public void LocalizedIdIsDynamic()
        {
            var textNewGame = new LocalizedText(id:1);
            Assert.IsTrue(textNewGame.NotEmpty);

            ResourceManager.LoadLanguage(Language.English);
            Assert.AreEqual("New Game", textNewGame.Text);

            ResourceManager.LoadLanguage(Language.Spanish);
            Assert.AreEqual("Nueva partida", textNewGame.Text);
        }

        [TestMethod]
        public void GameTextEnumIsDynamic()
        {
            LocalizedText textNewGame = GameText.NewGame;
            Assert.IsTrue(textNewGame.NotEmpty);

            ResourceManager.LoadLanguage(Language.English);
            Assert.AreEqual("New Game", textNewGame.Text);

            ResourceManager.LoadLanguage(Language.Spanish);
            Assert.AreEqual("Nueva partida", textNewGame.Text);
        }

        [TestMethod]
        public void RawTextIsConstant()
        {
            var rawText = new LocalizedText("RawText: {1}", LocalizationMethod.RawText);
            Assert.IsTrue(rawText.NotEmpty);

            ResourceManager.LoadLanguage(Language.English);
            Assert.AreEqual("RawText: {1}", rawText.Text);

            ResourceManager.LoadLanguage(Language.Spanish);
            Assert.AreEqual("RawText: {1}", rawText.Text); // should not change
        }

        [TestMethod]
        public void ParsedTextIsDynamic()
        {
            var parsed1 = new LocalizedText("Parsed: {1} ", LocalizationMethod.Parse);
            var parsed2 = new LocalizedText("Parsed: {1} {2} ", LocalizationMethod.Parse);
            Assert.IsTrue(parsed1.NotEmpty);

            ResourceManager.LoadLanguage(Language.English);
            Assert.AreEqual("Parsed: New Game ", parsed1.Text);
            Assert.AreEqual("Parsed: New Game Load Game ", parsed2.Text);

            ResourceManager.LoadLanguage(Language.Spanish);
            Assert.AreEqual("Parsed: Nueva partida ", parsed1.Text);
            Assert.AreEqual("Parsed: Nueva partida Cargar partida ", parsed2.Text);
        }

        void ParseAllText(string[] tokens)
        {
            var fonts = new Map<string, SpriteFont>();
            fonts.Add("Arial12", Fonts.Arial12);
            fonts.Add("Arial12Bold", Fonts.Arial12Bold);

            foreach (var font in fonts)
            {
                foreach (string text in tokens)
                {
                    try
                    {
                        font.Value.MeasureString(text);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"MeasureString failed Font={font.Key} Text={text} Error={e.Message}");
                    }
                }
            }
        }

        [TestMethod]
        public void EnsureRussianTextIsDrawable()
        {
            Fonts.LoadFonts(ResourceManager.RootContent);
            ResourceManager.LoadLanguage(Language.Russian);
            string[] tokens = Localizer.EnumerateTokens().ToArray();
            ParseAllText(tokens);
        }

        [TestMethod]
        public void EnsureSpanishTextIsDrawable()
        {
            Fonts.LoadFonts(ResourceManager.RootContent);
            ResourceManager.LoadLanguage(Language.Spanish);
            string[] tokens = Localizer.EnumerateTokens().ToArray();
            ParseAllText(tokens);
        }

        [TestMethod]
        public void ParsedTextSupportsNameIds()
        {
            var parsed1 = new LocalizedText("Parsed: {NewGame} ", LocalizationMethod.Parse);
            var parsed2 = new LocalizedText("Parsed: {NewGame} {LoadGame} ", LocalizationMethod.Parse);
            Assert.IsTrue(parsed1.NotEmpty);

            ResourceManager.LoadLanguage(Language.English);
            Assert.AreEqual("Parsed: New Game ", parsed1.Text);
            Assert.AreEqual("Parsed: New Game Load Game ", parsed2.Text);

            ResourceManager.LoadLanguage(Language.Spanish);
            Assert.AreEqual("Parsed: Nueva partida ", parsed1.Text);
            Assert.AreEqual("Parsed: Nueva partida Cargar partida ", parsed2.Text);
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
