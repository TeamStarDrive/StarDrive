using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Gameplay;
using Ship_Game.UI;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

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

        readonly Color ColorWar     = Color.Red;
        readonly Color ColorPeace   = Color.MediumPurple.Alpha(0.5f);
        readonly Color ColorNap     = Color.Yellow.Alpha(0.5f);
        readonly Color ColorTrade   = Color.DeepSkyBlue.Alpha(0.5f);
        readonly Color ColorBorders = Color.White.Alpha(0.75f);
        readonly Color ColorAlly    = Color.Green;

        readonly Array<EmpireAndIntelLevel> EmpiresAndIntel;
        readonly Graphics.Font LegendFont = Fonts.Arial14Bold;

        Empire Player => EmpireManager.Player;
        Empire SelectedEmpire;

        public RelationshipsDiagramScreen(GameScreen screen, Array<EmpireAndIntelLevel> empiresAndIntel)
            : base(screen, toPause: null)
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

            var list = AddList(new Vector2(Window.X + 25, Window.Y + 80), new Vector2(240, 400));
            void AddLegendItem(in LocalizedText text, Color color, float thickness)
            {
                var lb = new UILabel(text, LegendFont, color);
                var ln = new UILine(new Vector2(100, LegendFont.LineSpacing + 2), 0.8f, thickness, color);
                list.Add(new SplitElement(lb, ln));
            }
            AddLegendItem(GameText.PeaceTreaty, ColorPeace, 1f);
            AddLegendItem(GameText.NonaggressionPact3, ColorNap, 1f);
            AddLegendItem(GameText.TradeTreaty, ColorTrade, 1f);
            AddLegendItem(GameText.OpenBordersTreaty2, ColorBorders, 1f);
            AddLegendItem(GameText.Alliance, ColorAlly, 3f);
            AddLegendItem(GameText.AtWar, ColorWar, 3f);

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

        void DrawRelations(SpriteBatch batch)
        {
            Peer[] knownPeers = Peers.Filter(p => p.IntelLevel > 0);
            if (!ViewOnlyWarsOrAllies)
            {
                foreach (Peer us in knownPeers)
                    foreach (Peer peer in Peers)
                        if (ShowPeer(us.Empire, peer.Empire)) 
                            DrawPeerLinesNoWarOrAlliance(batch, us, peer);
            }

            // Drawing War/Alliance on top of all other lines
            foreach (Peer us in knownPeers)
                foreach (Peer peer in Peers)
                    if (ShowPeer(us.Empire, peer.Empire))
                        DrawPeerLinesWarOrAlliance(batch, us, peer);

            foreach (Peer empire in Peers)
            {
                batch.Draw(empire.Portrait, empire.Rect);
                batch.DrawRectangle(empire.Rect, Player.IsKnown(empire.Empire) || empire.Empire.isPlayer ? empire.Empire.EmpireColor : Color.Gray, 
                    SelectedEmpire == empire.Empire ? 3 : 1);
            }
        }

        bool ShowPeer(Empire us, Empire peer)
        {
            return us != peer && Player.IsKnown(peer)
                              && (SelectedEmpire == null || SelectedEmpire == us || SelectedEmpire == peer);
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