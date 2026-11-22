using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

/// <summary>
/// On a player's attack or attack move made with warships, support ships, carriers and bombers hold back:
/// they fight from their spot, carriers launching their fighters, and close in on no enemy until their next order.
/// </summary>
[TestClass]
public class SupportHoldsBackTests : StarDriveTest
{
    const string Warship = "Terran-Prototype";
    const string Support = "Kwark support mk1";
    const string Carrier = "TEST_Heavy Carrier mk1";
    const string Bomber = "Alliance-Class Mk Ia Hvy Assault";
    static readonly Vector2 Ours = new(200_000f, 300_000f);

    public SupportHoldsBackTests()
    {
        LoadStarterShips(Warship, Support, Carrier, Bomber, "Ving Defender");
        CreateUniverseAndPlayerEmpire();
        UnlockAllShipsFor(Player);
        UState.Objects.EnableParallelUpdate = false;
        UState.P.DebugDisableShipLaunch = true;
    }

    Ship Spawn(string design, Ship_Game.Empire owner, Vector2 at)
    {
        Ship ship = SpawnShip(design, owner, at);
        ship.AI.OrderHoldPosition();
        return ship;
    }

    Ship EnemyAt(Vector2 at) => Spawn("Ving Defender", Enemy, at);

    ShipMoveCommands Commands => new(Universe);

    [TestMethod]
    public void SupportShipsCarriersAndBombersAreTheShipsThatHoldBack()
    {
        Assert.IsFalse(Spawn(Warship, Player, Ours).HoldsBackFromAttacks, "a warship takes the attack");
        foreach (string design in new[] { Support, Carrier, Bomber })
        {
            Ship ship = Spawn(design, Player, Ours);
            AssertEqual(RoleType.WarSupport, ship.DesignRoleType, $"setup: {design} supports the warships");
            Assert.IsTrue(ship.HoldsBackFromAttacks, $"{design} holds back");
        }
    }

    // a support ship facing the enemy on a stance that closes in, as one with short range weapons would
    Ship ShortRangeSupport()
    {
        Ship support = Spawn(Support, Player, Ours + new Vector2(0f, 2_000f));
        support.AI.CombatState = CombatState.AttackRuns;
        support.Rotation = Vectors.Right.ToRadians();
        return support;
    }

    [TestMethod]
    public void ASupportShipOnAStanceThatClosesInDoesSoWhenItIsTheEnemysOnlyFoe()
    {
        Ship support = ShortRangeSupport();
        Ship enemy = EnemyAt(Ours + new Vector2(15_000f, 0f));
        RunObjectsSim(TestSimStep);
        Vector2 spot = support.Position;

        RunObjectsSim(8f);

        Assert.IsTrue(support.InCombat && support.AI.Target == enemy, "it fights the enemy it sees");
        AssertGreaterThan(support.Position.Distance(spot), 300f, "and closes in, as its stance says");
    }

    [TestMethod]
    public void ASupportShipAttackingWithAWarshipHoldsItsSpotInTheFight()
    {
        Ship warship = Spawn(Warship, Player, Ours);
        Ship support = ShortRangeSupport();
        Ship enemy = EnemyAt(Ours + new Vector2(15_000f, 0f));
        RunObjectsSim(TestSimStep);

        Commands.RightClickOnShip(new Array<Ship> { warship, support }, enemy);
        Assert.IsTrue(warship.AI.HasPriorityTarget && warship.AI.Target == enemy, "the warship attacks");
        Assert.IsTrue(support.AI.HoldsBack, "the support ship holds back");

        Vector2 spot = support.Position;
        RunObjectsSim(8f);
        Assert.IsTrue(support.InCombat && support.AI.Target == enemy, "setup: the support ship fights the enemy it sees");
        AssertLessThan(support.Position.Distance(spot), 100f, "from where it was, closing in on nothing whatever its stance");
        AssertLessThan(warship.Position.Distance(enemy.Position), 15_000f - 500f, "while the warship closes in");
    }

    [TestMethod]
    public void SupportShipsAttackingWithoutAWarshipCloseIn()
    {
        Ship support = Spawn(Support, Player, Ours);
        Ship enemy = EnemyAt(Ours + new Vector2(15_000f, 0f));
        RunObjectsSim(TestSimStep);

        Commands.RightClickOnShip(new Array<Ship> { support }, enemy);

        Assert.IsFalse(support.AI.HoldsBack, "with no warship to attack, the support ships make the attack");
        Assert.IsTrue(support.AI.HasPriorityTarget && support.AI.Target == enemy, "and attack the enemy themselves");
    }

