using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.GameScreens.ShipDesign.DesignIssues;

namespace Ship_Game
{
    public sealed class ShipDesignIssuesListItem : ScrollListItem<ShipDesignIssuesListItem>
    {
        private readonly DesignIssueDetails IssueDetails;
        private readonly SpriteFont SmallFont = Fonts.Arial12Bold;

        UILabel TitleLabel;
        UILabel ProblemLabel;
        UILabel RemediationLabel;
        readonly UIPanel IssueTexture;


        public ShipDesignIssuesListItem(DesignIssueDetails details)
        {
            IssueDetails      = details;
            IssueTexture      = Add(new UIPanel(Pos, IssueDetails.Texture));
            IssueTexture.Size = new Vector2(60, 60);
            //IssueTexture.DebugDraw = true;

            TitleLabel       = AddIssueLabel(IssueDetails.Title, 150, 50, SmallFont, TextAlign.Center, IssueDetails.Color);
            ProblemLabel     = AddIssueLabel(IssueDetails.Problem, 370, 200, SmallFont, TextAlign.VerticalCenter, Colors.Cream);
            RemediationLabel = AddIssueLabel(IssueDetails.Remediation, 370, 560, SmallFont, TextAlign.VerticalCenter, Colors.Cream);
        }

        UILabel AddIssueLabel(string text, float sizeX, float relativeX, SpriteFont font, TextAlign align, Color color)
        {
            string parsedText = font.ParseText(text, sizeX-30);
            UILabel label     = Add(new UILabel(parsedText, font, color));
            label.Size        = new Vector2(sizeX, 80);
            label.Align       = align;
            label.SetRelPos(relativeX, 0);
            return label;
        }

        public override void Draw(SpriteBatch batch)
        {
            batch.FillRectangle(Rect, RectColor);
            // workaround  for UIpanel which the Pos of the item is not set in the constructor
            IssueTexture.Pos = new Vector2(Pos.X, Pos.Y + 10);
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