using System;
using System.Linq.Expressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    using BoolExpression = Expression<Func<bool>>;

    public sealed class UICheckBox : UIElementV2
    {
        private readonly SpriteFont Font;
        private readonly string Text;
        private readonly string TipText;
        private readonly Ref<bool> Binding;

        private readonly Vector2 TextPos;
        private readonly Vector2 CheckPos;

        private static Rectangle CreateRect(float x, float y, SpriteFont font)
        {
            return new Rectangle((int)x, (int)y + (font.LineSpacing + 6) / 2 - 5, 10, 10);
        }

        public UICheckBox(float x, float y, Ref<bool> binding, SpriteFont font, string title, string tooltip)
            : base(CreateRect(x, y, font))
        {
            Binding = binding;
            Font    = font;
            Text    = title;
            TipText = tooltip;
            TextPos  = new Vector2(Rect.X + 15, Rect.Y + Rect.Height / 2 - font.LineSpacing / 2);
            CheckPos = new Vector2(Rect.X + 5 - font.MeasureString("x").X / 2f, 
                                   Rect.Y + 4 - font.LineSpacing / 2);
        }

        public UICheckBox(float x, float y, BoolExpression binding, SpriteFont font, string title, string tooltip)
            : this(x, y, new Ref<bool>(binding), font, title, tooltip)
        {
        }
        public UICheckBox(float x, float y, BoolExpression binding, SpriteFont font, string title, int tooltip)
            : this(x, y, new Ref<bool>(binding), font, title, Localizer.Token(tooltip))
        {
        }
        public UICheckBox(float x, float y, BoolExpression binding, SpriteFont font, int title, int tooltip)
            : this(x, y, new Ref<bool>(binding), font, Localizer.Token(title), Localizer.Token(tooltip))
        {
        }
        public UICheckBox(float x, float y, Func<bool> getter, Action<bool> setter, SpriteFont font, string title, int tooltip)
            : this(x, y, new Ref<bool>(getter, setter), font, title, Localizer.Token(tooltip))
        {
        }
        public UICheckBox(float x, float y, Func<bool> getter, Action<bool> setter, SpriteFont font, int title, int tooltip)
            : this(x, y, new Ref<bool>(getter, setter), font, Localizer.Token(title), Localizer.Token(tooltip))
        {
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Rect.HitTest(Mouse.GetState().Pos()) && !TipText.IsEmpty())
            {
                ToolTip.CreateTooltip(TipText);
            }
            spriteBatch.DrawRectangle(Rect, new Color(96, 81, 49));
            spriteBatch.DrawString(Font, Text, TextPos, Color.White);
            if (Binding.Value)
            {
                spriteBatch.DrawString(Fonts.Arial12Bold, "x", CheckPos, Color.White);
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (Rect.HitTest(input.CursorPosition) && input.LeftMouseClick)
            {
                Binding.Value = !Binding.Value;
                return true;
            }
            return false;
        }

        public override string ToString() => $"Checkbox '{Text}' value:{Binding.Value}";
    }
}