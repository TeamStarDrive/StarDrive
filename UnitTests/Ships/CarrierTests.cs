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
            // Excalibur class has all the bells and whistles
            LoadStarterShips("Excalibur-Class Supercarrier", "Ving Defender", 
                             "Alliance-Class Mk Ia Hvy Assault", "Assault Shuttle");
            CreateUniverseAndPlayerEmpire();
            UnlockAllShipsFor(Player);
            Carrier = SpawnShip("Excalibur-Class Supercarrier", Player, Vector2.Zero);
            Universe.Objects.Update(TestSimStep);
        }

        void SpawnEnemyShip()
        {
            Hostile = SpawnShip("Ving Defender", Enemy, new Vector2(5000));
            Hostile.AI.HoldPosition();
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

        void MoveFightersBy(Vector2 offset)
        {
            foreach (Ship fighter in  Carrier.Carrier.GetActiveFighters())
                fighter.Position += offset;
        }

        void TeleportCarrierWithFightersTo(Vector2 newPos)
        {
            Vector2 offset = newPos - Carrier.Position;
            Carrier.Position = newPos;
            MoveFightersBy(offset);
            RunObjectsSim(TestSimStep);
        }

        void SpawnEnemyShipAndEnsureFightersLaunch()
        {
            // need an enemy so that ships don't immediately ReturnToHangar
            SpawnEnemyShip();
            AssertFighters(active: MaxFighters, recalling: 0, "Fighters should have automatically launched");
            
            // move fighters further so they can't ReturnToHangar immediately
            MoveFightersBy(new Vector2(1500));
            RunObjectsSim(TestSimStep);
            AssertFighters(active: MaxFighters, recalling: 0, "Fighters should not recall with enemy nearby");
        }

        [TestMethod]
        public void RecallForWarp()
        {
            SpawnEnemyShipAndEnsureFightersLaunch();

            float dist = CarrierBays.RecallMoveDistance + 5000;
            Carrier.AI.OrderMoveTo(new Vector2(dist), Vectors.Up, true, AIState.AwaitingOrders);
            RunObjectsSim(TestSimStep);

            AssertFighters(active: MaxFighters, recalling: MaxFighters, "All fighters should be recalling due to Warp move");
        }
        
        [TestMethod]
        public void NoRecallDuringCombat()
        {
            SpawnEnemyShipAndEnsureFightersLaunch();

            Carrier.AI.OrderMoveTo(new Vector2(10000), Vectors.Up, true, AIState.AwaitingOrders);
            RunObjectsSim(ScanInterval);
            
            AssertFighters(active: MaxFighters, recalling: 0, "NO fighters should be recalling during combat");
        }

        [TestMethod]
        public void NoRecallWithin10k()
        {
            SpawnEnemyShipAndEnsureFightersLaunch();

            Carrier.AI.OrderMoveTo(new Vector2(10000), Vectors.Up, true, AIState.AwaitingOrders);
            RunObjectsSim(ScanInterval);
            
            AssertFighters(active: MaxFighters, recalling: 0, "NO fighters should be recalling within 10k");
        }

        [TestMethod]
        public void RecallDuringCombatMove()
        {
            SpawnEnemyShipAndEnsureFightersLaunch();

            TeleportCarrierWithFightersTo(Carrier.Position + new Vector2(Carrier.SensorRange + 25000));
            AssertFighters(active: MaxFighters, recalling: 0, "Fighters should not recall yet");

            // start combat warp, the ships should NOT recall, because there are enemies to fight
            Carrier.AI.OrderMoveTo(Carrier.Position + new Vector2(10000), Vectors.Up, true,
                                   AIState.AwaitingOrders, offensiveMove:true);
            RunObjectsSim(TestSimStep);
            AssertFighters(active: MaxFighters, recalling: 0, "Fighters should NOT recall during combat move");
        }

        [TestMethod]
        public void RecallWhenFarAway()
        {
            SpawnEnemyShipAndEnsureFightersLaunch();
            
            // move ship really far
            Carrier.Position = new Vector2(Carrier.SensorRange + 35000);

            // start warping even farther
            Carrier.AI.OrderMoveTo(Carrier.Position + new Vector2(15000),
                                   Vectors.Up, true, AIState.AwaitingOrders);

            // fighters should recall because Carrier is really far
            // this must override combat state
            RunObjectsSim(ScanInterval);
            AssertFighters(active: MaxFighters, recalling: MaxFighters, "Fighters should be recalling when far away");
        }

        [TestMethod]
        public void RecallDuringNoStopMove()
        {
            SpawnEnemyShipAndEnsureFightersLaunch();

            // move ship really far
            Carrier.Position = new Vector2(Carrier.SensorRange + 25000);

            // start warping away
            Carrier.AI.OrderMoveToNoStop(Carrier.Position + new Vector2(10000), Vectors.Up, true, AIState.AwaitingOrders);
            
            RunObjectsSim(ScanInterval);
            AssertFighters(active: MaxFighters, recalling: MaxFighters, "Fighters should be recalling during no stop move");
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
            SpawnEnemyShipAndEnsureFightersLaunch();

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

        [TestMethod]
        public void CarrierOrdnanceInSpace()
        {
            Carrier.ChangeOrdnance(-Carrier.OrdinanceMax); // remove all ordnance
            Assert.IsTrue(Carrier.Ordinance == 0, "Carrier ordnance storage should  be empty");

            ResupplyReason resupplyReason = Carrier.Supply.Resupply();
            Assert.IsTrue(resupplyReason == ResupplyReason.LowOrdnanceNonCombat, "Carrier should want to resupply non combat");

            Carrier.ChangeOrdnance(Carrier.OrdinanceMax); // add all ordnance
            Assert.AreEqual(Carrier.Ordinance, Carrier.OrdinanceMax, "Carrier ordnance storage should be full");

            SpawnEnemyShipAndEnsureFightersLaunch();
            float totalFightersOrdCost = Carrier.Carrier.GetActiveFighters().Sum(f => f.ShipOrdLaunchCost);
            float ordnanceInSpace      = Carrier.Carrier.OrdnanceInSpace;
            Assert.AreEqual(totalFightersOrdCost, ordnanceInSpace, "Carrier should track the fighter ord cost is launched");

            float ordCombatThreshold = Carrier.OrdinanceMax * ShipResupply.OrdnanceThresholdCombat;
            Carrier.ChangeOrdnance(-Carrier.OrdinanceMax); // remove all ordnance
            Assert.IsTrue(Carrier.OrdnancePercent > 0, "Carrier should track its ordnance in space (launched fighters), even with empty local storage");

            // set the carrier storage just below the threshold so it would want to resupply if it had no fighters launched
            Carrier.ChangeOrdnance(ordCombatThreshold - 40); 
            resupplyReason = Carrier.Supply.Resupply();
            Assert.IsTrue(resupplyReason == ResupplyReason.NotNeeded, "Carrier should not want to resupply when non combat");

            // cleanup
            Carrier.ChangeOrdnance(-Carrier.OrdinanceMax);
            Carrier.ChangeOrdnance(Carrier.OrdinanceMax - ordnanceInSpace);
        }
    }
}
