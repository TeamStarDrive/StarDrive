using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.ExpansionAI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.ExoticSystems;

/// <summary>
/// A computer empire sends a fleet to clear a system for research or mining only near home, where it has planets of
/// its own or within one and a half times its systems' average squared distance from its weighted center, or wherever
/// it knows of pirates or Remnants there. That holds when its planner wants a research station in the system, when its
/// research station there calls for help against an enemy's, when its planner wants to mine a planet there, and when
/// its mining station there is attacked.
/// </summary>
[TestClass]
public class ClearingFleetTests : StarDriveTest
{
    readonly Planet NewPlanet;
    readonly Planet EnemyHome;

    static readonly MethodInfo TryClearArea = typeof(ResearchStationPlanner).GetMethod("TryClearArea", BindingFlags.Instance | BindingFlags.NonPublic)!;
    static readonly MethodInfo ResearchCallForHelp = typeof(ProcessResearchStation).GetMethod("AiCallForHelpIfNeeded", BindingFlags.Instance | BindingFlags.NonPublic)!;
    static readonly MethodInfo ProcessMinables = typeof(MiningOpsPlanner).GetMethod("ProcessMinables", BindingFlags.Instance | BindingFlags.NonPublic)!;
    static readonly MethodInfo MiningCallForHelp = typeof(MiningOps).GetMethod("AiCallForHelpIfNeeded", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public ClearingFleetTests()
    {
        CreateUniverseAndPlayerEmpire();
        NewPlanet = AddDummyPlanet(new Vector2(10), 0, 0, 0);
        AddHomeWorldToEmpire(new Vector2(400000), Player);
        EnemyHome = AddHomeWorldToEmpire(new Vector2(-400000), Enemy);
        NewPlanet.GenerateNewFromPlanetType(new SeededRandom(), ResourceManager.Planets.RandomPlanet(PlanetCategory.GasGiant), scale: 1.5f, preDefinedPop: 16);
        if (!NewPlanet.IsResearchable)
            NewPlanet.SetResearchable(true, Universe.UState);
        NewPlanet.Mining = new Mineable(NewPlanet);
    }

    // the AI with a second colony and a navy, and a ship it knows of in the system, of an empire at war with it or of a faction
    Ship_Game.Empire HostileKnownInTheSystem(string faction)
    {
        AddDummyPlanetToEmpire(EnemyHome.Position + new Vector2(0f, -400_000f), Enemy);
        for (int i = 0; i < 6; ++i)
            SpawnShip("Vulcan Scout", Enemy, EnemyHome.Position + new Vector2(5000f, 1000f * i));
        Ship_Game.Empire hostile = Player;
        if (faction != null)
        {
            CreateAMinorFaction(faction);
            hostile = Faction;
            Assert.IsTrue(hostile.WeArePirates || hostile.WeAreRemnants, "setup: pirates or the Remnants");
        }
        Ship ship = SpawnShip("Vulcan Scout", hostile, NewPlanet.Position + new Vector2(8000f, 0f));
        UState.Objects.Update(TestSimStep);
        UState.Objects.Update(TestSimStep); // the systems list their ships and the empire its own
        Enemy.Threats.SetSeen(ship, fromBackgroundThread: false);
        Enemy.Threats.Update(new FixedSimTime(2f));
        Enemy.UpdateMilitaryStrengths();
        Assert.IsTrue(Enemy.IsAtWarWith(hostile), "setup: the AI is at war with the ship's empire");
        Assert.IsTrue(Enemy.KnownEnemyStrengthNoResearchStationsIn(NewPlanet.System) > 0f, "setup: the AI knows of the ship there");
        return hostile;
    }

    // the AI's center at the system, or at its home far from it
    void CenterTheAI(bool nearHome)
    {
        Enemy.WeightedCenter = nearHome ? NewPlanet.System.Position : EnemyHome.Position;
        Assert.AreEqual(nearHome, NewPlanet.System.Position.SqDist(Enemy.WeightedCenter) < Enemy.AverageSystemsSqdistFromCenter * 1.5f,
                        "setup: the system is near the AI's center, or far from it");
    }

    bool DefenseCalledFor(SolarSystem system) => Enemy.AI.HasGoal(g => g is DefendSystem d && d.TargetSystem == system);

    [TestMethod]
    [DataRow(false, null, false, DisplayName = "far from home, an empire at war: no fleet")]
    [DataRow(true, null, true, DisplayName = "near home: a fleet")]
    [DataRow(false, "Remnant", true, DisplayName = "far from home, the Remnants: a fleet")]
    [DataRow(false, "Corsairs", true, DisplayName = "far from home, pirates: a fleet")]
    public void AnAIClearsAResearchSystemOnlyNearHomeOrOfPiratesAndRemnants(bool nearHome, string faction, bool clears)
    {
        Ship_Game.Empire hostile = HostileKnownInTheSystem(faction);
        CenterTheAI(nearHome);
        SolarSystem system = NewPlanet.System;
        float known = Enemy.KnownEnemyStrengthIn(system);
        Assert.IsTrue(known <= Enemy.OffensiveStrength * 0.334f, "setup: the AI's navy can spare a fleet for it");

        TryClearArea.Invoke(Enemy.AI.ResearchStationsAI, new object[] { system, InfluenceStatus.Friendly, hostile, known });

        Assert.AreEqual(clears, DefenseCalledFor(system), clears ? "the AI sends a fleet to clear the system" : "the AI sends no fleet so far from home");
    }

    [TestMethod]
    [DataRow(false, null, false, DisplayName = "far from home: no fleet")]
    [DataRow(true, null, true, DisplayName = "near home: a fleet")]
    [DataRow(false, "Remnant", true, DisplayName = "far from home, the Remnants there too: a fleet")]
    [DataRow(false, "Corsairs", true, DisplayName = "far from home, pirates there too: a fleet")]
    public void AnAIResearchStationCallsAFleetAgainstAnEnemyStationOnlyNearHomeOrAgainstPiratesAndRemnants(bool nearHome, string faction, bool calls)
    {
        SolarSystem system = NewPlanet.System;
        Ship ours = SpawnShip("Basic Research Station", Enemy, NewPlanet.Position + new Vector2(3000f, 0f));
        Ship theirs = SpawnShip("Basic Research Station", Player, NewPlanet.Position + new Vector2(-3000f, 0f));
        HostileKnownInTheSystem(faction);
        CenterTheAI(nearHome);
        Assert.IsTrue(system.ShipList.Contains(theirs) && Enemy.IsAtWarWith(Player), "setup: a research station of an empire at war with the AI is in the system");
        var goal = new ProcessResearchStation(Enemy, NewPlanet) { TargetShip = ours };

        ResearchCallForHelp.Invoke(goal, null);

        Assert.AreEqual(calls, DefenseCalledFor(system), calls ? "the station calls a fleet to clear the system" : "the station calls no fleet so far from home");
    }

    [TestMethod]
    [DataRow(false, null, false, DisplayName = "far from home: no fleet")]
    [DataRow(true, null, true, DisplayName = "near home: a fleet")]
    [DataRow(false, "Remnant", true, DisplayName = "far from home, the Remnants: a fleet")]
    public void AnAIClearsAPlanetToMineOnlyNearHomeOrOfPiratesAndRemnants(bool nearHome, string faction, bool clears)
    {
        HostileKnownInTheSystem(faction);
        CenterTheAI(nearHome);

        ProcessMinables.Invoke(Enemy.AI.MiningOpsAI, new object[] { NewPlanet, InfluenceStatus.Neutral, new Goal[0] });

        Assert.AreEqual(clears, DefenseCalledFor(NewPlanet.System), clears ? "the AI sends a fleet to clear the planet's system" : "the AI sends no fleet so far from home");
    }

    [TestMethod]
    [DataRow(false, null, false, DisplayName = "far from home: no fleet")]
    [DataRow(true, null, true, DisplayName = "near home: a fleet")]
    [DataRow(false, "Remnant", true, DisplayName = "far from home, the Remnants: a fleet")]
    public void AnAIMiningStationUnderAttackCallsAFleetOnlyNearHomeOrAgainstPiratesAndRemnants(bool nearHome, string faction, bool calls)
    {
        Ship station = SpawnShip("Basic Mining Station", Enemy, NewPlanet.Position + new Vector2(NewPlanet.OrbitalRingRadius(0), 0f));
        HostileKnownInTheSystem(faction);
        CenterTheAI(nearHome);
        station.AI.BadGuysNear = true;
        var goal = new MiningOps(Enemy, NewPlanet, station);

        MiningCallForHelp.Invoke(goal, null);

        Assert.AreEqual(calls, DefenseCalledFor(NewPlanet.System), calls ? "the station calls a fleet to clear the system" : "the station calls no fleet so far from home");
    }
}
