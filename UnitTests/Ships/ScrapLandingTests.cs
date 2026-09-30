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
/// A ship sent to be scrapped lands once it is in range: of the nearest of the empire's shipyards at its
/// planet (the shipyard launch played backwards, so it starts as far out as a launch ends), or of the
/// planet itself when it has none (its radius + 300). It is scrapped once it has landed.
/// </summary>
[TestClass]
public class ScrapLandingTests : StarDriveTest
{
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

    float PlanetRange => Homeworld.Radius + 300f;
    float ShipyardRange => LandShip.ShipyardLandingRange(Scrapped);

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

        Assert.IsTrue(Scrapped.Position.InRadius(Homeworld.Position, PlanetRange), "the ship lands on the planet");
        AssertGreaterThan(Scrapped.Position.Distance(Homeworld.Position), PlanetRange - 50f,
                          "the landing starts as the ship comes within the planet's radius + 300");
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
    public void TheGoalStartsTheLandingOfAShipAlreadyInRange()
    {
        Ship shipyard = AddShipyard(new Vector2(0, 2500));
        OrderScrap(shipyard.Position + new Vector2(500, 0));
        Assert.IsFalse(Scrapped.IsLanding, "setup: the scrap order alone must not land the ship");

        Scrap.Evaluate();
        Assert.IsTrue(Scrapped.IsLanding, "the goal must start the landing of a ship it finds in range");

        Vector2 before = Scrapped.Position;
        RunObjectsSim(TestSimStep);
        AssertLessThan(Scrapped.Position.Distance(before), 0.1f, "a ship starting down from a standstill must ease into its glide");
    }

    [TestMethod]
    public void AShipInWarpDisabledOrDyingDoesNotStartItsLanding()
    {
        Ship shipyard = AddShipyard(new Vector2(0, 2500));
        void AssertDoesNotLand(Action<TestShip> setUp, string reason)
        {
            OrderScrap(shipyard.Position + new Vector2(500, 0));
            setUp(Scrapped);
            Scrap.Evaluate();
            Assert.IsFalse(Scrapped.IsLanding, reason);
        }

        AssertDoesNotLand(s => s.engineState = Ship.MoveState.Warp, "a ship still in warp must drop out of it before it starts down");
        AssertDoesNotLand(s => s.EMPDisabled = true, "an EMP-disabled ship must not escape the EMP by landing");
        AssertDoesNotLand(s => s.Dying = true, "a dying ship must not start a landing");
    }

    [TestMethod]
    public void TheGoalDoesNotRestartALandingTheShipStarted()
    {
        OrderScrap(Homeworld.Position + new Vector2(3000, 0));
        RunSimWhile((simTimeout: 60, fatal: true), () => !Scrapped.IsLanding);
        LandShip landing = Scrapped.LandShip;

        Scrap.Evaluate();
        Assert.AreSame(landing, Scrapped.LandShip, "the goal must carry on with the landing the ship started, not restart it");
    }

    [TestMethod]
    public void AScrappedShipWarpsInAndFliesStraightIntoItsShipyardLanding()
    {
        AssertFliesStraightIntoTheShipyard(new Vector2(8000, 2500));
    }

    [TestMethod]
    public void AScrappedShipAtSublightFliesStraightIntoItsShipyardLanding()
    {
        AssertFliesStraightIntoTheShipyard(new Vector2(5000, 2500));
    }

