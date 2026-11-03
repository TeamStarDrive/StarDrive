using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

/// <summary>
/// An AI fleet ship on its fleet's locked move fights back while an enemy ship's inhibitor holds it out of warp,
/// and goes back to the locked move once free.
/// </summary>
[TestClass]
public class InhibitedFleetMoveTests : StarDriveTest
{
    const string DreadnoughtName = "Dreadnought mk1-a";
    const string SlaverName = "Corsair-Slaver";
    static readonly Vector2 FleetPos = new(300_000f, 300_000f);
    static readonly Vector2 Destination = new(-300_000f, 300_000f);

    public InhibitedFleetMoveTests()
    {
        LoadStarterShips(DreadnoughtName, SlaverName);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
    }

    static TestShip Quiet(TestShip ship)
    {
        ship.AI.SetCombatTriggerDelay(1000f);
        return ship;
    }

    // two fleet ships together and a third far behind them, all on the fleet's locked move
    TestShip StragglerOnALockedFleetMove(Ship_Game.Empire owner)
    {
        TestShip first = Quiet(SpawnShip(DreadnoughtName, owner, FleetPos));
        TestShip second = Quiet(SpawnShip(DreadnoughtName, owner, FleetPos + new Vector2(500f, 0f)));
        TestShip straggler = Quiet(SpawnShip(DreadnoughtName, owner, FleetPos + new Vector2(0f, first.SensorRange * 2f)));
        Fleet fleet = owner.CreateFleet(1, null);
        foreach (TestShip ship in new[] { first, second, straggler })
            fleet.AddShip(ship);
        fleet.FleetTask = new MilitaryTask(AddDummyPlanet(new Vector2(-600_000f)), owner, MilitaryTaskImportance.Normal);
        fleet.MoveTo(Destination, FleetPos.DirectionToTarget(Destination), MoveOrder.Regular);
        Assert.IsTrue(straggler.AI.HasPriorityOrder, "setup: the fleet's move is locked");
        return straggler;
    }

    // a slaver of the given empire firing on the ship, its inhibitor listed the way the empire's turn lists it
    TestShip SlaverBeside(Ship ship, Ship_Game.Empire owner)
    {
        TestShip slaver = Quiet(SpawnShip(SlaverName, owner, ship.Position));
        slaver.Position = ship.Position + new Vector2(slaver.WeaponsMaxRange * 0.5f + ship.Radius, 0f);
        slaver.AI.OrderHoldPosition(MoveOrder.HoldPosition);
        slaver.AI.Target = ship;
        slaver.AI.HasPriorityTarget = true;
        AssertGreaterThan(slaver.InhibitionRadius, slaver.Position.Distance(ship.Position) + 3000f, "setup: the slaver's inhibitor reaches the ship");
        owner.Inhibitors.Add(slaver);
        return slaver;
    }

    static ShipAI.ShipGoal LastLeg(Ship ship) => ship.AI.OrderQueue.ToArray().LastOrDefault(
        g => g.WantedState == AIState.FormationMoveTo && g.Plan is ShipAI.Plan.MoveToWithin1000 or ShipAI.Plan.MakeFinalApproach);

    [TestMethod]
    public void AnAIFleetShipHeldOutOfWarpByAnEnemyShipFightsBackOnItsWayToTheSameSpot()
    {
        TestShip straggler = StragglerOnALockedFleetMove(Enemy);
        Vector2 spot = LastLeg(straggler).MovePosition;
        TestShip slaver = SlaverBeside(straggler, Player);
        RunObjectsSim(4f);

        Assert.IsTrue(straggler.Inhibited, "setup: the slaver holds the ship out of warp");
        Assert.IsFalse(straggler.AI.HasPriorityOrder, "the ship drops its locked move");
        Assert.IsTrue(LastLeg(straggler) is { HasAggressiveMoveOrder: true } leg && leg.MovePosition == spot, "its move turns aggressive, to the same spot");
        Assert.AreSame(slaver, straggler.AI.Target, "it takes on the ship that holds it");
        Assert.IsTrue(straggler.InCombat, "and keeps fighting there, though far from its fleet");
    }

