using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Universe;
using Ship_Game.Universe.SolarBodies;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets;

/// <summary>
/// Issue #327: a planet whose colony was wiped out is held for the empire that lost it, and an AI
/// does not call a system it never warned about stolen, except on Insane.
/// </summary>
[TestClass]
public class ColonyGraceTests : StarDriveTest
{
    Planet Lost;
    Planet Neighbour; // another planet in the same system

    void Setup(GameDifficulty difficulty = GameDifficulty.Normal)
    {
        CreateUniverseAndPlayerEmpire(settings: new UniverseParams { Difficulty = difficulty });
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        UState.StarDate = 1042.5f;
        Enemy.TestSetPersonality("Pacifist");
        AddHomeWorldToEmpire(new Vector2(20_000), Player);
        AddHomeWorldToEmpire(new Vector2(60_000), Enemy);

        var system = new SolarSystem(UState, new Vector2(100_000)) { Sun = SunType.RandomHabitableSun(UState.Random) };
        Lost = AddPlanet(system, new Vector2(105_000));
        Neighbour = AddPlanet(system, new Vector2(109_000));
        Lost.Name = "Grace Test Lost";
        UState.AddSolarSystem(system);
    }

    static Planet AddPlanet(SolarSystem system, Vector2 pos)
    {
        var p = new Planet(system.Universe.CreateId(), system, pos, fertility: 1f, minerals: 1f, maxPop: 4f);
        p.OrbitalAngle = p.Position.AngleToTarget(system.Position);
        p.TestSetOrbitalRadius(p.Position.Distance(system.Position) + p.Radius);
        system.RingList.Add(new SolarSystem.Ring { Asteroids = false, OrbitalDistance = p.OrbitalRadius, Planet = p });
        system.PlanetList.Add(p);
        return p;
    }

    void MakePeace() => Player.GetRelations(Enemy).AtWar = Enemy.GetRelations(Player).AtWar = false;

    void PassTurns(int turns) => UState.StarDate += turns * 0.1f;

    // the owner as its own attacker keeps war bookkeeping out of the way
    static void WipeOut(Planet p, Empire owner)
    {
        p.SetOwner(owner);
        p.WipeOutColony(owner);
    }

    [TestMethod]
    public void AWipedOutColonyIsHeldForTheEmpireThatLostIt()
    {
        Setup();
        MakePeace();
        WipeOut(Lost, Enemy);

        AssertEqual(50, Lost.ColonyGraceTurnsLeft(Player), "an empire at peace with the loser waits 50 turns");
        AssertEqual(0, Lost.ColonyGraceTurnsLeft(Enemy), "the empire that lost it may return at once");

        PassTurns(30);
        AssertEqual(20, Lost.ColonyGraceTurnsLeft(Player), "the hold counts down by turn");

        PassTurns(20);
        AssertEqual(0, Lost.ColonyGraceTurnsLeft(Player), "the hold ends after 50 turns");
    }

    [TestMethod]
    public void AnEmpireAtWarWithTheLoserIsNotHeldBack()
    {
        Setup();
        WipeOut(Lost, Enemy);
        AssertEqual(0, Lost.ColonyGraceTurnsLeft(Player), "at war, the loser's planet is fair game");

        MakePeace();
        AssertEqual(50, Lost.ColonyGraceTurnsLeft(Player), "peace brings the hold back for the time it has left");
    }

    [TestMethod]
    public void ARuthlessEmpireIsNotHeldBack()
    {
        Setup();
        MakePeace();
        WipeOut(Lost, Player);
        AssertEqual(50, Lost.ColonyGraceTurnsLeft(Enemy), "a Pacifist waits");

        Enemy.TestSetPersonality("Ruthless");
        AssertEqual(0, Lost.ColonyGraceTurnsLeft(Enemy), "a Ruthless empire does not");
    }

    [TestMethod]
    public void APlayerWhoseRaceIsRuthlessIsStillHeldBack()
    {
        Setup();
        Player.TestSetPersonality("Ruthless");
        MakePeace();
        WipeOut(Lost, Enemy);

        AssertEqual(50, Lost.ColonyGraceTurnsLeft(Player));
    }

    [TestMethod]
    public void ACapturedPlanetRetakenByTheLoserIsNotAReturn()
    {
        Setup();
        WipeOut(Lost, Enemy);
        Lost.SetOwner(Player); // at war, so the player may settle it
        Assert.IsNull(Lost.LostBy, "someone else settling the planet ends the record");

        PassTurns(10);
        Lost.SetOwner(Enemy);
        MakePeace();
        Assert.IsFalse(Lost.IsColonyGraceReturn(Enemy, Player), "a conquest is not a return to a lost colony");
    }

