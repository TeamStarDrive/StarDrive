using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Gameplay;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
    public class TraitsListItem : ScrollListItem<TraitsListItem>
    {
        readonly RaceDesignScreen Screen;
        readonly Graphics.Font TitleFont;
        readonly Graphics.Font DescrFont;
        string Description;
        public TraitEntry Trait;
        Color GrayedoutColor = new Color(100, 100, 100);

        public TraitsListItem(RaceDesignScreen screen, TraitEntry trait)
        {
            Screen = screen;
            Trait = trait;
            TitleFont = screen.LowRes ? Fonts.Arial11Bold : Fonts.Arial14Bold;
            DescrFont = screen.LowRes ? Fonts.Arial10 : Fonts.Arial12;
            Description = new LocalizedText(Trait.Trait.Description).Text;
        }

        public override int ItemHeight => TitleFont.LineSpacing * 3 + DescrFont.LineSpacing;

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);

            float textAreaWidth = Width - 40;
            string name = PaddedWithDots(TitleFont, Trait.Trait.LocalizedName.Text, textAreaWidth);
            int cost = Trait.Trait.Cost;

            var drawColor = GrayedoutColor;
            if (!Trait.Selected)
            {
                drawColor = GrayedoutColor;
            }
            if (Trait.Selected)
            {
                drawColor = (cost > 0 ? Color.ForestGreen : Color.Red);
            }
            else if (Trait.Excluded)
            {
                drawColor = (cost > 0 ? Color.MediumSeaGreen.Alpha(0.4f) : Color.LightCoral.Alpha(0.4f));
            }
            else if (Screen.TotalPointsUsed >= 0 && Screen.TotalPointsUsed - cost >= 0 || cost < 0)
            {
                drawColor = (cost > 0 ? Color.MediumSeaGreen : Color.LightCoral);
            }
            
            var pos = new Vector2(X + 4, Y);
            batch.DrawString(TitleFont, name, pos, drawColor);
            
            if (Trait.Selected) 
            {
                var linePos = new Vector2 (X + 4, Y + TitleFont.LineSpacing-1);
                var linePosEnd = new Vector2(X + 4 + TitleFont.TextWidth(Trait.Trait.LocalizedName.Text), Y + TitleFont.LineSpacing-1);
                batch.DrawLine(linePos, linePosEnd, drawColor, 2);
            }

            string costText = cost.ToString();
            var curs = new Vector2(pos.X + 30 + textAreaWidth - TitleFont.TextWidth(costText), Y);
            batch.DrawString(TitleFont, costText, curs, drawColor);

            pos.Y += TitleFont.LineSpacing;
            batch.DrawString(DescrFont, DescrFont.ParseText(GetDescrption(), textAreaWidth), pos, drawColor);
        }

        static float DotSpaceWidth;

        // Creates padded text: "Vulgar Animals . . . . . . . . . . . ."
        static string PaddedWithDots(Graphics.Font font, string name, float totalWidth)
        {
            if (DotSpaceWidth <= 0f)
                DotSpaceWidth = font.MeasureString(" .").X;

            float nameWidth = font.TextWidth(name);
            int numDots = (int)Math.Ceiling((totalWidth - nameWidth) / DotSpaceWidth);

            var sb = new StringBuilder(name, name.Length + numDots*2);
            for (int i = 0; i < numDots; ++i)
                sb.Append(" .");

            return sb.ToString();
        }

        string GetDescrption()
        {
            if (Trait.Selected)
                return Description;

            string extraDescription = string.Empty;
            if (Trait.Excluded)
            {
                string excludedText = Trait.ExcludedBy[0];
                for (int i = 1; i < Trait.ExcludedBy.Count; i++)
                    excludedText = $"{excludedText}, {Trait.ExcludedBy[i]}";

                extraDescription = $"(excluded by {excludedText}).";
            }
            else if (Screen.TotalPointsUsed - Trait.Trait.Cost < 0)
            {
                extraDescription = "(not enough points to spend).";
            }

            return $"{Description} {extraDescription}";
        }
    }
}
