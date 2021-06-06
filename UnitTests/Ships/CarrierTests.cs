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
    public class CarrierTests : StarDriveTest
    {
        public CarrierTests()
        {
            CreateGameInstance();
            // Excalibur class has all the bells and whistles
            LoadStarterShips("Excalibur-Class Supercarrier", "Ving Defender", "Supply Shuttle");
            CreateUniverseAndPlayerEmpire();

            foreach (string uid in ResourceManager.GetShipTemplateIds())
                Player.ShipsWeCanBuild.Add(uid);

            Universe.Objects.UpdateLists(true);
        }

        [TestMethod]
        public void FighterLaunch()
        {
            Ship ship = Ship.CreateShipAtPoint("Excalibur-Class Supercarrier", Player, Vector2.Zero);
            ship.InCombat = true;
            ship.Carrier.PrepShipHangars(Player);
            ship.Carrier.ScrambleFighters();
            Universe.Objects.UpdateLists();
            var fightersOut = Player.OwnedShips.Filter(s => s.IsHangarShip);
            Universe.Objects.UpdateLists();
            int ableToLaunchCount = ship.Carrier.AllFighterHangars.Length;
            Assert.AreEqual(ableToLaunchCount, fightersOut.Length, "Not all fighter hangars launched");
            ship.EngageStarDrive();
            var fighters = Player.OwnedShips.Filter(f => f.IsHangarShip);

            foreach (var fighter in fighters)
            {
                Assert.IsTrue(fighter.AI.State == AIState.ReturnToHangar);
            }
        }
    }
}
