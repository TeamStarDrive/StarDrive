using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class NotificationManager: IDisposable
    {
        private readonly ScreenManager ScreenManager;
        private readonly UniverseScreen Screen;
        private readonly int MaxEntriesToDisplay;
        private Rectangle NotificationArea;

        private static readonly object NotificationLocker = new object();
        private BatchRemovalCollection<Notification> NotificationList = 
            new BatchRemovalCollection<Notification>();
        private float Timer;
        public bool HitTest => NotificationArea.HitTest(Screen.Input.CursorPosition);
        public NotificationManager(ScreenManager screenManager, UniverseScreen screen)
        {
            Screen        = screen;
            ScreenManager = screenManager;

            var presentParams = screenManager.GraphicsDevice.PresentationParameters;
            NotificationArea  = new Rectangle(presentParams.BackBufferWidth - 70, 70, 70, 
                                             presentParams.BackBufferHeight - 70 - 275);
            MaxEntriesToDisplay = NotificationArea.Height / 70;
        }

        private Rectangle GetNotificationRect(int index)
        {
            return new Rectangle(NotificationArea.X,
                NotificationArea.Y + NotificationArea.Height - (index+1) * 70, 64, 64);
        }
        private Rectangle DefaultNotificationRect => GetNotificationRect(NotificationList.Count);
        private Rectangle DefaultClickRect => new Rectangle(NotificationArea.X, NotificationArea.Y, 64, 64);

        public void AddNotification(Notification notify, params string[] soundCueStrings)
        {
            notify.ClickRect = DefaultClickRect;
            notify.DestinationRect = DefaultNotificationRect;
            
            foreach (string cue in soundCueStrings)
                GameAudio.PlaySfxAsync(cue);

            lock (NotificationLocker)
                NotificationList.Add(notify);
        }

        public void AddAgentResultNotification(bool good, string result, Empire owner)
        {
            if (owner != EmpireManager.Player)
                return;

            AddNotification(new Notification
            {
                Message = result,
                IconPath = good ? "NewUI/icon_spy_notification" : "NewUI/icon_spy_notification_bad"
            }, good ? "sd_ui_spy_win_02" : "sd_ui_spy_fail_02");
        }

        public void AddBeingInvadedNotification(SolarSystem beingInvaded, Empire invader)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = invader,
                Message         = invader.data.Traits.Singular + Localizer.Token(1500) + '\n' + Localizer.Token(1501) + beingInvaded.Name + Localizer.Token(1502),
                ReferencedItem1 = beingInvaded,
                IconPath        = "NewUI/icon_planet_terran_01_mid",
                Action          = "SnapToSystem"
            }, "sd_notify_alert");
        }

        public void AddColonizedNotification(Planet wasColonized, Empire emp)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = emp,
                Message         = wasColonized.Name + Localizer.Token(1513),
                ReferencedItem1 = wasColonized,
                IconPath        = "Planets/" + wasColonized.PlanetType,
                Action          = "SnapToPlanet"
            }, "sd_ui_notification_colonized_01");
        }

        public void AddConqueredNotification(Planet wasConquered, Empire conquerer, Empire loser)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = conquerer,
                Message         = conquerer.data.Traits.Name + Localizer.Token(1503) + wasConquered.Name + "\n" + Localizer.Token(1504) + loser.data.Traits.Name,
                ReferencedItem1 = wasConquered.ParentSystem,
                IconPath        = "Planets/" + wasConquered.PlanetType,
                Action          = "SnapToSystem"
            }, "sd_troop_march_01");
        }

        public void AddEmpireDiedNotification(Empire thatDied)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = thatDied,
                Message         = thatDied.data.Traits.Name + " has been defeated",
                IconPath        = "NewUI/icon_planet_terran_01_mid",
                ClickRect       = DefaultClickRect,
                DestinationRect = DefaultNotificationRect
            }, "sd_troop_march_01");
        }

        public void AddEnemyTroopsLandedNotification(Planet where, Empire invader, Empire player)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = invader,
                Message         = invader.data.Traits.Singular + Localizer.Token(1507) + where.Name + "!",
                ReferencedItem1 = where,
                IconPath        = "Planets/" + where.PlanetType,
                Action          = "CombatScreen"
            }, "sd_notify_alert", "sd_troop_march_01");
        }

        public void AddForeignTroopsRemovedNotification(Planet where)
        {
            AddNotification(new Notification
            {
                Message         = "Foreign troops evacuated from " + where.Name,
                ReferencedItem1 = where,
                IconPath        = "Planets/" + where.PlanetType,
                Action          = "SnapToPlanet"
            }, "sd_notify_alert");
        }

        public void AddTroopsRemovedNotification(Planet where)
        {
            AddNotification(new Notification
            {
                Message         = "Your troops stationed on " + where.Name + " had to evacuate when " + where.Owner.data.Traits.Name + " colonized the planet",
                ReferencedItem1 = where,
                IconPath        = "Planets/" + where.PlanetType,
                Action          = "SnapToPlanet"
            }, "sd_notify_alert");
        }

        public void AddNotify(ExplorationEvent expEvent)
        {
            AddNotification(new Notification
            {
                Pause           = false,
                Message         = Localizer.Token(2295),
                ReferencedItem1 = expEvent,
                IconPath        = "ResearchMenu/icon_event_science",
                Action          = "LoadEvent"
            }, "sd_ui_notification_encounter");
        }

        public void AddNotify(ExplorationEvent expEvent, string cMessage)
        {
            AddNotification(new Notification
            {
                Pause           = false,
                Message         = cMessage,
                ReferencedItem1 = expEvent,
                IconPath        = "ResearchMenu/icon_event_science",
                Action          = "LoadEvent"
            }, "sd_ui_notification_encounter");
        }

        public void AddNotify(Technology.TriggeredEvent techEvent, string message)
        {
            AddNotify(ResourceManager.EventsDict[techEvent.EventUID], message);
        }
        public void AddNotify(Technology.TriggeredEvent techEvent)
        {
            AddNotify(ResourceManager.EventsDict[techEvent.EventUID]);
        }

        public void AddFoundSomethingInteresting(Planet p)
        {
            AddNotification(new Notification
            {
                Pause           = false,
                Message         = Localizer.Token(1505) + p.Name + Localizer.Token(1506),
                ReferencedItem1 = p.ParentSystem,
                ReferencedItem2 = p,
                IconPath        = "Planets/" + p.PlanetType,
                Action          = "SnapToExpandSystem"
            }, "sd_ui_notification_encounter");
        }

        public void AddMolePlantedNotification(Planet wasConquered, Empire us)
        {
            AddNotification(new Notification
            {
                Pause           = false,
                RelevantEmpire  = us,
                Message         = Localizer.Token(1510) + wasConquered.Name,
                ReferencedItem1 = wasConquered,
                IconPath        = "Planets/" + wasConquered.PlanetType,
                Action          = "SnapToPlanet"
            }, "sd_troop_march_01");
        }

        public void AddMoleRemovedNotification(Planet wasConquered, Empire us, Empire them)
        {
            AddNotification(new Notification
            {
                Pause           = false,
                RelevantEmpire  = us,
                Message         = "Removed " + them.data.Traits.Singular + " agent from " + wasConquered.Name,
                ReferencedItem1 = wasConquered,
                IconPath        = "Planets/" + wasConquered.PlanetType,
                Action          = "SnapToPlanet"
            }, "sd_troop_march_01");
        }

        public void AddMoneyWarning()
        {
            AddNotification(new Notification
            {
                Pause    = false,
                Message  = Localizer.Token(2296),
                IconPath = "UI/icon_warning_money"
            }, "sd_ui_notification_warning", "sd_trade_01");
        }

        public void AddNoMolesNotification(Empire us, Empire them)
        {
            AddNotification(new Notification
            {
                Pause          = false,
                RelevantEmpire = us,
                Message        = Localizer.Token(1508) + them.data.Traits.Singular + Localizer.Token(1509),
                IconPath       = "NewUI/icon_planet_terran_01_mid"
            }, "sd_troop_march_01");
        }

        public void AddPeacefulMergerNotification(Empire absorber, Empire target)
        {
            AddNotification(new Notification
            {
                RelevantEmpire = absorber,
                Message        = absorber.data.Traits.Name + " " + Localizer.Token(2258) + target.data.Traits.Name,
                IconPath       = "NewUI/icon_planet_terran_01_mid"
            }, "sd_troop_march_01");
        }

        public void AddPeaceTreatyEnteredNotification(Empire first, Empire second)
        {
            AddNotification(new Notification
            {
                Pause    = false,
                Message  = first.data.Traits.Name + " and " + second.data.Traits.Name + "\nare now at peace",
                IconPath = "UI/icon_peace"
            }, "sd_ui_notification_conquer_01");
        }

        public void AddPeaceTreatyExpiredNotification(Empire otherEmpire)
        {
            AddNotification(new Notification
            {
                Pause    = false,
                Message  = "Peace Treaty expired with \n" + otherEmpire.data.Traits.Name,
                IconPath = "UI/icon_peace_cancel"
            }, "sd_ui_notification_warning");
        }

        public void AddPlanetDiedNotification(Planet died, Empire owner)
        {
            AddNotification(new Notification
            {
                Message         = Localizer.Token(1511) + died.Name + Localizer.Token(1512),
                ReferencedItem1 = died.ParentSystem,
                IconPath        = "Planets/" + died.PlanetType,
                Action          = "SnapToSystem"
            }, "sd_ui_notification_warning");
        }

        public void AddRandomEventNotification(string message, string iconPath, string action, Planet p)
        {
            AddNotification(new Notification
            {
                Message         = message,
                Action          = action,
                ReferencedItem1 = p,
                IconPath        = iconPath ?? "ResearchMenu/icon_event_science_bad"
            }, "sd_ui_notification_encounter");
        }

        public void AddRebellionNotification(Planet beingInvaded, Empire invader)
        {
            AddNotification(new Notification
            {
                Message         = "Rebellion on " + beingInvaded.Name + "!",
                ReferencedItem1 = beingInvaded.ParentSystem,
                IconPath        = "UI/icon_rebellion",
                Action          = "SnapToSystem"
            }, "sd_troop_march_01", "sd_notify_alert");
        }

        public void AddResearchComplete(string unlocked, Empire emp)
        {
            // Techs using Icon Path need this for notifications
            string techIcon = "TechIcons/" + ResourceManager.TechTree[unlocked].IconPath;
            bool hasTechIcon = ResourceManager.TextureLoaded(techIcon);

            AddNotification(new Notification
            {
                Tech            = true,
                Message         = Localizer.Token(ResourceManager.TechTree[unlocked].NameIndex) + Localizer.Token(1514),
                ReferencedItem1 = unlocked,
                IconPath        = hasTechIcon ? techIcon : "TechIcons/" + unlocked,
                Action          = "ResearchScreen"
            }, "sd_ui_notification_research_01");
        }

        public void AddSurrendered(Empire absorber, Empire target)
        {
            AddNotification(new Notification
            {
                RelevantEmpire = absorber,
                Message        = target.data.Traits.Name + " " + Localizer.Token(2259) + absorber.data.Traits.Name,
                IconPath       = "NewUI/icon_planet_terran_01_mid"
            }, "sd_troop_march_01");
        }

        public void AddWarDeclaredNotification(Empire declarant, Empire other)
        {
            AddNotification(new Notification
            {
                Message  = declarant.data.Traits.Name + " and " + other.data.Traits.Name + "\nare now at war",
                IconPath = "ResearchMenu/icons_techroot_infantry_hover",
                Pause    = declarant.isPlayer || other.isPlayer
            }, "sd_troop_march_01", "sd_notify_alert");
        }

        public void AddWarStartedNotification(Empire first, Empire second)
        {
            AddNotification(new Notification
            {
                Message  = first.data.Traits.Name + " and " + second.data.Traits.Name + "\nare now at War",
                IconPath = "UI/icon_warning_money",
                Pause    = first.isPlayer || second.isPlayer
            }, "sd_ui_notification_startgame");
        }

        public void AddEmptyQueueNotification(Planet planet)
        {
            AddNotification(new Notification
            {
                Pause           = false,
                RelevantEmpire  = planet.Owner,
                Message         = planet.Name + " is not producing anything.",
                ReferencedItem1 = this, //this.system,
                IconPath        = "Planets/" + planet.PlanetType, //"UI/icon_warning_money",
                Action          = "SnapToPlanet" //"SnapToSystem",
            }, "sd_ui_notification_warning");
        }

        private void UpdateAllPositions()
        {
            for (int i = 0; i < NotificationList.Count; i++)
            {
                Notification n          = NotificationList[i];
                n.DestinationRect       = GetNotificationRect(i);
                n.ClickRect.X           = NotificationArea.X;
                n.transitionElapsedTime = 0f;
            }
        }

        public void Draw()
        {
            lock (NotificationLocker)
            {
                if (NotificationList.Count >= MaxEntriesToDisplay)  //fbedard: remove excess notifications
                {
                    for (int i = 0; i < NotificationList.Count; i++)
                    {
                        Notification n = NotificationList[i];
                        if (n.Action == "LoadEvent" || n.Pause) continue;
                        NotificationList.QueuePendingRemoval(n);
                        break;
                    }
                    NotificationList.ApplyPendingRemovals();
                    UpdateAllPositions();
                }

                for (int i = 0; i < NotificationList.Count && i <= MaxEntriesToDisplay; i++)
                {
                    Notification n = NotificationList[i];
                    if (n.IconPath != null)
                    {
                        SubTexture iconTex = ResourceManager.Texture(n.IconPath);
                        if (!n.Tech)
                        {
                            ScreenManager.SpriteBatch.Draw(iconTex, n.ClickRect, Color.White);
                        }
                        else
                        {
                            Rectangle rect = n.ClickRect;
                            if (rect.X == 0)
                                continue;

                            rect.X = n.ClickRect.X + n.ClickRect.Width / 2 - iconTex.Width / 2;
                            rect.Y = n.ClickRect.Y + n.ClickRect.Height / 2 - iconTex.Height / 2;

                            rect.Width = iconTex.Width;
                            rect.Height = iconTex.Height;
                            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("TechIcons/techbg"), rect, Color.White);
                            ScreenManager.SpriteBatch.Draw(iconTex, rect, Color.White);
                            ScreenManager.SpriteBatch.DrawRectangle(rect, new Color(32, 30, 18));
                        }
                    }
                    if (n.RelevantEmpire != null)
                    {
                        SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
                        KeyValuePair<string, Texture2D> item = ResourceManager.FlagTextures[n.RelevantEmpire.data.Traits.FlagIndex];
                        spriteBatch.Draw(item.Value, n.ClickRect, n.RelevantEmpire.EmpireColor);
                    }
                    if (n.ShowMessage)
                    {
                        Vector2 msgSize = Fonts.Arial12Bold.MeasureString(n.Message);
                        Vector2 cursor = new Vector2(n.ClickRect.X - msgSize.X - 3f, n.ClickRect.Y + 32 - msgSize.Y / 2f);
                        HelperFunctions.ClampVectorToInt(ref cursor);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, n.Message, cursor, n.Pause ? Color.Red : Color.White);
                    }
                }

            }
        }

        public bool HandleInput(InputState input)
        {
            bool retValue = false;
            bool recalculate = false;
            lock (NotificationLocker)
            {
                foreach (Notification n in NotificationList)
                {
                    if (!n.ClickRect.HitTest(input.CursorPosition))
                    {
                        n.ShowMessage = false;
                    }
                    else
                    {
                        if (input.LeftMouseReleased)
                        {
                            NotificationList.QueuePendingRemoval(n);
                            recalculate = true;
                            switch (n.Action)
                            {
                                case "SnapToPlanet":
                                    SnapToPlanet(n.ReferencedItem1 as Planet);
                                    break;
                                case "SnapToSystem":
                                    SnapToSystem(n.ReferencedItem1 as SolarSystem);
                                    break;
                                case "CombatScreen":
                                    SnapToCombat(n.ReferencedItem1 as Planet);
                                    break;
                                case "LoadEvent":
                                    TriggerExplorationEvent(n.ReferencedItem1 as ExplorationEvent);
                                    break;
                                case "ResearchScreen":
                                    ScreenManager.AddScreen(new ResearchPopup(Screen, n.ReferencedItem1 as string));
                                    break;
                                case "SnapToExpandSystem":
                                    SnapToExpandedSystem(n.ReferencedItem2 as Planet, n.ReferencedItem1 as SolarSystem);
                                    break;
                            }
                            retValue = true;
                        }
                        if (input.RightMouseClick && n.Action != "LoadEvent")
                        {
                            GameAudio.PlaySfxAsync("sub_bass_whoosh");
                            NotificationList.QueuePendingRemoval(n);
                            recalculate = true;
                            retValue = true;
                            // ADDED BY SHAHMATT (to unpause game on right clicking notification icon)
                            if (GlobalStats.PauseOnNotification && n.Pause)
                                Screen.Paused = false;
                        }
                        n.ShowMessage = true;
                    }
                }
                NotificationList.ApplyPendingRemovals();
                if (recalculate)
                    UpdateAllPositions();
            }
            return retValue;
        }

        public void ReSize()
        {
            NotificationArea = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 70, 70, 70, 
                                             ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 70 - 250);
            lock (NotificationLocker)
                UpdateAllPositions();
        }

        public void SnapToTechScreen()
        {
            
        }

        public void SnapToCombat(Planet p)
        {
            GameAudio.PlaySfxAsync("sub_bass_whoosh");
            Screen.SelectedPlanet = p;
            if (!Screen.SnapBackToSystem)
            {
                Screen.HeightOnSnap = Screen.CamHeight;
            }
            Screen.OpenCombatMenu();
        }

        public void SnapToPlanet(Planet p)
        {
            GameAudio.PlaySfxAsync("sub_bass_whoosh");
            Screen.SelectedPlanet = p;
            if (!Screen.SnapBackToSystem)
            {
                Screen.HeightOnSnap = Screen.CamHeight;
            }
            Screen.SnapViewPlanet(p);
        }
        public void SnapToExpandedSystem(Planet p, SolarSystem system)
        {
            GameAudio.PlaySfxAsync("sub_bass_whoosh");
            if (p != null) Screen.SelectedPlanet = p;
            Screen.SelectedSystem = system;
           // Screen.mouseWorldPos = p == null ? system.Position : p.Position;
            Screen.SnapViewSystem(system, UniverseScreen.UnivScreenState.GalaxyView); 
        }

        public void SnapToSystem(SolarSystem system)
        {
            GameAudio.PlaySfxAsync("sub_bass_whoosh");
            Screen.SnapViewSystem(system, UniverseScreen.UnivScreenState.SystemView);
        }

        public Outcome GetRandomOutcome(ExplorationEvent e)
        {
            int ranMax = e.PotentialOutcomes.Where(outcome => !outcome.OnlyTriggerOnce || !outcome.AlreadyTriggered)
                .Sum(outcome => outcome.Chance);

            int random = (int)RandomMath.RandomBetween(0, ranMax);
            Outcome triggeredOutcome = new Outcome();
            int cursor = 0;
            foreach (Outcome outcome in e.PotentialOutcomes)
            {
                if (outcome.OnlyTriggerOnce && outcome.AlreadyTriggered)
                    continue;
                cursor = cursor + outcome.Chance;
                if (random > cursor)
                    continue;
                triggeredOutcome = outcome;
                outcome.AlreadyTriggered = true;
                break;
            }
            return triggeredOutcome;
        }

        private void TriggerExplorationEvent(ExplorationEvent evt)
        {
            Outcome triggeredOutcome = GetRandomOutcome(evt);

            Empire empire = EmpireManager.Player;
            Screen.ScreenManager.AddScreen(new EventPopup(Screen, empire, evt, triggeredOutcome, false));
            evt.TriggerOutcome(empire, triggeredOutcome);
        }

        public void Update(float elapsedTime)
        {
            float date = Screen.StarDate;
            string dateString = date.ToString(CultureInfo.InvariantCulture);
            if (Timer < date && ResourceManager.EventsDict.ContainsKey(dateString))
            {
                Timer = date;
                TriggerExplorationEvent(ResourceManager.EventsDict[dateString]);
            }
            
            lock (NotificationLocker)
            {
                foreach (Notification n in NotificationList)
                {
                    n.transitionElapsedTime = n.transitionElapsedTime + elapsedTime;
                    float amount = (float)Math.Pow(n.transitionElapsedTime / n.transDuration, 2);
                    n.ClickRect.Y = (int)MathHelper.SmoothStep(n.ClickRect.Y, n.DestinationRect.Y, amount);
                    // ADDED BY SHAHMATT (pause game when there are any notifications)
                    //if (GlobalStats.PauseOnNotification && this.Screen.viewState > UniverseScreen.UnivScreenState.SystemView && n.ClickRect.Y >= n.DestinationRect.Y)
                    //fbedard : Add filter to pause
                    if (GlobalStats.PauseOnNotification && n.ClickRect.Y >= n.DestinationRect.Y && n.Pause)
                        Screen.Paused = true;
                }
            }
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        ~NotificationManager() { Destroy(); }
        private void Destroy()
        {
            NotificationList?.Dispose();
            NotificationList = null;
        }
    }
}