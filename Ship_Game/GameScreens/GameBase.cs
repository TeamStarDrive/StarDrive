using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Data;
using Ship_Game.Graphics;
using SynapseGaming.LightingSystem.Core;
using Vector2 = SDGraphics.Vector2;
using XnaColor = Microsoft.Xna.Framework.Color;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;

namespace Ship_Game
{
    public class GameBase : Game
    {
        #pragma warning disable CA2213 // managed by Game
        public GraphicsDeviceManager Graphics;
        #pragma warning restore CA2213
        LightingSystemPreferences Preferences;
        public static ScreenManager ScreenManager;
        public ScreenManager Manager => ScreenManager;

        // the size the game draws at: the backbuffer's, or ScreenTarget's while fullscreen stretches a lower resolution
        public static int ScreenWidth  { get; protected set; }
        public static int ScreenHeight { get; protected set; }
        public static Viewport Viewport;
        public static Vector2 ScreenSize   { get; protected set; }
        public static Vector2 ScreenCenter { get; protected set; }
        public static int MainThreadId { get; protected set; }

        public static GameBase Base;
        public new GameContentManager Content { get; }
        public static GameContentManager GameContent => Base?.Content;

        public int FrameId { get; protected set; }
        public UpdateTimes Elapsed { get; protected set; }

        /// <summary>
        /// Total elapsed Game time while the Game window has been active
        /// </summary>
        public float TotalElapsed { get; protected set; }

        public Form Form => (Form)Control.FromHandle(Window.Handle);

        /// <summary>
        /// TRUE if GraphicsDevice is not null or disposed
        /// </summary>
        public bool IsDeviceGood => GraphicsDevice is { IsDisposed: false, GraphicsDeviceStatus: GraphicsDeviceStatus.Normal };

        public GameBase()
        {
            MainThreadId = Thread.CurrentThread.ManagedThreadId;
            Base = this;

            string contentDir = Path.Combine(Directory.GetCurrentDirectory(), "Content");
            base.Content = Content = new GameContentManager(Services, "Game", contentDir);

            Graphics = new GraphicsDeviceManager(this)
            {
                PreferredDepthStencilFormat = DepthFormat.Depth16, // only supported: Depth24Stencil8,
            };
            Graphics.PreferMultiSampling = true;
            Graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Graphics.HardwareModeSwitch = false; // fullscreen is a borderless window covering the display
            Graphics.ApplyChanges();
        }

        // the game's screen while fullscreen shows a lower resolution than the display's, stretched over the display each frame
        public static RenderTarget2D ScreenTarget { get; private set; }
        static SpriteBatch ScreenTargetBatch;

        // sends drawing back to the game's screen after a pass into another render target
        public static void DrawToScreen(GraphicsDevice device) => device.SetRenderTarget(ScreenTarget);

        // where a screen of this size lands in an area: as large as fits without changing its shape, centered
        internal static XnaRectangle FitScreen(int areaWidth, int areaHeight, int screenWidth, int screenHeight)
        {
            float scale = Math.Min(areaWidth / (float)screenWidth, areaHeight / (float)screenHeight);
            int w = (int)Math.Round(screenWidth * scale);
            int h = (int)Math.Round(screenHeight * scale);
            return new XnaRectangle((areaWidth - w) / 2, (areaHeight - h) / 2, w, h);
        }

        // a window pixel as a pixel of a screen drawn into r; both directions map pixel centers so a round trip returns the same pixel
        internal static (int X, int Y) WindowToScreen(int x, int y, in XnaRectangle r, int screenWidth, int screenHeight)
        {
            int sx = (int)Math.Floor((x - r.X + 0.5f) * screenWidth / r.Width);
            int sy = (int)Math.Floor((y - r.Y + 0.5f) * screenHeight / r.Height);
            return (Math.Clamp(sx, 0, screenWidth - 1), Math.Clamp(sy, 0, screenHeight - 1));
        }

        internal static (int X, int Y) ScreenToWindow(int x, int y, in XnaRectangle r, int screenWidth, int screenHeight)
        {
            return (r.X + (int)Math.Floor((x + 0.5f) * r.Width / screenWidth),
                    r.Y + (int)Math.Floor((y + 0.5f) * r.Height / screenHeight));
        }

