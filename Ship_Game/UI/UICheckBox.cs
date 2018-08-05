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

        private Vector2 TextPos;
        private Vector2 CheckPos;

        public UICheckBox(UIElementV2 parent, float x, float y, Ref<bool> binding, SpriteFont font, string title, string tooltip)
            : base(parent, new Vector2(x,y))
        {
            Binding = binding;
            Font    = font;
            Text    = title;
            TipText = tooltip;
            PerformLegacyLayout(Pos);
        }

        public UICheckBox(UIElementV2 parent, float x, float y, BoolExpression binding, SpriteFont font, string title, string tooltip)
            : this(parent, x, y, new Ref<bool>(binding), font, title, tooltip)
        {
        }
        public UICheckBox(UIElementV2 parent, float x, float y, BoolExpression binding, SpriteFont font, string title, int tooltip)
            : this(parent, x, y, new Ref<bool>(binding), font, title, Localizer.Token(tooltip))
        {
        }
        public UICheckBox(UIElementV2 parent, float x, float y, BoolExpression binding, SpriteFont font, int title, int tooltip)
            : this(parent, x, y, new Ref<bool>(binding), font, Localizer.Token(title), Localizer.Token(tooltip))
        {
        }
        public UICheckBox(UIElementV2 parent, float x, float y, Func<bool> getter, Action<bool> setter, SpriteFont font, string title, int tooltip)
            : this(parent, x, y, new Ref<bool>(getter, setter), font, title, Localizer.Token(tooltip))
        {
        }
        public UICheckBox(UIElementV2 parent, float x, float y, Func<bool> getter, Action<bool> setter, SpriteFont font, int title, int tooltip)
            : this(parent, x, y, new Ref<bool>(getter, setter), font, Localizer.Token(title), Localizer.Token(tooltip))
        {
        }

        public override void Draw(SpriteBatch batch)
        {
            if (Rect.HitTest(Mouse.GetState().Pos()) && !TipText.IsEmpty())
            {
                ToolTip.CreateTooltip(TipText);
            }
            batch.DrawRectangle(Rect, new Color(96, 81, 49));
            batch.DrawString(Font, Text, TextPos, Color.White);
            if (Binding.Value)
            {
                batch.DrawString(Fonts.Arial12Bold, "x", CheckPos, Color.White);
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (!Rect.HitTest(input.CursorPosition))
                return false;

            if (input.LeftMouseClick)
                Binding.Value = !Binding.Value;

            // always capture input to prevent clicks from reaching elements under us
            return true;
        }

        private void PerformLegacyLayout(Vector2 pos)
        {
            int offset = (Font.LineSpacing + 6) / 2 - 5;
            Rect = new Rectangle((int)pos.X, (int)pos.Y + offset, 10, 10);
            RequiresLayout = false;
            Update();
        }

        public override void Update()
        {
            if (!Visible)
                return;
            base.Update();
            TextPos  = new Vector2(Rect.X + 15, Rect.Y + Rect.Height / 2 - Font.LineSpacing / 2);
            CheckPos = new Vector2(Rect.X + 5 - Font.MeasureString("x").X / 2f, 
                                   Rect.Y + 4 - Font.LineSpacing / 2);
        }

        public override string ToString() => $"Checkbox '{Text}' value:{Binding.Value}";
    }
}