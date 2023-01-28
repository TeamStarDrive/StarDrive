using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.UI;
using System;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public class UITextBox : UIPanel
    {
        readonly Graphics.Font DefaultFont;
        public readonly ScrollList<TextBoxItem> ItemsList;
        
        public UITextBox(in RectF rect, bool useBorder = true, Graphics.Font defaultFont = null)
            : base(rect, Color.TransparentBlack)
        {
            DefaultFont = defaultFont ?? Fonts.Arial12Bold;

            if (useBorder)
                ItemsList = base.Add(new SubmenuScrollList<TextBoxItem>(rect)).List;
            else
                ItemsList = base.Add(new ScrollList<TextBoxItem>(rect));
            
            // set local pos, so that ScrollList moves along with UITextBox
            ItemsList.SetLocalPos(LocalPos.Zero);
            ItemsList.EnableItemEvents = false;
        }

        public bool EnableTextBoxDebug
        {
            get => ItemsList.DebugDrawScrollList;
            set
            {
                ItemsList.DebugDrawScrollList = value;
                ItemsList.EnableItemHighlight = value;
            }
        }

        public Rectangle ItemsRect => ItemsList.ItemsHousing;

        public void Clear()
        {
            ItemsList.Reset();
        }

        public void AddLine(string line)
        {
            AddLine(line, DefaultFont, Color.White);
        }

        public void AddLine(string line, Graphics.Font font, Color color)
        {
            ItemsList.AddItem(new(line, font ?? DefaultFont, color));
        }

        // Parses and WRAPS textblock into separate lines
        public void AddLines(string textBlock, Graphics.Font font, Color color)
        {
            font ??= DefaultFont;
            string[] lines = font.ParseTextToLines(textBlock, ItemsList.ItemsHousing.W);
            foreach (string line in lines)
                AddLine(line, font, color);
        }

        public void AddElement(UIElementV2 element)
        {
            if (element != null)
                ItemsList.AddItem(new TextBoxItem(element));
        }

        // Clears the textbox and sets new textblock lines, using DefaultFont and Color.White
        public void SetLines(string textBlock)
        {
            Clear();
            AddLines(textBlock, DefaultFont, Color.White);
        }

        // Clears the textbox and sets new textblock lines
        public void SetLines(string textBlock, Graphics.Font font, Color color)
        {
            Clear();
            AddLines(textBlock, font, color);
        }

        public sealed class TextBoxItem : ScrollListItem<TextBoxItem>
        {
            readonly UIElementV2 Elem;
            public override int ItemHeight => (int)Math.Round(Elem.Height);
            public TextBoxItem(string line, Graphics.Font font, Color color)
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
                Elem.PerformLayout();
                RequiresLayout = false;
            }
            public override void Update(float fixedDeltaTime)
            {
                Elem.Update(fixedDeltaTime);
                base.Update(fixedDeltaTime);
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                Elem.Draw(batch, elapsed);
            }
        }
    }
}
