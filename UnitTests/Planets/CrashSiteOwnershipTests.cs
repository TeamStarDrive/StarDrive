using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
#pragma warning disable CA2213

namespace UnitTests.Planets;

/// <summary>
/// Issue #343: a crashed ship may only be recovered on a planet you own or one nobody owns.
/// Landing troops on a crash site mid-invasion used to pay the invader on the defender's world.
/// </summary>
[TestClass]
public class CrashSiteOwnershipTests : StarDriveTest
{
    readonly Planet P;

    public CrashSiteOwnershipTests()
    {
        CreateUniverseAndPlayerEmpire();
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        P = AddHomeWorldToEmpire(new Vector2(2000), Enemy);
    }

    // The recovery only runs for a tile one of OUR troops is standing on, so the landing is
    // part of the setup: without it the gate is never reached and every assertion is vacuous.
    PlanetGridSquare ArmedCrashSite(Planet planet, bool landOurTroop = true)
    {
        PlanetGridSquare tile = planet.TilesList.Find(t => t.CanCrashHere && !t.BuildingOnTile);
        Assert.IsNotNull(tile, "test needs a tile a ship can crash on");
        tile.CrashShip(Enemy, "Rocket Scout", "Wyvern", numTroopsSurvived: 0, planet, shipSize: 100);
        Assert.IsTrue(tile.IsCrashSiteActive, "test setup failed to arm the crash site");

        if (landOurTroop)
        {
            planet.AddTroop(ResourceManager.CreateTroop("Wyvern", Player), tile);
            Assert.IsTrue(tile.LockOnOurTroop(Player, out _), "test setup failed to land our troop");
        }
        return tile;
    }

    [TestMethod]
    public void ACrashSiteIsNotRecoveredOnAPlanetSomeoneElseStillOwns()
    {
        PlanetGridSquare tile = ArmedCrashSite(P);
        float moneyBefore = Player.Money;
        int shipsBefore = Player.OwnedShips.Count;

        tile.CheckAndTriggerEvent(P, Player);

        Assert.IsTrue(tile.IsCrashSiteActive, "an invader recovered a site on the defender's world");
        AssertEqual(0.01f, moneyBefore, Player.Money, "the invader was paid for it");
        AssertEqual(shipsBefore, Player.OwnedShips.Count, "the invader was given the wreck");
    }

    [TestMethod]
    public void ACrashSiteIsRecoveredOnceThePlanetIsOurs()
    {
        PlanetGridSquare tile = ArmedCrashSite(P);
        P.SetOwner(Player);

        tile.CheckAndTriggerEvent(P, Player);

        Assert.IsFalse(tile.IsCrashSiteActive, "the conquest resolved, so the site should be claimable");
    }

    [TestMethod]
    public void ACrashSiteOnAnUnownedPlanetIsStillRecovered()
    {
        Planet unowned = AddHomeWorldToEmpire(new Vector2(9000), Enemy);
        unowned.SetOwner(null);
        PlanetGridSquare tile = ArmedCrashSite(unowned);

        tile.CheckAndTriggerEvent(unowned, Player);

        Assert.IsFalse(tile.IsCrashSiteActive, "nobody owns it, so there is nothing to wait for");
    }

    // The troop that lands on the site targets whatever its tile holds, and an event tile scores
    // zero, so MoveTowardsTarget does nothing. Gating only the recovery would leave the troop
    // standing there for the whole invasion, waiting on the conquest it had stopped fighting.
    [TestMethod]
    public void ASiteWeCannotClaimIsNotATargetEither()
    {
        PlanetGridSquare tile = ArmedCrashSite(P, landOurTroop: false);

        Assert.IsFalse(tile.HostilesTargetsOnTile(Player, P.Owner, spaceCombat: false),
            "an unclaimable site pins the troop that steps on it");

        P.SetOwner(Player);
        Assert.IsTrue(tile.HostilesTargetsOnTile(Player, P.Owner, spaceCombat: false),
            "once the planet is ours the site is worth walking to again");
    }
}
