using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// A ship sent to be scrapped lands once it is within 200 of its target: the nearest of the empire's
/// shipyards at its planet, or the planet itself when it has none. It is scrapped once it has landed.
/// </summary>
[TestClass]
public class ScrapLandingTests : StarDriveTest
{
    const float LandingRange = 200f;
    readonly Planet Homeworld;
    TestShip Scrapped;
    Goal Scrap;

    public ScrapLandingTests()
    {
        LoadStarterShips("Shipyard");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Homeworld = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
        Player.UpdateRallyPoints();
    }

    Ship AddShipyard(Vector2 offset)
    {
        Ship shipyard = SpawnShip("Shipyard", Player, Homeworld.Position + offset);
        shipyard.TetherToPlanet(Homeworld);
        return shipyard;
    }

    void OrderScrap(Vector2 from) => OrderScrap(SpawnShip("Vulcan Scout", Player, from));

    void OrderScrap(TestShip ship)
    {
        Scrapped = ship;
        Scrapped.AI.OrderScrapShip();
        Scrap = Player.AI.FindGoal(g => g.Type == GoalType.ScrapShip && g.OldShip == Scrapped);
        Assert.IsNotNull(Scrap, "setup: the scrap order must make a scrap goal");
        AssertEqual(Homeworld, Scrap.PlanetBuildingAt, "setup: the ship must be sent to the homeworld");
    }

    // the goal is evaluated every second, standing in for the empire turn
    void RunWithScrapGoal(Func<bool> condition, Action body = null)
    {
        int frame = 0;
        RunSimWhile((simTimeout: 120, fatal: true), condition, () =>
        {
            if (++frame % 60 == 0)
                Scrap.Evaluate();
            body?.Invoke();
        });
    }

    void RunUntilLanding(Action body = null) => RunWithScrapGoal(() => !Scrapped.IsLanding, body);

    void RunUntilLanded()
    {
        RunWithScrapGoal(() => Scrapped.Active);
        Assert.IsFalse(Scrapped.Dying, "a landed ship is removed, not destroyed");
    }

    void MoveHomeworldAlongItsOrbit(float degrees)
    {
        Homeworld.OrbitalAngle += degrees;
        Homeworld.Position = Homeworld.System.Position.PointFromAngle(Homeworld.OrbitalAngle, Homeworld.OrbitalRadius);
    }

    [TestMethod]
    public void AScrappedShipLandsOnThePlanetAndIsScrappedOnceItHasLanded()
    {
        Homeworld.ProdHere = 0;
        OrderScrap(Homeworld.Position + new Vector2(5000, 0));
        RunUntilLanding();

        Assert.IsTrue(Scrapped.Position.InRadius(Homeworld.Position, LandingRange), "the ship lands on the planet");
        float scrapCost = Scrapped.GetScrapCost();
        float money = Player.Money;

        float paidWhileLanding = 0f;
        RunWithScrapGoal(() => Scrapped.Active, () =>
        {
            if (Scrapped.LandShip is { Done: false })
                paidWhileLanding = Math.Max(paidWhileLanding, Homeworld.ProdHere);
        });
        AssertEqual(0.01f, 0f, paidWhileLanding, "nothing is paid while the ship is on its way down");
        AssertEqual(0.01f, scrapCost, Homeworld.ProdHere, "half the ship's cost goes to the planet once it has landed");
        AssertGreaterThan(Player.Money, money, "credits come back once it has landed");
    }

    [TestMethod]
    public void APlanetLostDuringTheLandingGetsNoProduction()
    {
        Homeworld.ProdHere = 0;
        OrderScrap(Homeworld.Position + new Vector2(3000, 0));
        RunUntilLanding();

        float money = Player.Money;
        Homeworld.SetOwner(Enemy);
        float prodAfterCapture = Homeworld.ProdHere;
        RunUntilLanded();
        AssertEqual(0.01f, prodAfterCapture, Homeworld.ProdHere, "a planet lost during the landing gets none of the production");
        AssertGreaterThan(Player.Money, money, "the credits still come back");
    }

    [TestMethod]
    public void ALandedShipIsRemovedEvenIfItsScrapGoalIsGone()
    {
        Homeworld.ProdHere = 0;
        OrderScrap(Homeworld.Position + new Vector2(3000, 0));
        RunUntilLanding();

        Player.AI.RemoveGoal(Scrap);
        RunSimWhile((simTimeout: 60, fatal: true), () => Scrapped.Active);
        AssertEqual(0.01f, 0f, Homeworld.ProdHere, "with no scrap goal left, nothing pays for the ship");
    }

    [TestMethod]
    public void AShipStillLaunchingWaitsForTheLaunchBeforeItLands()
    {
        TestShip launching = SpawnShip("Vulcan Scout", Player, Homeworld.Position);
        launching.InitLaunch(LaunchPlan.Planet, 0f);
        Assert.IsTrue(launching.IsLaunching, "setup: the ship must be launching");

        OrderScrap(launching);
        RunUntilLanding();
        Assert.IsFalse(Scrapped.IsLaunching, "the landing must not start until the launch is over");
    }

