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

        private bool LowRes;

        private Submenu PlanetInfo;

        private Rectangle PlanetIcon;

        private MouseState currentMouse;

        private MouseState previousMouse;

        public UnexploredPlanetScreen(GameScreen screen, Planet p) : base(screen)
        {
            this.p = p;
            if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280)
            {
                LowRes = true;
            }
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

        public override void Draw(SpriteBatch batch)
        {
            TitleBar.Draw(batch);
            float x = Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, state.Y);
            Color c = new Color(255, 239, 208);
            batch.DrawString(Fonts.Laserian14, p.Name, TitlePos, c);
            PlanetMenu.Draw();
            PlanetInfo.Draw(batch);
            batch.Draw(p.PlanetTexture, PlanetIcon, Color.White);
            Vector2 PNameCursor = new Vector2(PlanetInfo.Menu.X + 20, PlanetInfo.Menu.Y + 45);
            batch.DrawString(Fonts.Arial20Bold, p.Name, PNameCursor, new Color(255, 239, 208));
            PNameCursor.Y = PNameCursor.Y + Fonts.Arial20Bold.LineSpacing * 2;
            float amount = 80f;
            if (GlobalStats.IsGerman)
            {
                amount = amount + 25f;
            }
            batch.DrawString(Fonts.Arial12Bold, "Class:", PNameCursor, Color.Orange);
            Vector2 InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
            batch.DrawString(Fonts.Arial12Bold, p.LocalizedCategory, InfoCursor, new Color(255, 239, 208));
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
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(385), ":"), PNameCursor, Color.Orange);
                SpriteBatch spriteBatch1 = ScreenManager.SpriteBatch;
                SpriteFont arial12Bold = Fonts.Arial12Bold;
                spriteBatch1.DrawString(arial12Bold, p.PopulationString, InfoCursor, new Color(255, 239, 208));
                Rectangle hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(385), ":")).X, Fonts.Arial12Bold.LineSpacing);
                if (hoverRect.HitTest(MousePos))
                {
                    ToolTip.CreateTooltip(75);
                }
                PNameCursor.Y = PNameCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(386), ":"), PNameCursor, Color.Orange);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, p.Fertility(EmpireManager.Player).String(), InfoCursor, new Color(255, 239, 208));
                hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(386), ":")).X, Fonts.Arial12Bold.LineSpacing);
                if (hoverRect.HitTest(MousePos))
                {
                    ToolTip.CreateTooltip(20);
                }
                PNameCursor.Y = PNameCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                InfoCursor = new Vector2(PNameCursor.X + amount, PNameCursor.Y);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(387), ":"), PNameCursor, Color.Orange);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, p.MineralRichness.String(), InfoCursor, new Color(255, 239, 208));
                hoverRect = new Rectangle((int)PNameCursor.X, (int)PNameCursor.Y, (int)Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(387), ":")).X, Fonts.Arial12Bold.LineSpacing);
                if (hoverRect.HitTest(MousePos))
                {
                    ToolTip.CreateTooltip(21);
                }
            }
            PNameCursor.Y = PlanetIcon.Y + PlanetIcon.Height + 20;
            string desc = parseText(p.Description, PlanetInfo.Menu.Width - 40);
            batch.DrawString(Fonts.Arial12Bold, desc, PNameCursor, new Color(255, 239, 208));
            /*if (this.p.Special != "None")     //This was removed, because the string "Special" was never assigned a valus other than "None" -Gretman
            {
                PNameCursor.Y = PNameCursor.Y + (Fonts.Arial12Bold.MeasureString(desc).Y + 10f);
                string special = this.p.Special;
                string str1 = special;
                if (special != null)
                {
                    if (str1 == "Gold Deposits")
                    {
                        d = this.parseText("This planet has extensive gold deposits and would produce +5 credits per turn if colonized.", (float)(this.PlanetInfo.Menu.Width - 40));
                        spriteBatch.DrawString(Fonts.Arial12Bold, d, PNameCursor, new Color(255, 239, 208));
                        return;
                    }
                    if (str1 == "Platinum Deposits")
                    {
                        d = this.parseText("This planet has extensive platinum deposits and would produce +10 credits per turn if colonized.", (float)(this.PlanetInfo.Menu.Width - 40));
                        spriteBatch.DrawString(Fonts.Arial12Bold, d, PNameCursor, new Color(255, 239, 208));
                        return;
                    }
                    if (str1 == "Artifacts")
                    {
                        d = this.parseText("This planet has extensive archaeological curosities, and would provide +2 research points per turn if colonized.", (float)(this.PlanetInfo.Menu.Width - 40));
                        spriteBatch.DrawString(Fonts.Arial12Bold, d, PNameCursor, new Color(255, 239, 208));
                        return;
                    }
                    if (str1 == "Ancient Machinery")
                    {
                        d = this.parseText("This planet has a cache of ancient but functional alien machinery, and would reap +2 production per turn if colonized.", (float)(this.PlanetInfo.Menu.Width - 40));
                        spriteBatch.DrawString(Fonts.Arial12Bold, d, PNameCursor, new Color(255, 239, 208));
                        return;
                    }
                    if (str1 != "Spice")
                    {
                        return;
                    }
                    d = this.parseText("The native creatures of this planet secrete an incredible spice-like element with brain-enhancing properties.  If colonized, this planet would produce +5 research per turn", (float)(this.PlanetInfo.Menu.Width - 40));
                    spriteBatch.DrawString(Fonts.Arial12Bold, d, PNameCursor, new Color(255, 239, 208));
                }
            }*/
        }

        public override bool HandleInput(InputState input)
        {
            currentMouse = Mouse.GetState();
            previousMouse = Mouse.GetState();
            return base.HandleInput(input);
        }

        private string parseText(string text, float Width)
        {
            string line = string.Empty;
            string returnString = string.Empty;
            string[] strArrays = text.Split(' ');
            for (int i = 0; i < strArrays.Length; i++)
            {
                string word = strArrays[i];
                if (Fonts.Arial12Bold.MeasureString(string.Concat(line, word)).Length() > Width)
                {
                    returnString = string.Concat(returnString, line, '\n');
                    line = string.Empty;
                }
                line = string.Concat(line, word, ' ');
            }
            return string.Concat(returnString, line);
        }
    }
}