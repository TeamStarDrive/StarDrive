using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Empires;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

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
            Homeworld = AddHomeWorldToEmpire(Vector2.Zero, Player);
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
            ResupplyReason resupplyReason = OurShip.Supply.Resupply();
            Assert.IsTrue(resupplyReason == ResupplyReason.NotNeeded, "Ship should not need resupply, it is brand new");

            OurShip.ChangeOrdnance(-OurShip.OrdinanceMax);
            resupplyReason = OurShip.Supply.Resupply();
            Assert.IsTrue(resupplyReason == ResupplyReason.LowOrdnanceNonCombat, "Ship should need resupply for low ordnance not in combat");
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
            Assert.IsTrue(resupplyReason == ResupplyReason.LowOrdnanceCombat, "Ship should need resupply if ordnance below combat threshold");
        }

        [TestMethod]
        public void ResupplyConditionNoCommand()
        {
            SpawnOurShip();
            ResupplyReason resupplyReason = OurShip.Supply.Resupply();
            Assert.IsTrue(resupplyReason == ResupplyReason.NotNeeded, "Ship should not need resupply, it is brand new");

            ShipModule command = OurShip.Modules.Find(m => m.IsCommandModule); // Should have only 1 command module
            command.Damage(OurShip, command.ActualMaxHealth);
            Update(EnemyScanInterval);
            resupplyReason = OurShip.Supply.Resupply();
            Assert.IsTrue(resupplyReason == ResupplyReason.NoCommand, "Ship should need resupply since it has no command module");
        }
    }
}
