using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;


namespace Ship_Game.GameScreens.DiplomacyScreen
{
    public sealed class RelationshipsDiagramScreen : GameScreen
    {
        private readonly Menu2 Window;
        private readonly Color Cream = Colors.Cream;
        private readonly Graphics.Font LargeFont = Fonts.Arial20Bold;
        Vector2 Center;
        Array<Peer> Peers = new Array<Peer>();

        public RelationshipsDiagramScreen(GameScreen screen, Rectangle rect) : base(screen)
        {
            IsPopup           = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;

            Rectangle diagramRect = new Rectangle(ScreenWidth / 2 - 500, ScreenHeight / 2 - 384, 1000, 768);
            Window = Add(new Menu2(diagramRect));
            int x  = (int)Window.X + 20;
            int y  = (int)Window.Y + 70;
            int w  = (int)Window.Width - 30;
            int h  = (int)Window.Height - 80;

            Center = new Vector2(Window.X + Window.Width / 2 + 100, Window.Y + Window.Height / 2);

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

        public override void LoadContent()
        {
            CloseButton(Window.Menu.Right - 40, Window.Menu.Y + 20);
            //Screen Title
            //string title    = "Current Ship Issues";
            //Vector2 menuPos = new Vector2(Window.Menu.CenterTextX(title, Fonts.Laserian14), Window.Menu.Y + 30);
            //Label(menuPos, title, Fonts.Laserian14, Cream);
            AddPeers();
            base.LoadContent();
        }

        void AddPeers()
        {
            int angle = 360 / EmpireManager.ActiveMajorEmpires.Length;
            int peerAngle = 0;
            foreach (Empire e in EmpireManager.ActiveMajorEmpires)
            {
                // todo add empires based on player knowledge on relations
                Peer peer = new Peer(Center, Window.Rect, peerAngle, e);
                Peers.Add(peer);
                Add(new UIPanel(peer.Rect, peer.Portrait));
                peerAngle += angle;
            }
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
            /*
            if (input.KeyPressed(Keys.T) && !GlobalStats.TakingInput)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }*/
            if (input.Escaped || input.RightMouseClick)
            {
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }

        struct Peer
        {
            public Rectangle Rect;
            //int Angle;

            public Vector2 PeacePos; // Gray
            public Vector2 WarPos; // Red
            public Vector2 NapPos; // Yellow
            public Vector2 TradePos; //  Blue
            public Vector2 OpenBordersPos; // Light Green
            public Vector2 AlliancePos; // Green
            public Empire Empire;
            public SubTexture Portrait;
            Vector2 Center;

            public Peer(Vector2 windowCenter, Rectangle window, int angle, Empire e)
            {
                Center   = windowCenter.PointFromAngle(angle, window.Height/2f - 80);
                Rect     = new Rectangle((int)Center.X - 47, (int)Center.Y - 55, 94, 111);
                Empire   = e;
                Portrait = ResourceManager.Texture("Portraits/" + Empire.data.PortraitName);
                WarPos   =  PeacePos = Center;
                NapPos   = Center.PointFromAngle(0, 45);
                TradePos = Center.PointFromAngle(90, 45);
                OpenBordersPos = Center.PointFromAngle(180, 45);
                AlliancePos    = Center.PointFromAngle(270, 45);
            }
        }
    }
}