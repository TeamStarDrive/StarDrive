using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;

namespace UnitTests.Universe
{
    [TestClass]
    public class ResearchStationCartoucheTests : StarDriveTest
    {
        const float SlotWidth = 182;

        static IEnumerable<(string Lang, string Text)> ReadTranslations(string token)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Content/GameText.yaml");
            Assert.IsTrue(File.Exists(path), $"GameText.yaml not found at {path}");

            bool inToken = false;
            foreach (string line in File.ReadLines(path))
            {
                if (line.StartsWith(token + ":"))       { inToken = true; continue; }
                if (inToken && !line.StartsWith(" "))   break;
                if (!inToken) continue;

                Match m = Regex.Match(line, @"^\s*([A-Z]{3}):\s*""(.*)""\s*$");
                if (m.Success)
                    yield return (m.Groups[1].Value, m.Groups[2].Value);
            }
        }

        [TestMethod]
        public void DeployedStationTextFitsEveryShippedTranslation()
        {
            var translations = new List<(string, string)>(ReadTranslations("ResearchStationDeployed"));
            Assert.IsTrue(translations.Count > 1,
                $"expected several translations, found {translations.Count}");

            foreach ((string lang, string text) in translations)
            {
                float width = Fonts.Arial10.MeasureString(text).X;
                System.Console.WriteLine($"{lang}: '{text}' = {width}px of {SlotWidth}px");
                Assert.IsTrue(width <= SlotWidth,
                    $"{lang} '{text}' is {width}px and overflows the {SlotWidth}px cartouche slot");
            }
        }
    }
}
