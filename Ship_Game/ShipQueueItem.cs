using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Ship_Game.Ships;

namespace Ship_Game
{
	public sealed class ShipQueueItem
	{
		public ShipData ShipToBuild;

		public Rectangle ListRect;

		private string IconPath;

		private Color IconColor;

		public Color ArrowColor = Color.White;

		public Rectangle ArrowRect;

		private int IconSize = 40;

		public ShipQueueItem(ShipData data, Rectangle qRect, Color c)
		{
			this.ShipToBuild = data;
			this.IconColor = c;
			this.ListRect = qRect;
			//this.Role role = data.Role;
			//string str = role;
            if (data.ShipCategory == ShipData.Category.Civilian)            
                this.IconPath = "TacticalIcons/symbol_freighter";            
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			spriteBatch.DrawLine(new Vector2((float)this.ListRect.X, (float)(this.ListRect.Y + this.ListRect.Height)), new Vector2((float)(this.ListRect.X + this.ListRect.Width), (float)(this.ListRect.Y + this.ListRect.Height)), Color.DarkBlue);
			Vector2 cursor = new Vector2((float)(this.ListRect.X + 5), (float)(this.ListRect.Y + this.ListRect.Height / 2 - this.IconSize / 2));
			this.ArrowRect = new Rectangle((int)cursor.X, (int)cursor.Y, 20, 20);
			spriteBatch.Draw(ResourceManager.Texture("UI/leftArrow"), this.ArrowRect, this.ArrowColor);
			cursor.X = cursor.X + 25f;
			Rectangle IconRect = new Rectangle((int)cursor.X, (int)cursor.Y, this.IconSize, this.IconSize);
			spriteBatch.Draw(ResourceManager.Texture(this.IconPath), IconRect, this.IconColor);
			cursor.X = cursor.X + 45f;
			spriteBatch.DrawString(Fonts.Arial12Bold, this.ShipToBuild.Name, cursor, Color.White);
			cursor.Y = cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
			spriteBatch.DrawString(Fonts.Arial12Bold, "Cost: 250", cursor, Color.Orange);
		}
	}
}