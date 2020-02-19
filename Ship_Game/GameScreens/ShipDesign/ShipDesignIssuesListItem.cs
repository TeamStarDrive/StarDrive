using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.GameScreens.ShipDesignScreen;

namespace Ship_Game.GameScreens.ShipDesign
{
    public sealed class ShipDesignIssuesListItem : ScrollListItem<ShipDesignIssuesListItem>
    {
        private readonly DesignIssueDetails IssueDetails;
        private readonly SpriteFont NormalFont = Fonts.Arial20Bold;
        private readonly SpriteFont SmallFont = Fonts.Arial12Bold;
        private readonly SpriteFont TinyFont = Fonts.Arial8Bold;

        UILabel WarningLabel;
        UIPanel IssueTexture;
        UILabel SeverityLabel;
        UILabel ProblemLabel;
        UILabel RemediationLabel;


        public ShipDesignIssuesListItem(DesignIssueDetails details)
        {
            IssueDetails = details;

            IssueTexture = Add(new UIPanel(Vector2.Zero, IssueDetails.Texture));
            IssueTexture.SetRelPos(0, 0);
            IssueTexture.Size = new Vector2(96, 96);


            // need to make a method out of this
            WarningLabel = Add(new UILabel(IssueDetails.Title, NormalFont, Color.White));
            WarningLabel.Align = TextAlign.Center;
            WarningLabel.Size = new Vector2(100, 80);
            WarningLabel.SetRelPos(100, 0);

            WarningLabel = Add(new UILabel(Severity, NormalFont, Color.White));
            WarningLabel.Align = TextAlign.Center;
            WarningLabel.Size = new Vector2(100, 80);
            WarningLabel.SetRelPos(200, 0);

            WarningLabel = Add(new UILabel(IssueDetails.Problem, SmallFont, Color.White));
            WarningLabel.Size = new Vector2(350, 20);
            WarningLabel.SetRelPos(300, 0);

            WarningLabel = Add(new UILabel(IssueDetails.Remediation, SmallFont, Color.White));
            WarningLabel.Size = new Vector2(350, 20);
            WarningLabel.SetRelPos(650, 0);
        }

        public override void Draw(SpriteBatch batch)
        {
            batch.FillRectangle(Rect, RectColor);
            base.Draw(batch);
        }

        Color RectColor
        {
            get
            {
                int divider = 10;
                Color color = new Color((byte)(IssueDetails.Color.R / divider),
                                        (byte)(IssueDetails.Color.G / divider),
                                        (byte)(IssueDetails.Color.B / divider));
                return color;
            }
        }

        string Severity
        {
            get
            {
                switch(IssueDetails.Severity)
                {
                    default:
                    case WarningLevel.None: return "None";
                    case WarningLevel.Minor: return "Minor";
                    case WarningLevel.Major: return "Major";
                    case WarningLevel.Critical: return "Critical";
                }
            }
        }
    }
}