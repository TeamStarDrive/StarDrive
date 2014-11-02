using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class DesignManager : GameScreen, IDisposable
	{
		private Vector2 Cursor = Vector2.Zero;

		private ShipDesignScreen screen;

		private string ShipName;

		private Selector selector;

		private Submenu SaveShips;

		private Menu1 SaveMenu;

		private Rectangle Window = new Rectangle();

		private Vector2 TitlePosition = new Vector2();

		private Vector2 EnternamePos = new Vector2();

		private UITextEntry EnterNameArea = new UITextEntry();

		private List<UIButton> Buttons = new List<UIButton>();

		private UIButton Save;

		//private UIButton Load;

		//private UIButton Options;

		//private UIButton Exit;

		private Submenu subAllDesigns;

		private ScrollList ShipDesigns;

		private MouseState currentMouse;

		private MouseState previousMouse;

		public DesignManager(ShipDesignScreen screen, string txt)
		{
			this.ShipName = txt;
			this.screen = screen;
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
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
			this.SaveMenu.Draw();
			this.SaveShips.Draw();
			this.EnterNameArea.Draw(Fonts.Arial20Bold, base.ScreenManager.SpriteBatch, this.EnternamePos, gameTime, (this.EnterNameArea.Hover ? Color.White : new Color(255, 239, 208)));
			this.subAllDesigns.Draw();
			this.ShipDesigns.Draw(base.ScreenManager.SpriteBatch);
			Vector2 bCursor = new Vector2((float)(this.subAllDesigns.Menu.X + 20), (float)(this.subAllDesigns.Menu.Y + 20));
			for (int i = this.ShipDesigns.indexAtTop; i < this.ShipDesigns.Entries.Count && i < this.ShipDesigns.indexAtTop + this.ShipDesigns.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.ShipDesigns.Entries[i];
				bCursor.Y = (float)e.clickRect.Y;
                //Changes by McShooterz: Prevent any error caused by a missing icon file
                ShipData shipdata;
                if (ResourceManager.HullsDict.TryGetValue((e.item as Ship).GetShipData().Hull, out shipdata))
                {
                    Texture2D Icon;
                    if(ResourceManager.TextureDict.TryGetValue(shipdata.IconPath, out Icon))
                        base.ScreenManager.SpriteBatch.Draw(Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                    else
                        base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ShipIcons/shuttle"], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                }
                else
                    base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ShipIcons/shuttle"], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
				Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (e.item as Ship).Name, tCursor, Color.White);
				tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, (e.item as Ship).Role, tCursor, Color.Orange);
				if (e.Plus != 0)
				{
					if (e.PlusHover != 0)
					{
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add_hover2"], e.addRect, Color.White);
					}
					else
					{
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_add"], e.addRect, Color.White);
					}
				}
				if (e.Edit != 0)
				{
					if (e.EditHover != 0)
					{
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit_hover2"], e.editRect, Color.White);
					}
					else
					{
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_build_edit"], e.editRect, Color.White);
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
        ~DesignManager() {
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
			this.selector = null;
			for (int i = 0; i < this.ShipDesigns.Entries.Count; i++)
			{
				ScrollList.Entry e = this.ShipDesigns.Entries[i];
				if (!HelperFunctions.CheckIntersection(e.clickRect, MousePos))
				{
					e.clickRectHover = 0;
				}
				else
				{
					this.selector = new Selector(base.ScreenManager, e.clickRect);
					if (e.clickRectHover == 0)
					{
						AudioManager.PlayCue("sd_ui_mouseover");
					}
					e.clickRectHover = 1;
					if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
					{
						this.EnterNameArea.Text = (e.item as Ship).Name;
						AudioManager.PlayCue("sd_ui_accept_alt3");
					}
				}
			}
			foreach (UIButton b in this.Buttons)
			{
				if (!HelperFunctions.CheckIntersection(b.Rect, MousePos))
				{
					b.State = UIButton.PressState.Normal;
				}
				else
				{
					if (b.State != UIButton.PressState.Hover && b.State != UIButton.PressState.Pressed)
					{
						AudioManager.PlayCue("mouse_over4");
					}
					b.State = UIButton.PressState.Hover;
					if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed)
					{
						b.State = UIButton.PressState.Pressed;
					}
					if (this.currentMouse.LeftButton != ButtonState.Pressed || this.previousMouse.LeftButton != ButtonState.Released)
					{
						continue;
					}
					string text = b.Text;
					if (text == null || !(text == "Save"))
					{
						continue;
					}
					AudioManager.PlayCue("sd_ui_accept_alt3");
					GlobalStats.TakingInput = false;
					this.EnterNameArea.HandlingInput = false;
					this.TrySave();
				}
			}
			this.EnterNameArea.ClickableArea = new Rectangle((int)this.EnternamePos.X, (int)this.EnternamePos.Y, 200, 30);
			if (!HelperFunctions.CheckIntersection(this.EnterNameArea.ClickableArea, MousePos))
			{
				this.EnterNameArea.Hover = false;
			}
			else
			{
				this.EnterNameArea.Hover = true;
				if (this.currentMouse.LeftButton == ButtonState.Released && this.previousMouse.LeftButton == ButtonState.Pressed)
				{
					this.EnterNameArea.HandlingInput = true;
				}
			}
			if (!this.EnterNameArea.HandlingInput)
			{
				GlobalStats.TakingInput = false;
			}
			else
			{
				GlobalStats.TakingInput = true;
				this.EnterNameArea.HandleTextInput(ref this.EnterNameArea.Text);
				if (input.CurrentKeyboardState.IsKeyDown(Keys.Enter))
				{
					this.EnterNameArea.HandlingInput = false;
				}
			}
			this.previousMouse = input.LastMouseState;
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			this.Window = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 250, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 500, 600);
			this.SaveMenu = new Menu1(base.ScreenManager, this.Window);
			Rectangle sub = new Rectangle(this.Window.X + 20, this.Window.Y + 20, this.Window.Width - 40, 80);
			this.SaveShips = new Submenu(base.ScreenManager, sub);
			this.SaveShips.AddTab("Save Ship Design");
			Vector2 Cursor = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 84), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 100));
			this.TitlePosition = new Vector2((float)(sub.X + 20), (float)(sub.Y + 45));
			Rectangle scrollList = new Rectangle(sub.X, sub.Y + 90, sub.Width, this.Window.Height - sub.Height - 50);
			this.subAllDesigns = new Submenu(base.ScreenManager, scrollList);
			this.subAllDesigns.AddTab("All Designs");
			this.ShipDesigns = new ScrollList(this.subAllDesigns);
			foreach (KeyValuePair<string, Ship_Game.Gameplay.Ship> Ship in ResourceManager.ShipsDict)
			{
				this.ShipDesigns.AddItem(Ship.Value);
			}
			this.EnternamePos = this.TitlePosition;
			this.EnterNameArea.ClickableArea = new Rectangle((int)(this.EnternamePos.X + Fonts.Arial20Bold.MeasureString("Design Name: ").X), (int)this.EnternamePos.Y - 2, 256, Fonts.Arial20Bold.LineSpacing);
			this.EnterNameArea.Text = this.ShipName;
			this.Save = new UIButton()
			{
				Rect = new Rectangle(sub.X + sub.Width - 88, this.EnterNameArea.ClickableArea.Y - 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"],
				Text = "Save"
			};
			this.Buttons.Add(this.Save);
			Cursor.Y = Cursor.Y + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height + 15);
			base.LoadContent();
		}

		private void OverWriteAccepted(object sender, EventArgs e)
		{
			AudioManager.PlayCue("echo_affirm1");
			if (this.screen != null)
			{
				this.screen.SaveShipDesign(this.EnterNameArea.Text);
			}
			Empire emp = EmpireManager.GetEmpireByName(this.screen.EmpireUI.screen.PlayerLoyalty);
			foreach (Planet p in emp.GetPlanets())
			{
				foreach (QueueItem qi in p.ConstructionQueue)
				{
					if (!qi.isShip || !(qi.sData.Name == this.EnterNameArea.Text))
					{
						continue;
					}
					qi.sData = ResourceManager.ShipsDict[this.EnterNameArea.Text].GetShipData();
					qi.Cost = ResourceManager.ShipsDict[this.EnterNameArea.Text].GetCost(emp);
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

		private void TrySave()
		{
			bool needOverWriteConfirmation = true;
			bool SaveOK = true;
			bool Reserved = false;
			foreach (KeyValuePair<string, Ship_Game.Gameplay.Ship> Ship in ResourceManager.ShipsDict)
			{
				if (this.EnterNameArea.Text != Ship.Value.Name)
				{
					continue;
				}
				needOverWriteConfirmation = true;
				SaveOK = false;
				if (!Ship.Value.reserved)
				{
					continue;
				}
				Reserved = true;
			}
			if (Reserved && !this.screen.EmpireUI.screen.Debug)
			{
				AudioManager.PlayCue("UI_Misc20");
				MessageBoxScreen messageBox = new MessageBoxScreen(string.Concat(this.EnterNameArea.Text, " is a reserved ship name and you cannot overwrite this design"));
				base.ScreenManager.AddScreen(messageBox);
				return;
			}
			if (!SaveOK)
			{
				if (needOverWriteConfirmation)
				{
					MessageBoxScreen messageBox = new MessageBoxScreen("Design name already exists.  Overwrite?");
					messageBox.Accepted += new EventHandler<EventArgs>(this.OverWriteAccepted);
					base.ScreenManager.AddScreen(messageBox);
				}
				return;
			}
			AudioManager.PlayCue("echo_affirm1");
			if (this.screen != null)
			{
				this.screen.SaveShipDesign(this.EnterNameArea.Text);
			}
			this.ExitScreen();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}