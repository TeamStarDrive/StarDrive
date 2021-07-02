using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Empires;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class CarrierTests : StarDriveTest
    {
        Ship Carrier;
        Ship Hostile;
        FixedSimTime ScanInterval = new FixedSimTime(EmpireConstants.EnemyScanInterval);

        public CarrierTests()
        {
            CreateGameInstance();
            // Excalibur class has all the bells and whistles
            LoadStarterShips("Excalibur-Class Supercarrier", "Ving Defender", "Supply Shuttle", "Alliance-Class Mk Ia Hvy Assault", "Assault Shuttle");
            CreateUniverseAndPlayerEmpire();

            foreach (string uid in ResourceManager.GetShipTemplateIds())
                Player.ShipsWeCanBuild.Add(uid);

            Carrier = Ship.CreateShipAtPoint("Excalibur-Class Supercarrier", Player, Vector2.Zero);
            Universe.Objects.Update(TestSimStep);
            Player.GetRelations(Enemy).AtWar = true;
        }

        void SpawnEnemyShip()
        {
            Hostile = Ship.CreateShipAtPoint("Ving Defender", Enemy, new Vector2(1000));
            Universe.Objects.Update(ScanInterval);
        }
        
        int ActiveFighters => Carrier.Carrier.GetActiveFighters().Count;

        int RecallingFighters => Carrier.Carrier.GetActiveFighters()
                                .Count(f => f.AI.State == AIState.ReturnToHangar);

        void LaunchFighters(Ship ship)
        {
            ship.Carrier.ScrambleFighters();
            Universe.Objects.Update(ScanInterval);

            Assert.AreEqual(ship.Carrier.AllFighterHangars.Length, ActiveFighters, "BUG: Not all fighter hangars launched");
        }

        void MoveShipWithoutFightersTo(Ship ship, Vector2 pos)
        {
            ship.Carrier.SetRecallFightersBeforeFTL(false);
            ship.AI.OrderMoveTo(pos, Vectors.Up, true, AIState.AwaitingOrders);
            while (ship.AI.OrderQueue.NotEmpty)
                Universe.Objects.Update(ScanInterval);
            ship.Carrier.SetRecallFightersBeforeFTL(true);
        }

        [TestMethod]
        public void RecallForWarp()
        {
            SpawnEnemyShip(); // need an enemy so that ships don't immediately ReturnToHangar
            LaunchFighters(Carrier);

            float dist = CarrierBays.RecallMoveDistance + 5000;
            Carrier.AI.OrderMoveTo(new Vector2(dist), Vectors.Up, true, AIState.AwaitingOrders);
            Universe.Objects.Update(ScanInterval);

            Assert.AreEqual(ActiveFighters, RecallingFighters, "All Fighters should be recalling due to Warp move");
        }

        [TestMethod]
        public void NoRecallWithin10k()
        {
            SpawnEnemyShip(); // need an enemy so that ships don't immediately ReturnToHangar
            LaunchFighters(Carrier);

            Carrier.AI.OrderMoveTo(new Vector2(10000), Vectors.Up, true, AIState.AwaitingOrders);
            Universe.Objects.Update(ScanInterval);

            Assert.AreEqual(0, RecallingFighters, "NO Fighters should be recalling within 10k");
        }

        [TestMethod]
        public void NoRecallDuringCombat()
        {
            SpawnEnemyShip();
            LaunchFighters(Carrier);

            Carrier.AI.OrderMoveTo(new Vector2(10000), Vectors.Up, true, AIState.AwaitingOrders);
            Universe.Objects.Update(ScanInterval);

            Assert.AreEqual(0, RecallingFighters, "NO Fighters should be recalling during combat");
        }

        [TestMethod]
        public void RecallWhenFarAway()
        {
            SpawnEnemyShip();
            LaunchFighters(Carrier);
            MoveShipWithoutFightersTo(Carrier, new Vector2(Carrier.SensorRange + 25000));
            
            // start warping away
            Carrier.AI.OrderMoveTo(Carrier.Center + new Vector2(10000), Vectors.Up, true, AIState.AwaitingOrders);
            Universe.Objects.Update(ScanInterval);

            Assert.AreEqual(ActiveFighters, RecallingFighters, "Fighters should be recalling because too far");
        }

        [TestMethod]
        public void RecallDuringNoStopMove()
        {
            SpawnEnemyShip();
            LaunchFighters(Carrier);
            MoveShipWithoutFightersTo(Carrier, new Vector2(Carrier.SensorRange + 25000));
            
            // start warping away
            Carrier.AI.OrderMoveToNoStop(Carrier.Center + new Vector2(10000), Vectors.Up, true, AIState.AwaitingOrders);
            Universe.Objects.Update(ScanInterval);

            Assert.AreEqual(ActiveFighters, RecallingFighters, "Fighters should be recalling because too far");
        }

        [TestMethod]
        public void RecallDuringCombatMove()
        {
            SpawnEnemyShip();
            LaunchFighters(Carrier);
            MoveShipWithoutFightersTo(Carrier, new Vector2(Carrier.SensorRange + 25000));
            
            // start warping away
            Carrier.AI.OrderMoveTo(Carrier.Center + new Vector2(10000), Vectors.Up, true, 
                                   AIState.AwaitingOrders, offensiveMove:true);
            Universe.Objects.Update(ScanInterval);

            Assert.AreEqual(ActiveFighters, RecallingFighters, "Fighters should be recalling because too far");
        }

        Fleet CreateFleet()
        {
            var friendlyShip = Ship.CreateShipAtPoint("Alliance-Class Mk Ia Hvy Assault", Player, Vector2.Zero);
            var fleet = new Fleet(new Array<Ship> { Carrier, friendlyShip }, Player);
            fleet.SetCommandShip(Carrier);
            fleet.AutoArrange();
            Player.FirstFleet = fleet;
            return fleet;
        }
        
        [TestMethod]
        public void RecallDuringFleetMove()
        {
            SpawnEnemyShip(); // need an enemy so that ships don't immediately ReturnToHangar
            Fleet fleet = CreateFleet();
            LaunchFighters(Carrier);
            fleet.MoveToNow(new Vector2(30000, 30000), Vectors.Up);
            Universe.Objects.Update(ScanInterval);

            Assert.AreEqual(ActiveFighters, RecallingFighters, "Fighters should be recalling because too far");
        }

        [TestMethod]
        public void ScrambleAssaultShips()
        {
            var friendlyShip = Ship.CreateShipAtPoint("Alliance-Class Mk Ia Hvy Assault", Player, Vector2.Zero);
            friendlyShip.Carrier.ScrambleAssaultShips(1);
            Universe.Objects.Update(ScanInterval);

            int assaultShips = Player.OwnedShips.Count(s => s.DesignRole == ShipData.RoleName.troop);
            Assert.AreNotEqual(0, assaultShips, "Should have launched assault ships");
        }
    }
}
