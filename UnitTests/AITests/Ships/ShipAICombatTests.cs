using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Empires;
using Ship_Game.Ships;
using UnitTests.Ships;

namespace UnitTests.AITests.Ships
{
    [TestClass]
    public class ShipAICombatTests : StarDriveTest
    {
        TestShip OurShip, WeakTarget, StrongTarget, FrigateTarget;
        readonly FixedSimTime EnemyScanInterval = new FixedSimTime(EmpireConstants.EnemyScanInterval);
        const string FrigateName = "TEST_Spearhead mk1-a";

        public ShipAICombatTests()
        {
            LoadStarterShips("Vulcan Scout", "Rocket Scout", FrigateName);
            CreateUniverseAndPlayerEmpire("Human");
            Universe.Objects.EnableParallelUpdate = false;
            Assert.AreEqual(0, Universe.Objects.Ships.Count);
        }

        TestShip SpawnNonCombat(string name, Ship_Game.Empire loyalty, Vector2 pos)
        {
            var ship = SpawnShip(name, loyalty, pos);
            ship.AI.SetCombatTriggerDelay(10f); // disable firing weapons
            ship.AI.TargetProjectiles = true;
            if (ship != OurShip && OurShip != null) // Rotate to LOOK AT our ship
                ship.Rotation = ship.Position.DirectionToTarget(OurShip.Position).ToRadians();
            return ship;
        }

        void SpawnOurShip(string name)
        {
            OurShip = SpawnNonCombat(name, Player, new Vector2(0, 0.1f));
            RunObjectsSim(TestSimStep);
            Assert.IsFalse(OurShip.InCombat, "ship should not be in combat yet");
        }

        void SpawnEnemyShips()
        {
            WeakTarget = SpawnNonCombat("Vulcan Scout", Enemy, new Vector2(0, -800));
            StrongTarget = SpawnNonCombat("Rocket Scout", Enemy, new Vector2(0, -900));
            RunObjectsSim(EnemyScanInterval);
            Assert.AreEqual(3, Universe.Objects.Ships.Count, "Expected limited # of Ships in AI Combat test");
            PrintTargets();
        }

        void SpawnStrongerEnemyGroup()
        {
            WeakTarget = SpawnNonCombat("Vulcan Scout", Enemy, new Vector2(0, -500));
            StrongTarget = SpawnNonCombat("Rocket Scout", Enemy, new Vector2(0, -600));
            FrigateTarget = SpawnNonCombat(FrigateName, Enemy, new Vector2(0, -800));
            RunObjectsSim(EnemyScanInterval);
            Assert.AreEqual(4, Universe.Objects.Ships.Count, "Expected limited # of Ships in AI Combat test");
            PrintTargets();
        }

        void PrintTargets()
        {
            Log.Write($"Targets: {OurShip.AI.PotentialTargets.Length}");
            Log.Write($"OurShip.Target: {OurShip.AI.Target}");
            foreach (Ship tgt in OurShip.AI.PotentialTargets)
                Log.Write($" - Dist:{tgt.Distance(OurShip).String()} {tgt}");
        }

        [TestMethod]
        public void EnemyShipsDetectedOnScan()
        {
            SpawnOurShip("Vulcan Scout");
            SpawnEnemyShips();
            Assert.AreEqual(2, OurShip.AI.PotentialTargets.Length);
            Assert.AreEqual(0, OurShip.AI.FriendliesNearby.Length);
            Assert.AreEqual(0, OurShip.AI.TrackProjectiles.Length);

            Assert.AreEqual(1, WeakTarget.AI.PotentialTargets.Length);
            Assert.AreEqual(1, WeakTarget.AI.FriendliesNearby.Length);
            Assert.AreEqual(0, WeakTarget.AI.TrackProjectiles.Length);
        }

        [TestMethod]
        public void HighestPriorityShipSelectedAsTarget()
        {
            SpawnOurShip("Vulcan Scout");
            SpawnEnemyShips();
            Assert.AreEqual(2, OurShip.AI.PotentialTargets.Length);
            Assert.AreEqual(1, WeakTarget.AI.PotentialTargets.Length);

            Assert.IsNotNull(WeakTarget.AI.Target, "No Target! BUG in ScanForCombatTargets()");
            Assert.AreEqual(OurShip, WeakTarget.AI.Target);

            var highestPriority = OurShip.AI.GetHighestPriorityTarget();
            Assert.IsNotNull(OurShip.AI.Target, "No Target! BUG in ScanForCombatTargets()");
            Assert.AreEqual(highestPriority, OurShip.AI.Target, "Expected highest priority target to be selected");
        }

