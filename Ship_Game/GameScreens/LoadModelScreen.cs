using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;

namespace Ship_Game
{
	public sealed class LoadModelScreen : GameScreen
	{
		private ShipToolScreen screen;
		//private MainMenuScreen mmscreen;
		//private Submenu subSave;
		private Rectangle Window;
		private Menu1 SaveMenu;
		private Submenu AllSaves;
		private Vector2 TitlePosition;
		private ScrollList SavesSL;
		private Selector selector;
		private FileInfo activeFile;
		private MouseState currentMouse;
		private MouseState previousMouse;

		//private float transitionElapsedTime;

		public LoadModelScreen(ShipToolScreen screen) : base(screen)
		{
			this.screen = screen;
			base.IsPopup = true;
		}

        protected override void Destroy()
        {
            SavesSL?.Dispose(ref SavesSL);
            base.Destroy();
        }

        public override void Draw(SpriteBatch spriteBatch)
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
		    selector?.Draw(ScreenManager.SpriteBatch);
		    base.ScreenManager.SpriteBatch.End();
		}

		public override bool HandleInput(InputState input)
		{
			this.selector = null;
			this.currentMouse = input.MouseCurr;
			Vector2 mousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
			if (input.Escaped || input.RightMouseClick)
			{
				this.ExitScreen();
			}
			foreach (UIButton b in this.Buttons)
			{
				if (!b.Rect.HitTest(mousePos))
				{
					b.State = UIButton.PressState.Default;
				}
				else
				{
					b.State = UIButton.PressState.Hover;
					if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed)
					{
						b.State = UIButton.PressState.Pressed;
					}
					if (this.currentMouse.LeftButton != ButtonState.Released || this.previousMouse.LeftButton != ButtonState.Pressed)
						continue;


                    // @todo What the hell is this stuff doing here?? LoadUniverseScreen in LoadModel???
					if (b.Launches != "Load")
						continue;
					if (activeFile != null)
					{
					    screen?.ExitScreen();
					    ScreenManager.AddScreen(new LoadUniverseScreen(activeFile));
					}
					else
					{
						GameAudio.PlaySfxAsync("UI_Misc20");
					}
					this.ExitScreen();
				}
			}
			this.SavesSL.HandleInput(input);
			foreach (ScrollList.Entry e in this.SavesSL.Copied)
			{
				if (!e.clickRect.HitTest(mousePos))
				{
					e.clickRectHover = 0;
				}
				else
				{
					if (e.clickRectHover == 0)
						GameAudio.PlaySfxAsync("sd_ui_mouseover");

					e.clickRectHover = 1;
					selector = new Selector(e.clickRect);
					if (!input.InGameSelect)
						continue;
					if (!(e.item is ModuleHeader moduleHeader) || !moduleHeader.HandleInput(input, e))
					{
                        var modelData = e.item as ModelData;
						if (modelData == null)
							continue;
						activeFile = modelData.FileInfo;
						GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        //Added by McShooterz: Temp fix for ship tool
						//this.EnterNameArea.Text = Path.GetFileNameWithoutExtension(this.activeFile.Name);
						screen.LoadModel(modelData.model, modelData.FileInfo.NameNoExt());
					}
					else return true; // scrollList entry clicked
				}
			}
			this.previousMouse = input.MousePrev;
			return base.HandleInput(input);
		}

		public override void LoadContent()
		{
			Window               = new Rectangle(0, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 400, 600);
			SaveMenu             = new Menu1(Window);
			Rectangle sub        = new Rectangle(Window.X + 20, Window.Y + 20, Window.Width - 40, 80);
			TitlePosition        = new Vector2(sub.X + 20, sub.Y + 45);
			Rectangle scrollList = new Rectangle(sub.X, sub.Y, sub.Width, Window.Height - 45);
			AllSaves             = new Submenu(scrollList);
			AllSaves.AddTab("Load Model");
			SavesSL              = new ScrollList(AllSaves, 55);
			ModuleHeader original = new ModuleHeader("Vanilla StarDrive");
			SavesSL.AddItem(original);

			foreach (FileInfo info in Dir.GetFiles("Content/Model/Ships", "xnb"))
			{
			    try
			    {
			        string fullDirectory = new DirectoryInfo(Environment.CurrentDirectory).FullName;
			        string fullFile = info.FullName;
			        int fileExtPos = fullFile.LastIndexOf(".", StringComparison.InvariantCulture);
			        if (fileExtPos >= 0)
			        {
			            fullFile = fullFile.Substring(0, fileExtPos);
			        }
			        if (fullFile.StartsWith(fullDirectory))
			        {
                        Log.Info("Relative path: {0}", fullFile.Substring(fullDirectory.Length + 1));
			        }
			        else
			        {
                        Log.Warning("Unable to make relative path");
			        }

			        ModelData data = new ModelData();
			        data.Name     = Path.GetFileNameWithoutExtension(info.FullName);
			        data.model    = TransientContent.Load<Model>(fullFile.Substring(fullDirectory.Length + 1));
			        data.FileInfo = info;
			        SavesSL.Entries[0].AddItem(data);
			    }
			    catch (Exception)
			    {
			    }
			}

			ModuleHeader mods = new ModuleHeader("StarDrive/Ship Tool/Models");
			SavesSL.AddItem(mods);

			foreach (FileInfo info in Dir.GetFiles("Ship Tool/Models", "xnb"))
			{
			    try
			    {
			        string fullDirectory = (new DirectoryInfo(Environment.CurrentDirectory)).FullName;
			        string fullFile = info.FullName;
			        int fileExtPos = fullFile.LastIndexOf(".", StringComparison.InvariantCulture);
			        if (fileExtPos >= 0)
			        {
			            fullFile = fullFile.Substring(0, fileExtPos);
			        }
			        if (fullFile.StartsWith(fullDirectory))
			        {
                        Log.Info("Relative path: {0}", fullFile.Substring(fullDirectory.Length + 1));
			        }
			        else
			        {
                        Log.Info("Unable to make relative path");
			        }
			        ModelData data = new ModelData();
			        data.Name     = Path.GetFileNameWithoutExtension(info.FullName);
			        data.model    = TransientContent.Load<Model>(fullFile.Substring(fullDirectory.Length + 1));
			        data.FileInfo = info;
			        SavesSL.Entries[1].AddItem(data);
			    }
			    catch (Exception)
			    {
			    }
			}
			base.LoadContent();
		}
	}
}