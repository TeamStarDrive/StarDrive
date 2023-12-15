using System.Collections.Generic;
using Ship_Game;
using Ship_Game.Ships;
using UnitTests.Ships;
using Ship_Game.Utils;
using Ship_Game.AI;
using Ship_Game.GameScreens.NewGame;
using Ship_Game.Universe;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Spatial;
using Vector2 = SDGraphics.Vector2;
using UnitTests.Serialization;
using Ship_Game.Gameplay;
#pragma warning disable CA2213

namespace UnitTests.Universe;

[TestClass]
public class ThreatMatrixTests : StarDriveTest
{
    protected bool EnableVisualization = false;
    Planet PlayerPlanet;
    Planet EnemyPlanet;

    const string SCOUT_NAME = "TEST_Vulcan Scout";
    const string ASTEROID_BASE = "Corsair Asteroid Base";

    public ThreatMatrixTests()
    {
        LoadStarterShips(ASTEROID_BASE, SCOUT_NAME);
        CreateUniverseAndPlayerEmpire();
        CreateThirdMajorEmpire();

        // set up two solar systems
        PlayerPlanet = AddDummyPlanetToEmpire(new(200_000, 200_000), Player);
        EnemyPlanet = AddDummyPlanetToEmpire(new(-200_000, -200_000), Enemy);
        //ThirdMajor.SignTreatyWith(Enemy, TreatyType.NonAggression);
        //ThirdMajor.SignTreatyWith(Player, TreatyType.NonAggression);

        ThirdMajor.GetRelations(Enemy, out Relationship thirdToEnemy);
        Enemy.GetRelations(ThirdMajor, out Relationship enemyToThird);
        ThirdMajor.GetRelations(Player, out Relationship thirdToPlayer);
        Player.GetRelations(ThirdMajor, out Relationship playerToThird);
        ThirdMajor.SetRelationsAsKnown(thirdToEnemy, Enemy);
        ThirdMajor.SetRelationsAsKnown(thirdToPlayer, Player);
        Player.SetRelationsAsKnown(playerToThird, ThirdMajor);
        Enemy.SetRelationsAsKnown(enemyToThird, ThirdMajor);

        Enemy.UpdateRelationships(false); 
        Player.UpdateRelationships(false);
        ThirdMajor.UpdateRelationships(false);
    }
    
