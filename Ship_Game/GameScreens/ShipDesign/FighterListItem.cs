using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public class FighterListItem : ScrollListItem<FighterListItem>
    {
        public IShipDesign Design;

        public FighterListItem(IShipDesign design)
        {
            Design = design;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            var bCursor = new Vector2(List.X + 15, Y);

            batch.Draw(Design.Icon, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
            var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
            Color color = ShipBuilder.GetHangarTextColor(Design.Name);
            batch.DrawString(Fonts.Arial12Bold, Design.Name, tCursor, color);
        }
    }
}
