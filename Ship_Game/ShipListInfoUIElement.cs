using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game
{
	public class ShipListInfoUIElement : UIElement
	{
		public List<ToggleButton> CombatStatusButtons = new List<ToggleButton>();

		private List<ShipListInfoUIElement.TippedItem> ToolTipItems = new List<ShipListInfoUIElement.TippedItem>();

		private Rectangle SliderRect;

		private Rectangle clickRect;

		private UniverseScreen screen;

		private List<Ship> ShipList = new List<Ship>();

		private Selector sel;

		public Rectangle LeftRect;

		public Rectangle RightRect;

		public Rectangle ShipInfoRect;

		private ScrollList SelectedShipsSL;

		public Rectangle Power;

		public Rectangle Shields;

		public Rectangle Ordnance;

		private ProgressBar pBar;

		private ProgressBar sBar;

		private ProgressBar oBar;

		public ToggleButton gridbutton;

		private Rectangle Housing;

		private SlidingElement sliding_element;

		public List<OrdersButton> Orders = new List<OrdersButton>();

		private Ship HoveredShip;

		private Rectangle DefenseRect;

		private Rectangle TroopRect;

		private bool isFleet;

		private bool AllShipsMine = true;

		private bool ShowModules = true;

		private Ship HoveredShipLast;

		private float HoverOff;

		private string fmt = "0";

		public ShipListInfoUIElement(Rectangle r, Ship_Game.ScreenManager sm, UniverseScreen screen)
		{
			this.Housing = r;
			this.screen = screen;
			this.ScreenManager = sm;
			this.ElementRect = r;
			this.sel = new Selector(this.ScreenManager, r, Color.Black);
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
			this.SliderRect = new Rectangle(r.X - 100, r.Y + r.Height - 140, 530, 130);
			this.LeftRect = new Rectangle(r.X, r.Y + 44, 180, r.Height - 44);
			this.sliding_element = new SlidingElement(this.SliderRect);
			this.RightRect = new Rectangle(this.LeftRect.X + this.LeftRect.Width, this.LeftRect.Y, 220, this.LeftRect.Height);
			float spacing = (float)(this.LeftRect.Height - 26 - 96);
			this.Power = new Rectangle(this.RightRect.X, this.LeftRect.Y + 12, 20, 20);
			Rectangle pbarrect = new Rectangle(this.Power.X + this.Power.Width + 15, this.Power.Y, 150, 18);
			this.pBar = new ProgressBar(pbarrect)
			{
				color = "green"
			};
			ShipListInfoUIElement.TippedItem ti = new ShipListInfoUIElement.TippedItem()
			{
				r = this.Power,
				TIP_ID = 27
			};
			this.Shields = new Rectangle(this.RightRect.X, this.LeftRect.Y + 12 + 20 + (int)spacing, 20, 20);
			Rectangle pshieldsrect = new Rectangle(this.Shields.X + this.Shields.Width + 15, this.Shields.Y, 150, 18);
			this.sBar = new ProgressBar(pshieldsrect)
			{
				color = "blue"
			};
			ti = new ShipListInfoUIElement.TippedItem()
			{
				r = this.Shields,
				TIP_ID = 28
			};
			this.DefenseRect = new Rectangle(this.Housing.X + 11, this.Housing.Y + 110, 22, 22);
			ti = new ShipListInfoUIElement.TippedItem()
			{
				r = this.DefenseRect,
				TIP_ID = 30
			};
			this.TroopRect = new Rectangle(this.Housing.X + 11, this.Housing.Y + 132, 23, 28);
			ti = new ShipListInfoUIElement.TippedItem()
			{
				r = this.TroopRect,
				TIP_ID = 37
			};
			Rectangle gridRect = new Rectangle(this.Housing.X + 16, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 45, 34, 24);
			this.gridbutton = new ToggleButton(gridRect, "SelectionBox/button_grid_active", "SelectionBox/button_grid_inactive", "SelectionBox/button_grid_hover", "SelectionBox/button_grid_pressed", "SelectionBox/icon_grid")
			{
				Active = true
			};
			this.clickRect = new Rectangle(this.ElementRect.X + this.ElementRect.Width - 16, this.ElementRect.Y + this.ElementRect.Height / 2 - 11, 11, 22);
			this.ShipInfoRect = new Rectangle(this.Housing.X + 60, this.Housing.Y + 110, 115, 115);
			Rectangle rectangle = new Rectangle(this.Housing.X + 187, this.Housing.Y + 120 + 20 + (int)spacing + 20 + (int)spacing, 20, 20);
			Vector2 OrdersBarPos = new Vector2((float)(this.Power.X + 12), (float)(this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 45));
			ToggleButton AttackRuns = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_headon");
			this.CombatStatusButtons.Add(AttackRuns);
			AttackRuns.Action = "attack";
			AttackRuns.HasToolTip = true;
			AttackRuns.WhichToolTip = 1;
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton Artillery = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_aft");
			this.CombatStatusButtons.Add(Artillery);
			Artillery.Action = "arty";
			Artillery.HasToolTip = true;
			Artillery.WhichToolTip = 2;
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton HoldPos = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_x");
			this.CombatStatusButtons.Add(HoldPos);
			HoldPos.Action = "hold";
			HoldPos.HasToolTip = true;
			HoldPos.WhichToolTip = 65;
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton OrbitLeft = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_left");
			this.CombatStatusButtons.Add(OrbitLeft);
			OrbitLeft.Action = "orbit_left";
			OrbitLeft.HasToolTip = true;
			OrbitLeft.WhichToolTip = 3;
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton OrbitRight = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_right");
			this.CombatStatusButtons.Add(OrbitRight);
			OrbitRight.Action = "orbit_right";
			OrbitRight.HasToolTip = true;
			OrbitRight.WhichToolTip = 4;
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton Evade = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_stop");
			this.CombatStatusButtons.Add(Evade);
			Evade.Action = "evade";
			Evade.HasToolTip = true;
			Evade.WhichToolTip = 6;
			Rectangle slsubRect = new Rectangle(this.RightRect.X, this.Housing.Y + 110 - 35, this.RightRect.Width - 5, 140);
			Submenu shipssub = new Submenu(this.ScreenManager, slsubRect);
			this.SelectedShipsSL = new ScrollList(shipssub, 24);
		}

		public void ClearShipList()
		{
			this.ShipList.Clear();
		}

		public override void Draw(GameTime gameTime)
		{
			float transitionOffset = MathHelper.SmoothStep(0f, 1f, base.TransitionPosition);
			int columns = this.Orders.Count / 2 + this.Orders.Count % 2;
			if (this.AllShipsMine)
			{
				this.sliding_element.Draw(this.ScreenManager, (int)((float)(columns * 55) * (1f - base.TransitionPosition)) + (this.sliding_element.Open ? 20 - columns : 0));
				foreach (OrdersButton ob in this.Orders)
				{
					Rectangle r = ob.clickRect;
					r.X = r.X - (int)(transitionOffset * 300f);
					ob.Draw(this.ScreenManager, r);
				}
			}
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["SelectionBox/unitselmenu_main"], this.Housing, Color.White);
			string text = (!this.isFleet || this.ShipList.Count <= 0 || this.ShipList.First<Ship>().fleet == null ? "Multiple Ships" : this.ShipList.First<Ship>().fleet.Name);
			Vector2 NamePos = new Vector2((float)(this.Housing.X + 41), (float)(this.Housing.Y + 65));
			this.SelectedShipsSL.Draw(this.ScreenManager.SpriteBatch);
			Vector2 drawCursor = new Vector2((float)this.RightRect.X, (float)(this.RightRect.Y + 10));
			for (int i = this.SelectedShipsSL.indexAtTop; i < this.SelectedShipsSL.Entries.Count && i < this.SelectedShipsSL.indexAtTop + this.SelectedShipsSL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.SelectedShipsSL.Entries[i];
				drawCursor.Y = (float)e.clickRect.Y;
				(e.item as SelectedShipEntry).Update(drawCursor);
				foreach (SkinnableButton button in (e.item as SelectedShipEntry).ShipButtons)
				{
					if (this.HoveredShip == button.ReferenceObject)
					{
						button.Hover = true;
					}
					button.Draw(this.ScreenManager);
				}
			}
			if (this.HoveredShip == null)
			{
				ShipListInfoUIElement hoverOff = this;
				hoverOff.HoverOff = hoverOff.HoverOff + (float)gameTime.ElapsedGameTime.TotalSeconds;
				if (this.HoverOff > 0.5f)
				{
					text = (!this.isFleet || this.ShipList.Count <= 0 || this.ShipList.First<Ship>().fleet == null ? "Multiple Ships" : this.ShipList.First<Ship>().fleet.Name);
					this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, text, NamePos, this.tColor);
					float FleetOrdnance = 0f;
					float FleetOrdnanceMax = 0f;
					foreach (Ship ship in this.ShipList)
					{
						FleetOrdnance = FleetOrdnance + ship.Ordinance;
						FleetOrdnanceMax = FleetOrdnanceMax + ship.OrdinanceMax;
					}
					if (FleetOrdnanceMax > 0f)
					{
						Rectangle pordrect = new Rectangle(45, this.Housing.Y + 115, 130, 18);
						this.oBar = new ProgressBar(pordrect)
						{
							Max = FleetOrdnanceMax,
							Progress = FleetOrdnance,
							color = "brown"
						};
						this.oBar.Draw(this.ScreenManager.SpriteBatch);
						this.Ordnance = new Rectangle(pordrect.X - 25, pordrect.Y, 20, 20);
						this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Modules/Ordnance"], this.Ordnance, Color.White);
					}
				}
			}
			else
			{
				this.HoverOff = 0f;
				this.HoveredShip.RenderOverlay(this.ScreenManager.SpriteBatch, this.ShipInfoRect, this.ShowModules);
				text = this.HoveredShip.VanityName;
				Vector2 tpos = new Vector2((float)(this.Housing.X + 41), (float)(this.Housing.Y + 65));
				string name = (this.HoveredShip.VanityName != "" ? this.HoveredShip.VanityName : this.HoveredShip.Name);
				SpriteFont TitleFont = Fonts.Arial20Bold;
				if (Fonts.Arial20Bold.MeasureString(name).X > 190f)
				{
					TitleFont = Fonts.Arial12Bold;
					tpos.Y = tpos.Y + (float)(Fonts.Arial20Bold.LineSpacing / 2 - TitleFont.LineSpacing / 2);
				}
				this.ScreenManager.SpriteBatch.DrawString(TitleFont, (this.HoveredShip.VanityName != "" ? this.HoveredShip.VanityName : this.HoveredShip.Name), tpos, this.tColor);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_shield"], this.DefenseRect, Color.White);
				Vector2 defPos = new Vector2((float)(this.DefenseRect.X + this.DefenseRect.Width + 2), (float)(this.DefenseRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2));
				SpriteBatch spriteBatch = this.ScreenManager.SpriteBatch;
				SpriteFont arial12Bold = Fonts.Arial12Bold;
				float mechanicalBoardingDefense = this.HoveredShip.MechanicalBoardingDefense + this.HoveredShip.TroopBoardingDefense;
				spriteBatch.DrawString(arial12Bold, mechanicalBoardingDefense.ToString(this.fmt), defPos, Color.White);
				text = HelperFunctions.parseText(Fonts.Arial12, ShipListScreenEntry.GetStatusText(this.HoveredShip), 130f);
				Vector2 ShipStatus = new Vector2((float)(this.sel.Menu.X + this.sel.Menu.Width - 170), NamePos.Y + (float)(Fonts.Arial20Bold.LineSpacing / 2) - Fonts.Arial12.MeasureString(text).Y / 2f + 2f);
				text = HelperFunctions.parseText(Fonts.Arial12, ShipListScreenEntry.GetStatusText(this.HoveredShip), 130f);
				HelperFunctions.ClampVectorToInt(ref ShipStatus);
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, ShipStatus, this.tColor);
				ShipStatus.Y = ShipStatus.Y + Fonts.Arial12Bold.MeasureString(text).Y;
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_troop"], this.TroopRect, Color.White);
				Vector2 troopPos = new Vector2((float)(this.TroopRect.X + this.TroopRect.Width + 2), (float)(this.TroopRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2));
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(this.HoveredShip.TroopList.Count, "/", this.HoveredShip.TroopCapacity), troopPos, Color.White);
				if (this.HoveredShip.Level > 0)
				{
					for (int i = 0; i < this.HoveredShip.Level; i++)
					{
						Rectangle star = new Rectangle(this.ShipInfoRect.X + this.ShipInfoRect.Width - 13 * i, this.ShipInfoRect.Y - 4, 12, 11);
						this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_star"], star, Color.White);
					}
				}
			}
			foreach (ToggleButton button in this.CombatStatusButtons)
			{
				button.Draw(this.ScreenManager);
			}
			this.gridbutton.Draw(this.ScreenManager);
		}

		public override bool HandleInput(InputState input)
		{
			List<Ship> ships = new List<Ship>();
			bool reset = false;
			for (int i = this.SelectedShipsSL.indexAtTop; i < this.SelectedShipsSL.Entries.Count && i < this.SelectedShipsSL.indexAtTop + this.SelectedShipsSL.entriesToDisplay; i++)
			{
				foreach (SkinnableButton button in (this.SelectedShipsSL.Entries[i].item as SelectedShipEntry).ShipButtons)
				{
					if ((button.ReferenceObject as Ship).Active)
					{
						continue;
					}
					reset = true;
					break;
				}
				if (reset)
				{
					break;
				}
			}
			if (reset)
			{
				this.SetShipList(this.ShipList, this.isFleet);
			}
			if (this.screen.SelectedShipList.Count == 0 || this.screen.SelectedShipList.Count == 1)
			{
				return false;
			}
			if (this.ShipList == null || this.ShipList.Count == 0)
			{
				return false;
			}
			if (this.gridbutton.HandleInput(input))
			{
				AudioManager.PlayCue("sd_ui_accept_alt3");
				this.ShowModules = !this.ShowModules;
				if (!this.ShowModules)
				{
					this.gridbutton.Active = false;
				}
				else
				{
					this.gridbutton.Active = true;
				}
				return true;
			}
			if (this.AllShipsMine)
			{
				foreach (ToggleButton button in this.CombatStatusButtons)
				{
					if (!HelperFunctions.CheckIntersection(button.r, input.CursorPosition))
					{
						button.Hover = false;
					}
					else
					{
						button.Hover = true;
						if (button.HasToolTip)
						{
							ToolTip.CreateTooltip(button.WhichToolTip, this.ScreenManager);
						}
						if (input.InGameSelect)
						{
							AudioManager.PlayCue("sd_ui_accept_alt3");
							string action = button.Action;
							string str = action;
							if (action != null)
							{
								if (str == "attack")
								{
									foreach (Ship ship in this.ShipList)
									{
										ship.GetAI().CombatState = CombatState.AttackRuns;
									}
								}
								else if (str == "arty")
								{
									foreach (Ship ship in this.ShipList)
									{
										ship.GetAI().CombatState = CombatState.Artillery;
									}
								}
								else if (str == "hold")
								{
									foreach (Ship ship in this.ShipList)
									{
										ship.GetAI().CombatState = CombatState.HoldPosition;
									}
								}
								else if (str == "orbit_left")
								{
									foreach (Ship ship in this.ShipList)
									{
										ship.GetAI().CombatState = CombatState.OrbitLeft;
									}
								}
								else if (str == "orbit_right")
								{
									foreach (Ship ship in this.ShipList)
									{
										ship.GetAI().CombatState = CombatState.OrbitRight;
									}
								}
								else if (str == "evade")
								{
									foreach (Ship ship in this.ShipList)
									{
										ship.GetAI().CombatState = CombatState.Evade;
									}
								}
							}
						}
					}
					if (this.HoveredShip == null)
					{
						button.Active = false;
					}
					else
					{
						string action1 = button.Action;
						string str1 = action1;
						if (action1 == null)
						{
							continue;
						}
						if (str1 == "attack")
						{
							if (this.HoveredShip.GetAI().CombatState != CombatState.AttackRuns)
							{
								button.Active = false;
							}
							else
							{
								button.Active = true;
							}
						}
						else if (str1 == "arty")
						{
							if (this.HoveredShip.GetAI().CombatState != CombatState.Artillery)
							{
								button.Active = false;
							}
							else
							{
								button.Active = true;
							}
						}
						else if (str1 == "hold")
						{
							if (this.HoveredShip.GetAI().CombatState != CombatState.HoldPosition)
							{
								button.Active = false;
							}
							else
							{
								button.Active = true;
							}
						}
						else if (str1 == "orbit_left")
						{
							if (this.HoveredShip.GetAI().CombatState != CombatState.OrbitLeft)
							{
								button.Active = false;
							}
							else
							{
								button.Active = true;
							}
						}
						else if (str1 != "orbit_right")
						{
							if (str1 == "evade")
							{
								if (this.HoveredShip.GetAI().CombatState != CombatState.Evade)
								{
									button.Active = false;
								}
								else
								{
									button.Active = true;
								}
							}
						}
						else if (this.HoveredShip.GetAI().CombatState != CombatState.OrbitRight)
						{
							button.Active = false;
						}
						else
						{
							button.Active = true;
						}
					}
				}
				if (this.sliding_element.HandleInput(input))
				{
					if (!this.sliding_element.Open)
					{
						base.State = UIElement.ElementState.TransitionOff;
					}
					else
					{
						base.State = UIElement.ElementState.TransitionOn;
					}
					return true;
				}
				if (base.State == UIElement.ElementState.Open)
				{
					bool orderhover = false;
					foreach (OrdersButton ob in this.Orders)
					{
						if (!ob.HandleInput(input, this.ScreenManager))
						{
							continue;
						}
						orderhover = true;
					}
					if (orderhover)
					{
						return true;
					}
				}
			}
			this.SelectedShipsSL.HandleInput(input);
			this.HoveredShipLast = this.HoveredShip;
			this.HoveredShip = null;
			for (int i = this.SelectedShipsSL.indexAtTop; i < this.SelectedShipsSL.Entries.Count && i < this.SelectedShipsSL.indexAtTop + this.SelectedShipsSL.entriesToDisplay; i++)
			{
				try
				{
					foreach (SkinnableButton button in (this.SelectedShipsSL.Entries[i].item as SelectedShipEntry).ShipButtons)
					{
						if (!HelperFunctions.CheckIntersection(button.r, input.CursorPosition))
						{
							button.Hover = false;
						}
						else
						{
							if (this.HoveredShipLast != (Ship)button.ReferenceObject)
							{
								AudioManager.PlayCue("sd_ui_mouseover");
							}
							button.Hover = true;
							this.HoveredShip = (Ship)button.ReferenceObject;
							if (!input.InGameSelect)
							{
								continue;
							}
							this.screen.SelectedFleet = null;
							this.screen.SelectedShipList.Clear();
							this.screen.SelectedShip = this.HoveredShip;
							return true;
						}
					}
				}
				catch
				{
				}
			}
			foreach (ShipListInfoUIElement.TippedItem ti in this.ToolTipItems)
			{
				if (!HelperFunctions.CheckIntersection(ti.r, input.CursorPosition))
				{
					continue;
				}
				ToolTip.CreateTooltip(ti.TIP_ID, this.ScreenManager);
			}
			if (HelperFunctions.CheckIntersection(this.ElementRect, input.CursorPosition))
			{
				return true;
			}
			return false;
		}

		public void SetShipList(List<Ship> shipList, bool isFleet)
		{
			this.Orders.Clear();
			this.isFleet = isFleet;
			if (shipList != this.ShipList)
			{
				this.SelectedShipsSL.indexAtTop = 0;
			}
			this.ShipList = shipList;
			this.SelectedShipsSL.Entries.Clear();
			SelectedShipEntry entry = new SelectedShipEntry();
			bool AllResupply = true;
			this.AllShipsMine = true;
			bool AllFreighters = true;
			for (int i = 0; i < shipList.Count; i++)
			{
				Ship ship = shipList[i];
				SkinnableButton button = new SkinnableButton(new Rectangle(0, 0, 20, 20), string.Concat("TacticalIcons/symbol_", ship.Role))
				{
					IsToggle = false,
					ReferenceObject = ship,
					BaseColor = ship.loyalty.EmpireColor
				};
				if (entry.ShipButtons.Count < 8)
				{
					entry.ShipButtons.Add(button);
				}
				if (entry.ShipButtons.Count == 8 || i == shipList.Count - 1)
				{
					this.SelectedShipsSL.AddItem(entry);
					entry = new SelectedShipEntry();
				}
				if (ship.GetAI().State != AIState.Resupply)
				{
					AllResupply = false;
				}
				if (ship.loyalty != EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty))
				{
					this.AllShipsMine = false;
				}
				if (ship.CargoSpace_Max == 0f)
				{
					AllFreighters = false;
				}
			}
			OrdersButton resupply = new OrdersButton(shipList, Vector2.Zero, OrderType.OrderResupply, 149)
			{
				SimpleToggle = true,
				Active = AllResupply
			};
			this.Orders.Add(resupply);
			OrdersButton SystemDefense = new OrdersButton(shipList, Vector2.Zero, OrderType.EmpireDefense, 150)
			{
				SimpleToggle = true,
				Active = false
			};
			this.Orders.Add(SystemDefense);
			OrdersButton Explore = new OrdersButton(shipList, Vector2.Zero, OrderType.Explore, 136)
			{
				SimpleToggle = true,
				Active = false
			};
			this.Orders.Add(Explore);
			OrdersButton ordersButton = new OrdersButton(shipList, Vector2.Zero, OrderType.DefineAO, 15);
			SystemDefense.SimpleToggle = true;
			if (AllFreighters)
			{
				OrdersButton tf = new OrdersButton(shipList, Vector2.Zero, OrderType.TradeFood, 151)
				{
					SimpleToggle = true
				};
				this.Orders.Add(tf);
				OrdersButton tpass = new OrdersButton(shipList, Vector2.Zero, OrderType.PassTran, 152)
				{
					SimpleToggle = true
				};
				this.Orders.Add(tpass);
			}
			int ex = 0;
			int y = 0;
			for (int i = 0; i < this.Orders.Count; i++)
			{
				OrdersButton ob = this.Orders[i];
				if (i % 2 == 0 && i > 0)
				{
					ex++;
					y = 0;
				}
				ob.clickRect.X = this.ElementRect.X + this.ElementRect.Width + 2 + 52 * ex;
				ob.clickRect.Y = this.sliding_element.Housing.Y + 15 + y * 52;
				y++;
			}
		}

		private struct TippedItem
		{
			public Rectangle r;

			public int TIP_ID;
		}
	}
}