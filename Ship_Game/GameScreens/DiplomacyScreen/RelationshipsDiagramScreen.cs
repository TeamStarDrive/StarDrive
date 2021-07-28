using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

namespace Ship_Game.GameScreens.DiplomacyScreen
{
    public sealed class RelationshipsDiagramScreen : GameScreen
    {
        private readonly Menu2 Window;
        readonly Array<Peer> Peers = new Array<Peer>();
        readonly Vector2 WeightCenter; // Offset from window center for circle of empires
        bool ViewTradeTreaties    = true;
        bool ViewOnlyWarsOrAllies = false;
        UICheckBox ViewTradeTreatiesCheckBox;
        UICheckBox ViewWarsOrAlliesCheckBox;
        UILabel Title;
        UILabel LegendWar;
        UILabel LegendPeace;
        UILabel LegendNap;
        UILabel LegendTrade;
        UILabel LegendBorders;
        UILabel LegendAlly;

        readonly Color ColorWar     = Color.Red;
        readonly Color ColorPeace   = Color.White.Alpha(0.5f);
        readonly Color ColorNap     = Color.Yellow.Alpha(0.5f);
        readonly Color ColorTrade   = Color.DeepSkyBlue.Alpha(0.5f);
        readonly Color ColorBorders = Color.White.Alpha(0.75f);
        readonly Color ColorAlly    = Color.Green;

        readonly Array<EmpireAndIntelLevel> EmpiresAndIntel;
        readonly Graphics.Font LegendFont = Fonts.Arial14Bold;

        Empire Player => EmpireManager.Player;
        Empire SelectedEmpire;

        public RelationshipsDiagramScreen(GameScreen screen, Array<EmpireAndIntelLevel> empiresAndIntel) : base(screen)
        {
            IsPopup           = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;

            Rectangle diagramRect = new Rectangle(ScreenWidth / 2 - 500, ScreenHeight / 2 - 384, 1000, 768);
            Window                = Add(new Menu2(diagramRect));
            WeightCenter          = new Vector2(Window.X + Window.Width / 2 + 100, Window.Y + Window.Height / 2);
            EmpiresAndIntel       = empiresAndIntel;
            AddPeers();
        }

        public override void LoadContent()
        {
            CloseButton(Window.Menu.Right - 40, Window.Menu.Y + 20);
            Title = Add(new UILabel(GameText.EmpireRelationships, Fonts.Arial20Bold, Color.Wheat));
            ViewWarsOrAlliesCheckBox  = Add(new UICheckBox(() => ViewOnlyWarsOrAllies, LegendFont,
                title: GameText.ViewWarsOrAlliancesName, tooltip: GameText.ViewWarsOrAlliancesTip));
            ViewTradeTreatiesCheckBox = Add(new UICheckBox(() => ViewTradeTreaties, LegendFont, 
                title: GameText.ViewTradeTreatiesName, tooltip: GameText.ViewTradeTreatiesTip));

            ViewWarsOrAlliesCheckBox.TextColor  = Color.Gray;
            ViewTradeTreatiesCheckBox.TextColor = Color.Gray;
            ViewWarsOrAlliesCheckBox.CheckedTextColor  = Color.White;
            ViewTradeTreatiesCheckBox.CheckedTextColor = Color.White;
            Vector2 legendPos = new Vector2(Window.X + 25, Window.Y + 100);
            LegendPeace = Add(new UILabel(legendPos, GameText.PeaceTreaty, LegendFont, Color.White));
            legendPos.Y += LegendFont.LineSpacing + 2;
            LegendNap = Add(new UILabel(legendPos, GameText.NonaggressionPact3, LegendFont, Color.White));
            legendPos.Y += LegendFont.LineSpacing + 2;
            LegendTrade = Add(new UILabel(legendPos, GameText.TradeTreaty, LegendFont, Color.White));
            legendPos.Y += LegendFont.LineSpacing + 2;
            LegendBorders = Add(new UILabel(legendPos, GameText.OpenBordersTreaty2, LegendFont, Color.White));
            legendPos.Y += LegendFont.LineSpacing + 2;
            LegendAlly = Add(new UILabel(legendPos, GameText.Alliance, LegendFont, Color.White));
            legendPos.Y += LegendFont.LineSpacing + 2;
            LegendWar = Add(new UILabel(legendPos, GameText.AtWar, LegendFont, Color.White));
            legendPos.Y += LegendFont.LineSpacing + 2;
            base.LoadContent();
        }

        public override void PerformLayout()
        {
            Title.Pos = new Vector2(Window.X + 25, Window.Y + 30);
            ViewTradeTreatiesCheckBox.Pos = new Vector2(Window.X + 25, Window.Bottom - 75);
            ViewWarsOrAlliesCheckBox.Pos  = new Vector2(Window.X + 25, Window.Bottom - 50);
        }

        void AddPeers()
        {
            int angle = 360 / EmpiresAndIntel.Count;
            int peerAngle = 0;
            foreach (EmpireAndIntelLevel empireAndIntelLevel in EmpiresAndIntel)
            {
                Peer peer = new Peer(WeightCenter, Window.Rect, peerAngle, empireAndIntelLevel);
                Peers.Add(peer);
                peerAngle += angle;
            }
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            base.Draw(batch, elapsed); // window
            DrawRelations(batch); // links and then portraits
            DrawLegendLines(batch);
            batch.End();
        }

