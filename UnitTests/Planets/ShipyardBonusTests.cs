using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets;

/// <summary>
/// A colony's shipyards cut the cost of what it builds. Only shipyards the colony's owner holds count, so the
/// count follows every change of owner, and the discount is applied once wherever a build cost is estimated.
/// </summary>
[TestClass]
public class ShipyardBonusTests : StarDriveTest
{
    readonly Planet Homeworld;
    readonly IShipDesign Scout;

    public ShipyardBonusTests()
    {
        LoadStarterShips("Shipyard", "Vulcan Scout", "Rocket Scout", "Dreadnought mk1-a");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Homeworld = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
        Player.UpdateRallyPoints();
        Scout = ResourceManager.Ships.GetDesign("Vulcan Scout");
    }

    Ship AddShipyard()
    {
        Ship shipyard = SpawnShip("Shipyard", Player, Homeworld.Position + new Vector2(0, 2500));
        shipyard.TetherToPlanet(Homeworld);
        AssertEqual(1, Homeworld.NumShipyards, "setup: the colony counts its shipyard");
        AssertEqual(0.001f, 0.75f, Homeworld.ShipCostModifier, "setup: one shipyard cuts costs by 25%");
        return shipyard;
    }

    void AssertNoShipyardBonus(string reason)
    {
        AssertEqual(0, Homeworld.NumShipyards, reason);
        AssertEqual(0.001f, 1f, Homeworld.ShipCostModifier, reason);
    }

    int TurnsToBuild(float cost) => Homeworld.TurnsUntilQueueComplete(cost, 1f, Scout);

    Goal OrderRefitToDreadnought(out float refitCost)
    {
        Ship refitting = SpawnShip("Vulcan Scout", Player, Homeworld.Position + new Vector2(8000, 0));
        IShipDesign refitTo = ResourceManager.Ships.GetDesign("Dreadnought mk1-a");
        refitCost = refitting.RefitCost(refitTo);
        AssertGreaterThan(refitCost, 500f, "setup: the refit must cost enough to show in the estimate");
        var refit = new RefitShip(refitting, refitTo, Player);
        Player.AI.AddGoalAndEvaluate(refit);
        AssertEqual(Homeworld, refit.PlanetBuildingAt, "setup: the refit must head for the colony");
        return refit;
    }

    [TestMethod]
    public void AShipyardTakenByAnotherEmpireStopsCountingForTheColony()
    {
        Ship shipyard = AddShipyard();
        shipyard.LoyaltyChangeFromBoarding(Enemy, addNotification: false);
        RunObjectsSim(TestSimStep);
        AssertEqual(Enemy, shipyard.Loyalty, "setup: the shipyard must change hands");
        AssertNoShipyardBonus("a shipyard boarded by another empire no longer counts for the colony");
    }

    [TestMethod]
    public void AColonyThatChangesHandsCountsOnlyItsNewOwnersShipyards()
    {
        Ship shipyard = AddShipyard();
        Homeworld.SetOwner(Enemy);
        AssertNoShipyardBonus("the new owner of a colony gets no discount from shipyards that are not theirs");

        shipyard.LoyaltyChangeByGift(Enemy, addNotification: false);
        RunObjectsSim(TestSimStep);
        AssertEqual(1, Homeworld.NumShipyards, "a shipyard that goes over with the colony counts for its new owner");
        AssertEqual(0.001f, 0.75f, Homeworld.ShipCostModifier, "a shipyard that goes over with the colony counts for its new owner");
    }