    [TestMethod]
    public void AnEmpireWithAPlanetInTheSystemWaitsHalfAsLong()
    {
        Setup();
        MakePeace();
        WipeOut(Lost, Enemy);
        Neighbour.SetOwner(Player);

        AssertEqual(25, Lost.ColonyGraceTurnsLeft(Player));
    }

    [TestMethod]
    public void TheHoldScalesWithTheProductionPace()
    {
        Setup();
        UState.P.Pace = 3f;
        MakePeace();
        WipeOut(Lost, Enemy);

        AssertEqual(100, Lost.ColonyGraceTurnsLeft(Player), "pace 3 is a production pace of 2");
    }

    [TestMethod]
    public void TheHoldSurvivesSaveAndLoad()
    {
        Setup();
        MakePeace();
        WipeOut(Lost, Enemy);
        PassTurns(10);

        SavedGame save = Universe.Save("UnitTest.ColonyGrace", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Planet lost = loaded.UState.Systems.SelectMany(s => s.PlanetList).First(p => p.Name == Lost.Name);

        AssertEqual(40, lost.ColonyGraceTurnsLeft(loaded.UState.Player));
    }

    [TestMethod]
    public void AColonizationOrderOnAHeldPlanetFails()
    {
        Setup();
        MakePeace();
        WipeOut(Lost, Enemy);
        SpawnShip("Colony Ship", Player, Lost.Position);

        Player.AI.AddGoalAndEvaluate(new MarkForColonization(Lost, Player, isManual: true));
        Assert.IsFalse(Player.AI.HasGoal(g => g.IsColonizationGoal(Lost)), "the order must not stand while the planet is held");

        PassTurns(50);
        Player.AI.AddGoalAndEvaluate(new MarkForColonization(Lost, Player, isManual: true));
        Assert.IsTrue(Player.AI.HasGoal(g => g.IsColonizationGoal(Lost)), "the order stands once the hold is over");
    }

    float EnemyAngerAfterPlayerReturnsIn(int turns)
    {
        Setup();
        MakePeace();
        Neighbour.SetOwner(Enemy);
        WipeOut(Lost, Player);
        Relationship enemyToPlayer = Enemy.GetRelations(Player);
        float angerBefore = enemyToPlayer.Anger_TerritorialConflict;

        PassTurns(turns);
        Lost.Colonize(SpawnShip("Colony Ship", Player, Lost.Position));
        return enemyToPlayer.Anger_TerritorialConflict - angerBefore;
    }

    [TestMethod]
    public void ReturningInTimeToALostColonyDoesNotSourRelations()
    {
        AssertEqual(0f, EnemyAngerAfterPlayerReturnsIn(20), "Enemy holds a planet there, so it waits 25 turns");
    }

    [TestMethod]
    public void ReturningLateToALostColonySoursRelations()
    {
        Assert.IsTrue(EnemyAngerAfterPlayerReturnsIn(30) > 0, "after Enemy's 25 turns the return is an ordinary settlement");
    }

    int StolenSystemsAfterCheckClaim()
    {
        Enemy.WeightedCenter = Lost.System.Position;
        Player.WeightedCenter = new Vector2(-1_000_000);
        Relationship enemyToPlayer = Enemy.GetRelations(Player);
        Enemy.AI.ExpansionAI.CheckClaim(Player, enemyToPlayer, Neighbour);
        return enemyToPlayer.StolenSystems.Count;
    }

    [TestMethod]
    public void SettlingANearbySystemWithoutAWarningIsNotTheft()
    {
        Setup(GameDifficulty.Brutal);
        MakePeace();
        Lost.SetOwner(Player);

        AssertEqual(0, StolenSystemsAfterCheckClaim());
    }

    [TestMethod]
    public void OnInsaneSettlingANearbySystemWithoutAWarningIsTheft()
    {
        Setup(GameDifficulty.Insane);
        MakePeace();
        Lost.SetOwner(Player);

        AssertEqual(1, StolenSystemsAfterCheckClaim());
    }

    [TestMethod]
    public void OnInsaneReturningInTimeToALostColonyIsNotTheft()
    {
        Setup(GameDifficulty.Insane);
        MakePeace();
        WipeOut(Lost, Player);
        PassTurns(10);
        Lost.SetOwner(Player);

        AssertEqual(0, StolenSystemsAfterCheckClaim());
    }
}
