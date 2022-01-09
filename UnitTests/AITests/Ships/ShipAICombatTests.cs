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
        TestShip Us, ScoutTarget, RocketFTarget, CorvetteTarget, FrigateTarget, CarrierTarget;
        readonly FixedSimTime EnemyScanInterval = new FixedSimTime(EmpireConstants.EnemyScanInterval);
        const string ScoutName = "Vulcan Scout";
        const string RocketFName = "Rocket Scout";
        const string CorvetteName = "Hornet mk2-a";
        const string FrigateName = "TEST_Spearhead mk1-a";
        const string CarrierName = "Light Carrier mk1-a";

        public ShipAICombatTests()
        {
            LoadStarterShips(ScoutName, RocketFName, CorvetteName, FrigateName, CarrierName);
            CreateUniverseAndPlayerEmpire("Human");
            Assert.AreEqual(0, Universe.Objects.Ships.Count);

            // settings these flags to get detailed and readable debug output
            Universe.Objects.EnableParallelUpdate = false;
            ShipAI.EnableTargetPriorityDebug = true;
        }

        TestShip SpawnNonCombat(string name, Ship_Game.Empire loyalty, float x, float y)
        {
            var ship = SpawnShip(name, loyalty, new Vector2(x, y));
            ship.AI.SetCombatTriggerDelay(10f); // disable firing weapons
            ship.AI.TargetProjectiles = true;
            if (ship != Us && Us != null) // Rotate to LOOK AT our ship
                ship.Rotation = ship.Position.DirectionToTarget(Us.Position).ToRadians();
            if (ship.DesignRole == RoleName.carrier)
                ship.Carrier.DisableFighterLaunch = true;
            return ship;
        }

        void SpawnOurShip(string name)
        {
            Us = SpawnNonCombat(name, Player, 0, 0.1f);
            RunObjectsSim(TestSimStep);
            Assert.IsFalse(Us.InCombat, "ship should not be in combat yet");
        }

        void SpawnEnemyShips()
        {
            ScoutTarget = SpawnNonCombat(ScoutName, Enemy, 0, -800);
            RocketFTarget = SpawnNonCombat(RocketFName, Enemy, 0, -900);
            RunObjectsSim(EnemyScanInterval);
            Assert.AreEqual(1+2, Universe.Objects.Ships.Count, "Expected limited # of Ships in AI Combat test");
            PrintTargets();
        }

        void SpawnStrongerEnemyGroup()
        {
            ScoutTarget = SpawnNonCombat(ScoutName, Enemy, 0, -500);
            RocketFTarget = SpawnNonCombat(RocketFName, Enemy, 0, -600);
            CorvetteTarget = SpawnNonCombat(CorvetteName, Enemy, 0, -700);
            FrigateTarget = SpawnNonCombat(FrigateName, Enemy, 0, -800);
            RunObjectsSim(EnemyScanInterval);
            Assert.AreEqual(1+4, Universe.Objects.Ships.Count, "Expected limited # of Ships in AI Combat test");
            PrintTargets();
        }

        void SpawnStrongEnemyGroupWithCarrier()
        {
            ScoutTarget = SpawnNonCombat(ScoutName, Enemy, 0, -500);
            RocketFTarget = SpawnNonCombat(RocketFName, Enemy, 0, -600);
            CorvetteTarget = SpawnNonCombat(CorvetteName, Enemy, 0, -700);
            FrigateTarget = SpawnNonCombat(FrigateName, Enemy, 0, -800);
            CarrierTarget = SpawnNonCombat(CarrierName, Enemy, 0, -900);
            RunObjectsSim(EnemyScanInterval);
            Assert.AreEqual(1+5, Universe.Objects.Ships.Count, "Expected limited # of Ships in AI Combat test");
            PrintTargets();
        }

        void PrintTargets()
        {
            Log.Write($"Targets: {Us.AI.PotentialTargets.Length}");
            Log.Write($"OurShip.Target: {Us.AI.Target}");
            foreach (Ship tgt in Us.AI.PotentialTargets)
                Log.Write($" - Dist:{tgt.Distance(Us).String()} {tgt}");
        }

        [TestMethod]
        public void EnemyShipsDetectedOnScan()
        {
            SpawnOurShip(ScoutName);
            SpawnEnemyShips();
            Assert.AreEqual(2, Us.AI.PotentialTargets.Length);
            Assert.AreEqual(0, Us.AI.FriendliesNearby.Length);
            Assert.AreEqual(0, Us.AI.TrackProjectiles.Length);

            Assert.AreEqual(1, ScoutTarget.AI.PotentialTargets.Length);
            Assert.AreEqual(1, ScoutTarget.AI.FriendliesNearby.Length);
            Assert.AreEqual(0, ScoutTarget.AI.TrackProjectiles.Length);
        }

        [TestMethod]
        public void HighestPriorityShipSelectedAsTarget()
        {
            SpawnOurShip(ScoutName);
            SpawnEnemyShips();
            Assert.AreEqual(2, Us.AI.PotentialTargets.Length);
            Assert.AreEqual(1, ScoutTarget.AI.PotentialTargets.Length);

            Assert.IsNotNull(ScoutTarget.AI.Target, "No Target! BUG in ScanForCombatTargets()");
            Assert.AreEqual(Us, ScoutTarget.AI.Target);

            var highestPriority = Us.AI.GetHighestPriorityTarget();
            Assert.IsNotNull(Us.AI.Target, "No Target! BUG in ScanForCombatTargets()");
            Assert.AreEqual(highestPriority, Us.AI.Target, "Expected highest priority target to be selected");
        }

        [TestMethod]
        public void FightersPreferWeakestFighters()
        {
            SpawnOurShip(ScoutName);
            SpawnStrongerEnemyGroup();
            Assert.IsNotNull(Us.AI.Target, "No Target! BUG in ScanForCombatTargets()");
            Assert.AreEqual(ScoutTarget, Us.AI.Target, "Expected weakest fighter to be selected");
        }

        [TestMethod]
        public void FighterAntiShipPreferLargerTarget()
        {
            SpawnOurShip(ScoutName);
            Us.ShipData.HangarDesignation = HangarOptions.AntiShip;
            SpawnStrongerEnemyGroup();

            Assert.IsNotNull(Us.AI.Target, "No Target! BUG in ScanForCombatTargets()");
            Assert.AreEqual(FrigateTarget, Us.AI.Target, "Expected large target to be selected since designation is AntiShip");
        }

        [TestMethod]
        public void FrigateInterceptorsPreferSmallerTarget()
        {
            SpawnOurShip(FrigateName);
            Us.ShipData.HangarDesignation = HangarOptions.Interceptor;
            SpawnStrongerEnemyGroup();

            Assert.IsNotNull(Us.AI.Target, "No Target! BUG in ScanForCombatTargets()");
            Assert.AreEqual(ScoutTarget, Us.AI.Target, "Expected small target to be selected since designation is Interceptor");
        }

        [TestMethod]
        public void CorvettesPreferOtherCorvettes()
        {
            SpawnOurShip(CorvetteName);
            SpawnStrongerEnemyGroup();
            Assert.IsNotNull(Us.AI.Target, "No Target! BUG in ScanForCombatTargets()");
            Assert.AreEqual(CorvetteTarget, Us.AI.Target, "Expected frigate to target another frigate");
        }

        [TestMethod]
        public void FrigatesPreferOtherFrigates()
        {
            SpawnOurShip(FrigateName);
            SpawnStrongerEnemyGroup();
            Assert.IsNotNull(Us.AI.Target, "No Target! BUG in ScanForCombatTargets()");
            Assert.AreEqual(FrigateTarget, Us.AI.Target, "Expected frigate to target another frigate");
        }

        [TestMethod]
        public void FrigatesPreferJuicyCarriers()
        {
            SpawnOurShip(FrigateName);
            SpawnStrongEnemyGroupWithCarrier();
            Assert.IsNotNull(Us.AI.Target, "No Target! BUG in ScanForCombatTargets()");
            Assert.AreEqual(CarrierTarget, Us.AI.Target, "Expected frigate to target another frigate");
        }

        [TestMethod]
        public void WeCanDetectEnemyRockets()
        {
            SpawnOurShip(ScoutName);
            SpawnEnemyShips();
            Assert.AreEqual(2, Us.AI.PotentialTargets.Length);
            Assert.AreEqual(0, Us.AI.TrackProjectiles.Length);
            
            RocketFTarget.AI.SetCombatTriggerDelay(0f); // enable weapons
            Assert.IsNotNull(RocketFTarget.AI.Target, "No Target! BUG in ScanForCombatTargets()");
            bool didFireWeapons = RocketFTarget.AI.FireWeapons(TestSimStep);
            Assert.IsTrue(didFireWeapons, "Rocket Scout couldn't fire on our Ship. BUG!!");

            var minTime = new FixedSimTime(EmpireConstants.ProjectileScanInterval);
            RunObjectsSim(minTime);
            PrintTargets();
            Assert.AreEqual(2, Us.AI.PotentialTargets.Length);
            Assert.That.GreaterThan(Us.AI.TrackProjectiles.Length, 1, "Rockets weren't detected! BUG!");
        }

        [TestMethod]
        public void InCombatAutoEnterWithEnemyShips()
        {
            SpawnOurShip(ScoutName);
            SpawnEnemyShips();
            RunObjectsSim(EnemyScanInterval);
            Assert.IsTrue(Us.InCombat, "ship should be in combat");
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
            Log.Write($"OurShip: {Us}");
            Log.Write($"OurShip.Target = {Us.AI.Target}  CombatState = {Us.AI.CombatState} InCombat = {Us.InCombat}");
            Log.Write($"ColonyShip.CombatState = {colonyShip.AI.CombatState}  InCombat = {colonyShip.InCombat}");
            Log.Write($"ColonyShip.Health = {colonyShip.Health}  percent = {colonyShip.HealthPercent}");
            foreach (var m in colonyShip.Modules)
                Log.Write($"  ColonyShip.Module {m}");
        }

        void DebugPrintStatus(TestShip target)
        {
            if (!Us.InCombat && target.Active)
            {
                Log.Write($"Ship left combat! {Us} Ord:{(Us.OrdnancePercent*100).String()}%");
                Log.Write($"Target: {target}");
            }
            else if (target.Active)
            {
                Log.Write($"Vatt={Us.CurrentVelocity.String()} Vtgt={target.CurrentVelocity.String()} "+
                          $"Dist={Us.Distance(target).String()} Nmod={target.Modules.Count(m => m.Active)}");
            }
            else
            {
                Log.Write($"Target dead! {target}");
            }
        }

        void AssertHighAlertTimesOutCorrectly()
        {
            float timer = 0f;
            RunSimWhile((simTimeout:20, fatal:false), () => Us.OnHighAlert, () => {
                timer += TestSimStep.FixedTime;
            });
            Assert.AreEqual(Ship.HighAlertSeconds, timer, 1f, $"Ship should remain OnHighAlert for {Ship.HighAlertSeconds} after combat was over");
        }

        [TestMethod]
        public void InCombatAutoEnterAndExitWhenColonyShipDestroyed()
        {
            SpawnOurShip(ScoutName);
            TestShip colonyShip = SpawnShip("Colony Ship", Enemy, new Vector2(0,-500));
            colonyShip.AI.PriorityHoldPosition();
            RunObjectsSim(EnemyScanInterval);
            Assert.IsTrue(Us.InCombat, "ship should be in combat");
            InjectSteroids(Us);

            RunSimWhile((simTimeout:60, fatal:false), () => colonyShip.Active, () =>
            {
                DebugPrintStatus(colonyShip);
                Assert.IsTrue(Us.InCombat || !colonyShip.Active, "ship must stay in combat until target destroyed");
                colonyShip.Velocity = Vector2.Zero; // BUG: there is a strange drift effect in sim
            });

            if (colonyShip.Active)
            {
                DebugPrintKillFailure(colonyShip);
                Assert.Fail("Failed to kill colony ship");
            }

            Assert.IsFalse(Us.InCombat, "ship must exit combat after target destroyed");
            AssertHighAlertTimesOutCorrectly();
        }

        [TestMethod]
        public void InCombatAutoEnterWithCombatMoveShouldKillColonyShip()
        {
            SpawnOurShip(ScoutName);
            TestShip colonyShip = SpawnShip("Colony Ship", Enemy, new Vector2(0,-500));
            colonyShip.AI.PriorityHoldPosition();
            RunObjectsSim(EnemyScanInterval);
            Assert.IsTrue(Us.InCombat, "ship should be in combat");
            InjectSteroids(Us);

            // now assign offensive move order
            Us.AI.OrderMoveTo(colonyShip.Position, Vectors.Up, true, AIState.AwaitingOrders, offensiveMove: true);

            Assert.IsFalse(Us.InCombat, "ship must exit combat after giving a move order, since giving a move order clears orders");
            Us.AI.CombatState = CombatState.HoldPosition;
            // Let the ship reacquire the target since giving an order caused an exit
            // combat (as it should, otherwise the ship will not enter combat again). 
            RunObjectsSim(EnemyScanInterval); 

            // our ship must remain in combat the whole time until enemy ship is destroyed
            RunSimWhile((simTimeout:60, fatal:false), () => colonyShip.Active, () =>
            {
                DebugPrintStatus(colonyShip);
                Assert.IsTrue(Us.InCombat || !colonyShip.Active, "ship must stay in combat until target destroyed");
                Assert.IsTrue(Us.OnHighAlert);
                colonyShip.Velocity = Vector2.Zero; // BUG: there is a strange drift effect in sim, maybe ship is Evading?
                Assert.AreEqual(CombatState.HoldPosition, colonyShip.AI.CombatState);
            });

            if (colonyShip.Active)
            {
                DebugPrintKillFailure(colonyShip);
                Assert.Fail("Failed to kill colony ship");
            }

            Assert.IsFalse(Us.InCombat, "ship must exit combat after target destroyed");
            AssertHighAlertTimesOutCorrectly();
        }

        [TestMethod]
        public void InCombatAutoExitWhenEnemyShipsDie()
        {
            SpawnOurShip(ScoutName);
            SpawnEnemyShips();
            Assert.IsTrue(Us.InCombat, "ship should be in combat");

            ScoutTarget.Die(ScoutTarget, cleanupOnly: true);
            RocketFTarget.Die(RocketFTarget, cleanupOnly: true);

            RunObjectsSim(EnemyScanInterval);
            Assert.IsFalse(Us.InCombat, "ship should have exited combat");
        }

        [TestMethod]
        public void InCombatAutoExitWhenEnemyShipsWarpAway()
        {
            SpawnOurShip(ScoutName);
            SpawnEnemyShips();
            Assert.IsTrue(Us.InCombat, "ship should be in combat");

            // move the ships to the middle of nowhere (outside sensor range)
            ScoutTarget.Position = new Vector2(200000, 200000);
            RocketFTarget.Position = new Vector2(200000, 200000);

            RunObjectsSim(EnemyScanInterval);
            Assert.IsFalse(Us.InCombat, "ship should have exited combat");
        }

        [TestMethod]
        public void NotInCombatWithHostilePlanets()
        {
            SpawnOurShip(ScoutName);
            Assert.IsFalse(Us.InCombat, "ship should not be in combat yet");

            AddDummyPlanetToEmpire(Enemy);
            RunObjectsSim(EnemyScanInterval);

            // verify block
            Assert.IsTrue(Us.System != null, "Test wont work without being in system");
            Assert.IsTrue(Us.AI.BadGuysNear, "Test wont work if badguys near false");

            Assert.IsFalse(Us.InCombat, "ship should not be in combat with a planet");
            RunObjectsSim(EnemyScanInterval);
            Assert.IsTrue(Us.OnHighAlert, "Enemy planet should have set OnHighAlert");

            // now teleport ship to safety:
            Us.Position = new Vector2(200_000f);
            Us.AI.HoldPosition();

            AssertHighAlertTimesOutCorrectly();
        }
    }
}
