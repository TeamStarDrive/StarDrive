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
        readonly LocalizedText Text;
        readonly LocalizedText Tooltip;
        Ref<bool> Binding;

        Vector2 TextPos;
        Vector2 CheckPos;

        public Action<UICheckBox> OnChange;
        public Color TextColor = Color.White;

        public bool Checked => Binding.Value;
        public override string ToString() => $"{TypeName} {ElementDescr} Text={Text} Checked={Checked}";

        public UICheckBox(float x, float y, Ref<bool> binding, SpriteFont font,
                          in LocalizedText title, in LocalizedText tooltip)
        {
            Pos = new Vector2(x, y);
            Binding = binding;
            Font    = font;
            Text    = title;
            Tooltip = tooltip;
            PerformLayout();
        }

        public UICheckBox(BoolExpression binding, SpriteFont font,
                          in LocalizedText title, in LocalizedText tooltip)
        {
            Binding = new Ref<bool>(binding);
            Font    = font;
            Text    = title;
            Tooltip = tooltip;
            PerformLayout();
        }

        public UICheckBox(float x, float y, BoolExpression binding, SpriteFont font,
                          in LocalizedText title, in LocalizedText tooltip)
            : this(x, y, new Ref<bool>(binding), font, title, tooltip)
        {
        }

        public UICheckBox(float x, float y, Func<bool> getter, Action<bool> setter, SpriteFont font,
                          in LocalizedText title, in LocalizedText tooltip)
            : this(x, y, new Ref<bool>(getter, setter), font, title, tooltip)
        {
        }


        public void Bind(BoolExpression binding)
        {
            Binding = new Ref<bool>(binding);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            var checkRect = new Rectangle((int)CheckPos.X, (int)CenterY - 6, 10, 12);
            batch.DrawRectangle(checkRect, new Color(96, 81, 49));
            //batch.DrawRectangle(Rect, Color.Red); // DEBUG

            if (Text.NotEmpty)
                batch.DrawString(Font, Text, TextPos, TextColor);

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
            {
                Binding.Value = !Binding.Value;
                OnChange?.Invoke(this);
            }
            else if (Tooltip.IsValid)
            {
                ToolTip.CreateTooltip(Tooltip);
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
            Size = new Vector2(h + Font.MeasureString(Text).X, h+1);
            TextPos  = new Vector2(Pos.X + 25, (int)CenterY - th);
            CheckPos = new Vector2(Pos.X + 6 - Font.TextWidth("x") / 2,
                                   Pos.Y + 5 - th);
        }
    }
}