using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestSupplyShuttles : StarDriveTest
    {
        public TestSupplyShuttles()
        {
            // Excalibur class has all the bells and whistles
            LoadStarterShips("TEST_Excalibur-Class Supercarrier",
                             "Corsair");
            CreateUniverseAndPlayerEmpire();
        }

        Ship CreateShip(Empire empire, string shipName, Vector2 pos)
        {
            return SpawnShip(shipName, empire, pos);
        }

        void UpdateStatus(Ship ship, CombatState state)
        {
            ship.AI.CombatState = state;
            ship.ShipStatusChanged = true;
            ship.AI.ScanForTargets(new FixedSimTime(1f));
            ship.Update(new FixedSimTime(1f));
        }

        [TestMethod]
        public void TestSupplyShuttle()
        {
            Ship ship = CreateShip(Player, "TEST_Excalibur-Class Supercarrier", Vector2.Zero);
            Ship target = CreateShip(Player, "Corsair", new Vector2(1000, 1000));
            RunObjectsSim(TestSimStep);

            target.ChangeOrdnance(-target.OrdinanceMax * 0.5f);
            UpdateStatus(ship, CombatState.Artillery);
            ship.UpdateResupply();
            Assert.IsTrue(ship.Carrier.HasSupplyShuttlesInSpace, "Supply Shuttle not found in sensors.");
        }
    }
}
