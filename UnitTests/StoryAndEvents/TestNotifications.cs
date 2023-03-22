using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;

namespace UnitTests.NotificationTests
{
    [TestClass]
    public class TestNotifications : StarDriveTest
    {
        NotificationManager NotifMgr;

        public TestNotifications()
        {
            CreateUniverseAndPlayerEmpire();
            AddDummyPlanetToEmpire(new Vector2(2000), Player);
            NotifMgr = new NotificationManager(Universe.ScreenManager, Universe);
        }

        /// <summary>
        /// Add 12 notifications. 4 spy, 4 planet, 4, 4 spy
        /// </summary>
        /// <param name="empire"></param>
        public void AddNotifications(Empire empire)
        {
            NotifMgr.AddAgentResult(true, "AgentTest", empire);
            NotifMgr.AddAgentResult(true, "AgentTest", empire);
            NotifMgr.AddAgentResult(true, "AgentTest", empire);
            NotifMgr.AddAgentResult(true, "AgentTest", empire);

            var planet = empire.GetPlanets().First();
            NotifMgr.AddPlanetDiedNotification(planet);
            NotifMgr.AddPlanetDiedNotification(planet);
            NotifMgr.AddPlanetDiedNotification(planet);
            NotifMgr.AddPlanetDiedNotification(planet);

            NotifMgr.AddAgentResult(true, "AgentTest", empire);
            NotifMgr.AddAgentResult(true, "AgentTest", empire);
            NotifMgr.AddAgentResult(true, "AgentTest", empire);
            NotifMgr.AddAgentResult(true, "AgentTest", empire);
        }

        [TestMethod]
        public void TestRemoveTooManyNotifications()
        {
            AddNotifications(Player);
            AssertEqual(12, NotifMgr.NumberOfNotifications);
            NotifMgr.Update(10f);
            AssertEqual(11, NotifMgr.NumberOfNotifications);
            NotifMgr.Update(10f);
            AssertEqual(10, NotifMgr.NumberOfNotifications);
            NotifMgr.Update(10f);
            NotifMgr.Update(10f);
            NotifMgr.Update(10f);
            AssertEqual(7, NotifMgr.NumberOfNotifications);
        }
    }
}