        [TestMethod]
        public void FightersPreferWeakestFighters()
        {
            SpawnOurShip("Vulcan Scout");
            SpawnStrongerEnemyGroup();
            Assert.IsNotNull(OurShip.AI.Target, "No Target! BUG in ScanForCombatTargets()");
            Assert.AreEqual(WeakTarget, OurShip.AI.Target, "Expected weakest fighter to be selected");
        }

        [TestMethod]
        public void FrigatesPreferOtherFrigates()
        {
            SpawnOurShip(FrigateName);
            SpawnStrongerEnemyGroup();
            Assert.IsNotNull(OurShip.AI.Target, "No Target! BUG in ScanForCombatTargets()");
            Assert.AreEqual(FrigateTarget, OurShip.AI.Target, "Expected frigate to target another frigate");
        }

        [TestMethod]
        public void WeCanDetectEnemyRockets()
        {
            SpawnOurShip("Vulcan Scout");
            SpawnEnemyShips();
            Assert.AreEqual(2, OurShip.AI.PotentialTargets.Length);
            Assert.AreEqual(0, OurShip.AI.TrackProjectiles.Length);
            
            WeakTarget.AI.SetCombatTriggerDelay(0f); // enable weapons
            Assert.IsNotNull(WeakTarget.AI.Target, "No Target! BUG in ScanForCombatTargets()");
            bool didFireWeapons = WeakTarget.AI.FireWeapons(TestSimStep);
            Assert.IsTrue(didFireWeapons, "Rocket Scout couldn't fire on our Ship. BUG!!");

            var minTime = new FixedSimTime(EmpireConstants.ProjectileScanInterval);
            RunObjectsSim(minTime);
            PrintTargets();
            Assert.AreEqual(2, OurShip.AI.PotentialTargets.Length);
            Assert.That.GreaterThan(OurShip.AI.TrackProjectiles.Length, 1, "Rockets weren't detected! BUG!");
        }

        [TestMethod]
        public void InCombatAutoEnterWithEnemyShips()
        {
            SpawnOurShip("Vulcan Scout");
            SpawnEnemyShips();
            RunObjectsSim(EnemyScanInterval);
            Assert.IsTrue(OurShip.InCombat, "ship should be in combat");
        }

        static void InjectSteroids(Ship s)
        {
            // inject some steroids into our vulcan cannons 
            foreach (var w in s.Weapons)
            {
                w.DamageAmount = 500f;
                w.OrdinanceRequiredToFire = 0f;
                w.FireDelay = 0.25f;
            }
            s.AI.SetCombatTriggerDelay(0f);
            s.AI.CombatState = CombatState.ShortRange;
        }

        void DebugPrintKillFailure(TestShip colonyShip)
        {
            Log.Write($"Failed to kill colony ship!: {colonyShip}");
            Log.Write($"OurShip: {OurShip}");
            Log.Write($"OurShip.Target = {OurShip.AI.Target}  CombatState = {OurShip.AI.CombatState} InCombat = {OurShip.InCombat}");
            Log.Write($"ColonyShip.CombatState = {colonyShip.AI.CombatState}  InCombat = {colonyShip.InCombat}");
            Log.Write($"ColonyShip.Health = {colonyShip.Health}  percent = {colonyShip.HealthPercent}");
            foreach (var m in colonyShip.Modules)
                Log.Write($"  ColonyShip.Module {m}");
        }

        void DebugPrintStatus(TestShip target)
        {
            if (!OurShip.InCombat && target.Active)
            {
                Log.Write($"Ship left combat! {OurShip} Ord:{(OurShip.OrdnancePercent*100).String()}%");
                Log.Write($"Target: {target}");
            }
            else if (target.Active)
            {
                Log.Write($"Vatt={OurShip.CurrentVelocity.String()} Vtgt={target.CurrentVelocity.String()} "+
                          $"Dist={OurShip.Distance(target).String()} Nmod={target.Modules.Count(m => m.Active)}");
            }
            else
            {
                Log.Write($"Target dead! {target}");
            }
        }

        void AssertHighAlertTimesOutCorrectly()
        {
            float timer = 0f;
            RunSimWhile((simTimeout:20, fatal:false), () => OurShip.OnHighAlert, () => {
                timer += TestSimStep.FixedTime;
            });
            Assert.AreEqual(Ship.HighAlertSeconds, timer, 1f, $"Ship should remain OnHighAlert for {Ship.HighAlertSeconds} after combat was over");
        }

