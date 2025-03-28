using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Universe;
using System.Windows.Forms;

namespace Ship_Game
{
    public sealed class NotificationManager
    {
        public readonly ScreenManager ScreenManager;
        public readonly UniverseScreen Screen;
        Rectangle NotificationArea;
        public int MaxEntriesToDisplay;

        readonly Array<Notification> NotificationList = new();
        public int NumberOfNotifications => NotificationList.Count;

        public bool HitTest => NotificationArea.HitTest(Screen.Input.CursorPosition);

        public NotificationManager(ScreenManager screenManager, UniverseScreen screen)
        {
            Screen = screen;
            ScreenManager = screenManager;
            UpdateNotificationArea();
        }

        public void Clear()
        {
            lock (NotificationList)
                NotificationList.Clear();
        }
        
        public bool IsNotificationPresent(string message)
        {
            lock (NotificationList)
                return NotificationList.Any(n => n.Message == message);
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

            lock (NotificationList)
                NotificationList.Add(notify);
        }

        public void AddAgentResult(bool good, string result, Empire owner, Planet planet = null)
        {
            if (!owner.isPlayer || owner.data.SpyMute)
                return;

            AddNotification(new Notification
            {
                Message         = result,
                SymbolPath      = good ? "NewUI/icon_spy_notification" : "NewUI/icon_spy_notification_bad",
                ReferencedItem1 = planet?.System ?? null,
                IconPath        = planet?.IconPath ?? null,
                Action          = planet != null ? "SnapToSystem" : "",
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

        public void AddCapitalTransfer(Planet from, Planet to)
        {
            AddNotification(new Notification
            {
                Message         = $"{to.Name}: {Localizer.Token(GameText.NotifyCapitalTransfer)} {from.Name}",
                ReferencedItem1 = to,
                IconPath        = to.IconPath,
                Action          = "SnapToPlanet"
            }, "sd_ui_notification_encounter");
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
                Message        = $"{our} {treaty} {with} {empire.Name} {wasRevoked}"
            }, "sd_ui_notification_warning");
        }

        public void NotifyPreparingForWar(Empire e)
        {
            AddNotification(new Notification
            {
                RelevantEmpire = e,
                Message        = $"{Localizer.Token(GameText.OurSpiesReport)} {e.Name} {Localizer.Token(GameText.TheyPreparingForWar)}"
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
            object item   = p.System;
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
                RelevantEmpire  = p.Universe.Remnants,
                Message         = message,
                ReferencedItem1 = p.System,
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


        public void AddBuildingDestroyedByLava(Planet p, Building b)   => AddBuildingDestroyed(p, b, GameText.WasDestroyedByALava);
        public void AddBuildingDestroyedByMeteor(Planet p, Building b) => AddBuildingDestroyed(p, b, GameText.WasDestroyedByAMeteor);

        public void AddBuildingDestroyed(Planet p, Building b, in LocalizedText text)
        {
            AddNotification(new Notification
            {
                Message         = $"{p.Name}: {b.TranslatedName.Text} {text.Text}",
                ReferencedItem1 = p,
                IconPath        = $"Buildings/icon_{b.Icon}_64x64",
                Action          = "SnapToPlanet"
            }, "sd_ui_notification_warning");
        }

        public void AddEmpireDiedNotification(Empire thatDied, bool IsRemnant = false)
        {
            string message = $"{thatDied.data.Traits.Name} {Localizer.Token(GameText.HasBeenDefeated)}";
            if (IsRemnant)
                message += $"\n{Localizer.Token(GameText.RemnantsDefeatedFleetsMayAttack)}";

            AddNotification(new Notification
            {
                RelevantEmpire  = thatDied,
                Message         = message,
                IconPath        = "NewUI/icon_planet_terran_01_mid",
                ClickRect       = DefaultClickRect,
                DestinationRect = DefaultNotificationRect
            }, "sd_troop_march_01");
        }

        public void AddEmpireMergedOrSurrendered(Empire empire, string msg)
        {
            AddNotification(new Notification
            {
                RelevantEmpire = empire,
                Message = msg,
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
            }, "sd_ui_notification_warning");
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
                IconPath = texturePath.IsEmpty() ?  planet.IconPath : texturePath,
                Action = planet.Owner == Screen.Player ? "SnapToPlanet" : "CombatScreen",
                Message = $"{planet.Name}: {text}"
            }, "sd_ui_notification_encounter");
        }

        public void AddStarvation(Planet planet)
        {
            AddNotification(new Notification
            {
                ReferencedItem1 = planet,
                IconPath = planet.IconPath,
                Action = "SnapToPlanet",
                Message = $"{Localizer.Token(GameText.StarvationOnPlanet)} {planet.Name}!"
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
        
        public void AddRemnantAbleToScanOrWarn(Empire remnants, GameText gameText)
        {
            AddNotification(new Notification
            {
                RelevantEmpire = remnants,
                Pause = false,
                Message = Localizer.Token(gameText)
            }, "sd_ui_notification_encounter"); ;
        }

        public void AddPiratesAbleToScan(Empire pirates)
        {
            AddNotification(new Notification
            {
                RelevantEmpire = pirates,
                Pause = false,
                Message = $"{Localizer.Token(GameText.CanScanPiratesEvent)}"
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
                ReferencedItem1 = p.System,
                ReferencedItem2 = p,
                IconPath        = p.IconPath,
                Action          = "SnapToExpandSystem"
            }, "sd_ui_notification_encounter");
        }

        public void AddMineablePlanet(Planet p)
        {
            AddNotification(new Notification
            {
                Pause           = false,
                Message         = $"{p.Name} {Localizer.Token(GameText.MineablePlanetNotification)}",
                ReferencedItem1 = p,
                IconPath        = p.IconPath,
                Action          = "SnapToPlanet"
            }, "sd_ui_notification_encounter");
        }

        public void AddReseachablePlanet(Planet p)
        {
            GameText text;
            switch (p.Category)
            {
                case PlanetCategory.Volcanic: text = GameText.ResearchablePlanetVolcanic; break;
                case PlanetCategory.GasGiant: text = GameText.ResearchablePlanetGasGiant; break;
                default:                      text = GameText.ResearchablePlanetDefault;  break;
            }

            AddNotification(new Notification
            {
                Pause           = false,
                Message         = $"{p.Name} { Localizer.Token(text)}",
                ReferencedItem1 = p,
                IconPath        = p.IconPath,
                Action          = "SnapToPlanet"
            }, "sd_ui_notification_encounter");
        }

        public void AddReseachableStar(SolarSystem s)
        {
            AddNotification(new Notification
            {
                Pause           = false,
                Message         = $"{s.Name}{Localizer.Token(GameText.ResearchableStar)}",
                ReferencedItem1 = s,
                IconPath        = s.Sun.IconPath,
                Action          = "SnapToSystem"
            }, "sd_ui_notification_encounter");
        }

        public void AddShipCrashed(Planet p, string message)
        {
            if (p.Universe.P.DisableCrashSiteWarning)
                return;

            AddNotification(new Notification
            {
                Pause           = false,
                Message         = message,
                ReferencedItem1 = p,
                IconPath        = p.IconPath,
                Action          = "SnapToPlanet"
            }, "sd_ui_notification_encounter");
        }

        /// <summary>
        /// Message the player regarding recovered ship on a planet.
        /// Null ship is safe here.
        /// </summary>
        public void AddShipRecovered(Planet p, Ship s, string message)
        {
            if (s == null && p.Universe.P.DisableCrashSiteWarning)
                return;

            var recover = new Notification
            {
                Pause   = false,
                Message = message
            };

            if (s != null)
            {
                recover.ReferencedItem1 = s;
                recover.IconPath        = s.ShipData.BaseHull.IconPath;
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

        public void AddMergeWithPlayer(Empire target)
        {
            AddNotification(new Notification
            {
                RelevantEmpire = target,
                Message = $"{target.data.Traits.Name} {Localizer.Token(GameText.AcceptedIntoOurEmpire)}",
                IconPath = "NewUI/icon_planet_terran_01_mid"
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
                ReferencedItem1 = p.System,
                IconPath        = p.IconPath,
                Action          = "SnapToSystem"
            }, "sd_ui_notification_warning");
        }

        public void AddMeteorShowerInSystem(Planet p)
        {
            AddNotification(new Notification
            {
                Message         = $"{p.Name}{Localizer.Token(GameText.MeteorShowerWarningNotOurPlanet)}",
                ReferencedItem1 = p.System,
                IconPath        = p.IconPath,
                Action          = "SnapToSystem"
            }, "sd_ui_notification_warning");
        }

        public void AddMeteorShowerTargetingOurPlanet(Planet p)
        {
            AddNotification(new Notification
            {
                Message         = $"{p.Name}{Localizer.Token(GameText.MeteorShowerWarning)}",
                ReferencedItem1 = p.System,
                IconPath        = p.IconPath,
                Action          = "SnapToSystem"
            }, "sd_notify_alert");
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
            Notification explorerDestroyed = new()
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

        public void AddExcessResearchStationRemoved(Ship station)
        {
            AddNotification(new Notification
            {
                Message = Localizer.Token(GameText.RemoveExcessResearchStation),
                Action = "SnapToShip",
                ReferencedItem1 = station,
                IconPath = station.BaseHull.IconPath ?? "ResearchMenu/icon_event_science_bad"
            }, "sd_ui_notification_encounter");
        }

        public void AddResearchStationRemoved(Planet planet)
        {
            AddNotification(new Notification
            {
                Message = Localizer.Token(GameText.RemoveResearchStationTerraform),
                Action = "SnapToPlanet",
                ReferencedItem1 = planet,
                IconPath = planet.IconPath
            }, "sd_ui_notification_encounter");
        }

        public void AddScrapUnlockNotification(string message, string iconPath)
        {
            AddNotification(new Notification
            {
                Message  = message,
                Action   = "ShipDesign",
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
            }, "sd_ui_notification_encounter");
        }

        public void AddResearchStationBuiltNotification(Ship s, ExplorableGameObject solarBody)
        {
            string message;
            if (solarBody is Planet planet)
            {
                message = $"{planet.System.Name}: {s.Name}" +
                    $" {Localizer.Token(GameText.ResearchStationBuiltPlanetNotify)} {planet.Name}";
            }
            else
            {
                message = $"{(solarBody as SolarSystem).Name}:" +
                    $" {s.Name} {Localizer.Token(GameText.ResearchStationBuiltSystemNotify)}";
            }

            AddNotification(new Notification
            {
                Message         = message,
                Action          = "SnapToShip",
                ReferencedItem1 = s,
                IconPath        = s.ShipData.IconPath
            }, "smallservo");
        }

        public void AddMiningStationBuiltNotification(Ship s, Planet planet)
        {
            AddNotification(new Notification
            {
                Message = $"{planet.System.Name}: {s.Name}" + $" {Localizer.Token(GameText.MiningStationBuiltPlanetNotify)} {planet.Name}",
                Action = "SnapToShip",
                ReferencedItem1 = s,
                IconPath = s.ShipData.IconPath
            }, "smallservo");
        }

        public void AddAbortLandNotification(Planet planet, Ship s)
        {
            string message = $"{planet.Name}: {Localizer.Token(GameText.AbortLandPlayerTroopsNoFleet)} {planet.Owner.Name}";

            AddNotification(new Notification
            {
                Message         = message,
                Action          = "SnapToShip",
                ReferencedItem1 = s,
                IconPath        = s.BaseHull.IconPath
            }, "sd_ui_notification_encounter");
        }

        public void AddAbortLandNotification(Planet planet, Fleet fleet)
        {
            string message =  $"{planet.Name}: {Localizer.Token(GameText.AbortLandPlayerTroopsNoFleet)} {planet.Owner.Name}" +
                              $"\n{fleet.Name} {Localizer.Token(GameText.AbortLandPlayerTroopsInFleet)}";

            AddNotification(new Notification
            {
                RelevantEmpire  = planet.Owner,
                Message         = message,
                Action          = "SnapToShip",
                ReferencedItem1 = planet,
                IconPath        = planet.IconPath
            }, "sd_ui_notification_encounter");
        }

        public void AddDestroyedPirateBase(Ship s, float reward)
        {
            string message = $"{Localizer.Token(GameText.DestroyedPirateBase)} {reward.String(0)} credits.";
            AddNotification(new Notification
            {
                RelevantEmpire  = s.Loyalty,
                Message         = message,
                ReferencedItem1 = s,
                DestinationRect = DefaultNotificationRect
            }, "sd_ui_notification_encounter");
        }

        public void AddScrapProgressNotification(string message, string iconPath, string techName)
        {
            AddNotification(new Notification
            {
                Message = message,
                Action = "ResearchScreen",
                ReferencedItem1 = techName,
                IconPath = iconPath ?? "ResearchMenu/icon_event_science_bad"
            }, "sd_ui_notification_encounter");
        }

        public void AddRebellionNotification(Planet beingInvaded)
        {
            string message = "Rebellion on " + beingInvaded.Name + "!";
            if (IsNotificationPresent(message))
                return;
            AddNotification(new Notification
            {
                Message         = message,
                ReferencedItem1 = beingInvaded.System,
                IconPath        = "UI/icon_rebellion",
                Action          = "SnapToSystem"
            }, "sd_troop_march_01", "sd_notify_alert");
        }

        public void AddRemnantHelpersGiftMessage(int storyStep, SolarSystem system, Empire remnants)
        {
            GameText messageIndex = GameText.RemnantHelpersGiftStep1;
            if (storyStep >= 3) messageIndex = GameText.RemnantHelpersGiftStep3Plus;
            if (storyStep >= 2) messageIndex = GameText.RemnantHelpersGiftStep2;

            AddNotification(new Notification
            {
                RelevantEmpire  = remnants,
                Message         = Localizer.Token(messageIndex),
                ReferencedItem1 = system,
                Action          = "SnapToSystem"
            }, "sd_ui_notification_encounter");
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

        public void AddWarDeclaredNotification(Empire attacker, Empire victim)
        {
            AddNotification(new Notification
            {
                Message = $"{attacker.data.Traits.Name} {Localizer.Token(GameText.And3)} {victim.data.Traits.Name}\n{Localizer.Token(GameText.AreNowAtWar)}",
                IconPath = "ResearchMenu/icons_techroot_infantry_hover",
                Pause    = attacker.isPlayer || victim.isPlayer
            }, "sd_troop_march_01", "sd_notify_alert");
        }

        public void AddDeclareWarViaAllyCall(Empire enemy, Empire requestingEmpire)
        {
            AddNotification(new Notification
            {
                RelevantEmpire = enemy,
                Message        = $"{enemy.Name} {Localizer.Token(GameText.DeclaredWarOnUsBecause)} {requestingEmpire.Name}",
            }, "sd_ui_notification_encounter");
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

        Notification[] GetNotificationsAtomic()
        {
            lock (NotificationList)
                return NotificationList.ToArr();
        }

        public void Draw(SpriteBatch batch)
        {
            Notification[] notifications = GetNotificationsAtomic();
            for (int i = 0; i < notifications.Length && i <= MaxEntriesToDisplay; ++i)
            {
                Notification n = notifications[i];
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
                else if (n.SymbolPath != null)
                {
                    batch.Draw(ResourceManager.Texture(n.SymbolPath), n.ClickRect, Color.White);
                }

                if (n.ShowMessage)
                {
                    Vector2 msgSize = Fonts.Arial12Bold.MeasureString(n.Message);
                    Vector2 cursor = new(n.ClickRect.X - msgSize.X - 3f, n.ClickRect.Y + 32 - msgSize.Y / 2f);
                    cursor = cursor.ToFloored();
                    batch.DrawString(Fonts.Arial12Bold, n.Message, cursor, n.Pause ? Color.Red : Color.White);
                }
            }
        }

        public bool HandleInput(InputState input)
        {
            Notification[] notifications = GetNotificationsAtomic();
            foreach (Notification n in notifications)
            {
                if (n.HandleInput(input, this))
                {
                    lock (NotificationList)
                    {
                        NotificationList.Remove(n);
                        UpdateAllPositions();
                    }
                    return true;
                }
            }
            return false;
        }

        public void ReSize()
        {
            UpdateNotificationArea();
            lock (NotificationList)
                UpdateAllPositions();
        }

        public void SnapToCombat(Planet p)
        {
            GameAudio.SubBassWhoosh();
            Screen.SnapViewColony(p, p.Owner != Screen.Player);
        }

        public void SnapToPlanet(Planet p)
        {
            GameAudio.SubBassWhoosh();
            Screen.SnapViewColony(p, combatView: false);
        }

        public void SnapToShip(Ship s)
        {
            GameAudio.SubBassWhoosh();
            Screen.SnapViewShip(s);
        }

        public void SnapToExpandedSystem(Planet p, SolarSystem system)
        {
            GameAudio.SubBassWhoosh();
            Screen.SnapViewSystem(system, p, UniverseScreen.UnivScreenState.GalaxyView);
        }

        public void SnapToSystem(SolarSystem system)
        {
            GameAudio.SubBassWhoosh();
            Screen.SnapViewSystem(system, null, UniverseScreen.UnivScreenState.SystemView);
        }

        public void Update(float elapsedRealTime)
        {
            Notification[] notifications = GetNotificationsAtomic();
            foreach (Notification n in notifications)
            {
                n.transitionElapsedTime += elapsedRealTime;
                float amount = (float) Math.Pow(n.transitionElapsedTime / n.transDuration, 2);
                n.ClickRect.Y = (int) Math.Ceiling(n.ClickRect.Y.SmoothStep(n.DestinationRect.Y, amount));
                // ADDED BY SHAHMATT (pause game when there are any notifications)
                //fbedard : Add filter to pause
                if (GlobalStats.PauseOnNotification && n.ClickRect.Y >= n.DestinationRect.Y && n.Pause)
                    Screen.UState.Paused = true;
            }

            // fbedard: remove excess notifications
            if (notifications.Length > MaxEntriesToDisplay)
            {
                for (int i = 0; i < notifications.Length; i++)
                {
                    Notification n = notifications[i];
                    if (n.DestinationRect.Y != n.ClickRect.Y) break;
                    if (n.Action != "LoadEvent" && !(GlobalStats.PauseOnNotification && n.Pause))
                    {
                        lock (NotificationList)
                        {
                            NotificationList.Remove(n);
                            UpdateAllPositions();
                        }
                        break;
                    }
                }
            }
        }
    }
}
