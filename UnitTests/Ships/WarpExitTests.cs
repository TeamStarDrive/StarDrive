using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

[TestClass]
public class WarpExitTests : StarDriveTest
{
    public WarpExitTests()
    {
        CreateUniverseAndPlayerEmpire();
        LoadStarterShips(Player.data.DefaultTroopShip);
        Player.GetRelations(Enemy).TurnsAtWar = 5;
    }

    Planet EnemyHomeworld(float gravityWellRange)
    {
        UState.P.GravityWellRange = gravityWellRange;
        Planet planet = AddHomeWorldToEmpire(new Vector2(400_000), Enemy);
        planet.GravityWellRadius = (float)(gravityWellRange * (1 + Math.Log(planet.Scale) / 1.5)); // as the planet's mesh sets it in a game
        return planet;
    }

    // how far from the planet's center a troop ship sent to land on it leaves warp, the simulation run at the given steps a second
    float WarpExitDistance(Planet target, int stepsPerSecond, out Ship ship)
    {
        ship = SpawnShip(Player.data.DefaultTroopShip, Player, target.Position + new Vector2(-300_000, 0));
        ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(ship);
        Assert.IsTrue(ship.AI.OrderLandAllTroops(target, clearOrders: true), "setup: the troop ship is ordered to land");
        var step = new FixedSimTime(1f / stepsPerSecond);
        bool warped = false;
        for (int i = 0; i < stepsPerSecond * 120; ++i)
        {
            UState.Objects.Update(step);
            if (ship.IsInWarp)
                warped = true;
            else if (warped)
                return ship.Position.Distance(target.Position);
        }
        Assert.Fail("setup: the troop ship never went to warp and back");
        return 0f;
    }

    [TestMethod]
    [DataRow(60)]
    [DataRow(10)]
    public void AShipLeavesWarpAtTheEdgeOfAGravityWellWhateverTheStepSize(int stepsPerSecond)
    {
        Planet target = EnemyHomeworld(8000);
        Assert.IsTrue(target.GravityWellRadius > 8000, "setup: the planet has a gravity well");

        float exit = WarpExitDistance(target, stepsPerSecond, out Ship ship);

        Assert.IsTrue(exit >= target.GravityWellRadius - 10f,
                      $"the ship leaves warp where it enters the well ({target.GravityWellRadius:0}), not a step inside it ({exit:0})");
        Assert.IsTrue(ship.Inhibited && ship.InhibitionSource == Ship.InhibitionType.GravityWell,
                      "stopped on the edge, it is held by the well as one caught inside it is");
    }

    [TestMethod]
    [DataRow(60)]
    [DataRow(10)]
    public void WithNoGravityWellsAShipLeavesWarpWhereItsLandingMeantTo(int stepsPerSecond)
    {
        Planet target = EnemyHomeworld(0);

        float exit = WarpExitDistance(target, stepsPerSecond, out Ship ship);

        Assert.IsTrue(exit >= ship.WarpOutDistance - target.Radius - 10f,
                      $"the ship leaves warp {ship.WarpOutDistance:0} from its landing spot on the planet, not a step closer ({exit:0} from the center)");
    }
}
