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

            // recall fighters
            ship.AI.OrderMoveTo(new Vector2(10000, 10000), Vectors.Up, true, AIState.AwaitingOrders);
            while (ship.engineState == Ship.MoveState.Sublight) UpdateShipAndHangars(ship);
            Assert.IsFalse(fightersOut.Any(f => f.Active));
            ship.HyperspaceReturn();
            ship.Update(TestSimStep);

            ship.Carrier.ScrambleFighters();
            Universe.Objects.UpdateLists();
            fightersOut = Player.OwnedShips.Filter(s => s.IsHangarShip);

            // recall in combat
            Player.GetRelations(Enemy).AtWar = true;
            var enemyShip = Ship.CreateShipAtPoint("Excalibur-Class Supercarrier", Enemy, Vector2.Zero);
            ship.AI.OrderMoveTo(new Vector2(10000, 10000), Vectors.Up, true, AIState.AwaitingOrders);
            var fighters = Player.OwnedShips.Filter(f => f.IsHangarShip);
            while (ship.engineState == Ship.MoveState.Sublight) UpdateShipAndHangars(ship);
            Assert.IsFalse(fighters.Any(f => f.Active));
            ship.HyperspaceReturn();
        }

        void ResetShipAndFighters(Ship ship)
        {
            ship.AI.ClearOrders();
            ship.Update(TestSimStep);

            foreach (var hangar in ship.Carrier.AllFighterHangars)
            {
                if (hangar.TryGetHangarShipActive(out Ship fighter))
                    fighter.AI.State = AIState.AwaitingOrders;
            }
        }

        void CheckFighterAIState(Ship[] fighters, AIState state)
        {
            foreach (var fighter in Player.OwnedShips.Filter(f=> f.Mothership != null))
            {
                Assert.IsTrue(fighter.AI.State == state);
            }
        }

        void UpdateShipAndHangars(Ship ship)
        {
            ship.Update(TestSimStep);
            foreach (var hanger in ship.Carrier.AllFighterHangars)
            {
                hanger.TryGetHangarShip(out Ship fighter);
                fighter?.Update(TestSimStep);
            }
        }
    }
}
