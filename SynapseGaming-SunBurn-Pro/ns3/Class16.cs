// Decompiled with JetBrains decompiler
// Type: ns3.Class16
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ns3
{
  internal class Class16
  {
    private static StringBuilder stringBuilder_0 = new StringBuilder();
    private static int int_0;
    private static float float_0;
    private static double double_0;

    private void method_0(StringBuilder stringBuilder_1, int int_1, int int_2)
    {
      if (int_1 <= 0)
      {
        stringBuilder_1.Append("0");
      }
      else
      {
        int length = stringBuilder_1.Length;
        int num1 = 0;
        int num2;
        for (; int_1 > 0; int_1 = num2)
        {
          num2 = int_1 / 10;
          switch (int_1 - num2 * 10)
          {
            case 0:
              stringBuilder_1.Insert(length, "0");
              break;
            case 1:
              stringBuilder_1.Insert(length, "1");
              break;
            case 2:
              stringBuilder_1.Insert(length, "2");
              break;
            case 3:
              stringBuilder_1.Insert(length, "3");
              break;
            case 4:
              stringBuilder_1.Insert(length, "4");
              break;
            case 5:
              stringBuilder_1.Insert(length, "5");
              break;
            case 6:
              stringBuilder_1.Insert(length, "6");
              break;
            case 7:
              stringBuilder_1.Insert(length, "7");
              break;
            case 8:
              stringBuilder_1.Insert(length, "8");
              break;
            case 9:
              stringBuilder_1.Insert(length, "9");
              break;
          }
          ++num1;
          if (num1 == int_2 && num1 != 0 && int_1 > 0)
            stringBuilder_1.Insert(length, ".");
        }
        if (num1 >= int_2)
          return;
        for (int index = num1; index < int_2; ++index)
          stringBuilder_1.Insert(length, "0");
        stringBuilder_1.Insert(length, ".");
      }
    }

    public Vector2 method_1(SpriteBatch spriteBatch_0, SpriteFont spriteFont_0, ref Vector2 vector2_0, Vector2 vector2_1, Color color_0)
    {
      spriteBatch_0.DrawString(spriteFont_0, stringBuilder_0, vector2_0, color_0, 0.0f, Vector2.Zero, vector2_1, SpriteEffects.None, 0.0f);
      vector2_0.Y = vector2_0.Y + 15f * vector2_1.Y;
      return spriteFont_0.MeasureString(stringBuilder_0) * vector2_1;
    }

    public void method_2(string string_0, int int_1)
    {
      stringBuilder_0.Length = 0;
      stringBuilder_0.Append(string_0);
      stringBuilder_0.Append(": ");
      this.method_0(stringBuilder_0, int_1, 0);
    }

    private void method_3(string string_0, float float_1)
    {
      stringBuilder_0.Length = 0;
      stringBuilder_0.Append(string_0);
      stringBuilder_0.Append(": ");
      this.method_0(stringBuilder_0, (int) (float_1 * 100.0), 2);
    }

    public void method_4(string string_0, double totalGameSeconds, bool bool_0)
    {
      if (totalGameSeconds - double_0 >= 0.5)
      {
        float_0 = int_0 / (float) (totalGameSeconds - double_0);
        int_0 = 0;
        double_0 = totalGameSeconds;
      }
      this.method_3(string_0, float_0);
      if (!bool_0)
        return;
      ++int_0;
    }

    public override string ToString()
    {
      return stringBuilder_0.ToString();
    }
  }
}
