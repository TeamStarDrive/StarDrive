using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;
using Ship_Game.GameScreens.ShipDesign.DesignIssues;

namespace Ship_Game.GameScreens.ShipDesignScreen
{
    public sealed class ShipDesignWarningScreen : GameScreen
    {
        private readonly Empire Player;
        private readonly Menu2 Window;
        private readonly Color TitleColor;
        private readonly Array<DesignIssueDetails> DesignIssues;
        private readonly ScrollList2<ShipDesignIssuesListItem> IssueList;

        public ShipDesignWarningScreen(GameScreen screen, Empire player, Array<DesignIssueDetails> issues, Color color) : base(screen)
        {
            Player            = player;
            TitleColor        = color;
            DesignIssues      = issues;
            IsPopup           = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;

            Window    = Add(new Menu2(new Rectangle(ScreenWidth / 2 - 500, ScreenHeight / 2 - 300, 1000, 600)));

            int x = (int)Window.X + 20;
            int y = (int)Window.Y + 60;
            int w = (int)Window.Width - 30;
            int h = (int)Window.Height - 80;

            IssueList = Add(new ScrollList2<ShipDesignIssuesListItem>(x, y, w, h, 80));
            IssueList.EnableItemHighlight = true;
            //IssueList.DebugDrawScrollList = true;
            //IssueList.DebugDraw = true;
        }

        void PopulateIssues()
        {
            foreach (DesignIssueDetails details in DesignIssues)
            {
                var d = new ShipDesignIssuesListItem(details);
                IssueList.AddItem(d);
            }
        }

        public override void LoadContent()
        {
            CloseButton(Window.Menu.Right - 40, Window.Menu.Y + 20);
            //Screen Title
            string title = "Current Ship Issues";
            Label(Window.Menu.CenterTextX(title, Fonts.Laserian14), Window.Menu.Y + 30, title, Fonts.Laserian14);
            PopulateIssues();
            base.LoadContent();
        }

         public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            base.Draw(batch);
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