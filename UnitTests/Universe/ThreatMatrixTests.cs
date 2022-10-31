using System;
using Ship_Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Ships;
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
        CreateThirdMajorEmpire();

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
            owner.EmpireShips.Add(s);
        }
        owner.EmpireShips.UpdatePublicLists();
        return spawnedStrength;
    }

    [TestMethod]
    public void FindClusters_OfASingleEmpire()
    {
        float str1 = CreateShipsAt(PlayerPlanet.Position, 5000f, Player, 40);
        float str2 = CreateShipsAt(PlayerPlanet.Position, 5000f, Enemy, 40);
        throw new NotImplementedException();
    }

    [TestMethod]
    public void FindHostileClusters()
    {
        float str1 = CreateShipsAt(PlayerPlanet.Position, 5000f, Player, 40);
        float str2 = CreateShipsAt(PlayerPlanet.Position, 5000f, Enemy, 40);

        throw new NotImplementedException();
    }

    [TestMethod]
    public void FindHostileClustersByDist()
    {
        float str1 = CreateShipsAt(PlayerPlanet.Position, 5000f, Player, 40);
        float str2 = CreateShipsAt(PlayerPlanet.Position, 5000f, Enemy, 40);

        
        throw new NotImplementedException();
    }

    [TestMethod]
    public void GetStrengthAt_OfPlayer()
    {
        throw new NotImplementedException();
    }

    [TestMethod]
    public void GetStrengthAt_OfRival()
    {
        
        throw new NotImplementedException();
    }

    [TestMethod]
    public void GetHostileStrengthAt_OfSpecific()
    {
        
        throw new NotImplementedException();
    }
    
    [TestMethod]
    public void GetHostileStrengthAt_OfAnyHostile()
    {
        
        throw new NotImplementedException();
    }
    
    [TestMethod]
    public void GetStrongestHostileAt_System()
    {
        
        throw new NotImplementedException();
    }
    
    [TestMethod]
    public void GetStrongestHostileAt_Location()
    {
        
        throw new NotImplementedException();
    }
    
    [TestMethod]
    public void GetAllFactionBases()
    {
        
        throw new NotImplementedException();
    }
    
    [TestMethod]
    public void GetAllSystemsWithFactions()
    {
        
        throw new NotImplementedException();
    }

    [TestMethod]
    public void KnownEmpireStrength()
    {
        
        throw new NotImplementedException();
    }

    [TestMethod]
    public void KnownEmpireStrengthInBorders()
    {
        
        throw new NotImplementedException();
    }

    [TestMethod]
    public void GetTechsFromPins()
    {
        
        throw new NotImplementedException();
    }
    
    [TestMethod]
    public void EmpireCanTrackItsOwnStrength()
    {
        Vector2 pos1 = PlayerPlanet.Position;
        Vector2 pos2 = pos1 + new Vector2(50_000);
        float str1 = CreateShipsAt(pos1, 5000f, Player, numShips:40);
        float str2 = CreateShipsAt(pos2, 5000f, Player, numShips:40);
        float str = str1 + str2;

        Ship[] allShips = Player.EmpireShips.OwnedShips;
        float allStrength = allShips.Sum(s => s.GetStrength());
        Assert.AreEqual(str, allStrength, "Total ships strength must equal expected spawned strength");

        Player.AI.ThreatMatrix.Update();

        float strAt1 = Player.AI.ThreatMatrix.GetStrengthAt(Player, pos1, 5000f);
        Assert.AreEqual(str1, strAt1, $"GetStrengthAt={strAt1} must equal str1={str1}");

        float strAt2 = Player.AI.ThreatMatrix.GetStrengthAt(Player, pos2, 5000f);
        Assert.AreEqual(str2, strAt2, $"GetStrengthAt={strAt2} must equal str2={str2}");

        float knownStr = Player.AI.ThreatMatrix.KnownEmpireStrength(Player);
        Assert.AreEqual(str, knownStr, $"KnownEmpireStrength(Player)={knownStr} must equal spawnedStr={str}");

        Assert.AreEqual(2, Player.AI.ThreatMatrix.AllClusters.Length, "There should be only 2 clusters");
    }

}
