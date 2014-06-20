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
	public class OptionsScreen : PopupWindow
	{
		public bool fade = true;

		public bool FromGame;

		private MainMenuScreen mmscreen;

		private Checkbox GamespeedCap;

		private Checkbox ForceFullSim;

		private UniverseScreen uScreen;

		private GameplayMMScreen gpmmscreen;

		private DropOptions ResolutionDropDown;

		private DropOptions AntiAliasingDD;

		private List<UIButton> Buttons = new List<UIButton>();

		private UIButton Apply;

		//private UIButton Cancel;

		private Rectangle MainOptionsRect;

		private Rectangle SecondaryOptionsRect;

		private int startingx;

		private int startingy;

		private bool startingfullscreen;

		private FloatSlider MusicVolumeSlider;

		private FloatSlider EffectsVolumeSlider;

		private List<OptionsScreen.Option> ResolutionOptions = new List<OptionsScreen.Option>();

		private OptionsScreen.Option Resolution;

		private OptionsScreen.Option FullScreen;

		private MouseState currentMouse;

		private MouseState previousMouse;

		private Game1.WindowMode StartingMode = Game1.Instance.CurrentMode;

		private Game1.WindowMode ModeToSet = Game1.WindowMode.Borderless;

		private int xtoApply;

		private int ytoApply;

		//private float transitionElapsedTime;

		public OptionsScreen(MainMenuScreen s, Rectangle dimensions)
		{
			this.r = dimensions;
			this.mmscreen = s;
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
			this.ModeToSet = Game1.Instance.CurrentMode;
		}

		public OptionsScreen(UniverseScreen s, GameplayMMScreen gpmmscreen, Rectangle dimensions)
		{
			this.gpmmscreen = gpmmscreen;
			this.uScreen = s;
			this.fade = false;
			base.IsPopup = true;
			this.FromGame = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0);
			base.TransitionOffTime = TimeSpan.FromSeconds(0);
			this.r = dimensions;
			this.ModeToSet = Game1.Instance.CurrentMode;
		}

		private void AcceptChanges(object sender, EventArgs e)
		{
			this.SaveConfigChanges();
			this.EffectsVolumeSlider.SetAmount(GlobalStats.Config.EffectsVolume);
			this.MusicVolumeSlider.SetAmount(GlobalStats.Config.MusicVolume);
		}

		private void ApplySettings()
		{
			try
			{
				this.startingx = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
				this.startingy = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
				Game1.Instance.graphics.SynchronizeWithVerticalRetrace = false;
				Game1.Instance.SetWindowMode(this.ModeToSet, (this.ResolutionDropDown.Active.ReferencedObject as OptionsScreen.Option).x, (this.ResolutionDropDown.Active.ReferencedObject as OptionsScreen.Option).y);
				base.Setup();
				if (this.FromGame)
				{
					this.uScreen.LoadGraphics();
					this.uScreen.NotificationManager.ReSize();
					this.gpmmscreen.LoadGraphics();
					this.LoadGraphics();
				}
				else
				{
					this.mmscreen.Buttons.Clear();
					this.mmscreen.LoadContent();
				}
				this.MainOptionsRect = new Rectangle(this.r.X + 20, this.r.Y + 175, 300, 375);
				this.SecondaryOptionsRect = new Rectangle(this.MainOptionsRect.X + this.MainOptionsRect.Width + 20, this.MainOptionsRect.Y, 210, 305);
				this.GamespeedCap = new Checkbox(new Vector2((float)(this.MainOptionsRect.X + 20), (float)(this.MainOptionsRect.Y + 300)), Localizer.Token(2206), new Ref<bool>(() => GlobalStats.LimitSpeed, (bool x) => GlobalStats.LimitSpeed = x), Fonts.Arial12Bold)
				{
					Tip_Token = 2205
				};
				this.Resolution = new OptionsScreen.Option()
				{
					Name = "Resolution:     ",
					NamePosition = new Vector2((float)(this.MainOptionsRect.X + 20), (float)this.MainOptionsRect.Y)
				};
				string xResolution = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth.ToString();
				string yResolution = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight.ToString();
				this.xtoApply = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
				this.ytoApply = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
				string reso = string.Concat(xResolution, " x ", yResolution);
				this.Resolution.ClickableArea = new Rectangle(this.MainOptionsRect.X + this.MainOptionsRect.Width / 2 + 10, (int)this.Resolution.NamePosition.Y, (int)Fonts.Arial20Bold.MeasureString(reso).X, Fonts.Arial20Bold.LineSpacing);
				this.Resolution.Value = reso;
				this.Resolution.highlighted = false;
				this.FullScreen = new OptionsScreen.Option()
				{
					Name = "Screen Mode:     ",
					NamePosition = new Vector2((float)(this.MainOptionsRect.X + 20), (float)(this.MainOptionsRect.Y + Fonts.Arial20Bold.LineSpacing * 2 + 2 + 20)),
					Value = Game1.Instance.CurrentMode,
					ClickableArea = new Rectangle(this.MainOptionsRect.X + this.MainOptionsRect.Width / 2 + 10, (int)this.FullScreen.NamePosition.Y, (int)Fonts.Arial20Bold.MeasureString(this.FullScreen.Value.ToString()).X, Fonts.Arial20Bold.LineSpacing)
				};
				Rectangle ftlRect = new Rectangle(this.MainOptionsRect.X + 20, (int)this.FullScreen.NamePosition.Y + 40, 270, 50);
				this.MusicVolumeSlider = new FloatSlider(ftlRect, "Music Volume");
				this.MusicVolumeSlider.SetAmount(GlobalStats.Config.MusicVolume);
				this.MusicVolumeSlider.amount = GlobalStats.Config.MusicVolume;
				ftlRect = new Rectangle(this.MainOptionsRect.X + 20, (int)this.FullScreen.NamePosition.Y + 120, 270, 50);
				this.EffectsVolumeSlider = new FloatSlider(ftlRect, "Effects Volume");
				this.EffectsVolumeSlider.SetAmount(GlobalStats.Config.EffectsVolume);
				this.EffectsVolumeSlider.amount = GlobalStats.Config.EffectsVolume;
				Vector2 Cursor = new Vector2((float)(this.SecondaryOptionsRect.X + 10), (float)(this.SecondaryOptionsRect.Y + 10));
				this.ResolutionOptions.Clear();
				this.ResolutionDropDown = new DropOptions(new Rectangle(this.MainOptionsRect.X + this.MainOptionsRect.Width / 2 + 10, (int)this.Resolution.NamePosition.Y + 3, 105, 18));
				foreach (DisplayMode mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
				{
					if (mode.Width < 1280)
					{
						continue;
					}
					OptionsScreen.Option reso1 = new OptionsScreen.Option();
					//{ //business as usual
						reso1.x = mode.Width;
						reso1.y = mode.Height;
						reso1.Name = string.Concat(reso1.x.ToString(), " x ", reso1.y.ToString());
						reso1.NamePosition = Cursor;
                        reso1.ClickableArea = new Rectangle((int)reso1.NamePosition.X, (int)reso1.NamePosition.Y, (int)Fonts.Arial12Bold.MeasureString(reso1.Name).X, Fonts.Arial12Bold.LineSpacing);
					//};
					bool oktoadd = true;
					foreach (OptionsScreen.Option opt in this.ResolutionOptions)
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
					this.ResolutionDropDown.AddOption(reso1.Name, reso1);
					this.ResolutionOptions.Add(reso1);
					Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				}
				foreach (OptionsScreen.Option resolut in this.ResolutionOptions)
				{
					if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth != resolut.x || base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight != resolut.y)
					{
						continue;
					}
					foreach (Entry e in this.ResolutionDropDown.Options)
					{
						if ((e.ReferencedObject as OptionsScreen.Option).Name != resolut.Name)
						{
							continue;
						}
						this.ResolutionDropDown.ActiveIndex = this.ResolutionDropDown.Options.IndexOf(e);
					}
				}
				Cursor = new Vector2((float)this.SecondaryOptionsRect.X, (float)(this.SecondaryOptionsRect.Y + this.SecondaryOptionsRect.Height + 15));
				this.Apply.Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
				this.Apply.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"];
				this.Apply.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"];
				this.Apply.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"];
				this.Apply.Text = Localizer.Token(13);
				if (this.StartingMode != Game1.Instance.CurrentMode || this.startingx != this.xtoApply || this.startingy != this.ytoApply)
				{
					MessageBoxScreen messageBox = new MessageBoxScreen(Localizer.Token(14), 10f);
					messageBox.Accepted += new EventHandler<EventArgs>(this.AcceptChanges);
					messageBox.Cancelled += new EventHandler<EventArgs>(this.CancelChanges);
					base.ScreenManager.AddScreen(messageBox);
				}
				else
				{
					this.AcceptChanges(this, EventArgs.Empty);
				}
			}
			catch
			{
				this.CancelChanges(this, EventArgs.Empty);
			}
		}

		private void CancelChanges(object sender, EventArgs e1)
		{
			Game1.Instance.graphics.PreferredBackBufferWidth = this.startingx;
			Game1.Instance.graphics.PreferredBackBufferHeight = this.startingy;
			Game1.Instance.graphics.SynchronizeWithVerticalRetrace = false;
			Game1.Instance.SetWindowMode(this.StartingMode, this.startingx, this.startingy);
			this.ModeToSet = this.StartingMode;
			base.Setup();
			if (this.FromGame)
			{
				this.uScreen.LoadGraphics();
				this.gpmmscreen.LoadGraphics();
				this.LoadGraphics();
			}
			else
			{
				this.mmscreen.Buttons.Clear();
				this.mmscreen.LoadContent();
			}
			this.MainOptionsRect = new Rectangle(this.r.X + 20, this.r.Y + 175, 300, 375);
			this.SecondaryOptionsRect = new Rectangle(this.MainOptionsRect.X + this.MainOptionsRect.Width, this.MainOptionsRect.Y, 210, 305);
			this.GamespeedCap = new Checkbox(new Vector2((float)(this.MainOptionsRect.X + 20), (float)(this.MainOptionsRect.Y + 300)), Localizer.Token(2206), new Ref<bool>(() => GlobalStats.LimitSpeed, (bool x) => GlobalStats.LimitSpeed = x), Fonts.Arial12Bold)
			{
				Tip_Token = 2205
			};
			this.Resolution = new OptionsScreen.Option()
			{
				Name = string.Concat(Localizer.Token(9), ":     "),
				NamePosition = new Vector2((float)(this.MainOptionsRect.X + 20), (float)(this.MainOptionsRect.Y + 20))
			};
			string xResolution = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth.ToString();
			string yResolution = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight.ToString();
			this.xtoApply = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
			this.ytoApply = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
			string reso = string.Concat(xResolution, " x ", yResolution);
			this.Resolution.ClickableArea = new Rectangle(this.MainOptionsRect.X + this.MainOptionsRect.Width / 2 + 10, (int)this.Resolution.NamePosition.Y, (int)Fonts.Arial20Bold.MeasureString(reso).X, Fonts.Arial20Bold.LineSpacing);
			this.Resolution.Value = reso;
			this.Resolution.highlighted = false;
			this.FullScreen = new OptionsScreen.Option()
			{
				Name = string.Concat(Localizer.Token(10), ":     "),
				NamePosition = new Vector2((float)(this.MainOptionsRect.X + 20), (float)(this.MainOptionsRect.Y + Fonts.Arial20Bold.LineSpacing * 2 + 2 + 20)),
				Value = Game1.Instance.CurrentMode,
				ClickableArea = new Rectangle(this.MainOptionsRect.X + this.MainOptionsRect.Width / 2 + 10, (int)this.FullScreen.NamePosition.Y, (int)Fonts.Arial20Bold.MeasureString(this.FullScreen.Value.ToString()).X, Fonts.Arial20Bold.LineSpacing)
			};
			Rectangle ftlRect = new Rectangle(this.MainOptionsRect.X + 20, (int)this.FullScreen.NamePosition.Y + 40, 270, 50);
			this.MusicVolumeSlider = new FloatSlider(ftlRect, "Music Volume");
			this.MusicVolumeSlider.SetAmount(GlobalStats.Config.MusicVolume);
			this.MusicVolumeSlider.amount = GlobalStats.Config.MusicVolume;
			ftlRect = new Rectangle(this.MainOptionsRect.X + 20, (int)this.FullScreen.NamePosition.Y + 120, 270, 50);
			this.EffectsVolumeSlider = new FloatSlider(ftlRect, "Effects Volume");
			this.EffectsVolumeSlider.SetAmount(GlobalStats.Config.EffectsVolume);
			this.EffectsVolumeSlider.amount = GlobalStats.Config.EffectsVolume;
			Vector2 Cursor = new Vector2((float)(this.SecondaryOptionsRect.X + 10), (float)(this.SecondaryOptionsRect.Y + 10));
			this.ResolutionOptions.Clear();
			this.ResolutionDropDown = new DropOptions(new Rectangle(this.MainOptionsRect.X + this.MainOptionsRect.Width / 2 + 10, (int)this.Resolution.NamePosition.Y + 3, 105, 18));
			foreach (DisplayMode mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
			{
				if (mode.Width < 1280)
				{
					continue;
				}
				OptionsScreen.Option reso1 = new OptionsScreen.Option();
				//{
					reso1.x = mode.Width;
					reso1.y = mode.Height;
					reso1.Name = string.Concat(reso1.x.ToString(), " x ", reso1.y.ToString());
					reso1.NamePosition = Cursor;
                    reso1.ClickableArea = new Rectangle((int)reso1.NamePosition.X, (int)reso1.NamePosition.Y, (int)Fonts.Arial12Bold.MeasureString(reso1.Name).X, Fonts.Arial12Bold.LineSpacing);
				//};
				bool oktoadd = true;
				foreach (OptionsScreen.Option opt in this.ResolutionOptions)
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
				this.ResolutionDropDown.AddOption(reso1.Name, reso1);
				this.ResolutionOptions.Add(reso1);
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
			}
			foreach (OptionsScreen.Option resolut in this.ResolutionOptions)
			{
				if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth != resolut.x || base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight != resolut.y)
				{
					continue;
				}
				foreach (Entry e in this.ResolutionDropDown.Options)
				{
					if ((e.ReferencedObject as OptionsScreen.Option).Name != resolut.Name)
					{
						continue;
					}
					this.ResolutionDropDown.ActiveIndex = this.ResolutionDropDown.Options.IndexOf(e);
				}
			}
			Cursor = new Vector2((float)this.SecondaryOptionsRect.X, (float)(this.SecondaryOptionsRect.Y + this.SecondaryOptionsRect.Height + 15));
			this.Apply.Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
			this.Apply.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"];
			this.Apply.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"];
			this.Apply.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"];
			this.Apply.Text = Localizer.Token(13);
		}

		public new void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual new void Dispose(bool disposing)
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
			if (this.fade)
			{
				base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			}
			base.DrawBase(gameTime);
			base.ScreenManager.SpriteBatch.Begin();
			Selector selector = new Selector(base.ScreenManager, this.MainOptionsRect, true);
			Selector selector1 = new Selector(base.ScreenManager, this.SecondaryOptionsRect, true);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.Resolution.Name, this.Resolution.NamePosition, new Color(255, 239, 208));
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.FullScreen.Name, this.FullScreen.NamePosition, new Color(255, 239, 208));
			Vector2 valuePos = new Vector2((float)(this.FullScreen.ClickableArea.X + 2), (float)this.FullScreen.ClickableArea.Y);
			if (!this.FullScreen.highlighted)
			{
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.FullScreen.Value.ToString(), valuePos, Color.White);
			}
			else
			{
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.FullScreen.Value.ToString(), valuePos, new Color(255, 239, 208));
			}
			foreach (UIButton b in this.Buttons)
			{
				b.Draw(base.ScreenManager.SpriteBatch);
			}
			this.GamespeedCap.Draw(base.ScreenManager);
			this.ForceFullSim.Draw(base.ScreenManager);
			this.MusicVolumeSlider.DrawDecimal(base.ScreenManager);
			this.EffectsVolumeSlider.DrawDecimal(base.ScreenManager);
			this.ResolutionDropDown.Draw(base.ScreenManager.SpriteBatch);
			ToolTip.Draw(base.ScreenManager);
			base.ScreenManager.SpriteBatch.End();
		}

		public override void ExitScreen()
		{
			this.SaveConfigChanges();
			base.ExitScreen();
		}

		~OptionsScreen()
		{
			this.Dispose(false);
		}

		public override void HandleInput(InputState input)
		{
			this.currentMouse = input.CurrentMouseState;
			Vector2 MousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
			this.GamespeedCap.HandleInput(input);
			this.ForceFullSim.HandleInput(input);
			if (!this.ResolutionDropDown.Open && !this.AntiAliasingDD.Open)
			{
				this.MusicVolumeSlider.HandleInput(input);
				GlobalStats.Config.MusicVolume = this.MusicVolumeSlider.amount;
				base.ScreenManager.musicCategory.SetVolume(this.MusicVolumeSlider.amount);
				this.EffectsVolumeSlider.HandleInput(input);
				GlobalStats.Config.EffectsVolume = this.EffectsVolumeSlider.amount;
				base.ScreenManager.weaponsCategory.SetVolume(this.EffectsVolumeSlider.amount);
			}
			if (!this.ResolutionDropDown.Open)
			{
				if (!HelperFunctions.CheckIntersection(this.FullScreen.ClickableArea, MousePos))
				{
					this.FullScreen.highlighted = false;
				}
				else
				{
					if (!this.FullScreen.highlighted)
					{
						AudioManager.PlayCue("sd_ui_mouseover");
					}
					this.FullScreen.highlighted = true;
					if (input.InGameSelect)
					{
						AudioManager.PlayCue("blip_click");
						OptionsScreen modeToSet = this;
						modeToSet.ModeToSet = (Game1.WindowMode)((int)modeToSet.ModeToSet + (int)Game1.WindowMode.Windowed);
						if (this.ModeToSet > Game1.WindowMode.Borderless)
						{
							this.ModeToSet = Game1.WindowMode.Fullscreen;
						}
						this.FullScreen.Value = this.ModeToSet.ToString();
					}
				}
			}
			if (input.Escaped || input.RightMouseClick)
			{
				this.ExitScreen();
			}
			for (int i = 0; i < this.Buttons.Count; i++)
			{
				UIButton b = this.Buttons[i];
				if (b != null)
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
						if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Released)
						{
							string launches = b.Launches;
							if (launches != null && launches == "Apply Settings")
							{
								this.ApplySettings();
							}
						}
					}
				}
			}
			this.ResolutionDropDown.HandleInput(input);
			this.previousMouse = input.LastMouseState;
			base.HandleInput(input);
		}

        public override void LoadContent()
        {
            base.LoadContent();
            this.MainOptionsRect = new Rectangle(this.r.X + 20, this.r.Y + 175, 300, 375);
            this.GamespeedCap = new Checkbox(new Vector2((float)(this.MainOptionsRect.X + 20), (float)(this.MainOptionsRect.Y + 300)), Localizer.Token(2206), new Ref<bool>((Func<bool>)(() => GlobalStats.LimitSpeed), (Action<bool>)(x => GlobalStats.LimitSpeed = x)), Fonts.Arial12Bold);
            this.GamespeedCap.Tip_Token = 2205;
            this.ForceFullSim = new Checkbox(new Vector2((float)(this.MainOptionsRect.X + 20), (float)(this.MainOptionsRect.Y + 330)), "Force Full Simulation", new Ref<bool>((Func<bool>)(() => GlobalStats.ForceFullSim), (Action<bool>)(x => GlobalStats.ForceFullSim = x)), Fonts.Arial12Bold);
            this.ForceFullSim.Tip_Token = 2205;
            this.SecondaryOptionsRect = new Rectangle(this.MainOptionsRect.X + this.MainOptionsRect.Width + 20, this.MainOptionsRect.Y, 210, 305);
            this.Resolution = new OptionsScreen.Option();
            this.Resolution.Name = Localizer.Token(9) + ":";
            this.Resolution.NamePosition = new Vector2((float)(this.MainOptionsRect.X + 20), (float)this.MainOptionsRect.Y);
            string str1 = this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth.ToString();
            string str2 = this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight.ToString();
            this.startingx = this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            this.startingy = this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
            this.startingfullscreen = Game1.Instance.graphics.IsFullScreen;
            this.xtoApply = this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            this.ytoApply = this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
            string text = str1 + " x " + str2;
            this.Resolution.ClickableArea = new Rectangle(this.MainOptionsRect.X + this.MainOptionsRect.Width / 2 + 10, (int)this.Resolution.NamePosition.Y, (int)Fonts.Arial20Bold.MeasureString(text).X, Fonts.Arial20Bold.LineSpacing);
            this.Resolution.Value = (object)text;
            this.Resolution.highlighted = false;
            this.FullScreen = new OptionsScreen.Option();
            this.FullScreen.Name = Localizer.Token(10) + ":";
            this.FullScreen.NamePosition = new Vector2((float)(this.MainOptionsRect.X + 20), (float)(this.MainOptionsRect.Y + Fonts.Arial20Bold.LineSpacing * 2 + 2 + 17));
            this.FullScreen.Value = (object)((object)Game1.Instance.CurrentMode).ToString();
            this.FullScreen.ClickableArea = new Rectangle(this.MainOptionsRect.X + this.MainOptionsRect.Width / 2 + 10, (int)this.FullScreen.NamePosition.Y, (int)Fonts.Arial20Bold.MeasureString(this.FullScreen.Value.ToString()).X, Fonts.Arial20Bold.LineSpacing);
            Rectangle r = new Rectangle(this.MainOptionsRect.X + 9, (int)this.FullScreen.NamePosition.Y + 40, 270, 50);
            this.MusicVolumeSlider = new FloatSlider(r, "Music Volume");
            this.MusicVolumeSlider.SetAmount(GlobalStats.Config.MusicVolume);
            this.MusicVolumeSlider.amount = GlobalStats.Config.MusicVolume;
            r = new Rectangle(this.MainOptionsRect.X + 9, (int)this.FullScreen.NamePosition.Y + 120, 270, 50);
            this.EffectsVolumeSlider = new FloatSlider(r, "Effects Volume");
            this.EffectsVolumeSlider.SetAmount(GlobalStats.Config.EffectsVolume);
            this.EffectsVolumeSlider.amount = GlobalStats.Config.EffectsVolume;
            Vector2 vector2 = new Vector2((float)(this.SecondaryOptionsRect.X + 10), (float)(this.SecondaryOptionsRect.Y + 10));
            this.ResolutionDropDown = new DropOptions(new Rectangle(this.MainOptionsRect.X + this.MainOptionsRect.Width / 2 + 10, (int)this.Resolution.NamePosition.Y - 2, 105, 18));
            foreach (DisplayMode displayMode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
            {
                if (displayMode.Width >= 1280)
                {
                    OptionsScreen.Option option1 = new OptionsScreen.Option();
                    option1.x = displayMode.Width;
                    option1.y = displayMode.Height;
                    option1.Name = option1.x.ToString() + " x " + option1.y.ToString();
                    option1.NamePosition = vector2;
                    option1.ClickableArea = new Rectangle((int)option1.NamePosition.X, (int)option1.NamePosition.Y, (int)Fonts.Arial12Bold.MeasureString(option1.Name).X, Fonts.Arial12Bold.LineSpacing);
                    bool flag = true;
                    foreach (OptionsScreen.Option option2 in this.ResolutionOptions)
                    {
                        if (option2.x == option1.x && option2.y == option1.y)
                            flag = false;
                    }
                    if (flag)
                    {
                        this.ResolutionDropDown.AddOption(option1.Name, (object)option1);
                        this.ResolutionOptions.Add(option1);
                        vector2.Y += (float)Fonts.Arial12Bold.LineSpacing;
                    }
                }
            }
            int qualityLevels = 0;
            this.AntiAliasingDD = new DropOptions(new Rectangle(this.MainOptionsRect.X + this.MainOptionsRect.Width / 2 + 10, (int)this.Resolution.NamePosition.Y + 26, 105, 18));
            if (GraphicsAdapter.DefaultAdapter.CheckDeviceMultiSampleType(DeviceType.Hardware, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Format, false, MultiSampleType.EightSamples, out qualityLevels))
                this.AntiAliasingDD.AddOption("8x AA", 8);
            if (GraphicsAdapter.DefaultAdapter.CheckDeviceMultiSampleType(DeviceType.Hardware, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Format, false, MultiSampleType.EightSamples, out qualityLevels))
                this.AntiAliasingDD.AddOption("4x AA", 4);
            if (GraphicsAdapter.DefaultAdapter.CheckDeviceMultiSampleType(DeviceType.Hardware, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Format, false, MultiSampleType.EightSamples, out qualityLevels))
                this.AntiAliasingDD.AddOption("2x AA", 2);
            this.AntiAliasingDD.AddOption("No AA", 0);
            this.AntiAliasingDD.ActiveIndex = 0;
            switch (GlobalStats.Config.AASamples)
            {
                case 0:
                    this.AntiAliasingDD.ActiveIndex = 3;
                    break;
                case 2:
                    this.AntiAliasingDD.ActiveIndex = 2;
                    break;
                case 4:
                    this.AntiAliasingDD.ActiveIndex = 1;
                    break;
                case 8:
                    this.AntiAliasingDD.ActiveIndex = 0;
                    break;
            }
            foreach (OptionsScreen.Option option in this.ResolutionOptions)
            {
                if (this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth == option.x && this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight == option.y)
                {
                    foreach (Entry entry in this.ResolutionDropDown.Options)
                    {
                        if ((entry.ReferencedObject as OptionsScreen.Option).Name == option.Name)
                            this.ResolutionDropDown.ActiveIndex = this.ResolutionDropDown.Options.IndexOf(entry);
                    }
                }
            }
            vector2 = new Vector2((float)this.SecondaryOptionsRect.X, (float)(this.SecondaryOptionsRect.Y + this.SecondaryOptionsRect.Height + 15));
            this.Apply = new UIButton();
            this.Apply.Rect = new Rectangle((int)vector2.X, (int)vector2.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
            this.Apply.NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"];
            this.Apply.HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"];
            this.Apply.PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"];
            this.Apply.Text = Localizer.Token(13);
            this.Apply.Launches = "Apply Settings";
            this.Buttons.Add(this.Apply);
        }

		private void LoadGraphics()
		{
			this.r = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - this.r.Width / 2, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - this.r.Height / 2, this.r.Width, this.r.Height);
			this.MainOptionsRect = new Rectangle(this.r.X + 20, this.r.Y + 175, 300, 375);
			this.SecondaryOptionsRect = new Rectangle(this.MainOptionsRect.X + this.MainOptionsRect.Width + 20, this.MainOptionsRect.Y, 210, 305);
			this.Resolution = new OptionsScreen.Option()
			{
				Name = string.Concat(Localizer.Token(9), ":"),
				NamePosition = new Vector2((float)(this.MainOptionsRect.X + 20), (float)this.MainOptionsRect.Y)
			};
			string xResolution = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth.ToString();
			string yResolution = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight.ToString();
			string reso = string.Concat(xResolution, " x ", yResolution);
			this.Resolution.ClickableArea = new Rectangle(this.MainOptionsRect.X + this.MainOptionsRect.Width / 2 + 10, (int)this.Resolution.NamePosition.Y, (int)Fonts.Arial20Bold.MeasureString(reso).X, Fonts.Arial20Bold.LineSpacing);
			this.Resolution.Value = reso;
			this.Resolution.highlighted = false;
			this.FullScreen = new OptionsScreen.Option()
			{
				Name = string.Concat(Localizer.Token(10), ":"),
				NamePosition = new Vector2((float)(this.MainOptionsRect.X + 20), (float)(this.MainOptionsRect.Y + Fonts.Arial20Bold.LineSpacing * 2 + 2 + 20)),
				Value = Game1.Instance.CurrentMode.ToString(),
				ClickableArea = new Rectangle(this.MainOptionsRect.X + this.MainOptionsRect.Width / 2 + 10, (int)this.FullScreen.NamePosition.Y, (int)Fonts.Arial20Bold.MeasureString(this.FullScreen.Value.ToString()).X, Fonts.Arial20Bold.LineSpacing)
			};
			Rectangle ftlRect = new Rectangle(this.MainOptionsRect.X + 20, (int)this.FullScreen.NamePosition.Y + 40, 270, 50);
			this.MusicVolumeSlider = new FloatSlider(ftlRect, "Music Volume");
			this.MusicVolumeSlider.SetAmount(GlobalStats.Config.MusicVolume);
			this.MusicVolumeSlider.amount = GlobalStats.Config.MusicVolume;
			ftlRect = new Rectangle(this.MainOptionsRect.X + 20, (int)this.FullScreen.NamePosition.Y + 120, 270, 50);
			this.EffectsVolumeSlider = new FloatSlider(ftlRect, "Effects Volume");
			this.EffectsVolumeSlider.SetAmount(GlobalStats.Config.EffectsVolume);
			this.EffectsVolumeSlider.amount = GlobalStats.Config.EffectsVolume;
			Vector2 Cursor = new Vector2((float)(this.SecondaryOptionsRect.X + 10), (float)(this.SecondaryOptionsRect.Y + 10));
			this.ResolutionDropDown = new DropOptions(new Rectangle(this.MainOptionsRect.X + this.MainOptionsRect.Width / 2 + 10, (int)this.Resolution.NamePosition.Y + 3, 105, 18));
			foreach (DisplayMode mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
			{
				if (mode.Width < 1280)
				{
					continue;
				}
				OptionsScreen.Option reso1 = new OptionsScreen.Option();
				//{
					reso1.x = mode.Width;
					reso1.y = mode.Height;
					reso1.Name = string.Concat(reso1.x.ToString(), " x ", reso1.y.ToString());
					reso1.NamePosition = Cursor;
                    reso1.ClickableArea = new Rectangle((int)reso1.NamePosition.X, (int)reso1.NamePosition.Y, (int)Fonts.Arial12Bold.MeasureString(reso1.Name).X, Fonts.Arial12Bold.LineSpacing);
				//};
				bool oktoadd = true;
				foreach (OptionsScreen.Option opt in this.ResolutionOptions)
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
				this.ResolutionDropDown.AddOption(reso1.Name, reso1);
				this.ResolutionOptions.Add(reso1);
				Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
			}
			foreach (OptionsScreen.Option resolut in this.ResolutionOptions)
			{
				if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth != resolut.x || base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight != resolut.y)
				{
					continue;
				}
				foreach (Entry e in this.ResolutionDropDown.Options)
				{
					if ((e.ReferencedObject as OptionsScreen.Option).Name != resolut.Name)
					{
						continue;
					}
					this.ResolutionDropDown.ActiveIndex = this.ResolutionDropDown.Options.IndexOf(e);
				}
			}
			Cursor = new Vector2((float)this.SecondaryOptionsRect.X, (float)(this.SecondaryOptionsRect.Y + this.SecondaryOptionsRect.Height + 15));
			this.Apply.Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
		}

		private void SaveConfigChanges()
		{
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			int musicVolume = (int)((float)(GlobalStats.Config.MusicVolume * 100f));
			string amt = musicVolume.ToString("00");
			config.AppSettings.Settings["MusicVolume"].Value = amt;
			int effectsVolume = (int)((float)(GlobalStats.Config.EffectsVolume * 100f));
			amt = effectsVolume.ToString("00");
			config.AppSettings.Settings["EffectsVolume"].Value = amt;
			config.AppSettings.Settings["XRES"].Value = Game1.Instance.graphics.PreferredBackBufferWidth.ToString();
			config.AppSettings.Settings["YRES"].Value = Game1.Instance.graphics.PreferredBackBufferHeight.ToString();
			config.AppSettings.Settings["WindowMode"].Value = GlobalStats.Config.WindowMode.ToString();
			config.AppSettings.Settings["RanOnce"].Value = "true";
			config.AppSettings.Settings["ForceFullSim"].Value = (GlobalStats.ForceFullSim ? "true" : "false");
			config.Save();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

		private class Option
		{
			public string Name;

			public Vector2 NamePosition;

			public Rectangle ClickableArea;

			public object Value;

			public int x;

			public int y;

			public bool highlighted;

			public Option()
			{
			}
		}
	}
}