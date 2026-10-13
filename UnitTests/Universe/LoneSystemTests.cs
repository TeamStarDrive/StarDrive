using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Universe.SolarBodies;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Universe;

[TestClass]
public class LoneSystemTests : StarDriveTest
{
    public LoneSystemTests()
    {
        CreateUniverseAndPlayerEmpire();
    }

    SolarSystem AddSystemWithoutPlanets(Vector2 pos)
    {
        var system = new SolarSystem(UState, pos) { Sun = SunType.RandomHabitableSun(UState.Random) };
        UState.AddSolarSystem(system);
        return system;
    }

    void AssertAlwaysPicks(SolarSystem expected, bool includeResearchable, string why)
    {
        for (int i = 0; i < 100; ++i)
        {
            Assert.IsTrue(HelperFunctions.GetLoneSystem(UState, out SolarSystem picked, includeResearchable), "setup: a lone system exists");
            Assert.AreSame(expected, picked, why);
        }
    }

    [TestMethod]
    public void APortalOrPirateBaseNeverOpensInASystemWithPlanets()
    {
        AddDummyPlanetToEmpire(new Vector2(100_000), Player);
        AddDummyPlanet(new Vector2(200_000));
        SolarSystem lone = AddSystemWithoutPlanets(new Vector2(300_000));

        AssertAlwaysPicks(lone, includeResearchable: true, "a Remnant portal goes only to a system without planets");
        AssertAlwaysPicks(lone, includeResearchable: false, "a pirate base goes only to a system without planets");
    }

    [TestMethod]
    public void APirateBaseSkipsALoneSystemWithPiratesOrResearch()
    {
        AddSystemWithoutPlanets(new Vector2(100_000)).SetPiratePresence(true);
        AddSystemWithoutPlanets(new Vector2(200_000)).SetResearchable(true, UState);
        SolarSystem lone = AddSystemWithoutPlanets(new Vector2(300_000));

        AssertAlwaysPicks(lone, includeResearchable: false, "pirates skip a lone system that already has pirates or a research spot");
    }
}