    protected void DebugVisualizeThreats(Empire owner)
    {
        var vis = new ThreatMatrixVisualization(this, owner);
        vis.OnInsert = (GameObject o) =>
        {
            ScanAndUpdateThreats(owner);
        };
        vis.OnRemove = (SpatialObjectBase[] objects) =>
        {
            foreach (SpatialObjectBase o in objects)
                if (o is ThreatCluster c)
                    foreach (Ship s in c.Ships)
                        s.InstantKill();
            ScanAndUpdateThreats(owner);
        };

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
            TestShip s = SpawnShipNoCombatHoldPos(SCOUT_NAME, owner, pos+random.Vector2D(radius));
            spawnedStrength += s.GetStrength();
        }
        return spawnedStrength;
    }

    static float Str(ThreatCluster[] clusters) => clusters.Sum(c => c.Strength);

    void ScanAndUpdateThreats(params Empire[] owners)
    {
        UState.Objects.Update(new(time:2.0f));
        foreach (Empire owner in owners)
            owner.Threats.Update(new(time:2.0f));
    }

    [TestMethod]
    public void FindClusters_OfASingleEmpire()
    {
        Vector2 pos = PlayerPlanet.Position;
        float str1 = CreateShipsAt(pos, 4000, Player, 40);
        float str2 = CreateShipsAt(pos, 4000, Enemy, 20);
        float str3 = CreateShipsAt(pos, 4000, ThirdMajor, 10);
        ScanAndUpdateThreats(Player);

        AssertEqual(str1, Str(Player.Threats.FindClusters(Player, pos, 6000)));
        AssertEqual(str2, Str(Player.Threats.FindClusters(Enemy, pos, 6000)));
        AssertEqual(str3, Str(Player.Threats.FindClusters(ThirdMajor, pos, 6000)));

        AssertEqual(1, Player.Threats.FindClusters(Player, pos, 6000).Length);
        AssertEqual(1, Player.Threats.FindClusters(Enemy, pos, 6000).Length);
        AssertEqual(1, Player.Threats.FindClusters(ThirdMajor, pos, 6000).Length);

        // make sure we can find multiple clusters
        float str4 = CreateShipsAt(pos + new Vector2(20000), 4000, Enemy, 15);
        ScanAndUpdateThreats(Player);

        //DebugVisualizeThreats(Player);

        AssertEqual(str2+str4, Str(Player.Threats.FindClusters(Enemy, pos, 40000)));
        AssertEqual(2, Player.Threats.FindClusters(Enemy, pos, 40000).Length);
    }

    [TestMethod]
    public void FindClusters_RememberEvenAfterVisionLost()
    {
        Vector2 pos1 = PlayerPlanet.Position;
        Vector2 pos2 = EnemyPlanet.Position;
        float str1 = CreateShipsAt(pos1, 5000, Player, 40);
        TestShip scout = SpawnShip(SCOUT_NAME, Player, pos2);
        float str2 = CreateShipsAt(pos2, 5000, Enemy, 40);
        ScanAndUpdateThreats(Player, Enemy);

        // GetStrengthAt is actually an alias for FindClusters
        AssertEqual(str2, Player.Threats.GetStrengthAt(Enemy, pos2, 5000));
        AssertEqual(scout.GetStrength(), Enemy.Threats.GetStrengthAt(Player, pos2, 5000));

        // now lose vision
        scout.InstantKill();
        ScanAndUpdateThreats(Player, Enemy);

        // we should still remember enemy stuff
        AssertEqual(str2, Player.Threats.GetStrengthAt(Enemy, pos2, 5000),
            "Should remember clusters missing from vision");

        // enemy should know that we just lost the scout because they saw it
        AssertEqual(0, Enemy.Threats.GetStrengthAt(Player, pos2, 5000),
            "Enemy should not remember our scout because they saw it die");
    }
    
    // forget about ships that we saw die
    [TestMethod]
    public void FindClusters_ForgetKilledShip()
    {
        Vector2 pos = PlayerPlanet.Position;
        TestShip playerShip = SpawnShip(SCOUT_NAME, Player, pos+new Vector2(500));
        TestShip enemyShip = SpawnShip(SCOUT_NAME, Enemy, pos-new Vector2(500));
        ScanAndUpdateThreats(Player);

        AssertEqual(1, Player.Threats.FindClusters(Enemy, pos, 5000).Length);
        AssertEqual(enemyShip.GetStrength(), Player.Threats.GetStrengthAt(Enemy, pos, 5000));

        enemyShip.InstantKill();
        ScanAndUpdateThreats(Player);
        ThreatCluster[] clusters = Player.Threats.FindClusters(Enemy, pos, 5000);
        AssertEqual(0, Str(clusters));
        AssertEqual(0, clusters.Length);

        // and also, WE must forget about OUR clusters if our ship dies!
        AssertEqual(1, Player.Threats.OurClusters.Length);
        AssertEqual(1, Player.Threats.FindClusters(Player, pos, 5000).Length);
        
        playerShip.InstantKill();
        ScanAndUpdateThreats(Player);

        AssertEqual(0, Player.Threats.OurClusters.Length);
        AssertEqual(0, Player.Threats.FindClusters(Player, pos, 5000).Length);
    }

    // regression test: forget empty clusters
    [TestMethod]
    public void FindClusters_ForgetEmptyClusters()
    {
        Vector2 pos = PlayerPlanet.Position;
        TestShip playerScoutShip = SpawnShip(SCOUT_NAME, Player, pos+new Vector2(500));

        // run the update several times, and spawn the ship in a new location every time
        var random = new SeededRandom(1337);
        for (int i = 0; i < 10; ++i)
        {
            TestShip enemyShip = SpawnShip(SCOUT_NAME, Enemy, pos + random.Vector2D(8_000));
            ScanAndUpdateThreats(Player);
            
            AssertEqual(1, Player.Threats.OurClusters.Length, "OurClusters.Length");
            AssertEqual(1, Player.Threats.RivalClusters.Length, "RivalClusters.Length");
            AssertEqual(1, Player.Threats.FindClusters(Enemy, pos, 10_000).Length);

            enemyShip.InstantKill();
            ScanAndUpdateThreats(Player);

            AssertEqual(1, Player.Threats.OurClusters.Length, "OurClusters.Length");
            AssertEqual(0, Player.Threats.RivalClusters.Length, "RivalClusters.Length");
            AssertEqual(0, Player.Threats.FindClusters(Enemy, pos, 10_000).Length);
        }
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

        AssertEqual(str2, Str(Player.Threats.FindHostileClusters(pos, 5000)));
        AssertEqual(1, Player.Threats.FindHostileClusters(pos, 5000).Length);

        AssertEqual(str3, Str(Player.Threats.FindHostileClusters(pos2, 5000)));
        AssertEqual(1, Player.Threats.FindHostileClusters(pos2, 5000).Length);

        AssertEqual(str2+str3, Str(Player.Threats.FindHostileClusters(pos, 30000)));
        AssertEqual(2, Player.Threats.FindHostileClusters(pos, 30000).Length);

        // and make sure enemy sees us as well:
        AssertEqual(str1, Str(Enemy.Threats.FindHostileClusters(pos, 5000)));
        AssertEqual(1, Enemy.Threats.FindHostileClusters(pos, 5000).Length);
    }

    [TestMethod]
    public void FindHostileClustersByDist()
    {
        Vector2 pos = PlayerPlanet.Position;
        Vector2 pos2 = pos + new Vector2(20000);
        Vector2 pos3 = pos + new Vector2(40000);
        CreateShipsAt(pos, 40000, Player, 40); // need to create some ships to act as scanners
        float str1 = CreateShipsAt(pos, 5000, Enemy, 30);
        float str2 = CreateShipsAt(pos2, 5000, Enemy, 20);
        float str3 = CreateShipsAt(pos3, 5000, Enemy, 10);
        ScanAndUpdateThreats(Player);

        if (EnableVisualization)
            DebugVisualizeThreats(Player);

        ThreatCluster[] clusters = Player.Threats.FindHostileClustersByDist(pos, 55000);
        AssertEqual((int)(str1+str2+str3), (int)Str(clusters));
        AssertEqual(3, clusters.Length);
    }

    [TestMethod]
    public void GetStrengthAt_OfSelf()
    {
        Vector2 pos = PlayerPlanet.Position;
        float str1 = CreateShipsAt(pos, 5000, Player, 40);
        float str2 = CreateShipsAt(pos, 5000, Enemy, 20);
        ScanAndUpdateThreats(Player, Enemy);

        AssertEqual(str1, Player.Threats.GetStrengthAt(Player, pos, 5000));
        AssertEqual(str2, Enemy.Threats.GetStrengthAt(Enemy, pos, 5000));
    }

    [TestMethod]
    public void GetStrengthAt_OfRival()
    {
        Vector2 pos = PlayerPlanet.Position;
        float str1 = CreateShipsAt(pos, 5000, Player, 40);
        float str2 = CreateShipsAt(pos, 5000, Enemy, 20);
        ScanAndUpdateThreats(Player, Enemy);

        AssertEqual(str2, Player.Threats.GetStrengthAt(Enemy, pos, 5000));
        AssertEqual(str1, Enemy.Threats.GetStrengthAt(Player, pos, 5000));
    }

    [TestMethod]
    public void GetHostileStrengthAt_OfSpecific()
    {
        Vector2 pos = PlayerPlanet.Position;
        float str1 = CreateShipsAt(pos, 5000, Player, 40);
        float str2 = CreateShipsAt(pos, 6000, Enemy, 20);
        CreateShipsAt(pos, 7000, ThirdMajor, 10);
        ScanAndUpdateThreats(Player, Enemy, ThirdMajor);

        AssertEqual(str2, Player.Threats.GetHostileStrengthAt(Enemy, pos, 5000));
        AssertEqual(str1, Enemy.Threats.GetHostileStrengthAt(Player, pos, 5000));
        // neutrals shouldn't be reported
        AssertEqual(0, Player.Threats.GetHostileStrengthAt(ThirdMajor, pos, 5000),
            "GetHostileStrengthAt(NeutralFaction) should always give 0");
        AssertEqual(0, Enemy.Threats.GetHostileStrengthAt(ThirdMajor, pos, 5000),
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
        AssertEqual(str2, Player.Threats.GetHostileStrengthAt(pos, 5000));
        AssertEqual(str1, Enemy.Threats.GetHostileStrengthAt(pos, 5000));
    }
    
    [TestMethod]
    public void GetStrongestHostileAt_System()
    {
        Vector2 pos = PlayerPlanet.Position;
        CreateShipsAt(pos, 5000, Player, 40);
        CreateShipsAt(pos, 6000, Enemy, 20);
        CreateShipsAt(pos, 7000, ThirdMajor, 10);
        ScanAndUpdateThreats(Player, Enemy, ThirdMajor);

        AssertEqual(Enemy, Player.Threats.GetStrongestHostileAt(PlayerPlanet.System));
        AssertEqual(Player, Enemy.Threats.GetStrongestHostileAt(PlayerPlanet.System));

        // a neutral faction does not see Player or Enemy as a hostile
        AssertEqual(null, ThirdMajor.Threats.GetStrongestHostileAt(PlayerPlanet.System));
    }
    
    [TestMethod]
    public void GetStrongestHostileAt_Location()
    {
        Vector2 pos = PlayerPlanet.Position;
        CreateShipsAt(pos, 5000, Player, 40);
        CreateShipsAt(pos, 6000, Enemy, 20);
        CreateShipsAt(pos, 7000, ThirdMajor, 10);
        ScanAndUpdateThreats(Player, Enemy, ThirdMajor);

        AssertEqual(Enemy, Player.Threats.GetStrongestHostileAt(pos, 5000));
        AssertEqual(Player, Enemy.Threats.GetStrongestHostileAt(pos, 5000));

        // a neutral faction does not see Player or Enemy as a hostile
        AssertEqual(null, ThirdMajor.Threats.GetStrongestHostileAt(pos, 5000));
    }
    
    [TestMethod]
    public void GetAllFactionBases()
    {
        Vector2 pos1 = PlayerPlanet.Position;
        Vector2 pos2 = EnemyPlanet.Position;
        CreateAMinorFaction("Corsair");
        float str1 = SpawnShip(ASTEROID_BASE, Faction, pos1).GetStrength();
        float str2 = SpawnShip(ASTEROID_BASE, Faction, pos2).GetStrength();
        CreateShipsAt(pos1, 5000, Player, 20);
        CreateShipsAt(pos2, 5000, Player, 20);
        ScanAndUpdateThreats(Player);

        ThreatCluster[] baseClusters = Player.Threats.GetAllFactionBases();
        AssertEqual(str1+str2, Str(baseClusters), "Must find Faction Bases");
        AssertEqual(2, baseClusters.Length);
    }
    
    [TestMethod]
    public void GetAllSystemsWithFactions()
    {
        Vector2 pos1 = PlayerPlanet.Position;
        Vector2 pos2 = EnemyPlanet.Position;
        CreateAMinorFaction("Corsair");
        float str1 = SpawnShip(ASTEROID_BASE, Faction, pos1).GetStrength();
        float str2 = SpawnShip(ASTEROID_BASE, Faction, pos2).GetStrength();
        CreateShipsAt(pos1, 5000, Player, 20);
        CreateShipsAt(pos2, 5000, Player, 20);
        ScanAndUpdateThreats(Player);

        var systemsWithFactions = Player.Threats.GetAllSystemsWithFactions();
        AssertEqual(2, systemsWithFactions.Count);
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
        
        static float KnownStr(Empire owner, Empire of) => owner.Threats.KnownEmpireStrength(of);

        AssertEqual(ene1+ene2, KnownStr(Player, Enemy), "Player should know about both Enemy groups thanks to scouts");
        AssertEqual(pla2, KnownStr(Enemy, Player), "Enemy should only know of Player scout group");
    }

    [TestMethod]
    public void KnownEmpireStrength_AfterVisionLost()
    {
        Vector2 pos1 = PlayerPlanet.Position;
        Vector2 pos2 = EnemyPlanet.Position;
        CreateShipsAt(pos1, 5000, Player, 40);
        TestShip scout = SpawnShip(SCOUT_NAME, Player, pos2);
        float ene1 = CreateShipsAt(pos2, 5000, Enemy, 20);
        float ene2 = CreateShipsAt(pos2+new Vector2(10000), 5000, Enemy, 20);
        ScanAndUpdateThreats(Player, Enemy);
        
        static float KnownStr(Empire owner, Empire of) => owner.Threats.KnownEmpireStrength(of);

        AssertEqual(ene1+ene2, KnownStr(Player, Enemy), "Player should know about both Enemy groups thanks to scouts");
        AssertEqual(scout.GetStrength(), KnownStr(Enemy, Player), "Enemy should only know of Player scout group");

        scout.InstantKill();
        ScanAndUpdateThreats(Player, Enemy);

        AssertEqual(ene1+ene2, KnownStr(Player, Enemy), "player should still remember about enemy groups");
        AssertEqual(0, KnownStr(Enemy, Player), "enemy should not know anything about Player's strength since they saw the ship die");
    }

    [TestMethod]
    public void KnownEmpireStrengthInBorders()
    {
        Vector2 pos1 = PlayerPlanet.Position;
        Vector2 pos2 = EnemyPlanet.Position;
        float str1 = CreateShipsAt(pos1, 5000, Player, 40);
        TestShip scout = SpawnShip(SCOUT_NAME, Player, pos2);
        float str3 = CreateShipsAt(pos2, 5000, Enemy, 20);
        float str4 = CreateShipsAt(pos2+new Vector2(10000), 5000, Enemy, 20);
        ScanAndUpdateThreats(Player, Enemy);

        static float KnownStr(Empire owner, Empire of) => owner.Threats.KnownEmpireStrengthInBorders(of);

        AssertEqual(0, KnownStr(Player, Enemy));
        AssertEqual(str1, KnownStr(Player, Player), "Player should know about his own ships");
        AssertEqual(str3+str4, KnownStr(Enemy, Enemy), "Enemy should know about his own ships");
        AssertEqual(scout.GetStrength(), KnownStr(Enemy, Player), "Enemy should only know of Player scout group");

        scout.InstantKill();
        ScanAndUpdateThreats(Player, Enemy);

        AssertEqual(0, KnownStr(Player, Enemy));
        AssertEqual(0, KnownStr(Enemy, Player), "enemy should not know anything about Player's strength since they saw the ship die");

        // TODO expand this test
    }

    [TestMethod]
    public void GetTechsFromPins()
    {
        ShipDesignUtils.MarkDesignsUnlockable();

        Vector2 pos1 = PlayerPlanet.Position;
        CreateShipsAt(pos1, 5000, Player, 40);
        CreateShipsAt(pos1, 5000, Enemy, 20);
        ScanAndUpdateThreats(Player, Enemy);

        HashSet<string> techs = new();
        Player.Threats.GetTechsFromPins(techs, Enemy);
        Assert.AreNotEqual(0, techs.Count);
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
        AssertEqual((int)str, (int)allStrength, "Total ships strength must equal expected spawned strength");

        float strAt1 = Player.Threats.GetStrengthAt(Player, pos1, 5000);
        AssertEqual(str1, strAt1, $"GetStrengthAt={strAt1} must equal str1={str1}");

        float strAt2 = Player.Threats.GetStrengthAt(Player, pos2, 5000);
        AssertEqual(str2, strAt2, $"GetStrengthAt={strAt2} must equal str2={str2}");

        float knownStr = Player.Threats.KnownEmpireStrength(Player);
        AssertEqual(str, knownStr, $"KnownEmpireStrength(Player)={knownStr} must equal spawnedStr={str}");

        AssertEqual(2, Player.Threats.OurClusters.Length, "There should be only 2 clusters");
    }

    // just to make sure that the updating logic will
    // not create crazy infinite amount of ClustersMap entries, causing
    // horrible perf
    [TestMethod]
    public void ClustersWillNotGrowInfinitely()
    {
        Vector2 pos = PlayerPlanet.Position;
        CreateShipsAt(pos, 5000, Player, numShips:40);
        CreateShipsAt(pos, 5000, Enemy, numShips:40);
        ScanAndUpdateThreats(Player, Enemy);

        int ours1 = Player.Threats.OurClusters.Length;
        int ours2 = Enemy.Threats.OurClusters.Length;
        int rivals1 = Player.Threats.RivalClusters.Length;
        int rivals2 = Enemy.Threats.RivalClusters.Length;
        int mapSize1 = Player.Threats.ClustersMap.Count;
        int mapSize2 = Enemy.Threats.ClustersMap.Count;

        for (int i = 0; i < 100; ++i)
        {
            ScanAndUpdateThreats(Player, Enemy);
            AssertEqual(ours1, Player.Threats.OurClusters.Length, $"Loop {i} Player OurClusters");
            AssertEqual(ours2, Enemy.Threats.OurClusters.Length, $"Loop {i} Enemy OurClusters");
            AssertEqual(rivals1, Player.Threats.RivalClusters.Length, $"Loop {i} Player RivalClusters");
            AssertEqual(rivals2, Enemy.Threats.RivalClusters.Length, $"Loop {i} Enemy RivalClusters");
            AssertEqual(mapSize1, Player.Threats.ClustersMap.Count, $"Loop {i} Player ClustersMap");
            AssertEqual(mapSize2, Enemy.Threats.ClustersMap.Count, $"Loop {i} Enemy ClustersMap");
        }
    }

    void AreEqual(ThreatCluster expected, ThreatCluster actual)
    {
        AssertEqual(expected.Loyalty?.Name, actual.Loyalty?.Name);
        AssertEqual(expected.System?.Name, actual.System?.Name);
        AssertEqual(expected.Strength, actual.Strength);
        AssertEqual(expected.Ships.Length, actual.Ships.Length);
        AssertEqual(expected.HasStarBases, actual.HasStarBases);
        AssertEqual(expected.InBorders, actual.InBorders);

        AssertEqual(expected.Active, actual.Active);
        AssertEqual(expected.Type, actual.Type);
        AssertEqual(expected.Position, actual.Position);
        AssertEqual(expected.Radius, actual.Radius);
    }

    [TestMethod]
    public void SerializedThreatMatricesAreEqual()
    {
        Vector2 pos = PlayerPlanet.Position;
        CreateShipsAt(pos, 5000, Player, numShips:40);
        CreateShipsAt(pos, 5000, Enemy, numShips:40);
        ScanAndUpdateThreats(Player, Enemy);

        UniverseState us = BinarySerializerTests.SerDes(UState);

        ThreatMatrix ser = UState.Player.Threats;
        ThreatMatrix des = us.Player.Threats;

        AssertEqual(ser.Owner.Name, des.Owner.Name);
        AssertEqual(ser.OurClusters.Length, des.OurClusters.Length, "OurClusters.Length");
        AssertEqual(ser.RivalClusters.Length, des.RivalClusters.Length, "RivalClusters.Length");
        
        for (int i = 0; i < ser.OurClusters.Length; ++i)
            AreEqual(ser.OurClusters[i], des.OurClusters[i]);

        for (int i = 0; i < ser.RivalClusters.Length; ++i)
            AreEqual(ser.RivalClusters[i], des.RivalClusters[i]);

        AssertEqual(ser.ClustersMap.Count, des.ClustersMap.Count, "ClustersMap.Count");
        AssertEqual(ser.ClustersMap.FullSize, des.ClustersMap.FullSize, "ClustersMap.FullSize");
    }
}
