using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Data;
using SynapseGaming.LightingSystem.Core;
using Point = Microsoft.Xna.Framework.Point;

namespace Ship_Game
{
    public class GameBase : Game
    {
        public GraphicsDeviceManager Graphics;
        LightingSystemPreferences Preferences;
        public static ScreenManager ScreenManager;

        // This is equivalent to PresentationParameters.BackBufferWidth
        public static int ScreenWidth  { get; protected set; }
        public static int ScreenHeight { get; protected set; }
        public static Viewport Viewport;
        public static Vector2 ScreenSize   { get; protected set; }
        public static Vector2 ScreenCenter { get; protected set; }
        public static int MainThreadId { get; protected set; }

        public static GameBase Base;
        public new GameContentManager Content { get; }
        public static GameContentManager GameContent => Base?.Content;

        public Form Form => (Form)Control.FromHandle(Window.Handle);

        public GameBase()
        {
            MainThreadId = Thread.CurrentThread.ManagedThreadId;
            Base = this;

            string contentDir = Path.Combine(Directory.GetCurrentDirectory(), "Content");
            base.Content = Content = new GameContentManager(Services, "Game", contentDir);

            Graphics = new GraphicsDeviceManager(this)
            {
                MinimumPixelShaderProfile = ShaderProfile.PS_2_0,
                MinimumVertexShaderProfile = ShaderProfile.VS_2_0,
                PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8,
                PreferMultiSampling = false
            };

            Graphics.PreparingDeviceSettings += PrepareDeviceSettings;
        }

        static void PrepareDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            GraphicsAdapter a = e.GraphicsDeviceInformation.Adapter;
            PresentationParameters p = e.GraphicsDeviceInformation.PresentationParameters;

            var samples = (MultiSampleType)GlobalStats.AntiAlias;
            if (a.CheckDeviceMultiSampleType(DeviceType.Hardware, a.CurrentDisplayMode.Format,
                false, samples, out int quality))
            {
                p.MultiSampleQuality = (quality == 1 ? 0 : 1);
                p.MultiSampleType    = samples;
            }
            else
            {
                p.MultiSampleType    = MultiSampleType.None;
                p.MultiSampleQuality = 0;
            }

            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PlatformContents;
        }

        protected void UpdateRendererPreferences(ref GraphicsSettings settings)
        {
            var p = new LightingSystemPreferences
            {
                ShadowQuality   = settings.ShadowQuality,
                MaxAnisotropy   = settings.MaxAnisotropy,
                ShadowDetail    = (DetailPreference) settings.ShadowDetail,
                EffectDetail    = (DetailPreference) settings.EffectDetail,
                TextureQuality  = (DetailPreference) settings.TextureQuality,
                TextureSampling = (SamplingPreference) settings.TextureSampling,
                PostProcessingDetail = DetailPreference.High,
            };

            if (Preferences != null && Preferences.Equals(p))
                return; // nothing changed.

            Log.Write(ConsoleColor.Magenta, "Apply 3D Graphics Preferences:");
            Log.Write(ConsoleColor.Magenta, $"  ShadowQuality:   {p.ShadowQuality}");
            Log.Write(ConsoleColor.Magenta, $"  ShadowDetail:    {p.ShadowDetail}");
            Log.Write(ConsoleColor.Magenta, $"  EffectDetail:    {p.EffectDetail}");
            Log.Write(ConsoleColor.Magenta, $"  TextureQuality:  {p.TextureQuality}");
            Log.Write(ConsoleColor.Magenta, $"  TextureSampling: {p.TextureSampling}");
            Log.Write(ConsoleColor.Magenta, $"  MaxAnisotropy:   {p.MaxAnisotropy}");

            Preferences = p;
            ScreenManager?.UpdatePreferences(p);
        }

        bool ApplySettings(ref GraphicsSettings settings)
        {
            GraphicsDevice before = Graphics.GraphicsDevice;
            Graphics.ApplyChanges();
            bool deviceChanged = before != Graphics.GraphicsDevice;

            PresentationParameters p = GraphicsDevice.PresentationParameters;
            ScreenWidth  = p.BackBufferWidth;
            ScreenHeight = p.BackBufferHeight;
            ScreenSize   = new Vector2(ScreenWidth, ScreenHeight);
            ScreenCenter = ScreenSize * 0.5f;
            Viewport     = GraphicsDevice.Viewport;

            UpdateRendererPreferences(ref settings);
            ScreenManager?.UpdateViewports();
            return deviceChanged;
        }

        // @return TRUE if graphics device changed
        public bool ApplyGraphics(GraphicsSettings settings)
        {
            DisplayMode currentMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

            // check if resolution from graphics settings is ok:
            if (currentMode.Width < settings.Width || currentMode.Height < settings.Height)
            {
                settings.Width  = currentMode.Width;
                settings.Height = currentMode.Height;
            }

            if (settings.Width <= 0 || settings.Height <= 0)
            {
                settings.Width  = 800;
                settings.Height = 600;
            }
            var form = (Form)Control.FromHandle(Window.Handle);
            if (Debugger.IsAttached && settings.Mode == WindowMode.Fullscreen)
                settings.Mode = WindowMode.Borderless;

            Graphics.PreferredBackBufferWidth = settings.Width;
            Graphics.PreferredBackBufferHeight = settings.Height;
            Graphics.SynchronizeWithVerticalRetrace = settings.VSync;

            switch (settings.Mode)
            {
                case WindowMode.Windowed:   form.FormBorderStyle = FormBorderStyle.Fixed3D; break;
                case WindowMode.Borderless: form.FormBorderStyle = FormBorderStyle.None;    break;
            }

            if (settings.Mode != WindowMode.Fullscreen && Graphics.IsFullScreen ||
                settings.Mode == WindowMode.Fullscreen && !Graphics.IsFullScreen)
            {
                Graphics.ToggleFullScreen();
            }

            bool deviceChanged = ApplySettings(ref settings);

            if (settings.Mode != WindowMode.Fullscreen) // set to screen center
            {
                form.WindowState = FormWindowState.Normal;
                form.ClientSize = new Size(settings.Width, settings.Height);

                // set form to the center of the primary screen
                Size size = Screen.PrimaryScreen.Bounds.Size;
                form.Location = new System.Drawing.Point(
                    size.Width / 2 - settings.Width / 2,
                    size.Height / 2 - settings.Height / 2);
            }

            return deviceChanged;
        }

        public void InitializeAudio()
        {
            GameAudio.Initialize(null, "Content/Audio/ShipGameProject.xgs", "Content/Audio/Wave Bank.xwb", "Content/Audio/Sound Bank.xsb");
        }

        protected override void Dispose(bool disposing)
        {
            GameAudio.Destroy();
        }
    }
}