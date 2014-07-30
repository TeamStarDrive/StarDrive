using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Ship_Game
{
	public class LoadDesigns : GameScreen, IDisposable
	{
		private Vector2 Cursor = Vector2.Zero;

		public ToggleButton playerDesignsToggle;

		private bool ShowAllDesigns = true;

		private ShipDesignScreen screen;

		private Rectangle Window = new Rectangle();

		private Vector2 TitlePosition = new Vector2();

		private Vector2 EnternamePos = new Vector2();

		private UITextEntry EnterNameArea = new UITextEntry();

		private List<UIButton> Buttons = new List<UIButton>();

		//private UIButton Save;

		private UIButton Load;

		//private UIButton Options;

		//private UIButton Exit;

		private Menu1 loadMenu;

		private Submenu SaveShips;

		private ScrollList ShipDesigns;

		private MouseState currentMouse;

		private MouseState previousMouse;

		public string ShipToDelete = "";

		private Selector selector;

		private ShipData selectedWIP;

		//private bool FirstRun = true;

		private List<UIButton> ShipsToLoad = new List<UIButton>();

		public LoadDesigns(ShipDesignScreen screen)
		{
			this.screen = screen;
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

		private void DeleteAccepted(object sender, EventArgs e)
		{
			AudioManager.PlayCue("echo_affirm");
			ResourceManager.ShipsDict[this.ShipToDelete].Deleted = true;
			this.Buttons.Clear();
			this.ShipsToLoad.Clear();
			this.ShipDesigns.Reset();
			ResourceManager.DeleteShip(this.ShipToDelete);
			this.LoadContent();
		}

		private void DeleteDataAccepted(object sender, EventArgs e)
		{
			AudioManager.PlayCue("echo_affirm");
			this.Buttons.Clear();
			this.ShipsToLoad.Clear();
			this.ShipDesigns.Reset();
			ResourceManager.DeleteShip(this.ShipToDelete);
			this.LoadContent();
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
				}
			}
		}

		public override void Draw(GameTime gameTime)
		{
			base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			base.ScreenManager.SpriteBatch.Begin();
			this.loadMenu.Draw();
			this.SaveShips.Draw();
			this.ShipDesigns.Draw(base.ScreenManager.SpriteBatch);
			this.EnterNameArea.Draw(Fonts.Arial20Bold, base.ScreenManager.SpriteBatch, this.EnternamePos, gameTime, (this.EnterNameArea.Hover ? Color.White : new Color(255, 239, 208)));
			Vector2 bCursor = new Vector2((float)(this.SaveShips.Menu.X + 20), (float)(this.SaveShips.Menu.Y + 20));
			for (int i = this.ShipDesigns.indexAtTop; i < this.ShipDesigns.Copied.Count && i < this.ShipDesigns.indexAtTop + this.ShipDesigns.entriesToDisplay; i++)
			{
				bCursor = new Vector2((float)(this.SaveShips.Menu.X + 20), (float)(this.SaveShips.Menu.Y + 20));
				ScrollList.Entry e = this.ShipDesigns.Copied[i];
				bCursor.Y = (float)e.clickRect.Y;
				if (e.item is ModuleHeader)
				{
					(e.item as ModuleHeader).Draw(base.ScreenManager, bCursor);
				}
				else if (!(e.item is ShipData))
				{
					bCursor.X = bCursor.X + 15f;
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.HullsDict[(e.item as Ship).GetShipData().Hull].IconPath], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (e.item as Ship).Name, tCursor, Color.White);
					tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, (e.item as Ship).Role, tCursor, Color.Orange);
					if (e.clickRectHover == 1 && !(e.item as Ship).reserved && !(e.item as Ship).FromSave)
					{
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_delete_hover1"], e.cancel, Color.White);
						if (HelperFunctions.CheckIntersection(e.cancel, new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y)))
						{
							base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_delete_hover2"], e.cancel, Color.White);
							ToolTip.CreateTooltip(78, base.ScreenManager);
						}
					}
					else if (!(e.item as Ship).reserved && !(e.item as Ship).FromSave)
					{
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_delete"], e.cancel, Color.White);
					}
				}
				else
				{
					bCursor.X = bCursor.X + 15f;
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.HullsDict[(e.item as ShipData).Hull].IconPath], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (e.item as ShipData).Name, tCursor, Color.White);
					tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, (e.item as ShipData).Role, tCursor, Color.Orange);
					if (e.clickRectHover != 1)
					{
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_delete"], e.cancel, Color.White);
					}
					else
					{
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_delete_hover1"], e.cancel, Color.White);
						if (HelperFunctions.CheckIntersection(e.cancel, new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y)))
						{
							base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_queue_delete_hover2"], e.cancel, Color.White);
							ToolTip.CreateTooltip(78, base.ScreenManager);
						}
					}
				}
			}
			if (this.selector != null)
			{
				this.selector.Draw();
			}
			foreach (UIButton b in this.Buttons)
			{
				b.Draw(base.ScreenManager.SpriteBatch);
			}
			this.playerDesignsToggle.Draw(base.ScreenManager);
			ToolTip.Draw(base.ScreenManager);
			base.ScreenManager.SpriteBatch.End();
		}

		public override void ExitScreen()
		{
			base.ExitScreen();
		}

		/*protected override void Finalize()
		{
			try
			{
				this.Dispose(false);
			}
			finally
			{
				base.Finalize();
			}
		}*/
        ~LoadDesigns() {
            //should implicitly do the same thing as the original bad finalize
        }

		public override void HandleInput(InputState input)
		{
			this.currentMouse = input.CurrentMouseState;
			Vector2 MousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
			this.ShipDesigns.HandleInput(input);
			if (input.Escaped || input.RightMouseClick)
			{
				this.ExitScreen();
			}
			foreach (UIButton b in this.Buttons)
			{
				if (!HelperFunctions.CheckIntersection(b.Rect, MousePos))
				{
					b.State = UIButton.PressState.Normal;
				}
				else
				{
					b.State = UIButton.PressState.Hover;
					if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed)
					{
						b.State = UIButton.PressState.Pressed;
					}
					if (this.currentMouse.LeftButton != ButtonState.Released || this.previousMouse.LeftButton != ButtonState.Pressed)
					{
						continue;
					}
					string launches = b.Launches;
					if (launches == null || !(launches == "Load"))
					{
						continue;
					}
					this.LoadShipToScreen();
				}
			}
			this.selector = null;
			for (int i = this.ShipDesigns.indexAtTop; i < this.ShipDesigns.Copied.Count && i < this.ShipDesigns.indexAtTop + this.ShipDesigns.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.ShipDesigns.Copied[i];
				if (e.item is ModuleHeader)
				{
					(e.item as ModuleHeader).HandleInput(input, e);
				}
				else if (e.item is ShipData)
				{
					if (!HelperFunctions.CheckIntersection(e.clickRect, MousePos))
					{
						e.clickRectHover = 0;
					}
					else
					{
						if (HelperFunctions.CheckIntersection(e.cancel, MousePos) && input.InGameSelect)
						{
							this.ShipToDelete = (e.item as ShipData).Name;
							MessageBoxScreen messageBox = new MessageBoxScreen("Confirm Delete:");
							messageBox.Accepted += new EventHandler<EventArgs>(this.DeleteDataAccepted);
							base.ScreenManager.AddScreen(messageBox);
						}
						this.selector = new Selector(base.ScreenManager, e.clickRect);
						if (e.clickRectHover == 0)
						{
							AudioManager.PlayCue("sd_ui_mouseover");
						}
						e.clickRectHover = 1;
						if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
						{
							this.EnterNameArea.Text = (e.item as ShipData).Name;
							this.selectedWIP = e.item as ShipData;
							AudioManager.PlayCue("sd_ui_accept_alt3");
						}
					}
				}
				else if (!HelperFunctions.CheckIntersection(e.clickRect, MousePos))
				{
					e.clickRectHover = 0;
				}
				else
				{
					if (HelperFunctions.CheckIntersection(e.cancel, MousePos) && !(e.item as Ship).reserved && !(e.item as Ship).FromSave && input.InGameSelect)
					{
						this.ShipToDelete = (e.item as Ship).Name;
						MessageBoxScreen messageBox = new MessageBoxScreen("Confirm Delete:");
						messageBox.Accepted += new EventHandler<EventArgs>(this.DeleteAccepted);
						base.ScreenManager.AddScreen(messageBox);
					}
					this.selector = new Selector(base.ScreenManager, e.clickRect);
					if (e.clickRectHover == 0)
					{
						AudioManager.PlayCue("sd_ui_mouseover");
					}
					e.clickRectHover = 1;
					if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
					{
						this.EnterNameArea.Text = (e.item as Ship).Name;
						AudioManager.PlayCue("sd_ui_accept_alt3");
					}
				}
			}
			if (this.playerDesignsToggle.HandleInput(input))
			{
				AudioManager.PlayCue("sd_ui_accept_alt3");
				this.ShowAllDesigns = !this.ShowAllDesigns;
				if (this.ShowAllDesigns)
				{
					this.playerDesignsToggle.Active = true;
				}
				else
				{
					this.playerDesignsToggle.Active = false;
				}
				this.ResetSL();
			}
			if (HelperFunctions.CheckIntersection(this.playerDesignsToggle.r, input.CursorPosition))
			{
				ToolTip.CreateTooltip(Localizer.Token(2225), base.ScreenManager);
			}
			this.previousMouse = input.LastMouseState;
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			this.Window = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 250, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 500, 600);
			this.loadMenu = new Menu1(base.ScreenManager, this.Window);
			Rectangle sub = new Rectangle(this.Window.X + 20, this.Window.Y + 60, this.Window.Width - 40, this.Window.Height - 80);
			this.SaveShips = new Submenu(base.ScreenManager, sub);
			this.SaveShips.AddTab(Localizer.Token(198));
			this.ShipDesigns = new ScrollList(this.SaveShips);
			Vector2 Cursor = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 84), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 100));
			this.TitlePosition = new Vector2((float)(this.Window.X + 20), (float)(this.Window.Y + 20));
			string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			Rectangle gridRect = new Rectangle(this.SaveShips.Menu.X + this.SaveShips.Menu.Width - 44, this.SaveShips.Menu.Y, 29, 20);
			this.playerDesignsToggle = new ToggleButton(gridRect, "SelectionBox/button_grid_active", "SelectionBox/button_grid_inactive", "SelectionBox/button_grid_hover", "SelectionBox/button_grid_pressed", "SelectionBox/icon_grid")
			{
				Active = this.ShowAllDesigns
			};
			FileInfo[] textList = HelperFunctions.GetFilesFromDirectory(string.Concat(path, "/StarDrive/WIP"));
			List<ShipData> WIPs = new List<ShipData>();
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileStream stream = fileInfoArray[i].OpenRead();
				ShipData newShipData = (ShipData)ResourceManager.serializer_shipdata.Deserialize(stream);
				stream.Close();
				stream.Dispose();
				if (EmpireManager.GetEmpireByName(this.screen.EmpireUI.screen.PlayerLoyalty).GetHDict().ContainsKey(newShipData.Hull) && EmpireManager.GetEmpireByName(this.screen.EmpireUI.screen.PlayerLoyalty).GetHDict()[newShipData.Hull])
				{
					WIPs.Add(newShipData);
				}
			}
			List<string> ShipRoles = new List<string>();
			if (this.screen != null)
			{
				foreach (KeyValuePair<string, Ship_Game.Gameplay.Ship> Ship in ResourceManager.ShipsDict)
				{
                    if (!EmpireManager.GetEmpireByName(this.screen.EmpireUI.screen.PlayerLoyalty).WeCanBuildThis(Ship.Key) || ShipRoles.Contains(Localizer.GetRole(Ship.Value.Role, EmpireManager.GetEmpireByName(this.screen.EmpireUI.screen.PlayerLoyalty))))
					{
						continue;
					}
                    ShipRoles.Add(Localizer.GetRole(Ship.Value.Role, EmpireManager.GetEmpireByName(this.screen.EmpireUI.screen.PlayerLoyalty)));
                    ModuleHeader mh = new ModuleHeader(Localizer.GetRole(Ship.Value.Role, EmpireManager.GetEmpireByName(this.screen.EmpireUI.screen.PlayerLoyalty)));
					this.ShipDesigns.AddItem(mh);
				}
				if (WIPs.Count > 0)
				{
					ShipRoles.Add("WIP");
					ModuleHeader mh = new ModuleHeader("WIP");
					this.ShipDesigns.AddItem(mh);
				}
				foreach (ScrollList.Entry e in this.ShipDesigns.Entries)
				{
					foreach (KeyValuePair<string, Ship_Game.Gameplay.Ship> Ship in ResourceManager.ShipsDict)
					{
                        if (!EmpireManager.GetEmpireByName(this.screen.EmpireUI.screen.PlayerLoyalty).WeCanBuildThis(Ship.Key) || !(Localizer.GetRole(Ship.Value.Role, EmpireManager.GetEmpireByName(this.screen.EmpireUI.screen.PlayerLoyalty)) == (e.item as ModuleHeader).Text) || Ship.Value.Name == "Subspace Projector" || Ship.Value.Name == "Shipyard" || Ship.Value.Deleted)
						{
							continue;
						}
						if (Ship.Value.reserved || Ship.Value.FromSave)
						{
							e.AddItem(Ship.Value);
						}
						else
						{
							e.AddItemWithCancel(Ship.Value);
						}
					}
					if ((e.item as ModuleHeader).Text != "WIP")
					{
						continue;
					}
					foreach (ShipData data in WIPs)
					{
						e.AddItemWithCancel(data);
					}
				}
			}
			this.EnternamePos = this.TitlePosition;
			this.EnterNameArea.Text = Localizer.Token(199);
			this.Load = new UIButton()
			{
				Rect = new Rectangle(sub.X + sub.Width - 88, (int)this.EnternamePos.Y - 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"],
				Launches = "Load",
				Text = Localizer.Token(8)
			};
			this.Buttons.Add(this.Load);
			Cursor.Y = Cursor.Y + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height + 15);
			base.LoadContent();
		}

		private void LoadShipToScreen()
		{
			if (this.screen != null)
			{
				if (ResourceManager.ShipsDict.ContainsKey(this.EnterNameArea.Text))
				{
					this.screen.ChangeHull(ResourceManager.ShipsDict[this.EnterNameArea.Text].GetShipData());
				}
				else if (this.selectedWIP != null)
				{
					this.screen.ChangeHull(this.selectedWIP);
				}
			}
			this.ExitScreen();
		}

		private string parseText(string text, float Width)
		{
			string line = string.Empty;
			string returnString = string.Empty;
			string[] strArrays = text.Split(new char[] { ' ' });
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				string word = strArrays[i];
				if (Fonts.Arial12Bold.MeasureString(string.Concat(line, word)).Length() > Width)
				{
					returnString = string.Concat(returnString, line, '\n');
					line = string.Empty;
				}
				line = string.Concat(line, word, ' ');
			}
			return string.Concat(returnString, line);
		}

		private void ResetSL()
		{
			this.ShipDesigns.Entries.Clear();
			this.ShipDesigns.Copied.Clear();
			string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			FileInfo[] textList = HelperFunctions.GetFilesFromDirectory(string.Concat(path, "/StarDrive/WIP"));
			List<ShipData> WIPs = new List<ShipData>();
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileStream stream = fileInfoArray[i].OpenRead();
				ShipData newShipData = (ShipData)ResourceManager.serializer_shipdata.Deserialize(stream);
				stream.Close();
				stream.Dispose();
				if (EmpireManager.GetEmpireByName(this.screen.EmpireUI.screen.PlayerLoyalty).GetHDict().ContainsKey(newShipData.Hull) && EmpireManager.GetEmpireByName(this.screen.EmpireUI.screen.PlayerLoyalty).GetHDict()[newShipData.Hull])
				{
					WIPs.Add(newShipData);
				}
			}
			List<string> ShipRoles = new List<string>();
			if (this.screen != null)
			{
				foreach (KeyValuePair<string, Ship_Game.Gameplay.Ship> Ship in ResourceManager.ShipsDict)
				{
					if (!this.ShowAllDesigns && !ResourceManager.ShipsDict[Ship.Key].IsPlayerDesign || !EmpireManager.GetEmpireByName(this.screen.EmpireUI.screen.PlayerLoyalty).WeCanBuildThis(Ship.Key) || ShipRoles.Contains(Localizer.GetRole(Ship.Value.Role, EmpireManager.GetEmpireByName(this.screen.EmpireUI.screen.PlayerLoyalty))))
					{
						continue;
					}
                    ShipRoles.Add(Localizer.GetRole(Ship.Value.Role, EmpireManager.GetEmpireByName(this.screen.EmpireUI.screen.PlayerLoyalty)));
					ModuleHeader mh = new ModuleHeader(Localizer.GetRole(Ship.Value.Role, EmpireManager.GetEmpireByName(this.screen.EmpireUI.screen.PlayerLoyalty)));
					this.ShipDesigns.AddItem(mh);
				}
				if (WIPs.Count > 0)
				{
					ShipRoles.Add("WIP");
					ModuleHeader mh = new ModuleHeader("WIP");
					this.ShipDesigns.AddItem(mh);
				}
				foreach (ScrollList.Entry e in this.ShipDesigns.Entries)
				{
					foreach (KeyValuePair<string, Ship_Game.Gameplay.Ship> Ship in ResourceManager.ShipsDict)
					{
						if (!this.ShowAllDesigns && !ResourceManager.ShipsDict[Ship.Key].IsPlayerDesign || !EmpireManager.GetEmpireByName(this.screen.EmpireUI.screen.PlayerLoyalty).WeCanBuildThis(Ship.Key) || !(Localizer.GetRole(Ship.Value.Role, EmpireManager.GetEmpireByName(this.screen.EmpireUI.screen.PlayerLoyalty)) == (e.item as ModuleHeader).Text) || Ship.Value.Name == "Subspace Projector" || Ship.Value.Name == "Shipyard" || Ship.Value.Deleted)
						{
							continue;
						}
						if (Ship.Value.reserved || Ship.Value.FromSave)
						{
							e.AddItem(Ship.Value);
						}
						else
						{
							e.AddItemWithCancel(Ship.Value);
						}
					}
					if ((e.item as ModuleHeader).Text != "WIP")
					{
						continue;
					}
					foreach (ShipData data in WIPs)
					{
						e.AddItemWithCancel(data);
					}
				}
			}
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}