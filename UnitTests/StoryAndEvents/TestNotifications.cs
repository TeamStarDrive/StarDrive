using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.GameScreens.LoadGame;
using UnitTests.UI;

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

        static ExplorationEvent StoryEvent(params string[] outcomeTexts)
        {
            var e = new ExplorationEvent { Name = "Remnant Portal", PotentialOutcomes = new() };
            foreach (string text in outcomeTexts)
            {
                e.PotentialOutcomes.Add(new Outcome
                {
                    Chance = 100, DescriptionText = text,
                    TroopsToSpawn = new(), FriendlyShipsToSpawn = new(), PirateShipsToSpawn = new(), RemnantShipsToSpawn = new()
                });
            }
            return e;
        }

        [TestMethod]
        public void TestRemnantStoryUpdateLogsTheStoryText()
        {
            ExplorationEvent storyEvent = StoryEvent("We suspect that there is a new remnant portal out there.");
            NotifMgr.AddRemnantUpdateNotify(storyEvent, storyEvent.PotentialOutcomes[0], Enemy);

            ImportantNotification[] events = UState.GetImportantEvents();
            AssertEqual(1, events.Length);
            AssertEqual("Remnant Story", events[0].Title);
            AssertEqual("Remnant Portal: We suspect that there is a new remnant portal out there.", events[0].Message);
        }

        [TestMethod]
        public void TestRemnantStoryLogShowsTheRolledOutcome()
        {
            ExplorationEvent storyEvent = StoryEvent("First outcome.", "Second outcome.");
            NotifMgr.AddRemnantUpdateNotify(storyEvent, storyEvent.PotentialOutcomes[1], Enemy);

            AssertEqual("Remnant Portal: Second outcome.", UState.GetImportantEvents()[0].Message);
        }

        [TestMethod]
        public void TestRemnantStoryOutcomeAppliesWhenRaisedAndTheClickOnlyShowsIt()
        {
            CreateAMinorFaction("Remnant");
            Remnants remnants = Faction.Remnants;
            Universe.NotificationManager = NotifMgr;
            Player.SetCapital(Player.GetPlanets()[0]); // ship grants spawn near the capital

            ExplorationEvent storyEvent = StoryEvent("The Remnants left credits in their wreckage.");
            storyEvent.Story = remnants.Story;
            storyEvent.StoryStep = remnants.StoryStep;
            storyEvent.PotentialOutcomes[0].MoneyGranted = 1234;

            var savedEvents = ResourceManager.EventsDict.ToArray();
            EventPopup popup = null;
            try
            {
                ResourceManager.EventsDict.Clear();
                ResourceManager.EventsDict["UnitTest.RemnantStory"] = storyEvent;
                typeof(Remnants).GetProperty(nameof(Remnants.Activated))!.SetValue(remnants, true);
                typeof(Remnants).GetProperty(nameof(Remnants.PlayerStepTriggerXp))!.SetValue(remnants, remnants.StepXpTrigger);

                float moneyBefore = Player.Money;
                remnants.IncrementKillsForStory(Player, 1);

                AssertEqual(1f, 1234f, Player.Money - moneyBefore, "the outcome applies when the event is raised, so a reload cannot lose it");
                AssertEqual("Remnant Portal: The Remnants left credits in their wreckage.", UState.GetImportantEvents()[0].Message);

                var mouse = new MockInputProvider { MousePos = new Vector2(GameBase.ScreenWidth - 40, 100) };
                var input = new InputState { Provider = mouse };
                mouse.LeftMouse = SDGraphics.Input.ButtonState.Pressed;
                input.Update(new UpdateTimes(1 / 60f, 0f));
                mouse.LeftMouse = SDGraphics.Input.ButtonState.Released;
                input.Update(new UpdateTimes(1 / 60f, 0f));
                Assert.IsTrue(NotifMgr.HandleInput(input), "setup: the click must reach the story notification");
                Game.Tick(); // screens added by AddScreen join on the next frame

                popup = Universe.ScreenManager.FindScreen<EventPopup>();
                Assert.IsNotNull(popup, "the click opens the event window");
                Assert.AreSame(storyEvent, popup.ExpEvent);
                AssertEqual(1f, 1234f, Player.Money - moneyBefore, "opening the window does not grant the outcome again");
            }
            finally
            {
                if (popup != null)
                    Universe.ScreenManager.RemoveScreen(popup);
                ResourceManager.EventsDict.Clear();
                foreach (var kv in savedEvents)
                    ResourceManager.EventsDict[kv.Key] = kv.Value;
            }
        }

        [TestMethod]
        public void TestOnlyRemnantsLeftOutcomeAppliesWhenRaised()
        {
            CreateAMinorFaction("Remnant");
            Remnants remnants = Faction.Remnants;
            Universe.NotificationManager = NotifMgr;
            Player.SetCapital(Player.GetPlanets()[0]);
            typeof(Remnants).GetProperty(nameof(Remnants.Story))!.SetValue(remnants, Remnants.RemnantStory.AncientHelpers);

            ExplorationEvent storyEvent = StoryEvent("The Remnants withdraw and leave their treasury behind.");
            storyEvent.Story = Remnants.RemnantStory.AncientHelpers;
            storyEvent.TriggerWhenOnlyRemnantsLeft = true;
            storyEvent.PotentialOutcomes[0].MoneyGranted = 4321;

            var savedEvents = ResourceManager.EventsDict.ToArray();
            try
            {
                ResourceManager.EventsDict.Clear();
                ResourceManager.EventsDict["UnitTest.RemnantStoryEnd"] = storyEvent;

                float moneyBefore = Player.Money;
                remnants.TriggerOnlyRemnantsLeftEvent();

                AssertEqual(1f, 4321f, Player.Money - moneyBefore, "the outcome applies when the event is raised");
                AssertEqual("Remnant Portal: The Remnants withdraw and leave their treasury behind.",
                            UState.GetImportantEvents()[0].Message);
            }
            finally
            {
                ResourceManager.EventsDict.Clear();
                foreach (var kv in savedEvents)
                    ResourceManager.EventsDict[kv.Key] = kv.Value;
            }
        }

        [TestMethod]
        public void TestRemnantScanAndWarnMessagesAreLogged()
        {
            NotifMgr.AddRemnantAbleToScanOrWarn(Enemy, GameText.CanScanRemnantsEvent);
            NotifMgr.AddRemnantAbleToScanOrWarn(Enemy, GameText.CanWarnRemnantsEvent);

            ImportantNotification[] events = UState.GetImportantEvents();
            AssertEqual(2, events.Length);
            AssertEqual("Remnant Story", events[0].Title);
            AssertEqual(Localizer.Token(GameText.CanScanRemnantsEvent), events[0].Message);
            AssertEqual("Remnant Story", events[1].Title);
            AssertEqual(Localizer.Token(GameText.CanWarnRemnantsEvent), events[1].Message);
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
