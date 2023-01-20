using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Planets
{
    [TestClass]
    public class CapitalTransfer : StarDriveTest
    {
        readonly Planet Homeworld;
        readonly Planet NewPlanet;
        readonly Planet EnemyHome;

        public CapitalTransfer()
        {
            CreateUniverseAndPlayerEmpire();
            Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
            NewPlanet = AddDummyPlanet(new Vector2(1000), 2, 1, 3);
            Homeworld = AddHomeWorldToEmpire(new Vector2(2000), Player);
            EnemyHome = AddHomeWorldToEmpire(new Vector2(3000), Enemy);

            PlanetType type = ResourceManager.Planets.RandomPlanet(PlanetCategory.Terran);
            var random = new SeededRandom();
            NewPlanet.GenerateNewFromPlanetType(random, type, scale:1.5f, preDefinedPop:16);
        }

        [TestMethod]
        public void InitialHomeworld()
        {
            Assert.IsTrue(Homeworld.HasCapital, "New Homeworld does not have a capital");
            Assert.IsTrue(Player.Capital == Homeworld, "The Player's capital is the created homeworld");
            Assert.IsTrue(Homeworld.HasSpacePort, "New Homeworld does not have a spaceport");
            Assert.IsTrue(EnemyHome.HasCapital, "Enemy Homeworld does not have a capital");
        }

        [TestMethod]
        public void TransferCapital()
        {
            Ship colonyShip = SpawnShip("Colony Ship", Enemy, Vector2.Zero);
            NewPlanet.Colonize(colonyShip);
            Assert.IsTrue(NewPlanet.Owner == Enemy, "New Planet after colonization is not owned by the enemy");

            Assert.IsTrue(NewPlanet.HasOutpost, "New Planet does not contain an Outpost");
            Assert.IsFalse(NewPlanet.HasCapital, "New Planet has a capital but should not");

            var capital = EnemyHome.FindBuilding(b => b.IsCapital);
            EnemyHome.ScrapBuilding(capital);
            Assert.IsFalse(EnemyHome.HasCapital, "Enemy Homeworld capital should have been scrapped");

            EnemyHome.ChangeOwnerByInvasion(Player, NewPlanet.Level);
            Enemy.TestAssignNewHomeWorldIfNeeded();
            Assert.IsTrue(NewPlanet.TestIsCapitalInQueue(), "New Planet Should have a capital in queue");

            Enemy.AddMoney(1000);
            UState.Debug = true; // to get the debug rush
            NewPlanet.Construction.RushProduction(0, 1000, rushButton: true);
            Assert.IsTrue(NewPlanet.IsHomeworld, "New planet should be a homeworld");
            Assert.IsTrue(NewPlanet.HasCapital, "New planet should have a capital, after rushing");

            // Perform DoGoverning to and expect to add outpost there
            EnemyHome.DoGoverning();
            Assert.IsTrue(EnemyHome.TestIsOutpostInQueue(), "Enemy home should have an outpost in queue after being taken by the player and DoGoverning");

            Enemy.AddMoney(1000);
            EnemyHome.Construction.RushProduction(0, 1000, rushButton: true);
            Assert.IsTrue(EnemyHome.HasOutpost, "Enemy home should have an outpost built");

            // Enemy retakes the planet
            EnemyHome.ChangeOwnerByInvasion(Enemy, NewPlanet.Level);
            Assert.IsFalse(NewPlanet.IsHomeworld, "New planet should not be a homeworld anymore");
            Assert.IsFalse(NewPlanet.HasCapital, "New planet should scrap the capital as part of transfer");
            Assert.IsTrue(EnemyHome.IsHomeworld, "Enemy original home planet should now be a homeworld");
            Assert.IsTrue(EnemyHome.TestIsCapitalInQueue(), "Enemy original home planet should now be building a capital");

            // place the capital and expect the outpost to be gone
            Enemy.AddMoney(1000);
            EnemyHome.Construction.RushProduction(0, 1000, rushButton: true);
            Assert.IsTrue(EnemyHome.HasCapital, "Enemy original home planet should now be building a capital");
            Assert.IsFalse(EnemyHome.HasOutpost, "built capital should remove the outpost as part of the Capital placement process");
        }
    }
}