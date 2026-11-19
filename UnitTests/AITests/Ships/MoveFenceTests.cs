using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

[TestClass]
public class MoveFenceTests : StarDriveTest
{
    static readonly Vector2 Click = new(300_000, 300_000);
    static readonly Vector2 Facing = Vectors.Up;
    readonly Ship[] Enemies;

    public MoveFenceTests()
    {
        CreateUniverseAndPlayerEmpire();
        UState.Events.Disabled = true;
        Enemies = new Ship[] { SpawnShipNoCombatHoldPos("Vulcan Scout", Enemy, Click + new Vector2(2000, 0)) };
    }

    // a group whose ships stand on their spots of a move to the click; the first one's spot can be set wide
    ShipGroup GroupOnItsSpots(int count, Vector2? firstOffset = null)
    {
        var ships = new Array<Ship>();
        for (int i = 0; i < count; ++i)
            ships.Add(SpawnShip("Vulcan Scout", Player, Click, Facing));
        var group = new ShipGroup(ships, Click, Click, Facing, Player);
        if (firstOffset is { } offset)
            group.Ships[0].RelativeFleetOffset = offset;
        foreach (Ship ship in group.Ships)
            ship.Position = Click + group.GetPositionFromDirection(ship, Facing);
        return group;
    }

    [TestMethod]
    public void AMoveNoShipWarpsForIsFollowedNextToAnEnemy()
    {
        ShipGroup group = GroupOnItsSpots(4);

        Vector2 moveTo = HelperFunctions.GetCorrectedMovePosWithAudio(group, Enemies, Click, Facing);

        Assert.AreEqual(Click, moveTo, "every ship flies to its spot at sublight, so the move stands where it was given");
    }

    [TestMethod]
    public void AWideFormationAlreadyOnItsSpotsIsFollowedNextToAnEnemy()
    {
        ShipGroup group = GroupOnItsSpots(4, firstOffset: new Vector2(10_000, 0));
        AssertGreaterThan(group.Ships[0].Position.Distance(Click), 7500f, "setup: one ship stands on a spot far from the click");

        Vector2 moveTo = HelperFunctions.GetCorrectedMovePosWithAudio(group, Enemies, Click, Facing);

        Assert.AreEqual(Click, moveTo, "each ship is measured from its own spot, so none warps and the move stands");
    }

    [TestMethod]
    public void AMoveAShipWarpsForIsKeptAwayFromAnEnemy()
    {
        ShipGroup group = GroupOnItsSpots(4);
        group.Ships[0].Position += new Vector2(0, 20_000);

        Vector2 moveTo = HelperFunctions.GetCorrectedMovePosWithAudio(group, Enemies, Click, Facing);

        AssertEqual(1f, 5000f, moveTo.Distance(Enemies[0].Position), "one ship warps to its spot, so the move is pulled back to the fence");
    }

    [TestMethod]
    public void AMoveAShipInWarpTakesIsKeptAwayFromAnEnemy()
    {
        ShipGroup group = GroupOnItsSpots(4);
        group.Ships[0].engineState = Ship.MoveState.Warp;

        Vector2 moveTo = HelperFunctions.GetCorrectedMovePosWithAudio(group, Enemies, Click, Facing);

        AssertEqual(1f, 5000f, moveTo.Distance(Enemies[0].Position), "a ship still at warp counts as warping there");
    }

    [TestMethod]
    public void AMoveASpoolingShipTakesIsKeptAwayFromAnEnemy()
    {
        ShipGroup group = GroupOnItsSpots(4);
        group.Ships[0].EngageStarDrive();
        Assert.IsTrue(group.Ships[0].IsSpooling, "setup: one ship spools for an earlier jump");

        Vector2 moveTo = HelperFunctions.GetCorrectedMovePosWithAudio(group, Enemies, Click, Facing);

        AssertEqual(1f, 5000f, moveTo.Distance(Enemies[0].Position), "a spooling ship is about to warp, so it counts as warping there");
    }

    [TestMethod]
    public void ASingleShipNearItsDestinationMovesNextToAnEnemy()
    {
        Ship near = SpawnShip("Vulcan Scout", Player, Click + new Vector2(0, 3000), Facing);
        Ship far = SpawnShip("Vulcan Scout", Player, Click + new Vector2(0, 20_000), Facing);

        Assert.AreEqual(Click, HelperFunctions.GetCorrectedMovePosWithAudio(near, Enemies, Click), "a short sublight move stands as given");
        AssertEqual(1f, 5000f, HelperFunctions.GetCorrectedMovePosWithAudio(far, Enemies, Click).Distance(Enemies[0].Position),
                    "a warp jump is pulled back to the fence");
    }
}
