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

        void CreateLegacyEspionageUniverse()
        {
            CreateUniverseAndPlayerEmpire();
            Player.Universe.P.UseLegacyEspionage = true;
            Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        }

        void PlantAMoleOnTheirColony()
        {
            CreateLegacyEspionageUniverse();
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

        [TestMethod]
        public void AnInjuredAgentSitsOutItsRecoveryWithRepeatMissionsOff()
        {
            CreateLegacyEspionageUniverse();
            Player.data.SpyMissionRepeat = false;

            // the roll cannot be fixed, so training runs until one injures its agent
            for (int attempt = 0; attempt < 1000; ++attempt)
            {
                var agent = new Agent { Mission = AgentMission.Training, PrevisousMission = AgentMission.Defending, TurnsRemaining = 1 };
                Player.data.AgentList.Add(agent);
                agent.Update(Player);
                if (agent.PrevisousMission != AgentMission.Training) // only an injury records the mission it was on
                {
                    Player.data.AgentList.Remove(agent);
                    continue;
                }

                Assert.AreEqual(AgentMission.Recovering, agent.Mission, "an injured agent recovers instead of defending at once");
                Assert.AreEqual(20, agent.TurnsRemaining, "for the recovery's turns");

                for (int turn = 0; turn < 20; ++turn)
                    agent.Update(Player);
                Assert.AreEqual(AgentMission.Defending, agent.Mission, "and defends once it has recovered");
                return;
            }
            Assert.Fail("setup: no training out of 1000 injured its agent");
        }

        [TestMethod]
        public void AMoleGivesTheVictimsDefendersNoExperience()
        {
            CreateLegacyEspionageUniverse();
            var defender = new Agent { Mission = AgentMission.Defending };
            Enemy.data.AgentList.Add(defender);
            var mole = new Agent { Mission = AgentMission.Undercover, TargetEmpire = Enemy.data.Traits.Name };
            Player.data.AgentList.Add(mole);

            for (int turn = 0; turn < 3; ++turn)
                mole.Update(Player);

            Assert.AreEqual(0, defender.Experience, "an undercover agent runs no mission, so it gives their defenders nothing to learn from");
            Assert.AreEqual(1, defender.Level, "and no level");
            Assert.AreEqual(AgentMission.Undercover, mole.Mission, "it stays undercover while its target stands");
        }

        [TestMethod]
        public void AMoleComesHomeWhenItsTargetIsDefeated()
        {
            PlantAMoleOnTheirColony();
            var agent = new Agent { Mission = AgentMission.Undercover, TargetPlanetId = TheirColony.Id, TargetEmpire = Enemy.data.Traits.Name };
            Player.data.AgentList.Add(agent);
            Enemy.SetAsDefeated();
            Assert.AreEqual(AgentMission.Undercover, agent.Mission, "setup: the defeat alone leaves the agent undercover");
            Assert.IsTrue(Player.data.MoleList.Contains(OurMole), "setup: and its mole in place");

            agent.Update(Player);

            Assert.AreEqual(AgentMission.Defending, agent.Mission, "the agent comes home to defend");
            Assert.IsFalse(Player.data.MoleList.Contains(OurMole), "and takes its mole with it");
        }

        [TestMethod]
        public void ARepeatTheTreasuryCannotPayForLeavesTheAgentDefending()
        {
            CreateLegacyEspionageUniverse();
            Player.data.SpyMissionRepeat = true;
            Player.data.OffensiveSpyBonus = 20; // every training roll then clears the injury bands
            Player.Money = 0f;
            var agent = new Agent { Mission = AgentMission.Training, TurnsRemaining = 1 };
            Player.data.AgentList.Add(agent);

            agent.Update(Player);
            Assert.AreEqual(AgentMission.Defending, agent.Mission, "a training the treasury cannot pay for again is not repeated");

            short trainings = agent.Training;
            agent.Update(Player);
            Assert.AreEqual(trainings, agent.Training, "and no free training runs the next turn");
        }
    }
}
