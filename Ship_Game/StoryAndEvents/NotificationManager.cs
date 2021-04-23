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
            string threatLevel = Localizer.Token(GameText.NthreatLevelVsOurForcesnthere);
            if      (strRatio < 0.1f)  threatLevel = $"{threatLevel} {Localizer.Token(GameText.Negligible)}"; // negligible
            else if (strRatio < 0.3f)  threatLevel = $"{threatLevel} {Localizer.Token(GameText.VeryLow)}"; // very low
            else if (strRatio < 0.5f)  threatLevel = $"{threatLevel} {Localizer.Token(GameText.Low)}"; // low
            else if (strRatio < 0.75f) threatLevel = $"{threatLevel} {Localizer.Token(GameText.Medium)}"; // medium
            else if (strRatio < 1f)    threatLevel = $"{threatLevel} {Localizer.Token(GameText.High)}"; // high
            else if (strRatio < 1.5f)  threatLevel = $"{threatLevel} {Localizer.Token(GameText.VeryHigh)}"; // very high
            else                       threatLevel = $"{threatLevel} {Localizer.Token(GameText.Overwhelming)}"; // overwhelming

            string message = invader.data.Traits.Singular
                             + Localizer.Token(GameText.ForcesSpottedIn) + '\n'
                             + Localizer.Token(GameText.The2) + beingInvaded.Name
                             + Localizer.Token(GameText.System2) + threatLevel;

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
                Message         = wasColonized.Name + Localizer.Token(GameText.WasColonizednclickForColonyScreen),
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
                case TreatyType.Alliance:      treaty = Localizer.Token(GameText.Alliance); break;
                case TreatyType.OpenBorders:   treaty = Localizer.Token(GameText.OpenBordersTreaty); break;
                case TreatyType.Trade:         treaty = Localizer.Token(GameText.TradeTreaty); break;
                case TreatyType.NonAggression: treaty = Localizer.Token(GameText.NonaggressionPact); break;
            }

            if (treaty.IsEmpty())
                return;

            string our        = Localizer.Token(GameText.Our);
            string with       = Localizer.Token(GameText.With);
            string wasRevoked = Localizer.Token(GameText.WasRevoked);
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
                Message         = conqueror.data.Traits.Name + Localizer.Token(GameText.Captured) + p.Name + "\n" + Localizer.Token(GameText.From) + loser.data.Traits.Name,
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
                Message         = $"{p.Name}: {Localizer.Token(GameText.CouldNotPlaceThisOrbital)} ({cost}).",
                ReferencedItem1 = p,
                IconPath        = path,
                Action          = "SnapToPlanet"
            }, "sd_ui_notification_warning");
        }

        public void AddBuildingConstructed(Planet p, Building b)
        {
            string constructionOf = Localizer.Token(GameText.ConstructionOf);
            string wasFinishedAt  = Localizer.Token(GameText.WasFinishedAt);
            AddNotification(new Notification
            {
                Message         = $"{constructionOf} {b.TranslatedName.Text}\n{wasFinishedAt} {p.Name}",
                ReferencedItem1 = p,
                IconPath        = $"Buildings/icon_{b.Icon}_64x64",
                Action          = "SnapToPlanet"
            }, "smallservo");
        }

        public void AddEnemyLaunchedTroopsVsFleet(Planet p, Empire enemy)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = enemy,
                Message         = Localizer.Token(GameText.TheEnemyHasLaunchedA),
                ReferencedItem1 = p,
                IconPath        = p.IconPath,
                Action          = "SnapToPlanet"
            }, "sd_notify_alert");
        }


        public void AddBuildingDestroyedByLava(Planet p, Building b)   => AddBuildingDestroyed(p, b, Localizer.Token(GameText.WasDestroyedByALava));
        public void AddBuildingDestroyedByMeteor(Planet p, Building b) => AddBuildingDestroyed(p, b, Localizer.Token(GameText.WasDestroyedByAMeteor));

        public void AddBuildingDestroyed(Planet p, Building b, string text)
        {
            AddNotification(new Notification
            {
                Message         = $"{p.Name}: {b.TranslatedName.Text} {text}",
                ReferencedItem1 = p,
                IconPath        = $"Buildings/icon_{b.Icon}_64x64",
                Action          = "SnapToPlanet"
            }, "sd_ui_notification_warning");
        }

        public void AddEmpireDiedNotification(Empire thatDied)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = thatDied,
                Message         = $"{thatDied.data.Traits.Name} {Localizer.Token(GameText.HasBeenDefeated)}",
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
                Message         = Localizer.Token(GameText.WeReturnedTheShipWhich),
                Action          = "SnapToShip",
                ClickRect       = DefaultClickRect,
                DestinationRect = DefaultNotificationRect
            }, "sd_troop_march_01");
        }

        public void AddPiratesAreGettingStronger(Empire pirates, int numBases)
        {
            string yourSpiesReportThat = Localizer.Token(GameText.OurSpiesReportThat);
            string areGettingStronger  = Localizer.Token(GameText.AreGettingStrongerntheyHaveAround);
            string bases               = Localizer.Token(GameText.Bases);
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
                Message         = Localizer.Token(GameText.OurScientistsReportThatThey)
            }, "sd_ui_notification_warning");;
        }

        public void AddRemnantsStoryActivation(Empire remnants)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = remnants,
                ClickRect       = DefaultClickRect,
                DestinationRect = DefaultNotificationRect,
                Message         = Localizer.Token(GameText.OurScientistsReportThatThey2)
            }, "sd_ui_notification_warning");
        }

        public void AddRemnantsNewPortal(Empire remnants)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = remnants,
                ClickRect       = DefaultClickRect,
                DestinationRect = DefaultNotificationRect,
                Message         = Localizer.Token(GameText.YourScientistsReportMassiveRadiation)
            }, "sd_ui_notification_encounter");
        }

        public void AddPiratesAreGettingWeaker(Empire pirates, int numBases)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = pirates,
                Message         = $"{Localizer.Token(GameText.OurSpiesReportThat)} {pirates.Name} {Localizer.Token(GameText.NumberOfBasesWasReducednto)} {numBases}.",
                ClickRect       = DefaultClickRect,
                DestinationRect = DefaultNotificationRect
            }, "sd_troop_march_01");
        }

        public void AddPiratesFlagshipSighted(Empire pirates)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = pirates,
                Message         = $"{Localizer.Token(GameText.OurSpiesReportThat)} {pirates.Name} {Localizer.Token(GameText.HaveAFlagshipnlurkingSomewhereIn)}",
                ClickRect       = DefaultClickRect,
                DestinationRect = DefaultNotificationRect
            }, "sd_troop_march_01");
        }

        public void AddEnemyTroopsLandedNotification(Planet where, Empire invader)
        {
            AddNotification(new Notification
            {
                RelevantEmpire  = invader,
                Message         = invader.data.Traits.Singular + Localizer.Token(GameText.TroopsAreLandingOn) + where.Name + "!",
                ReferencedItem1 = where,
                IconPath        = where.IconPath,
                Action          = "CombatScreen"
            }, "sd_notify_alert", "sd_troop_march_01");
        }

        public void AddForeignTroopsRemovedNotification(Planet where)
        {
            AddNotification(new Notification
            {
                Message         = $"{Localizer.Token(GameText.ForeignTroopsEvacuatedFrom)} {where.Name}",
                ReferencedItem1 = where,
                IconPath        = where.IconPath,
                Action          = "SnapToPlanet"
            }, "sd_troop_march_01");
        }

        public void AddTroopsRemovedNotification(Planet where)
        {
            string ourTroopsOn   = Localizer.Token(GameText.OurTroopsStationedOn);
            string hadToEvacuate = Localizer.Token(GameText.HadToEvacuateSincen);
            string ownsThePlanet = Localizer.Token(GameText.OwnsThePlanetNow);
            AddNotification(new Notification
            {
                Message         = $"{ourTroopsOn} {where.Name} {hadToEvacuate}{where.Owner.data.Traits.Name} {ownsThePlanet}",
                ReferencedItem1 = where,
                IconPath        = where.IconPath,
                Action          = "SnapToPlanet"
            }, "sd_troop_march_01");
        }

        public void AddMeteorRelated(Planet planet, string text, string texPath = "") => AddVolcanoRelated(planet, text, texPath);

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
                Message         = Localizer.Token(GameText.AnEventRequiresYourAttention),
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
                Message         = $"{expEvent.LocalizedName}\nClick for more info",
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
                Message         = Localizer.Token(GameText.ScansOf) + p.Name + Localizer.Token(GameText.RevealedAnAnomaly),
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
            string message = Localizer.Token(GameText.TreasuryWarningRunningOutOf);
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
                Message        = absorber.data.Traits.Name + " " + Localizer.Token(GameText.HasPeacefullyMergedIntoA) + target.data.Traits.Name,
                IconPath       = "NewUI/icon_planet_terran_01_mid"
            }, "sd_troop_march_01");
        }

        public void AddPeaceTreatyEnteredNotification(Empire first, Empire second)
        {
            AddNotification(new Notification
            {
                Pause    = false,
                Message  = $"{first.data.Traits.Name} {Localizer.Token(GameText.And3)} {second.data.Traits.Name}\n{Localizer.Token(GameText.AreNowAtPeace)}",
                IconPath = "UI/icon_peace"
            }, "sd_ui_notification_conquer_01");
        }

        public void AddPeaceTreatyExpiredNotification(Empire otherEmpire)
        {
            AddNotification(new Notification
            {
                Pause    = false,
                Message  = $"{Localizer.Token(GameText.PeaceTreatyExpiredWithn)} {otherEmpire.data.Traits.Name}",
                IconPath = "UI/icon_peace_cancel"
            }, "sd_ui_notification_warning");
        }

        public void AddPlanetDiedNotification(Planet p)
        {
            AddNotification(new Notification
            {
                Message         = Localizer.Token(GameText.TheLastColonistOn) + p.Name + Localizer.Token(GameText.HasBeenKilled),
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
                message += $"{Localizer.Token(GameText.WasDestroyedWhileExploringSystem)} {ship.System.Name}";
                explorerDestroyed.ReferencedItem1 = ship.System;
                explorerDestroyed.Action          = "SnapToSystem";
            }
            else
            {
                message += Localizer.Token(GameText.WasDestroyedWhileExploringDeepSpace);
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
            string message = $"{Localizer.Token(GameText.DestroyedPirateBase)} {reward.String(0)} credits.";
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
                Message         = tech.Name.Text + Localizer.Token(GameText.Unlocked),
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
                Message        = target.data.Traits.Name + " " + Localizer.Token(GameText.HasSurrenderedTo) + absorber.data.Traits.Name,
                IconPath       = "NewUI/icon_planet_terran_01_mid"
            }, "sd_troop_march_01");
        }

        public void AddWarDeclaredNotification(Empire declarant, Empire other)
        {
            AddNotification(new Notification
            {
                Message = $"{declarant.data.Traits.Name} {Localizer.Token(GameText.And3)} {other.data.Traits.Name}\n{Localizer.Token(GameText.AreNowAtWar)}",
                IconPath = "ResearchMenu/icons_techroot_infantry_hover",
                Pause    = declarant.isPlayer || other.isPlayer
            }, "sd_troop_march_01", "sd_notify_alert");
        }

        public void AddWarStartedNotification(Empire first, Empire second)
        {
            AddNotification(new Notification
            {
                Message  = $"{first.data.Traits.Name} {Localizer.Token(GameText.And3)} {second.data.Traits.Name}\n{Localizer.Token(GameText.AreNowAtWar)}",
                IconPath = "UI/icon_warning_money",
                Pause    = first.isPlayer || second.isPlayer
            }, "sd_ui_notification_startgame");
        }

        public void AddEmptyQueueNotification(Planet planet)
        {
            string message = $"{planet.Name} {Localizer.Token(GameText.IsNotProducingAnything)}";
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
