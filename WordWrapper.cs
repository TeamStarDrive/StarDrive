using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;
using System.Text;

public static class WordWrapper
{
	private static StringBuilder builder;

	public static char[] NewLine;

	static WordWrapper()
	{
		WordWrapper.builder = new StringBuilder(" ");
		WordWrapper.NewLine = new char[] { '\r', '\n' };
	}

	private static Vector2 MeasureCharacter(this SpriteFont font, char character)
	{
		WordWrapper.builder[0] = character;
		return font.MeasureString(WordWrapper.builder);
	}

    public static void WrapWord(StringBuilder original, StringBuilder target, SpriteFont font, Rectangle bounds, float scale)
    {
        int index1 = 0;
        float num1 = 0.0f;
        float num2 = 0.0f;
        for (int index2 = 0; index2 < original.Length; ++index2)
        {
            char ch = original[index2];
            float num3 = WordWrapper.MeasureCharacter(font, ch).X * scale;
            num1 += num3;
            num2 += num3;
            if ((int)ch != 13 && (int)ch != 10)
            {
                if ((double)num1 > (double)bounds.Width)
                {
                    if (char.IsWhiteSpace(ch))
                    {
                        target.Insert(index2, WordWrapper.NewLine);
                        num1 = 0.0f;
                        num2 = 0.0f;
                        continue;
                    }
                    else
                    {
                        target.Insert(index1, WordWrapper.NewLine);
                        target.Remove(index1 + WordWrapper.NewLine.Length, 1);
                        num1 = num2;
                        num2 = 0.0f;
                    }
                }
                else if (char.IsWhiteSpace(ch))
                {
                    index1 = target.Length;
                    num2 = 0.0f;
                }
            }
            else
            {
                num2 = 0.0f;
                num1 = 0.0f;
            }
            target.Append(ch);
        }
    }
}