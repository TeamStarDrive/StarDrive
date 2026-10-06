using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;

namespace UnitTests.Data;

[TestClass]
public class DiplomacyDialogContentTests
{
    static string LanguageDir(Language language) => $"Content/DiplomacyDialogs/{language}";

    static string[] DialogTypes(string file)
        => XDocument.Load(file).Descendants("DialogLine").Select(l => (string)l.Element("DialogType")).ToArray();

    [TestMethod]
    public void EveryDialogLineSitsInsideDialogs()
    {
        string[] misplaced = Directory.GetFiles("Content/DiplomacyDialogs", "*.xml", SearchOption.AllDirectories)
            .SelectMany(file => XDocument.Load(file).Descendants("DialogLine")
                .Where(line => line.Parent?.Name != "Dialogs")
                .Select(line => $"{file}: {(string)line.Element("DialogType")}"))
            .ToArray();

        Assert.AreEqual(0, misplaced.Length,
            "dialog lines outside <Dialogs> are never loaded:\n" + string.Join("\n", misplaced));
    }

    [TestMethod]
    public void TranslatedDialogTypesMatchTheEnglishOnes()
    {
        string[] unknown = Enum.GetValues<Language>().Where(l => l != Language.English)
            .SelectMany(language => Directory.GetFiles(LanguageDir(language), "*.xml"))
            .Where(file => File.Exists(Path.Combine(LanguageDir(Language.English), Path.GetFileName(file))))
            .SelectMany(file =>
            {
                string[] english = DialogTypes(Path.Combine(LanguageDir(Language.English), Path.GetFileName(file)));
                return DialogTypes(file).Where(type => !english.Contains(type)).Select(type => $"{file}: {type}");
            })
            .ToArray();

        Assert.AreEqual(0, unknown.Length,
            "dialog types the English file does not have are never asked for:\n" + string.Join("\n", unknown));
    }

    [TestMethod]
    public void EveryLanguageHasTheStatementSetsTheDiplomacyScreenUses()
    {
        string[] missing = Enum.GetValues<Language>()
            .SelectMany(language =>
            {
                string file = Path.Combine(LanguageDir(language), "SharedDiplomacy.xml");
                string[] sets = XDocument.Load(file).Descendants("StatementSet").Select(s => (string)s.Element("Name")).ToArray();
                return new[] { "Ordinary Discussion", "EmpireDiscuss" }.Where(n => !sets.Contains(n)).Select(n => $"{file}: {n}");
            })
            .ToArray();

        Assert.AreEqual(0, missing.Length,
            "the diplomacy screen finds its option sets by these names:\n" + string.Join("\n", missing));
    }
}
