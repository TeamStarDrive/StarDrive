using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

/// <summary>
/// A supply shuttle handing its ordnance to a ship, from a carrier's supply bay or from a colony, sends small cargo
/// shuttles from itself to that ship: one per 2 ordnance given, 1 to 5. They are only a visual effect, drawn for a
/// ship on screen at planet view or closer, and the supply shuttle does not land.
/// </summary>
[TestClass]
public class OrdnanceShuttleTests : StarDriveTest
{
    readonly Planet Homeworld;
    Ship Target;

    public OrdnanceShuttleTests()
    {
        LoadStarterShips("TEST_Excalibur-Class Supercarrier");
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Homeworld = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
        Player.UpdateRallyPoints();
        UState.ViewState = UniverseScreen.UnivScreenState.PlanetView;
    }

    void Step(Func<bool> condition, Action body = null)
    {
        for (double elapsed = 0; condition(); elapsed += TestSimStepD)
        {
            body?.Invoke();
            if (elapsed >= 120)
                throw new TimeoutException("Timed out in Step");
            if (Target != null)
                Target.InFrustum = true;
            UState.Objects.Update(TestSimStep);
            Universe.CargoShuttles.Update(Universe, TestSimStep);
        }
    }

    void StepOnce()
    {
        int steps = 0;
        Step(() => steps++ < 1);
    }

    Ship SpawnTarget(Vector2 position, float ordnanceMissing, Empire owner = null)
    {
        Target = SpawnShip("Rocket Scout", owner ?? Player, position);
        AssertGreaterThan(Target.OrdinanceMax, ordnanceMissing, "setup: the ship to rearm must hold more ordnance than it misses");
        Target.ChangeOrdnance(-ordnanceMissing);
        Target.AI.OrderHoldPosition(MoveOrder.HoldPosition);
        return Target;
    }

    Ship CarrierShuttleTo(Ship target)
    {
        Ship carrier = SpawnShip("TEST_Excalibur-Class Supercarrier", target.Loyalty, target.Position + new Vector2(0, 3000));
        ShipModule bay = carrier.Carrier.AllSupplyBays.First();
        Ship shuttle = Ship.CreateShipFromHangar(UState, bay, target.Loyalty, carrier.Position, carrier);
        carrier.OnShipLaunched(shuttle, bay);
        Assert.IsTrue(shuttle is { IsSupplyShuttle: true }, "setup: the carrier must launch a supply shuttle");
        Step(() => shuttle.IsLaunching);
        AssertGreaterThan(shuttle.Ordinance, 10f, "setup: the shuttle must carry ordnance to give");
        shuttle.AI.AddSupplyShipGoal(target);
        return shuttle;
    }

    static Vector2 DeepSpace => new(300_000);
    Vector2 InSystem => Homeworld.Position + new Vector2(6000, 2500);

    int InFlight(Ship shuttle) => Universe.CargoShuttles.InFlight(shuttle, Target);

    [TestMethod]
    public void OneCargoShuttlePer2OrdnanceGivenFrom1To5()
    {
        AssertEqual(1, CargoShuttles.OrdnanceShuttlesFor(0.5f), "any ordnance given takes at least one shuttle");
        AssertEqual(1, CargoShuttles.OrdnanceShuttlesFor(3.9f), "a shuttle carries 2 ordnance");
        AssertEqual(2, CargoShuttles.OrdnanceShuttlesFor(4f), "a shuttle carries 2 ordnance");
        AssertEqual(5, CargoShuttles.OrdnanceShuttlesFor(10f), "a shuttle carries 2 ordnance");
        AssertEqual(5, CargoShuttles.OrdnanceShuttlesFor(500f), "there are at most 5 shuttles");
    }

    [TestMethod]
    public void ACarrierSupplyShuttleSendsCargoShuttlesToTheShipItRearms()
    {
        Ship target = SpawnTarget(DeepSpace, ordnanceMissing: 6f);
        Ship shuttle = CarrierShuttleTo(target);
        float carried = shuttle.Ordinance;
        Step(() => shuttle.Ordinance >= carried);

        float given = carried - shuttle.Ordinance;
        AssertEqual(0.01f, 6f, given, "setup: the shuttle gives what the ship misses");
        AssertEqual(3, InFlight(shuttle), "one cargo shuttle per 2 ordnance given");
        Assert.IsFalse(shuttle.IsLanding, "the supply shuttle hands over without landing on the ship");

        Step(() => InFlight(shuttle) > 0, () => AssertLessThan(InFlight(shuttle), 4, "no more shuttles are sent"));
    }

