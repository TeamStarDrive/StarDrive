using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class UnlocksGrid
	{
		public List<UnlocksGrid.GridItem> GridOfUnlocks = new List<UnlocksGrid.GridItem>();

		public UnlocksGrid(List<UnlockItem> Unlocks, Rectangle r)
		{
			Vector2 Cursor = new Vector2((float)r.X, (float)r.Y);
			int Column = 0;
			int Row = 0;
			foreach (UnlockItem item in Unlocks)
			{
				UnlocksGrid.GridItem gi = new UnlocksGrid.GridItem()
				{
					rect = new Rectangle((int)Cursor.X + 32 * Column, (int)Cursor.Y + 32 * Row, 32, 32),
					item = item
				};
				this.GridOfUnlocks.Add(gi);
				Row++;
				if (Row != 2)
				{
					continue;
				}
				Row = 0;
				Column++;
			}
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			foreach (UnlocksGrid.GridItem gi in this.GridOfUnlocks)
			{
				UnlockItem unlock = gi.item;
				if (unlock.Type == "SHIPMODULE")
				{
					Rectangle IconRect = new Rectangle(gi.rect.X, gi.rect.Y, 16 * unlock.module.XSIZE, 16 * unlock.module.YSIZE);
					//{
                        IconRect.X = IconRect.X + 16 - IconRect.Width / 2;
                        IconRect.Y = gi.rect.Y + gi.rect.Height / 2 - IconRect.Height / 2;
					//};
					while (IconRect.Height > gi.rect.Height)
					{
						IconRect.Height = IconRect.Height - unlock.module.YSIZE;
						IconRect.Width = IconRect.Width - unlock.module.XSIZE;
						IconRect.X = gi.rect.X + 16 - IconRect.Width / 2;
						IconRect.Y = gi.rect.Y + gi.rect.Height / 2 - IconRect.Height / 2;
					}
					spriteBatch.Draw(ResourceManager.TextureDict[unlock.module.IconTexturePath], IconRect, Color.White);
				}
				if (unlock.Type == "TROOP")
				{
					Rectangle IconRect = new Rectangle(gi.rect.X, gi.rect.Y, 32, 32);
					unlock.troop.DrawIcon(spriteBatch, IconRect);
				}
				if (unlock.Type == "BUILDING")
				{
					Rectangle IconRect = new Rectangle(gi.rect.X, gi.rect.Y, 32, 32);
					spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Buildings/icon_", unlock.building.Icon, "_64x64")], IconRect, Color.White);
				}
				if (unlock.Type == "HULL")
				{
					Rectangle IconRect = new Rectangle(gi.rect.X, gi.rect.Y, 32, 32);
					spriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.HullsDict[unlock.privateName].IconPath], IconRect, Color.White);
				}
				if (unlock.Type != "ADVANCE")
				{
					continue;
				}
				Rectangle IconRect2 = new Rectangle(gi.rect.X, gi.rect.Y, 32, 32);
				spriteBatch.Draw(ResourceManager.TextureDict["TechIcons/star"], IconRect2, Color.White);
			}
		}

		public struct GridItem
		{
			public UnlockItem item;

			public Rectangle rect;
		}
	}
}