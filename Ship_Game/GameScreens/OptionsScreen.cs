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
		private MainMenuScreen mmscreen;
		private Checkbox GamespeedCap;
		private Checkbox ForceFullSim;
		private UniverseScreen uScreen;
		private GameplayMMScreen gpmmscreen;
		private DropOptions ResolutionDropDown;
		private UIButton Apply;
		private Rectangle MainOptionsRect;
		private Rectangle SecondaryOptionsRect;
		private int startingx;
		private int startingy;
		private FloatSlider MusicVolumeSlider;
		private FloatSlider EffectsVolumeSlider;
		private readonly Array<Option> ResolutionOptions = new Array<Option>();
		private Option Resolution;
		private Option FullScreen;
		private MouseState currentMouse;
		private MouseState previousMouse;
		private readonly WindowMode StartingMode = GlobalStats.WindowMode;
		private WindowMode ModeToSet = GlobalStats.WindowMode;
		private int xtoApply;
		private int ytoApply;
        private Checkbox pauseOnNotification;
        private FloatSlider IconSize;
        private FloatSlider ShipLimiter;
        private FloatSlider FreighterLimiter;
        private FloatSlider AutoSaveFreq; //Added by Gretman
        private Checkbox KeyboardArc;
        private Checkbox LockZoom;
        private Checkbox AutoErrorReport; // Added by RedFox

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
			mmscreen = s;
			IsPopup = true;
			TransitionOnTime = TimeSpan.FromSeconds(0.25);
			TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

		public OptionsScreen(UniverseScreen s, GameplayMMScreen universeMainMenuScreen, Rectangle dimensions) : base(s)
		{
			gpmmscreen = universeMainMenuScreen;
			uScreen = s;
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
			EffectsVolumeSlider.SetAmount(GlobalStats.EffectsVolume);
			MusicVolumeSlider.SetAmount(GlobalStats.MusicVolume);
        }

		private void ApplySettings()
		{
			try
			{
				startingx = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
				startingy = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
				Game1.Instance.Graphics.SynchronizeWithVerticalRetrace = true;

                var activeOpt = (Option)ResolutionDropDown.Active.ReferencedObject;
                Game1.Instance.SetWindowMode(ModeToSet, activeOpt.x, activeOpt.y);
				Setup();
				if (FromGame)
				{
					uScreen.LoadGraphics();
					uScreen.NotificationManager.ReSize();
					gpmmscreen.LoadGraphics();
					LoadGraphics();
				}
				else
				{
					mmscreen.LoadContent();
				}
				MainOptionsRect = new Rectangle(R.X + 20, R.Y + 175, 300, 375);
				SecondaryOptionsRect = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width + 20, MainOptionsRect.Y, 210, 305);



				Rectangle ftlRect = new Rectangle(MainOptionsRect.X + 9, (int)FullScreen.NamePosition.Y + 65, 270, 50);
				MusicVolumeSlider = new FloatSlider(ftlRect, "Music Volume");
				MusicVolumeSlider.SetAmount(GlobalStats.MusicVolume);
				MusicVolumeSlider.amount = GlobalStats.MusicVolume;
				ftlRect = new Rectangle(MainOptionsRect.X + 9, (int)ftlRect.Y + 50, 270, 50);
				EffectsVolumeSlider = new FloatSlider(ftlRect, "Effects Volume");
				EffectsVolumeSlider.SetAmount(GlobalStats.EffectsVolume);
				EffectsVolumeSlider.amount = GlobalStats.EffectsVolume;
                //ftlRect = new Rectangle(MainOptionsRect.X + 20, (int)FullScreen.NamePosition.Y + 200, 270, 50);
                //IconSize = new FloatSlider(ftlRect, "Icon Sizes",0,20,GlobalStats.IconSize);
                //IconSize.SetAmount(GlobalStats.IconSize);
                //IconSize.amount = GlobalStats.IconSize;
                
                Vector2 cursor = new Vector2((float)(SecondaryOptionsRect.X + 10), (float)(SecondaryOptionsRect.Y + 10));
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
						if ((e.ReferencedObject as Option).Name != resolut.Name)
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
				if (StartingMode != GlobalStats.WindowMode || startingx != xtoApply || startingy != ytoApply)
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
			Game1.Instance.Graphics.PreferredBackBufferWidth = startingx;
			Game1.Instance.Graphics.PreferredBackBufferHeight = startingy;
			Game1.Instance.Graphics.SynchronizeWithVerticalRetrace = false;
			Game1.Instance.SetWindowMode(StartingMode, startingx, startingy);
			ModeToSet = StartingMode;
			Setup();
			if (FromGame)
			{
				uScreen.LoadGraphics();
				gpmmscreen.LoadGraphics();
				LoadGraphics();
			}
			else
			{
				mmscreen.LoadContent();
			}
			MainOptionsRect = new Rectangle(R.X + 20, R.Y + 175, 300, 375);
			SecondaryOptionsRect = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width, MainOptionsRect.Y, 210, 305);
			GamespeedCap = new Checkbox(MainOptionsRect.X + 20, MainOptionsRect.Y + 300, () => GlobalStats.LimitSpeed, Fonts.Arial12Bold, title:2206, tooltip:2205);
			Resolution = new Option()
			{
				Name = string.Concat(Localizer.Token(9), ":     "),
				NamePosition = new Vector2((MainOptionsRect.X + 20), (MainOptionsRect.Y + 20))
			};
			xtoApply = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
			ytoApply = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
			string reso = xtoApply + " x " + ytoApply;
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
			MusicVolumeSlider.SetAmount(GlobalStats.MusicVolume);
			MusicVolumeSlider.amount = GlobalStats.MusicVolume;
            ftlRect = new Rectangle(MainOptionsRect.X + 20, (int)ftlRect.Y + 50, 270, 50);
			EffectsVolumeSlider = new FloatSlider(ftlRect, "Effects Volume");
			EffectsVolumeSlider.SetAmount(GlobalStats.EffectsVolume);
			EffectsVolumeSlider.amount = GlobalStats.EffectsVolume;
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
					if ((e.ReferencedObject as Option).Name != resolut.Name)
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
			ScreenManager.SpriteBatch.Begin();

            Color uiColor = new Color(255, 239, 208);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Resolution.Name, Resolution.NamePosition, uiColor);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, FullScreen.Name, FullScreen.NamePosition, uiColor);

			Vector2 valuePos = new Vector2(FullScreen.ClickableArea.X + 2, FullScreen.ClickableArea.Y);

            Color color = FullScreen.highlighted ? uiColor : Color.White;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, FullScreen.Value.ToString(), valuePos, color);

			foreach (UIButton b in Buttons)
				b.Draw(ScreenManager.SpriteBatch);

			GamespeedCap.Draw(ScreenManager);
			ForceFullSim.Draw(ScreenManager);
            pauseOnNotification.Draw(ScreenManager);
            KeyboardArc.Draw(ScreenManager);
            LockZoom.Draw(ScreenManager);
			MusicVolumeSlider.DrawPercent(ScreenManager);
			EffectsVolumeSlider.DrawPercent(ScreenManager);
            IconSize.DrawDecimal(ScreenManager);
			ResolutionDropDown.Draw(ScreenManager.SpriteBatch);
            FreighterLimiter.DrawDecimal(ScreenManager);
            AutoSaveFreq.DrawDecimal(ScreenManager);
            ShipLimiter.DrawDecimal(ScreenManager);
			ToolTip.Draw(ScreenManager);
			ScreenManager.SpriteBatch.End();
		}

		public override void ExitScreen()
		{
            GlobalStats.SaveSettings();
            base.ExitScreen();
		}


		public override void HandleInput(InputState input)
		{
			currentMouse = input.MouseCurr;
			Vector2 mousePos = new Vector2(currentMouse.X, currentMouse.Y);
			GamespeedCap.HandleInput(input);
			ForceFullSim.HandleInput(input);
            pauseOnNotification.HandleInput(input);
            KeyboardArc.HandleInput(input);
            LockZoom.HandleInput(input);
            IconSize.HandleInput(input);
            ShipLimiter.HandleInput(input);
            FreighterLimiter.HandleInput(input);
            AutoSaveFreq.HandleInput(input);

            GlobalStats.IconSize       = (int)IconSize.amountRange;
            GlobalStats.ShipCountLimit = (int)ShipLimiter.amountRange;
            GlobalStats.FreighterLimit = (int)FreighterLimiter.amountRange;
            GlobalStats.AutoSaveFreq   = (int)AutoSaveFreq.amountRange;

            if (!ResolutionDropDown.Open)// && !AntiAliasingDD.Open)
			{
				MusicVolumeSlider.HandleInput(input);
			    EffectsVolumeSlider.HandleInput(input);

                GlobalStats.MusicVolume   = MusicVolumeSlider.amount;
			    GlobalStats.EffectsVolume = EffectsVolumeSlider.amount;
			    GameAudio.ConfigureAudioSettings();
			}
			if (!ResolutionDropDown.Open)
			{
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
                        FullScreen.Value = (object)((object)ModeToSet).ToString();
                    }
                }
			}
			if (input.Escaped || input.RightMouseClick)
			{
				ExitScreen();
			}
		    for (int i = 0; i < Buttons.Count; i++)
		    {
		        UIButton b = Buttons[i];
		        if (!b.Rect.HitTest(mousePos))
		        {
		            b.State = UIButton.PressState.Default;
		            continue;
		        }

		        b.State = UIButton.PressState.Hover;
		        if (currentMouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Pressed)
		        {
		            b.State = UIButton.PressState.Pressed;
		        }
		        if (currentMouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Released)
		        {
		            if (b.Launches == "Apply Settings")
		                ApplySettings();
		        }
		    }
		    ResolutionDropDown.HandleInput(input);
			previousMouse = input.MousePrev;
			base.HandleInput(input);
		}

        private static Checkbox BindCheckbox(ref Vector2 pos, Expression<Func<bool>> binding, int title, int tooltip)
        {
            return Layout(ref pos, new Checkbox(pos.X, pos.Y, binding, Fonts.Arial12Bold, title, tooltip));
        }
        private static Checkbox BindCheckbox(ref Vector2 pos, Expression<Func<bool>> binding, string title, string tooltip)
        {
            return Layout(ref pos, new Checkbox(pos.X, pos.Y, binding, Fonts.Arial12Bold, title, tooltip));
        }
        private static Checkbox BindCheckbox(ref Vector2 pos, Expression<Func<bool>> binding, string title, int tooltip)
        {
            return Layout(ref pos, new Checkbox(pos.X, pos.Y, binding, Fonts.Arial12Bold, title, tooltip));
        }
        private static Checkbox Layout(ref Vector2 pos, Checkbox cb)
        {
            pos.Y += 30f;
            return cb;
        }

        public override void LoadContent()
        {
            base.LoadContent();
            MainOptionsRect = new Rectangle(R.X + 20, R.Y + 175, 300, 375);

            Resolution = new Option();
            Resolution.Name = Localizer.Token(9) + ":";
            Resolution.NamePosition = new Vector2(MainOptionsRect.X + 20, MainOptionsRect.Y);

            Vector2 pos = new Vector2(MainOptionsRect.X + MainOptionsRect.Width + 5, Resolution.NamePosition.Y);

            GamespeedCap        = BindCheckbox(ref pos, () => GlobalStats.LimitSpeed,          title: 2206, tooltip: 2205);
            ForceFullSim        = BindCheckbox(ref pos, () => GlobalStats.ForceFullSim,        "Force Full Simulation", tooltip: 5086);
            pauseOnNotification = BindCheckbox(ref pos, () => GlobalStats.PauseOnNotification, title: 6007, tooltip: 7004);
            KeyboardArc         = BindCheckbox(ref pos, () => GlobalStats.AltArcControl,       title: 6184, tooltip: 7081);
            LockZoom            = BindCheckbox(ref pos, () => GlobalStats.ZoomTracking,        title: 6185, tooltip: 7082);
            // @todo Add localization?... Or does anyone really care about non-english versions to be honest...?
            AutoErrorReport     = BindCheckbox(ref pos, () => GlobalStats.AutoErrorReport, 
                "Automatic Error Report", "Enable or disable");

            SecondaryOptionsRect = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width + 20, MainOptionsRect.Y, 210, 305);

            xtoApply = startingx = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            ytoApply = startingy = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
            string text = startingx + " x " + startingy;

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
            MusicVolumeSlider.SetAmount(GlobalStats.MusicVolume);
            MusicVolumeSlider.amount = GlobalStats.MusicVolume;
            r = new Rectangle(MainOptionsRect.X + 9, (int)r.Y + 50, 270, 50);
            EffectsVolumeSlider = new FloatSlider(r, "Effects Volume");
            EffectsVolumeSlider.SetAmount(GlobalStats.EffectsVolume);
            EffectsVolumeSlider.amount = GlobalStats.EffectsVolume;

            r = new Rectangle(MainOptionsRect.X + 9, (int)FullScreen.NamePosition.Y + 185, 225, 50);
            IconSize = new FloatSlider(r, "Icon Sizes", 0, 30, GlobalStats.IconSize);

            r = new Rectangle(MainOptionsRect.X + 9, (int)FullScreen.NamePosition.Y + 235, 225, 50);
            AutoSaveFreq = new FloatSlider(r, "Autosave Frequency", 60, 540, GlobalStats.AutoSaveFreq);      //Added by Gretman
            AutoSaveFreq.Tip_ID = 4100;                                                                      //

            int ships = 0;
            if (Empire.Universe != null ) ships= Empire.Universe.globalshipCount;

            r = new Rectangle(MainOptionsRect.X - 9 + MainOptionsRect.Width, (int)FullScreen.NamePosition.Y + 235, 225, 50);
            ShipLimiter = new FloatSlider(r, "All AI Ship Limit. AI Ships: "+ ships, 500, 3500, GlobalStats.ShipCountLimit);
            r = new Rectangle(MainOptionsRect.X - 9 + MainOptionsRect.Width, (int)FullScreen.NamePosition.Y + 185, 225, 50);
            FreighterLimiter = new FloatSlider(r, "Per AI Freighter Limit.", 25, 125, GlobalStats.FreighterLimit);


            Vector2 position = new Vector2(SecondaryOptionsRect.X + 10, SecondaryOptionsRect.Y + 10);
            ResolutionDropDown = new DropOptions(new Rectangle(MainOptionsRect.X + MainOptionsRect.Width / 2 + 10, (int)Resolution.NamePosition.Y - 2, 105, 18));
            foreach (DisplayMode displayMode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
            {
                if (displayMode.Width >= 1280)
                {
                    Option option1 = new Option
                    {
                        x = displayMode.Width,
                        y = displayMode.Height
                    };
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
                        if ((entry.ReferencedObject as Option).Name == option.Name)
                            ResolutionDropDown.ActiveIndex = ResolutionDropDown.Options.IndexOf(entry);
                    }
                }
            }
            position = new Vector2(SecondaryOptionsRect.X, SecondaryOptionsRect.Y + SecondaryOptionsRect.Height + 60);

            Buttons.Clear();
            var defaultBtn = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"];
            Apply = new UIButton
            {
                Rect = new Rectangle((int)position.X, (int) position.Y, defaultBtn.Width, defaultBtn.Height),
                NormalTexture = defaultBtn,
                HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
                PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
                Text = Localizer.Token(13),
                Launches = "Apply Settings"
            };
            Buttons.Add(Apply);
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
			MusicVolumeSlider.SetAmount(GlobalStats.MusicVolume);
			MusicVolumeSlider.amount = GlobalStats.MusicVolume;
            ftlRect = new Rectangle(MainOptionsRect.X + 20, (int)ftlRect.Y + 50, 270, 50);
			EffectsVolumeSlider = new FloatSlider(ftlRect, "Effects Volume");
			EffectsVolumeSlider.SetAmount(GlobalStats.EffectsVolume);
			EffectsVolumeSlider.amount = GlobalStats.EffectsVolume;
			Vector2 Cursor = new Vector2((float)(SecondaryOptionsRect.X + 10), (float)(SecondaryOptionsRect.Y + 10));
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
					reso1.NamePosition = Cursor;
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
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
			}
			foreach (Option resolut in ResolutionOptions)
			{
				if (p.BackBufferWidth != resolut.x || p.BackBufferHeight != resolut.y)
					continue;

				foreach (Entry e in ResolutionDropDown.Options)
				{
					if (((Option)e.ReferencedObject).Name != resolut.Name)
						continue;
					ResolutionDropDown.ActiveIndex = ResolutionDropDown.Options.IndexOf(e);
				}
			}
			Cursor = new Vector2((float)SecondaryOptionsRect.X, (float)(SecondaryOptionsRect.Y + SecondaryOptionsRect.Height + 15));

            var tex = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"];
            Apply.Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, tex.Width, tex.Height);
		}
	}
}