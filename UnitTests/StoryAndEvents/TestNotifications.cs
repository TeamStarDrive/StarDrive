using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;

namespace UnitTests.NotificationTests
{
    [TestClass]
    public class TestNotifications : StarDriveTest
    {
        NotificationManager NotificationManager;

        public TestNotifications()
        {
            CreateGameInstance();
            LoadPlanetContent();
        }

        void CreateTestEnv(out Empire empire)
        {
            CreateUniverseAndPlayerEmpire(out empire);
            AddDummyPlanetToEmpire(empire);
            NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        }

        /// <summary>
        /// Add 12 notifications. 4 spy, 4 planet, 4, 4 spy
        /// </summary>
        /// <param name="empire"></param>
        public void AddNotifications(Empire empire)
        {
            NotificationManager.AddAgentResult(true, "AgentTest", empire);
            NotificationManager.AddAgentResult(true, "AgentTest", empire);
            NotificationManager.AddAgentResult(true, "AgentTest", empire);
            NotificationManager.AddAgentResult(true, "AgentTest", empire);

            var planet = empire.GetPlanets().First();
            NotificationManager.AddPlanetDiedNotification(planet);
            NotificationManager.AddPlanetDiedNotification(planet);
            NotificationManager.AddPlanetDiedNotification(planet);
            NotificationManager.AddPlanetDiedNotification(planet);

            NotificationManager.AddAgentResult(true, "AgentTest", empire);
            NotificationManager.AddAgentResult(true, "AgentTest", empire);
            NotificationManager.AddAgentResult(true, "AgentTest", empire);
            NotificationManager.AddAgentResult(true, "AgentTest", empire);
        }

        [TestMethod]
        public void TestRemoveTooManyNotifications()
        {
            CreateTestEnv(out Empire empire);
            AddNotifications(empire);
            Assert.AreEqual(12, NotificationManager.NumberOfNotifications);
            NotificationManager.Update(10f);
            Assert.AreEqual(11, NotificationManager.NumberOfNotifications);
            NotificationManager.Update(10f);
            Assert.AreEqual(10, NotificationManager.NumberOfNotifications);
            NotificationManager.Update(10f);
            NotificationManager.Update(10f);
            NotificationManager.Update(10f);
            Assert.AreEqual(7, NotificationManager.NumberOfNotifications);
        }
    }
}
