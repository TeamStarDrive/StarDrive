using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.UI;
using System;
using System.Linq;
using Ship_Game.GameScreens.ShipDesign;

namespace Ship_Game.GameScreens.ShipDesignScreen
{
    public sealed class ShipDesignWarningScreen : GameScreen
    {
        readonly Empire Player;
        Menu2 Window;
        private readonly Color TitleColor;
        private readonly Array<DesignIssueDetails> DesignIssues;

        private ScrollList2<ShipDesignIssuesListItem> IssueList;

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
            Label(Window.Menu.CenterTextX(title, Fonts.Pirulen16), Window.Menu.Y + 20, title, Fonts.Pirulen16);
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

    public enum WarningLevel
    {
        None,
        Minor,
        Major,
        Critical
    }

    public struct DesignIssueDetails
    {
        public readonly DesignIssueType Type;
        public readonly WarningLevel Severity;
        public readonly Color Color;
        public readonly string Title;
        public readonly string Problem;
        public readonly string Remediation;
        public readonly SubTexture Texture;

        public DesignIssueDetails(DesignIssueType issueType)
        {
            Type = issueType;
            switch (issueType)
            {
                default:
                case DesignIssueType.NoCommand: 
                    Severity    = WarningLevel.Critical; 
                    Color       = Color.Red;
                    Title       = "Command Module Missing";
                    Problem     = "You Ship does not have a power command module, like a Cockpit or a bridge";
                    Remediation = "Add a command module to your ship (Cockpit, Bridge, etc.) amd make sure it is powered";
                    Texture     = ResourceManager.Texture("NewUI/IssueNoCommand");
                    break;
            }
        }
    }

    public enum DesignIssueType
    {
        NoCommand
    }
}