using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Fleets;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Fleets;

[TestClass]
public class ResetFleetTests : StarDriveTest
{
    public ResetFleetTests()
    {
        LoadStarterShips("Terran-Prototype");
        CreateUniverseAndPlayerEmpire();
        UState.StarDate = 1001f;
    }

    static bool IsListed(Empire empire, Fleet fleet)
    {
        foreach (Fleet f in empire.AllFleets)
            if (f == fleet)
                return true;
        return false;
    }

    static int CountResetFleets(Empire empire)
    {
        int count = 0;
        foreach (Fleet f in empire.AllFleets)
            if (f.Key == 0 && f.Ships.IsEmpty)
                ++count;
        return count;
    }

    Fleet CreateResetFleet(Empire owner)
    {
        Ship ship = SpawnShip("Terran-Prototype", owner, Vector2.Zero);
        Fleet fleet = owner.CreateFleet(owner.CreateFleetKey(), null);
        fleet.AddShip(ship);
        fleet.Reset(fleeIfOutmatched: false);
        AssertTrue(IsListed(owner, fleet), "setup: a reset fleet is still listed right after the reset");
        return fleet;
    }

    static readonly Vector2 MainBody = new(200_000f);

    // an AI fleet with somewhere to flee to, weighing player ships at face value
    Fleet AIFleet()
    {
        AddDummyPlanetToEmpire(new Vector2(-400_000f), Enemy);
        Enemy.FleetStrEmpireMultiplier[Player.Id] = 1f;
        return Enemy.CreateFleet(Enemy.CreateFleetKey(), null);
    }

    // the fleet's ships placed around a point, the first of them fighting
    Ship[] AddShips(Fleet fleet, Vector2 at, int count, SolarSystem system = null)
    {
        var ships = new Ship[count];
        for (int i = 0; i < count; ++i)
        {
            ships[i] = SpawnShip("Terran-Prototype", Enemy, at + new Vector2(0f, 1000f * i));
            ships[i].SetSystem(system);
            fleet.AddShip(ships[i]);
            ships[i].InCombat = i == 0;
        }
        return ships;
    }

    // player ships the AI has seen around a point
    void PlayerShipsSeen(Vector2 at, int count, SolarSystem system = null)
    {
        for (int i = 0; i < count; ++i)
        {
            Ship enemy = SpawnShip("Terran-Prototype", Player, at + new Vector2(8000f, 1000f * i));
            enemy.SetSystem(system);
            Enemy.Threats.SetSeen(enemy, fromBackgroundThread: false);
        }
        Enemy.Threats.Update(new FixedSimTime(2f));
    }

    [TestMethod]
    public void ADisbandedAIFleetFightingAWeakerForceDoesNotFlee()
    {
        Fleet fleet = AIFleet();
        Ship[] ships = AddShips(fleet, MainBody, 6);
        PlayerShipsSeen(MainBody, 1);

        fleet.Reset(fleeIfOutmatched: true);

        Assert.AreNotEqual(AIState.Flee, ships[0].AI.State, "a ship fighting a weaker force does not flee when its fleet breaks up");
        Assert.AreNotEqual(AIState.Flee, ships[1].AI.State, "nor does one that is not fighting");
    }

    [TestMethod]
    public void ADisbandedAIFleetOutmatchedWhereItIsFleesWithTheShipsInCombat()
    {
        Fleet fleet = AIFleet();
        Ship[] ships = AddShips(fleet, MainBody, 2);
        PlayerShipsSeen(MainBody, 12);

        fleet.Reset(fleeIfOutmatched: true);

        AssertEqual(AIState.Flee, ships[0].AI.State, "a ship fighting a force its fleet cannot beat flees when the fleet breaks up");
        Assert.AreNotEqual(AIState.Flee, ships[1].AI.State, "one that is not fighting does not flee");
    }

