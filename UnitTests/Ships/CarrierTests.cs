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
        TestShip Carrier;
        TestShip Hostile;
        FixedSimTime ScanInterval = new FixedSimTime(EmpireConstants.EnemyScanInterval);

        public CarrierTests()
        {
            CreateGameInstance();
            // Excalibur class has all the bells and whistles
            LoadStarterShips("Excalibur-Class Supercarrier", "Ving Defender", "Supply Shuttle", "Alliance-Class Mk Ia Hvy Assault", "Assault Shuttle");
            CreateUniverseAndPlayerEmpire();
            UnlockAllShipsFor(Player);
            Carrier = SpawnShip("Excalibur-Class Supercarrier", Player, Vector2.Zero);
            Universe.Objects.Update(TestSimStep);
        }

        protected override void OnObjectSimStep()
        {
            var fighters = Carrier.Carrier.GetActiveFighters();
            Log.Write($"Carrier: {Carrier.Position} fighters={fighters.Count}");
            foreach (Ship fighter in fighters)
            {
                Log.Write($"  Fighter dist={Carrier.Position.Distance(fighter.Position)} pos={fighter.Position}");
            }
        }

        void SpawnEnemyShip()
        {
            Hostile = SpawnShip("Ving Defender", Enemy, new Vector2(1000));
            RunObjectsSim(ScanInterval);
        }
        
        int MaxFighters => Carrier.Carrier.AllFighterHangars.Length;

        void AssertFighters(int active, int recalling, string recallMsg)
        {
            var fighters = Carrier.Carrier.GetActiveFighters();
            
            // looks like some ships have already returned to hangar?
            Assert.AreEqual(active, fighters.Count, "BUG: not all fighters are active");

            int actualRecalling = fighters.Count(s => s.AI.State == AIState.ReturnToHangar);
            Assert.AreEqual(recalling, actualRecalling, recallMsg);
        }

        void LaunchFighters(Vector2 offset = default)
        {
            Carrier.Carrier.ScrambleFighters();
            MoveFightersBy(offset);

            RunObjectsSim(TestSimStep);
            AssertFighters(active: MaxFighters, recalling: 0, "No fighters should be recalling right after Scramble");
        }

        void MoveFightersBy(Vector2 offset)
        {
            foreach (Ship fighter in  Carrier.Carrier.GetActiveFighters())
                fighter.Position += offset;
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
            LaunchFighters();

            float dist = CarrierBays.RecallMoveDistance + 5000;
            Carrier.AI.OrderMoveTo(new Vector2(dist), Vectors.Up, true, AIState.AwaitingOrders);
            RunObjectsSim(TestSimStep);

            AssertFighters(active: MaxFighters, recalling: MaxFighters, "All fighters should be recalling due to Warp move");
        }

        [TestMethod]
        public void NoRecallWithin10k()
        {
            SpawnEnemyShip();// need an enemy so that ships don't immediately ReturnToHangar
            RunObjectsSim(ScanInterval);
            LaunchFighters();

            Carrier.AI.OrderMoveTo(new Vector2(10000), Vectors.Up, true, AIState.AwaitingOrders);
            RunObjectsSim(ScanInterval);
            
            AssertFighters(active: MaxFighters, recalling: 0, "NO fighters should be recalling within 10k");
        }

        [TestMethod]
        public void NoRecallDuringCombat()
        {
            SpawnEnemyShip();
            AssertFighters(active: MaxFighters, recalling: 0, "Fighters should have automatically launched");

            Carrier.AI.OrderMoveTo(new Vector2(10000), Vectors.Up, true, AIState.AwaitingOrders);
            RunObjectsSim(ScanInterval);
            
            AssertFighters(active: MaxFighters, recalling: 0, "NO fighters should be recalling during combat");
        }

        [TestMethod]
        public void RecallWhenFarAway()
        {
            SpawnEnemyShip();
            AssertFighters(active: MaxFighters, recalling: 0, "Fighters should have automatically launched");

            MoveShipWithoutFightersTo(Carrier, new Vector2(Carrier.SensorRange + 25000));

            // start warping away
            Carrier.AI.OrderMoveTo(Carrier.Position + new Vector2(10000), Vectors.Up, true, AIState.AwaitingOrders);
            RunObjectsSim(ScanInterval);
            
            AssertFighters(active: MaxFighters, recalling: MaxFighters, "Fighters should be recalling when far away");
        }

        [TestMethod]
        public void RecallDuringNoStopMove()
        {
            SpawnEnemyShip();
            AssertFighters(active: MaxFighters, recalling: 0, "Fighters should have automatically launched");

            MoveShipWithoutFightersTo(Carrier, new Vector2(Carrier.SensorRange + 25000));

            // start warping away
            Carrier.AI.OrderMoveToNoStop(Carrier.Position + new Vector2(10000), Vectors.Up, true, AIState.AwaitingOrders);
            RunObjectsSim(ScanInterval);
            
            AssertFighters(active: MaxFighters, recalling: MaxFighters, "Fighters should be recalling during no stop move");
        }

        [TestMethod]
        public void RecallDuringCombatMove()
        {
            SpawnEnemyShip();
            AssertFighters(active: MaxFighters, recalling: 0, "Fighters should have automatically launched");
            MoveFightersBy(new Vector2(1500)); // move fighters further so they can't recover immediately

            MoveShipWithoutFightersTo(Carrier, new Vector2(Carrier.SensorRange + 25000));

            // start warping away
            Carrier.AI.OrderMoveTo(Carrier.Position + new Vector2(10000), Vectors.Up, true, 
                                   AIState.AwaitingOrders, offensiveMove:true);
            RunObjectsSim(ScanInterval);
            
            AssertFighters(active: MaxFighters, recalling: MaxFighters, "Fighters should be recalling during combat move");
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
            AssertFighters(active: MaxFighters, recalling: 0, "Fighters should have automatically launched");

            Fleet fleet = CreateFleet();
            fleet.MoveToNow(new Vector2(30000, 30000), Vectors.Up);
            RunObjectsSim(ScanInterval);
            
            AssertFighters(active: MaxFighters, recalling: MaxFighters, "Fighters should be recalling during fleet move");
        }

        [TestMethod]
        public void ScrambleAssaultShips()
        {
            var friendlyShip = Ship.CreateShipAtPoint("Alliance-Class Mk Ia Hvy Assault", Player, Vector2.Zero);
            friendlyShip.Carrier.ScrambleAssaultShips(1);
            RunObjectsSim(ScanInterval);

            int assaultShips = Player.OwnedShips.Count(s => s.DesignRole == ShipData.RoleName.troop);
            Assert.AreNotEqual(0, assaultShips, "Should have launched assault ships");
        }
    }
}
