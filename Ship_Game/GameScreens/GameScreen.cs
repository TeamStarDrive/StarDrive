using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public abstract class GameScreen : IDisposable
    {
        public bool IsLoaded;
        public bool AlwaysUpdate;
        private bool OtherScreenHasFocus;
        protected readonly Array<UIButton> Buttons = new Array<UIButton>();
        protected Texture2D BtnDefault;
        protected Texture2D BtnHovered;
        protected Texture2D BtnPressed;

        public bool IsActive => !OtherScreenHasFocus
                                && ScreenState == ScreenState.TransitionOn 
                                || ScreenState == ScreenState.Active;

        public bool IsExiting { get; protected set; }
        public bool IsPopup   { get; protected set; }

        public ScreenManager ScreenManager { get; internal set; }
        public ScreenState   ScreenState   { get; protected set; }
        public TimeSpan TransitionOffTime { get; protected set; } = TimeSpan.Zero;
        public TimeSpan TransitionOnTime  { get; protected set; } = TimeSpan.Zero;
        public float TransitionPosition   { get; protected set; } = 1f;

        public byte TransitionAlpha => (byte)(255f - TransitionPosition * 255f);


        // This should be used for content that gets unloaded once this GameScreen disappears
        public GameContentManager TransientContent;

        protected GameScreen(GameScreen parent)
        {
            // hook the content chain to parent screen if possible
            TransientContent = new GameContentManager(parent?.TransientContent ?? Game1.Instance.Content, GetType().Name);
            ScreenManager    = parent?.ScreenManager ?? Game1.Instance.ScreenManager;
        }

        public abstract void Draw(GameTime gameTime);

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

        public virtual void HandleInput(InputState input)
        {
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

        // Shared utility functions:
        protected UIButton Button(ref Vector2 pos, string launches, int localization)
        {
            return Button(ref pos, launches, Localizer.Token(localization));
        }

        protected UIButton Button(ref Vector2 pos, string launches, string text)
        {
            var button = new UIButton
            {
                NormalTexture  = BtnDefault,
                HoverTexture   = BtnHovered,
                PressedTexture = BtnPressed,
                Launches       = launches,
                Text           = text
            };
            Layout(ref pos, button);
            Buttons.Add(button);
            return button;
        }

        protected void Layout(ref Vector2 pos, UIButton button)
        {
            button.Rect = new Rectangle((int)pos.X, (int)pos.Y, BtnDefault.Width, BtnDefault.Height);
            pos.Y += BtnDefault.Height + 15;
        }


        // just draws a line, no fancy reprojections
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawLine(Vector2 screenPoint1, Vector2 screenPoint2, Color color, float thickness = 1f)
            => Primitives2D.DrawLine(ScreenManager.SpriteBatch, screenPoint1, screenPoint2, color, thickness);

        // just draws a circle, no fancy reprojections
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawCircle(Vector2 posOnScreen, float radius, int sides, Color color, float thickness = 1f)
            => Primitives2D.DrawCircle(ScreenManager.SpriteBatch, posOnScreen, radius, sides, color, thickness);

        //Just draws a given rectangle with a color fill
        public void DrawRectangle(Rectangle rectangle, Color edgeColor, Color fillColor)
        {            
            Primitives2D.FillRectangle(ScreenManager.SpriteBatch, rectangle, fillColor);
            DrawRectangle(rectangle, edgeColor);            
        }

        //Just draws a given rectangle
        public void DrawRectangle(Rectangle rectangle, Color edgeColor)
            => Primitives2D.DrawRectangle(ScreenManager.SpriteBatch, rectangle, edgeColor);        

        // just draws a texture to screen, no fancy reprojections, where screenPos is the texture CENTER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTexture(Texture2D texture, Vector2 posOnScreen, float scale, float rotation, Color color)
            => ScreenManager.SpriteBatch.Draw(texture, posOnScreen, null, color, rotation, texture.Center(), scale, SpriteEffects.None, 1f);

        // just draws a texture to screen, no fancy reprojections, where screenPos is the texture CENTER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTextureSized(Texture2D texture, Vector2 posOnScreen, float rotation, float width, float height, Color color)
        {
            var rect = new Rectangle((int)posOnScreen.X, (int)posOnScreen.Y, (int)width, (int)height);
            ScreenManager.SpriteBatch.Draw(texture, rect, null, color, rotation, texture.Center(), SpriteEffects.None, 1f);
        }
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
            if (HelperFunctions.CheckIntersection(rectangle, mousePos))
            {
                ToolTip.CreateTooltip(toolTipId, ScreenManager);                
            }
        }

        public void CheckToolTip(string text, Rectangle rectangle, Vector2 mousePos)
        {
            if (HelperFunctions.CheckIntersection(rectangle, mousePos))
            {
                ToolTip.CreateTooltip(text, ScreenManager);
            }
        }

        public void DrawString(Vector2 posOnScreen, float rotation, float textScale, Color textColor, string text)
        {
            Vector2 size = Fonts.Arial11Bold.MeasureString(text);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial11Bold, text, posOnScreen, textColor, rotation, size * 0.5f, textScale, SpriteEffects.None, 1f);
        }


        public void DrawModelMesh(Model model, Matrix world, Matrix view, Vector3 diffuseColor,Matrix projection, Texture2D projTex, float alpha =0f, bool textureEnabled = true, bool LightingEnabled = false)
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
                    basicEffect.LightingEnabled = false;
                }
                modelMesh.Draw();
            }
        }

        ~GameScreen() { Dispose(false); }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            TransientContent?.Dispose(ref TransientContent);
        }
    }
}