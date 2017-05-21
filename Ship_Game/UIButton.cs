using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    // Refactored by RedFox
    public sealed class UIButton
    {
        public enum PressState
        {
            Default, Hover, Pressed
        }
        public Rectangle Rect;
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
        public bool Active = true;

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

        private void DrawButtonText(SpriteBatch spriteBatch, Rectangle r, bool enabled, bool bold = true)
        {
            SpriteFont font = bold ? Fonts.Arial12Bold : Fonts.Arial12;

            Vector2 textCursor;
            textCursor.X = r.X + r.Width  / 2 - font.MeasureString(Text).X / 2f;
            textCursor.Y = r.Y + r.Height / 2 - font.LineSpacing / 2;
            if (State == PressState.Pressed)
                textCursor.Y += 1f; // pressed down effect

            spriteBatch.DrawString(font, Text, textCursor, enabled ? TextColor() : Color.Gray);
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle r)
        {
            if (!Active) return;
            spriteBatch.Draw(ButtonTexture(), r, Color.White);
            DrawButtonText(spriteBatch, r, enabled:true);
        }

        public void Draw(SpriteBatch spriteBatch, bool enabled = true)
        {
            if (!Active) return;
            spriteBatch.Draw(ButtonTexture(), Rect, Color.White);
            DrawButtonText(spriteBatch, Rect, enabled);
        }

        public void DrawLight(SpriteBatch spriteBatch)
        {
            if (!Active) return;
            spriteBatch.Draw(ButtonTexture(), Rect, Color.White);
            DrawButtonText(spriteBatch, Rect, enabled:true, bold:false);
        }
    }
}
