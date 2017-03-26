using Microsoft.Xna.Framework;
using System;
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

        public Vector3 ViewOrigin(Vector2 origin, Matrix projection, Matrix view, float zAxis =0f)
        {
            return ScreenManager.GraphicsDevice.Viewport.Project(origin.ToVec3(zAxis), projection, view, Matrix.Identity);            
        }

        public Vector3 ViewDestination(Vector2 origin, Matrix projection, Matrix view, float zAxis = 0f)
        {
            return ScreenManager.GraphicsDevice.Viewport.Project(origin.ToVec3(zAxis), projection, view, Matrix.Identity);
	    }

        public void DrawLineBase(Vector2 origin, Vector2 destination, Matrix projection, Matrix view, Color color,  float zAxis = 0f)
        {
            Primitives2D.DrawLine(ScreenManager.SpriteBatch, ViewOrigin(origin,projection,view).ToVec2(), ViewDestination(destination, projection, view).ToVec2(), color);            
        }
    }
}