    [TestMethod]
    public void ACarrierHoldingBackLaunchesItsFighters()
    {
        Ship warship = Spawn(Warship, Player, Ours);
        Ship carrier = Spawn(Carrier, Player, Ours + new Vector2(0f, 2_000f));
        Ship enemy = EnemyAt(Ours + new Vector2(8_000f, 0f));
        RunObjectsSim(TestSimStep);

        Commands.RightClickOnShip(new Array<Ship> { warship, carrier }, enemy);
        Assert.IsTrue(carrier.AI.HoldsBack, "setup: the carrier holds back");
        Vector2 spot = carrier.Position;
        RunObjectsSim(5f);

        AssertGreaterThan(carrier.Carrier.GetActiveFighters().Count, 0, "it launches its fighters at the enemy");
        AssertLessThan(carrier.Position.X, spot.X + 50f, "never moving toward the enemy");
    }

    [TestMethod]
    public void AHoldingBackShipThatKeepsItsDistanceBacksAwayFromAnEnemyClosingIn()
    {
        Ship warship = Spawn(Warship, Player, Ours - new Vector2(50_000f, 0f));
        Ship support = Spawn(Support, Player, Ours);
        support.Rotation = Vectors.Right.ToRadians();
        Ship enemy = EnemyAt(Ours + new Vector2(4_000f, 0f));
        AssertEqual(CombatState.Artillery, support.AI.CombatState, "setup: the support ship keeps its distance from enemies");
        RunObjectsSim(TestSimStep);
        AssertLessThan(enemy.Position.Distance(support.Position), support.DesiredCombatRange * 0.9f, "setup: the enemy is inside the support ship's range");

        Commands.RightClickOnShip(new Array<Ship> { warship, support }, enemy);
        Assert.IsTrue(support.AI.HoldsBack, "setup: the support ship holds back");
        Vector2 spot = support.Position;
        float east = spot.X, west = spot.X;
        for (double t = 0; t < 10.0; t += TestSimStepD)
        {
            RunObjectsSim(TestSimStep);
            east = System.Math.Max(east, support.Position.X);
            west = System.Math.Min(west, support.Position.X);
        }

        AssertLessThan(west, spot.X - 100f, "the support ship backs away from it");
        AssertLessThan(east, spot.X + 20f, "and never moves toward it");
    }

    [TestMethod]
    public void AHoldingBackShipOnAStanceThatClosesInHoldsWithAnEnemyClose()
    {
        Ship warship = Spawn(Warship, Player, Ours - new Vector2(50_000f, 0f));
        Ship support = ShortRangeSupport();
        Ship enemy = EnemyAt(support.Position + new Vector2(4_000f, 0f));
        RunObjectsSim(TestSimStep);

        Commands.RightClickOnShip(new Array<Ship> { warship, support }, enemy);
        Vector2 spot = support.Position;
        bool fought = false;
        float farthest = 0f;
        for (double t = 0; t < 6.0 && support.Active; t += TestSimStepD)
        {
            RunObjectsSim(TestSimStep);
            fought |= support.InCombat && support.AI.Target == enemy;
            farthest = System.Math.Max(farthest, support.Position.Distance(spot));
        }

        Assert.IsTrue(fought, "setup: the support ship fights the enemy beside it");
        AssertLessThan(farthest, 100f, "it holds its spot, neither closing in nor backing away");
    }

    [TestMethod]
    public void OnAnAttackMoveASupportShipAtWarpDropsOutWhereTheFightFindsIt()
    {
        Ship warship = Spawn(Warship, Player, Ours);
        Ship support = Spawn(Support, Player, Ours + new Vector2(0f, 2_000f));
        EnemyAt(Ours + new Vector2(300_000f, 20_000f));
        Vector2 dest = Ours + new Vector2(600_000f, 0f);
        var group = new ShipGroup(new Array<Ship> { warship, support }, dest, dest, Vectors.Right, Player);

        group.MoveTo(dest, Vectors.Right, MoveOrder.Aggressive);
        RunSimWhile((simTimeout: 60, fatal: true), () => !support.IsInWarp);
        bool atWarp = false;
        RunSimWhile((simTimeout: 60, fatal: true), () => !support.InCombat, () => atWarp = support.IsInWarp);
        Assert.IsTrue(atWarp, "setup: the fight finds the support ship at warp");
        RunObjectsSim(1f);

        Assert.IsFalse(support.IsSpoolingOrInWarp, "it drops out of warp to hold back there");
        Assert.IsTrue(support.AI.HoldsBack, "and still holds back");
    }