    [TestMethod]
    public void AnAIFleetShipFreedOfTheInhibitorGoesBackToItsLockedMove()
    {
        TestShip straggler = StragglerOnALockedFleetMove(Enemy);
        Vector2 spot = LastLeg(straggler).MovePosition;
        TestShip slaver = SlaverBeside(straggler, Player);
        RunObjectsSim(4f);
        Assert.IsFalse(straggler.AI.HasPriorityOrder, "setup: the ship fights back");

        Player.Inhibitors.Clear();
        slaver.Die(slaver, cleanupOnly: true);
        RunSimWhile((simTimeout: 10, fatal: false), () => !straggler.AI.HasPriorityOrder);

        Assert.IsFalse(straggler.IsInhibitedFromEnemyShips(), "setup: no enemy inhibitor reaches the ship any more");
        Assert.IsTrue(straggler.AI.HasPriorityOrder, "the ship goes back to its locked move");
        Assert.IsTrue(LastLeg(straggler) is { HasAggressiveMoveOrder: false } leg && leg.MovePosition == spot, "to the same spot");
    }

    [TestMethod]
    public void AnAIFleetShipInAGravityWellKeepsItsLockedMove()
    {
        TestShip straggler = StragglerOnALockedFleetMove(Enemy);
        Quiet(SpawnShip(DreadnoughtName, Player, straggler.Position + new Vector2(3000f, 0f))).AI.OrderHoldPosition(MoveOrder.HoldPosition);
        RunObjectsSim(TestSimStep);
        straggler.SetWarpInhibited(Ship.InhibitionType.GravityWell, secondsToInhibit: 10f);
        RunObjectsSim(3f);

        Assert.IsTrue(straggler.Inhibited && straggler.InhibitionSource == Ship.InhibitionType.GravityWell, "setup: a gravity well holds the ship");
        Assert.IsTrue(straggler.AI.HasPriorityOrder, "only an enemy ship's inhibitor turns the fleet's move aggressive");
        Assert.IsTrue(LastLeg(straggler) is { HasAggressiveMoveOrder: false });
    }

    [TestMethod]
    public void AShipFightingBackFarFromItsFleetIsNotPulledBackOntoItsLockedMove()
    {
        TestShip straggler = StragglerOnALockedFleetMove(Enemy);
        SlaverBeside(straggler, Player);
        bool switched = false, relocked = false;
        RunObjectsSim(6f, () =>
        {
            if (!straggler.AI.HasPriorityOrder)
                switched = true;
            else if (switched)
                relocked = true;
        });

        Assert.IsTrue(switched, "setup: the ship fights back");
        Assert.IsFalse(relocked, "while the enemy holds it, its fleet's leash does not pull it back onto a locked move");
    }

    // knocks out modules on internal slots until the ship wants repairs, keeping it well above its death line
    static void DamageForRepairs(Ship ship)
    {
        float wantsRepairsBelow = ShipResupply.DamageThreshold(ship.ShipData.ShipCategory);
        foreach (ShipModule m in ship.Modules)
        {
            if (ship.InternalSlotsHealthPercent < wantsRepairsBelow - 0.05f)
                break;
            if (ship.Grid.InternalSlotsOf(m) > 0 && !m.IsCommandModule)
                m.SetHealth(0f, "Test");
        }
        AssertLessThan(ship.InternalSlotsHealthPercent, wantsRepairsBelow, "setup: the ship wants repairs");
        AssertGreaterThan(ship.InternalSlotsHealthPercent, 0.55f, "setup: the ship is far from its death line");
    }

