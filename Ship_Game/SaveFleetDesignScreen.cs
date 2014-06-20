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
	public class SaveFleetDesignScreen : GameScreen, IDisposable
	{
		private Vector2 Cursor = Vector2.Zero;

		private Fleet f;

		private List<UIButton> Buttons = new List<UIButton>();

		//private Submenu subSave;

		private Rectangle Window;

		private Menu1 SaveMenu;

		private Submenu NameSave;

		private Submenu AllSaves;

		private Vector2 TitlePosition;

		private Vector2 EnternamePos;

		private UITextEntry EnterNameArea;

		private ScrollList SavesSL;

		private UIButton Save;

		private MouseState currentMouse;

		private MouseState previousMouse;

		//private float transitionElapsedTime;

		public SaveFleetDesignScreen(Fleet f)
		{
			this.f = f;
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

		private void DoSave()
		{
			FleetDesign d = new FleetDesign()
			{
				Name = this.EnterNameArea.Text
			};
			foreach (FleetDataNode node in this.f.DataNodes)
			{
				d.Data.Add(node);
			}
			d.FleetIconIndex = this.f.FleetIconIndex;
			string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			XmlSerializer Serializer = new XmlSerializer(typeof(FleetDesign));
			TextWriter WriteFileStream = new StreamWriter(string.Concat(path, "/StarDrive/Fleet Designs/", this.EnterNameArea.Text, ".xml"));
			Serializer.Serialize(WriteFileStream, d);
			WriteFileStream.Close();
		}

		public override void Draw(GameTime gameTime)
		{
			base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			base.ScreenManager.SpriteBatch.Begin();
			this.SaveMenu.Draw();
			this.NameSave.Draw();
			this.AllSaves.Draw();
			Vector2 bCursor = new Vector2((float)(this.AllSaves.Menu.X + 20), (float)(this.AllSaves.Menu.Y + 20));
			for (int i = this.SavesSL.indexAtTop; i < this.SavesSL.Entries.Count && i < this.SavesSL.indexAtTop + this.SavesSL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.SavesSL.Entries[i];
				bCursor.Y = (float)e.clickRect.Y;
				if (e.clickRectHover != 0)
				{
					bCursor.Y = (float)e.clickRect.Y;
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ShipIcons/Wisp"], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Path.GetFileNameWithoutExtension((e.item as FileInfo).Name), tCursor, Color.White);
					if (e.clickRect.Y == 0)
					{
					}
				}
				else
				{
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ShipIcons/Wisp"], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Path.GetFileNameWithoutExtension((e.item as FileInfo).Name), tCursor, Color.White);
					if (e.clickRect.Y == 0)
					{
					}
				}
			}
			this.SavesSL.Draw(base.ScreenManager.SpriteBatch);
			this.EnterNameArea.Draw(Fonts.Arial20Bold, base.ScreenManager.SpriteBatch, this.EnternamePos, gameTime, (this.EnterNameArea.Hover ? Color.White : Color.Orange));
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
        ~SaveFleetDesignScreen() {
            //should implicitly do the same thing as the original bad finalize
        }

		public override void HandleInput(InputState input)
		{
			this.currentMouse = input.CurrentMouseState;
			Vector2 MousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
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
					string text = b.Text;
					string str = text;
					if (text == null)
					{
						continue;
					}
					if (str == "Save")
					{
						this.DoSave();
						this.ExitScreen();
					}
					else if (str == "Exit to Windows")
					{
						Game1.Instance.Exit();
					}
				}
			}
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
					this.EnterNameArea.Text = "";
				}
			}
			if (this.EnterNameArea.HandlingInput)
			{
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
			this.Window = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 350, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 700, 600);
			this.SaveMenu = new Menu1(base.ScreenManager, this.Window);
			Rectangle sub = new Rectangle(this.Window.X + 20, this.Window.Y + 20, this.Window.Width - 40, 80);
			this.NameSave = new Submenu(base.ScreenManager, sub);
			this.NameSave.AddTab("Save Fleet As...");
			Vector2 Cursor = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 84), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 100));
			this.TitlePosition = new Vector2((float)(sub.X + 20), (float)(sub.Y + 45));
			Rectangle scrollList = new Rectangle(sub.X, sub.Y + 90, sub.Width, this.Window.Height - sub.Height - 50);
			this.AllSaves = new Submenu(base.ScreenManager, scrollList);
			this.AllSaves.AddTab("All Fleet Designs");
			this.SavesSL = new ScrollList(this.AllSaves);
			string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			FileInfo[] filesFromDirectory = HelperFunctions.GetFilesFromDirectory(string.Concat(path, "/StarDrive/Fleet Designs"));
			for (int i = 0; i < (int)filesFromDirectory.Length; i++)
			{
				FileInfo FI = filesFromDirectory[i];
				this.SavesSL.AddItem(FI);
			}
			FileInfo[] fileInfoArray = HelperFunctions.GetFilesFromDirectory("Content/FleetDesigns");
			for (int j = 0; j < (int)fileInfoArray.Length; j++)
			{
				FileInfo FI = fileInfoArray[j];
				this.SavesSL.AddItem(FI);
			}
			this.EnternamePos = this.TitlePosition;
			this.EnterNameArea = new UITextEntry()
			{
				Text = this.f.Name,
				ClickableArea = new Rectangle((int)this.EnternamePos.X, (int)this.EnternamePos.Y - 2, (int)Fonts.Arial20Bold.MeasureString(this.EnterNameArea.Text).X + 20, Fonts.Arial20Bold.LineSpacing)
			};
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
			base.LoadContent();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}