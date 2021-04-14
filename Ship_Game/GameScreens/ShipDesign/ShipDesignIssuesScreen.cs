using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;
using Ship_Game.ShipDesignIssues;

namespace Ship_Game.GameScreens.ShipDesignScreen
{
    public sealed class ShipDesignIssuesScreen : GameScreen
    {
        private readonly Menu2 Window;
        private readonly Color Cream = Colors.Cream;
        private readonly Array<DesignIssueDetails> DesignIssues;
        private readonly ScrollList2<ShipDesignIssuesListItem> IssueList;
        private readonly Graphics.Font LargeFont = Fonts.Arial20Bold;

        public ShipDesignIssuesScreen(GameScreen screen, Empire player, Array<DesignIssueDetails> issues) : base(screen)
        {
            DesignIssues      = issues;
            IsPopup           = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;

            Window = Add(new Menu2(new Rectangle(ScreenWidth / 2 - 500, ScreenHeight / 2 - 300, 1000, 540)));
            int x  = (int)Window.X + 20;
            int y  = (int)Window.Y + 70;
            int w  = (int)Window.Width - 30;
            int h  = (int)Window.Height - 80;

            IssueList = Add(new ScrollList2<ShipDesignIssuesListItem>(x, y, w, h, 80));
            IssueList.EnableItemHighlight = true;
            //IssueList.DebugDrawScrollList = true;
            //IssueList.DebugDraw = true;

            UILabel designIssueLabel = Add(new UILabel("Design Issue", LargeFont, Cream));
            UILabel descriptionLabel = Add(new UILabel("Issue Description", LargeFont, Cream));
            UILabel remediationLabel = Add(new UILabel("Remediation", LargeFont, Cream));
            designIssueLabel.Size    = new Vector2(230, 20);
            descriptionLabel.Size    = new Vector2(370, 20);
            remediationLabel.Size    = new Vector2(370, 20);
            designIssueLabel.Pos     = new Vector2(x, y - 10);
            descriptionLabel.Pos     = new Vector2(x + 180, y - 10);
            remediationLabel.Pos     = new Vector2(x + 550, y - 10);
            designIssueLabel.TextAlign   = TextAlign.HorizontalCenter;
            descriptionLabel.TextAlign   = TextAlign.HorizontalCenter;
            remediationLabel.TextAlign   = TextAlign.HorizontalCenter;
        }

        void PopulateIssues()
        {
            foreach (DesignIssueDetails details in DesignIssues)
            {
                var d = new ShipDesignIssuesListItem(details);
                IssueList.AddItem(d);
            }

            IssueList.SortDescending(item => item.IssueDetails.Severity);
        }

        public override void LoadContent()
        {
            CloseButton(Window.Menu.Right - 40, Window.Menu.Y + 20);
            //Screen Title
            string title    = "Current Ship Issues";
            Vector2 menuPos = new Vector2(Window.Menu.CenterTextX(title, Fonts.Laserian14), Window.Menu.Y + 30);
            Label(menuPos, title, Fonts.Laserian14, Cream);
            PopulateIssues();
            base.LoadContent();
        }

         public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            base.Draw(batch, elapsed);
            batch.End();
        }

        public override bool HandleInput(InputState input)
        {
            if (input.KeyPressed(Keys.T) && !GlobalStats.TakingInput)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }
            if (input.Escaped || input.RightMouseClick)
            {
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }
    }
}