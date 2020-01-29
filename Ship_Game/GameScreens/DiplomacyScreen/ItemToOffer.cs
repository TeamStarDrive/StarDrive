using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.GameScreens.DiplomacyScreen
{
    public sealed class ItemToOffer : ScrollListItem<ItemToOffer>
    {
        LocalizedText Words;
        public string SpecialInquiry = "";
        public string Response;
        public bool Selected;
        readonly SpriteFont Font = Fonts.Arial12Bold;
        UILabel Text;

        public ItemToOffer(in LocalizedText words)
        {
            Initialize(words, false);
        }

        public ItemToOffer(in LocalizedText words, string response)
        {
            Initialize(words, false);
            Response = response;
        }

        public ItemToOffer(in LocalizedText words, bool isHeader) : base(words.Text)
        {
            Initialize(words, isHeader:true);
        }

        void Initialize(in LocalizedText words, bool isHeader)
        {
            Words = words;
            if (!isHeader)
            {
                Text = Add(new UILabel(words, Font));
                Text.Align = TextAlign.VerticalCenter;
            }
        }

        public override void PerformLayout()
        {
            if (Text != null)
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

        public override void Draw(SpriteBatch batch)
        {
            if (Text != null)
            {
                Text.Color = Selected ? Color.Orange
                           : Hovered ? Color.LightYellow
                           : Color.White.Alpha(0.8f);
            }

            if (Hovered)
                batch.FillRectangle(Rect, Color.Black.AddRgb(0.05f).Alpha(0.33f));

            base.Draw(batch); // this will draw our Label
        }



        public override bool HandleInput(InputState input)
        {
            if (Hovered && input.LeftMouseClick)
            {
                Selected = !Selected;
            }

            // this will trigger the item on-click event
            if (base.HandleInput(input))
                return true;

            if (Hovered)
            {
                switch (Response)
                {
                    case "NAPact":
                        ToolTip.CreateTooltip(GameTips.NonAggression, "", BotRight);
                        break;
                    case "OpenBorders":
                        ToolTip.CreateTooltip(GameTips.OpenBorders, "", BotRight);
                        break;
                    case "Peace Treaty":
                        ToolTip.CreateTooltip(GameTips.PeaceTreaty, "", BotRight);
                        break;
                    case "TradeTreaty":
                        ToolTip.CreateTooltip(GameTips.TradeTreaty, "", BotRight);
                        break;
                    case "OfferAlliance":
                        ToolTip.CreateTooltip(GameTips.AllianceTreaty, "", BotRight);
                        break;
                }
            }
            return false;
        }
    }
}