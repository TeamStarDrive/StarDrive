using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// A hangar ship that cannot warp, whose carrier is far away, is let go and the carrier's bay is freed: an assault
/// shuttle lands its troops on a safe colony of ours in its system with room, another ship lands there itself and is
/// removed with no refund, and either is scuttled when its system has no such colony; any other ship carrying troops is
/// scuttled. One that can warp, or whose carrier is only out of sight, flies back. A ship that cannot warp is scrapped
/// at a safe port close by, or else at a safe colony of ours in its system.
/// </summary>
[TestClass]
public class LeftBehindHangarShipTests : StarDriveTest
{
    const string CarrierDesign = "TEST_Excalibur-Class Supercarrier";
    const string Fighter = "Fang Strafer";
    const string Shuttle = "Assault Shuttle";
    const string WarpScout = "Vulcan Scout";

    readonly Planet Homeworld;
    readonly Planet NobodysPlanet;
    readonly Planet OurOutpost;
    readonly Ship Carrier;

    public LeftBehindHangarShipTests()
    {
        LoadStarterShips(CarrierDesign, Fighter, Shuttle, WarpScout);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Homeworld = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
        NobodysPlanet = AddDummyPlanet(new Vector2(-600_000, 200_000));
        OurOutpost = AddDummyPlanetToEmpire(new Vector2(-600_000, -600_000), Player);
        Planet farPlanet = AddDummyPlanet(new Vector2(900_000, -900_000));
        Player.UpdateRallyPoints();
        Carrier = SpawnShip(CarrierDesign, Player, farPlanet.Position + new Vector2(farPlanet.Radius + 2000, 0));
        Carrier.AI.OrderHoldPosition(MoveOrder.HoldPosition);
    }

    // the starter designs all warp, so a design is copied without it
    Ship NoWarp(string designName, Vector2 at)
    {
        Assert.IsTrue(ResourceManager.Ships.GetDesign(designName, out IShipDesign from), $"setup: {designName} is loaded");
        ShipDesign design = ((ShipDesign)from).GetClone($"{designName} STL");
        design.SetDesignSlots(from.GetOrLoadDesignSlots(), updateRole: false);
        typeof(ShipDesign).GetProperty(nameof(ShipDesign.BaseCanWarp))!.SetValue(design, false);
        Ship ship = SpawnShip(Ship.CreateNewShipTemplate(Empire.Void, design), Player, at);
        ship.AI.OrderHoldPosition(MoveOrder.HoldPosition);
        Assert.IsFalse(ship.BaseCanWarp, $"setup: the {designName} copy cannot warp");
        return ship;
    }

    Ship NoWarpFighter(Vector2 at) => NoWarp(Fighter, at);

    // the player's default assault shuttle, which lands its troops like a troop ship
    Ship NoWarpAssaultShuttleWithATroop(Vector2 at)
    {
        Ship shuttle = NoWarp(Shuttle, at);
        Player.data.DefaultAssaultShuttle = shuttle.Name;
        ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(shuttle);
        Assert.IsTrue(shuttle.IsDefaultAssaultShuttle && shuttle.TroopCount == 1, "setup: the player's assault shuttle with a troop");
        return shuttle;
    }

    Ship Launched(Ship ship)
    {
        ShipModule bay = Carrier.Carrier.AllHangars.First(h => !h.TryGetHangarShip(out _));
        bay.ResetHangarShipWithReturnToHangar(ship);
        return ship;
    }

