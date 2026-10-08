using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
#pragma warning disable CA2213

namespace UnitTests.Data;

[TestClass]
public class DiplomacyDialogFallbackTests : StarDriveTest
{
    const string EnglishOnlyName = "EnglishOnlyDialogFallbackTest";
    const string EnglishOnlyPath = "Content/DiplomacyDialogs/English/" + EnglishOnlyName + ".xml";
    const string EnglishKulrathi = "Content/DiplomacyDialogs/English/Kulrathi.xml";
    const string GermanKulrathi = "Content/DiplomacyDialogs/German/Kulrathi.xml";
    const string CaseTestName = "ZzDialogCaseTest";
    const string CaseTestEnglishPath = "Content/DiplomacyDialogs/English/" + CaseTestName + ".xml";
    const string CaseTestGermanPath = "Content/DiplomacyDialogs/German/zzdialogcasetest.xml";

    static string FriendlyGreeting(IEnumerable<DialogLine> lines)
        => lines.First(l => l.DialogType == "Greeting").Friendly;

    static string FriendlyGreetingInFile(string path)
        => XDocument.Load(path).Descendants("DialogLine")
                    .First(l => (string)l.Element("DialogType") == "Greeting")
                    .Element("Friendly")!.Value;

    static string[] TypesInFile(string path)
        => XDocument.Load(path).Descendants("DialogLine").Select(l => (string)l.Element("DialogType")).ToArray();

    static XElement LineInFile(string path, string type)
        => XDocument.Load(path).Descendants("DialogLine").First(l => (string)l.Element("DialogType") == type);

    [TestMethod]
    public void LinesMissingFromATranslatedDialogComeFromEnglish()
    {
        string[] english = TypesInFile(EnglishKulrathi);
        string[] german = TypesInFile(GermanKulrathi);
        string[] missing = english.Except(german).ToArray();
        Assert.AreNotEqual(0, missing.Length, "setup: the German Kulrathi file lacks some English dialog types");
        string withDefault = missing.First(t => LineInFile(EnglishKulrathi, t).Element("Default") != null);

        Language saved = GlobalStats.Language;
        try
        {
            GlobalStats.Language = Language.German;
            ResourceManager.LoadDialogs();
            DialogLine[] loaded = ResourceManager.GetDiplomacyDialog("Kulrathi").Dialogs.ToArray();

            foreach (string type in english.Union(german))
            {
                string[] source = german.Contains(type) ? german : english;
                Assert.AreEqual(source.Count(t => t == type), loaded.Count(l => l.DialogType == type),
                                $"{type}: a German line stays alone, a missing one comes once from English");
            }
            Assert.AreEqual(LineInFile(EnglishKulrathi, withDefault).Element("Default")!.Value,
                            loaded.First(l => l.DialogType == withDefault).Default,
                            "a line the German file lacks reads the English text");
            Assert.AreEqual(FriendlyGreetingInFile(GermanKulrathi), FriendlyGreeting(loaded),
                            "a line the German file has stays German");
        }
        finally
        {
            GlobalStats.Language = saved;
            ResourceManager.LoadDialogs();
        }
    }

    [TestMethod]
    public void ADialogMissingInTheActiveLanguageFallsBackToEnglish()
    {
        Language saved = GlobalStats.Language;
        try
        {
            File.Copy(EnglishKulrathi, EnglishOnlyPath, overwrite: true);
            GlobalStats.Language = Language.German;
            ResourceManager.LoadDialogs();

            Assert.AreEqual(FriendlyGreetingInFile(EnglishKulrathi),
                            FriendlyGreeting(ResourceManager.GetDiplomacyDialog(EnglishOnlyName).Dialogs),
                            "a race with no German dialog file uses its English one");

            string german = FriendlyGreetingInFile(GermanKulrathi);
            Assert.AreNotEqual(FriendlyGreetingInFile(EnglishKulrathi), german,
                               "setup: the German Kulrathi greeting differs from the English one");
            Assert.AreEqual(german, FriendlyGreeting(ResourceManager.GetDiplomacyDialog("Kulrathi").Dialogs),
                            "a race with a German dialog file still uses the German one");
        }
        finally
        {
            File.Delete(EnglishOnlyPath);
            GlobalStats.Language = saved;
            ResourceManager.LoadDialogs();
        }
    }

