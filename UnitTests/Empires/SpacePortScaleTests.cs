using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using UnitTests.Serialization;

namespace UnitTests.Empires;

[TestClass]
public class SpacePortScaleTests : StarDriveTest
{
    static float GlobalScale => GlobalStats.Defaults.SpaceportScale;

    public SpacePortScaleTests()
    {
        CreateUniverseAndPlayerEmpire();
    }

    [TestMethod]
    public void ARaceFileWithoutAScaleKeepsItsModelSize()
    {
        foreach (IEmpireData race in ResourceManager.MajorRaces)
            AssertEqual(1f, ((EmpireData)race).SpacePortScale, $"{race.Name} has no SpacePortScale in its race file");
        AssertEqual(1f, Player.data.SpacePortScale, "an empire copies its race's scale");
    }

    [TestMethod]
    public void TheScaleIsKeptInASavedGame()
    {
        Player.data.SpacePortScale = 0.5f;
        AssertEqual(0.5f, BinarySerializerTests.SerDes(Player.data).SpacePortScale, "a scale from the race file");
        Player.data.SpacePortScale = 1f;
        AssertEqual(1f, BinarySerializerTests.SerDes(Player.data).SpacePortScale, "the default scale");
        Player.data.SpacePortScale = 0f;
        AssertEqual(0f, BinarySerializerTests.SerDes(Player.data).SpacePortScale, "a scale of 0, which a type-default skip would load as 1");
    }

    [TestMethod]
    public void TheRaceScaleResizesTheRaceSpacePortModel()
    {
        Player.data.SpacePortModel = "mod models/Chukk/Chukk_Station";
        Player.data.SpacePortScale = 0.5f;
        AssertEqual(0.0001f, GlobalScale * 0.5f, SpaceStation.StationScale(Player), "the race model is drawn at the global scale times the race scale");
    }

    [TestMethod]
    public void TheDefaultSpacePortKeepsTheGlobalScale()
    {
        Player.data.SpacePortModel = null;
        Player.data.SpacePortScale = 0.5f;
        AssertEqual(0.0001f, GlobalScale, SpaceStation.StationScale(Player), "a race without its own model draws the default station at the global scale");
        AssertEqual(0.0001f, GlobalScale, SpaceStation.StationScale(null), "an unowned port draws the default station at the global scale");
    }
}
