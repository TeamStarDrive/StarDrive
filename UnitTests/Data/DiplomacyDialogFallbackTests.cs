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

    static string FriendlyGreeting(IEnumerable<DialogLine> lines)
        => lines.First(l => l.DialogType == "Greeting").Friendly;

    static string FriendlyGreetingInFile(string path)
        => XDocument.Load(path).Descendants("DialogLine")
                    .First(l => (string)l.Element("DialogType") == "Greeting")
                    .Element("Friendly")!.Value;

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
}
