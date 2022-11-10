using System;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public class FleetDesignShipListItem : ScrollListItem<FleetDesignShipListItem>
    {
        readonly FleetDesignScreen Screen;
        public Ship Ship;
        public IShipDesign Design;

        public FleetDesignShipListItem(FleetDesignScreen screen, string headerText) : base(headerText)
        {
            Screen = screen;
        }

        // TODO: IMPLEMENT THIS PROPERLY, WE SHOULD SEE THE ACTIVE SHIPS
        public FleetDesignShipListItem(FleetDesignScreen screen, Ship ship)
        {
            Screen = screen;
            Ship = ship;
            AddPlus(new Vector2(-50, 0), "FleetDesignShipListItem.Add");
            AddEdit(new Vector2(-20, 0), "FleetDesignShipListItem.Edit");
        }

        public FleetDesignShipListItem(FleetDesignScreen screen, IShipDesign design)
        {
            Screen = screen;
            Design = design;
            AddPlus(new Vector2(-50, 0), "FleetDesignShipListItem.Add");
            AddEdit(new Vector2(-20, 0), "FleetDesignShipListItem.Edit");
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
            
            IShipDesign design = Ship?.ShipData ?? Design;
            if (design != null)
            {
                batch.Draw(design.Icon, new Rectangle((int)X, (int)Y, 29, 30), Color.White);

                var tCursor = new Vector2(X + 40f, Y + 3f);
                batch.DrawString(Fonts.Arial12Bold, Ship?.VanityName ?? design.Name, tCursor, Color.White);
                tCursor.Y += Fonts.Arial12Bold.LineSpacing;

                if (Hovered)
                {
                    batch.DrawString(Fonts.Arial8Bold, design.GetRole(), tCursor, Color.Orange);
                }
                else
                {
                    if (Screen.SubShips.SelectedIndex == 0)
                    {
                        batch.DrawString(Fonts.Arial12Bold, design.GetRole(), tCursor, Color.Orange);
                    }
                    else if (Ship != null)
                    {
                        if (Ship.System != null)
                            batch.DrawString(Fonts.Arial12Bold, $"{Ship.System.Name} system", tCursor, Color.Orange);
                        else
                            batch.DrawString(Fonts.Arial12Bold, "Deep Space", tCursor, Color.Orange);
                    }
                }
            }
        }
    }
}
