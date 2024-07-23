using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.GameScreens.Espionage;
using System;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Graphics;
using Ship_Game;
using SDGraphics;
using System.Drawing;
using Color = Microsoft.Xna.Framework.Graphics.Color;
using Font = Ship_Game.Graphics.Font;

namespace Ship_Game.GameScreens.EspionageNew
{
    public class EspionageLevelPanel : UIElementContainer
    {
        readonly InfiltrationScreen Screen;
        readonly Empire Player;
        readonly UIPanel LevelPanel;
        readonly UILabel Title, StatusLbl, Status, AvailableOps;
        readonly ProgressBar LevelProgress;
        readonly Font TitleFont, Font;
        readonly UIElementContainer LevelOps;
        int InfiltrationProgress;
        int LevelCost;
        readonly byte Level;
        bool ShowProgress;

        public EspionageLevelPanel(InfiltrationScreen screen, Empire player, in Rectangle rect, byte level)
            : base(rect)
        {
            Screen = screen;
            Player = player;
            Level = level;
            LevelPanel = Add(new UIPanel(rect, InfiltrationScreen.PanelBackground));

            string title = $"Infiltration Level {level}";

            Title     = Add(new UILabel(title, Fonts.Arial20Bold, Color.Wheat));
            StatusLbl = Add(new UILabel(GameText.InfiltrationStatus, Fonts.Arial12Bold, Color.Wheat));
            Status    = Add(new UILabel("", Fonts.Arial12Bold));
            AvailableOps  = Add(new UILabel(GameText.EspionageOperationsTitle, Fonts.Arial12Bold, Color.Wheat));
            LevelProgress = new ProgressBar(rect, 50, 0);
            LevelProgress.color = "green";
            TitleFont = screen.LowRes ? Fonts.Arial12Bold : Fonts.Arial20Bold;
            Font      = screen.LowRes ? Fonts.Arial8Bold : Fonts.Arial12Bold;
            Font Textfont = screen.LowRes? Fonts.Arial8Bold: Fonts.Arial12;
            int levelDescriptionY = rect.Y + 35;
            int passiveY = rect.Bottom - Font.LineSpacing*5;
            switch (level) 
            {
                default:
                case 1: LevelOps = Add(new InfiltrationOpsLevel1(screen, player, rect, levelDescriptionY, passiveY, Textfont)); break;
                case 2: LevelOps = Add(new InfiltrationOpsLevel2(screen, player, rect, levelDescriptionY, passiveY, Textfont)); break;
                case 3: LevelOps = Add(new InfiltrationOpsLevel3(screen, player, rect, levelDescriptionY, passiveY, Textfont)); break;
                case 4: LevelOps = Add(new InfiltrationOpsLevel4(screen, player, rect, levelDescriptionY, passiveY, Textfont)); break;
                case 5: LevelOps = Add(new InfiltrationOpsLevel5(screen, player, rect, levelDescriptionY, passiveY, Textfont)); break;
            }
        }

        public override void PerformLayout()
        {
            base.PerformLayout();
            Title.Pos        = new Vector2(HelperFunctions.GetMiddlePosForTitle(Title.Text.Text, TitleFont, Rect.Width, Rect.X), Rect.Y + 5);
            StatusLbl.Pos    = new Vector2(Rect.X + 5, Rect.Bottom - 20);
            Status.Pos       = new Vector2(Rect.X + 150, Rect.Bottom - 20);
            AvailableOps.Pos = new Vector2(HelperFunctions.GetMiddlePosForTitle(AvailableOps.Text.Text, Font, Rect.Width, Rect.X), Rect.Bottom - Font.LineSpacing*6 - 4);
            var levelRect    = new Rectangle(Rect.X + 5, Rect.Bottom - Font.LineSpacing * 7 - 15, Rect.Width - 10, 30);

            LevelProgress.SetRect(levelRect);
            RefreshEmpire();
        }

        public override void Update(float fixedDeltaTime)
        {
            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
            if (ShowProgress)
                LevelProgress.Draw(batch);

            int line1Y = Rect.Bottom - Font.LineSpacing - 8;
            int line2Y = Rect.Bottom - Font.LineSpacing*6 - 8;
            batch.DrawLine(new Vector2(Rect.X, line1Y), new Vector2(Rect.X + Rect.Width, line1Y), Color.Black, 3);
            batch.DrawLine(new Vector2(Rect.X, line2Y), new Vector2(Rect.X + Rect.Width, line2Y), Color.Black, 3);
            if (Status.Text == GameText.TerraformersInProgress)
                Status.Color = Screen.ApplyCurrentAlphaToColor(Status.Color);
            else
                Status.Color = Status.Color.Alpha(1);
        }

        public void RefreshEmpire()
        {
            if (Screen.SelectedEmpire.isPlayer)
                return;

            Ship_Game.Espionage espionage = Player.GetEspionage(Screen.SelectedEmpire);
            LevelProgress.Max = espionage.LevelCost(Level);
            if (espionage.Level >= Level)
                LevelProgress.Progress = LevelProgress.Max;
            else if (espionage.Level < Level - 1)
                LevelProgress.Progress = 0;
            else
                LevelProgress.Progress = espionage.LevelProgress;

            if (espionage.Level >= Level)
            {
                Color color = Player.EmpireColor;
                LevelPanel.Color = new Color((byte)(color.R * 0.15f), (byte)(color.G * 0.15f), (byte)(color.B * 0.15f));
            }
            else
            {
                LevelPanel.Color = InfiltrationScreen.PanelBackground;
            }

            Visible = Level <= espionage.Level + 1;
            if (Visible)
            {
                RefreshStatus(espionage);
                LevelOps.PerformLayout();
            }

            ShowProgress = espionage.Level < Level;
        }

        public void RefreshStatus(Ship_Game.Espionage espionage)
        {
            float pointPerTurn = espionage.GetProgressToIncrease(Player.EspionagePointsPerTurn, Player.CalcTotalEspionageWeight());
            if (espionage.Level >= Level)
            {
                Status.Text = espionage.LimitLevel >= Level ? GameText.InfiltrationStatusEstablished : GameText.Paused;
                Status.Color = espionage.LimitLevel >= Level ?  Color.LightGreen : Color.Gray;
            }
            else if (espionage.LevelProgress == 0 && pointPerTurn == 0 || espionage.Level < Level-1)
            {
                Status.Text = GameText.TerraformersNotStarted;
                Status.Color = Color.Gray;
            }
            else if (pointPerTurn > 0)
            {
                Status.Text = GameText.TerraformersInProgress;
                Status.Color = Color.Yellow;
            }
            else
            {
                Status.Text = GameText.InfiltrationStatusHalted;
                Status.Color = Screen.ApplyCurrentAlphaToColor(Color.Red);
            }

            float startPos = Rect.Right - Font.MeasureString(Status.Text.Text).X - 10;
            Status.Pos = new Vector2(startPos, Rect.Bottom - 20);
        }
    }
}
