using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using SDUtils;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships
{
    [TestClass]
    public class ShipBuilderTests : StarDriveTest
    {
        readonly string DefaultDroneName = GlobalStats.Defaults.DefaultEventDrone;

        public ShipBuilderTests()
        {
            LoadStarterShips(DefaultDroneName);
            CreateUniverseAndPlayerEmpire();
        }

        [TestMethod]
        public void VerifyDefaultDrone()
        {
            Assert.IsTrue(DefaultDroneName.NotEmpty(), "DefaultEventDrone Must contain a ship name");

            IShipDesign drone = ShipBuilder.PickCostEffectiveShipToBuild(RoleName.drone, Player, 1000, 1000);
            Assert.IsTrue(drone != null, "Drone Ship picked by Shipbuilder is null!");
            Assert.IsTrue(drone.Name == DefaultDroneName, $"Drone Ship Name is not {DefaultDroneName}");
        }

        static IShipDesign PickColonyShip(int cheapTurns, int midTurns, int bigTurns, out IShipDesign cheap, out IShipDesign mid, out IShipDesign big)
        {
            IShipDesign[] d = ResourceManager.Ships.Designs.Take(3).ToArray();
            (cheap, mid, big) = (d[0], d[1], d[2]);
            var turns = new Dictionary<IShipDesign, int> { [cheap] = cheapTurns, [mid] = midTurns, [big] = bigTurns };
            var score = new Dictionary<IShipDesign, float> { [cheap] = 80, [mid] = 160, [big] = 250 };
            return ShipBuilder.PickBestColonyShip(new Array<IShipDesign> { cheap, mid, big }, s => turns[s], s => score[s],
                                                  ShipBuilder.ColonyShipExtraBuildTurns);
        }

        [TestMethod]
        public void ColonyShipPickSkipsDesignsThatTakeTooLongToBuild()
        {
            IShipDesign picked = PickColonyShip(4, 4 + ShipBuilder.ColonyShipExtraBuildTurns, 30, out _, out IShipDesign mid, out _);
            Assert.AreEqual(mid, picked);
        }

        [TestMethod]
        public void ColonyShipPickTakesTheBestScoreWhenAllBuildQuickly()
        {
            IShipDesign picked = PickColonyShip(4, 6, 4 + ShipBuilder.ColonyShipExtraBuildTurns, out _, out _, out IShipDesign big);
            Assert.AreEqual(big, picked);
        }

        [TestMethod]
        public void ColonyShipPickTakesTheBestScoreWhenNoPortCanBuild()
        {
            IShipDesign picked = PickColonyShip(9999, 9999, 9999, out _, out _, out IShipDesign big);
            Assert.AreEqual(big, picked);
        }

        [TestMethod]
        public void ColonyShipPickReturnsNullWithoutDesigns()
        {
            Assert.IsNull(ShipBuilder.PickBestColonyShip(new Array<IShipDesign>(), s => 0, s => 0, ShipBuilder.ColonyShipExtraBuildTurns));
        }

        [TestMethod]
        public void TurnsToBuildAtTheBestPortGrowWithCost()
        {
            Planet home = AddHomeWorldToEmpire(new Vector2(1000), Player);
            home.HasSpacePort = true;
            Player.UpdateRallyPoints();
            Planet[] ports = Player.BestPortsToBuildShips();
            Assert.AreEqual(1, ports.Length);

            IShipDesign[] byCost = ResourceManager.Ships.Designs.Where(d => d.BaseCost > 0).OrderBy(d => d.BaseCost).ToArray();
            int cheap = Player.TurnsToBuildShipAt(ports, byCost[0]);
            int dear = Player.TurnsToBuildShipAt(ports, byCost[byCost.Length - 1]);
            Assert.IsTrue(cheap < dear && dear < 9999, $"cheap {cheap} turns, dear {dear} turns");
        }

        [TestMethod]
        public void TurnsToBuildWithoutAPortIsUnknown()
        {
            Planet[] ports = Player.BestPortsToBuildShips();
            Assert.AreEqual(0, ports.Length);
            Assert.AreEqual(9999, Player.TurnsToBuildShipAt(ports, ResourceManager.Ships.Designs.First()));
        }

        [TestMethod]
        public void ColonyShipExtraTurnsScaleWithProductionPace()
        {
            UState.P.Pace = 3f; // production pace 2
            Assert.AreEqual(ShipBuilder.ColonyShipExtraBuildTurns * 2f, ShipBuilder.ColonyShipMaxExtraTurns(Player), 0.001f);
        }
    }
}
