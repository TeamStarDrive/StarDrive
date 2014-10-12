using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class NotificationManager
	{
		private Ship_Game.ScreenManager ScreenManager;

		public Rectangle NotificationArea;

		private int numentriesToDisplay;

		private UniverseScreen screen;

		public BatchRemovalCollection<Notification> NotificationList = new BatchRemovalCollection<Notification>();
        private float Timer;

		public NotificationManager(Ship_Game.ScreenManager ScreenManager, UniverseScreen screen)
		{
			this.screen = screen;
			this.ScreenManager = ScreenManager;
			this.NotificationArea = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 70, 70, 70, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 70 - 250);
			this.numentriesToDisplay = this.NotificationArea.Height / 70;
		}

		public void AddAgentResultNotification(bool Good, string result, Empire Owner)
		{
			if (Owner != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
			{
				return;
			}
			Notification cNote = new Notification()
			{
				Message = result,
				IconPath = (Good ? "NewUI/icon_spy_notification" : "NewUI/icon_spy_notification_bad"),
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			if (!Good)
			{
				AudioManager.PlayCue("sd_ui_spy_fail_02");
			}
			else
			{
				AudioManager.PlayCue("sd_ui_spy_win_02");
			}
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

		public void AddBeingInvadedNotification(SolarSystem beingInvaded, Empire Invader)
		{
			Notification cNote = new Notification()
			{
				RelevantEmpire = Invader
			};
			string[] singular = new string[] { Invader.data.Traits.Singular, Localizer.Token(1500), "\n", Localizer.Token(1501), beingInvaded.Name, Localizer.Token(1502) };
			cNote.Message = string.Concat(singular);
			cNote.ReferencedItem1 = beingInvaded;
			cNote.IconPath = "NewUI/icon_planet_terran_01_mid";
			cNote.Action = "SnapToSystem";
			cNote.ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64);
			cNote.DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64);
			AudioManager.PlayCue("sd_notify_alert");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

		public void AddColonizedNotification(Planet wasColonized, Empire emp)
		{
			Notification cNote = new Notification()
			{
				RelevantEmpire = emp,
				Message = string.Concat(wasColonized.Name, Localizer.Token(1513)),
				ReferencedItem1 = wasColonized,
				IconPath = string.Concat("Planets/", wasColonized.planetType),
				Action = "SnapToPlanet",
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			AudioManager.PlayCue("sd_ui_notification_colonized_01");
			this.NotificationList.Add(cNote);
		}

		public void AddConqueredNotification(Planet wasConquered, Empire Conquerer, Empire Loser)
		{
			Notification cNote = new Notification()
			{
				RelevantEmpire = Conquerer
			};
			string[] name = new string[] { Conquerer.data.Traits.Name, Localizer.Token(1503), wasConquered.Name, "\n", Localizer.Token(1504), Loser.data.Traits.Name };
			cNote.Message = string.Concat(name);
			cNote.ReferencedItem1 = wasConquered.system;
			cNote.IconPath = string.Concat("Planets/", wasConquered.planetType);
			cNote.Action = "SnapToSystem";
			cNote.ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64);
			cNote.DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64);
			AudioManager.PlayCue("sd_troop_march_01");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

		public void AddEmpireDiedNotification(Empire thatDied)
		{
			Notification cNote = new Notification()
			{
				RelevantEmpire = thatDied,
				Message = string.Concat(thatDied.data.Traits.Name, " has been defeated"),
				IconPath = "NewUI/icon_planet_terran_01_mid",
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			AudioManager.PlayCue("sd_troop_march_01");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

		public void AddEnemyTroopsLandedNotification(Planet where, Empire Invader, Empire Player)
		{
			Notification cNote = new Notification()
			{
				RelevantEmpire = Invader,
				Message = string.Concat(Invader.data.Traits.Singular, Localizer.Token(1507), where.Name, "!"),
				ReferencedItem1 = where,
				IconPath = string.Concat("Planets/", where.planetType),
				Action = "CombatScreen",
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			AudioManager.PlayCue("sd_notify_alert");
			AudioManager.PlayCue("sd_troop_march_01");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

        public void AddForeignTroopsRemovedNotification(Planet where)
        {
            Notification cNote = new Notification()
            {
                Message = string.Concat("Foreign troops evacuated from ", where.Name),
                ReferencedItem1 = where,
                IconPath = string.Concat("Planets/", where.planetType),
                Action = "SnapToPlanet",
                ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
                DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
            };
            AudioManager.PlayCue("sd_notify_alert");
            lock (GlobalStats.NotificationLocker)
            {
                this.NotificationList.Add(cNote);
            }
        }

        public void AddTroopsRemovedNotification(Planet where)
        {
            Notification cNote = new Notification()
            {
                Message = string.Concat(where.Owner.data.Traits.Singular, " have colonized ", where.Name, " and your troops evacuated"),
                ReferencedItem1 = where,
                IconPath = string.Concat("Planets/", where.planetType),
                Action = "SnapToPlanet",
                ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
                DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
            };
            AudioManager.PlayCue("sd_notify_alert");
            lock (GlobalStats.NotificationLocker)
            {
                this.NotificationList.Add(cNote);
            }
        }

		public void AddEventNotification(ExplorationEvent expEvent)
		{
			Notification cNote = new Notification()
			{
				Message = Localizer.Token(2295),
				ReferencedItem1 = expEvent,
				IconPath = "ResearchMenu/icon_event_science",
				Action = "LoadEvent",
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			AudioManager.PlayCue("sd_ui_notification_encounter");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

        public void AddEventNotification(ExplorationEvent expEvent, string cMessage)
        {
            Notification cNote = new Notification()
            {
                Message = cMessage,
                ReferencedItem1 = expEvent,
                IconPath = "ResearchMenu/icon_event_science",
                Action = "LoadEvent",
                ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
                DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
            };
            AudioManager.PlayCue("sd_ui_notification_encounter");
            lock (GlobalStats.NotificationLocker)
            {
                this.NotificationList.Add(cNote);
            }
        }

		public void AddFoundSomethingInteresting(Planet p)
		{
			Notification cNote = new Notification()
			{
				Message = string.Concat(Localizer.Token(1505), p.Name, Localizer.Token(1506)),
				ReferencedItem1 = p.system,
				IconPath = string.Concat("Planets/", p.planetType),
				Action = "SnapToSystem",
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			AudioManager.PlayCue("sd_ui_notification_encounter");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

		public void AddMolePlantedNotification(Planet wasConquered, Empire Us)
		{
			Notification cNote = new Notification()
			{
				RelevantEmpire = Us,
				Message = string.Concat(Localizer.Token(1510), wasConquered.Name),
				ReferencedItem1 = wasConquered,
				IconPath = string.Concat("Planets/", wasConquered.planetType),
				Action = "SnapToPlanet",
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			AudioManager.PlayCue("sd_troop_march_01");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

		public void AddMoleRemovedNotification(Planet wasConquered, Empire Us, Empire them)
		{
			Notification cNote = new Notification()
			{
				RelevantEmpire = Us,
				Message = string.Concat("Removed ", them.data.Traits.Singular, " agent from ", wasConquered.Name),
				ReferencedItem1 = wasConquered,
				IconPath = string.Concat("Planets/", wasConquered.planetType),
				Action = "SnapToPlanet",
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			AudioManager.PlayCue("sd_troop_march_01");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

		public void AddMoneyWarning()
		{
			Notification cNote = new Notification()
			{
				Message = Localizer.Token(2296),
				IconPath = "UI/icon_warning_money",
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			AudioManager.PlayCue("sd_ui_notification_warning");
			AudioManager.PlayCue("sd_trade_01");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

		public void AddNoMolesNotification(Empire Us, Empire them)
		{
			Notification cNote = new Notification()
			{
				RelevantEmpire = Us,
				Message = string.Concat(Localizer.Token(1508), them.data.Traits.Singular, Localizer.Token(1509)),
				IconPath = "NewUI/icon_planet_terran_01_mid",
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			AudioManager.PlayCue("sd_troop_march_01");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

		public void AddPeacefulMergerNotification(Empire Absorber, Empire Target)
		{
			Notification cNote = new Notification()
			{
				RelevantEmpire = Absorber,
				Message = string.Concat(Absorber.data.Traits.Name, " ", Localizer.Token(2258), Target.data.Traits.Name),
				IconPath = "NewUI/icon_planet_terran_01_mid",
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			AudioManager.PlayCue("sd_troop_march_01");
			this.NotificationList.Add(cNote);
		}

		public void AddPeaceTreatyEnteredNotification(Empire First, Empire Second)
		{
			Notification cNote = new Notification()
			{
				Message = string.Concat(First.data.Traits.Name, " and ", Second.data.Traits.Name, "\n are now at peace"),
				IconPath = "UI/icon_peace",
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 78, 58),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			AudioManager.PlayCue("sd_ui_notification_conquer_01");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

		public void AddPeaceTreatyExpiredNotification(Empire otherEmpire)
		{
			Notification cNote = new Notification()
			{
				Message = string.Concat("Peace Treaty expired with \n", otherEmpire.data.Traits.Name),
				IconPath = "UI/icon_peace_cancel",
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			AudioManager.PlayCue("sd_ui_notification_warning");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

		public void AddPlanetDiedNotification(Planet died, Empire Owner)
		{
			Notification cNote = new Notification()
			{
				Message = string.Concat(Localizer.Token(1511), died.Name, Localizer.Token(1512)),
				ReferencedItem1 = died.system,
				IconPath = string.Concat("Planets/", died.planetType),
				Action = "SnapToSystem",
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			AudioManager.PlayCue("sd_ui_notification_warning");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

		public void AddRandomEventNotification(string Message, string IconPath, string Action, Planet p)
		{
			Notification cNote = new Notification()
			{
				Message = Message
			};
			if (Action != null)
			{
				cNote.Action = Action;
			}
			if (p != null)
			{
				cNote.ReferencedItem1 = p;
			}
			cNote.IconPath = (IconPath != null ? IconPath : "ResearchMenu/icon_event_science_bad");
			cNote.ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64);
			cNote.DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64);
			AudioManager.PlayCue("sd_ui_notification_encounter");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

		public void AddRebellionNotification(Planet beingInvaded, Empire Invader)
		{
			Notification cNote = new Notification()
			{
				Message = string.Concat("Rebellion on ", beingInvaded.Name, "!"),
				ReferencedItem1 = beingInvaded.system,
				IconPath = "UI/icon_rebellion",
				Action = "SnapToSystem",
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			AudioManager.PlayCue("sd_troop_march_01");
			AudioManager.PlayCue("sd_notify_alert");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

		public void AddResearchComplete(string unlocked, Empire emp)
		{
			Notification cNote = new Notification()
			{
				Tech = true,
				Message = string.Concat(Localizer.Token(ResourceManager.TechTree[unlocked].NameIndex), Localizer.Token(1514)),
				ReferencedItem1 = unlocked,
                //Added by McShooterz: Techs using Icon Path need this for notifications
                IconPath = ResourceManager.TextureDict.ContainsKey(string.Concat("TechIcons/", ResourceManager.TechTree[unlocked].IconPath)) ? string.Concat("TechIcons/", ResourceManager.TechTree[unlocked].IconPath) : string.Concat("TechIcons/", unlocked),
                //IconPath = string.Concat("TechIcons/", unlocked),
				Action = "ResearchScreen",
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			AudioManager.PlayCue("sd_ui_notification_research_01");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

		public void AddSurrendered(Empire Absorber, Empire Target)
		{
			Notification cNote = new Notification()
			{
				RelevantEmpire = Absorber,
				Message = string.Concat(Target.data.Traits.Name, " ", Localizer.Token(2259), Absorber.data.Traits.Name),
				IconPath = "NewUI/icon_planet_terran_01_mid",
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			AudioManager.PlayCue("sd_troop_march_01");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

		public void AddWarDeclaredNotification(Empire Declarant, Empire Other)
		{
			Notification cNote = new Notification()
			{
				Message = string.Concat(Declarant.data.Traits.Name, " and ", Other.data.Traits.Name, " are now at war"),
				IconPath = "ResearchMenu/icons_techroot_infantry_hover",
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 78, 58),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			AudioManager.PlayCue("sd_troop_march_01");
			AudioManager.PlayCue("sd_notify_alert");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

		public void AddWarStartedNotification(Empire First, Empire Second)
		{
			Notification cNote = new Notification()
			{
				Message = string.Concat(First.data.Traits.Name, " and ", Second.data.Traits.Name, "\n are now at War"),
				IconPath = "UI/icon_warning_money",
				ClickRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y, 64, 64),
				DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (this.NotificationList.Count + 1) * 70, 64, 64)
			};
			AudioManager.PlayCue("sd_ui_notification_startgame");
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.Add(cNote);
			}
		}

		public void Draw()
		{
			lock (GlobalStats.NotificationLocker)
			{
				for (int i = 0; i < this.NotificationList.Count && i <= this.numentriesToDisplay; i++)
				{
					Notification n = this.NotificationList[i];
					Rectangle clickRect = n.ClickRect;
					if (n.IconPath != null)
					{
						if (!n.Tech)
						{
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[n.IconPath], n.ClickRect, Color.White);
						}
						else
						{
							Rectangle rect = n.ClickRect;
							if (n.ClickRect.X == 0)
							{
								continue;
							}
							rect.X = n.ClickRect.X + n.ClickRect.Width / 2 - ResourceManager.TextureDict[n.IconPath].Width / 2;
							rect.Y = n.ClickRect.Y + n.ClickRect.Height / 2 - ResourceManager.TextureDict[n.IconPath].Height / 2;
							rect.Width = ResourceManager.TextureDict[n.IconPath].Width;
							rect.Height = ResourceManager.TextureDict[n.IconPath].Height;
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["TechIcons/techbg"], rect, Color.White);
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[n.IconPath], rect, Color.White);
							Primitives2D.DrawRectangle(this.ScreenManager.SpriteBatch, rect, new Color(32, 30, 18));
						}
					}
					if (n.RelevantEmpire != null)
					{
						SpriteBatch spriteBatch = this.ScreenManager.SpriteBatch;
						KeyValuePair<string, Texture2D> item = ResourceManager.FlagTextures[n.RelevantEmpire.data.Traits.FlagIndex];
						spriteBatch.Draw(item.Value, n.ClickRect, n.RelevantEmpire.EmpireColor);
					}
					if (n.ShowMessage)
					{
						Vector2 Cursor = new Vector2((float)n.ClickRect.X - Fonts.Arial12Bold.MeasureString(n.Message).X - 3f, (float)(n.ClickRect.Y + 32) - Fonts.Arial12Bold.MeasureString(n.Message).Y / 2f);
						HelperFunctions.ClampVectorToInt(ref Cursor);
						this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, n.Message, Cursor, Color.White);
					}
                //Label0:
                  //  continue;
				}
			}
		}

		public bool HandleInput(InputState input)
		{
			bool retValue = false;
			Vector2 MousePos = new Vector2((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y);
			bool Recalculate = false;
			lock (GlobalStats.NotificationLocker)
			{
				for (int i = 0; i < this.NotificationList.Count; i++)
				{
					Notification n = this.NotificationList[i];
					if (!HelperFunctions.CheckIntersection(n.ClickRect, MousePos))
					{
						n.ShowMessage = false;
					}
					else
					{
						if (input.CurrentMouseState.LeftButton == ButtonState.Released && input.LastMouseState.LeftButton == ButtonState.Pressed)
						{
							this.NotificationList.QueuePendingRemoval(n);
							Recalculate = true;
							string action = n.Action;
							string str = action;
							if (action != null)
							{
								if (str == "SnapToPlanet")
								{
									this.SnapToPlanet(n.ReferencedItem1 as Planet);
								}
								else if (str == "SnapToSystem")
								{
									this.SnapToSystem(n.ReferencedItem1 as SolarSystem);
								}
								else if (str == "CombatScreen")
								{
									this.SnapToCombat(n.ReferencedItem1 as Planet);
								}
								else if (str == "LoadEvent")
								{
									this.ScreenManager.AddScreen(new EventPopup(this.screen, EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty), n.ReferencedItem1 as ExplorationEvent, (n.ReferencedItem1 as ExplorationEvent).PotentialOutcomes[0]));
									(n.ReferencedItem1 as ExplorationEvent).TriggerOutcome(EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty), (n.ReferencedItem1 as ExplorationEvent).PotentialOutcomes[0]);
								}
								else if (str == "ResearchScreen")
								{
									this.ScreenManager.AddScreen(new ResearchPopup(this.screen, new Rectangle(0, 0, 600, 600), n.ReferencedItem1 as string));
								}
							}
							retValue = true;
						}
						if (input.CurrentMouseState.RightButton == ButtonState.Pressed && input.LastMouseState.RightButton == ButtonState.Released && n.Action != "LoadEvent")
						{
							AudioManager.PlayCue("sub_bass_whoosh");
							this.NotificationList.QueuePendingRemoval(n);
							Recalculate = true;
							retValue = true;
                            // ADDED BY SHAHMATT (to unpause game on right clicking notification icon)
                            if (GlobalStats.PauseOnNotification )
                                this.screen.Paused = false;
						}
						n.ShowMessage = true;
					}
				}
			}
			lock (GlobalStats.NotificationLocker)
			{
				this.NotificationList.ApplyPendingRemovals();
				if (Recalculate)
				{
					for (int i = 0; i < this.NotificationList.Count; i++)
					{
						Notification n = this.NotificationList[i];
						n.DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (i + 1) * 70, 64, 64);
						n.transitionElapsedTime = 0f;
						n.ClickRect = new Rectangle(this.NotificationArea.X, n.ClickRect.Y, n.ClickRect.Width, n.ClickRect.Height);
					}
				}
			}
			return retValue;
		}

		public void ReSize()
		{
			this.NotificationArea = new Rectangle(this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 70, 70, 70, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 70 - 250);
			lock (GlobalStats.NotificationLocker)
			{
				for (int i = 0; i < this.NotificationList.Count; i++)
				{
					Notification n = this.NotificationList[i];
					n.DestinationRect = new Rectangle(this.NotificationArea.X, this.NotificationArea.Y + this.NotificationArea.Height - (i + 1) * 70, n.DestinationRect.Width, n.DestinationRect.Height);
					n.transitionElapsedTime = 0f;
					if (i < this.numentriesToDisplay)
					{
						n.ClickRect = new Rectangle(this.NotificationArea.X, n.DestinationRect.Y, n.ClickRect.Width, n.ClickRect.Height);
					}
				}
			}
		}

		public void SnapToCombat(Planet p)
		{
			AudioManager.PlayCue("sub_bass_whoosh");
			this.screen.SelectedPlanet = p;
			if (!this.screen.SnapBackToSystem)
			{
				this.screen.HeightOnSnap = this.screen.camHeight;
			}
			this.screen.OpenCombatMenu(null);
		}

		public void SnapToPlanet(Planet p)
		{
			AudioManager.PlayCue("sub_bass_whoosh");
			this.screen.SelectedPlanet = p;
			if (!this.screen.SnapBackToSystem)
			{
				this.screen.HeightOnSnap = this.screen.camHeight;
			}
			this.screen.SnapViewPlanet(p);
		}

		public void SnapToSystem(SolarSystem system)
		{
			AudioManager.PlayCue("sub_bass_whoosh");
			this.screen.SnapViewSystem(system);
		}

		public void Update(float elapsedTime)
		{

            float date = this.screen.StarDate;
            if (Timer< date && ResourceManager.EventsDict.ContainsKey(date.ToString()))
            {
                Timer = date;
                ExplorationEvent ReferencedItem1 = ResourceManager.EventsDict[date.ToString()];
                this.screen.ScreenManager.AddScreen(new EventPopup(this.screen, EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty), ReferencedItem1 as ExplorationEvent, (ReferencedItem1 as ExplorationEvent).PotentialOutcomes[0]));
                (ReferencedItem1 as ExplorationEvent).TriggerOutcome(EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty), (ReferencedItem1 as ExplorationEvent).PotentialOutcomes[0]);
            }
            

            
            lock (GlobalStats.NotificationLocker)
			{
				for (int i = 0; i < this.NotificationList.Count; i++)
				{
					Notification n = this.NotificationList[i];
					Notification notification = n;
					notification.transitionElapsedTime = notification.transitionElapsedTime + elapsedTime;
					float amount = (float)Math.Pow((double)(n.transitionElapsedTime / n.transDuration), 2);
					n.ClickRect.Y = (int)MathHelper.SmoothStep((float)n.ClickRect.Y, (float)n.DestinationRect.Y, amount);
                    // ADDED BY SHAHMATT (pause game when there are any notifications)
                    if (GlobalStats.PauseOnNotification && this.screen.viewState > UniverseScreen.UnivScreenState.SystemView && n.ClickRect.Y >= n.DestinationRect.Y)
                        this.screen.Paused = true;                   
                    // END OF ADDED BY SHAHMATT
				}
			}
		}
	}
}