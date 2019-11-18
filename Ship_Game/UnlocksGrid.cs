using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game
{
    public enum UnlockType
    {
        SHIPMODULE,
        TROOP,
        BUILDING,
        HULL,
        ADVANCE
    }

    public sealed class UnlockItem
    {
        public UnlockType Type;
        public string HullUnlocked;
        public string privateName;
        public Building building;
        public ShipModule module;
        public Troop troop;
        public string Description;
    }

    public sealed class UnlocksGrid
	{
        public struct GridItem
        {
            public UnlockItem item;
            public Rectangle rect;
            public Vector2 Pos => new Vector2(rect.X, rect.Y);
        }

        public Array<GridItem> GridOfUnlocks = new Array<GridItem>();

        public UnlocksGrid(Array<UnlockItem> Unlocks, Rectangle r)
		{
			int x = 0;
			int y = 0;
			foreach (UnlockItem item in Unlocks)
			{
				var gi = new GridItem
				{
					rect = new Rectangle(r.X + 32 * x, r.Y + 32 * y, 32, 32),
					item = item
				};
				GridOfUnlocks.Add(gi);
				y++;
                if (y == 2)
                {
                    y = 0;
                    x++;
                }
            }
		}

		public void Draw(SpriteBatch batch)
		{
			foreach (GridItem gi in GridOfUnlocks)
            {
                UnlockItem unlock = gi.item;
                switch (unlock.Type)
                {
                    case UnlockType.SHIPMODULE:
                    {
                        var iconRect = new Rectangle(gi.rect.X, gi.rect.Y, 16 * unlock.module.XSIZE, 16 * unlock.module.YSIZE);
                        iconRect.X = iconRect.X + 16 - iconRect.Width / 2;
                        iconRect.Y = gi.rect.Y + gi.rect.Height / 2 - iconRect.Height / 2;

                        while (iconRect.Height > gi.rect.Height)
                        {
                            iconRect.Height = iconRect.Height - unlock.module.YSIZE;
                            iconRect.Width = iconRect.Width - unlock.module.XSIZE;
                            iconRect.X = gi.rect.X + 16 - iconRect.Width / 2;
                            iconRect.Y = gi.rect.Y + gi.rect.Height / 2 - iconRect.Height / 2;
                        }

                        batch.Draw(unlock.module.ModuleTexture, iconRect, Color.White);
                        break;
                    }
                    case UnlockType.TROOP:
                        unlock.troop.DrawIcon(batch, gi.rect);
                        break;
                    case UnlockType.BUILDING:
                        batch.Draw(ResourceManager.Texture($"Buildings/icon_{unlock.building.Icon}_64x64"), gi.rect, Color.White);
                        break;
                    case UnlockType.HULL:
                        if (ResourceManager.Hull(unlock.privateName, out ShipData hullData))
                            batch.Draw(hullData.Icon, gi.rect, Color.White);
                        break;
                    case UnlockType.ADVANCE:
                        batch.Draw(ResourceManager.Texture("TechIcons/star"), gi.rect, Color.White);
                        break;
                }
            }
		}
	}
}