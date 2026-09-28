using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;

namespace UnitTests.EspionageTests
{
    [TestClass]
    public class LegacyAgentTests : StarDriveTest
    {
        Planet TheirColony;
        Mole OurMole;

        void PlantAMoleOnTheirColony()
        {
            CreateUniverseAndPlayerEmpire();
            Player.Universe.P.UseLegacyEspionage = true;
            TheirColony = AddHomeWorldToEmpire(new Vector2(2000), Enemy);
            OurMole = new Mole { PlanetId = TheirColony.Id };
            Player.data.MoleList.Add(OurMole);
        }

        [TestMethod]
        public void AnAgentGoingUndercoverKeepsTheMoleItJustPlanted()
        {
            PlantAMoleOnTheirColony();
            var agent = new Agent { Mission = AgentMission.Infiltrate, TargetPlanetId = TheirColony.Id };

            agent.AssignMission(AgentMission.Undercover, Player, Enemy.data.Traits.Name);

            Assert.IsTrue(Player.data.MoleList.Contains(OurMole), "the mole planted by the infiltration must survive the status change");
        }

        [TestMethod]
        public void AnAgentLeavingUndercoverTakesItsMoleWithIt()
        {
            PlantAMoleOnTheirColony();
            var agent = new Agent { Mission = AgentMission.Undercover, TargetPlanetId = TheirColony.Id };

            agent.AssignMission(AgentMission.Defending, Player, "");

            Assert.IsFalse(Player.data.MoleList.Contains(OurMole), "reassigning an undercover agent withdraws its mole");
            AssertEqual(0, agent.TargetPlanetId, "and forgets the planet, so a later loss of that colony cannot recall it");
        }

        [TestMethod]
        public void AnUndercoverAgentComesHomeWhenItsColonyChangesHands()
        {
            PlantAMoleOnTheirColony();
            var agent = new Agent { Mission = AgentMission.Undercover, TargetPlanetId = TheirColony.Id };
            Player.data.AgentList.Add(agent);

            TheirColony.SetOwner(Player);

            Assert.IsFalse(Player.data.MoleList.Contains(OurMole), "the mole goes with the colony");
            Assert.AreEqual(AgentMission.Defending, agent.Mission, "the agent behind it comes home to defend");
        }

        [TestMethod]
        public void AMoleWhoseAgentRetiredGoesQuietlyWhenItsColonyChangesHands()
        {
            PlantAMoleOnTheirColony();
            Assert.IsFalse(Player.data.AgentList.Any(a => a.TargetPlanetId == TheirColony.Id),
                "setup: the agent that planted the mole has retired, so no agent is behind it");

            TheirColony.SetOwner(Player);

            Assert.IsFalse(Player.data.MoleList.Contains(OurMole), "the mole goes with the colony");
        }
    }
}
