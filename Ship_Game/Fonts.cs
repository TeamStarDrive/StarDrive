using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ship_Game
{
	internal static class Fonts
	{
		private static SpriteFont arial10;

		private static SpriteFont visitor10;

		private static SpriteFont visitor12;

		private static SpriteFont pirulen12;

		private static SpriteFont pirulen16;

		private static SpriteFont pirulen20;

		private static SpriteFont verdana12Bold;

		private static SpriteFont verdana14Bold;

		private static SpriteFont verdana12;

		private static SpriteFont arial12;

		private static SpriteFont tahomaBold9;

        private static SpriteFont arial11Bold;

		private static SpriteFont arial12Bold;

		private static SpriteFont arial14Bold;

		private static SpriteFont arial8Bold;

		private static SpriteFont arial20Bold;

		private static SpriteFont laserian14;

		//private static SpriteFont stratum14;
        //used but never set, will eliminate all references to avoid null pointer exception

		//private static SpriteFont stratum24;

		private static SpriteFont stratum72;

		private static SpriteFont consolas18;

		private static SpriteFont tahoma10;

		private static SpriteFont tahoma11;

		private static SpriteFont corbel14;

		public readonly static Color CountColor;

		public readonly static Color TitleColor;

		public readonly static Color CaptionColor;

		public readonly static Color HighlightColor;

		public readonly static Color DisplayColor;

		public readonly static Color DescriptionColor;

		public readonly static Color RestrictionColor;

		public readonly static Color ModifierColor;

		public readonly static Color MenuSelectedColor;

		public static SpriteFont Arial10
		{
			get
			{
				return Fonts.arial10;
			}
		}

        public static SpriteFont Arial11Bold
        {
            get
            {
                return Fonts.arial11Bold;
            }
        }

		public static SpriteFont Arial12
		{
			get
			{
				return Fonts.arial12;
			}
		}

		public static SpriteFont Arial12Bold
		{
			get
			{
				return Fonts.arial12Bold;
			}
		}

		public static SpriteFont Arial14Bold
		{
			get
			{
				return Fonts.arial14Bold;
			}
		}

		public static SpriteFont Arial20Bold
		{
			get
			{
				return Fonts.arial20Bold;
			}
		}

		public static SpriteFont Arial8Bold
		{
			get
			{
				return Fonts.arial8Bold;
			}
		}

		public static SpriteFont Consolas18
		{
			get
			{
				return Fonts.consolas18;
			}
		}

		public static SpriteFont Corbel14
		{
			get
			{
				return Fonts.corbel14;
			}
		}

		public static SpriteFont Laserian14
		{
			get
			{
				return Fonts.laserian14;
			}
		}

		public static SpriteFont Pirulen12
		{
			get
			{
				return Fonts.pirulen12;
			}
		}

		public static SpriteFont Pirulen16
		{
			get
			{
				return Fonts.pirulen16;
			}
		}

		public static SpriteFont Pirulen20
		{
			get
			{
				return Fonts.pirulen20;
			}
		}

		/*public static SpriteFont Stratum14
		{
			get
			{
				return Fonts.stratum14;
			}
		}

		public static SpriteFont Stratum24
		{
			get
			{
				return Fonts.stratum24;
			}
		}*/

		public static SpriteFont Stratum72
		{
			get
			{
				return Fonts.stratum72;
			}
		}

		public static SpriteFont Tahoma10
		{
			get
			{
				return Fonts.tahoma10;
			}
		}

		public static SpriteFont Tahoma11
		{
			get
			{
				return Fonts.tahoma11;
			}
		}

		public static SpriteFont TahomaBold9
		{
			get
			{
				return Fonts.tahomaBold9;
			}
		}

		public static SpriteFont Verdana12
		{
			get
			{
				return Fonts.verdana12;
			}
		}

		public static SpriteFont Verdana12Bold
		{
			get
			{
				return Fonts.verdana12Bold;
			}
		}

		public static SpriteFont Verdana14Bold
		{
			get
			{
				return Fonts.verdana14Bold;
			}
		}

		public static SpriteFont Visitor10
		{
			get
			{
				return Fonts.visitor10;
			}
		}

		public static SpriteFont Visitor12
		{
			get
			{
				return Fonts.visitor12;
			}
		}

		static Fonts()
		{
			Fonts.CountColor = new Color(79, 24, 44);
			Fonts.TitleColor = new Color(59, 18, 6);
			Fonts.CaptionColor = new Color(228, 168, 57);
			Fonts.HighlightColor = new Color(223, 206, 148);
			Fonts.DisplayColor = new Color(68, 32, 19);
			Fonts.DescriptionColor = new Color(0, 0, 0);
			Fonts.RestrictionColor = new Color(0, 0, 0);
			Fonts.ModifierColor = new Color(0, 0, 0);
			Fonts.MenuSelectedColor = new Color(248, 218, 127);
		}

		public static string BreakTextIntoLines(string text, int maximumCharactersPerLine, int maximumLines)
		{
			if (string.IsNullOrEmpty(text))
			{
				return string.Empty;
			}
			if (text.Length < maximumCharactersPerLine)
			{
				return text;
			}
			StringBuilder stringBuilder = new StringBuilder(text);
			int currentLine = 0;
			int newLineIndex = 0;
			while (text.Length - newLineIndex > maximumCharactersPerLine && currentLine < maximumLines)
			{
				text.IndexOf(' ', 0);
				for (int nextIndex = newLineIndex; nextIndex >= 0 && nextIndex < maximumCharactersPerLine; nextIndex = text.IndexOf(' ', newLineIndex + 1))
				{
					newLineIndex = nextIndex;
				}
				stringBuilder.Replace(' ', '\n', newLineIndex, 1);
				currentLine++;
			}
			return stringBuilder.ToString();
		}

		public static string BreakTextIntoLines(string text, int maximumCharactersPerLine)
		{
			if (string.IsNullOrEmpty(text))
			{
				return string.Empty;
			}
			if (text.Length < maximumCharactersPerLine)
			{
				return text;
			}
			StringBuilder stringBuilder = new StringBuilder(text);
			int currentLine = 0;
			int newLineIndex = 0;
			while (text.Length - newLineIndex > maximumCharactersPerLine)
			{
				text.IndexOf(' ', 0);
				for (int nextIndex = newLineIndex; nextIndex >= 0 && nextIndex < maximumCharactersPerLine; nextIndex = text.IndexOf(' ', newLineIndex + 1))
				{
					newLineIndex = nextIndex;
				}
				stringBuilder.Replace(' ', '\n', newLineIndex, 1);
				currentLine++;
			}
			return stringBuilder.ToString();
		}

		public static List<string> BreakTextIntoList(string text, SpriteFont font, int rowWidth)
		{
			List<string> lines = new List<string>();
			if (string.IsNullOrEmpty("text"))
			{
				lines.Add(string.Empty);
				return lines;
			}
			if (font.MeasureString(text).X <= (float)rowWidth)
			{
				lines.Add(text);
				return lines;
			}
			string[] words = text.Split(new char[] { ' ' });
			int currentWord = 0;
			while (currentWord < (int)words.Length)
			{
				int wordsThisLine = 0;
				string line = string.Empty;
				while (currentWord < (int)words.Length)
				{
					string testLine = line;
					if (testLine.Length >= 1)
					{
						testLine = (testLine[testLine.Length - 1] == '.' || testLine[testLine.Length - 1] == '?' || testLine[testLine.Length - 1] == '!' ? string.Concat(testLine, "  ", words[currentWord]) : string.Concat(testLine, " ", words[currentWord]));
					}
					else
					{
						testLine = string.Concat(testLine, words[currentWord]);
					}
					if (wordsThisLine > 0 && font.MeasureString(testLine).X > (float)rowWidth)
					{
						break;
					}
					line = testLine;
					wordsThisLine++;
					currentWord++;
				}
				lines.Add(line);
			}
			return lines;
		}

		public static void DrawCenteredText(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position, Color color)
		{
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			Vector2 textSize = font.MeasureString(text);
			Vector2 centeredPosition = new Vector2(position.X - (float)((int)textSize.X / 2), position.Y - (float)((int)textSize.Y / 2));
			spriteBatch.DrawString(font, text, centeredPosition, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f - position.Y / 720f);
		}

		public static string GetGoldString(int gold)
		{
			return string.Format("{0:n0}", gold);
		}

		public static void LoadContent(ContentManager contentManager)
		{
			Fonts.arial20Bold = contentManager.Load<SpriteFont>("Fonts/Arial20Bold");
			SpriteFont lineSpacing = Fonts.arial20Bold;
			lineSpacing.LineSpacing = lineSpacing.LineSpacing - 3;
			Fonts.arial14Bold = contentManager.Load<SpriteFont>("Fonts/Arial14Bold");
			Fonts.arial12Bold = contentManager.Load<SpriteFont>("Fonts/Arial12Bold");
			SpriteFont spriteFont = Fonts.arial12Bold;
			spriteFont.LineSpacing = spriteFont.LineSpacing - 2;
            Fonts.arial11Bold = contentManager.Load<SpriteFont>("Fonts/Arial11Bold");
			Fonts.arial10 = contentManager.Load<SpriteFont>("Fonts/Arial10");
			SpriteFont lineSpacing1 = Fonts.arial10;
			lineSpacing1.LineSpacing = lineSpacing1.LineSpacing - 2;
			Fonts.arial8Bold = contentManager.Load<SpriteFont>("Fonts/Arial8Bold");
			Fonts.arial12 = contentManager.Load<SpriteFont>("Fonts/Arial12");
			SpriteFont spriteFont1 = Fonts.arial12;
			spriteFont1.LineSpacing = spriteFont1.LineSpacing - 2;
			Fonts.stratum72 = contentManager.Load<SpriteFont>("Fonts/stratum72");
			Fonts.stratum72.Spacing = 1f;
			Fonts.corbel14 = contentManager.Load<SpriteFont>("Fonts/Corbel14");
			Fonts.laserian14 = contentManager.Load<SpriteFont>("Fonts/Laserian14");
			Fonts.pirulen16 = contentManager.Load<SpriteFont>("Fonts/Pirulen16");
			if (GlobalStats.Config.Language == "Russian" || GlobalStats.Config.Language == "Polish")
			{
				Fonts.pirulen12 = contentManager.Load<SpriteFont>("Fonts/Pirulen12");
				if (GlobalStats.Config.Language == "Russian")
				{
					SpriteFont spacing = Fonts.pirulen12;
					spacing.Spacing = spacing.Spacing - 3f;
				}
			}
			else
			{
				Fonts.pirulen12 = contentManager.Load<SpriteFont>("Fonts/Pirulen12a");
			}
			Fonts.pirulen20 = contentManager.Load<SpriteFont>("Fonts/Pirulen20");
			Fonts.consolas18 = contentManager.Load<SpriteFont>("Fonts/consolas18");
			SpriteFont lineSpacing2 = Fonts.consolas18;
			lineSpacing2.LineSpacing = lineSpacing2.LineSpacing - 4;
			SpriteFont spacing1 = Fonts.consolas18;
			spacing1.Spacing = spacing1.Spacing - 2f;
			Fonts.tahoma10 = contentManager.Load<SpriteFont>("Fonts/Tahoma10");
			Fonts.tahoma11 = contentManager.Load<SpriteFont>("Fonts/Tahoma11");
			Fonts.tahomaBold9 = contentManager.Load<SpriteFont>("Fonts/TahomaBold9");
			Fonts.visitor10 = contentManager.Load<SpriteFont>("Fonts/Visitor10");
			Fonts.visitor10.Spacing = 1f;
			Fonts.visitor12 = contentManager.Load<SpriteFont>("Fonts/Visitor12");
			Fonts.visitor12.Spacing = 1f;
			Fonts.verdana14Bold = contentManager.Load<SpriteFont>("Fonts/Verdana14Bold");
			Fonts.verdana12 = contentManager.Load<SpriteFont>("Fonts/Verdana12");
			Fonts.verdana12Bold = contentManager.Load<SpriteFont>("Fonts/Verdana12Bold");
		}

		public static void UnloadContent()
		{
		}
	}
}