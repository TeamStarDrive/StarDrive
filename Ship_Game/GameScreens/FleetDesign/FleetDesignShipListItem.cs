using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game
{
    public class FleetDesignShipListItem : ScrollListItem<FleetDesignShipListItem>
    {
        readonly FleetDesignScreen Screen;
        public Ship Ship;

        public FleetDesignShipListItem(FleetDesignScreen screen, string headerText) : base(headerText)
        {
            Screen = screen;
        }

        public FleetDesignShipListItem(FleetDesignScreen screen, Ship ship)
        {
            Screen = screen;
            Ship = ship;
            AddPlus(new Vector2(-50, 0), "FleetDesignShipListItem.Add");
            AddEdit(new Vector2(-20, 0), "FleetDesignShipListItem.Edit");
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
            
            if (Ship != null)
            {
                batch.Draw(Ship.shipData.Icon, new Rectangle((int)X, (int)Y, 29, 30), Color.White);

                var tCursor = new Vector2(X + 40f, Y + 3f);
                batch.DrawString(Fonts.Arial12Bold, Ship.ShipName, tCursor, Color.White);
                tCursor.Y += Fonts.Arial12Bold.LineSpacing;

                if (Hovered)
                {
                    batch.DrawString(Fonts.Arial8Bold, Ship.shipData.GetRole(), tCursor, Color.Orange);
                }
                else
                {
                    if (Screen.SubShips.SelectedIndex == 0)
                    {
                        batch.DrawString(Fonts.Arial12Bold, Ship.shipData.GetRole(), tCursor, Color.Orange);
                    }
                    else if (Ship.System == null)
                    {
                        batch.DrawString(Fonts.Arial12Bold, "Deep Space", tCursor, Color.Orange);
                    }
                    else
                    {
                        batch.DrawString(Fonts.Arial12Bold, $"{Ship.System.Name} system", tCursor, Color.Orange);
                    }
                }
            }
        }
    }
}
