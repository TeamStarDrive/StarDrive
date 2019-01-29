using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.GameScreens.MainMenu;
using Ship_Game.UI;

namespace Ship_Game
{
    public class GraphicsSettings
    {
        public WindowMode Mode;
        public int Width, Height;
        public int AntiAlias;
        public int MaxAnisotropy;
        public int TextureSampling;
        public int TextureQuality;
        public int ShadowDetail;
        public float ShadowQuality;
        public int EffectDetail;
        public bool RenderBloom;

        public static GraphicsSettings FromGlobalStats()
        {
            var settings = new GraphicsSettings();
            settings.LoadGlobalStats();
            return settings;
        }

        public GraphicsSettings GetClone() => (GraphicsSettings)MemberwiseClone();

        public void LoadGlobalStats()
        {
            Mode            = GlobalStats.WindowMode;
            Width           = GlobalStats.XRES;
            Height          = GlobalStats.YRES;
            AntiAlias       = GlobalStats.AntiAlias;
            MaxAnisotropy   = GlobalStats.MaxAnisotropy;
            TextureSampling = GlobalStats.TextureSampling;
            TextureQuality  = GlobalStats.TextureQuality;
            ShadowDetail    = GlobalStats.ShadowDetail;
            ShadowQuality   = GlobalStats.ShadowQuality;
            EffectDetail    = GlobalStats.EffectDetail;
            RenderBloom     = GlobalStats.RenderBloom;
        }

        public void SaveGlobalStats()
        {
            GlobalStats.WindowMode      = Mode;
            GlobalStats.XRES            = Width;
            GlobalStats.YRES            = Height;
            GlobalStats.AntiAlias       = AntiAlias;
            GlobalStats.MaxAnisotropy   = MaxAnisotropy;
            GlobalStats.TextureSampling = TextureSampling;
            GlobalStats.TextureQuality  = TextureQuality;
            GlobalStats.ShadowDetail    = ShadowDetail;
            GlobalStats.ShadowQuality   = ShadowQuality;
            GlobalStats.EffectDetail    = EffectDetail;
            GlobalStats.RenderBloom     = RenderBloom;
            GlobalStats.SaveSettings();
        }

        public void ApplyGraphicSettings()
        {
            SaveGlobalStats();
            StarDriveGame.Instance.ApplyGraphics(this);
        }

        public bool Equals(GraphicsSettings other)
        {
            if (this == other) return true;
            return Mode            == other.Mode 
                && Width           == other.Width 
                && Height          == other.Height 
                && AntiAlias       == other.AntiAlias 
                && MaxAnisotropy   == other.MaxAnisotropy 
                && TextureSampling == other.TextureSampling 
                && TextureQuality  == other.TextureQuality 
                && ShadowDetail    == other.ShadowDetail 
                && ShadowQuality.Equals(other.ShadowQuality) 
                && EffectDetail    == other.EffectDetail 
                && RenderBloom     == other.RenderBloom;
        }
    }

    public sealed class OptionsScreen : PopupWindow
    {
        public bool Fade = true;
        public bool FromGame;
        readonly MainMenuScreen MainMenu;
        readonly UniverseScreen Universe;
        readonly GameplayMMScreen UniverseMainMenu; // the little menu in universe view
        DropOptions<DisplayMode> ResolutionDropDown;
        Rectangle LeftArea;
        Rectangle RightArea;

        GraphicsSettings Original; // default starting options and those we have applied with success
        GraphicsSettings New;

        FloatSlider MusicVolumeSlider;
        FloatSlider EffectsVolumeSlider;

        FloatSlider IconSize;
        FloatSlider ShipLimiter;
        FloatSlider FreighterLimiter;
        FloatSlider AutoSaveFreq;     // Added by Gretman

        public OptionsScreen(MainMenuScreen s) : base(s, 600, 600)
        {
            MainMenu          = s;
            IsPopup           = true;
            TransitionOnTime  = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            TitleText         = Localizer.Token(4);
            MiddleText        = Localizer.Token(4004);
        }

        public OptionsScreen(UniverseScreen s, GameplayMMScreen universeMainMenuScreen) : base(s, 600, 720)
        {
            UniverseMainMenu  = universeMainMenuScreen;
            Universe          = s;
            Fade              = false;
            IsPopup           = true;
            FromGame          = true;
            TransitionOnTime  = TimeSpan.FromSeconds(0);
            TransitionOffTime = TimeSpan.FromSeconds(0);
        }


