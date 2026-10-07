using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using UnitTests.UI;

namespace UnitTests.NotificationTests
{
    [TestClass]
    public class OwlwokFreedomEventTests : StarDriveTest
    {
        [TestMethod]
        public void TheFirstCordrazineCaptureOpensTheResearchPathAtOnceAndTheClickOnlyShowsIt()
        {
            CreateUniverseAndPlayerEmpire("Human", enemyArchetype: "Cordrazine");
            var notifications = new NotificationManager(Universe.ScreenManager, Universe);
            Universe.NotificationManager = notifications;
            Assert.AreSame(Enemy, UState.Cordrazine, "setup: the enemy is the Cordrazine");
            Player.SetCapital(AddDummyPlanetToEmpire(new Vector2(-2000), Player)); // ship grants spawn near the capital
            Planet planet = AddDummyPlanetToEmpire(new Vector2(2000), Enemy);
            TechEntry owlwokFreedom = Player.GetTechEntry("Owlwok Freedom");
            Assert.IsFalse(owlwokFreedom.Discovered, "setup");

            EventPopup popup = null;
            try
            {
                planet.SetOwner(Player, attacker: Player);

                Assert.IsTrue(owlwokFreedom.Discovered, "the research path opens on the capture, so a reload before the click cannot lose it");
                AssertEqual(1, notifications.NumberOfNotifications, "setup: only the event notification");

                var mouse = new MockInputProvider { MousePos = new Vector2(GameBase.ScreenWidth - 40, 100) };
                var input = new InputState { Provider = mouse };
                mouse.LeftMouse = SDGraphics.Input.ButtonState.Pressed;
                input.Update(new UpdateTimes(1 / 60f, 0f));
                mouse.LeftMouse = SDGraphics.Input.ButtonState.Released;
                input.Update(new UpdateTimes(1 / 60f, 0f));
                Assert.IsTrue(notifications.HandleInput(input), "setup: the click must reach the event notification");
                Game.Tick();

                popup = Universe.ScreenManager.FindScreen<EventPopup>();
                Assert.IsNotNull(popup, "the click opens the event window");
                AssertEqual($"Owlwok Freedom at {planet.Name}", popup.TitleText);
                Assert.IsFalse(notifications.IsNotificationPresent(popup.TitleText), "showing the event again does not report an anomaly");
                Assert.IsTrue(owlwokFreedom.Discovered);
            }
            finally
            {
                if (popup != null)
                    Universe.ScreenManager.RemoveScreen(popup);
            }
        }
    }
}
