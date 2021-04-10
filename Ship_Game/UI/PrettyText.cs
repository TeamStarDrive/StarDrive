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
            public float OffsetX;
            public LocalizedText Text;
            public SpriteFont Font;
            public Color? Color;
        }

        Array<TextBlock> Blocks = new Array<TextBlock>();
        public bool NotEmpty => Blocks.NotEmpty;
        public Vector2 Size { get; private set; }
        SpriteFont TheFont = Fonts.Arial12Bold;

        public SpriteFont DefaultFont
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

        public void Clear()
        {
            Blocks.Clear();
        }
        
        // Sets the text to a single block
        public void SetText(in LocalizedText text, SpriteFont font = null, Color? color = null)
        {
            Clear();
            AddText(text, font, color);
        }

        // Add a rich text element
        public void AddText(in LocalizedText text, SpriteFont font = null, Color? color = null)
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
                block.OffsetX = newSize.X;

                SpriteFont font = block.Font ?? TheFont;
                Vector2 size = font.MeasureString(block.Text);
                newSize.X += size.X;
                newSize.Y = Math.Max(Size.Y, size.Y);
            }
            Size = newSize;
        }

        public void Draw(SpriteBatch batch, Vector2 pos, Color defaultColor, bool shadows)
        {
            foreach (TextBlock block in Blocks)
            {
                string text = block.Text.Text;
                var blockPos = new Vector2(pos.X + block.OffsetX, pos.Y);
                SpriteFont font = block.Font ?? TheFont;
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
