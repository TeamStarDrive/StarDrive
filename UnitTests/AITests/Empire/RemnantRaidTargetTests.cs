using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Empire
{
    [TestClass]
    public class RemnantRaidTargetTests : StarDriveTest
    {
        public RemnantRaidTargetTests()
        {
            CreateUniverseAndPlayerEmpire();
            CreateAMinorFaction("Remnant");
        }

        // the way the Remnants open one: the goal is made with the new ship, which is placed in its system on the next update
        Ship OpenPortal(SolarSystem system, bool firstStep = true)
        {
            Ship portal = SpawnShip("Vulcan Scout", Faction, system.Position + new Vector2(5_000, 0));
            var goal = new RemnantPortal(Faction, portal, system.Name);
            Faction.AI.AddGoal(goal);
            Assert.IsNull(portal.System, "setup: a new ship has no system yet");
            UState.Objects.Update(new(time: 1f)); // joins the spatial index
            UState.Objects.Update(new(time: 1f)); // placed in its system
            if (!firstStep)
                goal.AdvanceToNextStep();
            goal.Evaluate();
            return portal;
        }

        SolarSystem EmptySystem(Vector2 pos)
        {
            var system = new SolarSystem(UState, pos) { Sun = SunType.RandomHabitableSun(UState.Random) };
            UState.AddSolarSystem(system);
            return system;
        }

        [TestMethod]
        public void APortalRegistersItsSystemOnceItsShipIsPlaced()
        {
            SolarSystem system = EmptySystem(new Vector2(0, 300_000));
            OpenPortal(system);
            Assert.IsTrue(UState.HasRemnantPortal(system), "known portal systems feed the pathfinder and the raid rules");
        }

        [TestMethod]
        public void APortalFromAnOlderSaveRegistersItsSystem()
        {
            SolarSystem system = EmptySystem(new Vector2(0, 300_000));
            OpenPortal(system, firstStep: false);
            Assert.IsTrue(UState.HasRemnantPortal(system), "a portal goal past its first step registers its system too");
        }
    }
}
