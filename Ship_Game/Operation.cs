using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
	public class Operation
	{
		public string Name;

		public string UID;

		public int Cost;

		public int ToolTip;

		public bool Hover;

		public bool Selected;

		public Operation(string Name, string UID, int Cost, int ToolTip)
		{
			this.UID = UID;
			this.Name = Name;
			this.Cost = Cost;
			this.ToolTip = ToolTip;
		}

		public void Draw(float points, ScreenManager screenManager, Vector2 Position, float width)
		{
			Color orange;
			Color color;
			string toDraw = this.Name;
			while (Fonts.Arial12Bold.MeasureString(toDraw).X < width - 20f)
			{
				toDraw = string.Concat(toDraw, " .");
			}
			if (points < (float)this.Cost)
			{
				screenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, toDraw, Position, Color.Gray);
				Position.X = Position.X + width;
				Position.X = Position.X - Fonts.Arial12Bold.MeasureString(this.Cost.ToString()).X;
				screenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.Cost.ToString(), Position, Color.Gray);
				return;
			}
			SpriteBatch spriteBatch = screenManager.SpriteBatch;
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			string str = toDraw;
			Vector2 position = Position;
			if (this.Selected)
			{
				orange = Color.Orange;
			}
			else
			{
				orange = (this.Hover ? Color.White : Color.LightGray);
			}
			spriteBatch.DrawString(arial12Bold, str, position, orange);
			Position.X = Position.X + width;
			Position.X = Position.X - Fonts.Arial12Bold.MeasureString(this.Cost.ToString()).X;
			SpriteBatch spriteBatch1 = screenManager.SpriteBatch;
			SpriteFont spriteFont = Fonts.Arial12Bold;
			string str1 = this.Cost.ToString();
			Vector2 vector2 = Position;
			if (this.Selected)
			{
				color = Color.Orange;
			}
			else
			{
				color = (this.Hover ? Color.White : Color.LightGray);
			}
			spriteBatch1.DrawString(spriteFont, str1, vector2, color);
		}
	}
}