        static bool TryGetScreenTargetInWindow(out XnaRectangle bounds)
        {
            bounds = default;
            if (ScreenTarget == null || Base?.Window == null)
                return false;
            XnaRectangle client = Base.Window.ClientBounds;
            if (client.Width <= 0 || client.Height <= 0)
                return false;
            bounds = FitScreen(client.Width, client.Height, ScreenWidth, ScreenHeight);
            return true;
        }

        public static (int X, int Y) WindowToScreen(int x, int y)
        {
            return TryGetScreenTargetInWindow(out XnaRectangle r) ? WindowToScreen(x, y, r, ScreenWidth, ScreenHeight) : (x, y);
        }

        public static (int X, int Y) ScreenToWindow(int x, int y)
        {
            return TryGetScreenTargetInWindow(out XnaRectangle r) ? ScreenToWindow(x, y, r, ScreenWidth, ScreenHeight) : (x, y);
        }

        // the backbuffer covers the display in fullscreen, so a lower resolution gets its own render target
        void UpdateScreenTarget(GraphicsSettings settings, PresentationParameters p)
        {
            if (ScreenTarget != null)
            {
                GraphicsDevice.SetRenderTarget(null);
                ScreenTarget.Dispose();
                ScreenTarget = null;
            }
            if (settings.Mode != WindowMode.Fullscreen
                || settings.Width == p.BackBufferWidth && settings.Height == p.BackBufferHeight)
                return;

            ScreenTarget = RenderTargets.Create(GraphicsDevice, settings.Width, settings.Height, p.RenderTargetUsage);
            if (ScreenTargetBatch == null || ScreenTargetBatch.GraphicsDevice != GraphicsDevice)
            {
                ScreenTargetBatch?.Dispose();
                ScreenTargetBatch = new SpriteBatch(GraphicsDevice);
            }
        }

        // draws the game's screen over the display at the end of a frame, with black bars where the shapes differ
        protected void StretchScreenTarget()
        {
            if (ScreenTarget == null)
                return;
            GraphicsDevice device = GraphicsDevice;
            device.SetRenderTarget(null);
            device.Clear(XnaColor.Black);
            PresentationParameters p = device.PresentationParameters;
            ScreenTargetBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
                                    DepthStencilState.None, RasterizerState.CullNone);
            ScreenTargetBatch.Draw(ScreenTarget, FitScreen(p.BackBufferWidth, p.BackBufferHeight, ScreenWidth, ScreenHeight), XnaColor.White);
            ScreenTargetBatch.End();
            // the next frame starts with the blending and culling a default sprite batch leaves
            device.BlendState = BlendState.AlphaBlend;
            device.RasterizerState = RasterizerState.CullCounterClockwise;
        }

        void UpdateRendererPreferences(ref GraphicsSettings settings)
        {
            var p = new LightingSystemPreferences
            {
                MaxAnisotropy   = settings.MaxAnisotropy,
                ShadowQuality   = GlobalStats.GetShadowQuality(settings.ShadowDetail),
                ShadowDetail    = (DetailPreference) settings.ShadowDetail,
                EffectDetail    = (DetailPreference) settings.EffectDetail,
                TextureQuality  = (DetailPreference) settings.TextureQuality,
                TextureSampling = (SamplingPreference) settings.TextureSampling,
                PostProcessingDetail = DetailPreference.High,
            };

            if (Preferences != null && Preferences.Equals(p))
                return; // nothing changed.

            if (StarDriveGame.Instance != null)
            {
                Log.Write(ConsoleColor.Magenta, "Apply 3D Graphics Preferences:");
                Log.Write(ConsoleColor.Magenta, $"  Resolution:      {settings.Width}x{settings.Height} Fullscreen:{Graphics.IsFullScreen}");
                Log.Write(ConsoleColor.Magenta, $"  ShadowQuality:   {p.ShadowQuality}");
                Log.Write(ConsoleColor.Magenta, $"  ShadowDetail:    {p.ShadowDetail}");
                Log.Write(ConsoleColor.Magenta, $"  EffectDetail:    {p.EffectDetail}");
                Log.Write(ConsoleColor.Magenta, $"  TextureQuality:  {p.TextureQuality}");
                Log.Write(ConsoleColor.Magenta, $"  TextureSampling: {p.TextureSampling}");
                Log.Write(ConsoleColor.Magenta, $"  MaxAnisotropy:   {p.MaxAnisotropy}");
            }

            Preferences = p;
            ScreenManager?.UpdatePreferences(p);
        }

