using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using System.Threading.Tasks;

namespace Ship_Game
{
	public sealed class ModManager : GameScreen
	{
		private MainMenuScreen mmscreen;
		private Rectangle Window;
		private Menu1 SaveMenu;
		private Submenu NameSave;
		private Submenu AllSaves;
		private Vector2 TitlePosition;
		private Vector2 EnternamePos;
		private UITextEntry EnterNameArea;
		private UIButton Save;
		private UIButton Disable;
		private UIButton Visit;
		private UIButton shiptool;
		private Selector selector;
        private UIButton CurrentButton;

        private ScrollList ModsSL;
        private ModEntry SelectedMod;

		public ModManager(MainMenuScreen mmscreen) : base(mmscreen)
		{
			this.mmscreen = mmscreen;
			IsPopup = true;
			TransitionOnTime = TimeSpan.FromSeconds(0.25);
			TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

        protected override void Dispose(bool disposing)
        {
            ModsSL?.Dispose(ref ModsSL);
            base.Dispose(disposing);
        }

        public override void Draw(GameTime gameTime)
		{
            if (IsExiting)
                return;
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
			ScreenManager.SpriteBatch.Begin();
            SaveMenu.Draw();
            NameSave.Draw();
            AllSaves.Draw();
			for (int i = ModsSL.indexAtTop; i < ModsSL.Entries.Count && i < ModsSL.indexAtTop + ModsSL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = ModsSL.Entries[i];
				(e.item as ModEntry)?.Draw(ScreenManager, e.clickRect);
			}
			ModsSL.Draw(ScreenManager.SpriteBatch);
			EnterNameArea.Draw(Fonts.Arial12Bold, ScreenManager.SpriteBatch, EnternamePos, gameTime, (EnterNameArea.Hover ? Color.White : Color.Orange));
			foreach (UIButton b in Buttons)
			{
				b.Draw(ScreenManager.SpriteBatch);
			}
		    selector?.Draw(ScreenManager.SpriteBatch);
		    ScreenManager.SpriteBatch.End();
		}

		public override bool HandleInput(InputState input)
		{
            selector = null;
			if (CurrentButton == null && (input.Escaped || input.RightMouseClick))
			{
				ExitScreen();
			    return true;    
			}

			if (!IsExiting) foreach (UIButton b in Buttons)
			{
                if (CurrentButton != null && b.Launches != "Visit")
                    continue;
                if (!b.Rect.HitTest(input.CursorPosition))
				{
                    b.State = UIButton.PressState.Default;
				}
				else
				{
					b.State = UIButton.PressState.Hover;
					if (input.InGameSelect)
					{
						b.State = UIButton.PressState.Pressed;
					}
					if (!input.InGameSelect)
						continue;

					string launches = b.Launches;
					string str = launches;
					if (launches == null)
					{
						continue;
					}
					switch (str)
					{
					    case "Load":
					        if (SelectedMod == null)
					            continue;
					        CurrentButton = b;
					        b.Text = "Loading";
                            LoadModTask();
					        break;
					    case "Visit":
					        if (string.IsNullOrEmpty(SelectedMod?.mi.URL)) try
					        {
					            SteamManager.ActivateOverlayWebPage("http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=696");
					        }
					        catch
					        {
					            Process.Start("http://www.stardrivegame.com/forum/viewtopic.php?f=6&t=696");
					        }
					        else try
					        {
					            SteamManager.ActivateOverlayWebPage(SelectedMod.mi.URL);
					        }
					        catch
					        {
					            Process.Start(SelectedMod.mi.URL);
					        }
					        break;
					    default:
                            if (CurrentButton == null)
                            {
                                if (str == "shiptool")
                                {
                                    ScreenManager.AddScreen(new ShipToolScreen());
                                }
                                else if (str == "Disable")
                                {
                                    ClearMods();
                                }
                            }
					        break;
					}
				}
			}

            if (CurrentButton != null)
            {
                CurrentButton.State = UIButton.PressState.Pressed;
                return false;
            }
            ModsSL.HandleInput(input);
			foreach (ScrollList.Entry e in ModsSL.Entries)
			{
				if (!e.clickRect.HitTest(input.CursorPosition))
				{
					e.clickRectHover = 0;
				}
				else
				{
					if (e.clickRectHover == 0)
					{
						GameAudio.PlaySfxAsync("sd_ui_mouseover");
					}
					e.clickRectHover = 1;
					selector = new Selector(e.clickRect);
					if (!input.InGameSelect)
						continue;

					GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    SelectedMod = e.item as ModEntry;
					EnterNameArea.Text = SelectedMod.ModName;

                    foreach (UIButton button in Buttons)
                    {
                        if (button.Launches != "Visit")
                            continue;
                        if (string.IsNullOrEmpty(SelectedMod.mi.URL))
                            button.Text = Localizer.Token(4015);
                        else
                            button.Text = "Goto Mod URL";
                        break;
                    }
				}
			}
			return base.HandleInput(input);
		}
        private void ClearMods()
        {
            if (!GlobalStats.HasMod)
                return;

            Log.Info("ModManager.ClearMods");
            GlobalStats.LoadModInfo("");
            ResourceManager.LoadItAll();
            mmscreen.LoadContent();
            ExitScreen();
            mmscreen.ResetMusic();
        }
        private void LoadModTask()
        {
            Log.Info("ModManager.LoadMod {0}", SelectedMod.ModName);
            GlobalStats.LoadModInfo(SelectedMod);
            ResourceManager.LoadItAll();
            mmscreen.LoadContent();
            ExitScreen();
            mmscreen.ResetMusic();
        }
		public override void LoadContent()
		{
			Window = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 425, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 850, 600);
			SaveMenu = new Menu1(ScreenManager, Window);
			Rectangle sub = new Rectangle(Window.X + 20, Window.Y + 20, Window.Width - 40, 80);
            NameSave = new Submenu(ScreenManager, sub);
            NameSave.AddTab(Localizer.Token(4013));
			Vector2 cursor = new Vector2(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 84, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 100);
            TitlePosition = new Vector2(sub.X + 20, sub.Y + 45);
			Rectangle scrollList = new Rectangle(sub.X, sub.Y + 90, sub.Width, Window.Height - sub.Height - 50);
            AllSaves = new Submenu(ScreenManager, scrollList);
            AllSaves.AddTab(Localizer.Token(4013));
            ModsSL = new ScrollList(AllSaves, 140);

            var ser = new XmlSerializer(typeof(ModInformation));
			foreach (FileInfo info in Dir.GetFilesNoSub("Mods"))
			{
                if (info.Name.Contains(".txt"))
                    continue;
			    try
			    {
                    ModsSL.AddItem(new ModEntry(ser.Deserialize<ModInformation>(info)));
                }
			    catch (Exception ex)
			    {
                    Log.Warning("Load error in file {0}", info.Name);
			        ex.Data.Add("Load Error in file", info.Name);
			        throw;
			    }
			}
			EnternamePos  = TitlePosition;
			EnterNameArea = new UITextEntry();
			EnterNameArea.Text = "";
            EnterNameArea.ClickableArea = new Rectangle((int)EnternamePos.X, (int)EnternamePos.Y - 2, (int)Fonts.Arial20Bold.MeasureString(EnterNameArea.Text).X + 20, Fonts.Arial20Bold.LineSpacing);

            var defaultSmall  = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"];
            var hoverSmall   = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_hover"];
            var pressedSmall = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"];

            Save = new UIButton
			{
				Rect = new Rectangle(sub.X + sub.Width - 88, EnterNameArea.ClickableArea.Y - 2, defaultSmall.Width, defaultSmall.Height),
				NormalTexture  = defaultSmall,
				HoverTexture   = hoverSmall,
				PressedTexture = pressedSmall,
				Text = Localizer.Token(8),
				Launches = "Load"
			};
			Buttons.Add(Save);
			cursor.Y = cursor.Y + defaultSmall.Height + 15;

            var defaultTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"];
            var hoverTexture   = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"];
            var pressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"];

            Visit = new UIButton
			{
				Rect = new Rectangle(Window.X + 3, Window.Y + Window.Height + 20, defaultTexture.Width, defaultTexture.Height),
				NormalTexture  = defaultTexture,
				HoverTexture   = hoverTexture,
				PressedTexture = pressedTexture,
				Text = Localizer.Token(4015),
				Launches = "Visit"
			};
            Buttons.Add(Visit);

            shiptool = new UIButton
			{
				Rect = new Rectangle(Window.X + 200, Window.Y + Window.Height + 20, defaultTexture.Width, defaultTexture.Height),
				NormalTexture  = defaultTexture,
				HoverTexture   = hoverTexture,
				PressedTexture = pressedTexture,
                Text = Localizer.Token(4044),
				Launches = "shiptool"
			};
			Buttons.Add(shiptool);
			Disable = new UIButton
			{
				Rect = new Rectangle(Window.X + Window.Width - 172, Window.Y + Window.Height + 20, defaultTexture.Width, defaultTexture.Height),
				NormalTexture  = defaultTexture,
				HoverTexture   = hoverTexture,
				PressedTexture = pressedTexture,
				Text = Localizer.Token(4016),
				Launches = "Disable"
			};
			Buttons.Add(Disable);
			base.LoadContent();
		}
	}
}
