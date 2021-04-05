using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    public sealed class UnexploredPlanetScreen : PlanetScreen
    {
        private Planet p;
        private Menu2 TitleBar;
        private Vector2 TitlePos;
        private Menu1 PlanetMenu;

        //private Rectangle titleRect;

        private Submenu PlanetInfo;
        private Rectangle PlanetIcon;


        public UnexploredPlanetScreen(GameScreen screen, Planet p) : base(screen)
        {
            this.p = p;
            IsPopup = true; // allow right-click dismiss
            Rectangle titleRect = new Rectangle(5, 44, 405, 80);
            if (LowRes)
            {
                titleRect.Width = 365;
            }
            TitleBar = new Menu2(titleRect);
            TitlePos = new Vector2(titleRect.X + titleRect.Width / 2 - Fonts.Laserian14.MeasureString(p.Name).X / 2f, titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            Rectangle leftRect = new Rectangle(5, titleRect.Y + titleRect.Height + 5, titleRect.Width, 
                ScreenHeight - (titleRect.Y + titleRect.Height) - (int)(0.4f * ScreenHeight));
            if (leftRect.Height < 350)
            {
                leftRect.Height = 350;
            }
            PlanetMenu = new Menu1(leftRect);
            Rectangle psubRect = new Rectangle(leftRect.X + 20, leftRect.Y + 20, leftRect.Width - 40, leftRect.Height - 40);
            PlanetInfo = new Submenu(psubRect);
            PlanetInfo.AddTab("Planet Info");
            PlanetIcon = new Rectangle(psubRect.X + psubRect.Width - 148, leftRect.Y + 55, 128, 128);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            TitleBar.Draw(batch, elapsed);
            float x = Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, state.Y);
            Color c = Colors.Cream;
            batch.DrawString(Fonts.Laserian14, p.Name, TitlePos, c);
            PlanetMenu.Draw(batch, elapsed);
            PlanetInfo.Draw(batch, elapsed);
            batch.Draw(p.PlanetTexture, PlanetIcon, Color.White);
            Vector2 PNameCursor = new Vector2(PlanetInfo.X + 20, PlanetInfo.Y + 45);
            batch.DrawString(Fonts.Arial20Bold, p.Name, PNameCursor, Colors.Cream);
            PNameCursor.Y = PNameCursor.Y + Fonts.Arial20Bold.LineSpacing * 2;
            float amount = 80f;
            batch.DrawString(Fonts.Arial12Bold, "Class:", PNameCursor, Color.Orange);
            Vector2 InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, p.LocalizedCategory, InfoCursor, Colors.Cream);
            if (!p.Habitable)
            {
                PNameCursor.Y = PNameCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                InfoCursor = new Vector2(PNameCursor.X + 80f, PNameCursor.Y);
                batch.DrawString(Fonts.Arial12Bold, "Uninhabitable", PNameCursor, Color.Orange);
            }
            else
            {
                PNameCursor.Y = PNameCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(385)+":", PNameCursor, Color.Orange);
                SpriteFont arial12Bold = Fonts.Arial12Bold;
                batch.DrawString(arial12Bold, p.PopulationStringForPlayer, InfoCursor, Colors.Cream);
                Rectangle hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(385)+":").X, Fonts.Arial12Bold.LineSpacing);
                if (hoverRect.HitTest(MousePos))
                {
                    ToolTip.CreateTooltip(75);
                }
                PNameCursor.Y = PNameCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(386)+":", PNameCursor, Color.Orange);
                batch.DrawString(Fonts.Arial12Bold, p.FertilityFor(EmpireManager.Player).String(), InfoCursor, Colors.Cream);
                hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(386)+":").X, Fonts.Arial12Bold.LineSpacing);
                if (hoverRect.HitTest(MousePos))
                {
                    ToolTip.CreateTooltip(20);
                }
                PNameCursor.Y = PNameCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(387)+":", PNameCursor, Color.Orange);
                batch.DrawString(Fonts.Arial12Bold, p.MineralRichness.String(), InfoCursor, Colors.Cream);
                hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(Localizer.Token(387)+":").X, Fonts.Arial12Bold.LineSpacing);
                if (hoverRect.HitTest(MousePos))
                {
                    ToolTip.CreateTooltip(21);
                }
            }
            PNameCursor.Y = PlanetIcon.Y + PlanetIcon.Height + 20;
            string desc = Fonts.Arial12Bold.ParseText(p.Description, PlanetInfo.Width - 40);
            batch.DrawString(Fonts.Arial12Bold, desc, PNameCursor, Colors.Cream);
        }
    }
}