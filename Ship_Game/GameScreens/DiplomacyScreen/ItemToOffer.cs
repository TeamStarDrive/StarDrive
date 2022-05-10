using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.GameScreens.DiplomacyScreen
{
    public sealed class ItemToOffer : ScrollListItem<ItemToOffer>
    {
        LocalizedText Words;
        public string SpecialInquiry = "";
        public string Response;
        public bool Selected;
        readonly Graphics.Font Font = Fonts.Arial12Bold;
        UILabel Text;

        public override int ItemHeight => Font.LineSpacing + 2;

        public ItemToOffer(in LocalizedText words)
        {
            Initialize(words, false);
        }

        public ItemToOffer(in LocalizedText words, string response)
        {
            Initialize(words, false);
            Response = response;
        }

        public ItemToOffer(in LocalizedText words, bool isHeader)
        {
            Initialize(words, isHeader);
        }

        void Initialize(in LocalizedText words, bool isHeader)
        {
            Words = words;
            IsHeader = isHeader;
            Text = Add(new UILabel(words, Font));
            Text.TextAlign = TextAlign.VerticalCenter;
        }

        public override void PerformLayout()
        {
            Text.Rect = Rect;
            base.PerformLayout();
        }

        public override string ToString()
            => $"{TypeName} Response:\"{Response}\"  Words:\"{Words}\"  Inquiry:\"{SpecialInquiry}\"";

        public void ChangeSpecialInquiry(Array<string> items)
        {
            if (Selected)
                items.Add(SpecialInquiry);
            else
                items.Remove(SpecialInquiry);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (Hovered)
                batch.FillRectangle(Rect, Color.Black.AddRgb(0.05f).Alpha(0.33f));
            
            Text.Color = Selected ? Color.Orange
                       : Hovered ? Color.LightYellow
                       : Color.White.Alpha(0.8f);
            Text.Draw(batch, elapsed);
        }

        Vector2 TipPos => X < (GlobalStats.XRES*0.25f)
                          ? TopRight
                          : TopLeft - new Vector2(320, 0);

        public override bool HandleInput(InputState input)
        {
            // this will trigger the item on-click event
            if (base.HandleInput(input))
                return true;

            if (Hovered)
            {
                switch (Response)
                {
                    case "NAPact":
                        ToolTip.CreateTooltip(GameText.ANonaggressionPactIsA, "", TipPos);
                        break;
                    case "OpenBorders":
                        ToolTip.CreateTooltip(GameText.AnOpenBordersTreatyWill, "", TipPos);
                        break;
                    case "Peace Treaty":
                        ToolTip.CreateTooltip(GameText.PeaceTreatiesLastForTurns, "", TipPos);
                        break;
                    case "TradeTreaty":
                        ToolTip.CreateTooltip(GameText.TradeTreatiesCreateWealthFor, "", TipPos);
                        break;
                    case "OfferAlliance":
                        ToolTip.CreateTooltip(GameText.AlliancesAreSeriousCommitmentsBetween, "", TipPos);
                        break;
                }
            }
            return false;
        }
    }
}