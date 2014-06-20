using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ship_Game
{
	public class RootNode : Node
	{
		public SpriteFont TitleFont = Fonts.Visitor10;

		public NodeState nodeState;

		public string TechName;

		public Rectangle TitleRect;

		public Rectangle RootRect = new Rectangle(0, 0, 129, 76);

		private Rectangle IconRect;

		public RootNode(Vector2 Position, TechEntry Tech)
		{
			if (GlobalStats.Config.Language == "Russian" || GlobalStats.Config.Language == "Polish")
			{
				this.TitleFont = Fonts.Arial10;
			}
			this.tech = Tech;
			this.TechName = Localizer.Token(ResourceManager.TechTree[Tech.UID].NameIndex);
			this.RootRect.X = (int)Position.X;
			this.RootRect.Y = (int)Position.Y;
			this.IconRect = new Rectangle(this.RootRect.X + this.RootRect.Width / 2 - 39, this.RootRect.Y + this.RootRect.Height / 2 - 29, 78, 58);
			this.TitleRect = new Rectangle(this.RootRect.X + 2, this.RootRect.Y - 21, 108, 61);
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			string text;
			string[] textarray;
			Vector2 TextPos;
			int line;
			switch (this.nodeState)
			{
				case NodeState.Normal:
				{
					spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/techroot_title"], this.TitleRect, Color.White);
					text = HelperFunctions.parseText(this.TitleFont, this.TechName, 93f);
					textarray = Regex.Split(text, "\n");
					TextPos = new Vector2((float)(this.TitleRect.X + 10 + 46) - this.TitleFont.MeasureString(text).X / 2f, (float)(this.TitleRect.Y + 7 + 12) - this.TitleFont.MeasureString(text).Y / 2f);
					line = 0;
					string[] strArrays = textarray;
					for (int i = 0; i < (int)strArrays.Length; i++)
					{
						string word = strArrays[i];
						Vector2 newPos = new Vector2((float)(this.TitleRect.X + 10 + 46) - this.TitleFont.MeasureString(word).X / 2f, TextPos.Y + (float)(line * this.TitleFont.LineSpacing));
						newPos = new Vector2((float)((int)newPos.X), (float)((int)newPos.Y));
						spriteBatch.DrawString(this.TitleFont, word, newPos, new Color(131, 147, 172));
						line++;
					}
					spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/techroot_body"], this.RootRect, Color.White);
					spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("ResearchMenu/", ResourceManager.TechTree[this.tech.UID].IconPath)], this.IconRect, Color.White);
					return;
				}
				case NodeState.Hover:
				{
					spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/techroot_underglow_selhoverpress"], this.RootRect, Color.White);
					spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/techroot_title_selhoverpress"], this.TitleRect, Color.White);
					text = HelperFunctions.parseText(this.TitleFont, this.TechName, 93f);
					textarray = Regex.Split(text, "\n");
					TextPos = new Vector2((float)(this.TitleRect.X + 10 + 46) - this.TitleFont.MeasureString(text).X / 2f, (float)(this.TitleRect.Y + 7 + 12) - this.TitleFont.MeasureString(text).Y / 2f);
					line = 0;
					string[] strArrays1 = textarray;
					for (int j = 0; j < (int)strArrays1.Length; j++)
					{
						string word = strArrays1[j];
						Vector2 newPos = new Vector2((float)(this.TitleRect.X + 10 + 46) - this.TitleFont.MeasureString(word).X / 2f, TextPos.Y + (float)(line * this.TitleFont.LineSpacing));
						newPos = new Vector2((float)((int)newPos.X), (float)((int)newPos.Y));
						spriteBatch.DrawString(this.TitleFont, word, newPos, new Color(163, 198, 236));
						line++;
					}
					spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/techroot_body_selhover"], this.RootRect, Color.White);
					spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("ResearchMenu/", ResourceManager.TechTree[this.tech.UID].IconPath, "_hover")], this.IconRect, Color.White);
					return;
				}
				case NodeState.Press:
				{
					spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/techroot_underglow_selhoverpress"], this.RootRect, Color.White);
					spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/techroot_title_selhoverpress"], this.TitleRect, Color.White);
					text = HelperFunctions.parseText(this.TitleFont, this.TechName, 93f);
					textarray = Regex.Split(text, "\n");
					TextPos = new Vector2((float)(this.TitleRect.X + 10 + 46) - this.TitleFont.MeasureString(text).X / 2f, (float)(this.TitleRect.Y + 7 + 12) - this.TitleFont.MeasureString(text).Y / 2f);
					line = 0;
					string[] strArrays2 = textarray;
					for (int k = 0; k < (int)strArrays2.Length; k++)
					{
						string word = strArrays2[k];
						Vector2 newPos = new Vector2((float)(this.TitleRect.X + 10 + 46) - this.TitleFont.MeasureString(word).X / 2f, TextPos.Y + (float)(line * this.TitleFont.LineSpacing));
						newPos = new Vector2((float)((int)newPos.X), (float)((int)newPos.Y));
						spriteBatch.DrawString(this.TitleFont, word, newPos, new Color(163, 198, 236));
						line++;
					}
					spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/techroot_body_press"], this.RootRect, Color.White);
					spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("ResearchMenu/", ResourceManager.TechTree[this.tech.UID].IconPath, "_hover")], this.IconRect, Color.White);
					return;
				}
				default:
				{
					return;
				}
			}
		}

		public override bool HandleInput(InputState input)
		{
			if (HelperFunctions.CheckIntersection(this.RootRect, input.CursorPosition))
			{
				if (this.nodeState != NodeState.Press)
				{
					if (this.nodeState != NodeState.Hover)
					{
						AudioManager.PlayCue("mouse_over4");
					}
					this.nodeState = NodeState.Hover;
				}
				if (input.InGameSelect)
				{
					return true;
				}
			}
			else if (this.nodeState != NodeState.Press)
			{
				this.nodeState = NodeState.Normal;
			}
			return false;
		}
	}
}