    [TestMethod]
    public void NoCargoShuttlesWhenNothingIsGiven()
    {
        Ship target = SpawnTarget(DeepSpace, ordnanceMissing: 0f);
        Ship shuttle = CarrierShuttleTo(target);
        target.InFrustum = true;
        shuttle.SendOrdnanceShuttles(target, 0f);
        StepOnce();
        AssertEqual(0, InFlight(shuttle), "no ordnance given, no shuttles");

        target.InFrustum = true;
        shuttle.SendOrdnanceShuttles(target, 1f);
        StepOnce();
        AssertEqual(1, InFlight(shuttle), "while any ordnance given takes one");
    }

    [TestMethod]
    public void NoCargoShuttlesWhenZoomedOutPastPlanetView()
    {
        UState.ViewState = UniverseScreen.UnivScreenState.SystemView;
        Ship target = SpawnTarget(DeepSpace, ordnanceMissing: 6f);
        Ship shuttle = CarrierShuttleTo(target);
        float carried = shuttle.Ordinance;
        Step(() => shuttle.Ordinance >= carried);
        AssertEqual(0, InFlight(shuttle), "the shuttles are too small to see past planet view");
    }

    [TestMethod]
    public void NoCargoShuttlesForAShipOffScreen()
    {
        Ship target = SpawnTarget(DeepSpace, ordnanceMissing: 6f);
        Ship shuttle = CarrierShuttleTo(target);
        Target = null;
        target.InFrustum = false;
        float carried = shuttle.Ordinance;
        Step(() => shuttle.Ordinance >= carried);
        Assert.IsFalse(target.InFrustum, "setup: the ship must be off screen");
        AssertEqual(0, Universe.CargoShuttles.InFlight(shuttle, target), "no shuttles fly to a ship that is not on screen");
    }

    [TestMethod]
    public void NoCargoShuttlesFromASupplyShuttleOutOfOurSensors()
    {
        Ship target = SpawnTarget(DeepSpace, ordnanceMissing: 6f, Enemy);
        Ship shuttle = CarrierShuttleTo(target);
        float carried = shuttle.Ordinance;
        Step(() => shuttle.Ordinance >= carried);
        Assert.IsFalse(shuttle.InPlayerSensorRange, "setup: the supply shuttle must be out of our sensors");
        AssertEqual(0, InFlight(shuttle), "no shuttles fly from a supply shuttle we cannot see");
    }

    [TestMethod]
    public void AColonySupplyShuttleSendsCargoShuttlesToTheShipItRearms()
    {
        Ship target = SpawnTarget(InSystem, ordnanceMissing: 20f);
        Step(() => target.System == null);
        Player.AI.AddPlanetaryRearmGoal(target, Homeworld);
        Goal rearm = Player.AI.FindGoal(g => g.Type == GoalType.RearmShipFromPlanet && g.TargetShip == target);
        Assert.IsNotNull(rearm, "setup: the colony must take on the rearm");
        rearm.Evaluate();
        Ship shuttle = rearm.FinishedShip;
        Assert.IsTrue(shuttle is { IsSupplyShuttle: true }, "setup: the colony must launch a supply shuttle");

        Step(() => shuttle.IsLaunching);
        float carried = shuttle.Ordinance;
        AssertGreaterThan(carried, 20f, "setup: the shuttle must carry more than the ship misses");
        int frame = 0;
        Step(() => shuttle.Ordinance >= carried, () =>
        {
            if (++frame % 60 == 0)
                rearm.Evaluate();
        });
        float given = carried - shuttle.Ordinance;
        AssertGreaterThan(given, 10f, "setup: the shuttle must give at least 10 ordnance");
        AssertEqual(5, InFlight(shuttle), "10 ordnance or more given takes the most shuttles, 5");
        Assert.IsFalse(shuttle.IsLanding, "the supply shuttle hands over without landing on the ship");
    }
}
