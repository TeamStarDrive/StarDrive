using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
	public sealed class OptionsScreen : PopupWindow
	{
		public bool fade = true;
		public bool FromGame;
		private MainMenuScreen MainMenu;
		private UICheckBox GamespeedCap;
		private UICheckBox ForceFullSim;
		private UniverseScreen Universe;
		private GameplayMMScreen UniverseMainMenu; // the little menu in universe view
		private DropOptions<Option> ResolutionDropDown;
		private UIButton Apply;
		private Rectangle MainOptionsRect;
		private Rectangle SecondaryOptionsRect;


	    private readonly WindowMode StartingMode = GlobalStats.WindowMode;
	    private int OriginalWidth;
	    private int OriginalHeight;
	    private WindowMode ModeToSet = GlobalStats.WindowMode;
	    private int NewWidth;
	    private int NewHeight;

		private FloatSlider MusicVolumeSlider;
		private FloatSlider EffectsVolumeSlider;
		private readonly Array<Option> ResolutionOptions = new Array<Option>();
		private Option Resolution;
		private Option FullScreen;

        private UICheckBox PauseOnNotify;
        private FloatSlider IconSize;
        private FloatSlider ShipLimiter;
        private FloatSlider FreighterLimiter;
        private FloatSlider AutoSaveFreq; //Added by Gretman
        private UICheckBox KeyboardArc;
        private UICheckBox LockZoom;
        private UICheckBox AutoErrorReport; // Added by RedFox

        private class Option
        {
            public string Name;
            public Vector2 NamePosition;
            public Rectangle ClickableArea;
            public object Value;
            public int x;
            public int y;
            public bool highlighted;
        }

        public OptionsScreen(MainMenuScreen s, Rectangle dimensions) : base(s)
		{
			R = dimensions;
			MainMenu = s;
			IsPopup = true;
			TransitionOnTime = TimeSpan.FromSeconds(0.25);
			TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

		public OptionsScreen(UniverseScreen s, GameplayMMScreen universeMainMenuScreen, Rectangle dimensions) : base(s)
		{
			UniverseMainMenu = universeMainMenuScreen;
			Universe = s;
			fade = false;
			IsPopup = true;
			FromGame = true;
			TransitionOnTime = TimeSpan.FromSeconds(0);
			TransitionOffTime = TimeSpan.FromSeconds(0);
			R = dimensions;
		}

		private void AcceptChanges(object sender, EventArgs e)
		{
            GlobalStats.SaveSettings();
			EffectsVolumeSlider.Amount = GlobalStats.EffectsVolume;
			MusicVolumeSlider.Amount   = GlobalStats.MusicVolume;
        }

		private void ApplySettings()
		{
			try
			{
				OriginalWidth  = Game1.Instance.RenderWidth;
				OriginalHeight = Game1.Instance.RenderHeight;
				Game1.Instance.Graphics.SynchronizeWithVerticalRetrace = true;

                var activeOpt = ResolutionDropDown.ActiveValue;
                Game1.Instance.SetWindowMode(ModeToSet, activeOpt.x, activeOpt.y);
				Setup();
				if (FromGame)
				{
					Universe.LoadGraphics();
					Universe.NotificationManager.ReSize();
					UniverseMainMenu.LoadGraphics();
					LoadGraphics();
				}
				else
				{
					MainMenu.LoadContent();
				}
				MainOptionsRect = new Rectangle(R.X + 20, R.Y + 175, 300, 375);
				SecondaryOptionsRect = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width + 20, MainOptionsRect.Y, 210, 305);

				var ftlRect = new Rectangle(MainOptionsRect.X + 9, (int)FullScreen.NamePosition.Y + 65, 270, 50);
				MusicVolumeSlider = new FloatSlider(ftlRect, "Music Volume");
				MusicVolumeSlider.Amount = GlobalStats.MusicVolume;
				ftlRect = new Rectangle(MainOptionsRect.X + 9, (int)ftlRect.Y + 50, 270, 50);
				EffectsVolumeSlider = new FloatSlider(ftlRect, "Effects Volume");
				EffectsVolumeSlider.Amount = GlobalStats.EffectsVolume;
                
                var cursor = new Vector2(SecondaryOptionsRect.X + 10, SecondaryOptionsRect.Y + 10);
				ResolutionOptions.Clear();
				//ResolutionDropDown = new DropOptions(new Rectangle(MainOptionsRect.X + MainOptionsRect.Width / 2 + 10, (int)Resolution.NamePosition.Y + 3, 105, 18));
                ResolutionDropDown = new DropOptions(new Rectangle(MainOptionsRect.X + MainOptionsRect.Width / 2 + 10, (int)Resolution.NamePosition.Y - 2, 105, 18));
				foreach (DisplayMode mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
				{
					if (mode.Width < 1280)
					{
						continue;
					}
					Option reso1 = new Option();
					//{ //business as usual
						reso1.x = mode.Width;
						reso1.y = mode.Height;
						reso1.Name = string.Concat(reso1.x.ToString(), " x ", reso1.y.ToString());
						reso1.NamePosition = cursor;
                        reso1.ClickableArea = new Rectangle((int)reso1.NamePosition.X, (int)reso1.NamePosition.Y, (int)Fonts.Arial12Bold.MeasureString(reso1.Name).X, Fonts.Arial12Bold.LineSpacing);
					//};
					bool oktoadd = true;
					foreach (Option opt in ResolutionOptions)
					{
						if (opt.x != reso1.x || opt.y != reso1.y)
						{
							continue;
						}
						oktoadd = false;
					}
					if (!oktoadd)
					{
						continue;
					}
					ResolutionDropDown.AddOption(reso1.Name, reso1);
					ResolutionOptions.Add(reso1);
					cursor.Y = cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				}
				foreach (Option resolut in ResolutionOptions)
				{
					if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth != resolut.x || ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight != resolut.y)
					{
						continue;
					}
					foreach (Entry e in ResolutionDropDown.Options)
					{
						if ((e.ObjValue as Option).Name != resolut.Name)
						{
							continue;
						}
						ResolutionDropDown.ActiveIndex = ResolutionDropDown.Options.IndexOf(e);
					}
				}
				cursor = new Vector2(SecondaryOptionsRect.X, (SecondaryOptionsRect.Y + SecondaryOptionsRect.Height + 60));
				Apply.Rect = new Rectangle((int)cursor.X, (int)cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
				Apply.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"];
				Apply.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"];
				Apply.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"];
				Apply.Text = Localizer.Token(13);
				if (StartingMode != GlobalStats.WindowMode || OriginalWidth != NewWidth || OriginalHeight != NewHeight)
				{
					MessageBoxScreen messageBox = new MessageBoxScreen(this, Localizer.Token(14), 10f);
					messageBox.Accepted  += AcceptChanges;
					messageBox.Cancelled += CancelChanges;
					ScreenManager.AddScreen(messageBox);
				}
				else
				{
					AcceptChanges(this, EventArgs.Empty);
				}
			}
			catch
			{
				CancelChanges(this, EventArgs.Empty);
			}
		}

		private void CancelChanges(object sender, EventArgs e1)
		{
			Game1.Instance.Graphics.PreferredBackBufferWidth = OriginalWidth;
			Game1.Instance.Graphics.PreferredBackBufferHeight = OriginalHeight;
			Game1.Instance.Graphics.SynchronizeWithVerticalRetrace = false;
			Game1.Instance.SetWindowMode(StartingMode, OriginalWidth, OriginalHeight);
			ModeToSet = StartingMode;
			Setup();
			if (FromGame)
			{
				Universe.LoadGraphics();
				UniverseMainMenu.LoadGraphics();
				LoadGraphics();
			}
			else
			{
				MainMenu.LoadContent();
			}
			MainOptionsRect = new Rectangle(R.X + 20, R.Y + 175, 300, 375);
			SecondaryOptionsRect = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width, MainOptionsRect.Y, 210, 305);
			GamespeedCap = new UICheckBox(MainOptionsRect.X + 20, MainOptionsRect.Y + 300, () => GlobalStats.LimitSpeed, Fonts.Arial12Bold, title:2206, tooltip:2205);
			Resolution = new Option()
			{
				Name = string.Concat(Localizer.Token(9), ":     "),
				NamePosition = new Vector2((MainOptionsRect.X + 20), (MainOptionsRect.Y + 20))
			};
			NewWidth = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
			NewHeight = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
			string reso = NewWidth + " x " + NewHeight;
			Resolution.ClickableArea = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width / 2 + 10, (int)Resolution.NamePosition.Y, (int)Fonts.Arial20Bold.MeasureString(reso).X, Fonts.Arial20Bold.LineSpacing);
			Resolution.Value = reso;
			Resolution.highlighted = false;
			FullScreen = new Option()
			{
				Name = string.Concat(Localizer.Token(10), ":     "),
				NamePosition = new Vector2(MainOptionsRect.X + 20, MainOptionsRect.Y + Fonts.Arial20Bold.LineSpacing * 2 + 2 + 20),
				Value = GlobalStats.WindowMode,
				ClickableArea = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width / 2 + 10, (int)FullScreen.NamePosition.Y, (int)Fonts.Arial20Bold.MeasureString(FullScreen.Value.ToString()).X, Fonts.Arial20Bold.LineSpacing)
			};
			Rectangle ftlRect = new Rectangle(MainOptionsRect.X + 20, (int)FullScreen.NamePosition.Y + 40, 270, 50);
			MusicVolumeSlider = new FloatSlider(ftlRect, "Music Volume");
			MusicVolumeSlider.Amount = GlobalStats.MusicVolume;
            ftlRect = new Rectangle(MainOptionsRect.X + 20, (int)ftlRect.Y + 50, 270, 50);
			EffectsVolumeSlider = new FloatSlider(ftlRect, "Effects Volume");
			EffectsVolumeSlider.Amount = GlobalStats.EffectsVolume;
			Vector2 cursor = new Vector2((SecondaryOptionsRect.X + 10), (SecondaryOptionsRect.Y + 10));
			ResolutionOptions.Clear();
			ResolutionDropDown = new DropOptions(new Rectangle(MainOptionsRect.X + MainOptionsRect.Width / 2 + 10, (int)Resolution.NamePosition.Y + 3, 105, 18));
			foreach (DisplayMode mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
			{
				if (mode.Width < 1280)
				{
					continue;
				}
                string name = mode.Width + " x " + mode.Height;
                Option reso1 = new Option
			    {
			        x = mode.Width,
			        y = mode.Height,
                    Name = name,
                    NamePosition = cursor,
                    ClickableArea = new Rectangle((int)cursor.X, (int)cursor.Y, (int)Fonts.Arial12Bold.MeasureString(name).X, Fonts.Arial12Bold.LineSpacing)
                };
				bool oktoadd = true;
				foreach (Option opt in ResolutionOptions)
				{
					if (opt.x != reso1.x || opt.y != reso1.y)
					{
						continue;
					}
					oktoadd = false;
				}
				if (!oktoadd)
				{
					continue;
				}
				ResolutionDropDown.AddOption(reso1.Name, reso1);
				ResolutionOptions.Add(reso1);
				cursor.Y = cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
			}
			foreach (Option resolut in ResolutionOptions)
			{
				if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth != resolut.x || ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight != resolut.y)
				{
					continue;
				}
				foreach (Entry e in ResolutionDropDown.Options)
				{
					if ((e.ObjValue as Option).Name != resolut.Name)
					{
						continue;
					}
					ResolutionDropDown.ActiveIndex = ResolutionDropDown.Options.IndexOf(e);
				}
			}
			cursor = new Vector2((float)SecondaryOptionsRect.X, (float)(SecondaryOptionsRect.Y + SecondaryOptionsRect.Height + 15));
			Apply.Rect = new Rectangle((int)cursor.X, (int)cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
			Apply.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"];
			Apply.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"];
			Apply.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"];
			Apply.Text = Localizer.Token(13);
		}


		public override void Draw(GameTime gameTime)
		{
            if (fade) ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);

			DrawBase(gameTime);

            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
		    spriteBatch.Begin();

            var uiColor = new Color(255, 239, 208);
		    spriteBatch.DrawString(Fonts.Arial12Bold, Resolution.Name, Resolution.NamePosition, uiColor);
		    spriteBatch.DrawString(Fonts.Arial12Bold, FullScreen.Name, FullScreen.NamePosition, uiColor);

			var valuePos = new Vector2(FullScreen.ClickableArea.X + 2, FullScreen.ClickableArea.Y);

            Color color = FullScreen.highlighted ? uiColor : Color.White;
		    spriteBatch.DrawString(Fonts.Arial12Bold, FullScreen.Value.ToString(), valuePos, color);

			foreach (UIButton b in Buttons)
				b.Draw(ScreenManager.SpriteBatch);

			GamespeedCap.Draw(ScreenManager.SpriteBatch);
			ForceFullSim.Draw(ScreenManager.SpriteBatch);
            PauseOnNotify.Draw(ScreenManager.SpriteBatch);
            KeyboardArc.Draw(ScreenManager.SpriteBatch);
            LockZoom.Draw(ScreenManager.SpriteBatch);

			MusicVolumeSlider.DrawPercent(ScreenManager);
			EffectsVolumeSlider.DrawPercent(ScreenManager);

            FreighterLimiter.DrawDecimal(ScreenManager);
            IconSize.DrawDecimal(ScreenManager);
		    AutoSaveFreq.DrawDecimal(ScreenManager);
		    ShipLimiter.DrawDecimal(ScreenManager);

			ResolutionDropDown.Draw(ScreenManager.SpriteBatch);

			ToolTip.Draw(ScreenManager.SpriteBatch);
			ScreenManager.SpriteBatch.End();
		}

		public override void ExitScreen()
		{
            GlobalStats.SaveSettings();
            base.ExitScreen();
		}

		public override bool HandleInput(InputState input)
		{
            if (base.HandleInput(input))
            {
                GlobalStats.IconSize       = (int)IconSize.AmountRange;
                GlobalStats.ShipCountLimit = (int)ShipLimiter.AmountRange;
                GlobalStats.FreighterLimit = (int)FreighterLimiter.AmountRange;
                GlobalStats.AutoSaveFreq   = (int)AutoSaveFreq.AmountRange;

                GlobalStats.MusicVolume   = MusicVolumeSlider.Amount;
                GlobalStats.EffectsVolume = EffectsVolumeSlider.Amount;
                GameAudio.ConfigureAudioSettings();
                return true;
            }

            if (!ResolutionDropDown.Open)// && !AntiAliasingDD.Open)
			{
			    var mousePos = input.MouseScreenPos;
			    if (!FullScreen.ClickableArea.HitTest(mousePos))
			    {
			        FullScreen.highlighted = false;
			    }
			    else
			    {
			        if (!FullScreen.highlighted)
			        {
			            GameAudio.PlaySfxAsync("sd_ui_mouseover");
			        }
			        FullScreen.highlighted = true;
			        if (input.InGameSelect)
			        {
			            GameAudio.PlaySfxAsync("blip_click");
			            ++ModeToSet;
			            if (ModeToSet > WindowMode.Borderless)
			                ModeToSet = WindowMode.Fullscreen;
			            FullScreen.Value = ModeToSet.ToString();
			        }
			    }
			}

			return false;
		}


        public override void LoadContent()
        {
            base.LoadContent();
            MainOptionsRect = new Rectangle(R.X + 20, R.Y + 175, 300, 375);

            Resolution = new Option();
            Resolution.Name = Localizer.Token(9) + ":";
            Resolution.NamePosition = new Vector2(MainOptionsRect.X + 20, MainOptionsRect.Y);

            var pos = new Vector2(MainOptionsRect.X + MainOptionsRect.Width + 5, Resolution.NamePosition.Y);

            GamespeedCap    = Checkbox(ref pos, () => GlobalStats.LimitSpeed,          title: 2206, tooltip: 2205);
            ForceFullSim    = Checkbox(ref pos, () => GlobalStats.ForceFullSim,        "Force Full Simulation", tooltip: 5086);
            PauseOnNotify   = Checkbox(ref pos, () => GlobalStats.PauseOnNotification, title: 6007, tooltip: 7004);
            KeyboardArc     = Checkbox(ref pos, () => GlobalStats.AltArcControl,       title: 6184, tooltip: 7081);
            LockZoom        = Checkbox(ref pos, () => GlobalStats.ZoomTracking,        title: 6185, tooltip: 7082);
            AutoErrorReport = Checkbox(ref pos, () => GlobalStats.AutoErrorReport, "Automatic Error Report", "Enable or disable");

            SecondaryOptionsRect = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width + 20, MainOptionsRect.Y, 210, 305);

            NewWidth = OriginalWidth = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            NewHeight = OriginalHeight = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
            string text = OriginalWidth + " x " + OriginalHeight;

            Resolution.ClickableArea = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width / 2 + 10, (int)Resolution.NamePosition.Y, 
                                                    (int)Fonts.Arial20Bold.MeasureString(text).X, Fonts.Arial20Bold.LineSpacing);
            Resolution.Value = text;
            Resolution.highlighted = false;
            FullScreen = new Option();
            FullScreen.Name = Localizer.Token(10) + ":";
            FullScreen.NamePosition = new Vector2(MainOptionsRect.X + 20, MainOptionsRect.Y + Fonts.Arial20Bold.LineSpacing * 2 + 2 + 17);
            FullScreen.Value = GlobalStats.WindowMode.ToString();
            FullScreen.ClickableArea = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width / 2 + 10, (int)FullScreen.NamePosition.Y, 
                                                    (int)Fonts.Arial20Bold.MeasureString(FullScreen.Value.ToString()).X, Fonts.Arial20Bold.LineSpacing);

            Rectangle r = new Rectangle(MainOptionsRect.X + 9, (int)FullScreen.NamePosition.Y + 65, 270, 50);
            MusicVolumeSlider = new FloatSlider(r, "Music Volume");
            MusicVolumeSlider.Amount = GlobalStats.MusicVolume;
            r = new Rectangle(MainOptionsRect.X + 9, (int)r.Y + 50, 270, 50);
            EffectsVolumeSlider = new FloatSlider(r, "Effects Volume");
            EffectsVolumeSlider.Amount = GlobalStats.EffectsVolume;

            r = new Rectangle(MainOptionsRect.X + 9, (int)FullScreen.NamePosition.Y + 185, 225, 50);
            IconSize = new FloatSlider(r, "Icon Sizes", 0, 30, GlobalStats.IconSize);

            r = new Rectangle(MainOptionsRect.X + 9, (int)FullScreen.NamePosition.Y + 235, 225, 50);
            AutoSaveFreq = new FloatSlider(r, "Autosave Frequency", 60, 540, GlobalStats.AutoSaveFreq);
            AutoSaveFreq.Tip_ID = 4100;

            int ships = 0;
            if (Empire.Universe != null ) ships = Empire.Universe.globalshipCount;

            r = new Rectangle(MainOptionsRect.X - 9 + MainOptionsRect.Width, (int)FullScreen.NamePosition.Y + 235, 225, 50);
            ShipLimiter = new FloatSlider(r, "All AI Ship Limit. AI Ships: "+ ships, 500, 3500, GlobalStats.ShipCountLimit);
            r = new Rectangle(MainOptionsRect.X - 9 + MainOptionsRect.Width, (int)FullScreen.NamePosition.Y + 185, 225, 50);
            FreighterLimiter = new FloatSlider(r, "Per AI Freighter Limit.", 25, 125, GlobalStats.FreighterLimit);


            var position = new Vector2(SecondaryOptionsRect.X + 10, SecondaryOptionsRect.Y + 10);
            ResolutionDropDown = new DropOptions(new Rectangle(MainOptionsRect.X + MainOptionsRect.Width / 2 + 10, (int)Resolution.NamePosition.Y - 2, 105, 18));
            foreach (DisplayMode displayMode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
            {
                if (displayMode.Width >= 1280)
                {
                    var option1 = new Option { x = displayMode.Width, y = displayMode.Height };
                    option1.Name = option1.x + " x " + option1.y;
                    option1.NamePosition = position;
                    option1.ClickableArea = new Rectangle((int)option1.NamePosition.X, (int)option1.NamePosition.Y, (int)Fonts.Arial12Bold.MeasureString(option1.Name).X, Fonts.Arial12Bold.LineSpacing);
                    bool flag = true;
                    foreach (Option option2 in ResolutionOptions)
                    {
                        if (option2.x == option1.x && option2.y == option1.y)
                            flag = false;
                    }
                    if (flag)
                    {
                        ResolutionDropDown.AddOption(option1.Name, option1);
                        ResolutionOptions.Add(option1);
                        position.Y += Fonts.Arial12Bold.LineSpacing;
                    }
                }
            }

            foreach (Option option in ResolutionOptions)
            {
                if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth == option.x && ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight == option.y)
                {
                    foreach (Entry entry in ResolutionDropDown.Options)
                    {
                        if ((entry.ObjValue as Option).Name == option.Name)
                            ResolutionDropDown.ActiveIndex = ResolutionDropDown.Options.IndexOf(entry);
                    }
                }
            }

            position = new Vector2(SecondaryOptionsRect.X, SecondaryOptionsRect.Y + SecondaryOptionsRect.Height + 60);
            Apply = Button(ref position, "Apply Settings", 13);
            Apply.OnClick += button => ApplySettings();
        }



        // @todo This is all BS and needs to be refactored
		private void LoadGraphics()
		{
            var p = ScreenManager.GraphicsDevice.PresentationParameters;
            R = new Rectangle(p.BackBufferWidth / 2 - R.Width / 2, p.BackBufferHeight / 2 - R.Height / 2, R.Width, R.Height);
			MainOptionsRect = new Rectangle(R.X + 20, R.Y + 175, 300, 375);
			SecondaryOptionsRect = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width + 20, MainOptionsRect.Y, 210, 305);
			Resolution = new Option()
			{
				Name = string.Concat(Localizer.Token(9), ":"),
				NamePosition = new Vector2((float)(MainOptionsRect.X + 20), (float)MainOptionsRect.Y)
			};
			string xResolution = p.BackBufferWidth.ToString();
			string yResolution = p.BackBufferHeight.ToString();
			string reso = string.Concat(xResolution, " x ", yResolution);
			Resolution.ClickableArea = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width / 2 + 10, (int)Resolution.NamePosition.Y, (int)Fonts.Arial20Bold.MeasureString(reso).X, Fonts.Arial20Bold.LineSpacing);
			Resolution.Value = reso;
			Resolution.highlighted = false;
			FullScreen = new Option()
			{
				Name = string.Concat(Localizer.Token(10), ":"),
				NamePosition = new Vector2((float)(MainOptionsRect.X + 20), (float)(MainOptionsRect.Y + Fonts.Arial20Bold.LineSpacing * 2 + 2 + 20)),
				Value = GlobalStats.WindowMode.ToString(),
				ClickableArea = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width / 2 + 10, (int)FullScreen.NamePosition.Y, (int)Fonts.Arial20Bold.MeasureString(FullScreen.Value.ToString()).X, Fonts.Arial20Bold.LineSpacing)
			};
			Rectangle ftlRect = new Rectangle(MainOptionsRect.X + 20, (int)FullScreen.NamePosition.Y + 40, 270, 50);
			MusicVolumeSlider = new FloatSlider(ftlRect, "Music Volume");
			MusicVolumeSlider.Amount = GlobalStats.MusicVolume;
            ftlRect = new Rectangle(MainOptionsRect.X + 20, (int)ftlRect.Y + 50, 270, 50);
			EffectsVolumeSlider = new FloatSlider(ftlRect, "Effects Volume");
			EffectsVolumeSlider.Amount = GlobalStats.EffectsVolume;

			var cursor = new Vector2((float)(SecondaryOptionsRect.X + 10), (float)(SecondaryOptionsRect.Y + 10));
			ResolutionDropDown = new DropOptions(new Rectangle(MainOptionsRect.X + MainOptionsRect.Width / 2 + 10, (int)Resolution.NamePosition.Y + 3, 105, 18));
			foreach (DisplayMode mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
			{
				if (mode.Width < 1280)
				{
					continue;
				}
				Option reso1 = new Option();
				//{
					reso1.x = mode.Width;
					reso1.y = mode.Height;
					reso1.Name = string.Concat(reso1.x.ToString(), " x ", reso1.y.ToString());
					reso1.NamePosition = cursor;
                    reso1.ClickableArea = new Rectangle((int)reso1.NamePosition.X, (int)reso1.NamePosition.Y, (int)Fonts.Arial12Bold.MeasureString(reso1.Name).X, Fonts.Arial12Bold.LineSpacing);
				//};
				bool oktoadd = true;
				foreach (Option opt in ResolutionOptions)
				{
					if (opt.x != reso1.x || opt.y != reso1.y)
					{
						continue;
					}
					oktoadd = false;
				}
				if (!oktoadd)
				{
					continue;
				}
				ResolutionDropDown.AddOption(reso1.Name, reso1);
				ResolutionOptions.Add(reso1);
				cursor.Y = cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
			}
			foreach (Option resolut in ResolutionOptions)
			{
				if (p.BackBufferWidth != resolut.x || p.BackBufferHeight != resolut.y)
					continue;

				foreach (Entry e in ResolutionDropDown.Options)
				{
					if (((Option)e.ObjValue).Name != resolut.Name)
						continue;
					ResolutionDropDown.ActiveIndex = ResolutionDropDown.Options.IndexOf(e);
				}
			}
			cursor = new Vector2(SecondaryOptionsRect.X, (SecondaryOptionsRect.Y + SecondaryOptionsRect.Height + 15));

            var tex = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"];
            Apply.Rect = new Rectangle((int)cursor.X, (int)cursor.Y, tex.Width, tex.Height);
		}
	}
}