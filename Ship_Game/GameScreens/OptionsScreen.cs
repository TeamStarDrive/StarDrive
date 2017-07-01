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

        public OptionsScreen(UniverseScreen s, GameplayMMScreen universeMainMenuScreen) : base(s, 600, 720)
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
                return new[]{"Bilinear", "Trilinear"}[GlobalStats.TextureSampling];
            return "Anisotropic x" + GlobalStats.MaxAnisotropy;
        }

        private static string QualityString(int parameter)
        {
            return (uint)parameter <= 3 ? new[]{ "High", "Normal", "Low", "Ultra-Low" }[parameter] : "None";
        }

        
        private static void AntiAliasing_OnClick(UILabel label)
        {
            GlobalStats.AntiAlias = GlobalStats.AntiAlias == 0 ? 2 : GlobalStats.AntiAlias * 2;
            if (GlobalStats.AntiAlias > 8)
                GlobalStats.AntiAlias = 0;
            label.Text = AntiAliasString();
        }

        private static void TextureQuality_OnClick(UILabel label)
        {
            GlobalStats.TextureQuality = GlobalStats.TextureQuality == 3 ? 0 : GlobalStats.TextureQuality + 1;
            label.Text = QualityString(GlobalStats.TextureQuality);
        }

        private static void TextureFiltering_OnClick(UILabel label)
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
            label.Text = TextureFilterString();
        }

        private static void ShadowQuality_OnClick(UILabel label)
        {
            GlobalStats.ShadowDetail = GlobalStats.ShadowDetail == 3 ? 0 : GlobalStats.ShadowDetail + 1;
            GlobalStats.ShadowQuality = 1.0f - (0.33f * GlobalStats.ShadowDetail);
            label.Text = QualityString(GlobalStats.ShadowDetail);
        }

        private void Fullscreen_OnClick(UILabel label)
        {
            ++ModeToSet;
            if (ModeToSet > WindowMode.Borderless)
                ModeToSet = WindowMode.Fullscreen;
            label.Text = ModeToSet.ToString();
        }

        private static void EffectsQuality_OnClick(UILabel label)
        {
            GlobalStats.EffectDetail = GlobalStats.EffectDetail == 3 ? 0 : GlobalStats.EffectDetail + 1;
            label.Text = QualityString(GlobalStats.EffectDetail);
        }


        private void InitScreen()
        {
            LeftArea  = new Rectangle(Rect.X + 20, Rect.Y + 150, 300, 375);
            RightArea = new Rectangle(LeftArea.Right + 10, LeftArea.Y, 210, 330);

            float spacing = Fonts.Arial12Bold.LineSpacing * 1.6f;

            BeginVLayout(LeftArea.X, LeftArea.Y, spacing);
                Label($"{Localizer.Token(9)}: ");
                Label($"{Localizer.Token(10)}: ");
                Label("Anti Aliasing: ");
                Label("Texture Quality: ");
                Label("Texture Filtering: ");
                Label("Shadow Quality: ");
                Label("Effects Quality: ");
            EndLayout();

            BeginVLayout(LeftArea.Center.X, LeftArea.Y, spacing);
                ResolutionDropDown = DropOptions<DisplayMode>(105, 18, zorder:10);
                Label(GlobalStats.WindowMode.ToString(),         Fullscreen_OnClick);
                Label(AntiAliasString(),                         AntiAliasing_OnClick);
                Label(QualityString(GlobalStats.TextureQuality), TextureQuality_OnClick);
                Label(TextureFilterString(),                     TextureFiltering_OnClick);
                Label(QualityString(GlobalStats.ShadowDetail),   ShadowQuality_OnClick);
                Label(QualityString(GlobalStats.EffectDetail),   EffectsQuality_OnClick);
                Checkbox(() => GlobalStats.RenderBloom, "Bloom", 
                    "Disabling bloom effect will increase performance on low-end devices");
            EndLayout();

            BeginVLayout(LeftArea.X, LeftArea.Y + 190, 60);
                MusicVolumeSlider   = SliderPercent(270, 50, "Music Volume", 0f, 1f, GlobalStats.MusicVolume);
                EffectsVolumeSlider = SliderPercent(270, 50, "Effects Volume", 0f, 1f, GlobalStats.EffectsVolume);
                IconSize            = Slider(270, 50, "Icon Sizes", 0, 30, GlobalStats.IconSize);
                AutoSaveFreq        = Slider(270, 50, "Autosave Frequency", 60, 540, GlobalStats.AutoSaveFreq);
                AutoSaveFreq.TooltipId = 4100;
            EndLayout();

            BeginVLayout(RightArea.X, RightArea.Y + 190, 60);
                FreighterLimiter = Slider(225, 50, "Per AI Freighter Limit.", 25, 125, GlobalStats.FreighterLimit);
                ShipLimiter      = Slider(225, 50, $"All AI Ship Limit. AI Ships: {Empire.Universe?.globalshipCount ?? 0}", 
                                          500, 3500, GlobalStats.ShipCountLimit);
            EndLayout();

            BeginVLayout(RightArea.X, RightArea.Y, spacing);
                Checkbox(() => GlobalStats.ForceFullSim,        "Force Full Simulation", tooltip: 5086);
                Checkbox(() => GlobalStats.PauseOnNotification, title: 6007, tooltip: 7004);
                Checkbox(() => GlobalStats.AltArcControl,       title: 6184, tooltip: 7081);
                Checkbox(() => GlobalStats.ZoomTracking,        title: 6185, tooltip: 7082);
                Checkbox(() => GlobalStats.AutoErrorReport, "Automatic Error Report", 
                                "Send automatic error reports to Blackbox developers");
            EndLayout();

            BeginVLayout(RightArea.X, RightArea.Bottom + 60);
                Button(titleId:13, click: button => ApplySettings());
            EndLayout();

            CreateResolutionDropOptions();
        }

        private void CreateResolutionDropOptions()
        {
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