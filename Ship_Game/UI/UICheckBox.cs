using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq.Expressions;

namespace Ship_Game
{
    using BoolExpression = Expression<Func<bool>>;

    public sealed class UICheckBox : UIElementV2
    {
        readonly SpriteFont Font;
        readonly string Text;
        readonly string TipText;
        Ref<bool> Binding;

        Vector2 TextPos;
        Vector2 CheckPos;

        public Action<UICheckBox> OnChange;

        public bool Checked => Binding.Value;
        public override string ToString() => $"Checkbox {ElementDescr} Text=\"{Text}\" Checked={Checked}";

        public UICheckBox(UIElementV2 parent, float x, float y, Ref<bool> binding, SpriteFont font, string title, string tooltip)
            : base(parent, new Vector2(x,y))
        {
            Binding = binding;
            Font    = font;
            Text    = title;
            TipText = tooltip;
            PerformLayout();
        }

        public UICheckBox(BoolExpression binding, SpriteFont font, string title, string tooltip)
        {
            Binding = new Ref<bool>(binding);
            Font    = font;
            Text    = title;
            TipText = tooltip;
            PerformLayout();
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

        public void Bind(BoolExpression binding)
        {
            Binding = new Ref<bool>(binding);
        }

        public override void Draw(SpriteBatch batch)
        {
            var checkRect = new Rectangle((int)CheckPos.X, (int)CenterY - 6, 10, 12);
            batch.DrawRectangle(checkRect, new Color(96, 81, 49));
            //batch.DrawRectangle(Rect, Color.Red); // DEBUG

            if (Text.NotEmpty())
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

            if (!TipText.IsEmpty())
                ToolTip.CreateTooltip(TipText);

            if (input.LeftMouseClick)
            {
                Binding.Value = !Binding.Value;
                OnChange?.Invoke(this);
            }

            // always capture input to prevent clicks from reaching elements under us
            return true;
        }

        public override void PerformLayout()
        {
            RequiresLayout = false;
            Pos.X = (int)Pos.X;
            Pos.Y = (int)Pos.Y;
            int h = Math.Max(10, Font.LineSpacing);
            int th = Font.LineSpacing / 2;
            Size = new Vector2(h + Font.TextWidth(Text), h+1);
            TextPos  = new Vector2(Pos.X + 25, (int)CenterY - th);
            CheckPos = new Vector2(Pos.X + 6 - Font.TextWidth("x") / 2,
                                   Pos.Y + 5 - th);
        }
    }
}