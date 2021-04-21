using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game.Data.Yaml;
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

        public int TextWidth(in LocalizedText text)
        {
            return (int)XnaFont.MeasureString(text.Text).X;
        }

        public Vector2 MeasureString(in LocalizedText text)
        {
            return XnaFont.MeasureString(text.Text);
        }

        public string ParseText(in LocalizedText text, float maxLineWidth)
        {
            return ParseText(text.Text, maxLineWidth);
        }

        static char[] NewLineString = new char[]{ '\n' };
        static char[] TabString = new char[]{ ' ', ' ', ' ', ' ' }; // 4 spaces

        static StringView NextTextToken(ref StringView view)
        {
            int start = view.Start;
            int current = start;
            int eos = start + view.Length;
            char[] chars = view.Chars;

            while (current < eos)
            {
                switch (chars[current]) // is delimiter?
                {
                    case ' ':
                        if (start == current)
                        {
                            view.Skip(1);
                            ++start;
                            break;
                        }
                        goto get_word;
                    case '\n':
                        if (start == current)
                        {
                            view.Skip(1);
                            return new StringView(NewLineString);
                        }
                        goto get_word;
                    case '\t':
                        if (start == current)
                        {
                            view.Skip(1);
                            return new StringView(TabString);
                        }
                        goto get_word;
                    case '\\':
                        // TODO: THIS IS LEGACY DEPRECATED "\\n" string support!
                        //       CAN BE REMOVED WHEN ALL LOC IS CONVERTED TO YAML
                        if ((current+1) < eos && 'n' == chars[current+1])
                        {
                            view.Skip(2);
                            return new StringView(NewLineString);
                        }
                        break;
                }
                ++current;
            }

            get_word:
            int length = current - start;
            view.Skip(length);
            return new StringView(chars, start, length);
        }

        static Array<StringView> TokenizeText(string text, out int approxLen)
        {
            var textView = new StringView(text.ToCharArray());
            var words = new Array<StringView>();
            int len = 0;
            while (textView.Length > 0)
            {
                StringView word = NextTextToken(ref textView);
                if (word.Length > 0)
                {
                    len += word.Length + 1;
                    words.Add(word);
                }
            }
            approxLen = len;
            return words;
        }

        public string ParseText(string text, float maxLineWidth)
        {
            Array<StringView> words = TokenizeText(text, out int approxLen);
            var result = new StringBuilder(approxLen);
            var tmp = new StringBuilder(32);

            float lineLength = 0f;
            for (int i = 0; i < words.Count; ++i)
            {
                StringView word = words[i];
                if (word.Length == 1 && word.Char0 == '\n')
                {
                    result.Append('\n');
                    lineLength = 0f;
                    continue;
                }

                tmp.Clear();
                tmp.Append(word.Chars, word.Start, word.Length);
                float wordLength = XnaFont.MeasureString(tmp).X;
                float newLength = lineLength + wordLength;

                // not the first char in line? Word is not WS and prev word is not WS?
                bool prependSpace = false;
                if (lineLength > 0f && (word.Char0 != ' ' && result[result.Length-1] != ' '))
                {
                    prependSpace = true;
                    newLength += SpaceWidth;
                }

                if (newLength > maxLineWidth) // wrap this word to next line
                {
                    result.Append('\n');
                    lineLength = wordLength;
                    result.Append(word.Chars, word.Start, word.Length);
                }
                else // append text, and prepend space if needed
                {
                    if (prependSpace)
                        result.Append(' ');
                    lineLength = newLength;
                    result.Append(word.Chars, word.Start, word.Length);
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
