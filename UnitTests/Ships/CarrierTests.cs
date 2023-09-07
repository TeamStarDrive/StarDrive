using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Empires;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships
{
    [TestClass]
    public class CarrierTests : StarDriveTest
    {
        readonly TestShip Carrier;
        readonly FixedSimTime ScanInterval = new(EmpireConstants.EnemyScanInterval);
        TestShip Hostile;

        public CarrierTests()
        {
            LoadStarterShips("TEST_Heavy Carrier mk1",
                             "Ving Defender", 
                             "Alliance-Class Mk Ia Hvy Assault",
                             "Assault Shuttle",
                             "Terran Assault Shuttle");
            CreateUniverseAndPlayerEmpire();
            UnlockAllShipsFor(Player);
            Carrier = SpawnShip("TEST_Heavy Carrier mk1", Player, Vector2.Zero);
            UState.Objects.Update(TestSimStep);
            UState.P.DebugDisableShipLaunch = true;
        }

        void SpawnEnemyShip()
        {
            Hostile = SpawnShip("Ving Defender", Enemy, new Vector2(5000));
            Hostile.AI.OrderHoldPosition();
            RunObjectsSim(ScanInterval*2);
        }
        
        int MaxFighters => Carrier.Carrier.AllFighterHangars.Length;

        void AssertFighters(int active, int recalling, string recallMsg)
        {
            var fighters = Carrier.Carrier.GetActiveFighters();
            
            // looks like some ships have already returned to hangar?
            AssertEqual(active, fighters.Count, "BUG: not all fighters are active");

            int actualRecalling = fighters.Count(s => s.AI.State == AIState.ReturnToHangar);
            AssertEqual(recalling, actualRecalling, recallMsg);
        }

        void MoveFightersBy(Vector2 offset)
        {
            foreach (Ship fighter in Carrier.Carrier.GetActiveFighters())
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
            Carrier.AI.OrderMoveTo(new Vector2(dist), Vectors.Up, AIState.AwaitingOrders);
            RunObjectsSim(TestSimStep);

            AssertFighters(active: MaxFighters, recalling: MaxFighters, "All fighters should be recalling due to Warp move");
        }
        
        [TestMethod]
        public void NoRecallDuringCombat()
        {
            SpawnEnemyShipAndEnsureFightersLaunch();

            Carrier.AI.OrderMoveTo(new Vector2(CarrierBays.RecallMoveDistance * 0.5f), Vectors.Up, AIState.AwaitingOrders);
            RunObjectsSim(ScanInterval);
            
            AssertFighters(active: MaxFighters, recalling: 0, "NO fighters should be recalling when the" +
                                                              " carrier is jumping short distance");
        }

        [TestMethod]
        public void RecallDuringCombatMove()
        {
            SpawnEnemyShipAndEnsureFightersLaunch();

            TeleportCarrierWithFightersTo(Carrier.Position + new Vector2(Carrier.SensorRange + 25000));
            AssertFighters(active: MaxFighters, recalling: 0, "Fighters should not recall yet");

            // start combat warp, the ships should NOT recall, because there are enemies to fight
            Carrier.AI.OrderMoveTo(Carrier.Position + new Vector2(10000), Vectors.Up,
                                   AIState.AwaitingOrders, MoveOrder.Aggressive);
            RunObjectsSim(TestSimStep);
            AssertFighters(active: MaxFighters, recalling: 0, "Fighters should NOT recall during combat move");
        }

        [TestMethod]
        public void MustRecallWhenNoCombatAndFarAway()
        {
            Carrier.Carrier.ScrambleFighters();
            var fighters = Carrier.Carrier.GetActiveFighters();
            foreach (Ship fighter in fighters)
            {
                fighter.AI.OrderMoveTo(fighter.Position + new Vector2(-10000), Vectors.Up,
                                       AIState.AwaitingOrders);
                fighter.AI.SetPriorityOrder(true);
            }

            // move ship really far
            Carrier.Position = new Vector2(Carrier.SensorRange + 35000);

            // Fighters should recall because Carrier is really far they are are not combat
            // this should also override player/AI priority orders
            RunObjectsSim(ScanInterval);
            // allow deferred return to hangar
            RunObjectsSim(ScanInterval);
            AssertFighters(active: MaxFighters, recalling: 12, "Fighters should be recalling when outside of" +
                                                               " carrier sensor range and not in combat");
        }

        [TestMethod]
        public void NoRecallWhenInCombatAndFarAway()
        {
            SpawnEnemyShipAndEnsureFightersLaunch();

            // move ship really far
            Carrier.Position = new Vector2(Carrier.SensorRange + 35000);

            // fighters should recall because Carrier is really far and they are not in combat
            RunObjectsSim(ScanInterval);
            // allow deferred return to hangar (should not allow to return to hangar. We are testing it now :)
            RunObjectsSim(ScanInterval);
            AssertFighters(active: MaxFighters, recalling: 0, "Fighters should not be recalling when" +
                                                                        " far away and in combat");
        }

        [TestMethod]
        public void RecallOverRecallMoveDistanceInCombat()
        {
            SpawnEnemyShipAndEnsureFightersLaunch();

            // move ship a little
            Carrier.Position = new Vector2(Carrier.SensorRange * 0.5f);

            // start warping away
            Carrier.AI.OrderMoveToNoStop(Carrier.Position + new Vector2(CarrierBays.RecallMoveDistance + 10),
                Vectors.Up, AIState.AwaitingOrders);
            
            RunObjectsSim(ScanInterval);
            AssertFighters(active: MaxFighters, recalling: MaxFighters, "Fighters should be recalling during no stop" +
                                                                        " move since the carrier is warping and moving" +
                                                                        " more than RecallMoveDistance");
        }

        Fleet CreateFleet()
        {
            var friendlyShip = SpawnShip("Alliance-Class Mk Ia Hvy Assault", Player, Vector2.Zero);
            Fleet fleet = Player.CreateFleet(1, null);
            fleet.AddShips(new Array<Ship>{ Carrier, friendlyShip });
            fleet.SetCommandShip(Carrier);
            fleet.AutoArrange();
            return fleet;
        }

        [TestMethod]
        public void RecallDuringFleetMove()
        {
            SpawnEnemyShipAndEnsureFightersLaunch();

            Fleet fleet = CreateFleet();
            fleet.MoveTo(new Vector2(30000, 30000), Vectors.Up);
            RunObjectsSim(ScanInterval);
            
            AssertFighters(active: MaxFighters, recalling: MaxFighters, "Fighters should be recalling during fleet move");
        }

        [TestMethod]
        public void ScrambleAssaultShips()
        {
            var friendlyShip = SpawnShip("Alliance-Class Mk Ia Hvy Assault", Player, Vector2.Zero);
            friendlyShip.Carrier.ScrambleAssaultShips(1);
            RunObjectsSim(ScanInterval);

            int assaultShips = Player.OwnedShips.Count(s => s.DesignRole == RoleName.troop);
            Assert.AreNotEqual(0, assaultShips, "Should have launched assault ships");
        }

        [TestMethod]
        public void CarrierOrdnanceInSpace()
        {
            Carrier.ChangeOrdnance(-Carrier.OrdinanceMax); // remove all ordnance
            Assert.IsTrue(Carrier.Ordinance == 0, "Carrier ordnance storage should  be empty");

            // NOTE: This requires Carrier to have low ordnance production capability
            ResupplyReason resupplyReason = Carrier.Supply.Resupply();
            AssertEqual(resupplyReason, ResupplyReason.LowOrdnanceNonCombat, "Carrier should want to resupply non combat");

            Carrier.ChangeOrdnance(Carrier.OrdinanceMax); // add all ordnance
            AssertEqual(Carrier.Ordinance, Carrier.OrdinanceMax, "Carrier ordnance storage should be full");

            SpawnEnemyShipAndEnsureFightersLaunch();
            float totalFightersOrdCost = Carrier.Carrier.GetActiveFighters().Sum(f => f.ShipOrdLaunchCost);
            float ordnanceInSpace      = Carrier.Carrier.OrdnanceInSpace;
            AssertEqual(totalFightersOrdCost, ordnanceInSpace, "Carrier should track the fighter ord cost is launched");

            float ordCombatThreshold = Carrier.OrdinanceMax * ShipResupply.OrdnanceThresholdCombat;
            Carrier.ChangeOrdnance(-Carrier.OrdinanceMax); // remove all ordnance
            AssertGreaterThan(Carrier.OrdnancePercent, 0, "Carrier should track its ordnance in space (launched fighters), even with empty local storage");

            // set the carrier storage just below the threshold so it would want to resupply if it had no fighters launched
            Carrier.ChangeOrdnance(ordCombatThreshold - 40); 
            resupplyReason = Carrier.Supply.Resupply();
            AssertEqual(resupplyReason, ResupplyReason.NotNeeded, "Carrier should not want to resupply when in combat and has fighters launched");
        }
    }
}