        [TestMethod]
        public void InCombatAutoEnterAndExitWhenColonyShipDestroyed()
        {
            SpawnOurShip("Vulcan Scout");
            TestShip colonyShip = SpawnShip("Colony Ship", Enemy, new Vector2(0,-500));
            colonyShip.AI.PriorityHoldPosition();
            RunObjectsSim(EnemyScanInterval);
            Assert.IsTrue(OurShip.InCombat, "ship should be in combat");
            InjectSteroids(OurShip);

            RunSimWhile((simTimeout:60, fatal:false), () => colonyShip.Active, () =>
            {
                DebugPrintStatus(colonyShip);
                Assert.IsTrue(OurShip.InCombat || !colonyShip.Active, "ship must stay in combat until target destroyed");
                colonyShip.Velocity = Vector2.Zero; // BUG: there is a strange drift effect in sim
            });

            if (colonyShip.Active)
            {
                DebugPrintKillFailure(colonyShip);
                Assert.Fail("Failed to kill colony ship");
            }

            Assert.IsFalse(OurShip.InCombat, "ship must exit combat after target destroyed");
            AssertHighAlertTimesOutCorrectly();
        }

        [TestMethod]
        public void InCombatAutoEnterWithCombatMoveShouldKillColonyShip()
        {
            SpawnOurShip("Vulcan Scout");
            TestShip colonyShip = SpawnShip("Colony Ship", Enemy, new Vector2(0,-500));
            colonyShip.AI.PriorityHoldPosition();
            RunObjectsSim(EnemyScanInterval);
            Assert.IsTrue(OurShip.InCombat, "ship should be in combat");
            InjectSteroids(OurShip);

            // now assign offensive move order
            OurShip.AI.OrderMoveTo(colonyShip.Position, Vectors.Up, true, AIState.AwaitingOrders, offensiveMove: true);

            Assert.IsFalse(OurShip.InCombat, "ship must exit combat after giving a move order, since giving a move order clears orders");
            OurShip.AI.CombatState = CombatState.HoldPosition;
            // Let the ship reacquire the target since giving an order caused an exit
            // combat (as it should, otherwise the ship will not enter combat again). 
            RunObjectsSim(EnemyScanInterval); 

            // our ship must remain in combat the whole time until enemy ship is destroyed
            RunSimWhile((simTimeout:60, fatal:false), () => colonyShip.Active, () =>
            {
                DebugPrintStatus(colonyShip);
                Assert.IsTrue(OurShip.InCombat || !colonyShip.Active, "ship must stay in combat until target destroyed");
                Assert.IsTrue(OurShip.OnHighAlert);
                colonyShip.Velocity = Vector2.Zero; // BUG: there is a strange drift effect in sim, maybe ship is Evading?
                Assert.AreEqual(CombatState.HoldPosition, colonyShip.AI.CombatState);
            });

            if (colonyShip.Active)
            {
                DebugPrintKillFailure(colonyShip);
                Assert.Fail("Failed to kill colony ship");
            }

            Assert.IsFalse(OurShip.InCombat, "ship must exit combat after target destroyed");
            AssertHighAlertTimesOutCorrectly();
        }

        [TestMethod]
        public void InCombatAutoExitWhenEnemyShipsDie()
        {
            SpawnOurShip("Vulcan Scout");
            SpawnEnemyShips();
            Assert.IsTrue(OurShip.InCombat, "ship should be in combat");

            WeakTarget.Die(WeakTarget, cleanupOnly: true);
            StrongTarget.Die(StrongTarget, cleanupOnly: true);

            RunObjectsSim(EnemyScanInterval);
            Assert.IsFalse(OurShip.InCombat, "ship should have exited combat");
        }

        [TestMethod]
        public void InCombatAutoExitWhenEnemyShipsWarpAway()
        {
            SpawnOurShip("Vulcan Scout");
            SpawnEnemyShips();
            Assert.IsTrue(OurShip.InCombat, "ship should be in combat");

            // move the ships to the middle of nowhere (outside sensor range)
            WeakTarget.Position = new Vector2(200000, 200000);
            StrongTarget.Position = new Vector2(200000, 200000);

            RunObjectsSim(EnemyScanInterval);
            Assert.IsFalse(OurShip.InCombat, "ship should have exited combat");
        }

        [TestMethod]
        public void NotInCombatWithHostilePlanets()
        {
            SpawnOurShip("Vulcan Scout");
            Assert.IsFalse(OurShip.InCombat, "ship should not be in combat yet");

            AddDummyPlanetToEmpire(Enemy);
            RunObjectsSim(EnemyScanInterval);

            // verify block
            Assert.IsTrue(OurShip.System != null, "Test wont work without being in system");
            Assert.IsTrue(OurShip.AI.BadGuysNear, "Test wont work if badguys near false");

            Assert.IsFalse(OurShip.InCombat, "ship should not be in combat with a planet");
            RunObjectsSim(EnemyScanInterval);
            Assert.IsTrue(OurShip.OnHighAlert, "Enemy planet should have set OnHighAlert");

            // now teleport ship to safety:
            OurShip.Position = new Vector2(200_000f);
            OurShip.AI.HoldPosition();

            AssertHighAlertTimesOutCorrectly();
        }
    }
}