    [TestMethod]
    public void AFleetShipOnItsWayToRepairsIsLeftToThem()
    {
        Planet home = AddHomeWorldToEmpire(new Vector2(600_000f), Enemy);
        TestShip straggler = StragglerOnALockedFleetMove(Enemy);
        DamageForRepairs(straggler);
        straggler.AI.OrderResupply(home, clearOrders: false);
        Assert.IsTrue(straggler.Resupplying && straggler.AI.HasPriorityOrder, "setup: the ship heads for repairs");
        Assert.IsTrue(LastLeg(straggler) is { HasAggressiveMoveOrder: false }, "setup: its fleet's locked move waits behind the repairs");

        SlaverBeside(straggler, Player);
        bool leftRepairs = false;
        RunObjectsSim(4f, () => leftRepairs |= !straggler.Resupplying);

        Assert.IsTrue(straggler.Inhibited, "setup: the slaver holds the ship out of warp");
        Assert.IsFalse(leftRepairs, "a ship going for repairs is not turned back to fight on its fleet's move");
    }

    [TestMethod]
    public void AShipThatTurnsToRepairsWhileFightingBackKeepsToThemOnceFree()
    {
        AddHomeWorldToEmpire(new Vector2(600_000f), Enemy);
        TestShip straggler = StragglerOnALockedFleetMove(Enemy);
        TestShip slaver = SlaverBeside(straggler, Player);
        RunObjectsSim(4f);
        Assert.IsFalse(straggler.AI.HasPriorityOrder, "setup: the ship fights back");
        DamageForRepairs(straggler);
        RunSimWhile((simTimeout: 3, fatal: true), () => !straggler.Resupplying);

        Player.Inhibitors.Clear();
        slaver.Die(slaver, cleanupOnly: true);
        bool leftRepairs = false;
        RunObjectsSim(8f, () => leftRepairs |= !straggler.Resupplying);

        Assert.IsFalse(straggler.IsInhibitedFromEnemyShips(), "setup: no enemy inhibitor reaches the ship any more");
        Assert.IsFalse(leftRepairs, "once free, a ship gone for repairs is not put back on its fleet's locked move");
    }

    [TestMethod]
    public void AShipsWarpInhibitionAndFightBackSpotSurviveASave()
    {
        UState.StarDate = 1042.5f;
        TestShip ship = Quiet(SpawnShip(DreadnoughtName, Enemy, FleetPos));
        RunObjectsSim(TestSimStep);
        ship.SetWarpInhibited(Ship.InhibitionType.EnemyShip, secondsToInhibit: 5f);
        FieldInfo spotField = typeof(ShipAI).GetField("FightBackSpot", BindingFlags.Instance | BindingFlags.NonPublic)!;
        spotField.SetValue(ship.AI, FleetPos);

        SavedGame save = Universe.Save("UnitTest.InhibitedFleetMove", throwOnError: true);
        UniverseScreen loaded = LoadSave(save.SaveFile);
        Ship after = loaded.UState.Objects.FindShip(ship.Id);

        Assert.IsTrue(after.Inhibited && after.InhibitionSource == Ship.InhibitionType.EnemyShip,
                      "a loaded ship still knows an enemy ship holds it, so it does not drop its fight on the first update");
        AssertEqual(FleetPos, (Vector2)spotField.GetValue(after.AI), "and it remembers the move it turned aggressive");
    }

    [TestMethod]
    public void APlayerFleetShipHeldByAnEnemyShipKeepsItsLockedMove()
    {
        TestShip straggler = StragglerOnALockedFleetMove(Player);
        SlaverBeside(straggler, Enemy);
        RunObjectsSim(4f);

        Assert.IsTrue(straggler.Inhibited, "setup: the slaver holds the ship out of warp");
        Assert.IsTrue(straggler.AI.HasPriorityOrder, "the player's own locked move stands");
        Assert.IsTrue(LastLeg(straggler) is { HasAggressiveMoveOrder: false });
    }
}