    [TestMethod]
    public void ASupportShipHoldingBackDropsTheEnemyItWasToldToAttackBefore()
    {
        Ship warship = Spawn(Warship, Player, Ours);
        Ship support = ShortRangeSupport();
        Ship first = EnemyAt(Ours - new Vector2(15_000f, 0f));
        Ship second = EnemyAt(Ours + new Vector2(15_000f, 0f));
        RunObjectsSim(TestSimStep);
        support.AI.OrderAttackSpecificTarget(first);
        Assert.IsTrue(support.AI.HasPriorityTarget && support.AI.Target == first, "setup: the support ship was told to attack another enemy");

        Commands.RightClickOnShip(new Array<Ship> { warship, support }, second);

        Assert.IsTrue(support.AI.HoldsBack, "it holds back from the new attack");
        Assert.IsFalse(support.AI.HasPriorityTarget, "with no target the player picked");
        Assert.IsNull(support.AI.Target, "and without the enemy it was told to attack before");
    }

    [TestMethod]
    public void ASupportShipMovingWhenToldToHoldBackKeepsHoldingBackAfterItOvershoots()
    {
        Ship warship = Spawn(Warship, Player, Ours);
        Ship support = Spawn(Support, Player, Ours + new Vector2(0f, 2_000f));
        Ship enemy = EnemyAt(Ours + new Vector2(30_000f, 0f));
        support.AI.OrderMoveTo(Ours + new Vector2(0f, 600_000f), Vectors.Down);
        RunSimWhile((simTimeout: 60, fatal: true), () => !support.IsInWarp);

        Commands.RightClickOnShip(new Array<Ship> { warship, support }, enemy);
        Vector2 spot = support.Position;
        float farthest = 0f;
        RunSimWhile((simTimeout: 5, fatal: false), null, () => farthest = System.Math.Max(farthest, support.Position.Distance(spot)));

        AssertGreaterThan(farthest, 100f, "setup: the support ship overshot the spot it was told to hold");
        Assert.IsTrue(support.AI.HoldsBack, "flying back to that spot, it still holds back");
    }

    [TestMethod]
    public void ARightClickOnAShipWeMayNotAttackChangesNoSupportShipsOrders()
    {
        CreateThirdMajorEmpire();
        Ship warship = Spawn(Warship, Player, Ours);
        Ship support = Spawn(Support, Player, Ours + new Vector2(0f, 2_000f));
        Ship theirs = Spawn("Ving Defender", ThirdMajor, Ours + new Vector2(15_000f, 0f));
        Assert.IsFalse(Player.IsEmpireAttackable(ThirdMajor, theirs), "setup: we may not attack that empire's ships");
        RunObjectsSim(TestSimStep);
        ShipAI.ShipGoal before = support.AI.OrderQueue.PeekFirst;

        Commands.RightClickOnShip(new Array<Ship> { warship, support }, theirs);

        Assert.IsFalse(warship.AI.HasPriorityTarget, "setup: the warship does not take the attack");
        Assert.IsFalse(support.AI.HoldsBack, "so the support ship does not hold back");
        Assert.AreSame(before, support.AI.OrderQueue.PeekFirst, "and keeps its orders");
    }

    [TestMethod]
    public void AFleetsSupportShipHoldsBackFromTheFleetsAttack()
    {
        Ship warship = Spawn(Warship, Player, Ours);
        Ship bomber = Spawn(Bomber, Player, Ours + new Vector2(0f, 2_000f));
        Ship enemy = EnemyAt(Ours + new Vector2(6_000f, 0f));
        Fleet fleet = Player.CreateFleet(1, null);
        fleet.AddShip(warship);
        fleet.AddShip(bomber);
        RunObjectsSim(TestSimStep);

        Commands.MoveFleetToLocation(null, enemy, null, enemy.Position, Vectors.Up, fleet);

        Assert.IsTrue(warship.AI.HasPriorityTarget && warship.AI.Target == enemy, "the fleet's warship attacks");
        Assert.IsTrue(bomber.AI.HoldsBack, "its bomber holds back");
        Assert.IsFalse(bomber.AI.HasPriorityTarget, "and takes no part in the attack order");
    }

