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
    public class TestResupplyLogic : StarDriveTest
    {
        Ship OurShip, EnemyShip;
        Planet Homeworld;

        readonly FixedSimTime EnemyScanInterval = new FixedSimTime(EmpireConstants.EnemyScanInterval);

        public TestResupplyLogic()
        {
            CreateUniverseAndPlayerEmpire();
            Homeworld = AddHomeWorldToEmpire(new Vector2(1000), Player);
        }

        void SpawnOurShip()
        {
            OurShip = SpawnShip("Vulcan Scout", Player, new Vector2(40000, 0));
        }

        void SpawnEnemyShip()
        {
            EnemyShip = SpawnShip("Vulcan Scout", Enemy, new Vector2(40000, -1600));
        }

        void Update(FixedSimTime timeStep)
        {
            UState.Objects.Update(timeStep);
        }

        [TestMethod]
        public void ResupplyConditionOrdnanceNonCombat()
        {
            SpawnOurShip();
            Player.UpdateRallyPoints(); // required for resupply tests

            ResupplyReason resupplyReason = OurShip.Supply.Resupply();
            Assert.IsTrue(resupplyReason == ResupplyReason.NotNeeded, "Ship should not need resupply, it is brand new");

            OurShip.ChangeOrdnance(-OurShip.OrdinanceMax);
            resupplyReason = OurShip.Supply.Resupply();
            Assert.IsTrue(resupplyReason == ResupplyReason.LowOrdnanceCombatOrDepleted, "Ship should need resupply for low ordnance depleted");
            OurShip.AI.ProcessResupply(resupplyReason);
            Assert.IsTrue(OurShip.AI.IgnoreCombat, "Ship should ignore combat after processing resupply");
            Assert.IsTrue(OurShip.AI.HasPriorityOrder, "Ship should have a priority order when resupplying");

            ShipAI.ShipGoal orbitGoal = OurShip.AI.OrderQueue.PeekLast;
            Assert.IsTrue(orbitGoal.TargetPlanet == Homeworld, "Resupplying ship should want to orbit a planet");

            // Now manually order the ship to move, like a player might do
            OurShip.AI.OrderMoveTo(Homeworld.Position, Vector2.Zero);
            Assert.IsFalse(OurShip.AI.IgnoreCombat, "Move command should cancel Ignore Combat (cancel orders)");
            resupplyReason = OurShip.Supply.Resupply();
            Assert.IsTrue(resupplyReason == ResupplyReason.NotNeeded, "Override command by player, ship should  not resupply");
        }

        [TestMethod]
        public void ResupplyConditionOrdnanceInCombat()
        {
            SpawnOurShip();
            SpawnEnemyShip();
            Update(EnemyScanInterval);

            Assert.IsTrue(OurShip.InCombat, "ship should be in combat");
            Assert.IsTrue(EnemyShip.InCombat, "ship should be in combat");
            float maxOrdnance = OurShip.OrdinanceMax;
            float ordnanceToRemove = maxOrdnance * (1 - ShipResupply.OrdnanceThresholdNonCombat) - 1;
            OurShip.ChangeOrdnance(-ordnanceToRemove);

            ResupplyReason resupplyReason = OurShip.Supply.Resupply();
            Assert.IsTrue(resupplyReason == ResupplyReason.NotNeeded, "Ship should not need resupply if ordnance above non combat threshold");

            OurShip.ChangeOrdnance(OurShip.OrdinanceMax); // bring back to full ordnance
            ordnanceToRemove = maxOrdnance * (1 - ShipResupply.OrdnanceThresholdCombat) - 1;
            OurShip.ChangeOrdnance(-ordnanceToRemove);
            resupplyReason   = OurShip.Supply.Resupply();
            Assert.IsTrue(resupplyReason == ResupplyReason.NotNeeded, "Ship should not need resupply if ordnance above combat threshold");

            OurShip.ChangeOrdnance(-2); // Now go below he combat threshold
            resupplyReason = OurShip.Supply.Resupply();
            Assert.IsTrue(resupplyReason == ResupplyReason.LowOrdnanceCombatOrDepleted, "Ship should need resupply if ordnance below combat threshold");
        }

        [TestMethod]
        public void ADepletedStationOutOfCombatAsksAColonyForASupplyShuttle()
        {
            LoadStarterShips("Vulfen Small Station mk1-a");
            Ship station = SpawnShip("Vulfen Small Station mk1-a", Player, new Vector2(40000, 0));
            station.ChangeOrdnance(-station.OrdinanceMax);
            AssertLessThan(station.OrdnancePercent, ShipResupply.OrdnanceThresholdCombat, "setup: the station has run out of ordnance");
            AssertGreaterThan(Homeworld.NumSupplyShuttlesCanLaunch(), 0, "setup: the homeworld can launch a supply shuttle");

            ResupplyReason resupplyReason = station.Supply.Resupply();
            AssertEqual(ResupplyReason.RequestResupplyForOrbital, resupplyReason, "a station out of combat asks a colony however low it runs");
            station.AI.ProcessResupply(resupplyReason);
            Assert.IsTrue(Player.AI.HasGoal(g => g.Type == GoalType.RearmShipFromPlanet && g.TargetShip == station), "for a supply shuttle");
        }

        [TestMethod]
        public void ADepletedStationInCombatWaitsForASupplyShipNearby()
        {
            LoadStarterShips("Vulfen Small Station mk1-a");
            Ship station = SpawnShip("Vulfen Small Station mk1-a", Player, new Vector2(40000, 0));
            SpawnEnemyShip();
            Update(EnemyScanInterval);
            Assert.IsTrue(station.AI.BadGuysNear, "setup: an enemy is near the station");
            station.ChangeOrdnance(-station.OrdinanceMax);

            AssertEqual(ResupplyReason.LowOrdnanceCombatOrDepleted, station.Supply.Resupply(), "a station with kinetic weapons out of ordnance in combat asks no colony");
        }

        // a fleet ship out of ordnance heads home to resupply, then waits by a supply ship that comes in sight, and enemies near send it back to its fleet before it is rearmed
        Fleet FleetShipSentBackFromASupplyShip()
        {
            LoadStarterShips("TEST_Excalibur-Class Supercarrier");
            SpawnOurShip();
            Ship supplyShip = SpawnShip("TEST_Excalibur-Class Supercarrier", Player, OurShip.Position + new Vector2(1000, 0));
            supplyShip.ChangeOrdnance(supplyShip.OrdinanceMax * 0.9f - supplyShip.Ordinance); // a spawned ship can hold more than its maximum, which reads as no ordnance status
            Assert.IsTrue(supplyShip.SupplyShipCanSupply, "setup: a supply ship with ordnance to give is beside the ship");
            Fleet fleet = Player.CreateFleet(1, null);
            fleet.AddShip(OurShip);
            Player.UpdateRallyPoints();
            OurShip.ChangeOrdnance(-OurShip.OrdinanceMax);
            OurShip.AI.BadGuysNear = true;

            OurShip.AI.ProcessResupply(ResupplyReason.LowOrdnanceCombatOrDepleted);
            Assert.IsTrue(OurShip.AI.State == AIState.Resupply && OurShip.AI.FindGoal(ShipAI.Plan.Orbit, out ShipAI.ShipGoal home) && home.TargetPlanet == Homeworld,
                          "setup: with no supply ship in sight, the ship heads home to resupply");
            OurShip.AI.FriendliesNearby = new[] { supplyShip };
            OurShip.AI.ProcessResupply(ResupplyReason.LowOrdnanceCombatOrDepleted);
            Assert.AreEqual(AIState.ResupplyEscort, OurShip.AI.State, "setup: then a supply ship comes in sight and it waits by that instead");

            OurShip.AI.TerminateResupplyIfDone(SupplyType.Rearm, terminateIfEnemiesNear: true);
            AssertLessThan(OurShip.OrdnancePercent, ShipResupply.OrdnanceThresholdCombat, "setup: enemies near send it back before it is rearmed");
            return fleet;
        }

        [TestMethod]
        public void AFleetShipSentBackFromItsSupplyShipByEnemiesRejoinsItsFleetOutOfResupply()
        {
            Fleet fleet = FleetShipSentBackFromASupplyShip();

            Assert.IsFalse(OurShip.Resupplying, "the ship rejoins its fleet out of the resupply state, so a supply shuttle on its way keeps on to it");
            Assert.IsTrue(OurShip.CanTakeFleetOrders, "and its fleet can order it again");
            Assert.IsTrue(System.Array.Exists(OurShip.AI.OrderQueue.ToArray(), g => g.Plan == ShipAI.Plan.MakeFinalApproach && g.MovePosition == fleet.GetFinalPos(OurShip)),
                          "it flies back to its spot in the fleet");
        }

        [TestMethod]
        public void AFleetShipSentBackFromItsSupplyShipHeadsHomeToResupplyOnceNoSupplyShipIsInSight()
        {
            FleetShipSentBackFromASupplyShip();
            OurShip.AI.FriendliesNearby = Empty<Ship>.Array;
            OurShip.AI.ClearPriorityOrderAndTarget(); // as when it begins its final approach to its fleet spot

            OurShip.UpdateResupply();

            Assert.IsTrue(OurShip.AI.FindGoal(ShipAI.Plan.Orbit, out ShipAI.ShipGoal home) && home.TargetPlanet == Homeworld,
                          "with no supply ship in sight, it heads home to resupply instead of holding its fleet spot");
        }

        [TestMethod]
        public void ResupplyConditionNoCommand()
        {
            SpawnOurShip();
            Player.UpdateRallyPoints(); // required for resupply tests

            ResupplyReason resupplyReason = OurShip.Supply.Resupply();
            Assert.IsTrue(resupplyReason == ResupplyReason.NotNeeded, "Ship should not need resupply, it is brand new");

            // damage all command modules
            ShipModule[] command = OurShip.Modules.Filter(m => m.IsCommandModule);
            foreach (ShipModule cmd in command)
                cmd.Damage(OurShip, cmd.ActualMaxHealth);

            Update(EnemyScanInterval);
            resupplyReason = OurShip.Supply.Resupply();
            Assert.IsTrue(resupplyReason == ResupplyReason.NoCommand, "Ship should need resupply since it has no command module");
        }
    }
}
