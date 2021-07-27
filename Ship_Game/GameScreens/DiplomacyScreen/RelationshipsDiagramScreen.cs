using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;
using Ship_Game.Gameplay;


namespace Ship_Game.GameScreens.DiplomacyScreen
{
    public sealed class RelationshipsDiagramScreen : GameScreen
    {
        private readonly Menu2 Window;
        private readonly Color Cream = Colors.Cream;
        private readonly Graphics.Font LargeFont = Fonts.Arial20Bold;

        Array<Peer> Peers = new Array<Peer>();
        Vector2 WeightCenter;
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

            WeightCenter = new Vector2(Window.X + Window.Width / 2 + 100, Window.Y + Window.Height / 2);
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
                Peer peer = new Peer(WeightCenter, Window.Rect, peerAngle, e);
                Peers.Add(peer);
                //Add(new UIPanel(peer.Rect, peer.Portrait));
                peerAngle += angle;
            }
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            base.Draw(batch, elapsed);
            DrawRelations(batch);
            batch.End();
        }

        public override bool HandleInput(InputState input)
        {
            if (input.Escaped || input.RightMouseClick)
            {
                ExitScreen();
                return true;
            }

            return base.HandleInput(input);
        }

        void DrawRelations(SpriteBatch batch)
        {
            foreach (Peer empire in Peers)
            {
                foreach (Peer peer in Peers)
                {
                    if (empire.Empire != peer.Empire)
                        DrawPeerLinesNoWarOrAlliance(batch, empire, peer);
                }
            }

            foreach (Peer empire in Peers)
            {
                foreach (Peer peer in Peers)
                {
                    if (empire.Empire != peer.Empire)
                        DrawPeerLinesWarOrAlliance(batch, empire, peer);
                }
            }

            foreach (Peer empire in Peers)
            {
                batch.Draw(empire.Portrait, empire.Rect);
            }
        }

        void DrawPeerLinesNoWarOrAlliance(SpriteBatch batch, Peer source, Peer peer)
        {
            Relationship rel = source.Empire.GetRelationsOrNull(peer.Empire);
            if (rel == null || rel.AtWar || rel.Treaty_Alliance)
                return;

            if (rel.Treaty_Peace)
            {
                DrawPeerLine(batch, source.PeacePos, peer.PeacePos, Color.Gray); 
                return;
            }

            if (rel.Treaty_OpenBorders)
                DrawPeerLine(batch, source.OpenBordersPos, peer.OpenBordersPos, Color.LightSeaGreen);
            else if (rel.Treaty_Trade)
                DrawPeerLine(batch, source.TradePos, peer.TradePos, Color.DeepSkyBlue);
            else if (rel.Treaty_NAPact)
                DrawPeerLine(batch, source.NapPos, peer.NapPos, Color.White);
        }

        void DrawPeerLinesWarOrAlliance(SpriteBatch batch, Peer source, Peer peer)
        {
            Relationship rel = source.Empire.GetRelationsOrNull(peer.Empire);
            if (rel == null)
                return;

            if (rel.AtWar)
                DrawPeerLine(batch, source.WarPos, peer.WarPos, Color.Red, thickness: 3);
            else if (rel.Treaty_Alliance)
                DrawPeerLine(batch, source.AlliancePos, peer.AlliancePos, Color.Green, thickness: 3);
        }

        void DrawPeerLine(SpriteBatch batch, Vector2 pos1, Vector2 pos2, Color color, int thickness = 1)
        {
            batch.DrawLine(pos1, pos2, color.Alpha(0.75f), thickness);
        }

        struct Peer
        {
            public readonly Rectangle Rect;
            //int Angle;

            public Vector2 PeacePos; // Yellow
            public Vector2 WarPos; // Red
            public Vector2 NapPos; // White
            public Vector2 TradePos; //  Blue
            public Vector2 OpenBordersPos; // Light Green
            public Vector2 AlliancePos; // Green
            public readonly Empire Empire;
            public readonly SubTexture Portrait;
            Vector2 Center;

            public Peer(Vector2 center, Rectangle window, int angle, Empire e)
            {
                Center   = center.PointFromAngle(angle, window.Height/2f - 80);
                Rect     = new Rectangle((int)Center.X - 47, (int)Center.Y - 55, 94, 111);
                Empire   = e;
                Portrait = ResourceManager.Texture("Portraits/" + Empire.data.PortraitName);
                WarPos   = PeacePos = Center.PointFromAngle(180 + angle, 42);
                NapPos   = Center.PointFromAngle(180 + angle, 42); //Center.PointFromAngle(0, 10);
                TradePos = Center.PointFromAngle(180 + angle, 42);
                OpenBordersPos = Center.PointFromAngle(180 + angle, 42); ; //Center.PointFromAngle(180, 10);
                AlliancePos = Center.PointFromAngle(180 + angle, 42); ; //Center.PointFromAngle(270, 10);
            }
        }
    }
}