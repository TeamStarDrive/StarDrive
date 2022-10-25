using Ship_Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTests.Ships;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Universe;

[TestClass]
public class ThreatMatrixTests : StarDriveTest
{
    Planet PlayerPlanet;
    Planet EnemyPlanet;

    public ThreatMatrixTests()
    {
        CreateUniverseAndPlayerEmpire();
        // set up two solar systems
        PlayerPlanet = AddDummyPlanetToEmpire(new(200_000, 200_000), Player);
        EnemyPlanet = AddDummyPlanetToEmpire(new(-200_000, -200_000), Enemy);
    }

    float CreateShipsAt(Vector2 pos, float radius, Empire owner, int numShips)
    {
        var random = new SeededRandom(1337);
        float spawnedStrength = 0f;
        for (int i = 0; i < numShips; ++i)
        {
            TestShip s = SpawnShip("TEST_Vulcan Scout", owner, pos+random.Vector2D(radius));
            spawnedStrength += s.GetStrength();
        }
        return spawnedStrength;
    }

    [TestMethod]
    public void EmpireCanTrackItsOwnStrength()
    {
        float spawnedStr = CreateShipsAt(PlayerPlanet.Position, 5000f, Player, 40);

        Player.AI.ThreatMatrix.Update();
    }

}