    [TestMethod]
    public void AShipFightingAStrongerForceFarFromItsFleetFleesAlone()
    {
        Fleet fleet = AIFleet();
        Ship[] mainBody = AddShips(fleet, MainBody, 6);
        Vector2 away = MainBody + new Vector2(300_000f, 0f);
        Ship[] detached = AddShips(fleet, away, 1);
        PlayerShipsSeen(MainBody, 1);
        PlayerShipsSeen(away, 12);

        fleet.Reset(fleeIfOutmatched: true);

        AssertEqual(AIState.Flee, detached[0].AI.State, "a ship fighting a stronger force on its own flees when its fleet breaks up");
        Assert.AreNotEqual(AIState.Flee, mainBody[0].AI.State, "while the main body, fighting a weaker force, stays");
    }

    // a system wider than the deep space search, and points at its two far sides
    SolarSystem WideSystem(out Vector2 westSide, out Vector2 eastSide)
    {
        SolarSystem system = AddDummyPlanet(MainBody).System;
        westSide = system.Position - new Vector2(system.Radius * 0.8f, 0f);
        eastSide = system.Position + new Vector2(system.Radius * 0.7f, 0f);
        AssertGreaterThan(westSide.Distance(eastSide), Fleet.AssaultEnemyScanRadius + 20_000f, "setup: the sides are further apart than the deep space search");
        return system;
    }

    [TestMethod]
    public void AShipInASystemWeighsTheHostilesAnywhereInIt()
    {
        SolarSystem system = WideSystem(out Vector2 westSide, out Vector2 eastSide);
        Fleet fleet = AIFleet();
        Ship[] fighting = AddShips(fleet, westSide, 1, system);
        PlayerShipsSeen(eastSide, 12, system);

        fleet.Reset(fleeIfOutmatched: true);

        AssertEqual(AIState.Flee, fighting[0].AI.State, "a ship fighting in a system the player holds in strength flees, however far the player ships are");
    }

    [TestMethod]
    public void TheFleetsShipsAnywhereInASystemTakeOnItsHostilesTogether()
    {
        SolarSystem system = WideSystem(out Vector2 westSide, out Vector2 eastSide);
        Fleet fleet = AIFleet();
        Ship[] fighting = AddShips(fleet, westSide, 1, system);
        AddShips(fleet, eastSide, 6, system);
        PlayerShipsSeen(westSide, 4, system);

        fleet.Reset(fleeIfOutmatched: true);

        Assert.AreNotEqual(AIState.Flee, fighting[0].AI.State, "the fleet's seven ships in the system can beat the four player ships there, so the one fighting stays");
    }

    [TestMethod]
    public void AnAIFleetThatIsResetLeavesItsEmpiresFleets()
    {
        Fleet fleet = CreateResetFleet(Enemy);
        Fleet waiting = Enemy.CreateFleet(Enemy.CreateFleetKey(), null);
        AssertTrue(IsListed(Enemy, fleet), "setup: a new fleet does not replace the reset fleet");

        Enemy.UpdateFleets(TestSimStep);

        AssertFalse(IsListed(Enemy, fleet), "the AI's fleet update removes a reset fleet");
        AssertTrue(IsListed(Enemy, waiting), "a fleet with a key that has no ships yet stays");
    }

    [TestMethod]
    public void APlayerFleetThatIsResetStaysUntilTheGameIsLoaded()
    {
        Fleet fleet = CreateResetFleet(Player);
        Fleet waiting = Player.CreateFleet(Player.CreateFleetKey(), null);
        waiting.DataNodes.Add(new FleetDataNode { ShipName = "Terran-Prototype" });
        int waitingKey = waiting.Key;

        Player.UpdateFleets(TestSimStep);
        AssertTrue(IsListed(Player, fleet), "the player's fleet update keeps a reset fleet, which the fleet design screen gives a key back");

        SavedGame save = Universe.Save("UnitTest.ResetFleets", throwOnError: true);
        UniverseScreen loaded = LoadSave(save.SaveFile);

        AssertEqual(0, CountResetFleets(loaded.UState.Player), "a load drops the player's reset fleets");
        AssertTrue(loaded.UState.Player.GetFleetOrNull(waitingKey) != null,
                   "a load keeps a fleet with a key whose ships are still to be built");
    }
}
