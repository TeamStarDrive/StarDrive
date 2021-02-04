using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NAudio.CoreAudioApi;
using Ship_Game.Audio;
using Ship_Game.GameScreens.MainMenu;
using SynapseGaming.LightingSystem.Core;

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
        public int ShadowDetail; // 0=High, 1=Medium, 2=Low, 3=Off (DetailPreference enum)
        public int EffectDetail;
        public bool RenderBloom;
        public bool VSync;

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
            EffectDetail    = GlobalStats.EffectDetail;
            RenderBloom     = GlobalStats.RenderBloom;
            VSync           = GlobalStats.VSync;
        }

        void SetGlobalStats()
        {
            GlobalStats.WindowMode      = Mode;
            GlobalStats.XRES            = Width;
            GlobalStats.YRES            = Height;
            GlobalStats.AntiAlias       = AntiAlias;
            GlobalStats.MaxAnisotropy   = MaxAnisotropy;
            GlobalStats.TextureSampling = TextureSampling;
            GlobalStats.TextureQuality  = TextureQuality;
            GlobalStats.EffectDetail    = EffectDetail;
            GlobalStats.RenderBloom     = RenderBloom;
            GlobalStats.VSync           = VSync;
            GlobalStats.SetShadowDetail(ShadowDetail);
        }

        public void ApplyChanges()
        {
            // @note This MAY trigger StarDriveGame.UnloadContent() and LoadContent() !!!
            //       Only if graphics device reset fails and a new device must be created
            SetGlobalStats();
            bool deviceChanged = StarDriveGame.Instance.ApplyGraphics(this);
            
            // if device changed, then all game screens were already reloaded
            if (deviceChanged)
                return; // nothing to do here!

            // reload all screens, this is specific to StarDriveGame
            ScreenManager.Instance.LoadContent();
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
                && EffectDetail    == other.EffectDetail 
                && RenderBloom     == other.RenderBloom 
                && VSync           == other.VSync;
        }
    }

    public sealed class OptionsScreen : PopupWindow
    {
        readonly bool Fade = true;
        DropOptions<DisplayMode> ResolutionDropDown;
        DropOptions<MMDevice> SoundDevices;
        DropOptions<Language> CurrentLanguage;
        Rectangle LeftArea;
        Rectangle RightArea;

        GraphicsSettings Original; // default starting options and those we have applied with success
        GraphicsSettings New;

        FloatSlider MusicVolumeSlider;
        FloatSlider EffectsVolumeSlider;

        FloatSlider IconSize;
        FloatSlider AutoSaveFreq;

        FloatSlider SimulationFps;

        public OptionsScreen(MainMenuScreen mainMenu) : base(mainMenu, 600, 600)
        {
            IsPopup           = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;
            TitleText         = Localizer.Token(4);
            MiddleText        = Localizer.Token(4004);
            Original = GraphicsSettings.FromGlobalStats();
            New = Original.GetClone();
        }

        public OptionsScreen(UniverseScreen universe) : base(universe, 600, 720)
        {
            Fade              = false;
            IsPopup           = true;
            TransitionOnTime  = 0f;
            TransitionOffTime = 0f;
            Original = GraphicsSettings.FromGlobalStats();
            New = Original.GetClone();
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

        static string ShadowQualStr(int parameter)
        {
            return ((DetailPreference)parameter).ToString();
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
            // 0=High, 1=Medium, 2=Low, 3=Off
            New.ShadowDetail = New.ShadowDetail >= 3 ? 0 : New.ShadowDetail + 1;
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

        void Add(UIList graphics, string title, Func<UILabel, string> getText, Action<UILabel> onClick)
        {
            graphics.AddSplit(new UILabel($"{title}:"), new UILabel(getText, onClick))
                .Split = graphics.Width*0.4f;
        }

        void Add(UIList graphics, string title, UIElementV2 second)
        {
            graphics.AddSplit(new UILabel($"{title}:"), second)
                .Split = graphics.Width*0.4f;
        }

        void InitScreen()
        {
            LeftArea  = new Rectangle(Rect.X + 20, Rect.Y + 150, 290, 375);
            RightArea = new Rectangle(LeftArea.Right + 40, LeftArea.Y, 210, 330);

            UIList graphics = AddList(LeftArea.PosVec(), LeftArea.Size());
            graphics.Padding = new Vector2(2f, 4f);
            ResolutionDropDown = new DropOptions<DisplayMode>(105, 18);

            Add(graphics, new LocalizedText(9).Text, ResolutionDropDown);
            Add(graphics, new LocalizedText(10).Text,   l => New.Mode.ToString(),               Fullscreen_OnClick);
            Add(graphics, new LocalizedText(4147).Text, l => AntiAliasString(),                 AntiAliasing_OnClick);
            Add(graphics, new LocalizedText(4148).Text, l => QualityString(New.TextureQuality), TextureQuality_OnClick);
            Add(graphics, new LocalizedText(4149).Text, l => TextureFilterString(),             TextureFiltering_OnClick);
            Add(graphics, new LocalizedText(4150).Text, l => ShadowQualStr(New.ShadowDetail),   ShadowQuality_OnClick);
            Add(graphics, new LocalizedText(4151).Text, l => QualityString(New.EffectDetail),   EffectsQuality_OnClick);
            graphics.AddCheckbox(() => New.RenderBloom, 4145, 4146);

            graphics.ReverseZOrder(); // @todo This is a hacky workaround to zorder limitations
            graphics.ZOrder = 10;

            UIList botLeft = AddList(new Vector2(LeftArea.X, LeftArea.Y + 180), LeftArea.Size());
            botLeft.Padding = new Vector2(2f, 8f);
            botLeft.LayoutStyle = ListLayoutStyle.Clip;
            SoundDevices = new DropOptions<MMDevice>(180, 18);
            botLeft.AddSplit(new UILabel(new LocalizedText(4142).Text), SoundDevices);
            MusicVolumeSlider   = botLeft.Add(new FloatSlider(SliderStyle.Percent, 240f, 50f, new LocalizedText(4143).Text, 0f, 1f, GlobalStats.MusicVolume));
            EffectsVolumeSlider = botLeft.Add(new FloatSlider(SliderStyle.Percent, 240f, 50f, new LocalizedText(4144).Text, 0f, 1f, GlobalStats.EffectsVolume));

            botLeft.ReverseZOrder(); // @todo This is a hacky workaround to zorder limitations

            UIList botRight = AddList(new Vector2(RightArea.X, RightArea.Y + 180), RightArea.Size());
            botRight.Padding = new Vector2(2f, 8f);
            botRight.LayoutStyle = ListLayoutStyle.Clip;
            IconSize      = botRight.Add(new FloatSlider(SliderStyle.Decimal, 240f, 50f, new LocalizedText(4140).Text, 0,  30, GlobalStats.IconSize));
            AutoSaveFreq  = botRight.Add(new FloatSlider(SliderStyle.Decimal, 240f, 50f, new LocalizedText(4141).Text, 60, 540, GlobalStats.AutoSaveFreq));
            SimulationFps = botRight.Add(new FloatSlider(SliderStyle.Decimal, 240f, 50f, new LocalizedText(4138).Text, 10, 120, GlobalStats.SimulationFramesPerSecond));
            AutoSaveFreq.Tip = GameText.TheDelayBetweenAutoSaves;
            SimulationFps.Tip = new LocalizedText(4139).Text;

            UIList right = AddList(RightArea.PosVec(), RightArea.Size());
            right.Padding = new Vector2(2f, 4f);
            right.AddCheckbox(() => GlobalStats.PauseOnNotification,          title: 6007, tooltip: 7004);
            right.AddCheckbox(() => GlobalStats.AltArcControl,                title: 6184, tooltip: 7081);
            right.AddCheckbox(() => GlobalStats.ZoomTracking,                 title: 6185, tooltip: 7082);
            right.AddCheckbox(() => GlobalStats.AutoErrorReport,              title: 4130, tooltip: 4131);
            right.AddCheckbox(() => GlobalStats.DisableAsteroids,             title: 4132, tooltip: 4133);
            right.AddCheckbox(() => GlobalStats.NotifyEnemyInSystemAfterLoad, title: 4134, tooltip: 4135);
            right.AddCheckbox(() => GlobalStats.EnableSaveExportButton,       title: 4136, tooltip: 4137);

            CurrentLanguage = new DropOptions<Language>(105, 18);
            Add(right, new LocalizedText(4117).Text, CurrentLanguage);

            Add(new UIButton(new Vector2(RightArea.Right - 172, RightArea.Bottom + 60), Localizer.Token(13)))
                .OnClick = button => ApplyOptions();

            RefreshZOrder();
            PerformLayout();
            CreateResolutionDropOptions();
            CreateSoundDevicesDropOptions();
            CreateLanguageDropOptions();
        }

        void CreateResolutionDropOptions()
        {
            int screenWidth  = ScreenWidth;
            int screenHeight = ScreenHeight;

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

        void CreateSoundDevicesDropOptions()
        {
            MMDevice defaultDevice = AudioDevices.DefaultDevice;
            Array<MMDevice> devices = AudioDevices.Devices;

            SoundDevices.Clear();
            SoundDevices.AddOption("Default", null/*because it might change*/);

            foreach (MMDevice device in devices)
            {
                string isDefault = (device.ID == defaultDevice.ID) ? "* " : "";
                SoundDevices.AddOption($"{isDefault}{device.FriendlyName}", device);
                if (!AudioDevices.UserPrefersDefaultDevice && device.ID == AudioDevices.CurrentDevice.ID)
                    SoundDevices.ActiveIndex = devices.IndexOf(device) + 1;
            }

            SoundDevices.OnValueChange = OnAudioDeviceDropDownChange;
        }

        void CreateLanguageDropOptions()
        {
            foreach (Language language in (Language[]) Enum.GetValues(typeof(Language)))
            {
                CurrentLanguage.AddOption(language.ToString(), language);
            }
            CurrentLanguage.ActiveValue = GlobalStats.Language;
            CurrentLanguage.OnValueChange = OnLanguageDropDownChange;
        }

        void OnAudioDeviceDropDownChange(MMDevice newDevice)
        {
            if (newDevice == null)
                newDevice = AudioDevices.DefaultDevice;

            AudioDevices.SetUserPreference(newDevice);
            GameAudio.ReloadAfterDeviceChange(newDevice);

            GameAudio.SmallServo();
            GameAudio.TacticalPause();
        }

        void OnLanguageDropDownChange(Language newLanguage)
        {
            if (GlobalStats.Language != newLanguage)
            {
                GlobalStats.Language = newLanguage;
                ResourceManager.LoadLanguage(newLanguage);
                LoadContent(); // reload the options screen to update the text
            }
        }

        public override void LoadContent()
        {
            base.LoadContent();
            InitScreen();
        }

        void ApplyOptions()
        {
            try
            {
                New.Width  = ResolutionDropDown.ActiveValue.Width;
                New.Height = ResolutionDropDown.ActiveValue.Height;
                New.ApplyChanges();

                if (Original.Equals(New))
                {
                    AcceptChanges(); // auto-accept
                }
                else
                {
                    ScreenManager.AddScreen(new MessageBoxScreen(this, Localizer.Token(14), 10f)
                    {
                        Accepted = AcceptChanges,
                        Cancelled = CancelChanges
                    });
                }
            }
            catch
            {
                CancelChanges();
            }
        }

        void AcceptChanges()
        {
            Original = New.GetClone(); // accepted!
            GlobalStats.SaveSettings();

            EffectsVolumeSlider.RelativeValue = GlobalStats.EffectsVolume;
            MusicVolumeSlider.RelativeValue   = GlobalStats.MusicVolume;
        }

        void CancelChanges()
        {
            New = Original.GetClone(); // back to default!
            New.ApplyChanges();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (Fade) ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            base.Draw(batch, elapsed);
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
                GlobalStats.IconSize      = (int)IconSize.AbsoluteValue;
                GlobalStats.AutoSaveFreq  = (int)AutoSaveFreq.AbsoluteValue;
                GlobalStats.MusicVolume   = MusicVolumeSlider.RelativeValue;
                GlobalStats.EffectsVolume = EffectsVolumeSlider.RelativeValue;
                GlobalStats.SimulationFramesPerSecond = (int)SimulationFps.AbsoluteValue;
                GameAudio.ConfigureAudioSettings();
                return true;
            }
            return false;
        }
    }
}