using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Universe;

namespace Ship_Game
{
    public class CombatScreenOrbitListItem : ScrollListItem<CombatScreenOrbitListItem>
    {
        readonly UniverseState Universe;
        public readonly Troop Troop;

        public CombatScreenOrbitListItem(UniverseState us, Troop troop)
        {
            Universe = us;
            Troop = troop;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Color nameColor = Color.LightGray;
            Color statsColor = nameColor;
            Color backColor = Color.Black;
            if (Hovered)
            {
                nameColor  = Color.Gold;
                statsColor = Color.Orange;
                backColor = Color.Black.AddRgb(0.2f);
            }
            
            batch.FillRectangle(Rect, backColor.Alpha(0.3f));
            Troop.Draw(Universe, batch, new RectF(X + 2, Y, Height, Height));
            batch.DrawString(Fonts.Arial12Bold, Troop.Name, X + 40, Y + 2, nameColor);
            batch.DrawString(Fonts.Arial8Bold, $"{Troop.StrengthText}, Level: {Troop.Level}", X + 40, Y + 14, statsColor);
        }
    }
}
