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
    public class TestSupplyShuttles : StarDriveTest
    {
        public TestSupplyShuttles()
        {
            // Excalibur class has all the bells and whistles
            CreateGameInstance();
            // TODO: we need to fix this mess with Supply Shuttles
            LoadStarterShips(new[]{ "Excalibur-Class Supercarrier", "Corsair",
                                    "Supply_Shuttle", "Supply Shuttle" });
            
        }

        void CreateTestEnv(out Empire empire, out Ship ship, out Ship target)
        {
            CreateUniverseAndPlayerEmpire(out empire);
            ship = CreateShip(empire, "Excalibur-Class Supercarrier", Vector2.Zero);
            target = CreateShip(empire, "Corsair", new Vector2(1000, 1000));

            Universe.Objects.Update(TestSimStep);
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
            ship.shipStatusChanged = true;
            ship.AI.StartSensorScan(TestSimStep);
            
            ship.Update(new FixedSimTime(1f));
        }

        [TestMethod]
        public void TestSupplyShuttle()
        {
            CreateTestEnv(out Empire empire, out Ship ship, out Ship target);
            target.ChangeOrdnance(-target.OrdinanceMax * 0.5f);
            UpdateStatus(ship, CombatState.Artillery);
            ship.UpdateResupply();
            Assert.IsTrue(ship.Carrier.HasSupplyShuttlesInSpace, "Supply Shuttle not found in sensors.");
        }

        [TestMethod]
        public void TestSelfSupplyShuttle()
        {
            CreateTestEnv(out Empire empire, out Ship ship, out Ship target);
            ship.ChangeOrdnance(-(ship.OrdinanceMax -50));
            UpdateStatus(ship, CombatState.Artillery);
            ship.UpdateResupply();
            Assert.IsTrue(ship.Carrier.HasSupplyShuttlesInSpace);
        }
    }
}
