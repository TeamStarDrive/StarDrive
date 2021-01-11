using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game
{
    public enum UnlockType
    {
        ShipModule,
        Troop,
        Building,
        Hull,
        Advance
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
                    case UnlockType.ShipModule:
                        var iconRect = new Rectangle(gi.rect.X, gi.rect.Y, 16 * unlock.module.XSIZE, 16 * unlock.module.YSIZE);
                        int modW = unlock.module.XSIZE;
                        int modH = unlock.module.YSIZE;

                        if (modH > modW)
                        {
                            float ratio     = (float)modW / modH * gi.rect.Height;
                            iconRect.Width  = (int)ratio;
                            iconRect.Height = gi.rect.Height;
                        }
                        else if (modW > modH)
                        {
                            float ratio     = (float)modH / modW * (gi.rect.Width-2);
                            iconRect.Width  = gi.rect.Width-2;
                            iconRect.Height = (int)ratio;
                        }
                        else
                        {
                            iconRect.Width  = gi.rect.Width;
                            iconRect.Height = gi.rect.Height;
                        }
                        iconRect.X = gi.rect.X + 16 - iconRect.Width / 2;
                        iconRect.Y = gi.rect.Y + gi.rect.Height / 2 - iconRect.Height / 2;


                        batch.Draw(unlock.module.ModuleTexture, iconRect, Color.White);
                        break;
                    case UnlockType.Troop:
                        unlock.troop.DrawIcon(batch, gi.rect);
                        break;
                    case UnlockType.Building:
                        batch.Draw(ResourceManager.Texture($"Buildings/icon_{unlock.building.Icon}_64x64"), gi.rect, Color.White);
                        break;
                    case UnlockType.Hull:
                        if (ResourceManager.Hull(unlock.privateName, out ShipData hullData))
                            batch.Draw(hullData.Icon, gi.rect, Color.White);
                        break;
                    case UnlockType.Advance:
                        batch.Draw(ResourceManager.Texture("TechIcons/star"), gi.rect, Color.White);
                        break;
                }
            }
		}
	}
}