        bool ApplySettings(ref GraphicsSettings settings)
        {
            GraphicsDevice before = Graphics.GraphicsDevice;
            Graphics.ApplyChanges();
            bool deviceChanged = before != Graphics.GraphicsDevice;

            PresentationParameters p = GraphicsDevice.PresentationParameters;
            UpdateScreenTarget(settings, p);
            ScreenWidth  = ScreenTarget?.Width  ?? p.BackBufferWidth;
            ScreenHeight = ScreenTarget?.Height ?? p.BackBufferHeight;
            ScreenSize   = new Vector2(ScreenWidth, ScreenHeight);
            ScreenCenter = ScreenSize * 0.5f;
            Viewport     = ScreenTarget != null ? new Viewport(0, 0, ScreenWidth, ScreenHeight) : GraphicsDevice.Viewport;

            UpdateRendererPreferences(ref settings);
            ScreenManager?.UpdateViewports();
            return deviceChanged;
        }

        // the last settings ApplyGraphics applied and the backbuffer size they left, which OnActivated restores if the backbuffer drifts
        GraphicsSettings LastAppliedSettings;
        int AppliedBackBufferWidth, AppliedBackBufferHeight;
        bool Restoring; // re-entrancy guard for OnActivated → ApplyGraphics → ApplyChanges

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

            // the border is set before the backbuffer size: changing it fires a resize, and MonoGame then takes the form's size as the backbuffer's
            switch (settings.Mode)
            {
                case WindowMode.Windowed:   form.FormBorderStyle = FormBorderStyle.Fixed3D; break;
                case WindowMode.Borderless: form.FormBorderStyle = FormBorderStyle.None;    break;
                case WindowMode.Fullscreen: form.FormBorderStyle = FormBorderStyle.None;    break;
            }

            // fullscreen is a borderless window covering the display, so its backbuffer is the display's size
            bool fullscreen = settings.Mode == WindowMode.Fullscreen;
            Graphics.PreferredBackBufferWidth  = fullscreen ? currentMode.Width  : settings.Width;
            Graphics.PreferredBackBufferHeight = fullscreen ? currentMode.Height : settings.Height;
            Graphics.SynchronizeWithVerticalRetrace = settings.VSync;

            if (settings.Mode != WindowMode.Fullscreen && Graphics.IsFullScreen)
            {
                Graphics.ToggleFullScreen(); // Exiting fullscreen — always safe
            }
            else if (settings.Mode == WindowMode.Fullscreen && !Graphics.IsFullScreen)
            {
                // with no hardware mode switch DXGI is not asked for exclusive fullscreen, so this fallback is only a guard
                if (!TryEnterFullScreen(maxAttempts: 2, retryDelayMs: 500))
                {
                    settings.Mode = WindowMode.Borderless;
                    // FormBorderStyle is already None from the Fullscreen case above,
                    // which matches what Borderless wants — no border-restyle needed.
                    // The "if (settings.Mode != WindowMode.Fullscreen)" block below
                    // will now run and size/center the form correctly.
                }
            }

            // Phase 2.2: in MonoGame WindowsDX 3.8 the WinForms platform binds the
            // backbuffer size to the form's ClientSize via a SizeChanged handler. If we
            // call ApplyChanges() BEFORE the form has been resized, the backbuffer stays
            // at MonoGame's 800x480 default (the SizeChanged event hasn't fired yet to
            // override the preferred values). Result: a tiny 800x480 backbuffer
            // presented in the corner of the oversized form. Resize the form FIRST,
            // then call ApplySettings so the backbuffer tracks the form's ClientSize.
            if (settings.Mode != WindowMode.Fullscreen)
            {
                form.WindowState = FormWindowState.Normal;
                form.ClientSize = new Size(settings.Width, settings.Height);

                // set form to the center of the primary screen
                var bounds = Screen.PrimaryScreen.Bounds;
                Size size = bounds.Size;
                var pt = new System.Drawing.Point(
                    size.Width / 2 - settings.Width / 2,
                    size.Height / 2 - settings.Height / 2);

                // but also make sure that we stay inside the screen, otherwise XNA mouse cursor
                // position reporting goes crazy
                if (pt.X < bounds.Left) pt.X = bounds.Left;
                if (pt.Y < bounds.Top) pt.Y = bounds.Top;
                form.Location = pt;
            }