        string AntiAliasString()
        {
            if (New.AntiAlias == 0)
                return "No AA";
            return New.AntiAlias + "x MSAA";
        }

        string TextureFilterString()
        {
            if (New.MaxAnisotropy == 0)
                return new[]{"Bilinear", "Trilinear"}[New.TextureSampling];
            return "Anisotropic x" + New.MaxAnisotropy;
        }

        static string QualityString(int parameter)
        {
            return (uint)parameter <= 3 ? new[]{ "High", "Normal", "Low", "Ultra-Low" }[parameter] : "None";
        }


        void AntiAliasing_OnClick(UILabel label)
        {
            New.AntiAlias = New.AntiAlias == 0 ? 2 : New.AntiAlias * 2;
            if (New.AntiAlias > 8)
                New.AntiAlias = 0;
        }

        void TextureQuality_OnClick(UILabel label)
        {
            New.TextureQuality = New.TextureQuality == 3 ? 0 : New.TextureQuality + 1;
        }

        void TextureFiltering_OnClick(UILabel label)
        {
            New.TextureSampling += 1;
            if (New.TextureSampling >= 2)
            {
                New.MaxAnisotropy  += 1;
                New.TextureSampling = 2;
            }
            if (New.MaxAnisotropy > 4)
            {
                New.MaxAnisotropy   = 0;
                New.TextureSampling = 0;
            }
        }

        void ShadowQuality_OnClick(UILabel label)
        {
            New.ShadowDetail = New.ShadowDetail == 3 ? 0 : New.ShadowDetail + 1;
            New.ShadowQuality = 1.0f - (0.33f * New.ShadowDetail);
        }

        void Fullscreen_OnClick(UILabel label)
        {
            ++New.Mode;
            if (New.Mode > WindowMode.Borderless)
                New.Mode = WindowMode.Fullscreen;
        }

        void EffectsQuality_OnClick(UILabel label)
        {
            New.EffectDetail = New.EffectDetail == 3 ? 0 : New.EffectDetail + 1;
        }


        class KeyValueElement : UIElementV2
        {
            UILabel Key, Value;
            public KeyValueElement(UIElementV2 parent) : base(parent) {}

            public void Create(string title, Func<UILabel, string> getText)
            {
                Key = new UILabel(this, Vector2.Zero, $"{title}:");
                Value = new UILabel(this, Vector2.Zero, "");
                Value.DynamicText = getText;
            }

            public override void PerformLayout()
            {
                Key.Pos = Pos;
                Value.Pos = new Vector2(CenterX, Pos.Y);
            }

            public override bool HandleInput(InputState input)
            {
                return Key.HandleInput(input) || Value.HandleInput(input);
            }
            public override void Draw(SpriteBatch batch)
            {
                Key.Draw(batch);
                Value.Draw(batch);
            }
        }

        void Add(UIList graphics, string title, Func<UILabel, string> getText, Action<UILabel> onClick)
        {
            graphics.AddSplit(new UILabel($"{title}:"), new UILabel(getText, onClick))
                .Split = graphics.Width*0.5f;
        }

        void Add(UIList graphics, string title, UIElementV2 second)
        {
            graphics.AddSplit(new UILabel($"{title}:"), second)
                .Split = graphics.Width*0.5f;
        }