        public override bool HandleInput(InputState input)
        {
            HandleSelectedEmpire(input);
            if (input.Escaped || input.RightMouseClick)
            {
                ExitScreen();
                return true;
            }

            return base.HandleInput(input);
        }

        void HandleSelectedEmpire(InputState input)
        {
            SelectedEmpire = null;
            foreach (Peer peer in Peers)
            {
                if (peer.Rect.HitTest(input.CursorPosition))
                {
                    SelectedEmpire = peer.Empire;
                    break;
                }
            }
        }

        void DrawLegendLines(SpriteBatch batch)
        {
            int offset = 7;
            DrawLegendLine(LegendPeace.Y+offset,  ColorPeace, 1);
            DrawLegendLine(LegendNap.Y+offset, ColorNap, 1);
            DrawLegendLine(LegendTrade.Y+offset, ColorTrade, 1);
            DrawLegendLine(LegendBorders.Y+offset, ColorBorders, 1);
            DrawLegendLine(LegendAlly.Y+offset, ColorAlly, 3);
            DrawLegendLine(LegendWar.Y+offset, ColorWar, 3);

            // Local Method
            void DrawLegendLine(float y, Color color, float thickness)
            {
                Vector2 point1 = new Vector2(Window.X + 170, y);
                Vector2 point2 = new Vector2(Window.X + 250, y);
                batch.DrawLine(point1, point2, color, thickness);
            }
        }

        void DrawRelations(SpriteBatch batch)
        {
            if (!ViewOnlyWarsOrAllies)
            {
                foreach (Peer us in Peers.Filter(p => p.IntelLevel > 0))
                    foreach (Peer peer in Peers)
                        if (ShowPeer(us.Empire, peer.Empire)) 
                            DrawPeerLinesNoWarOrAlliance(batch, us, peer);
            }

            // Drawing War/Alliance on top of all other lines
            foreach (Peer us in Peers.Filter(p => p.IntelLevel > 0))
                foreach (Peer peer in Peers)
                    if (ShowPeer(us.Empire, peer.Empire))
                        DrawPeerLinesWarOrAlliance(batch, us, peer);

            foreach (Peer empire in Peers)
            {
                batch.Draw(empire.Portrait, empire.Rect);
                batch.DrawRectangle(empire.Rect, Player.IsKnown(empire.Empire) ? empire.Empire.EmpireColor : Color.Gray, 
                    SelectedEmpire == empire.Empire ? 3 : 1);
            }
        }

        bool ShowPeer(Empire us, Empire peer)
        {
            return us != peer && Player.IsKnown(peer)
                              && (SelectedEmpire == null
                                  || SelectedEmpire != null && (SelectedEmpire == us || SelectedEmpire == peer));
        }

        void DrawPeerLinesNoWarOrAlliance(SpriteBatch batch, Peer us, Peer peer)
        {
            Relationship rel = us.Empire.GetRelationsOrNull(peer.Empire);
            if (rel == null || rel.AtWar || rel.Treaty_Alliance)
                return;

            if (rel.Treaty_Peace)
            {
                DrawPeerLine(batch, us.LinkPos, peer.LinkPos, ColorPeace); 
                return;
            }

            if (rel.Treaty_OpenBorders)
                DrawPeerLine(batch, us.LinkPos, peer.LinkPos, ColorBorders);
            else if (rel.Treaty_NAPact)
                DrawPeerLine(batch, us.LinkPos, peer.LinkPos, ColorNap);

            if (ViewTradeTreaties && rel.Treaty_Trade)
                DrawPeerLine(batch, us.TradePos, peer.TradePos, ColorTrade);
        }

        void DrawPeerLinesWarOrAlliance(SpriteBatch batch, Peer source, Peer peer)
        {
            Relationship rel = source.Empire.GetRelationsOrNull(peer.Empire);
            if (rel == null)
                return;

            if (rel.AtWar)
                DrawPeerLine(batch, source.LinkPos, peer.LinkPos, ColorWar, thickness: 3);
            else if (rel.Treaty_Alliance)
                DrawPeerLine(batch, source.LinkPos, peer.LinkPos, ColorAlly, thickness: 3);
        }

        void DrawPeerLine(SpriteBatch batch, Vector2 pos1, Vector2 pos2, Color color, int thickness = 1)
        {
            batch.DrawLine(pos1, pos2, color, thickness);
        }

        readonly struct Peer
        {
            public readonly Rectangle Rect;
            public readonly int IntelLevel;
            public readonly Vector2 LinkPos;
            public readonly Vector2 TradePos;
            public readonly Empire Empire;
            public readonly SubTexture Portrait;

            public Peer(Vector2 weightedCenter, Rectangle window, int angle, EmpireAndIntelLevel empireAndIntel)
            {
                Vector2 center = weightedCenter.PointFromAngle(angle, window.Height/2f - 80);
                Rect       = new Rectangle((int)center.X - 47, (int)center.Y - 55, 94, 111);
                Empire     = empireAndIntel.Empire;
                IntelLevel = empireAndIntel.IntelLevel;
                LinkPos    = center.PointFromAngle(180 + angle, 45);
                TradePos   = center.PointFromAngle(170 + angle, 45);
                Portrait = EmpireManager.Player.IsKnown(empireAndIntel.Empire) || empireAndIntel.Empire.isPlayer
                            ? ResourceManager.Texture("Portraits/" + Empire.data.PortraitName)
                            : ResourceManager.Texture("Portraits/unknown");
            }
        }
    }
}