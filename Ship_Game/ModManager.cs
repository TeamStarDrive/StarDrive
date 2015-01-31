using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace Ship_Game
{
	public class ModManager : GameScreen, IDisposable
	{
		private Vector2 Cursor = Vector2.Zero;

		private List<UIButton> Buttons = new List<UIButton>();

		private MainMenuScreen mmscreen;

		//private Submenu subSave;

		private Rectangle Window;

		private Menu1 SaveMenu;

		private Submenu NameSave;

		private Submenu AllSaves;

		private Vector2 TitlePosition;

		private Vector2 EnternamePos;

		private UITextEntry EnterNameArea;

		private ScrollList ModsSL;

		private UIButton Save;

		private UIButton Disable;

		private UIButton Visit;

		private UIButton shiptool;

		private ModEntry ActiveEntry;

		private Selector selector;

		//private float transitionElapsedTime;

		public ModManager(MainMenuScreen mmscreen)
		{
			this.mmscreen = mmscreen;
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
			this.NameSave.Draw();
			this.AllSaves.Draw();
			Vector2 vector2 = new Vector2((float)(this.AllSaves.Menu.X + 20), (float)(this.AllSaves.Menu.Y + 20));
			for (int i = this.ModsSL.indexAtTop; i < this.ModsSL.Entries.Count && i < this.ModsSL.indexAtTop + this.ModsSL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.ModsSL.Entries[i];
				(e.item as ModEntry).Draw(base.ScreenManager, e.clickRect);
			}
			this.ModsSL.Draw(base.ScreenManager.SpriteBatch);
			this.EnterNameArea.Draw(Fonts.Arial12Bold, base.ScreenManager.SpriteBatch, this.EnternamePos, gameTime, (this.EnterNameArea.Hover ? Color.White : Color.Orange));
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


		public override void HandleInput(InputState input)
		{
			this.selector = null;
			if (input.Escaped || input.RightMouseClick)
			{
				this.ExitScreen();
			}
			foreach (UIButton b in this.Buttons)
			{
				if (!HelperFunctions.CheckIntersection(b.Rect, input.CursorPosition))
				{
					b.State = UIButton.PressState.Normal;
				}
				else
				{
					b.State = UIButton.PressState.Hover;
					if (input.InGameSelect)
					{
						b.State = UIButton.PressState.Pressed;
					}
					if (!input.InGameSelect)
					{
						continue;
					}
					string launches = b.Launches;
					string str = launches;
					if (launches == null)
					{
						continue;
					}
					if (str == "Load")
					{
						if (this.ActiveEntry == null)
						{
							continue;
						}

                        ResourceManager.Reset();
                        ResourceManager.Initialize(base.ScreenManager.Content);
                        ResourceManager.LoadEmpires();
                        GlobalStats.ActiveMod = this.ActiveEntry;
                        
                        ResourceManager.WhichModPath = this.ActiveEntry.ModPath;
                        
						ResourceManager.LoadMods(string.Concat("Mods/", this.ActiveEntry.ModPath));

						Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
						config.AppSettings.Settings["ActiveMod"].Value = this.ActiveEntry.ModPath;
						config.Save();
						this.ExitScreen();
						this.mmscreen.ResetMusic();
					}
					else if (str == "Visit")
					{
						Process.Start("http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=696");
					}
					else if (str == "shiptool")
					{
						base.ScreenManager.AddScreen(new ShipToolScreen());
					}
					else if (str == "Disable")
					{
						GlobalStats.ActiveMod = null;
						ResourceManager.WhichModPath = "Content";
						ResourceManager.Reset();
						ResourceManager.Initialize(base.ScreenManager.Content);
						ResourceManager.LoadEmpires();
						Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
						config.AppSettings.Settings["ActiveMod"].Value = "";
						config.Save();
						this.ExitScreen();
						this.mmscreen.ResetMusic();
					}
				}
			}
			this.ModsSL.HandleInput(input);
			foreach (ScrollList.Entry e in this.ModsSL.Entries)
			{
				if (!HelperFunctions.CheckIntersection(e.clickRect, input.CursorPosition))
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
					AudioManager.PlayCue("sd_ui_accept_alt3");
					this.EnterNameArea.Text = (e.item as ModEntry).mi.ModName;
					this.ActiveEntry = e.item as ModEntry;
				}
			}
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			this.Window = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 425, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 850, 600);
			this.SaveMenu = new Menu1(base.ScreenManager, this.Window);
			Rectangle sub = new Rectangle(this.Window.X + 20, this.Window.Y + 20, this.Window.Width - 40, 80);
			this.NameSave = new Submenu(base.ScreenManager, sub);
			this.NameSave.AddTab(Localizer.Token(4013));
			Vector2 Cursor = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 84), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 100));
			this.TitlePosition = new Vector2((float)(sub.X + 20), (float)(sub.Y + 45));
			Rectangle scrollList = new Rectangle(sub.X, sub.Y + 90, sub.Width, this.Window.Height - sub.Height - 50);
			this.AllSaves = new Submenu(base.ScreenManager, scrollList);
			this.AllSaves.AddTab(Localizer.Token(4013));
			this.ModsSL = new ScrollList(this.AllSaves, 140);
			FileInfo[] filesFromDirectoryNoSub = ResourceManager.GetFilesFromDirectoryNoSub("Mods");
			for (int i = 0; i < (int)filesFromDirectoryNoSub.Length; i++)
			{
				FileInfo FI = filesFromDirectoryNoSub[i];
				Stream file = FI.OpenRead();
                ModInformation data;
                if(FI.Name.Contains(".txt"))
                    continue;
                try
                {
                    data = (ModInformation)ResourceManager.ModSerializer.Deserialize(file);
                }
                catch (Exception ex)
                {
                    ex.Data.Add("Load Error in file", FI.Name);
                    
                    throw;
                }
				//file.Close();
				file.Dispose();
				ModEntry me = new ModEntry(base.ScreenManager, data, Path.GetFileNameWithoutExtension(FI.Name));
				this.ModsSL.AddItem(me);
			}
			this.EnternamePos = this.TitlePosition;
			this.EnterNameArea = new UITextEntry();
			//{
				this.EnterNameArea.Text = "";
                this.EnterNameArea.ClickableArea = new Rectangle((int)this.EnternamePos.X, (int)this.EnternamePos.Y - 2, (int)Fonts.Arial20Bold.MeasureString(this.EnterNameArea.Text).X + 20, Fonts.Arial20Bold.LineSpacing);
			//};
			this.Save = new UIButton()
			{
				Rect = new Rectangle(sub.X + sub.Width - 88, this.EnterNameArea.ClickableArea.Y - 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"],
				Text = Localizer.Token(8),
				Launches = "Load"
			};
			this.Buttons.Add(this.Save);
			Cursor.Y = Cursor.Y + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height + 15);
			this.Visit = new UIButton()
			{
				Rect = new Rectangle(this.Window.X + 3, this.Window.Y + this.Window.Height + 20, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
				Text = Localizer.Token(4015),
				Launches = "Visit"
			};
			this.Buttons.Add(this.Visit);
			this.shiptool = new UIButton()
			{
				Rect = new Rectangle(this.Window.X + 200, this.Window.Y + this.Window.Height + 20, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
				Text = Localizer.Token(4044),
				Launches = "shiptool"
			};
			this.Buttons.Add(this.shiptool);
			this.Disable = new UIButton()
			{
				Rect = new Rectangle(this.Window.X + this.Window.Width - 172, this.Window.Y + this.Window.Height + 20, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
				Text = Localizer.Token(4016),
				Launches = "Disable"
			};
			this.Buttons.Add(this.Disable);
			base.LoadContent();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}