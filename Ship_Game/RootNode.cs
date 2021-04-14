using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
	public sealed class RootNode : Node
	{
		public Graphics.Font TitleFont = Fonts.Visitor10;

		public NodeState nodeState;

		public string TechName;

		public Rectangle TitleRect;

		public Rectangle RootRect = new Rectangle(0, 0, 129, 76);
        public Vector2 RightPoint => new Vector2(RootRect.Right - 10,
                                                 RootRect.CenterY());
		private Rectangle IconRect;


		public RootNode(Vector2 position, TechEntry entry)
		{
			if (GlobalStats.IsRussian)
			{
				TitleFont = Fonts.Arial10;
			}
			Entry = entry;
			TechName = Localizer.Token(ResourceManager.TechTree[entry.UID].NameIndex);
			RootRect.X = (int)position.X;
			RootRect.Y = (int)position.Y;
			IconRect = new Rectangle(RootRect.X + RootRect.Width / 2 - 39, RootRect.Y + RootRect.Height / 2 - 29, 78, 58);
			TitleRect = new Rectangle(RootRect.X + 2, RootRect.Y - 21, 108, 61);
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			string text;
			string[] textarray;
			Vector2 TextPos;
			int line;
			switch (nodeState)
			{
				case NodeState.Normal:
				{
					spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/techroot_title"), TitleRect, Color.White);
					text = TitleFont.ParseText(TechName, 93f);
					textarray = Regex.Split(text, "\n");
					TextPos = new Vector2(TitleRect.X + 10 + 46 - TitleFont.MeasureString(text).X / 2f, TitleRect.Y + 7 + 12 - TitleFont.MeasureString(text).Y / 2f);
					line = 0;
					string[] strArrays = textarray;
					for (int i = 0; i < strArrays.Length; i++)
					{
						string word = strArrays[i];
						Vector2 newPos = new Vector2(TitleRect.X + 10 + 46 - TitleFont.MeasureString(word).X / 2f, TextPos.Y + line * TitleFont.LineSpacing);
						newPos = new Vector2((int)newPos.X, (int)newPos.Y);
						spriteBatch.DrawString(TitleFont, word, newPos, new Color(131, 147, 172));
						line++;
					}
					spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/techroot_body"), RootRect, Color.White);
					spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/"+ResourceManager.TechTree[Entry.UID].IconPath), IconRect, Color.White);
					return;
				}
				case NodeState.Hover:
				{
					spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/techroot_underglow_selhoverpress"), RootRect, Color.White);
					spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/techroot_title_selhoverpress"), TitleRect, Color.White);
					text = TitleFont.ParseText(TechName, 93f);
					textarray = Regex.Split(text, "\n");
					TextPos = new Vector2(TitleRect.X + 10 + 46 - TitleFont.MeasureString(text).X / 2f, TitleRect.Y + 7 + 12 - TitleFont.MeasureString(text).Y / 2f);
					line = 0;
					string[] strArrays1 = textarray;
					for (int j = 0; j < strArrays1.Length; j++)
					{
						string word = strArrays1[j];
						Vector2 newPos = new Vector2(TitleRect.X + 10 + 46 - TitleFont.MeasureString(word).X / 2f, TextPos.Y + line * TitleFont.LineSpacing);
						newPos = new Vector2((int)newPos.X, (int)newPos.Y);
						spriteBatch.DrawString(TitleFont, word, newPos, new Color(163, 198, 236));
						line++;
					}
					spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/techroot_body_selhover"), RootRect, Color.White);
					spriteBatch.Draw(ResourceManager.Texture(string.Concat("ResearchMenu/", ResourceManager.TechTree[Entry.UID].IconPath, "_hover")), IconRect, Color.White);
					return;
				}
				case NodeState.Press:
				{
					spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/techroot_underglow_selhoverpress"), RootRect, Color.White);
					spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/techroot_title_selhoverpress"), TitleRect, Color.White);
					text = TitleFont.ParseText(TechName, 93f);
					textarray = Regex.Split(text, "\n");
					TextPos = new Vector2(TitleRect.X + 10 + 46 - TitleFont.MeasureString(text).X / 2f, TitleRect.Y + 7 + 12 - TitleFont.MeasureString(text).Y / 2f);
					line = 0;
					string[] strArrays2 = textarray;
					for (int k = 0; k < strArrays2.Length; k++)
					{
						string word = strArrays2[k];
						Vector2 newPos = new Vector2(TitleRect.X + 10 + 46 - TitleFont.MeasureString(word).X / 2f, TextPos.Y + line * TitleFont.LineSpacing);
						newPos = new Vector2((int)newPos.X, (int)newPos.Y);
						spriteBatch.DrawString(TitleFont, word, newPos, new Color(163, 198, 236));
						line++;
					}
					spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/techroot_body_press"), RootRect, Color.White);
					spriteBatch.Draw(ResourceManager.Texture(string.Concat("ResearchMenu/", ResourceManager.TechTree[Entry.UID].IconPath, "_hover")), IconRect, Color.White);
					return;
				}
				default:
				{
					return;
				}
			}
		}

		public bool HandleInput(InputState input, Camera2D camera)
		{
            Vector2 rectPos = camera.GetScreenSpaceFromWorldSpace(new Vector2(RootRect.X, RootRect.Y)); 
            Rectangle moddedRect = new Rectangle((int)rectPos.X, (int)rectPos.Y, RootRect.Width, RootRect.Height);

            if (moddedRect.HitTest(input.CursorPosition))
			{
				if (nodeState != NodeState.Press)
				{
					if (nodeState != NodeState.Hover)
					{
						GameAudio.MouseOver();
					}
					nodeState = NodeState.Hover;
				}
				if (input.InGameSelect)
				{
					return true;
				}
			}
			else if (nodeState != NodeState.Press)
			{
				nodeState = NodeState.Normal;
			}
			return false;
		}
	}
}