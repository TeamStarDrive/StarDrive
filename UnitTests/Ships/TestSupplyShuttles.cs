using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;

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

        static Ship CreateShip(Empire empire, string shipName, Vector2 pos)
        {
            var ship = Ship.CreateShipAtPoint(shipName, empire, pos);
            ship.SetSystem(null);
            return ship;
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
            Universe.Objects.Update(TestSimStep);

            target.ChangeOrdnance(-target.OrdinanceMax * 0.5f);
            UpdateStatus(ship, CombatState.Artillery);
            ship.UpdateResupply();
            Assert.IsTrue(ship.Carrier.HasSupplyShuttlesInSpace, "Supply Shuttle not found in sensors.");
        }

        [TestMethod]
        public void TestSelfSupplyShuttle()
        {
            Ship ship = CreateShip(Player, "TEST_Excalibur-Class Supercarrier", Vector2.Zero);
            Universe.Objects.Update(TestSimStep);

            ship.ChangeOrdnance(-(ship.OrdinanceMax -50));
            UpdateStatus(ship, CombatState.Artillery);
            ship.UpdateResupply();
            Assert.IsTrue(ship.Carrier.HasSupplyShuttlesInSpace);
        }
    }
}
