using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq.Expressions;

namespace Ship_Game
{
    using BoolExpression = Expression<Func<bool>>;

    public sealed class UICheckBox : UIElementV2
    {
        public readonly Graphics.Font Font;
        public readonly LocalizedText Text;
        public readonly LocalizedText Tooltip;
        Ref<bool> Binding;

        public Action<UICheckBox> OnChange;
        public Color TextColor = Color.White;
        public Color CheckedTextColor = Color.White;

        int TextPadding = 4;
        int CheckBoxSize = 12;

        public bool Checked => Binding.Value;
        public override string ToString() => $"{TypeName} {ElementDescr} Text={Text} Checked={Checked}";

        public UICheckBox(float x, float y, Ref<bool> binding, Graphics.Font font,
                          in LocalizedText title, in LocalizedText tooltip)
        {
            Pos = new Vector2(x, y);
            Binding = binding;
            Font    = font;
            Text    = title;
            Tooltip = tooltip;
            PerformLayout();
        }

        public UICheckBox(BoolExpression binding, Graphics.Font font,
                          in LocalizedText title, in LocalizedText tooltip)
        {
            Binding = new Ref<bool>(binding);
            Font    = font;
            Text    = title;
            Tooltip = tooltip;
            PerformLayout();
        }

        public UICheckBox(float x, float y, BoolExpression binding, Graphics.Font font,
                          in LocalizedText title, in LocalizedText tooltip)
            : this(x, y, new Ref<bool>(binding), font, title, tooltip)
        {
        }

        public UICheckBox(float x, float y, Func<bool> getter, Action<bool> setter, Graphics.Font font,
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
            var checkBox = new Rectangle((int)Pos.X, (int)CenterY - CheckBoxSize/2, CheckBoxSize, CheckBoxSize);
            batch.DrawRectangle(checkBox, new Color(96, 81, 49));
            //batch.DrawRectangle(Rect, Color.Red); // DEBUG

            if (Text.NotEmpty)
            {
                var textPos = new Vector2(checkBox.X + CheckBoxSize + TextPadding, (int)CenterY - Font.LineSpacing / 2);
                batch.DrawString(Font, Text, textPos, Binding.Value ? CheckedTextColor : TextColor);
            }

            if (Binding.Value)
            {
                var check = ResourceManager.Texture("NewUI/Checkmark10x");
                var checkMark = checkBox.Bevel(-1);
                batch.Draw(check, checkMark, Color.White);
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
            int h = Math.Max(CheckBoxSize, Font.LineSpacing);
            Size = new Vector2(CheckBoxSize + TextPadding + Font.TextWidth(Text), h);
        }
    }
}