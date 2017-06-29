using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    // Refactored by RedFox
    public sealed class UIButton : IElement
    {
        public enum PressState
        {
            Default, Hover, Pressed
        }
        public Rectangle Rect { get; set; }
        public PressState State;
        public Texture2D NormalTexture;
        public Texture2D HoverTexture;
        public Texture2D PressedTexture;
        public string Text     = "";
        public string Launches = "";
        public readonly Color DefaultColor = new Color(255, 240, 189);
        public readonly Color HoverColor   = new Color(255, 240, 189);
        public readonly Color PressColor   = new Color(255, 240, 189);
        public int ToolTip;

        public bool Visible = true;
        public bool Enabled = false; // if false, button will be visible, but gray and not interactive

        public string ClickSfx = "echo_affirm";

        public delegate void ClickHandler(UIButton button);
        public event ClickHandler OnClick;

        // Automatic layout of the UI element
        public void Layout(Vector2 pos)
        {
            Rect = new Rectangle((int)pos.X, (int)pos.Y, NormalTexture.Width, NormalTexture.Height);
        }
        public void Layout(int x, int y)
        {
            Rect = new Rectangle(x, y, NormalTexture.Width, NormalTexture.Height);
        }

        private Texture2D ButtonTexture()
        {
            switch (State)
            {
                default:                 return NormalTexture;
                case PressState.Hover:   return HoverTexture;
                case PressState.Pressed: return PressedTexture;
            }
        }

        private Color TextColor()
        {
            switch (State)
            {
                default:                 return DefaultColor;
                case PressState.Hover:   return HoverColor;
                case PressState.Pressed: return PressColor;
            }
        }

        private void DrawButtonText(SpriteBatch spriteBatch, Rectangle r)
        {
            SpriteFont font = Fonts.Arial12Bold;

            Vector2 textCursor;
            textCursor.X = r.X + r.Width  / 2 - font.MeasureString(Text).X / 2f;
            textCursor.Y = r.Y + r.Height / 2 - font.LineSpacing / 2;
            if (State == PressState.Pressed)
                textCursor.Y += 1f; // pressed down effect

            spriteBatch.DrawString(font, Text, textCursor, Enabled ? TextColor() : Color.Gray);
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle r)
        {
            if (!Visible)
                return;
            spriteBatch.Draw(ButtonTexture(), r, Color.White);
            DrawButtonText(spriteBatch, r);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible)
                return;
            spriteBatch.Draw(ButtonTexture(), Rect, Color.White);
            DrawButtonText(spriteBatch, Rect);
        }

        public bool HandleInput(InputState input)
        {
            if (!Visible)
                return false;

            if (!Rect.HitTest(input.MouseScreenPos))
            {
                State = PressState.Default;
                return false;
            }

            if (State != PressState.Hover && State != PressState.Pressed)
                GameAudio.PlaySfxAsync("mouse_over4");

            if (State == PressState.Pressed && input.LeftMouseReleased)
            {
                State = PressState.Hover;
                OnClick?.Invoke(this);
                if (ClickSfx.NotEmpty())
                    GameAudio.PlaySfxAsync(ClickSfx);
                return true;
            }

            if (input.LeftMouseHeldDown)
            {
                State = PressState.Pressed;
                return true;
            }

            State = PressState.Hover;
            return false;
        }
    }
}
