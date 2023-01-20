using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
#pragma warning disable CA2213

namespace UnitTests.Planets;

[TestClass]
public class TestLandOnTile : StarDriveTest
{
    Planet P;
    Troop Enemy1;
    Troop Enemy2;
    Troop Friendly;

    public TestLandOnTile()
    {
        CreateUniverseAndPlayerEmpire();
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        AddDummyPlanetToEmpire(new(2000), Player);
        P = AddHomeWorldToEmpire(new(2000), Player);
        Enemy1   = ResourceManager.CreateTroop("Wyvern", Enemy);
        Enemy2   = ResourceManager.CreateTroop("Wyvern", Enemy);
        Friendly = ResourceManager.CreateTroop("Wyvern", Player);
        Assert.IsNotNull(Enemy1);
        Assert.IsNotNull(Enemy2);
        Assert.IsNotNull(Friendly);
    }

    bool GetTroopTile(Troop troop, out PlanetGridSquare troopTile)
    {
        troopTile = P.TilesList.Find(t => t.LockOnOurTroop(troop.Loyalty, out Troop troopToCheck) 
                                       && troop == troopToCheck);
        return troopTile != null;
    }

    bool GetCapitalTile(out PlanetGridSquare capitalTile)
    {
        capitalTile = P.TilesList.Find(t => t.BuildingOnTile && t.Building.IsCapital);
        return capitalTile != null;
    }

    PlanetGridSquare LandTroop(Troop troop, string name)
    {
        AssertTrue(troop.TryLandTroop(P), $"{name} Land failed");
        AssertTrue(P.Troops.Contains(troop), $"{name} not in Planet Troops list");
        AssertTrue(GetTroopTile(troop, out PlanetGridSquare landedAt), $"No {name} Tile");
        troop.UpdateAttackActions(troop.MaxStoredActions);
        return landedAt;
    }

    [TestMethod]
    public void LandEnemiesAndFriends()
    {
        for (int i = 0; i < 100; ++i)
        {
            Enemy1.Launch(forceLaunch: true);
            Enemy2.Launch(forceLaunch: true);
            Friendly.Launch(forceLaunch: true);

            AssertTrue(GetCapitalTile(out PlanetGridSquare capitalTile), "No capital");

            // land first enemy
            PlanetGridSquare enemy1Tile = LandTroop(Enemy1, "Enemy1");
            // land a second enemy
            PlanetGridSquare enemy2Tile = LandTroop(Enemy2, "Enemy2");
            // Combat land friendly troop, it should be close to the capital
            PlanetGridSquare friendlyTile = LandTroop(Friendly, "Friendly");

            AssertTrue(Enemy1.CanAttack, "Enemy1 cannot attack");
            AssertTrue(Enemy2.CanAttack, "Enemy2 cannot attack");
            AssertTrue(Friendly.CanAttack, "Friendly cannot attack");

            string positions = $"Capital : {capitalTile.X},{capitalTile.Y}\n" +
                               $"Friendly: {friendlyTile.X},{ friendlyTile.Y}\n" +
                               $"Enemy1  : {enemy1Tile.X},{ enemy1Tile.Y}\n" +
                               $"Enemy2  : {enemy2Tile.X},{ enemy2Tile.Y}\n"; 

            // Enemy should land out of capital's reach so it wont get hit on landing
            // and close to enemy1, as reinforcements
            
            AssertFalse(capitalTile.InRangeOf(enemy1Tile, 1), $"Enemy1 Too Close to Capital\n{positions}");
            AssertFalse(capitalTile.InRangeOf(enemy2Tile, 1), $"Enemy2 Too Close to Capital\n{positions}");
            AssertTrue(enemy1Tile.InRangeOf(enemy2Tile, 1), $"Enemy2 Too Far From Enemy1\n{positions}");
            AssertTrue(enemy1Tile != enemy2Tile, $"Enemies are on the same tile!\n{positions}");

            AssertTrue(friendlyTile.InRangeOf(capitalTile, 1), $"Friendly Too Far From Capital\n{positions}");
        }
    }
}
