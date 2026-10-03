using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.GameScreens.LoadGame;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Technologies;

/// <summary>
/// Every exotic resource refines at 0.5 to start, and each level of Refining Efficiency adds the 20% its text promises,
/// a tenth of a point of ratio, so the top level brings it to the cap of 1. A loaded game takes the bonus from the
/// levels researched, so levels researched when each gave 10% count at 20% too.
/// </summary>
[TestClass]
public class RefiningEfficiencyTests : StarDriveTest
{
    public RefiningEfficiencyTests()
    {
        CreateUniverseAndPlayerEmpire();
    }

    [TestMethod]
    public void EachLevelRaisesTheRefineRatioByATenthUpToOneAtTheTop()
    {
        Planet gasGiant = AddDummyPlanet(new Vector2(200_000), 0, 0, 0, new Vector2(205_000), explored: true);
        gasGiant.Mining = new Mineable(gasGiant);
        AssertEqual(0.0001f, 0.5f, gasGiant.Mining.RefineRatioFor(Player), "every exotic resource starts at 0.5");

        TechEntry tech = Player.GetTechEntry("RefiningEfficiency");
        Assert.AreEqual(5, tech.MaxLevel, "setup: Refining Efficiency has five levels");
        for (int level = 1; level <= tech.MaxLevel; ++level)
        {
            Player.UnlockTech(tech, TechUnlockType.Normal, null);
            Assert.AreEqual(level, tech.Level, "setup: each unlock researches one level");
            AssertEqual(0.0001f, 0.5f + 0.1f * level, gasGiant.Mining.RefineRatioFor(Player), $"refine ratio at level {level}");
        }
    }

    [TestMethod]
    public void ASaveMadeWhenEachLevelGaveTenPercentGetsTwentyPercentPerLevelOnLoad()
    {
        UState.StarDate = 1042.5f;
        TechEntry tech = Player.GetTechEntry("RefiningEfficiency");
        for (int level = 0; level < 3; ++level)
            Player.UnlockTech(tech, TechUnlockType.Normal, null);
        Player.data.RefiningRatioMultiplier = 1.3f;

        SavedGame save = Universe.Save("UnitTest.RefiningEfficiency", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Empire player = loaded.UState.GetEmpireById(Player.Id);
        AssertEqual(0.0001f, 1.6f, player.data.RefiningRatioMultiplier, "three researched levels give 20% each after a load");
    }
}