    void RecallCheck(Ship ship)
    {
        RunObjectsSim(TestSimStep);
        RunObjectsSim(TestSimStep);
        typeof(Ship).GetMethod("RecallHangarShipIfTooFarFromCarrier", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(ship, null);
        Universe.InvokePendingSimThreadActions();
    }

    static Vector2 Beside(Planet p) => p.Position + new Vector2(p.Radius + 1500, 0);

    bool CarrierHolds(Ship ship) => Carrier.Carrier.AllHangars.Any(h => h.TryGetHangarShip(out Ship s) && s == ship);

    ScrapShip ScrapGoal(Ship ship) => (ScrapShip)Player.AI.FindGoal(g => g.Type == GoalType.ScrapShip && g.OldShip == ship);

    static void AssertScuttled(Ship ship)
        => Assert.IsTrue(ship.ScuttleTimer > 0 && !ship.Active, $"it is scuttled (state {ship.AI.State}, timer {ship.ScuttleTimer}, active {ship.Active})");

    [TestMethod]
    public void AFighterLeftFarBehindLandsOnOurColonyInItsSystem()
    {
        Ship fighter = Launched(NoWarpFighter(Beside(OurOutpost)));
        RecallCheck(fighter);
        Assert.IsFalse(OurOutpost.HasSpacePort, "setup: a colony with no space port, far from our port");

        Assert.IsNull(fighter.Mothership, "the fighter is let go");
        Assert.IsFalse(CarrierHolds(fighter), "and the carrier's bay is freed");
        Assert.AreSame(OurOutpost, ScrapGoal(fighter)?.PlanetBuildingAt, "the fighter goes to our colony in its system");
        Assert.IsTrue(ScrapGoal(fighter).NoRefund, "to land there with no refund");
        Assert.IsTrue(fighter.ScuttleTimer <= 0, "and is not scuttled");
    }

    [TestMethod]
    public void AReleasedFighterGivesBackNothingThoughAScrappedOneDoes()
    {
        Ship released = Launched(NoWarpFighter(Beside(Homeworld)));
        RecallCheck(released);
        Ship ordinary = NoWarpFighter(Beside(Homeworld) + new Vector2(0, 1000));
        ordinary.AI.OrderScrapShip();
        MethodInfo scrapTheShip = typeof(ScrapShip).GetMethod("ScrapTheShip", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Assert.IsTrue(ScrapGoal(released)?.PlanetBuildingAt == Homeworld && ScrapGoal(ordinary)?.PlanetBuildingAt == Homeworld, "setup: both are scrapped at the homeworld");

        Homeworld.ProdHere = 0;
        float money = Player.Money;
        scrapTheShip.Invoke(ScrapGoal(released), null);
        Assert.IsFalse(released.Active, "setup: the released fighter is taken apart");
        AssertEqual(0f, Homeworld.ProdHere, "the released fighter gives back no production");
        AssertEqual(money, Player.Money, "and no credits");

        scrapTheShip.Invoke(ScrapGoal(ordinary), null);
        Assert.IsTrue(Homeworld.ProdHere > 0, "setup: an ordinary fighter scrapped there gives some production back");
        Assert.IsTrue(Player.Money > money, "setup: and some credits");
    }

    [TestMethod]
    public void AReleasedFighterOnItsWayCountsNoProductionComingToTheColony()
    {
        Ship released = Launched(NoWarpFighter(Beside(Homeworld)));
        RecallCheck(released);
        Assert.AreSame(Homeworld, ScrapGoal(released)?.PlanetBuildingAt, "setup: the released fighter is on its way to the homeworld");
        AssertGreaterThan(released.GetScrapCost(), 1f, "setup: an ordinary scrap of the fighter would count more than the tolerance");

        Goal[] scrapGoals = Player.AI.FindGoals(g => g.Type is GoalType.ScrapShip or GoalType.DeliverRefund);
        AssertEqual(1f, Homeworld.Storage.Max - Homeworld.ProdHere - Homeworld.IncomingProd, Homeworld.MissingProdHereForScrap(scrapGoals),
                    "a scrap that gives back nothing leaves the colony's room for production as it is");
    }

    // a fighter that cannot warp beside an outpost of ours, with our port close by in another system
    Ship NoWarpFighterByAnOutpostNearOurPort(out Planet outpost)
    {
        outpost = AddDummyPlanetToEmpire(Homeworld.Position + new Vector2(30_000, -5_000), Player); // the planet lies 5000 off on each axis
        Player.UpdateRallyPoints();
        Ship ship = NoWarpFighter(Beside(outpost));
        RunObjectsSim(TestSimStep);
        RunObjectsSim(TestSimStep);
        ship.SetSystem(outpost.System); // the two systems overlap, and each turn either may claim the ship
        Assert.IsTrue(Homeworld.HasSpacePort && Homeworld.Position.InRadius(ship.Position, 50_000),
                      $"setup: our port is close by in another system (port {Homeworld.HasSpacePort}, {Homeworld.Position.Distance(ship.Position):0} away)");
        return ship;
    }

    [TestMethod]
    public void AShipThatCannotWarpIsScrappedAtAPortCloseByBeforeAColonyInItsSystem()
    {
        Ship ship = NoWarpFighterByAnOutpostNearOurPort(out _);

        ship.AI.OrderScrapShip();
        Assert.AreSame(Homeworld, ScrapGoal(ship)?.PlanetBuildingAt, "it is scrapped at the port close by, not at the colony in its system");
    }

    [TestMethod]
    public void AShipThatCannotWarpPassesOverAnUnsafePortForASafeColonyInItsSystem()
    {
        Ship ship = NoWarpFighterByAnOutpostNearOurPort(out Planet outpost);
        Homeworld.Quarantine = true;
        Player.UpdateRallyPoints();
        Assert.IsTrue(!Homeworld.Safe && Player.FindNearestRallyPoint(ship.Position) == Homeworld, "setup: the nearest rally point is our port, which is not safe");

        ship.AI.OrderScrapShip();
        Assert.AreSame(outpost, ScrapGoal(ship)?.PlanetBuildingAt, "it is scrapped at the safe colony in its system");
    }

    [TestMethod]
    public void AFighterLeftInASystemWithNoColonyOfOursIsScuttled()
    {
        Ship fighter = Launched(NoWarpFighter(Beside(NobodysPlanet)));
        RecallCheck(fighter);
        Assert.IsFalse(NobodysPlanet.System.HasPlanetsOwnedBy(Player), "setup: a system with no colony of ours");

        Assert.IsFalse(CarrierHolds(fighter), "the carrier's bay is freed");
        AssertScuttled(fighter);
    }

    [TestMethod]
    public void AFighterLeftInDeepSpaceIsScuttled()
    {
        Ship fighter = Launched(NoWarpFighter(new Vector2(-400_000, -400_000)));
        RecallCheck(fighter);
        Assert.IsNull(fighter.System, "setup: the fighter is in deep space");

        Assert.IsFalse(CarrierHolds(fighter), "the carrier's bay is freed");
        AssertScuttled(fighter);
    }

    [TestMethod]
    public void AnAssaultShuttleLandsItsTroopOnOurColonyWithRoom()
    {
        Ship shuttle = Launched(NoWarpAssaultShuttleWithATroop(Beside(Homeworld)));
        RecallCheck(shuttle);
        Assert.IsTrue(Homeworld.FreeTilesWithRebaseOnTheWay(Player) > 0, "setup: the colony has room for a troop");

        Assert.IsFalse(CarrierHolds(shuttle), "the carrier's bay is freed");
        AssertEqual(AIState.Rebase, shuttle.AI.State, "the shuttle takes its troop down to the colony");
        Assert.IsTrue(shuttle.AI.FindGoal(ShipAI.Plan.Rebase, out ShipAI.ShipGoal rebase) && rebase.TargetPlanet == Homeworld, "the homeworld");
    }

    [TestMethod]
    public void AnAssaultShuttleIsScuttledWhereOurColonyHasNoRoom()
    {
        Ship shuttle = Launched(NoWarpAssaultShuttleWithATroop(Beside(OurOutpost)));
        RecallCheck(shuttle);
        AssertEqual(0, OurOutpost.FreeTilesWithRebaseOnTheWay(Player), "setup: our colony has no room for a troop");

        AssertScuttled(shuttle);
    }

    [TestMethod]
    public void AnAssaultShuttleWithNoColonyOfOursIsScuttled()
    {
        Ship shuttle = Launched(NoWarpAssaultShuttleWithATroop(Beside(NobodysPlanet)));
        RecallCheck(shuttle);

        Assert.IsFalse(CarrierHolds(shuttle), "the carrier's bay is freed");
        AssertScuttled(shuttle);
    }

    [TestMethod]
    public void AnotherShipCarryingATroopIsScuttledThoughOurColonyHasRoom()
    {
        Ship fighter = Launched(NoWarpFighter(Beside(Homeworld)));
        ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(fighter);
        RecallCheck(fighter);
        Assert.IsTrue(!fighter.IsDefaultTroopTransport && Homeworld.FreeTilesWithRebaseOnTheWay(Player) > 0, "setup: a fighter with a troop by a colony with room");

        AssertScuttled(fighter);
    }

    [TestMethod]
    public void AFighterLeftByAQuarantinedColonyIsScuttled()
    {
        OurOutpost.Quarantine = true;
        Ship fighter = Launched(NoWarpFighter(Beside(OurOutpost)));
        RecallCheck(fighter);
        Assert.IsFalse(OurOutpost.Safe, "setup: our only colony in its system is not safe");

        AssertScuttled(fighter);
    }

    [TestMethod]
    public void AShipThatCanWarpFliesBackToItsCarrier()
    {
        Ship scout = Launched(SpawnShip(WarpScout, Player, Beside(NobodysPlanet)));
        RecallCheck(scout);
        Assert.IsTrue(scout.BaseCanWarp, "setup: the ship can warp");

        Assert.AreSame(Carrier, scout.Mothership, "the carrier keeps it");
        Assert.IsTrue(CarrierHolds(scout), "in its bay");
    }

    [TestMethod]
    public void AFighterOnlyOutOfSightOfItsCarrierFliesBack()
    {
        Ship fighter = Launched(NoWarpFighter(Carrier.Position + new Vector2(Carrier.SensorRange + 5_000, 0)));
        RecallCheck(fighter);
        float distance = fighter.Position.Distance(Carrier.Position);
        Assert.IsTrue(distance > Carrier.SensorRange && distance < 100_000, $"setup: out of the carrier's sensor range but not far ({distance:0})");

        Assert.AreSame(Carrier, fighter.Mothership, "the carrier keeps it");
        Assert.IsTrue(CarrierHolds(fighter), "in its bay");
    }
}
