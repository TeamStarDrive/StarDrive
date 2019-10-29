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
        readonly RaceDesignScreen Screen;
        readonly SpriteFont TitleFont;
        readonly SpriteFont DescrFont;
        public TraitEntry Trait;
        public TraitsListItem(RaceDesignScreen screen, TraitEntry trait)
        {
            Screen = screen;
            Trait = trait;
            TitleFont = screen.LowRes ? Fonts.Arial11Bold : Fonts.Arial14Bold;
            DescrFont = screen.LowRes ? Fonts.Arial10 : Fonts.Arial12;
        }

        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);

            float textAreaWidth = Width - 40;
            string name = PaddedWithDots(TitleFont, Trait.trait.TraitName, textAreaWidth);
            int cost = Trait.trait.Cost;

            var tCursor = new Vector2(X, Y - 2);
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
            
            batch.DrawString(TitleFont, name, tCursor, drawColor);
            tCursor.Y += TitleFont.LineSpacing;

            string costText = cost.ToString();
            var curs = new Vector2(X + 30 + textAreaWidth - TitleFont.TextWidth(costText), Y);
            batch.DrawString(TitleFont, costText, curs, drawColor);
            batch.DrawString(DescrFont, DescrFont.ParseText(Localizer.Token(Trait.trait.Description), textAreaWidth), tCursor, drawColor);
        }

        static float DotSpaceWidth;

        // Creates padded text: "Vulgar Animals . . . . . . . . . . . ."
        static string PaddedWithDots(SpriteFont font, int localizedNameId, float totalWidth)
        {
            if (DotSpaceWidth <= 0f)
                DotSpaceWidth = font.MeasureString(" .").X;

            string name = Localizer.Token(localizedNameId);
            float nameWidth = font.TextWidth(name);
            int numDots = (int)Math.Ceiling((totalWidth - nameWidth) / DotSpaceWidth);

            var sb = new StringBuilder(name, name.Length + numDots*2);
            for (int i = 0; i < numDots; ++i)
                sb.Append(" .");

            return sb.ToString();
        }
    }
}
