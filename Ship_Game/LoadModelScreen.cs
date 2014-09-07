using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ship_Game
{
	public class LoadModelScreen : GameScreen, IDisposable
	{
		private Vector2 Cursor = Vector2.Zero;

		private ShipToolScreen screen;

		private List<UIButton> Buttons = new List<UIButton>();

		//private MainMenuScreen mmscreen;

		//private Submenu subSave;

		private Rectangle Window;

		private Menu1 SaveMenu;

		private Submenu AllSaves;

		private Vector2 TitlePosition;

		private Vector2 EnternamePos;

		private ScrollList SavesSL;

		private ContentManager LocalContent;

		private Selector selector;

		private FileInfo activeFile;

		private MouseState currentMouse;

		private MouseState previousMouse;

		//private float transitionElapsedTime;

		public LoadModelScreen(ShipToolScreen screen)
		{
			this.screen = screen;
			base.IsPopup = true;
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
			base.ScreenManager.SpriteBatch.Begin();
			this.SaveMenu.Draw();
			this.AllSaves.Draw();
			Vector2 bCursor = new Vector2((float)(this.AllSaves.Menu.X + 20), (float)(this.AllSaves.Menu.Y + 20));
			for (int i = this.SavesSL.indexAtTop; i < this.SavesSL.Copied.Count && i < this.SavesSL.indexAtTop + this.SavesSL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.SavesSL.Copied[i];
				bCursor.Y = (float)e.clickRect.Y;
				if (!(e.item is ModuleHeader))
				{
					ModelData data = e.item as ModelData;
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ShipIcons/Wisp"], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, data.Name, tCursor, Color.Orange);
				}
				else
				{
					(e.item as ModuleHeader).Draw(base.ScreenManager, bCursor);
				}
			}
			this.SavesSL.Draw(base.ScreenManager.SpriteBatch);
			foreach (UIButton b in this.Buttons)
			{
				b.Draw(base.ScreenManager.SpriteBatch);
			}
			if (this.selector != null)
			{
				this.selector.Draw();
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
        ~LoadModelScreen() {
            //should implicitly do the same thing as the original bad finalize
        }

		public override void HandleInput(InputState input)
		{
			this.selector = null;
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
					string launches = b.Launches;
					if (launches == null || !(launches == "Load"))
					{
						continue;
					}
					if (this.activeFile != null)
					{
						if (this.screen != null)
						{
							this.screen.ExitScreen();
						}
						base.ScreenManager.AddScreen(new LoadUniverseScreen(this.activeFile));
						/*if (this.mmscreen != null)  //would never have happened
						{
							this.mmscreen.ExitScreen();
						}*/
					}
					else
					{
						AudioManager.PlayCue("UI_Misc20");
					}
					this.ExitScreen();
				}
			}
			this.SavesSL.HandleInput(input);
			foreach (ScrollList.Entry e in this.SavesSL.Copied)
			{
				if (!HelperFunctions.CheckIntersection(e.clickRect, MousePos))
				{
					e.clickRectHover = 0;
				}
				else
				{
					if (e.clickRectHover == 0)
					{
						AudioManager.PlayCue("sd_ui_mouseover");
					}
					e.clickRectHover = 1;
					this.selector = new Selector(base.ScreenManager, e.clickRect);
					if (!input.InGameSelect)
					{
						continue;
					}
					if (!(e.item is ModuleHeader) || !(e.item as ModuleHeader).HandleInput(input, e))
					{
						if (!(e.item is ModelData))
						{
							continue;
						}
						this.activeFile = (e.item as ModelData).FileInfo;
						AudioManager.PlayCue("sd_ui_accept_alt3");
                        //Added by McShooterz: Temp fix for ship tool
						//this.EnterNameArea.Text = Path.GetFileNameWithoutExtension(this.activeFile.Name);
						this.screen.LoadModel((e.item as ModelData).model, Path.GetFileNameWithoutExtension((e.item as ModelData).FileInfo.Name));
					}
					else
					{
						return;
					}
				}
			}
			this.previousMouse = input.LastMouseState;
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			this.LocalContent = new ContentManager(Game1.Instance.Services, "");
			this.Window = new Rectangle(0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 400, 600);
			this.SaveMenu = new Menu1(base.ScreenManager, this.Window);
			Rectangle sub = new Rectangle(this.Window.X + 20, this.Window.Y + 20, this.Window.Width - 40, 80);
			Vector2 vector2 = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 84), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 100));
			this.TitlePosition = new Vector2((float)(sub.X + 20), (float)(sub.Y + 45));
			Rectangle scrollList = new Rectangle(sub.X, sub.Y, sub.Width, this.Window.Height - 45);
			this.AllSaves = new Submenu(base.ScreenManager, scrollList);
			this.AllSaves.AddTab("Load Model");
			this.SavesSL = new ScrollList(this.AllSaves, 55);
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			List<ModelData> modelDatas = new List<ModelData>();
			ModuleHeader original = new ModuleHeader("Vanilla StarDrive");
			this.SavesSL.AddItem(original);
			FileInfo[] filesFromDirectoryAndSubs = HelperFunctions.GetFilesFromDirectoryAndSubs("Content/Model/Ships");
			for (int i = 0; i < (int)filesFromDirectoryAndSubs.Length; i++)
			{
				FileInfo FI = filesFromDirectoryAndSubs[i];
				Stream file = FI.OpenRead();
				ModelData data = new ModelData();
				try
				{
					string fullDirectory = (new DirectoryInfo(Environment.CurrentDirectory)).FullName;
					string fullFile = FI.FullName;
					int fileExtPos = fullFile.LastIndexOf(".");
					if (fileExtPos >= 0)
					{
						fullFile = fullFile.Substring(0, fileExtPos);
					}
					if (fullFile.StartsWith(fullDirectory))
					{
						Console.WriteLine("Relative path: {0}", fullFile.Substring(fullDirectory.Length + 1));
					}
					else
					{
						Console.WriteLine("Unable to make relative path");
					}
					data.Name = Path.GetFileNameWithoutExtension(FI.FullName);
					data.model = this.LocalContent.Load<Model>(fullFile.Substring(fullDirectory.Length + 1));
					data.FileInfo = FI;
					this.SavesSL.Entries[0].AddItem(data);
				}
				catch
				{
                    continue;
				}
				file.Close();
				file.Dispose();
			//Label0:
              //  continue;
			}
			ModuleHeader Mods = new ModuleHeader("StarDrive/Ship Tool/Models");
			this.SavesSL.AddItem(Mods);
			FileInfo[] fileInfoArray = HelperFunctions.GetFilesFromDirectoryAndSubs("Ship Tool/Models");
			for (int j = 0; j < (int)fileInfoArray.Length; j++)
			{
				FileInfo FI = fileInfoArray[j];
				Stream file = FI.OpenRead();
				ModelData data = new ModelData();
				try
				{
					string fullDirectory = (new DirectoryInfo(Environment.CurrentDirectory)).FullName;
					string fullFile = FI.FullName;
					int fileExtPos = fullFile.LastIndexOf(".");
					if (fileExtPos >= 0)
					{
						fullFile = fullFile.Substring(0, fileExtPos);
					}
					if (fullFile.StartsWith(fullDirectory))
					{
						Console.WriteLine("Relative path: {0}", fullFile.Substring(fullDirectory.Length + 1));
					}
					else
					{
						Console.WriteLine("Unable to make relative path");
					}
					data.Name = Path.GetFileNameWithoutExtension(FI.FullName);
					data.model = this.LocalContent.Load<Model>(fullFile.Substring(fullDirectory.Length + 1));
					data.FileInfo = FI;
					this.SavesSL.Entries[1].AddItem(data);
				}
				catch
				{
					continue;
				}
				file.Close();
				file.Dispose();
            //Label1:
              //  continue;
			}
			this.EnternamePos = this.TitlePosition;
            //Added by McShooterz: Temp fix for ship tool
			//this.EnterNameArea = new UITextEntry()
			//{
				//Text = "",
				//ClickableArea = new Rectangle((int)this.EnternamePos.X, (int)this.EnternamePos.Y - 2, 20, Fonts.Arial20Bold.LineSpacing)
			//};
			base.LoadContent();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}