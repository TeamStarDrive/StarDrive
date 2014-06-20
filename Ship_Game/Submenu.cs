using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class Submenu
	{
		public Rectangle Menu;

		private Ship_Game.ScreenManager ScreenManager;

		public List<Submenu.Tab> Tabs = new List<Submenu.Tab>();

		public bool LowRes;

		private Rectangle UpperLeft;

		private Rectangle TR;

		private Rectangle topHoriz;

		private Rectangle botHoriz;

		private Rectangle BL;

		private Rectangle BR;

		private Rectangle VL;

		private Rectangle VR;

		private Rectangle TL;

		private SpriteFont toUse;

		private bool blue;

		private MouseState currentMouse;

		private MouseState previousMouse;

		public Submenu(Ship_Game.ScreenManager sm, Rectangle theMenu)
		{
			if (sm.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280)
			{
				this.LowRes = true;
			}
			if (!this.LowRes)
			{
				this.toUse = Fonts.Arial12Bold;
			}
			else
			{
				this.toUse = Fonts.Arial12Bold;
			}
			this.toUse = Fonts.Pirulen12;
			this.ScreenManager = sm;
			this.Menu = theMenu;
			this.UpperLeft = new Rectangle(theMenu.X, theMenu.Y, ResourceManager.TextureDict["NewUI/submenu_header_left"].Width, ResourceManager.TextureDict["NewUI/submenu_header_left"].Height);
			this.TL = new Rectangle(theMenu.X, theMenu.Y + 25 - 2, ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Height);
			this.TR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Width, theMenu.Y + 25 - 2, ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Height);
			this.BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Height + 2, ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Height);
			this.BR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Width, theMenu.Y + theMenu.Height + 2 - ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Height, ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Height);
			this.topHoriz = new Rectangle(theMenu.X + ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, theMenu.Y + 25 - 2, theMenu.Width - this.TR.Width - ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, 2);
			this.botHoriz = new Rectangle(theMenu.X + this.BL.Width, theMenu.Y + theMenu.Height, theMenu.Width - this.BL.Width - this.BR.Width, 2);
			this.VL = new Rectangle(theMenu.X, theMenu.Y + 25 + this.TR.Height - 2, 2, theMenu.Height - 25 - this.BL.Height - 2);
			this.VR = new Rectangle(theMenu.X + theMenu.Width - 2, theMenu.Y + 25 + this.TR.Height - 2, 2, theMenu.Height - 25 - this.BR.Height - 2);
		}

		public Submenu(bool Blue, Ship_Game.ScreenManager sm, Rectangle theMenu)
		{
			this.blue = Blue;
			if (sm.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280)
			{
				this.LowRes = true;
			}
			if (!this.LowRes)
			{
				this.toUse = Fonts.Arial12Bold;
			}
			else
			{
				this.toUse = Fonts.Arial12Bold;
			}
			this.toUse = Fonts.Pirulen12;
			this.ScreenManager = sm;
			this.Menu = theMenu;
			this.UpperLeft = new Rectangle(theMenu.X, theMenu.Y, ResourceManager.TextureDict["ResearchMenu/submenu_header_left"].Width, ResourceManager.TextureDict["ResearchMenu/submenu_header_left"].Height);
			this.TL = new Rectangle(theMenu.X, theMenu.Y + 25 - 2, ResourceManager.TextureDict["ResearchMenu/submenu_corner_TL"].Width, ResourceManager.TextureDict["ResearchMenu/submenu_corner_TL"].Height);
			this.TR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["ResearchMenu/submenu_corner_TR"].Width, theMenu.Y + 25 - 2, ResourceManager.TextureDict["ResearchMenu/submenu_corner_TR"].Width, ResourceManager.TextureDict["ResearchMenu/submenu_corner_TR"].Height);
			this.BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - ResourceManager.TextureDict["ResearchMenu/submenu_corner_BL"].Height + 2, ResourceManager.TextureDict["ResearchMenu/submenu_corner_BL"].Width, ResourceManager.TextureDict["ResearchMenu/submenu_corner_BL"].Height);
			this.BR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["ResearchMenu/submenu_corner_BR"].Width, theMenu.Y + theMenu.Height + 2 - ResourceManager.TextureDict["ResearchMenu/submenu_corner_BR"].Height, ResourceManager.TextureDict["ResearchMenu/submenu_corner_BR"].Width, ResourceManager.TextureDict["ResearchMenu/submenu_corner_BR"].Height);
			this.topHoriz = new Rectangle(theMenu.X + ResourceManager.TextureDict["ResearchMenu/submenu_corner_TL"].Width, theMenu.Y + 25 - 2, theMenu.Width - this.TR.Width - ResourceManager.TextureDict["ResearchMenu/submenu_corner_TL"].Width, 2);
			this.botHoriz = new Rectangle(theMenu.X + this.BL.Width, theMenu.Y + theMenu.Height, theMenu.Width - this.BL.Width - this.BR.Width, 2);
			this.VL = new Rectangle(theMenu.X, theMenu.Y + 25 + this.TR.Height - 2, 2, theMenu.Height - 25 - this.BL.Height - 2);
			this.VR = new Rectangle(theMenu.X + theMenu.Width - 2, theMenu.Y + 25 + this.TR.Height - 2, 2, theMenu.Height - 25 - this.BR.Height - 2);
		}

		public Submenu(Ship_Game.ScreenManager sm, Rectangle theMenu, bool LowRes)
		{
			this.LowRes = LowRes;
			if (sm.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280)
			{
				LowRes = true;
			}
			if (!LowRes)
			{
				this.toUse = Fonts.Arial12Bold;
			}
			else
			{
				this.toUse = Fonts.Arial12Bold;
			}
			this.toUse = Fonts.Pirulen12;
			this.ScreenManager = sm;
			this.Menu = theMenu;
			this.UpperLeft = new Rectangle(theMenu.X, theMenu.Y, ResourceManager.TextureDict["NewUI/submenu_header_left"].Width, ResourceManager.TextureDict["NewUI/submenu_header_left"].Height);
			this.TL = new Rectangle(theMenu.X, theMenu.Y + 25 - 2, ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Height);
			this.TR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Width, theMenu.Y + 25 - 2, ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Height);
			this.BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Height + 2, ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Height);
			this.BR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Width, theMenu.Y + theMenu.Height + 2 - ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Height, ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Height);
			this.topHoriz = new Rectangle(theMenu.X + ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, theMenu.Y + 25 - 2, theMenu.Width - this.TR.Width - ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, 2);
			this.botHoriz = new Rectangle(theMenu.X + this.BL.Width, theMenu.Y + theMenu.Height, theMenu.Width - this.BL.Width - this.BR.Width, 2);
			this.VL = new Rectangle(theMenu.X, theMenu.Y + 25 + this.TR.Height - 2, 2, theMenu.Height - 25 - this.BL.Height - 2);
			this.VR = new Rectangle(theMenu.X + theMenu.Width - 2, theMenu.Y + 25 + this.TR.Height - 2, 2, theMenu.Height - 25 - this.BR.Height - 2);
		}

		public void AddTab(string Title)
		{
			int w = (int)this.toUse.MeasureString(Title).X;
			float tabX = (float)(this.UpperLeft.X + this.UpperLeft.Width);
			foreach (Submenu.Tab ta in this.Tabs)
			{
				tabX = tabX + (float)ta.tabRect.Width;
				tabX = tabX + (float)ResourceManager.TextureDict["NewUI/submenu_header_right"].Width;
			}
			Rectangle tabRect = new Rectangle((int)tabX, this.UpperLeft.Y, w + 2, 25);
			Submenu.Tab t = new Submenu.Tab()
			{
				tabRect = tabRect,
				Title = Title
			};
			if (this.Tabs.Count != 0)
			{
				t.Selected = false;
			}
			else
			{
				t.Selected = true;
			}
			t.Hover = false;
			this.Tabs.Add(t);
		}

		public void Draw()
		{
			if (!this.blue)
			{
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_corner_TL"], this.TL, Color.White);
				if (this.Tabs.Count > 0 && this.Tabs[0].Selected)
				{
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_left"], this.UpperLeft, Color.White);
				}
				else if (this.Tabs.Count > 0 && !this.Tabs[0].Hover)
				{
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_left_unsel"], this.UpperLeft, Color.White);
				}
				else if (this.Tabs.Count > 0 && this.Tabs[0].Hover)
				{
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_hover_leftedge"], this.UpperLeft, Color.White);
				}
				if (this.Tabs.Count == 1)
				{
					foreach (Submenu.Tab t in this.Tabs)
					{
						this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_middle"], t.tabRect, Color.White);
						Rectangle right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, ResourceManager.TextureDict["NewUI/submenu_header_right"].Width, 25);
						this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_right"], right, Color.White);
						Vector2 textPos = new Vector2((float)t.tabRect.X, (float)(t.tabRect.Y + t.tabRect.Height / 2 - this.toUse.LineSpacing / 2));
						this.ScreenManager.SpriteBatch.DrawString(this.toUse, t.Title, textPos, new Color(255, 239, 208));
					}
				}
				else if (this.Tabs.Count > 1)
				{
					for (int i = 0; i < this.Tabs.Count; i++)
					{
						Submenu.Tab t = this.Tabs[i];
						if (t.Selected)
						{
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_middle"], t.tabRect, Color.White);
							Rectangle right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, ResourceManager.TextureDict["NewUI/submenu_header_right"].Width, 25);
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_right"], right, Color.White);
							if (this.Tabs.Count - 1 > i && !this.Tabs[i + 1].Selected)
							{
								if (this.Tabs[i + 1].Hover)
								{
									this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_hover_left"], right, Color.White);
								}
								else
								{
									this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_rightextend_unsel"], right, Color.White);
								}
							}
						}
						else if (!t.Hover)
						{
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_middle_unsel"], t.tabRect, Color.White);
							Rectangle right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, ResourceManager.TextureDict["NewUI/submenu_header_right_unsel"].Width, 25);
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_right_unsel"], right, Color.White);
							if (this.Tabs.Count - 1 > i)
							{
								if (this.Tabs[i + 1].Selected)
								{
									this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_rightextend"], right, Color.White);
								}
								else if (this.Tabs[i + 1].Hover)
								{
									this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_hover_left"], right, Color.White);
								}
								else
								{
									this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_rightextend_unsel"], right, Color.White);
								}
							}
						}
						else
						{
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_hover_mid"], t.tabRect, Color.White);
							Rectangle right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, ResourceManager.TextureDict["NewUI/submenu_header_hover_right"].Width, 25);
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_hover_right"], right, Color.White);
							if (this.Tabs.Count - 1 > i)
							{
								if (this.Tabs[i + 1].Selected)
								{
									this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_rightextend"], right, Color.White);
								}
								else if (this.Tabs[i + 1].Hover)
								{
									this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_hover_left"], right, Color.White);
								}
								else
								{
									this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_header_rightextend_unsel"], right, Color.White);
								}
							}
						}
						Vector2 textPos = new Vector2((float)t.tabRect.X, (float)(t.tabRect.Y + t.tabRect.Height / 2 - this.toUse.LineSpacing / 2));
						this.ScreenManager.SpriteBatch.DrawString(this.toUse, t.Title, textPos, new Color(255, 239, 208));
					}
				}
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_horiz_vert"], this.topHoriz, Color.White);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_corner_TR"], this.TR, Color.White);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_horiz_vert"], this.botHoriz, Color.White);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_corner_BR"], this.BR, Color.White);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_corner_BL"], this.BL, Color.White);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_horiz_vert"], this.VR, Color.White);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_horiz_vert"], this.VL, Color.White);
				return;
			}
			if (this.blue)
			{
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_corner_TL"], this.TL, Color.White);
				if (this.Tabs.Count > 0 && this.Tabs[0].Selected)
				{
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_left"], this.UpperLeft, Color.White);
				}
				else if (this.Tabs.Count > 0 && !this.Tabs[0].Hover)
				{
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_left_unsel"], this.UpperLeft, Color.White);
				}
				else if (this.Tabs.Count > 0 && this.Tabs[0].Hover)
				{
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_hover_leftedge"], this.UpperLeft, Color.White);
				}
				if (this.Tabs.Count == 1)
				{
					foreach (Submenu.Tab t in this.Tabs)
					{
						this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_middle"], t.tabRect, Color.White);
						Rectangle right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, ResourceManager.TextureDict["ResearchMenu/submenu_header_right"].Width, 25);
						this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_right"], right, Color.White);
						Vector2 textPos = new Vector2((float)t.tabRect.X, (float)(t.tabRect.Y + t.tabRect.Height / 2 - this.toUse.LineSpacing / 2));
						this.ScreenManager.SpriteBatch.DrawString(this.toUse, t.Title, textPos, new Color(255, 239, 208));
					}
				}
				else if (this.Tabs.Count > 1)
				{
					for (int i = 0; i < this.Tabs.Count; i++)
					{
						Submenu.Tab t = this.Tabs[i];
						if (t.Selected)
						{
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_middle"], t.tabRect, Color.White);
							Rectangle right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, ResourceManager.TextureDict["ResearchMenu/submenu_header_right"].Width, 25);
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_right"], right, Color.White);
							if (this.Tabs.Count - 1 > i && !this.Tabs[i + 1].Selected)
							{
								if (this.Tabs[i + 1].Hover)
								{
									this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_hover_left"], right, Color.White);
								}
								else
								{
									this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_rightextend_unsel"], right, Color.White);
								}
							}
						}
						else if (!t.Hover)
						{
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_middle_unsel"], t.tabRect, Color.White);
							Rectangle right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, ResourceManager.TextureDict["ResearchMenu/submenu_header_right_unsel"].Width, 25);
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_right_unsel"], right, Color.White);
							if (this.Tabs.Count - 1 > i)
							{
								if (this.Tabs[i + 1].Selected)
								{
									this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_rightextend"], right, Color.White);
								}
								else if (this.Tabs[i + 1].Hover)
								{
									this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_hover_left"], right, Color.White);
								}
								else
								{
									this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_rightextend_unsel"], right, Color.White);
								}
							}
						}
						else
						{
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_hover_mid"], t.tabRect, Color.White);
							Rectangle right = new Rectangle(t.tabRect.X + t.tabRect.Width, t.tabRect.Y, ResourceManager.TextureDict["ResearchMenu/submenu_header_hover_right"].Width, 25);
							this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_hover_right"], right, Color.White);
							if (this.Tabs.Count - 1 > i)
							{
								if (this.Tabs[i + 1].Selected)
								{
									this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_rightextend"], right, Color.White);
								}
								else if (this.Tabs[i + 1].Hover)
								{
									this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_hover_left"], right, Color.White);
								}
								else
								{
									this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_header_rightextend_unsel"], right, Color.White);
								}
							}
						}
						Vector2 textPos = new Vector2((float)t.tabRect.X, (float)(t.tabRect.Y + t.tabRect.Height / 2 - this.toUse.LineSpacing / 2));
						this.ScreenManager.SpriteBatch.DrawString(this.toUse, t.Title, textPos, new Color(255, 239, 208));
					}
				}
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_horiz_vert"], this.topHoriz, Color.White);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_corner_TR"], this.TR, Color.White);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_horiz_vert"], this.botHoriz, Color.White);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_corner_BR"], this.BR, Color.White);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_corner_BL"], this.BL, Color.White);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_horiz_vert"], this.VR, Color.White);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/submenu_horiz_vert"], this.VL, Color.White);
			}
		}

		public void HandleInput(object caller)
		{
			this.currentMouse = Mouse.GetState();
			Vector2 MousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
			for (int i = 0; i < this.Tabs.Count; i++)
			{
				Submenu.Tab t = this.Tabs[i];
				if (!HelperFunctions.CheckIntersection(t.tabRect, MousePos))
				{
					t.Hover = false;
				}
				else
				{
					t.Hover = true;
					if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
					{
						AudioManager.PlayCue("sd_ui_accept_alt3");
						t.Selected = true;
						foreach (Submenu.Tab t1 in this.Tabs)
						{
							if (t1 == t)
							{
								continue;
							}
							t1.Selected = false;
						}
						if (caller is ColonyScreen)
						{
							(caller as ColonyScreen).ResetLists();
						}
						if (caller is ShipDesignScreen)
						{
							(caller as ShipDesignScreen).ResetLists();
						}
						if (caller is RaceDesignScreen)
						{
							(caller as RaceDesignScreen).ResetLists();
						}
						if (caller is FleetDesignScreen)
						{
							(caller as FleetDesignScreen).PopulateShipSL();
						}
					}
				}
			}
			this.previousMouse = this.currentMouse;
		}

		public void HandleInputNoReset(object caller)
		{
			this.currentMouse = Mouse.GetState();
			Vector2 MousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
			foreach (Submenu.Tab t in this.Tabs)
			{
				if (!HelperFunctions.CheckIntersection(t.tabRect, MousePos))
				{
					t.Hover = false;
				}
				else
				{
					t.Hover = true;
					if (this.currentMouse.LeftButton != ButtonState.Pressed || this.previousMouse.LeftButton != ButtonState.Released)
					{
						continue;
					}
					t.Selected = true;
					foreach (Submenu.Tab t1 in this.Tabs)
					{
						if (t1 == t)
						{
							continue;
						}
						t1.Selected = false;
					}
				}
			}
			this.previousMouse = this.currentMouse;
		}

		public class Tab
		{
			public string Title;

			public Rectangle tabRect;

			public bool Selected;

			public bool Hover;

			public Tab()
			{
			}
		}
	}
}