        void InitScreen()
        {
            LeftArea  = new Rectangle(Rect.X + 20, Rect.Y + 150, 300, 375);
            RightArea = new Rectangle(LeftArea.Right + 10, LeftArea.Y, 210, 330);

            UIList graphics = List(LeftArea.PosVec(), LeftArea.Size());
            graphics.Padding = new Vector2(2f, 4f);
            ResolutionDropDown = new DropOptions<DisplayMode>(105, 18);

            Add(graphics, Localizer.Token(9), ResolutionDropDown);
            Add(graphics, Localizer.Token(10), l => New.Mode.ToString(),               Fullscreen_OnClick);
            Add(graphics, "Anti Aliasing",     l => AntiAliasString(),                 AntiAliasing_OnClick);
            Add(graphics, "Texture Quality",   l => QualityString(New.TextureQuality), TextureQuality_OnClick);
            Add(graphics, "Texture Filtering", l => TextureFilterString(),             TextureFiltering_OnClick);
            Add(graphics, "Shadow Quality",    l => QualityString(New.ShadowDetail),   ShadowQuality_OnClick);
            Add(graphics, "Effects Quality",   l => QualityString(New.EffectDetail),   EffectsQuality_OnClick);
            graphics.AddCheckbox(() => New.RenderBloom, "Bloom", 
                "Disabling bloom effect will increase performance on low-end devices");

            float spacing = Fonts.Arial12Bold.LineSpacing * 1.6f;

            UIList left = List(new Vector2(LeftArea.X, LeftArea.Y + 180), LeftArea.Size());
            left.Padding = new Vector2(2f, 8f);
            MusicVolumeSlider   = left.Add(new FloatSlider(SliderStyle.Percent, 270f, 50f, "Music Volume",   0f, 1f, GlobalStats.MusicVolume));
            EffectsVolumeSlider = left.Add(new FloatSlider(SliderStyle.Percent, 270f, 50f, "Effects Volume", 0f, 1f, GlobalStats.EffectsVolume));
            IconSize            = left.Add(new FloatSlider(SliderStyle.Decimal, 270f, 50f, "Icon Sizes",     0f,30f, GlobalStats.IconSize));
            AutoSaveFreq        = left.Add(new FloatSlider(SliderStyle.Decimal, 270f, 50f, "AutoSave Frequency", 60, 540, GlobalStats.AutoSaveFreq));
            AutoSaveFreq.LocalizeTooltipId = 4100;


            BeginVLayout(RightArea.X, RightArea.Y + 190, 60);
                FreighterLimiter = Slider(225, 50, "Per AI Freighter Limit.", 25, 125, GlobalStats.FreighterLimit);
                ShipLimiter      = Slider(225, 50, $"All AI Ship Limit. AI Ships: {Empire.Universe?.globalshipCount ?? 0}", 
                                          500, 3500, GlobalStats.ShipCountLimit);
            EndLayout();

            BeginVLayout(RightArea.X, RightArea.Y, spacing);
                Checkbox(() => GlobalStats.ForceFullSim, "Force Full Simulation", tooltip: 5086);
                Checkbox(() => GlobalStats.PauseOnNotification, title: 6007, tooltip: 7004);
                Checkbox(() => GlobalStats.AltArcControl,       title: 6184, tooltip: 7081);
                Checkbox(() => GlobalStats.ZoomTracking,        title: 6185, tooltip: 7082);
                Checkbox(() => GlobalStats.AutoErrorReport, "Automatic Error Report", 
                                "Send automatic error reports to Blackbox developers");
                Checkbox(() => GlobalStats.DisableAsteroids, "Disable Asteroids",           //Added by Gretman
                                "This will prevent asteroids from being generated in new games, offering performance improvements in mid to late game. This will not affect current games or existing saves.");
            if (Debugger.IsAttached)
                    Checkbox(() => GlobalStats.WarpBehaviorsSetting, "Warp Behaviors (experimental)",
                        "Experimental and untested feature for complex Shield behaviors during Warp");
            EndLayout();

            BeginVLayout(RightArea.X, RightArea.Bottom + 60);
                Button(titleId:13, click: button => ApplyGraphicsSettings());
            EndLayout();

            CreateResolutionDropOptions();
        }

        void CreateResolutionDropOptions()
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

        void ReloadGameContent()
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
            Original = GraphicsSettings.FromGlobalStats();
            New = Original.GetClone(); // fresh copy!
            base.LoadContent();
            InitScreen();
        }

        void ApplyGraphicsSettings()
        {
            try
            {
                New.Width = ResolutionDropDown.ActiveValue.Width;
                New.Height = ResolutionDropDown.ActiveValue.Height;
                New.ApplyGraphicSettings();

                ReloadGameContent();

                if (!Original.Equals(New))
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

        void AcceptChanges(object sender, EventArgs e)
        {
            New.ApplyGraphicSettings();
            Original = New.GetClone(); // accepted!

            EffectsVolumeSlider.RelativeValue = GlobalStats.EffectsVolume;
            MusicVolumeSlider.RelativeValue   = GlobalStats.MusicVolume;
        }

        void CancelChanges(object sender, EventArgs e1)
        {
            Original.ApplyGraphicSettings();
            New = Original.GetClone(); // back to default!
            ReloadGameContent();
        }


        public override void Draw(SpriteBatch batch)
        {
            if (Fade) ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            base.Draw(batch);
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