using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Fleets;
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
            LoadStarterShips("Excalibur-Class Supercarrier", "Ving Defender", "Supply Shuttle", "Alliance-Class Mk Ia Hvy Assault", "Assault Shuttle");
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
            ResetTest(ship);

            // recall fighters
            Vector2 movePos = new Vector2(30000, 30000);
            ship.AI.OrderMoveTo(movePos, Vectors.Up, true, AIState.AwaitingOrders);
            LaunchFighters(ship);
            TestMoveRecall(ship, movePos);

            // dont recall
            ship.AI.OrderMoveTo(new Vector2(10000), Vectors.Up, true, AIState.AwaitingOrders);
            LaunchFighters(ship);
            TestMoveRecall(ship, new Vector2(10000));

            // recall in combat
            ResetTest(ship);
            Player.GetRelations(Enemy).AtWar = true;
            var enemyShip = Ship.CreateShipAtPoint("Excalibur-Class Supercarrier", Enemy, Vector2.Zero);
            ship.AI.OrderMoveTo(new Vector2(10000, 10000), Vectors.Up, true, AIState.AwaitingOrders);
            LaunchFighters(ship);
            TestMoveRecall(ship, movePos);

            // recall when far away
            {
                ResetTest(ship);
                LaunchFighters(ship);
                MoveShipWithoutFightersTo(ship, new Vector2(ship.SensorRange + 25000, 0));
                float shipDistance = ship.Carrier.GetActiveFighters()[0].Center.Distance(ship.Center);
                Assert.IsTrue(shipDistance > 7500, $"fighter not far enough away: range {(int)shipDistance }");
                movePos = new Vector2(ship.SensorRange + 25000, ship.SensorRange + 25000);
                ship.AI.OrderMoveTo(movePos, Vectors.Up, true, AIState.AwaitingOrders);
                TestMoveRecall(ship, movePos);
            }

            // recall with no stop move
            {
                ResetTest(ship);
                LaunchFighters(ship);
                movePos = new Vector2(ship.SensorRange + 25000, 0);
                MoveShipWithoutFightersTo(ship, movePos);
                float shipDistance = ship.Carrier.GetActiveFighters()[0].Center.Distance(ship.Center);
                Assert.IsTrue(shipDistance > 7500, $"fighter not far enough away: range {(int)shipDistance }");
                ship.AI.OrderMoveToNoStop(new Vector2(ship.SensorRange + 25000, ship.SensorRange + 25000), Vectors.Up, true, AIState.AwaitingOrders);
                movePos = new Vector2(ship.SensorRange + 25000, ship.SensorRange + 25000);
                TestMoveRecall(ship, movePos);
            }

            // recall with combat move
            {
                ResetTest(ship);
                LaunchFighters(ship);
                MoveShipWithoutFightersTo(ship, new Vector2(ship.SensorRange + 25000, 0));
                LaunchFighters(ship);
                float shipDistance = ship.Carrier.GetActiveFighters()[0].Center.Distance(ship.Center);
                Assert.IsTrue(shipDistance > 7500, $"fighter not far enough away: range {(int)shipDistance }");
                ship.AI.OrderMoveTo(movePos, Vectors.Up, true, AIState.AwaitingOrders);
                TestMoveRecall(ship, movePos);
            }

            // fleet stuff
            ResetTest(ship);
            var friendlyShip = Ship.CreateShipAtPoint("Alliance-Class Mk Ia Hvy Assault", Player, Vector2.Zero);
            var fleet = new Fleet(new Array<Ship> { ship, friendlyShip }, Player);
            fleet.SetCommandShip(ship);
            fleet.AutoArrange();
            Player.FirstFleet = fleet;
            // recall in fleet
            {
                Player.GetRelations(Enemy).AtWar = true;
                fleet.MoveToNow(movePos, Vectors.Up);
                LaunchFighters(ship);
                TestMoveRecall(ship, movePos);
            }

            // Launch Assault Shuttle
            {
                ResetTest(friendlyShip);
                // wait till ready to recall
                while (friendlyShip.IsSpoolingOrInWarp || friendlyShip.Carrier.RecallingShipsBeforeWarp)
                    friendlyShip.Update(TestSimStep);
                friendlyShip.Carrier.ScrambleAssaultShips(1);
                Universe.Objects.Update(TestSimStep);
                var assaultShips = Player.OwnedShips.Filter(s => s.DesignRole == ShipData.RoleName.troop);
                Assert.IsTrue(assaultShips.Length > 0);
            }


        }

        void ResetTest(Ship ship)
        {
            // wait till ready to recall
            while (ship.IsSpoolingOrInWarp || ship.Carrier.AllFighterHangars.Any(h=>h.hangarTimer > 0))
                ship.Update(TestSimStep);

            // purge all fighters
            var fighters = Player.OwnedShips.Filter(s => s.IsHangarShip);
            foreach (var fighter in fighters) fighter.Active = false;
            fighters = Array.Empty<Ship>();
            var clear =Parallel.Run(() => Universe.Objects.UpdateLists());
            clear.Wait();

            fighters = Player.OwnedShips.Filter(s => s.IsHangarShip);
            Assert.AreEqual(0, fighters.Length);

            // move to zero
            ship.AI.OrderMoveTo(Vector2.Zero, Vectors.Up, true, AIState.AwaitingOrders);
            while (ship.AI.OrderQueue.NotEmpty)
                ship.Update(TestSimStep);

            // resupply
            ship.ChangeOrdnance(ship.OrdinanceMax);
        }

        Ship[] LaunchFighters(Ship ship)
        {
            ship.Carrier.PrepShipHangars(Player);
            ship.Carrier.ScrambleFighters();
            var clear = Parallel.Run(() => Universe.Objects.UpdateLists());
            clear.Wait();
            var fighters = Player.OwnedShips.Filter(s => s.IsHangarShip);
            int ableToLaunchCount = ship.Carrier.AllFighterHangars.Length;
            Assert.AreEqual(ableToLaunchCount, fighters.Length, "BUG: Not all fighter hangars launched");
            ship.Update(TestSimStep);
            return fighters;
        }

        void MoveShipWithoutFightersTo(Ship ship, Vector2 pos)
        {
            ship.Carrier.SetRecallFightersBeforeFTL(false);
            ship.AI.OrderMoveTo(pos, Vectors.Up, true, AIState.AwaitingOrders);
            while (ship.AI.OrderQueue.NotEmpty)
                ship.Update(TestSimStep);
            ship.Carrier.SetRecallFightersBeforeFTL(true);
        }

        void TestMoveRecall(Ship movingShip, Vector2 pos)
        {
            float startDistance = movingShip.Center.Distance(pos);
            var fighters = movingShip.Carrier.GetActiveFighters();

            bool testRecall = fighters.Any(f => f.AI.State == AIState.ReturnToHangar);
            bool wentToWarp = false;
            Assert.IsTrue(testRecall, "Test Error. No Fighters should be returning to hangar");
            var ships = Universe.GetMasterShipList().Filter(s => !s.IsHangarShip && s.loyalty == Player);

            while (movingShip.AI.OrderQueue.Count > 0)
            {
                Player.FirstFleet?.Update(TestSimStep);
                foreach (var ship in ships)
                {
                    ship.Update(TestSimStep);
                    ship.UpdateSensorsAndInfluence(TestVarTime);
                    if (ship.AI.State == AIState.HoldPosition)
                        ship.AI.ClearOrders();
                }

                wentToWarp |= movingShip.engineState == Ship.MoveState.Warp;

                if (startDistance > CarrierBays.RecallMoveDistance)
                {
                    if (testRecall)
                    {
                        int fightersRecalling = fighters.Count(f => f.AI.State == AIState.ReturnToHangar);
                        Assert.AreEqual(18, fightersRecalling, "BUG: All fighters should be inactive.");
                        testRecall = false;
                    }
                }
                foreach(var ship in fighters)
                {
                    ship.Update(TestSimStep);
                }
                Player.FirstFleet?.AveragePosition(true);
                Player.FirstFleet?.SetSpeed();
            }

            if (startDistance > CarrierBays.RecallMoveDistance)
                Assert.AreEqual(0, fighters.Count(f => f.Active), "BUG: All fighters should be inactive");
            else 
                Assert.AreEqual(18, fighters.Count(f => f.Active), "BUG: All fighters should be active");

            if (movingShip.fleet == null)
                Assert.IsTrue(wentToWarp, "Unknown Error: moving ship did not enter warp");
        }
    }
}
