using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
	public class ShipListScreenEntry
	{
		public Ship ship;

		public Rectangle TotalEntrySize;

		public Rectangle SysNameRect;

		public Rectangle ShipNameRect;

		public Rectangle RoleRect;

		public Rectangle OrdersRect;

		public Rectangle RefitRect;

		public Rectangle STRRect;

		public Rectangle MaintRect;

		public Rectangle TroopRect;

		public Rectangle FTLRect;

		public Rectangle STLRect;

		public Rectangle RemainderRect;

		private Rectangle ShipIconRect;

		private UITextEntry ShipNameEntry = new UITextEntry();

		private TexturedButton RefitButton;

		private TexturedButton ScrapButton;

		public ShipListScreen screen;

		public string Status_Text;

		private bool isScuttle;

		public ShipListScreenEntry(Ship s, int x, int y, int width1, int height, ShipListScreen caller)
		{
			this.screen = caller;
			this.ship = s;
			this.TotalEntrySize = new Rectangle(x, y, width1 - 60, height);
			this.SysNameRect = new Rectangle(x, y, (int)((float)this.TotalEntrySize.Width * 0.17f), height);
			this.ShipNameRect = new Rectangle(x + this.SysNameRect.Width, y, (int)((float)this.TotalEntrySize.Width * 0.175f), height);
			this.RoleRect = new Rectangle(x + this.SysNameRect.Width + this.ShipNameRect.Width, y, (int)((float)this.TotalEntrySize.Width * 0.05f), height);
			this.OrdersRect = new Rectangle(x + this.SysNameRect.Width + this.ShipNameRect.Width + this.RoleRect.Width, y, (int)((float)this.TotalEntrySize.Width * 0.175f), height);
			this.RefitRect = new Rectangle(this.OrdersRect.X + this.OrdersRect.Width, y, 125, height);
			this.STRRect = new Rectangle(this.RefitRect.X + this.RefitRect.Width, y, 35, height);
			this.MaintRect = new Rectangle(this.STRRect.X + this.STRRect.Width, y, 35, height);
			this.TroopRect = new Rectangle(this.MaintRect.X + this.MaintRect.Width, y, 35, height);
			this.FTLRect = new Rectangle(this.TroopRect.X + this.TroopRect.Width, y, 35, height);
			this.STLRect = new Rectangle(this.FTLRect.X + this.FTLRect.Width, y, 35, height);
			this.Status_Text = ShipListScreenEntry.GetStatusText(this.ship);
			this.ShipIconRect = new Rectangle(this.ShipNameRect.X + 5, this.ShipNameRect.Y + 2, 28, 28);
			string shipName = (this.ship.VanityName != "" ? this.ship.VanityName : this.ship.Name);
			this.ShipNameEntry.ClickableArea = new Rectangle(this.ShipIconRect.X + this.ShipIconRect.Width + 10, 2 + this.SysNameRect.Y + this.SysNameRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2, (int)Fonts.Arial20Bold.MeasureString(shipName).X, Fonts.Arial20Bold.LineSpacing);
			this.ShipNameEntry.Text = shipName;
			float width = (float)((int)((float)this.OrdersRect.Width * 0.8f));
			while (width % 10f != 0f)
			{
				width = width + 1f;
			}
			Rectangle refit = new Rectangle(this.RefitRect.X + this.RefitRect.Width / 2 - 5 - ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction_hover1"].Width, this.RefitRect.Y + this.RefitRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction_hover2"].Height / 2, ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction_hover2"].Width, ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction_hover2"].Height);
			this.RefitButton = new TexturedButton(refit, "NewUI/icon_queue_rushconstruction", "NewUI/icon_queue_rushconstruction_hover1", "NewUI/icon_queue_rushconstruction_hover2");
			Rectangle rectangle = new Rectangle(this.RefitRect.X + this.RefitRect.Width / 2 + 5, this.RefitRect.Y + this.RefitRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_queue_delete_hover1"].Height / 2, ResourceManager.TextureDict["NewUI/icon_queue_delete_hover1"].Width, ResourceManager.TextureDict["NewUI/icon_queue_delete_hover1"].Height);
			this.ScrapButton = new TexturedButton(refit, "NewUI/icon_queue_delete", "NewUI/icon_queue_delete_hover1", "NewUI/icon_queue_delete_hover2");
			if (this.ship.Role == "station" || this.ship.Role == "platform" || this.ship.Thrust <= 0f)
			{
				this.isScuttle = true;
			}
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager, GameTime gameTime)
		{
			Color TextColor = new Color(255, 239, 208);
			string sysname = (this.ship.GetSystem() != null ? this.ship.GetSystem().Name : Localizer.Token(150));
			if (Fonts.Arial20Bold.MeasureString(sysname).X <= (float)this.SysNameRect.Width)
			{
				Vector2 SysNameCursor = new Vector2((float)(this.SysNameRect.X + this.SysNameRect.Width / 2) - Fonts.Arial12Bold.MeasureString(sysname).X / 2f, (float)(2 + this.SysNameRect.Y + this.SysNameRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, sysname, SysNameCursor, TextColor);
			}
			else
			{
				Vector2 SysNameCursor = new Vector2((float)(this.SysNameRect.X + this.SysNameRect.Width / 2) - Fonts.Arial12Bold.MeasureString(sysname).X / 2f, (float)(2 + this.SysNameRect.Y + this.SysNameRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, sysname, SysNameCursor, TextColor);
			}
			Rectangle rectangle = new Rectangle(this.ShipNameRect.X + 5, this.ShipNameRect.Y + 25, this.ShipNameRect.Height - 50, this.ShipNameRect.Height - 50);
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.HullsDict[this.ship.GetShipData().Hull].IconPath], this.ShipIconRect, Color.White);
			Vector2 rpos = new Vector2()
			{
				X = (float)this.ShipNameEntry.ClickableArea.X,
				Y = (float)this.ShipNameEntry.ClickableArea.Y
			};
			this.ShipNameEntry.Draw(Fonts.Arial12Bold, ScreenManager.SpriteBatch, rpos, gameTime, TextColor);
			Vector2 rolePos = new Vector2((float)(this.RoleRect.X + this.RoleRect.Width / 2) - Fonts.Arial12Bold.MeasureString(Localizer.GetRole(this.ship.Role)).X / 2f, (float)(this.RoleRect.Y + this.RoleRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
			HelperFunctions.ClampVectorToInt(ref rolePos);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.GetRole(this.ship.Role), rolePos, TextColor);
			Vector2 StatusPos = new Vector2((float)(this.OrdersRect.X + this.OrdersRect.Width / 2) - Fonts.Arial12Bold.MeasureString(this.Status_Text).X / 2f, (float)(2 + this.SysNameRect.Y + this.SysNameRect.Height / 2) - Fonts.Arial12Bold.MeasureString(this.Status_Text).Y / 2f);
			HelperFunctions.ClampVectorToInt(ref StatusPos);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.Status_Text, StatusPos, TextColor);
			Vector2 MainPos = new Vector2((float)(this.MaintRect.X + this.MaintRect.Width - 2), (float)(this.MaintRect.Y + this.MaintRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
			Empire e = EmpireManager.GetEmpireByName(this.screen.empUI.screen.PlayerLoyalty);
			float Maint = this.ship.GetMaintCost();
			Maint = Maint + e.data.Traits.MaintMod * Maint;
			MainPos.X = MainPos.X - Fonts.Arial12.MeasureString(Maint.ToString("#.0")).X;
			HelperFunctions.ClampVectorToInt(ref MainPos);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, Maint.ToString("#.0"), MainPos, Color.White);
			Vector2 StrPos = new Vector2((float)(this.STRRect.X + this.STRRect.Width - 2), (float)(this.MaintRect.Y + this.MaintRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
			float x = StrPos.X;
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			float strength = this.ship.GetStrength();
			StrPos.X = x - arial12Bold.MeasureString(strength.ToString("0")).X;
			HelperFunctions.ClampVectorToInt(ref StrPos);
			SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
			SpriteFont arial12 = Fonts.Arial12;
			float single = this.ship.GetStrength();
			spriteBatch.DrawString(arial12, single.ToString("0"), StrPos, Color.White);
			Vector2 TroopPos = new Vector2((float)(this.TroopRect.X + this.TroopRect.Width - 2), (float)(this.MaintRect.Y + this.MaintRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
			//{
            TroopPos.X = TroopPos.X - Fonts.Arial12Bold.MeasureString(string.Concat(this.ship.TroopList.Count, "/", this.ship.TroopCapacity)).X;
			//};
			HelperFunctions.ClampVectorToInt(ref TroopPos);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(this.ship.TroopList.Count, "/", this.ship.TroopCapacity), TroopPos, Color.White);
			Vector2 FTLPos = new Vector2((float)(this.FTLRect.X + this.FTLRect.Width - 2), (float)(this.MaintRect.Y + this.MaintRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
			float x1 = FTLPos.X;
			SpriteFont spriteFont = Fonts.Arial12Bold;
			float fTLSpeed = this.ship.GetFTLSpeed() / 1000f;
			FTLPos.X = x1 - spriteFont.MeasureString(string.Concat(fTLSpeed.ToString("0"), "k")).X;
			HelperFunctions.ClampVectorToInt(ref FTLPos);
			SpriteBatch spriteBatch1 = ScreenManager.SpriteBatch;
			SpriteFont arial121 = Fonts.Arial12;
			float fTLSpeed1 = this.ship.GetFTLSpeed() / 1000f;
			spriteBatch1.DrawString(arial121, string.Concat(fTLSpeed1.ToString("0"), "k"), FTLPos, Color.White);
			Vector2 STLPos = new Vector2((float)(this.STLRect.X + this.STLRect.Width - 2), (float)(this.MaintRect.Y + this.MaintRect.Height / 2 - Fonts.Arial12.LineSpacing / 2));
			float single1 = STLPos.X;
			SpriteFont arial12Bold1 = Fonts.Arial12Bold;
			float sTLSpeed = this.ship.GetSTLSpeed();
			STLPos.X = single1 - arial12Bold1.MeasureString(sTLSpeed.ToString("0")).X;
			HelperFunctions.ClampVectorToInt(ref STLPos);
			SpriteBatch spriteBatch2 = ScreenManager.SpriteBatch;
			SpriteFont spriteFont1 = Fonts.Arial12;
			float sTLSpeed1 = this.ship.GetSTLSpeed();
			spriteBatch2.DrawString(spriteFont1, sTLSpeed1.ToString("0"), STLPos, Color.White);
			if (this.isScuttle)
			{
				float scuttleTimer = this.ship.ScuttleTimer;
			}
			this.RefitButton.Draw(ScreenManager);
			this.ScrapButton.Draw(ScreenManager);
		}

		public static string GetStatusText(Ship ship)
		{
			string which;
			string str;
			string text = "";
			switch (ship.GetAI().State)
			{
				case AIState.DoNothing:
				{
					text = Localizer.Token(183);
					break;
				}
				case AIState.Combat:
				{
					if (ship.GetAI().Intercepting)
					{
						if (ship.GetAI().Target == null)
						{
							break;
						}
						text = string.Concat(Localizer.Token(157), " ", (ship.GetAI().Target as Ship).VanityName);
						break;
					}
					else if (ship.GetAI().Target == null)
					{
						text = Localizer.Token(155);
						text = string.Concat(text, "\n", Localizer.Token(156));
						break;
					}
					else
					{
						text = string.Concat(Localizer.Token(158), " ", (ship.GetAI().Target as Ship).loyalty.data.Traits.Name);
						break;
					}
				}
				case AIState.HoldPosition:
				{
					text = Localizer.Token(180);
					break;
				}
				case AIState.ManualControl:
				{
					text = Localizer.Token(171);
					break;
				}
				case AIState.AwaitingOrders:
				{
					return Localizer.Token(153);
				}
				case AIState.AttackTarget:
				{
					if (ship.GetAI().Target == null)
					{
						text = Localizer.Token(155);
						text = string.Concat(text, "\n", Localizer.Token(156));
						break;
					}
					else
					{
						text = string.Concat(Localizer.Token(154), " ", (ship.GetAI().Target as Ship).VanityName);
						break;
					}
				}
				case AIState.Escort:
				{
					if (ship.GetAI().EscortTarget == null)
					{
						break;
					}
					text = string.Concat(Localizer.Token(179), " ", ship.GetAI().EscortTarget.Name);
					break;
				}
				case AIState.SystemTrader:
				{
					if (ship.GetAI().OrderQueue.Count <= 0)
					{
						text = string.Concat(Localizer.Token(164), "\n", Localizer.Token(165));
						break;
					}
					else
					{
						switch (ship.GetAI().OrderQueue.Last.Value.Plan)
						{
							case ArtificialIntelligence.Plan.PickupGoods:
							{
								which = ship.GetAI().FoodOrProd;
								if (which == "Prod")
								{
									which = "Production";
								}
								text = string.Concat(text, Localizer.Token(159), " ", ship.GetAI().start.Name);
								string pickingup = Localizer.Token(160);
								string str1 = text;
								string[] strArrays = new string[] { str1, "\n", pickingup, " ", null };
								strArrays[4] = (which == "Food" ? Localizer.Token(161) : Localizer.Token(162));
								text = string.Concat(strArrays);
								break;
							}
							case ArtificialIntelligence.Plan.DropOffGoods:
							{
								which = ship.GetAI().FoodOrProd;
								if (which == "Prod")
								{
									which = "Production";
								}
								text = string.Concat(text, Localizer.Token(159), " ", ship.GetAI().end.Name);
								string delivering = Localizer.Token(163);
								string str2 = text;
								string[] strArrays1 = new string[] { str2, "\n", delivering, " ", null };
								strArrays1[4] = (which == "Food" ? Localizer.Token(161) : Localizer.Token(162));
								text = string.Concat(strArrays1);
								break;
							}
						}
					}
					break;
				}
				case AIState.AttackRunner:
				case AIState.PatrolSystem:
				case AIState.Flee:
				case AIState.PirateRaiderCarrier:
				case AIState.AwaitingOffenseOrders:
				case AIState.MineAsteroids:
				case AIState.Intercept:
				case AIState.AssaultPlanet:
				case AIState.Exterminate:
				{
					if (ship.GetAI().OrderQueue.Count <= 0)
					{
						text = ship.GetAI().State.ToString();
						break;
					}
					else
					{
						text = ship.GetAI().OrderQueue.First.Value.Plan.ToString();
						break;
					}
				}
				case AIState.Orbit:
				{
					if (ship.GetAI().OrbitTarget == null)
					{
						text = Localizer.Token(182);
						break;
					}
					else
					{
						text = string.Concat(Localizer.Token(182), " ", ship.GetAI().OrbitTarget.Name);
						break;
					}
				}
				case AIState.PassengerTransport:
				{
					if (ship.GetAI().OrderQueue.Count <= 0)
					{
						text = string.Concat(Localizer.Token(168), "\n", Localizer.Token(165));
						break;
					}
					else
					{
						try
						{
							switch (ship.GetAI().OrderQueue.Last.Value.Plan)
							{
								case ArtificialIntelligence.Plan.PickupPassengers:
								{
									text = string.Concat(text, Localizer.Token(159), " ", ship.GetAI().start.Name);
									text = string.Concat(text, "\n", Localizer.Token(166));
									break;
								}
								case ArtificialIntelligence.Plan.DropoffPassengers:
								{
									text = string.Concat(text, Localizer.Token(159), " ", ship.GetAI().end.Name);
									text = string.Concat(text, "\n", Localizer.Token(167));
									break;
								}
							}
							break;
						}
						catch
						{
							str = "";
						}
						return str;
					}
				}
				case AIState.Colonize:
				{
					if (ship.GetAI().ColonizeTarget == null)
					{
						break;
					}
					text = string.Concat(Localizer.Token(169), " ", ship.GetAI().ColonizeTarget.Name);
					break;
				}
				case AIState.MoveTo:
				{
					if (!(ship.Velocity == Vector2.Zero) || ship.isTurning)
					{
						text = string.Concat(Localizer.Token(187), " ");
						if (ship.GetAI().OrderQueue.Count <= 0)
						{
							IOrderedEnumerable<SolarSystem> sortedList = 
								from system in UniverseScreen.SolarSystemList
								orderby Vector2.Distance(ship.GetAI().MovePosition, system.Position)
								select system;
							text = string.Concat(text, Localizer.Token(189), " ", sortedList.First<SolarSystem>().Name);
							if (sortedList.First<SolarSystem>().ExploredDict[EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty)])
							{
								break;
							}
							text = Localizer.Token(174);
							break;
						}
						else if (ship.GetAI().OrderQueue.Last.Value.Plan != ArtificialIntelligence.Plan.DeployStructure)
						{
							IOrderedEnumerable<SolarSystem> sortedList = 
								from system in UniverseScreen.SolarSystemList
								orderby Vector2.Distance(ship.GetAI().MovePosition, system.Position)
								select system;
							text = string.Concat(text, sortedList.First<SolarSystem>().Name);
							if (sortedList.First<SolarSystem>().ExploredDict[EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty)])
							{
								break;
							}
							text = Localizer.Token(174);
							break;
						}
						else
						{
							text = string.Concat(text, Localizer.Token(188), " ", ResourceManager.ShipsDict[ship.GetAI().OrderQueue.Last.Value.goal.ToBuildUID].Name);
							break;
						}
					}
					else
					{
						text = Localizer.Token(180);
						break;
					}
				}
				case AIState.Explore:
				{
					text = Localizer.Token(174);
					break;
				}
				case AIState.SystemDefender:
				{
					text = Localizer.Token(170);
					break;
				}
				case AIState.Resupply:
				{
					if (ship.GetAI().resupplyTarget == null)
					{
						text = Localizer.Token(173);
						break;
					}
					else
					{
						text = string.Concat(Localizer.Token(172), " ", ship.GetAI().resupplyTarget.Name);
						break;
					}
				}
				case AIState.Rebase:
				{
					text = Localizer.Token(178);
					break;
				}
				case AIState.Bombard:
				{
					if (ship.GetAI().OrderQueue.Count <= 0 || ship.GetAI().OrderQueue.First.Value.TargetPlanet == null)
					{
						break;
					}
					if (Vector2.Distance(ship.Center, ship.GetAI().OrderQueue.First.Value.TargetPlanet.Position) >= 2500f)
					{
						text = string.Concat(Localizer.Token(176), " ", ship.GetAI().OrderQueue.First.Value.TargetPlanet.Name);
						break;
					}
					else
					{
						text = string.Concat(Localizer.Token(175), " ", ship.GetAI().OrderQueue.First.Value.TargetPlanet.Name);
						break;
					}
				}
                case AIState.BombardTroops:
                {
                    if (ship.GetAI().OrderQueue.Count <= 0 || ship.GetAI().OrderQueue.First.Value.TargetPlanet == null)
                    {
                        break;
                    }
                    if (Vector2.Distance(ship.Center, ship.GetAI().OrderQueue.First.Value.TargetPlanet.Position) >= 2500f)
                    {
                        text = string.Concat("Soften", " ", ship.GetAI().OrderQueue.First.Value.TargetPlanet.Name);
                        break;
                    }
                    else
                    {
                        text = string.Concat(Localizer.Token(175), " ", ship.GetAI().OrderQueue.First.Value.TargetPlanet.Name);
                        break;
                    }
                }
				case AIState.Boarding:
				{
					text = Localizer.Token(177);
					break;
				}
				case AIState.ReturnToHangar:
				{
					text = Localizer.Token(181);
					break;
				}
				case AIState.Ferrying:
				{
					text = Localizer.Token(185);
					break;
				}
				case AIState.Refit:
				{
					text = Localizer.Token(184);
					break;
				}
				case AIState.Scrap:
				{
					text = Localizer.Token(186);
					break;
				}
				case AIState.FormationWarp:
				{
					text = "Moving in Formation";
					break;
				}
				case AIState.Scuttle:
				{
					text = string.Concat("Self Destruct: ", ship.ScuttleTimer.ToString("#"));
					break;
				}
				default:
				{
					goto case AIState.Exterminate;
				}
			}
			return text;
		}

		public void HandleInput(InputState input)
		{
			if (this.RefitButton.HandleInput(input))
			{
				AudioManager.PlayCue("echo_affirm");
				this.screen.ScreenManager.AddScreen(new RefitToWindow(this, this.screen));
			}
			if (this.ScrapButton.HandleInput(input))
			{
				if (!this.isScuttle)
				{
					this.Status_Text = ShipListScreenEntry.GetStatusText(this.ship);
				}
				else
				{
					this.Status_Text = ShipListScreenEntry.GetStatusText(this.ship);
				}
				AudioManager.PlayCue("echo_affirm");
				if (!this.isScuttle)
				{
					if (this.ship.GetAI().State == AIState.Scrap)
					{
						this.ship.GetAI().State = AIState.AwaitingOrders;
						this.ship.GetAI().OrderQueue.Clear();
					}
					else
					{
						this.ship.GetAI().OrderScrapShip();
					}
					this.Status_Text = ShipListScreenEntry.GetStatusText(this.ship);
				}
				else
				{
					if (this.ship.ScuttleTimer != -1f)
					{
						this.ship.ScuttleTimer = -1f;
						this.ship.GetAI().State = AIState.AwaitingOrders;
						this.ship.GetAI().HasPriorityOrder = false;
						this.ship.GetAI().OrderQueue.Clear();
					}
					else
					{
						this.ship.ScuttleTimer = 10f;
						this.ship.GetAI().State = AIState.Scuttle;
						this.ship.GetAI().HasPriorityOrder = true;
						this.ship.GetAI().OrderQueue.Clear();
					}
					this.Status_Text = ShipListScreenEntry.GetStatusText(this.ship);
				}
			}
			if (!HelperFunctions.CheckIntersection(this.ShipNameEntry.ClickableArea, input.CursorPosition))
			{
				this.ShipNameEntry.Hover = false;
			}
			else
			{
				this.ShipNameEntry.Hover = true;
				if (input.InGameSelect)
				{
					this.ShipNameEntry.HandlingInput = true;
				}
			}
			if (!this.ShipNameEntry.HandlingInput)
			{
				GlobalStats.TakingInput = false;
				return;
			}
			GlobalStats.TakingInput = true;
			this.ShipNameEntry.HandleTextInput(ref this.ship.VanityName);
			this.ShipNameEntry.Text = this.ship.VanityName;
		}

		public void SetNewPos(int x, int y)
		{
			this.TotalEntrySize = new Rectangle(x, y, this.TotalEntrySize.Width, this.TotalEntrySize.Height);
			this.SysNameRect = new Rectangle(x, y, (int)((float)this.TotalEntrySize.Width * 0.17f), this.TotalEntrySize.Height);
			this.ShipNameRect = new Rectangle(x + this.SysNameRect.Width, y, (int)((float)this.TotalEntrySize.Width * 0.2f), this.TotalEntrySize.Height);
			this.RoleRect = new Rectangle(x + this.SysNameRect.Width + this.ShipNameRect.Width, y, (int)((float)this.TotalEntrySize.Width * 0.05f), this.TotalEntrySize.Height);
			this.OrdersRect = new Rectangle(x + this.SysNameRect.Width + this.ShipNameRect.Width + this.RoleRect.Width, y, (int)((float)this.TotalEntrySize.Width * 0.2f), this.TotalEntrySize.Height);
			this.RefitRect = new Rectangle(this.OrdersRect.X + this.OrdersRect.Width, y, 125, this.TotalEntrySize.Height);
			this.STRRect = new Rectangle(this.RefitRect.X + this.RefitRect.Width, y, 35, this.TotalEntrySize.Height);
			this.MaintRect = new Rectangle(this.STRRect.X + this.STRRect.Width, y, 35, this.TotalEntrySize.Height);
			this.TroopRect = new Rectangle(this.MaintRect.X + this.MaintRect.Width, y, 35, this.TotalEntrySize.Height);
			this.FTLRect = new Rectangle(this.TroopRect.X + this.TroopRect.Width, y, 35, this.TotalEntrySize.Height);
			this.STLRect = new Rectangle(this.FTLRect.X + this.FTLRect.Width, y, 35, this.TotalEntrySize.Height);
			this.ShipIconRect = new Rectangle(this.ShipNameRect.X + 5, this.ShipNameRect.Y + 2, 28, 28);
			string shipName = (this.ship.VanityName != "" ? this.ship.VanityName : this.ship.Name);
			this.ShipNameEntry.ClickableArea = new Rectangle(this.ShipIconRect.X + this.ShipIconRect.Width + 10, 2 + this.SysNameRect.Y + this.SysNameRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2, (int)Fonts.Arial20Bold.MeasureString(shipName).X, Fonts.Arial20Bold.LineSpacing);
			Rectangle refit = new Rectangle(this.RefitRect.X + this.RefitRect.Width / 2 - 5 - ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction_hover1"].Width, this.RefitRect.Y + this.RefitRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction_hover2"].Height / 2, ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction_hover2"].Width, ResourceManager.TextureDict["NewUI/icon_queue_rushconstruction_hover2"].Height);
			this.RefitButton.r = refit;
			Rectangle scrap = new Rectangle(this.RefitRect.X + this.RefitRect.Width / 2 + 5, this.RefitRect.Y + this.RefitRect.Height / 2 - ResourceManager.TextureDict["NewUI/icon_queue_delete_hover1"].Height / 2, ResourceManager.TextureDict["NewUI/icon_queue_delete_hover1"].Width, ResourceManager.TextureDict["NewUI/icon_queue_delete_hover1"].Height);
			this.ScrapButton.r = scrap;
			this.RefitButton.LocalizerTip = 2213;
			this.ScrapButton.LocalizerTip = 2214;
			float width = (float)((int)((float)this.OrdersRect.Width * 0.8f));
			while (width % 10f != 0f)
			{
				width = width + 1f;
			}
		}
	}
}