    [TestMethod]
    public void AScrappedShipLandsOnTheNearestShipyardAndFollowsIt()
    {
        AddShipyard(new Vector2(-2500, 0));
        Ship shipyard = AddShipyard(new Vector2(0, 2500));
        OrderScrap(Homeworld.Position + new Vector2(6000, 2500));
        float closestToPlanet = float.MaxValue;
        RunUntilLanding(() => closestToPlanet = Math.Min(closestToPlanet, Scrapped.Position.Distance(Homeworld.Position)));
        Assert.IsTrue(Scrapped.Position.InRadius(shipyard.Position, LandingRange),
                      "the ship must land on the shipyard nearest to it, not on the planet");
        AssertGreaterThan(closestToPlanet, Homeworld.Radius, "the ship must fly straight to the shipyard, not over the planet first");

        MoveHomeworldAlongItsOrbit(3f);
        RunWithScrapGoal(() => !Scrapped.LandShip.Done);
        AssertLessThan(Scrapped.Position.Distance(shipyard.Position), 5f, "the ship must follow the shipyard as the planet moves and sink into it");
        RunUntilLanded();
    }

    [TestMethod]
    public void AScrappedShipGoesOnToAShipyardThatMovedWhileItFlew()
    {
        Ship shipyard = AddShipyard(new Vector2(0, 2500));
        OrderScrap(Homeworld.Position + new Vector2(6000, 2500));
        Vector2 orderedTo = shipyard.Position;
        MoveHomeworldAlongItsOrbit(10f);
        AssertGreaterThan(orderedTo.Distance(Homeworld.Position + shipyard.TetherOffset), LandingRange,
                          "setup: the shipyard must move out of range of where the ship was sent");

        RunUntilLanding();
        Assert.IsTrue(Scrapped.Position.InRadius(shipyard.Position, LandingRange),
                      "the ship must go on to where the shipyard is now");
    }

    [TestMethod]
    public void AShipWhoseShipyardIsGoneLandsOnThePlanet()
    {
        Ship shipyard = AddShipyard(new Vector2(0, 2500));
        OrderScrap(Homeworld.Position + new Vector2(6000, 2500));
        shipyard.QueueTotalRemoval();

        RunUntilLanding();
        Assert.IsTrue(Scrapped.Position.InRadius(Homeworld.Position, LandingRange),
                      "with its shipyard gone, the ship must land on the planet");
    }

    [TestMethod]
    public void AScrappedShipDoesNotTurnInPlaceAtTheShipyard()
    {
        Ship shipyard = AddShipyard(new Vector2(0, 2500));
        OrderScrap(Homeworld.Position + new Vector2(6000, 2500));

        Vector2 slowFacing = Vector2.Zero;
        float turnWhileSlow = 0f;
        RunUntilLanding(() =>
        {
            bool slowAtShipyard = Scrapped.CurrentVelocity < 20f
                               && Scrapped.Position.InRadius(shipyard.Position, 300f);
            if (slowAtShipyard && slowFacing == Vector2.Zero)
                slowFacing = Scrapped.Direction;
            if (slowFacing != Vector2.Zero)
                turnWhileSlow = Math.Max(turnWhileSlow, Scrapped.Direction.Distance(slowFacing));
        });

        Assert.AreNotEqual(Vector2.Zero, slowFacing, "setup: the ship must slow down at the shipyard");
        AssertLessThan(turnWhileSlow, 0.05f, "the ship must land facing the way it flew in, not turn in place first");
    }

    [TestMethod]
    public void AShipLandingForScrapIsStillScrapping()
    {
        OrderScrap(Homeworld.Position + new Vector2(3000, 0));
        RunUntilLanding();

        AssertEqual(AIState.Scrap, Scrapped.AI.State, "a ship landing for scrap stays in the scrap state");
        Assert.IsFalse(Player.AllFleetReadyShips().Contains(Scrapped), "a ship landing for scrap is not free for a fleet");
        Assert.IsFalse(Scrapped.CanBeScrapped, "a landing ship cannot be scrapped or refitted again");
        Assert.IsFalse(Enemy.IsEmpireAttackable(Player, Scrapped), "enemies must not target a landing ship");
    }

    [TestMethod]
    public void AShipyardLandingCarriesOnAfterALoad()
    {
        UState.StarDate = 1042.5f;
        Homeworld.ProdHere = 0;
        Ship shipyard = AddShipyard(new Vector2(0, 2500));
        OrderScrap(Homeworld.Position + new Vector2(3000, 2500));
        RunUntilLanding();
        float scrapCost = Scrapped.GetScrapCost();

        SavedGame save = Universe.Save("UnitTest.ScrapLanding", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Ship landing = loaded.UState.Objects.FindShip(Scrapped.Id);
        Ship loadedShipyard = loaded.UState.Objects.FindShip(shipyard.Id);
        Assert.IsTrue(landing is { Active: true, IsLanding: true, AI.State: AIState.Scrap },
                      "a ship landing for scrap must still be landing after a load, not back in play");
        Goal loadedScrap = loaded.UState.Player.AI.FindGoal(g => g.Type == GoalType.ScrapShip && g.OldShip == landing);
        Assert.IsNotNull(loadedScrap, "the scrap goal must survive the load to pay for the ship");

        int frame = 0;
        bool touchedDown = false;
        for (double time = 0; landing.Active; time += TestSimStepD)
        {
            AssertLessThan(time, 60.0, "the loaded landing must finish");
            if (!touchedDown && landing.LandShip.Done)
            {
                touchedDown = true;
                AssertLessThan(landing.Position.Distance(loadedShipyard.Position), 5f, "the loaded landing must still end on the shipyard");
            }
            if (++frame % 60 == 0)
                loadedScrap.Evaluate();
            loaded.UState.Objects.Update(TestSimStep);
        }
        Assert.IsTrue(touchedDown, "the loaded ship must touch down before it is removed");
        AssertEqual(0.01f, scrapCost, loadedScrap.PlanetBuildingAt.ProdHere, "the loaded landing must still pay the planet");
    }
}
