using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    /// <summary>
    /// Represents a RICH Text Element that can have multiple font effects for
    /// each character:
    ///  - Different Color
    ///  - Different Font
    ///  - Different Font Size
    ///  
    /// However, it can also be just plain old text if you need so
    /// </summary>
    public class PrettyText
    {
        class TextBlock
        {
            public Vector2 Offset;
            public LocalizedText Text;
            public Graphics.Font Font;
            public Color? Color;
        }

        Array<TextBlock> Blocks = new Array<TextBlock>();
        public bool NotEmpty => Blocks.NotEmpty;
        public Vector2 Size { get; private set; }
        Graphics.Font TheFont = Fonts.Arial12Bold;
        UIElementV2 ElemToUpdate;

        public Graphics.Font DefaultFont
        {
            get => TheFont;
            set
            {
                if (TheFont != value)
                {
                    TheFont = value;
                    UpdateSize();
                }
            }
        }

        /// <summary>
        /// Will update the provided element's size IF it has smaller size than the text
        /// </summary>
        public PrettyText(UIElementV2 elemToUpdateSize)
        {
            ElemToUpdate = elemToUpdateSize;
        }

        public PrettyText(UIElementV2 elemToUpdateSize, in LocalizedText text)
        {
            ElemToUpdate = elemToUpdateSize;
            AddText(text);
        }

        public void Clear()
        {
            Blocks.Clear();
        }
        
        // Sets the text to a single block
        public void SetText(in LocalizedText text, Graphics.Font font = null, Color? color = null)
        {
            Clear();
            AddText(text, font, color);
        }

        // Add a rich text element
        public void AddText(in LocalizedText text, Graphics.Font font = null, Color? color = null)
        {
            if (text.IsEmpty)
                return;
            Blocks.Add(new TextBlock{ Text = text, Font = font, Color = color });
            UpdateSize();
        }

        void UpdateSize()
        {
            Vector2 newSize = Vector2.Zero;
            foreach (TextBlock block in Blocks)
            {
                block.Offset.X = newSize.X;

                Graphics.Font font = block.Font ?? TheFont;
                Vector2 size = font.MeasureString(block.Text);
                newSize.X += size.X;
                newSize.Y = Math.Max(Size.Y, size.Y);

                block.Offset.Y = (newSize.Y / 2) - (size.Y / 2);
            }

            Size = newSize;

            if (ElemToUpdate != null)
            {
                // only update size if it's smaller:
                if (ElemToUpdate.Size.X < Size.X) ElemToUpdate.Size.X = Size.X;
                if (ElemToUpdate.Size.Y < Size.Y) ElemToUpdate.Size.Y = Size.Y;
            }
        }

        public void Draw(SpriteBatch batch, Vector2 pos, Color defaultColor, bool shadows)
        {
            foreach (TextBlock block in Blocks)
            {
                string text = block.Text.Text;
                Vector2 blockPos = pos + block.Offset;
                Graphics.Font font = block.Font ?? TheFont;
                Color color = block.Color ?? defaultColor;
                if (shadows)
                    batch.DrawDropShadowText(text, blockPos, font, color);
                else
                    batch.DrawString(font, Text, blockPos, color);
            }
        }

        public LocalizedText Text
        {
            get
            {
                if (Blocks.Count == 0)
                    return "";

                if (Blocks.Count == 1)
                    return Blocks[0].Text;

                var sb = new StringBuilder();
                foreach (TextBlock block in Blocks)
                    sb.Append(block.Text.Text);
                return sb.ToString();
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            
            return base.ToString();
        }
    }
}
