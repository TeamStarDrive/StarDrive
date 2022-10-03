﻿using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.UI;
using System;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public class UITextBox : UIPanel
    {
        readonly ScrollList<TextBoxItem> ItemsList;
        
        public UITextBox(in RectF rect) : base(rect, Color.TransparentBlack)
        {
            ItemsList = base.Add(new SubmenuScrollList<TextBoxItem>(rect)).List;
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
            AddLine(line, Fonts.Arial12Bold, Color.White);
        }

        public void AddLine(string line, Graphics.Font font, Color color)
        {
            ItemsList.AddItem(new TextBoxItem(line, font, color));
        }

        // Parses and WRAPS textblock into separate lines
        public void AddLines(string textBlock, Graphics.Font font, Color color)
        {
            string[] lines = font.ParseTextToLines(textBlock, ItemsList.ItemsHousing.W);
            foreach (string line in lines)
                AddLine(line, font, color);
        }

        public void AddElement(UIElementV2 element)
        {
            if (element != null)
                ItemsList.AddItem(new TextBoxItem(element));
        }

        class TextBoxItem : ScrollListItem<TextBoxItem>
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
