using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Data;
using Ship_Game.GameScreens;
using Ship_Game.UI;
using Ship_Game.Utils;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public abstract class GameScreen : MultiLayerDrawContainer, IDisposable
    {
        public InputState Input;
        bool OtherScreenHasFocus;

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
        bool Pauses = true;

        // multi cast exit delegate, called when a game screen is exiting
        public event Action OnExit;
        public bool IsDisposed { get; private set; }

        // This should be used for content that gets unloaded once this GameScreen disappears
        public GameContentManager TransientContent;

        public Matrix View, Projection;


        protected GameScreen(GameScreen parent, bool pause = true) 
            : this(parent, new Rectangle(0, 0, GameBase.ScreenWidth, GameBase.ScreenHeight), pause)
        {
        }
        
        protected GameScreen(GameScreen parent, in Rectangle rect, bool pause = true) : base(rect)
        {
            // hook the content chain to parent screen if possible
            TransientContent = new GameContentManager(parent?.TransientContent ?? GameBase.Base.Content, GetType().Name);
            ScreenManager    = parent?.ScreenManager ?? GameBase.ScreenManager;
            UpdateViewport();

            if (pause & Empire.Universe?.IsActive == true && Empire.Universe?.Paused == false)
            {
                Empire.Universe.Paused = true;
            }
            else
            {
                Pauses = false;
            }

            if (Input == null)
                Input = ScreenManager?.input;

            LowRes = ScreenWidth <= 1366 || ScreenHeight <= 720;
            HiRes  = ScreenWidth > 1920 || ScreenHeight > 1400;
        }

        ~GameScreen() { Destroy(); }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        protected virtual void Destroy()
        {
            IsDisposed = true;
            TransientContent?.Dispose(ref TransientContent);
        }

        // select size based on current res: Low, Normal, Hi
        protected int SelectSize(int lowRes, int normalRes, int hiRes)
        {
            if (LowRes) return lowRes;
            if (HiRes) return hiRes;
            return normalRes;
        }

        public void UpdateViewport() => Viewport = GameBase.Viewport;

        public void AddObject(ISceneObject so)    => ScreenManager.AddObject(so);
        public void RemoveObject(ISceneObject so) => ScreenManager.RemoveObject(so);
        public void AddLight(ILight light)        => ScreenManager.AddLight(light);
        public void RemoveLight(ILight light)     => ScreenManager.RemoveLight(light);

        public void AssignLightRig(LightRigIdentity identity, string rigContentPath)
        {
            var lightRig = TransientContent.Load<LightRig>(rigContentPath);
            ScreenManager.AssignLightRig(identity, lightRig);
        }

        public virtual void ExitScreen()
        {
            if (Pauses && Empire.Universe != null)
                Empire.Universe.Paused = Pauses = false;

            // if we got any tooltips, clear them now
            ToolTip.Clear();

            // call the exit event only once
            if (OnExit != null)
            {
                OnExit();
                OnExit = null;
            }

            if (TransitionOffTime.NotZero())
            {
                IsExiting = true;
                return;
            }
            ScreenManager.RemoveScreen(this);
        }

        public virtual void OnScreenRemoved()
        {
            Enabled = Visible = false;
            ScreenState = ScreenState.Hidden;
        }

        public virtual void ReloadContent()
        {
            UnloadContent();
            LoadContent();
        }

        public virtual void LoadContent()
        {
            DidLoadContent = true;
            PerformLayout();
        }

        public virtual void UnloadContent()
        {
            TransientContent?.Unload();
            Elements.Clear();
        }

        public override bool HandleInput(InputState input)
        {
            if (!Visible || !Enabled || !IsActive)
                return false;

            // First allow other UI elements to capture input
            if (base.HandleInput(input))
                return true;

            // only then check for ExitScreen condition
            if ((CanEscapeFromScreen && input.Escaped) ||
                (CanEscapeFromScreen && IsPopup && input.RightMouseClick))
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }
            return false;
        }

        public virtual void Update(UpdateTimes elapsed, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            if (IsDisposed)
            {
                Log.Error($"Screen {Name} Updated after being disposed!");
                return;
            }

            //Log.Info($"Update {Name} {DeltaTime:0.000}  DidLoadContent:{DidLoadContent}");

            Visible = ScreenState != ScreenState.Hidden;

            // Update new UIElementV2
            Update(elapsed.RealTime.Seconds);

            OtherScreenHasFocus = otherScreenHasFocus;
            if (IsExiting)
            {
                ScreenState = ScreenState.TransitionOff;
                if (!UpdateTransition(elapsed, TransitionOffTime, 1))
                {
                    ScreenManager.RemoveScreen(this);
                    IsExiting = false;
                }
            }
            else
            {
                if (coveredByOtherScreen)
                {
                    ScreenState = UpdateTransition(elapsed, TransitionOffTime, 1)
                        ? ScreenState.TransitionOff
                        : ScreenState.Hidden;
                }
                else
                {
                    ScreenState = UpdateTransition(elapsed, TransitionOnTime, -1)
                        ? ScreenState.TransitionOn
                        : ScreenState.Active;
                }
            }

            DidLoadContent = false;
        }

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

        protected Color ApplyCurrentAlphaToColor(Color color)
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

        public Vector2 ProjectTo2D(Vector3 position)
        {
            return Viewport.ProjectTo2D(position, Projection, View);
        }
        
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // just draws a line, no fancy reprojections
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawLine(Vector2 screenPoint1, Vector2 screenPoint2, Color color, float thickness = 1f)
            => ScreenManager.SpriteBatch.DrawLine(screenPoint1, screenPoint2, color, thickness);

        // just draws a circle, no fancy reprojections
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawCircle(Vector2 posOnScreen, float radius, Color color, float thickness = 1f)
            => ScreenManager.SpriteBatch.DrawCircle(posOnScreen, radius, color, thickness);

        // just draws a circle, no fancy reprojections
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawCircle(Vector2 posOnScreen, float radius, int sides, Color color, float thickness = 1f)
            => ScreenManager.SpriteBatch.DrawCircle(posOnScreen, radius, sides, color, thickness);

        // Just draws a given rectangle with a color fill
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawRectangle(Rectangle rectangle, Color edgeColor, Color fillColor, float thickness = 1f)
        {
            ScreenManager.SpriteBatch.FillRectangle(rectangle, fillColor);
            DrawRectangle(rectangle, edgeColor, thickness);               
        }

        // Just draws a given rectangle
        public void DrawRectangle(Rectangle rectangle, Color edgeColor, float thickness = 1f)
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
            var rect = new Rectangle((int)posOnScreen.X, (int)posOnScreen.Y, (int)width, (int)height);
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

        public void CheckToolTip(int toolTipId, Rectangle rectangle, Vector2 mousePos)
        {
            if (rectangle.HitTest(mousePos))
                ToolTip.CreateTooltip(toolTipId);
        }
        public void CheckToolTip(int toolTipId, Vector2 cursor, string words, string numbers, SpriteFont font, Vector2 mousePos)
        {
            var rect = new Rectangle((int)cursor.X, (int)cursor.Y, 
                font.TextWidth(words) + font.TextWidth(numbers), font.LineSpacing);
            CheckToolTip(toolTipId, rect, mousePos);
        }
        public Vector2 FontSpace(Vector2 cursor, float spacing, string drawnString, SpriteFont font)
        {
            cursor.X += (spacing - font.MeasureString(drawnString).X);
            return cursor;
        }
        // Draw string in screen coordinates. Text will be centered
        public void DrawString(Vector2 centerOnScreen, float rotation, float textScale, Color textColor, string text)
        {
            Vector2 size = Fonts.Arial11Bold.MeasureString(text);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial11Bold, text, centerOnScreen, textColor, rotation, size * 0.5f, textScale, SpriteEffects.None, 1f);
        }
        // Draw string in screen coordinates. No centering.
        public void DrawString(Vector2 posOnScreen, Color textColor, string text, SpriteFont font, float rotation = 0f, float textScale = 1f)
        {
            ScreenManager.SpriteBatch.DrawString(font, text, posOnScreen, textColor, rotation, Vector2.Zero, textScale, SpriteEffects.None, 1f);
        }
        public float Spacing(float amount)
        {          
            if (GlobalStats.IsGermanFrenchOrPolish) amount += 20f;
            return amount;
        }

        public void MakeMessageBox(GameScreen screen, Action cancelled, Action accepted, int localId, string okText, string cancelledText)
        {
            ScreenManager.AddScreenDeferred(new MessageBoxScreen(screen, localId, okText, cancelledText)
            {
                Cancelled = cancelled,
                Accepted = accepted
            });            
        }

        public void ExitMessageBox(GameScreen screen, Action cancelled, Action accepted, int localId)
        {
            MakeMessageBox(screen, cancelled, accepted, localId, "Save", "Exit");
        }

        public void DrawModelMesh(
            Model model, in Matrix world, in Matrix view, 
            Vector3 diffuseColor, in Matrix projection, 
            SubTexture projTex, 
            float alpha = 0f, 
            bool textureEnabled = true, 
            bool lightingEnabled = false)
        {
            foreach (ModelMesh modelMesh in model.Meshes)
            {
                foreach (Effect effect in modelMesh.Effects)
                {
                    var be = effect as BasicEffect;
                    if (be == null) continue;
                    be.World           = Matrix.CreateScale(50f) * world;
                    be.View            = view;
                    be.DiffuseColor    = diffuseColor;
                    be.Texture         = projTex.Texture;
                    be.Alpha           = alpha > 0 ? alpha : be.Alpha;                    
                    be.TextureEnabled  = true;
                    be.Projection      = projection;
                    be.LightingEnabled = lightingEnabled;
                }
                modelMesh.Draw();
            }
        }

        public void DrawTransparentModel(Model model, in Matrix world, SubTexture projTex, float scale)
        {
            DrawModelMesh(model, Matrix.CreateScale(scale) * world, View, Vector3.One, Projection, projTex);
            Device.RenderState.DepthBufferWriteEnable = true;
        }

        // this does some magic to convert a game position/coordinate to a drawable screen position
        public Vector2 ProjectToScreenPosition(Vector2 posInWorld, float zAxis = 0f)
        {
            return Viewport.ProjectTo2D(posInWorld.ToVec3(zAxis), Projection, View);
        }

        public void ProjectToScreenCoords(Vector2 posInWorld, float zAxis, float sizeInWorld, out Vector2 posOnScreen, out float sizeOnScreen)
        {
            posOnScreen  = ProjectToScreenPosition(posInWorld, zAxis);
            sizeOnScreen = ProjectToScreenPosition(new Vector2(posInWorld.X + sizeInWorld, posInWorld.Y),zAxis).Distance(ref posOnScreen);
        }

        public void ProjectToScreenCoords(Vector2 posInWorld, float sizeInWorld, out Vector2 posOnScreen, out float sizeOnScreen, float zAxis = 0)
        {
            ProjectToScreenCoords(posInWorld, zAxis, sizeInWorld, out posOnScreen, out sizeOnScreen);
        }

        public void ProjectToScreenCoords(Vector2 posInWorld, Vector2 sizeInWorld, out Vector2 posOnScreen, out Vector2 sizeOnScreen)
        {
            posOnScreen  = ProjectToScreenPosition(posInWorld);
            Vector2 size = ProjectToScreenPosition(new Vector2(posInWorld.X + sizeInWorld.X, posInWorld.Y + sizeInWorld.Y)) - posOnScreen;
            sizeOnScreen = new Vector2(Math.Abs(size.X), Math.Abs(size.Y));
        }

        public RectF ProjectToScreenRect(in RectF worldRect)
        {
            Vector2 topLeft = ProjectToScreenPosition(new Vector2(worldRect.X, worldRect.Y));
            Vector2 botRight = ProjectToScreenPosition(new Vector2(worldRect.X + worldRect.W, worldRect.Y + worldRect.H));
            RectF screenRect = default;
            screenRect.X = topLeft.X;
            screenRect.Y = topLeft.Y;
            screenRect.W = (botRight.X - topLeft.X);
            screenRect.H = (botRight.Y - topLeft.Y);
            return screenRect;
        }

        public Rectangle ProjectToScreenCoords(Vector2 posInWorld, float sizeInWorld)
        {
            ProjectToScreenCoords(posInWorld, 0f, sizeInWorld, out Vector2 pos, out float size);
            return new Rectangle((int)pos.X, (int)pos.Y, (int)size, (int)size);
        }

        public float ProjectToScreenSize(float sizeInWorld)
        {
            Vector2 a = ProjectToScreenPosition(Vector2.Zero);
            Vector2 b = ProjectToScreenPosition(new Vector2(sizeInWorld, 0f));
            return a.Distance(b);
        }

        public Vector3 UnprojectToWorldPosition3D(Vector2 screenSpace)
        {
            Vector3 pos = Viewport.Unproject(new Vector3(screenSpace, 0f), Projection, View, Matrix.Identity);
            Vector3 dir = Viewport.Unproject(new Vector3(screenSpace, 1f), Projection, View, Matrix.Identity) - pos;
            dir.Normalize();
            float num = -pos.Z / dir.Z;
            return (pos + num * dir);
        }

        public Vector2 UnprojectToWorldPosition(Vector2 screenSpace)
        {
            return UnprojectToWorldPosition3D(screenSpace).ToVec2();
        }

        public AABoundingBox2D UnprojectToWorldRect(in Rectangle screenR)
        {
            Vector2 topLeft  = UnprojectToWorldPosition(new Vector2(screenR.X, screenR.Y));
            Vector2 botRight = UnprojectToWorldPosition(new Vector2(screenR.Right, screenR.Bottom));
            return new AABoundingBox2D(topLeft, botRight);
        }

        // visible rectangle in world coordinates
        public AABoundingBox2D GetVisibleWorldRect()
        {
            return UnprojectToWorldRect(new Rectangle(0,0, Viewport.Width, Viewport.Height));
        }

        // Unprojects cursor screen pos to world 3D position
        public Vector3 CursorWorldPosition => UnprojectToWorldPosition3D(Input.CursorPosition);


        // projects the line from World positions into Screen positions, then draws the line
        public Vector2 DrawLineProjected(Vector2 startInWorld, Vector2 endInWorld, Color color, 
                                         float zAxis = 0f, float zAxisStart = -1f)
        {
            zAxisStart = zAxisStart < 0f ? zAxis : zAxisStart;
            Vector2 projPos = ProjectToScreenPosition(startInWorld, zAxisStart);
            DrawLine(projPos, ProjectToScreenPosition(endInWorld, zAxis), color);
            return projPos;
        }

        public void DrawLineWideProjected(Vector2 startInWorld, Vector2 endInWorld, Color color, float thickness)
        {
            Vector2 projPos = ProjectToScreenPosition(startInWorld);
            DrawLine(projPos, ProjectToScreenPosition(endInWorld), color, thickness);
        }

        public Vector2 DrawLineToPlanet(Vector2 startInWorld, Vector2 endInWorld, Color color)
            => DrawLineProjected(startInWorld, endInWorld, color, 2500);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawCircleProjected(Vector2 posInWorld, float radiusInWorld, Color color, float thickness = 1f)
        {
            ProjectToScreenCoords(posInWorld, radiusInWorld, out Vector2 screenPos, out float screenRadius);
            DrawCircle(screenPos, screenRadius, color, thickness);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawCircleProjected(Vector2 posInWorld, float radiusInWorld, int sides, Color color, float thickness = 1f)
        {
            ProjectToScreenCoords(posInWorld, radiusInWorld, out Vector2 screenPos, out float screenRadius);
            DrawCircle(screenPos, screenRadius, sides, color, thickness);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawCapsuleProjected(in Capsule capsuleInWorld, Color color, float thickness = 1f)
        {
            var capsuleOnScreen = new Capsule(
                ProjectToScreenPosition(capsuleInWorld.Start),
                ProjectToScreenPosition(capsuleInWorld.End),
                ProjectToScreenSize(capsuleInWorld.Radius)
            );
            ScreenManager.SpriteBatch.DrawCapsule(capsuleOnScreen, color, thickness);
        }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawCircleProjectedZ(Vector2 posInWorld, float radiusInWorld, Color color, float zAxis = 0f)
        {
            ProjectToScreenCoords(posInWorld, radiusInWorld, out Vector2 screenPos, out float screenRadius, zAxis);
            DrawCircle(screenPos, screenRadius, color);
        }

        // draws a projected circle, with an additional overlay texture
        public void DrawCircleProjected(Vector2 posInWorld, float radiusInWorld, Color color, float thickness, SubTexture overlay, Color overlayColor, float z = 0)
        {
            ProjectToScreenCoords(posInWorld, radiusInWorld, out Vector2 screenPos, out float screenRadius);
            float scale = screenRadius / (overlay.Width * .5f);
            DrawTexture(overlay, screenPos, scale, 0f, overlayColor);
            DrawCircle(screenPos, screenRadius, color, thickness);
        } 

        public void DrawRectangleProjected(Rectangle rectangle, Color edge)
        {
            Vector2 rectTopLeft  = ProjectToScreenPosition(new Vector2(rectangle.X, rectangle.Y));

            Vector2 rectBotRight = ProjectToScreenPosition(new Vector2(rectangle.X - rectangle.Width, rectangle.Y - rectangle.Height));
            var rect = new Rectangle((int)rectTopLeft.X, (int)rectTopLeft.Y, (int)(rectTopLeft.X - rectBotRight.X), (int)(rectTopLeft.Y - rectBotRight.Y));
            DrawRectangle(rect, edge);
        }

        public void DrawRectangleProjected(Rectangle rectangle, Color edge, Color fill)
        {
            Vector2 rectTopLeft  = ProjectToScreenPosition(new Vector2(rectangle.X, rectangle.Y));
            Vector2 rectBotRight = ProjectToScreenPosition(new Vector2(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height));
            var rect  = new Rectangle((int)rectTopLeft.X, (int)rectTopLeft.Y, 
                                    (int)Math.Abs(rectTopLeft.X - rectBotRight.X), (int)Math.Abs(rectTopLeft.Y - rectBotRight.Y));
            DrawRectangle(rect, edge, fill);            
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawRectangleProjected(Vector2 centerInWorld, Vector2 sizeInWorld, float rotation, Color color, float thickness = 1f)
        {
            ProjectToScreenCoords(centerInWorld, sizeInWorld, out Vector2 posOnScreen, out Vector2 sizeOnScreen);
            DrawRectangle(posOnScreen, sizeOnScreen, rotation, color, thickness);
        }

        public void DrawRectProjected(in AABoundingBox2D worldRect, Color color, float thickness = 1f)
        {
            Vector2 tl = ProjectToScreenPosition(new Vector2(worldRect.X1, worldRect.Y1));
            Vector2 br = ProjectToScreenPosition(new Vector2(worldRect.X2, worldRect.Y2));
            var screenRect = new AABoundingBox2D(tl, br);
            ScreenManager.SpriteBatch.DrawRectangle(screenRect, color, thickness);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTextureProjected(SubTexture texture, Vector2 posInWorld, float textureScale, Color color)
        {
            ProjectToScreenCoords(posInWorld, textureScale*2, out Vector2 posOnScreen, out float sizeOnScreen);
            DrawTextureSized(texture, posOnScreen, 0.0f, sizeOnScreen, sizeOnScreen, color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTextureProjected(SubTexture texture, Vector2 posInWorld, float textureScale, float rotation, Color color)
            => DrawTexture(texture, ProjectToScreenPosition(posInWorld), textureScale, rotation, color);

        public void DrawTextureWithToolTip(SubTexture texture, Color color, int tooltipID, Vector2 mousePos, int rectangleX, int rectangleY, int width, int height)
        {
            var rectangle = new Rectangle(rectangleX, rectangleY, width, height);
            ScreenManager.SpriteBatch.Draw(texture, rectangle, color);
            
            if (rectangle.HitTest(mousePos))
                ToolTip.CreateTooltip(tooltipID);                
        }

        public void DrawTextureWithToolTip(SubTexture texture, Color color, string text, Vector2 mousePos, int rectangleX, int rectangleY, int width, int height)
        {
            var rectangle = new Rectangle(rectangleX, rectangleY, width, height);
            ScreenManager.SpriteBatch.Draw(texture, rectangle, color);

            if (rectangle.HitTest(mousePos))
                ToolTip.CreateTooltip(text);
        }

        public void DrawStringProjected(Vector2 posInWorld, float rotation, float textScale, Color textColor, string text)
        {
            Vector2 screenPos = Empire.Universe.ProjectToScreenPosition(posInWorld);
            Vector2 size = Fonts.Arial11Bold.MeasureString(text);
            if (Primitives2D.IsIntersectingScreenPosSize(screenPos, size))
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial11Bold, text,
                    screenPos, textColor, rotation, size * 0.5f, textScale, SpriteEffects.None, 1f);
            }
        }

        public void DrawShadowStringProjected(Vector2 posInWorld, float rotation, float textScale, Color textColor, string text)
        {
            Vector2 screenPos = Empire.Universe.ProjectToScreenPosition(posInWorld);
            Vector2 size = Fonts.Arial12Bold.MeasureString(text);
            if (Primitives2D.IsIntersectingScreenPosSize(screenPos, size))
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text,
                    screenPos+new Vector2(2), Color.Black, rotation, size * 0.5f, textScale, SpriteEffects.None, 1f);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text,
                    screenPos, textColor, rotation, size * 0.5f, textScale, SpriteEffects.None, 1f);
            }
        }

        public void DrawStringProjected(Vector2 posInWorld, float sizeInWorld, Color textColor, string text)
        {
            Vector2 screenPos = Empire.Universe.ProjectToScreenPosition(posInWorld);
            Vector2 screenPos2 = Empire.Universe.ProjectToScreenPosition(posInWorld + new Vector2(sizeInWorld, 0f));

            float widthOnScreen = Math.Abs(screenPos2.X - screenPos.X);
            Vector2 size = Fonts.Arial11Bold.MeasureString(text);
            float scale = widthOnScreen / size.X;

            if (Primitives2D.IsIntersectingScreenPosSize(screenPos, size))
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial11Bold, text,
                    screenPos, textColor, 0f, size * 0.5f, scale, SpriteEffects.None, 1f);
            }
        }

    }
}