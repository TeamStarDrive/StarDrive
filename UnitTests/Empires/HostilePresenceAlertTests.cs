using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Empires;

/// <summary>
/// The "forces spotted" alert fires once for a colony system while hostile warships stay in it, whatever happens to
/// the player's other colonies. After a load, the Alert Enemy Presence After Load option decides whether the
/// warships still there are announced again.
/// </summary>
[TestClass]
public class HostilePresenceAlertTests : StarDriveTest
{
    readonly Planet Lost;
    readonly Planet Invaded;
    readonly Ship Guard;
    readonly Ship Warship;

    public HostilePresenceAlertTests()
    {
        CreateUniverseAndPlayerEmpire();
        Lost = AddDummyPlanetToEmpire(new Vector2(200_000), Player);
        Invaded = AddDummyPlanetToEmpire(new Vector2(-200_000), Player);
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        Guard = SpawnShip("Fang Strafer", Player, Invaded.Position + new Vector2(-5000f, 0f));
        Warship = SpawnShip("Fang Strafer", Enemy, Invaded.Position + new Vector2(5000f, 0f));
        Invaded.System.ShipList.Add(Guard);
        Invaded.System.ShipList.Add(Warship);
        Warship.KnownByEmpires.SetSeen(Player);
    }

    static int Alerts(UniverseScreen universe) => universe.NotificationManager.NumberOfNotifications;

    static void PlayerTakesATurn(UniverseScreen universe)
        => universe.UState.Player.ThreatDetector.AssessHostilePresenceForPlayerWarnings(universe.UState.Player);

    [TestMethod]
    public void ASystemIsNotAlertedAgainWhenAnotherColonyIsLost()
    {
        PlayerTakesATurn(Universe);
        AssertEqual(1, Alerts(Universe), "setup: the player is alerted to the warship");

        Lost.SetOwner(null);
        AssertEqual(Invaded.System, Player.GetOwnedSystems()[0], "setup: the invaded system moves up the player's list");
        PlayerTakesATurn(Universe);

        AssertEqual(1, Alerts(Universe), "the warship is not announced again while it stays");
    }

    // saved with the warship already announced, then loaded
    UniverseScreen SavedAndLoaded()
    {
        PlayerTakesATurn(Universe);
        AssertEqual(1, Alerts(Universe), "setup: the player is alerted to the warship");
        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.HostilePresenceAlert", throwOnError: true);
        UniverseScreen loaded = LoadSave(save.SaveFile);
        Assert.IsNotNull(loaded, "setup: the save loads");
        loaded.NotificationManager = new NotificationManager(loaded.ScreenManager, loaded);

        // the first sim step after a load fills the systems' ship lists and the player's sight
        SolarSystem invaded = loaded.UState.GetPlanet(Invaded.Id).System;
        invaded.ShipList.Add(loaded.UState.Objects.FindShip(Guard.Id)); // the player's own ships share the list
        Ship warship = loaded.UState.Objects.FindShip(Warship.Id);
        invaded.ShipList.Add(warship);
        warship.KnownByEmpires.SetSeen(loaded.UState.Player);
        return loaded;
    }

    [TestMethod]
    public void WithTheOptionOnALoadRemindsThePlayerOfEnemiesStillInASystem()
    {
        UniverseScreen loaded = SavedAndLoaded();
        Assert.IsTrue(GlobalStats.NotifyEnemyInSystemAfterLoad, "setup: the option is on");

        PlayerTakesATurn(loaded);

        AssertEqual(1, Alerts(loaded), "the load reminds the player of the warship");
    }

    [TestMethod]
    public void WithTheOptionOffALoadKeepsQuietAboutEnemiesStillInASystem()
    {
        UniverseScreen loaded = SavedAndLoaded();
        bool option = GlobalStats.NotifyEnemyInSystemAfterLoad;
        GlobalStats.NotifyEnemyInSystemAfterLoad = false;
        try
        {
            PlayerTakesATurn(loaded);
            AssertEqual(0, Alerts(loaded), "the load says nothing of the warship");
        }
        finally
        {
            GlobalStats.NotifyEnemyInSystemAfterLoad = option;
        }
    }
}
