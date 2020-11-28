// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.LightingSystemStatistics
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;

namespace SynapseGaming.LightingSystem.Core
{
    /// <summary>Tracks per-frame lighting and rendering statistics.</summary>
    public class LightingSystemStatistics
    {
        private static bool bool_0;
        private static bool bool_1;
        private static Dictionary<string, LightingSystemStatistic> dictionary_0 = new Dictionary<string, LightingSystemStatistic>(32);
        private static Class16 class16_0 = new Class16();
        private static Color color_0 = new Color(0, 0, 0, 180);
        private static Vector2 vector2_0;
        private const int int_0 = 10;

        /// <summary>Dictionary of all statistics.</summary>
        public static Dictionary<string, LightingSystemStatistic> Statistics => dictionary_0;

        /// <summary>Gets a statistic by name, creating it if necessary.</summary>
        /// <param name="name"></param>
        /// <param name="category">Category assign to the statistic if a new statistic object is created.</param>
        /// <returns></returns>
        public static LightingSystemStatistic GetStatistic(string name, LightingSystemStatisticCategory category)
        {
            LightingSystemStatistic lightingSystemStatistic1;
            if (dictionary_0.TryGetValue(name, out lightingSystemStatistic1))
                return lightingSystemStatistic1;
            LightingSystemStatistic lightingSystemStatistic2 = new LightingSystemStatistic(name, category);
            dictionary_0.Add(name, lightingSystemStatistic2);
            return lightingSystemStatistic2;
        }

        /// <summary>
        /// Ends statistic gathering for this frame and resets the AccumulationValue for all statistics.
        /// </summary>
        public static void CommitChanges()
        {
            if (!bool_0)
                return;
            foreach (KeyValuePair<string, LightingSystemStatistic> keyValuePair in dictionary_0)
                keyValuePair.Value.method_0();
            bool_0 = false;
            bool_1 = false;
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
        /// <param name="totalGameSeconds"></param>
        public static void Render(GraphicsDevice device, LightingSystemStatisticCategory categories, 
            Vector2 screenposition, Vector2 scale, Color color, double totalGameSeconds)
        {
            Vector2 vector2_1 = screenposition;
            SpriteBatch spriteBatch_0 = LightingSystemManager.Instance.method_9(device);
            SpriteFont spriteFont_0 = LightingSystemManager.Instance.ConsoleFont();
            spriteBatch_0.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.SaveState);
            spriteBatch_0.Draw(LightingSystemManager.Instance.EmbeddedTexture("White"), new Rectangle((int)screenposition.X - 10, (int)screenposition.Y - 10, (int)vector2_0.X + 20, (int)vector2_0.Y + 20), color_0);
            class16_0.method_4("FrameRate", totalGameSeconds, !bool_1);
            vector2_0 = class16_0.method_1(spriteBatch_0, spriteFont_0, ref screenposition, scale, color);
            bool_0 = true;
            bool_1 = true;
            foreach (KeyValuePair<string, LightingSystemStatistic> keyValuePair in dictionary_0)
            {
                if ((keyValuePair.Value.Category & categories) != LightingSystemStatisticCategory.None)
                {
                    class16_0.method_2(keyValuePair.Value.Name, keyValuePair.Value.Value);
                    Vector2 vector2_2 = class16_0.method_1(spriteBatch_0, spriteFont_0, ref screenposition, scale, color);
                    vector2_0 = Vector2.Max(vector2_0, vector2_2);
                }
            }
            vector2_0.Y = screenposition.Y - vector2_1.Y;
            spriteBatch_0.End();
        }

        /// <summary>
        /// Returns a string containing the names and values of all requested statistics.
        /// </summary>
        /// <param name="categories">Statistic categories to include.</param>
        /// <param name="totalGameSeconds">Current game time used in frame rate calculation.</param>
        public static string ToString(LightingSystemStatisticCategory categories, double totalGameSeconds)
        {
            string empty = string.Empty;
            class16_0.method_4("FrameRate", totalGameSeconds, !bool_1);
            string str = empty + class16_0 + "\r\n";
            bool_0 = true;
            bool_1 = true;
            foreach (KeyValuePair<string, LightingSystemStatistic> keyValuePair in dictionary_0)
            {
                if ((keyValuePair.Value.Category & categories) != LightingSystemStatisticCategory.None)
                {
                    class16_0.method_2(keyValuePair.Key, keyValuePair.Value.Value);
                    str = str + class16_0 + "\r\n";
                }
            }
            return str;
        }

        /// <summary>
        /// Returns a string containing the names and values of all statistics. Because this method
        /// does not take the current game time the frame rate is likely to be inaccurate.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            bool_0 = true;
            return ToString(LightingSystemStatisticCategory.All, 0.0);
        }

        /// <summary>Writes the requested statistics to a file.</summary>
        /// <param name="filename">Full path including filename of the file to write statistics to.</param>
        /// <param name="categories">Statistic categories to write to the file.</param>
        /// <param name="totalGameSeconds">Current game time used in frame rate calculation.</param>
        public static void SaveToFile(string filename, LightingSystemStatisticCategory categories, double totalGameSeconds)
        {
            File.WriteAllText(filename, ToString(categories, totalGameSeconds));
        }
    }
}
