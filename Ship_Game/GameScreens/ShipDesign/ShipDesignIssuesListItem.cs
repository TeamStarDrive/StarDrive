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

        UILabel TitleLabel;
        UILabel ProblemLabel;
        UILabel RemediationLabel;
        UIPanel IssueTexture;


        public ShipDesignIssuesListItem(DesignIssueDetails details)
        {
            IssueDetails = details;

            IssueTexture = Add(new UIPanel(Vector2.Zero, IssueDetails.Texture));
            IssueTexture.SetRelPos(100, 80);
            IssueTexture.Size      = new Vector2(96, 96);
            IssueTexture.DebugDraw = true;

            AddIssueLabel(TitleLabel, IssueDetails.Title, 100, 0, SmallFont, TextAlign.Center, IssueDetails.Color);
            AddIssueLabel(ProblemLabel, IssueDetails.Problem, 380, 200, SmallFont, TextAlign.VerticalCenter, Color.MintCream);
            AddIssueLabel(RemediationLabel, IssueDetails.Remediation, 380, 580, SmallFont, TextAlign.VerticalCenter, Color.White);
        }

        void AddIssueLabel(UILabel label, string text, float sizeX, float relativeX, SpriteFont font, TextAlign align, Color color)
        {
            string parsedText = font.ParseText(text, sizeX-20);
            label             = Add(new UILabel(parsedText, font, color));
            label.Size        = new Vector2(sizeX, 80);
            label.Align       = align;
            label.SetRelPos(relativeX, 0);
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