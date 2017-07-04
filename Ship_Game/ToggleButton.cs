using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    public sealed class ToggleButton
    {
        public Rectangle Rect;

        public object ReferenceObject;

        public string Action = "";

        public bool Active;

        public bool Hover;
        

       // private readonly string IconPath;

        public int WhichToolTip;

        public bool HasToolTip;

        public Color BaseColor = Color.White;

        private bool Pressed;

        private readonly Texture2D PressTexture;
        private readonly Texture2D HoverTexture;
        private readonly Texture2D ActiveTexture;
        private readonly Texture2D InactiveTexture;
        private readonly Texture2D IconTexture;
        private readonly Vector2 WordPos;
        private readonly string IconPath;
        private readonly Texture2D IconActive;
        //private readonly Texture2D Icon;
        private readonly Rectangle IconRect;

        public ToggleButton(Rectangle r, string activePath, string inactivePath, string hoverPath, string pressPath, string iconPath)
        {           
            Rect            = r;
            PressTexture    = ResourceManager.Texture(pressPath);
            HoverTexture    = ResourceManager.Texture(hoverPath);
            ActiveTexture   = ResourceManager.Texture(activePath);
            InactiveTexture = ResourceManager.Texture(inactivePath);                        
            IconTexture = ResourceManager.Texture(iconPath, false);
            IconActive = ResourceManager.Texture(string.Concat(iconPath, "_active"));
            //Icon = ResourceManager.Texture(IconPath);
            if (IconTexture == null)
            {
                IconPath = iconPath;
                WordPos = new Vector2(Rect.X + 12 - Fonts.Arial12Bold.MeasureString(IconPath).X / 2f, Rect.Y + 12 - Fonts.Arial12Bold.LineSpacing / 2);
            }
            else
                IconRect = new Rectangle(Rect.X + Rect.Width / 2 - IconTexture.Width / 2
                    , Rect.Y + Rect.Height / 2 - IconTexture.Height / 2
                    , IconTexture.Width, IconTexture.Height);
        }

        public void Draw(ScreenManager screenManager, bool resizeIcon = false)
        {
            Rectangle iconRect = resizeIcon ? IconRect : Rect;

            if (Pressed)
                screenManager.SpriteBatch.Draw(PressTexture, Rect, Color.White);
            else if (Hover)
                screenManager.SpriteBatch.Draw(HoverTexture, Rect, Color.White);
            else if (Active)
                screenManager.SpriteBatch.Draw(ActiveTexture, Rect, Color.White);
            else if (!Active)
                screenManager.SpriteBatch.Draw(InactiveTexture, Rect, Color.White);
            if (IconTexture == null)
            {
                if (Active)
                {
                    screenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, IconPath, WordPos, Color.White);
                    return;
                }

                screenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, IconPath, WordPos, Color.Gray);
            }
            else
            {
                if (Active && !resizeIcon)
                {
                    screenManager.SpriteBatch.Draw(IconActive, Rect, Color.White);
                    return;
                }
                screenManager.SpriteBatch.Draw(IconTexture, iconRect, Color.White);			    
            }
        }

        public void DrawIconResized(ScreenManager screenManager) => Draw(screenManager, true);


        public bool HandleInput(InputState input)
        {
            Pressed = false;
            if (!Rect.HitTest(input.CursorPosition))
                Hover = false;
            else
            {
                if (!Hover)
                    GameAudio.PlaySfxAsync("sd_ui_mouseover");
                Hover = true;
                if (input.MouseCurr.LeftButton == ButtonState.Pressed)
                    Pressed = true;
                if (input.InGameSelect)
                    return true;
            }
            return false;
        }
    }
}