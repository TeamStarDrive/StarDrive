using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.GameScreens.LoadGame;

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
            NotifMgr.MaxEntriesToDisplay = 7;
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

        [TestMethod]
        public void TestImportantNotificationIsLogged()
        {
            NotifMgr.AddEmpireDiedNotification(Enemy);
            ImportantNotification[] events = UState.GetImportantEvents();
            AssertEqual(1, events.Length);
            AssertEqual("Empire Defeated", events[0].Title);
            AssertEqual(UState.StarDate, events[0].StarDate);
            Assert.AreSame(Enemy, events[0].RelevantEmpire);
            Assert.IsTrue(events[0].Message.Contains(Enemy.data.Traits.Name));
        }

        [TestMethod]
        public void TestRegularNotificationsAreNotLogged()
        {
            AddNotifications(Player); // 12 regular notifications, none of them important
            AssertEqual(12, NotifMgr.NumberOfNotifications);
            AssertEqual(0, UState.GetImportantEvents().Length);
        }

        [TestMethod]
        public void TestImportantLogMessageOverridesUiMessage()
        {
            NotifMgr.AddNotification(new Notification
            {
                Important  = true,
                Title      = "Test Title",
                Message    = "Log worthy text\nClick for more info",
                LogMessage = "Log worthy text"
            });

            ImportantNotification[] events = UState.GetImportantEvents();
            AssertEqual(1, events.Length);
            AssertEqual("Log worthy text", events[0].Message);
        }

        [TestMethod]
        public void TestImportantEventsAreLoggedInOrder()
        {
            NotifMgr.AddEmpireDiedNotification(Enemy);
            NotifMgr.AddSurrendered(Player, Enemy);
            ImportantNotification[] events = UState.GetImportantEvents();
            AssertEqual(2, events.Length);
            AssertEqual("Empire Defeated", events[0].Title);
            AssertEqual("Empire Surrendered", events[1].Title);
        }

        [TestMethod]
        public void TestImportantEventsSurviveSaveLoad()
        {
            // advance beyond 1000 so the loaded universe skips CreateStartingShips,
            // which requires every major empire to own a planet (test Enemy has none).
            // also proves a non-default StarDate round-trips with the event.
            UState.StarDate = 1042.5f;
            NotifMgr.AddEmpireDiedNotification(Enemy);
            float starDate = UState.StarDate;

            SavedGame save = Universe.Save("UnitTest.ImportantEvents", throwOnError: true);
            UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);

            ImportantNotification[] events = loaded.UState.GetImportantEvents();
            AssertEqual(1, events.Length);
            AssertEqual("Empire Defeated", events[0].Title);
            AssertEqual(starDate, events[0].StarDate);
            AssertEqual(Enemy.data.Traits.Name, events[0].RelevantEmpire.data.Traits.Name);
        }
    }
}
