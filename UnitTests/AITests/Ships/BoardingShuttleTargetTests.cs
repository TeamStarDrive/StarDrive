using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

[TestClass]
public class BoardingShuttleTargetTests : StarDriveTest
{
    const string FrigateName = "TEST_Spearhead mk1-a";
    const string ShuttleName = "Assault Shuttle";
    const string FighterName = "Vulcan Scout";
    readonly TestShip Us;
    readonly TestShip EnemyFrigate;

    public BoardingShuttleTargetTests()
    {
        LoadStarterShips(FrigateName, ShuttleName, FighterName);
        CreateUniverseAndPlayerEmpire();
        Us = SpawnShip(FrigateName, Player, new Vector2(0f, 0.1f));
        EnemyFrigate = Facing(SpawnShip(FrigateName, Enemy, new Vector2(0f, -650f)));
    }

    TestShip Facing(TestShip ship)
    {
        ship.Rotation = ship.Position.DirectionToTarget(Us.Position).ToRadians();
        return ship;
    }

    Ship ShuttleBoarding(Ship target)
    {
        TestShip shuttle = Facing(SpawnShip(ShuttleName, Enemy, new Vector2(100f, -700f)));
        ResourceManager.CreateTroop("Wyvern", Enemy).LandOnShip(shuttle);
        shuttle.AI.OrderTroopToBoardShip(target);
        shuttle.AI.ChangeAIState(AIState.Boarding);
        return shuttle;
    }

    Ship Mate() => SpawnShip(FrigateName, Player, new Vector2(300f, 0.1f));

    Ship HighestPriority(params Ship[] targets)
    {
        Us.AI.PotentialTargets = targets;
        return Us.AI.GetHighestPriorityTarget();
    }

    [TestMethod]
    public void AShuttleBoardingUsComesBeforeAFrigateNearby()
    {
        Ship shuttle = ShuttleBoarding(Us);

        Assert.AreSame(shuttle, HighestPriority(EnemyFrigate, shuttle), "a shuttle about to board us is a threat to us, small as it is");
    }

    [TestMethod]
    public void AShuttleBoardingAFleetMateComesBeforeAFrigateNearby()
    {
        Ship mate = Mate();
        Fleet fleet = Player.CreateFleet(1, null);
        fleet.AddShip(Us);
        fleet.AddShip(mate);
        Ship shuttle = ShuttleBoarding(mate);

        Assert.AreSame(shuttle, HighestPriority(EnemyFrigate, shuttle), "a shuttle about to board a ship of our fleet is a threat to us too");
    }

    [TestMethod]
    public void AShuttleBoardingTheShipWeEscortComesBeforeAFrigateFiringOnUs()
    {
        Ship ward = Mate();
        Us.AI.EscortTarget = ward;
        EnemyFrigate.AI.Target = Us;
        Ship shuttle = ShuttleBoarding(ward);

        Assert.AreSame(shuttle, HighestPriority(EnemyFrigate, shuttle), "boarding the ship we escort is attacking it, which comes first for an escort");
    }

    [TestMethod]
    public void AFighterCountsAShuttleBoardingItsCarrier()
    {
        TestShip fighter = SpawnShip(FighterName, Player, new Vector2(0f, -100f));
        fighter.Mothership = Us;
        Ship shuttle = ShuttleBoarding(Us);
        Ship enemyFighter = Facing(SpawnShip(FighterName, Enemy, new Vector2(0f, -1400f)));

        fighter.AI.PotentialTargets = new[] { enemyFighter, shuttle };

        Assert.AreSame(shuttle, fighter.AI.GetHighestPriorityTarget(), "a shuttle about to board its carrier is a threat to the fighter's own side");
    }

    [TestMethod]
    public void AFighterCountsAShuttleBoardingItsCarriersFleet()
    {
        Ship mate = Mate();
        Fleet fleet = Player.CreateFleet(1, null);
        fleet.AddShip(Us);
        fleet.AddShip(mate);
        TestShip fighter = SpawnShip(FighterName, Player, new Vector2(0f, -100f));
        fighter.Mothership = Us;
        Ship shuttle = ShuttleBoarding(mate);
        Ship enemyFighter = Facing(SpawnShip(FighterName, Enemy, new Vector2(0f, -1400f)));

        fighter.AI.PotentialTargets = new[] { enemyFighter, shuttle };

        Assert.AreSame(shuttle, fighter.AI.GetHighestPriorityTarget(), "a fighter flies for its carrier's fleet");
    }

    [TestMethod]
    public void AShuttleBoardingAShipOutsideOurFleetKeepsItsLowPriority()
    {
        Ship shuttle = ShuttleBoarding(Mate());

        Assert.AreSame(EnemyFrigate, HighestPriority(EnemyFrigate, shuttle), "a small ship boarding a ship we neither are, escort nor fly with stays low");
    }
}
