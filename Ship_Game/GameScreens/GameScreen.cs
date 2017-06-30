using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public abstract class GameScreen : UIElementContainer, IDisposable
    {
        public InputState Input;
        public bool IsLoaded;
        public bool AlwaysUpdate;
        private bool OtherScreenHasFocus;

        public bool IsActive => !OtherScreenHasFocus
                                && ScreenState == ScreenState.TransitionOn 
                                || ScreenState == ScreenState.Active;

        public bool IsExiting { get; protected set; }
        public bool IsPopup   { get; protected set; }

        public Viewport Viewport { get; private set; }
        public ScreenManager ScreenManager { get; internal set; }
        public ScreenState   ScreenState   { get; protected set; }
        public TimeSpan TransitionOffTime { get; protected set; } = TimeSpan.Zero;
        public TimeSpan TransitionOnTime  { get; protected set; } = TimeSpan.Zero;
        public float TransitionPosition   { get; protected set; } = 1f;
        
        public byte TransitionAlpha => (byte)(255f - TransitionPosition * 255f);
        
        protected Texture2D BtnDefault;
        protected Texture2D BtnHovered;
        protected Texture2D BtnPressed;

        public Vector2 MousePos => Input.CursorPosition;
        public int ScreenWidth  => Game1.Instance.RenderWidth;
        public int ScreenHeight => Game1.Instance.RenderHeight;
        public Vector2 ScreenArea => Game1.Instance.RenderArea;
        public GameTime GameTime  => Game1.Instance.GameTime;

        // This should be used for content that gets unloaded once this GameScreen disappears
        public GameContentManager TransientContent;

        protected GameScreen(GameScreen parent) 
            : this(parent, new Rectangle(0, 0, Game1.Instance.RenderWidth, Game1.Instance.RenderHeight))
        {
        }

        protected GameScreen(GameScreen parent, Rectangle rect) : base(rect)
        {
            // hook the content chain to parent screen if possible
            TransientContent = new GameContentManager(parent?.TransientContent ?? Game1.Instance.Content, GetType().Name);
            ScreenManager    = parent?.ScreenManager ?? Game1.Instance.ScreenManager;
            UpdateViewport();
            if (Input == null)
                Input = ScreenManager.input;
        }

        public void UpdateViewport() => Viewport = Game1.Instance.Viewport;

        public void AddObject(ISceneObject so)    => ScreenManager.AddObject(so);
        public void RemoveObject(ISceneObject so) => ScreenManager.RemoveObject(so);
        public void AddLight(ILight light)        => ScreenManager.AddLight(light);
        public void RemoveLight(ILight light)     => ScreenManager.RemoveLight(light);
        public void RefreshLight(ILight light)    => ScreenManager.RefreshLight(light);
        public void RemoveAllLights()             => ScreenManager.RemoveAllLights();
        public void AssignLightRig(string rigContentPath)
        {
            var lightRig = TransientContent.Load<LightRig>(rigContentPath);
            ScreenManager.AssignLightRig(lightRig);
        }


        public virtual void ExitScreen()
        {
            ScreenManager.exitScreenTimer =.024f;
            if (TransitionOffTime != TimeSpan.Zero)
            {
                IsExiting = true;
                return;
            }
            ScreenManager.RemoveScreen(this);
        }


        public virtual void LoadContent()
        {
            BtnDefault = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"];
            BtnHovered = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"];
            BtnPressed = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"];
        }

        public virtual void UnloadContent()
        {
            TransientContent?.Unload();
            Elements.Clear();
            Buttons.Clear();
        }


        public virtual void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            OtherScreenHasFocus = otherScreenHasFocus;
            if (!IsExiting)
            {
                if (coveredByOtherScreen)
                {
                    ScreenState = UpdateTransition(gameTime, TransitionOffTime, 1)
                                ? ScreenState.TransitionOff : ScreenState.Hidden;
                    return;
                }
                ScreenState = UpdateTransition(gameTime, TransitionOnTime, -1)
                            ? ScreenState.TransitionOn : ScreenState.Active;
            }
            else
            {
                ScreenState = ScreenState.TransitionOff;
                if (UpdateTransition(gameTime, TransitionOffTime, 1))
                    return;
                ScreenManager.RemoveScreen(this);
                IsExiting = false;
            }
        }


        private bool UpdateTransition(GameTime gameTime, TimeSpan time, int direction)
        {
            float transitionDelta = (time != TimeSpan.Zero ? (float)(gameTime.ElapsedGameTime.TotalMilliseconds / time.TotalMilliseconds) : 1f);
            TransitionPosition += transitionDelta * direction;
            if (TransitionPosition > 0f && TransitionPosition < 1f)
                return true;

            TransitionPosition = MathHelper.Clamp(TransitionPosition, 0f, 1f);
            return false;
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        // just draws a line, no fancy reprojections
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawLine(Vector2 screenPoint1, Vector2 screenPoint2, Color color, float thickness = 1f)
            => ScreenManager.SpriteBatch.DrawLine(screenPoint1, screenPoint2, color, thickness);

        // just draws a circle, no fancy reprojections
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawCircle(Vector2 posOnScreen, float radius, int sides, Color color, float thickness = 1f)
            => ScreenManager.SpriteBatch.DrawCircle(posOnScreen, radius, sides, color, thickness);

        //Just draws a given rectangle with a color fill
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawRectangle(Rectangle rectangle, Color edgeColor, Color fillColor)
        {
            ScreenManager.SpriteBatch.FillRectangle(rectangle, fillColor);
            DrawRectangle(rectangle, edgeColor);               
        }

        //Just draws a given rectangle
        public void DrawRectangle(Rectangle rectangle, Color edgeColor)
            => ScreenManager.SpriteBatch.DrawRectangle(rectangle, edgeColor);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawRectangle(Vector2 center, Vector2 size, float rotation, Color color, float thickness = 1f)
            => ScreenManager.SpriteBatch.DrawRectangle(center, size, rotation, color, thickness);



        // just draws a texture to screen, no fancy reprojections, where screenPos is the texture CENTER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTexture(Texture2D texture, Vector2 posOnScreen, float scale, float rotation, Color color)
            => ScreenManager.SpriteBatch.Draw(texture, posOnScreen, null, color, rotation, texture.Center(), scale, SpriteEffects.None, 1f);

        public void DrawTexture(Texture2D texture, Vector2 posOnScreen, Color color)
            => ScreenManager.SpriteBatch.Draw(texture, posOnScreen, color);

        // just draws a texture to screen, no fancy reprojections, where screenPos is the texture CENTER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTextureSized(Texture2D texture, Vector2 posOnScreen, float rotation, float width, float height, Color color)
        {
            var rect = new Rectangle((int)posOnScreen.X, (int)posOnScreen.Y, (int)width, (int)height);
            ScreenManager.SpriteBatch.Draw(texture, rect, null, color, rotation, texture.Center(), SpriteEffects.None, 1f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTextureRect(Texture2D texture, Vector2 posOnScreen, Color color, float rotation = 0f)
            => DrawTextureRect(texture, posOnScreen, color, rotation, Vector2.Zero);

        // just draws a texture to screen, no fancy reprojections, where screenPos is the texture top left.
        public void DrawTextureRect(Texture2D texture, Vector2 posOnScreen, Color color, float rotation , Vector2 origin )
        {
            ScreenManager.SpriteBatch.Draw(texture, posOnScreen, null, color, rotation, Vector2.Zero, 0.5f, SpriteEffects.None, 1f);
        }

        // just draws a texture to screen, no fancy reprojections, where screenPos is the rectangle top left and texture exists in it
        public void DrawTextureRect(Texture2D texture, Vector2 posOnScreen, Rectangle? sourceRectangle, Color color, Vector2 origin, float rotation = 0f, float scale = 0, SpriteEffects effects = SpriteEffects.None, float layerDepth = 0f)
        {
            ScreenManager.SpriteBatch.Draw(texture,  posOnScreen, sourceRectangle, color, rotation, origin, scale, effects, layerDepth);
        }

        public void CheckToolTip(int toolTipId, Rectangle rectangle, Vector2 mousePos)
        {
            if (rectangle.HitTest(mousePos))
                ToolTip.CreateTooltip(toolTipId);                
        }

        public void CheckToolTip(string text, Rectangle rectangle, Vector2 mousePos)
        {
            if (rectangle.HitTest(mousePos))
                ToolTip.CreateTooltip(text);
        }
        public void CheckToolTip(string text, Vector2 cursor, string words, string numbers, SpriteFont font, Vector2 mousePos)
        {
            var rect = new Rectangle((int)cursor.X, (int)cursor.Y
                , (int)font.MeasureString(words).X + (int)font.MeasureString(numbers).X
                , font.LineSpacing);
            CheckToolTip(text, rect, mousePos);
        }
        public void CheckToolTip(int toolTipId, Vector2 cursor, string words, string numbers, SpriteFont font, Vector2 mousePos)
        {
            var rect = new Rectangle((int)cursor.X, (int)cursor.Y
                , (int)font.MeasureString(words).X + (int)font.MeasureString(numbers).X
                , font.LineSpacing);
            CheckToolTip(toolTipId, rect, mousePos);
        }

        public Vector2 FontSpace(Vector2 cursor, float spacing, string drawnString, SpriteFont font)
        {
            cursor.X += (spacing - font.MeasureString(drawnString).X);
            return cursor;
        }
        public Vector2 FontBackSpace(Vector2 cursor, float spacing, string drawnString, SpriteFont font)
        {
            cursor.X -= (spacing - font.MeasureString(drawnString).X);
            return cursor;
        }
        public void DrawString(Vector2 posOnScreen, float rotation, float textScale, Color textColor, string text)
        {
            Vector2 size = Fonts.Arial11Bold.MeasureString(text);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial11Bold, text, posOnScreen, textColor, rotation, size * 0.5f, textScale, SpriteEffects.None, 1f);
        }
        public void DrawString(Vector2 posOnScreen, Color textColor, string text, SpriteFont font, float rotation = 0f, float textScale = 1f)
        {
            Vector2 size = font.MeasureString(text);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial11Bold, text, posOnScreen, textColor, rotation, size * 0.5f, textScale, SpriteEffects.None, 1f);
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


        public void DrawModelMesh(Model model, Matrix world, Matrix view, Vector3 diffuseColor,Matrix projection, Texture2D projTex, float alpha =0f, bool textureEnabled = true, bool lightingEnabled = false)
        {
            foreach (ModelMesh modelMesh in model.Meshes)
            {
                foreach (Effect effect in modelMesh.Effects)
                {
                    var basicEffect = effect as BasicEffect;
                    if (basicEffect == null) continue;
                    basicEffect.World           = Matrix.CreateScale(50f) * world;
                    basicEffect.View            = view;
                    basicEffect.DiffuseColor    = new Vector3(1f, 1f, 1f);
                    basicEffect.Texture         = projTex;
                    basicEffect.Alpha           = alpha > 0 ? alpha : basicEffect.Alpha;                    
                    basicEffect.TextureEnabled  = true;
                    basicEffect.Projection      = projection;
                    basicEffect.LightingEnabled = lightingEnabled;
                }
                modelMesh.Draw();
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
            TransientContent?.Dispose(ref TransientContent);
        }
    }
}