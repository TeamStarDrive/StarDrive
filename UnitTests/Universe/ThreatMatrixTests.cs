using System;
using Ship_Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Ships;
using UnitTests.Ships;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;
using Ship_Game.AI;
using SgMotion;

namespace UnitTests.Universe;

[TestClass]
public class ThreatMatrixTests : StarDriveTest
{
    protected bool EnableVisualization = false;
    Planet PlayerPlanet;
    Planet EnemyPlanet;

    public ThreatMatrixTests()
    {
        LoadStarterShips("Corsair Asteroid Base");
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
    public void GetStrengthAt_OfSelf()
    {
        Vector2 pos = PlayerPlanet.Position;
        float str1 = CreateShipsAt(pos, 5000, Player, 40);
        float str2 = CreateShipsAt(pos, 5000, Enemy, 20);
        ScanAndUpdateThreats(Player, Enemy);

        Assert.AreEqual(str1, Player.Threats.GetStrengthAt(Player, pos, 5000));
        Assert.AreEqual(str2, Enemy.Threats.GetStrengthAt(Enemy, pos, 5000));
    }

    [TestMethod]
    public void GetStrengthAt_OfRival()
    {
        Vector2 pos = PlayerPlanet.Position;
        float str1 = CreateShipsAt(pos, 5000, Player, 40);
        float str2 = CreateShipsAt(pos, 5000, Enemy, 20);
        ScanAndUpdateThreats(Player, Enemy);

        Assert.AreEqual(str2, Player.Threats.GetStrengthAt(Enemy, pos, 5000));
        Assert.AreEqual(str1, Enemy.Threats.GetStrengthAt(Player, pos, 5000));
    }

    [TestMethod]
    public void GetHostileStrengthAt_OfSpecific()
    {
        Vector2 pos = PlayerPlanet.Position;
        float str1 = CreateShipsAt(pos, 5000, Player, 40);
        float str2 = CreateShipsAt(pos, 6000, Enemy, 20);
        CreateShipsAt(pos, 7000, ThirdMajor, 10);
        ScanAndUpdateThreats(Player, Enemy, ThirdMajor);

        Assert.AreEqual(str2, Player.Threats.GetHostileStrengthAt(Enemy, pos, 5000));
        Assert.AreEqual(str1, Enemy.Threats.GetHostileStrengthAt(Player, pos, 5000));

        // neutrals shouldn't be reported
        Assert.AreEqual(0, Player.Threats.GetHostileStrengthAt(ThirdMajor, pos, 5000),
            "GetHostileStrengthAt(NeutralFaction) should always give 0");
        Assert.AreEqual(0, Enemy.Threats.GetHostileStrengthAt(ThirdMajor, pos, 5000),
            "GetHostileStrengthAt(NeutralFaction) should always give 0");
    }
    
    [TestMethod]
    public void GetHostileStrengthAt_OfAnyHostile()
    {
        Vector2 pos = PlayerPlanet.Position;
        float str1 = CreateShipsAt(pos, 5000, Player, 40);
        float str2 = CreateShipsAt(pos, 6000, Enemy, 20);
        CreateShipsAt(pos, 7000, ThirdMajor, 10);
        ScanAndUpdateThreats(Player, Enemy, ThirdMajor);

        Assert.AreEqual(str2, Player.Threats.GetHostileStrengthAt(pos, 5000));
        Assert.AreEqual(str1, Enemy.Threats.GetHostileStrengthAt(pos, 5000));
    }
    
    [TestMethod]
    public void GetStrongestHostileAt_System()
    {
        Vector2 pos = PlayerPlanet.Position;
        CreateShipsAt(pos, 5000, Player, 40);
        CreateShipsAt(pos, 6000, Enemy, 20);
        CreateShipsAt(pos, 7000, ThirdMajor, 10);
        ScanAndUpdateThreats(Player, Enemy, ThirdMajor);

        Assert.AreEqual(Enemy, Player.Threats.GetStrongestHostileAt(PlayerPlanet.ParentSystem));
        Assert.AreEqual(Player, Enemy.Threats.GetStrongestHostileAt(PlayerPlanet.ParentSystem));

        // a neutral faction does not see Player or Enemy as a hostile
        Assert.AreEqual(null, ThirdMajor.Threats.GetStrongestHostileAt(PlayerPlanet.ParentSystem));
    }
    
    [TestMethod]
    public void GetStrongestHostileAt_Location()
    {
        Vector2 pos = PlayerPlanet.Position;
        CreateShipsAt(pos, 5000, Player, 40);
        CreateShipsAt(pos, 6000, Enemy, 20);
        CreateShipsAt(pos, 7000, ThirdMajor, 10);
        ScanAndUpdateThreats(Player, Enemy, ThirdMajor);

        Assert.AreEqual(Enemy, Player.Threats.GetStrongestHostileAt(pos, 5000));
        Assert.AreEqual(Player, Enemy.Threats.GetStrongestHostileAt(pos, 5000));

        // a neutral faction does not see Player or Enemy as a hostile
        Assert.AreEqual(null, ThirdMajor.Threats.GetStrongestHostileAt(pos, 5000));
    }
    
    [TestMethod]
    public void GetAllFactionBases()
    {
        Vector2 pos1 = PlayerPlanet.Position;
        Vector2 pos2 = EnemyPlanet.Position;
        CreateAMinorFaction("Corsair");
        float str1 = SpawnShip("Corsair Asteroid Base", Faction, pos1).GetStrength();
        float str2 = SpawnShip("Corsair Asteroid Base", Faction, pos2).GetStrength();
        CreateShipsAt(pos1, 5000, Player, 20);
        CreateShipsAt(pos2, 5000, Player, 20);
        ScanAndUpdateThreats(Player);

        ThreatCluster[] baseClusters = Player.Threats.GetAllFactionBases();
        Assert.AreEqual(str1+str2, Str(baseClusters), "Must find Faction Bases");
        Assert.AreEqual(2, baseClusters.Length);
    }
    
    [TestMethod]
    public void GetAllSystemsWithFactions()
    {
        Vector2 pos1 = PlayerPlanet.Position;
        Vector2 pos2 = EnemyPlanet.Position;
        CreateAMinorFaction("Corsair");
        float str1 = SpawnShip("Corsair Asteroid Base", Faction, pos1).GetStrength();
        float str2 = SpawnShip("Corsair Asteroid Base", Faction, pos2).GetStrength();
        CreateShipsAt(pos1, 5000, Player, 20);
        CreateShipsAt(pos2, 5000, Player, 20);
        ScanAndUpdateThreats(Player);

        SolarSystem[] systemsWithFactions = Player.Threats.GetAllSystemsWithFactions();
        Assert.AreEqual(2, systemsWithFactions.Length);
    }

    [TestMethod]
    public void KnownEmpireStrength()
    {
        Vector2 pos1 = PlayerPlanet.Position;
        Vector2 pos2 = EnemyPlanet.Position;
        float pla1 = CreateShipsAt(pos1, 5000, Player, 40);
        float pla2 = CreateShipsAt(pos2, 5000, Player, 5);
        float ene1 = CreateShipsAt(pos2, 5000, Enemy, 20);
        float ene2 = CreateShipsAt(pos2+new Vector2(15000), 5000, Enemy, 20);
        ScanAndUpdateThreats(Player, Enemy);

        Assert.AreEqual(ene1+ene2, Player.Threats.KnownEmpireStrength(Enemy),
            "Player should know about both Enemy groups thanks to scouts");
        Assert.AreEqual(pla2, Enemy.Threats.KnownEmpireStrength(Player),
            "Enemy should only know of Player scout group");
    }

    
    [TestMethod]
    public void KnownEmpireStrength_AfterVisionLost()
    {
        Vector2 pos1 = PlayerPlanet.Position;
        Vector2 pos2 = EnemyPlanet.Position;
        float pla1 = CreateShipsAt(pos1, 5000, Player, 40);
        TestShip scout = SpawnShip("TEST_Vulcan Scout", Player, pos2);
        float ene1 = CreateShipsAt(pos2, 5000, Enemy, 20);
        float ene2 = CreateShipsAt(pos2+new Vector2(10000), 5000, Enemy, 20);
        ScanAndUpdateThreats(Player, Enemy);

        Assert.AreEqual(ene1+ene2, Player.Threats.KnownEmpireStrength(Enemy),
            "Player should know about both Enemy groups thanks to scouts");
        Assert.AreEqual(scout.GetStrength(), Enemy.Threats.KnownEmpireStrength(Player),
            "Enemy should only know of Player scout group");

        scout.InstantKill();
        ScanAndUpdateThreats(Player, Enemy);

        Assert.AreEqual(ene1+ene2, Player.Threats.KnownEmpireStrength(Enemy),
            "player should still remember about enemy groups");
        Assert.AreEqual(0f, Enemy.Threats.KnownEmpireStrength(Player),
            "enemy should not know anything about Player's strength since they saw the ship die");
    }

    [TestMethod]
    public void KnownEmpireStrengthInBorders()
    {

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
