using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Audio;
using Ship_Game.Data;
using Ship_Game.GameScreens;
using Ship_Game.Graphics;
using Ship_Game.UI;
using Ship_Game.Utils;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;

using Matrix = SDGraphics.Matrix;
using Rectangle = SDGraphics.Rectangle;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using Vector2d = SDGraphics.Vector2d;
using Vector3d = SDGraphics.Vector3d;
using SDUtils;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public abstract class GameScreen : MultiLayerDrawContainer, IDisposable
    {
        public InputState Input;
        bool OtherScreenHasFocus;

        public float SlowFlashTimer { get; private set; } = 1;
        public float NormalFlashTimer { get; private set; } =1;
        public float FastFlashTimer { get; private set; } = 1;
        
        /// <summary>
        /// New Screen active event flag.
        /// This is a different simpler to understand flag than the legacy IsActive flag.
        ///
        /// Once a screen becomes visible, the BecameActive() event is triggered and IsScreenActive is set to true.
        /// Once an active screen becomes invisible, the BecameInActive() event triggers and IsScreenActive is set to false.
        /// </summary>
        public bool IsScreenActive { get; private set; }

        public bool IsActive => Enabled && !IsExiting && (!OtherScreenHasFocus || (GlobalStats.RestrictAIPlayerInteraction || System.Diagnostics.Debugger.IsAttached )) && 
            (ScreenState == ScreenState.TransitionOn || ScreenState == ScreenState.Active);

        public bool IsExiting { get; protected set; }

        // Popup screens can be dismissed via RightMouseClick
        public bool IsPopup { get; protected set; }

        // If TRUE, ESC key will close this screen
        public bool CanEscapeFromScreen { get; protected set; } = true;
        
        // LEGACY LAYOUT: Change layout and Font Size if ScreenWidth is too small
        public readonly bool LowRes;
        public readonly bool HiRes;

        // @return TRUE if content was loaded this frame
        public bool DidLoadContent { get; private set; }

        // TRUE if Update() has been run at least once on this GameScreen
        public bool DidRunUpdate { get; private set; }

        public Viewport Viewport { get; private set; }
        public ScreenManager ScreenManager { get; internal set; }
        public GraphicsDevice Device => ScreenManager.GraphicsDevice;
        public ScreenState ScreenState  { get; protected set; }
        public float TransitionOffTime  { get; protected set; }
        public float TransitionOnTime   { get; protected set; }
        public float TransitionPosition { get; protected set; } = 1f;

        public bool IsTransitioning => ScreenState == ScreenState.TransitionOn
                                    || ScreenState == ScreenState.TransitionOff;

        public byte TransitionAlpha => (byte)(255f - TransitionPosition * 255f);

        // This is equivalent to PresentationParameters.BackBufferWidth
        public int ScreenWidth      => GameBase.ScreenWidth;
        public int ScreenHeight     => GameBase.ScreenHeight;
        public Vector2 MousePos     => Input.CursorPosition;
        public Vector2 ScreenArea   => GameBase.ScreenSize;
        public Vector2 ScreenCenter => GameBase.ScreenCenter;

        // event called right after the screen has been loaded
        public Action OnLoaded;

        // multi cast exit delegate, called when a game screen is exiting
        public event Action OnExit;

        public bool IsDisposed { get; private set; }

        // This should be used for content that gets unloaded once this GameScreen disappears
        public GameContentManager TransientContent;

        public Matrix View = Matrix.Identity; // @see SetViewMatrix
        public Matrix Projection; // @see SetPerspectiveProjection
        public Matrix ViewProjection; // View * Projection
        public Matrix InverseViewProjection; // Inverse(View * Projection)
        public Matrix OrthographicProjection; // for drawing the UI

        // deferred renderer allows some basic commands to be queued up to be drawn. 
        // this is useful when wanted to draw from handle input routines and other areas. 
        public DeferredRenderer Renderer { get; }

        // Thread safe queue for running UI commands
        readonly SafeQueue<Action> PendingActions = new();

        // If this is set, the universe was paused
        UniverseScreen PausedUniverse;

        /// <summary>Game screen that is the same size as the current screen/window</summary>
        /// <param name="parent">Parent to this screen, or null</param>
        /// <param name="toPause">If not null, pauses the universe simulation until this screen finishes</param>
        protected GameScreen(GameScreen parent, UniverseScreen toPause)
            : this(parent, new Rectangle(0, 0, GameBase.ScreenWidth, GameBase.ScreenHeight), toPause)
        {
        }

        /// <summary>Game screen with a specific size</summary>
        /// <param name="parent">Parent to this screen, or null</param>
        /// <param name="rect">Initial container rect for this screen</param>
        /// <param name="toPause">If not null, pauses the universe simulation until this screen finishes</param>
        protected GameScreen(GameScreen parent, in Rectangle rect, UniverseScreen toPause) : base(rect)
        {
            // hook the content chain to parent screen if possible
            TransientContent = new GameContentManager(parent?.TransientContent ?? GameBase.Base.Content, GetType().Name);
            ScreenManager = parent?.ScreenManager ?? GameBase.ScreenManager;
            UpdateViewport();

            // if we have `toPause`, check if it's active and not already paused
            // this way only a single pausing screen will be allowed to resume the simulation automatically
            if (toPause != null && toPause.IsActive && !toPause.UState.Paused)
            {
                toPause.UState.Paused = true;
                PausedUniverse = toPause;
            }

            Input ??= parent?.Input ?? ScreenManager?.input;

            // Every time we open a screen, we should release any input handlers
            GlobalStats.TakingInput = false;

            LowRes = ScreenWidth <= 1366 || ScreenHeight <= 720;
            HiRes  = ScreenWidth > 1920 || ScreenHeight > 1400;

            Func<int> simTurnSource = null;
            if (parent is UniverseScreen us)
            {
                simTurnSource = () => us.SimTurnId;
            }
            Renderer = new DeferredRenderer(this, simTurnSource);
        }

        ~GameScreen() { Dispose(false); }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            RemoveAll();
            Mem.Dispose(ref TransientContent);
            PendingActions.Dispose();
        }

        // select size based on current res: Low, Normal, Hi
        protected int SelectSize(int lowRes, int normalRes, int hiRes)
        {
            if (LowRes) return lowRes;
            if (HiRes) return hiRes;
            return normalRes;
        }

        public void UpdateViewport() => Viewport = GameBase.Viewport;

        // Is it possible to add another dynamic light source?
        // Returns false if dynamic lights are disabled, or max dynamic lights already in scene
        public bool CanAddDynamicLight => GlobalStats.MaxDynamicLightSources > ScreenManager.ActiveDynamicLights;

        public void AddObject(ISceneObject so)    => ScreenManager.AddObject(so);
        public void RemoveObject(ISceneObject so) => ScreenManager.RemoveObject(so);
        public void AddLight(ILight light, bool dynamic) => ScreenManager.AddLight(light, dynamic);
        public void RemoveLight(ILight light, bool dynamic) => ScreenManager.RemoveLight(light, dynamic);

        public void AssignLightRig(LightRigIdentity identity, string rigContentPath)
        {
            var lightRig = TransientContent.Load<LightRig>(rigContentPath);
            ScreenManager.AssignLightRig(identity, lightRig);
        }

        // ExitScreen will also call this.Dispose(true)
        public virtual void ExitScreen()
        {
            IsExiting = true;

            if (PausedUniverse != null)
            {
                PausedUniverse.UState.Paused = false;
                PausedUniverse = null;
            }

            PendingActions.Clear();

            // if we got any tooltips, clear them now
            ToolTip.Clear();

            // call the exit event only once
            if (OnExit != null)
            {
                OnExit();
                OnExit = null;
            }

            // Every time we close a screen, make sure to Release input capture
            GlobalStats.TakingInput = false;

            if (TransitionOffTime.NotZero())
                return;

            ScreenManager.RemoveScreen(this);
        }

        // Calls ExitScreen and then always forcefully removes the screen
        public void ForceExit()
        {
            ExitScreen();
            if (ScreenManager.Screens.Contains(this))
                ScreenManager.RemoveScreen(this);
        }

        public void OnScreenRemoved()
        {
            Enabled = Visible = false;
            ScreenState = ScreenState.Hidden;
        }

        /// <summary>
        /// Called when this Screen has become visible.
        /// 1) first time this screen is shown
        /// 2) when another game screen which covered this one is removed
        /// </summary>
        public virtual void BecameActive()
        {
        }

        public void OnBecomeActive()
        {
            IsScreenActive = true;
            Log.Write($"BecameActive: {GetType().GetTypeName()}");
            BecameActive();
        }

        /// <summary>
        /// Called when this Screen is being covered by another screen,
        /// or when this screen is finally removed.
        /// This is not the same as OnExit which is for delayed exits
        /// </summary>
        public virtual void BecameInActive()
        {
        }

        public void OnBecomeInActive()
        {
            IsScreenActive = false;
            Log.Write($"BecameInActive: {GetType().GetTypeName()}");
            BecameInActive();
        }

        // NOTE: Optionally implemented by GameScreens to create their screen content
        //       This is also called when the screen is being reloaded
        public virtual void LoadContent() { }

        // Wrapper: should only be called by ScreenManager
        public void InvokeLoadContent()
        {
            LoadContent();
            DidLoadContent = true;
            PerformLayout();
            OnLoaded?.Invoke();
        }

        public virtual void UnloadContent()
        {
            if (IsScreenActive)
                OnBecomeInActive(); // forcefully become InActive during UnloadContent()

            TransientContent?.Unload();
            RemoveAll(); // using RemoveAll() here to ensure all necessary events are triggered, instead of Elements.Clear()
            DidLoadContent = false;
        }

        public virtual void ReloadContent()
        {
            UnloadContent();
            InvokeLoadContent();
        }

        public override bool HandleInput(InputState input)
        {
            if (!Visible || !Enabled || !IsActive)
                return false;

            // First allow other UI elements to capture input
            if (base.HandleInput(input))
                return true;

            // only then check for ExitScreen condition
            if (CanEscapeFromScreen && input.Escaped ||
                CanEscapeFromScreen && IsPopup && input.RightMouseClick)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }
            return false;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (IsDisposed)
                return;
            Renderer.Draw(batch);
            base.Draw(batch, elapsed);
        }

        public bool PreUpdate(UpdateTimes elapsed, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            if (IsDisposed)
                return false; // don't update

            // Process Pending Actions
            InvokePendingActions();

            OtherScreenHasFocus = otherScreenHasFocus;

            if (IsExiting)
            {
                ScreenState = ScreenState.TransitionOff;
                if (!UpdateTransition(elapsed, TransitionOffTime, 1))
                {
                    ScreenManager.RemoveScreen(this);
                }
            }
            else
            {
                if (coveredByOtherScreen) // if covered by another screen, there is no transition!
                {
                    ScreenState = ScreenState.Hidden;
                }
                else // not fully covered?
                {
                    ScreenState = UpdateTransition(elapsed, TransitionOnTime, -1)
                        ? ScreenState.TransitionOn
                        : ScreenState.Active;
                }
            }

            Visible = ScreenState != ScreenState.Hidden;

            SlowFlashTimer   -= elapsed.RealTime.Seconds / 4;
            NormalFlashTimer -= elapsed.RealTime.Seconds;
            FastFlashTimer   -= elapsed.RealTime.Seconds * 2;

            FastFlashTimer   = FastFlashTimer < elapsed.RealTime.Seconds ? 1 : FastFlashTimer;
            NormalFlashTimer = NormalFlashTimer < elapsed.RealTime.Seconds ? 1 : NormalFlashTimer;
            SlowFlashTimer   = SlowFlashTimer < elapsed.RealTime.Seconds ? 1 : SlowFlashTimer;
            return true; // all good
        }

        public void Update(UpdateTimes elapsed, bool isTopMost)
        {
            if (isTopMost) // show the cursor of the topmost visible screen
            {
                GameCursors.SetCurrentCursor(GetCurrentCursor());
            }

            DidRunUpdate = true;
            Update(elapsed.RealTime.Seconds); // Update UIElementV2
        }

        // TODO: This is deprecated by UIBasicAnimEffect system
        bool UpdateTransition(UpdateTimes elapsed, float transitionTime, int direction)
        {
            float transitionDelta = (transitionTime.NotZero()
                                  ? (elapsed.RealTime.Seconds / transitionTime) : 1f);

            TransitionPosition += transitionDelta * direction;
            if (TransitionPosition > 0f && TransitionPosition < 1f)
                return true;

            TransitionPosition = TransitionPosition.Clamped(0, 1);
            return false;
        }

        // Gets the current cursor blinking mask color [255,255,255,a]
        public Color CurrentFlashColor => ApplyCurrentAlphaToColor(new Color(255, 255, 255));
        public Color CurrentFlashColorRed => ApplyCurrentAlphaToColor(new Color(255, 0, 0));

        public Color ApplyCurrentAlphaToColor(Color color)
        {
            float f = Math.Abs(RadMath.Sin(GameBase.Base.TotalElapsed)) * 255f;
            return new Color(color, (byte)f);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void DrawMultiLayeredExperimental(ScreenManager manager, SpriteBatch batch,
                                                 DrawTimes elapsed, bool draw3D = false)
        {
            if (!Visible)
                return;
            DrawMulti(manager, batch, elapsed, this, draw3D, ref View, ref Projection);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // just draws a line, no fancy reprojections
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawLine(Vector2 screenPoint1, Vector2 screenPoint2, Color color, float thickness = 1f)
            => ScreenManager.SpriteBatch.DrawLine(screenPoint1, screenPoint2, color, thickness);

        // just draws a line, no fancy reprojections
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawLine(Vector2d screenPoint1, Vector2d screenPoint2, Color color, float thickness = 1f)
            => ScreenManager.SpriteBatch.DrawLine(screenPoint1, screenPoint2, color, thickness);

        // Draw a CrossHair on screen
        public void DrawCrossHair(in Vector2d center, double size, Color color, float thickness = 1f)
            => ScreenManager.SpriteBatch.DrawCrossHair(center, size, color, thickness);

        // just draws a circle, no fancy reprojections
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawCircle(Vector2 posOnScreen, float radius, Color color, float thickness = 1f)
            => ScreenManager.SpriteBatch.DrawCircle(posOnScreen, radius, color, thickness);
        
        // just draws a circle, no fancy reprojections
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawCircle(Vector2d posOnScreen, double radius, Color color, float thickness = 1f)
        {
            ScreenManager.SpriteBatch.DrawCircle(posOnScreen, radius, color, thickness);
        }

        // Just draws a given rectangle with a color fill
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawRectangle(in Rectangle rectangle, Color edgeColor, Color fillColor, float thickness = 1f)
        {
            ScreenManager.SpriteBatch.FillRectangle(rectangle, fillColor);
            DrawRectangle(rectangle, edgeColor, thickness);
        }

        // Just draws a given rectangle
        public void DrawRectangle(in Rectangle rectangle, Color edgeColor, float thickness = 1f)
            => ScreenManager.SpriteBatch.DrawRectangle(rectangle, edgeColor, thickness);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawRectangle(Vector2 center, Vector2 size, float rotation, Color color, float thickness = 1f)
            => ScreenManager.SpriteBatch.DrawRectangle(center, size, rotation, color, thickness);

        // just draws a texture to screen, no fancy reprojections, where screenPos is the texture CENTER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTexture(SubTexture texture, Vector2 posOnScreen, float scale, float rotation, Color color)
            => ScreenManager.SpriteBatch.Draw(texture, posOnScreen, color, rotation, texture.CenterF, scale, SpriteEffects.None, 1f);

        // just draws a texture to screen, no fancy reprojections, where screenPos is the texture CENTER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTextureSized(SubTexture texture, Vector2 posOnScreen, float rotation, float width, float height, Color color)
        {
            var rect = new RectF(posOnScreen.X, posOnScreen.Y, width, height);
            ScreenManager.SpriteBatch.Draw(texture, rect, color, rotation, texture.CenterF, SpriteEffects.None, 1f);
        }

        // just draws a texture to screen, no fancy reprojections, where screenPos is the texture CENTER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTextureSized(SubTexture texture, Vector2d posOnScreen, float rotation, double width, double height, Color color)
        {
            var rect = new RectF(posOnScreen.X, posOnScreen.Y, width, height);
            ScreenManager.SpriteBatch.Draw(texture, rect, color, rotation, texture.CenterF, SpriteEffects.None, 1f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTextureRect(SubTexture texture, Vector2 posOnScreen, Color color, float rotation = 0f)
            => DrawTextureRect(texture, posOnScreen, color, rotation, Vector2.Zero);

        // just draws a texture to screen, no fancy reprojections, where screenPos is the texture top left.
        public void DrawTextureRect(SubTexture texture, Vector2 posOnScreen, Color color, float rotation , Vector2 origin )
        {
            ScreenManager.SpriteBatch.Draw(texture, posOnScreen, color, rotation, Vector2.Zero, 0.5f, SpriteEffects.None, 1f);
        }

        public Vector2 FontSpace(Vector2 cursor, float spacing, LocalizedText drawnString, Font font)
        {
            cursor.X += (spacing - font.TextWidth(drawnString));
            return cursor;
        }

        // Draw string in screen coordinates. Text will be centered
        public void DrawString(Vector2 centerOnScreen, float rotation, float textScale, Color textColor, string text)
        {
            Vector2 size = Fonts.Arial11Bold.MeasureString(text);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial11Bold, text, centerOnScreen, textColor, rotation, size * 0.5f, textScale);
        }

        // Draw string in screen coordinates. No centering.
        public void DrawString(Vector2 posOnScreen, Color textColor, string text, Font font, float rotation = 0f, float textScale = 1f)
        {
            ScreenManager.SpriteBatch.DrawString(font, text, posOnScreen, textColor, rotation, Vector2.Zero, textScale);
        }

        public void MakeMessageBox(GameScreen screen, Action accepted, Action cancelled, GameText message, string okText, string cancelledText)
        {
            ScreenManager.AddScreen(new MessageBoxScreen(screen, message, okText, cancelledText)
            {
                Accepted = accepted,
                Cancelled = cancelled,
            });
        }

        public void ExitMessageBox(GameScreen screen, Action accepted, Action cancelled, GameText message)
        {
            MakeMessageBox(screen, accepted, cancelled, message, "Save", "Exit");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // Sets the View matrix and updates necessary variables to enable World <-> Screen coordinate conversion
        public void SetViewMatrix(in Matrix view)
        {
            View = view;
            UpdateWorldScreenProjection();
        }

        // Sets a common perspective projection to this screen
        // The default FOV is 45 degrees
        // @param maxDistance The maximum distance for objects on screen.
        //                    For Universe this is the Maximum supported HEIGHT of the CAMERA
        public void SetPerspectiveProjection(double fovYdegrees = 45, double maxDistance = 5000.0)
        {
            //SetProjection(Matrix.CreatePerspectiveFieldOfView(0.7853982f, Viewport.AspectRatio, 100f, 15000f)); // FleetDesignScreen
            //SetProjection(Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 1f, 120000f)); // ShipDesignScreen
            //SetProjection(Matrix.CreatePerspectiveFieldOfView(0.7853982f/*45 DEGREES*/, aspect, 100f, 3E+07f)); // UniverseScreen
            double fieldOfViewYrads = fovYdegrees.ToRadians();
            double aspectRatio = (double)Viewport.Width / Viewport.Height;
            Projection = Matrix.CreatePerspectiveFieldOfView(fieldOfViewYrads, aspectRatio, 100.0, maxDistance);
            UpdateWorldScreenProjection();
        }

        // Sets the view matrix and perspective projection in one operation
        public void SetViewPerspective(in Matrix view, double fovYdegrees = 45, double maxDistance = 5000.0)
        {
            View = view;
            SetPerspectiveProjection(fovYdegrees: fovYdegrees, maxDistance: maxDistance);
        }

        // visible rectangle in world coordinates
        public AABoundingBox2Dd VisibleWorldRect { get; private set; }

        protected void UpdateWorldScreenProjection()
        {
            View.Multiply(Projection, out ViewProjection);
            Matrix.Invert(in ViewProjection, out InverseViewProjection);

            OrthographicProjection = Matrix.CreateOrthographicOffCenter(0, Viewport.Width, Viewport.Height, 0, zNearPlane:1, zFarPlane:0);
            //OrthographicProjection = Matrix.CreateOrthographic(Viewport.Width, Viewport.Height, 0.0f, 5000.0f);

            VisibleWorldRect = UnprojectToWorldRect(new(0, 0, Viewport.Width, Viewport.Height));
        }

        public Vector2d ProjectToScreenPosition(Vector3d worldPos)
        {
            return Viewport.ProjectTo2D(worldPos, in ViewProjection);
        }

        // Projects World Pos into Screen Pos
        public Vector2d ProjectToScreenPosition(Vector3 worldPos)
        {
            return Viewport.ProjectTo2D(new Vector3d(worldPos), in ViewProjection);
        }

        // Projects World Pos into Screen Pos
        public Vector2d ProjectToScreenPosition(Vector2 posInWorld, float zAxis = 0f)
        {
            return Viewport.ProjectTo2D(new Vector3d(posInWorld, zAxis), in ViewProjection);
        }

        public void ProjectToScreenCoords(Vector3 posInWorld, float sizeInWorld,
                                          out Vector2d posOnScreen, out double sizeOnScreen)
        {
            // TODO: check accuracy of Pos and Size
            posOnScreen = ProjectToScreenPosition(posInWorld);
            var pos2 = ProjectToScreenPosition(new Vector3(posInWorld.X + sizeInWorld, posInWorld.Y, posInWorld.Z));
            sizeOnScreen = pos2.Distance(posOnScreen);
        }

        public void ProjectToScreenCoords(Vector2 posInWorld, float zAxis, float sizeInWorld,
                                          out Vector2d posOnScreen, out double sizeOnScreen)
        {
            // TODO: check accuracy of Pos and Size
            posOnScreen = ProjectToScreenPosition(posInWorld, zAxis);
            var pos2 = ProjectToScreenPosition(new Vector3(posInWorld.X + sizeInWorld, posInWorld.Y, zAxis));
            sizeOnScreen = pos2.Distance(posOnScreen);
        }

        public void ProjectToScreenCoords(Vector2 posInWorld, float sizeInWorld,
                                          out Vector2d posOnScreen, out double sizeOnScreen, float zAxis = 0)
        {
            ProjectToScreenCoords(posInWorld, zAxis, sizeInWorld, out posOnScreen, out sizeOnScreen);
        }

        public void ProjectToScreenCoords(Vector2 posInWorld, Vector2 sizeInWorld,
                                          out Vector2d posOnScreen, out Vector2d sizeOnScreen)
        {
            // TODO: check accuracy of Pos and Size
            posOnScreen  = ProjectToScreenPosition(posInWorld);
            Vector2d size = ProjectToScreenPosition(new Vector2(posInWorld.X + sizeInWorld.X, posInWorld.Y + sizeInWorld.Y)) - posOnScreen;
            sizeOnScreen = new Vector2d(Math.Abs(size.X), Math.Abs(size.Y));
        }
        
        public void ProjectToScreenCoordsF(Vector2 posInWorld, Vector2 sizeInWorld,
                                           out Vector2 posOnScreenF, out Vector2 sizeOnScreenF)
        {
            ProjectToScreenCoords(posInWorld, sizeInWorld,
                                          out Vector2d posOnScreen, out Vector2d sizeOnScreen);
            posOnScreenF = posOnScreen.ToVec2f();
            sizeOnScreenF = sizeOnScreen.ToVec2f();
        }

        public RectF ProjectToScreenRectF(in RectF worldRect)
        {
            Vector2d topLeft = ProjectToScreenPosition(new Vector2(worldRect.X, worldRect.Y));
            Vector2d botRight = ProjectToScreenPosition(new Vector2(worldRect.X + worldRect.W, worldRect.Y + worldRect.H));
            return new RectF(topLeft.X, topLeft.Y, (botRight.X - topLeft.X), (botRight.Y - topLeft.Y));
        }

        public RectF ProjectToScreenRectF(in Vector3 center, in Vector2 size)
        {
            Vector3d worldTL = new Vector3d(center.X - size.X*0.5, center.Y - size.Y*0.5, center.Z);
            Vector2d topLeft = ProjectToScreenPosition(worldTL);
            Vector2d botRight = ProjectToScreenPosition(new Vector3d(worldTL.X + size.X, worldTL.Y + size.Y, center.Z));
            return new RectF(topLeft.X, topLeft.Y, (botRight.X - topLeft.X), (botRight.Y - topLeft.Y));
        }

        public Rectangle ProjectToScreenRect(in RectF worldRect)
        {
            Vector2d topLeft = ProjectToScreenPosition(new Vector2(worldRect.X, worldRect.Y));
            Vector2d botRight = ProjectToScreenPosition(new Vector2(worldRect.X + worldRect.W, worldRect.Y + worldRect.H));
            return new Rectangle((int)topLeft.X, (int)topLeft.Y,
                                 (int)(botRight.X - topLeft.X),
                                 (int)(botRight.Y - topLeft.Y));
        }

        public Rectangle ProjectToScreenCoords(Vector2 posInWorld, float sizeInWorld)
        {
            ProjectToScreenCoords(posInWorld, 0f, sizeInWorld, out Vector2d pos, out double size);
            return new Rectangle((int)pos.X, (int)pos.Y, (int)size, (int)size);
        }

        public double ProjectToScreenSize(double sizeInWorld)
        {
            // NOTE: using Unproject here gives a huge precision and stability boost to the result
            //       because there is a float precision issue,
            //       where `Unproject & Project` transform doesn't give back the initial input
            //Vector3 screenWorld = UnprojectToWorldPosition3D(Vector2.Zero);
            //Vector2 a = ProjectToScreenPosition(new Vector2(screenWorld.X, screenWorld.Y));
            //Vector2 b = ProjectToScreenPosition(new Vector2(screenWorld.X + sizeInWorld, screenWorld.Y));
            //float sizeOnScreen = a.Distance(b);

            var topLeft = new Vector3d(VisibleWorldRect.X1, VisibleWorldRect.Y1, 0.0);
            Vector2d a = ProjectToScreenPosition(topLeft);
            Vector2d b = ProjectToScreenPosition(new Vector3d(topLeft.X + sizeInWorld, topLeft.Y, 0.0));
            double sizeOnScreen = a.Distance(b);
            return sizeOnScreen;
        }

        /// <summary>
        /// Unprojects a screenSpace 2D point into a 3D world position
        /// </summary>
        public Vector3d UnprojectToWorldPosition3D(Vector2 screenSpace, double ZPlane)
        {
            return Viewport.Unproject(screenSpace, ZPlane, in InverseViewProjection);
        }

        public Vector3d UnprojectToWorldPosition3D(Vector2 screenSpace)
        {
            return Viewport.Unproject(screenSpace, 0.0, in InverseViewProjection);
        }

        public Vector2 UnprojectToWorldPosition(Vector2 screenSpace, double ZPlane)
        {
            return UnprojectToWorldPosition3D(screenSpace, ZPlane).ToVec2f();
        }

        public Vector2 UnprojectToWorldPosition(Vector2 screenSpace)
        {
            return UnprojectToWorldPosition3D(screenSpace, ZPlane: 0.0).ToVec2f();
        }

        public AABoundingBox2Dd UnprojectToWorldRect(in AABoundingBox2D screenR)
        {
            Vector3d topLeft  = UnprojectToWorldPosition3D(new Vector2(screenR.X1, screenR.Y1));
            Vector3d botRight = UnprojectToWorldPosition3D(new Vector2(screenR.X2, screenR.Y2));
            return new AABoundingBox2Dd(topLeft, botRight);
        }

        public float UnprojectToWorldSize(float sizeOnScreen)
        {
            Vector3d left  = UnprojectToWorldPosition3D(new Vector2(-sizeOnScreen/2, 0));
            Vector3d right = UnprojectToWorldPosition3D(new Vector2(+sizeOnScreen/2, 0));

            return (float)left.Distance(right);
        }

        // Unprojects cursor screen pos to world 3D position
        public Vector3 CursorWorldPosition => UnprojectToWorldPosition3D(Input.CursorPosition).ToVec3f();
        public Vector2 CursorWorldPosition2D => UnprojectToWorldPosition(Input.CursorPosition);


        // projects the line from World positions into Screen positions, then draws the line
        public Vector2d DrawLineProjected(Vector2 startInWorld, Vector2 endInWorld, Color color, 
                                         float zAxis = 0f, float zAxisStart = -1f)
        {
            zAxisStart = zAxisStart < 0f ? zAxis : zAxisStart;
            Vector2d a = ProjectToScreenPosition(startInWorld, zAxisStart);
            Vector2d b = ProjectToScreenPosition(endInWorld, zAxis);
            DrawLine(a, b, color);
            return a;
        }

        public void DrawLineWideProjected(Vector2 startInWorld, Vector2 endInWorld, Color color, float thickness)
        {
            Vector2d a = ProjectToScreenPosition(startInWorld);
            Vector2d b = ProjectToScreenPosition(endInWorld);
            DrawLine(a, b, color, thickness);
        }

        public Vector2d DrawLineToPlanet(Vector2 startInWorld, Vector2 endInWorld, Color color)
            => DrawLineProjected(startInWorld, endInWorld, color, 2500);

        public void DrawCrossHairProjected(in Vector2 worldCenter, float worldSize, Color color, float thickness = 1f)
        {
            ProjectToScreenCoords(worldCenter, worldSize, out Vector2d screenPos, out double screenSize);
            DrawCrossHair(screenPos, screenSize, color, thickness);
        }

        public void DrawCircleProjected(Vector2 posInWorld, float radiusInWorld, Color color, float thickness = 1f)
        {
            ProjectToScreenCoords(posInWorld, radiusInWorld, out Vector2d screenPos, out double screenRadius);
            ScreenManager.SpriteBatch.DrawCircle(screenPos, screenRadius, color, thickness);
        }

        public void DrawCircleProjected(Vector2 posInWorld, float radiusInWorld, int sides, Color color, float thickness = 1f)
        {
            ProjectToScreenCoords(posInWorld, radiusInWorld, out Vector2d screenPos, out double screenRadius);
            ScreenManager.SpriteBatch.DrawCircle(screenPos, screenRadius, sides, color, thickness);
        }

        public void DrawCapsuleProjected(in Capsule capsuleInWorld, Color color, float thickness = 1f)
        {
            var capsuleOnScreen = new Capsule(
                ProjectToScreenPosition(capsuleInWorld.Start),
                ProjectToScreenPosition(capsuleInWorld.End),
                ProjectToScreenSize(capsuleInWorld.Radius)
            );
            ScreenManager.SpriteBatch.DrawCapsule(capsuleOnScreen, color, thickness);
        }

        public void DrawCircleProjectedZ(Vector2 posInWorld, float radiusInWorld, Color color, float zAxis = 0f)
        {
            ProjectToScreenCoords(posInWorld, radiusInWorld, out Vector2d screenPos, out double screenRadius, zAxis);
            ScreenManager.SpriteBatch.DrawCircle(screenPos, screenRadius, color);
        }

        public void DrawCircleProjected(in Vector3 posInWorld, float radiusInWorld, Color color)
        {
            ProjectToScreenCoords(posInWorld, radiusInWorld, out Vector2d screenPos, out double screenRadius);
            ScreenManager.SpriteBatch.DrawCircle(screenPos, screenRadius, color);
        }

        // draws a projected circle, with an additional overlay texture
        public void DrawCircleProjected(Vector2 posInWorld, float radiusInWorld, Color color, float thickness, SubTexture overlay, Color overlayColor, float z = 0)
        {
            ProjectToScreenCoords(posInWorld, radiusInWorld, out Vector2d screenPos, out double screenRadius);
            double scale = screenRadius / (overlay.Width * 0.5f);
            Vector2 pos = screenPos.ToVec2f();

            ScreenManager.SpriteBatch.Draw(overlay, pos, overlayColor, 0f, overlay.CenterF, (float)scale, SpriteEffects.None, 1f);
            ScreenManager.SpriteBatch.DrawCircle(screenPos, screenRadius, color, thickness);
        } 

        // projects a rectangle from World coordinates to Screen coordinates
        public void DrawRectangleProjected(in RectF worldRect, Color edge, float thickness = 1f)
        {
            Rectangle screenRect = ProjectToScreenRect(worldRect);
            DrawRectangle(screenRect, edge, thickness);
        }

        // projects a rectangle from World coordinates to Screen coordinates
        public void DrawRectangleProjected(in RectF worldRect, Color edge, Color fill, float thickness = 1f)
        {
            Rectangle screenRect = ProjectToScreenRect(worldRect);
            DrawRectangle(screenRect, edge, fill, thickness);
        }

        public void DrawRectangleProjected(Vector2 centerInWorld, Vector2 sizeInWorld, float rotation, Color color, float thickness = 1f)
        {
            ProjectToScreenCoords(centerInWorld, sizeInWorld, out Vector2d posOnScreen, out Vector2d sizeOnScreen);
            ScreenManager.SpriteBatch.DrawRectangle(posOnScreen, sizeOnScreen, rotation, color, thickness);
        }

        public void DrawRectProjected(in AABoundingBox2D worldRect, Color color, float thickness = 1f, float zAxis = 0f)
        {
            Vector2d tl = ProjectToScreenPosition(new Vector3d(worldRect.X1, worldRect.Y1, zAxis));
            Vector2d br = ProjectToScreenPosition(new Vector3d(worldRect.X2, worldRect.Y2, zAxis));
            var screenRect = new AABoundingBox2Dd(tl, br);
            ScreenManager.SpriteBatch.DrawRectangle(screenRect, color, thickness);
        }

        public void DrawRectProjected(in AABoundingBox2Di worldRect, Color color, float thickness = 1f)
        {
            Vector2d tl = ProjectToScreenPosition(new Vector3d(worldRect.X1, worldRect.Y1, 0f));
            Vector2d br = ProjectToScreenPosition(new Vector3d(worldRect.X2, worldRect.Y2, 0f));
            var screenRect = new AABoundingBox2Dd(tl, br);
            ScreenManager.SpriteBatch.DrawRectangle(screenRect, color, thickness);
        }

        public void DrawTextureProjected(SubTexture texture, Vector2 posInWorld, float textureScale, Color color)
        {
            ProjectToScreenCoords(posInWorld, textureScale*2, out Vector2d posOnScreen, out double sizeOnScreen);
            
            var rect = new Rectangle((int)posOnScreen.X, (int)posOnScreen.Y, (int)sizeOnScreen, (int)sizeOnScreen);
            ScreenManager.SpriteBatch.Draw(texture, rect, color, 0, texture.CenterF, SpriteEffects.None, 1f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTextureProjected(SubTexture texture, Vector2 posInWorld, float textureScale, float rotation, Color color)
        {
            var pos = ProjectToScreenPosition(posInWorld).ToVec2f();
            DrawTexture(texture, pos, textureScale, rotation, color);
        }

        public void DrawTextureWithToolTip(SubTexture texture, Color color, LocalizedText tooltip, Vector2 mousePos, int rectangleX, int rectangleY, int width, int height)
        {
            var rectangle = new Rectangle(rectangleX, rectangleY, width, height);
            ScreenManager.SpriteBatch.Draw(texture, rectangle, color);
            
            if (rectangle.HitTest(mousePos))
                ToolTip.CreateTooltip(tooltip);
        }

        public void DrawStringProjected(Vector2 posInWorld, float rotation, float textScale, Color textColor, string text)
        {
            DrawStringProjected(posInWorld, rotation, textScale, textColor, text, Fonts.Arial11Bold);
        }

        public void DrawStringProjected(Vector2 posInWorld, float rotation, float textScale, Color textColor, string text, Font font)
        {
            Vector2 pos = ProjectToScreenPosition(posInWorld).ToVec2f();
            Vector2 size = font.MeasureString(text);
            if (Primitives2D.IsIntersectingScreenPosSize(pos, size))
            {
                ScreenManager.SpriteBatch.DrawString(font, text, pos, textColor, rotation, size * 0.5f, textScale);
            }
        }

        public void DrawShadowStringProjected(Vector2 posInWorld, float rotation, float textScale, Color textColor, string text)
        {
            Vector2 pos = ProjectToScreenPosition(posInWorld).ToVec2f();
            Vector2 size = Fonts.Arial12Bold.MeasureString(text);
            if (Primitives2D.IsIntersectingScreenPosSize(pos, size))
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text,
                    pos+new Vector2(2), Color.Black, rotation, size * 0.5f, textScale);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text,
                    pos, textColor, rotation, size * 0.5f, textScale);
            }
        }

        public void DrawStringProjected(Vector2 posInWorld, float sizeInWorld, Color textColor, string text, bool shadow = false)
        {
            DrawStringProjected(posInWorld, sizeInWorld, textColor, text, Fonts.Arial11Bold, shadow);
        }

        // draws a string with 
        public void DrawStringProjected(Vector2 posInWorld, float sizeInWorld, Color textColor, string text, Font font,
                                        bool shadow = false, bool center = false)
        {
            Vector2d screenPos = ProjectToScreenPosition(posInWorld);
            Vector2 pos = screenPos.ToVec2f();
            Vector2 size = font.MeasureString(text);

            if (Primitives2D.IsIntersectingScreenPosSize(pos, size))
            {
                Vector2d screenPos2 = ProjectToScreenPosition(posInWorld + new Vector2(sizeInWorld, 0f));
                double widthOnScreen = Math.Abs(screenPos2.X - screenPos.X);
                double scale = widthOnScreen / size.Y;
                Vector2 origin = center ? size * 0.5f : Vector2.Zero;
                if (shadow)
                {
                    ScreenManager.SpriteBatch.DrawString(font, text, pos+new Vector2(2), Color.Black, 0f, origin, (float)scale);
                }
                ScreenManager.SpriteBatch.DrawString(font, text, pos, textColor, 0f, origin, (float)scale);
            }
        }

        /// <summary>
        /// This runs actions on the next GameScreen Update(), before Draw().
        /// 
        /// Action will only work while the screen is Visible.
        /// </summary>
        public void RunOnNextFrame(Action action)
        {
            if (action == null) throw new NullReferenceException(nameof(action));
            PendingActions.Enqueue(action);
        }

        /// <summary>
        /// True if current thread is the UI Thread
        /// </summary>
        public bool IsUIThread => Thread.CurrentThread.ManagedThreadId == GameBase.MainThreadId;

        /// <summary>
        /// Invokes all Pending actions.
        /// </summary>
        void InvokePendingActions()
        {
            while (PendingActions.TryDequeue(out Action action))
                action();
        }

        protected virtual GameCursor GetCurrentCursor()
        {
            return GameCursors.Regular; // default to regular cursor
        }
    }
}