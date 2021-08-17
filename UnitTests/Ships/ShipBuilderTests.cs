using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class ShipBuilderTests : StarDriveTest
    {
        readonly string DefaultDroneName = GlobalStats.DefaultEventDrone;

        public ShipBuilderTests()
        {
            LoadStarterShips(DefaultDroneName);
            CreateUniverseAndPlayerEmpire();
        }

        [TestMethod]
        public void VerifyDefaultDrone()
        {
            Assert.IsTrue(DefaultDroneName.NotEmpty(), "DefaultEventDrone Must contain a ship name");

            Ship drone = ShipBuilder.PickCostEffectiveShipToBuild(RoleName.drone, Player, 1000, 1000);
            Assert.IsTrue(drone != null, "Drone Ship picked by Shipbuilder is null!");
            Assert.IsTrue(drone.Name == DefaultDroneName, $"Drone Ship Name is not {DefaultDroneName}");
        }
    }
}
