using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestSupplyShuttles : StarDriveTest, IDisposable
    {
        readonly GameDummy Game;

        public TestSupplyShuttles()
        {
            // Excalibur class has all the bells and whistles
            ResourceManager.LoadStarterShipsForTesting(new[]{ "Excalibur-Class Supercarrier", "Corsair", "Supply Shuttle" });
            // UniverseScreen requires a game instance
            Game = new GameDummy();
            Game.Create();
        }

        public void Dispose()
        {
            Empire.Universe?.ExitScreen();
            Game.Dispose();
        }

        void CreateTestEnv(out Empire empire, out Ship ship, out Ship target)
        {
            var data = new UniverseData();
            empire   = data.CreateEmpire(ResourceManager.MajorRaces[0]);
            Empire.Universe = new UniverseScreen(data, empire);
            ship   = CreateShip(empire, "Excalibur-Class Supercarrier", Vector2.Zero);
            target = CreateShip(empire, "Corsair", new Vector2(1000, 1000));
            UniverseScreen.SpaceManager.Update(1f);
        }

        Ship CreateShip(Empire empire, string shipName, Vector2 pos)
        {
            var ship = Ship.CreateShipAtPoint(shipName, empire, pos);
            ship.SetSystem(null);
            return ship;
        }

        void UpdateStatus(Ship ship, CombatState state)
        {
            ship.AI.CombatState = state;
            ship.shipStatusChanged = true;
            ship.Update(1f);
        }

        [TestMethod]
        public void TestSupplyShuttle()
        {
            CreateTestEnv(out Empire empire, out Ship ship, out Ship target);
            target.ChangeOrdnance(-target.OrdinanceMax * 0.5f);
            UpdateStatus(ship, CombatState.Artillery);
            Assert.IsTrue(ship.Carrier.HasSupplyShuttlesInSpace);
        }

        [TestMethod]
        public void TestSelfSupplyShuttle()
        {
            CreateTestEnv(out Empire empire, out Ship ship, out Ship target);
            ship.ChangeOrdnance(-(ship.OrdinanceMax -50));
            UpdateStatus(ship, CombatState.Artillery);
            Assert.IsTrue(ship.Carrier.HasSupplyShuttlesInSpace);
        }
    }
}
