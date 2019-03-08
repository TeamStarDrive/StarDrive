using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Ship_Game.Audio;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public abstract class GameScreen : UIElementContainer, IDisposable
    {
        public InputState Input;
        bool OtherScreenHasFocus;

        public bool IsActive => !OtherScreenHasFocus && !IsExiting && 
            (ScreenState == ScreenState.TransitionOn || ScreenState == ScreenState.Active);

        public bool IsExiting { get; protected set; }
        public bool IsPopup   { get; protected set; }

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
        public int ScreenWidth      => StarDriveGame.Instance.ScreenWidth;
        public int ScreenHeight     => StarDriveGame.Instance.ScreenHeight;
        public Vector2 MousePos     => Input.CursorPosition;
        public Vector2 ScreenArea   => StarDriveGame.Instance.ScreenArea;
        public Vector2 ScreenCenter => StarDriveGame.Instance.ScreenArea * 0.5f;
        public GameTime GameTime    => StarDriveGame.Instance.GameTime;
        protected bool Pauses = true;

        // multi cast exit delegate, called when a game screen is exiting
        public event Action OnExit;

        // This should be used for content that gets unloaded once this GameScreen disappears
        public GameContentManager TransientContent;

        // Current delta time between this and last game frame
        public float DeltaTime { get; protected set; }

        //video player
        protected AudioHandle MusicPlaying = AudioHandle.Dummy;
        protected Video VideoFile;
        protected VideoPlayer VideoPlaying;
        protected Texture2D VideoTexture;

        protected Matrix View, Projection;

        protected GameScreen(GameScreen parent, bool pause = true) 
            : this(parent, new Rectangle(0, 0, StarDriveGame.Instance.ScreenWidth, StarDriveGame.Instance.ScreenHeight), pause)
        {
        }
        
        protected GameScreen(GameScreen parent, Rectangle rect, bool pause = true) : base(parent, rect)
        {
            // hook the content chain to parent screen if possible
            TransientContent = new GameContentManager(parent?.TransientContent ?? StarDriveGame.Instance.Content, GetType().Name);
            ScreenManager    = parent?.ScreenManager ?? StarDriveGame.Instance.ScreenManager;
            UpdateViewport();

            if (pause & Empire.Universe?.IsActive == true && Empire.Universe?.Paused == false)
                Empire.Universe.Paused = true;
            else Pauses = false;
            if (Input == null)
                Input = ScreenManager.input;
        }

        public void UpdateViewport() => Viewport = StarDriveGame.Instance.Viewport;

        public void AddObject(ISceneObject so)    => ScreenManager.AddObject(so);
        public void RemoveObject(ISceneObject so) => ScreenManager.RemoveObject(so);
        public void AddLight(ILight light)        => ScreenManager.AddLight(light);
        public void RemoveLight(ILight light)     => ScreenManager.RemoveLight(light);

        public void AssignLightRig(string rigContentPath)
        {
            var lightRig = TransientContent.Load<LightRig>(rigContentPath);
            ScreenManager.AssignLightRig(lightRig);
        }

        public virtual void ExitScreen()
        {
            ScreenManager.exitScreenTimer = 0.25f;            
            if (Pauses && Empire.Universe != null)
                Empire.Universe.Paused = Pauses = false;

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
            Empire.Universe?.ResetLighting();
            ScreenManager.RemoveScreen(this);
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

        public virtual void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            // @note If content was being loaded, we will force deltaTime to 1/60th
            //       This will prevent animations going nuts due to huge deltaTime
            DeltaTime = DidLoadContent ? (1.0f/60.0f) : StarDriveGame.Instance.DeltaTime;
            //Log.Info($"Update {Name} {DeltaTime:0.000}  DidLoadContent:{DidLoadContent}");

            Visible = ScreenState != ScreenState.Hidden;

            // Update new UIElementV2
            Update(DeltaTime);

            OtherScreenHasFocus = otherScreenHasFocus;
            if (!IsExiting)
            {
                if (coveredByOtherScreen)
                {
                    ScreenState = UpdateTransition(TransitionOffTime, 1)
                                ? ScreenState.TransitionOff : ScreenState.Hidden;
                }
                else
                {
                    ScreenState = UpdateTransition(TransitionOnTime, -1)
                                ? ScreenState.TransitionOn : ScreenState.Active;
                }
            }
            else
            {
                ScreenState = ScreenState.TransitionOff;
                if (!UpdateTransition(TransitionOffTime, 1))
                {
                    ScreenManager.RemoveScreen(this);
                    IsExiting = false;
                }
            }

            DidLoadContent = false;
        }
        

        bool UpdateTransition(float time, int direction)
        {
            float transitionDelta = (time.NotZero() ? (DeltaTime / time) : 1f);
            TransitionPosition += transitionDelta * direction;
            if (TransitionPosition > 0f && TransitionPosition < 1f)
                return true;

            TransitionPosition = TransitionPosition.Clamped(0, 1);
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        readonly Array<UIElementV2> BackElements = new Array<UIElementV2>();
        readonly Array<UIElementV2> BackAdditive = new Array<UIElementV2>();
        readonly Array<UIElementV2> ForeElements = new Array<UIElementV2>();
        readonly Array<UIElementV2> ForeAdditive = new Array<UIElementV2>();

        void ClearDrawLayers()
        {
            BackElements.Clear();
            BackAdditive.Clear();
            ForeElements.Clear();
            ForeAdditive.Clear();
        }

        void GatherDrawLayers()
        {
            int count = Elements.Count;
            UIElementV2[] elements = Elements.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
            {
                UIElementV2 e = elements[i];
                if (e.Visible) switch (e.DrawDepth)
                {
                    default:
                    case DrawDepth.Foreground:   ForeElements.Add(e); break;
                    case DrawDepth.Background:   BackElements.Add(e); break;
                    case DrawDepth.ForeAdditive: ForeAdditive.Add(e); break;
                    case DrawDepth.BackAdditive: BackAdditive.Add(e); break;
                }
            }
        }

        public void DrawMultiLayeredExperimental(ScreenManager manager, bool draw3D = false)
        {
            if (!Visible)
                return;
            
            GatherDrawLayers();

            if (draw3D) manager.BeginFrameRendering(GameTime, ref View, ref Projection);

            SpriteBatch batch = manager.SpriteBatch;

            if (BackElements.NotEmpty) BatchDrawSimple(batch, BackElements);
            if (BackAdditive.NotEmpty) BatchDrawAdditive(batch, BackAdditive);

            if (draw3D) manager.RenderSceneObjects();

            // @note Foreground is the default layer
            if (ForeElements.NotEmpty) BatchDrawSimple(batch, ForeElements, drawToolTip: true);
            if (ForeAdditive.NotEmpty) BatchDrawAdditive(batch, ForeAdditive);

            if (draw3D) manager.EndFrameRendering();

            ClearDrawLayers();
        }

        public void BatchDrawSimple(SpriteBatch batch, Array<UIElementV2> elements, bool drawToolTip = false)
        {
            batch.Begin();
            int count = elements.Count;
            UIElementV2[] items = elements.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
            {
                UIElementV2 e = items[i];
                if (e.Visible) e.Draw(batch);
            }
            if (drawToolTip)
            {
                if (ToolTip.Hotkey.IsEmpty())
                    ToolTip.Draw(batch);
            }
            batch.End();
        }

        public void BeginAdditive(SpriteBatch batch, bool saveState = false)
        {
            batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, 
                saveState ? SaveStateMode.SaveState : SaveStateMode.None);
            Device.RenderState.SourceBlend      = Blend.InverseDestinationColor;
            Device.RenderState.DestinationBlend = Blend.One;
            Device.RenderState.BlendFunction    = BlendFunction.Add;
        }
        
        public void BatchDrawAdditive(SpriteBatch batch, IReadOnlyList<UIElementV2> elements)
        {
            BeginAdditive(batch);

            int count = elements.Count;
            for (int i = 0; i < count; ++i)
            {
                UIElementV2 e = elements[i];
                if (e.Visible) e.Draw(batch);
            }

            batch.End();
        }

        
        public void DrawElementsActiveBatch(SpriteBatch batch, IReadOnlyList<UIElementV2> elements)
        {
            int count = elements.Count;
            for (int i = 0; i < count; ++i)
            {
                UIElementV2 e = elements[i];
                if (e.Visible) e.Draw(batch);
            }
        }

        public void DrawElementsAtDepth(SpriteBatch batch, DrawDepth depth)
        {
            int count = Elements.Count;
            UIElementV2[] items = Elements.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
            {
                UIElementV2 e = items[i];
                if (e.Visible && e.DrawDepth == depth)
                    e.Draw(batch);
            }
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
        public void DrawRectangle(Rectangle rectangle, Color edgeColor, Color fillColor)
        {
            ScreenManager.SpriteBatch.FillRectangle(rectangle, fillColor);
            DrawRectangle(rectangle, edgeColor);               
        }

        // Just draws a given rectangle
        public void DrawRectangle(Rectangle rectangle, Color edgeColor)
            => ScreenManager.SpriteBatch.DrawRectangle(rectangle, edgeColor);

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
        public void DrawString(Vector2 posOnScreen, float rotation, float textScale, Color textColor, string text)
        {
            Vector2 size = Fonts.Arial11Bold.MeasureString(text);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial11Bold, text, posOnScreen, textColor, rotation, size * 0.5f, textScale, SpriteEffects.None, 1f);
        }
        public void DrawString(Vector2 posOnScreen, Color textColor, string text, SpriteFont font, float rotation = 0f, float textScale = 1f)
        {
            ScreenManager.SpriteBatch.DrawString(font, text, posOnScreen, textColor, rotation, Vector2.Zero, textScale, SpriteEffects.None, 1f);
        }
        public float Spacing(float amount)
        {          
            if (GlobalStats.IsGermanFrenchOrPolish) amount += 20f;
            return amount;
        }

        public void MakeMessageBox(GameScreen screen, EventHandler<EventArgs> cancelled, EventHandler<EventArgs> accepted, int localId, string okText, string cancelledText)
        {
            var messageBox = new MessageBoxScreen(screen, localId, okText, cancelledText);
            messageBox.Cancelled += cancelled;
            messageBox.Accepted += accepted;
            ScreenManager.AddScreen(messageBox);            
        }
        public void ExitMessageBox(GameScreen screen, EventHandler<EventArgs> cancelled, EventHandler<EventArgs> accepted, int localId)
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

        public void PlayVideo(string videoPath, GameContentManager content = null)
        {
            content = content ?? TransientContent;
            if (videoPath.IsEmpty()) return;

            VideoFile = ResourceManager.LoadVideo(content, videoPath);
            VideoPlaying = new VideoPlayer
            {
                Volume = GlobalStats.MusicVolume,
                IsLooped = true
            };
            try
            {
                VideoPlaying.Play(VideoFile);
            }
            catch
            {
                Log.Error($"Video '{videoPath}' failed.");
            }
        }

        public void PlayEmpireMusic(Empire empire, bool warMusic)
        {
            if (!empire.data.ModRace)
            {
                if (empire.data.MusicCue != null)
                    if (warMusic)
                        MusicPlaying = GameAudio.PlayMusic("Stardrive_Combat 1c_114BPM");
                    else
                        MusicPlaying = GameAudio.PlayMusic(empire.data.MusicCue);
            }
        }

        ~GameScreen() { Destroy(); }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        protected virtual void Destroy()
        {
            if (MusicPlaying.IsPlaying)
            {
                MusicPlaying.Stop();
                GameAudio.SwitchBackToGenericMusic();
            }            
            
            if (VideoFile != null)
            {
                VideoPlaying.Stop();
            }
            if (VideoPlaying != null)
            {
                VideoFile = null;
                while (!VideoPlaying.IsDisposed)
                {
                    VideoPlaying.Dispose();
                }
            }
            VideoPlaying = null;
            TransientContent?.Dispose(ref TransientContent);
        }
    }
}