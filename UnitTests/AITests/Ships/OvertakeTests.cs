using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships;

[TestClass]
public class OvertakeTests : StarDriveTest
{
    const string Design = "Vulcan Scout";

    public OvertakeTests()
    {
        LoadStarterShips(Design);
        CreateUniverseAndPlayerEmpire();
    }

    [TestMethod]
    public void AnAIShipCuttingOffAFasterTargetAimsWhereTheTargetIsGoing()
    {
        TestShip chaser = SpawnShip(Design, Enemy, new Vector2(100_000f, 100_000f));
        TestShip target = SpawnShip(Design, Player, chaser.Position + new Vector2(8_000f, 0f));
        target.RotationDegrees = 270f;
        target.Velocity = new Vector2(chaser.MaxSTLSpeed + 200f, 0f);
        Assert.IsTrue(target.Direction.X < -0.99f, "setup: the target faces back toward the chaser while it flies away");

        for (int i = 0; i < 1000 && chaser.AI.State != AIState.Pursue; ++i)
            chaser.AI.MoveToEngageTarget(target, TestSimStep);

        Assert.AreEqual(AIState.Pursue, chaser.AI.State, "the chaser sets off to cut the target off");
        Assert.IsTrue(chaser.AI.MovePosition.X > target.Position.X + 5_000f,
                      $"it aims ahead of where the target flies, not where its nose points (aim {chaser.AI.MovePosition.X - target.Position.X:0} from the target)");
    }

    [TestMethod]
    public void TheSpreadAheadOfATargetLiesAheadOfIt()
    {
        var random = new Ship_Game.Utils.SeededRandom(1);
        Vector2 center = new(1_000f, 1_000f);
        for (int i = 0; i < 50; ++i)
        {
            Vector2 p = Ship_Game.ExtensionMethods.NewMathExt.GenerateRandomPointOnForwardHalfArc(center, 0f, 2_000f, random);
            Assert.IsTrue(p.Y <= center.Y + 0.01f, $"rotation 0 faces up, so the forward half lies above the center ({p})");
        }
    }
}
