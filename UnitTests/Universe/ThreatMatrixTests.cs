using System;
using Ship_Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Ships;
using UnitTests.Ships;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;
using Ship_Game.AI;
using Ship_Game.Spatial;

namespace UnitTests.Universe;

[TestClass]
public class ThreatMatrixTests : StarDriveTest
{
    protected bool EnableVisualization = false;
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
    
    protected void DebugVisualizeThreats(Empire owner)
    {
        var vis = new ThreatMatrixVisualization(owner);
        EnableMockInput(false); // switch from mocked input to real input
        Game.ShowAndRun(screen: vis); // run the sim
        EnableMockInput(true); // restore the mock input
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

    static float Str(ThreatCluster[] clusters) => clusters.Sum(c => c.Strength);

    void ScanAndUpdateThreats(params Empire[] owners)
    {
        UState.Objects.Update(FixedSimTime.One);
        foreach (Empire owner in owners)
            owner.Threats.Update();
    }

    [TestMethod]
    public void FindClusters_OfASingleEmpire()
    {
        Vector2 pos = PlayerPlanet.Position;
        float str1 = CreateShipsAt(pos, 5000, Player, 40);
        float str2 = CreateShipsAt(pos, 5000, Enemy, 20);
        float str3 = CreateShipsAt(pos, 5000, ThirdMajor, 10);
        ScanAndUpdateThreats(Player);

        Assert.AreEqual(str1, Str(Player.Threats.FindClusters(Player, pos, 5000)));
        Assert.AreEqual(str2, Str(Player.Threats.FindClusters(Enemy, pos, 5000)));
        Assert.AreEqual(str3, Str(Player.Threats.FindClusters(ThirdMajor, pos, 5000)));

        Assert.AreEqual(1, Player.Threats.FindClusters(Player, pos, 5000).Length);
        Assert.AreEqual(1, Player.Threats.FindClusters(Enemy, pos, 5000).Length);
        Assert.AreEqual(1, Player.Threats.FindClusters(ThirdMajor, pos, 5000).Length);

        // make sure we can find multiple clusters
        Vector2 pos2 = pos + new Vector2(20000);
        float str4 = CreateShipsAt(pos2, 5000, Enemy, 15);
        ScanAndUpdateThreats(Player);
        Assert.AreEqual(str2+str4, Str(Player.Threats.FindClusters(Enemy, pos, 30000)));
        Assert.AreEqual(2, Player.Threats.FindClusters(Enemy, pos, 30000).Length);
    }

    [TestMethod]
    public void FindHostileClusters()
    {
        Vector2 pos = PlayerPlanet.Position;
        Vector2 pos2 = pos + new Vector2(20000);
        float str1 = CreateShipsAt(pos, 5000, Player, 40);
        float str2 = CreateShipsAt(pos, 5000, Enemy, 20);
        float str3 = CreateShipsAt(pos2, 5000, Enemy, 15);
        ScanAndUpdateThreats(Player, Enemy);

        Assert.AreEqual(str2, Str(Player.Threats.FindHostileClusters(pos, 5000)));
        Assert.AreEqual(1, Player.Threats.FindHostileClusters(pos, 5000).Length);

        Assert.AreEqual(str3, Str(Player.Threats.FindHostileClusters(pos2, 5000)));
        Assert.AreEqual(1, Player.Threats.FindHostileClusters(pos2, 5000).Length);

        Assert.AreEqual(str2+str3, Str(Player.Threats.FindHostileClusters(pos, 30000)));
        Assert.AreEqual(2, Player.Threats.FindHostileClusters(pos, 30000).Length);

        // and make sure enemy sees us as well:
        Assert.AreEqual(str1, Str(Enemy.Threats.FindHostileClusters(pos, 5000)));
        Assert.AreEqual(1, Enemy.Threats.FindHostileClusters(pos, 5000).Length);
    }

    [TestMethod]
    public void FindHostileClustersByDist()
    {
        Vector2 pos = PlayerPlanet.Position;
        Vector2 pos2 = pos + new Vector2(20000);
        Vector2 pos3 = pos + new Vector2(30000);
        CreateShipsAt(pos, 40000, Player, 40); // need to create some ships to act as scanners
        float str1 = CreateShipsAt(pos, 5000, Enemy, 30);
        float str2 = CreateShipsAt(pos2, 5000, Enemy, 20);
        float str3 = CreateShipsAt(pos3, 5000, Enemy, 10);
        ScanAndUpdateThreats(Player);

        if (EnableVisualization)
            DebugVisualizeThreats(Player);

        ThreatCluster[] clusters = Player.Threats.FindHostileClustersByDist(pos, 45000);
        Assert.AreEqual(str1+str2+str3, Str(clusters));
        Assert.AreEqual(3, clusters.Length);
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
        float str1 = CreateShipsAt(pos1, 5000, Player, numShips:40);
        float str2 = CreateShipsAt(pos2, 5000, Player, numShips:40);
        float str = str1 + str2;
        ScanAndUpdateThreats(Player);

        Ship[] allShips = Player.EmpireShips.OwnedShips;
        float allStrength = allShips.Sum(s => s.GetStrength());
        Assert.AreEqual(str, allStrength, "Total ships strength must equal expected spawned strength");

        float strAt1 = Player.AI.ThreatMatrix.GetStrengthAt(Player, pos1, 5000);
        Assert.AreEqual(str1, strAt1, $"GetStrengthAt={strAt1} must equal str1={str1}");

        float strAt2 = Player.AI.ThreatMatrix.GetStrengthAt(Player, pos2, 5000);
        Assert.AreEqual(str2, strAt2, $"GetStrengthAt={strAt2} must equal str2={str2}");

        float knownStr = Player.AI.ThreatMatrix.KnownEmpireStrength(Player);
        Assert.AreEqual(str, knownStr, $"KnownEmpireStrength(Player)={knownStr} must equal spawnedStr={str}");

        Assert.AreEqual(2, Player.AI.ThreatMatrix.AllClusters.Length, "There should be only 2 clusters");
    }

}