    void AssertFliesStraightIntoTheShipyard(Vector2 from)
    {
        Ship shipyard = AddShipyard(new Vector2(0, 2500));
        OrderScrap(Homeworld.Position + from);

        bool moving = false, stopped = false;
        float speedBefore = 0f;
        RunSimWhile((simTimeout: 120, fatal: true), () => !Scrapped.IsLanding, () =>
        {
            speedBefore = Scrapped.CurrentVelocity;
            if (speedBefore > 100f) moving = true;
            if (moving && speedBefore < 10f) stopped = true;
        });

        Assert.IsTrue(moving, "setup: the ship must fly to the shipyard");
        Assert.IsFalse(stopped, "the ship must fly into its landing, not stop at the shipyard first");

        float launchSpeed = LaunchShip.ShipyardSpeed(Scrapped);
        float launchDistance = launchSpeed * LaunchShip.ShipyardDuration(Scrapped, LaunchShip.ShipyardRotationDegX(Scrapped));
        float startDistance = Scrapped.Position.Distance(shipyard.Position);
        AssertGreaterThan(startDistance, launchDistance - 60f, "the landing must start where a shipyard launch would end");
        AssertLessThan(startDistance, launchDistance + 1f, "the landing must start where a shipyard launch would end");
        AssertEqual(5f, launchSpeed, speedBefore, "setup: the ship must come in at launch speed");

        Vector2 before = Scrapped.Position;
        float glideTime = RunObjectsSim(10 * TestSimStep.FixedTime);
        float glideSpeed = Scrapped.Position.Distance(before) / glideTime;
        AssertEqual(launchSpeed * 0.1f, speedBefore, glideSpeed, "the glide must carry on at the speed the ship came in with");
    }

    [TestMethod]
    public void AScrappedShipLandsOnTheNearestShipyardAndFollowsIt()
    {
        AddShipyard(new Vector2(-2500, 0));
        Ship shipyard = AddShipyard(new Vector2(0, 2500));
        OrderScrap(Homeworld.Position + new Vector2(6000, 2500));
        float closestToPlanet = float.MaxValue;
        RunUntilLanding(() => closestToPlanet = Math.Min(closestToPlanet, Scrapped.Position.Distance(Homeworld.Position)));
        Assert.IsTrue(Scrapped.Position.InRadius(shipyard.Position, ShipyardRange),
                      "the ship must land on the shipyard nearest to it, not on the planet");
        AssertGreaterThan(closestToPlanet, Homeworld.Radius, "the ship must fly straight to the shipyard, not over the planet first");

        MoveHomeworldAlongItsOrbit(3f);
        RunWithScrapGoal(() => !Scrapped.LandShip.Done);
        AssertLessThan(Scrapped.Position.Distance(shipyard.Position), 5f, "the ship must follow the shipyard as the planet moves and sink into it");
        RunUntilLanded();
    }

    [TestMethod]
    public void AScrappedShipFollowsAShipyardThatMovesWhileItFlies()
    {
        Ship shipyard = AddShipyard(new Vector2(0, 2500));
        OrderScrap(Homeworld.Position + new Vector2(8000, 2500));
        Vector2 orderedTo = shipyard.Position;
        MoveHomeworldAlongItsOrbit(30f);
        RunObjectsSim(TestSimStep);
        AssertGreaterThan(orderedTo.Distance(shipyard.Position), ShipyardRange + 1000f,
                          "setup: the shipyard must move out of range of where the ship was sent");

        RunUntilLanding();
        AssertGreaterThan(Scrapped.Position.Distance(shipyard.Position), ShipyardRange - 60f,
                          "the ship must fly to where the shipyard is now and start down at its landing range");
        AssertLessThan(Scrapped.Position.Distance(shipyard.Position), ShipyardRange + 1f,
                       "the ship must fly to where the shipyard is now and start down at its landing range");
        RunWithScrapGoal(() => !Scrapped.LandShip.Done);
        AssertLessThan(Scrapped.Position.Distance(shipyard.Position), 5f, "the ship must land on the shipyard");
    }

    [TestMethod]
    public void AShipWhoseShipyardIsGoneLandsOnThePlanet()
    {
        Ship shipyard = AddShipyard(new Vector2(0, 2500));
        OrderScrap(Homeworld.Position + new Vector2(6000, 2500));
        shipyard.QueueTotalRemoval();

        RunUntilLanding();
        Assert.IsTrue(Scrapped.Position.InRadius(Homeworld.Position, PlanetRange),
                      "with its shipyard gone, the ship must land on the planet");
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
