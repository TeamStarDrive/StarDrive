using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.GameScreens.Espionage;
using System;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Graphics;
using Ship_Game;
using SDGraphics;

namespace Ship_Game.GameScreens.EspionageNew
{
    public class EspionageLevelPanel : UIElementContainer
    {
        readonly InfiltrationScreen Screen;
        readonly Empire Player;
        readonly UIPanel LevelPanel;
        readonly UILabel Title, StatusLbl, Status;
        readonly ProgressBar LevelProgress;
        readonly Font TitleFont, Font;
        readonly UIElementContainer LevelOps;
        int InfiltrationProgress;
        int LevelCost;
        readonly int Level;

        public EspionageLevelPanel(InfiltrationScreen screen, Empire player, in Rectangle rect, int level)
            : base(rect)
        {
            Screen = screen;
            Player = player;
            Level = level;
            LevelPanel = Add(new UIPanel(rect, InfiltrationScreen.PanelBackground));
            string title = $"Infiltration Level {level}";

            Title     = Add(new UILabel(title, Fonts.Arial20Bold, Color.White));
            StatusLbl = Add(new UILabel(GameText.InfiltrationStatus, Fonts.Arial12Bold, Color.White));
            Status    = Add(new UILabel("", Fonts.Arial12Bold));

            LevelProgress = new ProgressBar(rect, 50, 0);
            LevelProgress.color = "green";
            TitleFont = screen.LowRes ? Fonts.Arial12Bold : Fonts.Arial20Bold;

            Font = screen.LowRes ? Fonts.Arial8Bold : Fonts.Arial12Bold;
            switch (level) 
            {
                default:
                case 1: LevelOps = Add(new InfiltrationOpsLevel1(screen, player, rect)); break;
                case 2: LevelOps = Add(new InfiltrationOpsLevel2(screen, player, rect)); break;
                case 3: LevelOps = Add(new InfiltrationOpsLevel3(screen, player, rect)); break;
            }
        }

        public override void PerformLayout()
        {
            base.PerformLayout();
            Title.Pos = new Vector2(HelperFunctions.GetMiddlePosForTitle(Title.Text.Text, TitleFont, Rect.Width, Rect.X), Rect.Y + 35);
            StatusLbl.Pos = new Vector2(Rect.X + 5, Rect.Bottom - 20);
            Status.Pos    = new Vector2(Rect.X + 150, Rect.Bottom - 20);
            var levelRect = new Rectangle(Rect.X + 5, Rect.Y + 5, Rect.Width - 10, 30);
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
            LevelProgress.Draw(batch);
            if (Status.Text == GameText.TerraformersInProgress)
                Status.Color = Screen.ApplyCurrentAlphaToColor(Status.Color);
            else
                Status.Color = Status.Color.Alpha(1);
        }

        public void RefreshEmpire()
        {
            if (Screen.SelectedEmpire.isPlayer)
                return;

            Ship_Game.Espionage espionage = Player.GetRelations(Screen.SelectedEmpire).Espionage;
            LevelProgress.Max = espionage.LevelCost(Level);
            if (espionage.Level >= Level)
                LevelProgress.Progress = LevelProgress.Max;
            else if (espionage.Level < Level - 1)
                LevelProgress.Progress = 0;
            else
                LevelProgress.Progress = espionage.LevelProgress;

            if (espionage.Level >= Level)
                LevelPanel.Color = new Color((byte)(Player.EmpireColor.R * 0.15f), (byte)(Player.EmpireColor.G * 0.15f), (byte)(Player.EmpireColor.B * 0.15f));
            else
                LevelPanel.Color = InfiltrationScreen.PanelBackground;

            RefreshStatus(espionage);
        }

        public void RefreshStatus(Ship_Game.Espionage espionage)
        {
            float pointPerTurn = espionage.GetProgressToIncrease(Player.Research.TaxedResearch, Player.CalcTotalEspionageWeight());
            if (espionage.Level >= Level)
            {
                Status.Text = GameText.InfiltrationStatusEstablished;
                Status.Color = Color.LightGreen;
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
                Status.Color = Screen.ApplyCurrentAlphaToColor(Color.Pink);
            }

            float startPos = Rect.Right - Font.MeasureString(Status.Text.Text).X - 10;
            Status.Pos = new Vector2(startPos, Rect.Bottom - 20);
        }
    }
}
