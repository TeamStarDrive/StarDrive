using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Data.Yaml;

namespace UnitTests.Data;

[TestClass]
public class GameTextDataTests : StarDriveTest
{
    static readonly Regex Placeholder = new(@"\{(\d+)[^}]*\}");

    static IReadOnlyList<LangToken> LoadGameText()
    {
        var file = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "Content", "GameText.yaml"));
        Assert.IsTrue(file.Exists, $"GameText.yaml not found at {file.FullName}");
        return YamlParser.DeserializeArray<LangToken>(file);
    }

    static IEnumerable<(string Lang, string Text)> Translations(LangToken t)
    {
        if (!string.IsNullOrEmpty(t.RUS)) yield return ("RUS", t.RUS);
        if (!string.IsNullOrEmpty(t.SPA)) yield return ("SPA", t.SPA);
        if (!string.IsNullOrEmpty(t.UKR)) yield return ("UKR", t.UKR);
        if (!string.IsNullOrEmpty(t.GER)) yield return ("GER", t.GER);
        if (!string.IsNullOrEmpty(t.PTB)) yield return ("PTB", t.PTB);
        if (!string.IsNullOrEmpty(t.POL)) yield return ("POL", t.POL);
    }

    static string Placeholders(string text)
    {
        return string.Join(",", Placeholder.Matches(text ?? "").Select(m => m.Groups[1].Value).Distinct().OrderBy(s => s));
    }

    [TestMethod]
    public void EveryTextHasItsOwnKeyAndId()
    {
        IReadOnlyList<LangToken> tokens = LoadGameText();
        Assert.IsTrue(tokens.Count > 1000, "setup: the game's text is loaded");

        var problems = new List<string>();
        foreach (var key in tokens.GroupBy(t => t.NameId).Where(g => g.Count() > 1))
            problems.Add($"key {key.Key} appears {key.Count()} times");
        foreach (var id in tokens.Where(t => t.Id > 0).GroupBy(t => t.Id).Where(g => g.Count() > 1))
            problems.Add($"id {id.Key} is shared by {string.Join(", ", id.Select(t => t.NameId))}");

        Assert.AreEqual(0, problems.Count, string.Join("\n", problems));
    }

    [TestMethod]
    public void EveryTranslationKeepsTheEnglishPlaceholders()
    {
        var problems = new List<string>();
        foreach (LangToken t in LoadGameText())
        {
            string english = Placeholders(t.ENG);
            foreach ((string lang, string text) in Translations(t))
            {
                string translated = Placeholders(text);
                if (translated != english)
                    problems.Add($"{t.NameId} {lang}: {{{translated}}} vs ENG {{{english}}}");
            }
        }

        Assert.AreEqual(0, problems.Count, string.Join("\n", problems));
    }
}