    [TestMethod]
    public void LinesMissingFromAModDialogComeFromTheVanillaFile()
    {
        const string newLine = "OfferResponse_Reject_NearRival";
        const string modGreeting = "A greeting only the mod has";
        string modDir = $"Mods/__DialogFallbackTest{Environment.ProcessId}";
        Language saved = GlobalStats.Language;
        try
        {
            foreach (string leftover in Directory.GetDirectories("Mods", "__DialogFallbackTest*"))
                if (Directory.GetCreationTimeUtc(leftover) < DateTime.UtcNow.AddMinutes(-10)) // from a run that was stopped
                    Directory.Delete(leftover, recursive: true);
            Directory.CreateDirectory(modDir + "/DiplomacyDialogs/English");
            File.Copy("Mods/ExampleMod/Globals.yaml", modDir + "/Globals.yaml");
            XDocument kulrathi = XDocument.Load(EnglishKulrathi);
            kulrathi.Descendants("DialogLine").Where(l => (string)l.Element("DialogType") == newLine).Remove();
            kulrathi.Descendants("DialogLine").First(l => (string)l.Element("DialogType") == "Greeting").Element("Friendly")!.Value = modGreeting;
            kulrathi.Save(modDir + "/DiplomacyDialogs/English/Kulrathi.xml");

            GlobalStats.Language = Language.English;
            GlobalStats.SetActiveModNoSave(new ModEntry(GamePlayGlobals.Deserialize(new FileInfo(modDir + "/Globals.yaml"))));
            ResourceManager.InitContentDir();
            ResourceManager.LoadDialogs();
            DialogLine[] loaded = ResourceManager.GetDiplomacyDialog("Kulrathi").Dialogs.ToArray();

            Assert.AreEqual(modGreeting, FriendlyGreeting(loaded), "a line the mod file has stays the mod's");
            Assert.AreEqual(LineInFile(EnglishKulrathi, newLine).Element("Default")!.Value,
                            loaded.Single(l => l.DialogType == newLine).Default,
                            "a line the mod file lacks comes once from the vanilla file of the same name");
        }
        finally
        {
            GlobalStats.Language = saved;
            GlobalStats.SetActiveModNoSave(null);
            ResourceManager.InitContentDir();
            ResourceManager.LoadDialogs();
            if (Directory.Exists(modDir))
                Directory.Delete(modDir, recursive: true);
        }
    }

    [TestMethod]
    public void ARaceFileCanNameItsDialogInAnyCase()
    {
        Assert.AreSame(ResourceManager.GetDiplomacyDialog("Kulrathi"), ResourceManager.GetDiplomacyDialog("kulrathi"),
                       "file names ignore case on Windows, so the dialog name in a race file must too");
    }

    [TestMethod]
    public void ATranslatedDialogNamedInAnotherCaseStillReplacesTheEnglishOne()
    {
        Language saved = GlobalStats.Language;
        try
        {
            File.Copy(EnglishKulrathi, CaseTestEnglishPath, overwrite: true);
            File.Copy(GermanKulrathi, CaseTestGermanPath, overwrite: true);
            GlobalStats.Language = Language.German;
            ResourceManager.LoadDialogs();

            Assert.AreEqual(FriendlyGreetingInFile(GermanKulrathi),
                            FriendlyGreeting(ResourceManager.GetDiplomacyDialog(CaseTestName).Dialogs),
                            "the German file is used although its name differs in case from the English one");
        }
        finally
        {
            File.Delete(CaseTestEnglishPath);
            File.Delete(CaseTestGermanPath);
            GlobalStats.Language = saved;
            ResourceManager.LoadDialogs();
        }
    }
}