    [TestMethod]
    public void LoadingASaveRecountsTheShipyards()
    {
        UState.StarDate = 1042.5f;
        AddShipyard();
        Homeworld.NumShipyards = 3; // a count left stale by an older build

        SavedGame save = Universe.Save("UnitTest.ShipyardCount", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Planet loadedHomeworld = loaded.UState.GetPlanet(Homeworld.Id);
        AssertEqual(1, loadedHomeworld.NumShipyards, "a loaded colony counts the shipyards it really has");
        AssertEqual(0.001f, 0.75f, loadedHomeworld.ShipCostModifier, "a loaded colony counts the shipyards it really has");
    }

    [TestMethod]
    public void TheBuildTimeEstimateCountsTheShipyardDiscountOnce()
    {
        const float cost = 400f;
        Ship shipyard = AddShipyard();
        float modifier = Homeworld.ShipCostModifier;
        int withShipyard = TurnsToBuild(cost);

        shipyard.QueueTotalRemoval();
        AssertNoShipyardBonus("setup: the shipyard is gone");
        int atDiscountedCost = TurnsToBuild(cost * modifier);
        AssertLessThan(atDiscountedCost, 9999, "setup: the estimate must not hit its cap");
        Assert.AreNotEqual(atDiscountedCost, TurnsToBuild(cost * modifier * modifier),
                           "setup: the estimate must be fine enough to tell one discount from two");
        AssertEqual(atDiscountedCost, withShipyard, "with a shipyard, a build takes as long as its discounted cost, not twice discounted");
    }

    [TestMethod]
    public void ARefitHeadingForTheColonyIsEstimatedWithTheDiscountOnce()
    {
        const float cost = 40f;
        AddShipyard();
        float modifier = Homeworld.ShipCostModifier;
        Goal refit = OrderRefitToDreadnought(out float refitCost);
        int withRefit = TurnsToBuild(cost);

        Player.AI.RemoveGoal(refit);
        int asOneBuild = TurnsToBuild(cost + refitCost);
        AssertGreaterThan(Math.Abs(asOneBuild - TurnsToBuild(cost + refitCost * modifier)), 1,
                          "setup: the estimate must be fine enough to tell one discount from two");
        AssertEqual(1f, asOneBuild, withRefit, "a refit on its way counts at its discounted cost, like any build");
    }

    [TestMethod]
    public void ARefitAlreadyQueuedOrBuiltIsNotEstimatedAgain()
    {
        const float cost = 40f;
        int nothingQueued = TurnsToBuild(cost);
        Goal refit = OrderRefitToDreadnought(out float refitCost);
        int refitOnItsWay = TurnsToBuild(cost);
        AssertGreaterThan(refitOnItsWay, nothingQueued + 1, "setup: a refit on its way must count in the estimate");

        var queued = new QueueItem(Homeworld)
        {
            isShip = true,
            ShipData = refit.ToBuild,
            Cost = refitCost,
            Goal = refit,
            QType = QueueItemType.CombatShip,
        };
        Homeworld.Construction.EnqueueRefitShip(queued);
        AssertEqual(1f, refitOnItsWay, TurnsToBuild(cost), "a refit already in the queue counts once, not again through its goal");

        queued.ProductionSpent = queued.ActualCost;
        Homeworld.ProdHere = 10f;
        Player.AddMoney(1000f);
        Homeworld.Construction.RushProduction(0, 1f, rushButton: true);
        Assert.IsNotNull(refit.FinishedShip, "setup: the refit must be built");
        Assert.IsNotNull(refit.OldShip, "setup: the goal still knows its old ship, as an orbital refit does until it deploys");
        AssertEqual(1f, nothingQueued, TurnsToBuild(cost), "a refit already built no longer counts");
    }

    [TestMethod]
    public void RefittingAShipBeingBuiltAddsTheRefitCostOnce()
    {
        AddShipyard();
        Ship oldShip = SpawnShip("Vulcan Scout", Player, Homeworld.Position + new Vector2(8000, 0));
        IShipDesign refitTo = ResourceManager.Ships.GetDesign("Rocket Scout");
        var build = new QueueItem(Homeworld)
        {
            isShip = true,
            ShipData = oldShip.ShipData,
            Cost = 100f,
            ProductionSpent = 20f,
            QType = QueueItemType.CombatShip,
        };
        Homeworld.Construction.EnqueueRefitShip(build);

        float refitCost = oldShip.RefitCost(refitTo);
        AssertGreaterThan(refitCost, 0f, "setup: the refit must cost something");
        Homeworld.Construction.RefitShipsBeingBuilt(oldShip, refitTo);
        AssertEqual(0.01f, 100f + refitCost, build.Cost, "the refit cost is added undiscounted; the colony's discount applies once, to the whole");
    }

    [TestMethod]
    public void RefittingAQueuedRefitAddsTheRefitCostEvenBeforeWorkStarts()
    {
        Ship oldShip = SpawnShip("Vulcan Scout", Player, Homeworld.Position + new Vector2(8000, 0));
        IShipDesign refitTo = ResourceManager.Ships.GetDesign("Rocket Scout");
        QueueItem Queue(Goal goal, float cost)
        {
            var item = new QueueItem(Homeworld) { isShip = true, ShipData = oldShip.ShipData, Cost = cost, Goal = goal, QType = QueueItemType.CombatShip };
            Homeworld.Construction.EnqueueRefitShip(item);
            return item;
        }
        QueueItem refit = Queue(new RefitShip(Player), 5f);
        QueueItem build = Queue(null, 100f);

        float refitCost = oldShip.RefitCost(refitTo);
        float fullCost = refitTo.GetCost(Player);
        AssertGreaterThan(Math.Abs(fullCost - (5f + refitCost)), 1f, "setup: a refit and a full build must cost differently");
        Homeworld.Construction.RefitShipsBeingBuilt(oldShip, refitTo);
        AssertEqual(0.01f, 5f + refitCost, refit.Cost, "a queued refit keeps its refit cost and adds the new one, even before work starts");
        AssertEqual(0.01f, fullCost, build.Cost, "a build not yet started is repriced at the new design's full cost");
    }
}
