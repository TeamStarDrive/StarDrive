using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class NotificationManager : IDisposable
    {
        readonly ScreenManager ScreenManager;
        readonly UniverseScreen Screen;
        Rectangle NotificationArea;
        int MaxEntriesToDisplay;

        static readonly object NotificationLocker = new object();
        BatchRemovalCollection<Notification> NotificationList;
        public int NumberOfNotifications => NotificationList.Count;

        public bool HitTest => NotificationArea.HitTest(Screen.Input.CursorPosition);

        public bool IsNotificationPresent(string message)
        {
            lock (NotificationLocker)
                return NotificationList.Any(n => n.Message == message);
        }

    public NotificationManager(ScreenManager screenManager, UniverseScreen screen)
        {
            NotificationList = new BatchRemovalCollection<Notification>();
            Screen           = screen;
            ScreenManager    = screenManager;
            UpdateNotificationArea();
        }

        void UpdateNotificationArea()
        {
            NotificationArea = new Rectangle(GameBase.ScreenWidth - 70, 70, 70,
                                            GameBase.ScreenHeight - 70 - 275);
            MaxEntriesToDisplay = NotificationArea.Height / 70;
        }

        Rectangle GetNotificationRect(int index)
        {
            int yPos = (NotificationArea.Y + NotificationArea.Height - index * 70 - 70).Clamped(70, NotificationArea.Height );
            return new Rectangle(NotificationArea.X, yPos, 64, 64);
        }

        Rectangle DefaultNotificationRect => GetNotificationRect(NotificationList.Count);
        Rectangle DefaultClickRect => new Rectangle(NotificationArea.X, NotificationArea.Y, 64, 64);

        public void AddNotification(Notification notify, params string[] soundCueStrings)
        {
            notify.ClickRect = DefaultClickRect;
            notify.DestinationRect = DefaultNotificationRect;

            foreach (string cue in soundCueStrings)
                GameAudio.PlaySfxAsync(cue);

            lock (NotificationLocker)
                NotificationList.Add(notify);
        }

        public void AddAgentResult(bool good, string result, Empire owner)
        {
            if (!owner.isPlayer || owner.data.SpyMute)
                return;

            AddNotification(new Notification
            {
                Message = result,
                IconPath = good ? "NewUI/icon_spy_notification" : "NewUI/icon_spy_notification_bad"
            }, good ? "sd_ui_spy_win_02" : "sd_ui_spy_fail_02");
        }

        public void AddBeingInvadedNotification(SolarSystem beingInvaded, Empire invader, float strRatio)
        {
            string threatLevel = new LocalizedText(4152).Text;
            if      (strRatio < 0.1f)  threatLevel = $"{threatLevel} {new LocalizedText(4153).Text}"; // negligible
            else if (strRatio < 0.3f)  threatLevel = $"{threatLevel} {new LocalizedText(4154).Text}"; // very low
            else if (strRatio < 0.5f)  threatLevel = $"{threatLevel} {new LocalizedText(4155).Text}"; // low
            else if (strRatio < 0.75f) threatLevel = $"{threatLevel} {new LocalizedText(4156).Text}"; // medium
            else if (strRatio < 1f)    threatLevel = $"{threatLevel} {new LocalizedText(4157).Text}"; // high
            else if (strRatio < 1.5f)  threatLevel = $"{threatLevel} {new LocalizedText(4158).Text}"; // very high
            else                       threatLevel = $"{threatLevel} {new LocalizedText(4159).Text}"; // overwhelming

            string message = invader.data.Traits.Singular
                             + Localizer.Token(1500) + '\n'
                             + Localizer.Token(1501) + beingInvaded.Name
                             + Localizer.Token(1502) + threatLevel;

            AddNotification(new Notification
            {
                RelevantEmpire  = invader,
                Message         = message,
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
                IconPath        = wasColonized.IconPath,
                Action          = "SnapToPlanet"
            }, "sd_ui_notification_colonized_01");
        }

        public void AddTreatyBreak(Empire empire, TreatyType type)
        {
            string treaty = "";
            switch (type)
            {
                case TreatyType.Alliance:      treaty = new LocalizedText(1612).Text; break;
                case TreatyType.OpenBorders:   treaty = new LocalizedText(1216).Text; break;
                case TreatyType.Trade:         treaty = new LocalizedText(1215).Text; break;
                case TreatyType.NonAggression: treaty = new LocalizedText(1214).Text; break;
            }

            if (treaty.IsEmpty())
                return;

            string our        = new LocalizedText(4160).Text;
            string with       = new LocalizedText(4161).Text;
            string wasRevoked = new LocalizedText(4162).Text;
            AddNotification(new Notification
            {
                RelevantEmpire = empire,
                Message        = $"{our} {treaty} {with} {empire.Name} {wasRevoked}",
            }, "sd_ui_notification_warning");
        }

        public void AddAnomalyInvestigated(Planet p, string message)
        {
            AddNotification(new Notification
            {
                Message         = message,
                ReferencedItem1 = p,
                IconPath        = p.IconPath,
                Action          = "CombatScreen"
            }, "sd_ui_notification_encounter");
        }

        public void AddConqueredNotification(Planet p, Empire conqueror, Empire loser)
        {
            string action = "SnapToSystem";
            object item   = p.ParentSystem;
            if (conqueror.isPlayer)
            {
                action = "SnapToPlanet";
                item   = p;
            }

            AddNotification(new Notification
            {
                RelevantEmpire  = conqueror,
                Message         = conqueror.data.Traits.Name + Localizer.Token(1503) + p.Name + "\n" + Localizer.Token(1504) + loser.data.Traits.Name,
                ReferencedItem1 = item,
                IconPath        = p.IconPath,
                Action          = action
            }, "sd_troop_march_01");
        }

        public void AddIncomingRemnants(Planet p, string message)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = EmpireManager.Remnants,
                Message         = message,
                ReferencedItem1 = p.ParentSystem,
                IconPath        = p.IconPath,
                Action          = "SnapToSystem"
            }, "sd_troop_march_01");
        }

        public void AddOrbitalOverLimit(Planet p, int cost, string path)
        {
            AddNotification(new Notification
            {
                Message         = $"{p.Name}: {new LocalizedText(4277).Text} ({cost}).",
                ReferencedItem1 = p,
                IconPath        = path,
                Action          = "SnapToPlanet"
            }, "sd_ui_notification_warning");
        }

        public void AddBuildingConstructed(Planet p, Building b)
        {
            string constructionOf = new LocalizedText(4163).Text;
            string wasFinishedAt  = new LocalizedText(4164).Text;
            AddNotification(new Notification
            {
                Message         = $"{constructionOf} {b.TranslatedName.Text}\n{wasFinishedAt} {p.Name}",
                ReferencedItem1 = p,
                IconPath        = $"Buildings/icon_{b.Icon}_64x64",
                Action          = "SnapToPlanet"
            }, "smallservo");
        }

        public void AddBuildingDestroyedByLava(Planet p, Building b)
        {
            AddNotification(new Notification
            {
                Message = $"{p.Name}: {b.TranslatedName.Text} {new LocalizedText(4276).Text}",
                ReferencedItem1 = p,
                IconPath = $"Buildings/icon_{b.Icon}_64x64",
                Action = "SnapToPlanet"
            }, "sd_ui_notification_warning");
        }

        public void AddEmpireDiedNotification(Empire thatDied)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = thatDied,
                Message         = $"{thatDied.data.Traits.Name} {new LocalizedText(4165).Text}",
                IconPath        = "NewUI/icon_planet_terran_01_mid",
                ClickRect       = DefaultClickRect,
                DestinationRect = DefaultNotificationRect
            }, "sd_troop_march_01");
        }

        public void AddWeProtectedYou(Empire pirates)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = pirates,
                Message         = new LocalizedText(4166).Text,
                Action          = "SnapToShip",
                ClickRect       = DefaultClickRect,
                DestinationRect = DefaultNotificationRect
            }, "sd_troop_march_01");
        }

        public void AddPiratesAreGettingStronger(Empire pirates, int numBases)
        {
            string yourSpiesReportThat = new LocalizedText(4167).Text;
            string areGettingStronger  = new LocalizedText(4168).Text;
            string bases               = new LocalizedText(4169).Text;
            AddNotification(new Notification
            {
                RelevantEmpire  = pirates,
                Message         = $"{yourSpiesReportThat} {pirates.Name} {areGettingStronger} {numBases} {bases}",
                ClickRect       = DefaultClickRect,
                DestinationRect = DefaultNotificationRect
            }, "sd_troop_march_01");
        }

        public void AddRemnantsAreGettingStronger(Empire remnants)
        {
            AddNotification(new Notification
            {
                RelevantEmpire = remnants,
                ClickRect       = DefaultClickRect,
                DestinationRect = DefaultNotificationRect,
                Message         = new LocalizedText(4170).Text
            }, "sd_ui_notification_warning");;
        }

        public void AddRemnantsStoryActivation(Empire remnants)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = remnants,
                ClickRect       = DefaultClickRect,
                DestinationRect = DefaultNotificationRect,
                Message         = new LocalizedText(4171).Text
            }, "sd_ui_notification_warning");
        }

        public void AddRemnantsNewPortal(Empire remnants)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = remnants,
                ClickRect       = DefaultClickRect,
                DestinationRect = DefaultNotificationRect,
                Message         = new LocalizedText(4172).Text
            }, "sd_ui_notification_encounter");
        }

        public void AddPiratesAreGettingWeaker(Empire pirates, int numBases)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = pirates,
                Message         = $"{new LocalizedText(4167).Text} {pirates.Name} {new LocalizedText(4183).Text} {numBases}.",
                ClickRect       = DefaultClickRect,
                DestinationRect = DefaultNotificationRect
            }, "sd_troop_march_01");
        }

        public void AddPiratesFlagshipSighted(Empire pirates)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = pirates,
                Message         = $"{new LocalizedText(4167).Text} {pirates.Name} {new LocalizedText(4184).Text}",
                ClickRect       = DefaultClickRect,
                DestinationRect = DefaultNotificationRect
            }, "sd_troop_march_01");
        }

        public void AddEnemyTroopsLandedNotification(Planet where, Empire invader)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = invader,
                Message         = invader.data.Traits.Singular + Localizer.Token(1507) + where.Name + "!",
                ReferencedItem1 = where,
                IconPath        = where.IconPath,
                Action          = "CombatScreen"
            }, "sd_notify_alert", "sd_troop_march_01");
        }

        public void AddForeignTroopsRemovedNotification(Planet where)
        {
            AddNotification(new Notification
            {
                Message         = $"{new LocalizedText(4182).Text} {where.Name}",
                ReferencedItem1 = where,
                IconPath        = where.IconPath,
                Action          = "SnapToPlanet"
            }, "sd_troop_march_01");
        }

        public void AddTroopsRemovedNotification(Planet where)
        {
            string ourTroopsOn   = new LocalizedText(4179).Text;
            string hadToEvacuate = new LocalizedText(4180).Text;
            string ownsThePlanet = new LocalizedText(4181).Text;
            AddNotification(new Notification
            {
                Message         = $"{ourTroopsOn} {where.Name} {hadToEvacuate}{where.Owner.data.Traits.Name} {ownsThePlanet}",
                ReferencedItem1 = where,
                IconPath        = where.IconPath,
                Action          = "SnapToPlanet"
            }, "sd_troop_march_01");
        }

        public void AddVolcanoRelated(Planet planet, string text, string texturePath = "")
        {
            AddNotification(new Notification
            {
                ReferencedItem1 = planet,
                IconPath        = texturePath.IsEmpty() ?  planet.IconPath : texturePath,
                Action          = planet.Owner == EmpireManager.Player ? "SnapToPlanet" : "CombatScreen",
                Message         = $"{planet.Name}: {text}"
            }, "sd_ui_notification_encounter");
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

        public void AddRemnantUpdateNotify(ExplorationEvent expEvent, Empire remnants)
        {

            AddNotification(new Notification
            {
                RelevantEmpire  = remnants,
                Pause           = false,
                Message         = $"{expEvent.Name}\nClick for more info",
                ReferencedItem1 = expEvent,
                Action          = "LoadEvent"
            }, "sd_ui_notification_encounter");
        }

        public void AddNotify(Technology.TriggeredEvent techEvent, string message) => 
            AddNotify(ResourceManager.EventsDict[techEvent.EventUID], message);

        public void AddNotify(Technology.TriggeredEvent techEvent) => 
            AddNotify(ResourceManager.EventsDict[techEvent.EventUID]);

        public void AddFoundSomethingInteresting(Planet p)
        {
            AddNotification(new Notification
            {
                Pause           = false,
                Message         = Localizer.Token(1505) + p.Name + Localizer.Token(1506),
                ReferencedItem1 = p.ParentSystem,
                ReferencedItem2 = p,
                IconPath        = p.IconPath,
                Action          = "SnapToExpandSystem"
            }, "sd_ui_notification_encounter");
        }

        public void AddShipCrashed(Planet p, string message)
        {
            AddNotification(new Notification
            {
                Pause           = false,
                Message         = message,
                ReferencedItem1 = p,
                IconPath        = p.IconPath,
                Action          = "SnapToPlanet"
            }, "sd_ui_notification_encounter"); ;
        }

        /// <summary>
        /// Message the player regarding recovered ship on a planet.
        /// Null ship is safe here.
        /// </summary>
        public void AddShipRecovered(Planet p, Ship s, string message)
        {
            var recover = new Notification
            {
                Pause   = false,
                Message = message
            };

            if (s != null)
            {
                recover.ReferencedItem1 = s;
                recover.IconPath        = s.shipData.BaseHull.ActualIconPath;
                recover.Action          = "SnapToShip";
            }
            else
            {
                recover.ReferencedItem1 = p;
                recover.IconPath        = p.IconPath;
                recover.Action          = "SnapToPlanet";
            }

            AddNotification(recover, "sd_ui_notification_encounter");
        }

        public void AddMoneyWarning()
        {
            string message = LocalizedText.Parse("{LowMoneyWarning}").Text;  // Localizer.Token(2296);
            if (IsNotificationPresent(message))
                return;

            AddNotification(new Notification
            {
                Pause    = true,
                Message  = message,
                IconPath = "UI/icon_warning_money"
            }, "sd_ui_notification_warning", "sd_trade_01");
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
                Message  = $"{first.data.Traits.Name} {new LocalizedText(4174).Text} {second.data.Traits.Name}\n{new LocalizedText(4176).Text}",
                IconPath = "UI/icon_peace"
            }, "sd_ui_notification_conquer_01");
        }

        public void AddPeaceTreatyExpiredNotification(Empire otherEmpire)
        {
            AddNotification(new Notification
            {
                Pause    = false,
                Message  = $"{new LocalizedText(4177).Text} {otherEmpire.data.Traits.Name}",
                IconPath = "UI/icon_peace_cancel"
            }, "sd_ui_notification_warning");
        }

        public void AddPlanetDiedNotification(Planet p)
        {
            AddNotification(new Notification
            {
                Message         = Localizer.Token(1511) + p.Name + Localizer.Token(1512),
                ReferencedItem1 = p.ParentSystem,
                IconPath        = p.IconPath,
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

        public void AddExplorerDestroyedNotification(Ship ship)
        {
            string message = $"{ship.Name} ";
            Notification explorerDestroyed = new Notification
            {
                IconPath = ship.BaseHull.IconPath ?? "ResearchMenu/icon_event_science_bad"
            };

            if (ship.System != null)
            {
                message += $"{new LocalizedText(GameText.WasDestroyedWhileExploringSystem).Text} {ship.System.Name}";
                explorerDestroyed.ReferencedItem1 = ship.System;
                explorerDestroyed.Action          = "SnapToSystem";
            }
            else
            {
                message += new LocalizedText(GameText.WasDestroyedWhileExploringDeepSpace).Text;
            }

            explorerDestroyed.Message = message;
            AddNotification(explorerDestroyed, "sd_ui_notification_encounter");
        }

        public void AddScrapUnlockNotification(string message, string iconPath, string action)
        {
            AddNotification(new Notification
            {
                Message  = message,
                Action   = action,
                IconPath = iconPath ?? "ResearchMenu/icon_event_science_bad"
            }, "sd_ui_notification_encounter");
        }

        public void AddBoardNotification(string message, string iconPath, string action, Ship s, Empire boarder)
        {
            AddNotification(new Notification
            {
                RelevantEmpire = boarder,
                Message         = message,
                Action          = action,
                ReferencedItem1 = s,
                IconPath        = iconPath ?? "ResearchMenu/icon_event_science_bad"
            }, "sd_ui_notification_encounter");; 
        }

        public void AddDestroyedPirateBase(Ship s, float reward)
        {
            string message = $"{new LocalizedText(GameText.DestroyedPirateBase).Text} {reward.String(0)} credits.";
            AddNotification(new Notification
            {
                RelevantEmpire  = s.loyalty,
                Message         = message,
                ReferencedItem1 = s,
                DestinationRect = DefaultNotificationRect
            }, "sd_ui_notification_encounter");
        }

        public void AddScrapProgressNotification(string message, string iconPath, string action, string techName)
        {
            AddNotification(new Notification
            {
                Message = message,
                Action = action,
                ReferencedItem1 = techName,
                IconPath = iconPath ?? "ResearchMenu/icon_event_science_bad"
            }, "sd_ui_notification_encounter");
        }

        public void AddRebellionNotification(Planet beingInvaded, Empire invader)
        {
            string message = "Rebellion on " + beingInvaded.Name + "!";
            if (IsNotificationPresent(message))
                return;
            AddNotification(new Notification
            {
                Message         = message,
                ReferencedItem1 = beingInvaded.ParentSystem,
                IconPath        = "UI/icon_rebellion",
                Action          = "SnapToSystem"
            }, "sd_troop_march_01", "sd_notify_alert");
        }

        public void AddResearchComplete(string unlocked, Empire emp)
        {
            if (!ResourceManager.TryGetTech(unlocked, out Technology tech))
            {
                Log.Error($"Invalid Tech Notification: '{unlocked}'");
                return;
            }

            // Techs using Icon Path need this for notifications
            string techIcon = "TechIcons/" + tech.IconPath;
            bool hasTechIcon = ResourceManager.TextureLoaded(techIcon);

            AddNotification(new Notification
            {
                Tech            = true,
                Message         = Localizer.Token(tech.NameIndex) + Localizer.Token(1514),
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
                Message = $"{declarant.data.Traits.Name} {new LocalizedText(4174).Text} {other.data.Traits.Name}\n{new LocalizedText(4175).Text}",
                IconPath = "ResearchMenu/icons_techroot_infantry_hover",
                Pause    = declarant.isPlayer || other.isPlayer
            }, "sd_troop_march_01", "sd_notify_alert");
        }

        public void AddWarStartedNotification(Empire first, Empire second)
        {
            AddNotification(new Notification
            {
                Message  = $"{first.data.Traits.Name} {new LocalizedText(4174).Text} {second.data.Traits.Name}\n{new LocalizedText(4175).Text}",
                IconPath = "UI/icon_warning_money",
                Pause    = first.isPlayer || second.isPlayer
            }, "sd_ui_notification_startgame");
        }

        public void AddEmptyQueueNotification(Planet planet)
        {
            string message = $"{planet.Name} {new LocalizedText(4173).Text}";
            if (IsNotificationPresent(message))
                return;

            AddNotification(new Notification
            {
                Pause           = false,
                RelevantEmpire  = planet.Owner,
                Message         = message,
                ReferencedItem1 = this, //this.system,
                IconPath        = planet.IconPath, //"UI/icon_warning_money",
                Action          = "SnapToPlanet" //"SnapToSystem",
            }, "sd_ui_notification_warning");
        }

        void UpdateAllPositions()
        {
            for (int i = 0; i < NotificationList.Count; i++)
            {
                Notification n = NotificationList[i];
                n.DestinationRect        = GetNotificationRect(i);
                n.ClickRect.X            = NotificationArea.X;
                bool resetTransitionTime = n.transitionElapsedTime > n.transDuration;
                n.transitionElapsedTime  = resetTransitionTime ? 0f : n.transitionElapsedTime;
            }
        }

        public void Draw(SpriteBatch batch)
        {
            lock (NotificationLocker)
            {
                for (int i = 0; i < NotificationList.Count && i <= MaxEntriesToDisplay; i++)
                {
                    Notification n = NotificationList[i];
                    if (n.Icon != null || n.IconPath != null)
                    {
                        SubTexture iconTex = n.Icon ?? ResourceManager.Texture(n.IconPath);
                        if (!n.Tech)
                        {
                            batch.Draw(iconTex, n.ClickRect, Color.White);
                        }
                        else
                        {
                            Rectangle rect = n.ClickRect;
                            if (rect.X == 0)
                                continue;

                            rect.X = n.ClickRect.X + n.ClickRect.Width / 2 - iconTex.Width / 2;
                            rect.Y = n.ClickRect.Y + n.ClickRect.Height / 2 - iconTex.Height / 2;

                            rect.Width  = iconTex.Width;
                            rect.Height = iconTex.Height;
                            batch.Draw(ResourceManager.Texture("TechIcons/techbg"), rect, Color.White);
                            batch.Draw(iconTex, rect, Color.White);
                            batch.DrawRectangle(rect, new Color(32, 30, 18));
                        }
                    }
                    if (n.RelevantEmpire != null)
                    {
                        var flag = n.RelevantEmpire.data.Traits.FlagIndex;
                        batch.Draw(ResourceManager.Flag(flag), n.ClickRect, n.RelevantEmpire.EmpireColor);
                    }
                    if (n.ShowMessage)
                    {
                        Vector2 msgSize = Fonts.Arial12Bold.MeasureString(n.Message);
                        Vector2 cursor = new Vector2(n.ClickRect.X - msgSize.X - 3f, n.ClickRect.Y + 32 - msgSize.Y / 2f);
                        HelperFunctions.ClampVectorToInt(ref cursor);
                        batch.DrawString(Fonts.Arial12Bold, n.Message, cursor, n.Pause ? Color.Red : Color.White);
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
                                    ((ExplorationEvent) n.ReferencedItem1)?.TriggerExplorationEvent(Screen);
                                    break;
                                case "ResearchScreen":
                                    ScreenManager.AddScreen(new ResearchPopup(Screen, n.ReferencedItem1 as string));
                                    break;
                                case "SnapToExpandSystem":
                                    SnapToExpandedSystem(n.ReferencedItem2 as Planet, n.ReferencedItem1 as SolarSystem);
                                    break;
                                case "ShipDesign":
                                    ScreenManager.AddScreen(new ShipDesignScreen(Empire.Universe, Screen.EmpireUI));
                                    break;
                                case "SnapToShip":
                                    SnapToShip(n.ReferencedItem1 as Ship);
                                    break;
                            }
                            retValue = true;
                        }
                        if (input.RightMouseClick && n.Action != "LoadEvent")
                        {
                            GameAudio.SubBassWhoosh();
                            NotificationList.QueuePendingRemoval(n);
                            recalculate = true;
                            retValue    = true;
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
            UpdateNotificationArea();
            lock (NotificationLocker)
                UpdateAllPositions();
        }

        public void SnapToCombat(Planet p)
        {
            GameAudio.SubBassWhoosh();
            Screen.SelectedPlanet = p;
            Screen.SnapViewColony(p.Owner != EmpireManager.Player);
        }

        public void SnapToPlanet(Planet p)
        {
            GameAudio.SubBassWhoosh();
            Screen.SelectedPlanet = p;
            Screen.SnapViewColony(combatView: false);
        }

        public void SnapToShip(Ship s)
        {
            GameAudio.SubBassWhoosh();
            Screen.SelectedShip = s;
            Screen.SnapViewShip(s);
        }

        public void SnapToExpandedSystem(Planet p, SolarSystem system)
        {
            GameAudio.SubBassWhoosh();
            if (p != null) Screen.SelectedPlanet = p;
            Screen.SelectedSystem = system;
            Screen.SnapViewSystem(system, UniverseScreen.UnivScreenState.GalaxyView);
        }

        public void SnapToSystem(SolarSystem system)
        {
            GameAudio.SubBassWhoosh();
            Screen.SnapViewSystem(system, UniverseScreen.UnivScreenState.SystemView);
        }

        public void Update(float elapsedRealTime)
        {
            lock (NotificationLocker)
            {
                foreach (Notification n in NotificationList)
                {
                    n.transitionElapsedTime += elapsedRealTime;
                    float amount = (float) Math.Pow(n.transitionElapsedTime / n.transDuration, 2);
                    n.ClickRect.Y =
                        (int) Math.Ceiling(MathHelper.SmoothStep(n.ClickRect.Y, n.DestinationRect.Y, amount));
                    // ADDED BY SHAHMATT (pause game when there are any notifications)
                    //fbedard : Add filter to pause
                    if (GlobalStats.PauseOnNotification && n.ClickRect.Y >= n.DestinationRect.Y && n.Pause)
                        Screen.Paused = true;
                }
                if (NotificationList.Count > MaxEntriesToDisplay)  //fbedard: remove excess notifications
                {
                    for (int i = 0; i < NotificationList.Count; i++)
                    {
                        Notification n = NotificationList[i];
                        if (n.DestinationRect.Y != n.ClickRect.Y) break;
                        if (n.Action != "LoadEvent" && !(GlobalStats.PauseOnNotification && n.Pause))
                        {
                            NotificationList.QueuePendingRemoval(n);
                            break;
                        }
                    }
                    NotificationList.ApplyPendingRemovals();
                    UpdateAllPositions();
                }
            }
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        ~NotificationManager() { Destroy(); }

        void Destroy()
        {
            NotificationList?.Dispose();
            NotificationList = null;
        }
    }
}