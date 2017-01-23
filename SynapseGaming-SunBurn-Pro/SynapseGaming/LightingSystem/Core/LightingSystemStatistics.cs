// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.LightingSystemStatistics
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using System.Collections.Generic;
using System.IO;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>Tracks per-frame lighting and rendering statistics.</summary>
  public class LightingSystemStatistics
  {
    private static bool bool_0 = false;
    private static bool bool_1 = false;
    private static Dictionary<string, LightingSystemStatistic> dictionary_0 = new Dictionary<string, LightingSystemStatistic>(32);
    private static Class16 class16_0 = new Class16();
    private static Color color_0 = new Color((byte) 0, (byte) 0, (byte) 0, (byte) 180);
    private static Vector2 vector2_0 = new Vector2();
    private const int int_0 = 10;

    /// <summary>Dictionary of all statistics.</summary>
    public static Dictionary<string, LightingSystemStatistic> Statistics
    {
      get
      {
        return LightingSystemStatistics.dictionary_0;
      }
    }

    /// <summary>Gets a statistic by name, creating it if necessary.</summary>
    /// <param name="name"></param>
    /// <param name="category">Category assign to the statistic if a new statistic object is created.</param>
    /// <returns></returns>
    public static LightingSystemStatistic GetStatistic(string name, LightingSystemStatisticCategory category)
    {
      LightingSystemStatistic lightingSystemStatistic1;
      if (LightingSystemStatistics.dictionary_0.TryGetValue(name, out lightingSystemStatistic1))
        return lightingSystemStatistic1;
      LightingSystemStatistic lightingSystemStatistic2 = new LightingSystemStatistic(name, category);
      LightingSystemStatistics.dictionary_0.Add(name, lightingSystemStatistic2);
      return lightingSystemStatistic2;
    }

    /// <summary>
    /// Ends statistic gathering for this frame and resets the AccumulationValue for all statistics.
    /// </summary>
    public static void CommitChanges()
    {
      if (!LightingSystemStatistics.bool_0)
        return;
      foreach (KeyValuePair<string, LightingSystemStatistic> keyValuePair in LightingSystemStatistics.dictionary_0)
        keyValuePair.Value.method_0();
      LightingSystemStatistics.bool_0 = false;
      LightingSystemStatistics.bool_1 = false;
    }

    /// <summary>
    /// Renders stats to the screen. This can be slow on some hardware, rendering several
    /// categories when trying to capture the frame rate is not recommended.
    /// </summary>
    /// <param name="device"></param>
    /// <param name="categories">The statistic categories to render.</param>
    /// <param name="screenposition">Upper left corner to begin rendering.</param>
    /// <param name="scale">Text scale.</param>
    /// <param name="color">Text color.</param>
    /// <param name="gametime"></param>
    public static void Render(GraphicsDevice device, LightingSystemStatisticCategory categories, Vector2 screenposition, Vector2 scale, Color color, GameTime gametime)
    {
      Vector2 vector2_1 = screenposition;
      SpriteBatch spriteBatch_0 = LightingSystemManager.Instance.method_9(device);
      SpriteFont spriteFont_0 = LightingSystemManager.Instance.method_8();
      spriteBatch_0.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.SaveState);
      spriteBatch_0.Draw(LightingSystemManager.Instance.method_2("White"), new Rectangle((int) screenposition.X - 10, (int) screenposition.Y - 10, (int) LightingSystemStatistics.vector2_0.X + 20, (int) LightingSystemStatistics.vector2_0.Y + 20), LightingSystemStatistics.color_0);
      LightingSystemStatistics.class16_0.method_4("FrameRate", gametime, !LightingSystemStatistics.bool_1);
      LightingSystemStatistics.vector2_0 = LightingSystemStatistics.class16_0.method_1(spriteBatch_0, spriteFont_0, ref screenposition, scale, color);
      LightingSystemStatistics.bool_0 = true;
      LightingSystemStatistics.bool_1 = true;
      foreach (KeyValuePair<string, LightingSystemStatistic> keyValuePair in LightingSystemStatistics.dictionary_0)
      {
        if ((keyValuePair.Value.Category & categories) != LightingSystemStatisticCategory.None)
        {
          LightingSystemStatistics.class16_0.method_2(keyValuePair.Value.Name, keyValuePair.Value.Value);
          Vector2 vector2_2 = LightingSystemStatistics.class16_0.method_1(spriteBatch_0, spriteFont_0, ref screenposition, scale, color);
          LightingSystemStatistics.vector2_0 = Vector2.Max(LightingSystemStatistics.vector2_0, vector2_2);
        }
      }
      LightingSystemStatistics.vector2_0.Y = screenposition.Y - vector2_1.Y;
      spriteBatch_0.End();
    }

    /// <summary>
    /// Returns a string containing the names and values of all requested statistics.
    /// </summary>
    /// <param name="categories">Statistic categories to include.</param>
    /// <param name="gametime">Current game time used in frame rate calculation.</param>
    public static string ToString(LightingSystemStatisticCategory categories, GameTime gametime)
    {
      string empty = string.Empty;
      LightingSystemStatistics.class16_0.method_4("FrameRate", gametime, !LightingSystemStatistics.bool_1);
      string str = empty + LightingSystemStatistics.class16_0.ToString() + "\r\n";
      LightingSystemStatistics.bool_0 = true;
      LightingSystemStatistics.bool_1 = true;
      foreach (KeyValuePair<string, LightingSystemStatistic> keyValuePair in LightingSystemStatistics.dictionary_0)
      {
        if ((keyValuePair.Value.Category & categories) != LightingSystemStatisticCategory.None)
        {
          LightingSystemStatistics.class16_0.method_2(keyValuePair.Key, keyValuePair.Value.Value);
          str = str + LightingSystemStatistics.class16_0.ToString() + "\r\n";
        }
      }
      return str;
    }

    /// <summary>
    /// Returns a string containing the names and values of all statistics. Because this method
    /// does not take the current game time the frame rate is likely to be inaccurate.
    /// </summary>
    /// <returns></returns>
    public static string ToString()
    {
      LightingSystemStatistics.bool_0 = true;
      return LightingSystemStatistics.ToString(LightingSystemStatisticCategory.All, new GameTime());
    }

    /// <summary>Writes the requested statistics to a file.</summary>
    /// <param name="filename">Full path including filename of the file to write statistics to.</param>
    /// <param name="categories">Statistic categories to write to the file.</param>
    /// <param name="gametime">Current game time used in frame rate calculation.</param>
    public static void SaveToFile(string filename, LightingSystemStatisticCategory categories, GameTime gametime)
    {
      File.WriteAllText(filename, LightingSystemStatistics.ToString(categories, gametime));
    }
  }
}
