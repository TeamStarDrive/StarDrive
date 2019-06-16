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
    public class TestNotifications : StarDriveTest, IDisposable
    {
        readonly GameDummy Game;
        NotificationManager NotificationManager;

        public TestNotifications()
        {
            ResourceManager.LoadBasicContentForTesting();
            // UniverseScreen requires a game instance
            Game = new GameDummy();
            Game.Create();
        }

        public void Dispose()
        {
            Empire.Universe?.ExitScreen();
            Game.Dispose();
        }

        void CreateTestEnv(out Empire empire)
        {
            var data = new UniverseData();
            empire = data.CreateEmpire(ResourceManager.MajorRaces[0]);
            empire.isPlayer = true;
            Empire.Universe = new UniverseScreen(data, empire);
            Planet p = new Planet();
            var s = new SolarSystem();
            s.PlanetList.Add(p);
            p.ParentSystem = s;
            empire.AddPlanet(p);
            p.Type = ResourceManager.PlanetOrRandom(0);
            NotificationManager = new NotificationManager(Empire.Universe.ScreenManager, Empire.Universe);
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
            Assert.That.Equal(12, NotificationManager.NumberOfNotifications);
            NotificationManager.Update(10);
            Assert.That.Equal(11, NotificationManager.NumberOfNotifications);
            NotificationManager.Update(10);
            Assert.That.Equal(10, NotificationManager.NumberOfNotifications);
            NotificationManager.Update(10);
            NotificationManager.Update(10);
            NotificationManager.Update(10);
            Assert.That.Equal(7, NotificationManager.NumberOfNotifications);



        }
    }
}
