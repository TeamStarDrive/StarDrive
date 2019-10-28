using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public class TraitsListItem : ScrollListItem<TraitsListItem>
    {
        RaceDesignScreen Screen;
        public TraitEntry Trait;
        public TraitsListItem(RaceDesignScreen screen, TraitEntry trait)
        {
            Screen = screen;
            Trait = trait;
        }

        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);

            string name = PaddedWithDots(Trait.trait.TraitName, List.Width - 70);
            int cost = Trait.trait.Cost;

            var tCursor = new Vector2(X, Y + 3f);
            var drawColor = new Color(95, 95, 95, 95);

            if (!Trait.Selected)
            {
                drawColor = new Color(95, 95, 95, 95);
            }
            if (Trait.Selected)
            {
                drawColor = (cost > 0 ? Color.ForestGreen : Color.Crimson);
            }
            else if (Trait.Excluded)
            {
                drawColor = new Color(95, 95, 95, 95);
            }
            else if (Screen.TotalPointsUsed >= 0 && Screen.TotalPointsUsed - cost >= 0 || cost < 0)
            {
                drawColor = (cost > 0 ? Color.MediumSeaGreen : Color.LightCoral);
            }
            
            batch.DrawString(Fonts.Arial14Bold, name, tCursor, drawColor);
            tCursor.Y += Fonts.Arial14Bold.LineSpacing;

            var curs = new Vector2(X + List.Width - 45 - Fonts.Arial14Bold.TextWidth(cost.ToString()), Y);
            batch.DrawString(Fonts.Arial14Bold, cost.ToString(), curs, drawColor);
            batch.DrawString(Fonts.Arial12, Fonts.Arial12.ParseText(Localizer.Token(Trait.trait.Description), List.Width - 45), tCursor, drawColor);
        }

        static float DotSpaceWidth;

        // Creates padded text: "Vulgar Animals . . . . . . . . . . . ."
        static string PaddedWithDots(int localizedNameId, float totalWidth)
        {
            if (DotSpaceWidth <= 0f)
                DotSpaceWidth = Fonts.Arial14Bold.MeasureString(" .").X;

            string name = Localizer.Token(localizedNameId);
            float nameWidth = Fonts.Arial14Bold.MeasureString(name).X;
            int numDots = (int)Math.Ceiling((totalWidth - nameWidth) / DotSpaceWidth);

            var sb = new StringBuilder(name, name.Length + numDots*2);
            for (int i = 0; i < numDots; ++i)
                sb.Append(" .");

            return sb.ToString();
        }
    }
}
