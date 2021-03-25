using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    public class UITextBox : UIPanel
    {
        readonly ScrollList2<TextBoxItem> TextBox;

        public UITextBox(in Rectangle rect) : base(rect, Color.TransparentBlack)
        {
            TextBox = Add(new ScrollList2<TextBoxItem>(rect));
        }

        public UITextBox(Submenu background) : base(background.Rect, Color.TransparentBlack)
        {
            TextBox = Add(new ScrollList2<TextBoxItem>(background));
        }

        public void Clear()
        {
            TextBox.Reset();
        }

        public void AddLine(string line)
        {
            AddLine(line, Fonts.Arial12Bold, Color.White);
        }

        public void AddLine(string line, SpriteFont font, Color color)
        {
            TextBox.AddItem(new TextBoxItem(line, font, color));
        }

        // Parses a textblock into separate lines
        public void AddLines(string textBlock, SpriteFont font, Color color)
        {
            string[] lines = font.ParseTextToLines(textBlock, Width);
            foreach (string line in lines)
                AddLine(line, font, color);
        }

        public void AddElement(UIElementV2 element)
        {
            TextBox.AddItem(new TextBoxItem(element));
        }

        class TextBoxItem : ScrollListItem<TextBoxItem>
        {
            readonly UIElementV2 Elem;
            public override int ItemHeight => (int)Math.Round(Elem.Height);
            public TextBoxItem(string line, SpriteFont font, Color color)
            {
                Elem = new UILabel(line, font, color);
            }
            public TextBoxItem(UIElementV2 element)
            {
                Elem = element;
            }
            public override void PerformLayout()
            {
                Elem.Pos = Pos;
                RequiresLayout = false;
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                Elem?.Draw(batch, elapsed);
            }
        }
    }
}