            bool deviceChanged = ApplySettings(ref settings);

            PresentationParameters pp = GraphicsDevice.PresentationParameters;
            Log.Write(ConsoleColor.Cyan, $"ApplyGraphics: backbuffer={pp.BackBufferWidth}x{pp.BackBufferHeight} form={form.ClientSize.Width}x{form.ClientSize.Height} screen={ScreenWidth}x{ScreenHeight}");

            LastAppliedSettings = settings;
            AppliedBackBufferWidth = pp.BackBufferWidth;
            AppliedBackBufferHeight = pp.BackBufferHeight;
            return deviceChanged;
        }

        // if the backbuffer comes back from a deactivation at another size than ApplyGraphics left, the last settings are applied again
        protected override void OnActivated(object sender, EventArgs args)
        {
            base.OnActivated(sender, args);

            if (Restoring) return;
            if (LastAppliedSettings == null || Graphics == null || GraphicsDevice == null)
                return;
            if (LastAppliedSettings.Mode != WindowMode.Fullscreen)
                return;

            PresentationParameters p = GraphicsDevice.PresentationParameters;
            if (p.BackBufferWidth == AppliedBackBufferWidth
                && p.BackBufferHeight == AppliedBackBufferHeight)
                return;

            Log.Warning($"Alt-tab restore: backbuffer drifted to {p.BackBufferWidth}x{p.BackBufferHeight}, restoring to {AppliedBackBufferWidth}x{AppliedBackBufferHeight}");
            Restoring = true;
            try { ApplyGraphics(LastAppliedSettings); }
            finally { Restoring = false; }
        }

        // DXGI_ERROR_NOT_CURRENTLY_AVAILABLE — SetFullscreenState rejected the
        // transition. Catching by HRESULT avoids a dependency on SharpDX's
        // exception type here in GameBase; any wrapper that surfaces this code
        // gets handled the same way.
        const int DXGI_ERROR_NOT_CURRENTLY_AVAILABLE = unchecked((int)0x887A0022);

        // enters fullscreen, retrying if DXGI rejects it, and false after the last attempt with IsFullScreen reset
        bool TryEnterFullScreen(int maxAttempts, int retryDelayMs)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    Graphics.IsFullScreen = true;
                    Graphics.ApplyChanges();
                    return true;
                }
                catch (Exception ex) when (ex.HResult == DXGI_ERROR_NOT_CURRENTLY_AVAILABLE)
                {
                    // Reset the flag so a stale "true" doesn't leak into the
                    // Borderless fallback path or the next retry.
                    Graphics.IsFullScreen = false;
                    if (attempt < maxAttempts)
                    {
                        Log.Warning($"DXGI rejected fullscreen (attempt {attempt}/{maxAttempts}); retrying in {retryDelayMs}ms: {ex.Message}");
                        Thread.Sleep(retryDelayMs);
                    }
                    else
                    {
                        Log.Warning($"DXGI rejected fullscreen after {maxAttempts} attempts; falling back to Borderless: {ex.Message}");
                    }
                }
            }
            return false;
        }

        public void InitializeAudio()
        {
            GameAudio.Initialize(null, "Audio/AudioConfig.yaml");
        }

        protected void UpdateGame(GameTime gameTime)
        {
            if (Log.IsTerminating) // game is crashing, don't update anymore
            {
                Thread.Sleep(15);
                return;
            }

            try
            {
                ++FrameId;
                TotalElapsed = (float)gameTime.TotalGameTime.TotalSeconds;
                float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
                Elapsed = new UpdateTimes(deltaTime, TotalElapsed);

                if (IsDeviceGood) // only Update if device is OK
                {
                    // 1. Handle Input and 2. Update for each game screen
                    ScreenManager.Update(Elapsed);
                }

                base.Update(gameTime); // MonoGame Update
            }
            catch (Exception ex)
            {
                Log.ErrorDialog(ex, "UpdateGame() failed", Program.SCREEN_UPDATE_FAILURE);
            }
        }

        protected override void Dispose(bool disposing)
        {
            GameAudio.Destroy();
            if (ScreenManager != null)
                ResourceManager.UnloadAllData(ScreenManager);
            Mem.Dispose(ref ScreenManager);

            base.Dispose(disposing); // disposes Graphics
        }
    }
}