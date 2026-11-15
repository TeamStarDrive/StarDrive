using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets;

[TestClass]
public class CoreGovernorProductionTests : StarDriveTest
{
    readonly Planet Homeworld;
    int NumOutposts;

    public CoreGovernorProductionTests()
    {
        CreateUniverseAndPlayerEmpire();
        Homeworld = AddBuildingHomeworld(new Vector2(2000), Player);
    }

    Planet AddBuildingHomeworld(Vector2 pos, Empire owner)
    {
        Planet p = AddHomeWorldToEmpire(pos, owner);
        p.Population = p.MaxPopulation;
        p.UpdateDevelopmentLevel();
        owner.Research.SetTopic("FrigateConstruction");
        p.Construction.Enqueue(ResourceManager.GetBuildingTemplate("Nano Mine"));
        owner.UpdateMaxColonyValues();
        AssertTrue(p.ConstructionQueue.Count == 1 && !p.IsSabotaged && p.NonCybernetic, "setup: a homeworld building something");
        return p;
    }

    void AddOutposts(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Planet outpost = AddDummyPlanetToEmpire(new Vector2(100_000 + ++NumOutposts * 20_000, 100_000), Player);
            outpost.UpdateDevelopmentLevel();
            AssertEqual(1, outpost.Level, "setup: an outpost is level 1");
        }
        Player.UpdateMaxColonyValues();
    }

    static float ProductionShare(Planet p, Planet.ColonyType type)
    {
        p.CType = type;
        p.Food.Percent = 0;
        p.Prod.Percent = 0;
        p.Res.Percent  = 0;
        p.UpdateIncomes();
        switch (type) // the arguments DoGoverning passes
        {
            case Planet.ColonyType.Core:     p.AssignCoreWorldWorkers();                     break;
            case Planet.ColonyType.Research: p.AssignOtherWorldsWorkers(0.15f, 0.15f, 0, 0); break;
            case Planet.ColonyType.Military: p.AssignOtherWorldsWorkers(0.3f, 0.7f, 0, 1.5f); break;
        }
        AssertTrue(p.Res.Percent > 0, $"setup: research takes the rest of the workers ({type})");
        return p.Prod.Percent / (1 - p.Food.Percent);
    }

    static void SetPersonality(Empire e, string strategy, float opportunism)
    {
        typeof(EmpireResearch).GetProperty(nameof(EmpireResearch.Strategy))!.SetValue(e.Research, ResourceManager.GetEconomicStrategy(strategy));
        e.data.DiplomaticPersonality = new DTrait { Opportunism = opportunism };
    }

    [TestMethod]
    public void TheCoreBonusFadesAsTheEmpiresPlanetsDevelop()
    {
        Homeworld.Population = 1000;
        Homeworld.UpdateDevelopmentLevel();
        Player.UpdateMaxColonyValues();
        AssertEqual(2, Homeworld.Level, "setup: a homeworld of one billion is level 2");
        AssertEqual(0.0001f, 0.2f, Player.CoreProductionBonus, "an empire of fewer than 5 levels gets no more than the whole bonus");

        AddOutposts(3);
        AssertEqual(0.0001f, 0.2f, Player.CoreProductionBonus, "an empire of 5 levels gets the whole bonus");

        AddOutposts(18);
        AssertEqual(0.0001f, 0.12f, Player.CoreProductionBonus, "23 levels leave three fifths of it");

        AddOutposts(27);
        AssertEqual(0.0001f, 0f, Player.CoreProductionBonus, "50 levels leave none");

        AddOutposts(5);
        AssertEqual(0.0001f, 0f, Player.CoreProductionBonus, "more than 50 levels still leave none");
    }

    [TestMethod]
    public void ACoreWorldPutsTheCoreBonusOnProductionWhileItBuilds()
    {
        float research = ProductionShare(Homeworld, Planet.ColonyType.Research);
        float core     = ProductionShare(Homeworld, Planet.ColonyType.Core);
        AssertEqual(0.0001f, 0.2f, core - research, $"a young empire's core world puts 0.2 more of its workers on production than a research world (core {core}, research {research})");

        AddOutposts(50);
        research = ProductionShare(Homeworld, Planet.ColonyType.Research);
        core     = ProductionShare(Homeworld, Planet.ColonyType.Core);
        AssertEqual(0.0001f, 0f, core - research, $"a developed empire's core world splits like a research world (core {core}, research {research})");
    }

    [TestMethod]
    public void AMilitaryWorldTakesTheCoreBonusWhileItIsLarger()
    {
        float research = ProductionShare(Homeworld, Planet.ColonyType.Research);
        float military = ProductionShare(Homeworld, Planet.ColonyType.Military);
        AssertEqual(0.0001f, 0.2f, military - research, $"a young empire's military world takes the core bonus (military {military}, research {research})");

        AddOutposts(50);
        research = ProductionShare(Homeworld, Planet.ColonyType.Research);
        military = ProductionShare(Homeworld, Planet.ColonyType.Military);
        AssertEqual(0.0001f, 0.1f, military - research, $"a developed empire's military world keeps its own bonus (military {military}, research {research})");
    }

    [TestMethod]
    public void ThePlayersCoreWorldSplitsTheSameWhateverPersonalityTheEmpireDrew()
    {
        SetPersonality(Player, "Generalists", 0.2f);
        float generalists = ProductionShare(Homeworld, Planet.ColonyType.Core);
        SetPersonality(Player, "Technologists", 0.75f);
        float technologists = ProductionShare(Homeworld, Planet.ColonyType.Core);
        AssertEqual(0.0001f, generalists, technologists, "the player's governor ignores the empire's hidden personality");
    }

    [TestMethod]
    public void AnAICoreWorldStillSplitsByItsPersonality()
    {
        Planet aiHome = AddBuildingHomeworld(new Vector2(600_000), Enemy);
        SetPersonality(Enemy, "Generalists", 0.2f);
        float generalists = ProductionShare(aiHome, Planet.ColonyType.Core);
        SetPersonality(Enemy, "Technologists", 0.75f);
        float technologists = ProductionShare(aiHome, Planet.ColonyType.Core);
        AssertTrue(Math.Abs(generalists - technologists) > 0.01f, $"an AI governor follows its personality (generalists {generalists}, technologists {technologists})");
    }
}
