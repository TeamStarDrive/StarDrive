using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using System.IO;

namespace UnitTests.Serialization;

[TestClass]
public class SerializationRegressionTests : StarDriveTest
{
    [TestMethod]
    public void RaceSaveScreen_CanSaveAndLoad()
    {
        var save = new RaceSave()
        {
            Name = "Race Save",
            ModName = "MyMod",
            ModPath = "Mods/MyMod/",
            Traits = ResourceManager.MajorRaces[0].Traits.GetClone(),
        };
        string path = Path.GetTempFileName();
        SaveRaceScreen.Save(path, save);
        RaceSave load = SaveRaceScreen.Load(new(path));
        AssertMemberwiseEqual(save, load, "Expected saved RaceSave to equal loaded RaceSave");
    }

    [TestMethod]
    public void SaveNewGameSetupScreen_CanSaveAndLoad()
    {
        var save = new SetupSave(new())
        {
            Name = "New saved setup",
            ModName = "MyMod",
            ModPath = "Mods/MyMod/",
        };
        string path = Path.GetTempFileName();
        SaveNewGameSetupScreen.Save(path, save);
        SetupSave load = SaveNewGameSetupScreen.Load(new(path));
        AssertMemberwiseEqual(save, load, "Expected saved SetupSave to equal loaded SetupSave");
    }
}
