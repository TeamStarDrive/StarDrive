using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.NotificationTests
{
    [TestClass]
    public class TestNotifications : StarDriveTest
    {
        NotificationManager NotificationManager;

        public TestNotifications()
        {
            LoadPlanetContent();
            CreateGameInstance();
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
            NotificationManager.AddAgentResultNotification(true, "AgentTest", empire);
            NotificationManager.AddAgentResultNotification(true, "AgentTest", empire);
            NotificationManager.AddAgentResultNotification(true, "AgentTest", empire);
            NotificationManager.AddAgentResultNotification(true, "AgentTest", empire);

            var planet = empire.GetPlanets().First();
            NotificationManager.AddPlanetDiedNotification(planet, empire);
            NotificationManager.AddPlanetDiedNotification(planet, empire);
            NotificationManager.AddPlanetDiedNotification(planet, empire);
            NotificationManager.AddPlanetDiedNotification(planet, empire);

            NotificationManager.AddAgentResultNotification(true, "AgentTest", empire);
            NotificationManager.AddAgentResultNotification(true, "AgentTest", empire);
            NotificationManager.AddAgentResultNotification(true, "AgentTest", empire);
            NotificationManager.AddAgentResultNotification(true, "AgentTest", empire);
        }

        [TestMethod]
        public void TestRemoveTooManyNotifications()
        {
            CreateTestEnv(out Empire empire);
            AddNotifications(empire);
            Assert.AreEqual(12, NotificationManager.NumberOfNotifications);
            NotificationManager.Update(10);
            Assert.AreEqual(11, NotificationManager.NumberOfNotifications);
            NotificationManager.Update(10);
            Assert.AreEqual(10, NotificationManager.NumberOfNotifications);
            NotificationManager.Update(10);
            NotificationManager.Update(10);
            NotificationManager.Update(10);
            Assert.AreEqual(7, NotificationManager.NumberOfNotifications);
        }
    }
}
