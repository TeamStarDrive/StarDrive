using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Configuration;
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
		private readonly List<Option> ResolutionOptions = new List<Option>();
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
        private FloatSlider memoryLimit;
        private FloatSlider ShipLimiter;
        private FloatSlider FreighterLimiter;
        private FloatSlider AutoSaveFreq; //Added by Gretman
        private Checkbox KeyboardArc;
        private Checkbox LockZoom;

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

        public OptionsScreen(MainMenuScreen s, Rectangle dimensions)
		{
			R = dimensions;
			mmscreen = s;
			IsPopup = true;
			TransitionOnTime = TimeSpan.FromSeconds(0.25);
			TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

		public OptionsScreen(UniverseScreen s, GameplayMMScreen universeMainMenuScreen, Rectangle dimensions)
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
					mmscreen.ReloadContent();
				}
				MainOptionsRect = new Rectangle(R.X + 20, R.Y + 175, 300, 375);
				SecondaryOptionsRect = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width + 20, MainOptionsRect.Y, 210, 305);
				/*GamespeedCap = new Checkbox(new Vector2((float)(MainOptionsRect.X + 20), (float)(MainOptionsRect.Y + 300)), Localizer.Token(2206), new Ref<bool>(() => GlobalStats.LimitSpeed, (bool x) => GlobalStats.LimitSpeed = x), Fonts.Arial12Bold)
				{
					Tip_Token = 2205
				};
                pauseOnNotification = new Checkbox(new Vector2((float)(MainOptionsRect.X + 20), (float)(MainOptionsRect.Y + 360)), Localizer.Token(6007), new Ref<bool>(() => GlobalStats.PauseOnNotification, (bool x) => GlobalStats.PauseOnNotification = x), Fonts.Arial12Bold)
                {
                    Tip_Token = 7004
                };
				Resolution = new OptionsScreen.Option()
				{
					Name = "Resolution:     ",
					NamePosition = new Vector2((float)(MainOptionsRect.X + 20), (float)MainOptionsRect.Y)
				};
				string xResolution = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth.ToString();
				string yResolution = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight.ToString();
				xtoApply = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
				ytoApply = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
				string reso = string.Concat(xResolution, " x ", yResolution);
				Resolution.ClickableArea = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width / 2 + 10, (int)Resolution.NamePosition.Y, (int)Fonts.Arial20Bold.MeasureString(reso).X, Fonts.Arial20Bold.LineSpacing);
				Resolution.Value = reso;
				Resolution.highlighted = false;
				FullScreen = new OptionsScreen.Option()
				{
					Name = "Screen Mode:     ",
					NamePosition = new Vector2((float)(MainOptionsRect.X + 20), (float)(MainOptionsRect.Y + Fonts.Arial20Bold.LineSpacing * 2 + 2 + 20)),
					Value = Game1.Instance.CurrentMode,
					ClickableArea = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width / 2 + 10, (int)FullScreen.NamePosition.Y, (int)Fonts.Arial20Bold.MeasureString(FullScreen.Value.ToString()).X, Fonts.Arial20Bold.LineSpacing)
				};
                 */
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
                
                Vector2 Cursor = new Vector2((float)(SecondaryOptionsRect.X + 10), (float)(SecondaryOptionsRect.Y + 10));
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
				Cursor = new Vector2((float)SecondaryOptionsRect.X, (float)(SecondaryOptionsRect.Y + SecondaryOptionsRect.Height + 60));
				Apply.Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
				Apply.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"];
				Apply.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"];
				Apply.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"];
				Apply.Text = Localizer.Token(13);
				if (StartingMode != GlobalStats.WindowMode || startingx != xtoApply || startingy != ytoApply)
				{
					MessageBoxScreen messageBox = new MessageBoxScreen(Localizer.Token(14), 10f);
					messageBox.Accepted  += AcceptChanges;
					messageBox.Cancelled += CancelChanges;
					ScreenManager.AddScreen(messageBox);
				}
				else
				{
					AcceptChanges(this, EventArgs.Empty);
				}
                ConfigurationManager.AppSettings.Set("IconSize",GlobalStats.IconSize.ToString());
                ConfigurationManager.AppSettings.Set("PauseOnNotification", GlobalStats.PauseOnNotification.ToString());
                ConfigurationManager.AppSettings.Set("MemoryLimiter", GlobalStats.MemoryLimiter.ToString());
                ConfigurationManager.AppSettings.Set("shipcountlimit", GlobalStats.ShipCountLimit.ToString());
                ConfigurationManager.AppSettings.Set("ZoomTracking", GlobalStats.ZoomTracking.ToString());
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
				mmscreen.ReloadContent();
			}
			MainOptionsRect = new Rectangle(R.X + 20, R.Y + 175, 300, 375);
			SecondaryOptionsRect = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width, MainOptionsRect.Y, 210, 305);
			GamespeedCap = new Checkbox(new Vector2((float)(MainOptionsRect.X + 20), (float)(MainOptionsRect.Y + 300)), Localizer.Token(2206), new Ref<bool>(() => GlobalStats.LimitSpeed, (bool x) => GlobalStats.LimitSpeed = x), Fonts.Arial12Bold)
			{
				Tip_Token = 2205
			};
			Resolution = new Option()
			{
				Name = string.Concat(Localizer.Token(9), ":     "),
				NamePosition = new Vector2((float)(MainOptionsRect.X + 20), (float)(MainOptionsRect.Y + 20))
			};
			string xResolution = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth.ToString();
			string yResolution = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight.ToString();
			xtoApply = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
			ytoApply = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
			string reso = string.Concat(xResolution, " x ", yResolution);
			Resolution.ClickableArea = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width / 2 + 10, (int)Resolution.NamePosition.Y, (int)Fonts.Arial20Bold.MeasureString(reso).X, Fonts.Arial20Bold.LineSpacing);
			Resolution.Value = reso;
			Resolution.highlighted = false;
			FullScreen = new Option()
			{
				Name = string.Concat(Localizer.Token(10), ":     "),
				NamePosition = new Vector2((float)(MainOptionsRect.X + 20), (float)(MainOptionsRect.Y + Fonts.Arial20Bold.LineSpacing * 2 + 2 + 20)),
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
			Vector2 Cursor = new Vector2((float)(SecondaryOptionsRect.X + 10), (float)(SecondaryOptionsRect.Y + 10));
			ResolutionOptions.Clear();
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
			Cursor = new Vector2((float)SecondaryOptionsRect.X, (float)(SecondaryOptionsRect.Y + SecondaryOptionsRect.Height + 15));
			Apply.Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
			Apply.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"];
			Apply.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"];
			Apply.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"];
			Apply.Text = Localizer.Token(13);
		}


		public override void Draw(GameTime gameTime)
		{
			if (fade)
			{
				ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
			}
			DrawBase(gameTime);
			ScreenManager.SpriteBatch.Begin();
			Selector selector = new Selector(ScreenManager, MainOptionsRect, true);
			Selector selector1 = new Selector(ScreenManager, SecondaryOptionsRect, true);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Resolution.Name, Resolution.NamePosition, new Color(255, 239, 208));
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, FullScreen.Name, FullScreen.NamePosition, new Color(255, 239, 208));
			Vector2 valuePos = new Vector2((float)(FullScreen.ClickableArea.X + 2), (float)FullScreen.ClickableArea.Y);
			if (!FullScreen.highlighted)
			{
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, FullScreen.Value.ToString(), valuePos, Color.White);
			}
			else
			{
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, FullScreen.Value.ToString(), valuePos, new Color(255, 239, 208));
			}
			foreach (UIButton b in Buttons)
			{
				b.Draw(ScreenManager.SpriteBatch);
			}
			GamespeedCap.Draw(ScreenManager);
			ForceFullSim.Draw(ScreenManager);
            pauseOnNotification.Draw(ScreenManager);
            KeyboardArc.Draw(ScreenManager);
            LockZoom.Draw(ScreenManager);
			MusicVolumeSlider.DrawDecimal(ScreenManager);
			EffectsVolumeSlider.DrawDecimal(ScreenManager);
            IconSize.Draw(ScreenManager);
            memoryLimit.Draw(ScreenManager);
            //AntiAliasingDD.Draw(base.ScreenManager.SpriteBatch);
			ResolutionDropDown.Draw(ScreenManager.SpriteBatch);
            FreighterLimiter.Draw(ScreenManager);
            AutoSaveFreq.Draw(ScreenManager);
            ShipLimiter.Draw(ScreenManager);
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
			currentMouse = input.CurrentMouseState;
			Vector2 mousePos = new Vector2(currentMouse.X, currentMouse.Y);
			GamespeedCap.HandleInput(input);
			ForceFullSim.HandleInput(input);
            pauseOnNotification.HandleInput(input);
            KeyboardArc.HandleInput(input);
            LockZoom.HandleInput(input);
            IconSize.HandleInput(input);
            GlobalStats.IconSize = (int)IconSize.amountRange;
            memoryLimit.HandleInput(input);
            GlobalStats.MemoryLimiter = memoryLimit.amountRange;
            ShipLimiter.HandleInput(input);
            GlobalStats.ShipCountLimit = (int)ShipLimiter.amountRange;
            FreighterLimiter.HandleInput(input);
            GlobalStats.FreighterLimit = (int)FreighterLimiter.amountRange;
            AutoSaveFreq.HandleInput(input);
            GlobalStats.AutoSaveFreq = (int)AutoSaveFreq.amountRange;

            if (!ResolutionDropDown.Open)// && !AntiAliasingDD.Open)
			{
				MusicVolumeSlider.HandleInput(input);
				GlobalStats.MusicVolume = MusicVolumeSlider.amount;
				ScreenManager.musicCategory.SetVolume(MusicVolumeSlider.amount);
                ScreenManager.racialMusic.SetVolume(MusicVolumeSlider.amount);
                ScreenManager.combatMusic.SetVolume(MusicVolumeSlider.amount);
				EffectsVolumeSlider.HandleInput(input);
				GlobalStats.EffectsVolume = EffectsVolumeSlider.amount;
				ScreenManager.weaponsCategory.SetVolume(EffectsVolumeSlider.amount);
                ScreenManager.defaultCategory.SetVolume(EffectsVolumeSlider.amount *.5f);
                if (EffectsVolumeSlider.amount == 0 && MusicVolumeSlider.amount == 0)
                    ScreenManager.GlobalCategory.SetVolume(0);
                else
                    ScreenManager.GlobalCategory.SetVolume(1);
                        
			}
			if (!ResolutionDropDown.Open)
			{
				if (!HelperFunctions.CheckIntersection(FullScreen.ClickableArea, mousePos))
				{
					FullScreen.highlighted = false;
				}
				else
				{
					if (!FullScreen.highlighted)
					{
						AudioManager.PlayCue("sd_ui_mouseover");
					}
					FullScreen.highlighted = true;
                    if (input.InGameSelect)
                    {
                        AudioManager.PlayCue("blip_click");
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
				if (b != null)
				{
					if (!HelperFunctions.CheckIntersection(b.Rect, mousePos))
					{
						b.State = UIButton.PressState.Default;
					}
					else
					{
						b.State = UIButton.PressState.Hover;
						if (currentMouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Pressed)
						{
							b.State = UIButton.PressState.Pressed;
						}
						if (currentMouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Released)
						{
							string launches = b.Launches;
							if (launches != null && launches == "Apply Settings")
							{
								ApplySettings();
							}
						}
					}
				}
			}
			ResolutionDropDown.HandleInput(input);
			previousMouse = input.LastMouseState;
			base.HandleInput(input);
		}

        public override void LoadContent()
        {
            base.LoadContent();
            MainOptionsRect = new Rectangle(R.X + 20, R.Y + 175, 300, 375);

            Resolution = new Option();
            Resolution.Name = Localizer.Token(9) + ":";
            Resolution.NamePosition = new Vector2((float)(MainOptionsRect.X + 20), (float)MainOptionsRect.Y);

            GamespeedCap = new Checkbox(new Vector2((float)(MainOptionsRect.X + MainOptionsRect.Width + 5), (float)(Resolution.NamePosition.Y)), Localizer.Token(2206), new Ref<bool>((Func<bool>)(() => GlobalStats.LimitSpeed), (Action<bool>)(x => GlobalStats.LimitSpeed = x)), Fonts.Arial12Bold);
            GamespeedCap.Tip_Token = 2205;
            ForceFullSim = new Checkbox(new Vector2((float)(MainOptionsRect.X + MainOptionsRect.Width + 5), (float)(Resolution.NamePosition.Y + 30)), "Force Full Simulation", new Ref<bool>((Func<bool>)(() => GlobalStats.ForceFullSim), (Action<bool>)(x => GlobalStats.ForceFullSim = x)), Fonts.Arial12Bold);
            ForceFullSim.Tip_Token = 5086;
            pauseOnNotification = new Checkbox(new Vector2((float)(MainOptionsRect.X + MainOptionsRect.Width + 5), (float)(Resolution.NamePosition.Y + 60)), Localizer.Token(6007), new Ref<bool>((Func<bool>)(() => GlobalStats.PauseOnNotification), (Action<bool>)(x => GlobalStats.PauseOnNotification = x)), Fonts.Arial12Bold);
            pauseOnNotification.Tip_Token = 7004;

            KeyboardArc = new Checkbox(new Vector2((float)(MainOptionsRect.X + MainOptionsRect.Width + 5), (float)(Resolution.NamePosition.Y + 90)), Localizer.Token(6184), new Ref<bool>((Func<bool>)(() => GlobalStats.AltArcControl), (Action<bool>)(x => GlobalStats.AltArcControl = x)), Fonts.Arial12Bold);
            KeyboardArc.Tip_Token = 7081;
            
            LockZoom = new Checkbox(new Vector2((float)(MainOptionsRect.X + MainOptionsRect.Width + 5), (float)(Resolution.NamePosition.Y + 120)), Localizer.Token(6185), new Ref<bool>((Func<bool>)(() => GlobalStats.ZoomTracking), (Action<bool>)(x => GlobalStats.ZoomTracking = x)), Fonts.Arial12Bold);
            LockZoom.Tip_Token = 7082;

            SecondaryOptionsRect = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width + 20, MainOptionsRect.Y, 210, 305);
            
            string str1 = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth.ToString();
            string str2 = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight.ToString();
            startingx = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            startingy = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;

            xtoApply = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            ytoApply = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
            string text = str1 + " x " + str2;
            Resolution.ClickableArea = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width / 2 + 10, (int)Resolution.NamePosition.Y, (int)Fonts.Arial20Bold.MeasureString(text).X, Fonts.Arial20Bold.LineSpacing);
            Resolution.Value = (object)text;
            Resolution.highlighted = false;
            FullScreen = new Option();
            FullScreen.Name = Localizer.Token(10) + ":";
            FullScreen.NamePosition = new Vector2((float)(MainOptionsRect.X + 20), (float)(MainOptionsRect.Y + Fonts.Arial20Bold.LineSpacing * 2 + 2 + 17));
            FullScreen.Value = GlobalStats.WindowMode.ToString();
            FullScreen.ClickableArea = new Rectangle(MainOptionsRect.X + MainOptionsRect.Width / 2 + 10, (int)FullScreen.NamePosition.Y, (int)Fonts.Arial20Bold.MeasureString(FullScreen.Value.ToString()).X, Fonts.Arial20Bold.LineSpacing);
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
            
            memoryLimit = new FloatSlider(r, string.Concat("Memory limit. KBs In Use: ",(int)(GC.GetTotalMemory(true)/1000f)), 150000, 300000, GlobalStats.MemoryLimiter);
            int ships =0;

            r = new Rectangle(MainOptionsRect.X + 9, (int)FullScreen.NamePosition.Y + 290, 225, 50);    //
            AutoSaveFreq = new FloatSlider(r, "Autosave Frequency", 60, 540, GlobalStats.AutoSaveFreq);      //Added by Gretman
            AutoSaveFreq.Tip_ID = 4100;                                                                      //

            if (Empire.Universe != null )
             ships= Empire.Universe.globalshipCount;
            r = new Rectangle(MainOptionsRect.X - 9 + MainOptionsRect.Width, (int)FullScreen.NamePosition.Y + 235, 225, 50);
            ShipLimiter = new FloatSlider(r, "All AI Ship Limit. AI Ships: "+ ships, 500, 3500, GlobalStats.ShipCountLimit);
            r = new Rectangle(MainOptionsRect.X - 9 + MainOptionsRect.Width, (int)FullScreen.NamePosition.Y + 185, 225, 50);
            FreighterLimiter = new FloatSlider(r, "Per AI Freighter Limit.", 25, 125, GlobalStats.FreighterLimit);
            Vector2 vector2 = new Vector2(SecondaryOptionsRect.X + 10, SecondaryOptionsRect.Y + 10);

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
                    option1.NamePosition = vector2;
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
                        vector2.Y += Fonts.Arial12Bold.LineSpacing;
                    }
                }
            }
            //int qualityLevels = 0;          //Not referenced in code, removing to save memory
            //AntiAliasingDD = new DropOptions(new Rectangle(MainOptionsRect.X + MainOptionsRect.Width / 2 + 10, (int)Resolution.NamePosition.Y + 26, 105, 18));
            //if (GraphicsAdapter.DefaultAdapter.CheckDeviceMultiSampleType(DeviceType.Hardware, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Format, false, MultiSampleType.EightSamples, out qualityLevels))
            //    AntiAliasingDD.AddOption("8x AA", 8);
            //if (GraphicsAdapter.DefaultAdapter.CheckDeviceMultiSampleType(DeviceType.Hardware, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Format, false, MultiSampleType.EightSamples, out qualityLevels))
            //    AntiAliasingDD.AddOption("4x AA", 4);
            //if (GraphicsAdapter.DefaultAdapter.CheckDeviceMultiSampleType(DeviceType.Hardware, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Format, false, MultiSampleType.EightSamples, out qualityLevels))
            //    AntiAliasingDD.AddOption("2x AA", 2);
            //AntiAliasingDD.AddOption("No AA", 0);
            //AntiAliasingDD.ActiveIndex = 0;
            //switch (GlobalStats.AASamples)
            //{
            //    case 0:
            //        AntiAliasingDD.ActiveIndex = 3;
            //        break;
            //    case 2:
            //        AntiAliasingDD.ActiveIndex = 2;
            //        break;
            //    case 4:
            //        AntiAliasingDD.ActiveIndex = 1;
            //        break;
            //    case 8:
            //        AntiAliasingDD.ActiveIndex = 0;
            //        break;
            //}
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
            vector2 = new Vector2((float)SecondaryOptionsRect.X, (float)(SecondaryOptionsRect.Y + SecondaryOptionsRect.Height + 60));
            Apply = new UIButton();
            Apply.Rect = new Rectangle((int)vector2.X, (int)vector2.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
            Apply.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"];
            Apply.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"];
            Apply.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"];
            Apply.Text = Localizer.Token(13);
            Apply.Launches = "Apply Settings";
            Buttons.Add(Apply);
        }

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