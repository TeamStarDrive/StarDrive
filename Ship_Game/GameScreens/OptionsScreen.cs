using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
    public sealed class OptionsScreen : PopupWindow
    {
        public bool fade = true;
        public bool FromGame;
        private readonly MainMenuScreen MainMenu;
        private readonly UniverseScreen Universe;
        private readonly GameplayMMScreen UniverseMainMenu; // the little menu in universe view
        private DropOptions<DisplayMode> ResolutionDropDown;
        private Rectangle LeftArea;
        private Rectangle RightArea;

        private readonly WindowMode StartingMode = GlobalStats.WindowMode;
        private int OriginalWidth;
        private int OriginalHeight;
        private WindowMode ModeToSet = GlobalStats.WindowMode;
        private int NewWidth;
        private int NewHeight;

        private FloatSlider MusicVolumeSlider;
        private FloatSlider EffectsVolumeSlider;

        private FloatSlider IconSize;
        private FloatSlider ShipLimiter;
        private FloatSlider FreighterLimiter;
        private FloatSlider AutoSaveFreq;     // Added by Gretman

        public OptionsScreen(MainMenuScreen s) : base(s, 600, 600)
        {
            MainMenu = s;
            IsPopup = true;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            TitleText  = Localizer.Token(4);
            MiddleText = Localizer.Token(4004);
        }

        public OptionsScreen(UniverseScreen s, GameplayMMScreen universeMainMenuScreen) : base(s, 600, 700)
        {
            UniverseMainMenu = universeMainMenuScreen;
            Universe = s;
            fade = false;
            IsPopup = true;
            FromGame = true;
            TransitionOnTime = TimeSpan.FromSeconds(0);
            TransitionOffTime = TimeSpan.FromSeconds(0);
        }

        private static string AntiAliasString()
        {
            if (GlobalStats.AntiAlias == 0)
                return "No AA";
            return GlobalStats.AntiAlias + "x MSAA";
        }

        private static string TextureFilterString()
        {
            if (GlobalStats.MaxAnisotropy == 0)
                return new string[]{"Bilinear", "Trilinear"}[GlobalStats.TextureSampling];
            return "Anisotropic x" + GlobalStats.MaxAnisotropy;
        }

        private static string QualityString(int parameter)
        {
            return parameter >= 0 && parameter <= 3 //"Off" makes no sense for texture quality
                ? new string[] { "High", "Normal", "Low", "Ultra-Low" }[parameter] 
                : "Uh what?";
        }

        private void InitScreen()
        {
            LeftArea  = new Rectangle(Rect.X + 20, Rect.Y + 175, 300, 375);
            RightArea = new Rectangle(LeftArea.Right + 20, LeftArea.Y, 210, 305);

            float x  = LeftArea.X + 10;
            float y  = LeftArea.Y;
            float cx = LeftArea.Center.X;
            float startY = y;
            Label(x, y, $"{Localizer.Token(9)}: ");

            y += Fonts.Arial12Bold.LineSpacing * 1.5f;
            Label(x, y, $"{Localizer.Token(10)}: ");

            UILabel fullscreen = Label(cx, y, GlobalStats.WindowMode.ToString());
            fullscreen.OnClick += (label)=>
            {
                ++ModeToSet;
                if (ModeToSet > WindowMode.Borderless)
                    ModeToSet = WindowMode.Fullscreen;
                label.Text = ModeToSet.ToString();
            };

            y += Fonts.Arial12Bold.LineSpacing * 1.5f;
            Label(x, y, "AntiAliasing");
            UILabel aa = Label(cx, y, AntiAliasString());
            aa.OnClick += (label)=>
            {
                GlobalStats.AntiAlias = GlobalStats.AntiAlias == 0 ? 2 : GlobalStats.AntiAlias * 2;
                if (GlobalStats.AntiAlias > 8)
                    GlobalStats.AntiAlias = 0;
                aa.Text = AntiAliasString();
            };

            y += Fonts.Arial12Bold.LineSpacing * 1.5f;
            Label(x, y, "Texture Quality");
            UILabel tq = Label(cx, y, QualityString(GlobalStats.TextureQuality));
            tq.OnClick += (label) =>
            {

                GlobalStats.TextureQuality = GlobalStats.TextureQuality == 3 
                ? 0
                : GlobalStats.TextureQuality + 1;

                tq.Text = QualityString(GlobalStats.TextureQuality);
            };

            y += Fonts.Arial12Bold.LineSpacing * 1.5f;
            Label(x, y, "Texture Filtering");
            UILabel tf = Label(cx, y, TextureFilterString());
            tf.OnClick += (label) =>
            {

                GlobalStats.TextureSampling += 1;
                if (GlobalStats.TextureSampling >= 2)
                {
                    GlobalStats.MaxAnisotropy  += 1;
                    GlobalStats.TextureSampling = 2;
                }
                if (GlobalStats.MaxAnisotropy > 4)
                {
                    GlobalStats.MaxAnisotropy   = 0;
                    GlobalStats.TextureSampling = 0;
                }
                tf.Text = TextureFilterString();
            };

            y += Fonts.Arial12Bold.LineSpacing * 1.5f;
            Label(x, y, "Shadow Quality");
            UILabel sq = Label(cx, y, QualityString(GlobalStats.ShadowDetail));
            sq.OnClick += (label) =>
            {

                GlobalStats.ShadowDetail = GlobalStats.ShadowDetail == 3
                ? 0
                : GlobalStats.ShadowDetail + 1;
                //1.0f is max quality, pairing with detail level here to keep options clutter to a minimum.
                GlobalStats.ShadowQuality = 1.0f - (0.33f * GlobalStats.ShadowDetail);

                sq.Text = QualityString(GlobalStats.ShadowDetail);
            };

            y += Fonts.Arial12Bold.LineSpacing * 1.5f;
            Label(x, y, "Effects Quality");
            UILabel eq = Label(cx, y, QualityString(GlobalStats.EffectDetail));
            eq.OnClick += (label) =>
            {

                GlobalStats.EffectDetail = GlobalStats.EffectDetail == 3
                ? 0
                : GlobalStats.EffectDetail + 1;

                eq.Text = QualityString(GlobalStats.EffectDetail);
            };

            y += Fonts.Arial12Bold.LineSpacing * 2;
            var pos = new Vector2(cx, y);
            Checkbox(pos, () => GlobalStats.RenderBloom, "Bloom", 
                     "Disabling bloom effect will increase performance on low-end devices");


            int nextX = (int)x;
            int nextY = (int)pos.Y + 5;

            var r = new Rectangle(nextX, nextY, 270, 50);
            MusicVolumeSlider = SliderPercent(r, "Music Volume", 0f, 1f, GlobalStats.MusicVolume);

            r = new Rectangle(nextX, nextY + 50, 270, 50);
            EffectsVolumeSlider = SliderPercent(r, "Effects Volume", 0f, 1f, GlobalStats.EffectsVolume);

            r = new Rectangle(nextX, nextY + 110, 225, 50);
            IconSize = Slider(r, "Icon Sizes", 0, 30, GlobalStats.IconSize);
            r = new Rectangle(nextX, nextY + 160, 225, 50);
            AutoSaveFreq = Slider(r, "Autosave Frequency", 60, 540, GlobalStats.AutoSaveFreq);
            AutoSaveFreq.TooltipId = 4100;


            r = new Rectangle(RightArea.X, nextY, 225, 50);//nextY + 110, 225, 50);
            FreighterLimiter = Slider(r, "Per AI Freighter Limit.", 25, 125, GlobalStats.FreighterLimit);
            r = new Rectangle(RightArea.X, nextY + 50, 225, 50);//nextY + 160, 225, 50);
            ShipLimiter = Slider(r, $"All AI Ship Limit. AI Ships: {Empire.Universe?.globalshipCount ?? 0}", 
                                 500, 3500, GlobalStats.ShipCountLimit);

            pos = new Vector2(RightArea.X, RightArea.Y);
            var cb = Checkbox(pos, () => GlobalStats.LimitSpeed,          title: 2206, tooltip: 2205);

            Vector2 NextPos() => pos = new Vector2(pos.X, pos.Y + cb.Height + 15);
            Checkbox(NextPos(), () => GlobalStats.ForceFullSim,        "Force Full Simulation", tooltip: 5086);
            Checkbox(NextPos(), () => GlobalStats.PauseOnNotification, title: 6007, tooltip: 7004);
            Checkbox(NextPos(), () => GlobalStats.AltArcControl,       title: 6184, tooltip: 7081);
            Checkbox(NextPos(), () => GlobalStats.ZoomTracking,        title: 6185, tooltip: 7082);
            Checkbox(NextPos(), () => GlobalStats.AutoErrorReport, "Automatic Error Report", 
                                "Send automatic error reports to Blackbox developers");


            UIButton apply = Button(RightArea.X, RightArea.Y + RightArea.Height + 60, "Apply Settings", localization:13);
            apply.OnClick += button => ApplySettings();

            CreateResolutionDropOptions(startY);
        }

        private void CreateResolutionDropOptions(float y)
        {
            ResolutionDropDown = DropOptions<DisplayMode>(LeftArea.Center.X, (int)y, 105, 18);

            int screenWidth  = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            int screenHeight = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;

            DisplayModeCollection displayModes = GraphicsAdapter.DefaultAdapter.SupportedDisplayModes;
            foreach (DisplayMode mode in displayModes)
            {
                if (mode.Width < 1280 || mode.Format != SurfaceFormat.Bgr32)
                    continue;
                if (ResolutionDropDown.Contains(existing => mode.Width == existing.Width && mode.Height == existing.Height))
                    continue;

                ResolutionDropDown.AddOption($"{mode.Width} x {mode.Height}", mode);

                if (mode.Width == screenWidth && mode.Height == screenHeight)
                    ResolutionDropDown.ActiveIndex = ResolutionDropDown.Count-1;
            }
        }

        private void ReloadGameContent()
        {
            if (FromGame)
            {
                Universe.LoadGraphics();
                Universe.NotificationManager.ReSize();
                UniverseMainMenu.LoadContent();
            }
            else
            {
                MainMenu.LoadContent();
            }

            base.LoadContent();
            InitScreen();
        }

        public override void LoadContent()
        {
            NewWidth  = OriginalWidth  = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            NewHeight = OriginalHeight = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;

            base.LoadContent();
            InitScreen();
        }

        private void ApplySettings()
        {
            try
            {
                DisplayMode activeOpt = ResolutionDropDown.ActiveValue;
                Game1.Instance.SetWindowMode(ModeToSet, activeOpt.Width, activeOpt.Height);

                ReloadGameContent();

                if (StartingMode != GlobalStats.WindowMode || OriginalWidth != NewWidth || OriginalHeight != NewHeight)
                {
                    var messageBox = new MessageBoxScreen(this, Localizer.Token(14), 10f);
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

        private void AcceptChanges(object sender, EventArgs e)
        {
            GlobalStats.SaveSettings();
            EffectsVolumeSlider.RelativeValue = GlobalStats.EffectsVolume;
            MusicVolumeSlider.RelativeValue   = GlobalStats.MusicVolume;
        }

        private void CancelChanges(object sender, EventArgs e1)
        {
            Game1.Instance.SetWindowMode(StartingMode, OriginalWidth, OriginalHeight);

            ModeToSet = StartingMode;
            NewWidth  = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            NewHeight = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
            ReloadGameContent();
        }


        public override void Draw(SpriteBatch spriteBatch)
        {
            if (fade) ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            base.Draw(spriteBatch);
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
                GlobalStats.IconSize       = (int)IconSize.AbsoluteValue;
                GlobalStats.ShipCountLimit = (int)ShipLimiter.AbsoluteValue;
                GlobalStats.FreighterLimit = (int)FreighterLimiter.AbsoluteValue;
                GlobalStats.AutoSaveFreq   = (int)AutoSaveFreq.AbsoluteValue;

                GlobalStats.MusicVolume   = MusicVolumeSlider.RelativeValue;
                GlobalStats.EffectsVolume = EffectsVolumeSlider.RelativeValue;
                GameAudio.ConfigureAudioSettings();
                return true;
            }
            return false;
        }
    }
}