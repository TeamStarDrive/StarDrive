using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.GameScreens;

namespace Ship_Game.GameScreens.ShipDesign
{
    public sealed class ShipDesignIssuesListItem : ScrollListItem<ShipDesignIssuesListItem>
    {
        public readonly DesignIssueDetails IssueDetails;
        private readonly Graphics.Font NormalFont = Fonts.Arial12Bold;
        private readonly Color White = Color.LightGray;

        readonly UILabel TitleLabel;
        readonly UILabel ProblemLabel;
        readonly UILabel RemediationLabel;
        readonly UIPanel IssueTexture;


        public ShipDesignIssuesListItem(DesignIssueDetails details)
        {
            IssueDetails      = details;
            IssueTexture      = Add(new UIPanel(Pos, IssueDetails.Texture));
            IssueTexture.Size = new Vector2(60, 60);
            TitleLabel        = AddIssueLabel(IssueDetails.Title, 150, 65, NormalFont, TextAlign.VerticalCenter, IssueDetails.Color);
            ProblemLabel      = AddIssueLabel(IssueDetails.Problem, 370, 200, NormalFont, TextAlign.VerticalCenter, White);
            RemediationLabel  = AddIssueLabel(IssueDetails.Remediation, 370, 560, NormalFont,
                TextAlign.VerticalCenter, White, IssueDetails.AdditionalText);
        }

        UILabel AddIssueLabel(LocalizedText text, float sizeX, float relativeX, Graphics.Font font,
            TextAlign align, Color color, string additionalText = "")
        {
            string parsedText = font.ParseText($"{text.Text}{additionalText}", sizeX-30);
            UILabel label     = Add(new UILabel(parsedText, font, color));
            label.Size        = new Vector2(sizeX, 80);
            label.TextAlign   = align;
            label.SetRelPos(relativeX, 0);
           return label;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Color borderColor = RectColor(IssueDetails.Color, 3);
            batch.FillRectangle(Rect, RectColor(IssueDetails.Color, 10));
            batch.DrawRectangle(Rect, borderColor);

            int top            = Rect.Y;
            int bot            = Rect.Y + Rect.Height;
            var problemTop     = new Vector2(Rect.X + 190, top);
            var problemBot     = new Vector2(Rect.X + 190, bot);
            var descriptionTop = new Vector2(Rect.X + 550, top);
            var descriptionBot = new Vector2(Rect.X + 550, bot);

            batch.DrawLine(problemTop, problemBot, borderColor);
            batch.DrawLine(descriptionTop, descriptionBot, borderColor);
            // SetRelPos is not working for some reason, using Pos
            IssueTexture.Pos = new Vector2(Pos.X + 2, Pos.Y + 10);
            base.Draw(batch, elapsed);
        }

        Color RectColor(Color color, int divider)
        {
            Color divColor = new Color((byte)(color.R / divider),
                                       (byte)(color.G / divider),
                                       (byte)(color.B / divider));
            return divColor;
        }

        string Severity
        {
            get
            {
                switch(IssueDetails.Severity)
                {
                    default:
                    case WarningLevel.None:     return "None";
                    case WarningLevel.Minor:    return "Minor";
                    case WarningLevel.Major:    return "Major";
                    case WarningLevel.Critical: return "Critical";
                }
            }
        }
    }
}