    [TestMethod]
    public void OnAnAttackMoveSupportShipsTakeTheSameMoveAndHoldBack()
    {
        Ship warship = Spawn(Warship, Player, Ours);
        Ship support = Spawn(Support, Player, Ours + new Vector2(0f, 2_000f));
        Fleet fleet = Player.CreateFleet(1, null);
        fleet.AddShip(warship);
        fleet.AddShip(support);

        fleet.MoveTo(Ours + new Vector2(50_000f, 0f), Vectors.Right, MoveOrder.Aggressive);

        Assert.IsTrue(warship.AI.OrderQueue.PeekFirst.HasAggressiveMoveOrder, "the warship breaks off to fight on its way");
        Assert.IsTrue(support.AI.OrderQueue.PeekFirst.HasAggressiveMoveOrder, "the support ship takes the same move, on the same path");
        Assert.IsTrue(support.AI.HoldsBack, "but holds back when it fights");
        Assert.IsFalse(warship.AI.HoldsBack, "the warship does not");
    }

    [TestMethod]
    public void OnAnAttackMoveASupportShipStopsForTheFightAndCarriesOnAfterIt()
    {
        Ship warship = Spawn(Warship, Player, Ours);
        Ship support = ShortRangeSupport();
        Ship enemy = EnemyAt(Ours + new Vector2(25_000f, 20_000f));
        Fleet fleet = Player.CreateFleet(1, null);
        fleet.AddShip(warship);
        fleet.AddShip(support);
        RunObjectsSim(TestSimStep);

        fleet.MoveTo(Ours + new Vector2(60_000f, 0f), Vectors.Right, MoveOrder.Aggressive);
        RunSimWhile((simTimeout: 10, fatal: true), () => !support.InCombat);
        Assert.AreEqual(enemy, support.AI.Target, "setup: the support ship fights the enemy it meets on its way");
        Vector2 spot = support.Position;
        RunObjectsSim(5f);
        AssertLessThan(support.Position.Distance(spot), 150f, "it stops where the fight found it, closing in on nothing");

        enemy.QueueTotalRemoval();
        RunSimWhile((simTimeout: 30, fatal: false), () => support.Position.X < spot.X + 500f);
        AssertGreaterThan(support.Position.X, spot.X + 500f, "with the fight over it carries on with its move");
    }

    [TestMethod]
    public void AnAIGroupsSupportShipsDoNotHoldBack()
    {
        Ship warship = Spawn(Warship, Enemy, Ours);
        Ship support = Spawn(Support, Enemy, Ours + new Vector2(0f, 2_000f));
        Fleet fleet = Enemy.CreateFleet(1, null);
        fleet.AddShip(warship);
        fleet.AddShip(support);

        fleet.MoveTo(Ours + new Vector2(50_000f, 0f), Vectors.Right, MoveOrder.Aggressive);

        Assert.IsFalse(support.AI.HoldsBack, "the rule is for the player's orders; AI fleets keep their rear by their own tasks");
    }

    [TestMethod]
    public void HoldingBackLastsUntilTheNextOrderAndThroughARouteDrawnAgain()
    {
        Ship warship = Spawn(Warship, Player, Ours);
        Ship support = Spawn(Support, Player, Ours + new Vector2(0f, 2_000f));
        Fleet fleet = Player.CreateFleet(1, null);
        fleet.AddShip(warship);
        fleet.AddShip(support);
        fleet.MoveTo(Ours + new Vector2(600_000f, 0f), Vectors.Right, MoveOrder.Aggressive);
        ShipAI.ShipGoal before = support.AI.OrderQueue.PeekFirst;

        support.AI.OnSystemNewlyExplored(null);
        Assert.AreNotSame(before, support.AI.OrderQueue.PeekFirst, "setup: the move was issued again for the explored system");
        Assert.IsTrue(support.AI.HoldsBack, "a move issued again keeps the ship holding back");

        support.AI.OrderMoveTo(Ours, Vectors.Up);
        Assert.IsFalse(support.AI.HoldsBack, "the player's next order ends it");
    }
}
