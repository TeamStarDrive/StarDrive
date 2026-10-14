using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
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
        UState.Events.Disabled = true;
        UState.StarDate = 1001f;
        // an empire without planets is defeated on its turn, which clears all its fleets
        AddHomeWorldToEmpire(new Vector2(-200_000f, 0f), Player);
        AddHomeWorldToEmpire(new Vector2(200_000f, 0f), Enemy);
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
        fleet.Reset(fleeIfInCombat: false);
        AssertTrue(IsListed(owner, fleet), "setup: a reset fleet is still listed right after the reset");
        return fleet;
    }

    [TestMethod]
    public void AnAIFleetThatIsResetLeavesItsEmpiresFleets()
    {
        Fleet fleet = CreateResetFleet(Enemy);
        Fleet waiting = Enemy.CreateFleet(Enemy.CreateFleetKey(), null);

        Enemy.Update(UState, TestSimStep);

        AssertFalse(Enemy.IsDefeated, "setup: the AI must not be defeated, which would clear its fleets anyway");
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

        Player.Update(UState, TestSimStep);
        AssertFalse(Player.IsDefeated, "setup: the player must not be defeated, which would clear its fleets");
        AssertTrue(IsListed(Player, fleet), "the player's fleet update keeps a reset fleet, which the fleet design screen gives a key back");

        SavedGame save = Universe.Save("UnitTest.ResetFleets", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);

        AssertEqual(0, CountResetFleets(loaded.UState.Player), "a load drops the player's reset fleets");
        AssertTrue(loaded.UState.Player.GetFleetOrNull(waitingKey) != null,
                   "a load keeps a fleet with a key whose ships are still to be built");
    }
}
