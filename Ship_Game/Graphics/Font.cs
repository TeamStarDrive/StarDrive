using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Graphics
{
    public class Font
    {
        public string Name { get; private set; }
        public readonly SpriteFont XnaFont;
        public readonly int LineSpacing;
        public readonly float SpaceWidth; // width of a single whitespace " " character in fractions of a pixel
        public int NumCharacters => XnaFont.Characters.Count;

        public Font(GameContentManager content, string name)
        {
            Name = name;
            XnaFont = content.Load<SpriteFont>("Fonts/" + name);

            LineSpacing = XnaFont.LineSpacing;
            SpaceWidth = XnaFont.MeasureString(" ").X;
        }

        public Font(GameContentManager content, string name, float monoSpaceSpacing)
        {
            Name = name;
            XnaFont = content.Load<SpriteFont>("Fonts/" + name);
            XnaFont.Spacing = monoSpaceSpacing;

            LineSpacing = XnaFont.LineSpacing;
            SpaceWidth = XnaFont.MeasureString(" ").X;
        }

        public Vector2 MeasureString(string text)
        {
            return XnaFont.MeasureString(text);
        }

        public Vector2 MeasureString(StringBuilder text)
        {
            return XnaFont.MeasureString(text);
        }
        
        public Vector2 MeasureLines(Array<string> lines)
        {
            var size = new Vector2();
            foreach (string line in lines)
            {
                size.X = Math.Max(size.X, XnaFont.MeasureString(line).X);
                size.Y += XnaFont.LineSpacing + 2;
            }
            return size;
        }

        public int TextWidth(string text)
        {
            return (int)XnaFont.MeasureString(text).X;
        }

        public int TextWidth(int localizationId)
        {
            return (int)XnaFont.MeasureString(Localizer.Token(localizationId)).X;
        }

        public Vector2 MeasureString(in LocalizedText text)
        {
            return XnaFont.MeasureString(text.Text);
        }

        public string ParseText(in LocalizedText text, float maxLineWidth)
        {
            return ParseText(text.Text, maxLineWidth);
        }

        public string ParseText(string text, float maxLineWidth)
        {
            string[] words = text.Split(' ');
            return ParseText(words, maxLineWidth);
        }

        string ParseText(string[] words, float maxLineWidth)
        {
            var result = new StringBuilder();
            float lineLength = 0.0f;
            foreach (string w in words)
            {
                string word = w;
                if (word == "\\n" || word == "\n")
                {
                    result.Append('\n');
                    lineLength = 0f;
                    continue;
                }

                word = word.Replace("\t", "    ");

                float wordLength = XnaFont.MeasureString(word).X;
                if ((lineLength + wordLength) > maxLineWidth) // wrap this word to next line
                {
                    result.Append('\n');
                    lineLength = wordLength;
                    result.Append(word);
                }
                else
                {
                    if (result.Length != 0)
                    {
                        lineLength += SpaceWidth;
                        result.Append(' ');
                    }
                    lineLength += wordLength;
                    result.Append(word);
                }
            }
            return result.ToString();
        }

        public string[] ParseTextToLines(string text, float maxLineWidth)
        {
            string parsed = ParseText(text, maxLineWidth);
            string[] lines = parsed.Split('\n');
            return lines;
        }
    }
}
